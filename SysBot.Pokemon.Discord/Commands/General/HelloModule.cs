using Discord;
using Discord.Commands;
using System.Linq; // Include the System.Linq namespace.
using System.Threading.Tasks;
using System.IO; //Weed need System.IO to r/w local system files such as "rules.txt"

namespace SysBot.Pokemon.Discord
{
    public class HelloModule : ModuleBase<SocketCommandContext>
    {
        [Command("hello")]
        [Alias("hi")]
        [Summary("Say hello to the bot and get a response.")]
        public async Task PingAsync()
        {
            var str = SysCordSettings.Settings.HelloResponse;
            var msg = string.Format(str, Context.User.Mention);
            await ReplyAsync(msg).ConfigureAwait(false);
        }

        
    
        [Command("rules")]
        [Summary("Get the server rules.")]
        public async Task RulesAsync()
        {
            string folderPath = "variables";
            string rulesFilePath = Path.Combine(folderPath, "rules.txt");

            // Ensure the folder exists, create it if it doesn't.
            if (!Directory.Exists(folderPath))
            {
                Directory.CreateDirectory(folderPath);
            }

            if (!File.Exists(rulesFilePath))
            {
                // If the file doesn't exist, create it with the specified content.
                File.WriteAllText(rulesFilePath, "Rules File not Updated Yet");
            }

            string rulesContent = File.ReadAllText(rulesFilePath);

            var embed = new EmbedBuilder
            {
                Title = "Server Rules",
                Description = rulesContent,
                Color = new Color(0, 255, 0), // Green success commmand embed - 
            };

            await ReplyAsync(embed: embed.Build());
        }
                    
        
    } 
}