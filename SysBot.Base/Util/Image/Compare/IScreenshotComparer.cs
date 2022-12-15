using System;
using System.Collections.Generic;
using System.Text;

namespace SysBot.Base
{
    public interface IScreenshotComparer
    {
        public bool Compare(SwitchScreenshot screen);
        public int CompareRange(SwitchScreenshot screen);
    }
}
