using Avalonia;
using Avalonia.Markup.Xaml;
using MicroCoin.Modularization;
using System.Threading.Tasks;
using MicroCoin.BlockChain;
using MicroCoin.Net;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using NLog;
using NLog.Extensions.Logging;
using Prism.Events;
using LogLevel = NLog.LogLevel;
using System.Diagnostics;
using System;
using Avalonia.Threading;
using Avalonia.Controls;
using System.Linq;
using MicroCoin.Protocol;
using Avalonia.Controls.ApplicationLifetimes;
using System.Collections.Concurrent;

namespace MicroCoin.Wallet
{
    public class App : Application
    {
        public override void Initialize()
        {
            AvaloniaXamlLoader.Load(this);
            if (ApplicationLifetime != null)
            {
                if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime)
                {
                    if (!Design.IsDesignMode)
                    {
                        Task.Run(() => Init());
                    }
                }
            }
        }

        public override void OnFrameworkInitializationCompleted()
        {
            if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
            {
                desktop.MainWindow = new ProgressWindow();
                desktop.Exit+= (o, e) => ServiceLocator.ServiceProvider.Dispose();
            }
            else if (ApplicationLifetime is ISingleViewApplicationLifetime singleView)
                singleView.MainView = new MainWindow();
            base.OnFrameworkInitializationCompleted();
        }

        async Task Init()
        {
            var logconsole = new NLog.Targets.ColoredConsoleTarget("logconsole")
            {
                UseDefaultRowHighlightingRules = true,
                DetectConsoleAvailable = true
            };
            var config = new NLog.Config.LoggingConfiguration();
            var visualStudioOutput = new NLog.Targets.DebuggerTarget("VS-Debug");
            config.AddRule(LogLevel.Trace, LogLevel.Fatal, visualStudioOutput);
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
                throw new Exception("NO FIX SEED SERVER FOUND");
            }
            await SyncBlockChain();
            ServiceLocator.GetService<IDiscovery>().Start();
            ServiceLocator.GetService<INetServer>().Start();
            await Dispatcher.UIThread.InvokeAsync(() =>
            {
                if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
                {                    
                    Window pt = desktop.MainWindow;
                    desktop.MainWindow = new MainWindow();
                    desktop.MainWindow.Show();
                    pt.Close();
                }
            });
        }

        public Window MainWindow
        {
            get
            {
                if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
                    return desktop.MainWindow;
                return null;
            }
        }
        private async Task SyncBlockChain()
        {
            var bc = ServiceLocator.GetService<IBlockChain>();
            var bestNodes = ServiceLocator.GetService<IPeerManager>().GetNodes().Where(p => p.NetClient != null).OrderByDescending(p => p.BlockHeight);
            var error = false;
            Stopwatch stopwatch = new Stopwatch();
            stopwatch.Start();
            var origBlockHeight = bc.BlockHeight;
            ConcurrentQueue<Block> queue = new ConcurrentQueue<Block>();
            bc.StartConsumer(queue);
            var blockHeight = bc.BlockHeight;
            do
            {
                foreach (var bestNode in bestNodes)
                {
                    if (bestNode.BlockHeight > bc.BlockHeight)
                    {
                        if (!bestNode.Connected) bestNode.NetClient.Start();
                        if (!bestNode.Connected) continue;
                        var remoteBlock = bestNode.BlockHeight;
                        do
                        {
                            var elapsed = stopwatch.ElapsedMilliseconds / 1000;
                            if (elapsed == 0) elapsed = 1;
                            var speed = (bc.BlockHeight - origBlockHeight + 1) / elapsed;
                            if (speed == 0) speed = 1;
                            var remaining = (remoteBlock - bc.BlockHeight) / speed;
                            if (queue.Count > 2000)
                            {
                                while (queue.Count > 1000)
                                {
                                    await Task.Delay(10);
                                    _ = ShowProgess(bc, queue, remoteBlock, elapsed, speed, remaining);
                                }
                            }
                            _ = ShowProgess(bc, queue, remoteBlock, elapsed, speed, remaining);

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
                            blockHeight = (int)blocks.OrderByDescending(p => p.Id).First().Id;
                            blocks.ToList().ForEach((item) => queue.Enqueue(item));
                            /*var ok = await bc.AddBlocksAsync(blocks);
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
                            }*/
                        } while (remoteBlock > bc.BlockHeight);
                    }
                }
            } while (error);
        }

        private async Task ShowProgess(IBlockChain bc, ConcurrentQueue<Block> queue, uint remoteBlock, long elapsed, long speed, long remaining)
        {
            await Dispatcher.UIThread.InvokeAsync(() =>
            {
                if (MainWindow != null)
                {
                    if (MainWindow is ProgressWindow)
                    {

                        ((ProgressWindow)MainWindow).FindControl<ProgressBar>("progressBar").Maximum = remoteBlock;
                        ((ProgressWindow)MainWindow).FindControl<ProgressBar>("progressBar").Value = bc.BlockHeight;
                        ((ProgressWindow)MainWindow).FindControl<TextBlock>("blockHeight").Text = remoteBlock.ToString() + "/" + bc.BlockHeight.ToString();
                        ((ProgressWindow)MainWindow).FindControl<TextBlock>("remaining").Text = " Remaining: " + remaining.ToString() + " sec";
                        ((ProgressWindow)MainWindow).FindControl<TextBlock>("que").Text = " Queued: " + queue.Count.ToString() + " blocks";
                        ((ProgressWindow)MainWindow).FindControl<TextBlock>("speed").Text = " Speed: " + speed.ToString() + " block/sec";
                        ((ProgressWindow)MainWindow).FindControl<TextBlock>("elapsed").Text = " Elapsed: " + elapsed.ToString() + " sec";

                    }
                }
            });
        }
    }
}