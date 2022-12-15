using System.ComponentModel;

namespace SysBot.Pokemon
{
    public class TwitterSettings
    {
        private const string Authentication = nameof(Authentication);
        private const string Limits = nameof(Limits);

        // Auth

        [Category(Authentication), Description("The Twitter consumer key")]
        public string ConsumerKey { get; set; } = string.Empty;

        [Category(Authentication), Description("The Twitter consumer secret")]
        public string ConsumerSecret { get; set; } = string.Empty;

        [Category(Authentication), Description("The Twitter access token")]
        public string AccessToken { get; set; } = string.Empty;

        [Category(Authentication), Description("The Twitter access token secret")]
        public string AccessTokenSecret { get; set; } = string.Empty;

        // Limits because Twitter API is heavily restricted

        [Category(Limits), Description("The amount of mentions to get in the last 15 second interval.")]
        public int MentionCount { get; set; } = 20;

        [Category(Limits), Description("Ignore all new mentions for this interval if too many of them are new.")]
        public bool AntiAbuseIgnoreMentions { get; set; } = true;

        [Category(Limits), Description("(See above) Ignore all new mentions if the difference from the last interval is higher than this percentage (0 - 1) to prevent abuse.")]
        public float IgnorePercentage { get; set; } = 0.7f;
    }
}
