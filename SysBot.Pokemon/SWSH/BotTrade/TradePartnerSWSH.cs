using System;
using System.Diagnostics;
using PKHeX.Core;

namespace SysBot.Pokemon
{
    public class TradePartnerSWSH
    {
        public uint IDHash { get; }

        public int TID7 { get; }

        public string TID { get; }
        public string TrainerName { get; }

        public ulong NSAID { get; set; }

        public TradePartnerSWSH(byte[] TIDSID, byte[] trainerNameObject)
        {
            Debug.Assert(TIDSID.Length == 4);
            IDHash = BitConverter.ToUInt32(TIDSID, 0);
            TID7 = (int)Math.Abs(IDHash % 1_000_000);
            TID = $"{TID7:000000}";

            TrainerName = StringConverter8.GetString(trainerNameObject);
        }
    }
}
