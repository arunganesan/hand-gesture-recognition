using System;
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
    public enum RangeModeFormat
    {
        Default = 0, // If you're using Kinect Xbox you should use Default
        Near = 1,
    };
    
    public partial class Processor
    {
        #region Object properties
        //private Dictionary<Tuple<byte, byte, byte>, byte[]> nearest_cache = new Dictionary<Tuple<byte, byte, byte>, byte[]>();
        

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
        private System.Windows.Controls.Image image_;
        private Manager manager;
        public int lower, upper; // range for thresholding in show_mapped_depth(),  show_color_depth(). Set by the manager.
        
        // *OnEnable counters
        bool predict_on_enable_ = false;
        bool feature_extract_on_enable_ = false;

        //private FeatureExtractionLib.HandGestureFormat HandGestureValue;
        private FeatureExtractionLib.HandGestureFormat HandGestureValue;
        
        private KinectData data_;
        private short[] depth_;
        private byte[] rgb_;

        private byte[] rgb_tmp = new byte[3];
        private byte[] depth_label_;
        private float MinHueTarget, MaxHueTarget, MinSatTarget, MaxSatTarget; // max/min hue value of the target color. Used for hue detection
        
        private const float DesiredMinHue = 198f - .5f, DesiredMaxHue = 214f + .5f,
                                  DesiredMinSat = 0.174f, DesiredMaxSat = 0.397f; // Used for hue dection

        private static System.Drawing.Rectangle crop_values_;
        private System.Drawing.Rectangle crop_;
        private int width, height;
        private int kColorStride, kDepthStride;
        private System.Drawing.Point startDrag, endDrag;
        private bool dragging = false;
        
        byte[] color = new byte[3];
        double[] tmp_point = new double[3];
        double[] tmp_point2 = new double[3];
        byte[] tmp_byte = new byte[3];
        private Filter.Step[] pipeline = new Filter.Step[0];
        
        // predict output for each pixel. It is used when using GPU.
        private float[] predict_output_;
        private int[] predict_labels_;
        List<Tuple<byte, byte, byte>> label_colors;
                
        private readonly byte[] targetColor = new byte[] { 255, 0, 0 };
        private readonly byte[] backgroundColor = new byte[] { 255, 255, 255 };

        static private List<byte[]> centroidColor = new List<byte[]>(); // the color of the centroid
        static private List<byte> centroidLabel = new List<byte>(); // the label of the centroid
        private Dictionary<byte, byte[]> labelColor = new Dictionary<byte, byte[]>();

        byte targetLabel;
        readonly byte kBackgroundLabel = 0;

        private static WebSocketServer server = null;
        private static List<IWebSocketConnection> all_sockets_ = new List<IWebSocketConnection>();

        private static FeatureExtractionLib.FeatureExtraction Feature = null;
        List<int[]> listOfTransformedPairPosition;


        // References used for ProcessorState
        private ProcessorState state;
        private Ref<System.Drawing.Rectangle> crop_val_ref_;
        private Ref<System.Drawing.Rectangle> crop_ref_;
        private Ref<int> upper_ref_;
        private Ref<int> lower_ref_;
        private Ref<bool> predict_on_enable_ref_;
        private Ref<bool> feature_extract_on_enable_ref_;
        private Ref<bool> overlay_start_ref_;
        #endregion

        public Processor(Manager manager)
        {

            Debug.WriteLine("Start processor contruction");
            this.manager = manager;
            width = 640; height = 480;
            kColorStride = 4; kDepthStride = 1;
            image_ = new System.Windows.Controls.Image();
            image_.Width = width;
            image_.Height = height;
            depth_label_ = new byte[width * height];

            crop_values_ = new System.Drawing.Rectangle(
                Properties.Settings.Default.CropOffset.X,
                Properties.Settings.Default.CropOffset.Y,
                Properties.Settings.Default.CropSize.Width,
                Properties.Settings.Default.CropSize.Height);

            crop_ = new System.Drawing.Rectangle(0, 0, width - 1, height - 1);
            crop_ref_ = new Ref<System.Drawing.Rectangle>(() => crop_, val => { crop_ = val; });
            crop_val_ref_ = new Ref<System.Drawing.Rectangle>(() => crop_values_, val => { crop_values_ = val; });

            upper_ref_ = new Ref<int>(() => upper, val => { upper = val; });
            lower_ref_ = new Ref<int>(() => lower, val => { lower = val; });

            predict_on_enable_ref_ = new Ref<bool>(() => predict_on_enable_, val => { predict_on_enable_ = val; });
            feature_extract_on_enable_ref_ = new Ref<bool>(() => feature_extract_on_enable_, val => {feature_extract_on_enable_ = val; });

            overlay_start_ref_ = new Ref<bool>(() => overlayStart, val => { overlayStart = val; });

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
            image_.Source = bitmap_;

            image_.MouseLeftButtonUp += ImageClick;
            image_.MouseRightButtonDown += StartDrag;
            image_.MouseMove += Drag;
            image_.MouseRightButtonUp += EndDrag;

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

        public void setFeatureExtraction(ShowExtractedFeatureFormat setFeatureExtractionMode){
            ShowExtractedFeatureMode = setFeatureExtractionMode;
            // Setup FeatureExtraction Class
            //Default direcotry: "..\\..\\..\\Data";
            // To setup the mode, see README in the library

            // User dependent. Notice that this is important
            //FeatureExtraction.ModeFormat MyMode = FeatureExtraction.ModeFormat.F1000; 
            if (manager.ProcessorMode == Manager.ProcessorModeFormat.Michael)
            {
                FeatureExtraction.ModeFormat MyMode = FeatureExtraction.ModeFormat.Blue;
                Feature = new FeatureExtractionLib.FeatureExtraction(MyMode);
            }
            else
            {
                FeatureExtraction.ModeFormat MyMode = FeatureExtraction.ModeFormat.BlueDefault;
                Feature = new FeatureExtractionLib.FeatureExtraction(MyMode, "D:\\gr\\training\\blue");
            }


            // The following code is deprecated even it is commented. Look at the above! 
            //FeatureExtraction.ModeFormat MyMode = FeatureExtraction.ModeFormat.Blue;
            //FeatureExtraction.ModeFormat MyMode = FeatureExtraction.ModeFormat.BlueDefault;
            //Feature = new FeatureExtractionLib.FeatureExtraction(MyMode, "D:\\gr\\training\\blue");			
            //Feature = new FeatureExtractionLib.FeatureExtraction(FeatureExtraction.ModeFormat.F2000, "C:\\Users\\Michael Zhang\\Desktop\\HandGestureRecognition\\Experiments\\alglib");
            // Michael's code here
            //Feature = new FeatureExtractionLib.FeatureExtraction(FeatureExtraction.ModeFormat.Blue);            
            // end of deprecated
            label_colors = Util.GiveMeNColors(Feature.num_classes_);
            Feature.ReadOffsetPairsFromStorage();
            predict_output_ = new float[width * height * Feature.num_classes_];
            predict_labels_ = new int[width * height];
            Console.WriteLine("Allocate memory for predict_output");
            
            //Feature.GenerateOffsetPairs(); // use this to test the offset pairs parameters setting
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
            for (int x = crop_.X; x <= crop_.X + crop_.Width; x++)
            {
                for (int y = crop_.Y; y <= crop_.Y + crop_.Height; y++)
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

            kMeans_assignments = new int[crop_.Width * crop_.Height];
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
                    System.Drawing.Point crop_point = Util.toXY(i, crop_.Width, crop_.Height, 1);
                    int adjusted_i = Util.toID(crop_.X + crop_point.X, crop_.Y + crop_point.Y, width, height, 1);
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

                        double distance = Util.EuclideanDistance(point, tmp_point2);
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
                    System.Drawing.Point crop_point = Util.toXY(i, crop_.Width, crop_.Height, 1);
                    int adjusted_i = Util.toID(crop_.X + crop_point.X, crop_.Y + crop_point.Y, width, height, 1);
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

                    cluster_deltas[i] = Util.EuclideanDistance(tmp_point, tmp_point2);

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
                System.Drawing.Point crop_point = Util.toXY(i, crop_.Width, crop_.Height, 1);
                int adjusted_i = Util.toID(crop_.X + crop_point.X, crop_.Y + crop_point.Y, width, height, 1);

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
        
        #region Pause functionality
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
        
        private void UpdateKMeansCentroid(MouseButtonEventArgs e)
        {
            Console.WriteLine("Click acquired.");

            lock (centroidColor)
            {
                clearCentroids();
                System.Windows.Point click_position = e.GetPosition(image_);
                if (click_position.Y <= crop_.Y ||
                    click_position.Y >= crop_.Height + crop_.Y ||
                    click_position.X <= crop_.X ||
                    click_position.X >= crop_.Width + crop_.X)
                {
                    Console.WriteLine("Clicked outside of crop region. Using random point.");
                    click_position.X = crop_.X + 1;
                    click_position.Y = crop_.Y + 1;
                }

                int baseIndex = Util.toID((int)click_position.X - crop_.X, (int)click_position.Y - crop_.Y, crop_.Width, crop_.Height, 1);

                for (int i = 0; i < k; i++)
                {
                    byte label;
                    if (kMeans_assignments[baseIndex] == i) label = this.targetLabel;
                    else label = kBackgroundLabel;

                    if (label == this.targetLabel)
                    {
                        Console.WriteLine("Found a color of interest.");
                    }

                    AddCentroid((byte)kMeans_clusters[i, 0], (byte)kMeans_clusters[i, 1], (byte)kMeans_clusters[i, 2], label);
                }
            }

            overlayStart = false;
        }

        private void DummyPauseDelegate(MouseButtonEventArgs e) { }

        private void HideOverlayDelegate(MouseButtonEventArgs e) { overlayStart = false; }
        #endregion

        #region Centroid coloring
        private void SetCentroidColorAndLabel()
        {
            // First add label
            HandGestureValue = HandGestureFormat.Fist;
            //HandGestureValue = HandGestureFormat.OpenHand;
            // Set which hand gesture to use in the contruct function
            targetLabel = (byte)HandGestureValue;  // numerical value
            Console.WriteLine("targetLabel: {0}", targetLabel);
            labelColor.Add(targetLabel, new byte[] { 255, 0, 0 }); // target is red
            labelColor.Add(kBackgroundLabel, new byte[] { 255, 255, 255 }); // background is white
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
            AddCentroid(232, 242, 246, targetLabel);


            // For background color 
            AddCentroid(80, 80, 80, kBackgroundLabel);
            AddCentroid(250, 240, 240, kBackgroundLabel);
            AddCentroid(210, 180, 150, kBackgroundLabel);

            AddCentroid(110, 86, 244, kBackgroundLabel);
            AddCentroid(75, 58, 151, kBackgroundLabel);
            AddCentroid(153, 189, 206, kBackgroundLabel);
            AddCentroid(214, 207, 206, kBackgroundLabel);
            AddCentroid(122, 124, 130, kBackgroundLabel);

            AddCentroid(124, 102, 11, kBackgroundLabel);

        }

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
        #endregion

        private void ResetOverlay()
        {
            kEmptyOverlay.CopyTo(overlay_bitmap_bits_, 0);
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
                
        public void UpdateCropSettings()
        {
            Properties.Settings.Default.CropOffset = crop_values_.Location;
            Properties.Settings.Default.CropSize = crop_values_.Size;
            Properties.Settings.Default.Save();
        }

        private void GetAllFeaturesCPU() {
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
        
        private void ImageClick(object sender, MouseButtonEventArgs e)
        {
            if (paused)
            {
                UnPause(e);
                return;
            }

            System.Windows.Point click_position = e.GetPosition(image_);
            int baseIndex = ((int)click_position.Y * 640 + (int)click_position.X) * 4;
            Console.WriteLine("(x,y): (" + click_position.X + ", " + click_position.Y + ") RGB: {" + bitmap_bits_[baseIndex + 2] + ", " + bitmap_bits_[baseIndex + 1] + ", " + bitmap_bits_[baseIndex] + "}");

            return;

            if ((ShowExtractedFeatureMode & ShowExtractedFeatureFormat.Extract30FeacturesForEveryPixel) == ShowExtractedFeatureFormat.Extract30FeacturesForEveryPixel)
            {
                int depthIndex = (int)click_position.Y * 640 + (int)click_position.X;
                for (int i = 0; i < depth_.Length; i++)
                    depth_[i] = (short)(depth_[i] >> DepthImageFrame.PlayerIndexBitmaskWidth); // remember this
                int depthVal = depth_[depthIndex]; // >> DepthImageFrame.PlayerIndexBitmaskWidth;
                // Show offsets pair 
                Console.WriteLine("depth: {0}, baseIndex: {1}", depthVal, depthIndex);
                GetAllFeaturesCPU();
            }

            // Predict one pixel using CPU
            if ((ShowExtractedFeatureMode & ShowExtractedFeatureFormat.PredictOnePixelCPU) == ShowExtractedFeatureFormat.PredictOnePixelCPU)
            {
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

                double[] predictOutput = new double[0];

                Feature.PredictOnePixelCPU(depthIndex, depth_, ref predictOutput);
                Console.WriteLine("background: {0}, open hand: {1}, close hand: {2}", predictOutput[0], predictOutput[1], predictOutput[2]);
                ExecutionStopTime = DateTime.Now;
                ExecutionTime = ExecutionStopTime - ExecutionStartTime;
                Console.WriteLine("Use {0} ms for getting prediction", ExecutionTime.TotalMilliseconds.ToString());
            }

            // Predict all pixels using CPU
            if ((ShowExtractedFeatureMode & ShowExtractedFeatureFormat.PredictAllPixelsCPU) == ShowExtractedFeatureFormat.PredictAllPixelsCPU)
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

                double[] predictOutput = new double[0];
                List<Tuple<byte, byte, byte>> label_colors = Util.GiveMeNColors(Feature.num_classes_);
                ResetOverlay();

                for (int y = crop_.Y; y <= crop_.Y + crop_.Height; y++)
                {
                    for (int x = crop_.X; x <= crop_.X + crop_.Width; x++)
                    {
                        if (x == crop_.X) Console.WriteLine("Processing {0}% ({1}/{2})", (float)(y - crop_.Y) / crop_.Height * 100, (y - crop_.Y), crop_.Height);
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
                
                ExecutionStopTime = DateTime.Now;
                ExecutionTime = ExecutionStopTime - ExecutionStartTime;
                Console.WriteLine("Use {0} ms for getting prediction", ExecutionTime.TotalMilliseconds.ToString());

                ShowAverageAndVariance(predict_output_); // used for debug
                
                for (int depth_index = 0; depth_index < depth_.Length; depth_index++)
                {
                    int predict_label = 0, bitmap_index = depth_index * 4, y_index = depth_index * Feature.num_classes_;
                    for (int i = 1; i < Feature.num_classes_; i++)
                        if (predict_output_[y_index + i] > predict_output_[y_index + predict_label])
                            predict_label = i;
                    overlay_bitmap_bits_[bitmap_index + 2] = (int)label_colors[predict_label].Item1;
                    overlay_bitmap_bits_[bitmap_index + 1] = (int)label_colors[predict_label].Item2;
                    overlay_bitmap_bits_[bitmap_index + 0] = (int)label_colors[predict_label].Item3;
                }

                overlayStart = true;
                update(data_);
                Pause((PauseDelegate)HideOverlayDelegate);
            }
            // This got messed up in merging. This should be inside 
            // the above if condition.
            // EnablePool(); // ??? Need condition?

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

        private void StartDrag(object sender, MouseButtonEventArgs e)
        {
            System.Windows.Point click_position = e.GetPosition(image_);
            dragging = true;
            startDrag.X = (int)click_position.X;
            startDrag.Y = (int)click_position.Y;
        }

        private void Drag(object sender, MouseEventArgs e)
        {
            System.Windows.Point position = e.GetPosition(image_);
            if (dragging)
            {
                endDrag.X = (int)position.X;
                endDrag.Y = (int)position.Y;

                crop_values_.X = Math.Min(startDrag.X, endDrag.X);
                crop_values_.Y = Math.Min(startDrag.Y, endDrag.Y);
                crop_values_.Width = Math.Abs(startDrag.X - endDrag.X);
                crop_values_.Height = Math.Abs(startDrag.Y - endDrag.Y);
            }
        }

        private void EndDrag(object sender, MouseButtonEventArgs e)
        {
            System.Windows.Point click_position = e.GetPosition(image_);
            dragging = false;
            endDrag.X = (int)click_position.X;
            endDrag.Y = (int)click_position.Y;

            crop_values_.X = Math.Min(startDrag.X, endDrag.X);
            crop_values_.Y = Math.Min(startDrag.Y, endDrag.Y);
            crop_values_.Width = Math.Abs(startDrag.X - endDrag.X);
            crop_values_.Height = Math.Abs(startDrag.Y - endDrag.Y);
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
                // this should be changed to be more user friendly
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

        public System.Windows.Controls.Image getImage() { return image_; }

        // seems like a better name to be setPipeline?
        public void updatePipeline(params Filter.Step [] steps)
        {
            
            pipeline = new Filter.Step[steps.Length];
            for (int i = 0; i < steps.Length; i++) pipeline[i] = steps[i];
        }
		
        // Relay functions into filter
        public void EnableFeatureExtract() { Filter.EnableFeatureExtract(state); }
        public void EnablePredict() { Filter.EnablePredict(state); }
        // End.
		
		
        private void process(Filter.Step step)
        {
            Type type = typeof(Filter);
            MethodInfo Filtermethod = type.GetMethod(step.ToString());
            Filtermethod.Invoke(null, new object[]{state});
        }

        private void PackageState()
        {
            // so each ProcessorState needs to allocate a memory? That seems some overhead (Michael)
            state = new ProcessorState(
                crop_ref_, crop_val_ref_, upper_ref_, lower_ref_,
                data_, depth_, depth_label_, rgb_, bitmap_bits_,
                nearest_cache, labelColor, kBackgroundLabel, centroidColor, centroidLabel,
                predict_on_enable_ref_, feature_extract_on_enable_ref_,
                overlay_start_ref_, kNoOverlay, overlay_bitmap_bits_, kEmptyOverlay,
                Feature, predict_output_, predict_labels_,
                all_sockets_, pipeline,
                HandGestureValue, RangeModeValue);
        }

       public void update(KinectData data)
        {
            if (data_ == null)
            {
                this.data_ = data;
                this.depth_ = data.depth();
                this.rgb_ = data.color();

                PackageState();
            }

            lock (bitmap_lock_)
            {
                if (paused) return;
                foreach (Filter.Step step in pipeline) process(step);
            }
            bitmap_.Dispatcher.Invoke(new Action(() =>
            {
                bitmap_.WritePixels(new Int32Rect(0, 0, bitmap_.PixelWidth, bitmap_.PixelHeight),
                    bitmap_bits_, bitmap_.PixelWidth * sizeof(int), 0);
            }));
          
        }
    }
}
