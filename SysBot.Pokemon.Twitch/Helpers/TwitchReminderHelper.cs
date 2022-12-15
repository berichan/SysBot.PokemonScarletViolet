using PKHeX.Core;
using SysBot.Base;
using System;
using System.Collections.Generic;
using System.Text;
using TwitchLib.Client;

namespace SysBot.Pokemon.Twitch
{
    public class TwitchReminderHelper<T> where T : PKM, new()
    {
        private readonly TwitchClient Client;
        private readonly PokeTradeHubConfig Config;

        private List<string> NotifyPings = new List<string>();
        private object _sync = new object();

        public TwitchReminderHelper(TwitchClient client, PokeTradeHubConfig config) 
        { 
            Client = client;
            Config = config;
        }

        public void Remind(string username)
        {
            lock (_sync)
            {
                NotifyPings.Add($"@{username}");
                CheckReminderSend();
            }
        }

        private void CheckReminderSend()
        {
            try
            {
                if (NotifyPings.Count >= Config.Twitch.ReminderTagCount)
                {
                    string msg = "[REMINDER] " + string.Join(" ", NotifyPings) + $" You are all currently in the queue and below position {Config.Queues.ReminderAtPosition}. I will send you a trade starting message shortly! Ensure you are connected (L button) and ready!";
                    Client.SendMessage(Config.Twitch.Channel, msg);
                    NotifyPings.Clear();
                }
            } 
            catch (Exception e) 
            {
                LogUtil.LogError(e.Message + "\n" + e.StackTrace, nameof(TwitchReminderHelper<T>));
            }
        }
    }
}
