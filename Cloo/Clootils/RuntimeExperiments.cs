#region License

/*

Copyright (c) 2009 - 2011 Fatjon Sakiqi

Permission is hereby granted, free of charge, to any person
obtaining a copy of this software and associated documentation
files (the "Software"), to deal in the Software without
restriction, including without limitation the rights to use,
copy, modify, merge, publish, distribute, sublicense, and/or sell
copies of the Software, and to permit persons to whom the
Software is furnished to do so, subject to the following
conditions:

The above copyright notice and this permission notice shall be
included in all copies or substantial portions of the Software.

THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND,
EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES
OF MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND
NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT
HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY,
WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING
FROM, OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR
OTHER DEALINGS IN THE SOFTWARE.

*/

#endregion

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Runtime.InteropServices;
using Cloo;

namespace Clootils
{
    class GPUvsCPURuntimeExperiemnt : IExample
    {
        ComputeProgram program;

        string clProgramSource = @"
kernel void RuntimeExperiment(
    global  read_only float* a,  
    global  read_only int *iterNum,
    global  write_only float* c)
{
    int index = get_global_id(0);    
    int i;
    for (i=0; i<*iterNum; i++)
        c[index] = a[index]  + i  ;
}
";

        public enum CPUModeFormat
        {
            None,
            Run,
        };

        public enum GPUModeFormat
        {
            None,
            Run,
        };

        public enum ExperiementModeFormat { 
            OneIterNum,
            MultiIterNum,
        };

        private CPUModeFormat CPUMode;
        private GPUModeFormat GPUMode;
        private ExperiementModeFormat ExperimentMode;

        public string Name
        {
            get { return "Get runtime statistics of GPU and CPU"; }
        }

        public string Description
        {
            get { return "Demonstrates how the advantage in GPU over CPU"; }
        }

