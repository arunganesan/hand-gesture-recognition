using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using FeatureExtractionLib;

namespace TestModuleNamespace
{
    class TestModule
    {
        private FeatureExtraction Feature;        
        dforest.decisionforest decisionForest = new dforest.decisionforest();
        private int[] treesInt; // random forest in int type. (faster in GPU)
        private GPUCompute myGPU;
        #region oneFeatureVecor
        private double[] X = new double[2000] { 9402, -1041, 23, 1357, -248, 8228, 1044, 9406, -1824, 313, -9285, -8, -9164, -954, 9328, -1138, -3, 239, 47, 254, 973, -11, 9418, -9434, 190, 482, -2007, -1131, -851, 1082, -8378, -777, 9341, -210, 9407, 247, -8471, -9421, -258, 860, 9, -9410, -135, -939, -249, -8255, -9419, 1429, -245, -8281, -226, -1201, -1004, 1080, -9419, 108, 1851, 1066, -1374, -96, 930, 9411, -112, 858, -7477, 0, 910, -8, -1504, 1058, 9417, 176, -17, -70, 1060, 9419, 7583, 0, -8355, 0, -1111, 9408, -8485, 1053, 22, -92, 255, -9416, 9141, 4, -59, -953, 67, 12, 37, 1175, -9288, -130, 213, -1192, 0, 0, 0, 0, 8091, -1, 255, -10, 39, -989, 969, -46, 263, -837, 9170, 1841, 862, -116, 9416, -1029, 1106, 957, 141, -1130, -1465, -9418, -46, 289, 661, 175, 42, -9401, 987, 1008, -1283, 1009, -905, 76, 9419, -253, 41, -246, 23, -222, -1195, 242, -9428, -866, -69, -245, -63, 926, -274, 231, 9409, -9419, 255, -20, 993, 151, -1101, -1004, -9414, 255, 9429, -235, -251, -9357, 9176, -1120, -9427, 81, -8, -1885, -243, 772, 8400, -827, -9430, -23, -133, 1900, 231, -1162, 957, 1, -3, -2, 898, -233, 7, 100, -260, -9304, -8400, 193, 0, -8437, 8422, -248, -236, 764, 1, 18, -246, -110, -965, -322, -67, 8264, -8298, 9176, 1172, 1510, -1136, 221, 550, -1152, -35, 949, 806, 9291, -31, 230, -735, 9156, -238, -123, -1345, 10, -111, 821, 0, 181, 1453, 1617, -1364, -9422, -28, -21, -1173, 149, -1006, 1146, 6, -820, -9182, 882, -9425, 43, -908, -787, -8378, 137, -1179, -1472, 9176, 9, 8, -873, -186, -9426, 964, -9424, 0, 982, -64, -8272, 9423, 9176, 9425, -8370, -881, 9172, -226, 9399, -9178, 9381, 210, -9410, -136, 901, -4, 6, -22, -7955, 72, 23, 1408, 937, -2, 46, -128, 2, 128, 248, 0, -909, 1060, 9410, 9420, 8, 139, -9176, 9178, -199, -956, 896, 0, 75, -232, -9421, 5, 16, -1010, -258, 919, 35, -8002, 782, 113, 9421, -9419, 1139, -8246, 9170, -902, -13, -8370, -1840, 277, -211, 245, 9327, -9400, 2, -9401, -47, 1641, -38, 1257, 1815, 9322, -263, 2, 241, -213, -265, 1010, -1016, -317, -9417, -3, 1, 1019, 949, 251, -264, -1, -4, -1114, -8237, -116, 4, -2, -6, 205, -8281, 9178, -159, -34, 1405, -9411, -827, 117, 94, 1026, 7549, -9414, -242, -33, -4, -261, 202, -262, -9404, -9419, 923, -3, -948, -1973, 2100, 893, -259, 922, -18, -221, 1856, 256, 1099, 6, -773, 3, -1217, 188, 991, 9420, -8, 1, -253, 9195, -1693, 16, 7566, -94, -18, 2, 9288, -980, 9172, -9426, 5, 8355, -1865, -9406, -9396, 71, 9166, 640, -15, 138, -171, -14, -52, 714, 31, 8422, 51, -2, -2034, 9160, 0, -61, -76, 851, -1073, -239, 733, 253, -1103, -298, -1724, 9305, 1135, 242, 237, 1001, -10, -955, 1022, -246, 1149, 727, 9420, 964, -1291, 1379, -1816, 8498, 10, -214, 861, 171, -2120, 1871, -9421, 813, 11, -23, 120, 925, -1343, -29, 9418, -1123, 1209, -1142, 902, 286, -1, -238, 0, -1121, 0, -8306, 240, -9416, 2022, 981, 8, 1217, -1093, -5, 1012, 7, 0, 8517, -113, 237, 130, -9182, -20, -9316, 1130, 6, 9418, -660, 687, 20, 9429, -1042, -9415, 1591, -9324, -341, 24, -9357, -150, 12, 1001, -1096, 253, 238, 241, 0, 1887, -1068, -835, -7341, -80, 13, 1173, -1, 9, 1823, 1018, -9412, -1340, -9419, 47, 2026, -1066, -166, 11, -236, -9178, -284, -10, 9168, -26, 77, -1432, 893, -887, 8289, 1137, -1420, -9294, 233, 11, 9168, -8362, 1030, -9412, -811, -839, -831, -821, 10, 9412, -8, -14, 244, -8378, 238, 536, 8219, 5, 244, 925, -1869, 258, -9422, 165, -4, -9409, 54, -261, 0, -10, -9170, 9421, 875, 8429, 810, 868, -33, -122, 232, -53, 1073, 783, 96, -233, -1057, 8289, 1020, -235, -951, 8, -1217, 1972, 107, -107, -229, -282, -792, 1834, -21, 333, 221, 247, -1098, -1062, -7420, 5, -81, 7, -1, -19, 229, -1015, -1031, -8314, 207, -10, -8246, 7381, 666, -9331, 9415, -1, -1249, 1799, 9365, 940, -164, 76, 238, 827, -167, -257, -8530, 33, -261, -147, -12, -9428, -249, 240, -9409, -276, 1058, 75, -263, 8, 0, 9176, -31, -115, -243, -8347, 4, -55, 259, -862, 1048, 243, 929, -3, -815, -900, 942, 790, 8, -247, -9420, 99, -792, 239, 1478, 848, -4, 0, -976, 8323, 33, -16, 1208, 1462, -825, 0, 9162, 9403, -7, -9407, 0, 81, 246, 100, -2183, -233, 9414, -8347, 1107, 8536, 1157, 205, -2, -250, 784, -248, 218, 250, -9324, -9406, -9191, 710, -252, -9431, 8542, -33, -40, -133, 1777, -9417, 9406, 6, -249, -1060, 9162, -8437, 9424, -11, 4, 912, 1618, 1213, 4, -7401, 71, -79, 658, -153, -8498, 9419, -9170, 155, -1997, -1153, -12, -230, -9176, 26, -9418, 0, 205, 33, 110, -9315, 226, 843, -10, 1027, 6, 233, 189, -81, 0, -4, 0, -938, -689, -928, 950, 27, 106, -2112, 9415, -1016, -121, -266, -881, 0, 243, -131, -9166, -1965, -9421, -908, 202, -110, 125, -4, -9425, -5, -961, -2, 8080, 8, 0, -250, 139, 1249, 879, -1105, 208, 843, 148, 243, 2209, -46, -244, -223, 9410, -20, 7566, -8298, 7381, 11, -9289, -17, -245, -7320, -260, 8314, -325, 1074, -780, 8298, 1848, -1866, 938, 9330, -5, 356, -18, 172, -100, -9411, 215, -1044, -9424, 9422, 90, 257, 135, -2059, -9, -13, -1834, 193, -1063, -9176, -1990, 0, -16, 850, 1111, -101, 9162, 343, 8298, -143, -252, -678, 8298, -105, 9429, -9395, -1008, -1205, -762, 540, 9422, 2179, -6, -25, -201, 66, 9168, 65, 902, -242, 249, 1028, -1076, 9154, -9308, -4, -1396, 1015, 259, -4, -1183, 9433, -29, 1020, -9425, 53, 993, 9141, -9174, -247, -9172, -50, 9365, 7955, 7566, -4, -1108, 9412, -1153, 0, -5, 25, -215, 119, 957, 277, 9414, 0, 71, 1138, 249, -48, -6, 77, -3, 198, 9420, -9416, 235, -12, 9419, 7, -158, 162, 9176, 9419, -860, -244, 1049, 232, -101, 50, -9395, 243, 1712, -1961, -1060, -725, -1556, -17, 1147, -13, -23, -892, 752, 683, 8323, -9414, 21, -9429, 249, -9427, -1410, 0, 4, 1408, -978, -9411, -1019, -288, -23, 9093, -9420, -9402, 0, -9304, -1016, -117, -935, -253, 9184, 7549, 1124, -1204, -1118, 702, -1994, 9166, 9419, 164, 9308, 65, 9427, -11, 111, 3, -243, 955, 9420, 134, -1085, 219, 1020, 220, 59, -941, 248, 9202, -2117, -9156, -9137, 15, 1854, -1062, -24, 7440, 171, 1140, 1806, -237, 8415, -90, 814, 108, -8298, 230, -9392, -9413, -996, 237, 230, -244, 1040, 1178, 2167, 1718, 477, -2, 90, 253, 8272, -2114, 172, -234, -1081, 41, -8281, -1021, -820, 8415, -49, -4, -9414, -241, -1174, 752, 9166, -1, 760, 245, -14, -256, 782, 9132, -8536, 960, 1044, -9378, 1143, -963, 20, -1134, 795, 9191, -54, 1052, -9422, 154, 902, -2, 1067, 3, -1617, -10, -9168, -94, 87, -1913, 296, 1145, 1218, -255, -221, 663, 7943, -3, -1846, -63, -34, -7401, 237, -33, 259, 894, -18, -101, 119, -194, 111, 1109, 9361, 5, -129, -372, -810, -9180, 254, -21, 9081, -41, -943, 817, -1821, -1137, 141, 8228, 126, 891, -1341, -251, 245, -48, -12, 105, 9421, -7531, 8, 110, -253, 2, 9419, 1327, -253, -866, 1402, -78, -158, -1258, -10, 8370, -1081, 240, -9422, 230, -1998, 327, -253, -9295, -1095, 248, 4, 680, -149, 9295, 0, 9170, 7341, -239, 9403, -9415, -9428, -7257, 9300, 9176, 95, -1376, 190, -9170, -25, 134, 1847, 958, 9366, 164, -9315, 0, 179, -1104, 23, 1962, 1188, 53, 0, 8237, -1849, -207, -9174, -841, -1802, 18, 0, 279, 9121, -672, -893, -3, 9382, -150, 49, 9412, -9421, 9326, -8393, 9128, -9172, 9418, 9422, 30, 855, 9315, -1021, 38, -243, 157, 8370, -17, -1477, 763, -283, -5, 687, -1807, -666, -1195, -9406, 2, -1058, -9, -793, -835, -1009, 189, -1573, -109, -9164, 19, 427, 9421, -8347, 8422, -204, 1791, 9419, 101, 762, -1827, -257, -232, 245, -1117, -12, 8, 995, 1426, 8536, 8272, 243, 99, -1844, -9413, 164, 875, -1748, -1010, 9168, 9421, 2031, 1212, -9397, 1098, -220, 372, 43, 1015, 9379, -1022, 6, -12, 9156, -9174, -844, -1864, -94, -1521, 265, -760, 9180, -10, 178, -10, -112, 748, -7, 18, 4, -16, -251, -1143, 2, 100, -234, 1855, -9419, -7, -8400, -1871, 1902, -248, 239, -27, -814, -232, 89, -228, 249, -9182, 141, 1076, -260, -1036, 1570, -9304, -50, 0, 8, 1217, 0, -999, 822, -225, -216, -9419, -188, 0, -10, 17, -9170, -851, -1074, 9416, -4, 935, 8237, -11, -85, -1070, -1156, -130, 766, 132, -9401, 0, 9424, -4, -5, 9402, -1146, 203, -943, 1952, -284, 9141, -69, 685, -887, -1166, -247, -384, 0, -6, -9414, 9309, -172, 0, 1575, 1070, 0, 9312, -6, -245, -254, 1170, 153, 1, -102, 1623, 94, 916, -1819, 1050, -238, -121, -76, 4, -8554, -241, 9315, -366, -148, 9166, -51, -1297, -42, -910, -216, 87, -9166, 262, 9319, -9160, -1448, -9419, -98, -9380, -1216, -234, 1195, -264, -8437, -249, 118, -9379, -1188, -1229, -158, 9347, 1044, 9416, -79, 210, -908, 1068, -9176, 4, -234, 1712, 1126, 11, -9413, 750, -9162, 882, -11, -255, 854, 9170, 1351, 21, 9189, -1113, -254, -1089, 922, -814, 0, 791, 906, -54, -9423, 804, 4, 52, -145, -3, -1201, -9174, 3, -6, -1873, 9410, 0, 1322, 928, -252, 9366, -253, -170, 683, -9366, 996, -2118, -1092, -1840, 1159, 0, -11, 17, 0, 244, 1885, -9308, -80, -1074, 1132, 9409, -9304, -8339, -7257, -56, -833, -1110, 1164, -8542, -107, -9154, 9418, -237, 72, 1050, 9314, 803, -1027, -41, 0, -9417, -235, 246, -9162, 1108, -218, 0, 9323, 9407, -1161, 1106, 248, -2080, 9319, 685, 129, -32, 91, -127, -9170, 1, 1, -114, -129, 240, 915, 9191, 79, -235, 9418, 9427, 0, 254, -1699, -9397, -9126, -79, 1160, 1342, -9410, 9328, 1788, 2007, 0, -9170, 9160, -6, 17, -1848, -1041, 228, -1117, 8, -10, 9422, 1130, -9427, -911, -334, 236, 221, 9156, -1120, -932, -29, 952, -52, -2075, 71, 965, -6, 841, -9178, 9420, 230, 9415, -7458, 9420, 2, 243, -235, 11, -1995, -2092, 2075, 184, 981, 41, 6, 8, 9308, -80, 981, -1202, -1595, -244, -9362, -876, 9429, 8255, 923, 9407, 122, -896, 65, -2124, -91, 9301, -113, -9316, -7, -952, -107, -8471, 2, 0, 8530, -129, 1, 117, -1789, -9422, -775, 1109, -9426, 255, 1711, 841, -252, 96, 995, 1, 250, 0, -1031, -1804, -8491, -9404, 0, 961, -9362, 943, 224, -36, -1185, -8370, -8339, -965, -1079, -8255, -9415, -10, 8264, -1164, -95, 11, -30, 1583, -234, -1313, 69, -15, -791, 8132, 8091, -1115, -9423, -264, -9204, -786, 0, 9428, -1106, 6, -4, -13, -1370, -357, 9182, 0, 1033, 41, 916, 77, -107, -88, 1, 26, -223, -121, -9405, -9412, 9128, 5, 947, -54, 42, 1034, -90, 7361, -970, 7, 12, -1205, -9319, 4, 13, 86, 344, 166, 8451, 247, -1, 1860, 94, -8478, 211, 132, 53, -234, 8422, 9176, 137, -28, -330, 1892, -920, -947, 0, 5, 9409, 1098, -1146, 0, 17, 9417, 8264, 933, 1130, 4, -1157, 23, -106, 245, -9406, 9413, 9316, 1612, -9406, -8306, -16, 1059, 0, -137, 217, -178, -7566, 21, 1880, -1086, 21, 9336, -9288, 75, -9170, 2, 914, -229, 8264, 1156, -9170, 9054, 171, 18, 1045, 39, -249, -8237, 225, -319, -9170, 1056, 128, -1037, -9295, 9176, 12, 257, 1037, -1838, -9309, 9408, 1125, 1011, -825, 235, -8, -130, -9415, -249, 72, 798, 6, 25, 120, 240, -99, -9407, -270, 1106, 9164, 9224, -9410, -331, -9408, 927, 8485, 243, -7918, 9176, -8, 917, -4, -9430, -1845, 9176, -1147, 0, 9168, -888, 1040, -229, 0, 21, 22, 1237, -8306, -252, -247, 243, -9418, 980, 8504, -188, -846, 5, 1083, -1001, 9413, -226, 9409, -8191, -29, 10, 7401, 64, 248, 9316, 9419, -9411, -8, -3, 8059, -9176, -971, 9141, 148, 748, 0, 233, -241, 18, -8314, 245, -136, 9417, -46, 804, 21, -2055, 762, 3, 0, 9422, -630, 1377, 8255, -38, 4, 229, 1174, 1118, -234, 39, 5, -983, 1187, -11, 11, -19, -2163, -163, 870, 9351, 9174, 1188, 11, 9185, 92, -1069, 8255, -880, 1123, 1119, 2, 0, -1126, 205, -1558, -1122, -8370, 10, 6, 41, 95, 892, 1172, 167, 96, 9421, -1408, 9176, -336, 9336, -13, -239, 9421, -1429, 1084, -125, 196, -9168, 248, -244, -8485, -135, -1618, 245, 8, 209, 12, -911 };
        #endregion
                    
