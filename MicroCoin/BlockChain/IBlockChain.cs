//-----------------------------------------------------------------------
// This file is part of MicroCoin - The first hungarian cryptocurrency
// Copyright (c) 2019 Peter Nemeth
// IBlockChain.cs - Copyright (c) 2019 Németh Péter
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
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace MicroCoin.BlockChain
{
    public interface IBlockChain
    {
        int BlockHeight { get; }
        int Count { get; }

        void AddBlock(Block block);
        bool AddBlocks(IEnumerable<Block> blocks);
        Task<bool> AddBlocksAsync(IEnumerable<Block> blocks);
        void DeleteBlocks(uint from);
        void Dispose();
        Block GetBlock(uint blockNumber);
        BlockHeader GetBlockHeader(uint blockNumber);
        IEnumerable<Block> GetBlocks(uint startBlock, uint endBlock);
        Tuple<Hash, uint> GetTarget(uint block);
        void StartConsumer(ConcurrentQueue<Block> queue);
    }
}