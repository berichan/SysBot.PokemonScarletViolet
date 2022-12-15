using Tweetinvi.Models;
using System.Linq;
using System;

namespace SysBot.Pokemon.Twitter
{
    /// <summary>
    /// Class for comparing twitter mentions from last interval
    /// </summary>
    public class TwitterMentions
    {
        private ITweet[] MentionedTweets;
        private DateTimeOffset LatestTweet = default!;

        public TwitterMentions(ITweet[] initialMentions)
        {
            MentionedTweets = initialMentions;
            if (MentionedTweets.Length > 0)
                LatestTweet = MentionedTweets[0].CreatedAt;
        }

        /// <summary>
        /// Check for new mentions by passing in the next interval of mentions
        /// </summary>
        /// <param name="mentions">The mention list</param>
        /// <returns>New mentions only after the latest tweet from the last interval</returns>
        public ITweet[] CheckForNewMentions(ITweet[] mentions)
        {
            var newMentions = mentions.Except(MentionedTweets);
            newMentions = newMentions.Where((x) => DateTimeOffset.Compare(LatestTweet, x.CreatedAt) > 0);
            MentionedTweets = mentions;
            if (MentionedTweets.Length > 0)
                LatestTweet = MentionedTweets[0].CreatedAt;
            return newMentions.ToArray();
        }
    }
}
