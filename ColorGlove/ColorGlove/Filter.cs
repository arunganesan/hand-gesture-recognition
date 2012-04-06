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
        private static readonly int kColorStride = 4, kDepthStride = 1;



        /*********************************/
        /* FILTER FUNCTIONS              */
        /*********************************/
        #region Filter functions
        
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


        // Copies over the depth value to the buffer, normalized for the range of shorts.
        public static void CopyDepth(System.Drawing.Rectangle crop, short[] depth, byte[] rgb, byte[] bitmap_bits)
        {
            for (int x = crop.X; x <= crop.Width + crop.X; x++)
            {
                for (int y = crop.Y; y <= crop.Height + crop.Y; y++)
                {
                    int idx = Util.toID(x, y, width, height, kDepthStride);
                    bitmap_bits[4 * idx] =
                    bitmap_bits[4 * idx + 1] =
                    bitmap_bits[4 * idx + 2] =
                    bitmap_bits[4 * idx + 3] = (byte)(255 * (short.MaxValue - depth[idx]) / short.MaxValue);
                }
            }
        }

        // Calls helper function with white.
        public static void PaintWhite(System.Drawing.Rectangle crop, short[] depth, byte[] rgb, byte[] bitmap_bits) {
            Paint(crop, bitmap_bits, System.Drawing.Color.White);
        }

        // Calls helper function with green.
        public static void PaintGreen(System.Drawing.Rectangle crop, short[] depth, byte[] rgb, byte[] bitmap_bits)
        {
            Paint(crop, bitmap_bits, System.Drawing.Color.PaleGreen);
        }

        #endregion



        /*********************************/
        /* HELPER FUNCTIONS              */
        /*********************************/
        #region Helper functions
        
        // Sets everything within the crop to specified color.
        private static void Paint(System.Drawing.Rectangle crop, byte[] bitmap_bits, System.Drawing.Color color)
        {
            for (int x = crop.X; x <= crop.Width + crop.X; x++)
            {
                for (int y = crop.Y; y <= crop.Height + crop.Y; y++)
                {
                    int idx = Util.toID(x, y, width, height, kColorStride);
                    bitmap_bits[idx] = color.B;
                    bitmap_bits[idx + 1] = color.G;
                    bitmap_bits[idx + 2] = color.R;
                }
            }
        }

        #endregion
    }
}
