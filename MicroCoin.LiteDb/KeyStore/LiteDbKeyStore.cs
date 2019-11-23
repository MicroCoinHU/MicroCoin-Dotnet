//-----------------------------------------------------------------------
// This file is part of MicroCoin - The first hungarian cryptocurrency
// Copyright (c) 2019 Peter Nemeth
// LiteDbKeyStore.cs - Copyright (c) 2019 Németh Péter
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
using System.Collections.Generic;
using System.IO;
using LiteDB;
using MicroCoin.Cryptography;

namespace MicroCoin.KeyStore
{
    public class LiteDbKeyStore : IKeyStore, IDisposable
    {
        private readonly LiteDatabase keysDb;

        public LiteDbKeyStore()
        {
            keysDb = new LiteDatabase("Filename=" + Path.Combine(Params.Current.BaseFolder, "keys.mcc") + "; Journal=false; Async=true");
            BsonMapper.Global.Entity<KeyPair>().Id(p => p.Id)
                .Ignore(p=>p.PublicKey)
                ;
        }

        public void Add(KeyPair keyPair)
        {
            KeyPair kp = new KeyPair
            {
                D = keyPair.D,
                PublicKey = keyPair.PublicKey,
                CurveType = keyPair.CurveType
            };
            keysDb.GetCollection<KeyPair>().Upsert(kp);
        }

        public IEnumerable<KeyPair> All()
        {
            return keysDb.GetCollection<KeyPair>().FindAll();
        }

        public int Count()
        {
            return keysDb.GetCollection<KeyPair>().Count();
        }

        public void Dispose()
        {
            keysDb.Dispose();
        }

        public void Remove(KeyPair keyPair)
        {
            keysDb.GetCollection<KeyPair>().Delete(p => p.Id == keyPair.Id);
        }
    }
}
