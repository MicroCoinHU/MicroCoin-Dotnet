﻿//-----------------------------------------------------------------------
// This file is part of MicroCoin - The first hungarian cryptocurrency
// Copyright (c) 2019 Peter Nemeth
// ECKeyPair.cs - Copyright (c) 2019 Németh Péter
//-----------------------------------------------------------------------
// MicroCoin is free software: you can redistribute it and/or modify
// it under the terms of the GNU General Public License as published by
// the Free Software Foundation, either version 3 of the License, or
// (at your option) any later version.
//
// MicroCoin is distributed in the hope that it will be useful,
// but WITHOUT ANY WARRANTY; without even the implied warranty of
// MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the 
// GNU General Public License for more details.
//-----------------------------------------------------------------------
// You should have received a copy of the GNU General Public License
// along with MicroCoin. If not, see <http://www.gnu.org/licenses/>.
//-----------------------------------------------------------------------
using MicroCoin.Types;
using Org.BouncyCastle.Asn1.Sec;
using Org.BouncyCastle.Asn1.X9;
using Org.BouncyCastle.Crypto;
using Org.BouncyCastle.Crypto.Generators;
using Org.BouncyCastle.Crypto.Parameters;
using Org.BouncyCastle.Math;
using Org.BouncyCastle.Security;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;

namespace MicroCoin.Cryptography
{

    public class ECKeyPair : KeyPair, IEquatable<ECKeyPair>
    {        
        public BigInteger PrivateKey
        {
            get => D == null ? BigInteger.Zero : new BigInteger(D);
            set => D = value.ToByteArray();
        }
        
        public ByteString Name { get; set; }

        private ECParameters? _eCParameters;
        private static readonly Dictionary<ECCurveType, ECDsa> ecdsas = new Dictionary<ECCurveType, ECDsa>(5);
        public ECParameters ECParameters
        {
            get
            {
                if (_eCParameters != null) return _eCParameters.Value;
                ECCurve curve = ECCurve.CreateFromFriendlyName(CurveType.ToString().ToLower());
                ECParameters parameters = new ECParameters
                {
                    Curve = curve,
                    Q = PublicKey
                };
                if (D != null)
                {
                    if (D.Length < PublicKey.X.Length)
                    {
                        Span<byte> b = stackalloc byte[PublicKey.X.Length];
                        b.Fill(0);
                        D.CopyTo(b.Slice(b.Length - D.Length));
                        parameters.D = b.ToArray();
                    }
                    else
                    {
                        parameters.D = D;
                    }
                }
                parameters.Validate();
                _eCParameters = parameters;
                return _eCParameters.Value;
            }
        }

        public static ECKeyPair Import(string hex)
        {
            ECKeyPair keyPair = new ECKeyPair
            {
                CurveType = ECCurveType.Secp256K1
            };
            var privKeyInt = new BigInteger(+1, (Hash)hex);
            var parameters = SecNamedCurves.GetByName("secp256k1");
            var ecPoint = parameters.G.Multiply(privKeyInt);
            keyPair.D = privKeyInt.ToByteArrayUnsigned();
            var x = ecPoint.Normalize().XCoord.ToBigInteger().ToByteArrayUnsigned();
            var y = ecPoint.Normalize().YCoord.ToBigInteger().ToByteArrayUnsigned();
            ECPoint PublicKey = new ECPoint
            {
                X = x,
                Y = y
            };
            keyPair.PublicKey = PublicKey;
            return keyPair;
        }

        public static implicit operator ECParameters(ECKeyPair keyPair)
        {
            return keyPair.ECParameters;
        }

        public void SaveToStream(Stream s, in bool writeLength = true, in bool writePrivateKey = false,
            in bool writeName = false)
        {
            using (BinaryWriter bw = new BinaryWriter(s, Encoding.ASCII, true))
            {
                ushort xLen = (ushort)PublicKey.X.Length;
                ushort yLen = (ushort)PublicKey.Y.Length;
                if (xLen > 0 && yLen > 0)
                {
                    if (PublicKey.X[0] == 0) xLen--;
                    if (PublicKey.Y[0] == 0) yLen--;
                }
                var len = xLen + yLen + 6;
                if (writeName) Name.SaveToStream(bw);
                if (writeLength) bw.Write((ushort)len);
                bw.Write((ushort)CurveType);
                if (CurveType == ECCurveType.Empty)
                {
                    bw.Write((ushort)0);
                    bw.Write((ushort)0);
                    return;
                }

                bw.Write(xLen);
                bw.Write(PublicKey.X, PublicKey.X[0] == 0 ? 1 : 0, PublicKey.X.Length - (PublicKey.X[0] == 0 ? 1 : 0));
                if (CurveType == ECCurveType.Sect283K1)
                {
                    bw.Write(yLen);
                    bw.Write(PublicKey.Y, PublicKey.Y[0] == 0 ? 1 : 0, PublicKey.Y.Length - (PublicKey.Y[0] == 0 ? 1 : 0));
                }
                else
                {
                    bw.Write(yLen);
                    bw.Write(PublicKey.Y, PublicKey.Y[0] == 0 ? 1 : 0, PublicKey.Y.Length - (PublicKey.Y[0] == 0 ? 1 : 0));
                }

                if (writePrivateKey)
                {
                    D.SaveToStream(bw);
                }
            }
        }

