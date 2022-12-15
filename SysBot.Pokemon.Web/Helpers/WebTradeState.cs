using System;
using System.Collections.Generic;
using System.Text;

namespace SysBot.Pokemon.Web
{
    public enum WebTradeState
    {
        Initialising,
        TypingCode,
        Searching,
        FoundTrainer,
        Canceled,
        Finished,
        Errored
    }
}
