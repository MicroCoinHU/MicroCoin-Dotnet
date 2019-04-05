using MicroCoin.BlockChain;
using MicroCoin.Net;
using MicroCoin.Protocol;
using MicroCoin.Utils;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.ComponentModel;
using System.Text;
using Prism.Events;
using System.Threading;

namespace MicroCoin
{
    class Program
    {
        public static ServiceProvider ServiceProvider { get; set; }

        static void Main(string[] args)
        {
            Console.WriteLine("Hello World!");
            
            ServiceProvider = new ServiceCollection()
                .AddSingleton<IEventAggregator, EventAggregator>()                
                .BuildServiceProvider();

            NetClient client = new NetClient();
            if(client.Connect("blockexplorer.microcoin.hu", 4004))
            {
                HelloRequest request = new HelloRequest
                {
                    AccountKey = Cryptography.ECKeyPair.CreateNew(),
                    NodeServers = new NodeServerList()                   
                };
                Hash h = Cryptography.Utils.Sha256(Encoding.ASCII.GetBytes(Params.GenesisPayload));
                request.Block = new Block
                {
                    Header = new BlockHeader
                    {                        
                        AccountKey = null,
                        AvailableProtocol = 0,
                        BlockNumber = 0,
                        CompactTarget = 0,
                        Fee = 0,
                        Nonce = 0,
                        TransactionHash = new byte[0],
                        Payload = new byte[0],
                        ProofOfWork = new byte[0],
                        ProtocolVersion = 0,
                        Reward = 0,
                        CheckPointHash = h,
                        BlockSignature = 3,
                        Timestamp = 0
                    }
                };
                request.ServerPort = 1234;
                request.Timestamp = DateTime.UtcNow;
                request.Version = "2.0.0wN";
                request.WorkSum = 0;
                ServiceProvider.GetService<IEventAggregator>().GetEvent<NetworkEvent>().Subscribe(                  
                (e) => {
                    var r = e.Payload<HelloResponse>();
                    Console.WriteLine("Hello response received with block height: {0}", r.Block.Header.BlockNumber);
                },
                    ThreadOption.BackgroundThread, 
                    false, 
                    (np) => { return np.Header.Operation == NetOperationType.Hello && np.Header.RequestType == RequestType.Response; }
                );
                NetworkPacket<HelloRequest> networkPacket = new NetworkPacket<HelloRequest>(NetOperationType.Hello, RequestType.Request, request);
                client.Send(networkPacket);
                Console.ReadLine();
                client.Dispose();
            }
        }
    }
}
