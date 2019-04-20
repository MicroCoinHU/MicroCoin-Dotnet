//-----------------------------------------------------------------------
// This file is part of MicroCoin - The first hungarian cryptocurrency
// Copyright (c) 2019 Peter Nemeth
// CheckPointService.cs - Copyright (c) 2019 Németh Péter
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
using System.Collections.Concurrent;
using MicroCoin.Handlers;
using MicroCoin.Cryptography;
using System.IO;

namespace MicroCoin.CheckPoints
{
    public class InvalidTransactionException : Exception
    {
        public InvalidTransactionException(string message) : base(message)
        {
        }
    }

    public class CheckPointService : ICheckPointService
    {
        // Services
        private readonly ICheckPointStorage checkPointStorage;
        private readonly IBlockChain blockChain;
        private readonly ILogger<CheckPointService> logger;
        private readonly ICryptoService cryptoService;

        // Lists & caches
        private readonly IList<CheckPointBlock> modifiedBlocks = new List<CheckPointBlock>();
        private readonly IList<Account> modifiedAccounts = new List<Account>();
        private readonly Dictionary<Type, Type> validators = new Dictionary<Type, Type>();
        private readonly IList<string> pendingTransactions = new List<string>();
        private readonly List<Hash> hashBuffer = new List<Hash>();
        
        // internal variables
        private readonly object checkPointLock = new object();

        private ulong accumulatedWork = 0;

        public CheckPointService(ICheckPointStorage checkPointStorage, IEventAggregator eventAggregator,
            IBlockChain blockChain, ILogger<CheckPointService> logger, ICryptoService cryptoService)
        {
            this.checkPointStorage = checkPointStorage;
            this.blockChain = blockChain;
            this.logger = logger;
            this.cryptoService = cryptoService;
            eventAggregator.GetEvent<BlocksAdded>().Subscribe(ProcessBlock, ThreadOption.PublisherThread);
            eventAggregator.GetEvent<NewTransaction>().Subscribe(HandleNewTransaction, ThreadOption.PublisherThread);
        }

        public void HandleNewTransaction(ITransaction transaction)
        {
            try
            {
                //ProcessTransaction(transaction);
                logger.LogInformation("Process transaction {0} signer: {1}", transaction.TransactionType, transaction.SignerAccount);
            }
            catch (InvalidTransactionException e)
            {
                logger.LogWarning("Skipping invalid pending transaction {0}", e.Message);
            }
        }

        public Account GetAccount(AccountNumber accountNumber, bool @readonly = false)
        {
            var account = modifiedAccounts.FirstOrDefault(p=>p.AccountNumber == accountNumber);
            if (account != null) return account;
            var block = GetBlockForAccount(accountNumber);
            account = block.Accounts.FirstOrDefault(p => p.AccountNumber == accountNumber);
            if (!modifiedAccounts.Any(p => p.AccountNumber == account.AccountNumber))
            {
                modifiedAccounts.Add(account);
            }
            return account;
        }

        protected CheckPointBlock GetBlockForAccount(AccountNumber accountNumber)
        {
            int blockNumber = accountNumber / 5;
            var block = modifiedBlocks.FirstOrDefault(p => p.Id == blockNumber);
            if (block != null) return block;
            block = checkPointStorage.GetBlock(blockNumber);
            return block;
        }

        protected CheckPointBlock GetBlock(int blockNumber)
        {
            var block = modifiedBlocks.FirstOrDefault(p => p.Id == blockNumber);
            if (block != null) return block;
            block = checkPointStorage.GetBlock(blockNumber);
            return block;
        }

        protected void UpgradeBlocks()
        {
            hashBuffer.Clear();
            for (int i = 0; i < 101; i++)
            {
                var block = GetBlock(i);
                block.Header = blockChain.GetBlockHeader((uint)i);
                block.BlockHash = block.CalculateBlockHash(false);
                hashBuffer.Add(block.BlockHash);
                modifiedBlocks.Add(block);
            }
        }

