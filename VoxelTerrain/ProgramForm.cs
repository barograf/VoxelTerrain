using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using SlimDX;
using System.Drawing;
using System.Windows.Forms;
using SlimDX.Direct3D11;
using ManagedCuda;

namespace VoxelTerrain
{
    /// <summary>
    /// Contains main application skeleton.
    /// </summary>
    public partial class ProgramForm : FrameworkForm
    {
        /// <summary>
        /// Voxel terrain instance.
        /// </summary>
        private VoxelMesh voxelTerrain;

        /// <summary>
        /// Sky cube instance.
        /// </summary>
        private SkyCube skyCube;

        /// <summary>
        /// Indicates if mouse is over menu panel.
        /// </summary>
        private bool mouseAtMenu;

        /// <summary>
        /// Default constructor.
        /// </summary>
        public ProgramForm()
            : base()
        {
            InitializeComponent();
        }

        /// <summary>
        /// Default constructor.
        /// </summary>
        /// <param name="caption">Window caption string.</param>
        public ProgramForm(string caption)
            : base(caption)
        {
            InitializeComponent();
            Size = new Size(1024, 768);
            WindowState = FormWindowState.Maximized;
        }

        /// <summary>
        /// Initializes application.
        /// </summary>
        public override void Initialize()
        {
            base.Initialize();

            TexturePack pack = new TexturePack();
            pack.Color = LoadTexture(@"Textures\tex_detail_color.dds");
            pack.Bump = LoadTexture(@"Textures\tex_detail_bump.dds");
            textures.Add(pack);

            for (int i = 1; i <= 19; i++)
            {
                pack = new TexturePack();
                pack.Color = LoadTexture(@"Textures\tex_" + i + @"_color.dds");
                pack.Bump = LoadTexture(@"Textures\tex_" + i + @"_bump.dds");
                pack.Disp = LoadTexture(@"Textures\tex_" + i + @"_disp.dds");
                textures.Add(pack);
            }

            voxelTerrain = new VoxelMesh(graphicsDevice, camera);

            VoxelMeshSettings settings = voxelTerrain.Container.Settings;
            settings.DetailTexture = textures[0];
            settings.Texture1 = textures[1];
            settings.Texture2 = textures[2];
            settings.Texture3 = textures[3];
            voxelTerrain.Container.Settings = settings;

            skyCube = new SkyCube(graphicsDevice, camera);
        }

        /// <summary>
        /// Updates frame logic.
        /// </summary>
        public override void UpdateFrame(double deltaTime)
        {
            base.UpdateFrame(deltaTime);

            this.Text = "VoxelTerrain - FPS: " + ((int)(1 / deltaTime)).ToString("D");
        }

        /// <summary>
        /// Checks keyboard and mouse input.
        /// </summary>
        public override void CheckInput(double deltaTime)
        {
            if (!(mouseAtMenu && !swapChain.IsFullScreen))
                base.CheckInput(deltaTime);

            panelLeftMenu.Visible = mouseAtMenu = Cursor.Position.X - Location.X <= panelLeftMenu.Width;
        }

        /// <summary>
        /// Renders frame on the screen.
        /// </summary>
        public override void RenderFrame(double deltaTime)
        {
            base.RenderFrame(deltaTime);

            if (checkBoxUseEffects.Checked)
            {
                postProcess.GetRenderTarget(4).SetRenderTarget(depthStencilView);
                postProcess.GetRenderTarget(4).ClearRenderTarget(depthStencilView, new Color4(Color.Black));

                skyCube.Render();

                postProcess.GetRenderTarget(5).SetRenderTarget(depthStencilView);
                postProcess.GetRenderTarget(5).ClearRenderTarget(depthStencilView, new Color4(Color.Black));

                VoxelMeshSettings settings = voxelTerrain.Container.Settings;
                settings.FogTexture = postProcess.GetRenderTarget(4).GetShaderResourceView();
                voxelTerrain.Container.Settings = settings;

                voxelTerrain.Renderer.Render();

                SetBackBufferRenderTarget();

                quadRenderer.Render(postProcess.MakeBloomEffect(postProcess.AddFogTexture()), 1.0f);
            }
            else
            {
                ClearScreen(new Color4(Color.Black));

                skyCube.Render();
                voxelTerrain.Renderer.Render();
            }

            ShowFrame();
        }

        private void ProgramForm_Load(object sender, EventArgs e)
        {
            numericUpDownTexturePack1.Maximum = textures.Count - 1;
            numericUpDownTexturePack2.Maximum = textures.Count - 1;
            numericUpDownTexturePack3.Maximum = textures.Count - 1;
            numericUpDownTexturePackDetail.Maximum = textures.Count - 1;
            comboBoxTerrainType.SelectedIndex = 0;
            comboBoxComputingEngine.SelectedIndex = 0;
        }

