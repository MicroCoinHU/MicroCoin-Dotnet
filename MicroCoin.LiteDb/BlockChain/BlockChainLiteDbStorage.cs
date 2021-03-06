﻿//-----------------------------------------------------------------------
// This file is part of MicroCoin - The first hungarian cryptocurrency
// Copyright (c) 2019 Peter Nemeth
// BlockChainLiteDbStorage.cs - Copyright (c) 2019 Németh Péter
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
using MicroCoin.Transactions;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace MicroCoin.BlockChain
{
    public class BlockChainLiteDbStorage : IBlockChainStorage
    {
        private readonly LiteDatabase db;
        private readonly LiteDatabase trdb;        

        public BlockChainLiteDbStorage()
        {
            if (!Directory.Exists(Params.Current.DataFolder))
            {
                Directory.CreateDirectory(Params.Current.DataFolder);
            }

            db = new LiteDatabase("Filename=" + Path.Combine(Params.Current.DataFolder, "blockchain.mcc") + "; Journal=false; Async=true");
            trdb = new LiteDatabase("Filename=" + Path.Combine(Params.Current.DataFolder, "transactions.mcc") + "; Journal=false; Async=true");

            var mapper = BsonMapper.Global;
            mapper.Entity<ITransaction>().Id(p => p._id);

            mapper.Entity<ITransaction>()
                .Field(p => p.Block, "a")
                .Field(p => p.Fee, "b")
                .Field(p => p.Payload, "c")
                .Field(p => p.Signature, "d")
                .Field(p => p.SignerAccount, "e")
                .Field(p => p.TargetAccount, "f")
                .Field(p => p.TransactionType, "g");

            mapper.Entity<ChangeKeyTransaction>()
                .Field(p => p.Block, "a")
                .Field(p => p.Fee, "b")
                .Field(p => p.Payload, "c")
                .Field(p => p.Signature, "d")
                .Field(p => p.SignerAccount, "e")
                .Field(p => p.TargetAccount, "f")
                .Field(p => p.TransactionType, "g")
                .Field(p => p.Amount, "h")
                .Field(p => p.NewAccountKey, "i")
                .Field(p => p.TransactionCount, "j")                
                ;
                
            mapper.Entity<ListAccountTransaction>()
                .Field(p => p.Block, "a")
                .Field(p => p.Fee, "b")
                .Field(p => p.Payload, "c")
                .Field(p => p.Signature, "d")
                .Field(p => p.SignerAccount, "e")
                .Field(p => p.TargetAccount, "f")
                .Field(p => p.TransactionType, "g")
                .Field(p => p.Amount, "h")
                .Field(p => p.NewPublicKey, "i")
                .Field(p => p.TransactionCount, "j")
                .Field(p=>p.AccountPrice,"k")
                .Field(p=>p.AccountToPay, "l")
                .Field(p=>p.LockedUntilBlock, "m")                
                ;

            mapper.Entity<TransferTransaction>()
                .Field(p => p.Block, "a")
                .Field(p => p.Fee, "b")
                .Field(p => p.Payload, "c")
                .Field(p => p.Signature, "d")
                .Field(p => p.SignerAccount, "e")
                .Field(p => p.TargetAccount, "f")
                .Field(p => p.TransactionType, "g")
                .Field(p => p.Amount, "h")
                .Field(p => p.NewAccountKey, "i")
                .Field(p => p.TransactionCount, "j")
                .Field(p => p.AccountPrice, "k")
                .Field(p => p.TransactionStyle, "l")
                .Field(p => p.SellerAccount, "m")
                ;

            mapper.Entity<ChangeAccountInfoTransaction>()
                .Field(p => p.Block, "a")
                .Field(p => p.Fee, "b")
                .Field(p => p.Payload, "c")
                .Field(p => p.Signature, "d")
                .Field(p => p.SignerAccount, "e")
                .Field(p => p.TargetAccount, "f")
                .Field(p => p.TransactionType, "g")
                .Field(p => p.Amount, "h")
                .Field(p => p.NewAccountKey, "i")
                .Field(p => p.TransactionCount, "j")
                .Field(p => p.NewName, "k")
                .Field(p => p.NewType, "l");

            trdb.GetCollection<ITransaction>().EnsureIndex(p => p.Block);
        }

        public int Count
        {
            get
            {
                return db.GetCollection<BlockHeader>().Count();
            }
        }

        public int BlockHeight => db.GetCollection<BlockHeader>().Max(p => p.Id).AsInt32;

        public void AddBlock(Block block)
        {
            db.GetCollection<BlockHeader>().Insert(block.Header);
            trdb.GetCollection<ITransaction>().InsertBulk(block.Transactions);
        }

        public void AddBlocks(IEnumerable<Block> blocks)
        {
            db.GetCollection<BlockHeader>().InsertBulk(blocks.Select(p => p.Header));
            trdb.GetCollection<ITransaction>().InsertBulk(blocks.Where(p => p.Transactions != null).SelectMany(p => p.Transactions));
        }

        public async Task AddBlocksAsync(IEnumerable<Block> blocks)
        {
            var t1 = Task.Factory.StartNew(() =>
            {
                db.GetCollection<BlockHeader>().InsertBulk(blocks.Select(p => p.Header));
            });
            var t2 = Task.Factory.StartNew(() =>
            {
                trdb.GetCollection<ITransaction>().InsertBulk(blocks.Where(p => p.Transactions != null).SelectMany(p => p.Transactions));
            });
            await Task.WhenAll(t1, t2);            
        }

        public void DeleteBlocks(uint from)
        {
            db.GetCollection<BlockHeader>().Delete(p => p.Id >= from);
            trdb.GetCollection<ITransaction>().Delete(p => p.Block >= from);
        }

        public void Dispose()
        {
            db.Dispose();
            trdb.Dispose();
        }

        public Block GetBlock(uint blockNumber)
        {
            var bt = Task<BlockHeader>.Factory.StartNew(() => db.GetCollection<BlockHeader>().FindById((int)blockNumber));
            var tt = Task<IList<ITransaction>>.Factory.StartNew(() => trdb.GetCollection<ITransaction>().Find(p => p.Block == blockNumber).ToList());
            Task.WaitAll(bt, tt);
            var blockHeader = bt.Result;
            if (blockHeader == null) return null;
            var block = new Block
            {
                Header = blockHeader,
                Transactions = tt.Result
            };
            return block;
        }

        public BlockHeader GetBlockHeader(uint blockNumber)
        {
            return db.GetCollection<BlockHeader>().FindById((int)blockNumber);
        }

        public IEnumerable<BlockHeader> GetBlockHeaders(uint startBlock, uint endBlock)
        {
            return db.GetCollection<BlockHeader>().Find(p => p.Id >= startBlock && p.Id <= endBlock);
        }

        public IEnumerable<Block> GetBlocks(uint startBlock, uint endBlock)
        {
            var blockHeaders = GetBlockHeaders(startBlock, endBlock);
            var transactions = trdb.GetCollection<ITransaction>().Find(p => p.Block >= startBlock && p.Block <= endBlock);
            var blocks = new HashSet<Block>();
            foreach(BlockHeader header in blockHeaders)
            {
                var block = new Block
                {
                    Header = header,
                    Transactions = transactions.Where(p => p.Block == header.Id).ToList()
                };
                blocks.Add(block);
            }
            return blocks;
        }
    }
}