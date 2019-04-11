//-----------------------------------------------------------------------
// This file is part of MicroCoin - The first hungarian cryptocurrency
// Copyright (c) 2019 Peter Nemeth
// Node.cs - Copyright (c) 2019 %UserDisplayName%
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
using MicroCoin.Types;
using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Text;

namespace MicroCoin.Net
{
    public class Node
    {
        public ByteString IP { get; set; }
        public ushort Port { get; set; }
        public Timestamp LastConnection { get; set; }
        public IPEndPoint EndPoint { get => new IPEndPoint(IPAddress.Parse(IP), Port); }
        public NetClient NetClient { get; set; }
        public bool Connected => NetClient == null ? false : NetClient.IsConnected;
        public ushort ServerPort { get; internal set; }
        public uint BlockHeight { get; set; } = 0;

        public override string ToString()
        {
            return IP + ":" + Port;
        }

        public void LoadFromStream(Stream stream)
        {
            using(var br = new BinaryReader(stream))
            {
                IP = br.ReadBytes(br.ReadUInt16());
                Port = br.ReadUInt16();
                LastConnection = br.ReadUInt32();
                ServerPort = Params.ServerPort;
            }
        }
    }
}