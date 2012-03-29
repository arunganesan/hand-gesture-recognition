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

namespace ColorGlove
{
    public class Processor
    {

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
        static private Dictionary<Tuple<byte, byte, byte>, byte> nearest_cache = new Dictionary<Tuple<byte, byte, byte>, byte>(); // It seems not necessary to save the mapped result as byte[], byte should be enough.
        private WriteableBitmap bitmap;
        private byte[] bitmapBits;
        private byte[] tmpBuffer;
        private byte[] overlayBitmapBits;
        private bool overlayStart; 
        private System.Windows.Controls.Image image;
        private Manager manager;
        public int lower, upper; // range for thresholding in show_mapped_depth(),  show_color_depth(). Set by the manager.
        public enum Step
        {
            PaintWhite,
            PaintGreen,
            Color,
            Depth,
            Crop,
            MappedDepth,
            ColorMatch,
            ColorLabelingInRGB,
            OverlayOffset,
            Denoise,
        };

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
        private short[] depth;
        private byte[] rgb;
        private KinectData data;

        private byte[] rgb_tmp = new byte[3];
        private ColorImagePoint[] mapped;
        private byte[] depthLabel;
        private float MinHueTarget, MaxHueTarget, MinSatTarget, MaxSatTarget; // max/min hue value of the target color. Used for hue detection


        private const float DesiredMinHue = 198f - .5f, DesiredMaxHue = 214f + .5f,
                                  DesiredMinSat = 0.174f, DesiredMaxSat = 0.397f; // Used for hue dection

        private static System.Drawing.Rectangle cropValues;
        private System.Drawing.Rectangle crop;
        private int width, height;
        private int colorStride, depthStride;
        private System.Drawing.Point startDrag, endDrag;
        private bool dragging = false;
        
        byte[] color = new byte[3];
        double[] tmp_point = new double[3];
        double[] tmp_point2 = new double[3];
        byte[] tmp_byte = new byte[3];
        private Step[] pipeline = new Step[0];
        private KinectSensor sensor;

        //private Classifier classifier;

        private readonly byte[] targetColor = new byte[] { 255, 0, 0 };
        private readonly byte[] backgroundColor = new byte[] { 255, 255, 255 };

        static private List<byte[]> centroidColor = new List<byte[]>(); // the color of the centroid
        static private List<byte> centroidLabel = new List<byte>(); // the label of the centroid
        private Dictionary<byte, byte[]> labelColor = new Dictionary<byte, byte[]>();

        byte targetLabel, backgroundLabel;


        FeatureExtractionLib.FeatureExtraction Feature;
        List<int[]> listOfTransformedPairPosition;
        

        public Processor(KinectSensor sensor, Manager manager)
        {
            Debug.WriteLine("Start processor contruction");
            this.manager = manager;
            this.sensor = sensor;
            width = 640; height = 480;
            colorStride = 4; depthStride = 1;
            image = new System.Windows.Controls.Image();
            image.Width = width;
            image.Height = height;
            mapped = new ColorImagePoint[width * height];
            depthLabel = new byte[width * height];
            cropValues = new System.Drawing.Rectangle(220, 170, 150, 200);
            crop = new System.Drawing.Rectangle(0, 0, width - 1, height - 1);
            overlayStart = false;

            MinHueTarget = 360.0F;
            MaxHueTarget = 0.0F;
            MinSatTarget = 1F;
            MaxSatTarget = 0F;

            // Setup FeatureExtraction Class
            //Default direcotry: "..\\..\\..\\Data";
            // To setup the mode, see README in the library

            //FeatureExtraction.ModeFormat MyMode = FeatureExtraction.ModeFormat.BlueDefault; // User dependent. Notice that this is important
            FeatureExtraction.ModeFormat MyMode = FeatureExtraction.ModeFormat.BlueDefault;
            Feature = new FeatureExtractionLib.FeatureExtraction(MyMode);
            Feature.ReadOffsetPairsFromStorage();
            //Feature.GenerateOffsetPairs(); // use this to test the offset pairs parameters setting
			
            //classifier = new Classifier();
            this.bitmap = new WriteableBitmap(640, 480, 96, 96, PixelFormats.Bgr32, null);
            this.bitmapBits = new byte[640 * 480 * 4];
            tmpBuffer = new byte[640 * 480 * 4];
            overlayBitmapBits = new byte[640 * 480 * 4]; // overlay
            image.Source = bitmap;

            image.MouseLeftButtonUp += ImageClick;
            image.MouseRightButtonDown += StartDrag;
            image.MouseMove += Drag;
            image.MouseRightButtonUp += EndDrag;


            SetCentroidColorAndLabel();
            
            listOfTransformedPairPosition = new List<int[]>(); // remember to clear.
            Debug.WriteLine("Pass processor setting");
        }

