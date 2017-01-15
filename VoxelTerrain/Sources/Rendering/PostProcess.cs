using System;
using SlimDX.Direct3D11;
using SlimDX.D3DCompiler;
using SlimDX.DXGI;
using Device = SlimDX.Direct3D11.Device;
using Buffer = SlimDX.Direct3D11.Buffer;
using MapFlags = SlimDX.Direct3D11.MapFlags;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using SlimDX;
using System.Drawing;

namespace VoxelTerrain
{
    /// <summary>
    /// This object can apply various effects on textures using compute shaders.
    /// </summary>
    public class PostProcess
    {
        /// <summary>
        /// Virtual adapter used to perform rendering.
        /// </summary>
        private Device graphicsDevice;

        /// <summary>
        /// List of render targets used by post process object.
        /// </summary>
        private List<RenderTarget> renderTargets;

        /// <summary>
        /// A set of compute shader programs with effects.
        /// </summary>
        private Dictionary<string, ComputeShader> effects;

        /// <summary>
        /// Input buffer used by compute shader programs.
        /// </summary>
        private Buffer constantBuffer;

        /// <summary>
        /// Object with effects' settings.
        /// </summary>
        public PostProcessSettings Settings;

        /// <summary>
        /// Default constructor.
        /// </summary>
        /// <param name="graphicsDevice">Virtual adapter used to perform rendering.</param>
        /// <param name="width">Screen space width.</param>
        /// <param name="height">Screen space height.</param>
        public PostProcess(Device graphicsDevice, int width, int height)
        {
            this.graphicsDevice = graphicsDevice;

            renderTargets = new List<RenderTarget>();
            effects = new Dictionary<string, ComputeShader>();

            AddEffect("DownSample4x");
            AddEffect("BlurV");
            AddEffect("BlurH");
            AddEffect("BrightPass");
            AddEffect("UpSample4x");
            AddEffect("UpSample4xCombine");
            AddEffect("AddFogTexture");

            AddRenderTarget(width / 4, height / 4, Format.R8G8B8A8_UNorm);
            AddRenderTarget(width / 16, height / 16, Format.R8G8B8A8_UNorm);
            AddRenderTarget(width / 16, height / 16, Format.R8G8B8A8_UNorm);
            AddRenderTarget(width, height, Format.R8G8B8A8_UNorm);
            AddRenderTarget(width, height, Format.R8G8B8A8_UNorm);
            AddRenderTarget(width, height, Format.R8G8B8A8_UNorm);
            AddRenderTarget(width, height, Format.R8G8B8A8_UNorm);

            BufferDescription description = new BufferDescription()
            {
                BindFlags = BindFlags.ConstantBuffer,
                CpuAccessFlags = CpuAccessFlags.Write,
                OptionFlags = ResourceOptionFlags.None,
                SizeInBytes = 2 * Marshal.SizeOf(typeof(Vector4)),
                StructureByteStride = 2 * Marshal.SizeOf(typeof(Vector4)),
                Usage = ResourceUsage.Dynamic
            };

            constantBuffer = new Buffer(graphicsDevice, description);

            Settings.BloomSettings = new Vector4(0.08f, 0.18f, 0.8f, 1);
        }

        /// <summary>
        /// Adds new render target to the list.
        /// </summary>
        /// <param name="width">New render target width.</param>
        /// <param name="height">New render target height.</param>
        /// <param name="format">New render target format.</param>
        private void AddRenderTarget(int width, int height, Format format)
        {
            renderTargets.Add(new RenderTarget(graphicsDevice, width, height, format));
        }

        /// <summary>
        /// Adds shader program with specified entry point to the set.
        /// </summary>
        /// <param name="name">Shader program entry point.</param>
        private void AddEffect(string name)
        {
            effects.Add(name, new ComputeShader(graphicsDevice, ShaderPrecompiler.PrecompileOrLoad(@"Shaders\PostProcess.hlsl", name, "cs_5_0", ShaderFlags.None, EffectFlags.None)));
        }

