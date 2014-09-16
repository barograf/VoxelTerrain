using System;
using System.Collections.Generic;
using SlimDX.Windows;
using SlimDX.DXGI;
using SlimDX;
using Device = SlimDX.Direct3D11.Device;
using SlimDX.Direct3D11;
using System.Windows.Forms;
using SlimDX.DirectInput;
using System.IO;
using System.Drawing;
using System.Drawing.Imaging;

namespace VoxelTerrain
{
    /// <summary>
    /// Simplifies usage of SlimDX.
    /// </summary>
    public class FrameworkForm : RenderForm
    {
        /// <summary>
        /// Virtual adapter used to perform rendering.
        /// </summary>
        protected Device graphicsDevice;

        /// <summary>
        /// Swap chain object.
        /// </summary>
        protected SwapChain swapChain;

        /// <summary>
        /// Render target view object.
        /// </summary>
        protected RenderTargetView renderTargetView;

        /// <summary>
        /// Camera object.
        /// </summary>
        protected Camera camera;

        /// <summary>
        /// Keyboard object.
        /// </summary>
        protected Keyboard keyboard;

        /// <summary>
        /// Mouse object.
        /// </summary>
        protected Mouse mouse;

        /// <summary>
        /// Depth stencil view object.
        /// </summary>
        protected DepthStencilView depthStencilView;

        /// <summary>
        /// Depth texture object.
        /// </summary>
        protected Texture2D depthTexture;

        /// <summary>
        /// Back buffer texture object.
        /// </summary>
        protected Texture2D backBufferTexture;

        /// <summary>
        /// List of textures.
        /// </summary>
        protected List<TexturePack> textures;

        /// <summary>
        /// Rendering fill mode.
        /// </summary>
        protected FillMode fillMode;

        /// <summary>
        /// Post process object.
        /// </summary>
        protected PostProcess postProcess;

        /// <summary>
        /// Quad renderer object.
        /// </summary>
        protected QuadRenderer quadRenderer;

        /// <summary>
        /// Default constructor.
        /// </summary>
        public FrameworkForm()
        {

        }

        /// <summary>
        /// Initializes SlimDX and input devices.
        /// </summary>
        /// <param name="caption">Window caption string.</param>
        public FrameworkForm(string caption)
            : base(caption)
        {
            SwapChainDescription description = new SwapChainDescription()
            {
                BufferCount = 1,
                Flags = SwapChainFlags.None,
                IsWindowed = true,
                ModeDescription = new ModeDescription(ClientSize.Width, ClientSize.Height, new Rational(60, 1), Format.R8G8B8A8_UNorm),
                OutputHandle = Handle,
                SampleDescription = new SampleDescription(1, 0),
                SwapEffect = SwapEffect.Discard,
                Usage = Usage.RenderTargetOutput
            };

            try
            {
#if DEBUG
                Device.CreateWithSwapChain(DriverType.Hardware, DeviceCreationFlags.Debug, description, out graphicsDevice, out swapChain);
#else
                Device.CreateWithSwapChain(DriverType.Hardware, DeviceCreationFlags.None, description, out graphicsDevice, out swapChain);
#endif
            }
            catch
            {
                MessageBox.Show("An error has occurred during initialization process.");
                Environment.Exit(0);
            }
            finally
            {
                if (graphicsDevice.FeatureLevel != FeatureLevel.Level_11_0)
                {
                    MessageBox.Show("This program requires DirectX 11. Your version is " + graphicsDevice.FeatureLevel.ToString() + ".");
                    Environment.Exit(0);
                }
            }

            Factory factory = swapChain.GetParent<Factory>();
            factory.SetWindowAssociation(Handle, WindowAssociationFlags.IgnoreAltEnter);
            KeyDown += (o, e) =>
            {
                // Fixes Alt-Enter keyboard input bug in SlimDX.
                if (e.Alt && e.KeyCode == Keys.Enter)
                    swapChain.IsFullScreen = !swapChain.IsFullScreen;

                // Makes screenshot.
                if (e.KeyCode == Keys.F12)
                    MakeScreenshot(Application.StartupPath);
            };

            SizeChanged += (o, e) =>
            {
                // Dispose old resources.
                if (renderTargetView != null)
                    renderTargetView.Dispose();
                if (backBufferTexture != null)
                    backBufferTexture.Dispose();

                // Resize buffers.
                swapChain.ResizeBuffers(1, ClientSize.Width, ClientSize.Height, Format.R8G8B8A8_UNorm, SwapChainFlags.None);

                InitializeOutputMerger();

                camera.UpdateProjection();

                postProcess.Initialize(ClientSize.Width, ClientSize.Height);
            };

            fillMode = FillMode.Solid;
            InitializeOutputMerger();

            // Initializes input devices.
            DirectInput directInput = new DirectInput();
            keyboard = new Keyboard(directInput);
            keyboard.Acquire();
            mouse = new Mouse(directInput);
            mouse.Acquire();

            camera = new Camera(graphicsDevice, new Vector3(50, 50, 50), new Vector3(0, 0, 0), 0.1f, 1000.0f);
            textures = new List<TexturePack>();
            postProcess = new PostProcess(graphicsDevice, ClientSize.Width, ClientSize.Height);
            quadRenderer = new QuadRenderer(graphicsDevice);
        }

