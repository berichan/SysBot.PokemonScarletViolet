using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp;
using System;
using System.Collections.Generic;
using System.Text;

namespace SysBot.Base
{
    public class SwitchScreenshot
    {
        public Image<Rgba32> SwitchImage { get; private set; }
        public SwitchScreenshot(byte[] data)
        {
            SwitchImage = LoadImg(data);
        }

        private static Image<Rgba32> LoadImg(byte[] data)
        {
            var img = Image.Load<Rgba32>(data);
            return img;
        }
    }
}
