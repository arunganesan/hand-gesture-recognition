using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.Diagnostics;

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
            BlueDefault,
            F1000,
            F2000,
            F3000,
            Demo1000,
        };
        public enum CPUorGPUFormat
        {
            CPU=1,
            GPU=2,
        };
        private enum KinectModeFormat
        {
            Default = 0,
            Near = 1,
        };
        private enum RandomGenerationModeFormat
        {
            Default, // which is a box
            Circular, // uniform in a polar system (radius is uniform and angle is uniform)
        };

        public int num_classes_;
        private ModeFormat Mode;
        // one dimensional depth image
        private short[] depth;
        // one dimensional label image        
        private byte[] label;
        // for random forest 
        double[] RFfeatureVector;
        // feature vector for GPU
        short[] RFfeatureVectorShort; 
        private const string defaultDirectory = "..\\..\\..\\Data";
        public string directory;
        private const int width = 640, height = 480; 
        private string RF_model_file_path_;
        // random forest object from Alglib
        private dforest.decisionforest decisionForest;
        // trees in int type
        private int[] trees_int_;
        private GPUCompute myGPU_; // GPU
        private int uMin, uMax;        
        private RandomGenerationModeFormat RandomGenerationMode;
        private int numOfOffsetPairs;
        private Random _r;
        // used when generating the feature vectors from the image. It sample pixels that are the same class uniformly in an image.
        private int sampledNumberPerClass;
        // used when calculating the feature value. UpperBound is given when the depth is <0 or the pixel is out-of-bound.
        private int UpperBound; 
        private string traningFilename;
        private StreamWriter output_filestream_;
        private List<int> feature_vector_ = new List<int>();
        List<int> listOfTargetPosition, listOfBackgroundPosition;
        List<int[]> offset_pair_list_; 
        private KinectModeFormat kinect_mode_;
        private CPUorGPUFormat xPU_mode_;

        private int debug_current_depth_, max_depth_;
        private const int kMaxDepth = 1000;

        // Construct fiunction
        public FeatureExtraction(ModeFormat setMode= ModeFormat.Maize, string varDirectory = defaultDirectory, CPUorGPUFormat to_set_xPU_mode=CPUorGPUFormat.GPU)        
        {
            depth = new short[width * height];
            label = new byte[width * height];
            listOfTargetPosition = new List<int>();
            listOfBackgroundPosition = new List<int>();
            offset_pair_list_ = new List<int[]>();
            // used for random number generator                        
            _r= new Random(); 
            SetDirectory(varDirectory);
            SetMode(setMode);
            
             
            LoadRFModel();
            RFfeatureVector = new double[numOfOffsetPairs];
            RFfeatureVectorShort = new short[numOfOffsetPairs];
            xPU_mode_ = to_set_xPU_mode;
            if (xPU_mode_ == CPUorGPUFormat.GPU) {
                InitGPU();
            }
            
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
                    kinect_mode_ = KinectModeFormat.Near;
                    traningFilename = "Maize";
                    RandomGenerationMode = RandomGenerationModeFormat.Default;
                    break;                
                case ModeFormat.Blue:       
                    numOfOffsetPairs = 2000;
                    uMin = 500;
                    uMax = 40 * 2000;
                    //uMax = 100* 2000, 200*2000, 60*2000, 20*2000;
                    sampledNumberPerClass = 1000;
                    UpperBound = 10000;
                    kinect_mode_ = KinectModeFormat.Near;
                    traningFilename = "Blue";
                    RandomGenerationMode = RandomGenerationModeFormat.Circular;
                    RF_model_file_path_ = directory + "\\FeatureVectureBlue149.rf.model";
                    //RF_model_file_path_ = directory + "\\FeatureVectureBlue149.rf.model.prune.model";                    
                    num_classes_ = 3;
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
                    kinect_mode_ = KinectModeFormat.Default;
                    traningFilename = "BlueDefault";
                    RandomGenerationMode = RandomGenerationModeFormat.Circular;
                    RF_model_file_path_ = directory + "\\FeatureVectorBlueDefault.400.rf.model";
                    num_classes_ = 5;
                    break;
                case ModeFormat.Abstraction: // Operate in default Kinect mode, use a large box and a large number of offset
                    numOfOffsetPairs = 2000;
                    uMin = 2000;
                    uMax = 300 * 2000;
                    sampledNumberPerClass = 2000;
                    UpperBound = 10000;
                    kinect_mode_ = KinectModeFormat.Default;
                    traningFilename = "Abstraction";
                    RandomGenerationMode = RandomGenerationModeFormat.Default;
                    break;


                
                case ModeFormat.F1000:
                    numOfOffsetPairs = 1000;
                    uMin = 500;
                    uMax = 40 * 2000;
                    sampledNumberPerClass = 1000;
                    UpperBound = 10000;
                    kinect_mode_ = KinectModeFormat.Default;
                    traningFilename = "F1000";
                    RandomGenerationMode = RandomGenerationModeFormat.Circular;
                    //RFModelFilePath = directory + "\\FeatureVectorF1000.400.rf.model";
                    //RFModelFilePath = directory + "\\RF.1000.100.3.model";
                    RF_model_file_path_ = directory + "\\RF.1000.10.2.model";
                    num_classes_ = 5;
                    break;
                case ModeFormat.F2000:
                    numOfOffsetPairs = 2000;
                    uMin = 500;
                    uMax = 40 * 2000;
                    sampledNumberPerClass = 1000;
                    UpperBound = 10000;
                    kinect_mode_ = KinectModeFormat.Default;
                    traningFilename = "F2000";
                    RandomGenerationMode = RandomGenerationModeFormat.Circular;
                    //RF_model_file_path_ = directory + "\\FeatureVectorF2000.400.rf.model";
                    RF_model_file_path_ = directory + "\\RF.2000.689.3.model"; 
                    num_classes_ = 5;
                    break;
                case ModeFormat.F3000:
                    numOfOffsetPairs = 3000;
                    uMin = 500;
                    uMax = 40 * 2000;
                    sampledNumberPerClass = 1000;
                    UpperBound = 10000;
                    kinect_mode_ = KinectModeFormat.Default;
                    traningFilename = "F3000";
                    RandomGenerationMode = RandomGenerationModeFormat.Circular;
                    //RFModelFilePath = directory + "\\FeatureVectorF3000.400.rf.model";
                    RF_model_file_path_ = directory + "\\RF.2000.350.3.model"; 
                    num_classes_ = 5; 
                    break;
                case ModeFormat.Demo1000:
                    numOfOffsetPairs = 1000;
                    uMin = 500;
                    uMax = 40 * 2000;
                    sampledNumberPerClass = 1000;
                    UpperBound = 10000;
                    kinect_mode_ = KinectModeFormat.Near;
                    traningFilename = "Demo1000";
                    RandomGenerationMode = RandomGenerationModeFormat.Circular;
                    //RFModelFilePath = directory + "\\FeatureVectorF1000.400.rf.model";
                    //RFModelFilePath = directory + "\\RF.1000.100.3.model";
                    RF_model_file_path_ = directory + "\\RF.demo.1000.800.1.model";
                    num_classes_ = 3;
                    break;
               
            }
        }

        private void InitGPU()
        {
            Console.WriteLine("Start calling GPU");
            // initialize the GPU compute, including compiling. You can set which source to use in the construct function. See the default setting. 
            myGPU_ = new GPUCompute(); 
            
            // change the number of tree
            //decisionForest.ntrees = 3;

            //int[] new_trees;
            // Prune the random forest, change trees_int_
            PruneRFModel();
            
            #region transform                        
            /*
            // Transform the tree into breath-first format            
            // ? 
            new_trees = new int[trees_int_.Length];
            TransformTrees(new_trees, trees_int_, decisionForest.ntrees);
            // maybe need to clear the memory?
            trees_int_ = new_trees;
            Console.WriteLine("Successfuly transform the trees");
            // End of transformation            
             */
            #endregion

            // show max depth of each tree
            //FindMaxDepthRandomForest(trees_int_, decisionForest.ntrees);
            myGPU_.LoadTrees(trees_int_, (short)decisionForest.nclasses, (short)decisionForest.ntrees, decisionForest.nvars);
            Console.WriteLine("Successfuly load the trained random forest into GPU");
        }

        public void PruneRFModel()
        {
            #region prune
            int[] new_trees;
            // Prune the tree            
            Debug.WriteLine("Start pruning");
            new_trees = new int[trees_int_.Length];
            PruneTrees(ref new_trees, trees_int_, 20, decisionForest.ntrees);
            trees_int_ = new_trees;
            decisionForest.bufsize = trees_int_.Length;
            Console.WriteLine("Successfully prune the trees, the resulting tree size is {0}", trees_int_.Length);
            #endregion
        }

        #region transform tree into breath first
        // This function trasform the tree data structure from Alglib to breadth-first data structure.
        public void TransformTrees(int []new_tree, int [] old_tree, int num_trees)
        {
            //int [] new_trees = new int[trees_int_.Length];

            // Alglib TREE FORMAT (Very important-Michael)
            // W[Offs]      -   size of sub-array
            //     node info:
            // W[K+0]       -   variable number        (-1 for leaf mode)
            // W[K+1]       -   threshold              (class/value for leaf node)
            // W[K+2]       -   ">=" branch index      (absent for leaf node)
            // K+3            -   left child

            // New tree format in breadth-first 
            // W[Offs]      -   size of sub-array
            //     node info:
            // W[K+0]       -   variable number        (-1 for leaf mode)
            // W[K+1]       -   threshold              (class/value for leaf node)
            // W[K+2]       -   "<" branch index      (absent for leaf node)
            // W[K+2], W[K+2]+1            -   left child and right child index
            int offset = 0;
            for (int i = 0; i < num_trees; i++)
            {
                HelperTransformSingleTree(new_tree, old_tree, offset);
                
                offset += old_tree[offset];
            }
        }

        public void HelperTransformSingleTree(int[] new_tree, int[] old_tree, int new_tree_off)
        {            
            Queue<int> my_queue = new Queue<int>();            
            int new_tree_index = new_tree_off,                 
                new_tree_last_index = 0;
            // copy the size 
            new_tree[new_tree_index] = old_tree[new_tree_index];
            new_tree_index++;

            new_tree_last_index +=4;
            my_queue.Enqueue(new_tree_index);
            while (my_queue.Count != 0) {
                int top_index = my_queue.Dequeue();
                // copy the feature value and threshold
                new_tree[new_tree_index] = old_tree[top_index];
                new_tree[new_tree_index + 1] = old_tree[top_index + 1];
                // if the current node is not a leaf node
                if (new_tree[new_tree_index] != -1)
                {
                    // update the new tree branch
                    new_tree[new_tree_index + 2] = new_tree_last_index;
                    // if the left children is not a leaf node
                    if (old_tree[top_index + 3  ] != -1)
                        new_tree_last_index += 3;
                    // if the left children is a leaf node
                    else
                        new_tree_last_index += 2;
                    // make the left child and right child locate nearby
                    // if the right children is not a leaf node
                    if (old_tree[old_tree[top_index + 2  ] + new_tree_off ] != -1)
                        new_tree_last_index += 3;
                    else
                        // if the right children is a leaf node
                        new_tree_last_index += 2;
                    // advance the current node 
                    new_tree_index += 3;
                    // enqueue the node's children
                    my_queue.Enqueue(top_index + 3);
                    my_queue.Enqueue(old_tree[top_index + 2] + new_tree_off);
                }
                else
                    new_tree_index += 2;
            }
            //Console.WriteLine("Done"); //debug
        }

        #endregion

        // This function prune the Algbli-format tree to a depth-limit tree
        public void PruneTrees(ref int[]  new_tree, int[] old_tree, int max_depth, int num_tree)
        {
            int new_offset = 0, old_offset = 0, new_tree_size = 0;
            max_depth_ = 0;
            for (int i = 0; i < num_tree; i++)
            {
                Debug.WriteLine("Tree {0}", i);
                new_tree[new_offset] = HelperPruneSingleTree(new_tree, old_tree, new_offset, old_offset, 1, new_offset + 1, old_offset + 1, max_depth) - new_offset;
                old_offset += old_tree[old_offset]; // bug: old_offset = old_tree[old_offset]
                new_tree_size += new_tree[new_offset];
                new_offset += new_tree[new_offset];
            }
            Array.Resize(ref new_tree, new_tree_size);
        }

        // Helper, return the last new index the tree array
        public int HelperPruneSingleTree(int[] new_tree, int[] old_tree, int new_offset, int old_offset, int cur_depth, int index_new_tree, int index_old_tree, int max_depth)
        { 
            // first copy the feature value and threshold
            new_tree[index_new_tree] = old_tree[index_old_tree];
            new_tree[index_new_tree+1] = old_tree[index_old_tree+1];
            // if the node is a leaf
            if (old_tree[index_old_tree] == -1)
            {
                // return the last available index
                return index_new_tree + 2;
            }
            // the node is non-leaf
            // current depth is less than max
            if (cur_depth < max_depth)
            {
                // first copy the left pruned tree
                int last_index = HelperPruneSingleTree(new_tree, old_tree, new_offset, old_offset, cur_depth + 1, index_new_tree + 3, index_old_tree + 3, max_depth);
                new_tree[index_new_tree + 2] = last_index - new_offset;
                return HelperPruneSingleTree(new_tree, old_tree, new_offset, old_offset, cur_depth + 1, last_index, old_tree[index_old_tree + 2] + old_offset, max_depth);
            }
            // prune the tree, find the most probable labels in the subtree
            else { 
                int[] y=new int[decisionForest.nclasses];
                debug_current_depth_ = cur_depth;
                int explored_depth=0;
                HelperFindLablesInTree(old_tree, index_old_tree, old_offset, y, explored_depth);
                int max_label_count = 0, y_max=0;
                for (int i=0; i< decisionForest.nclasses; i++)
                    if (y[i] > max_label_count)
                    {
                        max_label_count = y[i];
                        y_max = i;
                    }
                new_tree[index_new_tree] = -1;
                new_tree[index_new_tree + 1] = y_max;
                return index_new_tree + 2;
            }
        }
        // Helper, give a distribution of the labels in a given node
        private void HelperFindLablesInTree(int[] tree, int index, int offset, int[] y, int explored_depth) {
            // is a leaf
            // if the depth is super long, make the prediction as background
            if (explored_depth > kMaxDepth)
            {
                y[0]++;
                return;
            }
            if (tree[index] == -1)
            {
                y[tree[index + 1]]++;
            }
            else
            { 
                // traverse the left child
                //Debug.WriteLine("depth: {0}", explored_depth);
                if (explored_depth > max_depth_) {
                    max_depth_ = explored_depth;
                    Debug.WriteLine("current max explored depth: {0}", explored_depth);
                }                
               // Debug.Assert(explored_depth < 1000, "depth is greater than 10000!");
                if (index + 3 >= tree.Length)
                {
                    Debug.WriteLine("left child is out of the bound of the old tree!. Index: {0}, Tree.Length: {1}", index, tree.Length);
                }
                Debug.Assert(index +3< tree.Length, "left child is out of the bound of the old tree!");
                
                HelperFindLablesInTree(tree, index + 3, offset, y, explored_depth+1);
                // traverse the right child
                Debug.Assert(tree[index + 2] + offset < tree.Length, "right child is out of the bound of the old tree!");
                HelperFindLablesInTree(tree, tree[index + 2] + offset, offset, y, explored_depth+1);
            }
        }

        // Test function: find the max depth of a random forest
        public void FindMaxDepthRandomForest(int[] tree, int n_tree) {
            int offset = 0;
            for (int i = 0; i < n_tree; i++)
            {
                //Console.WriteLine("Tree: {0}, Max depth: {1}", i, FindMaxDepthSingleTree(tree, offset + 1, offset));
                offset = tree[offset] + offset;
            }
        }
        
        // Test function: find the max depth of a tree
        private int FindMaxDepthSingleTree(int[] tree, int index, int offset)
        {
            if (tree[index] == -1)
                return 0;
            return Math.Max( FindMaxDepthSingleTree(tree, index+3, offset),  
                                    FindMaxDepthSingleTree(tree, tree[index+2] + offset, offset))
                                   +1;
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
            // Offset file name: "Offset[Mode].txt"
            return directory + "\\" + "Offset" + Mode + ".txt";
        }

        public void LoadRFModel(string file_name = "")
        {
            if (file_name == "")
                file_name = RF_model_file_path_;
            
            decisionForest = new dforest.decisionforest();
            alglib.serializer Serializer = new alglib.serializer();
            string modelFile = System.IO.File.ReadAllText(file_name);
            Serializer.ustart_str(modelFile);
            dforest.dfunserialize(Serializer, decisionForest);
            Serializer.stop();
            Console.WriteLine("Finish loading the RF model");
            Console.WriteLine("Total tree size: {0}", decisionForest.trees.Length);
            int treeSize = (int)(decisionForest.trees.Length / decisionForest.ntrees);
            Console.WriteLine("single tree size:{0}", treeSize);
            Console.WriteLine("Number of variable: {0}", decisionForest.nvars);
            Console.WriteLine("ntress: {0}", decisionForest.ntrees);
            Console.WriteLine("nclasses: {0}", decisionForest.nclasses);
            // turn the tree from double type to int type to make it more efficient
            trees_int_ = new int[decisionForest.trees.Length];
            Console.WriteLine("Length of the original tree: {0}", trees_int_.Length);
            for (int i = 0; i < decisionForest.trees.Length; i++)
                trees_int_[i] = (int)Math.Ceiling(decisionForest.trees[i]);

        }

        public void WriteRFModel(string file_name) { 
            double [] new_tree = new double[trees_int_.Length];
            for (int i=0; i< trees_int_.Length; i++)
                new_tree[i] = (double) trees_int_[i];
            decisionForest.trees = new_tree;

            alglib.serializer Serializer = new alglib.serializer();
            Serializer.alloc_start();
            dforest.dfalloc(Serializer, decisionForest);
            Serializer.sstart_str();
            dforest.dfserialize(Serializer, decisionForest);
            Serializer.stop();
            string model_file = Serializer.get_string();
            Console.WriteLine("Finish serializing the random forest");

            using (StreamWriter outfile =
                new StreamWriter(file_name))
                {
                    outfile.Write(model_file);
                };
            Console.WriteLine("Finish writting the file {0} to the disk", file_name);
        }

        #region FileOperations
        // listOfOffsetPairs format:
        // Pair1UX Pair1UY Pair1VX Pair1VY Pair2UX Pair2UY Pair2VX Pair2VY ...
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
                offset_pair_list_.Clear();

                for (int i = 1; i <= numOfOffsetPairs * 4; i += 4)
                {
                    int[] tmpArray = new int[4];
                    for (int j = 0; j < 4; j++)
                        tmpArray[j] = int.Parse(parts[i + j]);
                    offset_pair_list_.Add(tmpArray);
                }
                Console.WriteLine("Read {0} pairs from {1}", numOfOffsetPairs, filename);
                SR.Close();
                // copy the offset list to GPU
                if (xPU_mode_ == CPUorGPUFormat.GPU) {
                    int[] offset_list_int = new int [offset_pair_list_.Count * 4];
                    for (int i = 0; i < offset_pair_list_.Count; i++)
                    { 
                        int index = i*4;
                        offset_list_int[index] = offset_pair_list_[i][0];
                        offset_list_int[index+1] = offset_pair_list_[i][1];
                        offset_list_int[index + 2] = offset_pair_list_[i][2];
                        offset_list_int[index + 3] = offset_pair_list_[i][3];
                    }                        
                    myGPU_.LoadOffsets(offset_list_int);
                }
            }
            catch {
                Console.WriteLine("Something wrong. Couldn't read the file {0}", filename);
              
            }

        }

        public void WriteOffsetPairsToStorage()
        {
            // File format: numberOfPair Pair1UX Pair1UY Pair1VX Pair1VY Pair2UX Pair2UY Pair2VX Pair2VY
            string filepath = GetOffsetPairsFilename();

            Console.WriteLine("Writing to file: {0}", filepath);            
            using (StreamWriter filestream = new StreamWriter(filepath) )
            {
                filestream.Write(offset_pair_list_.Count);
                for (int i = 0; i < offset_pair_list_.Count; i++) {
                    filestream.Write(" {0} {1} {2} {3}", offset_pair_list_[i][0], offset_pair_list_[i][1], offset_pair_list_[i][2], offset_pair_list_[i][3]);
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

        // Generate offset pairs and write them into memory: listoOfOffsetPairs. Not write them into file yet.
        public void GenerateOffsetPairs()        
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
                offset_pair_list_.Add(new int[] { u[0], u[1], v[0], v[1] });
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
                int uX = offset_pair_list_[i][0], uY = offset_pair_list_[i][1], vX = offset_pair_list_[i][2], vY = offset_pair_list_[i][3];                                
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
                int uX = offset_pair_list_[i][0], uY = offset_pair_list_[i][1], vX = offset_pair_list_[i][2], vY = offset_pair_list_[i][3];
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

        private void GetFeatureVectorsFromOneDepthPoint(int oneDimensionIndex, short[] depthArray)
        /* 
         * Output: RFfeaturevector (global double)
         */
        {
            List<int[]> aListOfOffsetPosition = new List<int[]>();
            GetAllTransformedPairs(oneDimensionIndex, depthArray[oneDimensionIndex], aListOfOffsetPosition);
            //Console.WriteLine("Feature vector: {0}", label[oneDimensionIndex]);          
            //Console.WriteLine("aListOfOffsetPosition.Count:{0}", aListOfOffsetPosition.Count);            
            for (int i = 0; i < aListOfOffsetPosition.Count; i++)
            {
                //int uX = aListOfOffsetPosition[i][0], uY = aListOfOffsetPosition[i][1];
                int uDepth = GetDepthByPoint(aListOfOffsetPosition[i][0], aListOfOffsetPosition[i][1]);
                int vDepth = GetDepthByPoint(aListOfOffsetPosition[i][2], aListOfOffsetPosition[i][3]);
                RFfeatureVector[i] = (uDepth - vDepth);
            }            
        }

        public void PredictOnePixelCPU(int oneDimensionIndex, short[] depthArray, ref double[] predictOutput)
        {
            GetFeatureVectorsFromOneDepthPoint(oneDimensionIndex, depthArray);
            dforest.dfprocess(decisionForest, RFfeatureVector, ref predictOutput);
            //Console.WriteLine("y[0]:{0}, y[1]:{1},y[2]:{2}", predictOutput[0], predictOutput[1], predictOutput[2]);
        }

        // use GPU to predict the whole using per-pixel classification
        public void PredictGPU(short[] depth, ref float[] predict_output) {
            myGPU_.Predict(depth, ref predict_output);
        }

        public void ConviencePredictFeatureVectorGPU(short[] feature_vector, ref float[] predict_output) 
        {
            myGPU_.PredictFeatureVector(feature_vector, ref predict_output);    
        }

        /* Use the CPU for feature extraction
         * GPU for prediction
         * Test purpose
         */
        public void PredictOnePixelGPUWithCPUFeatureExtraction(int one_dimension_index, short[] depthArray, ref float[] predictOutput)
        {
            GetFeatureVectorsFromOneDepthPoint(one_dimension_index, depthArray);
            for (int i = 0; i < RFfeatureVector.Length; i++)
                 RFfeatureVectorShort[i] = (short) (RFfeatureVector[i]);
            myGPU_.PredictFeatureVector(RFfeatureVectorShort, ref predictOutput);    
        }

        #region TestRegion
        public void AddVectorViaGPUTest(short[] input_array, ref short[] output_array)
        {
            myGPU_.AddVectorTest(input_array, ref output_array);
        }

        public bool IsVectorAddingWrong(short[] input_array, short[] output_array)
        { 
            int count= input_array.Length;
            // true: if something wrong.
            if (input_array[0] != output_array[0] - (short)(trees_int_[0]) || input_array[count - 1] != output_array[count - 1] - (short)(trees_int_[count - 1])) {
                Console.WriteLine("Somethign wrong. treesInt[0]:{0} Before[0]: {1}, After[0]: {2};", (short)(trees_int_[0]), input_array[0], output_array[0]);
                Console.WriteLine("Somethign wrong. treesInt[{0}]:{1} Before[{0}]: {2}, After[{0}]: {3};", count - 1, (short)(trees_int_[count - 1]), input_array[count - 1], output_array[count - 1]);
                return true;
            }
            return false;
        }
        #endregion

        private void ExtractFeatureFromOneDepthPointAndWriteToFile(int oneDimensionIndex)
            /*
             * Extract feature vectors and write it a file
             */
        {            
            List<int[]> aListOfOffsetPosition = new List<int[]>();

            GetAllTransformedPairs(oneDimensionIndex, depth[oneDimensionIndex], aListOfOffsetPosition);
            //Console.WriteLine("Feature vector: {0}", label[oneDimensionIndex]);
            output_filestream_.Write("{0}", label[oneDimensionIndex]);
            //Console.WriteLine("aListOfOffsetPosition.Count:{0}", aListOfOffsetPosition.Count);
            feature_vector_.Clear();

            for (int i = 0; i < aListOfOffsetPosition.Count; i++)
            {
                //int uX = aListOfOffsetPosition[i][0], uY = aListOfOffsetPosition[i][1];
                int uDepth = GetDepthByPoint(aListOfOffsetPosition[i][0], aListOfOffsetPosition[i][1]);
                int vDepth = GetDepthByPoint(aListOfOffsetPosition[i][2], aListOfOffsetPosition[i][3]);
                feature_vector_.Add(uDepth - vDepth);
                //Console.Write(" {0}", uDepth - vDepth);
                
            }            
            for (int i=0; i< feature_vector_.Count; i++)
                if (feature_vector_[i]!=0) // only write non-zero feature, to utilize sparcity
                    output_filestream_.Write(" {0}:{1}", i + 1, feature_vector_[i]); //notice a plus sign here
            output_filestream_.WriteLine();
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
                //Console.WriteLine("countTargetLabel:{0}, countDepthMinusOne:{1}, countBackgroundLabel:{2}, totalNumber:{3}", countTargetLabel, countDepthMinusOne, countBackgounrdLabel, width * height);
                file.Close();
            };

        }

        public void GenerateFeatureVectorViaImageFiles(string training_set_size)
        {
            Array values = Enum.GetValues(typeof(HandGestureFormat));
            output_filestream_ = new StreamWriter(String.Format("{0}\\{1}.{2}.features.txt", directory, traningFilename, training_set_size));         
            foreach (HandGestureFormat val in values) // go through each directory
            {
                //Console.WriteLine ("{0}: {1}", Enum.GetName(typeof(HandGestureFormat), val), val);
                if (val == HandGestureFormat.Background)
                    continue;

                string subdirectory = String.Format("{0}\\{1}.{2}", directory, val, training_set_size);

                Console.WriteLine("Current directory: {0}", subdirectory);
                string[] fileEntries = Directory.GetFiles(subdirectory);
                int tmpCounter = 0;
                int total = fileEntries.Length;
                foreach (string filePath in fileEntries) // go through each file within the subdirectory
                {
                    tmpCounter++;                    
                    string fileName = Path.GetFileName(filePath);
                    Console.WriteLine("{3}: {1}/{2} ({0})", 
                                        fileName, tmpCounter, total, val);
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

            for (int i = 0; i < Math.Min(numerPerClass, listOfBackgroundPosition.Count); i++)
                ExtractFeatureFromOneDepthPointAndWriteToFile(listOfBackgroundPosition[i]);

            for (int i = 0; i < Math.Min(numerPerClass, listOfTargetPosition.Count); i++)
                ExtractFeatureFromOneDepthPointAndWriteToFile(listOfTargetPosition[i]);
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
