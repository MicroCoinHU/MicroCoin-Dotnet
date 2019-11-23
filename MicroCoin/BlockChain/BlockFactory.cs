//-----------------------------------------------------------------------
// This file is part of MicroCoin - The first hungarian cryptocurrency
// Copyright (c) 2019 Peter Nemeth
// BlockFactory.cs - Copyright (c) 2019 Németh Péter
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
using MicroCoin.Cryptography;
using System.Text;

namespace MicroCoin.BlockChain
{
    public class BlockFactory : IBlockFactory
    {
        private readonly ICryptoService cryptoService;

        public BlockFactory(ICryptoService cryptoService)
        {
            this.cryptoService = cryptoService;
        }

        public Block GenesisBlock()
        {
            return new Block
            {
                Header = new BlockHeader
                {
                    AccountKey = null,
                    AvailableProtocol = 0,
                    BlockNumber = 0,
                    CompactTarget = 0,
                    Fee = 0,
                    Nonce = 0,
                    TransactionHash = new byte[0],
                    Payload = new byte[0],
                    ProofOfWork = new byte[0],
                    ProtocolVersion = 0,
                    Reward = 0,
                    CheckPointHash = cryptoService.Sha256(Encoding.ASCII.GetBytes(Params.Current.GenesisPayload)),
                    BlockSignature = 3,
                    Timestamp = 0
                }
            };
        }
        public Block CreateBlock()
        {
            return new Block();
        }
    }
}
