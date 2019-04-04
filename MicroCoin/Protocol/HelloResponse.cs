//-----------------------------------------------------------------------
// This file is part of MicroCoin - The first hungarian cryptocurrency
// Copyright (c) 2018 Peter Nemeth
// HelloResponse.cs - Copyright (c) 2018 Németh Péter
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
using MicroCoin.Common;
using MicroCoin.Cryptography;
using MicroCoin.Net;
using MicroCoin.Utils;
using System;
using System.IO;
using System.Reflection;
using System.Text;

namespace MicroCoin.Protocol
{
    public class HelloResponse : IStreamSerializable
    {
        public ushort ServerPort { get; set; }
        public ECKeyPair AccountKey { get; set; }
        public Timestamp Timestamp { get; set; }
        public Block Block { get; set; }
        public NodeServerList NodeServers { get; set; }
        public ByteString Version { get; set; }
        public ulong WorkSum { get; set; }

        public void LoadFromStream(Stream stream)
        {
            using (BinaryReader br = new BinaryReader(stream, Encoding.ASCII, true))
            {
                ServerPort = br.ReadUInt16();
                AccountKey = new ECKeyPair();
                AccountKey.LoadFromStream(stream);
                Timestamp = br.ReadUInt32();
                Block = new Block();
                Block.LoadFromStream(stream);
                NodeServers = NodeServerList.LoadFromStream(stream, ServerPort);
                Version = ByteString.ReadFromStream(br);
                WorkSum = br.ReadUInt64();
            }
        }

        public void SaveToStream(Stream s)
        {
            using (BinaryWriter bw = new BinaryWriter(s, Encoding.Default, true))
            {
                bw.Write(ServerPort);
                AccountKey.SaveToStream(s);
                bw.Write(Timestamp);
                Block.SaveToStream(s);
                NodeServers.SaveToStream(s);
                Version.SaveToStream(bw);                                        
                bw.Write(WorkSum);
            }
        }

        public HelloResponse()
        {

        }

        public HelloResponse(Stream stream)
        {
            LoadFromStream(stream);
        }
    }
}
