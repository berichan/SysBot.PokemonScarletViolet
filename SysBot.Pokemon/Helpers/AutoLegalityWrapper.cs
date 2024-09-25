using System;
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
            InitializeCoreStrings();
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
            APILegality.AllowHOMETransferGeneration = false; // Don't allow home transfer generation for SV 
        }

        private static void InitializeTrainerDatabase(LegalitySettings cfg)
        {
            var externalSource = cfg.GeneratePathTrainerInfo;
            if (Directory.Exists(externalSource))
                TrainerSettings.LoadTrainerDatabaseFromPath(externalSource);

            // Seed the Trainer Database with enough fake save files so that we return a generation sensitive format when needed.
            var fallback = GetDefaultTrainer(cfg);
            for (byte generation = 1; generation <= PKX.Generation; generation++)
            {
                var versions = GameUtil.GetVersionsInGeneration(generation, PKX.Version);
                foreach (var version in versions)
                    RegisterIfNoneExist(fallback, generation, version);
            }
            // Manually register for LGP/E since Gen7 above will only register the 3DS versions.
            RegisterIfNoneExist(fallback, 7, GameVersion.GP);
            RegisterIfNoneExist(fallback, 7, GameVersion.GE);
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
            var exist = TrainerSettings.GetSavedTrainerData(version, generation, fallback);
            if (exist is SimpleTrainerInfo) // not anything from files; this assumes ALM returns SimpleTrainerInfo for non-user-provided fake templates.
                TrainerSettings.Register(fallback);
        }

        private static void InitializeCoreStrings()
        {
            var lang = Thread.CurrentThread.CurrentCulture.TwoLetterISOLanguageName[..2];
            LocalizationUtil.SetLocalization(typeof(LegalityCheckStrings), lang);
            LocalizationUtil.SetLocalization(typeof(MessageStrings), lang);
            RibbonStrings.ResetDictionary(GameInfo.Strings.ribbons);
            ParseSettings.ChangeLocalizationStrings(GameInfo.Strings.movelist, GameInfo.Strings.specieslist);
        }

        public static bool CanBeTraded(this PKM pkm)
        {
            if (pkm.IsNicknamed && StringsUtil.IsSpammyString(pkm.Nickname))
                return false;
            if (StringsUtil.IsSpammyString(pkm.OriginalTrainerName) && !IsFixedOT(new LegalityAnalysis(pkm).EncounterOriginal, pkm))
                return false;
            return !FormInfo.IsFusedForm(pkm.Species, pkm.Form, pkm.Format);
        }

        public static bool IsFixedOT(IEncounterTemplate t, PKM pkm) => t switch
        {
            IFixedTrainer { IsFixedTrainer: true } tr => true,
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
                return TrainerSettings.GetSavedTrainerData(GameVersion.SWSH, 8);
            if (typeof(T) == typeof(PB8))
                return TrainerSettings.GetSavedTrainerData(GameVersion.BDSP, 8);
            if (typeof(T) == typeof(PA8))
                return TrainerSettings.GetSavedTrainerData(GameVersion.PLA, 8);
            if (typeof(T) == typeof(PK9))
                return TrainerSettings.GetSavedTrainerData(GameVersion.SV, 9);

            throw new ArgumentException("Type does not have a recognized trainer fetch.", typeof(T).Name);
        }

        public static ITrainerInfo GetTrainerInfo(byte gen) => TrainerSettings.GetSavedTrainerData(gen, 0);

        public static PKM GetLegal(this ITrainerInfo sav, IBattleTemplate set, out string res)
        {
            var result = sav.GetLegalFromSet(set);
            res = result.Status.ToString();
            return result.Created;
        }

        public static string GetLegalizationHint(IBattleTemplate set, ITrainerInfo sav, PKM pk) => set.SetAnalysis(sav, pk);
        public static PKM LegalizePokemon(this PKM pk) => pk.Legalize();
        public static IBattleTemplate GetTemplate(ShowdownSet set) => new RegenTemplate(set);
    }
}
