using PKHeX.Core;
using PKHeX.Core.AutoMod;
using PKHeX.Core.Searching;
using SysBot.Base;
using System;
using System.Linq;
using System.Net.Sockets;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using static SysBot.Base.SwitchButton;
using static SysBot.Pokemon.PokeDataOffsetsSV;
using static SysBot.Pokemon.SpecialRequests;

namespace SysBot.Pokemon
{
    public class PokeTradeBotSV : PokeRoutineExecutor9, ICountBot
    {
        private readonly PokeTradeHub<PK9> Hub;
        private readonly TradeSettings TradeSettings;

        private readonly TradeAbuseSettings AbuseSettings;

        public ICountSettings Counts => TradeSettings;

        /// <summary>
        /// Folder to dump received trade data to.
        /// </summary>
        /// <remarks>If null, will skip dumping.</remarks>
        private readonly IDumper DumpSetting;

        /// <summary>
        /// Synchronized start for multiple bots.
        /// </summary>
        public bool ShouldWaitAtBarrier { get; private set; }

        /// <summary>
        /// Tracks failed synchronized starts to attempt to re-sync.
        /// </summary>
        public int FailedBarrier { get; private set; }

        private static readonly TrackedUserLog PreviousUsers = new();
        private static readonly TrackedUserLog PreviousUsersDistribution = new();


        public PokeTradeBotSV(PokeTradeHub<PK9> hub, PokeBotState cfg) : base(cfg)
        {
            Hub = hub;
            TradeSettings = hub.Config.Trade;
            DumpSetting = hub.Config.Folder;
            AbuseSettings = hub.Config.TradeAbuse;
        }

        public override async Task MainLoop(CancellationToken token)
        {
            try
            {
                await InitializeHardware(Hub.Config.Trade, token).ConfigureAwait(false);

                Log("Identifying trainer data of the host console.");
                var sav = await IdentifyTrainer(token).ConfigureAwait(false);

                var version = await GetVersionAsync(SwitchConnection, token).ConfigureAwait(false);
                Log($"botbase version identified as version {version}.");

                if (await IsKeyboardOpen(token).ConfigureAwait(false) && await IsConnected(token).ConfigureAwait(false))
                    await EstablishOverworldPokePortalMinimum(token, true).ConfigureAwait(false);
                else
                    await ReturnToOverworld(token).ConfigureAwait(false);

                await RestartGameIfCantTrade(false, null, token).ConfigureAwait(false);

                Log($"Starting main {nameof(PokeTradeBotSV)} loop.");
                await InnerLoop(sav, token).ConfigureAwait(false);
            }
#pragma warning disable CA1031 // Do not catch general exception types
            catch (Exception e)
#pragma warning restore CA1031 // Do not catch general exception types
            {
                Log(e.Message);
            }

            Log($"Ending {nameof(PokeTradeBotSV)} loop.");
            await HardStop().ConfigureAwait(false);
        }

        public override async Task HardStop()
        {
            UpdateBarrier(false);
            await CleanExit(TradeSettings, CancellationToken.None).ConfigureAwait(false);
        }

        private async Task<bool> ReturnToOverworld(CancellationToken token)
        {
            int tries = 15;
            while (!await CanPlayerMove(token).ConfigureAwait(false))
            {
                await Click(B, 1_000, token).ConfigureAwait(false);
                await Click(B, 1_000, token).ConfigureAwait(false);
                await Click(B, 0_500, token).ConfigureAwait(false);
                await Click(B, 0_500, token).ConfigureAwait(false);
                await Click(A, 1_000, token).ConfigureAwait(false);
                if (tries-- < 1)
                {
                    return false;
                }
            }

            await EstablishOverworldPokePortalMinimum(token).ConfigureAwait(false);

            return true;
        }

        private async Task<bool> ConnectIfNotConnected(CancellationToken token)
        {
            if (!await IsConnected(token).ConfigureAwait(false))
            {
                if (!await ReturnToOverworld(token).ConfigureAwait(false))
                    return false;

                await Task.Delay(1_000).ConfigureAwait(false);
                await Click(X, 1_000, token).ConfigureAwait(false);
                await Click(L, 8_000, token).ConfigureAwait(false);

                int tries = 11;
                while (!await IsConnected(token).ConfigureAwait(false))
                {
                    if (tries-- < 1)
                        return false;
                    await Task.Delay(1_000).ConfigureAwait(false);
                    await Click(B, 0_500, token).ConfigureAwait(false);
                }

                await Task.Delay(1_000).ConfigureAwait(false);
                for (int i = 0; i < 3; i++)
                    await Click(B, 0_500, token).ConfigureAwait(false);
            }

            return true;
        }

