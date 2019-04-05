using System;
using System.Collections.Generic;
using System.Text;

namespace MicroCoin.BlockChain
{
    public interface IBlockChain
    {
        void AddBlock(Block block);
        Block GetBlock(uint blockNumber);
        int Count { get; }
    }
}
