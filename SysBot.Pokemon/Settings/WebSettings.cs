using System.ComponentModel;

namespace SysBot.Pokemon
{
    public class WebSettings
    {
        private const string Network = nameof(Network);
        public override string ToString() => "Web and Uri Endpoint Settings";

        [Category(Network), Description("HTTP or HTTPS Endpoint")]
        public string URIEndpoint { get; set; } = string.Empty;

        [Category(Network), Description("The Auth ID")]
        public string AuthID { get; set; } = string.Empty;

        [Category(Network), Description("The Auth Token or Password")]
        public string AuthTokenOrString { get; set; } = string.Empty;

        [Category(Network), Description("The index (if any)")]
        public int QueueIndex { get; set; } = -1;

        [Category(Network), Description("The number of seed checks to feed the queue with (will index QueueIndex by 1 for each additional one)")]
        public int SCFeedCount { get; set; } = 1;
    }
}
