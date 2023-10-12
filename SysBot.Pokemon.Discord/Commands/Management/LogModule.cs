using Discord.Commands;
using Discord.WebSocket;
using SysBot.Base;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Discord;

namespace SysBot.Pokemon.Discord
{
    public class LogModule : ModuleBase<SocketCommandContext>
    {
        private class LogAction : ChannelAction<string, string>
        {
            public LogAction(ulong id, Action<string, string> messager, string channel) : base(id, messager, channel)
            {
            }
        }

        private static readonly Dictionary<ulong, LogAction> Channels = new();

        public static void RestoreLogging(DiscordSocketClient discord, DiscordSettings settings)
        {
            foreach (var ch in settings.LoggingChannels)
            {
                if (discord.GetChannel(ch.ID) is ISocketMessageChannel c)
                    AddLogChannel(c, ch.ID, LogUtil.Forwarders);
            }

            foreach (var ach in settings.AbuseLoggingChannels)
            {
                if (discord.GetChannel(ach.ID) is ISocketMessageChannel a)
                    AddLogChannel(a, ach.ID, NewAntiAbuse.Instance.Forwarders);
            }

            //updated the logging information to display me the updated build information. 
            //LogModule.cs is currently updated manually to reflect build changes. 
            LogUtil.LogInfo("ADDED LOGGING TO DISCORD CHANNELS ON BOT STARTUP: ", "Discord"); // Original Discord Notification of logging
            LogUtil.LogInfo("IF YOU ARE NOT THE DEVELOPER OF THIS BOT YOU CAN IGNORE OR REMOVE THE NEXT LINE", "Discord"); // Disclaimer
            LogUtil.LogInfo("[Current Build Berichan Bot(9/25/23 repo push merged into fork] [Xieon's Fork of Berichans Fork of the Sysbot code] - [PkHexCore:9/25/23] [ALM: +DLC -HomeTracker==0] [BuildLocation: X_AW_LT].", "Discord"); //updated each build updated to reflect correct information. 
            

        }

        [Command("logHere")]
        [Summary("Makes the bot log to the channel.")]
        [RequireSudo]
        public async Task AddLogAsync()
        {
            var c = Context.Channel;
            var cid = c.Id;
            if (Channels.TryGetValue(cid, out _))
            {
                await ReplyAsync("Already logging here.").ConfigureAwait(false);
                return;
            }

            AddLogChannel(c, cid, LogUtil.Forwarders);

            // Add to discord global loggers (saves on program close)
            SysCordSettings.Settings.LoggingChannels.AddIfNew(new[] { GetReference(Context.Channel) });
            await ReplyAsync("Added logging output to this channel!").ConfigureAwait(false);
        }

        private static void AddLogChannel(ISocketMessageChannel c, ulong cid, List<Action<string, string>> forwarder)
        {
            void Logger(string msg, string identity)
            {
                try
                {
                    c.SendMessageAsync(GetMessage(msg, identity));
                }

                catch (Exception ex)

                {
                    LogUtil.LogSafe(ex, identity);
                }
            }

            Action<string, string> l = Logger;
            forwarder.Add(l);
            static string GetMessage(string msg, string identity) => $"> [{DateTime.Now:hh:mm:ss}] - {identity}: {msg}";

            var entry = new LogAction(cid, l, c.Name);
            Channels.Add(cid, entry);
        }

        [Command("logInfo")]
        [Summary("Dumps the logging settings.")]
        [RequireSudo]
        public async Task DumpLogInfoAsync()
        {
            foreach (var c in Channels)
                await ReplyAsync($"{c.Key} - {c.Value}").ConfigureAwait(false);
        }

        [Command("logClear")]
        [Summary("Clears the logging settings in that specific channel.")]
        [RequireSudo]
        public async Task ClearLogsAsync()
        {
            var id = Context.Channel.Id;
            if (!Channels.TryGetValue(id, out var log))
            {
                await ReplyAsync("Not echoing in this channel.").ConfigureAwait(false);
                return;
            }
            LogUtil.Forwarders.Remove(log.Action);
            Channels.Remove(Context.Channel.Id);
            SysCordSettings.Settings.LoggingChannels.RemoveAll(z => z.ID == id);
            await ReplyAsync($"Logging cleared from channel: {Context.Channel.Name}").ConfigureAwait(false);
        }

        [Command("logClearAll")]
        [Summary("Clears all the logging settings.")]
        [RequireSudo]
        public async Task ClearLogsAllAsync()
        {
            foreach (var l in Channels)
            {
                var entry = l.Value;
                await ReplyAsync($"Logging cleared from {entry.ChannelName} ({entry.ChannelID}!").ConfigureAwait(false);
                LogUtil.Forwarders.Remove(entry.Action);
            }

            LogUtil.Forwarders.RemoveAll(y => Channels.Select(x => x.Value.Action).Contains(y));
            Channels.Clear();
            SysCordSettings.Settings.LoggingChannels.Clear();
            await ReplyAsync("Logging cleared from all channels!").ConfigureAwait(false);
        }

        private RemoteControlAccess GetReference(IChannel channel) => new()
        {
            ID = channel.Id,
            Name = channel.Name,
            Comment = $"Added by {Context.User.Username} on {DateTime.Now:yyyy.MM.dd-hh:mm:ss}",
        };
    }
}