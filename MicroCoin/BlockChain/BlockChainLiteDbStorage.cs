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
    class BlockChainLiteDbStorage : IBlockChain, IDisposable
    {
        private readonly LiteDatabase db = new LiteDatabase("blocks.db");

        public BlockChainLiteDbStorage()
        {
            var mapper = BsonMapper.Global;
            mapper.Entity<Block>().Field(p => p.Transactions, "a").Field(p => p.Header, "b");
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


            //            db.Engine.Shrink();
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
            /*
            var name = block.Header.BlockNumber.ToString();
            var ls = db.FileStorage.OpenWrite(name, name, null  );
            block.SaveToStream(ls);
            ls.Dispose();
            */
            db.GetCollection<Block>().Upsert(block);
        }

        public void AddBlocks(IEnumerable<Block> blocks)
        {
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
//            db.Shrink();
            db.Dispose();
        }

        public Block GetBlock(uint blockNumber)
        {
            return db.GetCollection<Block>().FindById((int)blockNumber);
        }
    }
}
