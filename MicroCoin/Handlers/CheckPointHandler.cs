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
