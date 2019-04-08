//-----------------------------------------------------------------------
// This file is part of MicroCoin - The first hungarian cryptocurrency
// Copyright (c) 2019 Peter Nemeth
// Utils.cs - Copyright (c) 2019 Németh Péter
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
using System.Linq;
using System.Security.Cryptography;
using System.Threading.Tasks;

namespace MicroCoin.Cryptography
{
    public static class Utils
    {
        public static async Task<ECSignature> GenerateSignatureAsync(Hash data, ECKeyPair keyPair) => await Task.Run<ECSignature>(() => GenerateSignature(data, keyPair));

        public static ECSignature GenerateSignature(Hash data, ECKeyPair keyPair)
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

        public static bool ValidateSignature(Hash data, ECSignature signature, ECKeyPair keyPair)
        {

            var derSignature = new DerSequence(
                new DerInteger(new BigInteger(1, signature.Signature.Take(32).ToArray())),
                new DerInteger(new BigInteger(1, signature.Signature.Skip(32).ToArray())))
                .GetDerEncoded();
            ISigner signer = SignerUtilities.GetSigner("NONEwithECDSA");
            X9ECParameters curve = SecNamedCurves.GetByName(keyPair.CurveType.ToString().ToLower());
            ECDomainParameters domain = new ECDomainParameters(curve.Curve, curve.G, curve.N, curve.H);
            FpCurve c = (FpCurve)curve.Curve;
            var publicKey = c.CreatePoint(new BigInteger(+1, keyPair.PublicKey.X), new BigInteger(+1, keyPair.PublicKey.Y));
            ECPublicKeyParameters publicKeyParameters = new ECPublicKeyParameters(publicKey, domain);
            signer.Init(false, publicKeyParameters);
            signer.BlockUpdate(data, 0, data.Length);
            bool ok = signer.VerifySignature(derSignature);
            return ok;
        }

        public static byte[] GenerateSharedKey(ECKeyPair myKey, System.Security.Cryptography.ECPoint otherKey)
        {
            string curveName = "secp256k1";
            var spec = ECNamedCurveTable.GetByName(curveName);
            ECDomainParameters domain = new ECDomainParameters(spec.Curve, spec.G, spec.N, spec.H);
            X9ECParameters ecP = ECNamedCurveTable.GetByName(curveName);
            FpCurve curve = (FpCurve)ecP.Curve;
            ECFieldElement x = curve.FromBigInteger(new BigInteger(1, otherKey.X));
            ECFieldElement y = curve.FromBigInteger(new BigInteger(1, otherKey.Y));
            Org.BouncyCastle.Math.EC.ECPoint q = curve.CreatePoint(x.ToBigInteger(), y.ToBigInteger());
            ECPublicKeyParameters pubKey = new ECPublicKeyParameters("ECDH", q, SecObjectIdentifiers.SecP256k1);
            ECPrivateKeyParameters prvkey = new ECPrivateKeyParameters(new BigInteger(1, myKey.D), domain);
            ECDHBasicAgreement agreement = new ECDHBasicAgreement();
            agreement.Init(prvkey);
            byte[] password = agreement.CalculateAgreement(pubKey).ToByteArrayUnsigned();
            return password;
        }

        public static ByteString DecryptString(Hash em, ECKeyPair myKey, System.Security.Cryptography.ECPoint otherKey)
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

        public static Hash EncryptString(ByteString data, ECKeyPair myKey, System.Security.Cryptography.ECPoint otherKey)
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

        public static Hash Sha256(Hash data)
        {
            using (SHA256Managed sha = new SHA256Managed())
            {
                return sha.ComputeHash(data);
            }
        }

        public static Hash DoubleSha256(Hash data)
        {
            using (SHA256Managed sha = new SHA256Managed())
            {
                Hash h = sha.ComputeHash(data);
                return sha.ComputeHash(h);
            }
        }

        public static Hash RipeMD160(Hash data)
        {
            var digest = new Org.BouncyCastle.Crypto.Digests.RipeMD160Digest();
            digest.BlockUpdate(data, 0, data.Length);
            Hash h = new byte[digest.GetDigestSize()];
            digest.DoFinal(h, 0);
            return h;
        }
    }
}