        //private FeatureExtractionLib.GPU myGPU;
        /*
        private int minOffset;
        private int maxOffset;
        private int NumofOffset;
        */

        static void Main(string[] args)
        {
            Console.WriteLine(
                "Hello World");

            TestModule FeatureExtractionTest = new TestModule();

            FeatureExtractionTest.SetupFeatureExtraction();
            // Test Random Forest
            // ##################
            FeatureExtractionTest.testDecisionForest();

            // ################
            // Generate feature vector file             
            //############################            
            /* 
                FeatureExtractionTest.TestGenerateFeatures();                        
                Console.WriteLine("Generated features.");
             */ 
            // ##########################

            // Test simple case for GPU
            /* #################### */
            //
            //FeatureExtractionTest.TestReduceDepthViaGPU();

            /* ###################### */          

            // Test loading trained random forest to GPU
            // ############################
            //FeatureExtractionTest.LoadTrainedRFModelToGPU();
            // ############################
            Console.ReadKey();

        }

        public TestModule() { 
            //myGPU = new GPUCompute();
        }

        private void TraverseTree(double[] tree, int index, int off, int [] treeInt) {
            if ((double)(tree[index]) != (double)(-1))
            {
                if (tree[index + 1] > 32767)
                {
                    Console.WriteLine("Some thing not good! Feature: {0}, Threshold: {1}", tree[index], tree[index + 1]);
                }
                treeInt[index] = (int)tree[index];
                treeInt[index + 1] = (int)Math.Ceiling(tree[index + 1]);
                treeInt[index + 2] = (int)tree[index + 2];
                TraverseTree(tree, index + 3, off, treeInt);
                TraverseTree(tree, off + (int)(tree[index + 2]), off, treeInt);
            }
            else {
                treeInt[index] = (int)tree[index];
                treeInt[index + 1] = (int)tree[index + 1];
            }

        }

