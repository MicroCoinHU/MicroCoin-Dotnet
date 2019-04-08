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
using System;
using System.Collections.Generic;
using System.Text;

namespace MicroCoin
{
    public static class Params
    {
        public static ushort ServerPort { get; set; } = 4004;
        public static string GenesisPayload { get; set; } = "(c) Peter Nemeth - Okes rendben okes";
        public static uint NetworkPacketMagic { get; internal set; } = 0x0A043580;
        public static ushort NetworkProtocolVersion { get; set; } = 6;
        public static ushort NetworkProtocolAvailable { get; set; } = 6;
    }
}
