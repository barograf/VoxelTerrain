using SlimDX.Direct3D11;

namespace VoxelTerrain
{
    /// <summary>
    /// Contains textures related to each other.
    /// </summary>
    public struct TexturePack
    {
        /// <summary>
        /// Color map.
        /// </summary>
        public ShaderResourceView Color;

        /// <summary>
        /// Bump map.
        /// </summary>
        public ShaderResourceView Bump;

        /// <summary>
        /// Displacement map.
        /// </summary>
        public ShaderResourceView Disp;
    }
}
