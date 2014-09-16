using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Buffer = SlimDX.Direct3D11.Buffer;

namespace VoxelTerrain
{
    public class VoxelMeshContainer
    {
        public VoxelMeshSettings Settings { get; set; }

        public int VertexCount { get; set; }

        public Buffer Geometry { get; set; }
    }
}
