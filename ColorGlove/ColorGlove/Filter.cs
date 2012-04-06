using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using FeatureExtractionLib;

namespace ColorGlove
{
    class Filter
    {

        private static int width = 640, height = 480;
        private static readonly int kColorStride = 4;

        // Copies over the RGB value to the buffer.
        public static void CopyColor(System.Drawing.Rectangle crop, short[] depth, byte[] rgb, byte[] bitmap_bits)
        {
            for (int x = crop.X; x <= crop.Width + crop.X; x++)
            {
                for (int y = crop.Y; y <= crop.Height + crop.Y; y++)
                {
                    int idx = Util.toID(x, y, width, height, kColorStride);
                    Array.Copy(rgb, idx, bitmap_bits, idx, kColorStride);
                }
            }
        }
    }
}
