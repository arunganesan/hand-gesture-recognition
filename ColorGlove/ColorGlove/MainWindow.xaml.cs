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
using System.Threading;

using System.Diagnostics;
using System.Drawing.Imaging;
using Microsoft.Kinect;

/*
 * Borrowed some code from: http://social.msdn.microsoft.com/Forums/en-US/kinectsdknuiapi/thread/c39bab30-a704-4de1-948d-307afd128dab
 */

namespace ColorGlove
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {

        private Manager m;
        public MainWindow()
        {
            InitializeComponent();
            m = new Manager(this);
            m.start();
        }

        private void Window_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Space)
                m.toggleProcessors();
            else if (e.Key == Key.S)
                m.saveImages();
            else if (e.Key == Key.K)
                m.kMeans();
            else if (e.Key == Key.Up)  // use up/down key to adjust range
                m.increaseRange(); 
            else if (e.Key == Key.Down)
                m.decreaseRange();
        }
    }


    public class Manager
    {
        private enum RangeModeFormat
        {
            Default = 0, // If you're using Kinect Xbox you should use Default in the KinectSetting region.
            Near = 1,
        };
        private RangeModeFormat RangeModeValue;
        private KinectSensor sensor;
        private byte[] colorPixels;
        private short[] depthPixels;
        private Processor [] processors;
        private int DepthTimeout;
        private int ColorTimeout;

        Thread poller;

        public Manager(MainWindow parent)  // Construct function
        {
            // Initialize Kinect, set near mode here
            #region KinectSetting
            RangeModeValue = RangeModeFormat.Near; // If it's kinect for Xbox, set to Default.
            DepthTimeout = 1000; // Timeout for the next fram in ms 
            ColorTimeout = 1000; // Timeout for the next fram in ms 
            int lower = 100, upper = 1000; // thresholding parameters. Can be adjusted by Up/Down key.
            #endregion

            #region Create sensor
            KinectSensor.KinectSensors.StatusChanged += (object sender, StatusChangedEventArgs e) =>
            {
                if (e.Sensor == sensor && e.Status != KinectStatus.Connected) setSensor(null);
                else if ((sensor == null) && (e.Status == KinectStatus.Connected)) setSensor(e.Sensor);
            };

            foreach (var sensor in KinectSensor.KinectSensors)
                if (sensor.Status == KinectStatus.Connected) setSensor(sensor);
            #endregion  

            #region Create and arrange Images
            int total_processors = 2;
            processors = new Processor[total_processors];
            for (int i = 0; i < total_processors; i++)
            {
                processors[i] = new Processor(this.sensor);
                processors[i].lower = lower;
                processors[i].upper = upper;
                Image image = processors[i].getImage();
                parent.mainContainer.Children.Add(image);
            }
            #endregion

            #region Processor configurations


            //processors[1].updatePipeline(Processor.Step.ColorMatch);
            //processors[2].updatePipeline(Processor.Step.ColorMatch);
            
            processors[0].updatePipeline(
                // Show the rgb image
                                         //Processor.Step.Color
                // Show the depth image                                         
                                        // Processor.Step.Depth    
                                         
                // Show the Color Labelling                                         
                                         Processor.Step.PaintWhite,
                                         Processor.Step.ColorMatch
                                          
           ); 
                                        
            processors[1].updatePipeline(
                // Show the rgb image
                                        Processor.Step.Color
                // Show Mapped Depth Using RGB
                                        //Processor.Step.PaintWhite,
                                        //Processor.Step.MappedDepth                                        
            );                            

            #endregion

            poller = new Thread(new ThreadStart(this.poll));
        }

        private void setSensor(KinectSensor newSensor)
        {
            if (sensor != null) sensor.Stop();
            sensor = newSensor;
            if (sensor != null)
            {
                Debug.Assert(sensor.Status == KinectStatus.Connected, "This should only be called with Connected sensors.");
                sensor.ColorStream.Enable(ColorImageFormat.RgbResolution640x480Fps30);
                sensor.DepthStream.Enable(DepthImageFormat.Resolution640x480Fps30);

                // Can set this in the construct function {// Commented out for XBox Kinect}
                if (RangeModeValue == RangeModeFormat.Near)
                    sensor.DepthStream.Range = DepthRange.Near; // set near mode 

                colorPixels = new byte[640 * 480 * 4];
                depthPixels = new short[640 * 480];
                sensor.Start();
            }
        }

        public void start()
        {
            poller.Start();
        }

        public void toggleProcessors()
        {

            if (poller.ThreadState == System.Threading.ThreadState.Suspended) poller.Resume(); // Michael: Visual Studo said it's deprecated. Need to change?
            else poller.Suspend();
            //else poller.Resume();
        }

        public void saveImages()
        {
            poller.Suspend();
            //foreach (Processor p in processors) p.processAndSave();
            processors[0].processAndSave();
            poller.Resume();
        }

        public void increaseRange()
        {
            processors[0].upper += 10;
            processors[1].upper += 10;
        }

        public void decreaseRange()
        {
            processors[0].upper -= 10;
            processors[1].upper -= 10;
        }

        public void poll()
        {
            while (true)
            {
                using (var frame = sensor.DepthStream.OpenNextFrame(DepthTimeout))
                    if (frame != null) frame.CopyPixelDataTo(depthPixels);

                using (var frame = sensor.ColorStream.OpenNextFrame(ColorTimeout))
                    if (frame != null) frame.CopyPixelDataTo(colorPixels);

                // Start all processing simultaneously in separate threads
                // XXX: For now, just send data to first processor
                foreach (Processor p in processors) p.update(depthPixels, colorPixels);
            }
        }


        public void kMeans()
        {
            processors[0].kMeans();
        }
    }

    // Michael: Maybe let the classifier region unattentive, since I am working on feature extraction now.
    
    #region classifer
    /*
    public class Classifier
    {
        Tuple<Vector, Vector>[] features;

        // "If an offset pixel lies on the background or outside the 
        // bounds of the image, the depth prove d_I(x') is given a large 
        // positive constant value"
        double outOfBounds = 10000;

        int width = 640, height = 480;

        public Classifier()
        {
            // Define features
            features = new Tuple<Vector, Vector>[2] {
                new Tuple<Vector, Vector>(new Vector(0,0), new Vector(0,-1000)), 
                new Tuple<Vector, Vector>(new Vector(1000, 1000), new Vector(-1000, 1000))
            };
        }

        public double [] extract_features(short[] depth, Point x)
        {
            double[] feature_vectors = new double[features.Length];
            for (int i = 0; i < feature_vectors.Length; i++) feature_vectors[i] = extract_feature(depth, i, x);
            return feature_vectors;

        }

        public double extract_feature(short[] depth, int idx, Point x) 
        {
            Debug.Assert(idx <= features.Length, "Trying to access nonexistent feature.");
            
            //return (double)depth[(int)(x.Y * width + x.X)];
            int x_linear = (int)(x.Y * width + x.X);
            if (depth[x_linear] < 0) return -1;
            
            // The depth in mm!
            //(double)(depth[(int)(x.Y * width + x.X)] >> DepthImageFrame.PlayerIndexBitmaskWidth);

            double depth_in_mm = (double)(depth[x_linear] >> DepthImageFrame.PlayerIndexBitmaskWidth);

            Tuple<Vector, Vector> feature = features[idx];
            
            Point x_u = x + feature.Item1 / depth_in_mm;
            double x_u_depth;
            if (x_u.X < 0 || x_u.X >= width || x_u.Y < 0 || x_u.Y >= height)
                x_u_depth = outOfBounds;
            else
            {
                int lin = (int)(x_u.Y * width + x_u.X);
                x_u_depth = depth[lin] >> DepthImageFrame.PlayerIndexBitmaskWidth;
                if (x_u_depth == -1) return -1;
            }


            Point x_v = x + feature.Item2 / depth_in_mm;
            double x_v_depth;
            if (x_v.X < 0 || x_v.X >= width || x_v.Y < 0 || x_v.Y >= height)
                x_v_depth = outOfBounds;
            else
            {
                int lin = (int)(x_v.Y * width + x_v.X);
                x_v_depth = depth[lin] >> DepthImageFrame.PlayerIndexBitmaskWidth;
                if (x_v_depth == -1) return -1;
            }

            return x_u_depth - x_v_depth;
        }
    }
     */ 
    #endregion

    public class Processor
    {

        //private Dictionary<Tuple<byte, byte, byte>, byte[]> nearest_cache = new Dictionary<Tuple<byte, byte, byte>, byte[]>();
        private enum RangeModeFormat
        {
            Default = 0, // If you're using Kinect Xbox you should use Default
            Near = 1,
        };

        private RangeModeFormat RangeModeValue = RangeModeFormat.Near;
        private Dictionary<Tuple<byte, byte, byte>, byte> nearest_cache = new Dictionary<Tuple<byte, byte, byte>, byte>(); // It seems not necessary to save the mapped result as byte[], byte should be enough.
        private WriteableBitmap bitmap;
        private byte[] bitmapBits;
        private Image image;
        public int lower, upper; // range for thresholding in show_mapped_depth(),  show_color_depth(). Set by the manager.
        public enum Step {
            PaintWhite, 
            Color, 
            Depth, 
            Crop, 
            MappedDepth, 
            ColorMatch,
            ColorLabelingInRGB,
        };

        public enum HandGestureFormat { 
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


        private const float DesiredMinHue = 198f - .5f , DesiredMaxHue = 214f + .5f,
                                  DesiredMinSat =  0.174f, DesiredMaxSat = 0.397f; // Used for hue dection

        // Used for cropping.
        int x_0 = 220, x_1 = 390, y_0 = 150, y_1 = 362;


        byte[] color = new byte[3];
        double[] tmp_point = new double[3];
        double[] tmp_point2 = new double[3];
        private Step [] pipeline = new Step[0];
        private KinectSensor sensor;

        //private Classifier classifier;
        
        private readonly byte[] targetColor = new byte[] {255, 0, 0};
        private readonly byte[] backgroundColor = new byte[] { 255, 255, 255 };

        private List<byte[]> centroidColor = new List<byte[]> (); // the color of the centroid
        private List<byte> centroidLabel = new List<byte> (); // the label of the centroid        
        private Dictionary<byte, byte[]> labelColor = new Dictionary<byte, byte[]>();

        byte targetLabel, backgroundLabel;

        /*
        {
                                  // start of target color
            {145, 170, 220},
            {170, 190, 250},
            {96,152,183},
            {180,211,230},
            {156,196,221}, 
            {80, 112, 144},
            {68, 99, 133},
            {76, 103, 141},
            {122, 154, 173},
            {120, 138, 162},
                                 // end of target color
                                 // start of background color
            {80, 80, 80},
            {250, 240, 240},
            {210, 180, 150},
            {110, 86, 244},
            {75,58,151},
            {153, 189, 206},
            {214, 207, 206},
            {122, 124, 130}
                                // end of background color
        };
        */
        /*
        byte[,] replacement = new byte[,]  // used for labeling
        {
            {255,0,0},
            {255,0,0},
            {255,0,0},
            {255,0,0},
            {255,0,0},
            {255,0,0},
            {255,0,0},
            {255,0,0},
            {255,0,0},
            {255,0,0},
                                // end
            {255,255,255},
            {255,255,255},
            {255,255,255},
            {255,255,255},
            {255,255,255},
            {255,255,255},
            {255,255,255},
            {255,255,255},
            {255,255,255}
        };
        */

        /*
        byte[,] colors = new byte[,] {
              {140, 140, 140},   // White  
              {30, 30, 85},      // Blue
              {55, 90, 70},      // Green
              {115, 30, 100}     // Pink
            };

        byte[,] replacement = new byte[,] {
              {255, 255, 255},   // White  
              {0, 0, 255},      // Blue
              {0, 255, 0},      // Green
              {255, 0, 0}     // Red
            };
        */

        public Processor(KinectSensor sensor)
        {
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
            
            //classifier = new Classifier();

            this.bitmap = new WriteableBitmap(640, 480, 96, 96, PixelFormats.Bgr32, null);
            this.bitmapBits = new byte[640 * 480 * 4];
            image.Source = bitmap;

            image.MouseLeftButtonUp += image_click;

            #region Set Centroid Color and Label Color
            

            // First add label
            HandGestureValue = HandGestureFormat.CloseHand; // Which hand gesture;


            targetLabel = (byte)HandGestureValue;  // numerical value
            Console.WriteLine("targetLabel: {0}", targetLabel);

            backgroundLabel = 0;
            
            labelColor.Add(targetLabel, new byte[] { 255, 0, 0 }); // target is red
            labelColor.Add(backgroundLabel, new byte[] { 255, 255, 255 }); // background is white
            
            // Then add arbitrary labeled centroids.
             // For target color (blue)
            
            addCentroid(145, 170, 220, targetLabel);
            addCentroid(170, 190, 250, targetLabel);
            addCentroid(96, 152, 183, targetLabel);
            addCentroid(180, 211, 230, targetLabel);
            addCentroid(156, 196, 221, targetLabel);
            addCentroid(80, 112, 144, targetLabel);
            addCentroid(68, 99, 133, targetLabel);
            addCentroid(76, 103, 141, targetLabel);
            addCentroid(122, 154, 173, targetLabel);
            addCentroid(120, 138, 162, targetLabel);
            addCentroid(109, 118, 137, targetLabel);
            addCentroid(94, 124, 145, targetLabel);
            addCentroid(78, 127, 153, targetLabel);
            addCentroid(146, 177, 200, targetLabel);
            addCentroid(155, 195, 199, targetLabel);
            addCentroid(142, 182, 195, targetLabel);

            // For background color 
            
            addCentroid(80, 80, 80, backgroundLabel);
            addCentroid(250, 240, 240, backgroundLabel);
            addCentroid(210, 180, 150, backgroundLabel);
            addCentroid(110, 86, 244, backgroundLabel);
            addCentroid(75, 58, 151, backgroundLabel);
            addCentroid(153, 189, 206, backgroundLabel);
            addCentroid(214, 207, 206, backgroundLabel);
            addCentroid(122, 124, 130, backgroundLabel);                
            
            #endregion

            
        }

        public void kMeans() 
        // This function doesn't work due to merge. Michael will look into that.
        {
            /*
            int k = 5;
            // k = 5 seems to do well with background cleared out.
            
            Random rand = new Random();

            // Randomly create 7 colors
            double[,] clusters = new double[k, 3];
            for (int i = 0; i < k; i++) {
                clusters[i, 0] = rand.Next(0, 255);
                clusters[i, 1] = rand.Next(0, 255);
                clusters[i, 2] = rand.Next(0, 255);
            }

            // XXX: Assumes cropping!!!
            int width = x_1 - x_0;
            int height = y_1 - y_0;

            int[] points = new int[width*height];
            double[] point = new double[3];

            int [] cluster_count = Enumerable.Repeat((int)0, k).ToArray();
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
                }

                delta = cluster_deltas[0];
                for (int i = 1; i < k; i++)
                    if (cluster_deltas[i] > delta) delta = cluster_deltas[i];

            }


            colors = new byte[k, 3];
            for (int i = 0; i < k; i++)
            {
                colors[i, 0] = (byte)clusters[i, 0];
                colors[i, 1] = (byte)clusters[i, 1];
                colors[i, 2] = (byte)clusters[i, 2];
            }

            // Uncomment this line for stock color replacement.
            //replacement = colors;
            

            Processor.nearest_cache.Clear();
            Console.WriteLine("Done!");

             */ 
        } 

        private void addCentroid(byte R, byte G, byte B, byte label)  // a helper function for adding labled centroid
        {
            centroidColor.Add(new byte[] { R, G, B });
            centroidLabel.Add(label);
        }


        private void image_click(object sender, MouseButtonEventArgs e) {
            Point click_position = e.GetPosition(image);
            int baseIndex = ((int)click_position.Y * 640 + (int)click_position.X) * 4;
            Console.WriteLine("(x,y): (" + click_position.X + ", " + click_position.Y + ") RGB: {" + bitmapBits[baseIndex + 2] + ", " + bitmapBits[baseIndex + 1] + ", " + bitmapBits[baseIndex] + "}");
            
            
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
            using (StreamWriter filestream = new StreamWriter(directory + "\\"  + filename + "_rgb.txt"))
            {
                filestream.Write(rgb[0]);
                for (int i = 1; i < rgb.Length; i++) filestream.Write(" " + rgb[i]);
            }

            //mapped depth
            
            //sensor.MapDepthFrameToColorFrame(DepthImageFormat.Resolution640x480Fps30, depth, ColorImageFormat.RgbResolution640x480Fps30, mapped);
            //int[] mapped_depth = Enumerable.Repeat(-1, 640 * 480).ToArray() ;
            List<int[]> depthAndLabel = new List<int[]> (); // -1 means non-hand 
            using (StreamWriter filestream = new StreamWriter(directory + "\\" + "depthLabel_" + filename + ".txt"))
            {
                for (int i = 0; i < depth.Length; i++)
                {
                    int depthVal = depth[i] >> DepthImageFrame.PlayerIndexBitmaskWidth;
                    byte label = depthLabel[i]; 
                    /*
                    ColorImagePoint depth_point = mapped[i]; // it's better to be name color_point, since it's mapped to the color image
                    if (depth_point.X >= 0 && depth_point.X < 640 && depth_point.Y >= 0 && depth_point.Y < 480)
                    {
                        int idx = (depth_point.Y * 640 + depth_point.X);
                        //mapped_depth[idx] = depth[i] >> DepthImageFrame.PlayerIndexBitmaskWidth;
                        // update label.
                    }
                     */
                    depthAndLabel.Add(new int[] { depthVal, label });
                }
                /* Output file format: 
                (depthVal, label) (depthVal, label) (depthVal, label) (depthVal, label) ...
                 */
                filestream.Write("({0},{1})", depthAndLabel[0][0], depthAndLabel[0][1]);
                for (int i = 1; i < depthAndLabel.Count; i++) filestream.Write(" ({0},{1})", depthAndLabel[i][0], depthAndLabel[i][1]);
            }
            
            /*
            // processed
            using (StreamWriter filestream = new StreamWriter(directory + "\\" + filename + "_processed.txt"))
            {
                filestream.Write(bitmapBits[0]);
                for (int i = 1; i < bitmapBits.Length; i++) filestream.Write(" " + bitmapBits[i]);
            }
             */
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
                case Step.Depth: show_depth(depth, rgb);  break;
                case Step.Color: show_color(depth, rgb); break;
                case Step.Crop: crop_color(depth, rgb); break;
                case Step.PaintWhite: paint_white(depth, rgb); break;
                case Step.MappedDepth: show_mapped_depth(depth, rgb); break;
                case Step.ColorMatch: show_color_match(depth, rgb); break;
                case Step.ColorLabelingInRGB: ColorLabellingInRGB(rgb); break;
            }
        }

        #region Filter functions
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

        private void show_color_match(short[] depth, byte[] rgb)
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
                        rgb_tmp[1] = rgb[baseIndex + 1] ;
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
            for (int i = 0; i < bgr.Length; i+=4) {
                bitmapBits[i + 3] = 255;
                rgb_tmp[0] =  bgr[i + 2] ;
                rgb_tmp[1] = bgr[i + 1];
                rgb_tmp[2] = bgr[i];

                setLabel(rgb_tmp); // do the labeling
                
                bitmapBits[i] = rgb_tmp[2];
                bitmapBits[i + 1] = rgb_tmp[1];
                bitmapBits[i + 2] = rgb_tmp[0];
            }
        }

        #endregion

        public void update(short[] depth, byte[] rgb)
        {
            this.depth = depth;
            this.rgb = rgb;
            //Array.Clear(bitmapBits, 0, bitmapBits.Length); // Zero-all the bitmapBits                    
            foreach (Step step in pipeline) process(step, depth, rgb);

            bitmap.Dispatcher.Invoke(new Action(() =>
            {
                bitmap.WritePixels(new Int32Rect(0, 0, bitmap.PixelWidth, bitmap.PixelHeight),
                    bitmapBits, bitmap.PixelWidth * sizeof(int), 0);
            }));
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
            else {
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
        /*  
        double euc_distance(byte[] point, int colorIdx)
        {
            return Math.Sqrt(Math.Pow(point[0] - centroidColor[colorIdx, 0], 2) +
                Math.Pow(point[1] - centroidColor[colorIdx, 1], 2) +
                Math.Pow(point[2] - centroidColor[colorIdx, 2], 2));
        }
        */

        //double ColourDistance(byte[] point, int colorIdx)
        
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
