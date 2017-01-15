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
    public class VoxelMesh : IVoxelMesh
    {
        public IRender Renderer { get; set; }

        public IGenerate Generator { get; set; }

        public VoxelMeshContainer Container { get; set; }

        /// <summary>
        /// Initializes object.
        /// </summary>
        /// <param name="graphicsDevice">Virtual adapter used to perform rendering.</param>
        /// <param name="camera">Reference to camera, which is used in some computations.</param>
        public VoxelMesh(Device graphicsDevice, Camera camera)
        {
            Container = new VoxelMeshContainer
            {
                Settings = new VoxelMeshSettings(graphicsDevice)
            };

            Renderer = new DefaultRenderer(graphicsDevice, camera, Container);
            Generator = new CPUGenerator(graphicsDevice, Container);
        }
    }
}
