﻿using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows.Documents;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Input;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Imaging;
using Microsoft.Kinect;
using FeatureExtractionLib;
using System.Runtime.Serialization.Formatters.Binary;
using System.Threading;
using System.Reflection;
using Fleck;

namespace ColorGlove
{
    public partial class Processor
    {
        #region Object properties
        //private Dictionary<Tuple<byte, byte, byte>, byte[]> nearest_cache = new Dictionary<Tuple<byte, byte, byte>, byte[]>();
        private enum RangeModeFormat
        {
            Default = 0, // If you're using Kinect Xbox you should use Default
            Near = 1,
        };

        public enum ShowExtractedFeatureFormat { 
            // use power of two to allow multiple selection
            None = 1,
            Extract30FeacturesForEveryPixel = 2,
            PredictOnePixelCPU = 4,
            PredictAllPixelsCPU = 8,
            ShowTransformedForOnePixel = 16,
            PredictAllPixelsGPU = 32,
        };

        private bool paused = false;
        private delegate void PauseDelegate (MouseButtonEventArgs e);
        private PauseDelegate pauseDelegate;
        
        // for Kmeans
        int k = 7;
        double[,] kMeans_clusters;
        int[] kMeans_assignments;

        private RangeModeFormat RangeModeValue = RangeModeFormat.Default;
        private ShowExtractedFeatureFormat ShowExtractedFeatureMode = ShowExtractedFeatureFormat.None;
        // It seems not necessary to save the mapped result as byte[], byte should be enough.
        static private Dictionary<Tuple<byte, byte, byte>, byte> nearest_cache = new Dictionary<Tuple<byte, byte, byte>, byte>();         
        private WriteableBitmap bitmap_;
        public byte[] bitmap_bits_;
        private byte[] tmp_buffer_;
        private int[] overlay_bitmap_bits_;
        private readonly int kNoOverlay = -1;
        private int[] kEmptyOverlay;

        private Object bitmap_lock_ = new Object();
        private static Object depth_lock_ = new Object();

        private bool overlayStart;
        private System.Windows.Controls.Image image;
        private Manager manager;
        public int lower, upper; // range for thresholding in show_mapped_depth(),  show_color_depth(). Set by the manager.
        public enum Step
        {
            PaintWhite,
            PaintGreen,
            CopyColor,
            CopyDepth,
            Crop,
            MappedDepth,
            ColorMatch,
            ColorLabelingInRGB,
            OverlayOffset,
            Denoise,
            EnableFeatureExtract,
            FeatureExtractOnEnable,
            EnablePredict,
            PredictOnEnable,
        };

        // *OnEnable counters
        bool predict_on_enable_ = false;
        bool feature_extract_on_enable_ = false;

        //private FeatureExtractionLib.Util.HandGestureFormat HandGestureValue;
        private FeatureExtractionLib.Util.HandGestureFormat HandGestureValue;
        /*
        public enum TestNewFeatureFormat { 
            Default,
            ShowHSLOrHSV,
        };

        public enum TestNewFeatureValue { 
            
        }
        */
        private ProcessorState state;

        private KinectData data_;
        private short[] depth_;
        private byte[] rgb_;

        private byte[] rgb_tmp = new byte[3];
        private byte[] depth_label_;
        private float MinHueTarget, MaxHueTarget, MinSatTarget, MaxSatTarget; // max/min hue value of the target color. Used for hue detection
        
        private const float DesiredMinHue = 198f - .5f, DesiredMaxHue = 214f + .5f,
                                  DesiredMinSat = 0.174f, DesiredMaxSat = 0.397f; // Used for hue dection

        private static System.Drawing.Rectangle cropValues;
        private System.Drawing.Rectangle crop;
        private Ref<System.Drawing.Rectangle> crop_ref_;
        private int width, height;
        private int kColorStride, kDepthStride;
        private System.Drawing.Point startDrag, endDrag;
        private bool dragging = false;
        
        byte[] color = new byte[3];
        double[] tmp_point = new double[3];
        double[] tmp_point2 = new double[3];
        byte[] tmp_byte = new byte[3];
        private Step[] pipeline = new Step[0];
        
        // predict output for each pixel. It is used when using GPU.
        private float[] predict_output_;
        private int[] predict_labels_;
        List<Tuple<byte, byte, byte>> label_colors;
        private enum PoolType
        {
            Centroid,
            Median,
        };
        
        private readonly byte[] targetColor = new byte[] { 255, 0, 0 };
        private readonly byte[] backgroundColor = new byte[] { 255, 255, 255 };

        static private List<byte[]> centroidColor = new List<byte[]>(); // the color of the centroid
        static private List<byte> centroidLabel = new List<byte>(); // the label of the centroid
        private Dictionary<byte, byte[]> labelColor = new Dictionary<byte, byte[]>();

        byte targetLabel, backgroundLabel;

        private static WebSocketServer server = null;
        private static List<IWebSocketConnection> all_sockets_ = new List<IWebSocketConnection>();

        private static FeatureExtractionLib.FeatureExtraction Feature = null;
        List<int[]> listOfTransformedPairPosition;
        #endregion

        public Processor(Manager manager)
        {
            Debug.WriteLine("Start processor contruction");
            this.manager = manager;
            width = 640; height = 480;
            kColorStride = 4; kDepthStride = 1;
            image = new System.Windows.Controls.Image();
            image.Width = width;
            image.Height = height;
            depth_label_ = new byte[width * height];

            cropValues = new System.Drawing.Rectangle(
                Properties.Settings.Default.CropOffset.X,
                Properties.Settings.Default.CropOffset.Y,
                Properties.Settings.Default.CropSize.Width,
                Properties.Settings.Default.CropSize.Height);

            crop = new System.Drawing.Rectangle(0, 0, width - 1, height - 1);
            crop_ref_ = new Ref<System.Drawing.Rectangle>(() => crop, val => { crop = val; });
            
            overlayStart = false;
            kEmptyOverlay = new int[width * height * 4];
            for (int i = 0; i < kEmptyOverlay.Length; i++) kEmptyOverlay[i] = kNoOverlay;

            MinHueTarget = 360.0F;
            MaxHueTarget = 0.0F;
            MinSatTarget = 1F;
            MaxSatTarget = 0F;            
			            
            bitmap_ = new WriteableBitmap(640, 480, 96, 96, PixelFormats.Bgr32, null);
            bitmap_bits_ = new byte[640 * 480 * 4];
            tmp_buffer_ = new byte[640 * 480 * 4];
            overlay_bitmap_bits_ = new int[640 * 480 * 4]; // overlay
            image.Source = bitmap_;

            image.MouseLeftButtonUp += ImageClick;
            image.MouseRightButtonDown += StartDrag;
            image.MouseMove += Drag;
            image.MouseRightButtonUp += EndDrag;
            
            SetCentroidColorAndLabel();

            
            FleckLog.Level = LogLevel.Debug;
            if (server == null)
            {
                server = new WebSocketServer("ws://localhost:8181");
                server.Start(socket =>
                {
                    socket.OnOpen = () =>
                    {
                        Console.WriteLine("Socket opened.");
                        all_sockets_.Add(socket);
                    };

                    socket.OnClose = () =>
                    {
                        Console.WriteLine("Socket closed.");
                        all_sockets_.Remove(socket);
                    };

                    socket.OnMessage = message =>
                    {
                        Console.WriteLine("Message received: {0}", message);
                        socket.Send(String.Format("Message received from you: {0}", message));
                    };
                });
            }

            listOfTransformedPairPosition = new List<int[]>(); // remember to clear.

            Debug.WriteLine("Pass processor setting");
        }

