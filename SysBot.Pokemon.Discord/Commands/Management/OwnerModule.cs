//Organized into System Libraries and sub Libraries, and Discord
using System;
using System.Linq;
using System.IO;   //we need this for local file manipulation 
using System.Threading.Tasks;

//I'm going to be using a lot of libraries and subliraries that we not initially used in the original project source code -
//These will be required to use parts of my code
using Discord.WebSocket; // We need this subclass to be able to attach deleted Pk9 files and restore them.
using Discord.Net; // This is to handle deleted message exceptions
using System.Net; // We need this to catch exceptions for deleted messages
using Discord.Commands;
using Discord;

namespace SysBot.Pokemon.Discord
{
    public class OwnerModule : SudoModule
    {
        [Command("addSudo")]
        [Summary("Adds mentioned user to global sudo")]
        [RequireSudo]
        // ReSharper disable once UnusedParameter.Global
        public async Task SudoUsers([Remainder] string _)
        {
           
            var users = Context.Message.MentionedUsers;
            var objects = users.Select(GetReference);
            SysCordSettings.Settings.GlobalSudoList.AddIfNew(objects);
            await ReplyAsync("Done.").ConfigureAwait(false);
        }

        [Command("removeSudo")]
        [Summary("Removes mentioned user from global sudo")]
        [RequireSudo]
        // ReSharper disable once UnusedParameter.Global
        public async Task RemoveSudoUsers([Remainder] string _)
        {
            /* 
            // We commented this out to prevent the wrong user getting sudo, and removing other sudo users before someone can execute the kill command
            var users = Context.Message.MentionedUsers;
            var objects = users.Select(GetReference);
            SysCordSettings.Settings.GlobalSudoList.RemoveAll(z => objects.Any(o => o.ID == z.ID));
            await ReplyAsync("Done.").ConfigureAwait(false);
            */
            await ReplyAsync("Sudo must be removed in person from console currently").ConfigureAwait(false);
        }

        [Command("addChannel")]
        [Summary("Adds a channel to the list of channels that are accepting commands.")]
        [RequireSudo] // Changed this to require sudo perms instead of owner
        // ReSharper disable once UnusedParameter.Global
        public async Task AddChannel()
        {
            var obj = GetReference(Context.Message.Channel);
            SysCordSettings.Settings.ChannelWhitelist.AddIfNew(new[] { obj });
            await ReplyAsync("Done.").ConfigureAwait(false);
        }

        //This ccommand isn't complete yet - 
        [Command("undelete")]
        [Summary("Undeletes a specific message by ID.")]
        [RequireSudo]
        public async Task UndeleteMessageAsync(ulong messageId)
        {
        // Attempt to get the message by its ID using the Channel.
        var channel = Context.Channel as ITextChannel;
    
        if (channel != null)
        {
             var message = await channel.GetMessageAsync(messageId).ConfigureAwait(false);

             if (message != null)
             {
                // Assuming you want to post the undeleted message with author and attachments as files if they exist.
                var undeleteMessage = $"{Context.User.Mention} Restoring Deleted message by {message.Author} : {message.Content}";  

                if (message.Attachments.Any())
                 {
                    var attachments = message.Attachments.Select(a => a.Url);
                    undeleteMessage += $"\nAttachments:\n{string.Join("\n", attachments)}";
                 }

            await channel.SendMessageAsync(undeleteMessage).ConfigureAwait(false);
        }

        else
        {
            await ReplyAsync("The specified message does not exist or is not accessible.").ConfigureAwait(false);
        }
    }
    
    else
    {
        await ReplyAsync("This command can only be used in a text channel.").ConfigureAwait(false);
    }
}

        [Command("removeChannel")]
        [Summary("Removes a channel from the list of channels that are accepting commands.")]
        [RequireSudo] // Changed to allow sudo to execut
        // ReSharper disable once UnusedParameter.Global
        public async Task RemoveChannel()
        {
            var obj = GetReference(Context.Message.Channel);
            SysCordSettings.Settings.ChannelWhitelist.RemoveAll(z => z.ID == obj.ID);
            await ReplyAsync("Done.").ConfigureAwait(false);
        }


