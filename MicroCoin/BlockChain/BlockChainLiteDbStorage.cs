//-----------------------------------------------------------------------
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
using System.Linq;
using System.Threading.Tasks;

namespace MicroCoin.BlockChain
{
    public class BlockChainLiteDbStorage : IBlockChainStorage
    {
        private readonly LiteDatabase db = new LiteDatabase("Filename=blockchain.db; Journal=false;");

        public BlockChainLiteDbStorage()
        {
            var mapper = BsonMapper.Global;
            mapper.Entity<Block>()
                .Field(p => p.Transactions, "a")
                .Field(p => p.Header, "b")
                .DbRef(p => p.Header, "BlockHeader")
                .DbRef(p => p.Transactions, "ITransaction");
            mapper.Entity<ITransaction>()
                .Field(p => p.AccountKey, "a")
                .Field(p => p.SignerAccount, "b")
                .Field(p => p.TargetAccount, "c")
                .Field(p => p.TransactionType, "d")
                .Field(p => p.Fee, "e")
                .Field(p => p.Payload, "f")
                .Field(p => p.Signature, "g");
            mapper.Entity<Transaction>()
                .Field(p => p.AccountKey, "a")
                .Field(p => p.SignerAccount, "b")
                .Field(p => p.TargetAccount, "c")
                .Field(p => p.TransactionType, "d")
                .Field(p => p.Fee, "e")
                .Field(p => p.Payload, "f")
                .Field(p => p.Signature, "g");
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
                .Field(p => p.Amount, "j");
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
                .Field(p => p.NumberOfOperations, "m");
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
                .Field(p => p.Signature, "m");
        }

        public int Count
        {
            get
            {
                return db.GetCollection<Block>().Count();
            }
        }

        public int BlockHeight => db.GetCollection<Block>().Max(p => p.Id).AsInt32;

        public void AddBlock(Block block)
        {
            db.GetCollection<BlockHeader>().Upsert(block.Header);
            db.GetCollection<ITransaction>().Upsert(block.Transactions);
            db.GetCollection<Block>().Upsert(block);
        }

        public void AddBlocks(IEnumerable<Block> blocks)
        {
            db.GetCollection<BlockHeader>().Upsert(blocks.Select(p => p.Header));
            db.GetCollection<ITransaction>().Upsert(blocks.Where(p => p.Transactions != null).SelectMany(p => p.Transactions));
            db.GetCollection<Block>().Upsert(blocks);
        }

        public async Task AddBlocksAsync(IEnumerable<Block> blocks)
        {
            await Task.Run(() =>
            {
                db.GetCollection<Block>().Upsert(blocks);
            });
        }

        public void Dispose()
        {
            db.Dispose();
        }

        public Block GetBlock(uint blockNumber)
        {
            return db.GetCollection<Block>()
                .Include(p => p.Header)
                .Include(p => p.Transactions)
                .FindById((int)blockNumber);
        }
    }
}