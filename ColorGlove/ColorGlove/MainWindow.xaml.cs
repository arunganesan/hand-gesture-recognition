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
                processors[i] = new Processor();
                Image image = processors[i].getImage();
                parent.mainContainer.Children.Add(image);
            }

            #region Processor configurations
            processors[0].updateFunction("depth");
            processors[1].updateFunction("rgb");
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

                // Don't continue until all threads complete

                //System.Threading.Thread.Sleep(1000);
            }
        }
    }

    public class Processor
    {
        private WriteableBitmap bitmap;
        private byte[] bitmapBits;
        private Image image;
        private string func = "depth";

        public Processor()
        {
            image = new Image();
            image.Width = 640;
            image.Height = 480;

            this.bitmap = new WriteableBitmap(640, 480, 96, 96, PixelFormats.Bgr32, null);
            this.bitmapBits = new byte[640 * 480 * 4];
            image.Source = bitmap;
        }

        public Image getImage() { return image; }

        public void updateFunction(string func)
        {
            this.func = func;
        }

        public void update(short[] depth, byte[] rgb)
        {
            if (func == "depth")
            {
                for (int i = 0; i < depth.Length; i++)
                {
                    bitmapBits[4 * i] = bitmapBits[4 * i + 1] = bitmapBits[4 * i + 2] = (byte)(255 * (short.MaxValue - depth[i]) / short.MaxValue);
                }
            } else if (func == "rgb") {
                bitmapBits = rgb;
            }

            bitmap.Dispatcher.Invoke(new Action(() =>
            {
                bitmap.WritePixels(new Int32Rect(0, 0, bitmap.PixelWidth, bitmap.PixelHeight),
                    bitmapBits, bitmap.PixelWidth * sizeof(int), 0);
            }));
        }
    }
}
