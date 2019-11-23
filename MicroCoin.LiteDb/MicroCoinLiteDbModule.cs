//-----------------------------------------------------------------------
// This file is part of MicroCoin - The first hungarian cryptocurrency
// Copyright (c) 2019 Peter Nemeth
// MicroCoinLiteDbModule.cs - Copyright (c) 2019 Németh Péter
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
using System;
using LiteDB;
using MicroCoin.BlockChain;
using MicroCoin.Chain;
using MicroCoin.CheckPoints;
using MicroCoin.Cryptography;
using MicroCoin.KeyStore;
using MicroCoin.LiteDb.CheckPoints;
using MicroCoin.Modularization;
using MicroCoin.Transactions;
using MicroCoin.Types;
using Microsoft.Extensions.DependencyInjection;

namespace MicroCoin.LiteDb
{
    public class MicroCoinLiteDbModule : IModule
    {
        public string Name => "MicroCoin LiteDb Storage engine";

        private void InitializeDb()
        {
            var mapper = BsonMapper.Global;
            mapper.ResolveMember = (type, memberInfo, memberMapper) =>
            {
                if (memberMapper.DataType == typeof(ECCurveType))
                {
                    memberMapper.Serialize = (obj, m) => new BsonValue((int)((ushort)obj));
                    memberMapper.Deserialize = (value, m) => (ECCurveType)value.AsInt32;
                }
                else if (memberMapper.DataType == typeof(AccountState))
                {
                    memberMapper.Serialize = (obj, m) => new BsonValue((int)obj);
                    memberMapper.Deserialize = (value, m) => (AccountState)value.AsInt32;
                }
                else if (memberMapper.DataType == typeof(TransactionType))
                {
                    memberMapper.Serialize = (obj, m) => new BsonValue((int)(uint)obj);
                    memberMapper.Deserialize = (value, m) => (TransactionType)value.AsInt32;
                }
                else if (memberMapper.DataType == typeof(TransferTransaction.TransferType))
                {
                    memberMapper.Serialize = (obj, m) => new BsonValue((int)(byte)obj);
                    memberMapper.Deserialize = (value, m) => (TransferTransaction.TransferType)value.AsInt32;
                }
            };

            mapper.RegisterType<Currency>(p => (long)(ulong)p, p => (Currency)p.AsInt64);
            mapper.RegisterType<Hash>(p => (byte[])p, p => p.AsBinary);
            mapper.RegisterType<ByteString>(p => (byte[])p, p => p.AsBinary);
            mapper.RegisterType<Timestamp>(p => (int)p, p => (Timestamp)p.AsInt32);
            mapper.RegisterType<AccountNumber>(p => (int)p, p => new AccountNumber((uint)p.AsInt32));

            mapper.Entity<ECKeyPair>()
                .Field(p => p.CurveType, "ct")
                .Id(p => p.Id)
                .Ignore(p => p.ECParameters)
                .Ignore(p => p.PublicKey)
                .Ignore(p => p.D)
                .Ignore(p => p.PrivateKey)
                .Ignore(p => p.Name);

            mapper.Entity<ECSignature>()
                .Ignore(p => p.Signature)
                .Ignore(p => p.SigCompat);

            mapper.Entity<BlockHeader>()
                .Field(p => p.AccountKey, "a")
                .Field(p => p.AvailableProtocol, "b")
                .Field(p => p.BlockNumber, "c")
                .Field(p => p.BlockSignature, "d")
                .Field(p => p.CheckPointHash, "e")
                .Field(p => p.CompactTarget, "f")
                .Field(p => p.Fee, "g")
                .Field(p => p.Nonce, "h")
                .Field(p => p.Payload, "i")
                .Field(p => p.ProofOfWork, "j")
                .Field(p => p.ProtocolVersion, "k")
                .Field(p => p.Reward, "l")
                .Field(p => p.Timestamp, "m")
                .Field(p => p.TransactionHash, "n");
        }

        public void RegisterModule(ServiceCollection serviceCollection)
        {
            InitializeDb();
            serviceCollection
                .AddSingleton<IKeyStore, LiteDbKeyStore>()
                .AddSingleton<ICheckPointStorage, CheckPointLiteDbStorage>()
                .AddSingleton<IAccountStorage, AccountLiteDbStorage>()
                .AddSingleton<IBlockChainStorage, BlockChainLiteDbStorage>();
        }

        public void InitModule(IServiceProvider serviceProvider)
        {
            var keyStore = serviceProvider.GetService<IKeyStore>();
            if (keyStore.Count() == 0)
            {
                keyStore.Add(ECKeyPair.CreateNew());
            }
        }
    }
}