        public async Task<bool> RestartGameIfCantTrade(bool skipInitialChecks, int? code, CancellationToken token, bool verboseLogging = false)
        {
            if (verboseLogging)
                Log("Something has failed so we will now be verbose.");

            if (!await IsGameRunning(token).ConfigureAwait(false))
                await StartGame(Hub.Config, token).ConfigureAwait(false);

            if (!await ConnectIfNotConnected(token).ConfigureAwait(false))
                return false;

            if (await IsKeyboardOpen(token).ConfigureAwait(false))
                return true;

            await ClearKeyboardBuffer(code, token).ConfigureAwait(false);

            if (verboseLogging)
                Log("At the IsSearching point.");

            // check if we are still searching
            if (await IsSearching(token).ConfigureAwait(false))
            {
                await Click(B, 1_000, token).ConfigureAwait(false);
                await Click(A, 1_800, token).ConfigureAwait(false);
                await Click(A, 0_500, token).ConfigureAwait(false);
                await Click(PLUS, 1_500, token).ConfigureAwait(false);

                if (await IsKeyboardOpen(token).ConfigureAwait(false))
                    return true;
            }

            if (!skipInitialChecks)
            {
                if (!await CanPlayerMove(token).ConfigureAwait(false) && await IsPokePortalLoaded(token, verboseLogging).ConfigureAwait(false))
                {
                    await Click(A, 1_500, token).ConfigureAwait(false);
                    await Click(PLUS, 1_500, token).ConfigureAwait(false);

                    if (await IsKeyboardOpen(token).ConfigureAwait(false))
                        return true;
                }

                // Go all the way back to overworld
                if (!await ReturnToOverworld(token).ConfigureAwait(false))
                {
                    if (verboseLogging)
                        Log("Could not return to overworld, restarting...");

                    await ReOpenGame(Hub.Config, token).ConfigureAwait(false);
                    await RestartGameIfCantTrade(true, code, token).ConfigureAwait(false);
                }
                else
                    await EstablishOverworldPokePortalMinimum(token).ConfigureAwait(false);
            }

            await Click(X, 1_000, token).ConfigureAwait(false);

            if (!skipInitialChecks)
            {
                // hold dpad up
                await PressAndHold(DUP, 2_300, 0_400, token).ConfigureAwait(false);
            }

            // Assuming we've unlocked picnic
            await Click(DRIGHT, 0_500, token).ConfigureAwait(false);
            await Click(DUP, 0_500, token).ConfigureAwait(false);
            await Click(DUP, 0_500, token).ConfigureAwait(false);
            await Click(DUP, 0_850, token).ConfigureAwait(false);
            await Click(A, 0_500, token).ConfigureAwait(false);

            int checks = 20;
            while (!await IsPokePortalLoaded(token, verboseLogging).ConfigureAwait(false))
            {
                await Task.Delay(0_800, token).ConfigureAwait(false);
                if (checks-- < 1)
                {
                    Log("Couldn't get to PokePortal, restarting...");
                    await ReOpenGame(Hub.Config, token).ConfigureAwait(false);
                    return false;
                }
            }

            if (!await IsConnected(token).ConfigureAwait(false))
            {
                Log("Not connected, trying again...");
                await ConnectIfNotConnected(token).ConfigureAwait(false);
                await RestartGameIfCantTrade(true, code, token).ConfigureAwait(false);
            }

            await Task.Delay(6_000 + Hub.Config.Timings.ExtraTimeOpenPokePortal, token).ConfigureAwait(false); // Takes around 6 seconds for pokeportal to load up

            await Click(DDOWN, 0_700, token).ConfigureAwait(false);
            await Click(DDOWN, 0_700, token).ConfigureAwait(false);
            await Click(A, 0_700, token).ConfigureAwait(false);
            await Click(PLUS, 1_500, token).ConfigureAwait(false);

            if (!await IsKeyboardOpen(token).ConfigureAwait(false))
            {
                if (verboseLogging)
                {
                    var connectState = await IsConnected(token).ConfigureAwait(false);
                    var pokePortalState = await IsPokePortalLoaded(token, true).ConfigureAwait(false);
                    Log($"At final keyboard check. Connected: {connectState}. PokePortal: {pokePortalState}.");
                }
                return false;
            }

            return true;
        }

        private async Task AttemptGetBackToPokePortal(CancellationToken token)
        {
            if (await CanPlayerMove(token).ConfigureAwait(false) || await IsKeyboardOpen(token).ConfigureAwait(false))
                return;

            int tries = 12;
            while (!await IsPokePortalLoaded(token).ConfigureAwait(false) && !await CanPlayerMove(token).ConfigureAwait(false) && tries-- > 0)
            {
                await Click(B, 0_500, token).ConfigureAwait(false);
                await Click(B, 0_500, token).ConfigureAwait(false);
                if (!await IsPokePortalLoaded(token).ConfigureAwait(false))
                    await Click(A, 1_000, token).ConfigureAwait(false);
            }

            if (await IsPokePortalLoaded(token).ConfigureAwait(false))
                await Task.Delay(3_000 + Hub.Config.Timings.ExtraTimeOpenPokePortal, token).ConfigureAwait(false);
        }

        private async Task InnerLoop(SAV9SV sav, CancellationToken token)
        {
            while (!token.IsCancellationRequested)
            {
                Config.IterateNextRoutine();
                var task = Config.CurrentRoutineType switch
                {
                    PokeRoutineType.Idle => DoNothing(token),
                    _ => DoTrades(sav, token),
                };
                try
                {
                    await task.ConfigureAwait(false);
                }
                catch (SocketException e)
                {
                    Log(e.Message);
                    Connection.Reset();
                }
            }
        }