        public void LoadTrainedRFModelToGPU() {
            LoadRFModel();
            Console.WriteLine("Total tree size: {0}", decisionForest.trees.Length);            
            int treeSize = (int)(decisionForest.trees.Length/3);
            Console.WriteLine("single tree size:{0}", treeSize);             
            Console.WriteLine("Number of variable: {0}", decisionForest.nvars);
            Console.WriteLine("ntress: {0}", decisionForest.ntrees);
            Console.WriteLine("nclasses: {0}", decisionForest.nclasses);
             
            // display a very few of the tree
            /* ######################
            for (int i=0; i<10; i++)
                Console.WriteLine("trees[{0}]: {1}", i, decisionForest.trees[i]);
            Console.WriteLine("Second tree size: trees[trees[0]]: {0}", decisionForest.trees[(int) (decisionForest.trees[0])]);
            // ######################
            */ 
            
            // test round...
            /* ###################
            Console.WriteLine("round(1.5):{0}", Math.Round(1.5));
            Console.WriteLine("round(-1.5):{0}", Math.Round(-1.5));
            Console.WriteLine("Celling(1.5):{0}", Math.Ceiling(1.5));
            Console.WriteLine("Ceiling(-1.5):{0}", Math.Ceiling(-1.5));
            */
            // ###################
            
            // turn the trees from double to int by using ceiling.
            int off = 0;
            treesInt = new int[decisionForest.trees.Length];
            for (int i = 0; i < decisionForest.ntrees; i++)
            {
                treesInt[off] = (int) (decisionForest.trees[off]);
                Console.WriteLine("Tree {0}", i + 1);
                TraverseTree(decisionForest.trees, off+1, off, treesInt);
                off = off +   (int) (decisionForest.trees[off]) ;
            }
            Console.WriteLine("Finish going through all threshold");
            // the above isn't necessary. One can just scan the tree array...

            // test if tressInt and decisionForest.trees are the same
            Random _r= new Random();
            for (int i = 0; i < decisionForest.trees.Length; i++) { 
                //int index= _r.Next(decisionForest.trees.Length);
                if ( Math.Ceiling(decisionForest.trees[i]) != treesInt[i]  )
                    //Console.WriteLine("trees[{0}]:{1}, treesInt[{0}]:{2}", index, decisionForest.trees[index], treesInt[index]);
                    Console.WriteLine("Something wrong! trees[{0}]:{1}, treesInt[{0}]:{2}", i, decisionForest.trees[i], treesInt[i]);
            }

            myGPU.LoadTrees(treesInt);
            Console.WriteLine("Successfuly load the tree to GPU");
            // tree format
            /*
             *   trees[0]: total size.
             *   trees[K+0]       -   variable number        (-1 for leaf mode, K starts from 1)
             *   trees[K+1]       -   threshold              (class/value for leaf node)
             *   trees[K+2]       -   ">=" branch index      (absent for leaf node)
             * 
             */

            /*
            index = 2;
            while (true)
            {
                
                if (decisionForest.trees[index] > 32767) 
                    Console.WriteLine("Out of short bound, trees[{0}]: {1}, variable: {2}", index, decisionForest.trees[index], decisionForest.trees[index-1]);
                index += 3;
                if (index >= treeSize)
                    break;
            }
             */ 
            
        }

