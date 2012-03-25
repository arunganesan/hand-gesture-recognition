using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;

namespace FeatureExtractionLib
{
    public class FeatureExtraction
    {
        /* README 
         * How to use mode:
            * There are serval parameters in offset pairs, kinect mode. We use a human readable namespace mapping:
            * mode name -> parameter set.
            * Users of this library should first should the mode name before using the library.
            * Users can also add new mode name, and its corresponding parameter set.
         
         * How to generate offset pairs:
            * If the user wants a new offset pairs, he needs to use a new Mode. After setting the parameters, he can call GenerateOffsetPairs() to generate a set of 
            * offset pairs, and use WriteOffsetPairsToStorage() to store in file system (he doesn't need to care about the file name). And he can use 
            * ReadOffsetPairsFromStorage() to get the offset pairs from the file system (again he doesn't need to worry about the file name, since it's bind into the mode name)
         * How to get transformed points via the offset pairs for a given depth point:
            * First the user needs to specify the mode, then he calls ReadOffsetPairsFromStorage() to get the offset pairs. Finally, he can call 
            * FeatureExtractionLib.FeatureExtraction.GetAllTransformedPairs(..) for a given depth point to get all transformed pair of points.
         
         * How to generate feature vector file:
            * Call FeatureExtractionLib.FeatureExtraction.GenerateFeatureVectorViaImageFiles()     
        */
        public enum ModeFormat { 
            Maize,              
            Blue, // Operate in near Kinect mode, use a large box and a large number of offset
            //Blue149,
            Abstraction, // Operate in default Kinect mode, use a large box and a large number of offset
            BlueDefault
        };

        private ModeFormat Mode;
        private short[] depth; // one dimensional depth image
        private byte[] label; // one dimensional label image        
        double[] RFfeatureVector; // for random forest 
        private const string defaultDirectory = "..\\..\\..\\Data";
        public string directory;
        private const int width = 640, height = 480;        
        private string RFModelFilePath;
        private dforest.decisionforest decisionForest;
        private int uMin, uMax;
        // can try to generate circularly uniform offset pair
        private enum RandomGenerationModeFormat { 
            Default, // which is a box
            Circular, // uniform in a polar system (radius is uniform and angle is uniform)
        };
        private RandomGenerationModeFormat RandomGenerationMode;

        private int numOfOffsetPairs;
        private Random _r;        
        private int sampledNumberPerClass; // used when generating the feature vectors from the image. It sample pixels that are the same class uniformly in an image.
        private int UpperBound; // used when calculating the feature value. UpperBound is given when the depth is <0 or the pixel is out-of-bound.
        private string traningFilename;
        private StreamWriter outputFilestream;
        private List<int> featureVector = new List<int>();

        List<int> listOfTargetPosition, listOfBackgroundPosition;

        List<int[]> listOfOffsetPairs; // Is this an efficient enough data structure? Or is two-dimensional array more efficient. Leave it on future work

        private enum KinectModeFormat
        {
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
        //private HandGestureFormat HandGestureValue;

        public FeatureExtraction(ModeFormat setMode= ModeFormat.Maize, string varDirectory = defaultDirectory)
        // Construct fiunction
        {
            depth = new short[width * height];

            label = new byte[width * height];
            listOfTargetPosition = new List<int>();
            listOfBackgroundPosition = new List<int>();
            listOfOffsetPairs = new List<int[]>();            
            _r= new Random(); // used for random number generator                        
            SetDirectory(varDirectory);
            SetMode(setMode);
            LoadRFModel();
            RFfeatureVector = new double[numOfOffsetPairs];
        }

