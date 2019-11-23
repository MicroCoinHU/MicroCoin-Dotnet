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
    public readonly struct Currency
    {
        public readonly decimal Value { get; }

        public Currency(in decimal value)
        {
            Value = value;
        }

        public static implicit operator decimal(in Currency m)
        {
            return m.Value;
        }

        public static implicit operator ulong(in Currency m)
        {
            return (ulong)(m.Value * 10000M);
        }

        public static implicit operator Currency(in ulong m)
        {
            return new Currency( m / 10000M );
        }

        public static Currency operator +(in Currency a, in Currency b)
        {
            return new Currency(a.Value + b.Value);
        }

        public static Currency operator -(in Currency a, in Currency b)
        {
            return new Currency(a.Value - b.Value);
        }

        public static Currency operator -(in Currency a, in ulong b)
        {
            return new Currency(a.Value - b);
        }

        public static Currency operator +(in Currency a, in ulong b)
        {
            return new Currency(a.Value + b);
        }

        public readonly override bool Equals(object obj)
        {
            return ((ulong)this).Equals(obj);
        }

        public readonly override string ToString()
        {
            return Value.ToString("G");
        }

        public readonly string ToString(string format)
        {
            return Value.ToString(format);
        }

        public readonly override int GetHashCode()
        {
            return Value.GetHashCode();
        }

        public static explicit operator Currency(in long v)
        {
            return (ulong)v; 
        }
    }
}