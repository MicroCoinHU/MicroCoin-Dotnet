using System;
using System.Collections.Generic;
using System.Text;
using System.Linq;

namespace MicroCoin.Net
{
    public class PeerManager : IPeerManager
    {
        private readonly IList<Node> peers = new List<Node>();
        private readonly object lobj = new object();
        public void AddNew(Node node)
        {
            lock (lobj)
            {
                if(!peers.Any(p=>p.IP == node.IP && p.Port == node.Port && p.ServerPort == node.ServerPort))
                {
                    peers.Add(node);
                }
            }
        }

        public void Dispose()
        {
            lock (lobj) {
                foreach (var item in peers)
                {
                    item.NetClient?.Dispose();
                    item.NetClient = null;                    
                }
                peers.Clear();
           }
        }

        public IEnumerable<Node> GetNodes()
        {
            lock (lobj)
            {
                return new List<Node>(peers);
            }
        }

        public void Remove(Node node)
        {
            lock (lobj)
            {
                peers.Remove(node);
            }
        }
    }
}
