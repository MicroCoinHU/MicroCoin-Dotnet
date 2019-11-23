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
using MicroCoin.Types;
using Microsoft.Extensions.Logging;
using Prism.Events;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Threading;
using System.Threading.Tasks;

namespace MicroCoin.BlockChain
{
    public class BlocksAdded : PubSubEvent<Block> { }

    public class BlockChainService : IBlockChain, IDisposable
    {
        private readonly IBlockChainStorage blockChainStorage;
        private readonly IEventAggregator eventAggregator;
        private readonly ILogger<BlockChainService> logger;
        private readonly ConcurrentBag<Block> blockCache = new ConcurrentBag<Block>();
        private Thread consumerThread;
        private bool stopped = false;

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

        public BlockChainService(IBlockChainStorage blockChainStorage, 
            IEventAggregator eventAggregator, ILogger<BlockChainService> logger)
        {
            this.blockChainStorage = blockChainStorage;
            this.eventAggregator = eventAggregator;
            this.logger = logger;
            logger.LogInformation("Blockchain loaded. BlockHeight: {0}", blockChainStorage.BlockHeight);
        }

        private bool IsValid(BlockHeader block)
        {
            return block.IsValid();
        }

        public void AddBlock(Block block)
        {
            if (block.Id <= blockChainStorage.BlockHeight)
            {
                logger.LogInformation("Skipping block {0} due my block height is {1}", block.Id, blockChainStorage.BlockHeight);
                return;
            }
            if (!IsValid(block.Header)) return;
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

            if (blocks.Min(p => p.Id) > BlockHeight + 1)
                return false;

            Parallel.ForEach(blocks, (block, state) =>
            {
                if (!IsValid(block.Header))
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
                if (block.Header.CompactTarget != GetTarget(block.Header.BlockNumber+1                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                      ).Item2)
                    return false;
                if(block.Id <= BlockHeight)
                {
                    var myBlock = GetBlockHeader(block.Id);                    
                    if (myBlock != null && myBlock.CompactTarget >= block.Header.CompactTarget)
                    {
                        continue; // My chain is "longer" or equal, so block is invalid for me, or already included in the chain
                                  // Skip it now, the sender will maintain the orphan chain
                    }
                    else if (myBlock != null && myBlock.CompactTarget == block.Header.CompactTarget)
                    {
                        continue; // Already included
                    }
                    else if (myBlock != null && myBlock.CompactTarget < block.Header.CompactTarget)
                    {
                        throw new Exception("I'm orphan");
                    }
                }
                try
                {
                    eventAggregator.GetEvent<BlocksAdded>().Publish(block);
                    blockCache.Add(block);
                }
                catch (Exception e) {
                    blockCache.Clear();
                    return false;
                }
            }
            return true;
        }

        public void StartConsumer(ConcurrentQueue<Block> blocks)
        {
            consumerThread = new Thread(() =>
             {
                 HashSet<Block> bs = new HashSet<Block>(500);
                 while (!stopped)
                 {
                     if (blocks.Count == 0)
                     {
                         Thread.Sleep(100);
                         continue;
                     }
                     var max = Math.Min(blocks.Count, 100);
                     for (int i = 0; i < max; i++)
                     {
                         if (blocks.TryDequeue(out Block b))
                         {
                             bs.Add(b);
                         }
                     }
                     if (ProcessBlocks(bs))
                     {
                         blockChainStorage.AddBlocks(blockCache);
                         blockCache.Clear();
                     }
                     else
                     {
                         throw new Exception("Invalid blocks!");
                     }
                     bs.Clear();
                 }
             });
            consumerThread.Start();
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
                logger.LogTrace("Processing {0} blocks", blocks.Count());
                newBlocksOk = ProcessBlocks(blocks);
                logger.LogTrace("Processed {0} blocks", blocks.Count());
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
            /*if (blockCache.Count > 0)
            {
                blockChainStorage.AddBlocks(blockCache);
                blockCache.Clear();
            }*/
            stopped = true;
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

        public BlockHeader GetBlockHeader(uint blockNumber)
        {
            var block = blockCache.FirstOrDefault(p => p.Id == blockNumber);
            if (block != null) return block.Header;
            return blockChainStorage.GetBlockHeader(blockNumber);
        }

        protected uint CompactFromTarget(in Hash targetPow)
        {
            BigInteger bn = new BigInteger(targetPow.Reverse());
            return CompactFromTarget(bn);
        }

        protected uint CompactFromTarget(in BigInteger targetPow)
        {
            BigInteger bn = targetPow;
            BigInteger bn2 = BigInteger.Parse("0800000000000000000000000000000000000000000000000000000000000000", System.Globalization.NumberStyles.HexNumber);
            uint nbits = 4;
            while ((bn < bn2) && (nbits < 231))
            {
                bn2 >>= 1;
                nbits++;
            }

            uint i = Params.Current.MinimumDifficulty >> 24;
            if (nbits < i)
            {
                return Params.Current.MinimumDifficulty;
            }
            int s = 256 - 25 - (int)nbits;
            bn >>= s;
            return (nbits << 24) + ((uint)bn & 0x00FFFFFF) ^ 0x00FFFFFF;
        }

        public Tuple<Hash, uint> GetTarget(uint block)
            {
            var blockCount = block - 1;
            if (blockCount < 1 || block == 0)
                return Tuple.Create(TargetFromCompact(Params.Current.MinimumDifficulty), Params.Current.MinimumDifficulty);

            var lastBlock = GetBlockHeader(blockCount - 1);
            BlockHeader lastCheckPointBlock;
            uint calcBlocks;
            if (blockCount > Params.Current.DifficultyAdjustFrequency)
            {
                calcBlocks = (uint)Params.Current.DifficultyAdjustFrequency;
            }
            else
            {
                calcBlocks = blockCount - 1;
            }
            lastCheckPointBlock = GetBlockHeader((uint)Math.Max((int)(blockCount - calcBlocks - 1), 0));
            Hash actualTarget = TargetFromCompact(lastBlock.CompactTarget);
            DateTime ts1 = lastBlock.Timestamp;
            DateTime ts2 = lastCheckPointBlock.Timestamp;
            var tsReal = ts1.Subtract(ts2).TotalSeconds;
            long tsTeorical = calcBlocks * Params.Current.BlockTime;
            long factor1000 = (long)((tsTeorical - tsReal) * 1000 / tsTeorical) * -1;
            long factor1000Min = -500 / (Params.Current.DifficultyAdjustFrequency / 2);
            long factor1000Max = 1000 / (Params.Current.DifficultyAdjustFrequency / 2);
            if (factor1000 < factor1000Min) factor1000 = factor1000Min;
            else if (factor1000 > factor1000Max) factor1000 = factor1000Max;
            else if (factor1000 == 0) return Tuple.Create(actualTarget, CompactFromTarget(actualTarget));
            calcBlocks /= 10;
            if (calcBlocks == 0) calcBlocks = 1;
            ts2 = GetBlockHeader((uint)Math.Max((int)(blockCount - calcBlocks - 1), 0)).Timestamp;
            var tsRealStop = ts1.Subtract(ts2).TotalSeconds;
            var tsTeoricalStop = calcBlocks * Params.Current.BlockTime;
            if (
                (tsTeorical > tsReal && tsTeoricalStop > tsRealStop) ||
                (tsTeoricalStop < tsRealStop && tsTeorical < tsReal)
                )
            {
                byte[] aT = actualTarget;
                var bnact = new BigInteger(aT.Reverse().ToArray());
                var bnaux = new BigInteger(aT.Reverse().ToArray());
                bnact *= factor1000;
                bnact /= 1000;
                bnact += bnaux;
                var nt = CompactFromTarget(bnact);
                var newTarget = nt;
                return Tuple.Create(TargetFromCompact(newTarget), newTarget);
            }

            return Tuple.Create(actualTarget, CompactFromTarget(actualTarget));
        }

        protected Hash TargetFromCompact(in uint encoded)
        {
            uint nbits = encoded >> 24;
            uint i = Params.Current.MinimumDifficulty >> 24;
            if (nbits < i)
            {
                nbits = i;
            }
            else if (nbits > 231)
            {
                nbits = 231;
            }
            uint offset = encoded << 8 >> 8;
            offset = ((offset ^ 0x00FFFFFF) | (0x01000000));
            BigInteger bn = new BigInteger(offset);
            uint shift = (256 - nbits - 25);
            bn <<= (int)shift;
            byte[] r = new byte[32];
            byte[] ba = bn.ToByteArray().Reverse().ToArray();
            for (var index = 0; index < ba.Length; index++)
            {
                r[32 + index - ba.Length] = ba[index];
            }
            return r;
        }
    }
}