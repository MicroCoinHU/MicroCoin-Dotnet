using MicroCoin.CheckPoints;
using Prism.Events;
using System;
using System.Linq;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace MicroCoin.BlockChain
{
    public class BlocksAdded : PubSubEvent<Block> { }

    public class BlockChainService : IBlockChain
    {
        private readonly IBlockChainStorage blockChainStorage;
        private readonly IEventAggregator eventAggregator;
        private readonly List<Block> blockCache = new List<Block>();

        public int BlockHeight {
            get {
                if (blockCache.Count > 0)
                {
                    return (int) blockCache.Max(p => p.Id);
                }
                return blockChainStorage.BlockHeight;
            }
        }
        public int Count => blockChainStorage.Count;

        public BlockChainService(IBlockChainStorage blockChainStorage, IEventAggregator eventAggregator)
        {
            this.blockChainStorage = blockChainStorage;
            this.eventAggregator = eventAggregator;
        }

        public void AddBlock(Block block)
        {
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
                foreach (var block in blocks)
                {
                    if (!block.Header.IsValid()) return;
                    blockCache.Add(block);
                    eventAggregator.GetEvent<BlocksAdded>().Publish(block);
                }
            }
            finally
            {
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
