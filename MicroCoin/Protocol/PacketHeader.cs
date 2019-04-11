//-----------------------------------------------------------------------
// This file is part of MicroCoin - The first hungarian cryptocurrency
// Copyright (c) 2019 Peter Nemeth
// PacketHeader.cs - Copyright (c) 2019 %UserDisplayName%
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
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace MicroCoin.Protocol
{
    public enum RequestType : ushort { None = 0, Request, Response, AutoSend, Unknown }
    public enum NetOperationType : ushort
    {
        Hello = 1,
        Error = 2,
        Message = 3,
        Transactions = 0x05,
        Blocks = 0x10,
        NewBlock = 0x11,
        AddOperations = 0x20,
        CheckPoint = 0x21
    }

    public class PacketHeader
    {
        private static uint lastRequestId = 1;

        public static int Size = 4 + 2 + 2 + 2 + 4 + 2 + 2 + 4;
        public uint Magic { get; set; } = Params.NetworkPacketMagic;
        public RequestType RequestType { get; set; } = RequestType.Request;
        public NetOperationType Operation { get; set; }
        public ushort Error { get; set; }
        public uint RequestId { get; set; }
        public ushort ProtocolVersion { get; set; }
        public ushort AvailableProtocol { get; set; }
        public int DataLength { get; set; }

        private static readonly object RequestLock = new object();

        public PacketHeader()
        {
            lock (RequestLock)
            {
                RequestId = lastRequestId++;
            }
            Operation = NetOperationType.Hello;
            ProtocolVersion = Params.NetworkProtocolVersion;
            AvailableProtocol = Params.NetworkProtocolAvailable;
            Error = 0;
        }

        internal virtual void SaveToStream(Stream s)
        {
            using (BinaryWriter br = new BinaryWriter(s, Encoding.ASCII, true))
            {
                br.Write(Magic);
                br.Write((ushort)RequestType);
                br.Write((ushort)Operation);
                br.Write(Error);
                br.Write(RequestId);
                br.Write(ProtocolVersion);
                br.Write(AvailableProtocol);
                br.Write(DataLength);
            }
        }
    }
}
