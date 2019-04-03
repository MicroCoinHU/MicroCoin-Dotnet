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
        public static readonly object RequestLock = new object();

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
