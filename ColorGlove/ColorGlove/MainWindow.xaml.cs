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
        private WriteableBitmap[] _bitmaps = new WriteableBitmap[2];
        private byte[][] _bitmapBits = new byte[2][];
        private ColorImagePoint[] _mappedDepthLocations;
        private byte[] _colorPixels = new byte[0];
        private short[] _depthPixels = new short[0];
        private Dictionary<Tuple<byte, byte, byte>, byte[]> nearest_cache = new Dictionary<Tuple<byte, byte, byte>, byte[]>();
        private int upper = 900, lower = 100; // used for thresholding interested object.
        private int threshold = 10000; //?
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
        
        // For the color mapping
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

                keep_polling();
            }

        }

        public void keep_polling() {
             using (var frame = _sensor.ColorStream.OpenNextFrame(200))
                {
                    if (frame != null)
                    {
                        Console.WriteLine("At least we made it this far.");
                        _colorPixels = new byte[frame.PixelDataLength];
                        _bitmaps[0] = new WriteableBitmap(640, 480, 96, 96, PixelFormats.Bgr32, null);
                        _bitmapBits[0] = new byte[640 * 480 * 4];
                        frame.CopyPixelDataTo(_bitmapBits[0]);
                        _bitmaps[0].WritePixels(new Int32Rect(0, 0, _bitmaps[0].PixelWidth, _bitmaps[0].PixelHeight), _bitmapBits[0], _bitmaps[0].PixelWidth * sizeof(int), 0);
                        image1.Source = _bitmaps[0];
                    }
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
                        _bitmaps[0] = new WriteableBitmap(640, 480, 96.0, 96.0, PixelFormats.Bgr32, null);
                        _bitmaps[1] = new WriteableBitmap(640, 480, 96.0, 96.0, PixelFormats.Bgr32, null);
                        _bitmapBits[0] = new byte[640 * 480 * 4];
                        _bitmapBits[1] = new byte[640 * 480 * 4];
                        this.image1.Source = _bitmaps[0]; // Assign the WPF element to _bitmap
                        this.image2.Source = _bitmaps[1]; // Assign the WPF element to _bitmap
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

            process_data();

            _bitmaps[0].WritePixels(new Int32Rect(0, 0, _bitmaps[0].PixelWidth, _bitmaps[0].PixelHeight), _bitmapBits[0], _bitmaps[0].PixelWidth * sizeof(int), 0);
            _bitmaps[1].WritePixels(new Int32Rect(0, 0, _bitmaps[1].PixelWidth, _bitmaps[1].PixelHeight), _bitmapBits[1], _bitmaps[1].PixelWidth * sizeof(int), 0);

        }
        #endregion

        // Entry point into custom data processing function
        void process_data()
        {
            show_color(0);
            //display_only_depth(0);
            //display_only_mapped(1);
            //rgb_on_mapped(0);
            //color_mapped(0);
            
            // Pipeline model
            //show_color(1);
            //display_all_depth(1);
            //masked_depth(1);
            paint_white(_bitmapBits[1]);
            show_near_mapped(_bitmapBits[1]);
        }

        void show_color(int display)
        {
            _bitmapBits[display] = _colorPixels;
        }

        void display_only_depth(int display)
        {
            // with thresholding, uninteresting region will be white.
            for (int i = 0; i < _depthPixels.Length; i++)
            {
                //Console.WriteLine(_depthPixels[i]);
                
                //Debug.WriteLine(depthVal);
                if (_depthPixels[i] < threshold) _bitmapBits[display][4 * i] = 
                                                _bitmapBits[display][4 * i + 1] =
                                                _bitmapBits[display][4 * i + 2] =
                                                _bitmapBits[display][4 * i + 3] = (byte)(255 * (threshold - _depthPixels[i]) / threshold);
                else _bitmapBits[display][4 * i] =
                    _bitmapBits[display][4 * i + 1] =
                    _bitmapBits[display][4 * i + 2] =
                    _bitmapBits[display][4 * i + 3] = (byte)255;
            }

        }

        void display_all_depth(int display)
        {
            // with thresholding, uninteresting region will be white.
            for (int i = 0; i < _depthPixels.Length; i++)
            {
                //Console.WriteLine(_depthPixels[i]);
                int max = 32767;

                //Debug.WriteLine(depthVal);
                _bitmapBits[display][4 * i] =
                _bitmapBits[display][4 * i + 1] =
                _bitmapBits[display][4 * i + 2] =
                _bitmapBits[display][4 * i + 3] = (byte)(255 * (max - _depthPixels[i]) / max);    
            }

        }

        void masked_depth(int display)
        {
            // with thresholding, uninteresting region will be white.
            for (int i = 0; i < _depthPixels.Length; i++)
            {
                //Console.WriteLine(_depthPixels[i]);
                int max = 32767;
                int x_0 = 220, x_1 = 410, y_0 = 93, y_1 = 362;

                int y = i/640;
                int x = i % 640;

                if (x > x_0 && x < x_1 && y > y_0 && y < y_1)
                    _bitmapBits[display][4 * i] =
                    _bitmapBits[display][4 * i + 1] =
                    _bitmapBits[display][4 * i + 2] =
                    _bitmapBits[display][4 * i + 3] = (byte)(255 * (max - _depthPixels[i]) / max);
                else
                    _bitmapBits[display][4 * i] =
                    _bitmapBits[display][4 * i + 1] =
                    _bitmapBits[display][4 * i + 2] =
                    _bitmapBits[display][4 * i + 3] = (byte)255;
            }
        }
        
        void display_only_mapped(int display)
        {
            if (RGBModeValue == RGBModeFormat.YuvResolution640x480Fps15)
                this._sensor.MapDepthFrameToColorFrame(DepthImageFormat.Resolution640x480Fps30, _depthPixels, ColorImageFormat.YuvResolution640x480Fps15, _mappedDepthLocations);
            else if (RGBModeValue == RGBModeFormat.RgbResolution640x480Fps30)
                this._sensor.MapDepthFrameToColorFrame(DepthImageFormat.Resolution640x480Fps30, _depthPixels, ColorImageFormat.RgbResolution640x480Fps30, _mappedDepthLocations);
           

            for (int i = 0; i < _depthPixels.Length; i++)
            {
                int depthVal = _depthPixels[i] >> DepthImageFrame.PlayerIndexBitmaskWidth;

                ColorImagePoint point = _mappedDepthLocations[i];
                if ((point.X >= 0 && point.X < 640) && (point.Y >= 0 && point.Y < 480))
                {
                    int baseIndex = (point.Y * 640 + point.X) * 4;
                    if ((depthVal <= upper) && (depthVal > lower)) 
                        _bitmapBits[display][baseIndex] =
                        _bitmapBits[display][baseIndex + 1] =
                        _bitmapBits[display][baseIndex + 2] = (byte)(255 * (upper - depthVal) / upper);
                    else _bitmapBits[display][baseIndex] = _bitmapBits[display][baseIndex + 1] = _bitmapBits[display][baseIndex + 2] = (byte)255;
                }
            }
        }

        void rgb_on_mapped(int display)
        {
            if (RGBModeValue == RGBModeFormat.YuvResolution640x480Fps15)
                this._sensor.MapDepthFrameToColorFrame(DepthImageFormat.Resolution640x480Fps30, _depthPixels, ColorImageFormat.YuvResolution640x480Fps15, _mappedDepthLocations);
            else if (RGBModeValue == RGBModeFormat.RgbResolution640x480Fps30)
                this._sensor.MapDepthFrameToColorFrame(DepthImageFormat.Resolution640x480Fps30, _depthPixels, ColorImageFormat.RgbResolution640x480Fps30, _mappedDepthLocations);


            for (int i = 0; i < _depthPixels.Length; i++)
            {
                int depthVal = _depthPixels[i] >> DepthImageFrame.PlayerIndexBitmaskWidth;

                ColorImagePoint point = _mappedDepthLocations[i];
                if ((point.X >= 0 && point.X < 640) && (point.Y >= 0 && point.Y < 480))
                {
                    int baseIndex = (point.Y * 640 + point.X) * 4;
                    if ((depthVal <= upper) && (depthVal > lower))
                    {

                        _bitmapBits[display][baseIndex] = _colorPixels[baseIndex];
                        _bitmapBits[display][baseIndex + 1] = _colorPixels[baseIndex + 1];
                        _bitmapBits[display][baseIndex + 2] = _colorPixels[baseIndex + 2];

                    }
                    else _bitmapBits[display][baseIndex] = _bitmapBits[display][baseIndex + 1] = _bitmapBits[display][baseIndex + 2] = (byte)255;
                }
            }

        }

        void color_mapped(int display)
        {
            Debug.Assert(_bitmapBits[display].Length == _colorPixels.Length);
            for (int i = 0; i < _colorPixels.Length; i += 4) // need to do the copy before MapDepthToColor
            {
                _bitmapBits[display][i + 3] = 255;
                _bitmapBits[display][i + 2] = _colorPixels[i + 2];
                _bitmapBits[display][i + 1] = _colorPixels[i + 1];
                _bitmapBits[display][i] = _colorPixels[i];                    
            }
            
            // XXX: I think we should write the mapped depth image to a local variable. - Arun
            if (RGBModeValue == RGBModeFormat.YuvResolution640x480Fps15)
                this._sensor.MapDepthFrameToColorFrame(DepthImageFormat.Resolution640x480Fps30, _depthPixels, ColorImageFormat.YuvResolution640x480Fps15, _mappedDepthLocations);
            else if (RGBModeValue == RGBModeFormat.RgbResolution640x480Fps30)
                this._sensor.MapDepthFrameToColorFrame(DepthImageFormat.Resolution640x480Fps30, _depthPixels, ColorImageFormat.RgbResolution640x480Fps30, _mappedDepthLocations);
            Debug.Assert(RGBModeValue == RGBModeFormat.RgbResolution640x480Fps30);
            Array.Clear(_usedcolorPixels, 0, _usedcolorPixels.Length);
            
            for (int i = 0; i < _depthPixels.Length; i++)
            {
                int depthVal = _depthPixels[i] >> DepthImageFrame.PlayerIndexBitmaskWidth;                       
                if ((depthVal <= upper) && (depthVal > lower))
                {
                    ColorImagePoint point = _mappedDepthLocations[i];
                    if ((point.X >= 0 && point.X < 640) && (point.Y >= 0 && point.Y < 480))
                    {
                        int baseIndex = (point.Y * 640 + point.X) * 4;
                        _usedcolorPixels[baseIndex] = 1;                        
                    }
                }
            }
            
            for (int i = 0; i < _bitmapBits[display].Length; i += 4)
                if (_usedcolorPixels[i] == 0)
                {
                    _bitmapBits[display][i] = (byte)(255);
                    _bitmapBits[display][i + 1] = (byte)(255);
                    _bitmapBits[display][i + 2] = (byte)(255);                    
                }
        }

        void paint_white(byte[] imageBits)
        {
            for (int i = 0; i < imageBits.Length; i++) imageBits[i] = (byte)255;
        }
        
        void show_near_mapped(byte[] imageBits)
        {
            ColorImagePoint[] _mapped = new ColorImagePoint[_depthPixels.Length];
            if (RGBModeValue == RGBModeFormat.YuvResolution640x480Fps15)
                this._sensor.MapDepthFrameToColorFrame(DepthImageFormat.Resolution640x480Fps30, _depthPixels, ColorImageFormat.YuvResolution640x480Fps15, _mapped);
            else if (RGBModeValue == RGBModeFormat.RgbResolution640x480Fps30)
                this._sensor.MapDepthFrameToColorFrame(DepthImageFormat.Resolution640x480Fps30, _depthPixels, ColorImageFormat.RgbResolution640x480Fps30, _mapped);

            byte[] rgb = new byte[3];

            for (int i = 0; i < _depthPixels.Length; i++)
            {
                int depthVal = _depthPixels[i] >> DepthImageFrame.PlayerIndexBitmaskWidth;
                if ((depthVal <= upper) && (depthVal > lower))
                {
                    ColorImagePoint point = _mapped[i];
                    if ((point.X >= 0 && point.X < 640) && (point.Y >= 0 && point.Y < 480))
                    {
                        int baseIndex = (point.Y * 640 + point.X) * 4;
                        rgb[0] = (byte)((int)_colorPixels[baseIndex + 2]/10*10);
                        rgb[1] = (byte)((int)_colorPixels[baseIndex + 1]/10*10);
                        rgb[2] = (byte)((int)_colorPixels[baseIndex]/10*10);

                        nearest_color(rgb);
                        
                        imageBits[baseIndex] = rgb[2];
                        imageBits[baseIndex + 1] = rgb[1];
                        imageBits[baseIndex + 2] = rgb[0];
                    }
                }
            }
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

            
            nearest_cache.Add(new Tuple<byte,byte,byte>(point[0], point[1], point[2]), 
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

        private void image1_click(object sender, MouseButtonEventArgs e)
        {
            Point click_position = e.GetPosition(image1);
            int baseIndex = ((int)click_position.Y * 640 + (int)click_position.X) * 4;
            Console.WriteLine("(x,y): (" + click_position.X + ", " + click_position.Y + ") RGB: (" + _colorPixels[baseIndex + 2] + ", " + _colorPixels[baseIndex + 1] + ", " + _colorPixels[baseIndex] + ")");
        }

        private void image2_click(object sender, MouseButtonEventArgs e)
        {
            Point click_position = e.GetPosition(image1);
            int baseIndex = ((int)click_position.Y * 640 + (int)click_position.X) * 4;
            Console.WriteLine("RGB: (" + _colorPixels[baseIndex + 2] + ", " + _colorPixels[baseIndex + 1] + ", " + _colorPixels[baseIndex] + ")");
        }

    }
}
