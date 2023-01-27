using PKHeX.Core;
using PKHeX.Core.AutoMod;
using SysBot.Base;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using TwitchLib.Client;
using TwitchLib.Client.Events;
using TwitchLib.Client.Models;
using TwitchLib.Communication.Clients;
using TwitchLib.Communication.Events;
using TwitchLib.Communication.Models;

namespace SysBot.Pokemon.Twitch
{
    public class TwitchBot<T> where T : PKM, new()
    {
        private const string CommandAddition = "sv";

        internal static PokeTradeHub<T> Hub = default!;
        internal static TradeQueueInfo<T> Info => Hub.Queues.Info;

        internal static readonly List<TwitchQueue<T>> QueuePool = new();
        private readonly TwitchClient client;
        private readonly string Channel;
        private readonly TwitchSettings Settings;
        private readonly TwitchReminderHelper<T> reminderHelper;

        private readonly string MessagePrefix;

        private readonly Dictionary<ulong, DateTime> UsedLastMap = new();

        public static bool CanQueueTwitch { get; set; } = true;

        public TwitchBot(TwitchSettings settings, PokeTradeHub<T> hub)
        {
            Hub = hub;
            Settings = settings;

            var credentials = new ConnectionCredentials(settings.Username.ToLower(), settings.Token);

            var clientOptions = new ClientOptions
            {
                MessagesAllowedInPeriod = settings.ThrottleMessages,
                ThrottlingPeriod = TimeSpan.FromSeconds(settings.ThrottleSeconds),

                WhispersAllowedInPeriod = settings.ThrottleWhispers,
                WhisperThrottlingPeriod = TimeSpan.FromSeconds(settings.ThrottleWhispersSeconds),

                // message queue capacity is managed (10_000 for message & whisper separately)
                // message send interval is managed (50ms for each message sent)
            };

            Channel = settings.Channel;
            WebSocketClient customClient = new(clientOptions);
            client = new TwitchClient(customClient);

            var cmd = settings.CommandPrefix;
            client.Initialize(credentials, Channel, cmd, cmd);

            client.OnLog += Client_OnLog;
            client.OnJoinedChannel += Client_OnJoinedChannel;
            client.OnMessageReceived += Client_OnMessageReceived;
            client.OnWhisperReceived += Client_OnWhisperReceived;
            client.OnChatCommandReceived += Client_OnChatCommandReceived;
            client.OnWhisperCommandReceived += Client_OnWhisperCommandReceived;
            client.OnConnected += Client_OnConnected;
            client.OnDisconnected += Client_OnDisconnected;
            client.OnLeftChannel += Client_OnLeftChannel;

            client.OnMessageSent += (_, e)
                => LogUtil.LogText($"[{client.TwitchUsername}] - Message Sent in {e.SentMessage.Channel}: {e.SentMessage.Message}");
            client.OnWhisperSent += (_, e)
                => LogUtil.LogText($"[{client.TwitchUsername}] - Whisper Sent to @{e.Receiver}: {e.Message}");

            client.OnMessageThrottled += (_, e)
                => LogUtil.LogError($"Message Throttled: {e.Message}", "TwitchBot");
            client.OnWhisperThrottled += (_, e)
                => LogUtil.LogError($"Whisper Throttled: {e.Message}", "TwitchBot");

            client.OnError += (_, e) =>
                LogUtil.LogError(e.Exception.Message + Environment.NewLine + e.Exception.StackTrace, "TwitchBot");
            client.OnConnectionError += (_, e) =>
                LogUtil.LogError(e.BotUsername + Environment.NewLine + e.Error.Message, "TwitchBot");

            client.Connect();

            EchoUtil.Forwarders.Add(msg => client.SendMessage(Channel, msg));

            MessagePrefix = PokeDataUtil.FileExtensionToGame(typeof(T)) ?? string.Empty;

            reminderHelper = new TwitchReminderHelper<T>(client, hub.Config);

            // Turn on if verified
            // Hub.Queues.Forwarders.Add((bot, detail) => client.SendMessage(Channel, $"{bot.Connection.Name} is now trading (ID {detail.ID}) {detail.Trainer.TrainerName}"));
        }

