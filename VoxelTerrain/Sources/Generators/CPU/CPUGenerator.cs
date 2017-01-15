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
    class CPUGenerator : IGenerate
    {
        /// <summary>
        /// Virtual adapter used to perform rendering.
        /// </summary>
        private Device graphicsDevice;

        private VoxelMeshContainer container;

        /// <summary>
        /// Poisson disc samples used in ambient occlusion computing.
        /// </summary>
        private static Vector3[] poissonDisc =
		{
			new Vector3(0.7768153f, 0.3749168f, -0.5059598f),
			new Vector3(0.08306061f, 0.9473661f, -0.3091901f),
			new Vector3(0.6623104f, 0.7395632f, 0.1199641f),
			new Vector3(0.9948989f, 0.0497775f, 0.08774123f),
			new Vector3(0.104239f, 0.2789151f, -0.9546416f),
			new Vector3(0.5960904f, 0.01746058f, -0.8027275f),
			new Vector3(0.4458466f, 0.1886109f, 0.8750125f),
			new Vector3(-0.07843895f, 0.4710891f, 0.8785911f),
			new Vector3(-0.3749092f, 0.9266203f, 0.02859987f),
			new Vector3(0.1367656f, 0.9223449f, 0.3613518f),
			new Vector3(0.8283083f, 0.01616495f, 0.5600392f),
			new Vector3(-0.607545f, 0.08607148f, 0.7896079f),
			new Vector3(-0.8451187f, 0.3715429f, 0.384357f),
			new Vector3(-0.9599981f, 0.1915317f, -0.2042528f),
			new Vector3(-0.3972329f, 0.09472971f, -0.9128156f),
			new Vector3(-0.6823229f, 0.4382687f, -0.585112f),
			new Vector3(0.2192558f, -0.6357883f, -0.7400677f),
			new Vector3(0.78632f, -0.4650563f, -0.406723f),
			new Vector3(0.1986186f, -0.9779121f, 0.065104f),
			new Vector3(-0.5403743f, -0.5149913f, -0.6654168f),
			new Vector3(-0.4253772f, -0.9048665f, 0.0164598f),
			new Vector3(0.642599f, -0.01937828f, -0.7659575f),
			new Vector3(-0.2056696f, -0.1125926f, -0.9721229f),
			new Vector3(0.9760811f, -0.1599507f, 0.1472464f),
			new Vector3(0.2192492f, -0.03539763f, -0.9750266f),
			new Vector3(0.5748524f, -0.5088353f, 0.6408051f),
			new Vector3(-0.1517765f, -0.2752149f, 0.9493264f),
			new Vector3(-0.5343058f, -0.5594884f, 0.6336324f),
			new Vector3(-0.9227477f, -0.1243654f, 0.3647878f),
			new Vector3(-0.9137639f, -0.3176999f, -0.2531843f),
			new Vector3(0.4282694f, -0.02752976f, 0.9032317f),
			new Vector3(0.8152075f, -0.0009064535f, 0.5791682f)
		};

        private bool isDisposed = false;

        public CPUGenerator(Device graphicsDevice, VoxelMeshContainer container)
        {
            this.graphicsDevice = graphicsDevice;
            this.container = container;
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!isDisposed)
            {
                if (disposing)
                {

                }
            }

            isDisposed = true;
        }

        ~CPUGenerator()
        {
            Dispose(false);
        }

        /// <summary>
        /// Generates mesh using mathematical functions.
        /// </summary>
        /// <param name="width">Mesh width.</param>
        /// <param name="height">Mesh height.</param>
        /// <param name="depth">Mesh depth.</param>
        public void GenerateFromFormula(int width, int height, int depth)
        {
            Voxel[, ,] voxels = new Voxel[width, height, depth];

            float area = (float)Math.Sqrt(width * depth);

            Vector3 center = new Vector3(width / 2.0f, height / 2.0f, depth / 2.0f);

            Vector3[] pillars = new Vector3[]
			{
				new Vector3(width / 4.0f, 0, depth / 4.0f),
				new Vector3(width * 3.0f / 4.0f, 0, depth * 3.0f / 4.0f),
				new Vector3(width * 2.0f / 4.0f, 0, depth / 4.0f)
			};

            NoiseCube n1 = new NoiseCube(16, 16, 16);
            NoiseCube n2 = new NoiseCube(16, 16, 16);
            NoiseCube n3 = new NoiseCube(16, 16, 16);
            NoiseCube n4 = new NoiseCube(16, 16, 16);

            Parallel.For(0, width, (x) =>
            {
                for (int y = 0; y < height; y++)
                {
                    for (int z = 0; z < depth; z++)
                    {
                        Vector3 position = new Vector3(x, y, z);
                        float weight = 0;

                        float distanceFromCenter = new Vector2(x - center.X, z - center.Z).Length();
                        distanceFromCenter = distanceFromCenter < 0.1f ? 0.1f : distanceFromCenter;

                        // Create pillars.
                        for (int i = 0; i < pillars.Length; i++)
                        {
                            float distance = new Vector2(x - pillars[i].X, z - pillars[i].Z).Length();
                            distance = distance < 0.1f ? 0.1f : distance;
                            weight += area / distance;
                        }

                        // Subtract values near center.
                        weight -= area / distanceFromCenter;

                        // Subtract big values at outer edge.
                        weight -= (float)Math.Pow(distanceFromCenter, 3) / (float)Math.Pow(area, 1.5f);

                        // Create helix.
                        double coordinate = 3 * Math.PI * y / height;
                        Vector2 helix = new Vector2((float)Math.Cos(coordinate), (float)Math.Sin(coordinate));
                        weight += Vector2.Dot(helix, new Vector2(x - center.X, z - center.Z));

                        // Create shelves.
                        weight += 10 * (float)Math.Cos(coordinate * 4 / 3);

                        // Add a little randomness.
                        weight += n1.GetInterpolatedValue(x / 32.04, y / 32.01, z / 31.97) * 8.0f;
                        weight += n2.GetInterpolatedValue(x / 8.01, y / 7.96, z / 7.98) * 4.0f;
                        weight += n3.GetInterpolatedValue(x / 4.01, y / 4.04, z / 3.96) * 2.0f;
                        weight += n4.GetInterpolatedValue(x / 2.02, y / 1.98, z / 1.97) * 1.0f;

                        voxels[x, y, z] = new Voxel()
                        {
                            Position = position,
                            Weight = weight
                        };
                    }
                }
            });

            ComputeNormal(voxels);
            ComputeAmbient(voxels);
            CreateGeometryBuffer(ComputeTriangles(voxels, container.Settings.LevelOfDetail));
        }

        /// <summary>
        /// Generates mesh using some random values interpolated with trilinear interpolation.
        /// </summary>
        /// <param name="width">Mesh widht.</param>
        /// <param name="height">Mesh height.</param>
        /// <param name="depth">Mesh depth.</param>
        public void GenerateFromNoiseCube(int width, int height, int depth)
        {
            Voxel[, ,] voxels = new Voxel[width, height, depth];

            Vector3 center = new Vector3(width / 2, height / 2, depth / 2);

            NoiseCube n1 = new NoiseCube(16, 16, 16);
            NoiseCube n2 = new NoiseCube(16, 16, 16);
            NoiseCube n3 = new NoiseCube(16, 16, 16);
            NoiseCube n4 = new NoiseCube(16, 16, 16);

            Parallel.For(0, width, (x) =>
            {
                for (int y = 0; y < height; y++)
                {
                    for (int z = 0; z < depth; z++)
                    {
                        Vector3 position = new Vector3(x, y, z);
                        float weight = center.Y - y;

                        weight += n1.GetInterpolatedValue(x / 32.04, y / 32.01, z / 31.97) * 64.0f;
                        weight += n2.GetInterpolatedValue(x / 8.01, y / 7.96, z / 7.98) * 4.0f;
                        weight += n3.GetInterpolatedValue(x / 4.01, y / 4.04, z / 3.96) * 2.0f;
                        weight += n4.GetInterpolatedValue(x / 2.02, y / 1.98, z / 1.97) * 1.0f;

                        voxels[x, y, z] = new Voxel()
                        {
                            Position = position,
                            Weight = weight
                        };
                    }
                }
            });

            ComputeNormal(voxels);
            ComputeAmbient(voxels);
            CreateGeometryBuffer(ComputeTriangles(voxels, container.Settings.LevelOfDetail));
        }

        /// <summary>
        /// Generates mesh using some random values interpolated with trilinear interpolation.
        /// Uses coordinates transformed with warp values.
        /// </summary>
        /// <param name="width">Mesh widht.</param>
        /// <param name="height">Mesh height.</param>
        /// <param name="depth">Mesh depth.</param>
        public void GenerateFromNoiseCubeWithWarp(int width, int height, int depth)
        {
            Voxel[, ,] voxels = new Voxel[width, height, depth];

            Vector3 center = new Vector3(width / 2, height / 2, depth / 2);

            NoiseCube n0 = new NoiseCube(16, 16, 16);
            NoiseCube n1 = new NoiseCube(16, 16, 16);
            NoiseCube n2 = new NoiseCube(16, 16, 16);
            NoiseCube n3 = new NoiseCube(16, 16, 16);
            NoiseCube n4 = new NoiseCube(16, 16, 16);

            Parallel.For(0, width, (x) =>
            {
                for (int y = 0; y < height; y++)
                {
                    for (int z = 0; z < depth; z++)
                    {
                        Vector3 position = new Vector3(x, y, z);
                        float weight = center.Y - y;

                        float warp = n0.GetInterpolatedValue(x * 0.004, y * 0.004, z * 0.004);
                        float cx = x + warp * 8;
                        float cy = y + warp * 8;
                        float cz = z + warp * 8;

                        weight += n1.GetInterpolatedValue(cx / 32.04, cy / 32.01, cz / 31.97) * 64.0f;
                        weight += n2.GetInterpolatedValue(cx / 8.01, cy / 7.96, cz / 7.98) * 4.0f;
                        weight += n3.GetInterpolatedValue(cx / 4.01, cy / 4.04, cz / 3.96) * 2.0f;
                        weight += n4.GetInterpolatedValue(cx / 2.02, cy / 1.98, cz / 1.97) * 1.0f;

                        voxels[x, y, z] = new Voxel()
                        {
                            Position = position,
                            Weight = weight
                        };
                    }
                }
            });

            ComputeNormal(voxels);
            ComputeAmbient(voxels);
            CreateGeometryBuffer(ComputeTriangles(voxels, container.Settings.LevelOfDetail));
        }

        /// <summary>
        /// Computes ambient color for each voxel.
        /// </summary>
        /// <param name="voxels">Array of voxels.</param>
        private void ComputeAmbient(Voxel[, ,] voxels)
        {
            int width = voxels.GetLength(0);
            int height = voxels.GetLength(1);
            int depth = voxels.GetLength(2);

            float stepLength = (width * container.Settings.AmbientRayWidth / 100.0f) / container.Settings.AmbientSamplesCount;

            Parallel.For(0, width, (x) =>
            {
                for (int y = 0; y < height; y++)
                {
                    for (int z = 0; z < depth; z++)
                    {
                        float ambient = 0;
                        Vector3 position = voxels[x, y, z].Position;

                        for (int i = 0; i < poissonDisc.Length; i++)
                        {
                            float sample = 0;

                            for (int j = 0; j < container.Settings.AmbientSamplesCount; j++)
                            {
                                // Ray starting point is situated in a small distance from center to avoid rendering artifacts.
                                int stepNumber = j + 2;

                                int cx = (int)Helper.Clamp(position.X + stepNumber * stepLength * poissonDisc[i].X, 0, width - 1);
                                int cy = (int)Helper.Clamp(position.Y + stepNumber * stepLength * poissonDisc[i].Y, 0, height - 1);
                                int cz = (int)Helper.Clamp(position.Z + stepNumber * stepLength * poissonDisc[i].Z, 0, depth - 1);

                                sample += voxels[cx, cy, cz].Weight > 0 ? 0 : 1;
                            }

                            ambient += sample / container.Settings.AmbientSamplesCount;
                        }

                        voxels[x, y, z].Ambient = ambient / poissonDisc.Length;
                    }
                }
            });
        }

        /// <summary>
        /// Computes normal vectors based on weight of each voxel.
        /// </summary>
        /// <param name="voxels">Array of voxels.</param>
        private void ComputeNormal(Voxel[, ,] voxels)
        {
            int width = voxels.GetLength(0);
            int height = voxels.GetLength(1);
            int depth = voxels.GetLength(2);

            Parallel.For(0, width, (x) =>
            {
                for (int y = 0; y < height; y++)
                {
                    for (int z = 0; z < depth; z++)
                    {
                        int xi = x == width - 1 ? x : x + 1;
                        int xd = x == 0 ? x : x - 1;

                        int yi = y == height - 1 ? y : y + 1;
                        int yd = y == 0 ? y : y - 1;

                        int zi = z == depth - 1 ? z : z + 1;
                        int zd = z == 0 ? z : z - 1;

                        Vector3 normal = new Vector3()
                        {
                            X = voxels[xi, y, z].Weight - voxels[xd, y, z].Weight,
                            Y = voxels[x, yi, z].Weight - voxels[x, yd, z].Weight,
                            Z = voxels[x, y, zi].Weight - voxels[x, y, zd].Weight
                        };

                        normal = -normal;
                        normal.Normalize();

                        voxels[x, y, z].Normal = normal;
                    }
                }
            });
        }

        /// <summary>
        /// Creates shader vertex buffer based on computed triangles.
        /// </summary>
        /// <param name="triangles">Array of triangles.</param>
        private void CreateGeometryBuffer(List<VoxelMeshVertex> triangles)
        {
            container.VertexCount = triangles.Count;
            int bufferSize = Marshal.SizeOf(typeof(VoxelMeshVertex)) * container.VertexCount;

            if (bufferSize == 0)
                return;

            DataStream stream = new DataStream(bufferSize, true, true);
            stream.WriteRange(triangles.ToArray());
            stream.Position = 0;

            if (container.Geometry != null)
                container.Geometry.Dispose();

            container.Geometry = new Buffer(graphicsDevice, stream, new BufferDescription()
            {
                BindFlags = BindFlags.VertexBuffer,
                CpuAccessFlags = CpuAccessFlags.None,
                OptionFlags = ResourceOptionFlags.None,
                SizeInBytes = bufferSize,
                Usage = ResourceUsage.Default
            });

            stream.Dispose();
        }

        /// <summary>
        /// Computes triangle positions and normal vectors based on array of voxels.
        /// </summary>
        /// <param name="voxels">Array of voxels.</param>
        /// <param name="levelOfDetail">Lower value makes more triangles.</param>
        /// <returns>List of triangles.</returns>
        private List<VoxelMeshVertex> ComputeTriangles(Voxel[, ,] voxels, int levelOfDetail)
        {
            List<VoxelMeshVertex> triangles = new List<VoxelMeshVertex>();

            for (int x = 0; x < voxels.GetLength(0) - levelOfDetail; x += levelOfDetail)
            {
                for (int y = 0; y < voxels.GetLength(1) - levelOfDetail; y += levelOfDetail)
                {
                    for (int z = 0; z < voxels.GetLength(2) - levelOfDetail; z += levelOfDetail)
                    {
                        Voxel[] cubeVoxels = new Voxel[]
						{
						   voxels[x, y, z],
						   voxels[x, y, z + levelOfDetail],
						   voxels[x + levelOfDetail, y, z + levelOfDetail],
						   voxels[x + levelOfDetail, y, z],
						   voxels[x, y + levelOfDetail, z],
						   voxels[x, y + levelOfDetail, z + levelOfDetail],
						   voxels[x + levelOfDetail, y + levelOfDetail, z + levelOfDetail],
						   voxels[x + levelOfDetail, y + levelOfDetail, z]
						};

                        triangles.AddRange(VoxelCube.ComputeTriangles(cubeVoxels));
                    }
                }
            }

            return triangles;
        }
    }
}
