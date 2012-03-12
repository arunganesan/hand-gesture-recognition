/*
using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows.Controls;
using System.Windows.Input;
using System.Threading;
using System.Diagnostics;
using Microsoft.Kinect;
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
using System.Threading;

using System.Diagnostics;
using System.Drawing.Imaging;
using Microsoft.Kinect;

namespace ColorGlove
{

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
        private Processor[] processors;
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
                                         Processor.Step.Color
                // Show the depth image                                         
                                        // Processor.Step.Depth    
                                         
                // Show the Color Labelling                                         
                                        // Processor.Step.PaintWhite,
                                        // Processor.Step.ColorMatch
                                          
           ); 
           

            //processors[0].updatePipeline(Processor.Step.ColorMatch);
            
            processors[1].updatePipeline(
                // Show the rgb image
                                        // Processor.Step.Color
                // Show the depth image                                         
                //                        Processor.Step.Depth,
                // Show overlap offest
                //                        Processor.Step.OverlayOffset
                // Show Mapped Depth Using RGB
                                        //Processor.Step.PaintWhite,
                                        //Processor.Step.MappedDepth
                // Show the Color Labelling
                                        Processor.Step.PaintWhite,
                                        Processor.Step.ColorMatch
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

                
                //if (RangeModeValue == RangeModeFormat.Near)
                //    sensor.DepthStream.Range = DepthRange.Near; // set near mode 

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
            processors[0].increaseRange();
            processors[1].increaseRange();
        }

        public void decreaseRange()
        {
            processors[0].decreaseRange();
            processors[1].decreaseRange();
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
}
