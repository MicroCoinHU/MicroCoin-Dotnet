using MicroCoin.BlockChain;
using MicroCoin.Cryptography;
using MicroCoin.Net;
using MicroCoin.Protocol;
using System;
using System.Collections.Generic;
using System.Text;

namespace MicroCoin.Handlers
{
    public class HelloHandler : IHandler<HelloRequest>, IHandler<HelloResponse>
    {
        private readonly IBlockChain blockChain;
        private readonly IPeerManager peerManager;
        public HelloHandler(IBlockChain blockChain, IPeerManager peerManager)
        {
            this.blockChain = blockChain;
            this.peerManager = peerManager;
        }

        protected void HandleRequest(NetworkPacket packet)
        {
            var hello = packet.Payload<HelloRequest>();
            var response = new HelloResponse()
            {
                AccountKey = ECKeyPair.CreateNew(),
                NodeServers = new NodeServerList(),
                ServerPort = Params.ServerPort,
                Timestamp = DateTime.UtcNow,
                Version = "2.0.0wN",
                Block = blockChain.GetBlock((uint)blockChain.BlockHeight),
                WorkSum = blockChain.GetWorkSum()
            };
            packet.Node.NetClient.Send(new NetworkPacket<HelloResponse>(NetOperationType.Hello, RequestType.Response, response));
            CheckPeers(hello.NodeServers);
            if (hello.Block.Header.BlockNumber > blockChain.BlockHeight)
            {
                var blockRequest = new BlockRequest()
                {
                    StartBlock = (uint) (blockChain.BlockHeight + 1),
                    NumberOfBlocks = 10000
                };
                packet.Node.NetClient.Send(new NetworkPacket<BlockRequest>(NetOperationType.Blocks, RequestType.Request, blockRequest));
            }
        }

        protected void CheckPeers(NodeServerList peers)
        {
            foreach (var node in peers)
            {
                peerManager.AddNew(node.Value);
            }
        }

        protected void HandleResponse(NetworkPacket packet)
        {
            var hello = packet.Payload<HelloRequest>();
            CheckPeers(hello.NodeServers);
            if (hello.Block.Header.BlockNumber > blockChain.BlockHeight)
            {
                var blockRequest = new BlockRequest()
                {
                    StartBlock = (uint)(blockChain.BlockHeight + 1),
                    NumberOfBlocks = 10000
                };
                packet.Node.NetClient.Send(new NetworkPacket<BlockRequest>(NetOperationType.Blocks, RequestType.Request, blockRequest));
            }
        }

        public void Handle(NetworkPacket packet)
        {
            Console.WriteLine("Handle hello {0} from {1}", packet.Header.RequestType, packet.Node.EndPoint);
            switch (packet.Header.RequestType)
            {
                case RequestType.Request: HandleRequest(packet); break;
                case RequestType.Response: HandleResponse(packet); break;
                default: throw new ArgumentException("Not a hello message received");
            }
        }
    }
}
