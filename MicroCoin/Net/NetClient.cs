using MicroCoin.Protocol;
using Prism.Events;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.IO;
using System.Net.Sockets;
using System.Text;
using System.Threading;

namespace MicroCoin.Net
{
    public class NetworkEvent : PubSubEvent<NetworkPacket> { }

    public class NetClient : IDisposable
    {
        private readonly object clientLock = new object();

        protected TcpClient TcpClient { get; set; } = new TcpClient();
        private Thread Thread { get; set; }
        public bool IsConnected { get; set; }
        public bool Connect(string remoteHost, ushort port, int timeout = 10000)
        {
            lock (clientLock)
            {
                if (IsConnected) return IsConnected;
                var asyncResult = TcpClient.BeginConnect(remoteHost, port, null, null);
                IsConnected = asyncResult.AsyncWaitHandle.WaitOne(TimeSpan.FromMilliseconds(timeout));
                TcpClient.EndConnect(asyncResult);
                if (IsConnected)
                {
                    Thread = new Thread(HandleClient);
                    Thread.Start();
                }
                return IsConnected;
            }
        }

        public void Send(NetworkPacket packet)
        {
            lock (clientLock)
            {
                using (var sendStream = new MemoryStream())
                {
                    packet.Header.DataLength = packet.Data.Length;
                    packet.Header.SaveToStream(sendStream);
                    sendStream.Write(packet.Data, 0, packet.Data.Length);
                    sendStream.Position = 0;
                    sendStream.CopyTo(TcpClient.GetStream());
                    TcpClient.GetStream().Flush();
                }
            }
        }

        public void HandleClient()
        {
            try
            {
                var stream = TcpClient.GetStream();
                using (var br = new BinaryReader(stream, Encoding.Default, true))
                {
                    try
                    {
                        while (true)
                        {                            
                            var header = new PacketHeader
                            {
                                Magic = br.ReadUInt32()
                            };
                            if (TcpClient.Available < PacketHeader.Size - sizeof(uint))
                            {
                                break;
                            }
                            if (header.Magic == Params.NetworkPacketMagic)
                            {
                                header.RequestType = (RequestType)br.ReadUInt16();
                                header.Operation = (NetOperationType)br.ReadUInt16();
                                header.Error = br.ReadUInt16();
                                header.RequestId = br.ReadUInt32();
                                header.ProtocolVersion = br.ReadUInt16();
                                header.AvailableProtocol = br.ReadUInt16();
                                header.DataLength = br.ReadInt32();

                                if (TcpClient.Available < header.DataLength)
                                    return;

                                var packet = new NetworkPacket(header)
                                {
                                    Client = this,
                                    Data = br.ReadBytes(header.DataLength)
                                };

                                Program.ServiceProvider
                                    .GetService<IEventAggregator>()
                                    .GetEvent<NetworkEvent>()
                                    .Publish(packet);
                            }
                            else
                            {
                                break;
                            }
                        }
                    }
                    catch(IOException e)
                    {
                        return;
                    }
                }
            }
            finally
            {
                if (TcpClient.Connected)
                {
                    TcpClient.Close();
                }
            }
        }

        public void Dispose()
        {
            if (TcpClient.Connected)
            {
                TcpClient.Close();
            }
            TcpClient.Dispose();
            GC.SuppressFinalize(this);
        }
    }
}
