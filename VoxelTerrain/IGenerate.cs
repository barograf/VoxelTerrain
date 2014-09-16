using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace VoxelTerrain
{
    public interface IGenerate : IDisposable
    {
        void GenerateFromNoiseCubeWithWarp(int width, int height, int depth);

        void GenerateFromNoiseCube(int width, int height, int depth);

        void GenerateFromFormula(int width, int height, int depth);
    }
}
