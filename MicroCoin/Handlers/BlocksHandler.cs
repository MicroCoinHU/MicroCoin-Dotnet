using MicroCoin.Net;
using MicroCoin.Protocol;
using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.Extensions.DependencyInjection;
using MicroCoin.BlockChain;
using System.Linq;

namespace MicroCoin.Handlers
{
    public class BlocksHandler : IHandler<BlockRequest>, IHandler<BlockResponse>, IHandler<NewBlockRequest>
    {
        private readonly IBlockChain blockChain;
        public BlocksHandler(IBlockChain blockChain)
        {
            this.blockChain = blockChain;
        }

        public void HandleResponse(NetworkPacket packet)
        {
            var blocks = packet.Payload<BlockResponse>().Blocks;
            if (blocks.Count == 0) return;
            foreach (var b in blocks)
            {
                Console.WriteLine("{0}: {1}", b.Header.BlockNumber, b.Header.Payload);
            }
            blockChain.AddBlocks(blocks);
            
            if (blocks.Last().Id < packet.Node.BlockHeight)
            {
                NetworkPacket<BlockRequest> blockRequest = new NetworkPacket<BlockRequest>(NetOperationType.Blocks, RequestType.Request)
                {
                    Message = new BlockRequest
                    {
                        StartBlock = blocks.Last().Id,
                        NumberOfBlocks = 10000
                    }
                };
                packet.Node.NetClient.Send(blockRequest);
            }
        }

        public void HandleNewBlock(NetworkPacket packet)
        {
            var block = packet.Payload<NewBlockRequest>().Block;
            Console.WriteLine("New block received with height {0}", block.Header.BlockNumber);
            if (block.Header.BlockNumber > blockChain.BlockHeight)
            {
                NetworkPacket<BlockRequest> blockRequest = new NetworkPacket<BlockRequest>(NetOperationType.Blocks, RequestType.Request)
                {
                    Message = new BlockRequest
                    {
                        StartBlock = (uint)blockChain.BlockHeight,
                        NumberOfBlocks = 100
                    }
                };
                packet.Node.NetClient.Send(blockRequest);
            }
        }

        public void Handle(NetworkPacket packet)
        {
            if (packet.Header.Operation == NetOperationType.NewBlock) HandleNewBlock(packet);
            if(packet.Header.Operation == NetOperationType.Blocks)
            {
                switch (packet.Header.RequestType)
                {
                    case RequestType.Request: return;
                    case RequestType.Response: HandleResponse(packet); break;
                    default: return;
                }
            }
        }
    }
}
