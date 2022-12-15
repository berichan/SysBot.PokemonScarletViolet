using System;
using System.Collections.Generic;
using System.Text;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Advanced;
using SixLabors.ImageSharp.Formats;
using SixLabors.ImageSharp.Formats.Png;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing;

namespace SysBot.Base
{
    public class RegionScreenshotComparer : IScreenshotComparer
    {
        public readonly int X, Y, XSize, YSize;
        public readonly Rgba32 ExpectedColor;
        public int DeadZone { get; set; }

        public RegionScreenshotComparer(int x, int y, int xSize, int ySize, Rgba32 expectedColor, int deadZone = 15) // Deadzone in the range 0-255
        { 
            X = x;
            Y = y;
            XSize = xSize;
            YSize = ySize;
            ExpectedColor = expectedColor;
            DeadZone = deadZone;
        }


        public bool Compare(SwitchScreenshot screen)
        {
            var image = screen.SwitchImage;
            var newImage = image.Clone(
                    i => i.Crop(new Rectangle(X, Y, XSize, YSize)));

            int totalR = 0, totalG = 0, totalB = 0;
            int totalPixels = newImage.Width * newImage.Height;
            for (int i = 0; i < newImage.Width; i++)
            {
                for (int j = 0; j < newImage.Height; j++)
                {
                    totalR += newImage[i, j].R;
                    totalG += newImage[i, j].G;
                    totalB += newImage[i, j].B;
                }
            }

            newImage.Dispose();

            var avgR = totalR / totalPixels;
            var avgG = totalG / totalPixels;
            var avgB = totalB / totalPixels;

            if (!(Math.Max(ExpectedColor.R - DeadZone, 0) < avgR && avgR <= Math.Min(ExpectedColor.R + DeadZone, byte.MaxValue)))
                return false;
            if (!(Math.Max(ExpectedColor.G - DeadZone, 0) < avgG && avgG <= Math.Min(ExpectedColor.G + DeadZone, byte.MaxValue)))
                return false;
            if (!(Math.Max(ExpectedColor.B - DeadZone, 0) < avgB && avgB <= Math.Min(ExpectedColor.B + DeadZone, byte.MaxValue)))
                return false;

            return true;
        }

        public int CompareRange(SwitchScreenshot screen)
        {
            var image = screen.SwitchImage;
            var newImage = image.Clone(
                    i => i.Crop(new Rectangle(X, Y, XSize, YSize)));

            int totalR = 0, totalG = 0, totalB = 0;
            int totalPixels = newImage.Width * newImage.Height;
            for (int i = 0; i < newImage.Width; i++)
            {
                for (int j = 0; j < newImage.Height; j++)
                {
                    totalR += newImage[i, j].R;
                    totalG += newImage[i, j].G;
                    totalB += newImage[i, j].B;
                }
            }

            newImage.Dispose();

            var avgR = totalR / totalPixels;
            var avgG = totalG / totalPixels;
            var avgB = totalB / totalPixels;

            int range = 0;
            range += Math.Abs(ExpectedColor.R - avgR);
            range += Math.Abs(ExpectedColor.G - avgG);
            range += Math.Abs(ExpectedColor.B - avgB);

            return range;
        }
    }
}
