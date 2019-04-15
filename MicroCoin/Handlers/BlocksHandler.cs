//-----------------------------------------------------------------------
// This file is part of MicroCoin - The first hungarian cryptocurrency
// Copyright (c) 2019 Peter Nemeth
// BlocksHandler.cs - Copyright (c) 2019 Németh Péter
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
using System.Collections.Concurrent;

namespace MicroCoin.Handlers
{
    public class BlocksHandler : IHandler<BlockRequest>, IHandler<BlockResponse>, IHandler<NewBlockRequest>
    {
        private readonly IBlockChain blockChain;
        private readonly object handlerLock = new object();
        private readonly ConcurrentBag<uint> processedBlocks = new ConcurrentBag<uint>();
        private readonly ConcurrentBag<Block> newBlocks = new ConcurrentBag<Block>();
        private readonly IPeerManager peerManager;
        public BlocksHandler(IBlockChain blockChain, IPeerManager peerManager)
        {
            this.blockChain = blockChain;
            this.peerManager = peerManager;
        }

        public void HandleResponse(NetworkPacket packet)
        {
            var blocks = packet.Payload<BlockResponse>().Blocks;
            if (blocks.Count == 0) return;
            if (blocks.All(p => p.Header.BlockSignature == 2))
            {
                blockChain.AddBlocks(blocks);
                newBlocks.Clear();
            }
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
            if (!processedBlocks.Contains(block.Id))
            {
                processedBlocks.Add(block.Id);
                newBlocks.Add(block);
                foreach (var peer in peerManager.GetNodes().Where(p => p.Connected))
                {
                    if (!peer.EndPoint.Equals(packet.Node.EndPoint))
                    {
                        peer.NetClient.Send(new NetworkPacket<NewBlockRequest>(new NewBlockRequest(block)));
                    }
                }
            }
            if (block.Header.BlockNumber > blockChain.BlockHeight)
            {
                NetworkPacket<BlockRequest> blockRequest = new NetworkPacket<BlockRequest>(NetOperationType.Blocks, RequestType.Request)
                {
                    Message = new BlockRequest
                    {
                        StartBlock = (uint)blockChain.BlockHeight + 1,
                        NumberOfBlocks = 100
                    }
                };
                packet.Node.NetClient.Send(blockRequest);
            }
        }

        public void Handle(NetworkPacket packet)
        {
            lock (handlerLock)
            {
                if (packet.Header.Operation == NetOperationType.NewBlock)
                {
                    HandleNewBlock(packet);
                }
                if (packet.Header.Operation == NetOperationType.Blocks)
                {
                    switch (packet.Header.RequestType)
                    {
                        case RequestType.Request: HandleRequest(packet); break;
                        case RequestType.Response: HandleResponse(packet); break;
                        default: return;
                    }
                }
                if (packet.Header.Operation == NetOperationType.BlockHeader)
                {
                    switch (packet.Header.RequestType)
                    {
                        case RequestType.Request: HandleBlockHeaderRequest(packet); break;
                        case RequestType.Response: HandleResponse(packet); break;
                        default: return;
                    }
                }
            }
        }

        private void HandleRequest(NetworkPacket packet)
        {
            var request = packet.Payload<BlockRequest>();
            var blocks = blockChain.GetBlocks(request.StartBlock, request.EndBlock);
            if (blocks.Count() == request.EndBlock - request.StartBlock + 1)
            {
                packet.Node.NetClient.Send(new NetworkPacket<BlockResponse>(new BlockResponse(blocks)), packet.Header.RequestId);
            }
        }

        private void HandleBlockHeaderRequest(NetworkPacket packet)
        {
            var request = packet.Payload<BlockHeaderRequest>();
            var response = new BlockHeaderResponse
            {
                Blocks = new HashSet<Block>()
            };
            for (uint i = request.StartBlock; i <= request.EndBlock; i++)
            {
                if (newBlocks.Any(p => p.Id == i))
                {
                    response.Blocks.Add(newBlocks.First(p => p.Id == i));
                }
                else
                {
                    var block = blockChain.GetBlock(i);
                    if (block != null)
                    {
                        response.Blocks.Add(block);
                    }
                }
            }
            if (response.Blocks.Count > 0)
            {
                packet.Node.NetClient.Send(new NetworkPacket<BlockHeaderResponse>(response), packet.Header.RequestId);
            }
        }
    }
}