        private void SetMode(ModeFormat setMode) {
            Mode = setMode;
            switch (setMode) {
                case ModeFormat.Maize:   
                    numOfOffsetPairs = 2000;
                    uMin = 500;
                    uMax = 50 * 2000;
                    sampledNumberPerClass = 2000;
                    UpperBound = 10000;
                    KinectMode = KinectModeFormat.Near;
                    traningFilename = "Maize";
                    RandomGenerationMode = RandomGenerationModeFormat.Default;
                    break;                
                case ModeFormat.Blue:       
                    numOfOffsetPairs = 2000;
                    uMin = 500;
                    uMax = 40 * 2000;
                    sampledNumberPerClass = 1000;
                    UpperBound = 10000;
                    KinectMode = KinectModeFormat.Near;
                    traningFilename = "Blue";
                    RandomGenerationMode = RandomGenerationModeFormat.Circular;
                    RFModelFilePath = directory + "\\FeatureVectureBlue149.rf.model";
                    break;
                /*
                case ModeFormat.Blue149:
                    numOfOffsetPairs = 2000;
                    uMin = 500;
                    uMax = 40 * 2000;
                    sampledNumberPerClass = 1000;
                    UpperBound = 10000;
                    KinectMode = KinectModeFormat.Near;
                    traningFilename = "Blue";
                    RandomGenerationMode = RandomGenerationModeFormat.Circular;
                    // 149 imgs
                    break;
                 */ 
                case ModeFormat.BlueDefault:
                    numOfOffsetPairs = 2000;
                    uMin = 500;
                    uMax = 40 * 2000;
                    sampledNumberPerClass = 1000;
                    UpperBound = 10000;
                    KinectMode = KinectModeFormat.Default;
                    traningFilename = "BlueDefault";
                    RandomGenerationMode = RandomGenerationModeFormat.Circular;
                    break;
                case ModeFormat.Abstraction: // Operate in default Kinect mode, use a large box and a large number of offset
                    numOfOffsetPairs = 2000;
                    uMin = 2000;
                    uMax = 300 * 2000;
                    sampledNumberPerClass = 2000;
                    UpperBound = 10000;
                    KinectMode = KinectModeFormat.Default;
                    traningFilename = "Abstraction";
                    RandomGenerationMode = RandomGenerationModeFormat.Default;
                    break;

            }
            traningFilename = "FeatureVector" + traningFilename + ".txt";
        }
        
        private void SetDirectory(string dir)
        // set working directory
        {     
            directory = dir;
            Console.WriteLine("Current directory: {0}", directory);
            Console.WriteLine("Current working directory: {0}", Directory.GetCurrentDirectory());
        }

        public void SetOffsetMax(int x)
        {            
                uMax = x;
        }

        public void SetOffsetMin(int x)
        {            
                uMin = x;
        }

        private string GetOffsetPairsFilename()
        {          
            return directory + "\\" + "Offset" + Mode + ".txt";
        }

        private void LoadRFModel() {
            decisionForest = new dforest.decisionforest();
            alglib.serializer Serializer = new alglib.serializer();
            string modelFile = System.IO.File.ReadAllText(RFModelFilePath);
            Serializer.ustart_str(modelFile);
            dforest.dfunserialize(Serializer, decisionForest);
            Serializer.stop();
            Console.WriteLine("Finish loading the RF model");
        }

        #region FileOperations
        public void ReadOffsetPairsFromStorage()
        {
            string filename = GetOffsetPairsFilename();
            //Console.WriteLine("Filename: {0}", filename);
            try
            {
                StreamReader SR = new StreamReader(filename);

                string line = SR.ReadLine(); // read the whole file into memory
                char[] delimiters = new char[] { ' ' };
                string[] parts = line.Split(delimiters, StringSplitOptions.RemoveEmptyEntries);
                numOfOffsetPairs = int.Parse(parts[0]);
                listOfOffsetPairs.Clear();

                for (int i = 1; i <= numOfOffsetPairs * 4; i += 4)
                {
                    int[] tmpArray = new int[4];
                    for (int j = 0; j < 4; j++)
                        tmpArray[j] = int.Parse(parts[i + j]);
                    listOfOffsetPairs.Add(tmpArray);
                }
                Console.WriteLine("Read {0} pairs from {1}", numOfOffsetPairs, filename);
                SR.Close();
            }
            catch {
                Console.WriteLine("Something wrong");
            }

        }

        public void WriteOffsetPairsToStorage()
        {
            // File format: numberOfPair Pair1UX Pair1UY Pair1VX Pair1VY Pair2UX Pair2UY Pair2VX Pair2VY
            string filepath = GetOffsetPairsFilename();

            Console.WriteLine("Writing to file: {0}", filepath);            
            using (StreamWriter filestream = new StreamWriter(filepath) )
            {
                filestream.Write(listOfOffsetPairs.Count);
                for (int i = 0; i < listOfOffsetPairs.Count; i++) {
                    filestream.Write(" {0} {1} {2} {3}", listOfOffsetPairs[i][0], listOfOffsetPairs[i][1], listOfOffsetPairs[i][2], listOfOffsetPairs[i][3]);
                }
            }
            Console.WriteLine("Finish writing to file: {0}", filepath);            
        }
        
        #endregion
        
