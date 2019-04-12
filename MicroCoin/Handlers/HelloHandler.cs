//-----------------------------------------------------------------------
// This file is part of MicroCoin - The first hungarian cryptocurrency
// Copyright (c) 2019 Peter Nemeth
// HelloHandler.cs - Copyright (c) 2019 Németh Péter
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
                Version = Params.ProgramVersion,
                Block = blockChain.GetBlock((uint)blockChain.BlockHeight),
                WorkSum = 0
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
            switch (packet.Header.RequestType)
            {
                case RequestType.Request: HandleRequest(packet); break;
                case RequestType.Response: HandleResponse(packet); break;
                default: throw new ArgumentException("Not a hello message received");
            }
        }
    }
}