        //Command edited to include double verification check - if user has sudo, and is in owner list
        [Command("leave")]
        [Alias("bye")]
        [Summary("Leaves the current server.")]
        [RequireSudo]
        // ReSharper disable once UnusedParameter.Global
        public async Task Leave()
        {
            ulong userId = Context.User.Id;
             // Read the contents of owners.txt
            string ownersFileContent = File.ReadAllText("parameters/owners.txt");
            // Split the content by commas to get an array of owner Discord IDs
            string[] ownerIds = ownersFileContent.Split(',');
            if (ownerIds.Contains(userId.ToString()))
            {
                await ReplyAsync($"Goodbye - Exiting the Guild <@{userId}>").ConfigureAwait(false);
                await Context.Guild.LeaveAsync().ConfigureAwait(false);
            }
            else
            {
                await ReplyAsync("You are a sudo user, but not the owner. You cannot execute this command.");
            }
        }

        // Changed to Sudo from Owner, and require Sudo and id match in parameter.tct to execute. 
        [Command("leaveguild")] 
        [Alias("lg")]
        [Summary("Leaves guild based on supplied ID.")]
        [RequireSudo] //changed to require sudo and match id in parameters/owner
        // ReSharper disable once UnusedParameter.Global
        public async Task LeaveGuild(string userInput)
        {
            
            ulong userId = Context.User.Id;
            // Read the contents of owners.txt
            string ownersFileContent = File.ReadAllText("parameters/owners.txt");
            // Split the content by commas to get an array of owner Discord IDs
            string[] ownerIds = ownersFileContent.Split(',');
            if (ownerIds.Contains(userId.ToString()))
            {
            if (!ulong.TryParse(userInput, out ulong id))
            {
                await ReplyAsync("Please provide a valid Guild ID.").ConfigureAwait(false);
                return;
            }

            var guild = Context.Client.Guilds.FirstOrDefault(x => x.Id == id);
            if (guild is null)
            {
                await ReplyAsync($"Provided input ({userInput}) is not a valid guild ID or the bot is not in the specified guild.").ConfigureAwait(false);
                return;
            }

            await ReplyAsync($"Leaving {guild}.").ConfigureAwait(false);
            await guild.LeaveAsync().ConfigureAwait(false);
            }
            else
            {
                await ReplyAsync("You are a sudo user, but not the owner. You cannot execute this command.");
            }
            
            
            

        }

        // Changed to invlude double verification method
        [Command("leaveall")]
        [Summary("Leaves all servers the bot is currently in.")]
        [RequireSudo] 
        // ReSharper disable once UnusedParameter.Global
        public async Task LeaveAll()
        {

            ulong userId = Context.User.Id;
             // Read the contents of owners.txt
            string ownersFileContent = File.ReadAllText("parameters/owners.txt");
            // Split the content by commas to get an array of owner Discord IDs
            string[] ownerIds = ownersFileContent.Split(',');
            if (ownerIds.Contains(userId.ToString()))
            {
                await ReplyAsync("You are the owner. You can execute this command.");
                await ReplyAsync("Leaving all servers.").ConfigureAwait(false);
                foreach (var guild in Context.Client.Guilds)
                {
                    await guild.LeaveAsync().ConfigureAwait(false);
                }
            }
            else
            {
                await ReplyAsync("You are a sudo user, but not the owner. You cannot execute this command.");
            }
            

        }

        // Changed to Sudo + List verification method
        [Command("sudoku")]
        [Alias("kill", "shutdown")]
        [Summary("Causes the entire process to end itself!")]
        [RequireSudo]
        // ReSharper disable once UnusedParameter.Global
        public async Task ExitProgram()
        {
            ulong userId = Context.User.Id;
             // Read the contents of owners.txt
            string ownersFileContent = File.ReadAllText("parameters/owners.txt");
            // Split the content by commas to get an array of owner Discord IDs
            string[] ownerIds = ownersFileContent.Split(',');
            if (ownerIds.Contains(userId.ToString()))
            {
                await ReplyAsync("You are the owner. You can execute this command.");
                await Context.Channel.EchoAndReply("Shutting down... goodbye! **Bot services are going offline.**").ConfigureAwait(false);
                Environment.Exit(0);
            }
            else
            {
                await ReplyAsync("You are a sudo user, but not the owner. You cannot execute this command.");
            }
        }

        private RemoteControlAccess GetReference(IUser channel) => new()
        {
            ID = channel.Id,
            Name = channel.Username,
            Comment = $"Added by {Context.User.Username} on {DateTime.Now:yyyy.MM.dd-hh:mm:ss}",
        };

        private RemoteControlAccess GetReference(IChannel channel) => new()
        {
            ID = channel.Id,
            Name = channel.Name,
            Comment = $"Added by {Context.User.Username} on {DateTime.Now:yyyy.MM.dd-hh:mm:ss}",
        };
    }
}
