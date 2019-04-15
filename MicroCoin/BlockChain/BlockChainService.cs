//-----------------------------------------------------------------------
// This file is part of MicroCoin - The first hungarian cryptocurrency
// Copyright (c) 2019 Peter Nemeth
// BlockChainService.cs - Copyright (c) 2019 Németh Péter
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
using MicroCoin.Common;
using MicroCoin.Transactions;
using Microsoft.Extensions.Logging;
using Prism.Events;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace MicroCoin.BlockChain
{
    public class BlocksAdded : PubSubEvent<Block> { }

    public class BlockChainService : IBlockChain
    {
        private readonly IBlockChainStorage blockChainStorage;
        private readonly IEventAggregator eventAggregator;
        private readonly ILogger<BlockChainService> logger;
        private readonly ConcurrentBag<Block> blockCache = new ConcurrentBag<Block>();

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


        protected bool ProcessBlocks(IEnumerable<Block> blocks)
        {
            uint badBlockNumber = uint.MaxValue;
            object blockLock = new object();
            Parallel.ForEach(blocks, (block, state) =>
            {
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
            foreach (var block in blocks.Where(b => b.Id < badBlockNumber))
            {
                if(block.Id <= BlockHeight)
                {
                    var myBlock = GetBlock(block.Id);
                    if (myBlock != null && myBlock.Header.CompactTarget >= block.Header.CompactTarget)
                    {
                        continue; // My chain is "longer" or equal, so block is invalid for me, or already included in the chain
                                  // Skip it now, the sender will maintain the orphan chain
                    }
                    else if (myBlock != null && myBlock.Header.CompactTarget == block.Header.CompactTarget)
                    {
                        continue; // Already included
                    }
                    else if (myBlock != null && myBlock.Header.CompactTarget < block.Header.CompactTarget)
                    {
                        // I'm orphan?
                        return false;
                    }
                }
                eventAggregator.GetEvent<BlocksAdded>().Publish(block);
                blockCache.Add(block);
            }
            return true;
        }

        public bool AddBlocks(IEnumerable<Block> blocks)
        {
            var newBlocksOk = false;
            try
            {
                newBlocksOk = ProcessBlocks(blocks);
            }
            finally
            {
                if (newBlocksOk)
                {
                    blockChainStorage.AddBlocks(blockCache);
                }
                blockCache.Clear();
            }
            return newBlocksOk;
        }

        public async Task<bool> AddBlocksAsync(IEnumerable<Block> blocks)
        {
            var newBlocksOk = false;
            try
            {
                newBlocksOk = ProcessBlocks(blocks);
            }
            finally
            {
                if (newBlocksOk)
                {
                    logger.LogInformation("Saving blocks");
                    await blockChainStorage.AddBlocksAsync(blocks);
                }
                blockCache.Clear();
            }
            return newBlocksOk;
        }

        public void Dispose()
        {
            if (blockCache.Count > 0)
            {
                blockChainStorage.AddBlocks(blockCache);
                blockCache.Clear();
            }
        }

        public Block GetBlock(uint blockNumber)
        {
            var block = blockCache.FirstOrDefault(p => p.Id == blockNumber);
            if (block != null) return block;
            return blockChainStorage.GetBlock(blockNumber);
        }

        public void DeleteBlocks(uint from)
        {
            blockChainStorage.DeleteBlocks(from);
        }

        public IEnumerable<Block> GetBlocks(uint startBlock, uint endBlock)
        {
            return blockChainStorage.GetBlocks(startBlock, endBlock);
        }
    }
}