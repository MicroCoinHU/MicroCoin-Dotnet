using System;
using System.Threading.Tasks;

namespace MicroCoin.Net
{
    public interface INetClient : IDisposable
    {
        bool IsConnected { get; }
        bool Started { get; set; }
        bool Connect(Node node, int timeout = 500);
        void Send(NetworkPacket packet);
        Task<NetworkPacket> SendAndWaitAsync(NetworkPacket packet);
        void Start();
    }
}