using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Diagnostics;
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
            ShowOverlay,
            Denoise,
            EnableFeatureExtract,
            FeatureExtractOnEnable,
            EnablePredict,
            PredictOnEnable,
        };

        private enum PoolType
        {
            Centroid,
            Median,
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

        // Enables the prediction....
        public static void EnablePredict(ProcessorState state) 
        {
            bool val = state.predict_on_enable_.Value;
            Enable(ref val);
            state.predict_on_enable_.Value = val;
        }

        // Enables the feature extraction....
        public static void EnableFeatureExtract(ProcessorState state) 
        {
            bool val = state.feature_extract_on_enable_.Value;
            Enable(ref val);
            state.feature_extract_on_enable_.Value = val;
        }

        // Simply copies over the overlay bits to the bitmap with two 
        // conditions. First, it only copies within the crop. Second, it 
        // doesnt copy values that are set to "kNoOverlay". This allows for 
        // drawing on top of existing images.
        public static void ShowOverlay(ProcessorState state)
        {
            if (state.overlay_start_.Value)
            {
                for (int x = state.crop.Value.X; x <= state.crop.Value.Width + state.crop.Value.X; x++)
                {
                    for (int y = state.crop.Value.Y; y <= state.crop.Value.Height + state.crop.Value.Y; y++)
                    {
                        int idx = Util.toID(x, y, width, height, kColorStride);
                        if (state.overlay_bitmap_bits_[idx] != state.kNoOverlay) state.bitmap_bits[idx] = (byte)state.overlay_bitmap_bits_[idx];
                        if (state.overlay_bitmap_bits_[idx + 1] != state.kNoOverlay) state.bitmap_bits[idx + 1] = (byte)state.overlay_bitmap_bits_[idx + 1];
                        if (state.overlay_bitmap_bits_[idx + 2] != state.kNoOverlay) state.bitmap_bits[idx + 2] = (byte)state.overlay_bitmap_bits_[idx + 2];
                        if (state.overlay_bitmap_bits_[idx + 3] != state.kNoOverlay) state.bitmap_bits[idx + 3] = (byte)state.overlay_bitmap_bits_[idx + 3];
                    }
                }
            }
        }

        // Runs the prediction algorithm for each pixel and pools the results. 
        // The classes are drawn onto the overlay layer, and overlay is turned 
        // on. 
        public static void PredictOnEnable(ProcessorState state)
        {
            if (state.predict_on_enable_.Value == false) return;
            AdjustDepth(state);
            PredictGPU(state);
            DrawPredictionOverlay(state);
            Pooled gesture = Pool(PoolType.Median, state);
            SendToSockets(gesture, state);
            state.predict_on_enable_.Value = false;
        }

        private void FeatureExtractOnEnable(ProcessorState state)
        {
            if (state.feature_extract_on_enable_.Value == false) return;

            int color_match_index = Array.IndexOf(state.pipeline, Filter.Step.MatchColors);
            int this_index = Array.IndexOf(state.pipeline, Filter.Step.FeatureExtractOnEnable);
            Debug.Assert(color_match_index != -1 && this_index > color_match_index, "ColorMatch must precede this step in the pipeline.");

            var directory = "D:\\gr\\training\\blue\\" + state.hand_gesture_value_ + state.range_mode_value_;
            //var directory = "..\\..\\..\\Data" + "\\" + HandGestureValue + RangeModeValue;  // assume the directory exist
            TimeSpan t = (DateTime.UtcNow - new DateTime(1970, 1, 1));
            string filename = t.TotalSeconds.ToString();


            List<int[]> depthAndLabel = new List<int[]>(); // -1 means non-hand 
            using (StreamWriter filestream = new StreamWriter(directory + "\\" + "depthLabel_" + filename + ".txt"))
            {
                for (int i = 0; i < state.depth.Length; i++)
                {
                    int depthVal = state.depth[i] >> DepthImageFrame.PlayerIndexBitmaskWidth; // notice that the depth has been processed
                    byte label = state.depth_label_[i];
                    depthAndLabel.Add(new int[] { depthVal, label });
                }

                // Output file format:
                //(depthVal, label) (depthVal, label) (depthVal, label) (depthVal, label) ...

                filestream.Write("({0},{1})", depthAndLabel[0][0], depthAndLabel[0][1]);
                for (int i = 1; i < depthAndLabel.Count; i++) filestream.Write(" ({0},{1})", depthAndLabel[i][0], depthAndLabel[i][1]);
            }

            state.feature_extract_on_enable_.Value = false;
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

        // Just may be *the dumbest* function...
        private static void Enable(ref bool enable_variable) { enable_variable = true; }


        // Scales all values in the depth image by the bitmaskshift.
        private static void AdjustDepth(ProcessorState state)
        {
            for (int i = 0; i < state.depth.Length; i++)
                state.depth[i] = (short)(state.depth[i] >> DepthImageFrame.PlayerIndexBitmaskWidth);
        }

        // Uses (awesome) GPU code for prediction, and counts the majority 
        // class for each point. Stores the probability distr in predict_output_
        // and the majority value in predict_labels_.
        private static void PredictGPU(ProcessorState state)
        {
            state.feature.PredictGPU(state.depth, ref state.predict_output_);
            ShowAverageAndVariance(state.predict_output_, state);

            for (int y = state.crop.Value.Y; y <= state.crop.Value.Y + state.crop.Value.Height; y++)
            {
                for (int x = state.crop.Value.X; x <= state.crop.Value.X + state.crop.Value.Width; x++)
                {
                    int depth_index = Util.toID(x, y, width, height, kDepthStride);
                    int predict_label = 0;
                    int y_index = depth_index * state.feature.num_classes_;

                    for (int i = 1; i < state.feature.num_classes_; i++)
                        if (state.predict_output_[y_index + i] > state.predict_output_[y_index + predict_label])
                            predict_label = i;

                    state.predict_labels_[depth_index] = predict_label;
                }
            }
        }


        private static void ShowAverageAndVariance(float[] a, ProcessorState state)
        {
            float sum = 0;
            for (int i = 0; i < a.Length; i++)
                sum += a[i];
            Console.WriteLine("Average: {0}, Max: {1}", sum / a.Length, a.Max());
            float[] per_tree_visited_level = new float[a.Length / 3];
            for (int i = 0; i < per_tree_visited_level.Length; i++)
                per_tree_visited_level[i] = a[i * 3];
            string tmp = string.Join(" ", per_tree_visited_level.Select(x => x.ToString()).ToArray());
            System.IO.File.WriteAllText(state.feature.directory + "\\visited_levels.txt", tmp);
        }


        // Uses the prediction output from PredictGPU to color in the overlay.
        // If the point belongs to the background, doesn't draw anything.
        private static void DrawPredictionOverlay(ProcessorState state)
        {
            List<Tuple<byte,byte,byte>> label_colors = Util.GiveMeNColors(state.feature.num_classes_);
            ResetOverlay(state);

            for (int y = state.crop.Value.Y; y <= state.crop.Value.Y + state.crop.Value.Height; y++)
            {
                for (int x = state.crop.Value.X; x <= state.crop.Value.X + state.crop.Value.Width; x++)
                {
                    int depth_index = Util.toID(x, y, width, height, kDepthStride);
                    int predict_label = state.predict_labels_[depth_index];

                    int bitmap_index = depth_index * 4;
                    if (predict_label != (int)Util.HandGestureFormat.Background)
                    {
                        state.overlay_bitmap_bits_[bitmap_index + 2] = (int)label_colors[predict_label].Item1;
                        state.overlay_bitmap_bits_[bitmap_index + 1] = (int)label_colors[predict_label].Item2;
                        state.overlay_bitmap_bits_[bitmap_index + 0] = (int)label_colors[predict_label].Item3;
                    }
                }
            }

            state.overlay_start_.Value = true;
        }

        private static void ResetOverlay(ProcessorState state)
        {
            state.kEmptyOverlay.CopyTo(state.overlay_bitmap_bits_, 0);
        }

        // Uses the per-pixel classes from PredictGPU to pool the location of 
        // gestures. Supports multiple types of pooling algorithms. Each 
        // algorithm is described before each section.
        private static Pooled Pool(PoolType type, ProcessorState state)
        {
            // Median pooling. The median X, Y and depths are calculated
            // for the majority class. The pooled location takes on these
            // values.

            int[] label_counts = new int[state.feature.num_classes_];
            Array.Clear(label_counts, 0, label_counts.Length);

            List<int>[] label_sorted_x = new List<int>[state.feature.num_classes_];
            List<int>[] label_sorted_y = new List<int>[state.feature.num_classes_];
            List<int>[] label_sorted_depth = new List<int>[state.feature.num_classes_];

            for (int i = 1; i < state.feature.num_classes_; i++)
            {
                label_sorted_x[i] = new List<int>();
                label_sorted_y[i] = new List<int>();
                label_sorted_depth[i] = new List<int>();
            }

            for (int y = state.crop.Value.Y; y <= state.crop.Value.Y + state.crop.Value.Height; y++)
            {
                for (int x = state.crop.Value.X; x <= state.crop.Value.X + state.crop.Value.Width; x++)
                {
                    int depth_index = Util.toID(x, y, width, height, kDepthStride);
                    int predict_label = state.predict_labels_[depth_index];

                    label_counts[predict_label]++;
                    if (predict_label != (int)Util.HandGestureFormat.Background)
                    {
                        label_sorted_x[predict_label].Add(x);
                        label_sorted_y[predict_label].Add(y);
                        label_sorted_depth[predict_label].Add(state.depth[depth_index]);
                    }
                }
            }

            Tuple<int, int> max = Util.MaxNonBackground(label_counts);
            int max_index = max.Item1, max_value = max.Item2;
            int total_non_background = label_counts.Sum() - label_counts[0];

            Console.WriteLine("Most common gesture is {0} (appears {1}/{2} times).",
                ((Util.HandGestureFormat)max_index).ToString(),
                max_value, total_non_background);

            System.Drawing.Point center = new System.Drawing.Point();
            int center_depth = 0;

            if (type == PoolType.Centroid)
            {
                center.X = (int)(label_sorted_x[max_index].Average());
                center.Y = (int)(label_sorted_y[max_index].Average());
                center_depth = (int)(label_sorted_depth[max_index].Average());
            }
            else if (type == PoolType.Median)
            {
                label_sorted_x[max_index].Sort();
                label_sorted_y[max_index].Sort();
                label_sorted_depth[max_index].Sort();

                center.X = (int)(label_sorted_x[max_index].ElementAt(max_value / 2));
                center.Y = (int)(label_sorted_y[max_index].ElementAt(max_value / 2));
                center_depth = (int)(label_sorted_depth[max_index].ElementAt(max_value / 2));
            }

            Pooled gesture = new Pooled(center, center_depth, (Util.HandGestureFormat)max_index);
            Console.WriteLine("Center: ({0}px, {1}px, {2}cm)", center.X, center.Y, center_depth);
            DrawCrosshairAt(center, center_depth, state);
            return gesture;
        }

        // Draws a crosshair at the specific point in the overlay buffer
        private static void DrawCrosshairAt(System.Drawing.Point xy, int depth, ProcessorState state)
        {
            int box_length = 20;
            int x, y;
            System.Drawing.Color paint = System.Drawing.Color.Black;

            x = xy.X - box_length / 2;
            for (int i = 0; i < box_length; i++)
            {
                PaintAt(x + i, xy.Y - 1, paint, state);
                PaintAt(x + i, xy.Y, paint, state);
                PaintAt(x + i, xy.Y + 1, paint, state);
            }

            y = xy.Y - box_length / 2;
            for (int i = 0; i < box_length; i++)
            {
                PaintAt(xy.X - 1, y + i, paint, state);
                PaintAt(xy.X, y + i, paint, state);
                PaintAt(xy.X + 1, y + i, paint, state);
            }
        }


        // Helper function for drawing custom    shapes on the overlay buffer
        private static void PaintAt(int x, int y, System.Drawing.Color paint, ProcessorState state)
        {
            int idx = Util.toID(x, y, width, height, kColorStride);

            state.overlay_bitmap_bits_[idx] = paint.B;
            state.overlay_bitmap_bits_[idx + 1] = paint.G;
            state.overlay_bitmap_bits_[idx + 2] = paint.R;
        }

        // Writes the gesture to the sockets. Uses gestures.ToString() method
        private static void SendToSockets(Pooled gesture, ProcessorState state)
        {
            string message = gesture.ToString();
            Console.WriteLine("Sending: {0}", message);
            foreach (var socket in state.all_sockets_.ToList()) socket.Send(message);
        }

        #endregion
    }
}
