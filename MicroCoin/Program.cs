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
using MicroCoin.BlockChain;
using MicroCoin.Net;
using MicroCoin.Protocol;
using MicroCoin.Types;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.ComponentModel;
using System.Text;
using Prism.Events;
using System.Threading;
using MicroCoin.Common;
using System.Linq;
using MicroCoin.Cryptography;
using MicroCoin.CheckPoints;
using LiteDB;
using MicroCoin.Chain;
using System.Threading.Tasks;
using System.Collections.Generic;

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
                if (memberMapper.MemberName == "CurveType" || memberMapper.MemberName == "ct")
                {
                    memberMapper.Serialize = (obj, m) => new BsonValue((int)((ushort)obj));
                    memberMapper.Deserialize = (value, m) => (ECCurveType)value.AsInt32;
                }
                else if (memberMapper.MemberName == "State")
                {
                    memberMapper.Serialize = (obj, m) => new BsonValue((int)obj);
                    memberMapper.Deserialize = (value, m) => (AccountState)value.AsInt32;
                }
            };
            mapper.RegisterType<Currency>(p => p.value, p => new Currency(p.AsDecimal));
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

            ServiceLocator.ServiceProvider = new ServiceCollection()
                .AddSingleton<IEventAggregator, EventAggregator>()
                .AddSingleton<IBlockChain, BlockChainLiteDbStorage>()
                .AddSingleton<ICheckPointStorage, CheckPointLiteDbStorage>()
                .BuildServiceProvider();
