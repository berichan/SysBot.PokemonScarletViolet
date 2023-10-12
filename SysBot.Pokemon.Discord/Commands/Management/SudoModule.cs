// System Libraries being used
using System;
using System.IO; // added systtem.io for file manipulation
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

//Discord Libraries
using Discord;
using Discord.Commands;
using Discord.Net;    //commented out until we use it, which eventually we weill. 

namespace SysBot.Pokemon.Discord
{
    public class SudoModule : ModuleBase<SocketCommandContext>
    {
        [Command("blacklist")]
        [Summary("Blacklists mentioned user.")]
        [RequireSudo]
        // ReSharper disable once UnusedParameter.Global
        public async Task BlackListUsers([Remainder] string _)
        {
            var users = Context.Message.MentionedUsers;
            var objects = users.Select(GetReference);
            SysCordSettings.Settings.UserBlacklist.AddIfNew(objects);
            await ReplyAsync("Done.").ConfigureAwait(false);
        }

        [Command("blacklistComment")]
        [Summary("Adds a comment for a blacklisted user ID.")]
        [RequireSudo]
        // ReSharper disable once UnusedParameter.Global
        public async Task BlackListUsers(ulong id, [Remainder] string comment)
        {
            var obj = SysCordSettings.Settings.UserBlacklist.List.Find(z => z.ID == id);
            if (obj is null)
            {
                await ReplyAsync($"Unable to find a user with that ID ({id}).").ConfigureAwait(false);
                return;
            }

            var oldComment = obj.Comment;
            obj.Comment = comment;
            await ReplyAsync($"Done. Changed existing comment ({oldComment}) to ({comment}).").ConfigureAwait(false);
        }

        [Command("unblacklist")]
        [Summary("Un-Blacklists mentioned user.")]
        [RequireSudo]
        // ReSharper disable once UnusedParameter.Global
        public async Task UnBlackListUsers([Remainder] string _)
        {
            var users = Context.Message.MentionedUsers;
            var objects = users.Select(GetReference);
            SysCordSettings.Settings.UserBlacklist.RemoveAll(z => objects.Any(o => o.ID == z.ID));
            await ReplyAsync("Done.").ConfigureAwait(false);
        }

        [Command("blacklistId")]
        [Summary("Blacklists IDs. (Useful if user is not in the server).")]
        [RequireSudo]
        public async Task BlackListIDs([Summary("Comma Separated Discord IDs")][Remainder] string content)
        {
            var IDs = GetIDs(content);
            var objects = IDs.Select(GetReference);
            SysCordSettings.Settings.UserBlacklist.AddIfNew(objects);
            await ReplyAsync("Done.").ConfigureAwait(false);
        }

        [Command("unBlacklistId")]
        [Summary("Un-Blacklists IDs. (Useful if user is not in the server).")]
        [RequireSudo]
        public async Task UnBlackListIDs([Summary("Comma Separated Discord IDs")][Remainder] string content)
        {
            var IDs = GetIDs(content);
            SysCordSettings.Settings.UserBlacklist.RemoveAll(z => IDs.Any(o => o == z.ID));
            await ReplyAsync("Done.").ConfigureAwait(false);
        }

        [Command("permcheck")]
        [RequireSudo] // Use RequireSudo attribute
        public async Task PermCheck()
        {
         ulong userId = Context.User.Id;
        // Read the contents of owners.txt
        string ownersFileContent = File.ReadAllText("parameters/owners.txt");
        // Split the content by commas to get an array of owner Discord IDs
        string[] ownerIds = ownersFileContent.Split(',');
        if (ownerIds.Contains(userId.ToString()))
        {
            // You can execute the command logic here for owners
            await ReplyAsync("You are the owner. You can execute this command.");
        }
        else
        {
            // Provide a message for sudo users who are not owners
            await ReplyAsync("You are a sudo user, but not the owner. You cannot execute this command.");
        }
        }


        // Sudo Verification required only
        [Command("removeChannel")]
        [Summary("Removes a channel from the list of channels that are accepting commands.")]
        [RequireSudo] // Changed to allow sudo to execute
        // ReSharper disable once UnusedParameter.Global
        public async Task RemoveChannel()
        {
            var obj = GetReference((IUser)Context.Message.Channel);
            SysCordSettings.Settings.ChannelWhitelist.RemoveAll(z => z.ID == obj.ID);
            await ReplyAsync("Done.").ConfigureAwait(false);
        }


        [Command("blacklistSummary")]
        [Alias("printBlacklist", "blacklistPrint")]
        [Summary("Prints the list of blacklisted users.")]
        [RequireSudo]
        public async Task PrintBlacklist()
        {
            var lines = SysCordSettings.Settings.UserBlacklist.Summarize();
            var msg = string.Join("\n", lines);
            await ReplyAsync(Format.Code(msg)).ConfigureAwait(false);
        }

        [Command("removeAlt")]
        [Alias("removeLog", "rmAlt")]
        [Summary("Removes an identity (name-id) from the local user-to-trader AntiAbuse database")]
        [RequireSudo]
        public async Task RemoveAltAsync([Remainder] string identity)
        {
            if (NewAntiAbuse.Instance.Remove(identity))
                await ReplyAsync($"{identity} has been removed from the database.").ConfigureAwait(false);
            else
                await ReplyAsync($"{identity} is not a valid identity.").ConfigureAwait(false);
        }

        private RemoteControlAccess GetReference(IUser channel) => new()
        {
            ID = channel.Id,
            Name = channel.Username,
            Comment = $"Added by {Context.User.Username} on {DateTime.Now:yyyy.MM.dd-hh:mm:ss}",
        };

        private RemoteControlAccess GetReference(ulong id) => new()
        {
            ID = id,
            Name = "Manual",
            Comment = $"Added by {Context.User.Username} on {DateTime.Now:yyyy.MM.dd-hh:mm:ss}",
        };

        protected static IEnumerable<ulong> GetIDs(string content)
        {
            return content.Split(new[] { ",", ", ", " " }, StringSplitOptions.RemoveEmptyEntries)
                .Select(z => ulong.TryParse(z, out var x) ? x : 0).Where(z => z != 0);
        }
    }
}