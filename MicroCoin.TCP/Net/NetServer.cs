//-----------------------------------------------------------------------
// This file is part of MicroCoin - The first hungarian cryptocurrency
// Copyright (c) 2019 Peter Nemeth
// NetServer.cs - Copyright (c) 2019 Németh Péter
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
using MicroCoin.Modularization;
using Microsoft.Extensions.Logging;
using System.Net;
using System.Net.Sockets;
using System.Threading;

namespace MicroCoin.Net
{
    public class NetServer : INetServer
    {
        private readonly TcpListener tcpListener = new TcpListener(IPAddress.Any, Params.Current.ServerPort);
        private Thread listenerThread = null;
        private readonly IPeerManager peerManager;
        private readonly ILogger<INetServer> logger;

        public NetServer(IPeerManager peerManager, ILogger<INetServer> logger)
        {
            this.peerManager = peerManager;
            this.logger = logger;
        }

        public void Start()
        {
            listenerThread = new Thread(() =>
            {
                while (true)
                {
                    tcpListener.Start();
                    try
                    {
                        var client = tcpListener.AcceptTcpClient();
                        if (client == null) continue;
                        logger?.LogInformation("New client connection {0}", client.Client.RemoteEndPoint);
                        var netClient = ServiceLocator.GetService<INetClient>();
                        peerManager.AddNew(netClient.HandleClient(client));
                    }
                    catch (SocketException ex)
                    {
                        logger.LogError(ex, "Socket exception");
                        return;
                    }
                }
            });
            listenerThread.Start();
        }

        public void Dispose()
        {
            tcpListener.Stop();
            listenerThread?.Interrupt();
        }
    }
}
