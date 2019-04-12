//-----------------------------------------------------------------------
// This file is part of MicroCoin - The first hungarian cryptocurrency
// Copyright (c) 2019 Peter Nemeth
// CheckPointLiteDbStorage.cs - Copyright (c) 2019 Németh Péter
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
using MicroCoin.BlockChain;
using MicroCoin.Chain;
using MicroCoin.Types;
using System;
using System.Collections.Generic;
using System.Linq;

namespace MicroCoin.CheckPoints
{
    public class CheckPointLiteDbStorage : ICheckPointStorage, IDisposable
    {
        private readonly LiteDatabase db = new LiteDatabase("Filename=blockchain.db; Journal=false; Async=true");
        private readonly LiteDatabase accountdb = new LiteDatabase("Filename=accounts.db; Journal=false; Async=true");
        private readonly LiteDatabase checkpointdb = new LiteDatabase("Filename=checkpoints.db; Journal=false; Async=true");

        public CheckPointLiteDbStorage()
        {
            var mapper = BsonMapper.Global;
            mapper.Entity<CheckPointBlock>()
                .Field(p => p.AccumulatedWork, "b")
                .Field(p => p.BlockHash, "c")
                .Ignore(p => p.Header)
                .Ignore(p => p.Accounts);

            mapper.Entity<Account>()
                .Id(p => p.AccountNumber)
                .Field(p => p.AccountInfo, "a")
                .Field(p => p.AccountNumber, "b")
                .Field(p => p.AccountType, "c")
                .Field(p => p.Balance, "d")
                .Field(p => p.BlockNumber, "e")
                .Field(p => p.Name, "f")
                .Field(p => p.NumberOfOperations, "g")
                .Field(p => p.UpdatedBlock, "h")
                .Ignore(p => p.VisibleBalance)
                .Ignore(p => p.UpdatedByBlock)
                .Ignore(p => p.Saved)
                .Ignore(p => p.NameAsString);

            mapper.Entity<AccountInfo>()
                .Field(p => p.AccountToPayPrice, "a")
                .Field(p => p.AccountKey, "b")
                .Field(p => p.LockedUntilBlock, "c")
                .Field(p => p.NewPublicKey, "d")
                .Field(p => p.Price, "e")
                .Field(p => p.State, "f")
                .Ignore(p => p.StateString)
                .Ignore(p => p.VisiblePrice);
        }

        public void AddAccounts(IList<Account> modifiedAccounts)
        {
            accountdb.GetCollection<Account>().Upsert(modifiedAccounts);
        }

        public void AddBlock(CheckPointBlock block)
        {
            accountdb.GetCollection<Account>().Upsert(block.Accounts);
            checkpointdb.GetCollection<CheckPointBlock>().Insert(block);
        }

        public void AddBlocks(IEnumerable<CheckPointBlock> blocks)
        {
            accountdb.GetCollection<Account>().Upsert(blocks.SelectMany(p => p.Accounts));
            checkpointdb.GetCollection<CheckPointBlock>().InsertBulk(blocks);
        }

        public void Dispose()
        {
            db.Dispose();
            checkpointdb.Dispose();
            accountdb.Dispose();
        }

        public Account GetAccount(AccountNumber accountNumber)
        {
            return accountdb.GetCollection<Account>().FindById((int)accountNumber);
        }

        public CheckPointBlock GetBlock(int blockNumber)
        {
            var cb = checkpointdb.GetCollection<CheckPointBlock>().FindById(blockNumber);
            if (cb == null) return null;
            cb.Header = db.GetCollection<BlockHeader>().FindById(blockNumber);
            var minAccount = blockNumber * 5;
            var maxAccount = blockNumber * 5 + 4;
            for(var i = minAccount; i <= maxAccount; i++)
            {
                cb.Accounts.Add(accountdb.GetCollection<Account>().FindById(i));
            }
            return cb;
        }
    }
}