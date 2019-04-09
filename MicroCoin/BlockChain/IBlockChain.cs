using System.Collections.Generic;
using System.Threading.Tasks;

namespace MicroCoin.BlockChain
{
    public interface IBlockChain
    {
        int BlockHeight { get; }
        int Count { get; }

        void AddBlock(Block block);
        void AddBlocks(IEnumerable<Block> blocks);
        Task AddBlocksAsync(IEnumerable<Block> blocks);
        void Dispose();
        Block GetBlock(uint blockNumber);
    }
}