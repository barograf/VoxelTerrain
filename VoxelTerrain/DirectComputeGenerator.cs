using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Device = SlimDX.Direct3D11.Device;
using Buffer = SlimDX.Direct3D11.Buffer;
using MapFlags = SlimDX.Direct3D11.MapFlags;
using SlimDX.Direct3D11;
using System.Runtime.InteropServices;
using SlimDX;
using SlimDX.D3DCompiler;
using SlimDX.DXGI;

namespace VoxelTerrain
{
    class DirectComputeGenerator : IGenerate, IDisposable
    {
        private Device graphicsDevice;

        private VoxelMeshContainer container;

        private ComputeShader computePositionWeightNoiseCube;

        private ComputeShader computePositionWeightNoiseCubeWarp;

        private ComputeShader computeNormalAmbient;

        private ComputeShader computeMarchingCubesCases;

        private ComputeShader computeMarchingCubesVertices;

        private ComputeShader computePositionWeightFormula;

        private Buffer constantBuffer;

        private DirectComputeConstantBuffer constantBufferContainer;

        private DirectComputePrefixScan prefixScan;

        private bool isDisposed = false;

        public DirectComputeGenerator(Device graphicsDevice, VoxelMeshContainer container)
        {
            this.graphicsDevice = graphicsDevice;
            this.container = container;

            Initialize();
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
                computePositionWeightNoiseCube.Dispose();
                computeNormalAmbient.Dispose();
                computeMarchingCubesCases.Dispose();
                computeMarchingCubesVertices.Dispose();
                computePositionWeightNoiseCubeWarp.Dispose();
                computePositionWeightFormula.Dispose();
                constantBuffer.Dispose();
            }

            isDisposed = true;
        }

        ~DirectComputeGenerator()
        {
            Dispose(false);
        }

        private void Initialize()
        {
            constantBuffer = new Buffer(graphicsDevice, new BufferDescription()
            {
                BindFlags = BindFlags.ConstantBuffer,
                CpuAccessFlags = CpuAccessFlags.Write,
                OptionFlags = ResourceOptionFlags.None,
                SizeInBytes = (int)Math.Ceiling(Marshal.SizeOf(typeof(DirectComputeConstantBuffer)) / 16.0) * 16,
                Usage = ResourceUsage.Dynamic
            });

            computePositionWeightNoiseCube = new ComputeShader(graphicsDevice, ShaderPrecompiler.PrecompileOrLoad(@"Shaders\TerrainGenerator.hlsl", "PositionWeightNoiseCube", "cs_5_0", ShaderFlags.None, EffectFlags.None));
            computeNormalAmbient = new ComputeShader(graphicsDevice, ShaderPrecompiler.PrecompileOrLoad(@"Shaders\TerrainGenerator.hlsl", "NormalAmbient", "cs_5_0", ShaderFlags.None, EffectFlags.None));
            computeMarchingCubesCases = new ComputeShader(graphicsDevice, ShaderPrecompiler.PrecompileOrLoad(@"Shaders\TerrainGenerator.hlsl", "MarchingCubesCases", "cs_5_0", ShaderFlags.None, EffectFlags.None));
            computeMarchingCubesVertices = new ComputeShader(graphicsDevice, ShaderPrecompiler.PrecompileOrLoad(@"Shaders\TerrainGenerator.hlsl", "MarchingCubesVertices", "cs_5_0", ShaderFlags.None, EffectFlags.None));
            computePositionWeightNoiseCubeWarp = new ComputeShader(graphicsDevice, ShaderPrecompiler.PrecompileOrLoad(@"Shaders\TerrainGenerator.hlsl", "PositionWeightNoiseCubeWarp", "cs_5_0", ShaderFlags.None, EffectFlags.None));
            computePositionWeightFormula = new ComputeShader(graphicsDevice, ShaderPrecompiler.PrecompileOrLoad(@"Shaders\TerrainGenerator.hlsl", "PositionWeightFormula", "cs_5_0", ShaderFlags.None, EffectFlags.None));

            prefixScan = new DirectComputePrefixScan(graphicsDevice);
        }

