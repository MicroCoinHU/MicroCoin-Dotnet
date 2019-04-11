//-----------------------------------------------------------------------
// This file is part of MicroCoin - The first hungarian cryptocurrency
// Copyright (c) 2019 Peter Nemeth
// DotNet.cs - Copyright (c) 2019 %UserDisplayName%
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
using System.Security.Cryptography;
using System.Text;
using MicroCoin.Types;

namespace MicroCoin.Cryptography
{
    public class DotNet : ICryptography
    {
        public bool ValidateSignature(Hash data, ECSignature signature, ECKeyPair keyPair)
        {
            using(var ecdsa = ECDsa.Create(keyPair))
            {
                if(8 * signature.R.Length < ecdsa.KeySize)
                {
                    var R = new byte[(int)Math.Ceiling((double)(ecdsa.KeySize / 8))];
                    signature.R.CopyTo(R, R.Length - signature.R.Length);
                    signature.R = R;
                }
                if (8 * signature.S.Length < ecdsa.KeySize)
                {
                    var S = new byte[(int)Math.Ceiling((double)(ecdsa.KeySize / 8))];
                    signature.S.CopyTo(S, S.Length - signature.S.Length);
                    signature.S = S;
                }
                return ecdsa.VerifyHash(data, signature.Signature);
            }
        }
    }
}
