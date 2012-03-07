using System;
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
        }
    }


    public class Manager
    {
        private KinectSensor sensor;
        private byte[] colorPixels;
        private short[] depthPixels;
        private Processor [] processors;
        Thread poller;

        public Manager(MainWindow parent)
        {
            // Initialize Kinect
            KinectSensor.KinectSensors.StatusChanged += (object sender, StatusChangedEventArgs e) =>
            {
                if (e.Sensor == sensor && e.Status != KinectStatus.Connected) setSensor(null);
                else if ((sensor == null) && (e.Status == KinectStatus.Connected)) setSensor(e.Sensor);
            };

            foreach (var sensor in KinectSensor.KinectSensors)
                if (sensor.Status == KinectStatus.Connected) setSensor(sensor);
        
            // Create and arrange Images
            int total_processors = 2;
            processors = new Processor[total_processors];
            for (int i = 0; i < total_processors; i++)
            {
                processors[i] = new Processor(this.sensor);
                Image image = processors[i].getImage();
                parent.mainContainer.Children.Add(image);
            }

            #region Processor configurations

            processors[0].updatePipeline(Processor.Step.Crop,
                                         Processor.Step.PaintWhite,
                                         Processor.Step.MappedDepth);

            processors[1].updatePipeline(Processor.Step.Crop,
                                         Processor.Step.PaintWhite, 
                                         Processor.Step.ColorMatch);
            //processors[1].updatePipeline(Processor.Step.ColorMatch);
            //processors[2].updatePipeline(Processor.Step.ColorMatch);
            
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

                // Commented out for XBox Kinect
                //if (RangeModeValue == RangeModeFormat.Near)
                //    _sensor.DepthStream.Range = DepthRange.Near; // set near mode 

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

            if (poller.ThreadState == System.Threading.ThreadState.Suspended) poller.Resume();
            else poller.Suspend();
            //else poller.Resume();
        }

        public void poll()
        {
            while (true)
            {
                using (var frame = sensor.DepthStream.OpenNextFrame(1000))
                    if (frame != null) frame.CopyPixelDataTo(depthPixels);

                using (var frame = sensor.ColorStream.OpenNextFrame(1000))
                    if (frame != null) frame.CopyPixelDataTo(colorPixels);

                // Start all processing simultaneously in separate threads
                // XXX: For now, just send data to first processor
                foreach (Processor p in processors) p.update(depthPixels, colorPixels);
            }
        }
    }

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
                new Tuple<Vector, Vector>(new Vector(0,0), new Vector(0,-10)), 
                new Tuple<Vector, Vector>(new Vector(10, 10), new Vector(-10, 10))
            };
        }

        // Extract feature
        public double extract_feature(short[] depth, int idx, Point x) 
        {

            Debug.Assert(idx <= features.Length, "Trying to access nonexistent feature.");
            Tuple<Vector, Vector> feature = features[idx];
            int x_linear = (int)(x.Y * this.width + x.X);
            
            Point x_u = x + feature.Item1 / depth[x_linear];
            double x_u_depth;
            if (x_u.X < 0 || x_u.X >= width || x_u.Y < 0 || x_u.Y >= height)
                x_u_depth = outOfBounds;
            else
            {
                int lin = (int)(x_u.Y * width + x_u.X);
                x_u_depth = depth[lin];
            }
            

            Point x_v = x + feature.Item2 / depth[x_linear];
            double x_v_depth;
            if (x_v.X < 0 || x_v.X >= width || x_v.Y < 0 || x_v.Y >= height)
                x_v_depth = outOfBounds;
            else
            {
                int lin = (int)(x_v.Y * width + x_v.X);
                x_v_depth = depth[lin];
            }

            return x_u_depth - x_v_depth;

        }
    }

    public class Processor
    {
        private enum RangeModeFormat
        {
            Default = 0, // If you're using Kinect Xbox you should use Default
            Near = 1,
        };

        private RangeModeFormat RangeModeValue = RangeModeFormat.Near;
        private Dictionary<Tuple<byte, byte, byte>, byte[]> nearest_cache = new Dictionary<Tuple<byte, byte, byte>, byte[]>();
        private WriteableBitmap bitmap;
        private byte[] bitmapBits;
        private Image image;
        private int lower = 400, upper = 2000;
        public enum Step {PaintWhite, Color, Depth, Crop, MappedDepth, ColorMatch};

        private short[] depth;
        private byte[] rgb;

        private Step [] pipeline = new Step[0];
        private KinectSensor sensor;

        private Classifier classifier;

        byte[,] colors = new byte[,] {
            //{20,20,20},
            {150, 170, 230},
            {65, 45, 50},
            {170,130,130},
            {220, 210, 230},
            {0,0,255}
        };

        byte[,] replacement = new byte[,] {
            {255,0,0},
            {255,255,255},
            {255,255,255},
            {255,255,255},
            {255,255,255}
        };
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

            classifier = new Classifier();

            this.bitmap = new WriteableBitmap(640, 480, 96, 96, PixelFormats.Bgr32, null);
            this.bitmapBits = new byte[640 * 480 * 4];
            image.Source = bitmap;

            image.MouseLeftButtonUp += image_click;
        }

        private void image_click(object sender, MouseButtonEventArgs e) {
            Point click_position = e.GetPosition(image);
            int baseIndex = ((int)click_position.Y * 640 + (int)click_position.X) * 4;
            Console.WriteLine("(x,y): (" + click_position.X + ", " + click_position.Y + ") RGB: (" + bitmapBits[baseIndex + 2] + ", " + bitmapBits[baseIndex + 1] + ", " + bitmapBits[baseIndex] + ")");

            // Extract feature from this point:
            double feature = classifier.extract_feature(depth, 1, click_position);
            Console.WriteLine("Feature " + feature);
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
            int x_0 = 220, x_1 = 390, y_0 = 150, y_1 = 362;
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
                    //bitmapBits[4 * ((y - y_1) * (x_1 - x_0) + (x - x_0))] =
                    //bitmapBits[4 * ((y - y_1) * (x_1 - x_0) + (x - x_0)) + 1] =
                    //bitmapBits[4 * ((y - y_1) * (x_1 - x_0) + (x - x_0)) + 2] =
                    //bitmapBits[4 * ((y - y_1) * (x_1 - x_0) + (x - x_0)) + 3] = (byte)(255 * (max - depth[i]) / max);
            }
        }

        private void paint_white(short[] depth, byte[] rgb)
        {
            for (int i = 0; i < rgb.Length; i++) if (bitmapBits[i] != 0) bitmapBits[i] = 255;
        }

        private void show_mapped_depth(short[] depth, byte[] rgb)
        {
            ColorImagePoint[] mapped = new ColorImagePoint[depth.Length];
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
        {
            byte[] rgb_tmp = new byte[3];
            
            ColorImagePoint[] mapped = new ColorImagePoint[depth.Length];
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
                        rgb_tmp[0] = (byte)((int)rgb[baseIndex + 2] / 10 * 10);
                        rgb_tmp[1] = (byte)((int)rgb[baseIndex + 1] / 10 * 10);
                        rgb_tmp[2] = (byte)((int)rgb[baseIndex] / 10 * 10);

                        nearest_color(rgb_tmp);

                        bitmapBits[baseIndex] = rgb_tmp[2];
                        bitmapBits[baseIndex + 1] = rgb_tmp[1];
                        bitmapBits[baseIndex + 2] = rgb_tmp[0];
                    }
                }
            }
        }
        #endregion

        public void update(short[] depth, byte[] rgb)
        {
            this.depth = depth;
            this.rgb = rgb;
            foreach (Step step in pipeline) process(step, depth, rgb);

            bitmap.Dispatcher.Invoke(new Action(() =>
            {
                bitmap.WritePixels(new Int32Rect(0, 0, bitmap.PixelWidth, bitmap.PixelHeight),
                    bitmapBits, bitmap.PixelWidth * sizeof(int), 0);
            }));
        }

        #region Color matching
        void nearest_color(byte[] point)
        {
            // In place rewriting of the array
            //if (nearest_cache.ContainsKey(point))
            Tuple<byte, byte, byte> t = new Tuple<byte, byte, byte>(point[0], point[1], point[2]);
            if (nearest_cache.ContainsKey(t))
            {
                //Console.WriteLine("Actually matching.");
                point[0] = nearest_cache[t][0];
                point[1] = nearest_cache[t][1];
                point[2] = nearest_cache[t][2];
                return;
            }

            //int minIdx = 0;
            double minDistance = 1000000;
            int minColor = -1;

            for (int idx = 0; idx < colors.GetLength(0); idx++)
            {
                double distance = euc_distance(point, idx);
                if (distance < minDistance)
                {
                    minColor = idx;
                    minDistance = distance;
                }
            }


            nearest_cache.Add(new Tuple<byte, byte, byte>(point[0], point[1], point[2]),
                new byte[] {replacement[minColor, 0],
                            replacement[minColor, 1], 
                            replacement[minColor, 2]});

            //Console.WriteLine(nearest_cache.Count());

            point[0] = replacement[minColor, 0];
            point[1] = replacement[minColor, 1];
            point[2] = replacement[minColor, 2];
        }

        double euc_distance(byte[] point, int colorIdx)
        {
            return Math.Sqrt(Math.Pow(point[0] - colors[colorIdx, 0], 2) +
                Math.Pow(point[1] - colors[colorIdx, 1], 2) +
                Math.Pow(point[2] - colors[colorIdx, 2], 2));
        }
        #endregion

    }
}
