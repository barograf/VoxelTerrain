using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace VoxelTerrain
{
    public struct DirectComputeConstantBuffer
    {
        public int Width;
        public int Height;
        public int Depth;
        public float AmbientRayWidth;
        public int NearestWidth;
        public int NearestHeight;
        public int NearestDepth;
        public int AmbientSamplesCount;
        public int Seed;
        public int PrefixSize;
        public int PrefixN;
        public int PrefixArrayLength;
    }
}
