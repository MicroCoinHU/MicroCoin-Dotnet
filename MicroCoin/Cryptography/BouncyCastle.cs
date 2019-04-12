//-----------------------------------------------------------------------
// This file is part of MicroCoin - The first hungarian cryptocurrency
// Copyright (c) 2019 Peter Nemeth
// BouncyCastle.cs - Copyright (c) 2019 Németh Péter
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
using System;
using System.Collections.Generic;
using System.Text;
using MicroCoin.Types;
using Org.BouncyCastle.Asn1;
using Org.BouncyCastle.Asn1.Sec;
using Org.BouncyCastle.Asn1.X9;
using Org.BouncyCastle.Crypto;
using Org.BouncyCastle.Crypto.Parameters;
using Org.BouncyCastle.Math;
using Org.BouncyCastle.Security;

namespace MicroCoin.Cryptography
{
    public class BouncyCastle : ICryptography
    {
        public bool ValidateSignature(Hash data, ECSignature signature, ECKeyPair keyPair)
        {
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
    }
}