        public void StartingDistribution(string message)
        {
            Task.Run(async () =>
            {
                client.SendMessage(Channel, "5...");
                await Task.Delay(1_000).ConfigureAwait(false);
                client.SendMessage(Channel, "4...");
                await Task.Delay(1_000).ConfigureAwait(false);
                client.SendMessage(Channel, "3...");
                await Task.Delay(1_000).ConfigureAwait(false);
                client.SendMessage(Channel, "2...");
                await Task.Delay(1_000).ConfigureAwait(false);
                client.SendMessage(Channel, "1...");
                await Task.Delay(1_000).ConfigureAwait(false);
                if (!string.IsNullOrWhiteSpace(message))
                    client.SendMessage(Channel, message);
            });
        }

        private bool AddToTradeQueue(T pk, int code, OnWhisperReceivedArgs e, RequestSignificance sig, PokeRoutineType type, int strLen, out string msg)
        {
            // var user = e.WhisperMessage.UserId;
            var userID = ulong.Parse(e.WhisperMessage.UserId);
            var name = e.WhisperMessage.DisplayName;

            var trainer = new PokeTradeTrainerInfo(name, ulong.Parse(e.WhisperMessage.UserId));
            var notifier = new TwitchTradeNotifier<T>(pk, trainer, code, e.WhisperMessage.Username, client, Channel, Hub.Config.Twitch, Info.Count + 1);
            notifier.ReminderHelper = reminderHelper;
            var tt = type == PokeRoutineType.SeedCheck ? PokeTradeType.Seed : PokeTradeType.Specific;
            var detail = new PokeTradeDetail<T>(pk, trainer, notifier, tt, code, sig == RequestSignificance.Favored);
            var trade = new TradeEntry<T>(detail, userID, type, name);

            var added = Info.AddToTradeQueue(trade, userID, sig == RequestSignificance.Owner);

            if (added == QueueResultAdd.AlreadyInQueue)
            {
                msg = $"@{name}: Sorry, you are already in the queue.";
                return false;
            }

            var position = Info.CheckPosition(userID);
            notifier.QueueSizeEntry = position.Position;
            msg = $"@{name}: Added to the {type} queue, unique ID: {detail.ID}. Current Position: {position.Position}";

            var botct = Info.Hub.Bots.Count;
            if (position.Position > botct)
            {
                var eta = Info.Hub.Config.Queues.EstimateDelay(position.Position, botct);
                msg += $". Estimated: {eta:F1} minutes.";
            }

            if (strLen < 8) msg += " Your code was not 8 digits as requested, ensure you've added 0s at the beginning of it when searching.";
            if (strLen > 8) msg += " Your code was not 8 digits as requested, ensure you only search for the first 8 digits.";
            return true;
        }

        private void Client_OnLog(object? sender, OnLogArgs e)
        {
            if (e.Data.Contains("Reconnecting"))
                return;
            LogUtil.LogText($"[{client.TwitchUsername}] -[{e.BotUsername}] {e.Data}");
        }

        private void Client_OnConnected(object? sender, OnConnectedArgs e)
        {
            LogUtil.LogText($"[{client.TwitchUsername}] - Connected {e.AutoJoinChannel} as {e.BotUsername}");
        }

        private void Client_OnDisconnected(object? sender, OnDisconnectedEventArgs e)
        {
            LogUtil.LogText($"[{client.TwitchUsername}] - Disconnected.");
            while (!client.IsConnected)
                client.Reconnect();
        }

        private void Client_OnJoinedChannel(object? sender, OnJoinedChannelArgs e)
        {
            LogUtil.LogInfo($"Joined {e.Channel}", e.BotUsername);
            client.SendMessage(e.Channel, "Connection routine has completed.");
        }

