using SlimDX.Direct3D11;
using SlimDX.D3DCompiler;

namespace VoxelTerrain
{
    /// <summary>
    /// This object can render texture onto screen.
    /// </summary>
    public class QuadRenderer
    {
        /// <summary>
        /// Virtual adapter used to perform rendering.
        /// </summary>
        private Device graphicsDevice;

        /// <summary>
        /// Shader program of quad renderer.
        /// </summary>
        private Effect effect;

        /// <summary>
        /// Default constructor.
        /// </summary>
        /// <param name="graphicsDevice">Virtual adapter used to perform rendering.</param>
        public QuadRenderer(Device graphicsDevice)
        {
            this.graphicsDevice = graphicsDevice;

            effect = new Effect(graphicsDevice, ShaderPrecompiler.PrecompileOrLoad(@"Shaders\QuadRenderer.hlsl", "fx_5_0", ShaderFlags.None, EffectFlags.None));
        }

        /// <summary>
        /// Renders texture onto screen and scales it.
        /// </summary>
        /// <param name="texture">Texture to render onto screen.</param>
        /// <param name="scale">Scaling value.</param>
        public void Render(ShaderResourceView texture, float scale)
        {
            effect.GetVariableByName("xScale").AsScalar().Set(scale);
            effect.GetVariableByName("xTexture").AsResource().SetResource(texture);
            graphicsDevice.ImmediateContext.InputAssembler.PrimitiveTopology = PrimitiveTopology.TriangleStrip;
            effect.GetTechniqueByIndex(0).GetPassByIndex(0).Apply(graphicsDevice.ImmediateContext);
            
            graphicsDevice.ImmediateContext.Draw(4, 0);
        }
    }
}