        private async Task DoNothing(CancellationToken token)
        {
            int waitCounter = 0;
            while (!token.IsCancellationRequested && Config.NextRoutineType == PokeRoutineType.Idle)
            {
                if (waitCounter == 0)
                    Log("No task assigned. Waiting for new task assignment.");
                waitCounter++;
                if (waitCounter % 10 == 0 && Hub.Config.AntiIdle)
                    await Click(B, 1_000, token).ConfigureAwait(false);
                else
                    await Task.Delay(1_000, token).ConfigureAwait(false);
            }
        }

        private async Task DoTrades(SAV9SV sav, CancellationToken token)
        {
            var type = Config.CurrentRoutineType;
            int waitCounter = 0;
            while (!token.IsCancellationRequested && Config.NextRoutineType == type)
            {
                await AttemptClearTradePartnerPointer(token).ConfigureAwait(false);
                var (detail, priority) = GetTradeData(type);
                if (detail is null)
                {
                    await WaitForQueueStep(waitCounter++, token).ConfigureAwait(false);
                    continue;
                }
                waitCounter = 0;

                detail.IsProcessing = true;
                if (!await RestartGameIfCantTrade(false, detail.Code, token).ConfigureAwait(false))
                    await RestartGameIfCantTrade(false, detail.Code, token, true).ConfigureAwait(false);
                string tradetype = $" ({detail.Type})";
                Log($"Starting next {type}{tradetype} Bot Trade. Getting data...");
                Hub.Config.Stream.StartTrade(this, detail, Hub);
                Hub.Queues.StartTrade(this, detail);

                await PerformTrade(sav, detail, type, priority, token).ConfigureAwait(false);

                // return to original position if required
                await RestartGameIfCantTrade(false, null, token).ConfigureAwait(false);
            }
        }

        private async Task WaitForQueueStep(int waitCounter, CancellationToken token)
        {
            if (waitCounter == 0)
            {
                // Updates the assets.
                Hub.Config.Stream.IdleAssets(this);
                Log("Nothing to check, waiting for new users...");
            }

            const int interval = 10;
            if (waitCounter % interval == interval - 1 && Hub.Config.AntiIdle)
                await Click(B, 1_000, token).ConfigureAwait(false);
            else
                await Task.Delay(1_000, token).ConfigureAwait(false);
        }

        protected virtual (PokeTradeDetail<PK9>? detail, uint priority) GetTradeData(PokeRoutineType type)
        {
            if (Hub.Queues.TryDequeue(type, out var detail, out var priority))
                return (detail, priority);
            if (Hub.Queues.TryDequeueLedy(out detail))
                return (detail, PokeTradePriorities.TierFree);
            return (null, PokeTradePriorities.TierFree);
        }

        private async Task AttemptClearTradePartnerPointer(CancellationToken token)
        {
            (var valid, var offs) = await ValidatePointerAll(LinkTradePartnerNIDPointer, token).ConfigureAwait(false);
            if (valid)
                await SwitchConnection.WriteBytesAbsoluteAsync(new byte[8], offs, token).ConfigureAwait(false);

            (valid, offs) = await ValidatePointerAll(LinkTradePartnerNameSlot1Pointer, token).ConfigureAwait(false);
            if (valid)
                await SwitchConnection.WriteBytesAbsoluteAsync(new byte[4], offs, token).ConfigureAwait(false);

            (valid, offs) = await ValidatePointerAll(LinkTradePartnerNameSlot2Pointer, token).ConfigureAwait(false);
            if (valid)
                await SwitchConnection.WriteBytesAbsoluteAsync(new byte[4], offs, token).ConfigureAwait(false);
        }

        private async Task PerformTrade(SAV9SV sav, PokeTradeDetail<PK9> detail, PokeRoutineType type, uint priority, CancellationToken token)
        {
            PokeTradeResult result;
            try
            {
                result = await PerformLinkCodeTrade(sav, detail, token).ConfigureAwait(false);
                if (result == PokeTradeResult.Success)
                    return;
            }
            catch (SocketException socket)
            {
                Log(socket.Message);
                result = PokeTradeResult.ExceptionConnection;
                HandleAbortedTrade(detail, type, priority, result);
                throw; // let this interrupt the trade loop. re-entering the trade loop will recheck the connection.
            }
            catch (Exception e)
            {
                Log(e.Message);
                result = PokeTradeResult.Exception_NPM;
            }

            HandleAbortedTrade(detail, type, priority, result);
        }

