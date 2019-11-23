//-----------------------------------------------------------------------
// This file is part of MicroCoin - The first hungarian cryptocurrency
// Copyright (c) 2019 Peter Nemeth
// ChangeKeyTransactionValidator.cs - Copyright (c) 2019 Németh Péter
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
using MicroCoin.CheckPoints;
using MicroCoin.Cryptography;

namespace MicroCoin.Transactions.Validators
{
    public class ChangeKeyTransactionValidator : ITransactionValidator<ChangeKeyTransaction>
    {

        private readonly ICheckPointService checkPointService;
        private readonly IBlockChain blockChain;
        private readonly ICryptoService cryptoService;

        public ChangeKeyTransactionValidator(ICheckPointService checkPointService, IBlockChain blockChain, ICryptoService cryptoService)
        {
            this.checkPointService = checkPointService;
            this.blockChain = blockChain;
            this.cryptoService = cryptoService;
        }


        public bool IsValid(ChangeKeyTransaction transaction)
        {
            if (transaction.Fee < 0) return false;
            if (!transaction.IsValid()) return false;

            var blockHeight = blockChain.BlockHeight;

            var signerAccount = checkPointService.GetAccount(transaction.SignerAccount, true);
            //if (signerAccount.AccountInfo.State != AccountState.Normal) return false;

            var targetAccount = checkPointService.GetAccount(transaction.TargetAccount, true);
            //if (targetAccount.AccountInfo.State != AccountState.Normal) return false;

            if (signerAccount.AccountInfo.LockedUntilBlock > blockHeight) return false;
            if (targetAccount.AccountInfo.LockedUntilBlock > blockHeight) return false;

            if (signerAccount.Balance < transaction.Fee) return false;
            if (!cryptoService.ValidateSignature(transaction.GetHash(), transaction.Signature, signerAccount.AccountInfo.AccountKey))
            {
                return false;
            }
            return true;
        }
    }

}