        private void numericUpDownFieldOfView_ValueChanged(object sender, EventArgs e)
        {
            camera.FieldOfView = (float)numericUpDownFieldOfView.Value;
        }

        private void numericUpDownCameraSpeed_ValueChanged(object sender, EventArgs e)
        {
            camera.Speed = (float)numericUpDownCameraSpeed.Value;
        }

        private void numericUpDownTexturePack1_ValueChanged(object sender, EventArgs e)
        {
            VoxelMeshSettings settings = voxelTerrain.Container.Settings;
            settings.Texture1 = textures[(int)numericUpDownTexturePack1.Value];
            voxelTerrain.Container.Settings = settings;
        }

        private void numericUpDownTextureCoordinates1_ValueChanged(object sender, EventArgs e)
        {
            VoxelMeshSettings settings = voxelTerrain.Container.Settings;
            settings.TextureCoordinates.X = (float)numericUpDownTextureCoordinates1.Value;
            voxelTerrain.Container.Settings = settings;
        }

        private void numericUpDownTexturePack2_ValueChanged(object sender, EventArgs e)
        {
            VoxelMeshSettings settings = voxelTerrain.Container.Settings;
            settings.Texture2 = textures[(int)numericUpDownTexturePack2.Value];
            voxelTerrain.Container.Settings = settings;
        }

        private void numericUpDownTextureCoordinates2_ValueChanged(object sender, EventArgs e)
        {
            VoxelMeshSettings settings = voxelTerrain.Container.Settings;
            settings.TextureCoordinates.Y = (float)numericUpDownTextureCoordinates2.Value;
            voxelTerrain.Container.Settings = settings;
        }

        private void numericUpDownTexturePack3_ValueChanged(object sender, EventArgs e)
        {
            VoxelMeshSettings settings = voxelTerrain.Container.Settings;
            settings.Texture3 = textures[(int)numericUpDownTexturePack3.Value];
            voxelTerrain.Container.Settings = settings;
        }

        private void numericUpDownTextureCoordinates3_ValueChanged(object sender, EventArgs e)
        {
            VoxelMeshSettings settings = voxelTerrain.Container.Settings;
            settings.TextureCoordinates.Z = (float)numericUpDownTextureCoordinates3.Value;
            voxelTerrain.Container.Settings = settings;
        }

        private void numericUpDownTexturePackDetail_ValueChanged(object sender, EventArgs e)
        {
            VoxelMeshSettings settings = voxelTerrain.Container.Settings;
            settings.DetailTexture = textures[(int)numericUpDownTexturePackDetail.Value];
            voxelTerrain.Container.Settings = settings;
        }

        private void numericUpDownTextureCoordinatesDetail_ValueChanged(object sender, EventArgs e)
        {
            VoxelMeshSettings settings = voxelTerrain.Container.Settings;
            settings.TextureCoordinates.W = (float)numericUpDownTextureCoordinatesDetail.Value;
            voxelTerrain.Container.Settings = settings;
        }

        private void numericUpDownDispPower1_ValueChanged(object sender, EventArgs e)
        {
            VoxelMeshSettings settings = voxelTerrain.Container.Settings;
            settings.DisplacementPower.X = (float)numericUpDownDispPower1.Value;
            voxelTerrain.Container.Settings = settings;
        }

        private void numericUpDownDispPower2_ValueChanged(object sender, EventArgs e)
        {
            VoxelMeshSettings settings = voxelTerrain.Container.Settings;
            settings.DisplacementPower.Y = (float)numericUpDownDispPower2.Value;
            voxelTerrain.Container.Settings = settings;
        }

        private void numericUpDownDispPower3_ValueChanged(object sender, EventArgs e)
        {
            VoxelMeshSettings settings = voxelTerrain.Container.Settings;
            settings.DisplacementPower.Z = (float)numericUpDownDispPower3.Value;
            voxelTerrain.Container.Settings = settings;
        }

        private void checkBoxWireframe_CheckedChanged(object sender, EventArgs e)
        {
            SetFillMode(checkBoxWireframe.Checked ? FillMode.Wireframe : FillMode.Solid);
        }

        private void numericUpDownBumpPower1_ValueChanged(object sender, EventArgs e)
        {
            VoxelMeshSettings settings = voxelTerrain.Container.Settings;
            settings.BumpPower.X = (float)numericUpDownBumpPower1.Value;
            voxelTerrain.Container.Settings = settings;
        }

