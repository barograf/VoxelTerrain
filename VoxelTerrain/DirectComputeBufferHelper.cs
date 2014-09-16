using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Device = SlimDX.Direct3D11.Device;
using Buffer = SlimDX.Direct3D11.Buffer;
using MapFlags = SlimDX.Direct3D11.MapFlags;
using SlimDX.Direct3D11;
using SlimDX;
using System.Runtime.InteropServices;

namespace VoxelTerrain
{
    public static class DirectComputeBufferHelper
    {
        public static T[] CopyBuffer<T>(Device graphicsDevice, Resource source, int offset, int count) where T : struct
        {
            Buffer destination = new Buffer(graphicsDevice, new BufferDescription()
            {
                BindFlags = BindFlags.None,
                CpuAccessFlags = CpuAccessFlags.Read,
                OptionFlags = ResourceOptionFlags.None,
                Usage = ResourceUsage.Staging,
                SizeInBytes = (source as Buffer).Description.SizeInBytes,
                StructureByteStride = (source as Buffer).Description.StructureByteStride
            });

            graphicsDevice.ImmediateContext.CopyResource(source, destination);

            DataBox data = graphicsDevice.ImmediateContext.MapSubresource(destination, MapMode.Read, MapFlags.None);
            T[] result = new T[count];
            data.Data.Position = offset * Marshal.SizeOf(typeof(T));
            data.Data.ReadRange<T>(result, 0, count);
            graphicsDevice.ImmediateContext.UnmapSubresource(destination, 0);

            destination.Dispose();

            return result;
        }

        public static T[] CopyBuffer<T>(Device graphicsDevice, Resource source) where T : struct
        {
            return CopyBuffer<T>(graphicsDevice, source, 0, (source as Buffer).Description.SizeInBytes / (source as Buffer).Description.StructureByteStride);
        }
    }
}
