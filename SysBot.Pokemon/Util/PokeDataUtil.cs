using System;
using System.Collections.Generic;
using PKHeX.Core;
using System.Linq;

namespace SysBot.Pokemon
{
    public static class PokeDataUtil
    {
        private static readonly Dictionary<Type, string> GameTypeNameMap = new()
        {
            { typeof(PB7), "LGPE" },
            { typeof(PK8), "SWSH" },
            { typeof(PB8), "BDSP" },
            { typeof(PA8), "PLA" },
            { typeof(PK9), "SV" },
        };

        public static string? CollateSpecies(this PKM[] pokes)
        {
            if (pokes == null || pokes.Length < 1)
                return null;
            string toRet = string.Concat(pokes.Select(z => $", {(Species)z.Species}"));
            return toRet.TrimStart(", ");
        }

        public static string? FileExtensionToGame(this Type switchPKMType)
        {
            if (!GameTypeNameMap.ContainsKey(switchPKMType))
                return null;

            return GameTypeNameMap[switchPKMType];
        }
    }
}