        public void ProcessBlock(Block block)
        {
            try
            {
                lock (checkPointLock)
                {
                    if (pendingTransactions.Count > 0)
                    {
                        if (block.Transactions != null && block.Transactions.Count > 0)
                        {
                            pendingTransactions.Clear();
                            modifiedAccounts.Clear();
                        }
                    }
                    if (GetBlock((int)block.Id) != null) return;
                    if (block.Id == 101)
                    {
                        UpgradeBlocks();
                    }
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
                            TransactionCount = 0,
                            UpdatedBlock = block.Id,
                            Balance = i % 5 == 0 ? (1000000UL + totalFee) : 0UL
                        };
                        account.AccountInfo.AccountKey = block.Header.AccountKey;
                        account.AccountInfo.State = AccountState.Normal;
                        checkPointBlock.Accounts.Add(account);
                    }
                    accumulatedWork += block.Header.CompactTarget;
                    checkPointBlock.AccumulatedWork = accumulatedWork;
                    foreach (var n in modifiedBlocks)
                    {
                        n.BlockHash = n.CalculateBlockHash(block.Id < 101);
                        hashBuffer[(int)n.Id] = n.BlockHash;
                    }
                    Hash sha;
                    if (hashBuffer.Count == 0)
                    {
                        sha = cryptoService.Sha256(Params.GenesisPayload);
                    }
                    else
                    {
                        using (var ms = new MemoryStream(hashBuffer.Count * 32))
                        {
                            foreach (var h in hashBuffer)
                            {
                                ms.Write(h, 0, 32);
                            }
                            sha = cryptoService.Sha256(ms);
                        }
                    }

                    if (sha != checkPointBlock.Header.CheckPointHash)
                    {
                        throw new Exception("Invalid checkpoint hash");
                    }
                    if (block.Transactions != null)
                    {
                        foreach (ITransaction item in block.Transactions)
                        {
                            try
                            {
                                var accounts = ProcessTransaction(item, block);
                                var mBlocks = new List<CheckPointBlock>();
                                foreach (var account in accounts)
                                {
                                    int id = account.AccountNumber / 5;
                                    CheckPointBlock mBlock;
                                    if (mBlocks.Any(p => p.Id == id))
                                    {
                                        mBlock = mBlocks.First(p => p.Id == id);
                                    }
                                    else
                                    {
                                        mBlock = GetBlock(id);
                                        if (mBlock.Header == null)
                                            mBlock.Header = blockChain.GetBlockHeader((uint)id);
                                        mBlocks.Add(mBlock);
                                    }
                                    int accountIndex = account.AccountNumber % 5;
                                    mBlock.Accounts[accountIndex] = account;
                                }
                                foreach(var mBlock in mBlocks)
                                {
                                    mBlock.BlockHash = mBlock.CalculateBlockHash(block.Id < 101);
                                    hashBuffer[(int)mBlock.Id] = mBlock.BlockHash;
                                    if (!modifiedBlocks.Any(p => p.Id == mBlock.Id))
                                    {
                                        modifiedBlocks.Add(mBlock);
                                    }
                                }
                            }
                            catch (InvalidTransactionException e)
                            {
                                ProcessTransaction(item, block);
                            }
                        }
                    }
                    checkPointBlock.BlockHash = checkPointBlock.CalculateBlockHash(block.Id < 101);
                    hashBuffer.Add(checkPointBlock.BlockHash);
                    if (!modifiedBlocks.Any(p => p.Id == checkPointBlock.Id))
                    {
                        modifiedBlocks.Add(checkPointBlock);
                    }
                    if (checkPointBlock.Id > 0 && (checkPointBlock.Id + 1) % 100 == 0)
                    {
                        logger.LogInformation("Saving {0} blocks and {1} accounts", modifiedBlocks.Count, modifiedAccounts.Count);
                        checkPointStorage.AddBlocks(modifiedBlocks);
                        if (modifiedAccounts.Count > 0) 
                            checkPointStorage.AddAccounts(modifiedAccounts);
                        logger.LogInformation("Saved {0} blocks and {1} accounts new height: {2}", modifiedBlocks.Count, modifiedAccounts.Count, checkPointBlock.Id);
                        modifiedBlocks.Clear();
                        modifiedAccounts.Clear();
                    }
                }
            }
            catch(Exception e)
            {
                Debug.Fail(e.StackTrace);
            }
        }

        private IList<Account> ProcessTransaction(ITransaction transaction, Block block = null)
        {
            lock (checkPointLock)
            {
                var modifiedAccounts = new List<Account>();
                var sha = transaction.SHA();
                if (pendingTransactions.Contains(sha))
                {
                    if (block != null)
                    {
                        var accounts = transaction.GetModifiedAccounts(this);
                        foreach (var account in accounts)
                        {
                            if (!modifiedAccounts.Any(p => p.AccountNumber == account.AccountNumber))
                            {
                                modifiedAccounts.Add(account);
                            }
                        }
                        pendingTransactions.Remove(sha);
                    }
                    return modifiedAccounts;
                }
                dynamic validator = GetValidator(transaction);
                if (validator != null && transaction != null)
                {
                    if (!validator.IsValid((dynamic)transaction))
                    {
                        if (block != null)
                        {
                            blockChain.DeleteBlocks(block.Id);
                        }
                        throw new InvalidTransactionException("Invalid transaction");
                    }
                    var accounts = ApplyTransaction(transaction, block);
                    foreach(var account in accounts)
                    {
                        if (!modifiedAccounts.Any(p => p.AccountNumber == account.AccountNumber))
                        {
                            modifiedAccounts.Add(account);
                        }
                    }
                }
                else
                {
                    throw new InvalidTransactionException("Unknown transaction");
                }
                if (block == null)
                {
                    pendingTransactions.Add(sha);
                }
                return modifiedAccounts;
            }
        }

        private IList<Account> ApplyTransaction(ITransaction transaction, Block block)
        {
            var modified = transaction.Apply(this);
            foreach (var account in modified)
            {
                if (block != null)
                {
                    account.UpdatedBlock = block.Id;
                }
                if (!modifiedAccounts.Any(p => p.AccountNumber == account.AccountNumber))
                {
                    modifiedAccounts.Add(account);
                }
            }
            return modified;
        }

        private dynamic GetValidator(ITransaction transaction)
        {
            Type validatorType;
            if (validators.ContainsKey(transaction.GetType()))
            {
                validatorType = validators[transaction.GetType()];
            }
            else
            {
                validatorType = typeof(ITransactionValidator<>).MakeGenericType(transaction.GetType());
                validators.Add(transaction.GetType(), validatorType);
            }
            dynamic validator = ServiceLocator.ServiceProvider.GetService(validatorType);
            return validator;
        }

        public void LoadFromBlockChain()
        {
            var lastBlock = checkPointStorage.LastBlock;
            if (lastBlock != null)
            {
                accumulatedWork = lastBlock.AccumulatedWork;
                hashBuffer.Clear();
                hashBuffer.AddRange(checkPointStorage.CheckPointHash);
            }
            var start = (blockChain.BlockHeight / 100) * 100;
            for (uint i = (uint)start; i <= blockChain.BlockHeight; i++)
            {
                var block = blockChain.GetBlock(i);
                if (block == null) return;
                ProcessBlock(block);
            }
        }
    }
}
