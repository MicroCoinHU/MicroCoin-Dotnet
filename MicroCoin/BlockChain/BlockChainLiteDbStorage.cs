//-----------------------------------------------------------------------
// This file is part of MicroCoin - The first hungarian cryptocurrency
// Copyright (c) 2019 Peter Nemeth
// BlockChainLiteDbStorage.cs - Copyright (c) 2019 %UserDisplayName%
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
using System.Linq;
using System.Threading.Tasks;

namespace MicroCoin.BlockChain
{
    public class BlockChainLiteDbStorage : IBlockChainStorage
    {
        private readonly LiteDatabase db = new LiteDatabase("Filename=blockchain.db; Journal=false;");        
        private readonly LiteDatabase trdb = new LiteDatabase("Filename=transactions.db; Journal=false;");        

        public BlockChainLiteDbStorage()
        {
            var mapper = BsonMapper.Global;
            mapper.Entity<Block>()
                //.Field(p => p.Transactions, "a")
                .Ignore(p => p.Transactions)
                .Field(p => p.Header, "b")
                .DbRef(p => p.Header, "bh");
                //.DbRef(p => p.Transactions, "tr");
            mapper.Entity<ITransaction>()                
                .Id(p=>p._id)               
                .Field(p => p.AccountKey, "a")
                .Field(p => p.SignerAccount, "b")
                .Field(p => p.TargetAccount, "c")
                .Field(p => p.TransactionType, "d")
                .Field(p => p.Fee, "e")
                .Field(p => p.Payload, "f")
                .Field(p => p.Signature, "g")
                .Field(p => p.Block, "h");
            mapper.Entity<Transaction>()
                .Field(p => p.AccountKey, "a")
                .Field(p => p.SignerAccount, "b")
                .Field(p => p.TargetAccount, "c")
                .Field(p => p.TransactionType, "d")
                .Field(p => p.Fee, "e")
                .Field(p => p.Payload, "f")
                .Field(p => p.Signature, "g")
                .Field(p => p.Block, "h");
            mapper.Entity<ChangeKeyTransaction>()
                .Field(p => p.AccountKey, "a")
                .Field(p => p.SignerAccount, "b")
                .Field(p => p.TargetAccount, "c")
                .Field(p => p.TransactionType, "d")
                .Field(p => p.Fee, "e")
                .Field(p => p.Payload, "f")
                .Field(p => p.Signature, "g")
                .Field(p => p.NumberOfOperations, "h")
                .Field(p => p.NewAccountKey, "i")
                .Field(p => p.Amount, "j")
                .Field(p => p.Block, "k");
            mapper.Entity<ListAccountTransaction>()
                .Field(p => p.AccountKey, "a")
                .Field(p => p.SignerAccount, "b")
                .Field(p => p.TargetAccount, "c")
                .Field(p => p.TransactionType, "d")
                .Field(p => p.Fee, "e")
                .Field(p => p.Payload, "f")
                .Field(p => p.Signature, "g")
                .Field(p => p.AccountPrice, "h")
                .Field(p => p.Amount, "i")
                .Field(p => p.AccountToPay, "j")
                .Field(p => p.LockedUntilBlock, "k")
                .Field(p => p.NewPublicKey, "l")
                .Field(p => p.NumberOfOperations, "m")
                .Field(p => p.Block, "n");
            mapper.Entity<TransferTransaction>()
                .Field(p => p.AccountKey, "a")
                .Field(p => p.SignerAccount, "b")
                .Field(p => p.TargetAccount, "c")
                .Field(p => p.SellerAccount, "d")
                .Field(p => p.TransactionStyle, "e")
                .Field(p => p.NumberOfOperations, "f")
                .Field(p => p.NewAccountKey, "g")
                .Field(p => p.AccountPrice, "h")
                .Field(p => p.Amount, "i")
                .Field(p => p.TransactionType, "j")
                .Field(p => p.Fee, "k")
                .Field(p => p.Payload, "l")
                .Field(p => p.Signature, "m")
                .Field(p => p.Block, "n");
            //mapper.Entity<Block>().Field(p => p.Header, "h");
            //.Field(p => p.Transactions, "t");
            trdb.GetCollection<ITransaction>("tr").EnsureIndex(p => p.Block);
        }

        public int Count
        {
            get
            {
                return db.GetCollection<BlockHeader>("bh").Count();
            }
        }

        public int BlockHeight => db.GetCollection<BlockHeader>("bh").Max(p => p.Id).AsInt32;

        public void AddBlock(Block block)
        {
            db.GetCollection<BlockHeader>("bh").Upsert(block.Header);
            trdb.GetCollection<ITransaction>("tr").Upsert(block.Transactions);
        }

        public void AddBlocks(IEnumerable<Block> blocks)
        {
            db.GetCollection<BlockHeader>("bh").Upsert(blocks.Select(p => p.Header));
            trdb.GetCollection<ITransaction>("tr").Upsert(blocks.Where(p => p.Transactions != null).SelectMany(p => p.Transactions));
        }

        public async Task AddBlocksAsync(IEnumerable<Block> blocks)
        {
            
            var t1 = Task.Factory.StartNew(() =>
            {
                db.GetCollection<BlockHeader>("bh").Upsert(blocks.Select(p => p.Header));
            });
            var t2 = Task.Factory.StartNew(() =>
            {
                trdb.GetCollection<ITransaction>("tr").Upsert(blocks.Where(p => p.Transactions != null).SelectMany(p => p.Transactions));
            });
            await Task.WhenAll(t1, t2, t2);
        }

        public void Dispose()
        {
            db.Dispose();
        }

        public Block GetBlock(uint blockNumber)
        {
            var bt = Task<BlockHeader>.Factory.StartNew(() => db.GetCollection<BlockHeader>("bh").FindById((int)blockNumber));
            var tt = Task<IList<ITransaction>>.Factory.StartNew(() => trdb.GetCollection<ITransaction>("tr").Find(p => p.Block == blockNumber).ToList());
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
    }
}