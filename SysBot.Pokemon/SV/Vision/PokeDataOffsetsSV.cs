using System;
using System.Collections.Generic;
using System.Text;

namespace SysBot.Pokemon
{
    public static class PokeDataOffsetsSV
    {
        public const string ScarletID = "0100A3D008C5C000";
        public const string VioletID = "01008F6008C5E000";

        public static IReadOnlyList<long> BoxStartPokemonPointer { get; } = new long[] { 0x4763BB8, 0x1A0, 0x30, 0x9D0, 0x00 };
        public static IReadOnlyList<long> MyStatusPointer { get; } = new long[] { 0x4741FA0, 0x198, 0x00, 0x40 };

        public static IReadOnlyList<long> LinkTradePartnerPokemonPointer { get; } = new long[] { 0x473A110, 0x48, 0x58, 0x40, 0x148 };
        public static IReadOnlyList<long> LinkTradePartnerNameSlot1Pointer { get; } = new long[] { 0x473A110, 0x48, 0xB0, 0x00 };
        public static IReadOnlyList<long> LinkTradePartnerNameSlot2Pointer { get; } = new long[] { 0x473A110, 0x48, 0xE0, 0x00 };
        public static IReadOnlyList<long> LinkTradePartnerNIDPointer { get; } = new long[] { 0x475EA28, 0xF8, 0x08 };

        public static IReadOnlyList<long> OverworldPointer { get; } = new long[] { 0x47AFA18, 0x00, 0x388, 0x3C0, 0x00, 0x71C };
        public static IReadOnlyList<long> IsSearchingPointer { get; } = new long[] { 0x4763C00, 0x58 }; // 0 no search, 1 search, 2 unknown (still searching) 
        public static IReadOnlyList<long> ConnectionPointer { get; } = new long[] { 0x4763E08, 0x10 }; // 0 not connected, 1 connected, 2 adhoc 

        public static IReadOnlyList<long> KeyboardBufferPointer { get; } = new long[] { 0x473B3A8, 0x30, 0x00 };

        public static IReadOnlyList<long> PokePortalPointer = new long[] { 0x47AFA18, 0x00, 0x3C0, 0x3C0, 0x598 };

        public const int BoxFormatSlotSize = 0x158;
        public const int TradeFormatSlotSize = 0x148;
    }
}
