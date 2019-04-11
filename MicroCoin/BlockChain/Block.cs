//-----------------------------------------------------------------------
// This file is part of MicroCoin - The first hungarian cryptocurrency
// Copyright (c) 2019 Peter Nemeth
// Block.cs - Copyright (c) 2019 %UserDisplayName%
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
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace MicroCoin.BlockChain
{
    public class Block
    {
        public uint Id { get => Header.BlockNumber; set => Header.BlockNumber = value; }
        public BlockHeader Header { get; set; }
        public IList<ITransaction> Transactions { get; set; }

        public Block()
        {
            Header = new BlockHeader();
        }

        public static Block GenesisBlock()
        {
            return new Block
            {
                Header = new BlockHeader
                {
                    AccountKey = null,
                    AvailableProtocol = 0,
                    BlockNumber = 0,
                    CompactTarget = 0,
                    Fee = 0,
                    Nonce = 0,
                    TransactionHash = new byte[0],
                    Payload = new byte[0],
                    ProofOfWork = new byte[0],
                    ProtocolVersion = 0,
                    Reward = 0,
                    CheckPointHash = Cryptography.Utils.Sha256(Encoding.ASCII.GetBytes(Params.GenesisPayload)),
                    BlockSignature = 3,
                    Timestamp = 0
                }
            };
        }

        public Block(Stream stream) : this()
        {
            LoadFromStream(stream);
        }

        public void LoadFromStream(Stream stream)
        {
            Header = new BlockHeader(stream);
            if (Header.BlockSignature == 1 || Header.BlockSignature == 3)
            {
                return;
            }
            using (var br = new BinaryReader(stream, Encoding.Default, true))
            {
                var TransactionCount = br.ReadUInt32();
                if (TransactionCount <= 0) return;
                Transactions = new List<ITransaction>();
                for (var i = 0; i < TransactionCount; i++)
                {
                    var transactionType = (TransactionType)br.ReadUInt32();
                    ITransaction t = TransactionFactory.FromType(transactionType);
                    t.TransactionType = transactionType;
                    t.LoadFromStream(stream);
                    t.Block = Header.BlockNumber;
                    Transactions.Add(t);
                }
            }
        }
        public void SaveToStream(Stream stream)
        {
            Header.SaveToStream(stream);
            if (Header.BlockSignature == 1 || Header.BlockSignature == 3)
            {
                return;
            }
            using (var bw = new BinaryWriter(stream, Encoding.ASCII, true))
            {
                if (Transactions == null)
                {
                    bw.Write((uint)0);
                    return;
                }
                bw.Write((uint)Transactions.Count);
                foreach (var t in Transactions)
                {
                    bw.Write((uint)t.TransactionType);
                    t.SaveToStream(stream);
                }
            }
        }
    }
}
