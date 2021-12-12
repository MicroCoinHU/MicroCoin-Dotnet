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
using Microsoft.Extensions.DependencyInjection;
using Prism.Events;
using System;
using System.Linq;
using System.Threading.Tasks;
using NLog;
using NLog.Extensions.Logging;
using Microsoft.Extensions.Logging;
using LogLevel = NLog.LogLevel;
using MicroCoin.Modularization;

namespace MicroCoin
{
    class Program
    {
        static async Task Main(string[] args)
        {
            Console.WriteLine("Hello World!");

            var logconsole = new NLog.Targets.ColoredConsoleTarget("logconsole")
            {
                UseDefaultRowHighlightingRules = true,
                DetectConsoleAvailable = true
            };
            var config = new NLog.Config.LoggingConfiguration();
            config.AddRule(LogLevel.Debug, LogLevel.Fatal, logconsole);
            LogManager.Configuration = config;

            ServiceCollection serviceCollection = new ServiceCollection();
            ModuleManager moduleManager = new ModuleManager();
            moduleManager.LoadModules(serviceCollection);

            ServiceLocator.ServiceProvider = serviceCollection
                .AddSingleton<IEventAggregator, EventAggregator>()
                .AddLogging(builder =>
                {
                    builder
                        .SetMinimumLevel(Microsoft.Extensions.Logging.LogLevel.Trace)
                        .AddNLog(new NLogProviderOptions()
                        {
                            CaptureMessageTemplates = true,
                            CaptureMessageProperties = true
                        });
                })
                .BuildServiceProvider();
            
            moduleManager.InitModules(ServiceLocator.ServiceProvider);

            if (!await ServiceLocator.GetService<IDiscovery>().DiscoverFixedSeedServers())
            {
                throw new Exception("NO FIX SEED SERVERS FOUND");
            }
            await SyncBlockChain();
            ServiceLocator.GetService<IDiscovery>().Start();
            ServiceLocator.GetService<INetServer>().Start();
            Console.ReadLine();
            ServiceLocator.ServiceProvider.Dispose();
        }

        private static async Task SyncBlockChain()
        {
            var bc = ServiceLocator.GetService<IBlockChain>();
            var bestNodes = ServiceLocator.GetService<IPeerManager>().GetNodes().Where(p => p.NetClient != null).OrderByDescending(p => p.BlockHeight);
            var error = false;
            do
            {
                foreach (var bestNode in bestNodes)
                {
                    if (bestNode.BlockHeight > bc.BlockHeight)
                    {
                        var remoteBlock = bestNode.BlockHeight;
                        do
                        {
                            var blockHeight = bc.BlockHeight;
                            NetworkPacket<BlockRequest> blockRequest = new NetworkPacket<BlockRequest>(NetOperationType.Blocks, RequestType.Request)
                            {
                                Message = new BlockRequest
                                {
                                    StartBlock = (uint)(blockHeight > 0 ? blockHeight + 1 : blockHeight),
                                    NumberOfBlocks = 100
                                }
                            };
                            NetworkPacket response;
                            try
                            {
                                response = await bestNode.NetClient.SendAndWaitAsync(blockRequest);
                            }
                            catch (Exception)
                            {
                                error = true;
                                break;
                            }
                            error = false;
                            var blocks = response.Payload<BlockResponse>().Blocks;
                            var ok = await bc.AddBlocksAsync(blocks);
                            if (!ok)
                            {
                                // I'm orphan?
                                foreach (var block in blocks.OrderByDescending(p => p.Id))
                                {
                                    var myBlock = bc.GetBlock(block.Id);
                                    if (myBlock != null && (block.Header.CompactTarget == myBlock.Header.CompactTarget))
                                    {
                                        // On baseblock
                                    }
                                }
                            }
                        } while (remoteBlock > bc.BlockHeight);
                    }
                }
            } while (error);
        }

    }
}