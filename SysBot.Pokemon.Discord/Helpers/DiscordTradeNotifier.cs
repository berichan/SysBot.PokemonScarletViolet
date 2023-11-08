using Discord;
using Discord.WebSocket;
using PKHeX.Core;
using System;
using System.Linq;
// System.IO was used for local r/w of files on the bot's host system
// Implementation of database reading for embed files, and tracking needs to be edited to be cleaner
// Need to add path fields the winforms for the location of the database folder instead of hard coding
using System.IO;
using Microsoft.Extensions.Primitives;

// Needed for Dictionary Implemenation 
using System.Collections.Generic;
using System.Timers;

namespace SysBot.Pokemon.Discord
{
    public class DiscordTradeNotifier<T> : IPokeTradeNotifier<T> where T : PKM, new()
    {
        public string IdentifierLocator => "Discord";
        private T Data { get; }
        private PokeTradeTrainerInfo Info { get; }
        private int Code { get; }
        private SocketUser Trader { get; }
        private ISocketMessageChannel CommandSentChannel { get; }
        public Action<PokeRoutineExecutor<T>>? OnFinish { private get; set; }
        public int QueueSizeEntry { get; set; }
        public bool ReminderSent { get; set; } = false;

        public readonly PokeTradeHub<T> Hub = SysCord<T>.Runner.Hub;
       
        public DiscordTradeNotifier(T data, PokeTradeTrainerInfo info, int code, SocketUser trader, ISocketMessageChannel commandSentChannel)
        {
            Data = data;
            Info = info;
            Code = code;
            Trader = trader;
            CommandSentChannel = commandSentChannel;

            QueueSizeEntry = Hub.Queues.Info.Count;
        }

        public void TradeInitialize(PokeRoutineExecutor<T> routine, PokeTradeDetail<T> info)
        {
            var receive = Data.Species == 0 ? string.Empty : $" ({Data.Nickname})";
            Trader.SendMessageAsync($"Initializing trade{receive}. Please be ready. Your code is **{Code:0000 0000}**.").ConfigureAwait(false);
        }

        public void TradeSearching(PokeRoutineExecutor<T> routine, PokeTradeDetail<T> info)
        {
            //Ping the user in DM's as well as send them a DM
            //This way it lights up the chatbox as well as alerts the user with an additional ping - 
            //When testing the timing if a user searches exactly when they recieve the ping they will never have a stream sniped trade
            var failping = Data.Species == 0 ? string.Empty : $" ({Data.Nickname})";
            Trader.SendMessageAsync($"Initializing trade{failping}. ```Ping Alert Added by XGC so you don't miss your trades with our bots.```").ConfigureAwait(false);
       
       
           //Reminder of the trade code, Bots IGN, and trade notification 
           //Test formatting 
            var name = Info.TrainerName;
            var trainer = string.IsNullOrEmpty(name) ? string.Empty : $", {name}";
            Trader.SendMessageAsync($"# Hey  {trainer} ! Your code is ``**{Code:0000 0000}**``. My IGN is ``{routine.InGameName}`` - ```I'M SEARCHING FOR YOU IN GAME RIGHT NOW .```").ConfigureAwait(false);
        }

        public void TradeCanceled(PokeRoutineExecutor<T> routine, PokeTradeDetail<T> info, PokeTradeResult msg)
        {
            OnFinish?.Invoke(routine);
            Trader.SendMessageAsync($"Trade canceled: {msg}").ConfigureAwait(false);
            if (msg == PokeTradeResult.NoTrainerWasFound)
                CommandSentChannel.SendMessageAsync($"{Trader.Mention} - Something happened with your trade: {msg}. Please try again, or contact staff for support from: https://discord.com/channels/829181609156411463/1131210656138412122 or https://discord.com/channels/829181609156411463/1117131583984508938 ");
        }

