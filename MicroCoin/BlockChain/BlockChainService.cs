using Microsoft.Extensions.Logging;
using Prism.Events;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace MicroCoin.BlockChain
{
    public class BlocksAdded : PubSubEvent<Block> { }

    public class BlockChainService : IBlockChain
    {
        private readonly IBlockChainStorage blockChainStorage;
        private readonly IEventAggregator eventAggregator;
        private readonly ILogger<BlockChainService> logger;
        private readonly List<Block> blockCache = new List<Block>();

        public int BlockHeight
        {
            get
            {
                if (blockCache.Count > 0)
                {
                    return (int)blockCache.Max(p => p.Id);
                }
                return blockChainStorage.BlockHeight;
            }
        }
        public int Count => blockChainStorage.Count + blockCache.Count();

        public BlockChainService(IBlockChainStorage blockChainStorage, IEventAggregator eventAggregator, ILogger<BlockChainService> logger)
        {
            this.blockChainStorage = blockChainStorage;
            this.eventAggregator = eventAggregator;
            this.logger = logger;
        }

        public void AddBlock(Block block)
        {
            if (block.Id <= blockChainStorage.BlockHeight)
            {
                logger.LogInformation("Skipping block {0} due my block height is {1}", block.Id, blockChainStorage.BlockHeight);
                return;
            }
            if (!block.Header.IsValid()) return;
            blockChainStorage.AddBlock(block);
            try
            {
                eventAggregator.GetEvent<BlocksAdded>().Publish(block);
            }
            catch (Exception e)
            {
                throw e;
            }
        }

        public void AddBlocks(IEnumerable<Block> blocks)
        {
            try
            {
                uint badBlockNumber = blocks.Max(p => p.Id) + 1;
                object blockLock = new object();
                Parallel.ForEach(blocks, (block, state) => {
                    if (!block.Header.IsValid())
                    {
                        lock (blockLock)
                        {
                            if (badBlockNumber > block.Id)
                            {
                                badBlockNumber = block.Id;
                            }
                            state.Stop();
                        }
                    }
                });
                foreach (var block in blocks)
                {
                    blockCache.Add(block);
                    eventAggregator.GetEvent<BlocksAdded>().Publish(block);
                }
            }
            finally
            {
                logger.LogDebug("Saving blocks");
                blockChainStorage.AddBlocks(blockCache);
                blockCache.Clear();
            }
        }

        public async Task AddBlocksAsync(IEnumerable<Block> blocks)
        {
            await blockChainStorage.AddBlocksAsync(blocks);
        }

        public void Dispose()
        {
            if (blockCache.Count > 0)
            {
                blockChainStorage.AddBlocks(blockCache);
                blockCache.Clear();
            }
            return;
        }

        public Block GetBlock(uint blockNumber)
        {
            var block = blockCache.FirstOrDefault(p => p.Id == blockNumber);
            if (block != null) return block;
            return blockChainStorage.GetBlock(blockNumber);
        }
    }
}
