using MicroCoin.BlockChain;
using MicroCoin.Chain;
using MicroCoin.Types;

namespace MicroCoin.CheckPoints
{
    public interface ICheckPointService
    {
        void ApplyBlock(Block block);
        Account GetAccount(AccountNumber accountNumber, bool @readonly = false);
        void LoadFromBlockChain();
    }
}