//            var last = ServiceLocator.GetService<IBlockChain>().GetBlock(ServiceLocator.GetService<IBlockChain>().BlockHeight);
            HelloRequest request = new HelloRequest
            {
                AccountKey = Cryptography.ECKeyPair.CreateNew(),
                NodeServers = new NodeServerList(),
                Block = new Block
                {
                    Header = new BlockHeader
                    {
                        AccountKey = null,
                        AvailableProtocol = 0,
                        BlockNumber = 0,
                        CompactTarget = 0,
                        Fee = 0,
                        Nonce = 0,
                        TransactionHash = new byte[0],
                        Payload = new byte[0],
                        ProofOfWork = new byte[0],
                        ProtocolVersion = 0,
                        Reward = 0,
                        CheckPointHash = Cryptography.Utils.Sha256(Encoding.ASCII.GetBytes(Params.GenesisPayload)),
                        BlockSignature = 3,
                        Timestamp = 0
                    }
                }
            };
            request.ServerPort = 0;
            request.Timestamp = DateTime.UtcNow;
            request.Version = "2.0.0wN";
            request.WorkSum = 0;
            ServiceLocator.EventAggregator.GetEvent<NetworkEvent>().Subscribe((e) =>
            {
                var blocks = e.Payload<BlockResponse>().Blocks;
                foreach (var b in blocks)
                {
                    Console.WriteLine("{0}: {1}", b.Header.BlockNumber, b.Header.Payload);
                }
                ServiceLocator.GetService<IBlockChain>().AddBlocks(blocks);
                if (blocks.Last().Id < e.Client.Node.BlockHeight)
                {
                    NetworkPacket<BlockRequest> blockRequest = new NetworkPacket<BlockRequest>(NetOperationType.Blocks, RequestType.Request);
                    blockRequest.Message = new BlockRequest
                    {
                        StartBlock = blocks.Last().Id,
                        NumberOfBlocks = 10000
                    };
                    e.Client.Send(blockRequest);
                }
            }, ThreadOption.BackgroundThread,
                false, (p) => p.Header.RequestType == RequestType.Response && p.Header.Operation == NetOperationType.Blocks);

            ServiceLocator.EventAggregator.GetEvent<NetworkEvent>().Subscribe(
            (e) =>
            {
                var r = e.Payload<HelloResponse>();
                e.Client.Node.BlockHeight = r.Block.Header.BlockNumber;
                Console.WriteLine("{1} - Hello response received with block height: {0}", r.Block.Header.BlockNumber, e.Client.Node.EndPoint.ToString());
                var blockChain = ServiceLocator.GetService<IBlockChain>();
                var bc = blockChain.GetBlock(r.Block.Header.BlockNumber);
                if (bc == null)
                {
                    bc = blockChain.GetBlock((uint)blockChain.BlockHeight);
                }
                if (blockChain.BlockHeight < e.Client.Node.BlockHeight)
                {
                    NetworkPacket<BlockRequest> blockRequest = new NetworkPacket<BlockRequest>(NetOperationType.Blocks, RequestType.Request);
                    blockRequest.Message = new BlockRequest
                    {
                        StartBlock = (uint)blockChain.BlockHeight,
                        NumberOfBlocks = 10000
                    };
                    e.Client.Send(blockRequest);
                }                
                /*
                CheckPointRequest dt = new CheckPointRequest()
                {
                    CheckPointBlockCount = (uint)(blockChain.BlockHeight / 100) * 100,
                    StartBlock = 0,
                    EndBlock = 10000, // (uint) ((blockChain.BlockHeight/100)*100-1),
                    CheckPointHash = blockChain.GetBlock((uint)((blockChain.BlockHeight / 100) * 100)).Header.CheckPointHash
                };
                NetworkPacket<CheckPointRequest> np = new NetworkPacket<CheckPointRequest>(NetOperationType.CheckPoint, RequestType.Request, dt);
                e.Client.Send(np);
                */
            },
                ThreadOption.BackgroundThread,
                false,
                (np) => { return np.Header.Operation == NetOperationType.Hello && np.Header.RequestType == RequestType.Response; }
            );

            ServiceLocator.EventAggregator.GetEvent<NetworkEvent>().Subscribe((p) =>
            {
                var block = p.Payload<NewBlockRequest>().Block;
                Console.WriteLine("New block received with height {0}", block.Header.BlockNumber);
                var blockChain = ServiceLocator.GetService<IBlockChain>();
                if (block.Header.BlockNumber > blockChain.BlockHeight)
                {
                    NetworkPacket<BlockRequest> blockRequest = new NetworkPacket<BlockRequest>(NetOperationType.Blocks, RequestType.Request);
                    blockRequest.Message = new BlockRequest
                    {
                        StartBlock = (uint)blockChain.BlockHeight,
                        NumberOfBlocks = 100
                    };
                    p.Client.Send(blockRequest);
                }
            }, ThreadOption.BackgroundThread, false, (p) => p.Header.Operation == NetOperationType.NewBlock);
            ServiceLocator.EventAggregator.GetEvent<NetworkEvent>().Subscribe((p) =>
            {
                Console.WriteLine("Hello request received from {0}", p.Client.Node.IP);
                HelloResponse response = new HelloResponse();
                response.AccountKey = ECKeyPair.CreateNew();
                response.NodeServers = new NodeServerList();
                response.ServerPort = 0;
                response.Timestamp = DateTime.Now;
                response.Version = "2.0.0wN";
                response.WorkSum = 0;
                response.Block = new Block
                {
                    Header = new BlockHeader
                    {
                        AccountKey = null,
                        AvailableProtocol = 0,
                        BlockNumber = 0,
                        CompactTarget = 0,
                        Fee = 0,
                        Nonce = 0,
                        TransactionHash = new byte[0],
                        Payload = new byte[0],
                        ProofOfWork = new byte[0],
                        ProtocolVersion = 0,
                        Reward = 0,
                        CheckPointHash = Utils.Sha256(Encoding.ASCII.GetBytes(Params.GenesisPayload)),
                        BlockSignature = 3,
                        Timestamp = 0
                    }
                };
                p.Client.Send(new NetworkPacket<HelloResponse>(NetOperationType.Hello, RequestType.Response, response));

            }, ThreadOption.BackgroundThread, true,
            (p) => p.Header.Operation == NetOperationType.Hello && p.Header.RequestType == RequestType.Request
            );

            ServiceLocator.EventAggregator.GetEvent<NetworkEvent>().Subscribe((p) =>
            {
                var data = p.Payload<CheckPointResponse>();
                var blockChain = ServiceLocator.GetService<IBlockChain>();
                var end = data.EndBlock + 10000;
                if (end > (blockChain.BlockHeight / 100) * 100 - 1)
                {
                    end = (uint) (blockChain.BlockHeight / 100) * 100 - 1;
                }
                CheckPointRequest dt = new CheckPointRequest()
                {
                    CheckPointBlockCount = (uint)(blockChain.BlockHeight / 100) * 100,
                    StartBlock = data.EndBlock,
                    EndBlock = end,
                    CheckPointHash = blockChain.GetBlock((uint)((blockChain.BlockHeight / 100) * 100)).Header.CheckPointHash
                };
                NetworkPacket<CheckPointRequest> np = new NetworkPacket<CheckPointRequest>(NetOperationType.CheckPoint, RequestType.Request, dt);
                p.Client.Send(np);
            }, ThreadOption.BackgroundThread, false, p => p.Header.Operation == NetOperationType.CheckPoint && p.Header.RequestType == RequestType.Response);
            NetClient client = new NetClient();
            var clients = new List<NetClient>();
            if (client.Connect("127.0.0.1", 4004))
            {
                NetworkPacket<HelloRequest> networkPacket = new NetworkPacket<HelloRequest>(NetOperationType.Hello, RequestType.Request, request);
                var hello = await client.SendAndWaitAsync(networkPacket);
                var bc = ServiceLocator.GetService<IBlockChain>();                
                if (hello.Payload<HelloResponse>().Block.Header.BlockNumber > bc.BlockHeight)
                {
                    var remoteBlock = hello.Payload<HelloResponse>().Block.Header.BlockNumber;
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
                        var response = await client.SendAndWaitAsync(blockRequest);                        
                        var blocks = response.Payload<BlockResponse>().Blocks;
                        Console.WriteLine("Checking {0} blocks.", blocks.Count);
                        Parallel.ForEach(blocks, (b) =>
                        {
                            if (!b.Header.IsValid())
                            {
                                throw new Exception(string.Format("Invalid block {0}", b.Header.BlockNumber));
                            }
                            if (b.Transactions != null)
                            {
                                Parallel.ForEach(b.Transactions, (t) =>
                                {
                                    if (!t.IsValid()) throw new Exception(string.Format("Invalid transaction {0}", t.ToString()));
                                });
                            }
                        });
                        bc.AddBlocks(blocks);
                        Console.WriteLine("Added {0} blocks. New block height {1}", blocks.Count, bc.BlockHeight);
                    } while (remoteBlock > bc.BlockHeight);
                }

                foreach(var h in hello.Payload<HelloResponse>().NodeServers)
                {
                    var c = new NetClient();
                    if (c.Connect(h.Value.IP, h.Value.Port))
                    {
                        clients.Add(c);
                        c.Start();
                        new Timer((state) =>
                        {
                            if (!((NetClient)state).IsConnected) return;
                            ((NetClient)state).Send(networkPacket);
                        }, c, 0, 60000);
                    }
                    else
                    {
                        c.Dispose();
                    }
                }
                client.Start();
                Timer timer = new Timer((state) =>
                {
                    client.Send(networkPacket);
                }, null, 0, 60000);
            }
            Console.ReadLine();
            client.Dispose();
            ServiceLocator.ServiceProvider.Dispose();
        }
    }
}