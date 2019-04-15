//-----------------------------------------------------------------------
// This file is part of MicroCoin - The first hungarian cryptocurrency
// Copyright (c) 2019 Peter Nemeth
// BlockRequest.cs - Copyright (c) 2019 Németh Péter
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
using MicroCoin.Common;
using System.IO;
using System.Text;

namespace MicroCoin.Protocol
{
    public class BlockRequest : IStreamSerializable, INetworkPayload
    {
        public uint StartBlock { get; set; }
        public uint EndBlock { get; set; }
        public uint NumberOfBlocks { get; set; }

        public virtual NetOperationType NetOperation => NetOperationType.Blocks;

        public RequestType RequestType => RequestType.Request;

        public BlockRequest()
        {
        }

        public void LoadFromStream(Stream s)
        {
            using (BinaryReader br = new BinaryReader(s, Encoding.ASCII, true)) {
                StartBlock = br.ReadUInt32();
                EndBlock = br.ReadUInt32();
            }
        }


        public void SaveToStream(Stream s)
        {
            using(BinaryWriter bw = new BinaryWriter(s, Encoding.ASCII, true)) {
                bw.Write(StartBlock);
                bw.Write(NumberOfBlocks+StartBlock-1);
            }
        }
    }
}