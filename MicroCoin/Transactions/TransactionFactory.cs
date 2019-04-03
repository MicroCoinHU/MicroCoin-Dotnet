using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text;

namespace MicroCoin.Transactions
{
    public class TransactionFactory
    {
        public static ITransaction FromType(TransactionType transactionType)
        {
            switch (transactionType)
            {
                case TransactionType.Transaction:
                case TransactionType.BuyAccount:
                    return new TransferTransaction();
                case TransactionType.ChangeKey:
                case TransactionType.ChangeKeySigned:
                    return new ChangeKeyTransaction();
                case TransactionType.ListAccountForSale:
                case TransactionType.DeListAccountForSale:
                    return new ListAccountTransaction();
                case TransactionType.ChangeAccountInfo:
                    return new ChangeAccountInfoTransaction();
                default:
                    return null;
            }
        }
    }
}
