using PKHeX.Core;
using SysBot.Base;
using System;
using System.Linq;
using System.Collections.Generic;
using Tweetinvi;
using Tweetinvi.Models;
using Tweetinvi.Core.Models.Properties;
using System.Threading.Tasks;
using Tweetinvi.Parameters;

namespace SysBot.Pokemon.Twitter
{
    // As Twitter is incredibly stingy with its limits, the user is only notified when we start searching
    public class TwitterTradeNotifier<T> : IPokeTradeNotifier<T> where T : PKM, new()
    {
        private T Data { get; }
        private PokeTradeTrainerInfo Info { get; }
        private int Code { get; }
        private string Username { get; }
        private TwitterClient Client { get; }
        private IUser UserToDM { get; }
        private TwitterSettings Settings { get; }

        public TwitterTradeNotifier(T data, PokeTradeTrainerInfo info, int code, string username, TwitterClient client, IUser user, TwitterSettings settings)
        {
            Data = data;
            Info = info;
            Code = code;
            Username = username;
            Client = client;
            UserToDM = user;
            Settings = settings;

            LogUtil.LogText($"Created trade details for {Username} - {Code}");
        }

        public Action<PokeRoutineExecutor<T>>? OnFinish { private get; set; }

        public string IdentifierLocator => throw new NotImplementedException();

        bool IPokeTradeNotifier<T>.ReminderSent { get => throw new NotImplementedException(); }
        public int QueueSizeEntry { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }

        public void SendNotification(PokeRoutineExecutor<T> routine, PokeTradeDetail<T> info, string message)
        {
            // Eat generic messages
            LogUtil.LogText(message);
        }

        public void TradeCanceled(PokeRoutineExecutor<T> routine, PokeTradeDetail<T> info, PokeTradeResult msg)
        {
            OnFinish?.Invoke(routine);
            var line = $"@{info.Trainer.TrainerName}: Trade canceled, {msg}";
            LogUtil.LogText(line);
            SendMessage(line);
        }

        public void TradeFinished(PokeRoutineExecutor<T> routine, PokeTradeDetail<T> info, T result)
        {
            // Eat success messages
            OnFinish?.Invoke(routine);
            var tradedToUser = Data.Species;
            var message = $"@{info.Trainer.TrainerName}: " + (tradedToUser != 0 ? $"Trade finished. Enjoy your {(Species)tradedToUser}!" : "Trade finished!");
            LogUtil.LogText(message);
        }

        public void TradeInitialize(PokeRoutineExecutor<T> routine, PokeTradeDetail<T> info)
        {
            var receive = Data.Species == 0 ? string.Empty : $" ({Data.Nickname})";
            var msg = $"@{info.Trainer.TrainerName} (ID: {info.ID}): Initializing trade{receive} with you. Your trade code is: {info.Code:0000 0000}";
            LogUtil.LogText(msg);
            SendMessage(msg);
        }

        public void TradeSearching(PokeRoutineExecutor<T> routine, PokeTradeDetail<T> info)
        {
            var name = Info.TrainerName;
            var trainer = string.IsNullOrEmpty(name) ? string.Empty : $", @{name}";
            var message = $"I'm waiting for you{trainer}! My IGN is {routine.InGameName}. Your trade code is: {info.Code:0000 0000}";
            LogUtil.LogText(message);
            SendMessage($"@{info.Trainer.TrainerName} {message}");
        }

        public void SendNotification(PokeRoutineExecutor<T> routine, PokeTradeDetail<T> info, PokeTradeSummary message)
        {
            // Eat generic notifications
            var msg = message.Summary;
            if (message.Details.Count > 0)
                msg += ", " + string.Join(", ", message.Details.Select(z => $"{z.Heading}: {z.Detail}"));
            LogUtil.LogText(msg);
        }

        public void SendNotification(PokeRoutineExecutor<T> routine, PokeTradeDetail<T> info, T result, string message)
        {
            // Eat end detail messages
            var msg = $"Details for {result.FileName}: " + message;
            LogUtil.LogText(msg);
        }

        private void SendMessage(string msg, params string[] options)
        {
            var quickOptions = new List<IQuickReplyOption>();
            foreach (var op in options)
                quickOptions.Add(new QuickReplyOption() { Label = op });

            var taskSendMsq = Task.Run(async () => await Client.Messages.PublishMessageAsync(new PublishMessageParameters(msg, UserToDM.Id)
            {
                QuickReplyOptions = quickOptions.ToArray()
            }));
        }

        public void SendReminder(int position, string message)
        {
            throw new NotImplementedException();
        }
    }
}