        public void SetTestModule(ShowExtractedFeatureFormat setTestModuleValue){
            ShowExtractedFeatureMode = setTestModuleValue;
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
                    idx = Util.toID(x, y, width, height, depthStride);
                    depthVal = depth[idx] >> DepthImageFrame.PlayerIndexBitmaskWidth;
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
        //
        // XXX: Code is very ugly. And doesn't work with crop. And choosing
        // just one color doesnt do a good job. Maybe sbhould also pick the 
        // background colors somehow (right click?)
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

            kMeans_assignments = new int[width * height];
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
                    int y = i / width;
                    int x = i % width;
                    int adjusted_y = y, adjusted_x = x;
                    //int adjusted_y = y_0 + y;
                    //int adjusted_x = x_0 + x;
                    int adjusted_i = adjusted_y * 640 + adjusted_x;

                    point[0] = rgb[adjusted_i * 4 + 2];
                    point[1] = rgb[adjusted_i * 4 + 1];
                    point[2] = rgb[adjusted_i * 4];

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
                    int y = i / width;
                    int x = i % width;
                    int adjusted_y = y, adjusted_x = x;
                    //int adjusted_y = y_0 + y;
                    //int adjusted_x = x_0 + x;
                    int adjusted_i = adjusted_y * 640 + adjusted_x;

                    cluster_centers[kMeans_assignments[i], 0] += rgb[adjusted_i * 4 + 2];
                    cluster_centers[kMeans_assignments[i], 1] += rgb[adjusted_i * 4 + 1];
                    cluster_centers[kMeans_assignments[i], 2] += rgb[adjusted_i * 4];
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
                bitmapBits[4 * i + 2] = (byte)kMeans_clusters[kMeans_assignments[i], 0];
                bitmapBits[4 * i + 1] = (byte)kMeans_clusters[kMeans_assignments[i], 1];
                bitmapBits[4 * i + 0] = (byte)kMeans_clusters[kMeans_assignments[i], 2];
            }

            Console.WriteLine("Done K-Means. Pausing for user click.");
            Processor.nearest_cache.Clear();

            updateHelper();
            pause(new PauseDelegate(this.updateKMeansCentroid));         
        }

