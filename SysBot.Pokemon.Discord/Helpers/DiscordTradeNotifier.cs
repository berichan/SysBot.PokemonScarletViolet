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
            
            
            
            
            // Declare local variables that derive their values from the Data.IV_() fields.
            var atk = Data.IV_ATK;
            var def = Data.IV_DEF;
            var spd = Data.IV_SPD;
            var spe = Data.IV_SPE;
            var spa = Data.IV_SPA;
            var hp = Data.IV_HP;
            var perfect = $"";
            var total = Data.IVTotal;
            var offby = 186 - total;

            if(total == 186)
            {
                perfect = $":star: Perfect IV's :star";
            }
            else
            {
                perfect = $"This pokemon is missing {offby} to be perfect IV";
            }
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

            /*
            Dictionary<int, string> ballEmotes = new()
            {
                { 1, ":ball_Master:" },
                { 2, ":ball_Ultra:" },
                { 3, ":ball_Great:" },
                { 4, ":ball_Poke:" },
                { 5, ":ball_safari:" },
                { 6, ":ball_net:" },
                { 7, ":ball_dive:" },
                { 8, ":ball_nest:" },
                { 9, ":ball_repeat:" },
                { 10, ":ball_timer:" },
                { 11, ":ball_luxury:" },
                { 12, ":ball_pre:" },
                { 13, ":ball_dusk:" },
                { 14, ":ball_heal:" },
                { 15, ":ball_quick:" },
                { 16, ":ball_cherish:" },
                { 17, ":ball_fast:" },
                { 18, ":ball_level:" },
                { 19, ":ball_lure:" },
                { 20, ":ball_heavy:" },
                { 21, ":ball_love:" },
                { 22, ":ball_friend:" },
                { 23, ":ball_love:" },
                { 24, ":ball_sport:" },
                { 25, ":ball_dream:" },
                { 27, ":ball_strange:" },
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
*/


/*
            // Establish Variables that are set to the different Data.(Field) variables
            
            var item = Data.HeldItem;
            var holding = "";

                //
                if(item < 0)
                {
                    holding = "No item";
                }
                else
                {
                    holding = $"Holding Item # {Data.HeldItem}";
                }
*/
                string holding = $"";
                var ot = Data.OT_Name;               // The IGN of the trainer who requested a trade. 
                var tid = Data.DisplayTID;          //The TID as it appears in game. 
                var stats = $"ATK:{atk} / DEF:{def} / SpD:{spd} / SpA:{spa} / Spe:{spe} / HP:{hp}";       //build the stats straings
                // Check if Data.IsShiny is true and set the shiny string accordingly
                string shiny = Data.IsShiny ? ":sparkles:" : "";
    

                        // If the pokemon traded {(Species)tradedToUser} matches the name of a pokemon in our folder we display that image in the embed - 
                        // This code could be cleaner - Variable for if exists - if yes then embed.WithImageUrl, else nothing, send to channel
                        //if (File.Exists(imagePath))
                        if (File.Exists(imagePath))     
                        {
                            var embed = new EmbedBuilder();
                            var colon = $":";
                            var dream = $"ball_dream";
                            var displaytest = $"{colon}{dream}{colon}";


                            //// Test Code for the Moves. 
                            var moveList = Data.Moves.ToList<ushort>;

                            embed.WithTitle("XGC MEMBER TRADE REQUEST COMPLETED");
                            embed.AddField("Trainer", Trader.Mention, true); // Display trainer's name in an inline field
                            embed.AddField("Received Pokémon", $"{shiny}{(Species)tradedToUser} {holding}", true); // Display received Pokémon with or without shiny indicator
                            embed.AddField("Trainer IG Info", $"OT: {ot} / TID: {tid}",true);
                            embed.AddField("IV Spread", $"{stats}", true);
                            embed.AddField("MOVES #'s", $"{m1}\n{m2}\n{m3}\n{m4}",true); 
                            embed.AddField("Move List" , moveList);    // New line Added to the Embed
                            embed.AddField("Breloom?", $"Breloom override is off, if it was on would you have gotten Breloom instead?{breloom}");

  //                          var mblink = $"data:image/jpeg;base64,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";

                           
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
                                ballEmbed = $"Your Pokemon should be in a Master Ball"; // new format test 
                            }         
                            else
                            {
                                ballEmbed = $"That balls image is not currently available - you recieved a {Data.Ball}";  //Displays the number of the ball if no image.
                                
                            }
                            embed.AddField("BALL", ballEmbed);
  */

                            // Displays a heart on the embed thanking users for using the bot
                            embed.AddField("Thanks for being a member", ":heart:"); 
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

