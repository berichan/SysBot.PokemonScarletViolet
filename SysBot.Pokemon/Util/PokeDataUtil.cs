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

        public static string? FileExtensionToGame(this Type switchPKMType)
        {
            if (!GameTypeNameMap.ContainsKey(switchPKMType))
                return null;

            return GameTypeNameMap[switchPKMType];
        }
    }
}
