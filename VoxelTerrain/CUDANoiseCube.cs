using ManagedCuda;
using ManagedCuda.BasicTypes;
using ManagedCuda.CudaRand;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace VoxelTerrain
{
    public class CUDANoiseCube : IDisposable
    {
        private CudaRandDevice randomDevice;

        private bool isDisposed = false;

        public CUDANoiseCube()
        {
            randomDevice = new CudaRandDevice(GeneratorType.PseudoDefault);
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

                randomDevice.Dispose();
            }

            isDisposed = true;
        }

        ~CUDANoiseCube()
        {
            Dispose(false);
        }
        
        public CudaArray3D GenerateUniformArray(int width, int height, int depth)
        {
            int count = width * height * depth;

            CudaDeviceVariable<float> randomVariable = new CudaDeviceVariable<float>(count);
            CudaArray3D randomArray = new CudaArray3D(CUArrayFormat.Float, width, height, depth, CudaArray3DNumChannels.One, CUDAArray3DFlags.None);

            randomDevice.SetPseudoRandomGeneratorSeed((ulong)DateTime.Now.Ticks);
            randomDevice.GenerateUniform32(randomVariable.DevicePointer, count);

            randomArray.CopyFromDeviceToThis(randomVariable.DevicePointer, sizeof(float));

            randomVariable.Dispose();

            return randomArray;
        }
    }
}