        private async Task<PokeTradeResult> PerformLinkCodeTrade(SAV9SV sav, PokeTradeDetail<PK9> poke, CancellationToken token)
        {
            if (poke.Type == PokeTradeType.Random)
                SetText(sav, $"Trade code: {poke.Code:0000 0000}\r\nSending: {(Species)poke.TradeData.Species}{(poke.TradeData.IsEgg ? " (egg)" : string.Empty)}");
            else
                SetText(sav, "Running a\nSpecific trade.");

            UpdateBarrier(poke.IsSynchronized);
            poke.TradeInitialize(this);
            Hub.Config.Stream.EndEnterCode(this);

            if (poke.Type != PokeTradeType.Random)
                Hub.Config.Stream.StartEnterCode(this);

            var toSend = poke.TradeData;
            if (toSend.Species != 0)
                await SetBoxPokemon(toSend, token, sav).ConfigureAwait(false);

            if (!await IsKeyboardOpen(token).ConfigureAwait(false))
            {
                await Click(A, 0_500, token).ConfigureAwait(false);
                return PokeTradeResult.RecoverStart;
            }

            if (!await BeginTradeViaCode(poke, poke.Code, token).ConfigureAwait(false))
            {
                for (int i = 0; i < 5; ++i)
                    await Click(B, 0_500, token).ConfigureAwait(false);
                await RestartGameIfCantTrade(false, null, token).ConfigureAwait(false);
                return PokeTradeResult.RecoverOpenBox;
            }

            poke.TradeSearching(this);

            // Wait to hit the bot or quit if no trade partner found
            int inBoxChecks = Hub.Config.Trade.TradeWaitTime;
            while (await IsPokePortalLoaded(token).ConfigureAwait(false))
            {
                if (inBoxChecks-- < 0)
                {
                    await Click(B, 1_500, token).ConfigureAwait(false);
                    if (await IsPokePortalLoaded(token).ConfigureAwait(false))
                    {
                        await Click(A, 1_500, token).ConfigureAwait(false);
                        await ClearKeyboardBuffer(null, token).ConfigureAwait(false);
                        await Click(PLUS, 0_800, token).ConfigureAwait(false);
                        return PokeTradeResult.NoTrainerWasFound;
                    }
                }

                await Task.Delay(1_000, token).ConfigureAwait(false);
            }

            // Still going through dialog and extremely laggy box opening.
            await Task.Delay(2_000, token).ConfigureAwait(false);

            Hub.Config.Stream.EndEnterCode(this);

            if (poke.Type == PokeTradeType.Random)
                await ClearKeyboardBuffer(null, token).ConfigureAwait(false);

            var tradePartnerNID = await GetTradePartnerNID(token).ConfigureAwait(false);
            var tradePartner = await FetchIDFromTradeOffset(token).ConfigureAwait(false);
            tradePartner.NSAID = tradePartnerNID;

            bool multi = false;
            bool IsSafe = poke.Trainer.ID == 0 || NewAntiAbuse.Instance.LogUser(tradePartner.IDHash, tradePartnerNID, tradePartner.TrainerName, poke.Trainer.TrainerName, Hub.Config.TradeAbuse.MultiAbuseEchoMention, out multi);
            if (!IsSafe || (multi && AbuseSettings.AllowMultiAccountUse))
            {
                Log($"Found known abuser: {tradePartner.TrainerName}-{tradePartner.SID}-{tradePartner.TID} ({poke.Trainer.TrainerName}) (NID: {tradePartnerNID}) origin: {poke.Notifier.IdentifierLocator}");
                poke.SendNotification(this, $"Your savedata is associated with a known abuser. Consider not being an abuser, and you will no longer see this message.");
                return PokeTradeResult.SuspiciousActivity;
            }

            Log($"Found trading partner: {tradePartner.TrainerName}-{tradePartner.TID}-{tradePartner.SID} ({poke.Trainer.TrainerName}) (NID: {tradePartnerNID}) [CODE:{poke.Code:00000000}]");

            if (BadUserList.Users.Contains(tradePartnerNID))
                return PokeTradeResult.SuspiciousActivity;

            poke.SendNotification(this, $"Found Trading Partner: {tradePartner.TrainerName}. TID: {tradePartner.TID} SID: {tradePartner.SID} Waiting for a Pokémon...");

            if (poke.Type == PokeTradeType.Dump)
                return await ProcessDumpTradeAsync(poke, token).ConfigureAwait(false);

            if (poke.Type == PokeTradeType.Random)
                if (CheckPartnerReputation(poke, tradePartnerNID, tradePartner.TrainerName, token) != PokeTradeResult.Success)
                    return PokeTradeResult.SuspiciousActivity;

            // Confirm Box 1 Slot 1
            if (poke.Type == PokeTradeType.Specific)
            {
                for (int i = 0; i < 10; i++)
                    await Click(A, 0_500, token).ConfigureAwait(false);
            }

            var offered = await ReadUntilPresentPointer(LinkTradePartnerPokemonPointer, 25_000, 1_000, TradeFormatSlotSize, token).ConfigureAwait(false);
            Log("Pointer is present with a pokemon.");

            var offset = await SwitchConnection.PointerAll(LinkTradePartnerPokemonPointer, token).ConfigureAwait(false);
            var oldEC = await SwitchConnection.ReadBytesAbsoluteAsync(offset, 4, token).ConfigureAwait(false);
            if (offered is null)
            {
                Log("Offered is NULL");
                await AttemptGetBackToPokePortal(token).ConfigureAwait(false);
                return PokeTradeResult.NoPokemonDetected;
            }

            SpecialTradeType itemReq = SpecialTradeType.None;
            if (poke.Type == PokeTradeType.Seed)
                itemReq = CheckItemRequest(ref offered, this, poke, tradePartner.TrainerName, sav);
            if (itemReq == SpecialTradeType.FailReturn)
                return PokeTradeResult.IllegalTrade;

            if (poke.Type == PokeTradeType.Seed && itemReq == SpecialTradeType.None)
            {
                // Immediately exit, we aren't trading anything.
                poke.SendNotification(this, "SSRNo held item or valid request!");
                return await EndQuickTradeAsync(poke, offered, token).ConfigureAwait(false);
            }

            PokeTradeResult update;
            (toSend, update) = await GetEntityToSend(sav, poke, offered, oldEC, toSend, tradePartner, poke.Type == PokeTradeType.Seed ? itemReq : null, token).ConfigureAwait(false);
            if (update != PokeTradeResult.Success)
            {
                if (itemReq != SpecialTradeType.None)
                {
                    poke.SendNotification(this, "SSRYour request isn't legal. Please try a different Pokémon or request.");
                    if (!string.IsNullOrWhiteSpace(Hub.Config.Web.URIEndpoint))
                        AddToPlayerLimit(tradePartner.IDHash.ToString(), -1);
                }

                return update;
            }

            if (itemReq == SpecialTradeType.WonderCard)
                poke.SendNotification(this, "SSRDistribution success!");
            else if (itemReq != SpecialTradeType.None && itemReq != SpecialTradeType.Shinify)
                poke.SendNotification(this, "SSRSpecial request successful!");
            else if (itemReq == SpecialTradeType.Shinify)
                poke.SendNotification(this, "SSRShinify success! Thanks for being part of the community!");

            Log("Confirming trade...");

            var tradeResult = await ConfirmAndStartTrading(poke, token).ConfigureAwait(false);
            if (tradeResult == PokeTradeResult.Hiccup_Server || tradeResult == PokeTradeResult.TrainerHasBadConnection)
            {
                Log("Connection hiccup detected! Waiting it out...");
                await Click(A, 0_100, token).ConfigureAwait(false);
                await Task.Delay(2_900, token).ConfigureAwait(false);
            }
            else if (tradeResult != PokeTradeResult.Success)
                return tradeResult;

            if (token.IsCancellationRequested)
                return PokeTradeResult.RoutineCancel;

            // Trade was Successful!
            var received = await ReadBoxPokemon(1, 1, token).ConfigureAwait(false);
            // Pokémon in b1s1 is same as the one they were supposed to receive (was never sent).
            if (SearchUtil.HashByDetails(received) == SearchUtil.HashByDetails(toSend))
            {
                Log($"User did not complete the trade. Sent:");
                if (tradeResult == PokeTradeResult.TrainerHasBadConnection)
                    return PokeTradeResult.TrainerHasBadConnection;
                return PokeTradeResult.NoPokemonDetected;
            }

            poke.SendNotification(this, received, $"You sent me {(Species)received.Species} for {(Species)toSend.Species}!");

            // As long as we got rid of our inject in b1s1, assume the trade went through.
            Log("User completed the trade.");
            poke.TradeFinished(this, received);

            await AttemptGetBackToPokePortal(token).ConfigureAwait(false);
            return PokeTradeResult.Success;
        }