        /// <summary>
        /// Sets specified shader program and performs computations.
        /// </summary>
        /// <param name="shader">Name of shader program.</param>
        /// <param name="outputScale">Screen space scale factor.</param>
        /// <param name="output">Output render target.</param>
        /// <param name="input">Input render targets.</param>
        private void Dispatch(string shader, int outputScale, RenderTarget output, params RenderTarget[] input)
        {
            Viewport viewport = graphicsDevice.ImmediateContext.Rasterizer.GetViewports()[0];

            graphicsDevice.ImmediateContext.ComputeShader.Set(effects[shader]);
            graphicsDevice.ImmediateContext.ComputeShader.SetConstantBuffer(constantBuffer, 0);

            for (int i = 0; i < input.Length; i++)
                graphicsDevice.ImmediateContext.ComputeShader.SetShaderResource(input[i].GetShaderResourceView(), i);
            graphicsDevice.ImmediateContext.ComputeShader.SetUnorderedAccessView(output.GetUnorderedAccessView(), 0);

            graphicsDevice.ImmediateContext.Dispatch((int)Math.Ceiling(viewport.Width / (32 * outputScale)), (int)Math.Ceiling(viewport.Height / (32 * outputScale)), 1);
            
            graphicsDevice.ImmediateContext.ComputeShader.SetUnorderedAccessView(null, 0);
        }

        /// <summary>
        /// Makes bloom effect using specified input texture.
        /// </summary>
        /// <param name="texture">Texture to apply effect on.</param>
        /// <returns>Texture with applied effect.</returns>
        public ShaderResourceView MakeBloomEffect(RenderTarget texture)
        {
            Viewport viewport = graphicsDevice.ImmediateContext.Rasterizer.GetViewports()[0];

            DataBox data = graphicsDevice.ImmediateContext.MapSubresource(constantBuffer, MapMode.WriteDiscard, MapFlags.None);
            data.Data.Write<Vector4>(new Vector4(viewport.Width, viewport.Height, 0, 0));
            data.Data.Write<Vector4>(Settings.BloomSettings);
            graphicsDevice.ImmediateContext.UnmapSubresource(constantBuffer, 0);

            // DownSample4x to RT0
            Dispatch("DownSample4x", 4, renderTargets[0], texture);

            // DownSample4x to RT1
            Dispatch("DownSample4x", 16, renderTargets[1], renderTargets[0]);

            // BrightPass to RT2
            Dispatch("BrightPass", 16, renderTargets[2], renderTargets[1]);

            // BlurV to RT1
            Dispatch("BlurV", 16, renderTargets[1], renderTargets[2]);

            // BlurH to RT2
            Dispatch("BlurH", 16, renderTargets[2], renderTargets[1]);

            // BlurV to RT1
            Dispatch("BlurV", 16, renderTargets[1], renderTargets[2]);

            // BlurH to RT2
            Dispatch("BlurH", 16, renderTargets[2], renderTargets[1]);

            // UpSample4x to RT0
            Dispatch("UpSample4x", 4, renderTargets[0], renderTargets[2]);

            // UpSample4xCombine RT0 and texture to RT3
            Dispatch("UpSample4xCombine", 1, renderTargets[3], texture, renderTargets[0]);

            return renderTargets[3].GetShaderResourceView();
        }

        /// <summary>
        /// Combines original texture with fog texture.
        /// </summary>
        /// <returns>Texture with applied effect.</returns>
        public RenderTarget AddFogTexture()
        {
            // AddFogTexture RT4 and RT5 to RT6
            Dispatch("AddFogTexture", 1, renderTargets[6], renderTargets[4], renderTargets[5]);

            return renderTargets[6];
        }

        /// <summary>
        /// Initializes render targets.
        /// </summary>
        /// <param name="width">Screen space width.</param>
        /// <param name="height">Screen space height.</param>
        public void Initialize(int width, int height)
        {
            renderTargets[0].Initialize(width / 4, height / 4);
            renderTargets[1].Initialize(width / 16, height / 16);
            renderTargets[2].Initialize(width / 16, height / 16);
            renderTargets[3].Initialize(width, height);
            renderTargets[4].Initialize(width, height);
            renderTargets[5].Initialize(width, height);
            renderTargets[6].Initialize(width, height);
        }

        /// <summary>
        /// Gets render target with specified index in a list.
        /// </summary>
        /// <param name="index">List index.</param>
        /// <returns>Render target at specified position.</returns>
        public RenderTarget GetRenderTarget(int index)
        {
            return renderTargets[index];
        }
    }
}
