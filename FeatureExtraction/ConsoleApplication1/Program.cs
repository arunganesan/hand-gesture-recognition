using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
//using System.Diagnostics;
using FeatureExtraction;
namespace ConsoleApplication1
{
    class Program
    {
        static void Main(string[] args)
        {
            Console.WriteLine(
                "Hello World");
            string directory = "C:\\Users\\Michael Zhang\\Desktop\\HandGestureRecognition\\ColorGlove\\ColorGlove\\bin\\Release\\training_samples";
            FeatureExtraction.FeatureExtraction Feature = new FeatureExtraction.FeatureExtraction(directory);
            //Feature.testEnum();
            Feature.readDirectory();
            Feature.testSample();
            //Feature.testSplit();
            Console.ReadKey();
        }
    }
}
