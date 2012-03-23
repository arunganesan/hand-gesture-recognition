using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using System.Diagnostics;
using Microsoft.Kinect;

namespace ColorGlove
{
    class DataFeed
    {
        public enum RangeModeFormat { Default, Near };
        public enum DataSource { Kinect, File };
        private RangeModeFormat RangeModeValue;
        private DataSource source;
        private KinectSensor sensor_;
        Tuple<byte[], short[]> data;
        private byte[] colorPixels;
        private short[] depthPixels;
        private int width = 640, height = 480, stride = 4;
        // Timeout for the next fram in ms 
        private int DepthTimeout = 1000;
        private int ColorTimeout = 1000;
        
        public DataFeed(DataSource source, RangeModeFormat setRangeModeValue)
        {
            this.source = source;
            RangeModeValue = setRangeModeValue;
            colorPixels = new byte[width * height * stride];
            depthPixels = new short[width * height];
            data = new Tuple<byte[], short[]>(colorPixels, depthPixels);

            // Create the Kinect sensor anyway. Need for mapping depth to image.
            KinectSensor.KinectSensors.StatusChanged += (object sender, StatusChangedEventArgs e) =>
            {
                if (e.Sensor == sensor_ && e.Status != KinectStatus.Connected) setSensor(null);
                else if ((sensor_ == null) && (e.Status == KinectStatus.Connected)) setSensor(e.Sensor);
            };

            foreach (var sensor in KinectSensor.KinectSensors)
                if (sensor.Status == KinectStatus.Connected) setSensor(sensor);
        }

        private void setSensor(KinectSensor newSensor)
        {
            Console.WriteLine("Set sensor!");
            if (sensor_ != null) sensor_.Stop();
            sensor_ = newSensor;
            if (sensor_ != null)
            {
                Debug.Assert(sensor_.Status == KinectStatus.Connected, "This should only be called with Connected sensors.");
                sensor_.ColorStream.Enable(ColorImageFormat.RgbResolution640x480Fps30);
                sensor_.DepthStream.Enable(DepthImageFormat.Resolution640x480Fps30);
                
                if (RangeModeValue == RangeModeFormat.Near)
                    sensor_.DepthStream.Range = DepthRange.Near; // set near mode 

                colorPixels = new byte[640 * 480 * 4];
                depthPixels = new short[640 * 480];
                sensor_.Start();
            }
        }

        public KinectSensor sensor() { return sensor_; }

        public Tuple<byte[], short[]> PullData()
        {
            using (var frame = sensor_.DepthStream.OpenNextFrame(DepthTimeout))
                if (frame != null) 
                    frame.CopyPixelDataTo(data.Item2);

            using (var frame = sensor_.ColorStream.OpenNextFrame(ColorTimeout))
                if (frame != null) 
                    frame.CopyPixelDataTo(data.Item1);

            return data;
        }
    }
}
