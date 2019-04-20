//-----------------------------------------------------------------------
// This file is part of MicroCoin - The first hungarian cryptocurrency
// Copyright (c) 2019 Peter Nemeth
// CryptoService.cs - Copyright (c) 2019 Németh Péter
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
using Org.BouncyCastle.Asn1;
using Org.BouncyCastle.Asn1.Sec;
using Org.BouncyCastle.Asn1.X9;
using Org.BouncyCastle.Crypto;
using Org.BouncyCastle.Crypto.Agreement;
using Org.BouncyCastle.Crypto.Parameters;
using Org.BouncyCastle.Math;
using Org.BouncyCastle.Math.EC;
using Org.BouncyCastle.Security;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Threading.Tasks;

namespace MicroCoin.Cryptography
{
    public class CryptoService : ICryptoService
    {
        private readonly SHA256 sha = SHA256.Create();
        private readonly object shaLock = new object();
        private readonly List<ECCurveType> notSupportedECs = new List<ECCurveType>();
        public async Task<ECSignature> GenerateSignatureAsync(Hash data, ECKeyPair keyPair) => await Task.Run<ECSignature>(() => GenerateSignature(data, keyPair));
        public CryptoService()
        {

        }

        public ECSignature GenerateSignature(Hash data, ECKeyPair keyPair)
        {
            try
            {
                ISigner signer = SignerUtilities.GetSigner("NONEwithECDSA");
                X9ECParameters curve = SecNamedCurves.GetByName(keyPair.CurveType.ToString().ToLower());
                ECDomainParameters domain = new ECDomainParameters(curve.Curve, curve.G, curve.N, curve.H);
                BigInteger D = new BigInteger(1, keyPair.D);
                ECPrivateKeyParameters parameters = new ECPrivateKeyParameters(D, domain);
                signer.Init(true, parameters);
                signer.BlockUpdate(data, 0, data.Length);
                byte[] sigBytes = signer.GenerateSignature();
                Asn1InputStream decoder = new Asn1InputStream(sigBytes);
                DerInteger r, s;
                try
                {
                    DerSequence seq = (DerSequence)decoder.ReadObject();
                    r = (DerInteger)seq[0];
                    s = (DerInteger)seq[1];
                }
                finally
                {
                    decoder.Dispose();
                }
                var rArr = r.Value.ToByteArrayUnsigned();
                var sArr = s.Value.ToByteArrayUnsigned();
                return new ECSignature
                {
                    R = rArr,
                    S = sArr,
                    SigCompat = sigBytes
                };
            }
            catch (Exception)
            {
                return new ECSignature();
            }
        }

        public bool ValidateSignature(Hash data, ECSignature signature, ECKeyPair keyPair)
        {
            if (keyPair.CurveType != ECCurveType.Sect283K1 && !notSupportedECs.Contains(keyPair.CurveType))
            {
                try
                {
                    using (var ecdsa = ECDsa.Create(keyPair))
                    {                        
                        while (signature.S.Length * 8 < ecdsa.KeySize)
                        {
                            var list = signature.S.ToList();
                            list.Insert(0, 0);
                            signature.S = list.ToArray();
                        }
                        while (signature.R.Length * 8 < ecdsa.KeySize)
                        {
                            var list = signature.R.ToList();
                            list.Insert(0, 0);
                            signature.R = list.ToArray();
                        }
                        return ecdsa.VerifyHash(data, signature.Signature);
                    }
                }
                catch (Exception)
                {
                    notSupportedECs.Add(keyPair.CurveType);
                }
            }
            var derSignature = new DerSequence(
                new DerInteger(new BigInteger(1, signature.R)),
                new DerInteger(new BigInteger(1, signature.S)))
                .GetDerEncoded();
            X9ECParameters curve = SecNamedCurves.GetByName(keyPair.CurveType.ToString().ToLower());
            ECDomainParameters domain = new ECDomainParameters(curve.Curve, curve.G, curve.N, curve.H);
            var publicKey = curve.Curve.CreatePoint(new BigInteger(+1, keyPair.PublicKey.X), new BigInteger(+1, keyPair.PublicKey.Y));
            ECPublicKeyParameters publicKeyParameters = new ECPublicKeyParameters(publicKey, domain);
            ISigner signer = SignerUtilities.GetSigner("NONEwithECDSA");
            signer.Init(false, publicKeyParameters);
            signer.BlockUpdate(data, 0, data.Length);
            return signer.VerifySignature(derSignature);
        }

