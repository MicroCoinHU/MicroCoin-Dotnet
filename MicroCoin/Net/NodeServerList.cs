//-----------------------------------------------------------------------
// This file is part of MicroCoin - The first hungarian cryptocurrency
// Copyright (c) 2019 Peter Nemeth
// NodeServerList.cs - Copyright (c) 2019 Németh Péter
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
using System;
using System.Linq;
using System.Collections.Concurrent;
using System.IO;
using System.Net;
using System.Text;

namespace MicroCoin.Net
{
    public class NodeServerList : ConcurrentDictionary<string, Node>, IDisposable
    {
        internal void SaveToStream(Stream s)
        {            
            using (BinaryWriter bw = new BinaryWriter(s, Encoding.ASCII, true))
            {
                bw.Write((uint)Count);
                foreach(var item in this)
                {
                    item.Value.IP.SaveToStream(bw);
                    bw.Write(item.Value.Port);
                    bw.Write(item.Value.LastConnection);
                }
            }
        }

        internal void TryAddNew(string key, Node nodeServer)
        {
            if (ContainsKey(key)) return;
            TryAdd(key, nodeServer);
        }

        internal static NodeServerList LoadFromStream(Stream stream, ushort serverPort)
        {
            NodeServerList ns = new NodeServerList();
            using (BinaryReader br = new BinaryReader(stream, Encoding.ASCII, true))
            {
                uint serverCount = br.ReadUInt32();
                for(int i = 0; i < serverCount; i++)
                {
                    Node server = new Node();
                    ushort iplen = br.ReadUInt16();
                    server.IP = br.ReadBytes(iplen);
                    server.Port = br.ReadUInt16();
                    server.LastConnection = br.ReadUInt32();
                    server.ServerPort = serverPort;
                    ns.TryAdd(server.ToString(), server);
                }
            }
            return ns;
        }

        internal void UpdateNodeServers(NodeServerList nodeServers)
        {
            IPAddress[] localIPs = Dns.GetHostAddresses(Dns.GetHostName());
            foreach (var nodeServer in nodeServers)
            {
                if (IPAddress.IsLoopback(nodeServer.Value.EndPoint.Address)) continue;
                if (localIPs.Contains(nodeServer.Value.EndPoint.Address)) continue;
                if (ContainsKey(nodeServer.Value.ToString())) continue;
                if (nodeServer.Value.Port != Params.ServerPort) continue;
                TryAddNew(nodeServer.Value.ToString(), nodeServer.Value);
            }
            if (Count <= 100) return;
            foreach (var l in nodeServers)
            {
                TryRemove(l.Key, out Node n);
            }
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!disposing) return;
            Clear();
        }
    }
    }