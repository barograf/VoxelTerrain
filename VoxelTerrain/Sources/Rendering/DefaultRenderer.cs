using System;
using System.Collections.Generic;
using SlimDX.Direct3D11;
using Device = SlimDX.Direct3D11.Device;
using Buffer = SlimDX.Direct3D11.Buffer;
using SlimDX.D3DCompiler;
using SlimDX.DXGI;
using SlimDX;
using System.Runtime.InteropServices;
using System.Threading.Tasks;

namespace VoxelTerrain
{
    public class DefaultRenderer : IRender
    {
        /// <summary>
        /// Virtual adapter used to perform rendering.
        /// </summary>
        private Device graphicsDevice;

        /// <summary>
        /// Reference to camera, which is used in some computations.
        /// </summary>
        private Camera camera;

        /// <summary>
        /// Compiled shader used to render voxel terrain.
        /// </summary>
        private Effect shader;

        /// <summary>
        /// Geometry layout used in shader.
        /// </summary>
        private InputLayout layout;

        private VoxelMeshContainer container;

        public DefaultRenderer(Device graphicsDevice, Camera camera, VoxelMeshContainer container)
        {
            this.graphicsDevice = graphicsDevice;
            this.camera = camera;
            this.container = container;

#if DEBUG
            shader = new Effect(graphicsDevice, ShaderPrecompiler.PrecompileOrLoad(@"Shaders\VoxelMesh.hlsl", "fx_5_0", ShaderFlags.Debug, EffectFlags.None));
#else
            shader = new Effect(graphicsDevice, ShaderPrecompiler.PrecompileOrLoad(@"Shaders\VoxelMesh.hlsl", "fx_5_0", ShaderFlags.None, EffectFlags.None));
#endif

            layout = new InputLayout(graphicsDevice, shader.GetTechniqueByIndex(0).GetPassByIndex(0).Description.Signature, new[]
			{
				new InputElement("POSITION", 0, Format.R32G32B32_Float, 0, 0, InputClassification.PerVertexData, 0),
				new InputElement("NORMAL", 0, Format.R32G32B32_Float, 12, 0, InputClassification.PerVertexData, 0),
				new InputElement("AMBIENT", 0, Format.R32_Float, 24, 0, InputClassification.PerVertexData, 0)
			});
        }

        public void Render()
        {
            Viewport viewport = graphicsDevice.ImmediateContext.Rasterizer.GetViewports()[0];

            shader.GetVariableByName("xWorld").AsMatrix().SetMatrix(Matrix.Identity);
            shader.GetVariableByName("xView").AsMatrix().SetMatrix(camera.View);
            shader.GetVariableByName("xProjection").AsMatrix().SetMatrix(camera.Projection);
            shader.GetVariableByName("xCameraPosition").AsVector().Set(camera.Position);
            shader.GetVariableByName("xTexture1Color").AsResource().SetResource(container.Settings.Texture1.Color);
            shader.GetVariableByName("xTexture1Bump").AsResource().SetResource(container.Settings.Texture1.Bump);
            shader.GetVariableByName("xTexture1Disp").AsResource().SetResource(container.Settings.Texture1.Disp);
            shader.GetVariableByName("xTexture2Color").AsResource().SetResource(container.Settings.Texture2.Color);
            shader.GetVariableByName("xTexture2Bump").AsResource().SetResource(container.Settings.Texture2.Bump);
            shader.GetVariableByName("xTexture2Disp").AsResource().SetResource(container.Settings.Texture2.Disp);
            shader.GetVariableByName("xTexture3Color").AsResource().SetResource(container.Settings.Texture3.Color);
            shader.GetVariableByName("xTexture3Bump").AsResource().SetResource(container.Settings.Texture3.Bump);
            shader.GetVariableByName("xTexture3Disp").AsResource().SetResource(container.Settings.Texture3.Disp);
            shader.GetVariableByName("xTextureDetailColor").AsResource().SetResource(container.Settings.DetailTexture.Color);
            shader.GetVariableByName("xTextureDetailBump").AsResource().SetResource(container.Settings.DetailTexture.Bump);
            shader.GetVariableByName("xFrustumPlanes").AsVector().Set(camera.FrustumPlanes);
            shader.GetVariableByName("xDiffuseLight1").AsVector().Set(container.Settings.DiffuseLight1);
            shader.GetVariableByName("xDiffuseLight2").AsVector().Set(container.Settings.DiffuseLight2);
            shader.GetVariableByName("xDiffuseLight3").AsVector().Set(container.Settings.DiffuseLight3);
            shader.GetVariableByName("xTextureCoordinates").AsVector().Set(container.Settings.TextureCoordinates);
            shader.GetVariableByName("xDisplacementPower").AsVector().Set(container.Settings.DisplacementPower);
            shader.GetVariableByName("xColorizationTexture").AsResource().SetResource(container.Settings.ColorizationTexture);
            shader.GetVariableByName("xBumpPower").AsVector().Set(container.Settings.BumpPower);
            shader.GetVariableByName("xTessellationFactor").AsVector().Set(container.Settings.TessellationFactor);
            shader.GetVariableByName("xDiffuseIntensity").AsVector().Set(container.Settings.DiffuseIntensity);
            shader.GetVariableByName("xSpecularIntensity").AsVector().Set(container.Settings.SpecularIntensity);
            shader.GetVariableByName("xSpecularRange").AsVector().Set(container.Settings.SpecularRange);
            shader.GetVariableByName("xFogTexture").AsResource().SetResource(container.Settings.FogTexture);
            shader.GetVariableByName("xViewport").AsVector().Set(new Vector2(viewport.Width, viewport.Height));
            shader.GetVariableByName("xFogSettings").AsVector().Set(container.Settings.FogSettings);

            graphicsDevice.ImmediateContext.InputAssembler.InputLayout = layout;
            graphicsDevice.ImmediateContext.InputAssembler.PrimitiveTopology = PrimitiveTopology.PatchListWith3ControlPoints;
            graphicsDevice.ImmediateContext.InputAssembler.SetVertexBuffers(0, new VertexBufferBinding(container.Geometry, Marshal.SizeOf(typeof(VoxelMeshVertex)), 0));
            shader.GetTechniqueByIndex(0).GetPassByIndex(0).Apply(graphicsDevice.ImmediateContext);

            graphicsDevice.ImmediateContext.Draw(container.VertexCount, 0);
        }
    }
}
