using ManagedCuda;
using SlimDX;
using SlimDX.Direct3D11;
using Device = SlimDX.Direct3D11.Device;
using Buffer = SlimDX.Direct3D11.Buffer;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using ManagedCuda.VectorTypes;
using System.Reflection;
using ManagedCuda.BasicTypes;
using ManagedCuda.CudaRand;

namespace VoxelTerrain
{
    class CUDAGenerator : IGenerate, IDisposable
    {
        private Device graphicsDevice;

        private VoxelMeshContainer container;

        private CudaContext context;

        private CUmodule module;

        private CudaKernel kernelPositionWeightNoiseCube;

        private CudaKernel kernelPositionWeightNoiseCubeWarp;

        private CudaKernel kernelPositionWeightFormula;

        private CudaKernel kernelNormalAmbient;

        private CudaKernel kernelMarchingCubesCases;

        private CudaKernel kernelMarchingCubesVertices;

        private CUDAPrefixScan prefixScan;

        private bool isDisposed = false;

        public CUDAGenerator(Device graphicsDevice, VoxelMeshContainer container)
        {
            this.graphicsDevice = graphicsDevice;
            this.container = container;

            InitializeCUDA();
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

                prefixScan.Dispose();
                // Unloading every single kernel will cause an error.
                context.UnloadModule(module);
                context.Dispose();
            }

            isDisposed = true;
        }

        ~CUDAGenerator()
        {
            Dispose(false);
        }

        public void GenerateFromNoiseCubeWithWarp(int width, int height, int depth)
        {
            Generate(kernelPositionWeightNoiseCubeWarp, width, height, depth);
        }

        public void GenerateFromNoiseCube(int width, int height, int depth)
        {
            Generate(kernelPositionWeightNoiseCube, width, height, depth);
        }

        public void GenerateFromFormula(int width, int height, int depth)
        {
            Generate(kernelPositionWeightFormula, width, height, depth);
        }

