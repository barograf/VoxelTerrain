using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace VoxelTerrain
{
    public interface IVoxelMesh
    {
        IRender Renderer { get; set; }

        IGenerate Generator { get; set; }
    }
}
