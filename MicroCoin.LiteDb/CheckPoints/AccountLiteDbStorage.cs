using LiteDB;
using MicroCoin.Chain;
using MicroCoin.CheckPoints;
using MicroCoin.Types;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace MicroCoin.LiteDb.CheckPoints
{
    public class AccountLiteDbStorage : IAccountStorage, IDisposable
    {
        private readonly LiteDatabase accountsdb;

        public AccountLiteDbStorage()
        {
            if (!Directory.Exists(Params.Current.DataFolder))
            {
                Directory.CreateDirectory(Params.Current.DataFolder);
            }

            var mapper = BsonMapper.Global;

            mapper.Entity<Account>()
                .Id(p => p.AccountNumber)
                .Field(p => p.AccountInfo, "a")
                .Field(p => p.AccountNumber, "b")
                .Field(p => p.AccountType, "c")
                .Field(p => p.Balance, "d")
                .Field(p => p.BlockNumber, "e")
                .Field(p => p.Name, "f")
                .Field(p => p.TransactionCount, "g")
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
                .Ignore(p => p.AccountNumber)
                .Ignore(p => p.VisiblePrice);

            accountsdb = new LiteDatabase("Filename=" + Path.Combine(Params.Current.DataFolder, "accounts.mcc") + "; Journal=false; Async=true");

            accountsdb.GetCollection<Account>().EnsureIndex(p => p.Balance);
            accountsdb.GetCollection<Account>().EnsureIndex(p => p.Name);
        }

        public void AddAccounts(IList<Account> modifiedAccounts)
        {
            accountsdb.GetCollection<Account>().Upsert(modifiedAccounts);
        }

        public void Dispose()
        {
            accountsdb.Dispose();
        }

        public Account GetAccount(AccountNumber accountNumber)
        {
            return accountsdb.GetCollection<Account>().FindById((int)accountNumber);
        }

        public int GetAccountCount()
        {
            return accountsdb.GetCollection<Account>().Count();
        }

        public ICollection<Account> GetAccounts(AccountNumber from, int limit)
        {
            return accountsdb.GetCollection<Account>().Find(p => p.AccountNumber >= from, 0, limit).ToHashSet();
        }

        public decimal GetTotalBalance()
        {
            return 0;
            //return accountsdb.GetCollection<Account>().Find(p => p.Balance > 0).Sum(p => (decimal)p.Balance);
        }
    }
}
