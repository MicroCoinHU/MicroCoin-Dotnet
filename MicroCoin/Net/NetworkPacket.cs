using MicroCoin.Common;
using MicroCoin.Protocol;
using System;
using System.IO;

namespace MicroCoin.Net
{
    public class NetworkPacket
    {
        public PacketHeader Header { get; private set; }
        public byte[] Data { get; set; }
        public NetClient Client { get; set; }

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
            Data = data;
        }

        public TPacket Payload<TPacket>() where TPacket : class, IStreamSerializable, new()
        {
            TPacket message = new TPacket();
            using (var ms = new MemoryStream(Data))
            {
                message.LoadFromStream(ms);
            }
            return message;
        }
    }

    public class NetworkPacket<T> : NetworkPacket where T : class, IStreamSerializable, new()
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
                    Data = ms.ToArray();
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

        public NetworkPacket(PacketHeader header) : base(header)
        {

        }
    }
}