        public async void TradeFinished(PokeRoutineExecutor<T> routine, PokeTradeDetail<T> info, T result)
        {
            OnFinish?.Invoke(routine);
            var tradedToUser = Data.Species;
            var message = tradedToUser != 0 ? $"Trade finished. Enjoy your {(Species)tradedToUser}!" : "Trade finished!";

            // Send the initial trade message
            await Trader.SendMessageAsync(message).ConfigureAwait(false);

            // If return PK files is true send the user a DM with attached PK file. 
            if (result.Species != 0 && Hub.Config.Discord.ReturnPKMs)
            {
                // Send the Pokémon using SendPKMAsync if needed
                await Trader.SendPKMAsync(result, "Here's what you traded me!").ConfigureAwait(false);
            }


            //////////////////////////////////////////////////////////////
            //BEGINNING OF THE FULL  EMBED CODE ADDED TO DISCORDTRADENOTIFIER.CS
            // Define the path to the folder containing PNG files
            var folderPath = @"C:\Users\Xieon\Desktop\repositories\test\SysBot.PokemonScarletViolet\SysBot.Pokemon.WinForms\bin\x64\Debug\net7.0-windows\pkmnpic";
            // Create the full path to the PNG file based on {(Species)tradedToUser}
            var imageName = $"{((Species)tradedToUser).ToString().ToLower()}.png";
            var imagePath = Path.Combine(folderPath, imageName);
 


            // Code for a chance of a Breloom being sent
            Random random = new();
            bool breloom = random.Next(0, 100) < 2; // 2% chance of being true
            
            
            
            
            //stats
            var atk = Data.IV_ATK;
            var def = Data.IV_DEF;
            var spd = Data.IV_SPD;
            var spe = Data.IV_SPE;
            var spa = Data.IV_SPA;
            var hp = Data.IV_HP;
            //moves
            var m1 = Data.Move1;
            var m2 = Data.Move2;
            var m3 = Data.Move3;
            var m4 = Data.Move4;
            var moveset = $"Moves {m1}  /  {m2}  / {m3}  / {m4} "; 

            // Beginning of the ball code - Must be changed to reflect the correct emotes for the discord servers it's operating in
            // Define a dictionary to map integer values to emote strings

            // not used currently string? ballEmote = null; // Declare ballEmote outside of the if statement
            string? ballEmbed = null;
            string? noball = null;

            
            Dictionary<int, string> ballEmotes = new()
            {
                { 1, ":ball_Master:" },
                { 2, ":ball_Ultra:" },
                { 3, ":ball_Great:" },
                { 4, ":ball_Poke:" },
                { 5, ":ball_net:" },
                { 6, ":ball_dive:" },
                { 7, ":ball_nest:" },
                { 8, ":ball_repeat:" },
                { 9, ":ball_timer:" },
                { 10, ":ball_luxury:" },
                { 11, ":ball_pre:" },
                { 12, ":ball_dusk:" },
                { 13, ":ball_heal:" },
                { 14, ":ball_quick:" },
                { 15, ":ball_fast:" },
                { 16, ":ball_Level:" },
                { 17, ":ball_lure:" },
                { 18, ":ball_heavy:" },
                { 19, ":ball_love:" },
                { 20, ":ball_friend:" },
                { 21, ":ball_moon:" },
                { 22, ":ball_dream:" },
                { 23, ":ball_beast:" },
                { 24, ":ball_safari:" },
                { 25, ":ball_cherish:" }
            };

            

               // Random random = new Random()
               // Assuming Data.Ball is an integer representing the ball type

int ballValue = Data.Ball;
    
// Check if the Data.Ball value exists in the dictionary, and if so, retrieve the corresponding emote
if (ballEmotes.TryGetValue(ballValue, out string ballEmoteFromDictionary)) // This w
{
    // Assign the retrieved ball emote to ballEmote
    
    ballEmbed = ballEmoteFromDictionary;
}


else
{
    // Handle the case where Data.Ball does not match any known value
    ballEmbed = noball;
}

var ot = Data.OT_Name;
var tid = Data.DisplayTID;
var stats = $"ATK:{atk} / DEF:{def} / SpD:{spd} / SpA:{spa} / Spe:{spe} / HP:{hp}";

// Check if Data.IsShiny is true and set the shiny string accordingly
string shiny = Data.IsShiny ? ":sparkles:" : "";
    

                        // If the pokemon traded {(Species)tradedToUser} matches the name of a pokemon in our folder we display that image in the embed - 
                        // This code could be cleaner - Variable for if exists - if yes then embed.WithImageUrl, else nothing, send to channel
                        //if (File.Exists(imagePath))
                        if (File.Exists(imagePath))     
                        {
                            var embed = new EmbedBuilder();
                            embed.WithTitle("XGC HAS COMPLETED A MEMBER TRADE REQUEST");
                            embed.AddField("Trainer", Trader.Mention, true); // Display trainer's name in an inline field
                            embed.AddField("Received Pokémon", $"{shiny}{(Species)tradedToUser}", true); // Display received Pokémon with or without shiny indicator
                            embed.AddField("Trainer IG Info", $"OT: {ot} / TID: {tid}");
                            embed.AddField("IV Spread", $"{stats}", true);
                            embed.AddField("MOVES #'s", $"{m1}\n{m2}\n{m3}\n{m4}");
                            embed.AddField("Breloom?", $"Breloom override is off, if it was on would you have gotten Breloom instead?{breloom}");


                           
                            //If the ball requested is in XGC Server - how do we display it as an image in the embed? 
                            /*
                            if(Data.Ball== 7 )//  https://cdn.discordapp.com/emojis/1089614882132996257.webp?size=160&quality=lossless   Nest ball test
                            { 
                                ballEmbed =  $":nest_ball: https://cdn.discordapp.com/emojis/1089614882132996257.webp?size=160&quality=lossless";
                            }                
                            else if(Data.Ball==22)
                            {
                                ballEmbed = $"https://discord.com/channels/829181609156411463/1154192793699369010/1157837932737073172";  //loveball test
                            }
                            else if(Data.Ball ==1)   
                            {
                                ballEmbed = $"Your Pokemon should be in a Master Ball {colon}{dream}{colon}"; // new format test 
                            }         
                            else
                            {
                                ballEmbed = $"That balls image is not currently available - you recieved a {Data.Ball}";  //Displays the number of the ball if no image.
                            }
                            embed.AddField("BALL", ballEmbed);
                            */

                            embed.AddField("Thanks for being a member", ":heart:"); // Display a heart thanking the user for using the bot
                            // Attach the POKEMON.PNG file to the embed as an image
                            embed.WithImageUrl($"attachment://{imageName}");
                            // Send the embed with the attached image in tbalhe same channel where the command was sent
                          await CommandSentChannel.SendFileAsync(imagePath, embed: embed.Build()).ConfigureAwait(false);
                        }
          
            else
            {
                // Create and send embed to display trainer's name and received Pokémon Stats
                var embed = new EmbedBuilder();
                embed.WithTitle("XIEON's GAMING CORNER COMPLETED TRADE");
                embed.AddField("Trainer Name", Trader.Mention, true); // Display trainer's name  
                embed.AddField("Received Pokémon", $"{(Species)tradedToUser}", true); // Display received Pokémon 
                embed.AddField("Trainer Info",$"OT:{ot} / TID:{tid}");
                embed.AddField("IV Spread",$"{stats}",true);
                //embed.AddField("Ball",$"{ballemote}",true);
                embed.AddField("Thanks for being a member", ":heart:"); // Display a heart thanking the user for using the bot
                await CommandSentChannel.SendMessageAsync(embed: embed.Build()).ConfigureAwait(false); // Send the embed in the same channel where the command was sent
            }
        }


