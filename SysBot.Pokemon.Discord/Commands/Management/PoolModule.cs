using Discord;
using Discord.Commands;
using PKHeX.Core;
using System;
using System.IO;
using System.Linq;
using System.Text; // Used for creating combined Stringe. 
using System.Threading.Tasks; 

namespace SysBot.Pokemon.Discord
{
    [Summary("Distribution Pool Module")]
    public class PoolModule<T> : ModuleBase<SocketCommandContext> where T : PKM, new()
    {
        private object msg;
        private object options;

        [Command("poolReload")]
        [Summary("Reloads the bot pool from the setting's folder.")]
        [RequireSudo]
        public async Task ReloadPoolAsync()
        {
            var me = SysCord<T>.Runner;
            var hub = me.Hub;

            var pool = hub.Ledy.Pool.Reload(hub.Config.Folder.DistributeFolder);
            if (!pool)
                await ReplyAsync("Failed to reload from folder.").ConfigureAwait(false);
            else
                await ReplyAsync($"Reloaded from folder. Pool count: {hub.Ledy.Pool.Count}").ConfigureAwait(false);
        }

            //New command for displaying the pool. 
            [Command("pool")]
            [Summary("Displays the details of Pokémon in the random pool.")]
            public async Task DisplayPoolCountAsync()
            {
                var me = SysCord<T>.Runner;
                var hub = me.Hub;
                var pool = hub.Ledy.Pool;
                var count = pool.Count;

                if (count > 0)
                {
                    var total = count;

                    // Initialize a counter to keep track of displayed Pokémon.
                    var displayedPokémon = 0;

                    var embed = new EmbedBuilder();
                    var pokémonInformation = new StringBuilder();

                    while (count > 0 && displayedPokémon < 10) // Continue until 20 Pokémon are displayed.
                    {
                        var lines = pool.Files.Select((z, i) => $"{i + 1:00}: {z.Key} = {(Species)z.Value.RequestInfo.Species}");
                        var msg = string.Join("\n", lines);

                        pokémonInformation.AppendLine($"Count: {count}");
                        pokémonInformation.AppendLine(msg);

                        count--;
                        displayedPokémon++; // Increment the counter.
                    }

                    embed.AddField("Total Pokémon in Pool", total, true);
                    embed.AddField("Pokémon Information", pokémonInformation.ToString(), false);

                    await ReplyAsync("Pool Details", embed: embed.Build()).ConfigureAwait(false);
                }
                else
                {
                    await ReplyAsync($"Pool Count: {count}").ConfigureAwait(false);
                }
            }
                
                [Command("poolpic")]
                [Summary("Displays the first 10 Pokémon images in the pool.")]
                public async Task DisplayPoolPicturesAsync(params string[] pokemonNames)
                {
                    var me = SysCord<T>.Runner;
                    var hub = me.Hub;
                    _ = hub.Ledy.Pool;

                    var embed = new EmbedBuilder();
                    embed.Title = "Pool Pictures";

                    var displayedPokémon = 0;

                    foreach (var pokemonName in pokemonNames)
                    {
                        if (displayedPokémon >= 10)
                        {
                            break; // Limit to 10 thumbnails.
                        }

                        var imagePath = $".\\pkmnpic\\{pokemonName}.png"; // Adjust the path as needed.

                        if (File.Exists(imagePath))
                        {
                            var thumbnailUrl = $"attachment://{pokemonName}.png";
                            var imageStream = new FileStream(imagePath, FileMode.Open, FileAccess.Read);
                            embed.AddField($"The First 10 Pokemon in the XGC Current Distro",$"The first 10 Pokemon", true);
                            // Add the Pokémon name as a field.
                            embed.AddField("Pokémon", pokemonName);

                            // Set the thumbnail for the embed.
                            embed.WithThumbnailUrl(thumbnailUrl);

                            displayedPokémon++; // Increment the counter.
                        }
                    }

                    await ReplyAsync(embed: embed.Build());
                }


        private Task ReplyAsync(Embed embed, bool isTTS, object options, bool isEphemeral, object allowedMentions, object value)
        {
            throw new NotImplementedException();
        }
    }
}