        public byte[] GenerateSharedKey(ECKeyPair myKey, System.Security.Cryptography.ECPoint otherKey)
        {
            string curveName = "secp256k1";
            var spec = ECNamedCurveTable.GetByName(curveName);
            ECDomainParameters domain = new ECDomainParameters(spec.Curve, spec.G, spec.N, spec.H);
            X9ECParameters ecP = ECNamedCurveTable.GetByName(curveName);
            FpCurve curve = (FpCurve)ecP.Curve;
            ECFieldElement x = curve.FromBigInteger(new BigInteger(1, otherKey.X));
            ECFieldElement y = curve.FromBigInteger(new BigInteger(1, otherKey.Y));
            var q = curve.CreatePoint(x.ToBigInteger(), y.ToBigInteger());
            ECPublicKeyParameters pubKey = new ECPublicKeyParameters("ECDH", q, SecObjectIdentifiers.SecP256k1);
            ECPrivateKeyParameters prvkey = new ECPrivateKeyParameters(new BigInteger(1, myKey.D), domain);
            ECDHBasicAgreement agreement = new ECDHBasicAgreement();
            agreement.Init(prvkey);
            return agreement.CalculateAgreement(pubKey).ToByteArrayUnsigned();
        }

        public ByteString DecryptString(Hash em, ECKeyPair myKey, System.Security.Cryptography.ECPoint otherKey)
        {
            using (AesManaged tdes = new AesManaged())
            {
                tdes.Key = GenerateSharedKey(myKey, otherKey);
                tdes.Mode = CipherMode.ECB;
                tdes.Padding = PaddingMode.PKCS7;
                using (ICryptoTransform decrypt = tdes.CreateDecryptor())
                {
                    return decrypt.TransformFinalBlock(em, 0, em.Length);
                }
            }
        }

        public Hash EncryptString(ByteString data, ECKeyPair myKey, System.Security.Cryptography.ECPoint otherKey)
        {
            using (AesManaged tdes = new AesManaged())
            {
                tdes.Key = GenerateSharedKey(myKey, otherKey);
                tdes.Mode = CipherMode.ECB;
                tdes.Padding = PaddingMode.PKCS7;
                using (ICryptoTransform crypt = tdes.CreateEncryptor())
                {
                    return crypt.TransformFinalBlock(data, 0, data.Length);
                }
            }
        }

        public Hash Sha256(Hash data)
        {
            lock(shaLock)
            {
                return sha.ComputeHash(data);
            }
        }

        public Hash DoubleSha256(Hash data)
        {
            lock (shaLock)
            {
                Hash h = sha.ComputeHash(data);
                return sha.ComputeHash(h);
            }
        }

        public Hash RipeMD160(Hash data)
        {
            var digest = new Org.BouncyCastle.Crypto.Digests.RipeMD160Digest();
            digest.BlockUpdate(data, 0, data.Length);
            Hash h = new byte[digest.GetDigestSize()];
            digest.DoFinal(h, 0);
            return h;
        }

        public void Dispose()
        {
            sha.Dispose();
        }

        public Hash Sha256(Stream hashBuffer)
        {
            lock (shaLock)
            {
                var pos = hashBuffer.Position;
                hashBuffer.Position = 0;
                try
                {
                    return sha.ComputeHash(hashBuffer);
                }
                finally
                {
                    hashBuffer.Position = pos;
                }
            }
        }
    }
}
