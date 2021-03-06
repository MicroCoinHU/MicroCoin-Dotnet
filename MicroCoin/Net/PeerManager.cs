﻿//-----------------------------------------------------------------------
// This file is part of MicroCoin - The first hungarian cryptocurrency
// Copyright (c) 2019 Peter Nemeth
// PeerManager.cs - Copyright (c) 2019 Németh Péter
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
using System.Collections.Generic;
using System.Linq;
using Microsoft.Extensions.Logging;

namespace MicroCoin.Net
{
    public class PeerManager : IPeerManager
    {
        private readonly IList<Node> peers = new List<Node>();
        private readonly object lobj = new object();
        private readonly ILogger<IPeerManager> logger;

        public PeerManager(ILogger<IPeerManager> logger)
        {
            this.logger = logger;
        }


        public void AddNew(Node node)
        {
            lock (lobj)
            {
                if(!peers.Any(p=>(p.IP == node.IP) && (p.Port == node.Port)))
                {
                    peers.Add(node);
                    logger.LogTrace("{0} added to peers", node.EndPoint);
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
                logger.LogTrace("{0} removed from peers", node.EndPoint);
                node.NetClient?.Dispose();
            }
        }
    }
}
