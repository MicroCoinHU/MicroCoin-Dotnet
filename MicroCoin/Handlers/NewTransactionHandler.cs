using MicroCoin.Net;
using MicroCoin.Protocol;
using MicroCoin.Transactions;
using Prism.Events;
using System;
using System.Collections.Generic;
using System.Text;

namespace MicroCoin.Handlers
{
    public class NewTransaction : PubSubEvent<ITransaction> { }
    public class NewTransactionHandler : IHandler<NewTransactionRequest>
    {
        private readonly IEventAggregator eventAggregator;
        private readonly object handlerLock = new object();

        public NewTransactionHandler(IEventAggregator eventAggregator)
        {
            this.eventAggregator = eventAggregator;
        }

        public void Handle(NetworkPacket packet)
        {
            lock (handlerLock)
            {
                var request = packet.Payload<NewTransactionRequest>();
                foreach (var transaction in request.Transactions)
                {
                    eventAggregator.GetEvent<NewTransaction>().Publish(transaction);
                }
            }
        }
    }
}
