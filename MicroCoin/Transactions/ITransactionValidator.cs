using System;
using System.Collections.Generic;
using System.Text;

namespace MicroCoin.Transactions
{
    public interface ITransactionValidator<T> where T : class, ITransaction
    {
        bool IsValid(T transaction);
    }
}
