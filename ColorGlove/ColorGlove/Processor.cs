/*
using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows.Documents;
using System.Windows;
using System.Windows.Controls;
//using System.Windows.Data;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Input;
//using System.Windows.Navigation;
//using System.Windows.Shapes;
using System.Diagnostics;
using System.Drawing.Imaging;
using Microsoft.Kinect;
using FeatureExtractionLib;
*/

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
using System.Drawing.Imaging;
using Microsoft.Kinect;
using FeatureExtractionLib;

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

        private RangeModeFormat RangeModeValue = RangeModeFormat.Near;
        static private Dictionary<Tuple<byte, byte, byte>, byte> nearest_cache = new Dictionary<Tuple<byte, byte, byte>, byte>(); // It seems not necessary to save the mapped result as byte[], byte should be enough.
        private WriteableBitmap bitmap;
        private byte[] bitmapBits;
        private byte[] overlayBitmapBits;
        private Image image;
        public int lower, upper; // range for thresholding in show_mapped_depth(),  show_color_depth(). Set by the manager.
        public enum Step
        {
            PaintWhite,
            Color,
            Depth,
            Crop,
            MappedDepth,
            ColorMatch,
            ColorLabelingInRGB,
            OverlayOffset,
            Denoise,
        };

        public enum HandGestureFormat
        {
            OpenHand = 1,
            CloseHand = 2,
        };

        private HandGestureFormat HandGestureValue;
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
        private byte[] rgb_tmp = new byte[3];
        private ColorImagePoint[] mapped;
        private byte[] depthLabel;
        private float MinHueTarget, MaxHueTarget, MinSatTarget, MaxSatTarget; // max/min hue value of the target color. Used for hue detection


        private const float DesiredMinHue = 198f - .5f, DesiredMaxHue = 214f + .5f,
                                  DesiredMinSat = 0.174f, DesiredMaxSat = 0.397f; // Used for hue dection

        // Used for cropping.
        int x_0 = 0, x_1 = 640, y_0 = 0, y_1 = 480;


        byte[] color = new byte[3];
        double[] tmp_point = new double[3];
        double[] tmp_point2 = new double[3];
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
        List<int[]> listOfOffsetPosition;
        

        public Processor(KinectSensor sensor)
        {
            Debug.WriteLine("Start processor contruction");
            this.sensor = sensor;
            image = new Image();
            image.Width = 640;
            image.Height = 480;
            mapped = new ColorImagePoint[640 * 480];
            depthLabel = new byte[640 * 480];

            MinHueTarget = 360.0F;
            MaxHueTarget = 0.0F;
            MinSatTarget = 1F;
            MaxSatTarget = 0F;

            // Setup FeatureExtraction Class
            FeatureExtraction.OffsetModeFormat OffsetMode = FeatureExtraction.OffsetModeFormat.PairsOf2000UniformDistribution;
            FeatureExtraction.KinectModeFormat KinectMode = FeatureExtraction.KinectModeFormat.Near;
            Feature = new FeatureExtraction(KinectMode, OffsetMode);
            Feature.ReadOffsetPairsFromFile();

            //classifier = new Classifier();
            this.bitmap = new WriteableBitmap(640, 480, 96, 96, PixelFormats.Bgr32, null);
            this.bitmapBits = new byte[640 * 480 * 4];
            overlayBitmapBits = new byte[640 * 480 * 4]; // overlay
            image.Source = bitmap;

            image.MouseLeftButtonUp += image_click;

            SetCentroidColorAndLabel();
            
            // Offset displaying is on.
            // Generate offset pair
            /*
            int minOffset = 50 * 2000;
            int maxOffset = 500;
            Feature.SetOffsetMax(minOffset);
            Feature.SetOffsetMin(maxOffset);
            Feature.GenerateOffsetPairs(2000);
            */
            listOfOffsetPosition = new List<int[]>(); // remember to clear.
            Debug.WriteLine("Pass processor setting");
        }

        private void SetCentroidColorAndLabel()
        {
            // First add label
            HandGestureValue = HandGestureFormat.CloseHand; // Which hand gesture;
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

            // For background color 

            AddCentroid(80, 80, 80, backgroundLabel);
            AddCentroid(250, 240, 240, backgroundLabel);
            AddCentroid(210, 180, 150, backgroundLabel);
            AddCentroid(110, 86, 244, backgroundLabel);
            AddCentroid(75, 58, 151, backgroundLabel);
            AddCentroid(153, 189, 206, backgroundLabel);
            AddCentroid(214, 207, 206, backgroundLabel);
            AddCentroid(122, 124, 130, backgroundLabel);

        }

        public void increaseRange()
        {
            upper += 10;
        }

        public void decreaseRange()
        {
            upper -= 10;
        }

        public void kMeans()
        {
            int k = 5;
            // k = 5 seems to do well with background cleared out.
            
            Random rand = new Random();

            // Randomly create K colors
            double[,] clusters = new double[k, 3];
            for (int i = 0; i < k; i++) {
                clusters[i, 0] = rand.Next(0, 255);
                clusters[i, 1] = rand.Next(0, 255);
                clusters[i, 2] = rand.Next(0, 255);
            }

            int width = x_1 - x_0;
            int height = y_1 - y_0;

            int[] points = new int[width*height];
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

            double delta = 10000, epsilon = 10;

            double minDistance = 10000;
            int minCluster = -1;
            while (delta > epsilon)
            {
                Console.WriteLine("Delta: " + delta);
                // Step 1: label each point as a cluster
                for (int i = 0; i < points.Length; i++)
                {
                    int y = i / width;
                    int x = i % width;
                    int adjusted_y = y_0 + y;
                    int adjusted_x = x_0 + x;
                    int adjusted_i = adjusted_y * 640 + adjusted_x;

                    point[0] = rgb[adjusted_i * 4 + 2];
                    point[1] = rgb[adjusted_i * 4 + 1];
                    point[2] = rgb[adjusted_i * 4];

                    minDistance = 10000;
                    for (int idx = 0; idx < clusters.GetLength(0); idx++)
                    {
                        tmp_point2[0] = clusters[idx, 0];
                        tmp_point2[1] = clusters[idx, 1];
                        tmp_point2[2] = clusters[idx, 2];

                        double distance = euc_distance(point, tmp_point2);
                        if (distance < minDistance)
                        {
                            minCluster = idx;
                            minDistance = distance;
                        }
                    }

                    points[i] = minCluster;
                    cluster_count[minCluster] ++;

                }

                // Step 2: update the cluster center values
                for (int i = 0; i < points.Length; i++)
                {
                    int y = i / width;
                    int x = i % width;
                    int adjusted_y = y_0 + y;
                    int adjusted_x = x_0 + x;
                    int adjusted_i = adjusted_y * 640 + adjusted_x;

                    cluster_centers[points[i], 0] += rgb[adjusted_i * 4 + 2];
                    cluster_centers[points[i], 1] += rgb[adjusted_i * 4 + 1];
                    cluster_centers[points[i], 2] += rgb[adjusted_i * 4];
                }

                for (int i = 0; i < k; i++)
                {
                    double r = cluster_centers[i, 0] / cluster_count[i];
                    double g = cluster_centers[i, 1] / cluster_count[i];
                    double b = cluster_centers[i, 2] / cluster_count[i];

                    tmp_point[0] = clusters[i, 0];
                    tmp_point[1] = clusters[i, 1];
                    tmp_point[2] = clusters[i, 2];

                    tmp_point2[0] = r;
                    tmp_point2[1] = g;
                    tmp_point2[2] = b;

                    cluster_deltas[i] = euc_distance(tmp_point, tmp_point2);

                    clusters[i, 0] = r;
                    clusters[i, 1] = g;
                    clusters[i, 2] = b;

                    if (max_cluster == -1 || cluster_count[i] > cluster_count[max_cluster]) 
                        max_cluster = i;
                }

                delta = cluster_deltas[0];
                for (int i = 1; i < k; i++)
                    if (cluster_deltas[i] > delta) delta = cluster_deltas[i];

            }

            clearCentroids();
            
            for (int i = 0; i < k; i++)
            {
                byte label;
                if (i == max_cluster) label = this.targetLabel;
                else label = this.backgroundLabel;

                addCentroid((byte)clusters[i, 0], (byte)clusters[i, 1], (byte)clusters[i, 2], label);
            }
            
            // Uncomment this line for stock color replacement.
            //replacement = colors;
            
            Processor.nearest_cache.Clear();
            Console.WriteLine("Done!");

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

        private void image_click(object sender, MouseButtonEventArgs e)
        {
            Point click_position = e.GetPosition(image);
            int baseIndex = ((int)click_position.Y * 640 + (int)click_position.X) * 4;
            Console.WriteLine("(x,y): (" + click_position.X + ", " + click_position.Y + ") RGB: {" + bitmapBits[baseIndex + 2] + ", " + bitmapBits[baseIndex + 1] + ", " + bitmapBits[baseIndex] + "}");
            int depthIndex = (int)click_position.Y * 640 + (int)click_position.X;
            int depthVal = depth[depthIndex] >> DepthImageFrame.PlayerIndexBitmaskWidth;
            // Show offsets pair 
            Console.WriteLine("depth: {0}, baseIndex: {1}", depthVal, depthIndex);

            listOfOffsetPosition.Clear();

            Feature.GetAllOffsetPairs(depthIndex, depthVal, listOfOffsetPosition);
            int bitmapIndex, X, Y;
            Array.Clear(overlayBitmapBits, 0, overlayBitmapBits.Length);
            
            for (int i = 0; i < listOfOffsetPosition.Count; i++)
            {
                X = listOfOffsetPosition[i][0];
                Y = listOfOffsetPosition[i][1];
                if (X >= 0 && X < 640 && Y >= 0 && Y < 480)
                {
                    bitmapIndex = (Y * 640 + X) * 4;                    
                    overlayBitmapBits[bitmapIndex + 2] = 255;                                        
                }
                X = listOfOffsetPosition[i][2];
                Y = listOfOffsetPosition[i][3];
                if (X >= 0 && X < 640 && Y >= 0 && Y < 480)
                {
                    bitmapIndex = (Y * 640 + X) * 4;                    
                    overlayBitmapBits[bitmapIndex + 2] = 255;
                }
            }


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

        public void processAndSave()
        {
            foreach (Step step in pipeline) process(step, depth, rgb);
            bitmap.Dispatcher.Invoke(new Action(() =>
            {
                bitmap.WritePixels(new Int32Rect(0, 0, bitmap.PixelWidth, bitmap.PixelHeight),
                    bitmapBits, bitmap.PixelWidth * sizeof(int), 0);
            }));

            var directory = "training_samples" + "\\" + HandGestureValue;  // should check here.
            TimeSpan t = (DateTime.UtcNow - new DateTime(1970, 1, 1));
            string filename = t.TotalSeconds.ToString();

            // rgb
            using (StreamWriter filestream = new StreamWriter(directory + "\\" + filename + "_rgb.txt"))
            {
                filestream.Write(rgb[0]);
                for (int i = 1; i < rgb.Length; i++) filestream.Write(" " + rgb[i]);
            }

            //mapped depth

            //sensor.MapDepthFrameToColorFrame(DepthImageFormat.Resolution640x480Fps30, depth, ColorImageFormat.RgbResolution640x480Fps30, mapped);
            //int[] mapped_depth = Enumerable.Repeat(-1, 640 * 480).ToArray() ;
            List<int[]> depthAndLabel = new List<int[]>(); // -1 means non-hand 
            using (StreamWriter filestream = new StreamWriter(directory + "\\" + "depthLabel_" + filename + ".txt"))
            {
                for (int i = 0; i < depth.Length; i++)
                {
                    int depthVal = depth[i] >> DepthImageFrame.PlayerIndexBitmaskWidth;
                    byte label = depthLabel[i];                    
                    depthAndLabel.Add(new int[] { depthVal, label });
                }
                /* Output file format: 
                (depthVal, label) (depthVal, label) (depthVal, label) (depthVal, label) ...
                 */
                filestream.Write("({0},{1})", depthAndLabel[0][0], depthAndLabel[0][1]);
                for (int i = 1; i < depthAndLabel.Count; i++) filestream.Write(" ({0},{1})", depthAndLabel[i][0], depthAndLabel[i][1]);
            }

            
        }

        public Image getImage() { return image; }

        public void updatePipeline(params Step[] steps)
        {
            pipeline = new Step[steps.Length];
            for (int i = 0; i < steps.Length; i++) pipeline[i] = steps[i];
        }

        private void process(Step step, short[] depth, byte[] rgb)
        {
            switch (step)
            {
                case Step.Depth: show_depth(depth, rgb); break;
                case Step.Color: show_color(depth, rgb); break;
                case Step.Crop: crop_color(depth, rgb); break;
                case Step.PaintWhite: paint_white(depth, rgb); break;
                case Step.MappedDepth: show_mapped_depth(depth, rgb); break;
                case Step.ColorMatch: show_color_match(depth, rgb); break;
                case Step.ColorLabelingInRGB: ColorLabellingInRGB(rgb); break;
                case Step.OverlayOffset: ShowOverlayOffset(); break;
                case Step.Denoise: Denoise(); break;
            }
        }

        #region Filter functions
        private void Denoise()
        {
        }

        private void show_depth(short[] depth, byte[] rgb)
        {
            for (int i = 0; i < depth.Length; i++)
            {
                bitmapBits[4 * i] = bitmapBits[4 * i + 1] = bitmapBits[4 * i + 2] = (byte)(255 * (short.MaxValue - depth[i]) / short.MaxValue);
            }
        }

        private void show_color(short[] depth, byte[] rgb)
        {
            bitmapBits = rgb;
        }

        private void crop_color(short[] depth, byte[] rgb)
        {
            //byte[] bitmapBits = new byte[(x_1 - x_0) * (y_1 - y_0) * 4];
            //this.bitmap = new WriteableBitmap((x_1 - x_0), (y_1 - y_0), 96, 96, PixelFormats.Bgr32, null);
            //image.Source = bitmap;

            x_0 = 220; x_1 = 390; y_0 = 150; y_1 = 362;

            for (int i = 0; i < depth.Length; i++)
            {
                //Console.WriteLine(_depthPixels[i]);
                int max = 32767;

                int y = i / 640;
                int x = i % 640;


                if (x >= x_0 && x < x_1 && y >= y_0 && y < y_1)
                {
                    bitmapBits[4 * i] = rgb[4 * i];
                    bitmapBits[4 * i + 1] = rgb[4 * i + 1];
                    bitmapBits[4 * i + 2] = rgb[4 * i + 2];
                    bitmapBits[4 * i + 3] = rgb[4 * i + 3];
                }
                else
                {
                    bitmapBits[4 * i] =
                    bitmapBits[4 * i + 1] =
                    bitmapBits[4 * i + 2] =
                    bitmapBits[4 * i + 3] = 0;
                }
            }
        }

        private void paint_white(short[] depth, byte[] rgb)
        // make everything white except for something
        {
            //for (int i = 0; i < rgb.Length; i++) if (bitmapBits[i] != 0) bitmapBits[i] = 255; // Michael: why "if (bitmapBits[i] != 0)"
            for (int i = 0; i < rgb.Length; i++) bitmapBits[i] = 255;
            //Array.Clear(bitmapBits, 0, bitmapBits.Length);
        }

        private void show_mapped_depth(short[] depth, byte[] rgb)
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

        private void show_color_match(short[] depth, byte[] rgb)  // Is it necessary to pass the arguments? Since they are alreay private members.
        // Mainly for labelling.  Matches the rgb to the nearest color. The set of colors are in listed in the array "colors"
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

                    if ((point.X >= 0 && point.X < 640) && (point.Y >= 0 && point.Y < 480) && bitmapBits[baseIndex] != 0)
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

        private void ColorLabellingInRGB(byte[] bgr)
        // Label the RGB image without involving depth image
        {
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
            for (int i = 0; i < bitmapBits.Length; i += 4) {
                if (overlayBitmapBits[i + 2] == 255) {
                    bitmapBits[i + 2] = 255;
                    bitmapBits[i + 1] = 0;
                    bitmapBits[i ] = 0;
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

        public void update(short[] depth, byte[] rgb)
        {
            this.depth = depth;
            this.rgb = rgb;
            //Array.Clear(bitmapBits, 0, bitmapBits.Length); // Zero-all the bitmapBits                    
            foreach (Step step in pipeline) process(step, depth, rgb);
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

            for (int idx = 0; idx < centroidColor.Count; idx++)
            {
                double distance = EuclideanDistance(point, centroidColor[idx]);
                if (distance < minDistance)
                {
                    minColorLabel = centroidLabel[idx];
                    minDistance = distance;
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