        public void Run(ComputeContext context, TextWriter log)
        {
            CPUMode = CPUModeFormat.None;
            GPUMode = GPUModeFormat.Run;
            ExperimentMode = ExperiementModeFormat.OneIterNum;

            try
            {

                // Create the arrays and fill them with random data.
                int count = 640 * 480; // 
                int repeatTimes = 100;
                //const int maxIndex = 640 * 480;
                int[] myIterNum =new int[1]{ 30};
                float[] arrA = new float[count];
                float[] arrB = new float[count];
                float[] arrC = new float[count];

                

                Random rand = new Random();
                for (int i = 0; i < count; i++)
                {
                    arrA[i] = (float)(rand.NextDouble() * 100);
                    arrB[i] = (float)(rand.NextDouble() * 100);
                }


                // Create the input buffers and fill them with data from the arrays.
                // Access modifiers should match those in a kernel.
                // CopyHostPointer means the buffer should be filled with the data provided in the last argument.


                program = new ComputeProgram(context, clProgramSource);
                program.Build(null, null, null, IntPtr.Zero);

                ComputeBuffer<float> a = new ComputeBuffer<float>(context, ComputeMemoryFlags.ReadOnly | ComputeMemoryFlags.CopyHostPointer, arrA);
                //ComputeBuffer<float> b = new ComputeBuffer<float>(context, ComputeMemoryFlags.ReadOnly | ComputeMemoryFlags.CopyHostPointer, arrB);
                                
                // The output buffer doesn't need any data from the host. Only its size is specified (arrC.Length).
                
                ComputeBuffer<float> c = new ComputeBuffer<float>(context, ComputeMemoryFlags.WriteOnly, arrC.Length);
                

                // Create and build the opencl program.

                // Create the kernel function and set its arguments.
                ComputeKernel kernel = program.CreateKernel("RuntimeExperiment");
                ComputeCommandQueue commands = new ComputeCommandQueue(context, context.Devices[0], ComputeCommandQueueFlags.None);

                DateTime ExecutionStartTime; //Var will hold Execution Starting Time
                DateTime ExecutionStopTime;//Var will hold Execution Stopped Time
                TimeSpan ExecutionTime;//Var will count Total Execution Time-Our Main Hero
                
                List<int> ListIterNum = new List<int>();

                if (ExperimentMode == ExperiementModeFormat.MultiIterNum)
                {
                    for (int ii = 30; ii < 500; ii += 5)
                    {
                        ListIterNum.Add(ii);
                    }
                }
                else if (ExperimentMode == ExperiementModeFormat.OneIterNum) ;
                    ListIterNum.Add(300);
                Console.WriteLine("Start runing experiment");
                double perTaskTime;

                for (int tmp = 0; tmp < ListIterNum.Count; tmp++)
                {
                    myIterNum[0] = ListIterNum[tmp];

                    if (GPUMode == GPUModeFormat.Run)
                    {
                        Console.WriteLine("Current iter number {0}", myIterNum[0]);
                        ExecutionStartTime = DateTime.Now; //Gets the system Current date time expressed as local time
                        ComputeBuffer<int> iterNum = new ComputeBuffer<int>(context, ComputeMemoryFlags.ReadOnly | ComputeMemoryFlags.CopyHostPointer, myIterNum);



                        for (int repeatCounter = 0; repeatCounter < repeatTimes; repeatCounter++)
                        {
                            kernel.SetMemoryArgument(0, a);
                            //kernel.SetMemoryArgument(1, b);
                            kernel.SetMemoryArgument(1, iterNum);
                            //kernel.SetMemoryArgument(2, c);
                            kernel.SetMemoryArgument(2, c);
                            //kernel.SetMemoryArgument(3, 

                            // Create the event wait list. An event list is not really needed for this example but it is important to see how it works.
                            // Note that events (like everything else) consume OpenCL resources and creating a lot of them may slow down execution.
                            // For this reason their use should be avoided if possible.
                            //ComputeEventList eventList = new ComputeEventList();

                            // Create the command queue. This is used to control kernel execution and manage read/write/copy operations.


                            // Execute the kernel "count" times. After this call returns, "eventList" will contain an event associated with this command.
                            // If eventList == null or typeof(eventList) == ReadOnlyCollection<ComputeEventBase>, a new event will not be created.
                            //commands.Execute(kernel, null, new long[] { count }, null, eventList);
                            commands.Execute(kernel, null, new long[] { count }, null, null); // set the work-item size here.
                             
                            // Read back the results. If the command-queue has out-of-order execution enabled (default is off), ReadFromBuffer 
                            // will not execute until any previous events in eventList (in our case only eventList[0]) are marked as complete 
                            // by OpenCL. By default the command-queue will execute the commands in the same order as they are issued from the host.
                            // eventList will contain two events after this method returns.
                            //commands.ReadFromBuffer(c, ref arrC, false, eventList);
                            commands.ReadFromBuffer(c, ref arrC, false, null);

                            // A blocking "ReadFromBuffer" (if 3rd argument is true) will wait for itself and any previous commands
                            // in the command queue or eventList to finish execution. Otherwise an explicit wait for all the opencl commands 
                            // to finish has to be issued before "arrC" can be used. 
                            // This explicit synchronization can be achieved in two ways:

                            // 1) Wait for the events in the list to finish,
                            //eventList.Wait();

                            // 2) Or simply use
                            commands.Finish();
                        }
                        ExecutionStopTime = DateTime.Now;
                        ExecutionTime = ExecutionStopTime - ExecutionStartTime;
                        perTaskTime = ExecutionTime.TotalMilliseconds / repeatTimes;
                        log.WriteLine("Use {0} ms using GPU with iteration number {1}", perTaskTime, myIterNum[0]);
                        Console.WriteLine("Use {0} ms using GPU", perTaskTime);

                    }
                    // Do that using CPU
                    /*   ############################ */
                    if (CPUMode == CPUModeFormat.Run)
                    {
                        ExecutionStartTime = DateTime.Now; //Gets the system Current date time expressed as local time
                        for (int repeatCounter = 0; repeatCounter < repeatTimes; repeatCounter++)
                        {
                            for (int i = 0; i < count; i++)
                            {
                                //arrC[i] = arrA[i] + arrB[i];
                                int j;
                                for (j = 0; j < myIterNum[0]; j++)
                                    arrC[i] = arrA[i] + j;
                            }
                        }
                        ExecutionStopTime = DateTime.Now;
                        ExecutionTime = ExecutionStopTime - ExecutionStartTime;
                        perTaskTime = ExecutionTime.TotalMilliseconds / repeatTimes;
                        log.WriteLine("Use {0} ms using CPU  with iteration number {1}", perTaskTime, myIterNum[0]);
                        Console.WriteLine("Use {0} ms using CPU  with iteration number {1}", perTaskTime, myIterNum[0]);
                        
                    }

                }
                log.WriteLine("arrA[0]:{0}, arrC[0]:{1}, arrA[{2}]:{3}, arrC[{2}]:{4},", arrA[0], arrC[0], count-1, arrA[count-1], arrC[count-1]);
            }
            catch (Exception e)
            {
                log.WriteLine(e.ToString());
            }
        }
    }
}