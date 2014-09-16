using SlimDX.Direct3D11;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Device = SlimDX.Direct3D11.Device;
using Buffer = SlimDX.Direct3D11.Buffer;
using MapFlags = SlimDX.Direct3D11.MapFlags;
using SlimDX.D3DCompiler;
using SlimDX.DXGI;
using SlimDX;

namespace VoxelTerrain
{
    public class DirectComputeNoiseCube : IDisposable
    {
        private Device graphicsDevice;

        private ComputeShader computeFillNoiseTexture;

        private bool isDisposed = false;

        public DirectComputeNoiseCube(Device graphicsDevice)
        {
            this.graphicsDevice = graphicsDevice;

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

                computeFillNoiseTexture.Dispose();
            }

            isDisposed = true;
        }

        ~DirectComputeNoiseCube()
        {
            Dispose(false);
        }

        private void Initialize()
        {
            computeFillNoiseTexture = new ComputeShader(graphicsDevice, ShaderPrecompiler.PrecompileOrLoad(@"Shaders\TerrainGenerator.hlsl", "FillNoiseTexture", "cs_5_0", ShaderFlags.None, EffectFlags.None));
        }

        public ShaderResourceView GenerateNoiseTexture(Buffer constantBuffer, DirectComputeConstantBuffer container)
        {
            Texture3D noiseTexture = new Texture3D(graphicsDevice, new Texture3DDescription()
            {
                BindFlags = BindFlags.ShaderResource | BindFlags.UnorderedAccess,
                CpuAccessFlags = CpuAccessFlags.None,
                Format = Format.R32_Float,
                MipLevels = 1,
                OptionFlags = ResourceOptionFlags.None,
                Usage = ResourceUsage.Default,
                Width = container.Width,
                Height = container.Height,
                Depth = container.Depth
            });

            UnorderedAccessView noiseTextureUAV = new UnorderedAccessView(graphicsDevice, noiseTexture);

            DataBox data = graphicsDevice.ImmediateContext.MapSubresource(constantBuffer, MapMode.WriteDiscard, MapFlags.None);
            data.Data.Write<DirectComputeConstantBuffer>(container);
            graphicsDevice.ImmediateContext.UnmapSubresource(constantBuffer, 0);

            graphicsDevice.ImmediateContext.ComputeShader.Set(computeFillNoiseTexture);
            graphicsDevice.ImmediateContext.ComputeShader.SetConstantBuffer(constantBuffer, 0);
            graphicsDevice.ImmediateContext.ComputeShader.SetUnorderedAccessView(noiseTextureUAV, 0);

            Vector3 gridDim = new Vector3((float)Math.Ceiling(container.Width / 8.0f), (float)Math.Ceiling(container.Height / 8.0f), (float)Math.Ceiling(container.Depth / 8.0f));

            graphicsDevice.ImmediateContext.Dispatch((int)gridDim.X, (int)gridDim.Y, (int)gridDim.Z);

            noiseTextureUAV.Dispose();

            return new ShaderResourceView(graphicsDevice, noiseTexture);
        }
    }
}
