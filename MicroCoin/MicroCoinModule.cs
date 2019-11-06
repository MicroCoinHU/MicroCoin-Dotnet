//-----------------------------------------------------------------------
// This file is part of MicroCoin - The first hungarian cryptocurrency
// Copyright (c) 2019 Peter Nemeth
// MicroCoinModule.cs - Copyright (c) 2019 Németh Péter
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
using MicroCoin.BlockChain;
using MicroCoin.CheckPoints;
using MicroCoin.Cryptography;
using MicroCoin.Handlers;
using MicroCoin.Modularization;
using MicroCoin.Net;
using MicroCoin.Net.Events;
using MicroCoin.Protocol;
using MicroCoin.Transactions;
using MicroCoin.Transactions.Validators;
using Microsoft.Extensions.DependencyInjection;
using Prism.Events;
using System;

namespace MicroCoin
{
    public class MicroCoinModule : IModule
    {
        public string Name => "MicroCoin main module";

        public void InitModule(IServiceProvider serviceProvider)
        {
            serviceProvider.GetService<ICheckPointService>().LoadFromBlockChain();            
         
            serviceProvider
                .GetService<IEventAggregator>()
                .GetEvent<NetworkEvent>()
                .Subscribe(serviceProvider.GetService<IHandler<HelloRequest>>().Handle,
                    ThreadOption.BackgroundThread,
                    false, (p) => p.Header.Operation == NetOperationType.Hello);

            serviceProvider
                .GetService<IEventAggregator>()
                .GetEvent<NetworkEvent>()
                .Subscribe(serviceProvider.GetService<IHandler<NewTransactionRequest>>().Handle,
                ThreadOption.BackgroundThread,
                false, (p) => p.Header.Operation == NetOperationType.NewTransaction && p.Header.RequestType == RequestType.AutoSend);

            serviceProvider
                .GetService<IEventAggregator>()
                .GetEvent<NetworkEvent>()
                .Subscribe(serviceProvider.GetService<IHandler<BlockResponse>>().Handle,
                ThreadOption.BackgroundThread,
                false,
                (p) => p.Header.Operation == NetOperationType.Blocks ||
                       p.Header.Operation == NetOperationType.NewBlock ||
                       p.Header.Operation == NetOperationType.BlockHeader
                       );

            serviceProvider
                .GetService<IEventAggregator>()
                .GetEvent<NetworkEvent>().Subscribe(
                serviceProvider.GetService<IHandler<CheckPointResponse>>().Handle,
                ThreadOption.BackgroundThread,
                false,
                p => p.Header.Operation == NetOperationType.CheckPoint && p.Header.RequestType == RequestType.Response);

            serviceProvider
                .GetService<IEventAggregator>()
                .GetEvent<NewServerConnection>().Subscribe((node) =>
                {
                    if (node.NetClient != null && node.Connected)
                        node.NetClient.Send(new NetworkPacket<HelloRequest>(HelloRequest.NewRequest(serviceProvider.GetService<IBlockChain>(), serviceProvider.GetService<IBlockFactory>())));
                }, ThreadOption.BackgroundThread, false);
        }

        public void RegisterModule(ServiceCollection serviceCollection)
        {
            serviceCollection
                  .AddSingleton<IBlockChain, BlockChainService>()
                  .AddSingleton<ICheckPointService, CheckPointService>()
                  .AddSingleton<IPeerManager, PeerManager>()
                  .AddSingleton<IBlockFactory, BlockFactory>()
                  .AddSingleton<ICryptoService, CryptoService>()
                  .AddSingleton<IHandler<NewTransactionRequest>, NewTransactionHandler>()
                  .AddSingleton<IHandler<HelloRequest>, HelloHandler>()
                  .AddSingleton<IHandler<HelloResponse>, HelloHandler>()
                  .AddSingleton<IHandler<BlockResponse>, BlocksHandler>()
                  .AddSingleton<IHandler<NewBlockRequest>, BlocksHandler>()
                  .AddSingleton<IHandler<CheckPointResponse>, CheckPointHandler>()
                  .AddSingleton<IDiscovery, Discovery>()
                  .AddSingleton<ITransactionValidator<TransferTransaction>, TransferTransactionValidator>()
                  .AddSingleton<ITransactionValidator<ChangeKeyTransaction>, ChangeKeyTransactionValidator>()
                  .AddSingleton<ITransactionValidator<ChangeAccountInfoTransaction>, ChangeAccountInfoTransactionValidator>()
                  .AddSingleton<ITransactionValidator<ListAccountTransaction>, ListAccountTransactionValidator>();
        }
    }
}
