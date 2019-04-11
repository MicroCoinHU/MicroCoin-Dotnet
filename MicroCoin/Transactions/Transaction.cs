//-----------------------------------------------------------------------
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
using MicroCoin.Types;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace MicroCoin.Transactions
{
    public abstract class Transaction : ITransaction
    {
        public uint Id { get; set; }
        private ByteString _payload;
        public AccountNumber SignerAccount { get; set; }
        public uint NumberOfOperations { get; set; }
        public AccountNumber TargetAccount { get; set; }
        public ByteString Payload
        {
            get
            {
//                if (_payload.IsReadable) return _payload;
//                ByteString bs = (string)(new Hash(_payload));
                return _payload;
            }
            set => _payload = value;
        }
        public ECSignature Signature { get; set; }
        public ECKeyPair AccountKey { get; set; }
        public Currency Fee { get; set; }
        public Currency Amount { get; set; }
        public abstract byte[] GetHash();
        public abstract void SaveToStream(Stream s);
        public abstract void LoadFromStream(Stream s);
        public TransactionType TransactionType { get; set; }

        public ECSignature GetSignature()
        {
            return Utils.GenerateSignature(GetHash(), AccountKey);
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
            MemoryStream ms = new MemoryStream();
            try
            {
                using (BinaryWriter bw = new BinaryWriter(ms))
                {
                    bw.Write(block);
                    bw.Write(SignerAccount);
                    bw.Write(NumberOfOperations);
                    Hash data;
                    using (MemoryStream m = new MemoryStream())
                    {
                        SaveToStream(m);
                        data = m.ToArray();                                                
                    }
                    Hash hh = Utils.RipeMD160(data);
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
            using(var ms = new MemoryStream())
            {
                SaveToStream(ms);
                return ms.ToArray();
            }
        }

        abstract public IList<Account> Apply(ICheckPointService checkPointService);
    }
}