//-----------------------------------------------------------------------
// This file is part of MicroCoin - The first hungarian cryptocurrency
// Copyright (c) 2019 Peter Nemeth
// CheckPointResponse.cs - Copyright (c) 2019 %UserDisplayName%
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
using ComponentAce.Compression.Libs.zlib;
using MicroCoin.CheckPoints;
using MicroCoin.Common;
using MicroCoin.Types;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace MicroCoin.Protocol
{
    public class CheckPointResponse : IStreamSerializable, INetworkPayload
    {
        public ByteString Magic { get; set; }
        public ushort Protocol { get; set; }
        public uint BlockCount { get; set; }
        public uint StartBlock { get; set; }
        public uint EndBlock { get; set; }
        public Hash Hash { get; set; }
        public long HeaderEnd { get; set; }
        public uint[] Offsets { get; private set; }
        public ByteString CheckPointResponseMagic { get; set; }
        public ushort Version { get; set; }
        protected uint UncompressedSize { get; set; }
        protected uint CompressedSize { get; set; }
        public ICollection<CheckPointBlock> CheckPoints { get; set; } = new List<CheckPointBlock>();

        public NetOperationType NetOperation => NetOperationType.CheckPoint;
        public RequestType RequestType => RequestType.Response;

        private static void DecompressData(byte[] inData, out byte[] outData)
        {
            using (MemoryStream outMemoryStream = new MemoryStream())
            {
                using (ZOutputStream outZStream = new ZOutputStream(outMemoryStream))
                {
                    using (Stream inMemoryStream = new MemoryStream(inData))
                    {
                        CopyStream(inMemoryStream, outZStream);
                        outZStream.finish();
                        outData = outMemoryStream.ToArray();
                    }
                }
            }
        }

        private static void CompressData(byte[] inData, out byte[] outData)
        {
            using (MemoryStream outMemoryStream = new MemoryStream())
            using (ZOutputStream outZStream = new ZOutputStream(outMemoryStream, zlibConst.Z_DEFAULT_COMPRESSION))
            using (Stream inMemoryStream = new MemoryStream(inData))
            {
                CopyStream(inMemoryStream, outZStream);
                outZStream.finish();
                outData = outMemoryStream.ToArray();
            }
        }

        private static void CopyStream(Stream input, Stream output)
        {
            byte[] buffer = new byte[2000];
            int len;
            while ((len = input.Read(buffer, 0, 2000)) > 0)
            {
                output.Write(buffer, 0, len);
            }
            output.Flush();
        }

        public void LoadFromStream(Stream stream)
        {
            using (BinaryReader br = new BinaryReader(stream, Encoding.Default, true))
            {
                CheckPointResponseMagic = ByteString.ReadFromStream(br);
                Version = br.ReadUInt16();
                UncompressedSize = br.ReadUInt32();
                CompressedSize = br.ReadUInt32();
                DecompressData(br.ReadBytes((int)CompressedSize), out byte[] decompressed);
                using (var unCompressed = new MemoryStream(decompressed))
                {
                    using (var br2 = new BinaryReader(unCompressed, Encoding.ASCII, true))
                    {
                        unCompressed.Position = unCompressed.Length - 34;
                        Hash = ByteString.ReadFromStream(br2);
                        unCompressed.Position = 0;
                        ushort len = br2.ReadUInt16();
                        Magic = br2.ReadBytes(len);
                        Protocol = br2.ReadUInt16();
                        Version = br2.ReadUInt16();
                        BlockCount = br2.ReadUInt32();
                        StartBlock = br2.ReadUInt32();
                        EndBlock = br2.ReadUInt32();
                        long pos = unCompressed.Position;
                        HeaderEnd = pos;
                        Offsets = new uint[(EndBlock - StartBlock + 1)];
                        //var checkPoints = new List<CheckPointBlock>();
                        for (int i = 0; i < Offsets.Length; i++)
                        {
                            Offsets[i] = (uint)(br2.ReadUInt32());
                            var cb = new CheckPointBlock();
                            var p = unCompressed.Position;
                            unCompressed.Position = Offsets[i] + HeaderEnd;
                            cb.LoadFromStream(unCompressed);
                            CheckPoints.Add(cb);
                            if (i % 10000 == 0)
                            {
                                //ServiceLocator.GetService<ICheckPointStorage>().AddBlocks(list);
                                //list.Clear();
                            }
                            //Console.WriteLine("Adding block {0}", cb.Header.BlockNumber);
                            unCompressed.Position = p;
                        }
                        //if (list.Count > 0)
                        //{
                        //    ServiceLocator.GetService<ICheckPointStorage>().AddBlocks(list);
                        //}
                    }
                }
            }
        }

        public void SaveToStream(Stream stream)
        {
            throw new NotImplementedException();
        }
    }
}
