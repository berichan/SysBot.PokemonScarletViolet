using System;
using System.Collections.Generic;
using System.Text;

namespace SysBot.Pokemon
{
    public static class PokeDataOffsetsSV
    {
        public const string ScarletID = "0100A3D008C5C000";
        public const string VioletID = "01008F6008C5E000";

        public static IReadOnlyList<long> BoxStartPokemonPointer { get; } = new long[] { 0x44E4FE8, 0x110, 0x9B0, 0x00 };
        public static IReadOnlyList<long> MyStatusPointer { get; } = new long[] { 0x44C1C18, 0x100, 0x40 };

        public static IReadOnlyList<long> LinkTradePartnerPokemonPointer { get; } = new long[] { 0x44E50D0, 0x120, 0x1C8, 0xB8, 0x10, 0x30, 0x00 };
        public static IReadOnlyList<long> LinkTradePartnerNameSlot1Pointer { get; } = new long[] { 0x44E5130, 0x28, 0xB0, 0x00 };
        public static IReadOnlyList<long> LinkTradePartnerNameSlot2Pointer { get; } = new long[] { 0x44E5130, 0x28, 0xE0, 0x00 };
        public static IReadOnlyList<long> LinkTradePartnerNIDPointer { get; } = new long[] { 0x44DFCA8, 0xF8, 0x08 };

        public static IReadOnlyList<long> OverworldPointer { get; } = new long[] { 0x4530D58, 0x00, 0x388, 0x3C0, 0x00, 0x1A1C };
        public static IReadOnlyList<long> IsSearchingPointer { get; } = new long[] { 0x44E4F38, 0x58 }; // 0 no search, 1 search, 2 unknown (still searching)
        public static IReadOnlyList<long> ConnectionPointer { get; } = new long[] { 0x44E5140, 0x10 }; // 0 not connected, 1 connected, 2 adhoc

        public static IReadOnlyList<long> KeyboardBufferPointer { get; } = new long[] { 0x44BC9D8, 0x30, 0x00 };

        public static IReadOnlyList<long> PokePortalPointer = new long[] { 0x4530D58, 0x00, 0x3C0, 0x3C0, 0x598 };

        public const int BoxFormatSlotSize = 0x158;
        public const int TradeFormatSlotSize = 0x148;
    }
}
