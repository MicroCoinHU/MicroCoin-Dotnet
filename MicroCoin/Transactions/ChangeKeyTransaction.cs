﻿//-----------------------------------------------------------------------
// This file is part of MicroCoin - The first hungarian cryptocurrency
// Copyright (c) 2019 Peter Nemeth
// ChangeKeyTransaction.cs - Copyright (c) 2019 Németh Péter
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
using MicroCoin.Chain;
using MicroCoin.CheckPoints;
using MicroCoin.Cryptography;
using MicroCoin.Types;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace MicroCoin.Transactions
{
    public sealed class ChangeKeyTransaction : Transaction
    {
        public ECKeyPair NewAccountKey { get; set; }

        public ChangeKeyTransaction()
        {
        }

        public ChangeKeyTransaction(Stream s, TransactionType transactionType)
        {
            TransactionType = transactionType;
            LoadFromStream(s);
        }

        public override void SaveToStream(Stream s)
        {
            using (BinaryWriter bw = new BinaryWriter(s, Encoding.ASCII, true))
            {
                bw.Write(SignerAccount);
                if (TransactionType == TransactionType.ChangeKeySigned)
                {
                    bw.Write(TargetAccount);
                }
                bw.Write(TransactionCount);
                bw.Write(Fee);
                Payload.SaveToStream(bw);
                AccountKey.SaveToStream(s, false);
                NewAccountKey.SaveToStream(s);
                Signature.SaveToStream(s);
            }
        }

        public override void LoadFromStream(Stream s)
        {
            using (BinaryReader br = new BinaryReader(s, Encoding.Default, true))
            {
                SignerAccount = br.ReadUInt32();
                switch (TransactionType)
                {
                    case TransactionType.ChangeKey:
                        TargetAccount = SignerAccount;
                        break;
                    case TransactionType.ChangeKeySigned:
                        TargetAccount = br.ReadUInt32();
                        break;
                }

                TransactionCount = br.ReadUInt32();
                Fee = br.ReadUInt64();
                Payload = ByteString.ReadFromStream(br);
                AccountKey = new ECKeyPair();
                AccountKey.LoadFromStream(s, false);
                NewAccountKey = new ECKeyPair();
                NewAccountKey.LoadFromStream(s);
                Signature = new ECSignature(s);
            }
        }

        public override byte[] GetHash()
        {
            using (MemoryStream ms = new MemoryStream(512))
            {
                using (BinaryWriter bw = new BinaryWriter(ms))
                {
                    bw.Write(SignerAccount);
                    if (TargetAccount != SignerAccount)
                    {
                        bw.Write(TargetAccount);
                    }

                    bw.Write(TransactionCount);
                    bw.Write(Fee);
                    if (Payload != "")
                    {
                        Payload.SaveToStream(bw, false);
                    }

                    if (AccountKey?.PublicKey.X != null && AccountKey.PublicKey.X.Length > 0 && AccountKey.PublicKey.Y.Length > 0)
                    {
                        bw.Write((ushort) AccountKey.CurveType);
                        bw.Write(AccountKey.PublicKey.X);
                        bw.Write(AccountKey.PublicKey.Y);
                    }
                    else
                    {
                        bw.Write((ushort) 0);
                    }

                    NewAccountKey.SaveToStream(ms, false);
                    return ms.ToArray();
                }
            }
        }

        public override bool IsValid()
        {
            if (!base.IsValid()) return false;
            if (NewAccountKey.CurveType == ECCurveType.Empty) return false;
            if (NewAccountKey.PublicKey.X.Length == 0) return false;
            if (NewAccountKey.PublicKey.Y.Length == 0) return false;
            return true;
        }

        public override IList<Account> Apply(ICheckPointService checkPointService)
        {
            var account = checkPointService.GetAccount(TargetAccount);
            account.AccountInfo.AccountKey = NewAccountKey;
            account.AccountInfo.State = AccountState.Normal;
            account.AccountInfo.Price = 0;
            account.AccountInfo.AccountToPayPrice = 0;
            account.AccountInfo.NewPublicKey = new ECKeyPair();
            account.AccountInfo.LockedUntilBlock = 0;
            var signer = checkPointService.GetAccount(SignerAccount);
            signer.Balance -= Fee;
            signer.TransactionCount++;
            return new List<Account> { account, signer };
        }

        public override IList<Account> GetModifiedAccounts(ICheckPointService checkPointService)
        {
            var account = checkPointService.GetAccount(TargetAccount);
            var signer = checkPointService.GetAccount(SignerAccount);
            return new List<Account> { account, signer };
        }
    }

}