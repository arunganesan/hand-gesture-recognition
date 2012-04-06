﻿using System;
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
        private KinectData data;
        private Processor[] processors;
        private enum ProcessorModeFormat { 
            Arun,
            Michael,
        }
        Thread poller;
        DataFeed datafeed;
        ProcessorModeFormat ProcessorMode = ProcessorModeFormat.Arun; // set the mode for processor here

        public Manager(MainWindow parent)  // Construct function
        {
            if (ProcessorMode == ProcessorModeFormat.Michael)
                datafeed = new DataFeed(DataFeed.DataSource.Kinect, DataFeed.RangeModeFormat.Near);
            else
                datafeed = new DataFeed(DataFeed.DataSource.Kinect, DataFeed.RangeModeFormat.Default);
            
            #region Create and arrange Images
            int total_processors = 2;
            processors = new Processor[total_processors];
            for (int i = 0; i < total_processors; i++)
            {
                processors[i] = new Processor(this);
                processors[i].lower = 100;
                processors[i].upper = 1000;
                Image image = processors[i].getImage();
                parent.mainContainer.Children.Add(image);
            }
            #endregion

            #region Processor configurations
			
            
            if (ProcessorMode == ProcessorModeFormat.Michael)
            {
                processors[0].updatePipeline(
                    // Show the rgb image
                                            Processor.Step.CopyColor
                    // Show the depth image                                         
                    // Processor.Step.Depth    

                    // Show the Color Labelling                                         
                    // Processor.Step.PaintWhite,
                    // Processor.Step.ColorMatch
                    );


                //processors[0].updatePipeline(Processor.Step.ColorMatch);
                //processors[1].SetTestModule(Processor.ShowExtractedFeatureFormat.PredictOnePixelCPU | Processor.ShowExtractedFeatureFormat.ShowTransformedForOnePixel); // 
                // one should call SetTestModule to active the FeatureExtractionLib
                processors[1].SetTestModule(Processor.ShowExtractedFeatureFormat.PredictAllPixelsGPU); 
                processors[1].updatePipeline(
                    // Show the rgb image
                    // Processor.Step.CopyColor
                    // Show the depth image                                         
                                            Processor.Step.Depth,
                    // Show overlap offest
                                            Processor.Step.OverlayOffset
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
                processors[0].SetTestModule(Processor.ShowExtractedFeatureFormat.ShowTransformedForOnePixel); 
                processors[0].updatePipeline(
                    Processor.Step.PaintWhite,
                    Processor.Step.Crop,
                    Processor.Step.PaintGreen,
                    Processor.Step.CopyColor,
                    //Processor.Step.ColorMatch,
                    //Processor.Step.FeatureExtractOnEnable,
                    Processor.Step.OverlayOffset
                );


                processors[1].SetTestModule(Processor.ShowExtractedFeatureFormat.PredictAllPixelsGPU);
                processors[1].updatePipeline(
                    //Processor.Step.PaintGreen,
                    Processor.Step.Crop,
                    Processor.Step.Depth,
                    //Processor.Step.EnablePredict,
                    Processor.Step.PredictOnEnable,
                    Processor.Step.OverlayOffset);
                
                 
                /*processors[1].SetTestModule(Processor.ShowExtractedFeatureFormat.ShowTransformedForOnePixel);
                processors[1].updatePipeline(
                   Processor.Step.PaintGreen,
                   Processor.Step.Depth,
                   Processor.Step.OverlayOffset
                );*/
                
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
            if (poller.ThreadState == System.Threading.ThreadState.Suspended) poller.Resume(); 
            else poller.Suspend();
        }


        public void saveImages()
        {
            foreach(Processor p in processors) p.EnableFeatureExtract();
            //poller.Suspend();
            //foreach (Processor p in processors) p.processAndSave();
            //processors[0].ProcessAndSave();
            // use processor[0] to save the depth image
            //poller.Resume();
        }


        public void AutoRange()
        {
            processors[0].AutoDetectRange();
            processors[1].AutoDetectRange();
        }

        public void increaseRange()
        {
            processors[0].IncreaseRange();
            processors[1].IncreaseRange();
        }

        public void decreaseRange()
        {
            processors[0].DecreaseRange();
            processors[1].DecreaseRange();
        }

        public void poll()
        {
            while (true)
            {
                data = datafeed.PullData(); 
                foreach (Processor p in processors) p.update(data);
            }
        }


        public void kMeans() { processors[0].kMeans(); }
        

        // Just adds pooling to the pipeline. 
        // XXX: This doesn't have to happen for only processor 1. Pooling can 
        // be enabled for all processors and only the ones with the pooling 
        // added to the pipeline will then perform that action.
        public void Pool() 
        {
            processors[1].EnablePool(); 
        }
    }
}
