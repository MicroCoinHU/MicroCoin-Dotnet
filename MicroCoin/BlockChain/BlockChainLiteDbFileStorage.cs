//-----------------------------------------------------------------------
// This file is part of MicroCoin - The first hungarian cryptocurrency
// Copyright (c) 2019 Peter Nemeth
// BlockChainLiteDbFileStorage.cs - Copyright (c) 2019 Németh Péter
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
using LiteDB;
using MicroCoin.Cryptography;
using MicroCoin.Transactions;
using MicroCoin.Types;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace MicroCoin.BlockChain
{
    class BlockChainLiteDbFileStorage : IBlockChain, IDisposable
    {
        private readonly LiteDatabase db = new LiteDatabase("Filename=blocks.file.db;Async=true");

        public BlockChainLiteDbFileStorage()
        {            

        }

        public int Count
        {
            get
            {
                return db.FileStorage.FindAll().Count();
            }
        }

        public int BlockHeight => throw new NotImplementedException();

        public void AddBlock(Block block)
        {
            if (!block.Header.IsValid())
            {
                throw new Exception("Invalid block!!!");
            }
            using (var ms = new MemoryStream())
            {
                var name = block.Header.BlockNumber.ToString();
                using (var ls = db.FileStorage.OpenWrite(name, name, null))
                {
                    block.SaveToStream(ms);
                    ms.Position = 0;
                    ms.CopyTo(ls);
                }
            }
        }

        public void AddBlocks(IEnumerable<Block> block)
        {
            throw new NotImplementedException();
        }

        public Task AddBlocksAsync(IEnumerable<Block> blocks)
        {
            throw new NotImplementedException();
        }

        public void Dispose()
        {
            db.Dispose();
        }

        public Block GetBlock(uint blockNumber)
        {
            var b = new Block();
            using (var ls = db.FileStorage.FindById(blockNumber.ToString()).OpenRead())
            {
                using(var ms = new MemoryStream())
                {
                    ls.CopyTo(ms);
                    ms.Position = 0;
                    b.LoadFromStream(ms);
                    return b;
                }
            }            
        }
    }
}