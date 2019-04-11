//-----------------------------------------------------------------------
// This file is part of MicroCoin - The first hungarian cryptocurrency
// Copyright (c) 2019 Peter Nemeth
// CheckPointHandler.cs - Copyright (c) 2019 %UserDisplayName%
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
using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.Extensions.DependencyInjection;
using MicroCoin.Protocol;
using MicroCoin.BlockChain;

namespace MicroCoin.Handlers
{
    public class CheckPointHandler : IHandler<CheckPointResponse>
    {
        private readonly IBlockChain blockChain;

        public CheckPointHandler(IBlockChain blockChain)
        {
            this.blockChain = blockChain;
        }

        public void HandleResponse(NetworkPacket packet)
        {
            var data = packet.Payload<CheckPointResponse>();
            var end = data.EndBlock + 10000;
            if (end > (blockChain.BlockHeight / 100) * 100 - 1)
            {
                end = (uint)(blockChain.BlockHeight / 100) * 100 - 1;
            }
            CheckPointRequest dt = new CheckPointRequest()
            {
                CheckPointBlockCount = (uint)(blockChain.BlockHeight / 100) * 100,
                StartBlock = data.EndBlock,
                EndBlock = end,
                CheckPointHash = blockChain.GetBlock((uint)((blockChain.BlockHeight / 100) * 100)).Header.CheckPointHash
            };
            NetworkPacket<CheckPointRequest> np = new NetworkPacket<CheckPointRequest>(NetOperationType.CheckPoint, RequestType.Request, dt);
            packet.Node.NetClient.Send(np);

        }

        public void Handle(NetworkPacket packet)
        {
            if (packet.Header.RequestType == RequestType.Response)
            {
                HandleResponse(packet);
            }
        }
    }
}
