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
    class CompareGPUCPU : IExample
    {
        ComputeProgram program;

        string clProgramSource = @"
kernel void CompareGPUCPU(
    global  read_only float* a,    
    global write_only float* c )
{
    int index = get_global_id(0);    
    int i;
    for (i=0; i<330*5; i++)
        c[index] = a[index]  + i;
}
";

        public string Name
        {
            get { return "Compare GPU and CPU"; }
        }

        public string Description
        {
            get { return "Demonstrates how the advantage in GPU over CPU"; }
        }

        public void Run(ComputeContext context, TextWriter log)
        {
            try
            {
                // Create the arrays and fill them with random data.
                int count = 640*480; // 
                float[] arrA = new float[count];
                float[] arrB = new float[count];
                float[] arrC = new float[count];

                Random rand = new Random();
                for (int i = 0; i < count; i++)
                {
                    arrA[i] = (float)(rand.NextDouble() * 100);
                    arrB[i] = (float)(rand.NextDouble() * 100);
                }

                DateTime ExecutionStartTime; //Var will hold Execution Starting Time
                DateTime ExecutionStopTime;//Var will hold Execution Stopped Time
                TimeSpan ExecutionTime;//Var will count Total Execution Time-Our Main Hero
                ExecutionStartTime = DateTime.Now; //Gets the system Current date time expressed as local time
                // Create the input buffers and fill them with data from the arrays.
                // Access modifiers should match those in a kernel.
                // CopyHostPointer means the buffer should be filled with the data provided in the last argument.
                ComputeBuffer<float> a = new ComputeBuffer<float>(context, ComputeMemoryFlags.ReadOnly | ComputeMemoryFlags.CopyHostPointer, arrA);
                //ComputeBuffer<float> b = new ComputeBuffer<float>(context, ComputeMemoryFlags.ReadOnly | ComputeMemoryFlags.CopyHostPointer, arrB);

                // The output buffer doesn't need any data from the host. Only its size is specified (arrC.Length).
                ComputeBuffer<float> c = new ComputeBuffer<float>(context, ComputeMemoryFlags.WriteOnly, arrC.Length);

                // Create and build the opencl program.
                program = new ComputeProgram(context, clProgramSource);
                program.Build(null, null, null, IntPtr.Zero);

                // Create the kernel function and set its arguments.
                ComputeKernel kernel = program.CreateKernel("CompareGPUCPU");
                kernel.SetMemoryArgument(0, a);
                //kernel.SetMemoryArgument(1, b);
                //kernel.SetMemoryArgument(2, c);
                kernel.SetMemoryArgument(1, c);

                // Create the event wait list. An event list is not really needed for this example but it is important to see how it works.
                // Note that events (like everything else) consume OpenCL resources and creating a lot of them may slow down execution.
                // For this reason their use should be avoided if possible.
                //ComputeEventList eventList = new ComputeEventList();

                // Create the command queue. This is used to control kernel execution and manage read/write/copy operations.
                ComputeCommandQueue commands = new ComputeCommandQueue(context, context.Devices[0], ComputeCommandQueueFlags.None);

                // Execute the kernel "count" times. After this call returns, "eventList" will contain an event associated with this command.
                // If eventList == null or typeof(eventList) == ReadOnlyCollection<ComputeEventBase>, a new event will not be created.
                //commands.Execute(kernel, null, new long[] { count }, null, eventList);
                commands.Execute(kernel, null, new long[] { count }, null, null);

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

                ExecutionStopTime = DateTime.Now;
                ExecutionTime = ExecutionStopTime - ExecutionStartTime;
                log.WriteLine("Use {0} ms using GPU", ExecutionTime.TotalMilliseconds.ToString());
                // Print the results to a log/console.
                /*
                for (int i = 0; i < count; i++)
                    log.WriteLine("{0} + {1} = {2}", arrA[i], arrB[i], arrC[i]);
                 */ 
                // Do that using CPU
                ExecutionStartTime = DateTime.Now; //Gets the system Current date time expressed as local time
                for (int i = 0; i < count; i++)
                {
                    //arrC[i] = arrA[i] + arrB[i];
                    int j;
                    for (j = 0; j < 330*5; j++)
                        arrC[i] = arrA[i] + j;
                }
                ExecutionStopTime = DateTime.Now;
                ExecutionTime = ExecutionStopTime - ExecutionStartTime;
                log.WriteLine("Use {0} ms using CPU", ExecutionTime.TotalMilliseconds.ToString());
            }
            catch (Exception e)
            {
                log.WriteLine(e.ToString());
            }
        }
    }
}