        public void SetTestModule(ShowExtractedFeatureFormat setTestModuleValue){
            ShowExtractedFeatureMode = setTestModuleValue;
            // Setup FeatureExtraction Class
            //Default direcotry: "..\\..\\..\\Data";
            // To setup the mode, see README in the library

            // User dependent. Notice that this is important
            //FeatureExtraction.ModeFormat MyMode = FeatureExtraction.ModeFormat.F1000; 
            //FeatureExtraction.ModeFormat MyMode = FeatureExtraction.ModeFormat.Blue;
            FeatureExtraction.ModeFormat MyMode = FeatureExtraction.ModeFormat.BlueDefault;
            Feature = new FeatureExtractionLib.FeatureExtraction(MyMode, "D:\\gr\\training\\blue");
            label_colors = Util.GiveMeNColors(Feature.num_classes_);
            Feature.ReadOffsetPairsFromStorage();
            predict_output_ = new float[width * height * Feature.num_classes_];
            predict_labels_ = new int[width * height];
            Console.WriteLine("Allocate memory for predict_output");
            
            //Feature.GenerateOffsetPairs(); // use this to test the offset pairs parameters setting
        }

        private void SetCentroidColorAndLabel()
        {
            // First add label
            HandGestureValue = Util.HandGestureFormat.Fist;
            //HandGestureValue = HandGestureFormat.OpenHand;
            // Set which hand gesture to use in the contruct function
            targetLabel = (byte)HandGestureValue;  // numerical value
            Console.WriteLine("targetLabel: {0}", targetLabel);
            backgroundLabel = 0;
            labelColor.Add(targetLabel, new byte[] { 255, 0, 0 }); // target is red
            labelColor.Add(backgroundLabel, new byte[] { 255, 255, 255 }); // background is white
            // Then add arbitrary labeled centroids.
            // For target color (blue)

            AddCentroid(145, 170, 220, targetLabel);
            AddCentroid(170, 190, 250, targetLabel);
            AddCentroid(96, 152, 183, targetLabel);
            AddCentroid(180, 211, 230, targetLabel);
            AddCentroid(156, 196, 221, targetLabel);
            AddCentroid(80, 112, 144, targetLabel);
            AddCentroid(68, 99, 133, targetLabel);
            AddCentroid(76, 103, 141, targetLabel);
            AddCentroid(122, 154, 173, targetLabel);
            AddCentroid(120, 138, 162, targetLabel);
            AddCentroid(109, 118, 137, targetLabel);
            AddCentroid(94, 124, 145, targetLabel);
            AddCentroid(78, 127, 153, targetLabel);
            AddCentroid(146, 177, 200, targetLabel);
            AddCentroid(155, 195, 199, targetLabel);
            AddCentroid(142, 182, 195, targetLabel);
            AddCentroid(146, 189, 211, targetLabel);
            AddCentroid(159, 198, 214, targetLabel);
            AddCentroid(147, 196, 210, targetLabel);
            

            // For background color 
            AddCentroid(80, 80, 80, backgroundLabel);
            AddCentroid(250, 240, 240, backgroundLabel);
            AddCentroid(210, 180, 150, backgroundLabel);
            
            AddCentroid(110, 86, 244, backgroundLabel);
            AddCentroid(75, 58, 151, backgroundLabel);
            AddCentroid(153, 189, 206, backgroundLabel);
            AddCentroid(214, 207, 206, backgroundLabel);
            AddCentroid(122, 124, 130, backgroundLabel);

            AddCentroid(124, 102, 11, backgroundLabel);
            
        }

        public void IncreaseRange()
        {
            upper += 10;
        }

        public void DecreaseRange()
        {
            upper -= 10;
        }

        // Finds nearest depth object and sets the upper bound to epsilon plus that.
        public void AutoDetectRange()
        {
            int epsilon = 60;
            int min = short.MaxValue;
            int depthVal, idx;
            for (int x = crop.X; x <= crop.X + crop.Width; x++)
            {
                for (int y = crop.Y; y <= crop.Y + crop.Height; y++)
                {
                    idx = Util.toID(x, y, width, height, kDepthStride);
                    depthVal = depth_[idx] >> DepthImageFrame.PlayerIndexBitmaskWidth;
                    if (depthVal < min && depthVal > lower)
                    {
                        min = depthVal;
                    }
                }
            }

            upper = min + epsilon;
        }

