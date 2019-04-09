using MicroCoin.CheckPoints;
using Prism.Events;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace MicroCoin.BlockChain
{
    public class BlocksAdded : PubSubEvent<Block> { }

    public class BlockChainService : IBlockChain
    {
        private readonly IBlockChainStorage blockChainStorage;
        private readonly IEventAggregator eventAggregator;

        public int BlockHeight => blockChainStorage.BlockHeight;
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
            foreach (var block in blocks)
            {
                AddBlock(block);
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
            return blockChainStorage.GetBlock(blockNumber);
        }
    }
}
