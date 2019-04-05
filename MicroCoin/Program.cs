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
using MicroCoin.Common;
using System.Linq;

namespace MicroCoin
{
    class Program
    {

        static void Main(string[] args)
        {
            Console.WriteLine("Hello World!");

            ServiceLocator.ServiceProvider = new ServiceCollection()
                .AddSingleton<IEventAggregator, EventAggregator>()
                .AddSingleton<IBlockChain, BlockChainLiteDbFileStorage>()
                .BuildServiceProvider();

            NetClient client = new NetClient();
            if(client.Connect("127.0.0.1", 4004))
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
                ServiceLocator.EventAggregator.GetEvent<NetworkEvent>().Subscribe((e) => {
                    var blocks = e.Payload<BlockResponse>().Blocks;
                    foreach (var b in blocks)
                    {
                        Console.WriteLine("{0}: {1}", b.Header.BlockNumber, b.Header.Payload);
                        ServiceLocator.GetService<IBlockChain>().AddBlock(b);
                    }
                    if (blocks.Last().Id < 100000)
                    {
                        NetworkPacket<BlockRequest> blockRequest = new NetworkPacket<BlockRequest>(NetOperationType.Blocks, RequestType.Request);
                        blockRequest.Message = new BlockRequest
                        {
                            StartBlock = blocks.Last().Id,
                            NumberOfBlocks = 10000
                        };
                        e.Client.Send(blockRequest);
                    }
                    //     Block block = ServiceLocator.GetService<IBlockChain>().GetBlock(13);
                }, ThreadOption.BackgroundThread,
                    false, (p) => p.Header.RequestType == RequestType.Response && p.Header.Operation == NetOperationType.Blocks);
                ServiceLocator.EventAggregator.GetEvent<NetworkEvent>().Subscribe(                  
                (e) => {
                    var r = e.Payload<HelloResponse>();
                    Console.WriteLine("Hello response received with block height: {0}", r.Block.Header.BlockNumber);
                    NetworkPacket<BlockRequest> blockRequest = new NetworkPacket<BlockRequest>(NetOperationType.Blocks, RequestType.Request);
                    blockRequest.Message = new BlockRequest
                    {
                        StartBlock = 99000,
                        NumberOfBlocks = 10000
                    };
                    e.Client.Send(blockRequest);
                },
                    ThreadOption.BackgroundThread, 
                    false, 
                    (np) => { return np.Header.Operation == NetOperationType.Hello && np.Header.RequestType == RequestType.Response; }
                );
                NetworkPacket<HelloRequest> networkPacket = new NetworkPacket<HelloRequest>(NetOperationType.Hello, RequestType.Request, request);
                client.Send(networkPacket);
                Console.ReadLine();
                client.Dispose();
                ServiceLocator.ServiceProvider.Dispose();
            }
        }
    }
}