        // Uses K-means to identify different colors in the image. Then pauses
        // this processor until the user clicks on a color. That color is 
        // chosen to be the target.
        public void kMeans()
        {
            // k = 5 seems to do well with background cleared out.
            
            Random rand = new Random();

            // Randomly create K colors
            kMeans_clusters = new double[k, 3];
            for (int i = 0; i < k; i++) {
                kMeans_clusters[i, 0] = rand.Next(0, 255);
                kMeans_clusters[i, 1] = rand.Next(0, 255);
                kMeans_clusters[i, 2] = rand.Next(0, 255);
            }

            //int width = x_1 - x_0;
            //int height = y_1 - y_0;

            kMeans_assignments = new int[crop.Width * crop.Height];
            double[] point = new double[3];

            int [] cluster_count = Enumerable.Repeat((int)0, k).ToArray();
            int max_cluster = -1;

            double[,] cluster_centers = new double[k, 3];
            for (int i = 0; i < k; i++)
            {
                cluster_centers[i, 0] =
                cluster_centers[i, 1] =
                cluster_centers[i, 2] = 0;
            }

            double[] cluster_deltas = new double[k];

            double delta = 10000, epsilon = 0.1;

            double minDistance = 10000;
            int minCluster = -1;
            while (delta > epsilon)
            {
                Console.WriteLine("Delta: " + delta);
                // Step 1: label each point as a cluster
                for (int i = 0; i < kMeans_assignments.Length; i++)
                {
                    System.Drawing.Point crop_point = Util.toXY(i, crop.Width, crop.Height, 1);
                    int adjusted_i = Util.toID(crop.X + crop_point.X, crop.Y + crop_point.Y, width, height, 1);
                    //int y = i / width;
                    //int x = i % width;
                    //int adjusted_y = y, adjusted_x = x;
                    //int adjusted_y = y_0 + y;
                    //int adjusted_x = x_0 + x;
                    //int adjusted_i = adjusted_y * 640 + adjusted_x;

                    point[0] = rgb_[adjusted_i * 4 + 2];
                    point[1] = rgb_[adjusted_i * 4 + 1];
                    point[2] = rgb_[adjusted_i * 4];

                    minDistance = 10000;
                    for (int idx = 0; idx < kMeans_clusters.GetLength(0); idx++)
                    {
                        tmp_point2[0] = kMeans_clusters[idx, 0];
                        tmp_point2[1] = kMeans_clusters[idx, 1];
                        tmp_point2[2] = kMeans_clusters[idx, 2];

                        double distance = euc_distance(point, tmp_point2);
                        if (distance < minDistance)
                        {
                            minCluster = idx;
                            minDistance = distance;
                        }
                    }

                    kMeans_assignments[i] = minCluster;
                    cluster_count[minCluster] ++;

                }

                // Step 2: update the cluster center values
                for (int i = 0; i < kMeans_assignments.Length; i++)
                {
                    System.Drawing.Point crop_point = Util.toXY(i, crop.Width, crop.Height, 1);
                    int adjusted_i = Util.toID(crop.X + crop_point.X, crop.Y + crop_point.Y, width, height, 1);
                    //int y = i / width;
                    //int x = i % width;
                    //int adjusted_y = y, adjusted_x = x;
                    //int adjusted_y = y_0 + y;
                    //int adjusted_x = x_0 + x;
                    //int adjusted_i = adjusted_y * 640 + adjusted_x;

                    cluster_centers[kMeans_assignments[i], 0] += rgb_[adjusted_i * 4 + 2];
                    cluster_centers[kMeans_assignments[i], 1] += rgb_[adjusted_i * 4 + 1];
                    cluster_centers[kMeans_assignments[i], 2] += rgb_[adjusted_i * 4];
                }

                for (int i = 0; i < k; i++)
                {
                    double r = cluster_centers[i, 0] / cluster_count[i];
                    double g = cluster_centers[i, 1] / cluster_count[i];
                    double b = cluster_centers[i, 2] / cluster_count[i];

                    tmp_point[0] = kMeans_clusters[i, 0];
                    tmp_point[1] = kMeans_clusters[i, 1];
                    tmp_point[2] = kMeans_clusters[i, 2];

                    tmp_point2[0] = r;
                    tmp_point2[1] = g;
                    tmp_point2[2] = b;

                    cluster_deltas[i] = euc_distance(tmp_point, tmp_point2);

                    kMeans_clusters[i, 0] = r;
                    kMeans_clusters[i, 1] = g;
                    kMeans_clusters[i, 2] = b;

                    if (max_cluster == -1 || cluster_count[i] > cluster_count[max_cluster]) 
                        max_cluster = i;
                }

                delta = cluster_deltas[0];
                for (int i = 1; i < k; i++)
                    if (cluster_deltas[i] > delta) delta = cluster_deltas[i];

            }
            
            // Show all colors, wait for the user's click. 
            for (int i = 0; i < kMeans_assignments.Length; i++)
            {
                System.Drawing.Point crop_point = Util.toXY(i, crop.Width, crop.Height, 1);
                int adjusted_i = Util.toID(crop.X + crop_point.X, crop.Y + crop_point.Y, width, height, 1);

                overlay_bitmap_bits_[4 * adjusted_i + 2] = (int)kMeans_clusters[kMeans_assignments[i], 0];
                overlay_bitmap_bits_[4 * adjusted_i + 1] = (int)kMeans_clusters[kMeans_assignments[i], 1];
                overlay_bitmap_bits_[4 * adjusted_i + 0] = (int)kMeans_clusters[kMeans_assignments[i], 2];
            }

            Processor.nearest_cache.Clear();
            
            Console.WriteLine("Done K-Means. Pausing for user click.");
            overlayStart = true;
            update(data_);
            Pause(new PauseDelegate(this.UpdateKMeansCentroid));
        }

        private void UpdateKMeansCentroid(MouseButtonEventArgs e)
        {
            Console.WriteLine("Click acquired.");

            lock (centroidColor)
            {
                clearCentroids();
                System.Windows.Point click_position = e.GetPosition(image);
                if (click_position.Y <= crop.Y ||
                    click_position.Y >= crop.Height + crop.Y ||
                    click_position.X <= crop.X ||
                    click_position.X >= crop.Width + crop.X)
                {
                    Console.WriteLine("Clicked outside of crop region. Using random point.");
                    click_position.X = crop.X + 1;
                    click_position.Y = crop.Y + 1;
                }

                int baseIndex = Util.toID((int)click_position.X - crop.X, (int)click_position.Y - crop.Y, crop.Width, crop.Height, 1);
                
                for (int i = 0; i < k; i++)
                {
                    byte label;
                    if (kMeans_assignments[baseIndex] == i) label = this.targetLabel;
                    else label = this.backgroundLabel;

                    if (label == this.targetLabel)
                    {
                        Console.WriteLine("Found a color of interest.");
                    }

                    AddCentroid((byte)kMeans_clusters[i, 0], (byte)kMeans_clusters[i, 1], (byte)kMeans_clusters[i, 2], label);
                }
            }

            overlayStart = false;
        }

        private void DummyPauseDelegate(MouseButtonEventArgs e) {}

        private void HideOverlayDelegate(MouseButtonEventArgs e) { overlayStart = false; }

        private void AddCentroid(byte R, byte G, byte B, byte label)  // a helper function for adding labled centroid
        {
            centroidColor.Add(new byte[] { R, G, B });
            centroidLabel.Add(label);
        }

        private void clearCentroids()
        {
            centroidColor.Clear();
            centroidLabel.Clear();
        }

        private void Pause(PauseDelegate func)
        {
            pauseDelegate = func;
            paused = true;
        }

        private void UnPause(MouseButtonEventArgs e = null)
        {
            pauseDelegate(e);
            Console.WriteLine("Unpausing.");
            paused = false;
        }

        private void ResetOverlay()
        {
            kEmptyOverlay.CopyTo(overlay_bitmap_bits_, 0);
        }

