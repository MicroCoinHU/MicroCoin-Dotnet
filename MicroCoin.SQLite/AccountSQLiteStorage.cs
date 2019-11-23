using MicroCoin.Chain;
using MicroCoin.CheckPoints;
using MicroCoin.Types;
using System;
using System.Collections.Generic;
using System.Linq;

namespace MicroCoin.SQLite
{
    public class AccountSQLiteStorage : IAccountStorage, IDisposable
    {
        private readonly MicroCoinDBContext dBContext;

        public AccountSQLiteStorage()
        {
            dBContext = new MicroCoinDBContext();
        }

        public void AddAccounts(IList<Account> modifiedAccounts)
        {
            modifiedAccounts.ToList().ForEach(p => p.AccountInfo.AccountNumber = p.AccountNumber);
            dBContext.Accounts.RemoveRange(
                dBContext.Accounts.Where(p => modifiedAccounts.Select(p => p.AccountNumber).Contains(p.AccountNumber)
                ));
            dBContext.Accounts.AddRange(modifiedAccounts);
            dBContext.SaveChanges();
        }

        public void Dispose()
        {
            dBContext.Dispose();
        }

        public Account GetAccount(AccountNumber accountNumber)
        {
            return dBContext.Accounts.Find(accountNumber);
        }

        public int GetAccountCount()
        {
            return dBContext.Accounts.Count();
        }

        public ICollection<Account> GetAccounts(AccountNumber from, int limit)
        {
            return dBContext.Accounts.Where(p => p.AccountNumber >= from).Take(limit).ToHashSet();
        }

        public decimal GetTotalBalance()
        {
            return dBContext.Accounts.Sum(p => (decimal)p.Balance);
        }
    }
}