        private void updateKMeansCentroid(MouseButtonEventArgs e)
        {
            Console.WriteLine("Click acquired.");

            lock (centroidColor)
            {
                clearCentroids();
                System.Windows.Point click_position = e.GetPosition(image);
                int baseIndex = ((int)click_position.Y * 640 + (int)click_position.X);
                
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

        private void pause(PauseDelegate func)
        {
            pauseDelegate = func;
            paused = true;
        }

        private void unPause(MouseButtonEventArgs e)
        {
            pauseDelegate(e);
            Console.WriteLine("Unpausing.");
            paused = false;
        }

        private void ImageClick(object sender, MouseButtonEventArgs e)
        {
            System.Windows.Point click_position = e.GetPosition(image);
            int baseIndex = ((int)click_position.Y * 640 + (int)click_position.X) * 4;
            Console.WriteLine("(x,y): (" + click_position.X + ", " + click_position.Y + ") RGB: {" + bitmapBits[baseIndex + 2] + ", " + bitmapBits[baseIndex + 1] + ", " + bitmapBits[baseIndex] + "}");
            int depthIndex = (int)click_position.Y * 640 + (int)click_position.X;
            for (int i = 0; i < depth.Length; i++)
                depth[i] = (short)(depth[i] >> DepthImageFrame.PlayerIndexBitmaskWidth); // remember this
            int depthVal= depth[depthIndex]; // >> DepthImageFrame.PlayerIndexBitmaskWidth;
            // Show offsets pair 
            Console.WriteLine("depth: {0}, baseIndex: {1}", depthVal, depthIndex);
            
            if ((ShowExtractedFeatureMode & ShowExtractedFeatureFormat.Extract30FeacturesForEveryPixel) == ShowExtractedFeatureFormat.Extract30FeacturesForEveryPixel )
            {
                GetAllFeatures( );
            }

            if ((ShowExtractedFeatureMode & ShowExtractedFeatureFormat.PredictOnePixelCPU) == ShowExtractedFeatureFormat.PredictOnePixelCPU) {
                // timer start
                DateTime ExecutionStartTime; //Var will hold Execution Starting Time
                DateTime ExecutionStopTime;//Var will hold Execution Stopped Time
                TimeSpan ExecutionTime;//Var will count Total Execution Time-Our Main Hero                
                ExecutionStartTime = DateTime.Now; //Gets the system Current date time expressed as local time
                                
                double [] predictOutput = new double[0];

                Feature.PredictOnePixel(depthIndex, depth, ref predictOutput); 
                Console.WriteLine("background: {0}, open hand: {1}, close hand: {2}",predictOutput[0], predictOutput[1], predictOutput[2]);
                ExecutionStopTime = DateTime.Now;
                ExecutionTime = ExecutionStopTime - ExecutionStartTime;
                Console.WriteLine("Use {0} ms for getting prediction", ExecutionTime.TotalMilliseconds.ToString());
            }

            if ((ShowExtractedFeatureMode & ShowExtractedFeatureFormat.PredictAllPixelsCPU) == ShowExtractedFeatureFormat.PredictAllPixelsCPU) {
                DateTime ExecutionStartTime; //Var will hold Execution Starting Time
                DateTime ExecutionStopTime;//Var will hold Execution Stopped Time
                TimeSpan ExecutionTime;//Var will count Total Execution Time-Our Main Hero                
                ExecutionStartTime = DateTime.Now; //Gets the system Current date time expressed as local time

                double[] predictOutput = new double[0];
                
                for (depthIndex = 0; depthIndex < depth.Length; depthIndex++) {
                    int bitmapIndex = depthIndex * 4;                    
                    Feature.PredictOnePixel(depthIndex, depth, ref predictOutput);
                    int predictLabel = 0;                    
                    for (int i = 1; i < 3; i++)
                        if (predictOutput[i] > predictOutput[predictLabel])
                            predictLabel = i;
                    
                    if (predictLabel == 0)
                    {
                        overlayBitmapBits[bitmapIndex + 2] = 255;
                        overlayBitmapBits[bitmapIndex + 1] = 255;
                        overlayBitmapBits[bitmapIndex + 0] = 255;
                    }
                    else if (predictLabel == 1)
                    {
                        overlayBitmapBits[bitmapIndex + 2] = 255;
                        overlayBitmapBits[bitmapIndex + 1] = 0;
                        overlayBitmapBits[bitmapIndex + 0] = 0;
                    }
                    else if (predictLabel == 2)
                    {
                        overlayBitmapBits[bitmapIndex + 2] = 0;
                        overlayBitmapBits[bitmapIndex + 1] = 255;
                        overlayBitmapBits[bitmapIndex + 0] = 0;
                    }
                    /* 
                    overlayBitmapBits[bitmapIndex + 3] = 255;
                    overlayBitmapBits[bitmapIndex + 2] = 0; // 255;
                    overlayBitmapBits[bitmapIndex + 1] = 0; // 255;
                    overlayBitmapBits[bitmapIndex + 0] = 255;
                     */ 
                }
                //System.Threading.Thread.Sleep(1000);                
                ExecutionStopTime = DateTime.Now;
                ExecutionTime = ExecutionStopTime - ExecutionStartTime;
                Console.WriteLine("Use {0} ms for getting prediction", ExecutionTime.TotalMilliseconds.ToString());
                //updateHelper(); // update the current bitmap                
                //paused = true; // to take a look
                overlayStart = true;
            }
            
            if ((ShowExtractedFeatureMode & ShowExtractedFeatureFormat.ShowTransformedForOnePixel) == ShowExtractedFeatureFormat.ShowTransformedForOnePixel)
            {
                DateTime ExecutionStartTime; //Var will hold Execution Starting Time
                DateTime ExecutionStopTime;//Var will hold Execution Stopped Time
                TimeSpan ExecutionTime;//Var will count Total Execution Time-Our Main Hero
                ExecutionStartTime = DateTime.Now; //Gets the system Current date time expressed as local time             
                depthVal = depth[depthIndex]; // >> DepthImageFrame.PlayerIndexBitmaskWidth;
                listOfTransformedPairPosition.Clear();
                Feature.GetAllTransformedPairs(depthIndex, depthVal, listOfTransformedPairPosition);
                int bitmapIndex, X, Y;
                Array.Clear(overlayBitmapBits, 0, overlayBitmapBits.Length);

                for (int i = 0; i < listOfTransformedPairPosition.Count; i++)
                {
                    X = listOfTransformedPairPosition[i][0];
                    Y = listOfTransformedPairPosition[i][1];
                    if (X >= 0 && X < 640 && Y >= 0 && Y < 480)
                    {
                        bitmapIndex = (Y * 640 + X) * 4;
                        overlayBitmapBits[bitmapIndex + 2] = 255;
                        //bitmapBits[bitmapIndex + 2] = 255; // test
                    }
                    X = listOfTransformedPairPosition[i][2];
                    Y = listOfTransformedPairPosition[i][3];
                    if (X >= 0 && X < 640 && Y >= 0 && Y < 480)
                    {
                        bitmapIndex = (Y * 640 + X) * 4;
                        overlayBitmapBits[bitmapIndex + 2] = 255;
                        //bitmapBits[bitmapIndex + 2] = 255; // test 
                    }
                }
                ExecutionStopTime = DateTime.Now;
                ExecutionTime = ExecutionStopTime - ExecutionStartTime;
                Console.WriteLine("Use {0} ms for getting transformed points", ExecutionTime.TotalMilliseconds.ToString());
                updateHelper(); // update the current bitmap                
                paused = true; // to take a look
            }
             
            //if (paused) unPause(e);  // Why this? -Michael
                        
            // timer off 
             
            // Print HSL
            /*
            System.Drawing.Color color = System.Drawing.Color.FromArgb(bitmapBits[baseIndex + 2], bitmapBits[baseIndex + 1], bitmapBits[baseIndex]);
            float hue = color.GetHue();
            float saturation = color.GetSaturation();
            float lightness = color.GetBrightness();
            Console.WriteLine("HSL ({0:0.000}, {1:0.000}, {2:0.000})", hue, saturation, lightness);
            if (hue < MinHueTarget) MinHueTarget = hue;
            if (hue > MaxHueTarget) MaxHueTarget = hue;
            if (saturation < MinSatTarget) MinSatTarget = saturation;
            if (saturation > MaxSatTarget) MaxSatTarget = saturation;

            Console.WriteLine("Hue (Min,Max), {0:0.000}, {1:0.000}; Saturation (Min,Max), {2:0.000}, {3:0.000}", MinHueTarget, MaxHueTarget, MinSatTarget, MaxSatTarget);
            */
            // Print distance to a reference point
            //    Console.WriteLine("Distance between RGB({0}, {1}, {2}) and RGB(90, 175, 221) is {3}", bitmapBits[baseIndex + 2], bitmapBits[baseIndex + 1], bitmapBits[baseIndex], ColorDistance(new byte[] { bitmapBits[baseIndex + 2], bitmapBits[baseIndex + 1], bitmapBits[baseIndex] }, new byte[] { 90, 175, 221 }));


            // Extract feature from this point:
            /*
            double[] features = classifier.extract_features(depth, click_position);
            Console.Write("Features: [ ");
            foreach (double feature in features) Console.Write(feature + " ");
            Console.WriteLine("]");
             */
        }

        private void GetAllFeatures() {
            // timer start
            DateTime ExecutionStartTime; //Var will hold Execution Starting Time
            DateTime ExecutionStopTime;//Var will hold Execution Stopped Time
            TimeSpan ExecutionTime;//Var will count Total Execution Time-Our Main Hero

            ExecutionStartTime = DateTime.Now; //Gets the system Current date time expressed as local time
            for (int depthIndex = 0; depthIndex < depth.Length; depthIndex++) // test for looping through all pixels
            {
                short depthVal = depth[depthIndex];
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
        }

        public void ProcessAndSave()
        {
            lock (bitmapBits)
            {
                // Prevent the rest of the application from updating bitmapBits while it is being written.


                /*
            var directory = "..\\..\\..\\Data" + "\\" + HandGestureValue + RangeModeValue;  // assume the directory exist
            TimeSpan t = (DateTime.UtcNow - new DateTime(1970, 1, 1));
            string filename = t.TotalSeconds.ToString();
                */
            
            // rgb
                foreach (Step step in pipeline) process(step);
                
                bitmap.Dispatcher.Invoke(new Action(() =>
                {
                    bitmap.WritePixels(new Int32Rect(0, 0, bitmap.PixelWidth, bitmap.PixelHeight),
                        bitmapBits, bitmap.PixelWidth * sizeof(int), 0);
                }));

                var directory = "D:\\gr\\training\\blue\\" + HandGestureValue;
                //var directory = "..\\..\\..\\Data" + "\\" + HandGestureValue + RangeModeValue;  // assume the directory exist
                TimeSpan t = (DateTime.UtcNow - new DateTime(1970, 1, 1));
                string filename = t.TotalSeconds.ToString();

                //mapped depth  

                //sensor.MapDepthFrameToColorFrame(DepthImageFormat.Resolution640x480Fps30, depth, ColorImageFormat.RgbResolution640x480Fps30, mapped);
                //int[] mapped_depth = Enumerable.Repeat(-1, 640 * 480).ToArray() ;

                // Michael: Do we have to create this list? Can't we just write
                //          the depth val and label directly to the file?

                List<int[]> depthAndLabel = new List<int[]>(); // -1 means non-hand 
                using (StreamWriter filestream = new StreamWriter(directory + "\\" + "depthLabel_" + filename + ".txt"))
                {
                    for (int i = 0; i < depth.Length; i++)
                    {
                        int depthVal = depth[i] >> DepthImageFrame.PlayerIndexBitmaskWidth; // notice that the depth has been processed
                        byte label = depthLabel[i];
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
                bformatter.Serialize(stream, data);
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
            switch (step)
            {
                case Step.Depth: CopyDepth(); break;
                case Step.Color: CopyColor(); break;
                case Step.Crop: Crop(); break;
                case Step.PaintWhite: PaintWhite(); break;
                case Step.PaintGreen: PaintGreen(); break;
                case Step.MappedDepth: show_mapped_depth(); break;
                case Step.ColorMatch: MatchColors(); break;
                case Step.ColorLabelingInRGB: ColorLabellingInRGB(); break;
                case Step.OverlayOffset: ShowOverlayOffset(); break;
                case Step.Denoise: Denoise(); break;
            }
        }

        #region Filter functions
        
        private void Denoise()
        {
            int x, y;
            int totalSurrounding = 0;
            int width = 640, height = 480;
            int[] sumSurrounding = new int[] { 0, 0, 0 };

            // XXX: Doesn't work with cropping.
            for (int i = 0; i < bitmapBits.Length; i += 4)
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
                    sumSurrounding[0] += bitmapBits[((y - 1) * width + (x - 1)) * 4];
                    sumSurrounding[1] += bitmapBits[((y - 1) * width + (x - 1)) * 4 + 1];
                    sumSurrounding[2] += bitmapBits[((y - 1) * width + (x - 1)) * 4 + 2];
                    totalSurrounding++;
                }

                if (y != 0)
                {
                    sumSurrounding[0] += bitmapBits[((y - 1) * width + (x)) * 4];
                    sumSurrounding[1] += bitmapBits[((y - 1) * width + (x)) * 4 + 1];
                    sumSurrounding[2] += bitmapBits[((y - 1) * width + (x)) * 4 + 2];
                    totalSurrounding++;
                }

                if (y != 0 && x != width - 1)
                {
                    sumSurrounding[0] += bitmapBits[((y - 1) * width + (x + 1)) * 4];
                    sumSurrounding[1] += bitmapBits[((y - 1) * width + (x + 1)) * 4 + 1];
                    sumSurrounding[2] += bitmapBits[((y - 1) * width + (x + 1)) * 4 + 2];
                    totalSurrounding++;
                }

                if (x != width - 1)
                {
                    sumSurrounding[0] += bitmapBits[((y) * width + (x + 1)) * 4];
                    sumSurrounding[1] += bitmapBits[((y) * width + (x + 1)) * 4 + 1];
                    sumSurrounding[2] += bitmapBits[((y) * width + (x + 1)) * 4 + 2];
                    totalSurrounding++;
                }

                if (y != height - 1 && x != width - 1)
                {
                    sumSurrounding[0] += bitmapBits[((y + 1) * width + (x + 1)) * 4];
                    sumSurrounding[1] += bitmapBits[((y + 1) * width + (x + 1)) * 4 + 1];
                    sumSurrounding[2] += bitmapBits[((y + 1) * width + (x + 1)) * 4 + 2];
                    totalSurrounding++;
                }

                if (y != height - 1)
                {
                    sumSurrounding[0] += bitmapBits[((y + 1) * width + (x)) * 4];
                    sumSurrounding[1] += bitmapBits[((y + 1) * width + (x)) * 4 + 1];
                    sumSurrounding[2] += bitmapBits[((y + 1) * width + (x)) * 4 + 2];
                    totalSurrounding++;
                }

                if (y != height - 1 && x != 0)
                {
                    sumSurrounding[0] += bitmapBits[((y + 1) * width + (x - 1)) * 4];
                    sumSurrounding[1] += bitmapBits[((y + 1) * width + (x - 1)) * 4 + 1];
                    sumSurrounding[2] += bitmapBits[((y + 1) * width + (x - 1)) * 4 + 2];
                    totalSurrounding++;
                }

                if (x != 0)
                {
                    sumSurrounding[0] += bitmapBits[((y) * width + (x - 1)) * 4];
                    sumSurrounding[1] += bitmapBits[((y) * width + (x - 1)) * 4 + 1];
                    sumSurrounding[2] += bitmapBits[((y) * width + (x - 1)) * 4 + 2];
                    totalSurrounding++;
                }

                tmpBuffer[i] = (byte)(sumSurrounding[0] / totalSurrounding);
                tmpBuffer[i + 1] = (byte)(sumSurrounding[1] / totalSurrounding);
                tmpBuffer[i + 2] = (byte)(sumSurrounding[2] / totalSurrounding);
                tmpBuffer[i + 3] = 255;
            }

            Array.Copy(tmpBuffer, bitmapBits, tmpBuffer.Length);
        }

        // Copies over the depth value to the buffer, normalized for the range of shorts.
        private void CopyDepth()
        {
            for (int x = crop.X; x <= crop.Width + crop.X; x++)
            {
                for (int y = crop.Y; y <= crop.Height + crop.Y; y++)
                {
                    int idx = Util.toID(x, y, width, height, depthStride);
                    bitmapBits[4 * idx] =
                        bitmapBits[4 * idx + 1] =
                        bitmapBits[4 * idx + 2] =
                        bitmapBits[4 * idx + 3] = (byte)(255 * (short.MaxValue - depth[idx]) / short.MaxValue);
                }
            }
        }

        // Copies over the RGB value to the buffer.
        private void CopyColor()
        {
            for (int x = crop.X; x <= crop.Width + crop.X; x++) 
            {
                for (int y = crop.Y; y <= crop.Height + crop.Y; y++)
                {
                    int idx = Util.toID(x, y, width, height, colorStride);
                    Array.Copy(rgb, idx, bitmapBits, idx, colorStride);
                }
            }
        }

        // Adjusts the cropping parameters
        private void Crop()
        {
            crop.X = cropValues.X; crop.Y = cropValues.Y;
            crop.Width = cropValues.Width; crop.Height = cropValues.Height;
        }

        // Sets everything within the crop to white.
        private void PaintWhite()
        {
            for (int x = crop.X; x <= crop.Width + crop.X; x++)
            {
                for (int y = crop.Y; y <= crop.Height + crop.Y; y++)
                {
                    int idx = Util.toID(x, y, width, height, colorStride);
                    bitmapBits[idx] = 
                    bitmapBits[idx + 1] = 
                    bitmapBits[idx + 2] = 255;
                }
            }
        }

        private void PaintGreen()
        {
            for (int x = crop.X; x <= crop.Width + crop.X; x++)
            {
                for (int y = crop.Y; y <= crop.Height + crop.Y; y++)
                {
                    int idx = Util.toID(x, y, width, height, colorStride);
                    bitmapBits[idx] = 153;
                    bitmapBits[idx + 1] = 204;
                    bitmapBits[idx + 2] = 153;
                }
            }
        }

        private void show_mapped_depth()
        {
            //ColorImagePoint[] mapped = new ColorImagePoint[depth.Length];
            sensor.MapDepthFrameToColorFrame(DepthImageFormat.Resolution640x480Fps30, depth, ColorImageFormat.RgbResolution640x480Fps30, mapped);
            for (int i = 0; i < depth.Length; i++)
            {
                int depthVal = depth[i] >> DepthImageFrame.PlayerIndexBitmaskWidth;
                if ((depthVal <= upper) && (depthVal > lower))
                {
                    ColorImagePoint point = mapped[i];

                    int baseIndex = (point.Y * 640 + point.X) * 4;
                    if ((point.X >= 0 && point.X < 640) && (point.Y >= 0 && point.Y < 480) && bitmapBits[baseIndex] != 0)
                    {
                        bitmapBits[baseIndex] = rgb[baseIndex];
                        bitmapBits[baseIndex + 1] = rgb[baseIndex + 1];
                        bitmapBits[baseIndex + 2] = rgb[baseIndex + 2];
                    }
                }
            }
        }

        // Mainly for labelling.  Matches the rgb to the nearest color. The set of colors are in listed in the array "colors"
        private void MatchColors()
        {
            sensor.MapDepthFrameToColorFrame(DepthImageFormat.Resolution640x480Fps30, depth, ColorImageFormat.RgbResolution640x480Fps30, mapped);
            Array.Clear(depthLabel, 0, depthLabel.Length);  // background label is 0. So can use Clear method.
            for (int i = 0; i < depth.Length; i++)
            {
                int depthVal = depth[i] >> DepthImageFrame.PlayerIndexBitmaskWidth;
                if ((depthVal <= upper) && (depthVal > lower))
                {
                    ColorImagePoint point = mapped[i];
                    int baseIndex = (point.Y * 640 + point.X) * 4;

                    if (point.X > crop.X && point.X <= (crop.X + crop.Width) &&
                        point.Y > crop.Y && point.Y <= (crop.Y + crop.Height))
                    {
                        rgb_tmp[0] = rgb[baseIndex + 2];
                        rgb_tmp[1] = rgb[baseIndex + 1];
                        rgb_tmp[2] = rgb[baseIndex];

                        byte label = nearest_color(rgb_tmp);
                        depthLabel[i] = label;

                        bitmapBits[baseIndex] = rgb_tmp[2];
                        bitmapBits[baseIndex + 1] = rgb_tmp[1];
                        bitmapBits[baseIndex + 2] = rgb_tmp[0];
                    }
                }
            }
        }

        // Label the RGB image without involving depth image
        private void ColorLabellingInRGB()
        {
            byte[] bgr = rgb;
            byte[] rgb_tmp = new byte[3];
            for (int i = 0; i < bgr.Length; i += 4)
            {
                bitmapBits[i + 3] = 255;
                rgb_tmp[0] = bgr[i + 2];
                rgb_tmp[1] = bgr[i + 1];
                rgb_tmp[2] = bgr[i];

                setLabel(rgb_tmp); // do the labeling

                bitmapBits[i] = rgb_tmp[2];
                bitmapBits[i + 1] = rgb_tmp[1];
                bitmapBits[i + 2] = rgb_tmp[0];
            }
        }

        private void ShowOverlayOffset() {
            if ((ShowExtractedFeatureMode & ShowExtractedFeatureFormat.PredictAllPixelsCPU) == ShowExtractedFeatureFormat.PredictAllPixelsCPU)
            {
                if (overlayStart)
                    overlayBitmapBits.CopyTo(bitmapBits, 0);
            }
            else 
            {
                for (int i = 0; i < bitmapBits.Length; i += 4)
                {

                    if (overlayBitmapBits[i + 2] == 255)
                    {
                        bitmapBits[i + 2] = 255;
                        bitmapBits[i + 1] = 0;
                        bitmapBits[i] = 0;
                    }
                }
            }
        }

        #endregion

        private void updateHelper()
        {
            bitmap.Dispatcher.Invoke(new Action(() =>
            {
                bitmap.WritePixels(new Int32Rect(0, 0, bitmap.PixelWidth, bitmap.PixelHeight),
                    bitmapBits, bitmap.PixelWidth * sizeof(int), 0);
            }));
        }

        public void update(KinectData data)
        {
            if (paused) return;
            this.data = data;
            this.depth = data.depth();
            this.rgb = data.color();
            //Array.Clear(bitmapBits, 0, bitmapBits.Length); // Zero-all the bitmapBits                    
            foreach (Step step in pipeline) process(step);
            updateHelper();

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
