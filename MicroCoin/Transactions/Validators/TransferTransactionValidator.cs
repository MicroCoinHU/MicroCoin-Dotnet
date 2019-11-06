//-----------------------------------------------------------------------
// This file is part of MicroCoin - The first hungarian cryptocurrency
// Copyright (c) 2019 Peter Nemeth
// TransferTransaction.cs - Copyright (c) 2019 Németh Péter
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
using MicroCoin.Cryptography;

namespace MicroCoin.Transactions.Validators
{
    public class TransferTransactionValidator : ITransactionValidator<TransferTransaction>
    {
        private readonly ICheckPointService checkPointService;
        private readonly IBlockChain blockChain;
        private readonly ICryptoService cryptoService;

        public TransferTransactionValidator(ICheckPointService checkPointService, IBlockChain blockChain, ICryptoService cryptoService)
        {
            this.checkPointService = checkPointService;
            this.blockChain = blockChain;
            this.cryptoService = cryptoService;
        }

        public bool IsValid(TransferTransaction transaction)
        {
            if (transaction.Amount < 0) return false;
            if (transaction.Fee < 0) return false;
            var senderAccount = checkPointService.GetAccount(transaction.SignerAccount, true);
            var targetAccount = checkPointService.GetAccount(transaction.TargetAccount, true);
            if (senderAccount.AccountInfo.LockedUntilBlock > blockChain.BlockHeight) return false;
            if (!cryptoService.ValidateSignature(transaction.GetHash(), transaction.Signature, senderAccount.AccountInfo.AccountKey))
            {
                return false;
            }
            if (transaction.TransactionStyle == TransferTransaction.TransferType.Transaction)
            {
                if (transaction.TargetAccount.Equals(transaction.SignerAccount)) return false;
                if (senderAccount.Balance < transaction.Amount + transaction.Fee) return false;
                var blockHeight = blockChain.BlockHeight;
                if (5 * (blockHeight + 1) < senderAccount.AccountNumber) return false;
                if (senderAccount.AccountInfo.LockedUntilBlock > blockHeight) return false;
                if (senderAccount.TransactionCount + 1 != transaction.TransactionCount) return false;
                if (5 * (blockHeight + 1) < targetAccount.AccountNumber) return false;
            }
            else if (transaction.TransactionStyle == TransferTransaction.TransferType.BuyAccount)
            {
                if (transaction.SellerAccount == transaction.TargetAccount) return false;
                if (transaction.SignerAccount == transaction.TargetAccount) return false;
                if (targetAccount.AccountInfo.State != AccountState.Sale) return false;
            }
            return true;
        }
    }
}