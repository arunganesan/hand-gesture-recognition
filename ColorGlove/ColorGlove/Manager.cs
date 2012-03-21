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
        private KinectSensor sensor_;
        private Tuple<byte[], short[]> data;
        private Processor[] processors;
        private enum ProcessorModeFormat { 
            Arun,
            Michael,
        }
        Thread poller;
        DataFeed datafeed;

        public Manager(MainWindow parent)  // Construct function
        {
            datafeed = new DataFeed(DataFeed.DataSource.Kinect);
            sensor_ = datafeed.sensor();

            #region Create and arrange Images
            int total_processors = 2;
            processors = new Processor[total_processors];
            for (int i = 0; i < total_processors; i++)
            {
                processors[i] = new Processor(this.sensor_, this);
                processors[i].lower = 100;
                processors[i].upper = 1000;
                Image image = processors[i].getImage();
                parent.mainContainer.Children.Add(image);
            }
            #endregion

            #region Processor configurations
			
            ProcessorModeFormat ProcessorMode= ProcessorModeFormat.Arun; // set he mode for processor here
            if (ProcessorMode == ProcessorModeFormat.Michael)
            {
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
                                            Processor.Step.Depth
                    // Show overlap offest
                    //                        Processor.Step.OverlayOffset
                    // Show Mapped Depth Using RGB
                    // Processor.Step.PaintWhite,
                    // Processor.Step.MappedDepth
                    // Show the Color Labelling
                    // Processor.Step.PaintWhite,
                    // Processor.Step.ColorMatch
                    // Denoise
                    //                        Processor.Step.Denoise
                );
            }
            else if (ProcessorMode == ProcessorModeFormat.Arun) {
                processors[0].updatePipeline(
                    Processor.Step.Crop,
                    Processor.Step.Color
                );

                processors[1].updatePipeline(
                    Processor.Step.Crop,
                    Processor.Step.PaintGreen,
                    Processor.Step.ColorMatch
                );
            }
            #endregion

            poller = new Thread(new ThreadStart(this.poll));
        }

        public void start()
        {
            poller.Start();
        }

        public void toggleProcessors()
        {
            // Michael: Visual Studo said it's deprecated. Need to change?
            // Arun: Their reason is that if a thread is holding a lock while 
            //          suspended, it may deadlock. We're not using any locks
            //          and pausing a thread like this is convenient, so I think 
            //          we can continue.

            if (poller.ThreadState == System.Threading.ThreadState.Suspended) poller.Resume(); 
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


        public void AutoRange()
        {
            processors[0].AutoDetectRange();
            processors[1].AutoDetectRange();
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
                data = datafeed.PullData();
                foreach (Processor p in processors) p.update(data.Item2, data.Item1);
            }
        }


        public void kMeans()
        {
            processors[0].kMeans();
        }
    }
}