        private void Generate(ComputeShader computePositionWeight, int width, int height, int depth)
        {
            int count = width * height * depth;
            int widthD = width - 1;
            int heightD = height - 1;
            int depthD = depth - 1;
            int countD = widthD * heightD * depthD;

            int nearestW = NearestPowerOfTwo(widthD);
            int nearestH = NearestPowerOfTwo(heightD);
            int nearestD = NearestPowerOfTwo(depthD);
            int nearestCount = nearestW * nearestH * nearestD;

            Vector3 gridDim = new Vector3((float)Math.Ceiling(width / 8.0f), (float)Math.Ceiling(height / 8.0f), (float)Math.Ceiling(depth / 8.0f));
            Vector3 gridDimD = new Vector3((float)Math.Ceiling(widthD / 8.0f), (float)Math.Ceiling(heightD / 8.0f), (float)Math.Ceiling(depthD / 8.0f));

            constantBufferContainer = new DirectComputeConstantBuffer()
            {
                Width = 16,
                Height = 16,
                Depth = 16,
                Seed = (int)DateTime.Now.Ticks
            };

            DirectComputeNoiseCube noiseCube = new DirectComputeNoiseCube(graphicsDevice);

            ShaderResourceView noiseSRV = noiseCube.GenerateNoiseTexture(constantBuffer, constantBufferContainer);

            Buffer voxelsBuffer = new Buffer(graphicsDevice, new BufferDescription()
            {
                BindFlags = BindFlags.UnorderedAccess,
                CpuAccessFlags = CpuAccessFlags.None,
                OptionFlags = ResourceOptionFlags.StructuredBuffer,
                Usage = ResourceUsage.Default,
                SizeInBytes = Marshal.SizeOf(typeof(Voxel)) * count,
                StructureByteStride = Marshal.SizeOf(typeof(Voxel))
            });

            UnorderedAccessView voxelsUAV = new UnorderedAccessView(graphicsDevice, voxelsBuffer);

            constantBufferContainer = new DirectComputeConstantBuffer()
            {
                Width = width,
                Height = height,
                Depth = depth,
                AmbientRayWidth = container.Settings.AmbientRayWidth,
                AmbientSamplesCount = container.Settings.AmbientSamplesCount,
                NearestWidth = nearestW,
                NearestHeight = nearestH,
                NearestDepth = nearestD
            };

            DataBox data = graphicsDevice.ImmediateContext.MapSubresource(constantBuffer, MapMode.WriteDiscard, MapFlags.None);
            data.Data.Write<DirectComputeConstantBuffer>(constantBufferContainer);
            graphicsDevice.ImmediateContext.UnmapSubresource(constantBuffer, 0);

            graphicsDevice.ImmediateContext.ComputeShader.Set(computePositionWeight);
            graphicsDevice.ImmediateContext.ComputeShader.SetConstantBuffer(constantBuffer, 0);
            graphicsDevice.ImmediateContext.ComputeShader.SetUnorderedAccessView(voxelsUAV, 0);
            graphicsDevice.ImmediateContext.ComputeShader.SetShaderResource(noiseSRV, 0);

            graphicsDevice.ImmediateContext.Dispatch((int)gridDim.X, (int)gridDim.Y, (int)gridDim.Z);

            graphicsDevice.ImmediateContext.ComputeShader.Set(computeNormalAmbient);

            graphicsDevice.ImmediateContext.Dispatch((int)gridDim.X, (int)gridDim.Y, (int)gridDim.Z);

            Buffer offsetsBuffer = new Buffer(graphicsDevice, new BufferDescription()
            {
                BindFlags = BindFlags.UnorderedAccess,
                CpuAccessFlags = CpuAccessFlags.None,
                OptionFlags = ResourceOptionFlags.StructuredBuffer,
                Usage = ResourceUsage.Default,
                SizeInBytes = Marshal.SizeOf(typeof(int)) * countD,
                StructureByteStride = Marshal.SizeOf(typeof(int))
            });

            UnorderedAccessView offsetsUAV = new UnorderedAccessView(graphicsDevice, offsetsBuffer);

            Buffer trisCountBuffer = new Buffer(graphicsDevice, new BufferDescription()
            {
                BindFlags = BindFlags.UnorderedAccess,
                CpuAccessFlags = CpuAccessFlags.None,
                OptionFlags = ResourceOptionFlags.StructuredBuffer,
                Usage = ResourceUsage.Default,
                SizeInBytes = Marshal.SizeOf(typeof(int)) * nearestCount,
                StructureByteStride = Marshal.SizeOf(typeof(int))
            });

            UnorderedAccessView trisCountUAV = new UnorderedAccessView(graphicsDevice, trisCountBuffer);

            graphicsDevice.ImmediateContext.ClearUnorderedAccessView(trisCountUAV, new int[] { 0, 0, 0, 0 });

            graphicsDevice.ImmediateContext.ComputeShader.Set(computeMarchingCubesCases);
            graphicsDevice.ImmediateContext.ComputeShader.SetUnorderedAccessView(offsetsUAV, 1);
            graphicsDevice.ImmediateContext.ComputeShader.SetUnorderedAccessView(trisCountUAV, 2);

            graphicsDevice.ImmediateContext.Dispatch((int)gridDimD.X, (int)gridDimD.Y, (int)gridDimD.Z);

            UnorderedAccessView prefixSumsUAV = prefixScan.PrefixSumArray(constantBuffer, trisCountUAV);

            int lastTrisCount = DirectComputeBufferHelper.CopyBuffer<int>(graphicsDevice, trisCountBuffer, nearestCount - 1, 1)[0];

            int lastPrefixSum = DirectComputeBufferHelper.CopyBuffer<int>(graphicsDevice, prefixSumsUAV.Resource, nearestCount - 1, 1)[0];

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

                Buffer verticesBuffer = new Buffer(graphicsDevice, new BufferDescription()
                {
                    BindFlags = BindFlags.UnorderedAccess,
                    CpuAccessFlags = CpuAccessFlags.None,
                    OptionFlags = ResourceOptionFlags.StructuredBuffer,
                    Usage = ResourceUsage.Default,
                    SizeInBytes = Marshal.SizeOf(typeof(VoxelMeshVertex)) * totalVerticesCount,
                    StructureByteStride = Marshal.SizeOf(typeof(VoxelMeshVertex))
                });

                UnorderedAccessView verticesUAV = new UnorderedAccessView(graphicsDevice, verticesBuffer);

                constantBufferContainer = new DirectComputeConstantBuffer()
                {
                    Width = width,
                    Height = height,
                    Depth = depth,
                    NearestWidth = nearestW,
                    NearestHeight = nearestH,
                    NearestDepth = nearestD
                };

                data = graphicsDevice.ImmediateContext.MapSubresource(constantBuffer, MapMode.WriteDiscard, MapFlags.None);
                data.Data.Write<DirectComputeConstantBuffer>(constantBufferContainer);
                graphicsDevice.ImmediateContext.UnmapSubresource(constantBuffer, 0);

                graphicsDevice.ImmediateContext.ComputeShader.Set(computeMarchingCubesVertices);
                graphicsDevice.ImmediateContext.ComputeShader.SetUnorderedAccessView(trisCountUAV, 2);
                graphicsDevice.ImmediateContext.ComputeShader.SetUnorderedAccessView(prefixSumsUAV, 3);
                graphicsDevice.ImmediateContext.ComputeShader.SetUnorderedAccessView(verticesUAV, 5);

                graphicsDevice.ImmediateContext.Dispatch((int)gridDimD.X, (int)gridDimD.Y, (int)gridDimD.Z);

                graphicsDevice.ImmediateContext.CopyResource(verticesBuffer, container.Geometry);

                verticesUAV.Dispose();
                verticesBuffer.Dispose();
            }
            else
            {
                container.VertexCount = 0;

                if (container.Geometry != null)
                    container.Geometry.Dispose();
            }

            for (int i = 0; i <= 5; i++)
            {
                graphicsDevice.ImmediateContext.ComputeShader.SetUnorderedAccessView(null, i);
            }

            prefixSumsUAV.Resource.Dispose();
            prefixSumsUAV.Dispose();
            noiseCube.Dispose();
            noiseSRV.Resource.Dispose();
            noiseSRV.Dispose();
            voxelsBuffer.Dispose();
            voxelsUAV.Dispose();
            offsetsBuffer.Dispose();
            offsetsUAV.Dispose();
            trisCountBuffer.Dispose();
            trisCountUAV.Dispose();
        }

        private int NearestPowerOfTwo(int x)
        {
            return (int)Math.Pow(2, Math.Ceiling(Math.Log(x) / Math.Log(2)));
        }

        public void GenerateFromNoiseCubeWithWarp(int width, int height, int depth)
        {
            Generate(computePositionWeightNoiseCubeWarp, width, height, depth);
        }

        public void GenerateFromNoiseCube(int width, int height, int depth)
        {
            Generate(computePositionWeightNoiseCube, width, height, depth);
        }

        public void GenerateFromFormula(int width, int height, int depth)
        {
            Generate(computePositionWeightFormula, width, height, depth);
        }
    }
}