        /// <summary>
        /// Sets original render target conntected with back buffer.
        /// </summary>
        public void SetBackBufferRenderTarget()
        {
            graphicsDevice.ImmediateContext.OutputMerger.SetTargets(depthStencilView, renderTargetView);
        }

        /// <summary>
        /// Initializes various graphics device resources.
        /// </summary>
        private void InitializeOutputMerger()
        {
            backBufferTexture = Texture2D.FromSwapChain<Texture2D>(swapChain, 0);
            renderTargetView = new RenderTargetView(graphicsDevice, backBufferTexture);

            SetDepthStencilView();
            SetRasterizerState();

            graphicsDevice.ImmediateContext.Rasterizer.SetViewports(new Viewport(0, 0, ClientSize.Width, ClientSize.Height, 0, 1));
            graphicsDevice.ImmediateContext.OutputMerger.SetTargets(depthStencilView, renderTargetView);
        }

        /// <summary>
        /// Makes screenshot and saves it in specified directory.
        /// </summary>
        /// <param name="directory">Directory path.</param>
        private void MakeScreenshot(string directory)
        {
            string path;
            int k = 0;
            do
            {
                path = directory + @"\" + DateTime.Now.ToLocalTime().ToString().Replace(':', '-') + " " + k + ".png";
                k++;
            } while (File.Exists(path));

            Texture2D.SaveTextureToFile(graphicsDevice.ImmediateContext, backBufferTexture, ImageFileFormat.Png, path);
        }

        /// <summary>
        /// Sets depth stencil view.
        /// </summary>
        private void SetDepthStencilView()
        {
            if (depthTexture != null)
                depthTexture.Dispose();

            Texture2DDescription depthBufferDescription = new Texture2DDescription()
            {
                ArraySize = 1,
                BindFlags = BindFlags.DepthStencil,
                CpuAccessFlags = CpuAccessFlags.None,
                Format = Format.D24_UNorm_S8_UInt,
                Height = ClientSize.Height,
                MipLevels = 1,
                OptionFlags = ResourceOptionFlags.None,
                SampleDescription = new SampleDescription(1, 0),
                Usage = ResourceUsage.Default,
                Width = ClientSize.Width
            };

            depthTexture = new Texture2D(graphicsDevice, depthBufferDescription);

            DepthStencilStateDescription stencilDescription = new DepthStencilStateDescription()
            {
                BackFace = new DepthStencilOperationDescription()
                {
                    Comparison = Comparison.Always,
                    DepthFailOperation = StencilOperation.Decrement,
                    FailOperation = StencilOperation.Keep,
                    PassOperation = StencilOperation.Keep
                },
                DepthComparison = Comparison.Less,
                DepthWriteMask = DepthWriteMask.All,
                FrontFace = new DepthStencilOperationDescription()
                {
                    Comparison = Comparison.Always,
                    DepthFailOperation = StencilOperation.Increment,
                    FailOperation = StencilOperation.Keep,
                    PassOperation = StencilOperation.Keep
                },
                IsDepthEnabled = true,
                IsStencilEnabled = true,
                StencilReadMask = byte.MaxValue,
                StencilWriteMask = byte.MaxValue
            };

            DepthStencilState depthStencilState = DepthStencilState.FromDescription(graphicsDevice, stencilDescription);

            graphicsDevice.ImmediateContext.OutputMerger.DepthStencilState = depthStencilState;
            graphicsDevice.ImmediateContext.OutputMerger.DepthStencilReference = 1;

            DepthStencilViewDescription depthStencilViewDescription = new DepthStencilViewDescription()
            {
                ArraySize = 1,
                Dimension = DepthStencilViewDimension.Texture2D,
                FirstArraySlice = 0,
                Flags = DepthStencilViewFlags.None,
                Format = Format.D24_UNorm_S8_UInt,
                MipSlice = 0
            };

            if (depthStencilView != null)
                depthStencilView.Dispose();

            depthStencilView = new DepthStencilView(graphicsDevice, depthTexture, depthStencilViewDescription);
        }

