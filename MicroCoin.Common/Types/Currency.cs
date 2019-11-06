//-----------------------------------------------------------------------
// This file is part of MicroCoin - The first hungarian cryptocurrency
// Copyright (c) 2019 Peter Nemeth
// Currency.cs - Copyright (c) 2019 Németh Péter
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

namespace MicroCoin.Types
{
    public struct Currency
    {
        public decimal value { get; }

        public Currency(decimal value)
        {
            this.value = value;
        }

        public static implicit operator decimal(Currency m)
        {
            return m.value;
        }

        public static implicit operator ulong(Currency m)
        {
            return (ulong)(m.value*10000M);
        }


        public static implicit operator Currency(ulong m)
        {
            return new Currency( m / 10000M );
        }

        public static Currency operator +(Currency a, Currency b)
        {
            return new Currency(a.value + b.value);
        }

        public static Currency operator -(Currency a, Currency b)
        {
            return new Currency(a.value - b.value);
        }

        public static Currency operator -(Currency a, ulong b)
        {
            return new Currency(a.value - b);
        }

        public static Currency operator +(Currency a, ulong b)
        {
            return new Currency(a.value + b);
        }

        public override bool Equals(object obj)
        {
            return ((ulong)this).Equals(obj);
        }

        public override string ToString()
        {
            return value.ToString("G");
        }

        public string ToString(string format)
        {
            return value.ToString(format);
        }

        public override int GetHashCode()
        {
            return value.GetHashCode();
        }

        public static explicit operator Currency(long v)
        {
            return (ulong)v;            
        }
    }
}