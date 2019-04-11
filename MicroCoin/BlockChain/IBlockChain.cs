﻿//-----------------------------------------------------------------------
// This file is part of MicroCoin - The first hungarian cryptocurrency
// Copyright (c) 2019 Peter Nemeth
// IBlockChain.cs - Copyright (c) 2019 %UserDisplayName%
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