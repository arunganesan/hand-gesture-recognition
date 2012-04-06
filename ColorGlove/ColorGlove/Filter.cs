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
        public static void CopyColor(ProcessorState state)
        {
            for (int x = state.crop.Value.X; x <= state.crop.Value.Width + state.crop.Value.X; x++)
            {
                for (int y = state.crop.Value.Y; y <= state.crop.Value.Height + state.crop.Value.Y; y++)
                {
                    int idx = Util.toID(x, y, width, height, kColorStride);
                    Array.Copy(state.rgb, idx, state.bitmap_bits, idx, kColorStride);
                }
            }
        }


        // Copies over the depth value to the buffer, normalized for the range of shorts.
        public static void CopyDepth(ProcessorState state)
        {
            for (int x = state.crop.Value.X; x <= state.crop.Value.Width + state.crop.Value.X; x++)
            {
                for (int y = state.crop.Value.Y; y <= state.crop.Value.Height + state.crop.Value.Y; y++)
                {
                    int idx = Util.toID(x, y, width, height, kDepthStride);
                    
                    state.bitmap_bits[4 * idx] =
                    state.bitmap_bits[4 * idx + 1] =
                    state.bitmap_bits[4 * idx + 2] =
                    state.bitmap_bits[4 * idx + 3] = (byte)(255 * (short.MaxValue - state.depth[idx]) / short.MaxValue);
                }
            }
        }

        // Calls helper function with white.
        public static void PaintWhite(ProcessorState state)
        {
            Paint(state, System.Drawing.Color.White);
        }

        // Calls helper function with green.
        public static void PaintGreen(ProcessorState state)
        {
            Paint(state, System.Drawing.Color.PaleGreen);
        }

        #endregion



        /*********************************/
        /* HELPER FUNCTIONS              */
        /*********************************/
        #region Helper functions
        
        // Sets everything within the crop to specified color.
        private static void Paint(ProcessorState state, System.Drawing.Color color)
        {
            for (int x = state.crop.Value.X; x <= state.crop.Value.Width + state.crop.Value.X; x++)
            {
                for (int y = state.crop.Value.Y; y <= state.crop.Value.Height + state.crop.Value.Y; y++)
                {
                    int idx = Util.toID(x, y, width, height, kColorStride);
                    state.bitmap_bits[idx] = color.B;
                    state.bitmap_bits[idx + 1] = color.G;
                    state.bitmap_bits[idx + 2] = color.R;
                }
            }
        }

        #endregion
    }
}
