//-----------------------------------------------------------------------
// This file is part of MicroCoin - The first hungarian cryptocurrency
// Copyright (c) 2019 Peter Nemeth
// CheckPointBlock.cs - Copyright (c) 2019 %UserDisplayName%
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
using MicroCoin.BlockChain;
using MicroCoin.Chain;
using MicroCoin.Cryptography;
using MicroCoin.Types;
using System;
using System.Collections.Generic;
using System.IO;
using System.Security.Cryptography;
using System.Text;

namespace MicroCoin.CheckPoints
{
    /// <summary>
    /// One entry in the checkpoint
    /// </summary>
    public class CheckPointBlock : IEquatable<CheckPointBlock>
    {
        public BlockHeader Header { get; set; }

        public uint Id { get => Header.BlockNumber; set => Header.BlockNumber = value; }

        /// <summary>
        /// List of all accounts
        /// </summary>
        /// <value>The accounts.</value>
        public IList<Account> Accounts { get; set; } = new List<Account>(5);
        /// <summary>
        /// The block hash
        /// </summary>
        /// <value>The block hash.</value>
        public Hash BlockHash { get; set; }
        /// <summary>
        /// Gets or sets the accumulated work.
        /// </summary>
        /// <value>The accumulated work.</value>
        public ulong AccumulatedWork { get; set; }

        public void SaveToStream(Stream stream)
        {
            using(BinaryWriter bw = new BinaryWriter(stream, Encoding.Default, true))
            {
                SaveToStream(bw);
            }
        }

        internal void SaveToStream(BinaryWriter bw, bool saveHash = true, bool proto1 = false)
        {
            bw.Write(Header.BlockNumber);
            if (!proto1)
            {
                Header.AccountKey.SaveToStream(bw.BaseStream, false);
                bw.Write(Header.Reward);
                bw.Write(Header.Fee);
                bw.Write(Header.ProtocolVersion);
                bw.Write(Header.AvailableProtocol);
                bw.Write(Header.Timestamp);
                bw.Write(Header.CompactTarget);
                bw.Write(Header.Nonce);
                Header.Payload.SaveToStream(bw);
                Header.CheckPointHash.SaveToStream(bw);
                Header.TransactionHash.SaveToStream(bw);
                Header.ProofOfWork.SaveToStream(bw);
            }
            for (int i = 0; i < 5; i++)
            {
                Accounts[i].SaveToStream(bw, saveHash, !proto1);
            }
            if (proto1) bw.Write(Header.Timestamp);
            if (saveHash)
            {
                BlockHash.SaveToStream(bw);
            }
            if (!proto1)
            {
                bw.Write(AccumulatedWork);
            }
        }

        public CheckPointBlock() 
        {
            Header = new BlockHeader();
        }

        public CheckPointBlock(Stream stream) : this()
        {
            LoadFromStream(stream);
        }

        public Hash CalculateBlockHash(bool checkproto = false)
        {
            MemoryStream ms = new MemoryStream();
            try
            {
                using (BinaryWriter bw = new BinaryWriter(ms, Encoding.ASCII, true))
                {
                    //SaveToStream(bw, false, checkproto ? ProtocolVersion<2 : false);
                    SaveToStream(bw, false, false);
                    ms.Position = 0;                    
                    using (SHA256Managed sha = new SHA256Managed())
                    {
                        return sha.ComputeHash(ms);
                    }
                }
            }
            finally
            {                
                ms.Dispose();
            }
        }

        public bool Equals(CheckPointBlock other)
        {
            if (other == null) return false;
            if (other.AccumulatedWork != AccumulatedWork) return false;
            if (other.Header.AvailableProtocol != Header.AvailableProtocol) return false;
            if (other.Header.BlockNumber != Header.BlockNumber) return false;
        //    if (other.BlockSignature != BlockSignature) return false;            
            if (other.Header.CheckPointHash != Header.CheckPointHash) return false;
            if (other.Header.CompactTarget != Header.CompactTarget) return false;
            if (other.Header.Fee != Header.Fee) return false;
            if (other.Header.Nonce != Header.Nonce) return false;
            if (other.Header.Payload != Header.Payload) return false;
            if (other.Header.ProofOfWork != Header.ProofOfWork) return false;
            if (other.Header.ProtocolVersion != Header.ProtocolVersion) return false;
            if (other.Header.Reward != Header.Reward) return false;
            if (other.Header.Timestamp != Header.Timestamp) return false;
            if (other.Header.TransactionHash != Header.TransactionHash) return false;
            if (other.Accounts.Count != Accounts.Count) return false;
            if (!other.Header.AccountKey.Equals(Header.AccountKey)) return false;
            for (int i = 0; i < Accounts.Count; i++)
            {
                if (!Accounts[i].Equals(other.Accounts[i])) return false;
            }
            return true;
        }

        public void LoadFromStream(Stream stream)
        {
            using (BinaryReader br = new BinaryReader(stream, Encoding.Default, true))
            {
                Header.BlockNumber = br.ReadUInt32();
                Header.AccountKey = new ECKeyPair();
                Header.AccountKey.LoadFromStream(stream, false);
                Header.Reward = br.ReadUInt64();
                Header.Fee = br.ReadUInt64();
                Header.ProtocolVersion = br.ReadUInt16();
                Header.AvailableProtocol = br.ReadUInt16();
                Header.Timestamp = br.ReadUInt32();
                Header.CompactTarget = br.ReadUInt32();
                Header.Nonce = br.ReadInt32();
                ushort len = br.ReadUInt16();
                Header.Payload = br.ReadBytes(len);
                len = br.ReadUInt16();
                Header.CheckPointHash = br.ReadBytes(len);
                len = br.ReadUInt16();
                Header.TransactionHash = br.ReadBytes(len);
                len = br.ReadUInt16();
                Header.ProofOfWork = br.ReadBytes(len);
                for (int i = 0; i < 5; i++)
                {
                    Account acc = new Account(stream);
                    Accounts.Add(acc);
                }
                BlockHash = Hash.ReadFromStream(br);
                AccumulatedWork = br.ReadUInt64();
            }
        }
    }
}