        private void GetOnePairRandomOffset(int [] arrayInt)
            /*Randomly genearte a pair of offset*/
        {
            if (RandomGenerationMode == RandomGenerationModeFormat.Default)
            {
                arrayInt[0] = _r.Next(uMin, uMax);
                arrayInt[1] = _r.Next(uMin, uMax);
                int flipX = _r.Next(2), flipY = _r.Next(2);
                if (flipX == 0)
                    arrayInt[0] = arrayInt[0] * -1;
                if (flipY == 0)
                    arrayInt[1] = arrayInt[1] * -1;
            }
            else if (RandomGenerationMode == RandomGenerationModeFormat.Circular) {
                float radius = _r.Next((int) (uMin * 1.414), (int) (uMax * 1.141));
                float angle = _r.Next(361);
                arrayInt[0] = (int) (radius * Math.Cos(angle / 180.0 * Math.PI));
                arrayInt[1] = (int)(radius * Math.Sin(angle / 180.0 * Math.PI));
            }
        }

        public void GenerateOffsetPairs()
        // Generate offset pairs and write them into memory: listoOfOffsetPairs. Not write them into file yet.
        {
            // Ask permission
            /*
            Console.WriteLine("The progrom will now generate new pairs of offsets. Risks include training again. Are you sure you would like to continue?(Y/N)");
            ConsoleKeyInfo cki = Console.ReadKey();
            
            if (cki.Key.ToString() == "Y")
            */
            //numOfOffsetPairs = setNumOfOffsetPairs; This is set in the Mode
            Console.WriteLine("Generating...");
            int [] u={0,0}, v={0,0};            
            for (int i = 0; i < numOfOffsetPairs; i++)
            {
                GetOnePairRandomOffset(u);
                GetOnePairRandomOffset(v);
                listOfOffsetPairs.Add(new int[] { u[0], u[1], v[0], v[1] });
                //Console.WriteLine("U:({0},{1}), V:({2},{3})", u[0], u[1], v[0], v[1]);
            }
            
            
        }

        private int HelperGetOffset(int Origin, int Offset, int curDepth) {
            return (int)(Origin + (double)((Offset + 0.0) / (curDepth + 0.0)));
        }

        public void GetAllTransformedPairs(int curPosition, int curDepth, List<int[]> listOfTransformedPairPosition)
        {
            int CurX = curPosition % width,  CurY = curPosition/ width;
            for (int i = 0; i < numOfOffsetPairs; i++) { 
                int uX = listOfOffsetPairs[i][0], uY = listOfOffsetPairs[i][1], vX = listOfOffsetPairs[i][2], vY = listOfOffsetPairs[i][3];                                
                int newUX = HelperGetOffset(CurX, uX, curDepth);                
                int newUY = HelperGetOffset(CurY, uY, curDepth);
                int newVX = HelperGetOffset(CurX, vX, curDepth);
                int newVY = HelperGetOffset(CurY, vY, curDepth);
                listOfTransformedPairPosition.Add(new int[] { newUX, newUY, newVX, newVY });
            }
        }

        public void GetFirstNTransformedPairs(int curPosition, int curDepth, List<int[]> listOfTransformedPairPosition, int N)
        {
            int CurX = curPosition % width, CurY = curPosition / width;
            for (int i = 0; i < N; i++)
            {
                int uX = listOfOffsetPairs[i][0], uY = listOfOffsetPairs[i][1], vX = listOfOffsetPairs[i][2], vY = listOfOffsetPairs[i][3];
                int newUX = HelperGetOffset(CurX, uX, curDepth);
                int newUY = HelperGetOffset(CurY, uY, curDepth);
                int newVX = HelperGetOffset(CurX, vX, curDepth);
                int newVY = HelperGetOffset(CurY, vY, curDepth);
                //listOfTransformedPairPosition.Add(new int[] { newUX, newUY, newVX, newVY });
            }
        }

        private int GetDepthByPoint(int X, int Y){
            if (X >= 0 && X < width && Y >= 0 && Y < height)
            {
                int UIndex = Y * width + X;
                if (depth[UIndex] < 0)
                    return UpperBound;
                else
                    return depth[UIndex];
            }
            else
                return UpperBound;
        }

