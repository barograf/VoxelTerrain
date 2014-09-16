using System;
using SlimDX.Direct3D11;
using SlimDX;
using SlimDX.DXGI;
using Device = SlimDX.Direct3D11.Device;

namespace VoxelTerrain
{
    /// <summary>
    /// Allows retrieving interpolated values from a three-dimensional noise table.
    /// </summary>
    public class NoiseCube
    {
        /// <summary>
        /// Width of a cube.
        /// </summary>
        private readonly int width;

        /// <summary>
        /// Height of a cube.
        /// </summary>
        private readonly int height;

        /// <summary>
        /// Depth of a cube.
        /// </summary>
        private readonly int depth;

        /// <summary>
        /// Three-dimensional table with random values.
        /// </summary>
        private float[, ,] values;

        /// <summary>
        /// Object which generates random numbers.
        /// </summary>
        private static Random random = new Random();

        /// <summary>
        /// Creates a cube with specified dimensions and randomizes all values.
        /// </summary>
        /// <param name="width">Width of a cube.</param>
        /// <param name="height">Height of a cube.</param>
        /// <param name="depth">Depth of a cube.</param>
        public NoiseCube(int width, int height, int depth)
        {
            this.width = width;
            this.height = height;
            this.depth = depth;
            values = new float[width, height, depth];

            RandomizeValues();
        }

        /// <summary>
        /// Randomizes all values in a cube.
        /// </summary>
        public void RandomizeValues()
        {
            for (int x = 0; x < width; x++)
            {
                for (int y = 0; y < height; y++)
                {
                    for (int z = 0; z < depth; z++)
                    {
                        values[x, y, z] = (float)random.NextDouble() * 2 - 1;
                    }
                }
            }
        }

        public ShaderResourceView ToTexture3D(Device graphicsDevice, Format format)
        {
            int sizeInBytes = sizeof(float) * width * height * depth;

            DataStream stream = new DataStream(sizeInBytes, true, true);
            for (int x = 0; x < width; x++)
                for (int y = 0; y < height; y++)
                    for (int z = 0; z < depth; z++)
                        stream.Write(values[x, y, z]);
            stream.Position = 0;

            DataBox dataBox = new DataBox(sizeof(float) * width, sizeof(float) * width * height, stream);
            Texture3DDescription description = new Texture3DDescription()
            {
                BindFlags = BindFlags.ShaderResource,
                CpuAccessFlags = CpuAccessFlags.None,
                Depth = depth,
                Format = format,
                Height = height,
                MipLevels = 1,
                OptionFlags = ResourceOptionFlags.None,
                Usage = ResourceUsage.Default,
                Width = width
            };
            Texture3D texture = new Texture3D(graphicsDevice, description, dataBox);

            stream.Dispose();

            return new ShaderResourceView(graphicsDevice, texture);
        }

        public ShaderResourceView ToTexture3D(Device graphicsDevice)
        {
            return ToTexture3D(graphicsDevice, Format.R8G8B8A8_UNorm);
        }

        /// <summary>
        /// Computes interpolated value of a noise using trilinear interpolation.
        /// </summary>
        /// <param name="x">Coordinate along width.</param>
        /// <param name="y">Coordinate along height.</param>
        /// <param name="z">Coordinate along depth.</param>
        /// <returns>Interpolated value.</returns>
        public float GetInterpolatedValue(double x, double y, double z)
        {
            int w = width - 1;
            int h = height - 1;
            int d = depth - 1;

            x = x < 0 ? x % w + w : x % w;
            y = y < 0 ? y % h + h : y % h;
            z = z < 0 ? z % d + d : z % d;

            int ix = (int)x;
            int iy = (int)y;
            int iz = (int)z;

            double dx = x - ix;
            double dy = y - iy;
            double dz = z - iz;

            int ixi = ix + 1;
            int iyi = iy + 1;
            int izi = iz + 1;

            ixi = ixi == width ? 0 : ixi;
            iyi = iyi == height ? 0 : iyi;
            izi = izi == depth ? 0 : izi;

            double c1 = values[ix, iy, iz];
            double c2 = values[ix, iy, izi];
            double c3 = values[ixi, iy, izi];
            double c4 = values[ixi, iy, iz];
            double c5 = values[ix, iyi, iz];
            double c6 = values[ix, iyi, izi];
            double c7 = values[ixi, iyi, izi];
            double c8 = values[ixi, iyi, iz];

            double c14 = c4 * dx + c1 * (1 - dx);
            double c23 = c3 * dx + c2 * (1 - dx);
            double c58 = c8 * dx + c5 * (1 - dx);
            double c67 = c7 * dx + c6 * (1 - dx);

            double c1423 = c23 * dz + c14 * (1 - dz);
            double c5867 = c67 * dz + c58 * (1 - dz);

            double result = c5867 * dy + c1423 * (1 - dy);

            return (float)result;
        }
    }
}
