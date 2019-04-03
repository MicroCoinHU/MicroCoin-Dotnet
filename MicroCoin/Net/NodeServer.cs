using MicroCoin.Utils;
using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Text;

namespace MicroCoin.Net
{
    public class NodeServer
    {
        public ByteString IP { get; set; }
        public ushort Port { get; set; }
        public Timestamp LastConnection { get; set; }
        public IPEndPoint EndPoint => new IPEndPoint(IPAddress.Parse(IP), Port);
        public TcpClient TcpClient { get; set; }
        public bool Connected { get; set; }
        public ushort ServerPort { get; internal set; }

        private readonly object _clientLock = new object();
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