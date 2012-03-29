﻿using System;
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
kernel void ReduceDepth(
    global  read_only short* a,      
    global  write_only short* c)
{
    int index = get_global_id(0);    
    c[index] = a[index] - 1;
}
";
        private ComputeKernel kernel;
        private ComputeContext context;
        private ComputeCommandQueue commands;

        private ComputeBuffer<short> a;
        private ComputeBuffer<short> c;
        private int count;
        public GPUCompute() 
        // Constructor function
        {
            ComputePlatform platform = ComputePlatform.Platforms[0];
            ComputeContextPropertyList properties = new ComputeContextPropertyList(platform);
            IList<ComputeDevice> devices = new List<ComputeDevice>();
            devices.Add(platform.Devices[0]);
            Console.WriteLine("Platform name: {0}", platform.Devices[0].Name);
            context = new ComputeContext(devices, properties, null, IntPtr.Zero);
            program = new ComputeProgram(context, clProgramSource);
            program.Build(null, null, null, IntPtr.Zero); 
            // built the GPU program            
            Console.WriteLine("Build success");
            kernel = program.CreateKernel("ReduceDepth");
            commands = new ComputeCommandQueue(context, context.Devices[0], ComputeCommandQueueFlags.None);

            count = 640 * 480;
            a = new ComputeBuffer<short>(context, ComputeMemoryFlags.ReadOnly, count);
            c = new ComputeBuffer<short>(context, ComputeMemoryFlags.WriteOnly, count);            
        }

        public void AddDepthPerPixel( short [] BeforeDepth, short [] AfterDepth)
        {
            commands.WriteToBuffer(BeforeDepth, a, true, null);
            kernel.SetMemoryArgument(0, a);                                                        
            kernel.SetMemoryArgument(1, c);
            commands.Execute(kernel, null, new long[] { BeforeDepth.Length }, null, null); // set the work-item size here.
            commands.ReadFromBuffer(c, ref AfterDepth, true, null);
            //commands.Finish();

            
        }
    }
}