        private void Generate(CudaKernel kernelPositionWeight, int width, int height, int depth)
        {
            int count = width * height * depth;
            int widthD = width - 1;
            int heightD = height - 1;
            int depthD = depth - 1;
            int countDecremented = widthD * heightD * depthD;

            dim3 blockDimensions = new dim3(8, 8, 8);
            dim3 gridDimensions = new dim3((int)Math.Ceiling(width / 8.0), (int)Math.Ceiling(height / 8.0), (int)Math.Ceiling(depth / 8.0));
            dim3 gridDimensionsDecremented = new dim3((int)Math.Ceiling(widthD / 8.0), (int)Math.Ceiling(heightD / 8.0), (int)Math.Ceiling(depthD / 8.0));

            CUDANoiseCube noiseCube = new CUDANoiseCube();

            CudaArray3D noiseArray = noiseCube.GenerateUniformArray(16, 16, 16);
            CudaTextureArray3D noiseTexture = new CudaTextureArray3D(kernelPositionWeight, "noiseTexture", CUAddressMode.Wrap, CUFilterMode.Linear, CUTexRefSetFlags.NormalizedCoordinates, noiseArray);

            CudaDeviceVariable<Voxel> voxelsDev = new CudaDeviceVariable<Voxel>(count);

            kernelPositionWeight.BlockDimensions = blockDimensions;
            typeof(CudaKernel).GetField("_gridDim", BindingFlags.Instance | BindingFlags.NonPublic).SetValue(kernelPositionWeight, gridDimensions);

            kernelPositionWeight.Run(voxelsDev.DevicePointer, width, height, depth);

            kernelNormalAmbient.BlockDimensions = blockDimensions;
            typeof(CudaKernel).GetField("_gridDim", BindingFlags.Instance | BindingFlags.NonPublic).SetValue(kernelNormalAmbient, gridDimensions);

            kernelNormalAmbient.Run(voxelsDev.DevicePointer, width, height, depth, container.Settings.AmbientRayWidth, container.Settings.AmbientSamplesCount);

            int nearestW = NearestPowerOfTwo(widthD);
            int nearestH = NearestPowerOfTwo(heightD);
            int nearestD = NearestPowerOfTwo(depthD);
            int nearestCount = nearestW * nearestH * nearestD;

            CudaDeviceVariable<int> trisCountDevice = new CudaDeviceVariable<int>(nearestCount);
            trisCountDevice.Memset(0);
            CudaDeviceVariable<int> offsetsDev = new CudaDeviceVariable<int>(countDecremented);

            kernelMarchingCubesCases.BlockDimensions = blockDimensions;
            typeof(CudaKernel).GetField("_gridDim", BindingFlags.Instance | BindingFlags.NonPublic).SetValue(kernelMarchingCubesCases, gridDimensionsDecremented);

            kernelMarchingCubesCases.Run(voxelsDev.DevicePointer, width, height, depth, offsetsDev.DevicePointer, trisCountDevice.DevicePointer, nearestW, nearestH, nearestD);

            CudaDeviceVariable<int> prefixSumsDev = prefixScan.PrefixSumArray(trisCountDevice, nearestCount);

            int lastTrisCount = 0;
            trisCountDevice.CopyToHost(ref lastTrisCount, (nearestCount - 1) * sizeof(int));

            int lastPrefixSum = 0;
            prefixSumsDev.CopyToHost(ref lastPrefixSum, (nearestCount - 1) * sizeof(int));

            int totalVerticesCount = (lastTrisCount + lastPrefixSum) * 3;

            if (totalVerticesCount > 0)
            {
                if (container.Geometry != null)
                    container.Geometry.Dispose();

                container.VertexCount = totalVerticesCount;

                container.Geometry = new Buffer(graphicsDevice, new BufferDescription()
                {
                    BindFlags = BindFlags.VertexBuffer,
                    CpuAccessFlags = CpuAccessFlags.None,
                    OptionFlags = ResourceOptionFlags.None,
                    SizeInBytes = Marshal.SizeOf(typeof(VoxelMeshVertex)) * totalVerticesCount,
                    Usage = ResourceUsage.Default
                });

                CudaDirectXInteropResource directResource = new CudaDirectXInteropResource(container.Geometry.ComPointer, CUGraphicsRegisterFlags.None, CudaContext.DirectXVersion.D3D11, CUGraphicsMapResourceFlags.None);
                
                kernelMarchingCubesVertices.BlockDimensions = blockDimensions;
                typeof(CudaKernel).GetField("_gridDim", BindingFlags.Instance | BindingFlags.NonPublic).SetValue(kernelMarchingCubesVertices, gridDimensionsDecremented);

                directResource.Map();
                kernelMarchingCubesVertices.Run(directResource.GetMappedPointer(), voxelsDev.DevicePointer, prefixSumsDev.DevicePointer, offsetsDev.DevicePointer, width, height, depth, nearestW, nearestH, nearestD);
                directResource.UnMap();

                directResource.Dispose();
            }
            else
            {
                container.VertexCount = 0;

                if (container.Geometry != null)
                    container.Geometry.Dispose();
            }

            noiseCube.Dispose();
            prefixSumsDev.Dispose();
            trisCountDevice.Dispose();
            offsetsDev.Dispose();
            noiseArray.Dispose();
            noiseTexture.Dispose();
            voxelsDev.Dispose();
        }

        private int NearestPowerOfTwo(int x)
        {
            return (int)Math.Pow(2, Math.Ceiling(Math.Log(x) / Math.Log(2)));
        }

        private void InitializeCUDA()
        {
            context = new CudaContext(CudaContext.GetMaxGflopsDevice(), graphicsDevice.ComPointer, CUCtxFlags.SchedAuto, CudaContext.DirectXVersion.D3D11);

            module = context.LoadModulePTX(@"Kernels\kernel.ptx");

            kernelPositionWeightNoiseCube = new CudaKernel("position_weight_noise_cube", module, context);
            kernelNormalAmbient = new CudaKernel("normal_ambient", module, context);
            kernelMarchingCubesCases = new CudaKernel("marching_cubes_cases", module, context);
            kernelMarchingCubesVertices = new CudaKernel("marching_cubes_vertices", module, context);
            kernelPositionWeightNoiseCubeWarp = new CudaKernel("position_weight_noise_cube_warp", module, context);
            kernelPositionWeightFormula = new CudaKernel("position_weight_formula", module, context);

            prefixScan = new CUDAPrefixScan(module, context);
        }
    }
}
