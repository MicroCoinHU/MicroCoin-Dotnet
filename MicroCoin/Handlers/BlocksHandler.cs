//-----------------------------------------------------------------------
// This file is part of MicroCoin - The first hungarian cryptocurrency
// Copyright (c) 2019 Peter Nemeth
// BlocksHandler.cs - Copyright (c) 2019 %UserDisplayName%
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
