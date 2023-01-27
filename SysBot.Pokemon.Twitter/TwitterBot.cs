using PKHeX.Core;
using SysBot.Base;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Tweetinvi;
using Tweetinvi.Models;
using Tweetinvi.Core.Exceptions;
using Tweetinvi.Parameters;
using System.Threading;

namespace SysBot.Pokemon.Twitter
{
    public class TwitterBot
    {
        private static PokeTradeHub<PK8> Hub = default!;
        internal static TradeQueueInfo<PK8> Info => Hub.Queues.Info;

        internal static readonly List<TwitterQueue> QueuePool = new List<TwitterQueue>();
        private readonly TwitterClient client;
        private readonly TwitterSettings Settings;
        private readonly IAuthenticatedUser TwitterUser = default!;
        private readonly GetMentionsTimelineParameters MentionParams = default!;

        private readonly TwitterMentions? localMentions = default!;

        public TwitterBot(TwitterSettings settings, PokeTradeHub<PK8> hub)
        {
            Hub = hub;
            Settings = settings;

            client = new TwitterClient(Settings.ConsumerKey, Settings.ConsumerSecret, Settings.AccessToken, Settings.AccessTokenSecret);

            client.Events.OnTwitterException += HandleTwitterException;

            try
            {
                var taskAuthenticate = Task.Run(async() => await client.Users.GetAuthenticatedUserAsync());
                TwitterUser = taskAuthenticate.Result;
                LogUtil.LogText($"Successfully authenticated as: @{TwitterUser.ScreenName}");

                MentionParams = new GetMentionsTimelineParameters { PageSize = Settings.MentionCount };
                var taskGetInitialMentions = Task.Run(async () => await client.Timelines.GetMentionsTimelineAsync(MentionParams));
                localMentions = new TwitterMentions(taskGetInitialMentions.Result);
                Task.Run(() => CheckMentionsForTradesAsync(localMentions, CancellationToken.None)); 
            }
            catch (Exception e)
            {
                LogUtil.LogError($"Unable to authenticate with error: {e.Message}", nameof(TwitterBot));
            }
        }

        public async Task CheckMentionsForTradesAsync(TwitterMentions mentions, CancellationToken token)
        {
            // Check for new mentions every 15 seconds (60 per 15 mins) to avoid rate limit
            while (!token.IsCancellationRequested)
            {
                // handle new requests for trades

                var currentMentions = await client.Timelines.GetMentionsTimelineAsync(MentionParams);
                var newMentions = mentions.CheckForNewMentions(currentMentions);
                if (newMentions.Length > 0)
                    foreach (var tweet in newMentions)
                        await HandleNewMention(tweet);

                // handle twitter queue via DMs



                await Task.Delay(15_000, token).ConfigureAwait(false);
            }
        }

        private async Task<bool> HandleNewMention(ITweet tweet)
        {
            // extract pokemon name
            var tweetBody = tweet.Text.Replace($"@{TwitterUser.ScreenName}", string.Empty).TrimStart().ToLower();
            if (tweetBody.StartsWith("trade "))
            {
                var set = tweetBody.Substring("trade ".Length);
                var success = TwitterCommandsHelper.AddToWaitingList(set, tweet.CreatedBy.ScreenName, out var msg, out var pk8);

                if (success)
                {
                    var relationship = await client.Users.GetRelationshipBetweenAsync(TwitterUser, tweet.CreatedBy);
                    if (!relationship.CanSendDirectMessage)
                    {
                        await ReplyToTweetAsync(tweet, $"@{tweet.CreatedBy} I cannot DM you, either follow me or ensure you can accept direct messages. You are now on cooldown.");
                        return false;
                    }
                    else
                    {
                        await ReplyToTweetAsync(tweet, msg);
                        return true;
                    }
                }
            }

            return false;
        }

        private void HandleTwitterException(object? sender, ITwitterException e)
        {
            LogUtil.LogError($"({e.CreationDate}) {e.Content}", nameof(TwitterBot));
        }

