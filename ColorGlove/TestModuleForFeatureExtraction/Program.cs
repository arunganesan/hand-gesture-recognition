using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace TestModuleForFeatureExtraction
{
    class Program
    {
        static void Main(string[] args)
        {
            Console.WriteLine(
                "Hello World");
            string directory = "C:\\Users\\Michael Zhang\\Desktop\\HandGestureRecognition\\ColorGlove\\ColorGlove\\bin\\Release\\training_samples";
            FeatureExtractionLib.FeatureExtraction Feature = new FeatureExtractionLib.FeatureExtraction(directory);
            //Feature.testEnum();
            Feature.readDirectory();
            Feature.generateOffsetPairs(2000);
            //Feature.testSample();
            //Feature.testSplit();
            Console.ReadKey();
        }
    }
}
