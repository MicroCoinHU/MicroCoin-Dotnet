using MicroCoin.Protocol;
using System;
using System.IO;
using System.Net.Sockets;
using System.Text;
using System.Threading;

namespace MicroCoin.Net
{
    public class NetClient : IDisposable
    {
        public event EventHandler<NetworkPacket> PacketReceived;
        public event EventHandler Connected;
        public event EventHandler Disconnected;

        private object clientLock = new object();

        protected TcpClient tcpClient = new TcpClient();
        public bool IsConnected { get; set; }

        public bool Connect(string remoteHost, ushort port, int timeout = 10000)
        {
            lock (clientLock)
            {
                if (IsConnected) return IsConnected;
                var asyncResult = tcpClient.BeginConnect(remoteHost, port, null, null);
                IsConnected = asyncResult.AsyncWaitHandle.WaitOne(TimeSpan.FromMilliseconds(timeout));
                tcpClient.EndConnect(asyncResult);
                if (IsConnected)
                {
                    new Thread(HandleClient).Start();
                    Connected?.Invoke(this, EventArgs.Empty);
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
                    sendStream.CopyTo(tcpClient.GetStream());
                    tcpClient.GetStream().Flush();
                }
            }
        }

        public void HandleClient()
        {
            var stream = tcpClient.GetStream();
            using (var br = new BinaryReader(stream, Encoding.Default, true))
            {
                while (true)
                {
                    var header = new PacketHeader
                    {
                        Magic = br.ReadUInt32()
                    };
                    if (tcpClient.Available < PacketHeader.Size - sizeof(uint))
                    {
                        break;
                    }
                    if (header.Magic == Params.NetworkPacketMagic)
                    {
                        header.RequestType = (RequestType) br.ReadUInt16();
                        header.Operation = (NetOperationType) br.ReadUInt16();
                        header.Error = br.ReadUInt16();
                        header.RequestId = br.ReadUInt32();
                        header.ProtocolVersion = br.ReadUInt16();
                        header.AvailableProtocol = br.ReadUInt16();
                        header.DataLength = br.ReadInt32();
                        if (tcpClient.Available < header.DataLength) return;
                        if (header.Operation == NetOperationType.Hello && header.RequestType == RequestType.Response)
                        {
                            var packet = new NetworkPacket<HelloResponse>(header)
                            {
                                Data = br.ReadBytes(header.DataLength)
                            };
                            PacketReceived?.Invoke(this, packet);
                        }
                        else
                        {
                            var packet = new NetworkPacket(header)
                            {
                                Data = br.ReadBytes(header.DataLength)
                            };
                            PacketReceived?.Invoke(this, packet);
                        }
                    }
                }
            }
            if (tcpClient.Connected)
            {
                tcpClient.Close();
                Disconnected?.Invoke(this, EventArgs.Empty);
            }
        }

        public void Dispose()
        {
            if (tcpClient.Connected)
            {
                Disconnected?.Invoke(this, EventArgs.Empty);
                tcpClient.Close();
            }
            tcpClient.Dispose();
            GC.SuppressFinalize(this);
        }
    }
}
