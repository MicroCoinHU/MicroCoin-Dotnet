//-----------------------------------------------------------------------
// This file is part of MicroCoin - The first hungarian cryptocurrency
// Copyright (c) 2019 Peter Nemeth
// BlockHeader.cs - Copyright (c) 2019 Németh Péter
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
using MicroCoin.Cryptography;
using MicroCoin.Types;
using System.IO;
using System.Security.Cryptography;
using System.Text;

namespace MicroCoin.BlockChain
{
    public class BlockHeader
    {
        public uint Id { get => BlockNumber; set => BlockNumber = value; }
        public byte BlockSignature { get; set; } = 2;
        public ushort ProtocolVersion { get; set; }
        public ushort AvailableProtocol { get; set; }
        public uint BlockNumber { get; set; }
        public ECKeyPair AccountKey { get; set; }
        public Currency Reward { get; set; }
        public Currency Fee { get; set; }
        public Timestamp Timestamp { get; set; }
        public uint CompactTarget { get; set; }
        public int Nonce { get; set; }
        public ByteString Payload { get; set; }
        public Hash CheckPointHash { get; set; }
        public Hash TransactionHash { get; set; }
        public Hash ProofOfWork { get; set; }
        internal BlockHeader(Stream stream)
        {
            using (var br = new BinaryReader(stream, Encoding.ASCII, true))
            {
                BlockSignature = br.ReadByte();
                if (BlockSignature > 0)
                {
                    ProtocolVersion = br.ReadUInt16();
                    AvailableProtocol = br.ReadUInt16();
                }
                BlockNumber = br.ReadUInt32();
                AccountKey = new ECKeyPair();
                AccountKey.LoadFromStream(stream);
                Reward = br.ReadUInt64();
                Fee = br.ReadUInt64();
                Timestamp = br.ReadUInt32();
                CompactTarget = br.ReadUInt32();
                Nonce = br.ReadInt32();
                ushort stringLength = br.ReadUInt16();
                if (stringLength > 0)
                {
                    Payload = br.ReadBytes(stringLength);
                }
                stringLength = br.ReadUInt16();
                if (stringLength > 0)
                {
                    CheckPointHash = br.ReadBytes(stringLength);
                }
                stringLength = br.ReadUInt16();
                if (stringLength > 0)
                {
                    TransactionHash = br.ReadBytes(stringLength);
                }
                stringLength = br.ReadUInt16();
                if (stringLength > 0)
                {
                    ProofOfWork = br.ReadBytes(stringLength);
                }
            }
        }

        internal BlockHeader()
        {
            AccountKey = new ECKeyPair();
        }

        internal virtual void SaveToStream(Stream s)
        {
            using (var bw = new BinaryWriter(s, Encoding.ASCII, true))
            {
                bw.Write(BlockSignature);
                bw.Write(ProtocolVersion);
                bw.Write(AvailableProtocol);
                bw.Write(BlockNumber);
                if (AccountKey == null)
                {
                    bw.Write((ushort)6);
                    bw.Write((ushort)0);
                    bw.Write((ushort)0);
                    bw.Write((ushort)0);
                }
                else
                {
                    AccountKey.SaveToStream(s);
                }
                bw.Write(Reward);
                bw.Write(Fee);
                bw.Write(Timestamp);
                bw.Write(CompactTarget);
                bw.Write(Nonce);
                Payload.SaveToStream(bw);
                CheckPointHash.SaveToStream(bw);
                TransactionHash.SaveToStream(bw);
                ProofOfWork.SaveToStream(bw);
            }
        }

        public bool IsValid()
        {
            if (Reward < 0) return false;
            if (Fee < 0) return false;
            return ProofOfWork.Length == 0 || ProofOfWorkIsValid();
        }

        public bool ProofOfWorkIsValid()
        {
            var header = GetBlockHeaderForHash();
            Hash headerHash = header.GetBlockHeaderHash((uint)Nonce, Timestamp);
            using (SHA256Managed sha = new SHA256Managed())
            {
                Hash hash = Utils.DoubleSha256(headerHash);
                return hash.SequenceEqual(ProofOfWork);
            }

        }

        public Hash CalcProofOfWork()
        {
            var header = GetBlockHeaderForHash();
            Hash headerHash = header.GetBlockHeaderHash((uint)Nonce, Timestamp);
            Hash hash = Utils.DoubleSha256(headerHash);
            return hash;
        }


        public BlockHeaderForHash GetBlockHeaderForHash()
        {
            BlockHeaderForHash header = new BlockHeaderForHash
            {
                Part1 = GetPart1(),
                MinerPayload = Payload,
                Part3 = GetPart3()
            };
            return header;
        }

        public Hash GetPart1()
        {
            using (BinaryWriter bw = new BinaryWriter(new MemoryStream()))
            {
                bw.Write(BlockNumber);
                AccountKey.SaveToStream(bw.BaseStream, false);
                bw.Write(Reward);
                bw.Write(ProtocolVersion);
                bw.Write(AvailableProtocol);
                //uint newTarget = BlockChain.TargetToCompact(BlockChain.Instance.GetNewTarget());
                bw.Write(CompactTarget);
                return (bw.BaseStream as MemoryStream)?.ToArray();
            }
        }

        public Hash GetPart3()
        {
            using (BinaryWriter bw = new BinaryWriter(new MemoryStream()))
            {
                CheckPointHash.SaveToStream(bw, false);
                TransactionHash.SaveToStream(bw, false);
                bw.Write((uint)Fee);
                return (bw.BaseStream as MemoryStream)?.ToArray();
            }
        }
    }
}