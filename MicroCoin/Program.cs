using MicroCoin.BlockChain;
using MicroCoin.Net;
using MicroCoin.Protocol;
using MicroCoin.Utils;
using System;
using System.Text;

namespace MicroCoin
{
    class Program
    {
        static void Main(string[] args)
        {
            Console.WriteLine("Hello World!");
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
                NetworkPacket<HelloRequest> networkPacket = new NetworkPacket<HelloRequest>(NetOperationType.Hello, RequestType.Request, request);
                client.Send(networkPacket);
            }
        }
    }
}