        private async Task<PokeTradeResult> ConfirmAndStartTrading(PokeTradeDetail<PK9> detail, CancellationToken token)
        {
            var oldPKData = await SwitchConnection.PointerPeek(BoxFormatSlotSize, BoxStartPokemonPointer, token).ConfigureAwait(false);

            await Click(A, 3_000, token).ConfigureAwait(false);
            for (int i = 0; i < 14; i++)
            {
                await Click(A, 1_500, token).ConfigureAwait(false);
            }

            await Click(A, 3_000, token).ConfigureAwait(false);
            var tradeCounter = 0;
            while (await IsPokePortalLoaded(token).ConfigureAwait(false)) // PokePortal is loaded during trade animation
            {
                await Click(A, 1_000, token).ConfigureAwait(false);
                tradeCounter++;

                var v1 = await SwitchConnection.PointerPeek(BoxFormatSlotSize, BoxStartPokemonPointer, token).ConfigureAwait(false);
                if (!v1.SequenceEqual(oldPKData))
                {
                    await Task.Delay(26_000, token).ConfigureAwait(false);
                    return PokeTradeResult.Success;
                }
                if (tradeCounter >= Hub.Config.Trade.TradeAnimationMaxDelaySeconds)
                    break;
            }

            if (detail.Type == PokeTradeType.Specific && !await IsPokePortalLoaded(token).ConfigureAwait(false)) // One last chance to force them to take the pokemon
                for (int i = 0; i < 8; i++)
                    await Click(A, 0_400, token).ConfigureAwait(false);
            
            // If we don't detect a B1S1 change, the trade didn't go through in that time.
            return PokeTradeResult.TrainerHasBadConnection;
        }

