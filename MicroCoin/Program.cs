//-----------------------------------------------------------------------
// This file is part of MicroCoin - The first hungarian cryptocurrency
// Copyright (c) 2019 Peter Nemeth
// Program.cs - Copyright (c) 2019 Németh Péter
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
using LiteDB;
using MicroCoin.BlockChain;
using MicroCoin.Chain;
using MicroCoin.CheckPoints;
using MicroCoin.Common;
using MicroCoin.Cryptography;
using MicroCoin.Handlers;
using MicroCoin.Net;
using MicroCoin.Protocol;
using MicroCoin.Types;
using Microsoft.Extensions.DependencyInjection;
using Prism.Events;
using System;
using System.Linq;
using System.Threading.Tasks;
using NLog;
using NLog.Extensions.Logging;
using Microsoft.Extensions.Logging;
using LogLevel = NLog.LogLevel;
using MicroCoin.Transactions;
using System.Diagnostics;
using System.IO;

namespace MicroCoin
{
    class Program
    {
        static async Task Main(string[] args)
        {
            Console.WriteLine("Hello World!");
            var mapper = BsonMapper.Global;
            mapper.ResolveMember = (type, memberInfo, memberMapper) =>
            {
                if (memberMapper.DataType  == typeof(ECCurveType))
                {
                    memberMapper.Serialize = (obj, m) => new BsonValue((int)((ushort)obj));
                    memberMapper.Deserialize = (value, m) => (ECCurveType)value.AsInt32;
                }
                else if (memberMapper.DataType == typeof(AccountState))
                {
                    memberMapper.Serialize = (obj, m) => new BsonValue((int)obj);
                    memberMapper.Deserialize = (value, m) => (AccountState)value.AsInt32;
                }
                else if(memberMapper.DataType == typeof(TransactionType))
                {
                    memberMapper.Serialize = (obj, m) => new BsonValue((int)(uint)obj);
                    memberMapper.Deserialize = (value, m) => (TransactionType)value.AsInt32;
                }
                else if (memberMapper.DataType == typeof(TransferTransaction.TransferType))
                {
                    memberMapper.Serialize = (obj, m) => new BsonValue((int)(byte)obj);
                    memberMapper.Deserialize = (value, m) => (TransferTransaction.TransferType)value.AsInt32;
                }
            };
            mapper.RegisterType<Currency>(p => (long)(ulong)p, p => (Currency)p.AsInt64);
            mapper.RegisterType<Hash>(p => (byte[])p, p => p.AsBinary);
            mapper.RegisterType<ByteString>(p => (byte[])p, p => p.AsBinary);
            mapper.RegisterType<Timestamp>(p => (DateTime)p, p => p.AsDateTime);
            mapper.RegisterType<AccountNumber>(p => (int)p, p => new AccountNumber((uint)p.AsInt32));            

            mapper.Entity<ECKeyPair>()
                .Field(p => p.CurveType, "ct")
                .Ignore(p => p.ECParameters)
                .Ignore(p => p.PublicKey)
                .Ignore(p => p.D)
                .Ignore(p => p.PrivateKey)
                .Ignore(p => p.Name);
            mapper.Entity<ECSignature>()
                .Ignore(p => p.Signature)
                .Ignore(p => p.SigCompat);
            mapper.Entity<BlockHeader>()
                .Field(p => p.AccountKey, "a")
                .Field(p => p.AvailableProtocol, "b")
                .Field(p => p.BlockNumber, "c")
                .Field(p => p.BlockSignature, "d")
                .Field(p => p.CheckPointHash, "e")
                .Field(p => p.CompactTarget, "f")
                .Field(p => p.Fee, "g")
                .Field(p => p.Nonce, "h")
                .Field(p => p.Payload, "i")
                .Field(p => p.ProofOfWork, "j")
                .Field(p => p.ProtocolVersion, "k")
                .Field(p => p.Reward, "l")
                .Field(p => p.Timestamp, "m")
                .Field(p => p.TransactionHash, "n");

            var config = new NLog.Config.LoggingConfiguration();

            var logconsole = new NLog.Targets.ColoredConsoleTarget("logconsole")
            {
                UseDefaultRowHighlightingRules = true
            };
            config.AddRule(LogLevel.Trace, LogLevel.Fatal, logconsole);

            LogManager.Configuration = config;

            ServiceLocator.ServiceProvider = new ServiceCollection()
                .AddSingleton<IEventAggregator, EventAggregator>()
                .AddSingleton<IBlockChainStorage, BlockChainLiteDbStorage>()
                .AddSingleton<IBlockChain, BlockChainService>()
                .AddSingleton<ICheckPointStorage, CheckPointLiteDbStorage>()
                .AddSingleton<ICheckPointService, CheckPointService>()
                .AddSingleton<IPeerManager, PeerManager>()
                .AddSingleton<IHandler<HelloRequest>, HelloHandler>()
                .AddSingleton<IHandler<HelloResponse>, HelloHandler>()
                .AddSingleton<IHandler<BlockResponse>, BlocksHandler>()
                .AddSingleton<IHandler<NewBlockRequest>, BlocksHandler>()
                .AddSingleton<IHandler<CheckPointResponse>, CheckPointHandler>()
                .AddSingleton<IDiscovery, Discovery>()
                .AddTransient<INetClient, NetClient>()
                .AddSingleton<ITransactionValidator<TransferTransaction>, TransferTransactionValidator>()
                .AddSingleton<ITransactionValidator<ChangeKeyTransaction>, ChangeKeyTransactionValidator>()
                .AddSingleton<ITransactionValidator<ChangeAccountInfoTransaction>, ChangeAccountInfoTransactionValidator>()
                .AddLogging(builder =>
                {
                    builder.SetMinimumLevel( Microsoft.Extensions.Logging.LogLevel.Trace);
                   
                    builder.AddNLog(new NLogProviderOptions()
                    {
                        CaptureMessageTemplates = true,
                        CaptureMessageProperties = true,                       
                    });
                })
                .BuildServiceProvider();

            ServiceLocator.GetService<ICheckPointService>().LoadFromBlockChain();

            ServiceLocator
                .GetService<IEventAggregator>()
                .GetEvent<NetworkEvent>()
                .Subscribe(ServiceLocator.ServiceProvider.GetService<IHandler<HelloRequest>>().Handle,
                ThreadOption.BackgroundThread,
                false, (p) => p.Header.Operation == NetOperationType.Hello);

            ServiceLocator
                .EventAggregator
                .GetEvent<NetworkEvent>()
                .Subscribe(ServiceLocator.ServiceProvider.GetService<IHandler<BlockResponse>>().Handle,
                ThreadOption.BackgroundThread,
                false,
                (p) => p.Header.Operation == NetOperationType.Blocks ||
                       p.Header.Operation == NetOperationType.NewBlock ||
                       p.Header.Operation == NetOperationType.BlockHeader
                       );

            ServiceLocator.EventAggregator.GetEvent<NetworkEvent>().Subscribe(
                ServiceLocator.ServiceProvider.GetService<IHandler<CheckPointResponse>>().Handle,
                ThreadOption.BackgroundThread,
                false,
                p => p.Header.Operation == NetOperationType.CheckPoint && p.Header.RequestType == RequestType.Response);

            ServiceLocator.EventAggregator.GetEvent<NewServerConnection>().Subscribe((node) =>
            {
                if (node.NetClient != null && node.Connected)
                    node.NetClient.Send(new NetworkPacket<HelloRequest>(HelloRequest.NewRequest(ServiceLocator.GetService<IBlockChain>())));
            }, ThreadOption.BackgroundThread, false);

            if (!await ServiceLocator.GetService<IDiscovery>().DiscoverFixedSeedServers())
            {
                throw new Exception("NO FIX SEEDS FOUND");
            }
            var bc = ServiceLocator.GetService<IBlockChain>();
            var bestNodes = ServiceLocator.GetService<IPeerManager>().GetNodes().Where(p => p.NetClient != null).OrderByDescending(p => p.BlockHeight);
#if debugrt || true
            var block = bc.GetBlock(88359);           
            var pow = block.Header.CalcProofOfWork();
            Hash ophash = Utils.Sha256("");
            foreach(var t in block.Transactions)
            {
                Hash hb = t.Serialize();
                ophash = Utils.Sha256(ophash + Utils.Sha256(hb));
            }
            var ix = (uint)block.Header.Nonce;
            block.Header.TransactionHash = ophash;
            block.Header.Timestamp -= (uint)TimeSpan.FromHours(2).TotalSeconds;
            Console.WriteLine(block.Header.CalcProofOfWork());
            foreach (var bestNode in bestNodes)
            {
                try
                {
                    var x = await bestNode.NetClient.SendAndWaitAsync(new NetworkPacket<BlockRequest>(new BlockRequest()
                    {
                        StartBlock = 88359,
                        NumberOfBlocks = 1
                    }));
                    Console.WriteLine(x.Header.DataLength);
                    var bs = x.Payload<BlockResponse>().Blocks;
                    var b = bs.First(p => p.Id == 88359);
                    using (FileStream f1 = File.Open("f1", System.IO.FileMode.OpenOrCreate))
                    {
                        b.SaveToStream(f1);
                    }
                    using (FileStream f1 = File.Open("f2", System.IO.FileMode.OpenOrCreate))
                    {
                        f1.Write(x.RawData, 4, x.RawData.Length-4);
                    }

                    for (int k = 0; k < block.Transactions.Count; k++)
                    {
                        var t1 = block.Transactions[k];
                        var t2 = b.Transactions[k];
                    }
                }
                catch
                {
                    continue;
                }
            }
#endif
            foreach (var bestNode in bestNodes)
            {
                if (bestNode.BlockHeight > bc.BlockHeight)
                {
                    var remoteBlock = bestNode.BlockHeight;
                    do
                    {
                        NetworkPacket<BlockRequest> blockRequest = new NetworkPacket<BlockRequest>(NetOperationType.Blocks, RequestType.Request)
                        {
                            Message = new BlockRequest
                            {
                                StartBlock = (uint)(bc.BlockHeight > 0 ? bc.BlockHeight + 1 : bc.BlockHeight),
                                NumberOfBlocks = 10000
                            }
                        };
/*
                        var p = blockRequest;
                        p.Header.Operation = NetOperationType.Transactions;
                        var ro = await bestNode.NetClient.SendAndWaitAsync(p);
                        var x = ro.Payload<BlockResponse>();

                        p.Header.Operation = NetOperationType.Blocks;
                        ro = await bestNode.NetClient.SendAndWaitAsync(p);
                        x = ro.Payload<BlockResponse>();
*/
                        NetworkPacket response;
                        try
                        {
                            response = await bestNode.NetClient.SendAndWaitAsync(blockRequest);
                        }
                        catch (Exception)
                        {
                            break;
                        }
                        var blocks = response.Payload<BlockResponse>().Blocks;
                        var st = Stopwatch.StartNew();
                        await bc.AddBlocksAsync(blocks);
                        st.Stop();
                        Console.WriteLine("Added {0} blocks with {2} transactions in {3}. New block height {1}", blocks.Count, bc.BlockHeight, blocks.Sum(p=>p.Transactions?.Count), st.Elapsed);
                    } while (remoteBlock > bc.BlockHeight);
                }
            }

            ServiceLocator.GetService<IDiscovery>().Start();
            Console.ReadLine();
            ServiceLocator.ServiceProvider.Dispose();
        }
    }
}