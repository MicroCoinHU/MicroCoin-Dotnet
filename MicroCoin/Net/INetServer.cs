using System;

namespace MicroCoin.Net
{
    public interface INetServer : IDisposable
    {
        void Start();
    }
}