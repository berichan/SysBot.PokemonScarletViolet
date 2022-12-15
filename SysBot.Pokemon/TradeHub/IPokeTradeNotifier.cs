using PKHeX.Core;
using System;

namespace SysBot.Pokemon
{
    public interface IPokeTradeNotifier<T> where T : PKM, new()
    {
        /// <summary> The destination or type of this notifier </summary>
        public string IdentifierLocator { get; }

        /// <summary> Size of the queue at entry </summary>
        public int QueueSizeEntry { get; set; }
        /// <summary> Has a reminder been sent? </summary>
        public bool ReminderSent { get; }

        /// <summary> Notifies when a trade bot is initializing at the start. </summary>
        void TradeInitialize(PokeRoutineExecutor<T> routine, PokeTradeDetail<T> info);
        /// <summary> Notifies when a trade bot is searching for the partner. </summary>
        void TradeSearching(PokeRoutineExecutor<T> routine, PokeTradeDetail<T> info);
        /// <summary> Notifies when a trade bot notices the trade was canceled. </summary>
        void TradeCanceled(PokeRoutineExecutor<T> routine, PokeTradeDetail<T> info, PokeTradeResult msg);
        /// <summary> Notifies when a trade bot finishes the trade. </summary>
        void TradeFinished(PokeRoutineExecutor<T> routine, PokeTradeDetail<T> info, T result);

        /// <summary> Sends a notification when called with parameters. </summary>
        void SendNotification(PokeRoutineExecutor<T> routine, PokeTradeDetail<T> info, string message);
        /// <summary> Sends a notification when called with parameters. </summary>
        void SendNotification(PokeRoutineExecutor<T> routine, PokeTradeDetail<T> info, PokeTradeSummary message);
        /// <summary> Sends a notification when called with parameters. </summary>
        void SendNotification(PokeRoutineExecutor<T> routine, PokeTradeDetail<T> info, T result, string message);

        /// <summary> Sends a reminder </summary>
        void SendReminder(int position, string message);

        /// <summary> Notifies when a trade bot is initializing at the start. </summary>
        Action<PokeRoutineExecutor<T>>? OnFinish { set; }
    }
}