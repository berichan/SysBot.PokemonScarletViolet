using System;
using System.Collections.Generic;
using System.Text;

namespace SysBot.Base
{
    public class MultiRangeCompare<T> where T : IScreenshotComparer
    {
        public readonly T[] Comparers;

        public MultiRangeCompare(params T[] comparers)
        {
            Comparers = comparers;
        }

        public bool Compare(SwitchScreenshot screen)
        {
            foreach (var c in Comparers)
                if (!c.Compare(screen))
                    return false;
            return true;
        }
    }
}
