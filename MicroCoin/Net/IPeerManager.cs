using System;
using System.Collections.Generic;
using System.Text;

namespace MicroCoin.Net
{
    public interface IPeerManager : IDisposable
    {
        void AddNew(Node node);
        void Remove(Node node);
        IEnumerable<Node> GetNodes();

    }
}
