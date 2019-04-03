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
