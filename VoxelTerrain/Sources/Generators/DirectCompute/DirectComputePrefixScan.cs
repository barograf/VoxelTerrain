using SlimDX.Direct3D11;
using Device = SlimDX.Direct3D11.Device;
using Buffer = SlimDX.Direct3D11.Buffer;
using MapFlags = SlimDX.Direct3D11.MapFlags;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using SlimDX.D3DCompiler;
using System.Runtime.InteropServices;
using SlimDX;

namespace VoxelTerrain
{
    public class DirectComputePrefixScan : IDisposable
    {
        private const int maxBatchElements = 67108864;

        private const int threadBlockSize = 256;

        private const int minShortArraySize = 4;

        private const int maxShortArraySize = 4 * threadBlockSize;

        private const int minLargeArraySize = 8 * threadBlockSize;

        private const int maxLargeArraySize = 4 * threadBlockSize * threadBlockSize;

        private ComputeShader computeScanExclusiveShared;

        private ComputeShader computeScanExclusiveShared2;

        private ComputeShader computeUniformUpdate;

        private Device graphicsDevice;

        private bool isDisposed = false;

        public DirectComputePrefixScan(Device graphicsDevice)
        {
            this.graphicsDevice = graphicsDevice;
            computeScanExclusiveShared = new ComputeShader(graphicsDevice, ShaderPrecompiler.PrecompileOrLoad(@"Shaders\TerrainGenerator.hlsl", "ScanExclusiveShared", "cs_5_0", ShaderFlags.None, EffectFlags.None));
            computeScanExclusiveShared2 = new ComputeShader(graphicsDevice, ShaderPrecompiler.PrecompileOrLoad(@"Shaders\TerrainGenerator.hlsl", "ScanExclusiveShared2", "cs_5_0", ShaderFlags.None, EffectFlags.None));
            computeUniformUpdate = new ComputeShader(graphicsDevice, ShaderPrecompiler.PrecompileOrLoad(@"Shaders\TerrainGenerator.hlsl", "UniformUpdate", "cs_5_0", ShaderFlags.None, EffectFlags.None));
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

                computeScanExclusiveShared.Dispose();
                computeScanExclusiveShared2.Dispose();
                computeUniformUpdate.Dispose();
            }

            isDisposed = true;
        }

        ~DirectComputePrefixScan()
        {
            Dispose(false);
        }

        public UnorderedAccessView PrefixSumArray(Buffer constantBuffer, UnorderedAccessView trisCountUAV)
        {
            int arrayLength = trisCountUAV.Description.ElementCount;
            int batchSize = trisCountUAV.Description.ElementCount / arrayLength;

            if (!IsPowerOfTwo(trisCountUAV.Description.ElementCount))
                throw new Exception("Input array length is not power of two.");

            Buffer buffer = new Buffer(graphicsDevice, new BufferDescription()
            {
                BindFlags = BindFlags.UnorderedAccess,
                CpuAccessFlags = CpuAccessFlags.None,
                OptionFlags = ResourceOptionFlags.StructuredBuffer,
                Usage = ResourceUsage.Default,
                SizeInBytes = Marshal.SizeOf(typeof(int)) * trisCountUAV.Description.ElementCount,
                StructureByteStride = Marshal.SizeOf(typeof(int))
            });

            UnorderedAccessView bufferUAV = new UnorderedAccessView(graphicsDevice, buffer);

            Buffer output = new Buffer(graphicsDevice, new BufferDescription()
            {
                BindFlags = BindFlags.UnorderedAccess,
                CpuAccessFlags = CpuAccessFlags.None,
                OptionFlags = ResourceOptionFlags.StructuredBuffer,
                Usage = ResourceUsage.Default,
                SizeInBytes = Marshal.SizeOf(typeof(int)) * trisCountUAV.Description.ElementCount,
                StructureByteStride = Marshal.SizeOf(typeof(int))
            });

            UnorderedAccessView outputUAV = new UnorderedAccessView(graphicsDevice, output);

            DirectComputeConstantBuffer constantBufferContainer = new DirectComputeConstantBuffer()
            {
                PrefixSize = 4 * threadBlockSize,
                PrefixN = (batchSize * arrayLength) / (4 * threadBlockSize),
                PrefixArrayLength = arrayLength / (4 * threadBlockSize)
            };

            DataBox data = graphicsDevice.ImmediateContext.MapSubresource(constantBuffer, MapMode.WriteDiscard, MapFlags.None);
            data.Data.Write<DirectComputeConstantBuffer>(constantBufferContainer);
            graphicsDevice.ImmediateContext.UnmapSubresource(constantBuffer, 0);

            Vector3 gridDim = new Vector3((batchSize * arrayLength) / (4 * threadBlockSize), 1, 1);
            Vector3 gridDimShared2 = new Vector3((int)Math.Ceiling(((batchSize * arrayLength) / (4 * threadBlockSize)) / (double)threadBlockSize), 1, 1);

            graphicsDevice.ImmediateContext.ComputeShader.Set(computeScanExclusiveShared);
            graphicsDevice.ImmediateContext.ComputeShader.SetConstantBuffer(constantBuffer, 0);
            graphicsDevice.ImmediateContext.ComputeShader.SetUnorderedAccessView(trisCountUAV, 2);
            graphicsDevice.ImmediateContext.ComputeShader.SetUnorderedAccessView(outputUAV, 3);
            graphicsDevice.ImmediateContext.ComputeShader.SetUnorderedAccessView(bufferUAV, 4);

            graphicsDevice.ImmediateContext.Dispatch((int)gridDim.X, (int)gridDim.Y, (int)gridDim.Z);
            
            graphicsDevice.ImmediateContext.ComputeShader.Set(computeScanExclusiveShared2);

            graphicsDevice.ImmediateContext.Dispatch((int)gridDimShared2.X, (int)gridDimShared2.Y, (int)gridDimShared2.Z);

            graphicsDevice.ImmediateContext.ComputeShader.Set(computeUniformUpdate);

            graphicsDevice.ImmediateContext.Dispatch((int)gridDim.X, (int)gridDim.Y, (int)gridDim.Z);

            buffer.Dispose();
            bufferUAV.Dispose();

            return outputUAV;
        }

        private bool IsPowerOfTwo(int x)
        {
            return (x != 0) && ((x & (x - 1)) == 0);
        }
    }
}
