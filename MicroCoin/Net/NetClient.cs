using MicroCoin.Protocol;
using System;
using System.Collections.Generic;
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

        public bool Connect(string IP, ushort port, int timeout = 10000)
        {
            var asyncResult = tcpClient.BeginConnect(IP, port, null, null);
            IsConnected = asyncResult.AsyncWaitHandle.WaitOne(TimeSpan.FromMilliseconds(timeout));
            tcpClient.EndConnect(asyncResult);
            new Thread(HandleClient).Start();
            if (IsConnected)
                Connected?.Invoke(this, new EventArgs());
            return IsConnected;
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
                    var packet = new NetworkPacket();
                    packet.Header.Magic = br.ReadUInt32();
                    if (tcpClient.Available < PacketHeader.Size - sizeof(uint))
                    {
                        break;
                    }
                    if (packet.Header.Magic == Params.NetworkPacketMagic)
                    {
                        packet.Header.RequestType = (RequestType) br.ReadUInt16();
                        packet.Header.Operation = (NetOperationType) br.ReadUInt16();
                        packet.Header.Error = br.ReadUInt16();
                        packet.Header.RequestId = br.ReadUInt32();
                        packet.Header.ProtocolVersion = br.ReadUInt16();
                        packet.Header.AvailableProtocol = br.ReadUInt16();
                        packet.Header.DataLength = br.ReadInt32();
                        if (tcpClient.Available < packet.Header.DataLength) return;
                        packet.Data = br.ReadBytes(packet.Header.DataLength);
                        PacketReceived?.Invoke(this, packet);
                    }
                }
            }
            if (tcpClient.Connected)
            {
                tcpClient.Close();
                Disconnected?.Invoke(this, new EventArgs());
            }
        }

        public void Dispose()
        {
            if (tcpClient.Connected)
            {
                Disconnected?.Invoke(this, new EventArgs());
                tcpClient.Close();
            }
            tcpClient.Dispose();
            GC.SuppressFinalize(this);
        }
    }
}
