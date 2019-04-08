using System;
using System.Collections.Generic;
using System.Text;
using System.Linq;
using System.Threading;
using Prism.Events;
using MicroCoin.Protocol;
using MicroCoin.BlockChain;
using MicroCoin.Common;
using System.Threading.Tasks;

namespace MicroCoin.Net
{


    public class Discovery : IDiscovery
    {
        private readonly IPeerManager peerManager;
        private Thread discoveryThread;
        private readonly IBlockChain blockChain;

        public Discovery(IPeerManager peerManager, IBlockChain blockChain)
        {
            this.peerManager = peerManager;
            this.blockChain = blockChain;
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
            Console.WriteLine("Discoverfixed");
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
                        Console.WriteLine("Send hello");
                        var hello = await cl.SendAndWaitAsync(networkPacket);
                        node.BlockHeight = hello.Payload<HelloResponse>().Block.Header.BlockNumber;
                        peerManager.AddNew(node);
                        Console.WriteLine("{0} alive", server.Address);
                    }
                    catch (Exception e)
                    {
                        Console.WriteLine("{0} dead", server.Address);
                        cl.Dispose();
                    }
                }
                else
                {
                    Console.WriteLine("{0} dead", server.Address);
                    cl.Dispose();
                }
            }
            Console.WriteLine("Fixed ended");
            return peerManager.GetNodes().Count() > 0;
        }

        protected void Discover()
        {
            try
            {
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
                    var rand = new Random();
                    var needHello = peerManager.GetNodes().Where(p => p.Connected && p.LastConnection < DateTime.Now.Subtract(new TimeSpan(0, 0, 30 + rand.Next(30))));
                    foreach (var node in needHello)
                    {
                        if (!node.NetClient.Started) node.NetClient.Start();
                    }
                    var nodesToConnect = peerManager.GetNodes().Where(p => p.Connected == false);
                    foreach (var elem in nodesToConnect)
                    {
                        Console.WriteLine("Discovering peer: {0}", elem.EndPoint);
                        var client = ServiceLocator.GetService<INetClient>();
                        client.Connect(elem);
                        if (elem.NetClient!=null && elem.NetClient.IsConnected)
                        {
                            elem.NetClient.Start();
                            Console.WriteLine("Peer {0} alive", elem.EndPoint);
                        }
                        else
                        {
                            Console.WriteLine("Peer {0} died", elem.EndPoint);
                            elem.NetClient?.Dispose();
                            elem.NetClient = null;
                            peerManager.Remove(elem);
                        }
                    }
                    Thread.Sleep(1000);
                }
            }catch(ThreadInterruptedException e)
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
