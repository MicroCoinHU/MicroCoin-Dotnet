using Prism.Events;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace MicroCoin.BlockChain
{
    public class BlocksAdded : PubSubEvent<IEnumerable<Block>> { }

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
            blockChainStorage.AddBlock(block);
            eventAggregator.GetEvent<BlocksAdded>().Publish(new HashSet<Block>() { block });
        }

        public void AddBlocks(IEnumerable<Block> blocks)
        {
            blockChainStorage.AddBlocks(blocks);
            eventAggregator.GetEvent<BlocksAdded>().Publish(blocks);
        }

        public async Task AddBlocksAsync(IEnumerable<Block> blocks)
        {
            await AddBlocksAsync(blocks);
            eventAggregator.GetEvent<BlocksAdded>().Publish(blocks);
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
