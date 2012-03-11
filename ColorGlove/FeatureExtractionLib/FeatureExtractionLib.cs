using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;

namespace FeatureExtractionLib
{
    public class FeatureExtraction
    {
        //private short[,] twoDimensionDepth;
        private short[] depth; // one dimensional depth image
        private byte[] label; // one dimensional label image
        //private byte[,] twoDimensionLabel;
        private string directory;
        private const int width = 640, height = 480;

        List<int> listOfTargetPosition, listOfBackgroundPosition;

        List<int[]> listOfOffsetPairs; // Is this an efficient enough data structure? Or is two-dimensional array more efficient. Leave it on future work

        public enum KinectModeFormat { 
            Default = 0,
            Near = 1,
        };

        private KinectModeFormat KinectMode;

        public enum HandGestureFormat
        {
            Background = 0,
            OpenHand = 1,
            CloseHand = 2,
        };
        private HandGestureFormat HandGestureValue;

        public FeatureExtraction(string directory)
        {
            depth = new short[width * height];
            label = new byte[width * height];
            listOfTargetPosition = new List<int>();
            listOfBackgroundPosition = new List<int>();
            listOfOffsetPairs = new List<int[]>();

            setDirectory(directory);
            readOffsetPairs();


            //HandGestureValue = HandGestureFormat.CloseHand;
            //byte tmp = (byte) HandGestureValue;
            //string tmp = "H" + HandGestureValue;
            //Console.WriteLine(tmp);
        }

        public void setKinectMode() { 
        }

        private void setDirectory(string s)
        {
            directory = s;
        }

        private void readOffsetPairs()
        {

        }

        public void generateOffsetPairs(int numOfOffsetPairs)
        {
            Console.WriteLine("The progrom will now generate new pairs of offsets. Risks include training again. Are you sure you would like to continue?(Y/N)");
            ConsoleKeyInfo cki = Console.ReadKey();
            if (cki.Key.ToString() == "Y")
            {
                Console.WriteLine("Generating...");

            }
            else
                Console.WriteLine("No");
        }

        public void testEnum()
        {
            Array values = Enum.GetValues(typeof(HandGestureFormat));
            foreach (HandGestureFormat val in values)
            {
                Console.WriteLine("Name: {0}, numerical value: {1}", val, (byte)val);
            }
        }

        public void testSplit()
        {
            string s = "(-1,0) (123,1)";
            char[] delimiters = new char[] { '(', ')', ',', ' ' };
            string[] parts = s.Split(delimiters,
                             StringSplitOptions.RemoveEmptyEntries);
            Console.Write(parts.Length);
            for (int i = 0; i < parts.Length; i++)
            {
                Console.WriteLine(parts[i]);
            }
        }

        public void testSample()
        {
            randomSample(100);
        }

        public void readDirectory()
        {

            Array values = Enum.GetValues(typeof(HandGestureFormat));
            foreach (HandGestureFormat val in values)
            {
                //Console.WriteLine ("{0}: {1}", Enum.GetName(typeof(HandGestureFormat), val), val);
                if (val == HandGestureFormat.Background)
                    continue;
                string subdirectory = directory + "\\" + val;
                string[] fileEntries = Directory.GetFiles(subdirectory);
                foreach (string filePath in fileEntries)
                {
                    string fileName = Path.GetFileName(filePath);
                    string prefix = fileName.Substring(0, 10);
                    if (prefix == "depthLabel")
                        readFile(filePath);
                    //Console.WriteLine(fileName);
                    return; // debug
                }
                //break; // debug
            }

        }

        public void readFile(string filePath)
        {

            using (System.IO.StreamReader file = new System.IO.StreamReader(filePath))
            {
                string line = file.ReadLine(); // read the whole file into memory
                char[] delimiters = new char[] { '(', ')', ',', ' ' };
                string[] parts = line.Split(delimiters, StringSplitOptions.RemoveEmptyEntries);
                //int X = 0, Y = 0;
                int countTargetLabel = 0;
                int countDepthMinusOne = 0;
                int countBackgounrdLabel = 0;
                for (int i = 0; i < width * height; i++)
                {
                    //twoDimensionDepth[X, Y] = (short) int.Parse( parts[2*i]);
                    depth[i] = (short)int.Parse(parts[2 * i]);
                    //twoDimensionLabel[X, Y] = (byte) int.Parse(parts[2 * i + 1]);
                    label[i] = (byte)int.Parse(parts[2 * i + 1]);
                    if (depth[i] == -1)
                        countDepthMinusOne++;
                    if (label[i] != 0)
                        countTargetLabel++;
                    else
                        countBackgounrdLabel++;
                }
                Console.WriteLine("countTargetLabel:{0}, countDepthMinusOne:{1}, countBackgroundLabel:{2}, totalNumber:{3}", countTargetLabel, countDepthMinusOne, countBackgounrdLabel, width * height);

            };

        }

        private void extractFeature(int position)
        {
            /*
            Debug.Assert(position <= depth.Length, "Trying to access nonexistent feature.");

            if (depth[position] < 0) return;

            // The depth in mm!            
            Tuple<Vector, Vector> feature = features[position];

            int X = position % width, Y = position - X* width;

            Point x_u = x + feature.Item1 / depth_in_mm;
            double x_u_depth;
            if (x_u.X < 0 || x_u.X >= width || x_u.Y < 0 || x_u.Y >= height)
                x_u_depth = outOfBounds;
            else
            {
                int lin = (int)(x_u.Y * width + x_u.X);
                x_u_depth = depth[lin] >> DepthImageFrame.PlayerIndexBitmaskWidth;
                if (x_u_depth == -1) return -1;
            }


            Point x_v = x + feature.Item2 / depth_in_mm;
            double x_v_depth;
            if (x_v.X < 0 || x_v.X >= width || x_v.Y < 0 || x_v.Y >= height)
                x_v_depth = outOfBounds;
            else
            {
                int lin = (int)(x_v.Y * width + x_v.X);
                x_v_depth = depth[lin] >> DepthImageFrame.PlayerIndexBitmaskWidth;
                if (x_v_depth == -1) return -1;
            }

            return x_u_depth - x_v_depth;
             */
        }

        public void Shuffle(List<int> list)
        {
            Random rng = new Random();
            int n = list.Count;
            while (n > 1)
            {
                n--;
                int k = rng.Next(n + 1);
                int value = list[k];
                list[k] = list[n];
                list[n] = value;
            }
        }

        private void randomSample(int numerPerClass)
        // randomly sample numerPerClass of pixel in the depth image
        {
            // First make two lists for the labeled pixels
            listOfBackgroundPosition.Clear();
            listOfTargetPosition.Clear();
            for (int i = 0; i < depth.Length; i++)
            {
                if (label[i] == (byte)HandGestureFormat.Background)
                {
                    listOfBackgroundPosition.Add(i);
                }
                else
                {
                    listOfTargetPosition.Add(i);
                }
            }
            //listOfTargetPosition.Shuffle();
            Shuffle(listOfTargetPosition);
            Shuffle(listOfBackgroundPosition);
            for (int i = 0; i < numerPerClass; i++)
            {
                extractFeature(listOfTargetPosition[i]);
                extractFeature(listOfBackgroundPosition[i]);
            }
        }


    }
}
