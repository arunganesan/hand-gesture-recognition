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

        #region Variables
        private KinectSensor _sensor;
        private WriteableBitmap _bitmaps;
        private byte[] _bitmapBits;
        private ColorImagePoint[] _mappedDepthLocations;
        private byte[] _colorPixels = new byte[0];
        private short[] _depthPixels = new short[0];
        private Dictionary<Tuple<byte, byte, byte>, byte[]> nearest_cache = new Dictionary<Tuple<byte, byte, byte>, byte[]>();
        
        private short[] _usedcolorPixels = new short[640 * 480 * 4];
        private enum RGBModeFormat {
            RgbResolution640x480Fps30 = 0,
            YuvResolution640x480Fps15= 1,
        };
        private enum RangeModeFormat
        {
            Defalut = 0, // If you're using Kinect Xbox you should use Default
            Near = 1,
        };

        //private RGBModeFormat RGBModeValue = RGBModeFormat.YuvResolution640x480Fps15;
        private RGBModeFormat RGBModeValue = RGBModeFormat.RgbResolution640x480Fps30;
        private RangeModeFormat RangeModeValue = RangeModeFormat.Near;
        
        
        Thread poller;
        #endregion

        #region Kinect setup functions

        private void SetSensor(KinectSensor newSensor)
        {
            if (_sensor != null) _sensor.Stop();
            _sensor = newSensor;
            if (_sensor != null)
            {
                Debug.Assert(_sensor.Status == KinectStatus.Connected, "This should only be called with Connected sensors.");
                if (RGBModeValue == RGBModeFormat.YuvResolution640x480Fps15)
                    _sensor.ColorStream.Enable(ColorImageFormat.YuvResolution640x480Fps15);
                else if (RGBModeValue == RGBModeFormat.RgbResolution640x480Fps30)
                    _sensor.ColorStream.Enable(ColorImageFormat.RgbResolution640x480Fps30);
                
                _sensor.DepthStream.Enable(DepthImageFormat.Resolution640x480Fps30);

                // Commented out for XBox Kinect
                //if (RangeModeValue == RangeModeFormat.Near)
                //    _sensor.DepthStream.Range = DepthRange.Near; // set near mode 


                // Poll the next frame just to see if it works
                //_sensor.AllFramesReady += _sensor_AllFramesReady; // Register event
                _sensor.Start();


                this._bitmaps = new WriteableBitmap(640, 480, 96, 96, PixelFormats.Bgr32, null);
                this._bitmapBits = new byte[640 * 480 * 4];
                this._depthPixels = new short[640 * 480];
                image1.Source = _bitmaps;
                poller = new Thread(new ThreadStart(this.keep_polling));
                poller.Start();
            }

        }

        public void keep_polling() {

            while (true)
            {
                using (var frame = _sensor.DepthStream.OpenNextFrame(1000))
                {
                    if (frame != null)
                    {
                        frame.CopyPixelDataTo(_depthPixels);
                        for (int i = 0; i < _depthPixels.Length; i++) {
                            _bitmapBits[4 * i] = _bitmapBits[4 * i + 1] = _bitmapBits[4 * i + 2] = (byte)(255*(short.MaxValue - _depthPixels[i]) / short.MaxValue);
                        }

                        _bitmaps.Dispatcher.Invoke(new Action(() => 
                        {
                            _bitmaps.WritePixels(new Int32Rect(0, 0, _bitmaps.PixelWidth, _bitmaps.PixelHeight),
                                _bitmapBits, _bitmaps.PixelWidth * sizeof(int), 0);
                        }));
                    }
                }

                //System.Threading.Thread.Sleep(1000);
            }
        }

        public MainWindow()
        {
            InitializeComponent();

            KinectSensor.KinectSensors.StatusChanged += (object sender, StatusChangedEventArgs e) =>
            {
                if (e.Sensor == _sensor && e.Status != KinectStatus.Connected) SetSensor(null);
                else if ((_sensor == null) && (e.Status == KinectStatus.Connected)) SetSensor(e.Sensor);
            };


            foreach (var sensor in KinectSensor.KinectSensors)
                if (sensor.Status == KinectStatus.Connected) SetSensor(sensor);
        }


        void _sensor_AllFramesReady(object sender, AllFramesReadyEventArgs e)
        {
            using (ColorImageFrame colorFrame = e.OpenColorImageFrame())
            {
                if (colorFrame != null)
                {
                    Debug.Assert(colorFrame.Width == 640 && colorFrame.Height == 480, "This app only uses 640x480.");

                    if (_colorPixels.Length != colorFrame.PixelDataLength)
                    {
                        _colorPixels = new byte[colorFrame.PixelDataLength];
                        _bitmaps = new WriteableBitmap(640, 480, 96.0, 96.0, PixelFormats.Bgr32, null);
                        _bitmapBits = new byte[640 * 480 * 4];
                        this.image1.Source = _bitmaps; // Assign the WPF element to _bitmap
                    }

                    colorFrame.CopyPixelDataTo(_colorPixels);
                }
            }

            using (DepthImageFrame depthFrame = e.OpenDepthImageFrame())
            {
                if (depthFrame != null)
                {
                    Debug.Assert(depthFrame.Width == 640 && depthFrame.Height == 480, "This app only uses 640x480.");

                    if (_depthPixels.Length != depthFrame.PixelDataLength)
                    {
                        _depthPixels = new short[depthFrame.PixelDataLength];
                        _mappedDepthLocations = new ColorImagePoint[depthFrame.PixelDataLength];
                    }

                    depthFrame.CopyPixelDataTo(_depthPixels);
                }
            }


            _bitmaps.WritePixels(new Int32Rect(0, 0, _bitmaps.PixelWidth, _bitmaps.PixelHeight), _bitmapBits, _bitmaps.PixelWidth * sizeof(int), 0);

        }
        #endregion

    }


    public class Manager
    {
        public Manager()
        {

        }

        public void start()
        {

        }
    }
}
