//-----------------------------------------------------------------------
// This file is part of MicroCoin - The first hungarian cryptocurrency
// Copyright (c) 2019 Peter Nemeth
// BlockChainFileStorage.cs - Copyright (c) 2019 Németh Péter
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
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading.Tasks;

namespace MicroCoin.BlockChain
{
    public class BlockChainFileStorage : IBlockChain, IDisposable
    {
        private readonly FileStream fileStream;
        private readonly FileStream indexStream;

        public BlockChainFileStorage()
        {
            fileStream = new FileStream("blocks.chain", FileMode.OpenOrCreate);
            indexStream = new FileStream("blocks.idx", FileMode.OpenOrCreate);
        }

        public int Count => throw new NotImplementedException();

        public int BlockHeight => throw new NotImplementedException();

        public void AddBlock(Block block)
        {
            using (BinaryWriter bw = new BinaryWriter(indexStream, Encoding.ASCII, true))
            {
                bw.Write(block.Header.BlockNumber);
                long pos = fileStream.Position;
                bw.Write(pos);
                block.SaveToStream(fileStream);
                bw.Write(fileStream.Position - pos);
            }
        }

        public void AddBlocks(IEnumerable<Block> block)
        {
            throw new NotImplementedException();
        }

        public async Task AddBlocksAsync(IEnumerable<Block> blocks)
        {
            throw new NotImplementedException();
        }

        public void Dispose()
        {
            fileStream.Dispose();
            indexStream.Dispose();
        }

        public Block GetBlock(uint blockNumber)
        {
            throw new NotImplementedException();
        }
    }
}
