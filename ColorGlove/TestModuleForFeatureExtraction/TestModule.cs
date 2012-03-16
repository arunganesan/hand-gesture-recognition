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
            FeatureExtractionTest.TestGenerateFeatures();
            //FeatureExtractionTest.TestGenerateOffset();
            Console.ReadKey();
        }

        public void SetupFeatureExtraction() {
            //Default direcotry: "..\\..\\..\\Data";
            // To setup the mode, see README in the library
            FeatureExtraction.ModeFormat MyMode = FeatureExtraction.ModeFormat.Blue;            
                        Feature = new FeatureExtractionLib.FeatureExtraction(
                                                                                     MyMode);                                                                                     
        }

        private void TestGenerateFeatures()
        {
            Feature.ReadOffsetPairsFromStorage();
            Feature.GenerateFeatureVectorViaImageFiles();
        }

        private void TestDisplay(){
            Feature.ReadOffsetPairsFromStorage();
            // Dispaly offset            
            List<int[]> listOfOffsetPosition = new List<int[]>();

            int curPosition = 300000;
            Feature.GetAllTransformedPairs(curPosition, 500, listOfOffsetPosition);
            int CurX = curPosition % 640, CurY = curPosition / 640;
            Console.WriteLine("Cur({0},{1})", CurX, CurY);
            for (int i = 0; i < listOfOffsetPosition.Count; i++)
            {
                Console.WriteLine("ShiftXU:({0},{1}), ShiftXV:({2},{3})", listOfOffsetPosition[i][0], listOfOffsetPosition[i][1], listOfOffsetPosition[i][2], listOfOffsetPosition[i][3]);
            }            
        }

        private void TestGenerateOffset()
        {            
            Feature.GenerateOffsetPairs(); 
            Feature.WriteOffsetPairsToStorage();                    
        }
    }
}
