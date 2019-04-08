using System;
using System.Threading.Tasks;

namespace MicroCoin.Net
{
    public interface IDiscovery : IDisposable
    {
        void Start();
        void Stop();
        Task<bool> DiscoverFixedSeedServers();
    }
}