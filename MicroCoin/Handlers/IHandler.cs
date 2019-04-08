using MicroCoin.Common;
using MicroCoin.Net;
using System;
using System.Collections.Generic;
using System.Text;

namespace MicroCoin.Handlers
{
    public interface IHandler
    {
        void Handle(NetworkPacket packet);
    }

    public interface IHandler<T> : IHandler where T:IStreamSerializable
    {

    }

}
