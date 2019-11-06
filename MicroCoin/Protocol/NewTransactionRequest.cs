//-----------------------------------------------------------------------
// This file is part of MicroCoin - The first hungarian cryptocurrency
// Copyright (c) 2019 Peter Nemeth
// NewTransactionRequest.cs - Copyright (c) 2019 Németh Péter
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
using MicroCoin.Transactions;
using MicroCoin.Types;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace MicroCoin.Protocol
{
    public class NewTransactionRequest : INetworkPayload
    {
        public IList<ITransaction> Transactions { get; set; }
        public DateTime Created { get; set; } = DateTime.Now;

        public NetOperationType NetOperation => NetOperationType.NewTransaction;

        public RequestType RequestType => RequestType.AutoSend;

        public NewTransactionRequest(ITransaction[] transactions)
        {
            Transactions = transactions;
        }

        public NewTransactionRequest()
        {
        }

        public void LoadFromStream(Stream stream)
        {
            using (BinaryReader br = new BinaryReader(stream))
            {
                uint transactionCount = br.ReadUInt32();
                Transactions = new List<ITransaction>();
                for (int i = 0; i < transactionCount; i++)
                {
                    TransactionType type = (TransactionType)br.ReadByte();
                    switch (type)
                    {
                        case TransactionType.Transaction:
                        case TransactionType.BuyAccount:
                            Transactions.Add(new TransferTransaction(stream));
                            break;
                        case TransactionType.ChangeKey:
                        case TransactionType.ChangeKeySigned:
                            Transactions.Add(new ChangeKeyTransaction(stream, type));
                            break;
                        case TransactionType.ListAccountForSale:
                        case TransactionType.DeListAccountForSale:
                            Transactions.Add(new ListAccountTransaction(stream));
                            break;
                        case TransactionType.ChangeAccountInfo:
                            Transactions.Add(new ChangeAccountInfoTransaction(stream));
                            break;
                        default:
                            stream.Position = stream.Length;
                            return;
                    }
                }
            }
        }

        public void SaveToStream(Stream stream)
        {
            using (BinaryWriter bw = new BinaryWriter(stream, Encoding.ASCII, true))
            {
                bw.Write((uint)Transactions.Count);
                foreach (var t in Transactions)
                {
                    bw.Write((byte)t.TransactionType);
                    t.SaveToStream(stream);
                }
            }
        }

        public Hash GetHash()
        {
            MemoryStream ms = new MemoryStream();
            try
            {
                using (BinaryWriter bw = new BinaryWriter(ms))
                {
                    foreach (var t in Transactions)
                    {
                        bw.Write(t.Fee);
                        t.Payload.SaveToStream(bw);
                        bw.Write(t.SignerAccount);
                        bw.Write(t.TargetAccount);
                        t.Signature.SaveToStream(ms);
                        t.AccountKey.SaveToStream(ms);
                        bw.Write((uint)t.TransactionType);
                        bw.Write(Created.Hour);
                        bw.Write(Created.Minute / 10);
                    }
                    using (System.Security.Cryptography.SHA256Managed sha = new System.Security.Cryptography.SHA256Managed())
                    {
                        ms.Position = 0;
                        return sha.ComputeHash(ms);
                    }
                }
            }
            finally
            {
                ms.Dispose();
            }
        }

        public T GetTransaction<T>(int i) where T : class, ITransaction
        {
            return Transactions[i] as T;
        }


    }
}