        private void Client_OnMessageReceived(object? sender, OnMessageReceivedArgs e)
        {
            LogUtil.LogText($"[{client.TwitchUsername}] - Received message: @{e.ChatMessage.Username}: {e.ChatMessage.Message}");
            if (client.JoinedChannels.Count == 0)
                client.JoinChannel(e.ChatMessage.Channel);
        }

        private void Client_OnLeftChannel(object? sender, OnLeftChannelArgs e)
        {
            LogUtil.LogText($"[{client.TwitchUsername}] - Left channel {e.Channel}");
            client.JoinChannel(e.Channel);
        }

        private void Client_OnChatCommandReceived(object? sender, OnChatCommandReceivedArgs e)
        {
            if (!Hub.Config.Twitch.AllowCommandsViaChannel || Hub.Config.Twitch.UserBlacklist.Contains(e.Command.ChatMessage.Username))
                return;

            var msg = e.Command.ChatMessage;
            var c = e.Command.CommandText.ToLower();
            var args = e.Command.ArgumentsAsString;
            var response = HandleCommand(msg, c, args, false);
            if (response.Length == 0)
                return;

            var channel = e.Command.ChatMessage.Channel;
            client.SendMessage(channel, $"|{MessagePrefix}| " + response);
        }

        private void Client_OnWhisperCommandReceived(object? sender, OnWhisperCommandReceivedArgs e)
        {
            if (!Hub.Config.Twitch.AllowCommandsViaWhisper || Hub.Config.Twitch.UserBlacklist.Contains(e.Command.WhisperMessage.Username))
                return;

            var msg = e.Command.WhisperMessage;
            var c = e.Command.CommandText.ToLower();
            var args = e.Command.ArgumentsAsString;
            var response = HandleCommand(msg, c, args, true);
            if (response.Length == 0)
                return;

            client.SendWhisper(msg.Username, response);
        }

        private string HandleCommand(TwitchLibMessage m, string c, string args, bool whisper)
        {
            bool sudo() => m is ChatMessage ch && (ch.IsBroadcaster || Settings.IsSudo(m.Username));
            bool subscriber() => m is ChatMessage {IsSubscriber: true};

            if (args.StartsWith("<") && args.EndsWith(">"))
                args = args[1..^1];

            switch (c)
            {
                // User Usable Commands
                case "trade":
                case CommandAddition + "trade":
                case "trade" + CommandAddition:
                    HandleUsage(ulong.Parse(m.UserId));
                    _ = TwitchCommandsHelper<T>.AddToWaitingList(args, m.DisplayName, m.Username, ulong.Parse(m.UserId), subscriber(), out string msg);
                    return msg;
                case "ts":
                case "queue":
                case "position":
                case CommandAddition + "ts":
                case CommandAddition + "queue":
                case CommandAddition + "position":
                    var pString = Info.GetPositionString(ulong.Parse(m.UserId), out var isInQueue);
                    if (isInQueue || HasRecentlyUsedBot(ulong.Parse(m.UserId)))
                        return $"@{m.Username}: {pString}";
                    return string.Empty;
                case "tc":
                case "cancel":
                case "remove":
                case CommandAddition + "tc":
                case CommandAddition + "cancel":
                case CommandAddition + "remove":
                    var cString = TwitchCommandsHelper<T>.ClearTrade(ulong.Parse(m.UserId), out var inQueue);
                    if (inQueue || HasRecentlyUsedBot(ulong.Parse(m.UserId)))
                        return $"@{m.Username}: {cString}";
                    return string.Empty;
                case "code" when whisper:
                    return TwitchCommandsHelper<T>.GetCode(ulong.Parse(m.UserId));

                // Sudo Only Commands
                case CommandAddition + "tca" when !sudo():
                case CommandAddition + "pr" when !sudo():
                case CommandAddition + "pc" when !sudo():
                case CommandAddition + "tt" when !sudo():
                case CommandAddition + "tcu" when !sudo():
                case CommandAddition + "ttv" when !sudo():
                    return "This command is locked for sudo users only!";

                case CommandAddition + "tca":
                    Info.ClearAllQueues();
                    return "Cleared all queues!";

                case CommandAddition + "pr":
                    return Info.Hub.Ledy.Pool.Reload(Hub.Config.Folder.DistributeFolder) ? $"Reloaded from folder. Pool count: {Info.Hub.Ledy.Pool.Count}" : "Failed to reload from folder.";

                case CommandAddition + "pc":
                    return $"The pool count is: {Info.Hub.Ledy.Pool.Count}";

                case CommandAddition + "tt":
                    return Info.Hub.Queues.Info.ToggleQueue()
                        ? "Users are now able to join the trade queue."
                        : "Changed queue settings: **Users CANNOT join the queue until it is turned back on.**";

                case CommandAddition + "tcu":
                    return TwitchCommandsHelper<T>.ClearTrade(args);

                case CommandAddition + "ttv":
                    CanQueueTwitch = !CanQueueTwitch;
                    return CanQueueTwitch ? "New Twitch orders are ON" : "New Twitch orders are OFF";

                default:
                    if (c.Contains("trade") || c.Contains("request"))
                        RemoveRecentUserIfExists(ulong.Parse(m.UserId));
                    return string.Empty;
            }
        }

