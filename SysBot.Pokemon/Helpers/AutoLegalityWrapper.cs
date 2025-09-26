﻿using System;
using PKHeX.Core;
using PKHeX.Core.AutoMod;
using System.IO;
using System.Threading;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace SysBot.Pokemon
{
    public static class AutoLegalityWrapper
    {
        private static bool Initialized;

        public static void EnsureInitialized(LegalitySettings cfg)
        {
            if (Initialized)
                return;
            Initialized = true;
            InitializeAutoLegality(cfg);
        }

        private static void InitializeAutoLegality(LegalitySettings cfg)
        {
            EncounterEvent.RefreshMGDB(cfg.MGDBPath);
            InitializeTrainerDatabase(cfg);
            InitializeSettings(cfg);
        }

        private static void InitializeSettings(LegalitySettings cfg)
        {
            APILegality.SetAllLegalRibbons = cfg.SetAllLegalRibbons;
            APILegality.SetMatchingBalls = cfg.SetMatchingBalls;
            APILegality.ForceSpecifiedBall = cfg.ForceSpecifiedBall;
            APILegality.ForceLevel100for50 = cfg.ForceLevel100for50;
            Legalizer.EnableEasterEggs = cfg.EnableEasterEggs;
            APILegality.AllowTrainerOverride = cfg.AllowTrainerDataOverride;
            APILegality.AllowBatchCommands = cfg.AllowBatchCommands;
            APILegality.Timeout = cfg.Timeout;
        }

        private static void InitializeTrainerDatabase(LegalitySettings cfg)
        {
            var externalSource = cfg.GeneratePathTrainerInfo;
            if (Directory.Exists(externalSource))
                TrainerSettings.LoadTrainerDatabaseFromPath(externalSource);

            // Seed the Trainer Database with enough fake save files so that we return a generation sensitive format when needed.
            var fallback = GetDefaultTrainer(cfg);
            for (byte generation = 1; generation <= Latest.Generation; generation++)
            {
                var versions = GameUtil.GetVersionsInGeneration(generation, Latest.Version);
                foreach (var version in versions)
                    RegisterIfNoneExist(fallback, generation, version);
            }
        }

        private static SimpleTrainerInfo GetDefaultTrainer(LegalitySettings cfg)
        {
            var OT = cfg.GenerateOT;
            if (OT.Length == 0)
                OT = "Blank"; // Will fail if actually left blank.
            var fallback = new SimpleTrainerInfo(GameVersion.Any)
            {
                Language = (byte)cfg.GenerateLanguage,
                TID16 = cfg.GenerateTID16,
                SID16 = cfg.GenerateSID16,
                OT = OT,
                Generation = 0,
            };
            return fallback;
        }

        private static void RegisterIfNoneExist(SimpleTrainerInfo fallback, byte generation, GameVersion version)
        {
            fallback = new SimpleTrainerInfo(version)
            {
                Language = fallback.Language,
                TID16 = fallback.TID16,
                SID16 = fallback.SID16,
                OT = fallback.OT,
                Generation = generation,
            };
            var exist = TrainerSettings.GetSavedTrainerData(generation, version, fallback);
            if (exist is SimpleTrainerInfo) // not anything from files; this assumes ALM returns SimpleTrainerInfo for non-user-provided fake templates.
                TrainerSettings.Register(fallback);
        }

        public static bool CanBeTraded(this PKM pk)
        {
            if (pk.IsNicknamed)
            {
                Span<char> nick = stackalloc char[pk.TrashCharCountNickname];
                int len = pk.LoadString(pk.NicknameTrash, nick);
                nick = nick[..len];
                if (StringsUtil.IsSpammyString(nick))
                    return false;
            }
            {
                Span<char> ot = stackalloc char[pk.TrashCharCountTrainer];
                int len = pk.LoadString(pk.OriginalTrainerTrash, ot);
                ot = ot[..len];
                if (StringsUtil.IsSpammyString(ot) && !IsFixedOT(new LegalityAnalysis(pk).EncounterOriginal, pk))
                    return false;
            }
            return !FormInfo.IsFusedForm(pk.Species, pk.Form, pk.Format);
        }

        public static bool IsFixedOT(IEncounterTemplate t, PKM pkm) => t switch
        {
            IFixedTrainer { IsFixedTrainer: true } => true,
            MysteryGift g => !g.IsEgg && g switch
            {
                WC9 wc9 => wc9.GetHasOT(pkm.Language),
                WA8 wa8 => wa8.GetHasOT(pkm.Language),
                WB8 wb8 => wb8.GetHasOT(pkm.Language),
                WC8 wc8 => wc8.GetHasOT(pkm.Language),
                WB7 wb7 => wb7.GetHasOT(pkm.Language),
                { Generation: >= 5 } gift => gift.OriginalTrainerName.Length > 0,
                _ => true,
            },
            _ => false,
        };

        public static ITrainerInfo GetTrainerInfo<T>() where T : PKM, new()
        {
            if (typeof(T) == typeof(PK8))
                return TrainerSettings.GetSavedTrainerData(GameVersion.SWSH);
            if (typeof(T) == typeof(PB8))
                return TrainerSettings.GetSavedTrainerData(GameVersion.BDSP);
            if (typeof(T) == typeof(PA8))
                return TrainerSettings.GetSavedTrainerData(GameVersion.PLA);
            if (typeof(T) == typeof(PK9))
                return TrainerSettings.GetSavedTrainerData(GameVersion.SV);

            throw new ArgumentException("Type does not have a recognized trainer fetch.", typeof(T).Name);
        }

        public static ITrainerInfo GetTrainerInfo(byte gen) => TrainerSettings.GetSavedTrainerData(gen);

        public static PKM GetLegal(this ITrainerInfo sav, IBattleTemplate set, out string res)
        {
            var result = sav.GetLegalFromSet(set);
            res = result.Status switch
            {
                LegalizationResult.Regenerated => "Regenerated",
                LegalizationResult.Failed => "Failed",
                LegalizationResult.Timeout => "Timeout",
                LegalizationResult.VersionMismatch => "VersionMismatch",
                _ => "",
            };
            return result.Created;
        }

        public static string GetLegalizationHint(IBattleTemplate set, ITrainerInfo sav, PKM pk) => set.SetAnalysis(sav, pk);
        public static PKM LegalizePokemon(this PKM pk) => pk.Legalize();
        public static IBattleTemplate GetTemplate(ShowdownSet set) => new RegenTemplate(set);
    }
}
