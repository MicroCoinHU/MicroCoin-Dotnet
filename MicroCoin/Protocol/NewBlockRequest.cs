//-----------------------------------------------------------------------
// This file is part of MicroCoin - The first hungarian cryptocurrency
// Copyright (c) 2019 Peter Nemeth
// NewBlockRequest.cs - Copyright (c) 2019 Németh Péter
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
using MicroCoin.Common;
using System.IO;

namespace MicroCoin.Protocol
{
    public class NewBlockRequest : IStreamSerializable, INetworkPayload
    {
        public Block Block { get; set; }
        public NetOperationType NetOperation => NetOperationType.NewBlock;
        public RequestType RequestType => RequestType.AutoSend;

        public NewBlockRequest()
        {
        }

        public NewBlockRequest(Block block) : base()
        {
            Block = block;
        }

        public void SaveToStream(Stream stream)
        {
            Block.SaveToStream(stream);
        }

        public void LoadFromStream(Stream stream)
        {
            Block = new Block();
            Block.LoadFromStream(stream);
        }
    }
}