        static readonly byte[] EmptyByteArray = new byte[16];
        private async Task<bool> BeginTradeViaCode(PokeTradeDetail<PK9> poke, int tradeCode, CancellationToken token)
        {
            if (!await IsKeyboardOpen(token).ConfigureAwait(false))
            {
                Log($"Starting new trade, but keyboard was not open!");
                return false;
            }

            Log($"Starting new trade, keyboard is open! Entering Link Trade code: {tradeCode:0000 0000}...");
            poke.SendNotification(this, $"Entering Link Trade Code: {tradeCode:0000 0000}...");

            // Just inject the code instead
            var offs = await SwitchConnection.PointerAll(KeyboardBufferPointer, token).ConfigureAwait(false);
            var keyboardbytes = await SwitchConnection.ReadBytesAbsoluteAsync(offs, 16, token).ConfigureAwait(false);
            if (keyboardbytes.SequenceEqual(EmptyByteArray))
            {
                // get out of keyboard
                await Click(PLUS, 1_000, token).ConfigureAwait(false);

                // as we inject the code, a wait should be placed here to give the other trainer time to setup
                if (poke.Type == PokeTradeType.Specific)
                    await Task.Delay(Hub.Config.Timings.ExtraTimeOpenCodeEntry, token).ConfigureAwait(false);

                // inject
                var codeText = $"{tradeCode:00000000}";
                var codeBytes = Encoding.Unicode.GetBytes(codeText);
                await SwitchConnection.WriteBytesAbsoluteAsync(codeBytes, offs, token).ConfigureAwait(false);

                // get back in (cycle)
                await Click(PLUS, 1_000, token).ConfigureAwait(false); 
            }

            // Wait for Barrier to trigger all bots simultaneously.
            WaitAtBarrierIfApplicable(token);

            await Task.Delay(0_500, token).ConfigureAwait(false);
            await Click(PLUS, 1_000, token).ConfigureAwait(false);
            for (int i = 0; i < 5; ++i)
                await Click(A, 0_500, token).ConfigureAwait(false);

            int checks = 3;
            while (!await IsSearching(token).ConfigureAwait(false))
            {
                await Task.Delay(0_800).ConfigureAwait(false);
                if (checks-- < 0)
                    return false;
            }    

            return true;
        }

        private async Task<PokeTradeResult> ProcessDumpTradeAsync(PokeTradeDetail<PK9> detail, CancellationToken token)
        {
            int ctr = 0;
            var time = TimeSpan.FromSeconds(Hub.Config.Trade.MaxDumpTradeTime);
            var start = DateTime.Now;
            var pkprev = new PK9();
            while (ctr < Hub.Config.Trade.MaxDumpsPerTrade && DateTime.Now - start < time)
            {
                var pk = await ReadUntilPresentPointer(LinkTradePartnerPokemonPointer, 3_000, 1_000, TradeFormatSlotSize, token).ConfigureAwait(false);
                if (pk == null || pk.Species < 1 || !pk.ChecksumValid || SearchUtil.HashByDetails(pk) == SearchUtil.HashByDetails(pkprev))
                    continue;

                // Save the new Pokémon for comparison next round.
                pkprev = pk;

                // Send results from separate thread; the bot doesn't need to wait for things to be calculated.
                if (DumpSetting.Dump)
                {
                    var subfolder = detail.Type.ToString().ToLower();
                    DumpPokemon(DumpSetting.DumpFolder, subfolder, pk); // received
                }

                var la = new LegalityAnalysis(pk);
                var verbose = la.Report(true);
                Log($"Shown Pokémon is: {(la.Valid ? "Valid" : "Invalid")}.");

                detail.SendNotification(this, pk, verbose);
                ctr++;
            }

            Log($"Ended Dump loop after processing {ctr} Pokémon.");
            if (ctr == 0)
                return PokeTradeResult.NoPokemonDetected;

            TradeSettings.AddCompletedDumps();
            detail.Notifier.SendNotification(this, detail, $"Dumped {ctr} Pokémon.");
            detail.Notifier.TradeFinished(this, detail, new PK9()); // blank
            return PokeTradeResult.Success;
        }

        protected virtual async Task<(PK9 toSend, PokeTradeResult check)> GetEntityToSend(SAV9SV sav, PokeTradeDetail<PK9> poke, PK9 offered, byte[] oldEC, PK9 toSend, TrainerIDBlock partnerID, SpecialTradeType? stt, CancellationToken token)
        {
            return poke.Type switch
            {
                PokeTradeType.Random => await HandleRandomLedy(sav, poke, offered, toSend, partnerID, token).ConfigureAwait(false),
                PokeTradeType.Clone => await HandleClone(sav, poke, offered, oldEC, token).ConfigureAwait(false),
                PokeTradeType.Seed when stt is not SpecialTradeType.WonderCard => await HandleClone(sav, poke, offered, oldEC, token).ConfigureAwait(false),
                PokeTradeType.Seed when stt is SpecialTradeType.WonderCard => await JustInject(sav, offered, token).ConfigureAwait(false),
                _ => (toSend, PokeTradeResult.Success),
            };
        }

