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
using MicroCoin.CheckPoints;
using MicroCoin.Types;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace MicroCoin.LiteDb.CheckPoints
{
    public class CheckPointLiteDbStorage : ICheckPointStorage, IDisposable
    {
        private LiteDatabase checkpointdb;
        private readonly IBlockChainStorage blockChainStorage;
        private readonly IAccountStorage accountStorage;

        public CheckPointLiteDbStorage(IBlockChainStorage blockChainStorage, IAccountStorage accountStorage)
        {
            this.blockChainStorage = blockChainStorage;
            this.accountStorage = accountStorage;

            if (!Directory.Exists(Params.Current.DataFolder))
            {
                Directory.CreateDirectory(Params.Current.DataFolder);
            }

            checkpointdb = new LiteDatabase("Filename=" + Path.Combine(Params.Current.DataFolder, "checkpoints.mcc") + "; Journal=false; Async=true");

            var mapper = BsonMapper.Global;
            mapper.Entity<CheckPointBlock>()
                .Field(p => p.AccumulatedWork, "b")
                .Field(p => p.BlockHash, "c")
                .Ignore(p => p.Header)
                .Ignore(p => p.Accounts);
        }

        public CheckPointBlock LastBlock
        {
            get
            {
                var id = checkpointdb.GetCollection<CheckPointBlock>().Max(p => p.Id).AsInt64;
                var block = checkpointdb.GetCollection<CheckPointBlock>().FindById(id);
                if (block == null) return null;
                for (uint i = (uint)id * 5; i < (id + 1) * 5; i++)
                {
                    block.Accounts.Add(accountStorage.GetAccount(i));
                }
                return block;
            }
        }

        public List<Hash> CheckPointHash
        {
            get
            {
                return checkpointdb.GetCollection<CheckPointBlock>().FindAll().Select(p => p.BlockHash).ToList();
            }
        }

        public void AddBlock(CheckPointBlock block)
        {
            accountStorage.AddAccounts(block.Accounts);
            checkpointdb.GetCollection<CheckPointBlock>().Upsert(block);
        }

        public void AddBlocks(IEnumerable<CheckPointBlock> blocks)
        {
            accountStorage.AddAccounts(blocks.SelectMany(p => p.Accounts).ToList());
            checkpointdb.GetCollection<CheckPointBlock>().Upsert(blocks);
        }

        public void Dispose()
        {
            checkpointdb.Dispose();
        }

        public Account GetAccount(AccountNumber accountNumber)
        {
            return accountStorage.GetAccount(accountNumber);
        }

        public CheckPointBlock GetBlock(int blockNumber)
        {
            var cb = checkpointdb.GetCollection<CheckPointBlock>().FindById(blockNumber);
            if (cb == null) return null;
            cb.Header = blockChainStorage.GetBlockHeader((uint)blockNumber);
            var minAccount = blockNumber * 5;
            var maxAccount = blockNumber * 5 + 4;
            for (var i = minAccount; i <= maxAccount; i++)
            {
                cb.Accounts.Add(accountStorage.GetAccount((uint)i));
            }
            return cb;
        }

        private void Backup(string name)
        {
            var id = checkpointdb.GetCollection<CheckPointBlock>().Max(p => p.Id).AsInt64;
            id = id / 100 % 10;
            string dataPath = Params.Current.DataFolder;
            string backupPath = Path.Combine(Params.Current.DataFolder, "backups");
            if (!Directory.Exists(backupPath)) Directory.CreateDirectory(backupPath);
            string newName = Path.Combine(backupPath, string.Format("{0}_{1}.mcc", name, id));
            string oldName = Path.Combine(dataPath, string.Format("{0}.mcc", name));
            if (File.Exists(oldName))
            {
                File.Copy(oldName, newName, true);
            }
        }

        public void SaveState()
        {
            Backup("accounts");
            Backup("checkpoints");
        }

        public void UpdateBlock(CheckPointBlock block)
        {
            checkpointdb.GetCollection<CheckPointBlock>().Update(block);
        }

        public ICollection<Account> GetAccounts(AccountNumber from, int limit)
        {
            return accountStorage.GetAccounts(from, limit);
        }

        public int GetAccountCount()
        {
            return accountStorage.GetAccountCount();
        }

        public decimal GetTotalBalance()
        {
            return 0;
        }
    }
}