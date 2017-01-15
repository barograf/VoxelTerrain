using SlimDX;

namespace VoxelTerrain
{
    /// <summary>
    /// Represents a voxel mesh structure stored in vertex buffer.
    /// </summary>
    public struct VoxelMeshVertex
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
    }
}
