using PKHeX.Core;
using System;
using System.Collections.Generic;
using System.Text;

namespace SysBot.Pokemon.Twitter
{
    public static class TwitterCommandsHelper
    {
        // Helper functions for commands
        public static bool AddToWaitingList(string setstring, string username, out string msg, out PKM pkm)
        {
            pkm = default!;

            if (!TwitterBot.Info.GetCanQueue())
            {
                msg = "Sorry, I am not currently accepting queue requests!";
                return false;
            }

            var set = ShowdownUtil.ConvertToShowdown(setstring);
            if (set == null)
            {
                msg = $"Skipping trade, @{username}: Empty nickname provided for the species.";
                return false;
            }
            var template = AutoLegalityWrapper.GetTemplate(set);
            if (template.Species < 1)
            {
                msg = $"Skipping trade, @{username}: Please read what you are supposed to type as the command argument.";
                return false;
            }

            if (set.InvalidLines.Count != 0)
            {
                msg = $"Skipping trade, @{username}: Unable to parse Showdown Set:\n{string.Join("\n", set.InvalidLines)}";
                return false;
            }

            var sav = AutoLegalityWrapper.GetTrainerInfo(PKX.Generation);
            pkm = sav.GetLegal(template, out var result);

            if (!pkm.CanBeTraded())
            {
                msg = $"Skipping trade, @{username}: Provided Pokémon content is blocked from trading!";
                return false;
            }

            var valid = new LegalityAnalysis(pkm).Valid;
            if (valid && pkm is PK8 pk8)
            {
                var tq = new TwitterQueue(pk8, new PokeTradeTrainerInfo(username), username);
                TwitterBot.QueuePool.RemoveAll(z => z.UserName == username); // remove old requests if any
                TwitterBot.QueuePool.Add(tq);
                msg = $"@{username} - added to the waiting list. Please DM a 8-digit trade code to me! Your request from the waiting list will be removed if you are too slow!";
                return true;
            }

            var reason = result == "Timeout" ? "Set took too long to generate." : "Unable to legalize the Pokémon.";
            msg = $"Skipping trade, @{username}: {reason}";
            return false;
        }
    }
}
