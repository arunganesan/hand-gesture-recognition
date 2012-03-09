using System;
using System.Diagnostics;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using Microsoft.Kinect;

namespace AlignRGBDepth
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private KinectSensor _sensor;
        private WriteableBitmap _bitmap;
        private byte[] _bitmapBits;
        private ColorImagePoint[] _mappedDepthLocations;
        private byte[] _colorPixels = new byte[0];
        private short[] _depthPixels = new short[0];
        const float MaxDepthDistance = 4095; // max value returned
        const float MinDepthDistance =400; // 100; // min value returned
        const float MaxDepthDistanceOffset = MaxDepthDistance - MinDepthDistance;
        public enum BackgroundFormat
        {
            Undefined = 0,
            PureDepth = 1,
            PureRGB = 2,
            AllGrey = 3,
        };
        public enum ThresholdFormat
        {
            Undefined = 0,
            MixedWhite = 1,
            AllBlack = 2,
            PureRGB =3,
        };
        public enum UnthresholdFormat
        {
            Undefined = 0,
            AllWhite = 1,
            AllWhiteExceptMappedInThreshold = 2, // Will turn those out of range RGB pixels that haven't appeared in the mapped pixels in the thresholding (Needs change of wording)
        };
        public enum RangeModeFormat
        {
            Defalut = 0, // If you're using Kinect Xbox you should use Default
            Near = 1, 
        };
        public enum FastModeFormat { 
            Default = 0,
            AllocateArrayInAdvace = 1, 
        };

        // Some settings: 
        private BackgroundFormat BackgroundValue = BackgroundFormat.PureRGB; //PureDepth; //.PureRGB;
        //private BackgroundFormat BackgroundValue = BackgroundFormat.PureDepth;
        private ThresholdFormat ThresholdValue = ThresholdFormat.PureRGB; //.PureRGB; // MixedWhite
        private UnthresholdFormat UnthressholdValue = UnthresholdFormat.AllWhiteExceptMappedInThreshold; //AllWhite;
        //private UnthresholdFormat UnthressholdValue = UnthresholdFormat.Undefined;
        private RangeModeFormat RangeModeValue = RangeModeFormat.Near; //Default;
        //private FastModeFormat FastModeValue = FastModeFormat.AllocateArrayInAdvace;
        private int rangeMin = 400, rangeMax = 4000;
        private short[] _usedcolorPixels = new short[640*480*4];
        private const int ColorPixelDataLength = 1228800;
        private const int DepthPixelDataLength = 307200;
        

        private void SetSensor(KinectSensor newSensor)
        {
            if (_sensor != null)
            {
                _sensor.Stop();
            }

            _sensor = newSensor;

            if (_sensor != null)
            {
                Debug.WriteLine("Hello world");                 
                Debug.Assert(_sensor.Status == KinectStatus.Connected, "This should only be called with Connected sensors.");
                _sensor.ColorStream.Enable(ColorImageFormat.RgbResolution640x480Fps30);
                _sensor.DepthStream.Enable(DepthImageFormat.Resolution640x480Fps30);
                if (RangeModeValue == RangeModeFormat.Near)
                    _sensor.DepthStream.Range = DepthRange.Near;
                //newSensor.DepthStream.Range = DepthRange.Near; // Set the near mode 
                _sensor.AllFramesReady += _sensor_AllFramesReady; // Register event
                _sensor.Start();
            }
        }

        void _sensor_AllFramesReady(object sender, AllFramesReadyEventArgs e)
        {
            bool gotColor = false;
            bool gotDepth = false;

            using (ColorImageFrame colorFrame = e.OpenColorImageFrame())
            {
                if (colorFrame != null)
                {
                    Debug.Assert(colorFrame.Width == 640 && colorFrame.Height == 480, "This app only uses 640x480.");

                    if (_colorPixels.Length != colorFrame.PixelDataLength)
                    {
                        Debug.WriteLine("Color pixelDataLength, {0}", colorFrame.PixelDataLength);
                        _colorPixels = new byte[colorFrame.PixelDataLength];                        
                        _bitmapBits = new byte[640 * 480 * 4];
                        _bitmap = new WriteableBitmap(640, 480, 96.0, 96.0, PixelFormats.Bgr32, null);
                        this.Image.Source = _bitmap; // Assign the WPF element to _bitmap
                    }

                    colorFrame.CopyPixelDataTo(_colorPixels);
                    gotColor = true;
                }
            }

            using (DepthImageFrame depthFrame = e.OpenDepthImageFrame())
            {
                if (depthFrame != null)
                {
                    Debug.Assert(depthFrame.Width == 640 && depthFrame.Height == 480, "This app only uses 640x480.");

                    if (_depthPixels.Length != depthFrame.PixelDataLength)
                    {
                        Debug.WriteLine("Depth pixelDataLength, {0}", depthFrame.PixelDataLength);                        
                        _depthPixels = new short[depthFrame.PixelDataLength];
                        _mappedDepthLocations = new ColorImagePoint[depthFrame.PixelDataLength];

                    }

                    depthFrame.CopyPixelDataTo(_depthPixels);
                    gotDepth = true;
                }
            }

            for (int i = 0; i < _colorPixels.Length; i += 4)
            {
                _bitmapBits[i + 3] = 255;

                if (BackgroundValue == BackgroundFormat.PureDepth)
                {
                    byte intensity = CalculateIntensityFromDepth(_depthPixels[i / 4]);
                    _bitmapBits[i + 2] = intensity;
                    _bitmapBits[i + 1] = intensity;
                    _bitmapBits[i] = intensity;

                }
                else if (BackgroundValue == BackgroundFormat.PureRGB)
                {
                    _bitmapBits[i + 2] = _colorPixels[i + 2];
                    _bitmapBits[i + 1] = _colorPixels[i + 1];
                    _bitmapBits[i] = _colorPixels[i];
                }
                else if (BackgroundValue == BackgroundFormat.AllGrey)
                {
                    _bitmapBits[i + 2] = 125;
                    _bitmapBits[i + 1] = 125;
                    _bitmapBits[i] = 125;
                }
            }
            
            if (ThresholdValue != ThresholdFormat.Undefined)
            {
                this._sensor.MapDepthFrameToColorFrame(DepthImageFormat.Resolution640x480Fps30, _depthPixels, ColorImageFormat.RgbResolution640x480Fps30, _mappedDepthLocations);
                //Debug.WriteLine(_depthPixels.Length);
                
                if (UnthressholdValue == UnthresholdFormat.AllWhiteExceptMappedInThreshold)
                {
                    Array.Clear(_usedcolorPixels, 0, _usedcolorPixels.Length);                    
                }

                for (int i = 0; i < _depthPixels.Length; i++)
                {
                    int depthVal = _depthPixels[i] >> DepthImageFrame.PlayerIndexBitmaskWidth;                                     
                    if ((depthVal < rangeMax) && (depthVal > rangeMin))
                    {
                        ColorImagePoint point = _mappedDepthLocations[i];   
                        if ((point.X >= 0 && point.X < 640) && (point.Y >= 0 && point.Y < 480))
                        {                            
                            int baseIndex = (point.Y * 640 + point.X) * 4;
                            if (ThresholdValue == ThresholdFormat.AllBlack)
                            {
                                _bitmapBits[baseIndex] = (byte)(0);
                                _bitmapBits[baseIndex + 1] = (byte)(0);
                                _bitmapBits[baseIndex + 2] = (byte)(0);
                            }
                            else if (ThresholdValue == ThresholdFormat.MixedWhite)
                            {

                                _bitmapBits[baseIndex] = (byte)((_bitmapBits[baseIndex] + 255) >> 1);
                                _bitmapBits[baseIndex + 1] = (byte)((_bitmapBits[baseIndex + 1] + 255) >> 1);
                                _bitmapBits[baseIndex + 2] = (byte)((_bitmapBits[baseIndex] + 255) >> 1);
                            }
                            // else (ThresholdValue == ThresholdFormat.PureRGB) {Don't do anything}

                            if (UnthressholdValue == UnthresholdFormat.AllWhiteExceptMappedInThreshold)
                            {
                                _usedcolorPixels[baseIndex] = 1;                                
                            }
                        }
                        else
                        {
                            Debug.WriteLine("Out of range");
                        }

                    }                                 
                    else
                    {
                        /*
                        if (UnthressholdValue == UnthresholdFormat.AllWhite)
                        {
                            //if ((point.X >= 0 && point.X < 640) && (point.Y >= 0 && point.Y < 480))
                            {
                                int baseIndex = (point.Y * 640 + point.X) * 4;
                                _bitmapBits[baseIndex] = (byte)(255);
                                _bitmapBits[baseIndex + 1] = (byte)(255);
                                _bitmapBits[baseIndex + 2] = (byte)(255);
                                
                            }
                        }
                         */ 
                    }

                }
                // end the loop for the depth pixel
                if (UnthressholdValue == UnthresholdFormat.AllWhiteExceptMappedInThreshold)
                {
                    for (int i=0; i<_bitmapBits.Length; i+=4)
                        if (_usedcolorPixels[i]==0) {                            
                            _bitmapBits[i] = (byte)(255);
                            _bitmapBits[i + 1] = (byte)(255);
                            _bitmapBits[i + 2] = (byte)(255);
                              
                        }
                }
            }
            _bitmap.WritePixels(new Int32Rect(0, 0, _bitmap.PixelWidth, _bitmap.PixelHeight), _bitmapBits, _bitmap.PixelWidth * sizeof(int), 0);
            
        }

        public static byte CalculateIntensityFromDepth(int distance)
        {
            //formula for calculating monochrome intensity for histogram
            return (byte)(255 - (255 * Math.Max(distance - MinDepthDistance, 0)
                / (MaxDepthDistanceOffset)));
        }

        public MainWindow()
        {
            InitializeComponent();

            KinectSensor.KinectSensors.StatusChanged += (object sender, StatusChangedEventArgs e) =>
            {
                if (e.Sensor == _sensor)
                {
                    if (e.Status != KinectStatus.Connected)
                    {
                        SetSensor(null);
                    }
                }
                else if ((_sensor == null) && (e.Status == KinectStatus.Connected))
                {
                    SetSensor(e.Sensor);
                }
            };

            foreach (var sensor in KinectSensor.KinectSensors)
            {
                if (sensor.Status == KinectStatus.Connected)
                {
                    SetSensor(sensor);
                }
            }
        }
    }
}