        public void TestReduceDepthViaGPU()
        {
            int count = 640 * 480;
            short[] BeforeDepth = new short[count];
            short[] AfterDepth = new short[count];
            
            LoadTrainedRFModelToGPU();            
            const int maxTmp = 1000;
            DateTime ExecutionStartTime; //Var will hold Execution Starting Time
            DateTime ExecutionStopTime;//Var will hold Execution Stopped Time
            TimeSpan ExecutionTime;//Var will count Total Execution Time-Our Main Hero
            ExecutionStartTime = DateTime.Now; //Gets the system Current date time expressed as local time

            /*
            for (int i = 0; i < count; i++)
                Trees[i] = (short)(i % 4445);
            */
            //myGPU.LoadTrees(Trees);

            bool testSuccess = true;
            for (int tmp = 0; tmp < maxTmp; tmp++)
            {
                for (int i = 0; i < count; i++)
                    BeforeDepth[i] = (short)((i + tmp) % 256);
                myGPU.AddDepthPerPixel(BeforeDepth, AfterDepth);
                //Console.WriteLine("Before[0]: {0}, Before[{1}]: {2}; After[0]: {3}, After[{1}]: {4}", BeforeDepth[0], count, BeforeDepth[count-1], AfterDepth[0], AfterDepth[count-1]);
                if (BeforeDepth[0] != AfterDepth[0]-(short) (treesInt[0]) || BeforeDepth[count - 1] != AfterDepth[count - 1] - (short) (treesInt[count-1]))
                {
                    //Console.WriteLine("Something went wrong. Before[0]: {0}, Before[{1}]: {2}; After[0]: {3}, After[{1}]: {4}", BeforeDepth[0], count, BeforeDepth[count - 1], AfterDepth[0], AfterDepth[count - 1]);
                    Console.WriteLine("Somethign wrong. treesInt[0]:{0} Before[0]: {1}, After[0]: {2};", (short) (treesInt[0]), BeforeDepth[0], AfterDepth[0]);
                    Console.WriteLine("Somethign wrong. treesInt[{0}]:{1} Before[{0}]: {2}, After[{0}]: {3};", count-1, (short) (treesInt[count-1]), BeforeDepth[count-1], AfterDepth[count-1]);
                    testSuccess = false;
                }
            }

            ExecutionStopTime = DateTime.Now;
            ExecutionTime = ExecutionStopTime - ExecutionStartTime;
            double perTaskTime = ExecutionTime.TotalMilliseconds / maxTmp;            
            Console.WriteLine("Use {0} ms using GPU", perTaskTime);
            if (testSuccess)
                Console.WriteLine(" Success test add depth using the tree!");
            
        }

