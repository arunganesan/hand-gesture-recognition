using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Cloo;

namespace FeatureExtractionLib
{
    public class GPUCompute
    {
        private ComputeProgram program;
        private string clProgramSource = @"
kernel void dfprocess(
        global read_only short* meta_tree, 
        global read_only int* trees, 
        global read_only short* x,
        global write_only float* y)
{
        int index= get_global_id(0);    
}  
";
        private string clProgramSourceRD = @"
kernel void ReduceDepth(
    global  read_only short* a, 
    global  read_only int* trees,     
    global  write_only short* c)
{
    int index = get_global_id(0);    
    c[index] = a[index] + (short) (trees[index]);
}
";
        private ComputeKernel kernel;
        private ComputeContext context;
        private ComputeCommandQueue commands;

        private ComputeBuffer<short> a;
        private ComputeBuffer<short> c;
        private ComputeBuffer<int> trees;
        private ComputeBuffer<short> meta_tree;
        private ComputeBuffer<short> x; // feature vector
        private ComputeBuffer<double> y; // predict output

        private int count;

        public enum ComputeModeFormat { 
            ReduceDepth = 1,
            PredictWithFeatures = 2,
        };
        private ComputeModeFormat ComputeMode;
        public GPUCompute(ComputeModeFormat SetComputeMode = ComputeModeFormat.PredictWithFeatures) 
        // Constructor function
        {
            ComputePlatform platform = ComputePlatform.Platforms[0];
            ComputeContextPropertyList properties = new ComputeContextPropertyList(platform);
            IList<ComputeDevice> devices = new List<ComputeDevice>();
            devices.Add(platform.Devices[0]);
            Console.WriteLine("Platform name: {0}", platform.Devices[0].Name);
            context = new ComputeContext(devices, properties, null, IntPtr.Zero);
            ComputeMode = SetComputeMode;
            Console.WriteLine("Compute Mode: {0}", ComputeMode);            
            if (ComputeMode == ComputeModeFormat.ReduceDepth)
                program = new ComputeProgram(context, clProgramSourceRD);
            else if (ComputeMode == ComputeModeFormat.PredictWithFeatures)
                program = new ComputeProgram(context, clProgramSource);            
            program.Build(null, null, null, IntPtr.Zero); 
            // built the GPU program            
            Console.WriteLine("Build success");            
            count = 640 * 480;            
            if (ComputeMode == ComputeModeFormat.ReduceDepth)
            {
                kernel = program.CreateKernel("ReduceDepth");                
                commands = new ComputeCommandQueue(context, context.Devices[0], ComputeCommandQueueFlags.None);                
                a = new ComputeBuffer<short>(context, ComputeMemoryFlags.ReadOnly, count);
                c = new ComputeBuffer<short>(context, ComputeMemoryFlags.WriteOnly, count);
                kernel.SetMemoryArgument(0, a);
                kernel.SetMemoryArgument(2, c);
            }
            else if (ComputeMode == ComputeModeFormat.PredictWithFeatures) {
                kernel = program.CreateKernel("dfprocess");
                Console.WriteLine("Sucessfully create kernel");
                commands = new ComputeCommandQueue(context, context.Devices[0], ComputeCommandQueueFlags.None);
                
                //x = new ComputeBuffer<short>(context, ComputeMemoryFlags.ReadOnly, feature_length);
                
                
                
            }
        }

        public void LoadTrees(int[] toLoadTrees, short nclasses=0, short ntrees=0, int nfeatures=0) {
            /*
            trees = new ComputeBuffer<int>(context, ComputeMemoryFlags.ReadOnly, toLoadTrees.Length);
            kernel.SetMemoryArgument(1, trees);
            commands.WriteToBuffer(toLoadTrees, trees, true, null);
             */
            if (ComputeMode == ComputeModeFormat.PredictWithFeatures)
            {
                short[] host_meta_tree = new short[2];
                host_meta_tree[0] = nclasses;
                host_meta_tree[1] = ntrees;
                
                meta_tree = new ComputeBuffer<short>(context, ComputeMemoryFlags.ReadOnly | ComputeMemoryFlags.CopyHostPointer, host_meta_tree);
                kernel.SetMemoryArgument(0, meta_tree);
                trees = new ComputeBuffer<int>(context, ComputeMemoryFlags.ReadOnly, toLoadTrees.Length);
                kernel.SetMemoryArgument(1, trees);
                commands.WriteToBuffer(toLoadTrees, trees, true, null);
                x = new ComputeBuffer<short>(context, ComputeMemoryFlags.ReadOnly, nfeatures);
                kernel.SetMemoryArgument(2, x);
                y = new ComputeBuffer<double>(context, ComputeMemoryFlags.WriteOnly, nclasses);
                kernel.SetMemoryArgument(3, y);
                
            }
        }

        public void AddDepthPerPixel( short [] BeforeDepth, short [] AfterDepth)
        {
            commands.WriteToBuffer(BeforeDepth, a, true, null);            
            commands.Execute(kernel, null, new long[] { BeforeDepth.Length }, null, null); // set the work-item size here.
            commands.Finish();
            commands.ReadFromBuffer(c, ref AfterDepth, true, null);
            
        }
    }
}
