﻿//-----------------------------------------------------------------------
// This file is part of MicroCoin - The first hungarian cryptocurrency
// Copyright (c) 2019 Peter Nemeth
// Transaction.cs - Copyright (c) 2019 Németh Péter
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
using MicroCoin.Chain;
using MicroCoin.CheckPoints;
using MicroCoin.Cryptography;
using MicroCoin.Modularization;
using System.Collections.Generic;
using System.IO;
using System.Text;
using MicroCoin.Types;

namespace MicroCoin.Transactions
{
    public abstract class Transaction : ITransaction
    {
        public long _id { get; set; }
        public uint Block { get; set; }

        private ByteString _payload;
        public AccountNumber SignerAccount { get; set; }
        public uint TransactionCount { get; set; }
        public AccountNumber TargetAccount { get; set; }
        public ByteString Payload
        {
            get
            {
                return _payload;
            }
            set => _payload = value;
        }
        public ECSignature Signature { get; set; }
        public ECKeyPair AccountKey { get; set; } = new ECKeyPair();
        public Currency Fee { get; set; }
        public Currency Amount { get; set; }
        public abstract byte[] GetHash();
        public abstract void SaveToStream(Stream s);
        public abstract void LoadFromStream(Stream s);
        public TransactionType TransactionType { get; set; }

        public ECSignature GetSignature()
        {
            return ServiceLocator.GetService<ICryptoService>().GenerateSignature(GetHash(), AccountKey);
        }

        public bool SignatureValid()
        {
            return true;
        }

        public virtual bool IsValid()
        {
            return true;
        }

        public Hash GetOpHash(uint block)
        {
            MemoryStream ms = new MemoryStream(512);
            try
            {
                using (BinaryWriter bw = new BinaryWriter(ms))
                {
                    bw.Write(block);
                    bw.Write(SignerAccount);
                    bw.Write(TransactionCount);
                    Hash data;
                    using (MemoryStream m = new MemoryStream(512))
                    {
                        SaveToStream(m);
                        data = m.ToArray();                                                
                    }
                    Hash hh = ServiceLocator.GetService<ICryptoService>().RipeMD160(data);
                    string s = hh;
                    s = s.Substring(0, 20);
                    bw.Write(Encoding.ASCII.GetBytes(s), 0, 20);
                    return ms.ToArray();
                }
            }
            finally
            {
                ms?.Dispose();
            }
        }

        public Hash Serialize()
        {
            using(var ms = new MemoryStream(512))
            {
                SaveToStream(ms);
                return ms.ToArray();
            }
        }

        public Hash SHA()
        {
            return ServiceLocator.GetService<ICryptoService>().Sha256(GetHash());
        }

        public abstract IList<Account> Apply(ICheckPointService checkPointService);
        public abstract IList<Account> GetModifiedAccounts(ICheckPointService checkPointService);
    }
}