        private void LoadRFModel() {
            string modelFilePath = Feature.directory + "\\FeatureVectureBlue149.rf.model";
            Console.WriteLine("Model file path {0}", modelFilePath);
            string modelFile = System.IO.File.ReadAllText(modelFilePath);            
            alglib.serializer Serializer = new alglib.serializer();
            Serializer.ustart_str(modelFile);
            dforest.dfunserialize(Serializer, decisionForest);
            Serializer.stop();
            Console.WriteLine("Finish loading the RF model");
        }

        public void testDecisionForest()
        {
            // Use CPU first
            LoadRFModel();            
            double[] y=new double[3];
            dforest.dfprocess(decisionForest, X,  ref y);
            Console.WriteLine("Use CPU background: {0}, open hand: {1}, close hand: {2}",y[0], y[1], y[2]);
            // Then use GPU
            float[] predict_output = new float[3];
            short[] X_short = new short[X.Length];
            for (int i = 0; i < X.Length; i++)
                X_short[i] = (short)X[i];
            Feature.ConviencePredictFeatureVectorGPU(X_short, ref predict_output);
            Console.WriteLine("Use GPU background: {0}, open hand: {1}, close hand: {2}", predict_output[0], predict_output[1], predict_output[2]);
        }

        public void SetupFeatureExtraction() {
            //Default direcotry: "..\\..\\..\\Data";
            // To setup the mode, see README in the library
            //FeatureExtraction.ModeFormat MyMode = FeatureExtraction.ModeFormat.BlueDefault;
            FeatureExtraction.ModeFormat MyMode = FeatureExtraction.ModeFormat.Blue;
            //Feature = new FeatureExtraction(MyMode, "D:\\gr\\training\\blue\\");
            Feature = new FeatureExtraction(MyMode);
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
