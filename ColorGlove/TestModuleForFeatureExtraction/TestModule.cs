using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;


namespace TestModuleForFeatureExtraction
{
    class TestModule
    {
        static void Main(string[] args)
        {
            Console.WriteLine(
                "Hello World");
            // test divide by
            int a = 11, b = 5;
            int c = a / b;
            Console.WriteLine("c={0}", c);

            
            string directory = "C:\\Users\\Michael Zhang\\Desktop\\HandGestureRecognition\\ColorGlove\\ColorGlove\\bin\\Release\\training_samples";
            FeatureExtractionLib.FeatureExtraction Feature = new FeatureExtractionLib.FeatureExtraction( 
                                                                                     FeatureExtractionLib.FeatureExtraction.KinectModeFormat.Near,
                                                                                     directory);

            //Feature.testEnum();
            Feature.readDirectory();

            // // Test offset
            int minOffset = 50*2000;
            int maxOffset = 500;
            Feature.SetOffsetMax(minOffset);
            Feature.SetOffsetMin(maxOffset); 
            Feature.generateOffsetPairs(20);
            
            List<int[]> listOfOffsetPosition = new List<int[]>();

            int curPosition = 300000;
            Feature.getAllOffsetPairs( curPosition, 500, listOfOffsetPosition);
            int CurX = curPosition % 640,  CurY = curPosition / 640;
            Console.WriteLine("Cur({0},{1})", CurX, CurY);
            for (int i = 0; i < listOfOffsetPosition.Count; i++)
            {
                Console.WriteLine("ShiftXU:({0},{1}), ShiftXV:({2},{3})", listOfOffsetPosition[i][0], listOfOffsetPosition[i][1], listOfOffsetPosition[i][2], listOfOffsetPosition[i][3]);
            }
               
                //Feature.testSample();
                //Feature.testSplit();
                Console.ReadKey();
        }
    }
}
