//-----------------------------------------------------------------------
// This file is part of MicroCoin - The first hungarian cryptocurrency
// Copyright (c) 2019 Peter Nemeth
// NetClient.cs - Copyright (c) 2019 Németh Péter
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
using MicroCoin.Protocol;
using Prism.Events;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.IO;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using MicroCoin.Common;
using MicroCoin.Types;
using System.Net;
using System.Threading.Tasks;

namespace MicroCoin.Net
{
    public class NetworkEvent : PubSubEvent<NetworkPacket> { }

    public class NetClient : IDisposable
    {
        private readonly object clientLock = new object();
        protected TcpClient TcpClient { get; set; } = new TcpClient();
        private Thread Thread { get; set; }
        public Node Node { get; set; }
        public bool IsConnected { get; set; }
        public bool Started { get; set; }
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
                    Node = new Node
                    {
                        IP = remoteHost,
                        Port = port,
                        TcpClient = TcpClient,
                        LastConnection = DateTime.Now,
                        Connected = true
                    };
                }
                return IsConnected;
            }
        }

        public void Start()
        {
            lock (clientLock)
            {
                Started = true;
                Thread = new Thread(HandleClient);
                Thread.Start();
            }
        }

        public async Task<NetworkPacket> SendAndWaitAsync(NetworkPacket packet)
        {
            lock (clientLock)
            {
                if (Started) throw new InvalidOperationException("Client already started");
                if (!IsConnected) throw new InvalidOperationException("Not connected");
            }
            using (var sendStream = new MemoryStream())
            {
                packet.Header.DataLength = packet.RawData.Length;
                packet.Header.SaveToStream(sendStream);
                await sendStream.WriteAsync(packet.RawData, 0, packet.RawData.Length);
                sendStream.Position = 0;
                await sendStream.CopyToAsync(TcpClient.GetStream());
                await TcpClient.GetStream().FlushAsync();
                sendStream.SetLength(0);
                using (BinaryReader br = new BinaryReader(TcpClient.GetStream(), Encoding.ASCII, true))
                {
                    do
                    {
                        var header = new PacketHeader
                        {
                            Magic = br.ReadUInt32()
                        };
                        if (header.Magic == Params.NetworkPacketMagic)
                        {
                            header.RequestType = (RequestType)br.ReadUInt16();
                            header.Operation = (NetOperationType)br.ReadUInt16();
                            header.Error = br.ReadUInt16();
                            header.RequestId = br.ReadUInt32();
                            header.ProtocolVersion = br.ReadUInt16();
                            header.AvailableProtocol = br.ReadUInt16();
                            header.DataLength = br.ReadInt32();
                            if (header.Error > 0)
                            {
                                ByteString message = br.ReadBytes(header.DataLength);
                                throw new Exception(message);
                            }
                            if (header.RequestType != RequestType.Response || header.Operation != packet.Header.Operation || header.RequestId != packet.Header.RequestId)
                            {
                                br.ReadBytes(header.DataLength);
                                continue;
                            }
                            return new NetworkPacket(header)
                            {
                                Client = this,
                                RawData = br.ReadBytes(header.DataLength)
                            };
                        }
                        throw new InvalidDataException("Invalid magic");
                    } while (true);
                }
            }
        }

        public void Send(NetworkPacket packet)
        {
            lock (clientLock)
            {
                using (var sendStream = new MemoryStream())
                {
                    packet.Header.DataLength = packet.RawData.Length;
                    packet.Header.SaveToStream(sendStream);
                    sendStream.Write(packet.RawData, 0, packet.RawData.Length);
                    sendStream.Position = 0;
                    sendStream.CopyTo(TcpClient.GetStream());
                    TcpClient.GetStream().Flush();
                }
            }
        }

        protected void HandleClient()
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

                                if (header.Error > 0)
                                {
                                    ByteString message = br.ReadBytes(header.DataLength);
                                    Console.WriteLine(this.TcpClient.Client.RemoteEndPoint.ToString() + " " + message);
                                    break;
                                }
                                var packet = new NetworkPacket(header)
                                {
                                    Client = this,
                                    RawData = br.ReadBytes(header.DataLength)
                                };
                                
                                ServiceLocator
                                    .EventAggregator
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
                        Console.WriteLine(e.Message);
                        return;
                    }
                }
            }
            finally
            {
                IsConnected = false;
                if (TcpClient.Connected)
                {
                    Console.WriteLine("Disconnected from {0}", Node?.EndPoint.ToString());
                    TcpClient.Close();
                }
            }
        }

        public void Dispose()
        {
            if (TcpClient.Connected)
            {
                TcpClient.Close();
                IsConnected = false;
            }
            TcpClient.Dispose();
            GC.SuppressFinalize(this);
        }
    }
}