        private async Task<(PK9 toSend, PokeTradeResult check)> HandleClone(SAV9SV sav, PokeTradeDetail<PK9> poke, PK9 offered, byte[] oldEC, CancellationToken token)
        {
            if (Hub.Config.Discord.ReturnPKMs)
                poke.SendNotification(this, offered, "Here's what you showed me!");

            var la = new LegalityAnalysis(offered);
            if (!la.Valid)
            {
                Log($"Clone request (from {poke.Trainer.TrainerName}) has detected an invalid Pokémon: {(Species)offered.Species}.");
                if (DumpSetting.Dump)
                    DumpPokemon(DumpSetting.DumpFolder, "hacked", offered);

                var report = la.Report();
                Log(report);
                poke.SendNotification(this, "This Pokémon is not legal per PKHeX's legality checks. I am forbidden from cloning this. Exiting trade.");
                poke.SendNotification(this, report);

                return (offered, PokeTradeResult.IllegalTrade);
            }

            // Inject the shown Pokémon.
            var clone = (PK9)offered.Clone();
            if (Hub.Config.Legality.ResetHOMETracker)
                clone.Tracker = 0;

            poke.SendNotification(this, $"**Cloned your {(Species)clone.Species}!**\nNow press B to cancel your offer and trade me a Pokémon you don't want.");
            Log($"Cloned a {(Species)clone.Species}. Waiting for user to change their Pokémon...");

            // Separate this out from WaitForPokemonChanged since we compare to old EC from original read.
            var valid = false;
            var offset = 0ul;
            while (!valid)
            {
                await Task.Delay(0_500, token).ConfigureAwait(false);
                (valid, offset) = await ValidatePointerAll(LinkTradePartnerPokemonPointer, token).ConfigureAwait(false);
            }

            var pkmChanged = await ReadUntilChanged(offset, oldEC, 15_000, 0_200, false, true, token).ConfigureAwait(false);

            if (!pkmChanged)
            {
                poke.SendNotification(this, "**HEY CHANGE IT NOW OR I AM LEAVING!!!**");
                // They get one more chance.
                pkmChanged = await ReadUntilChanged(offset, oldEC, 15_000, 0_200, false, true, token).ConfigureAwait(false);
            }

            // resolve pointer for any shifts
            offset = await SwitchConnection.PointerAll(LinkTradePartnerPokemonPointer, token).ConfigureAwait(false);
            var pk2 = await ReadUntilPresent(offset, 3_000, 1_000, BoxFormatSlotSize, token).ConfigureAwait(false);
            if (!pkmChanged || pk2 == null || SearchUtil.HashByDetails(pk2) == SearchUtil.HashByDetails(offered))
            {
                Log("Trade partner did not change their Pokémon.");
                return (offered, PokeTradeResult.NoPokemonDetected);
            }

            await SetBoxPokemon(clone, token, sav).ConfigureAwait(false);
            await Click(A, 0_800, token).ConfigureAwait(false);

            for (int i = 0; i < 5; i++)
                await Click(A, 0_500, token).ConfigureAwait(false);

            return (clone, PokeTradeResult.Success);
        }

        private async Task<(PK9 toSend, PokeTradeResult check)> HandleRandomLedy(SAV9SV sav, PokeTradeDetail<PK9> poke, PK9 offered, PK9 toSend, TrainerIDBlock partner, CancellationToken token)
        {
            // Allow the trade partner to do a Ledy swap.
            var config = Hub.Config.Distribution;
            var trade = Hub.Ledy.GetLedyTrade(offered, partner.NSAID, config.LedySpecies);
            if (trade != null)
            {
                if (trade.Type == LedyResponseType.AbuseDetected)
                {
                    var msg = $"Found {partner.TrainerName} has been detected for abusing Ledy trades.";
                    EchoUtil.Echo(msg);

                    return (toSend, PokeTradeResult.SuspiciousActivity);
                }

                toSend = trade.Receive;
                poke.TradeData = toSend;

                poke.SendNotification(this, "Injecting the requested Pokémon.");
                await Click(A, 0_800, token).ConfigureAwait(false);
                await SetBoxPokemon(toSend, token, sav).ConfigureAwait(false);
                await Task.Delay(1_000, token).ConfigureAwait(false);
            }
            else if (config.LedyQuitIfNoMatch)
            {
                return (toSend, PokeTradeResult.TrainerRequestBad);
            }

            for (int i = 0; i < 5; i++)
            {
                await Click(A, 0_500, token).ConfigureAwait(false);
            }

            return (toSend, PokeTradeResult.Success);
        }

        private async Task<(PK9 toSend, PokeTradeResult check)> JustInject(SAV9SV sav, PK9 offered, CancellationToken token)
        {
            await Click(A, 0_800, token).ConfigureAwait(false);
            await SetBoxPokemon(offered, token, sav).ConfigureAwait(false);

            for (int i = 0; i < 5; i++)
                await Click(A, 0_500, token).ConfigureAwait(false);

            return (offered, PokeTradeResult.Success);
        }

        private async Task<PokeTradeResult> EndQuickTradeAsync(PokeTradeDetail<PK9> detail, PK9 pk, CancellationToken token)
        {
            int attempts = 20;
            while (!await IsPokePortalLoaded(token).ConfigureAwait(false))
            {
                await Click(B, 0_800, token).ConfigureAwait(false);
                await Click(B, 0_800, token).ConfigureAwait(false);
                await Click(A, 1_200, token).ConfigureAwait(false);
                if (attempts-- < 1)
                    return PokeTradeResult.RecoverReturnOverworld;
            }

            detail.TradeFinished(this, pk);

            if (DumpSetting.Dump && !string.IsNullOrEmpty(DumpSetting.DumpFolder))
                DumpPokemon(DumpSetting.DumpFolder, "quick", pk);

            return PokeTradeResult.Success;
        }

        private void HandleAbortedTrade(PokeTradeDetail<PK9> detail, PokeRoutineType type, uint priority, PokeTradeResult result)
        {
            detail.IsProcessing = false;
            if (result.ShouldAttemptRetry() && detail.Type != PokeTradeType.Random && !detail.IsRetry)
            {
                detail.IsRetry = true;
                Hub.Queues.Enqueue(type, detail, Math.Min(priority, PokeTradePriorities.Tier2));
                detail.SendNotification(this, "Oops! Something happened. I'm going to requeue you for another attempt, give me a moment.");
            }
            else
            {
                detail.SendNotification(this, $"Oops! Something happened. Canceling the trade due to reason: {result}.");
                detail.TradeCanceled(this, result);
            }
        }

