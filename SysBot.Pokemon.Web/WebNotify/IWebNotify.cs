using PKHeX.Core;
using System;
using System.Collections.Generic;
using System.Text;

namespace SysBot.Pokemon.Web
{
    public interface IWebNotify<T> where T : PKM, new()
    {
        void NotifyServerOfSeedInfo(SeedSearchResult r, T result);
        void NotifyServerOfState(WebTradeState state, string trainerName, params KeyValuePair<string, string>[] additionalParams);
    }
}
