//-----------------------------------------------------------------------
// This file is part of MicroCoin - The first hungarian cryptocurrency
// Copyright (c) 2019 Peter Nemeth
// Params.cs - Copyright (c) 2019 Németh Péter
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
using System.Collections.Generic;
using System.Net;
using MicroCoin.Cryptography;
using MicroCoin.Types;

namespace MicroCoin
{
    public interface IConfiguration
    {
        int BlockTime { get; }
        string DataFolder { get; }
        string BaseFolder { get; }
        int DifficultyAdjustFrequency { get; set; }
        int DifficultyCalcFrequency { get; set; }
        ICollection<IPEndPoint> FixedSeedServers { get; set; }
        ByteString GenesisPayload { get; set; }
        string LogFolder { get; }
        uint MinimumBlocksToUseAccount { get; set; }
        uint MinimumDifficulty { get; set; }
        uint NetworkPacketMagic { get; set; }
        ushort NetworkProtocolAvailable { get; set; }
        ushort NetworkProtocolVersion { get; set; }
        ECKeyPair NodeKey { get; set; }
        string ProgramVersion { get; }
        ushort ServerPort { get; set; }
    }
}