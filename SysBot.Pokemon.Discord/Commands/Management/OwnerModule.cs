//Organized into System Libraries and sub Libraries, and Discord
using System;
using System.Linq;
using System.IO;   //we need this for local file manipulation 
using System.Threading.Tasks;

//I'm going to be using a lot of libraries and subliraries that we not initially used in the original project source code -
//These will be required to use parts of my codeusing Discord.WebSocket; // We need this subclass to be able to attach deleted Pk9 files and restore them.
using Discord.Net; // This is to handle deleted message exceptions
using System.Net; // We need this to catch exceptions for deleted messages
using Discord.Commands;
using Discord;
using System.Collections.Generic;

namespace SysBot.Pokemon.Discord
{
    public class OwnerModule : SudoModule
    {

        private List<ulong> ownerIds; // Declare a field to store the loaded owner IDs
       

        [Command("addSudo")]
        [Summary("Adds mentioned user to global sudo")]
        [RequireSudo]
        // ReSharper disable once UnusedParameter.Global
        public async Task SudoUsers([Remainder] string _)
        {
            ownerIds = LoadOwnerIdsFromFile();

            var users = Context.Message.MentionedUsers;
            var objects = users.Select(GetReference);
            SysCordSettings.Settings.GlobalSudoList.AddIfNew(objects);
            await ReplyAsync("Done.").ConfigureAwait(false);
        }
   
        private List<ulong> LoadOwnerIdsFromFile()    
        {
        // Define the path to your owners.txt file
        string ownersFilePath = Path.Combine("parameters", "owners.txt");

        if (File.Exists(ownersFilePath))
        {
            // Read the file and split it by commas to get a list of owner IDs
            string[] ownerIdsStr = File.ReadAllText(ownersFilePath).Split(',');
            // Convert the strings to ulong and return the list of owner IDs
            List<ulong> ownerIds = ownerIdsStr.Select(str => ulong.Parse(str.Trim())).ToList();
            return ownerIds;
         }
    else
    {
        // If the file doesn't exist, create it and make it empty
        File.WriteAllText(ownersFilePath, string.Empty);

        // Return an empty list since there are no owner IDs yet
        return new List<ulong>();;
    }
}

        [Command("removeSudo")]
        [Summary("Removes mentioned user from global sudo")]
        [RequireSudo]
        // ReSharper disable once UnusedParameter.Global
        public async Task RemoveSudoUsers([Remainder] string _)
        {
            
            ulong userId = Context.User.Id;
            // Read the contents of owners.txt
            string ownersFileContent = File.ReadAllText("parameters/owners.txt");
            // Split the content by commas to get an array of owner Discord IDs
            string[] ownerIds = ownersFileContent.Split(',');
            if (ownerIds.Contains(userId.ToString()))
            {
                var users = Context.Message.MentionedUsers;
                var objects = users.Select(GetReference);
                SysCordSettings.Settings.GlobalSudoList.RemoveAll(z => objects.Any(o => o.ID == z.ID));
                await ReplyAsync("Done -Owner Removed Sudo from User ").ConfigureAwait(false);
            }
            else
            {
                await ReplyAsync("You are a sudo user, but not the owner. You cannot execute this command.");
            }
        }


        // Sudo  required
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

        //This ccommand isn't complete yet - Sudo command only currently
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

        // Sudo + Owners.txt Verification method implemented
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

        // Sudo + Owners.txt Verification method implemented
        [Command("leaveguild")] 
        [Alias("lg")]
        [Summary("Leaves guild based on supplied ID.")]
        [RequireSudo] 
        //changed to require sudo and match id in parameters/owner
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

        // Sudo + Owners.txt Verification method implemented
        [Command("leaveall")]
        [Summary("Leaves all servers the bot is currently in.")]
        [RequireSudo] 
        // ReSharper disable once UnusedParameter.Globa
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

        // Sudo + Owners.txt Verification method implemented
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
