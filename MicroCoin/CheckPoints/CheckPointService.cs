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

        public Account GetAccount(AccountNumber accountNumber)
        {            
            var block = GetBlockForAccount(accountNumber);
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

        public void ApplyBlock(Block block)
        {
            CheckPointBlock checkPointBlock = new CheckPointBlock
            {
                Header = block.Header
            };
            for (var i = block.Id * 5; i < block.Id * 5 + 5; i++)
            {
                var account = new Account();
                account.AccountNumber = i;
                ulong totalFee = 0;
                if (block.Transactions != null)
                    totalFee = (ulong)(block.Transactions.Sum(p => p.Fee.value) * 10000);
                account.Balance = i % 5 == 0 ? (1000000UL + totalFee) : 0UL;                
                account.BlockNumber = block.Id;
                account.NumberOfOperations = 0;
                account.UpdatedBlock = block.Id;
                account.AccountInfo.AccountKey = block.Header.AccountKey;
                account.AccountInfo.State = AccountState.Normal;
                checkPointBlock.Accounts.Add(account);
            }
            if (block.Transactions != null)
            {
                foreach (ITransaction item in block.Transactions)
                {
                    var validatorType = typeof(ITransactionValidator<>).MakeGenericType(item.GetType());
                    dynamic validator = ServiceLocator.ServiceProvider.GetService(validatorType);
                    if (validator != null)                        
                    {                        
                        if(item.SignerAccount == 5503)
                        {
                            Debug.WriteLine("OK");
                        }
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
                                var accountBlock = GetBlockForAccount(account.AccountNumber);
                                if(!modifiedBlocks.Any(p=>p.Id == accountBlock.Id))
                                {
                                    modifiedBlocks.Add(accountBlock);
                                }
                                accountBlock.Accounts[account.AccountNumber % 5] = account;
                                //accountBlock.Accounts[account.AccountNumber % 5].AccountInfo = account.AccountInfo;
                            }
                        }
                    }
                    else
                    {
                        GetAccount(item.SignerAccount).NumberOfOperations += 1;
                        logger.LogWarning("No validator found for transaction type {0}", item.GetType());
                    }
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
                ApplyBlock(block);
            }
        }
    }
}
