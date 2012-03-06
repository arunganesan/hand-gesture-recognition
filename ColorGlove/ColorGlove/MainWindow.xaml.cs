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
 * Built off extremely useful code from: http://social.msdn.microsoft.com/Forums/en-US/kinectsdknuiapi/thread/c39bab30-a704-4de1-948d-307afd128dab
 */

namespace ColorGlove
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {

        private enum RangeModeFormat
        {
            Defalut = 0, // If you're using Kinect Xbox you should use Default
            Near = 1,
        };
        
        private RangeModeFormat RangeModeValue = RangeModeFormat.Near;
        
        public MainWindow()
        {
            InitializeComponent();
            Manager m = new Manager(this);
            m.start();
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
            processors = new Processor[2];
            for (int i = 0; i < 2; i++)
            {
                processors[i] = new Processor(this.sensor);
                Image image = processors[i].getImage();
                parent.mainContainer.Children.Add(image);
            }

            #region Processor configurations
            processors[0].updatePipeline(Processor.Step.Color);
            processors[1].updatePipeline(Processor.Step.PaintWhite, Processor.Step.MappedDepth);
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

    public class Processor
    {
        private Dictionary<Tuple<byte, byte, byte>, byte[]> nearest_cache = new Dictionary<Tuple<byte, byte, byte>, byte[]>();
        private WriteableBitmap bitmap;
        private byte[] bitmapBits;
        private Image image;

        public enum Step {PaintWhite, Color, Depth, Crop, MappedDepth, ColorMatch};
        
        private Step [] pipeline;
        private KinectSensor sensor;

        
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

        public Processor(KinectSensor sensor)
        {
            this.sensor = sensor; 
            image = new Image();
            image.Width = 640;
            image.Height = 480;

            this.bitmap = new WriteableBitmap(640, 480, 96, 96, PixelFormats.Bgr32, null);
            this.bitmapBits = new byte[640 * 480 * 4];
            image.Source = bitmap;
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
                case Step.Crop: crop_image(depth, rgb); break;
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

        private void crop_image(short[] depth, byte[] rgb)
        {
        }

        private void paint_white(short[] depth, byte[] rgb)
        {
            for (int i = 0; i < rgb.Length; i++) bitmapBits[i] = 255;
        }

        private void show_mapped_depth(short[] depth, byte[] rgb)
        {
            ColorImagePoint[] mapped = new ColorImagePoint[depth.Length];
            sensor.MapDepthFrameToColorFrame(DepthImageFormat.Resolution640x480Fps30, depth, ColorImageFormat.RgbResolution640x480Fps30, mapped);
            for (int i = 0; i < depth.Length; i++)
            {
                int depthVal = depth[i] >> DepthImageFrame.PlayerIndexBitmaskWidth;
                if ((depthVal <= 1500) && (depthVal > 400))
                {
                    ColorImagePoint point = mapped[i];
                    if ((point.X >= 0 && point.X < 640) && (point.Y >= 0 && point.Y < 480))
                    {
                        int baseIndex = (point.Y * 640 + point.X) * 4;
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
            for (int i = 0; i < bitmapBits.Length; i++) bitmapBits[i] = 255;

            ColorImagePoint[] mapped = new ColorImagePoint[depth.Length];
            sensor.MapDepthFrameToColorFrame(DepthImageFormat.Resolution640x480Fps30, depth, ColorImageFormat.RgbResolution640x480Fps30, mapped);
            for (int i = 0; i < depth.Length; i++)
            {
                int depthVal = depth[i] >> DepthImageFrame.PlayerIndexBitmaskWidth;
                if ((depthVal <= 1500) && (depthVal > 400))
                {
                    ColorImagePoint point = mapped[i];
                    if ((point.X >= 0 && point.X < 640) && (point.Y >= 0 && point.Y < 480))
                    {
                        int baseIndex = (point.Y * 640 + point.X) * 4;
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
