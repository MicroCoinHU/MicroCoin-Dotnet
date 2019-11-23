using MicroCoin.Chain;
using MicroCoin.Types;
using System;
using System.Collections.Generic;
using System.Text;

namespace MicroCoin.CheckPoints
{
    public interface IAccountStorage
    {
        Account GetAccount(AccountNumber accountNumber);
        ICollection<Account> GetAccounts(AccountNumber from, int limit);
        void AddAccounts(IList<Account> modifiedAccounts);
        int GetAccountCount();
        decimal GetTotalBalance();

    }
}
