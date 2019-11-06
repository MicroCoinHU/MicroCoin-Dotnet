//-----------------------------------------------------------------------
// This file is part of MicroCoin - The first hungarian cryptocurrency
// Copyright (c) 2019 Peter Nemeth
// KeyPair.cs - Copyright (c) 2019 Németh Péter
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
using System.Security.Cryptography;

namespace MicroCoin.Cryptography
{
    public class KeyPair
    {
        public ECCurveType CurveType { get; set; } = ECCurveType.Empty;
        public ECPoint PublicKey { get; set; }
        public byte[] X { get => PublicKey.X; set { PublicKey = new ECPoint() { X = value, Y = PublicKey.Y }; } }
        public byte[] Y { get => PublicKey.Y; set { PublicKey = new ECPoint() { Y = value, X = PublicKey.X }; } }

        public int Id { get; set; }

        public byte[] D { get; set; }
        public KeyPair()
        {
            PublicKey = new ECPoint() { X = new byte[0], Y = new byte[0] };
        }
    }

}