using LiteDB;
using MicroCoin.Cryptography;
using MicroCoin.Transactions;
using MicroCoin.Utils;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;

namespace MicroCoin.BlockChain
{
    class BlockChainLiteDbFileStorage : IBlockChain, IDisposable
    {
        private LiteDatabase db = new LiteDatabase("blocks.file.db");

        public BlockChainLiteDbFileStorage()
        {
            var mapper = BsonMapper.Global;
            mapper.Entity<ECKeyPair>().Ignore(p => p.ECParameters).Ignore(p=>p.PublicKey).Ignore(p => p.D).Ignore(p => p.PrivateKey).Ignore(p => p.Name);
            mapper.Entity<ECSignature>().Ignore(p => p.Signature).Ignore(p => p.SigCompat);
            mapper.Entity<Block>().Field(p => p.Transactions, "t").Field(p => p.Header, "h");
            mapper.Entity<BlockHeader>().Field(p => p.AccountKey, "a").Field(p => p.AvailableProtocol, "ap")
                .Field(p => p.BlockNumber, "bn").Field(p => p.BlockSignature, "bs").Field(p => p.CheckPointHash, "ch")
                .Field(p => p.CompactTarget, "ct").Field(p => p.Fee, "f").Field(p => p.Nonce, "n")
                .Field(p => p.Payload, "p").Field(p => p.ProofOfWork, "pow").Field(p => p.ProtocolVersion, "pv")
                .Field(p => p.Reward, "r").Field(p => p.Timestamp, "ts").Field(p => p.TransactionHash, "th");
            mapper.Entity<ITransaction>().Field(p => p.AccountKey, "ak").Field  (p => p.SignerAccount, "sa")
                .Field(p => p.TargetAccount, "ta")
                .Field(p => p.TransactionType, "t").Field(p => p.Fee, "f").Field(p => p.Payload, "p").Field(p => p.Signature, "s");
            mapper.RegisterType<Currency>(p=>p.value, p=>new Currency(p.AsDecimal));
            mapper.RegisterType<Hash>(p=>(byte[])p, p=>p.AsBinary);
            mapper.RegisterType<ByteString>(p=>(byte[])p, p=>p.AsBinary);
            mapper.RegisterType<Timestamp>(p=>(DateTime)p, p=>p.AsDateTime);
            mapper.RegisterType<AccountNumber>(p => (int)p, p=>new AccountNumber((uint)p.AsInt32));
        }

        public int Count
        {
            get
            {
                return db.FileStorage.FindAll().Count();
            }
        }

        public void AddBlock(Block block)
        {            
            using (var ms = new MemoryStream())
            {
                var name = block.Header.BlockNumber.ToString();
                using (var ls = db.FileStorage.OpenWrite(name, name, null))
                {
                    block.SaveToStream(ms);
                    ms.Position = 0;
                    ms.CopyTo(ls);
                }
            }
        }

        public void Dispose()
        {
            db.Dispose();
        }

        public Block GetBlock(uint blockNumber)
        {
            var b = new Block();
            using (var ls = db.FileStorage.FindById(blockNumber.ToString()).OpenRead())
            {
                using(var ms = new MemoryStream())
                {
                    ls.CopyTo(ms);
                    ms.Position = 0;
                    b.LoadFromStream(ms);
                    return b;
                }
            }            
        }
    }
}
