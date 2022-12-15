using PKHeX.Core;
using SysBot.Base;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using System.Collections.Generic;
using System;

namespace SysBot.Pokemon
{
    public abstract class PokeRoutineExecutor<T> : PokeRoutineExecutorBase where T : PKM, new()
    {
        protected PokeRoutineExecutor(IConsoleBotManaged<IConsoleConnection, IConsoleConnectionAsync> cfg) : base(cfg)
        {
        }

        public abstract Task<T> ReadPokemon(ulong offset, CancellationToken token);
        public abstract Task<T> ReadPokemon(ulong offset, int size, CancellationToken token);
        public abstract Task<T> ReadPokemonPointer(IEnumerable<long> jumps, int size, CancellationToken token);
        public abstract Task<T> ReadBoxPokemon(int box, int slot, CancellationToken token);

        public async Task<T?> ReadUntilPresent(ulong offset, int waitms, int waitInterval, int size, CancellationToken token)
        {
            int msWaited = 0;
            while (msWaited < waitms)
            {
                var pk = await ReadPokemon(offset, size, token).ConfigureAwait(false);
                if (pk.Species != 0 && pk.ChecksumValid)
                    return pk;
                await Task.Delay(waitInterval, token).ConfigureAwait(false);
                msWaited += waitInterval;
            }
            return null;
        }

        public async Task<T?> ReadUntilPresentPointer(IReadOnlyList<long> jumps, int waitms, int waitInterval, int size, CancellationToken token)
        {
            int msWaited = 0;
            while (msWaited < waitms)
            {
                var pk = await ReadPokemonPointer(jumps, size, token).ConfigureAwait(false);
                if (pk.Species != 0)
                {
                    if (pk.ChecksumValid)
                        return pk;
                    else if (pk is PK9 g9)
                    {
                        Log($"bad checksum: {g9.Checksum}= {BitConverter.ToUInt64(pk.DecryptedPartyData, 0):X16}{BitConverter.ToUInt64(pk.DecryptedPartyData, 16):X16}{BitConverter.ToUInt64(pk.DecryptedPartyData, 32):X16}{BitConverter.ToUInt64(pk.DecryptedPartyData, 48):X16}");
                    }
                }
                await Task.Delay(waitInterval, token).ConfigureAwait(false);
                msWaited += waitInterval;
            }

            // try the direct approach
            var pk9 = await SwitchConnection.PointerPeek(size, jumps, token).ConfigureAwait(false);
            if (BitConverter.ToUInt32(pk9, 0) != 0)
            {
                Log("value valid");
                return new PK9(pk9) as T;
            }

            return null;
        }

        protected async Task<(bool, ulong)> ValidatePointerAll(IEnumerable<long> jumps, CancellationToken token)
        {
            var solved = await SwitchConnection.PointerAll(jumps, token).ConfigureAwait(false);
            return (solved != 0, solved);
        }

        public static void DumpPokemon(string folder, string subfolder, T pk)
        {
            if (!Directory.Exists(folder))
                return;
            var dir = Path.Combine(folder, subfolder);
            Directory.CreateDirectory(dir);
            var fn = Path.Combine(dir, Util.CleanFileName(pk.FileName));
            File.WriteAllBytes(fn, pk.DecryptedPartyData);
            LogUtil.LogInfo($"Saved file: {fn}", "Dump");
        }
    }
}