        public void PredictOnePixel(int oneDimensionIndex, short[] depthArray, ref double[] predictOutput)
        {
            List<int[]> aListOfOffsetPosition = new List<int[]>();

            depth = depthArray;
            GetAllTransformedPairs(oneDimensionIndex, depth[oneDimensionIndex], aListOfOffsetPosition);
            //Console.WriteLine("Feature vector: {0}", label[oneDimensionIndex]);          
            //Console.WriteLine("aListOfOffsetPosition.Count:{0}", aListOfOffsetPosition.Count);            
            for (int i = 0; i < aListOfOffsetPosition.Count; i++)
            {
                //int uX = aListOfOffsetPosition[i][0], uY = aListOfOffsetPosition[i][1];
                int uDepth = GetDepthByPoint(aListOfOffsetPosition[i][0], aListOfOffsetPosition[i][1]);
                int vDepth = GetDepthByPoint(aListOfOffsetPosition[i][2], aListOfOffsetPosition[i][3]);
                RFfeatureVector[i] = (uDepth - vDepth);                
            }            
            dforest.dfprocess(decisionForest, RFfeatureVector, ref predictOutput);
            //Console.WriteLine("y[0]:{0}, y[1]:{1},y[2]:{2}", predictOutput[0], predictOutput[1], predictOutput[2]);
        }

        private void ExtractFeatureFromOneDepthPoint(int oneDimensionIndex)
            /*
             * Extract feature vectors and write it a file
             */
        {            
            List<int[]> aListOfOffsetPosition = new List<int[]>();

            GetAllTransformedPairs(oneDimensionIndex, depth[oneDimensionIndex], aListOfOffsetPosition);
            //Console.WriteLine("Feature vector: {0}", label[oneDimensionIndex]);
            outputFilestream.Write("{0}", label[oneDimensionIndex]);
            //Console.WriteLine("aListOfOffsetPosition.Count:{0}", aListOfOffsetPosition.Count);
            featureVector.Clear();

            for (int i = 0; i < aListOfOffsetPosition.Count; i++)
            {
                //int uX = aListOfOffsetPosition[i][0], uY = aListOfOffsetPosition[i][1];
                int uDepth = GetDepthByPoint(aListOfOffsetPosition[i][0], aListOfOffsetPosition[i][1]);
                int vDepth = GetDepthByPoint(aListOfOffsetPosition[i][2], aListOfOffsetPosition[i][3]);
                featureVector.Add(uDepth - vDepth);
                //Console.Write(" {0}", uDepth - vDepth);
                
            }            
            for (int i=0; i< featureVector.Count; i++)
                if (featureVector[i]!=0) // only write non-zero feature, to utilize sparcity
                    outputFilestream.Write(" {0}:{1}", i + 1, featureVector[i]); //notice a plus sign here
            outputFilestream.WriteLine();
        }

        private void ReadImageFileToGetDepthAndLabel(string filePath)
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
                    depth[i] = (short)int.Parse(parts[2 * i]);                  
                    label[i] = (byte)int.Parse(parts[2 * i + 1]);
                    if (depth[i] == -1)
                        countDepthMinusOne++;
                    if (label[i] != 0)
                        countTargetLabel++;
                    else
                        countBackgounrdLabel++;
                }
                Console.WriteLine("countTargetLabel:{0}, countDepthMinusOne:{1}, countBackgroundLabel:{2}, totalNumber:{3}", countTargetLabel, countDepthMinusOne, countBackgounrdLabel, width * height);
                file.Close();
            };

        }

        public void GenerateFeatureVectorViaImageFiles()
        {

            Array values = Enum.GetValues(typeof(HandGestureFormat));
            outputFilestream = new StreamWriter(directory + "\\" + traningFilename);         
            foreach (HandGestureFormat val in values) // go through each directory
            {
                //Console.WriteLine ("{0}: {1}", Enum.GetName(typeof(HandGestureFormat), val), val);
                if (val == HandGestureFormat.Background)
                    continue;
                string subdirectory = directory + "\\" + val + KinectMode;
                Console.WriteLine("Current directoray: {0}", subdirectory);
                string[] fileEntries = Directory.GetFiles(subdirectory);
                int tmpCounter = 0;
                foreach (string filePath in fileEntries) // go through each file within the subdirectory
                {
                    tmpCounter++;                    
                    string fileName = Path.GetFileName(filePath);
                    Console.WriteLine("Generating feature vecor for files {0}", fileName);
                    string prefix = fileName.Substring(0, 10);
                    if (prefix == "depthLabel")
                    {
                        ReadImageFileToGetDepthAndLabel(filePath);
                        RandomSample(sampledNumberPerClass); // including writing to file
                    }
                    /*
                    if (tmpCounter==1) 
                        break; // debug
                    */ 
                }
                //break; // debug
            }

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

        private void RandomSample(int numerPerClass)
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
                ExtractFeatureFromOneDepthPoint(listOfTargetPosition[i]);                
                ExtractFeatureFromOneDepthPoint(listOfBackgroundPosition[i]);
                //return; //debug
            }
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
            RandomSample(100);
        }
    }
}
