//-----------------------------------------------------------------------
// This file is part of MicroCoin - The first hungarian cryptocurrency
// Copyright (c) 2019 Peter Nemeth
// Discovery.cs - Copyright (c) 2019 Németh Péter
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
using System;
using System.Collections.Generic;
using System.Text;
using System.Linq;
using System.Threading;
using MicroCoin.Protocol;
using MicroCoin.BlockChain;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using MicroCoin.Modularization;

namespace MicroCoin.Net
{
    public class Discovery : IDiscovery
    {
        private readonly IPeerManager peerManager;
        private readonly ILogger<Discovery> logger;
        private Thread discoveryThread;

        public Discovery(IPeerManager peerManager, ILogger<Discovery> logger)
        {
            this.peerManager = peerManager;
            this.logger = logger;
            discoveryThread = new Thread(Discover)
            {
                Name = "discovery"
            };
        }

        public void Stop()
        {
            discoveryThread?.Interrupt();
            discoveryThread = null;
        }

        public void Start()
        {
            discoveryThread.Start();
        }

        public async Task<bool> DiscoverFixedSeedServers()
        {
            HelloRequest request = HelloRequest.NewRequest(ServiceLocator.GetService<IBlockChain>());
            NetworkPacket<HelloRequest> networkPacket = new NetworkPacket<HelloRequest>(request);
            logger.LogTrace("Discovering fixed servers");
            foreach (var server in Params.FixedSeedServers)
            {
                var node = new Node
                {
                    IP = server.Address.ToString(),
                    Port = (ushort)server.Port,
                    BlockHeight = 0,
                    NetClient = null
                };
                var cl = ServiceLocator.GetService<INetClient>();
                cl.Connect(node);
                if (cl.IsConnected)
                {
                    try
                    {
                        logger.LogTrace("Sending hello to {0}", node.EndPoint);
                        var hello = await cl.SendAndWaitAsync(networkPacket);
                        node.BlockHeight = hello.Payload<HelloResponse>().Block.Header.BlockNumber;
                        peerManager.AddNew(node);
                        logger.LogInformation("{0} alive", server.Address);
                    }
                    catch (Exception e)
                    {
                        logger.LogTrace("{0} dead ({1})", server.Address, e.Message);
                        cl.Dispose();
                    }
                }
                else
                {
                    logger.LogTrace("{0} dead", server.Address);
                    cl.Dispose();
                }
            }
            logger.LogTrace("All fixed servers discovered");
            return peerManager.GetNodes().Count() > 0;
        }

        protected void Discover()
        {
            try
            {
                var rand = new Random();
                while (true)
                {
                    if (peerManager.GetNodes().Count() == 0)
                    {
                        foreach (var item in Params.FixedSeedServers)
                        {
                            peerManager.AddNew(new Node
                            {
                                IP = item.Address.ToString(),
                                Port = (ushort)item.Port
                            });
                        }
                    }
                    var needHello = peerManager.GetNodes().Where(p => p.Connected && p.LastConnection < DateTime.UtcNow.Subtract(new TimeSpan(0, 0, 45 + rand.Next(30))));
                    foreach (var node in needHello)
                    {
                        if (!node.NetClient.Started) node.NetClient.Start();
                        HelloRequest request = HelloRequest.NewRequest(ServiceLocator.GetService<IBlockChain>());
                        NetworkPacket<HelloRequest> networkPacket = new NetworkPacket<HelloRequest>(request);
                        node.NetClient.Send(networkPacket);
                    }
                    var nodesToConnect = peerManager.GetNodes().Where(p => p.Connected == false && p.LastConnectionAttempt < DateTime.Now.Subtract(TimeSpan.FromSeconds(300)));
                    foreach (var elem in nodesToConnect)
                    {
                        logger.LogTrace("Discovering peer: {0}", elem.EndPoint);
                        elem.LastConnectionAttempt = DateTime.Now;
                        var client = ServiceLocator.GetService<INetClient>();
                        client.Connect(elem);
                        if (elem.NetClient != null && elem.NetClient.IsConnected)
                        {
                            elem.NetClient.Start();
                            elem.ConnectionAttemps = 0;
                            logger.LogInformation("Peer {0} alive", elem.EndPoint);
                        }
                        else
                        {
                            elem.ConnectionAttemps += 1;
                            elem.NetClient?.Dispose();
                            elem.NetClient = null;
                            logger.LogTrace("Peer {0} died. Connection attemps: {1}", elem.EndPoint, elem.ConnectionAttemps);
                        }
                    }                   
                    foreach(var peer in peerManager.GetNodes().Where(p => p.ConnectionAttemps >= 5).ToList())
                    {
                        peerManager.Remove(peer);
                    }                    
                    logger.LogInformation("Total {0} peers. {1} alive and {2} died.", peerManager.GetNodes().Count(), peerManager.GetNodes().Count(p=>p.Connected), peerManager.GetNodes().Count(p => !p.Connected));
                    logger.LogTrace("{0} disctinct hosts", peerManager.GetNodes().GroupBy(p => p.EndPoint.Address.ToString()).Count());
                    foreach(var node in peerManager.GetNodes().Where(p => p.Connected))
                    {
                        logger.LogTrace("{0} - Last connection: {1}", node.EndPoint, node.LastConnection);
                    }
                    Thread.Sleep(10000);
                }
            }
            catch (ThreadInterruptedException)
            {
                return;
            }
        }

        public void Dispose()
        {
            Stop();
        }
    }
}
