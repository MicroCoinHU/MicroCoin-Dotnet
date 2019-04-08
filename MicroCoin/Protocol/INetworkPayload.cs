using MicroCoin.Common;
using System;
using System.Collections.Generic;
using System.Text;

namespace MicroCoin.Protocol
{
    public interface INetworkPayload : IStreamSerializable
    {
        NetOperationType NetOperation { get; }
        RequestType RequestType { get; }
    }
}
