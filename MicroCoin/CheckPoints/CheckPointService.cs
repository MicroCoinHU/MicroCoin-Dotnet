using MicroCoin.BlockChain;
using MicroCoin.Chain;
using MicroCoin.Types;
using System;
using System.Collections.Generic;
using System.Text;

namespace MicroCoin.CheckPoints
{
    public class CheckPointService : ICheckPointService
    {
        private readonly ICheckPointStorage checkPointStorage;
        private readonly IList<CheckPointBlock> modifiedBlocks = new List<CheckPointBlock>();

        public CheckPointService(ICheckPointStorage checkPointStorage)
        {
            this.checkPointStorage = checkPointStorage;
        }

        public Account GetAccount(AccountNumber accountNumber)
        {
            return checkPointStorage.GetAccount(accountNumber);
        }

        public void ApplyBlock(Block block)
        {
            CheckPointBlock checkPointBlock = new CheckPointBlock
            {
                Header = block.Header
            };
            for (var i = block.Id * 5; i < block.Id * 5 + 5; i++)
            {
                var index = (int)i;
                checkPointBlock.Accounts[index].AccountNumber = i;
                checkPointBlock.Accounts[index].Balance = i % 5 == 0 ? 1000000UL : 0UL;
                checkPointBlock.Accounts[index].BlockNumber = block.Id;
                checkPointBlock.Accounts[index].NumberOfOperations = 0;
                checkPointBlock.Accounts[index].UpdatedBlock = block.Id;
                checkPointBlock.Accounts[index].AccountInfo.AccountKey = block.Header.AccountKey;
            }
            foreach (var item in block.Transactions)
            {
            }
            modifiedBlocks.Add(checkPointBlock);
            if (checkPointBlock.Id > 0 && checkPointBlock.Id % 100 == 0)
            {

            }
        }
    }
}