        private void ImageClick(object sender, MouseButtonEventArgs e)
        {
            if (paused)
            {
                UnPause(e);
                return;
            }

            System.Windows.Point click_position = e.GetPosition(image);
            int baseIndex = ((int)click_position.Y * 640 + (int)click_position.X) * 4;
            Console.WriteLine("(x,y): (" + click_position.X + ", " + click_position.Y + ") RGB: {" + bitmap_bits_[baseIndex + 2] + ", " + bitmap_bits_[baseIndex + 1] + ", " + bitmap_bits_[baseIndex] + "}");
            
            
            if ((ShowExtractedFeatureMode & ShowExtractedFeatureFormat.Extract30FeacturesForEveryPixel) == ShowExtractedFeatureFormat.Extract30FeacturesForEveryPixel )
            {
                int depthIndex = (int)click_position.Y * 640 + (int)click_position.X;
                for (int i = 0; i < depth_.Length; i++)
                    depth_[i] = (short)(depth_[i] >> DepthImageFrame.PlayerIndexBitmaskWidth); // remember this
                int depthVal = depth_[depthIndex]; // >> DepthImageFrame.PlayerIndexBitmaskWidth;
                // Show offsets pair 
                Console.WriteLine("depth: {0}, baseIndex: {1}", depthVal, depthIndex);
                GetAllFeatures();
            }

            // Predict one pixel using CPU
            if ((ShowExtractedFeatureMode & ShowExtractedFeatureFormat.PredictOnePixelCPU) == ShowExtractedFeatureFormat.PredictOnePixelCPU) {
                int depthIndex = (int)click_position.Y * 640 + (int)click_position.X;
                for (int i = 0; i < depth_.Length; i++)
                    depth_[i] = (short)(depth_[i] >> DepthImageFrame.PlayerIndexBitmaskWidth); // remember this
                int depthVal = depth_[depthIndex]; // >> DepthImageFrame.PlayerIndexBitmaskWidth;
                // Show offsets pair 
                Console.WriteLine("depth: {0}, baseIndex: {1}", depthVal, depthIndex);
                // timer start
                DateTime ExecutionStartTime; //Var will hold Execution Starting Time
                DateTime ExecutionStopTime;//Var will hold Execution Stopped Time
                TimeSpan ExecutionTime;//Var will count Total Execution Time-Our Main Hero                
                ExecutionStartTime = DateTime.Now; //Gets the system Current date time expressed as local time
                                
                double [] predictOutput = new double[0];

                Feature.PredictOnePixelCPU(depthIndex, depth_, ref predictOutput); 
                Console.WriteLine("background: {0}, open hand: {1}, close hand: {2}",predictOutput[0], predictOutput[1], predictOutput[2]);
                ExecutionStopTime = DateTime.Now;
                ExecutionTime = ExecutionStopTime - ExecutionStartTime;
                Console.WriteLine("Use {0} ms for getting prediction", ExecutionTime.TotalMilliseconds.ToString());
            }

            // Predict all pixels using CPU
            if ((ShowExtractedFeatureMode & ShowExtractedFeatureFormat.PredictAllPixelsCPU) == ShowExtractedFeatureFormat.PredictAllPixelsCPU) {
                int depthIndex = (int)click_position.Y * 640 + (int)click_position.X;
                for (int i = 0; i < depth_.Length; i++)
                    depth_[i] = (short)(depth_[i] >> DepthImageFrame.PlayerIndexBitmaskWidth); // remember this
                int depthVal = depth_[depthIndex]; // >> DepthImageFrame.PlayerIndexBitmaskWidth;
                // Show offsets pair 
                Console.WriteLine("depth: {0}, baseIndex: {1}", depthVal, depthIndex);
                DateTime ExecutionStartTime; //Var will hold Execution Starting Time
                DateTime ExecutionStopTime;//Var will hold Execution Stopped Time
                TimeSpan ExecutionTime;//Var will count Total Execution Time-Our Main Hero                
                ExecutionStartTime = DateTime.Now; //Gets the system Current date time expressed as local time

                double[] predictOutput = new double[0];
                List<Tuple<byte, byte, byte>> label_colors = Util.GiveMeNColors(Feature.num_classes_);
                ResetOverlay();

                for (int y = crop.Y; y <= crop.Y + crop.Height; y++)
                {
                    for (int x = crop.X; x <= crop.X + crop.Width; x++)
                    {
                        if (x == crop.X) Console.WriteLine("Processing {0}% ({1}/{2})", (float)(y - crop.Y) / crop.Height * 100, (y - crop.Y), crop.Height);
                        depthIndex = Util.toID(x, y, width, height, kDepthStride);

                        int bitmap_index = depthIndex * 4;
                        Feature.PredictOnePixelCPU(depthIndex, depth_, ref predictOutput);
                        int predict_label = 0;
                        
                        for (int i = 1; i < Feature.num_classes_; i++)
                            if (predictOutput[i] > predictOutput[predict_label])
                                predict_label = i;

                        overlay_bitmap_bits_[bitmap_index + 2] = (int)label_colors[predict_label].Item1;
                        overlay_bitmap_bits_[bitmap_index + 1] = (int)label_colors[predict_label].Item2;
                        overlay_bitmap_bits_[bitmap_index + 0] = (int)label_colors[predict_label].Item3;
                    }
                }

                //System.Threading.Thread.Sleep(1000);                
                ExecutionStopTime = DateTime.Now;
                ExecutionTime = ExecutionStopTime - ExecutionStartTime;
                Console.WriteLine("Use {0} ms for getting prediction", ExecutionTime.TotalMilliseconds.ToString());
                
                overlayStart = true; 
                update(data_);
                Pause((PauseDelegate)HideOverlayDelegate);
            }

            // Predict all pixels using GPU
            if ((ShowExtractedFeatureMode & ShowExtractedFeatureFormat.PredictAllPixelsGPU) == ShowExtractedFeatureFormat.PredictAllPixelsGPU) 
            {
                List<Tuple<byte, byte, byte>> label_colors = Util.GiveMeNColors(Feature.num_classes_);
                DateTime ExecutionStartTime;  
                DateTime ExecutionStopTime;  
                TimeSpan ExecutionTime; 
                ExecutionStartTime = DateTime.Now;  

                Feature.PredictGPU(depth_, ref predict_output_);
                ShowAverageAndVariance(predict_output_); // used for debug
                
                ExecutionStopTime = DateTime.Now;
                ExecutionTime = ExecutionStopTime - ExecutionStartTime;
                Console.WriteLine("Use {0} ms for getting prediction", ExecutionTime.TotalMilliseconds.ToString());
                for (int depth_index = 0; depth_index < depth_.Length; depth_index++)
                {
                    int predict_label = 0,  bitmap_index = depth_index * 4, y_index= depth_index * Feature.num_classes_;
                    for (int i = 1; i < Feature.num_classes_; i++)
                        if (predict_output_[y_index  + i] > predict_output_[y_index + predict_label])
                            predict_label = i;
                    overlay_bitmap_bits_[bitmap_index + 2] = (int)label_colors[predict_label].Item1;
                    overlay_bitmap_bits_[bitmap_index + 1] = (int)label_colors[predict_label].Item2;
                    overlay_bitmap_bits_[bitmap_index + 0] = (int)label_colors[predict_label].Item3;
                }
                
                overlayStart = true;
                update(data_);
                Pause((PauseDelegate)HideOverlayDelegate);
            }
                
            EnablePool(); // ??? Need condition?
            
            if ((ShowExtractedFeatureMode & ShowExtractedFeatureFormat.ShowTransformedForOnePixel) == ShowExtractedFeatureFormat.ShowTransformedForOnePixel)
            {
                int depthIndex = (int)click_position.Y * 640 + (int)click_position.X;
                for (int i = 0; i < depth_.Length; i++)
                    depth_[i] = (short)(depth_[i] >> DepthImageFrame.PlayerIndexBitmaskWidth); // remember this
                int depthVal = depth_[depthIndex]; // >> DepthImageFrame.PlayerIndexBitmaskWidth;
                // Show offsets pair 
                Console.WriteLine("depth: {0}, baseIndex: {1}", depthVal, depthIndex);
                DateTime ExecutionStartTime; //Var will hold Execution Starting Time
                DateTime ExecutionStopTime;//Var will hold Execution Stopped Time
                TimeSpan ExecutionTime;//Var will count Total Execution Time-Our Main Hero
                ExecutionStartTime = DateTime.Now; //Gets the system Current date time expressed as local time             
                depthVal = depth_[depthIndex]; // >> DepthImageFrame.PlayerIndexBitmaskWidth;
                listOfTransformedPairPosition.Clear();
                Feature.GetAllTransformedPairs(depthIndex, depthVal, listOfTransformedPairPosition);
                int bitmapIndex, X, Y;

                ResetOverlay();
                //Array.Clear(overlayBitmapBits, 0, overlayBitmapBits.Length);
                //bitmapBits.CopyTo(overlayBitmapBits, 0);
                
                for (int i = 0; i < listOfTransformedPairPosition.Count; i++)
                {
                    X = listOfTransformedPairPosition[i][0];
                    Y = listOfTransformedPairPosition[i][1];
                    if (X >= 0 && X < 640 && Y >= 0 && Y < 480)
                    {
                        bitmapIndex = (Y * 640 + X) * 4;
                        overlay_bitmap_bits_[bitmapIndex + 0] =
                        overlay_bitmap_bits_[bitmapIndex + 1] =
                        overlay_bitmap_bits_[bitmapIndex + 3] =
                        0;
                        overlay_bitmap_bits_[bitmapIndex + 2] = 255;
                        //bitmapBits[bitmapIndex + 2] = 255; // test
                    }
                    X = listOfTransformedPairPosition[i][2];
                    Y = listOfTransformedPairPosition[i][3];
                    if (X >= 0 && X < 640 && Y >= 0 && Y < 480)
                    {
                        bitmapIndex = (Y * 640 + X) * 4;
                        overlay_bitmap_bits_[bitmapIndex + 0] =
                        overlay_bitmap_bits_[bitmapIndex + 1] =
                        overlay_bitmap_bits_[bitmapIndex + 3] =
                        0;
                        overlay_bitmap_bits_[bitmapIndex + 2] = 255;
                    }
                }

                ExecutionStopTime = DateTime.Now;
                ExecutionTime = ExecutionStopTime - ExecutionStartTime;
                Console.WriteLine("Use {0} ms for getting transformed points", ExecutionTime.TotalMilliseconds.ToString());

                overlayStart = true;
            }
        }