        public void SendNotification(PokeRoutineExecutor<T> routine, PokeTradeDetail<T> info, string message)
        {
            Trader.SendMessageAsync(message.TrimStart("SSR")).ConfigureAwait(false);
        }

        public void SendNotification(PokeRoutineExecutor<T> routine, PokeTradeDetail<T> info, PokeTradeSummary message)
        {
            if (message.ExtraInfo is SeedSearchResult r)
            {
                SendNotificationZ3(r);
                return;
            }
            var msg = message.Summary;
            if (message.Details.Count > 0)
                msg += ", " + string.Join(", ", message.Details.Select(z => $"{z.Heading}: {z.Detail}"));
            Trader.SendMessageAsync(msg).ConfigureAwait(false);
        }

        public void SendNotification(PokeRoutineExecutor<T> routine, PokeTradeDetail<T> info, T result, string message)
        {
            if (result.Species != 0 && (Hub.Config.Discord.ReturnPKMs || info.Type == PokeTradeType.Dump))
                Trader.SendPKMAsync(result, message).ConfigureAwait(false);
        }

        private void SendNotificationZ3(SeedSearchResult r)
        {
            var lines = r.ToString();

            var embed = new EmbedBuilder { Color = Color.LighterGrey };
            embed.AddField(x =>
            {
                x.Name = $"Seed: {r.Seed:X16}";
                x.Value = lines;
                x.IsInline = false;
            });
            var msg = $"Here are the details for `{r.Seed:X16}`:";
            Trader.SendMessageAsync(msg, embed: embed.Build()).ConfigureAwait(false);
        }

        public void SendReminder(int position, string message)
        {
            if (ReminderSent)
                return;
            ReminderSent = true;
            Trader.SendMessageAsync($"[Reminder] {Trader.Mention} You are currently position {position} in the queue. Your trade will start soon!");
        }
    }
}

