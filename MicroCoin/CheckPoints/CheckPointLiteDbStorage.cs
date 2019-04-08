using LiteDB;
using MicroCoin.BlockChain;
using MicroCoin.Chain;
using MicroCoin.Cryptography;
using MicroCoin.Types;
using System;
using System.Collections.Generic;
using System.Text;
using System.Linq;

namespace MicroCoin.CheckPoints
{
    public class CheckPointLiteDbStorage : ICheckPointStorage, IDisposable
    {
        private readonly LiteDatabase db = new LiteDatabase("checkpoints.db");

        public CheckPointLiteDbStorage()
        {
            var mapper = BsonMapper.Global;
            mapper.Entity<CheckPointBlock>()
                .Field(p => p.Accounts, "a")
                .Field(p => p.AccumulatedWork, "b")
                .Field(p => p.BlockHash, "c")
                .Field(p => p.Header, "d");                
            mapper.Entity<Account>()
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

        public void AddBlock(CheckPointBlock block)
        {
            db.GetCollection<CheckPointBlock>().Upsert(block);
        }

        public void AddBlocks(IEnumerable<CheckPointBlock> blocks)
        {
            db.GetCollection<CheckPointBlock>().Upsert(blocks);            
        }

        public void Dispose()
        {
            db.Dispose();
        }

        public Account GetAccount(AccountNumber accountNumber)
        {
            var block = GetBlock(accountNumber / 5);
            if (block != null)
                return block.Accounts.FirstOrDefault(p => p.AccountNumber == accountNumber);
            return null;
        }

        public CheckPointBlock GetBlock(int blockNumber)
        {
            return db.GetCollection<CheckPointBlock>().FindById(blockNumber);
        }
    }
}
