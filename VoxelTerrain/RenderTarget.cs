using SlimDX.Direct3D11;
using SlimDX.DXGI;
using Device = SlimDX.Direct3D11.Device;
using SlimDX;

namespace VoxelTerrain
{
    /// <summary>
    /// This object is used in post process techniques. Scene can be rendered into its texture.
    /// </summary>
    public class RenderTarget
    {
        /// <summary>
        /// Virtual adapter used to perform rendering.
        /// </summary>
        private Device graphicsDevice;

        /// <summary>
        /// Texture object.
        /// </summary>
        private Texture2D texture;

        /// <summary>
        /// Object used to pass texture into shader program as an input resource.
        /// </summary>
        private ShaderResourceView shaderResourceView;

        /// <summary>
        /// Object used to pass texture into compute shader as an output resource.
        /// </summary>
        private UnorderedAccessView unorderedAccessView;

        /// <summary>
        /// Render target connected with texture object.
        /// </summary>
        private RenderTargetView renderTargetView;

        /// <summary>
        /// Texture width.
        /// </summary>
        private int width;

        /// <summary>
        /// Texture height.
        /// </summary>
        private int height;

        /// <summary>
        /// Texture format.
        /// </summary>
        private Format format;

        /// <summary>
        /// Creates render target object used specified values.
        /// </summary>
        /// <param name="graphicsDevice">Virtual adapter used to perform rendering.</param>
        /// <param name="width">Texture width.</param>
        /// <param name="height">Texture height.</param>
        /// <param name="format">Texture format.</param>
        public RenderTarget(Device graphicsDevice, int width, int height, Format format)
        {
            this.graphicsDevice = graphicsDevice;

            Initialize(width, height, format);
        }

        /// <summary>
        /// Sets render target as an actual target in output merger.
        /// </summary>
        /// <param name="depthStencilView">Depth stencil target.</param>
        public void SetRenderTarget(DepthStencilView depthStencilView)
        {
            graphicsDevice.ImmediateContext.OutputMerger.SetTargets(depthStencilView, renderTargetView);
        }

        /// <summary>
        /// Clears render target and depth stencil target textures with specified color.
        /// </summary>
        /// <param name="depthStencilView">Depth stencil target.</param>
        /// <param name="color">Clear color.</param>
        public void ClearRenderTarget(DepthStencilView depthStencilView, Color4 color)
        {
            graphicsDevice.ImmediateContext.ClearRenderTargetView(renderTargetView, color);
            graphicsDevice.ImmediateContext.ClearDepthStencilView(depthStencilView, DepthStencilClearFlags.Depth, 1.0f, 0);
        }

        /// <summary>
        /// Clears render target texture with specified color.
        /// </summary>
        /// <param name="color">Clear color.</param>
        public void ClearRenderTarget(Color4 color)
        {
            graphicsDevice.ImmediateContext.ClearRenderTargetView(renderTargetView, color);
        }

        /// <summary>
        /// Returns shader resource view object connected with the texture.
        /// </summary>
        /// <returns></returns>
        public ShaderResourceView GetShaderResourceView()
        {
            return shaderResourceView;
        }

        /// <summary>
        /// Returns unordered access view object connected with the texture.
        /// </summary>
        /// <returns></returns>
        public UnorderedAccessView GetUnorderedAccessView()
        {
            return unorderedAccessView;
        }

        /// <summary>
        /// Initializes render target object using remembered settings values.
        /// </summary>
        public void Initialize()
        {
            Initialize(this.width, this.height, this.format);
        }

        /// <summary>
        /// Initializes render target object using new width, height and remembered format value.
        /// </summary>
        /// <param name="width">New texture width.</param>
        /// <param name="height">New texture height.</param>
        public void Initialize(int width, int height)
        {
            Initialize(width, height, this.format);
        }

        /// <summary>
        /// Initializes render target object using specified values. If resources have been created
        /// earlier then it will dispose them.
        /// </summary>
        /// <param name="width">Texture width.</param>
        /// <param name="height">Texture height.</param>
        /// <param name="format">Texture format.</param>
        public void Initialize(int width, int height, Format format)
        {
            this.width = width;
            this.height = height;
            this.format = format;

            Texture2DDescription textureDescription = new Texture2DDescription()
            {
                ArraySize = 1,
                BindFlags = BindFlags.RenderTarget | BindFlags.ShaderResource | BindFlags.UnorderedAccess,
                CpuAccessFlags = CpuAccessFlags.None,
                Format = format,
                Height = height,
                MipLevels = 1,
                OptionFlags = ResourceOptionFlags.None,
                SampleDescription = new SampleDescription(1, 0),
                Usage = ResourceUsage.Default,
                Width = width
            };

            if (texture != null)
                texture.Dispose();
            texture = new Texture2D(graphicsDevice, textureDescription);

            if (renderTargetView != null)
                renderTargetView.Dispose();
            renderTargetView = new RenderTargetView(graphicsDevice, texture);

            if (shaderResourceView != null)
                shaderResourceView.Dispose();
            shaderResourceView = new ShaderResourceView(graphicsDevice, texture);

            if (unorderedAccessView != null)
                unorderedAccessView.Dispose();
            unorderedAccessView = new UnorderedAccessView(graphicsDevice, texture);
        }
    }
}
