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
using MicroCoin.Types;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace MicroCoin.Transactions
{
    public sealed class TransferTransaction : Transaction
    {
        public enum TransferType : byte { Transaction, TransactionAndBuyAccount, BuyAccount }
        public Currency AccountPrice { get; set; }
        public AccountNumber SellerAccount { get; set; }
        public ECKeyPair NewAccountKey { get; set; }
        public TransferType TransactionStyle { get; set; }

        public TransferTransaction(Stream stream)
        {
            LoadFromStream(stream);
        }

        public TransferTransaction()
        {

        }

        public override byte[] GetHash()
        {
            var bytes = new List<byte>(200);
            bytes.AddRange(BitConverter.GetBytes(SignerAccount));
            bytes.AddRange(BitConverter.GetBytes(TransactionCount));
            bytes.AddRange(BitConverter.GetBytes(TargetAccount));
            bytes.AddRange(BitConverter.GetBytes(Amount));
            bytes.AddRange(BitConverter.GetBytes(Fee));
            if (Payload.Length > 0)
                bytes.AddRange((byte[])Payload);
            if (AccountKey != null)
            {
                bytes.AddRange(BitConverter.GetBytes((ushort)AccountKey.CurveType));
                if (AccountKey?.PublicKey.X != null && AccountKey.PublicKey.X.Length > 0 && AccountKey.PublicKey.Y.Length > 0)
                {
                    bytes.AddRange(AccountKey.PublicKey.X);
                    bytes.AddRange(AccountKey.PublicKey.Y);
                }
            }
            else
            {
                bytes.AddRange(BitConverter.GetBytes((ushort)ECCurveType.Empty));
            }
            if (TransactionStyle == TransferType.BuyAccount)
            {
                bytes.AddRange(BitConverter.GetBytes(AccountPrice));
                bytes.AddRange(BitConverter.GetBytes(SellerAccount));
                bytes.AddRange(BitConverter.GetBytes((ushort)NewAccountKey.CurveType));
                if (NewAccountKey?.PublicKey.X != null && NewAccountKey.PublicKey.X.Length > 0 && NewAccountKey.PublicKey.Y.Length > 0)
                {
                    bytes.AddRange(NewAccountKey.PublicKey.X);
                    bytes.AddRange(NewAccountKey.PublicKey.Y);
                }
            }
            return bytes.ToArray();
        }

        public override void SaveToStream(Stream s)
        {
            using (BinaryWriter bw = new BinaryWriter(s, Encoding.ASCII, true))
            {
                bw.Write((uint)SignerAccount);
                bw.Write(TransactionCount);
                bw.Write((uint)TargetAccount);
                bw.Write(Amount);
                bw.Write(Fee);
                Payload.SaveToStream(bw);
                AccountKey.SaveToStream(s, false);
                if (TransactionStyle == TransferType.BuyAccount || TransactionStyle == TransferType.TransactionAndBuyAccount)
                {
                    bw.Write((byte)TransactionStyle);
                    bw.Write(AccountPrice);
                    bw.Write((uint)SellerAccount);
                    NewAccountKey.SaveToStream(s, false);
                }
                Signature.SaveToStream(s);
            }
        }
        public override void LoadFromStream(Stream stream)
        {
            using (BinaryReader br = new BinaryReader(stream, Encoding.ASCII, true))
            {
                SignerAccount = br.ReadUInt32();
                TransactionCount = br.ReadUInt32();
                TargetAccount = br.ReadUInt32();
                Amount = br.ReadUInt64();
                Fee = br.ReadUInt64();
                Payload = ByteString.ReadFromStream(br);
                AccountKey = new ECKeyPair();
                AccountKey.LoadFromStream(stream, false);
                byte b = br.ReadByte();
                TransactionStyle = (TransferType)b;
                if (b > 2) { stream.Position -= 1; TransactionStyle = TransferType.Transaction; TransactionType = TransactionType.Transaction; }
                if (b > 0 && b < 3)
                {
                    AccountPrice = br.ReadUInt64();
                    SellerAccount = br.ReadUInt32();
                    NewAccountKey = new ECKeyPair();
                    NewAccountKey.LoadFromStream(stream, false);
                    switch (TransactionStyle)
                    {
                        case TransferType.BuyAccount: TransactionType = TransactionType.BuyAccount; break;
                        case TransferType.TransactionAndBuyAccount: TransactionType = TransactionType.BuyAccount; break;
                        default: TransactionType = TransactionType.Transaction; break;
                    }
                }
                Signature = new ECSignature(stream);
            }
        }

        public override bool IsValid()
        {
            if (!base.IsValid()) return false;
            if (TransactionStyle != TransferType.BuyAccount) return true;
            if (AccountPrice < 0) return false;
            return true;
        }

        public override IList<Account> Apply(ICheckPointService checkPointService)
        {
            var sender = checkPointService.GetAccount(SignerAccount);
            var target = checkPointService.GetAccount(TargetAccount);
            var seller = checkPointService.GetAccount(SellerAccount);
            if (TransactionStyle == TransferType.Transaction)
            {
                sender.Balance -= Amount;
                sender.Balance -= Fee;
                target.Balance += Amount;
                sender.TransactionCount++;
                return new List<Account> { sender, target };
            }
            if(TransactionStyle == TransferType.BuyAccount || TransactionStyle == TransferType.TransactionAndBuyAccount)
            {
                seller.Balance += target.AccountInfo.Price;
                sender.Balance -= (Amount + Fee);
                target.Balance += (Amount - target.AccountInfo.Price);
                target.AccountInfo.AccountKey = NewAccountKey;
                target.AccountInfo.State = AccountState.Normal;
                target.AccountInfo.LockedUntilBlock = 0;
                target.AccountInfo.AccountToPayPrice = 0;
                target.AccountInfo.NewPublicKey = new ECKeyPair();
                target.AccountInfo.Price = 0;
                sender.TransactionCount++;
                if (Block != 0)
                {
                    target.UpdatedBlock = Block;
                    seller.UpdatedBlock = Block;
                }
                return new List<Account>() { sender, seller, target };
            }
            return new List<Account>();
        }
    }

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
            //if (senderAccount.AccountInfo.State != AccountState.Normal) return false;
            if (senderAccount.AccountInfo.LockedUntilBlock > blockChain.BlockHeight) return false;
            if (!cryptoService.ValidateSignature(transaction.GetHash(), transaction.Signature, senderAccount.AccountInfo.AccountKey))
            {
                return false;
            }
            if (transaction.TransactionStyle == TransferTransaction.TransferType.Transaction)
            {
                if (transaction.TargetAccount.Equals(transaction.SignerAccount)) return false;
                if (senderAccount.Balance < (transaction.Amount + transaction.Fee)) return false;
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