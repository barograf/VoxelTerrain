using SlimDX;

namespace VoxelTerrain
{
    /// <summary>
    /// Represents a structure used to compute terrain geometry.
    /// </summary>
    public struct Voxel
    {
        /// <summary>
        /// Position of a voxel.
        /// </summary>
        public Vector3 Position;

        /// <summary>
        /// Normal vector of a voxel.
        /// </summary>
        public Vector3 Normal;

        /// <summary>
        /// Voxel ambient color.
        /// </summary>
        public float Ambient;

        /// <summary>
        /// Weight of a voxel.
        /// </summary>
        public float Weight;
    }
}
