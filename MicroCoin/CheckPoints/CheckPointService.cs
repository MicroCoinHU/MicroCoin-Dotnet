//-----------------------------------------------------------------------
// This file is part of MicroCoin - The first hungarian cryptocurrency
// Copyright (c) 2019 Peter Nemeth
// CheckPointService.cs - Copyright (c) 2019 %UserDisplayName%
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
using MicroCoin.Common;
using MicroCoin.Transactions;
using MicroCoin.Types;
using System;
using System.Linq;
using System.Collections.Generic;
using System.Text;
using Microsoft.Extensions.Logging;
using Prism.Events;
using System.Diagnostics;

namespace MicroCoin.CheckPoints
{
    public class CheckPointService : ICheckPointService
    {
        private readonly ICheckPointStorage checkPointStorage;
        private readonly IBlockChain blockChain;
        private readonly ILogger<CheckPointService> logger;

        private readonly IList<CheckPointBlock> modifiedBlocks = new List<CheckPointBlock>();

        public CheckPointService(ICheckPointStorage checkPointStorage, IEventAggregator eventAggregator, IBlockChain blockChain, ILogger<CheckPointService> logger)
        {
            this.checkPointStorage = checkPointStorage;
            this.blockChain = blockChain;
            this.logger = logger;
            eventAggregator.GetEvent<BlocksAdded>().Subscribe(ApplyBlock, ThreadOption.PublisherThread);
        }

        public Account GetAccount(AccountNumber accountNumber, bool @readonly = false)
        {
            CheckPointBlock block;
            if (@readonly)
            {
                int blockNumber = accountNumber / 5;
                block = modifiedBlocks.FirstOrDefault(p => p.Id == blockNumber);
                if (block != null)
                {
                    return block.Accounts.FirstOrDefault(p => p.AccountNumber == accountNumber);
                }
                else
                {
                    return checkPointStorage.GetAccount(accountNumber);
                }
            }
            block = GetBlockForAccount(accountNumber);
            return block.Accounts.FirstOrDefault(p => p.AccountNumber == accountNumber);
        }

        protected CheckPointBlock GetBlockForAccount(AccountNumber accountNumber)
        {
            int blockNumber = accountNumber / 5;
            var block = modifiedBlocks.FirstOrDefault(p => p.Id == blockNumber);
            if (block != null) return block;
            block = checkPointStorage.GetBlock(blockNumber);
            modifiedBlocks.Add(block);
            return block;
        }

        private Dictionary<Type, Type> validators = new Dictionary<Type, Type>();

        public void ApplyBlock(Block block)
        {
            CheckPointBlock checkPointBlock = new CheckPointBlock
            {
                Header = block.Header
            };
            for (var i = block.Id * 5; i < block.Id * 5 + 5; i++)
            {
                ulong totalFee = 0;
                if (block.Transactions != null)
                    totalFee = (ulong)(block.Transactions.Sum(p => p.Fee.value) * 10000);
                var account = new Account
                {
                    AccountNumber = i,
                    BlockNumber = block.Id,
                    NumberOfOperations = 0,
                    UpdatedBlock = block.Id,
                    Balance = i % 5 == 0 ? (1000000UL + totalFee) : 0UL
                };
                account.AccountInfo.AccountKey = block.Header.AccountKey;
                account.AccountInfo.State = AccountState.Normal;
                checkPointBlock.Accounts.Add(account);
            }
            if (block.Transactions != null)
            {
                var st = Stopwatch.StartNew();
                foreach (ITransaction item in block.Transactions)
                {
                    Type validatorType = null;
                    if (validators.ContainsKey(item.GetType()))
                    {
                        validatorType = validators[item.GetType()];
                    }
                    else
                    {
                        validatorType = typeof(ITransactionValidator<>).MakeGenericType(item.GetType());
                        validators.Add(item.GetType(), validatorType);
                    }
                    dynamic validator = ServiceLocator.ServiceProvider.GetService(validatorType);
                    if (validator != null)                        
                    {                        
                        if (!validator.IsValid((dynamic)item))
                        {
                            validator.IsValid((dynamic)item);
                            throw new Exception("Invalid transaction");
                        }
                        else
                        {
                            var modified = item.Apply(this);
                            foreach(var account in modified)
                            {
                                account.UpdatedBlock = block.Id;
                                var accountBlock = modifiedBlocks.FirstOrDefault(p => p.Id == account.AccountNumber / 5);
                                if (accountBlock == null)
                                {
                                    accountBlock = GetBlockForAccount(account.AccountNumber);
                                    modifiedBlocks.Add(accountBlock);
                                }
                                accountBlock.Accounts[account.AccountNumber % 5] = account;
                            }
                        }
                    }
                    else
                    {
                        GetAccount(item.SignerAccount).NumberOfOperations += 1;
                        logger.LogWarning("No validator found for transaction type {0}", item.GetType());
                    }
                }
                st.Stop();
                if (block.Transactions.Count >= 10)
                {
                    double speed = 0;
                    if (st.ElapsedMilliseconds > 0)
                    {
                        speed = block.Transactions.Count / (st.Elapsed.TotalSeconds);
                    }
                    logger.LogInformation("Processed {0} transactions in {1}, Speed {2} T/s", block.Transactions.Count, st.Elapsed.TotalSeconds, speed);
                }
            }
            modifiedBlocks.Add(checkPointBlock);
            if (checkPointBlock.Id > 0 && (checkPointBlock.Id + 1) % 100 == 0)
            {
                checkPointStorage.AddBlocks(modifiedBlocks);
                modifiedBlocks.Clear();
            }
        }

        public void LoadFromBlockChain()
        {
            var start = (blockChain.BlockHeight / 100) * 100;
            for (uint i = (uint)start; i <= blockChain.BlockHeight; i++)
            {
                var block = blockChain.GetBlock(i);                
                if (block == null) return;
                if (block.Transactions != null)
                    Debug.WriteLine("OK");
                ApplyBlock(block);
            }
        }
    }
}