        private void ShowAverageAndVariance(float[] a)
        {
            float sum = 0;
            for (int i = 0; i < a.Length; i++)
                sum += a[i];
            Console.WriteLine("Average: {0}, Max: {1}", sum / a.Length, a.Max());
            float[] per_tree_visited_level = new float[a.Length / 3];
            for (int i = 0; i < per_tree_visited_level.Length; i++)
                per_tree_visited_level[i] = a[i * 3];
            string tmp =  string.Join(" ", per_tree_visited_level.Select(x => x.ToString()).ToArray());
            System.IO.File.WriteAllText(Feature.directory+ "\\visited_levels.txt", tmp);
        }

        // Enables the prediction step in the pipeline
        public void EnablePool() { Enable(ref predict_on_enable_); }
        public void EnableFeatureExtract() { Enable(ref feature_extract_on_enable_); }
        public void Enable(ref bool enable_variable) { enable_variable = true; }

        public void UpdateCropSettings()
        {
            Properties.Settings.Default.CropOffset = cropValues.Location;
            Properties.Settings.Default.CropSize = cropValues.Size;
            Properties.Settings.Default.Save();
        }

        private void GetAllFeatures() {
            // timer start
            DateTime ExecutionStartTime; //Var will hold Execution Starting Time
            DateTime ExecutionStopTime;//Var will hold Execution Stopped Time
            TimeSpan ExecutionTime;//Var will count Total Execution Time-Our Main Hero

            ExecutionStartTime = DateTime.Now; //Gets the system Current date time expressed as local time
            for (int depthIndex = 0; depthIndex < depth_.Length; depthIndex++) // test for looping through all pixels
            {
                short depthVal = depth_[depthIndex];
                listOfTransformedPairPosition.Clear();
                //Feature.GetAllTransformedPairs(depthIndex, depthVal, listOfTransformedPairPosition);
                Feature.GetFirstNTransformedPairs(depthIndex, depthVal, listOfTransformedPairPosition, 33);
                int bitmapIndex, X, Y;
                //Array.Clear(overlayBitmapBits, 0, overlayBitmapBits.Length);
                /*
                for (int i = 0; i < listOfTransformedPairPosition.Count; i++)
                {
                    X = listOfTransformedPairPosition[i][0];
                    Y = listOfTransformedPairPosition[i][1];
                    if (X >= 0 && X < 640 && Y >= 0 && Y < 480)
                    {
                        bitmapIndex = (Y * 640 + X) * 4;                        
                    }
                    X = listOfTransformedPairPosition[i][2];
                    Y = listOfTransformedPairPosition[i][3];
                    if (X >= 0 && X < 640 && Y >= 0 && Y < 480)
                    {
                        bitmapIndex = (Y * 640 + X) * 4;                        
                    }
                }
                */
            }
            ExecutionStopTime = DateTime.Now;
            ExecutionTime = ExecutionStopTime - ExecutionStartTime;
            Console.WriteLine("Use {0} ms for getting the 33 transformed points", ExecutionTime.TotalMilliseconds.ToString());
        }

        private void StartDrag(object sender, MouseButtonEventArgs e)
        {
            System.Windows.Point click_position = e.GetPosition(image);
            dragging = true;
            startDrag.X = (int)click_position.X;
            startDrag.Y = (int)click_position.Y;
        }

        private void Drag(object sender, MouseEventArgs e)
        {
            System.Windows.Point position = e.GetPosition(image);
            if (dragging)
            {
                endDrag.X = (int)position.X;
                endDrag.Y = (int)position.Y;

                cropValues.X = Math.Min(startDrag.X, endDrag.X);
                cropValues.Y = Math.Min(startDrag.Y, endDrag.Y);
                cropValues.Width = Math.Abs(startDrag.X - endDrag.X);
                cropValues.Height = Math.Abs(startDrag.Y - endDrag.Y);
            }
        }

        private void EndDrag(object sender, MouseButtonEventArgs e)
        {
            System.Windows.Point click_position = e.GetPosition(image);
            dragging = false;
            endDrag.X = (int)click_position.X;
            endDrag.Y = (int)click_position.Y;

            cropValues.X = Math.Min(startDrag.X, endDrag.X);
            cropValues.Y = Math.Min(startDrag.Y, endDrag.Y);
            cropValues.Width = Math.Abs(startDrag.X - endDrag.X);
            cropValues.Height = Math.Abs(startDrag.Y - endDrag.Y);
            UpdateCropSettings();
            //Console.WriteLine("New crop values are {0}", cropValues);
        }

        public void ProcessAndSave()
        {
            lock (bitmap_lock_)
            {
                // Prevent the rest of the application from updating bitmapBits while it is being written.
                /*
            var directory = "..\\..\\..\\Data" + "\\" + HandGestureValue + RangeModeValue;  // assume the directory exist
            TimeSpan t = (DateTime.UtcNow - new DateTime(1970, 1, 1));
            string filename = t.TotalSeconds.ToString();
                */
                
                update(data_);
                var directory = "D:\\gr\\training\\blue\\" + HandGestureValue;
                //var directory = "..\\..\\..\\Data" + "\\" + HandGestureValue + RangeModeValue;  // assume the directory exist
                TimeSpan t = (DateTime.UtcNow - new DateTime(1970, 1, 1));
                string filename = t.TotalSeconds.ToString();


                List<int[]> depthAndLabel = new List<int[]>(); // -1 means non-hand 
                using (StreamWriter filestream = new StreamWriter(directory + "\\" + "depthLabel_" + filename + ".txt"))
                {
                    for (int i = 0; i < depth_.Length; i++)
                    {
                        int depthVal = depth_[i] >> DepthImageFrame.PlayerIndexBitmaskWidth; // notice that the depth has been processed
                        byte label = depth_label_[i];
                        depthAndLabel.Add(new int[] { depthVal, label });
                    }

                    // Output file format: 
                    //(depthVal, label) (depthVal, label) (depthVal, label) (depthVal, label) ...
                    //
            
                    filestream.Write("({0},{1})", depthAndLabel[0][0], depthAndLabel[0][1]);
                    for (int i = 1; i < depthAndLabel.Count; i++) filestream.Write(" ({0},{1})", depthAndLabel[i][0], depthAndLabel[i][1]);
                }

                // Save the Kinect Data
                Stream stream = File.Open(directory + "\\data_" + filename + ".obj", FileMode.Create);
                BinaryFormatter bformatter = new BinaryFormatter();
                bformatter.Serialize(stream, data_);
                stream.Close();
            }
        }

