//-----------------------------------------------------------------------
// This file is part of MicroCoin - The first hungarian cryptocurrency
// Copyright (c) 2019 Peter Nemeth
// ByteString.cs - Copyright (c) 2019 Németh Péter
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
using System.IO;
using System.Linq;
using System.Text;

namespace MicroCoin.Types
{
    public readonly struct ByteString
    {

        private readonly byte[] _value;

        public readonly int Length => _value == null ? 0 : _value.Length;
        public readonly bool IsReadable => _value == null ? true : !_value.Any(p => char.IsControl((char)p));

        public ByteString(byte[] b)
        {
            _value = b;
        }

        public static implicit operator ByteString(string s)
        {
            return new ByteString(s == null ? new byte[0] : Encoding.Default.GetBytes(s));
        }

        public static implicit operator string(ByteString s)
        {
            return s._value == null ? null : Encoding.UTF8.GetString(s._value);
        }

        public static implicit operator byte[](ByteString s)
        {
            return s._value;
        }

        public static implicit operator ByteString(byte[] s)
        {
            return new ByteString(s);
        }

        public static ByteString ReadFromStream(BinaryReader br)
        {
            ushort len = br.ReadUInt16();
            ByteString bs = br.ReadBytes(len);
            if (bs._value == null) return new byte[0];
            return bs;
        }

        public readonly void SaveToStream(BinaryWriter bw)
        {
            _value.SaveToStream(bw);
        }

        public override readonly string ToString()
        {
            return this;
        }

        public readonly void SaveToStream(BinaryWriter bw, bool writeLengths)
        {
            if (writeLengths) _value.SaveToStream(bw);
            else
            {
                if (_value == null) return;
                bw.Write(_value);
            }
        }
    }
}