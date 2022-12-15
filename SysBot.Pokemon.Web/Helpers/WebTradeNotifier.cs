using PKHeX.Core;
using SysBot.Base;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace SysBot.Pokemon.Web
{
    public class WebTradeNotifier<T> : IPokeTradeNotifier<T> where T : PKM, new()
    {
        public string IdentifierLocator => "Web";
        private T Data { get; }
        private PokeTradeTrainerInfo Info { get; }
        private int Code { get; }
        private IWebNotify<T> WebNotify { get; }
        private T Result { get; set; }
        private string OtherTrainer { get; set; } = string.Empty;
        public int QueueSizeEntry { get; set; } = -1;

        public bool ReminderSent { get; set; } = true;

        public WebTradeNotifier(T data, PokeTradeTrainerInfo info, int code, IWebNotify<T> notifier)
        {
            Data = data;
            Info = info;
            Code = code;
            WebNotify = notifier;
            Result = new T();
        }

        public Action<PokeRoutineExecutor<T>>? OnFinish { private get; set; }

        public void TradeInitialize(PokeRoutineExecutor<T> routine, PokeTradeDetail<T> info)
        {
            NotifyServerOfState(WebTradeState.Initialising);
            LogUtil.LogText($"Code: {info.Code:0000 0000}");
        }

        public void TradeSearching(PokeRoutineExecutor<T> routine, PokeTradeDetail<T> info)
        {
            NotifyServerOfState(WebTradeState.Searching, new KeyValuePair<string, string>("option", $"{info.Code:0000 0000}"));
        }

        public void TradeCanceled(PokeRoutineExecutor<T> routine, PokeTradeDetail<T> info, PokeTradeResult msg)
        {
            OnFinish?.Invoke(routine);
            NotifyServerOfState(WebTradeState.Canceled);
        }

        public void TradeFinished(PokeRoutineExecutor<T> routine, PokeTradeDetail<T> info, T result)
        {
            NotifyServerOfState(WebTradeState.Finished);
            Result = result;
            OnFinish?.Invoke(routine);
        }

        public void SendNotification(PokeRoutineExecutor<T> routine, PokeTradeDetail<T> info, string message)
        {
            if (message.TryStringBetweenStrings("Trading Partner: ", " SID:", out var trainerName))
            {
                OtherTrainer = trainerName;
                NotifyServerOfState(WebTradeState.FoundTrainer, new KeyValuePair<string, string>("option", trainerName));
            }
            if (message.TryStringBetweenStrings("Link Trade Code: ", "...", out var code))
                NotifyServerOfState(WebTradeState.TypingCode, new KeyValuePair<string, string>("option", code));

            if (message.Contains("Unable to calculate seeds: "))
                NotifyServerOfState(WebTradeState.Finished, new KeyValuePair<string, string>("option", OtherTrainer + ": " + message.Replace("Unable to calculate seeds: ", "Seedcheck failure! ")));
            if (message.Contains("This Pokémon is already shiny!"))
                NotifyServerOfState(WebTradeState.Finished, new KeyValuePair<string, string>("option", OtherTrainer + ": I can't Seedcheck a shiny Pokémon!"));
            if (message.StartsWith("SSR"))
                NotifyServerOfState(WebTradeState.Finished, new KeyValuePair<string, string>("option", OtherTrainer + ": " + message.Substring(3)));
        }

        public void SendNotification(PokeRoutineExecutor<T> routine, PokeTradeDetail<T> info, PokeTradeSummary message)
        {
            if (message.ExtraInfo is SeedSearchResult r)
            {
                NotifyServerOfTradeInfo(r);
                LogUtil.LogText($"Seed: {r.Seed:X16}");
            }
        }

        public void SendNotification(PokeRoutineExecutor<T> routine, PokeTradeDetail<T> info, T result, string message)
        {
            SendNotification(routine, info, message);
        }

        private void NotifyServerOfState(WebTradeState state, params KeyValuePair<string, string>[] additionalParams)
            => WebNotify.NotifyServerOfState(state, Info.TrainerName, additionalParams);

        private void NotifyServerOfTradeInfo(SeedSearchResult r)
            => WebNotify.NotifyServerOfSeedInfo(r, Result);

        public void SendReminder(int position, string message)
        {

        }
    }
}