        public System.Windows.Controls.Image getImage() { return image; }

        public void updatePipeline(params Step[] steps)
        {
            pipeline = new Step[steps.Length];
            for (int i = 0; i < steps.Length; i++) pipeline[i] = steps[i];
        }

        private void process(Step step)
        {
            // Wrap object.
            //Console.WriteLine("crop: {0}, depth_: {1}, rgb: {2}, bitmap: {3}", &crop, &depth_, &rgb_, &bitmap_bits_);
            switch (step)
            {
                case Step.CopyColor:
                case Step.CopyDepth:
                case Step.PaintWhite:
                case Step.PaintGreen:
                    Type type = typeof(Filter);
                    MethodInfo Filtermethod = type.GetMethod(step.ToString());
                    Filtermethod.Invoke(null, new object[]{state});
                    break;
                case Step.Crop: Crop(); break;
                case Step.ColorMatch: MatchColors(); break;
                case Step.ColorLabelingInRGB: ColorLabellingInRGB(); break;
                case Step.OverlayOffset: ShowOverlay(); break;
                case Step.Denoise: Denoise(); break;
                case Step.EnablePredict: Enable(ref predict_on_enable_); break;
                case Step.EnableFeatureExtract: Enable(ref feature_extract_on_enable_); break;
                case Step.FeatureExtractOnEnable: FeatureExtractOnEnable(); break;
                case Step.PredictOnEnable: PredictOnEnable(); break;
            }
        }

        #region Filter functions

        // Runs the prediction algorithm for each pixel and pools the results. 
        // The classes are drawn onto the overlay layer, and overlay is turned 
        // on. 
        private void PredictOnEnable()
        {
            if (predict_on_enable_ == false) return;
            AdjustDepth();
            PredictGPU();
            DrawPredictionOverlay();
            Pooled gesture = Pool(PoolType.Median);
            SendToSockets(gesture);
            predict_on_enable_ = false;
        }

        private void FeatureExtractOnEnable()
        {
            if (feature_extract_on_enable_ == false) return;
            
            int color_match_index = Array.IndexOf(pipeline, Step.ColorMatch);
            int this_index = Array.IndexOf(pipeline, Step.FeatureExtractOnEnable);
            Debug.Assert(color_match_index != -1 && this_index > color_match_index, "ColorMatch must precede this step in the pipeline.");
            
            var directory = "D:\\gr\\training\\blue\\" + HandGestureValue + RangeModeValue;
            //var directory = "..\\..\\..\\Data" + "\\" + HandGestureValue + RangeModeValue;  // assume the directory exist
            TimeSpan t = (DateTime.UtcNow - new DateTime(1970, 1, 1));
            string filename = t.TotalSeconds.ToString();


            List<int[]> depthAndLabel = new List<int[]>(); // -1 means non-hand 
            using (StreamWriter filestream = new StreamWriter(directory + "\\" + "depthLabel_" + filename + ".txt"))
            {
                for (int i = 0; i < depth_.Length; i++)
                {
                    int depthVal = depth_[i] >> DepthImageFrame.PlayerIndexBitmaskWidth; // notice that the depth has been processed
                    byte label = depth_label_[i];
                    depthAndLabel.Add(new int[] { depthVal, label });
                }

                // Output file format:
                //(depthVal, label) (depthVal, label) (depthVal, label) (depthVal, label) ...

                filestream.Write("({0},{1})", depthAndLabel[0][0], depthAndLabel[0][1]);
                for (int i = 1; i < depthAndLabel.Count; i++) filestream.Write(" ({0},{1})", depthAndLabel[i][0], depthAndLabel[i][1]);
            }

            feature_extract_on_enable_ = false;
        }

        // Scales all values in the depth image by the bitmaskshift.
        private void AdjustDepth()
        {
            for (int i = 0; i < depth_.Length; i++)
                depth_[i] = (short)(depth_[i] >> DepthImageFrame.PlayerIndexBitmaskWidth);
        }

        // Uses (awesome) GPU code for prediction, and counts the majority 
        // class for each point. Stores the probability distr in predict_output_
        // and the majority value in predict_labels_.
        private void PredictGPU()
        {
            Feature.PredictGPU(depth_, ref predict_output_);
            ShowAverageAndVariance(predict_output_);

            for (int y = crop.Y; y <= crop.Y + crop.Height; y++)
            {
                for (int x = crop.X; x <= crop.X + crop.Width; x++)
                {
                    int depth_index = Util.toID(x, y, width, height, kDepthStride);
                    int predict_label = 0;
                    int y_index = depth_index * Feature.num_classes_;

                    for (int i = 1; i < Feature.num_classes_; i++)
                        if (predict_output_[y_index + i] > predict_output_[y_index + predict_label])
                            predict_label = i;

                    predict_labels_[depth_index] = predict_label;
                }
            }
        }

        // Uses the prediction output from PredictGPU to color in the overlay.
        // If the point belongs to the background, doesn't draw anything.
        private void DrawPredictionOverlay()
        {
            ResetOverlay();

            for (int y = crop.Y; y <= crop.Y + crop.Height; y++)
            {
                for (int x = crop.X; x <= crop.X + crop.Width; x++)
                {
                    int depth_index = Util.toID(x, y, width, height, kDepthStride);
                    int predict_label = predict_labels_[depth_index];

                    int bitmap_index = depth_index * 4;
                    if (predict_label != (int)Util.HandGestureFormat.Background)
                    {
                        overlay_bitmap_bits_[bitmap_index + 2] = (int)label_colors[predict_label].Item1;
                        overlay_bitmap_bits_[bitmap_index + 1] = (int)label_colors[predict_label].Item2;
                        overlay_bitmap_bits_[bitmap_index + 0] = (int)label_colors[predict_label].Item3;
                    }
                }
            }

            overlayStart = true;
        }

