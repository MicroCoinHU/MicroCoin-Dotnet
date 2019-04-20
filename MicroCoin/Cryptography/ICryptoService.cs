//-----------------------------------------------------------------------
// This file is part of MicroCoin - The first hungarian cryptocurrency
// Copyright (c) 2019 Peter Nemeth
// ICryptoService.cs - Copyright (c) 2019 Németh Péter
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
using System.IO;
using System.Security.Cryptography;
using System.Threading.Tasks;
using MicroCoin.Types;

namespace MicroCoin.Cryptography
{
    public interface ICryptoService : IDisposable
    {
        ByteString DecryptString(Hash em, ECKeyPair myKey, ECPoint otherKey);
        Hash DoubleSha256(Hash data);
        Hash EncryptString(ByteString data, ECKeyPair myKey, ECPoint otherKey);
        byte[] GenerateSharedKey(ECKeyPair myKey, ECPoint otherKey);
        ECSignature GenerateSignature(Hash data, ECKeyPair keyPair);
        Task<ECSignature> GenerateSignatureAsync(Hash data, ECKeyPair keyPair);
        Hash RipeMD160(Hash data);
        Hash Sha256(Hash data);
        bool ValidateSignature(Hash data, ECSignature signature, ECKeyPair keyPair);
        Hash Sha256(Stream hashBuffer);
    }
}