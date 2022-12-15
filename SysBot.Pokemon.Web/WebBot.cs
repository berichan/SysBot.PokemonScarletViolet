using NLog.Fluent;
using PKHeX.Core;
using SysBot.Base;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace SysBot.Pokemon.Web
{

    public class WebBot<T> where T : PKM, new()
    {
        private readonly PokeTradeHub<T> Hub;
        private TradeQueueInfo<T> Info => Hub.Queues.Info;

        private readonly string URI;
        private readonly string AuthID, AuthString;
        private readonly int QueueIndex;

        private readonly IWebNotify<T> WebNotifierInstance;

        private const int Code = 1111_7477; // while I test (what I actually meant was forever)

        public WebBot(WebSettings settings, PokeTradeHub<T> hub)
        {
            Hub = hub;
            URI = settings.URIEndpoint;
            AuthID = settings.AuthID;
            AuthString = settings.AuthTokenOrString;
            QueueIndex = settings.QueueIndex;
            WebNotifierInstance = new SignalRNotify<T>(AuthID, AuthString, URI);

            for (int i = 0; i < settings.SCFeedCount; ++i)
            {
                ulong index = (ulong)(QueueIndex + i);
                Task.Run(() => loopTrades(index));
            }
        }

        private async void loopTrades(ulong toAdd = ulong.MaxValue)
        {
            var trainerDetail = "Berichan" + (toAdd == ulong.MaxValue ? "" : toAdd.ToString());
            var userID = toAdd == ulong.MaxValue ? 0ul : toAdd;
            var trainer = new PokeTradeTrainerInfo(trainerDetail);
            var pk = new T();
            while (true)
            {
                if (!Hub.Queues.GetQueue(PokeRoutineType.SeedCheck).Contains(trainerDetail))
                {
                    await Task.Delay(100).ConfigureAwait(false);

                    var notifier = new WebTradeNotifier<T>(pk, trainer, Code, WebNotifierInstance);
                    var detail = new PokeTradeDetail<T>(pk, trainer, notifier, PokeTradeType.Seed, Code, false);
                    var trade = new TradeEntry<T>(detail, userID, PokeRoutineType.SeedCheck, "");

                    Info.AddToTradeQueue(trade, userID, false);
                }

                await Task.Delay(1_000).ConfigureAwait(false);
            }
        }
    }
}