        // Uses the per-pixel classes from PredictGPU to pool the location of 
        // gestures. Supports multiple types of pooling algorithms. Each 
        // algorithm is described before each section.
        private Pooled Pool(PoolType type)
        {
            // Median pooling. The median X, Y and depths are calculated
            // for the majority class. The pooled location takes on these
            // values.

            int[] label_counts = new int[Feature.num_classes_];
            Array.Clear(label_counts, 0, label_counts.Length);

            List<int>[] label_sorted_x = new List<int>[Feature.num_classes_];
            List<int>[] label_sorted_y = new List<int>[Feature.num_classes_];
            List<int>[] label_sorted_depth = new List<int>[Feature.num_classes_];

            for (int i = 1; i < Feature.num_classes_; i++)
            {
                label_sorted_x[i] = new List<int>();
                label_sorted_y[i] = new List<int>();
                label_sorted_depth[i] = new List<int>();
            }

            for (int y = crop.Y; y <= crop.Y + crop.Height; y++)
            {
                for (int x = crop.X; x <= crop.X + crop.Width; x++)
                {
                    int depth_index = Util.toID(x, y, width, height, kDepthStride);
                    int predict_label = predict_labels_[depth_index];

                    label_counts[predict_label]++;
                    if (predict_label != (int)Util.HandGestureFormat.Background)
                    {
                        label_sorted_x[predict_label].Add(x);
                        label_sorted_y[predict_label].Add(y);
                        label_sorted_depth[predict_label].Add(depth_[depth_index]);
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
            DrawCrosshairAt(center, center_depth);
            return gesture;
        }

        // Draws a crosshair at the specific point in the overlay buffer
        private void DrawCrosshairAt(System.Drawing.Point xy, int depth)
        {
            int box_length = 20;
            int x, y;
            System.Drawing.Color paint = System.Drawing.Color.Black;

            x = xy.X - box_length / 2;
            for (int i = 0; i < box_length; i++)
            {
                PaintAt(x + i, xy.Y - 1, paint);
                PaintAt(x + i, xy.Y, paint);
                PaintAt(x + i, xy.Y + 1, paint);
            }

            y = xy.Y - box_length / 2;
            for (int i = 0; i < box_length; i++)
            {
                PaintAt(xy.X - 1, y + i, paint);
                PaintAt(xy.X, y + i, paint);
                PaintAt(xy.X + 1, y + i, paint);
            }
        }

        // Helper function for drawing custom    shapes on the overlay buffer
        private void PaintAt(int x, int y, System.Drawing.Color paint)
        {
            int idx = Util.toID(x, y, width, height, kColorStride);

            overlay_bitmap_bits_[idx] = paint.B;
            overlay_bitmap_bits_[idx + 1] = paint.G;
            overlay_bitmap_bits_[idx + 2] = paint.R;
        }

        private void SendToSockets(Pooled gesture)
        {
            string message = gesture.ToString();
            Console.WriteLine("Sending: {0}", message);
            foreach (var socket in all_sockets_.ToList()) socket.Send(message);
        }

        private void Denoise()
        {
            int x, y;
            int totalSurrounding = 0;
            int width = 640, height = 480;
            int[] sumSurrounding = new int[] { 0, 0, 0 };

            // XXX: Doesn't work with cropping.
            for (int i = 0; i < bitmap_bits_.Length; i += 4)
            {
                x = i / 4 % width;
                y = i / 4 / width;

                // Average of surrounding points
                sumSurrounding[0] = 0;
                sumSurrounding[1] = 0;
                sumSurrounding[2] = 0;
                totalSurrounding = 0;

                if (y != 0 && x != 0)
                {
                    sumSurrounding[0] += bitmap_bits_[((y - 1) * width + (x - 1)) * 4];
                    sumSurrounding[1] += bitmap_bits_[((y - 1) * width + (x - 1)) * 4 + 1];
                    sumSurrounding[2] += bitmap_bits_[((y - 1) * width + (x - 1)) * 4 + 2];
                    totalSurrounding++;
                }

                if (y != 0)
                {
                    sumSurrounding[0] += bitmap_bits_[((y - 1) * width + (x)) * 4];
                    sumSurrounding[1] += bitmap_bits_[((y - 1) * width + (x)) * 4 + 1];
                    sumSurrounding[2] += bitmap_bits_[((y - 1) * width + (x)) * 4 + 2];
                    totalSurrounding++;
                }

                if (y != 0 && x != width - 1)
                {
                    sumSurrounding[0] += bitmap_bits_[((y - 1) * width + (x + 1)) * 4];
                    sumSurrounding[1] += bitmap_bits_[((y - 1) * width + (x + 1)) * 4 + 1];
                    sumSurrounding[2] += bitmap_bits_[((y - 1) * width + (x + 1)) * 4 + 2];
                    totalSurrounding++;
                }

                if (x != width - 1)
                {
                    sumSurrounding[0] += bitmap_bits_[((y) * width + (x + 1)) * 4];
                    sumSurrounding[1] += bitmap_bits_[((y) * width + (x + 1)) * 4 + 1];
                    sumSurrounding[2] += bitmap_bits_[((y) * width + (x + 1)) * 4 + 2];
                    totalSurrounding++;
                }

                if (y != height - 1 && x != width - 1)
                {
                    sumSurrounding[0] += bitmap_bits_[((y + 1) * width + (x + 1)) * 4];
                    sumSurrounding[1] += bitmap_bits_[((y + 1) * width + (x + 1)) * 4 + 1];
                    sumSurrounding[2] += bitmap_bits_[((y + 1) * width + (x + 1)) * 4 + 2];
                    totalSurrounding++;
                }

                if (y != height - 1)
                {
                    sumSurrounding[0] += bitmap_bits_[((y + 1) * width + (x)) * 4];
                    sumSurrounding[1] += bitmap_bits_[((y + 1) * width + (x)) * 4 + 1];
                    sumSurrounding[2] += bitmap_bits_[((y + 1) * width + (x)) * 4 + 2];
                    totalSurrounding++;
                }

                if (y != height - 1 && x != 0)
                {
                    sumSurrounding[0] += bitmap_bits_[((y + 1) * width + (x - 1)) * 4];
                    sumSurrounding[1] += bitmap_bits_[((y + 1) * width + (x - 1)) * 4 + 1];
                    sumSurrounding[2] += bitmap_bits_[((y + 1) * width + (x - 1)) * 4 + 2];
                    totalSurrounding++;
                }

                if (x != 0)
                {
                    sumSurrounding[0] += bitmap_bits_[((y) * width + (x - 1)) * 4];
                    sumSurrounding[1] += bitmap_bits_[((y) * width + (x - 1)) * 4 + 1];
                    sumSurrounding[2] += bitmap_bits_[((y) * width + (x - 1)) * 4 + 2];
                    totalSurrounding++;
                }

                tmp_buffer_[i] = (byte)(sumSurrounding[0] / totalSurrounding);
                tmp_buffer_[i + 1] = (byte)(sumSurrounding[1] / totalSurrounding);
                tmp_buffer_[i + 2] = (byte)(sumSurrounding[2] / totalSurrounding);
                tmp_buffer_[i + 3] = 255;
            }

            Array.Copy(tmp_buffer_, bitmap_bits_, tmp_buffer_.Length);
        }

        // Adjusts the cropping parameters
        private void Crop()
        {
            crop.X = cropValues.X; crop.Y = cropValues.Y;
            crop.Width = cropValues.Width; crop.Height = cropValues.Height;
        }

        // This function is used mainly for labelling. It serves two purposes. 
        // First, it finds the nearest color match to each pixel within some 
        // threshold. Then, it records the label based on the color matching 
        // which is later used for creating the training file.
        // 
        // This function writes the color match
        private void MatchColors()
        {
            Array.Clear(depth_label_, 0, depth_label_.Length);  // background label is 0. So can use Clear method.
            for (int i = 0; i < depth_.Length; i++)
            {
                int depthVal = depth_[i] >> DepthImageFrame.PlayerIndexBitmaskWidth;
                if ((depthVal <= upper) && (depthVal > lower))
                {
                    ColorImagePoint point = data_.mapped()[i];
                    int baseIndex = Util.toID(point.X, point.Y, width, height, kColorStride);
                    
                    if (point.X > crop.X && point.X <= (crop.X + crop.Width) &&
                        point.Y > crop.Y && point.Y <= (crop.Y + crop.Height))
                    {
                        rgb_tmp[0] = rgb_[baseIndex + 2];
                        rgb_tmp[1] = rgb_[baseIndex + 1];
                        rgb_tmp[2] = rgb_[baseIndex];

                        byte label = nearest_color(rgb_tmp);
                        depth_label_[i] = label;

                        bitmap_bits_[baseIndex] = rgb_tmp[2];
                        bitmap_bits_[baseIndex + 1] = rgb_tmp[1];
                        bitmap_bits_[baseIndex + 2] = rgb_tmp[0];
                    }
                }
            }
        }

        // Label the RGB image without involving depth image
        private void ColorLabellingInRGB()
        {
            byte[] bgr = rgb_;
            byte[] rgb_tmp = new byte[3];
            for (int i = 0; i < bgr.Length; i += 4)
            {
                bitmap_bits_[i + 3] = 255;
                rgb_tmp[0] = bgr[i + 2];
                rgb_tmp[1] = bgr[i + 1];
                rgb_tmp[2] = bgr[i];

                setLabel(rgb_tmp); // do the labeling

                bitmap_bits_[i] = rgb_tmp[2];
                bitmap_bits_[i + 1] = rgb_tmp[1];
                bitmap_bits_[i + 2] = rgb_tmp[0];
            }
        }

        private void ShowOverlay() {
            if (overlayStart)
            {
                //Console.WriteLine("Overlay enabled. Drawing overlay.");
                for (int x = crop.X; x <= crop.Width + crop.X; x++)
                {
                    for (int y = crop.Y; y <= crop.Height + crop.Y; y++)
                    {
                        int idx = Util.toID(x, y, width, height, kColorStride);
                        if (overlay_bitmap_bits_[idx] != kNoOverlay) bitmap_bits_[idx] = (byte)overlay_bitmap_bits_[idx];
                        if (overlay_bitmap_bits_[idx + 1] != kNoOverlay) bitmap_bits_[idx + 1] = (byte)overlay_bitmap_bits_[idx + 1];
                        if (overlay_bitmap_bits_[idx + 2] != kNoOverlay) bitmap_bits_[idx + 2] = (byte)overlay_bitmap_bits_[idx + 2];
                        if (overlay_bitmap_bits_[idx + 3] != kNoOverlay) bitmap_bits_[idx + 3] = (byte)overlay_bitmap_bits_[idx + 3];
                    }
                }
            }
        }

        #endregion

        private void updateHelper()
        {
            //Console.WriteLine("Thread {0} about to dispatch.", Thread.CurrentThread.ManagedThreadId);
            bitmap_.Dispatcher.Invoke(new Action(() =>
            {
                bitmap_.WritePixels(new Int32Rect(0, 0, bitmap_.PixelWidth, bitmap_.PixelHeight),
                    bitmap_bits_, bitmap_.PixelWidth * sizeof(int), 0);
            }));
        }

        public void update(KinectData data)
        {
            // XXX: Bitmap locking should be unnecessary when working with 
            // the pipeline!


            if (data_ == null)
            {
                this.data_ = data;
                this.depth_ = data.depth();
                this.rgb_ = data.color();
                
                // Package the state
                state = new ProcessorState(
                    crop_ref_, 
                    depth_, 
                    rgb_, 
                    bitmap_bits_);
            }

            lock (bitmap_lock_)
            {
                if (paused) return;
                foreach (Step step in pipeline) process(step);
            }
            // The helper always runs in the MainThread anyway (dispatch). If 
            // this is inside the critical section, and the main thread sleeps
            // deadlock in a strage way!
            updateHelper();
            //Thread.Sleep(5000);
        }

        #region Color matching

        void setLabel(byte[] point)
        {
            // Using HSV for color labeling

            System.Drawing.Color color = System.Drawing.Color.FromArgb(point[0], point[1], point[2]);
            float hue = color.GetHue();
            float sat = color.GetSaturation();
            if (hue >= DesiredMinHue && hue <= DesiredMaxHue && sat >= DesiredMinSat && sat <= DesiredMaxSat)
            {
                //Array.Copy(targetColor, point, 3);                
            }
            else
            {
                Array.Copy(backgroundColor, point, 3);
            }
        }

        byte nearest_color(byte[] point)
        {
            // In place rewriting of the array
            //if (nearest_cache.ContainsKey(point))
            Tuple<byte, byte, byte> t = new Tuple<byte, byte, byte>(point[0], point[1], point[2]);
            if (nearest_cache.ContainsKey(t))
            {
                //Console.WriteLine("Actually matching.");
                Array.Copy(labelColor[nearest_cache[t]], point, 3);
                return nearest_cache[t]; // should return the label
            }

            //int minIdx = 0;
            double minDistance = 1000000;
            byte minColorLabel = backgroundLabel;

            lock (centroidColor)
            {
                for (int idx = 0; idx < centroidColor.Count; idx++)
                {
                    double distance = EuclideanDistance(point, centroidColor[idx]);
                    if (distance < minDistance)
                    {
                        minColorLabel = centroidLabel[idx];
                        minDistance = distance;
                    }
                }
            }

            nearest_cache.Add(new Tuple<byte, byte, byte>(point[0], point[1], point[2]),
                minColorLabel);


            //Console.WriteLine(nearest_cache.Count());
            Array.Copy(labelColor[minColorLabel], point, 3);
            return minColorLabel;
        }

        double euc_distance(double[] point, double[] point2)
        {
            return Math.Sqrt(Math.Pow(point[0] - point2[0], 2) +
                Math.Pow(point[1] - point2[1], 2) +
                Math.Pow(point[2] - point2[2], 2));
        }
        
        double ColorDistance(byte[] point1, byte[] point2) // using human perception for the color metric
        {

            long rmean = ((long)point1[0] + (long)point2[0]) / 2;
            long r = (long)point1[0] - (long)point2[0];
            long g = (long)point1[1] - (long)point2[1];
            long b = (long)point2[2] - (long)point2[2];
            return Math.Sqrt((((512 + rmean) * r * r) >> 8) + 4 * g * g + (((767 - rmean) * b * b) >> 8));
        }

        double EuclideanDistance(byte[] point1, byte[] point2)
        {
            return Math.Sqrt(Math.Pow(point1[0] - point2[0], 2) +
                Math.Pow(point1[1] - point2[1], 2) +
                Math.Pow(point1[2] - point2[2], 2));
        }
        #endregion

    }
}
