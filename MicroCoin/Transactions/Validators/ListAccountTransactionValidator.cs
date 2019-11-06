//-----------------------------------------------------------------------
// This file is part of MicroCoin - The first hungarian cryptocurrency
// Copyright (c) 2019 Peter Nemeth
// ListAccountTransactionValidator.cs - Copyright (c) 2019 Németh Péter
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
using MicroCoin.BlockChain;
using MicroCoin.Chain;
using MicroCoin.CheckPoints;

namespace MicroCoin.Transactions.Validators
{
    public class ListAccountTransactionValidator : ITransactionValidator<ListAccountTransaction>
    {

        private readonly ICheckPointService checkPointService;
        private readonly IBlockChain blockChain;
        public ListAccountTransactionValidator(ICheckPointService checkPointService, IBlockChain blockChain)
        {
            this.checkPointService = checkPointService;
            this.blockChain = blockChain;
        }

        public bool IsValid(ListAccountTransaction transaction)
        {
            var blockHeight = blockChain.BlockHeight;

            var signer = checkPointService.GetAccount(transaction.SignerAccount);
            if (signer.Balance < transaction.Fee) return false;
            if (signer.AccountInfo.LockedUntilBlock > blockHeight) return false;
            if (transaction.SignerAccount != transaction.TargetAccount)
            {
                //if (signer.AccountInfo.State != AccountState.Normal) return false;
            }
            var target = checkPointService.GetAccount(transaction.TargetAccount);
            if (target.AccountInfo.LockedUntilBlock > blockHeight) return false;
            if (transaction.TransactionType == TransactionType.ListAccountForSale)
            {
                //if (target.AccountInfo.State != AccountState.Normal)
                //    return false;
            }
            else
            {
                // if (target.AccountInfo.State != AccountState.Sale)
                //     return false;
            }

            var seller = checkPointService.GetAccount(transaction.AccountToPay);
            if (seller.AccountInfo.State != AccountState.Normal) return false;
            return true;
        }
    }
}
