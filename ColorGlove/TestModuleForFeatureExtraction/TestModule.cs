using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using FeatureExtractionLib;

namespace TestModuleNamespace
{
    class TestModule
    {
        private FeatureExtractionLib.FeatureExtraction Feature;
        private int minOffset;
        private int maxOffset;
        private int NumofOffset;

        static void Main(string[] args)
        {
            Console.WriteLine(
                "Hello World");            
            // test FeatureExtactionLib
            TestModule FeatureExtractionTest = new TestModule();
            FeatureExtractionTest.SetupFeatureExtraction();
            //FeatureExtractionTest.TestDisplay();

            Console.ReadKey();
        }

        public void SetupFeatureExtraction() {
            //Default direcotry: "..\\..\\..\\Data";
            // Need to setup the offset mode, and Kinect mode (near/default)                
            FeatureExtraction.OffsetModeFormat OffsetMode = FeatureExtraction.OffsetModeFormat.PairsOf2000UniformDistribution;
            FeatureExtraction.KinectModeFormat KinectMode = FeatureExtraction.KinectModeFormat.Near;
                        Feature = new FeatureExtractionLib.FeatureExtraction(
                                                                                     KinectMode,
                                                                                     OffsetMode);
                                                                                     //directory);                        
        }

        private void TestDisplay(){
            Feature.ReadOffsetPairsFromFile();
            // Dispaly offset            
            List<int[]> listOfOffsetPosition = new List<int[]>();

            int curPosition = 300000;
            Feature.GetAllOffsetPairs(curPosition, 500, listOfOffsetPosition);
            int CurX = curPosition % 640, CurY = curPosition / 640;
            Console.WriteLine("Cur({0},{1})", CurX, CurY);
            for (int i = 0; i < listOfOffsetPosition.Count; i++)
            {
                Console.WriteLine("ShiftXU:({0},{1}), ShiftXV:({2},{3})", listOfOffsetPosition[i][0], listOfOffsetPosition[i][1], listOfOffsetPosition[i][2], listOfOffsetPosition[i][3]);
            }            
        }

        private void TestGenerateOffset()
        {
            // Set up parameter for generating offset pairs
            minOffset = 50 * 2000;
            maxOffset = 500;
            NumofOffset = 2000;
            // Generate offset file and write to file            
            Feature.SetOffsetMax(minOffset);
            Feature.SetOffsetMin(maxOffset);
            Feature.GenerateOffsetPairs(NumofOffset); // Note this number has to be consistent with FeatureExtraction.OffsetModeFormat                        
            Feature.WriteOffsetPairsToFile();
                    
        }
    }
}
