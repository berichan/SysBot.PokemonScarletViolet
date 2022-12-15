using System;
using System.Diagnostics;
using System.Text;
using PKHeX.Core;

namespace SysBot.Pokemon
{
    public class TrainerIDBlock
    {
        public uint IDHash { get; }

        public int TID7 { get; }
        public int SID7 { get; }

        public string TID { get; }
        public string SID { get; }
        public string TrainerName { get; set; }

        public byte Game { get; }
        public byte Language { get; }
        public byte Gender { get; }

        public ulong NSAID { get; set; }

        public TrainerIDBlock()
        {
            TID = string.Empty;
            SID = string.Empty;
            TrainerName = string.Empty;
        }

        public TrainerIDBlock(byte[] TIDSID, byte[] idbytes, byte[] trainerNameObject)
        {
            Debug.Assert(TIDSID.Length == 4);
            IDHash = BitConverter.ToUInt32(TIDSID, 0);
            TID7 = (int)Math.Abs(IDHash % 1_000_000);
            SID7 = (int)Math.Abs(IDHash / 1_000_000);
            TID = $"{TID7:000000}";
            SID = $"{SID7:0000}";

            Game = idbytes[0];
            Gender = idbytes[1];
            Language = idbytes[3];

            TrainerName = Encoding.Unicode.GetString(trainerNameObject).TrimEnd('\0');
        }
    }
}
