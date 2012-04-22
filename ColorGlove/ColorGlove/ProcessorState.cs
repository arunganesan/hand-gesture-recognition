using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using FeatureExtractionLib;
using Microsoft.Kinect;
using Fleck;

namespace ColorGlove
{
    // why we need to this? Isn't that by just passing the state in processor to the filter function is enough. 
    // this seems to add some copying overhead (Michael)
    public class ProcessorState
    {
        public Ref<System.Drawing.Rectangle> crop;
        public Ref<System.Drawing.Rectangle> crop_values;
        public Ref<int> upper;
        public Ref<int> lower;
        public KinectData data_;
        public short[] depth;
        public byte[] rgb, bitmap_bits;
        public byte[] depth_label_;

        public Dictionary<Tuple<byte, byte, byte>, byte> nearest_cache_;
        public Dictionary<byte, byte[]> label_color_;
        public byte kBackgroundLabel;
        public List<byte[]> centroid_colors_;
        public List<byte> centroid_labels_;

        public Ref<bool> predict_on_enable_;
        public Ref<bool> feature_extract_on_enable_;

        public Ref<bool> overlay_start_;
        public int kNoOverlay;
        public int[] overlay_bitmap_bits_;
        public int[] kEmptyOverlay;

        public FeatureExtractionLib.FeatureExtraction feature;
        public float[] predict_output_;
        public int[] predict_labels_;
        public int[] pool_;

        public List<IWebSocketConnection> all_sockets_;

        public Filter.Step[] pipeline;

        public HandGestureFormat hand_gesture_value_;
        public RangeModeFormat range_mode_value_;

        public ProcessorState(
            Ref<System.Drawing.Rectangle> crop, Ref<System.Drawing.Rectangle> crop_values, 
            Ref<int> upper, Ref<int> lower,
            KinectData data_, short[] depth, byte[] depth_label_, byte[] rgb, byte[] bitmap_bits,
            Dictionary<Tuple<byte, byte, byte>, byte> nearest_cache_, Dictionary<byte, byte[]> label_color_,
            byte kBackgroundLabel, List<byte[]> centroid_colors_, List<byte> centroid_labels,
            Ref<bool> predict_on_enable_, Ref<bool>feature_extract_on_enable_,
            Ref<bool> overlay_start_, int kNoOverlay, int[] overlay_bitmap_bits_, int[] kEmptyOverlay,
            FeatureExtractionLib.FeatureExtraction feature, float[] predict_output_, int[] predict_labels_,
            List<IWebSocketConnection> all_sockets_,
            Filter.Step[] pipeline,
            HandGestureFormat hand_gesture_value_, RangeModeFormat range_mode_value_, int[] pool)
        {
            this.crop = crop;
            this.crop_values = crop_values;
            this.upper = upper;
            this.lower = lower;
            this.data_ = data_;
            this.depth = depth;
            this.rgb = rgb;
            this.bitmap_bits = bitmap_bits;
            this.depth_label_ = depth_label_;
            
            this.nearest_cache_ = nearest_cache_;
            this.label_color_ = label_color_;
            this.kBackgroundLabel = kBackgroundLabel;
            this.centroid_colors_ = centroid_colors_;
            this.centroid_labels_ = centroid_labels;

            this.predict_on_enable_ = predict_on_enable_;
            this.feature_extract_on_enable_ = feature_extract_on_enable_;

            this.overlay_start_ = overlay_start_;
            this.kNoOverlay = kNoOverlay;
            this.overlay_bitmap_bits_ = overlay_bitmap_bits_;
            this.kEmptyOverlay = kEmptyOverlay;

            this.feature = feature;
            this.predict_output_ = predict_output_;
            this.predict_labels_ = predict_labels_;

            this.all_sockets_ = all_sockets_;
            this.pipeline = pipeline;

            this.hand_gesture_value_ = hand_gesture_value_;
            this.range_mode_value_ = range_mode_value_;

            this.pool_ = pool;
        }
    }
}
