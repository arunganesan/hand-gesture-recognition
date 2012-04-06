using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using FeatureExtractionLib;
using Microsoft.Kinect;

namespace ColorGlove
{


    public class Filter
    {
        public enum Step
        {
            PaintWhite,
            PaintGreen,
            CopyColor,
            CopyDepth,
            Crop,
            MappedDepth,
            MatchColors,
            ColorLabelingInRGB,
            OverlayOffset,
            Denoise,
            EnableFeatureExtract,
            FeatureExtractOnEnable,
            EnablePredict,
            PredictOnEnable,
        };
        
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


        // Adjusts the cropping parameters
        public static void Crop(ProcessorState state)
        {
            state.crop.Value = state.crop_values.Value;
        }


        // This function is used mainly for labelling. It serves two purposes. 
        // First, it finds the nearest color match to each pixel within some 
        // threshold. Then, it records the label based on the color matching 
        // which is later used for creating the training file.
        // 
        // This function writes the color match
        public static void MatchColors(ProcessorState state)
        {
            byte[] rgb_tmp = new byte[3];
            Array.Clear(state.depth_label_, 0, state.depth_label_.Length);  // background label is 0. So can use Clear method.
            for (int i = 0; i < state.depth.Length; i++)
            {
                int depthVal = state.depth[i] >> DepthImageFrame.PlayerIndexBitmaskWidth;
                if ((depthVal <= state.upper.Value) && (depthVal > state.lower.Value))
                {
                    ColorImagePoint point = state.data_.mapped()[i];
                    int baseIndex = Util.toID(point.X, point.Y, width, height, kColorStride);

                    if (point.X > state.crop.Value.X && point.X <= (state.crop.Value.X + state.crop.Value.Width) &&
                        point.Y > state.crop.Value.Y && point.Y <= (state.crop.Value.Y + state.crop.Value.Height))
                    {
                        rgb_tmp[0] = state.rgb[baseIndex + 2];
                        rgb_tmp[1] = state.rgb[baseIndex + 1];
                        rgb_tmp[2] = state.rgb[baseIndex];

                        byte label = NearestColor(rgb_tmp, state);
                        state.depth_label_[i] = label;

                        state.bitmap_bits[baseIndex] = rgb_tmp[2];
                        state.bitmap_bits[baseIndex + 1] = rgb_tmp[1];
                        state.bitmap_bits[baseIndex + 2] = rgb_tmp[0];
                    }
                }
            }
        }

        #endregion



        /*********************************/
        /* HELPER FUNCTIONS              */
        /*********************************/
        #region Helper functions
        
        // Tried to manually map, but it isnt any faster than reflection
        /*
        public static void Process(Step step, ProcessorState state)
        {
            switch (step)
            {
                case Filter.Step.CopyColor: CopyColor(state); break;
                case Filter.Step.CopyDepth: CopyDepth(state); break;
                case Filter.Step.PaintWhite: PaintWhite(state); break;
                case Filter.Step.PaintGreen: PaintGreen(state); break;
                case Filter.Step.Crop: Crop(state); break;
            }
        }
        */
        
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


        private static byte NearestColor (byte[] point, ProcessorState state)
        {
            // In place rewriting of the array
            //if (nearest_cache.ContainsKey(point))
            Tuple<byte, byte, byte> t = new Tuple<byte, byte, byte>(point[0], point[1], point[2]);
            if (state.nearest_cache_.ContainsKey(t))
            {
                //Console.WriteLine("Actually matching.");
                Array.Copy(state.label_color_[state.nearest_cache_[t]], point, 3);
                return state.nearest_cache_[t]; // should return the label
            }

            //int minIdx = 0;
            double minDistance = 1000000;
            byte minColorLabel = state.kBackgroundLabel;

            lock (state.centroid_colors_)
            {
                for (int idx = 0; idx < state.centroid_colors_.Count; idx++)
                {
                    double distance = Util.EuclideanDistance(point, state.centroid_colors_[idx]);
                    if (distance < minDistance)
                    {
                        minColorLabel = state.centroid_labels_[idx];
                        minDistance = distance;
                    }
                }
            }

            state.nearest_cache_.Add(new Tuple<byte, byte, byte>(point[0], point[1], point[2]),
                minColorLabel);


            //Console.WriteLine(nearest_cache.Count());
            Array.Copy(state.label_color_[minColorLabel], point, 3);
            return minColorLabel;
        }
        #endregion
    }
}
