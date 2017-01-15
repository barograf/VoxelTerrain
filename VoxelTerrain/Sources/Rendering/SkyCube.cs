using SlimDX.Direct3D11;
using SlimDX.D3DCompiler;
using SlimDX;

namespace VoxelTerrain
{
    /// <summary>
    /// This object can generate and render sky effect using perlin noise algorithm.
    /// </summary>
    public class SkyCube
    {
        /// <summary>
        /// Virtual adapter used to perform rendering.
        /// </summary>
        private Device graphicsDevice;

        /// <summary>
        /// Instance of a camera object.
        /// </summary>
        private Camera camera;

        /// <summary>
        /// Shader program used to perform rendering.
        /// </summary>
        private Effect shader;

        /// <summary>
        /// Set of noise textures used in perlin noise algorithm.
        /// </summary>
        private ShaderResourceView[] noiseTextures;

        /// <summary>
        /// Object's default constructor.
        /// </summary>
        /// <param name="graphicsDevice">Virtual adapter used to perform rendering.</param>
        /// <param name="camera">Instance of a camera object.</param>
        public SkyCube(Device graphicsDevice, Camera camera)
        {
            this.graphicsDevice = graphicsDevice;
            this.camera = camera;

            GenerateSky();

            shader = new Effect(graphicsDevice, ShaderPrecompiler.PrecompileOrLoad(@"Shaders\SkyCube.hlsl", "fx_5_0", ShaderFlags.None, EffectFlags.None));
        }

        /// <summary>
        /// Generates sky textures.
        /// </summary>
        public void GenerateSky()
        {
            if (noiseTextures != null)
                for (int i = 0; i < noiseTextures.Length; i++)
                    if (noiseTextures[i] != null)
                        noiseTextures[i].Dispose();

            noiseTextures = new ShaderResourceView[4];
            for (int i = 0; i < 4; i++)
                noiseTextures[i] = new NoiseCube(16, 16, 16).ToTexture3D(graphicsDevice);
        }

        /// <summary>
        /// Renders sky cube on the screen.
        /// </summary>
        public void Render()
        {
            shader.GetVariableByName("xNoiseTexture").AsResource().SetResourceArray(noiseTextures);
            shader.GetVariableByName("xWorld").AsMatrix().SetMatrix(Matrix.Scaling(256, 256, 256) * Matrix.Translation(camera.Position));
            shader.GetVariableByName("xView").AsMatrix().SetMatrix(camera.View);
            shader.GetVariableByName("xProjection").AsMatrix().SetMatrix(camera.Projection);

            graphicsDevice.ImmediateContext.InputAssembler.PrimitiveTopology = PrimitiveTopology.PointList;
            shader.GetTechniqueByIndex(0).GetPassByIndex(0).Apply(graphicsDevice.ImmediateContext);

            // It uses geometry shader to produce sky cube, so that no vertex buffer is needed.
            graphicsDevice.ImmediateContext.Draw(1, 0);
        }
    }
}
