//-----------------------------------------------------------------------
// This file is part of MicroCoin - The first hungarian cryptocurrency
// Copyright (c) 2019 Peter Nemeth
// BlockResponse.cs - Copyright (c) 2019 Németh Péter
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
using System.Collections.Generic;
using System.IO;
using System.Text;
using MicroCoin.Common;

namespace MicroCoin.Protocol
{
    public class BlockResponse : IStreamSerializable, INetworkPayload
    {
        public ICollection<Block> Blocks { get; set; }

        public virtual NetOperationType NetOperation => NetOperationType.Blocks;

        public RequestType RequestType => RequestType.Response;

        public BlockResponse()
        {
        }

        public BlockResponse(IEnumerable<Block> blocks) : this()
        {
            Blocks = new List<Block>(blocks);
        }

        public override string ToString()
        {
            return string.Format("Blocks response with {0} blocks", Blocks.Count);
        }

        public void SaveToStream(Stream stream)
        {
            using (BinaryWriter bw = new BinaryWriter(stream, Encoding.ASCII, true))
            {
                bw.Write((uint)Blocks.Count);
                foreach (var b in Blocks)
                {
                    b.SaveToStream(stream);
                }
            }
        }

        public void LoadFromStream(Stream stream)
        {
            Blocks = new HashSet<Block>();
            using (BinaryReader br = new BinaryReader(stream, Encoding.ASCII, true))
            {
                uint BlockCount = br.ReadUInt32();
                while (true)
                {
                    if (stream.Position >= stream.Length - 1)
                    {
                        break;
                    }
                    try
                    {
                        Blocks.Add(new Block(stream));
                    }
                    catch (EndOfStreamException e)
                    {
                        throw e;
                    }
                }
            }
        }
    }
}
