//-----------------------------------------------------------------------
// This file is part of MicroCoin - The first hungarian cryptocurrency
// Copyright (c) 2019 Peter Nemeth
// NewTransactionHandler.cs - Copyright (c) 2019 Németh Péter
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
using MicroCoin.Net;
using MicroCoin.Protocol;
using MicroCoin.Transactions;
using MicroCoin.Types;
using Prism.Events;
using System.Collections.Concurrent;
using System.Linq;

namespace MicroCoin.Handlers
{
    public class NewTransaction : PubSubEvent<ITransaction> { }
    public class NewTransactionHandler : IHandler<NewTransactionRequest>
    {
        private readonly IEventAggregator eventAggregator;
        private readonly IPeerManager peerManager;
        private readonly object handlerLock = new object();
        private readonly ConcurrentBag<Hash> processedTransactions = new ConcurrentBag<Hash>();

        public NewTransactionHandler(IEventAggregator eventAggregator, IPeerManager peerManager)
        {
            this.eventAggregator = eventAggregator;
            this.peerManager = peerManager;
        }

        public void Handle(NetworkPacket packet)
        {
            lock (handlerLock)
            {
                var request = packet.Payload<NewTransactionRequest>();
                if (processedTransactions.Count(p => p.Equals(request.Transactions.First().SHA())) > 0)
                {
                    return;
                }
                foreach (var transaction in request.Transactions)
                {
                    eventAggregator.GetEvent<NewTransaction>().Publish(transaction);
                }
                foreach (var peer in peerManager.GetNodes().Where(p => p.Connected))
                {
                    if (!peer.EndPoint.Equals(packet.Node.EndPoint))
                    {
                        peer.NetClient.Send(new NetworkPacket<NewTransactionRequest>(new NewTransactionRequest(request.Transactions.ToArray())));                        
                    }
                }
                foreach(var transaction in request.Transactions)
                    processedTransactions.Add(transaction.SHA());
            }
        }
    }
}