        /// <summary>
        /// Sets rasterizer state.
        /// </summary>
        protected void SetRasterizerState()
        {
            RasterizerStateDescription rasterizerStateDescription = new RasterizerStateDescription()
            {
                CullMode = CullMode.None,
                DepthBias = 0,
                DepthBiasClamp = 0.0f,
                FillMode = fillMode,
                IsAntialiasedLineEnabled = false,
                IsDepthClipEnabled = true,
                IsFrontCounterclockwise = false,
                IsMultisampleEnabled = false,
                IsScissorEnabled = false,
                SlopeScaledDepthBias = 0.0f
            };

            RasterizerState rasterizerState = RasterizerState.FromDescription(graphicsDevice, rasterizerStateDescription);

            graphicsDevice.ImmediateContext.Rasterizer.State = rasterizerState;
        }

        /// <summary>
        /// Shows frame on the screen.
        /// </summary>
        protected void ShowFrame()
        {
            swapChain.Present(0, PresentFlags.None);
        }

        /// <summary>
        /// Loads texture from file.
        /// </summary>
        /// <param name="path">File path.</param>
        /// <returns>Loaded texture.</returns>
        protected ShaderResourceView LoadTexture(string path)
        {
            return ShaderResourceView.FromFile(graphicsDevice, path);
        }

        /// <summary>
        /// Removes all unmanaged objects loaded by SlimDX.
        /// </summary>
        public void DisposeFramework()
        {
            foreach (ComObject instance in ObjectTable.Objects)
            {
                ObjectTable.Remove(instance);
            }
        }

        /// <summary>
        /// Sets rendering fill mode.
        /// </summary>
        /// <param name="fillMode">Desired fill mode.</param>
        public void SetFillMode(FillMode fillMode)
        {
            this.fillMode = fillMode;
            SetRasterizerState();
        }

        /// <summary>
        /// Clears render target and depth views with specified color.
        /// </summary>
        /// <param name="color">Clear color.</param>
        public void ClearScreen(Color4 color)
        {
            graphicsDevice.ImmediateContext.ClearRenderTargetView(renderTargetView, color);
            graphicsDevice.ImmediateContext.ClearDepthStencilView(depthStencilView, DepthStencilClearFlags.Depth, 1.0f, 0);
        }

        /// <summary>
        /// Initializes application.
        /// </summary>
        public virtual void Initialize()
        {

        }

        /// <summary>
        /// Updates frame logic.
        /// </summary>
        public virtual void UpdateFrame(double deltaTime)
        {

        }

        /// <summary>
        /// Checks keyboard and mouse input.
        /// </summary>
        public virtual void CheckInput(double deltaTime)
        {
            IList<Key> pressedKeys = keyboard.GetCurrentState().PressedKeys;

            if (pressedKeys.Contains(Key.W))
                camera.MovePosition(MoveDirection.Forward, deltaTime);
            else if (pressedKeys.Contains(Key.S))
                camera.MovePosition(MoveDirection.Backward, deltaTime);
            if (pressedKeys.Contains(Key.A))
                camera.MovePosition(MoveDirection.Left, deltaTime);
            else if (pressedKeys.Contains(Key.D))
                camera.MovePosition(MoveDirection.Right, deltaTime);

            MouseState firstMouseState = mouse.GetCurrentState();
            MouseState secondMouseState = mouse.GetCurrentState();

            if (firstMouseState.IsPressed(0) && secondMouseState.IsPressed(0))
            {
                int differenceX = firstMouseState.X - secondMouseState.X;
                int differenceY = firstMouseState.Y - secondMouseState.Y;
                camera.MoveLook(new Vector2(differenceX, differenceY), 1.0f);
            }
        }

        /// <summary>
        /// Renders frame on the screen.
        /// </summary>
        public virtual void RenderFrame(double deltaTime)
        {

        }
    }
}