        private bool AddToTradeQueue(PK8 pk8, int code, IUser user, RequestSignificance sig, PokeRoutineType type, out string msg)
        {
            // var user = e.WhisperMessage.UserId;
            var userID = (ulong)user.Id;
            var name = user.ScreenName;

            var trainer = new PokeTradeTrainerInfo(name);
            var notifier = new TwitterTradeNotifier<PK8>(pk8, trainer, code, name, client, user, Hub.Config.Twitter);
            var tt = type == PokeRoutineType.SeedCheck ? PokeTradeType.Seed : PokeTradeType.Specific;
            var detail = new PokeTradeDetail<PK8>(pk8, trainer, notifier, tt, code, sig == RequestSignificance.Favored);
            var trade = new TradeEntry<PK8>(detail, userID, type, name);

            var added = Info.AddToTradeQueue(trade, userID, sig == RequestSignificance.Favored);

            if (added == QueueResultAdd.AlreadyInQueue)
            {
                msg = $"@{name}: Sorry, you are already in the queue.";
                return false;
            }

            var position = Info.CheckPosition(userID, type);
            msg = $"@{name}: Added to the {type} queue, unique ID: {detail.ID}. Current Position: {position.Position}";

            var botct = Info.Hub.Bots.Count;
            if (position.Position > botct)
            {
                var eta = Info.Hub.Config.Queues.EstimateDelay(position.Position, botct);
                msg += $". Estimated: {eta:F1} minutes.";
            }
            return true;
        }

        private bool HandleWhisper(IUser messagingUser, string messageBody)
        {
            LogUtil.LogText($"[{TwitterUser.ScreenName}] - @{messagingUser.ScreenName}: {messageBody}");
            if (QueuePool.Count > 10)
            {
                var removed = QueuePool[0];
                QueuePool.RemoveAt(0); // First in, first out
                LogUtil.LogText($"Removed @{removed.DisplayName} ({(Species)removed.Pokemon.Species}) from the waiting list: stale request.");
            }

            var user = QueuePool.FindLast(q => q.UserName == messagingUser.ScreenName);
            if (user == null)
            {
                SendDirectMessage("Sorry, you are not in the queue. Please @ me another request publicly. Have a nice day!", messagingUser.Id);
                return false;
            }
            QueuePool.Remove(user);
            var msg = messageBody;
            try
            {
                int code = Util.ToInt32(msg);
                //var sig = GetUserSignificance(user);
                var _ = AddToTradeQueue(user.Pokemon, code, messagingUser, RequestSignificance.None, PokeRoutineType.LinkTrade, out string message);
                SendDirectMessage(message, messagingUser.Id);
            }

            catch (Exception ex)

            {
                LogUtil.LogError($"{ex.Message}", nameof(TwitterBot));
                return false;
            }

            return true;
        }

        private async Task<ITweet?> ReplyToTweetAsync(ITweet tweet, string reply)
        {
            var serverReply = await client.Tweets.PublishTweetAsync(new PublishTweetParameters(reply)
            {
                InReplyToTweet = tweet
            });
            LogUtil.LogText($"Tweeted [{reply}] to {tweet.CreatedBy.ScreenName}");
            return serverReply;
        }

        private IMessage? SendDirectMessage(string msg, long messagingUserId)
        {
            var serverReply = Task.Run(async () => await client.Messages.PublishMessageAsync(new PublishMessageParameters(msg, messagingUserId))).Result;
            LogUtil.LogText($"DMed [{msg}] to {messagingUserId}");
            return serverReply;
        }

        /// <summary>
        /// Gets the limit of all functionality used by this app
        /// </summary>
        /// <returns>Remaining limit of the value closest to 0</returns>
        /*private async Task<int> GetTopLimitAsync()
        {
            var rateLimits = await client.RateLimits.GetRateLimitsAsync();
            int directMessageLimitList = rateLimits.DirectMessagesListLimit.Remaining;
            int directMessageLimitShow = rateLimits.DirectMessagesShowLimit.Remaining;
        }*/
    }
}