        private void Client_OnWhisperReceived(object? sender, OnWhisperReceivedArgs e)
        {
            LogUtil.LogText($"[{client.TwitchUsername}] - @{e.WhisperMessage.Username}: {e.WhisperMessage.Message}");
            if (QueuePool.Count > 100)
            {
                var removed = QueuePool[0];
                QueuePool.RemoveAt(0); // First in, first out
                //client.SendMessage(Channel, $"Removed @{removed.DisplayName} ({(Species)removed.Pokemon.Species}) from the waiting list: You did not whisper.");
            }

            var user = QueuePool.FindLast(q => q.UserName == e.WhisperMessage.Username);
            if (user == null)
                return;
            QueuePool.Remove(user);
            var msg = e.WhisperMessage.Message;
            try
            {
                int code = ExtractNumber(msg, out int len);
                if (code < 1)
                    code = Util.ToInt32(msg);
                var sig = GetUserSignificance(user);
                AddToTradeQueue(user.Pokemon, code, e, sig, PokeRoutineType.LinkTrade, len, out string message);
                client.SendMessage(Channel, message);
            }

            catch (Exception ex)

            {
                LogUtil.LogSafe(ex, nameof(TwitchBot<T>));
                LogUtil.LogError($"{ex.Message}", nameof(TwitchBot<T>));
            }
        }

        private RequestSignificance GetUserSignificance(TwitchQueue<T> user)
        {
            var name = user.UserName;
            if (name == Channel)
                return RequestSignificance.Owner;
            if (Settings.IsSudo(user.UserName))
                return RequestSignificance.Favored;
            return user.IsSubscriber ? RequestSignificance.Favored : RequestSignificance.None;
        }

        private void HandleUsage(ulong user)
        {
            if (UsedLastMap.ContainsKey(user))
                UsedLastMap[user] = DateTime.Now;
            else
                UsedLastMap.Add(user, DateTime.Now);
        }

        private bool HasRecentlyUsedBot(ulong user)
        {
            if (!UsedLastMap.ContainsKey(user))
                return false;

            var recentlyUsed = DateTime.Now - UsedLastMap[user];
            if (recentlyUsed.TotalHours < 1)
                return true;
            else
                UsedLastMap.Remove(user);

            return false;
        }

        private void RemoveRecentUserIfExists(ulong user)
        {
            if (UsedLastMap.ContainsKey(user))
                UsedLastMap.Remove(user);
        }

        private int ExtractNumber(string str1, out int strLen) // We want the length so we can tell people off for not using 8-digit codes
        {
            string str2 = string.Empty;
            for (int i = 0; i < str1.Length; i++)
            {
                if (char.IsDigit(str1[i]))
                    str2 += str1[i];
            }
            strLen = str2.Length;
            if (str2.Length > 0)
                if (int.TryParse(str2, out var val))
                    return val;
            return -1;
        }
    }
}
