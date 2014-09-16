using ManagedCuda;
using ManagedCuda.BasicTypes;
using ManagedCuda.VectorTypes;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace VoxelTerrain
{
    public class CUDAPrefixScan : IDisposable
    {
        private const int maxBatchElements = 67108864;

        private const int threadBlockSize = 256;

        private const int minShortArraySize = 4;

        private const int maxShortArraySize = 4 * threadBlockSize;

        private const int minLargeArraySize = 8 * threadBlockSize;

        private const int maxLargeArraySize = 4 * threadBlockSize * threadBlockSize;

        private CudaKernel kernelScanExclusiveShared;

        private CudaKernel kernelScanExclusiveShared2;

        private CudaKernel kernelUniformUpdate;

        private CudaContext context;

        private bool isDisposed = false;

        public CUDAPrefixScan(CUmodule module, CudaContext context)
        {
            this.context = context;
            kernelScanExclusiveShared = new CudaKernel("scanExclusiveShared", module, context);
            kernelScanExclusiveShared2 = new CudaKernel("scanExclusiveShared2", module, context);
            kernelUniformUpdate = new CudaKernel("uniformUpdate", module, context);
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!isDisposed)
            {
                if (disposing)
                {

                }
            }

            isDisposed = true;
        }

        ~CUDAPrefixScan()
        {
            Dispose(false);
        }

        public CudaDeviceVariable<T> PrefixSumArray<T>(CudaDeviceVariable<T> input, int n) where T : struct
        {
            int arrayLength = n;
            int batchSize = n / arrayLength;

            if (!IsPowerOfTwo(n))
                throw new Exception("Input array length is not power of two.");

            CudaDeviceVariable<T> output = new CudaDeviceVariable<T>(n);
            CudaDeviceVariable<T> buffer = new CudaDeviceVariable<T>(n);

            kernelScanExclusiveShared.BlockDimensions = threadBlockSize;
            kernelScanExclusiveShared.GridDimensions = (batchSize * arrayLength) / (4 * threadBlockSize);
            kernelScanExclusiveShared.Run(output.DevicePointer, input.DevicePointer, 4 * threadBlockSize);

            kernelScanExclusiveShared2.BlockDimensions = threadBlockSize;
            kernelScanExclusiveShared2.GridDimensions = (int)Math.Ceiling(((batchSize * arrayLength) / (4 * threadBlockSize)) / (double)threadBlockSize);
            kernelScanExclusiveShared2.Run(buffer.DevicePointer, output.DevicePointer, input.DevicePointer, (batchSize * arrayLength) / (4 * threadBlockSize), arrayLength / (4 * threadBlockSize));

            kernelUniformUpdate.BlockDimensions = threadBlockSize;
            kernelUniformUpdate.GridDimensions = (batchSize * arrayLength) / (4 * threadBlockSize);
            kernelUniformUpdate.Run(output.DevicePointer, buffer.DevicePointer);

            buffer.Dispose();

            return output;
        }

        private bool IsPowerOfTwo(int x)
        {
            return (x != 0) && ((x & (x - 1)) == 0);
        }
    }
}
