using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using SlimDX;
using SlimDX.Direct3D11;

namespace VoxelTerrain
{
    /// <summary>
    /// Contains variables used in voxel terrain shader program.
    /// </summary>
    public class VoxelMeshSettings
    {
        /// <summary>
        /// Contains settings of bump power used in a shader.
        /// X = first texture pack bump power
        /// Y = second texture pack bump power
        /// Z = third texture pack bump power
        /// W = detail texture pack bump power
        /// </summary>
        public Vector4 BumpPower;

        /// <summary>
        /// Defines density of each texture pack's coordinates.
        /// X = first texture pack texture coordinates
        /// Y = second texture pack texture coordinates
        /// Z = third texture pack texture coordinates
        /// W = detail texture pack texture coordinates
        /// </summary>
        public Vector4 TextureCoordinates;

        /// <summary>
        /// Defines displacement power for each texture pack.
        /// X = first texture pack displacement power
        /// Y = second texture pack displacement power
        /// Z = third texture pack displacement power
        /// </summary>
        public Vector3 DisplacementPower;

        /// <summary>
        /// First texture pack used in terrain shader.
        /// </summary>
        public TexturePack Texture1;

        /// <summary>
        /// Second texture pack used in terrain shader.
        /// </summary>
        public TexturePack Texture2;

        /// <summary>
        /// Third texture pack used in terrain shader.
        /// </summary>
        public TexturePack Texture3;

        /// <summary>
        /// Detail texture pack used in terrain shader.
        /// </summary>
        public TexturePack DetailTexture;

        /// <summary>
        /// Texture which is used to slightly colorize terrain to make it more varied.
        /// </summary>
        public ShaderResourceView ColorizationTexture;

        /// <summary>
        /// Texture with rendered fog. It needs to be updated every frame.
        /// </summary>
        public ShaderResourceView FogTexture;

        /// <summary>
        /// Number of samples used in ambient occlusion algorithm. More samples equals better quality.
        /// </summary>
        public int AmbientSamplesCount;

        /// <summary>
        /// Defines ray length as a percentage of terrain size. Samples used in ambient occlusion algorithm
        /// are placed in equal distances on that ray.
        /// </summary>
        public float AmbientRayWidth;

        /// <summary>
        /// Direction of first light used in a scene.
        /// </summary>
        public Vector3 DiffuseLight1;

        /// <summary>
        /// Direction of second light used in a scene.
        /// </summary>
        public Vector3 DiffuseLight2;

        /// <summary>
        /// Direction of third light used in a scene.
        /// </summary>
        public Vector3 DiffuseLight3;

        /// <summary>
        /// Vector with geometry tessellation settings.
        /// X = minimum tessellation factor
        /// Y = maximum tessellation factor
        /// Z = minimum tessellation distance
        /// W = maximum tessellation distance
        /// </summary>
        public Vector4 TessellationFactor;

        /// <summary>
        /// This variable indicates terrain quality. Higher value equals lower quality of a terrain.
        /// </summary>
        public int LevelOfDetail;

        /// <summary>
        /// Defines intensity of each light used in a scene.
        /// X = first light
        /// Y = second light
        /// Z = third light
        /// </summary>
        public Vector3 DiffuseIntensity;

        /// <summary>
        /// Defines specular intensity of each light used in a scene.
        /// X = first light
        /// Y = second light
        /// Z = third light
        /// </summary>
        public Vector3 SpecularIntensity;

        /// <summary>
        /// Defines specular range of each light used in a scene.
        /// X = first light
        /// Y = second light
        /// Z = third light
        /// </summary>
        public Vector3 SpecularRange;

        /// <summary>
        /// Contains linear fog settings.
        /// X = minimum fog distance
        /// Y = maximum fog distance
        /// </summary>
        public Vector2 FogSettings;

        public VoxelMeshSettings(Device graphicsDevice)
        {
            AmbientSamplesCount = 16;
            AmbientRayWidth = 20;
            DiffuseLight1 = new Vector3(-1, -1, -1);
            DiffuseLight2 = new Vector3(1, -1, -1);
            DiffuseLight3 = new Vector3(-1, -1, 1);
            TextureCoordinates = new Vector4(32, 32, 32, 1);
            DisplacementPower = new Vector3(1, 1, 1);
            ColorizationTexture = new NoiseCube(16, 16, 16).ToTexture3D(graphicsDevice);
            BumpPower = new Vector4(1, 1, 1, 1);
            TessellationFactor = new Vector4(1, 8, 15, 30);
            LevelOfDetail = 1;
            DiffuseIntensity = new Vector3(1, 1, 1);
            SpecularIntensity = new Vector3(1, 1, 1);
            SpecularRange = new Vector3(8, 8, 8);
            FogSettings = new Vector2(25, 100);
            Texture1 = new TexturePack();
            Texture2 = new TexturePack();
            Texture3 = new TexturePack();
            DetailTexture = new TexturePack();
            FogTexture = null;
        }
    }
}
