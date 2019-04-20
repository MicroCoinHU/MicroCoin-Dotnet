using MicroCoin.Common;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;

namespace MicroCoin.Net
{
    public class NetServer : INetServer
    {
        private readonly TcpListener tcpListener = new TcpListener(IPAddress.Any, Params.ServerPort);
        private Thread listenerThread = null;
        private readonly IPeerManager peerManager;
        private readonly ILogger<NetServer> logger;

        public NetServer(IPeerManager peerManager, ILogger<NetServer> logger)
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
                    var client = tcpListener.AcceptTcpClient();
                    if (client == null) continue;
                    logger?.LogInformation("New client connection {0}", client.Client.RemoteEndPoint);
                    var netClient = ServiceLocator.GetService<INetClient>();
                    peerManager.AddNew(netClient.HandleClient(client));
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
