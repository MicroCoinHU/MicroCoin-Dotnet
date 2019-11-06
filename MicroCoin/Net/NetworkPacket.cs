//-----------------------------------------------------------------------
// This file is part of MicroCoin - The first hungarian cryptocurrency
// Copyright (c) 2019 Peter Nemeth
// NetworkPacket.cs - Copyright (c) 2019 Németh Péter
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
using MicroCoin.Protocol;
using System.IO;

namespace MicroCoin.Net
{
    public class NetworkPacket
    {
        public PacketHeader Header { get; private set; }
        public byte[] RawData { get; set; }
        public Node Node { get; set; }

        public NetworkPacket(NetOperationType netOperationType, RequestType requestType)
        {
            Header = new PacketHeader
            {
                AvailableProtocol = Params.NetworkProtocolAvailable,
                ProtocolVersion = Params.NetworkProtocolVersion,
                Error = 0,
                Magic = Params.NetworkPacketMagic,
                RequestType = requestType,
                Operation = netOperationType
            };
        }

        public NetworkPacket()
        {
            Header = new PacketHeader();
        }

        public NetworkPacket(PacketHeader header)
        {
            Header = header;
        }
        public NetworkPacket(PacketHeader header, byte[] data)
        {
            Header = header;
            RawData = data;
        }

        public TPacket Payload<TPacket>() where TPacket : class, IStreamSerializable, new()
        {
            TPacket message = new TPacket();
            using (var ms = new MemoryStream(RawData))
            {
                message.LoadFromStream(ms);
            }
            return message;
        }
    }

    public class NetworkPacket<T> : NetworkPacket where T : class, INetworkPayload, new()
    {
        private T message = null;
        public T Message
        {
            get
            {
                if (message == null)
                {
                    message = Payload<T>();
                }
                return message;
            }
            set
            {
                message = value;
                using (var ms = new MemoryStream())
                {
                    message.SaveToStream(ms);
                    ms.Position = 0;
                    RawData = ms.ToArray();
                }
            }
        }

        public NetworkPacket(NetOperationType netOperationType, RequestType requestType) : base(netOperationType, requestType)
        {

        }

        public NetworkPacket(NetOperationType netOperationType, RequestType requestType, T data) : base(netOperationType, requestType)
        {
            Message = data;
        }

        public NetworkPacket(T data) : this(data.NetOperation, data.RequestType, data)
        {

        }

        public NetworkPacket(PacketHeader header) : base(header)
        {

        }
    }
}