        private void WaitAtBarrierIfApplicable(CancellationToken token)
        {
            if (!ShouldWaitAtBarrier)
                return;
            var opt = Hub.Config.Distribution.SynchronizeBots;
            if (opt == BotSyncOption.NoSync)
                return;

            var timeoutAfter = Hub.Config.Distribution.SynchronizeTimeout;
            if (FailedBarrier == 1) // failed last iteration
                timeoutAfter *= 2; // try to re-sync in the event things are too slow.

            var result = Hub.BotSync.Barrier.SignalAndWait(TimeSpan.FromSeconds(timeoutAfter), token);

            if (result)
            {
                FailedBarrier = 0;
                return;
            }

            FailedBarrier++;
            Log($"Barrier sync timed out after {timeoutAfter} seconds. Continuing.");
        }


        /// <summary>
        /// Checks if the barrier needs to get updated to consider this bot.
        /// If it should be considered, it adds it to the barrier if it is not already added.
        /// If it should not be considered, it removes it from the barrier if not already removed.
        /// </summary>
        private void UpdateBarrier(bool shouldWait)
        {
            if (ShouldWaitAtBarrier == shouldWait)
                return; // no change required

            ShouldWaitAtBarrier = shouldWait;
            if (shouldWait)
            {
                Hub.BotSync.Barrier.AddParticipant();
                Log($"Joined the Barrier. Count: {Hub.BotSync.Barrier.ParticipantCount}");
            }
            else
            {
                Hub.BotSync.Barrier.RemoveParticipant();
                Log($"Left the Barrier. Count: {Hub.BotSync.Barrier.ParticipantCount}");
            }
        }

        private void SetText(SAV9SV sav, string text)
        {
            System.IO.File.WriteAllText($"code{sav.OT}-{sav.DisplayTID}.txt", text);
        }

        private async Task ClearKeyboardBuffer(int? code, CancellationToken token)
        {
            (var valid, var offs) = await ValidatePointerAll(KeyboardBufferPointer, token).ConfigureAwait(false);
            if (!valid)
                return;

            if (code.HasValue)
            {
                var codeText = $"{code:00000000}";
                var codeBytes = Encoding.Unicode.GetBytes(codeText);
                await SwitchConnection.WriteBytesAbsoluteAsync(codeBytes, offs, token).ConfigureAwait(false);
            }
            else
                await SwitchConnection.WriteBytesAbsoluteAsync(new byte[0x10], offs, token).ConfigureAwait(false);
        }

        private PokeTradeResult CheckPartnerReputation(PokeTradeDetail<PK9> poke, ulong TrainerNID, string TrainerName, CancellationToken token)
        {
            bool quit = false;
            var user = poke.Trainer;
            var isDistribution = poke.Type == PokeTradeType.Random;
            var useridmsg = isDistribution ? "" : $" ({user.ID})";
            var list = isDistribution ? PreviousUsersDistribution : PreviousUsers;

            var cooldown = list.TryGetPrevious(TrainerNID);
            if (cooldown != null)
            {
                var delta = DateTime.Now - cooldown.Time;
                Log($"Last saw {user.TrainerName} {delta.TotalMinutes:F1} minutes ago (OT: {TrainerName}).");

                var cd = AbuseSettings.TradeCooldown;
                if (cd != 0 && TimeSpan.FromMinutes(cd) > delta)
                {
                    poke.Notifier.SendNotification(this, poke, "You have ignored the trade cooldown set by the bot owner. The owner has been notified.");
                    var msg = $"Found {user.TrainerName}{useridmsg} ignoring the {cd} minute trade cooldown. Last encountered {delta.TotalMinutes:F1} minutes ago.";
                    if (AbuseSettings.EchoNintendoOnlineIDCooldown)
                        msg += $"\nID: {TrainerNID}";
                    if (!string.IsNullOrWhiteSpace(AbuseSettings.CooldownAbuseEchoMention))
                        msg = $"{AbuseSettings.CooldownAbuseEchoMention} {msg}";
                    EchoUtil.Echo(msg);
                    quit = true;
                }
            }

            // Try registering the partner in our list of recently seen.
            // Get back the details of their previous interaction.
            var previous = isDistribution
                ? list.TryRegister(TrainerNID, TrainerName)
                : list.TryRegister(TrainerNID, TrainerName, poke.Trainer.ID);

            if (quit)
                return PokeTradeResult.SuspiciousActivity;

            var entry = AbuseSettings.BannedIDs.List.Find(z => z.ID == TrainerNID);
            if (entry != null)
            {
                var msg = $"{user.TrainerName}{useridmsg} is a banned user, and was encountered in-game using OT: {TrainerName}.";
                if (!string.IsNullOrWhiteSpace(entry.Comment))
                    msg += $"\nUser was banned for: {entry.Comment}";
                if (!string.IsNullOrWhiteSpace(AbuseSettings.BannedIDMatchEchoMention))
                    msg = $"{AbuseSettings.BannedIDMatchEchoMention} {msg}";
                EchoUtil.Echo(msg);
                return PokeTradeResult.SuspiciousActivity;
            }

            return PokeTradeResult.Success;
        }
    }
}
