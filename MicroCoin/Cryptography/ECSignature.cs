//-----------------------------------------------------------------------
// This file is part of MicroCoin - The first hungarian cryptocurrency
// Copyright (c) 2019 Peter Nemeth
// ECSignature.cs - Copyright (c) 2019 Németh Péter
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

namespace MicroCoin.Cryptography
{
    public struct ECSignature : IECSignature
    {
        public byte[] R { get; set; }
        public byte[] S { get; set; }
        public byte[] SigCompat { get; set; }
        public readonly byte[] Signature
        {
            get
            {
                var ret = R.ToList();
                ret.AddRange(S);
                return ret.ToArray();
            }
        }

        public ECSignature(byte[] R, byte[] S, byte[] SigCompat)
        {
            this.R = R;
            this.S = S;
            this.SigCompat = SigCompat;
        }
        public ECSignature(Stream stream)
        {
            SigCompat = new byte[0];
            using (var br = new BinaryReader(stream, Encoding.ASCII, true))
            {
                var len = br.ReadUInt16();
                int offset = 0;
                R = new byte[len];
                br.Read(R, offset, len);
                len = br.ReadUInt16();
                S = new byte[len];
                br.Read(S, offset, len);
            }
        }

        public readonly void SaveToStream(Stream stream)
        {
            using (var bw = new BinaryWriter(stream, Encoding.ASCII, true))
            {
                ushort rlen = (ushort)R.Length;
                ushort slen = (ushort)S.Length;
                bw.Write(rlen);
                bw.Write(R, 0, rlen);
                bw.Write(slen);
                bw.Write(S, 0, slen);
            }
        }
    }
}