        private void numericUpDownBumpPower2_ValueChanged(object sender, EventArgs e)
        {
            VoxelMeshSettings settings = voxelTerrain.Container.Settings;
            settings.BumpPower.Y = (float)numericUpDownBumpPower2.Value;
            voxelTerrain.Container.Settings = settings;
        }

        private void numericUpDownBumpPower3_ValueChanged(object sender, EventArgs e)
        {
            VoxelMeshSettings settings = voxelTerrain.Container.Settings;
            settings.BumpPower.Z = (float)numericUpDownBumpPower3.Value;
            voxelTerrain.Container.Settings = settings;
        }

        private void numericUpDownBumpPowerDetail_ValueChanged(object sender, EventArgs e)
        {
            VoxelMeshSettings settings = voxelTerrain.Container.Settings;
            settings.BumpPower.W = (float)numericUpDownBumpPowerDetail.Value;
            voxelTerrain.Container.Settings = settings;
        }

        private void numericUpDownTessellationMinimum_ValueChanged(object sender, EventArgs e)
        {
            VoxelMeshSettings settings = voxelTerrain.Container.Settings;
            settings.TessellationFactor.X = (float)numericUpDownTessellationMinimum.Value;
            voxelTerrain.Container.Settings = settings;
        }

        private void numericUpDownTessellationMaximum_ValueChanged(object sender, EventArgs e)
        {
            VoxelMeshSettings settings = voxelTerrain.Container.Settings;
            settings.TessellationFactor.Y = (float)numericUpDownTessellationMaximum.Value;
            voxelTerrain.Container.Settings = settings;
        }

        private void numericUpDownTessellationMinimumDistance_ValueChanged(object sender, EventArgs e)
        {
            if (numericUpDownTessellationMaximumDistance.Value < numericUpDownTessellationMinimumDistance.Value)
                numericUpDownTessellationMaximumDistance.Value = numericUpDownTessellationMinimumDistance.Value;

            VoxelMeshSettings settings = voxelTerrain.Container.Settings;
            settings.TessellationFactor.Z = (float)numericUpDownTessellationMinimumDistance.Value;
            voxelTerrain.Container.Settings = settings;
        }

        private void numericUpDownTessellationMaximumDistance_ValueChanged(object sender, EventArgs e)
        {
            if (numericUpDownTessellationMaximumDistance.Value < numericUpDownTessellationMinimumDistance.Value)
                numericUpDownTessellationMaximumDistance.Value = numericUpDownTessellationMinimumDistance.Value;

            VoxelMeshSettings settings = voxelTerrain.Container.Settings;
            settings.TessellationFactor.W = (float)numericUpDownTessellationMaximumDistance.Value;
            voxelTerrain.Container.Settings = settings;
        }

        private void buttonTerrainGenerate_Click(object sender, EventArgs e)
        {
            int width = (int)numericUpDownTerrainWidth.Value;
            int height = (int)numericUpDownTerrainHeight.Value;
            int depth = (int)numericUpDownTerrainDepth.Value;

            VoxelMeshSettings settings = voxelTerrain.Container.Settings;
            settings.LevelOfDetail = (int)numericUpDownTerrainLevelOfDetail.Value;
            voxelTerrain.Container.Settings = settings;

            switch (comboBoxTerrainType.SelectedIndex)
            {
                case 0:
                    voxelTerrain.Generator.GenerateFromFormula(width, height, depth);
                    break;
                case 1:
                    voxelTerrain.Generator.GenerateFromNoiseCube(width, height, depth);
                    break;
                case 2:
                    voxelTerrain.Generator.GenerateFromNoiseCubeWithWarp(width, height, depth);
                    break;
            }
        }

        private void numericUpDownDiffuseIntensity1_ValueChanged(object sender, EventArgs e)
        {
            VoxelMeshSettings settings = voxelTerrain.Container.Settings;
            settings.DiffuseIntensity.X = (float)numericUpDownDiffuseIntensity1.Value;
            voxelTerrain.Container.Settings = settings;
        }

        private void numericUpDownDiffuseIntensity2_ValueChanged(object sender, EventArgs e)
        {
            VoxelMeshSettings settings = voxelTerrain.Container.Settings;
            settings.DiffuseIntensity.Y = (float)numericUpDownDiffuseIntensity2.Value;
            voxelTerrain.Container.Settings = settings;
        }

        private void numericUpDownDiffuseIntensity3_ValueChanged(object sender, EventArgs e)
        {
            VoxelMeshSettings settings = voxelTerrain.Container.Settings;
            settings.DiffuseIntensity.Z = (float)numericUpDownDiffuseIntensity3.Value;
            voxelTerrain.Container.Settings = settings;
        }

