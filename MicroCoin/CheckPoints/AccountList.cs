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
using MicroCoin.Chain;
using System;
using System.Collections.Generic;
using System.Collections;
using System.Collections.Specialized;

namespace MicroCoin.CheckPoints
{
    public class AccountList : IList, IEnumerable<Account>, IEnumerable, IReadOnlyCollection<Account>, IReadOnlyList<Account>
    {
        public class AccountEnumerator : IEnumerator<Account>, IEnumerator
        {
            private int index = -1;
            private readonly AccountList accounts;
            private readonly int count = 0;
            public AccountEnumerator(AccountList accounts)
            {
                this.accounts = accounts;
                count = 0;
            }

            public Account Current => accounts[index];
            object IEnumerator.Current => accounts[index];

            public void Dispose()
            {
            }

            public bool MoveNext()
            {
                index++;
                return index < count;
            }

            public void Reset()
            {
                index = -1;
            }
        }

        private readonly ICheckPointService checkPointService;
        private readonly AccountEnumerator accountEnumerator;

        public event NotifyCollectionChangedEventHandler CollectionChanged;

        public AccountList(ICheckPointService checkPointService)
        {
            this.checkPointService = checkPointService;
            accountEnumerator = new AccountEnumerator(this);
        }
        public Account this[int index] => checkPointService.GetAccount((uint)index, true);

        public int Count
        {
            get
            {
                return checkPointService.GetAccountCount();
            }
        }

        public bool IsFixedSize => true;

        public bool IsReadOnly => true;

        public bool IsSynchronized => false;

        public object SyncRoot { get; } = new object();
        object IList.this[int index] { 
            get => checkPointService.GetAccount((uint)index, true);
            set => throw new NotImplementedException(); 
        }

        public IEnumerator<Account> GetEnumerator()
        {
            return accountEnumerator;
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return accountEnumerator;
        }
        public bool Contains(Account item)
        {
            return item.AccountNumber < Count;
        }

        public int IndexOf(Account item)
        {
            return item.AccountNumber;
        }

        public int Add(object value)
        {
            throw new NotImplementedException();
        }

        public void Clear()
        {
            throw new NotImplementedException();
        }

        public bool Contains(object value)
        {
            return ((Account)value).AccountNumber < Count;
        }

        public int IndexOf(object value)
        {
            return ((Account)value).AccountNumber;
        }

        public void Insert(int index, object value)
        {
            throw new NotImplementedException();
        }

        public void Remove(object value)
        {
            throw new NotImplementedException();
        }

        public void RemoveAt(int index)
        {
            throw new NotImplementedException();
        }

        public void CopyTo(Array array, int index)
        {
            for (int i= 0;i< Count;i++)
            {
                array.SetValue(this[i], i + index);                
            }
        }
    }
}