        public static ECKeyPair CreateNew(string name = "")
        {
            SecureRandom secureRandom = new SecureRandom();
            X9ECParameters curve = SecNamedCurves.GetByName("secp256k1");
            ECDomainParameters domain = new ECDomainParameters(curve.Curve, curve.G, curve.N, curve.H);
            ECKeyPairGenerator generator = new ECKeyPairGenerator();
            ECKeyGenerationParameters keygenParams = new ECKeyGenerationParameters(domain, secureRandom);
            generator.Init(keygenParams);
            AsymmetricCipherKeyPair keypair = generator.GenerateKeyPair();
            ECPrivateKeyParameters privParams = (ECPrivateKeyParameters)keypair.Private;
            ECPublicKeyParameters pubParams = (ECPublicKeyParameters)keypair.Public;
            ECKeyPair k = new ECKeyPair
            {
                CurveType = ECCurveType.Secp256K1,
                PrivateKey = privParams.D,
                Name = name
            };
            k.PublicKey = new ECPoint
            {
                X = pubParams.Q.Normalize().XCoord.ToBigInteger().ToByteArrayUnsigned(),
                Y = pubParams.Q.Normalize().YCoord.ToBigInteger().ToByteArrayUnsigned()
            };
            return k;
        }

        public void LoadFromStream(Stream stream, bool doubleLen = true, bool readPrivateKey = false,
            bool readName = false)
        {
            using (BinaryReader br = new BinaryReader(stream, Encoding.ASCII, true))
            {
                if (readName) Name = ByteString.ReadFromStream(br);
                if (doubleLen)
                {
                    ushort len = br.ReadUInt16();
                    if (len == 0) return;
                }

                CurveType = (ECCurveType)br.ReadUInt16();
                ushort xLen = br.ReadUInt16();
                var X = new BigInteger(1, br.ReadBytes(xLen)).ToByteArrayUnsigned();
                ushort yLen = br.ReadUInt16();
                var Y = new BigInteger(1, br.ReadBytes(yLen)).ToByteArrayUnsigned();
                try
                {
                    if (CurveType != ECCurveType.Empty && CurveType != ECCurveType.Sect283K1)
                    {
                        ECDsa ecdsa;
                        if (ecdsas.ContainsKey(CurveType))
                        {
                            ecdsa = ecdsas[CurveType];
                        }
                        else
                        {
                            ECCurve curve = ECCurve.CreateFromFriendlyName(CurveType.ToString().ToLower());
                            ecdsa = ECDsa.Create(curve);
                            ecdsas.Add(CurveType, ecdsa);
                        }
                        while (X.Length * 8 < ecdsa.KeySize)
                        {
                            var padded = X.ToList();
                            padded.Insert(0, 0);
                            X = padded.ToArray();
                        }
                        if (Y.Length * 8 < ecdsa.KeySize)
                        {
                            var padded = Y.ToList();
                            padded.Insert(0, 0);
                            Y = padded.ToArray();
                        }
                    }
                }
                catch (Exception e)
                {
                    // ECdsa implementation not supported, fallback to bouncycastle
                }
                PublicKey = new ECPoint() { X = X, Y = Y };
                if (readPrivateKey)
                {
                    D = Hash.ReadFromStream(br);
                }
            }
        }

        public bool Equals(ECKeyPair other)
        {
            if (other == null) return false;
            if (other.CurveType != CurveType) return false;
            if (!PublicKey.X.SequenceEqual(other.PublicKey.X)) return false;
            if (!PublicKey.Y.SequenceEqual(other.PublicKey.Y)) return false;
            return true;
        }

        public void DecriptKey(ByteString password)
        {
            byte[] b = new byte[32];
            var salt = D.Skip(8).Take(8).ToArray();
            SHA256Managed managed = new SHA256Managed();
            managed.Initialize();
            managed.TransformBlock(password, 0, password.Length, b, 0);
            managed.TransformFinalBlock(salt, 0, salt.Length);
            var digest = managed.Hash;
            managed.Dispose();
            managed = new SHA256Managed();
            managed.Initialize();
            managed.TransformBlock(digest, 0, digest.Length, b, 0);
            managed.TransformBlock(password, 0, password.Length, b, 0);
            salt = D.Skip(8).Take(8).ToArray();
            managed.TransformFinalBlock(salt, 0, salt.Length);
            var iv = managed.Hash;
            managed.Dispose();
            RijndaelManaged aesEncryption = new RijndaelManaged
            {
                KeySize = 256,
                BlockSize = 128,
                Mode = CipherMode.CBC,
                Padding = PaddingMode.PKCS7
            };
            byte[] encryptedBytes = D.Skip(16).ToArray(); //Crazy Salt...
            aesEncryption.IV = iv.Take(16).ToArray();
            aesEncryption.Key = digest;
            ICryptoTransform decrypto = aesEncryption.CreateDecryptor();
            Hash hash = decrypto.TransformFinalBlock(encryptedBytes, 0, encryptedBytes.Length);
            ByteString bs = hash;
            Hash h2 = bs.ToString(); // dirty hack
            D = h2;
            aesEncryption.Dispose();
        }

        public override string ToString()
        {
            return $"{Name} - {CurveType}";
        }
    }

}