using System;
using System.Collections.ObjectModel;
using System.Text;
using Cloo;

namespace MHArmory.Search.OpenCL
{
    class Host : IDisposable
    {
        private ComputeContext Context { get; }
        private ComputeProgram Program { get; }
        private const string KernelName = "search";

        public Host()
        {
            byte[] source = TrimUTFBOM(Properties.Resources.search);
            string sourceStr = Encoding.ASCII.GetString(source);

            // TODO: We need a proper platform / device selection mechanism.
            ComputePlatform platform = ComputePlatform.Platforms[2];
            var properties = new ComputeContextPropertyList(platform);
            Context = new ComputeContext(ComputeDeviceTypes.All, properties, null, IntPtr.Zero);
            Program = new ComputeProgram(Context, sourceStr);
            var optionsBuilder = new HostOptionsBuilder();
            optionsBuilder.AddDefine("MAX_RESULTS", SearchLimits.ResultCount);
            string options = optionsBuilder.ToString();
            Program.Build(null, options, null, IntPtr.Zero);
        }

        // For some reason when you embed a string to an assembly's resources, it adds a UTF BOM,
        // and the OpenCL compiler doesn't like that. So this is to remove it manually.
        private byte[] TrimUTFBOM(byte[] data)
        {
            byte[] bom = Encoding.UTF8.GetPreamble();
            if (data.Length < bom.Length)
            {
                return data;
            }

            for (int i = 0; i < bom.Length; i++)
            {
                if (bom[i] != data[i])
                {
                    return data;
                }
            }

            byte[] copy = new byte[data.Length - bom.Length];
            Buffer.BlockCopy(data, bom.Length, copy, 0, copy.Length);
            return copy;
        }

        public SerializedSearchResults Run(SerializedSearchParameters searchParameters)
        {
            ushort[] resultCount = new ushort[1];
            byte[] resultData = new byte[SearchLimits.ResultCount * (sizeof(ushort) * 6 + 1 + 3 * 7 * 3)];

            // Not that much data, better copy to device (CopyHostPointer) and back rather than query the host (UseHostPointer)
            var headerBuffer = new ComputeBuffer<byte>(Context, ComputeMemoryFlags.ReadOnly | ComputeMemoryFlags.CopyHostPointer, searchParameters.Header);
            var equipmentBuffer = new ComputeBuffer<byte>(Context, ComputeMemoryFlags.ReadOnly | ComputeMemoryFlags.CopyHostPointer, searchParameters.Equipment);
            var decoBuffer = new ComputeBuffer<byte>(Context, ComputeMemoryFlags.ReadOnly | ComputeMemoryFlags.CopyHostPointer, searchParameters.Decorations);
            var desiredSkillBuffer = new ComputeBuffer<byte>(Context, ComputeMemoryFlags.ReadOnly | ComputeMemoryFlags.CopyHostPointer, searchParameters.DesiredSkills);
            var resultCountBuffer = new ComputeBuffer<ushort>(Context, ComputeMemoryFlags.ReadWrite | ComputeMemoryFlags.CopyHostPointer, resultCount);
            var resultBuffer = new ComputeBuffer<byte>(Context, ComputeMemoryFlags.WriteOnly | ComputeMemoryFlags.CopyHostPointer, resultData);

            ComputeDevice device = Context.Devices[0]; // We need to run this on a single device, otherwise atomics don't work properly
            using (ComputeCommandQueue queue = new ComputeCommandQueue(Context, device, ComputeCommandQueueFlags.None))
            {
                using (ComputeKernel kernel = Program.CreateKernel(KernelName))
                {
                    kernel.SetMemoryArgument(0, headerBuffer);
                    kernel.SetMemoryArgument(1, equipmentBuffer);
                    kernel.SetMemoryArgument(2, decoBuffer);
                    kernel.SetMemoryArgument(3, desiredSkillBuffer);
                    kernel.SetMemoryArgument(4, resultCountBuffer);
                    kernel.SetMemoryArgument(5, resultBuffer);

                    long[] globalWorkSize = new long[] { searchParameters.Combinations };
                    queue.Execute(kernel, null, globalWorkSize, null, null);
                }
                queue.ReadFromBuffer(resultCountBuffer, ref resultCount, true, null);
                queue.ReadFromBuffer(resultBuffer, ref resultData, true, null);
                queue.Finish();
            }

            var result = new SerializedSearchResults();
            result.ResultCount = resultCount[0];
            result.Data = resultData;

            return result;
        }

        public void Dispose()
        {
            Program.Dispose();
            Context.Dispose();
        }
    }
}