        private void numericUpDownSpecularIntensity1_ValueChanged(object sender, EventArgs e)
        {
            VoxelMeshSettings settings = voxelTerrain.Container.Settings;
            settings.SpecularIntensity.X = (float)numericUpDownSpecularIntensity1.Value;
            voxelTerrain.Container.Settings = settings;
        }

        private void numericUpDownSpecularIntensity2_ValueChanged(object sender, EventArgs e)
        {
            VoxelMeshSettings settings = voxelTerrain.Container.Settings;
            settings.SpecularIntensity.Y = (float)numericUpDownSpecularIntensity2.Value;
            voxelTerrain.Container.Settings = settings;
        }

        private void numericUpDownSpecularIntensity3_ValueChanged(object sender, EventArgs e)
        {
            VoxelMeshSettings settings = voxelTerrain.Container.Settings;
            settings.SpecularIntensity.Z = (float)numericUpDownSpecularIntensity3.Value;
            voxelTerrain.Container.Settings = settings;
        }

        private void numericUpDownSpecularRange1_ValueChanged(object sender, EventArgs e)
        {
            VoxelMeshSettings settings = voxelTerrain.Container.Settings;
            settings.SpecularRange.X = (float)numericUpDownSpecularRange1.Value;
            voxelTerrain.Container.Settings = settings;
        }

        private void numericUpDownSpecularRange2_ValueChanged(object sender, EventArgs e)
        {
            VoxelMeshSettings settings = voxelTerrain.Container.Settings;
            settings.SpecularRange.Y = (float)numericUpDownSpecularRange2.Value;
            voxelTerrain.Container.Settings = settings;
        }

        private void numericUpDownSpecularRange3_ValueChanged(object sender, EventArgs e)
        {
            VoxelMeshSettings settings = voxelTerrain.Container.Settings;
            settings.SpecularRange.Z = (float)numericUpDownSpecularRange3.Value;
            voxelTerrain.Container.Settings = settings;
        }

        private void numericUpDownLuminance_ValueChanged(object sender, EventArgs e)
        {
            postProcess.Settings.BloomSettings.X = (float)numericUpDownLuminance.Value;
        }

        private void numericUpDownMiddleGray_ValueChanged(object sender, EventArgs e)
        {
            postProcess.Settings.BloomSettings.Y = (float)numericUpDownMiddleGray.Value;
        }

        private void numericUpDownWhiteCutoff_ValueChanged(object sender, EventArgs e)
        {
            postProcess.Settings.BloomSettings.Z = (float)numericUpDownWhiteCutoff.Value;
        }

        private void numericUpDownGlowPower_ValueChanged(object sender, EventArgs e)
        {
            postProcess.Settings.BloomSettings.W = (float)numericUpDownGlowPower.Value;
        }

        private void numericUpDownFogMinimumDistance_ValueChanged(object sender, EventArgs e)
        {
            if (numericUpDownFogMaximumDistance.Value < numericUpDownFogMinimumDistance.Value)
                numericUpDownFogMaximumDistance.Value = numericUpDownFogMinimumDistance.Value;

            VoxelMeshSettings settings = voxelTerrain.Container.Settings;
            settings.FogSettings.X = (float)numericUpDownFogMinimumDistance.Value;
            voxelTerrain.Container.Settings = settings;
        }

        private void numericUpDownFogMaximumDistance_ValueChanged(object sender, EventArgs e)
        {
            if (numericUpDownFogMaximumDistance.Value < numericUpDownFogMinimumDistance.Value)
                numericUpDownFogMaximumDistance.Value = numericUpDownFogMinimumDistance.Value;

            VoxelMeshSettings settings = voxelTerrain.Container.Settings;
            settings.FogSettings.Y = (float)numericUpDownFogMaximumDistance.Value;
            voxelTerrain.Container.Settings = settings;
        }

        private void buttonGenerateSky_Click(object sender, EventArgs e)
        {
            skyCube.GenerateSky();
        }

        private void comboBoxComputingEngine_SelectedValueChanged(object sender, EventArgs e)
        {
            voxelTerrain.Generator.Dispose();

            switch (comboBoxComputingEngine.SelectedIndex)
            {
                case 0:
                    voxelTerrain.Generator = new CPUGenerator(graphicsDevice, voxelTerrain.Container);
                    break;
                case 1:
                    try
                    {
                        voxelTerrain.Generator = new CUDAGenerator(graphicsDevice, voxelTerrain.Container);
                    }
                    catch (CudaException) { }
                    break;
                case 2:
                    voxelTerrain.Generator = new DirectComputeGenerator(graphicsDevice, voxelTerrain.Container);
                    break;
            }
        }
    }
}
