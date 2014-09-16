#define BLOCK_DIM_X 8
#define BLOCK_DIM_Y 8
#define BLOCK_DIM_Z 8
#define UINT_MAX 4294967295.0f
#define POISSON_DISC_LEN 32
#define THREADBLOCK_SIZE 256
#define THREADBLOCK_SHARED_SIZE (2 * THREADBLOCK_SIZE)
#define PI 3.14159265f

struct Voxel
{
	float3 Position;
	float3 Normal;
	float Ambient;
	float Weight;
};

struct VoxelMeshVertex
{
	float3 Position;
	float3 Normal;
	float Ambient;
};

struct XorwowState
{
	uint d;
	uint x;
	uint y;
	uint z;
	uint w;
	uint v;
};

RWTexture3D<float> noiseTextureRW : register(u0);
Texture3D<float> noiseTexture : register(t0);

RWStructuredBuffer<Voxel> voxels : register(u0);

RWStructuredBuffer<int> offsets : register(u1);

RWStructuredBuffer<int> triangleCounts : register(u2);

RWStructuredBuffer<int> prefixSums : register(u3);
RWStructuredBuffer<int> prefixSumsBuffer : register(u4);

RWStructuredBuffer<VoxelMeshVertex> vertices : register(u5);

groupshared uint prefixShared[2 * THREADBLOCK_SIZE];
groupshared uint prefixBuffer;

static const float poissonDisc[POISSON_DISC_LEN][3] =
{
	{ 0.7768153f, 0.3749168f, -0.5059598f },
	{ 0.08306061f, 0.9473661f, -0.3091901 },
	{ 0.6623104f, 0.7395632f, 0.1199641 },
	{ 0.9948989f, 0.0497775f, 0.08774123 },
	{ 0.104239f, 0.2789151f, -0.9546416 },
	{ 0.5960904f, 0.01746058f, -0.8027275 },
	{ 0.4458466f, 0.1886109f, 0.8750125 },
	{ -0.07843895f, 0.4710891f, 0.8785911 },
	{ -0.3749092f, 0.9266203f, 0.02859987 },
	{ 0.1367656f, 0.9223449f, 0.3613518 },
	{ 0.8283083f, 0.01616495f, 0.5600392 },
	{ -0.607545f, 0.08607148f, 0.7896079 },
	{ -0.8451187f, 0.3715429f, 0.384357 },
	{ -0.9599981f, 0.1915317f, -0.2042528 },
	{ -0.3972329f, 0.09472971f, -0.9128156 },
	{ -0.6823229f, 0.4382687f, -0.585112 },
	{ 0.2192558f, -0.6357883f, -0.7400677 },
	{ 0.78632f, -0.4650563f, -0.406723 },
	{ 0.1986186f, -0.9779121f, 0.065104 },
	{ -0.5403743f, -0.5149913f, -0.6654168 },
	{ -0.4253772f, -0.9048665f, 0.0164598 },
	{ 0.642599f, -0.01937828f, -0.7659575 },
	{ -0.2056696f, -0.1125926f, -0.9721229 },
	{ 0.9760811f, -0.1599507f, 0.1472464 },
	{ 0.2192492f, -0.03539763f, -0.9750266 },
	{ 0.5748524f, -0.5088353f, 0.6408051 },
	{ -0.1517765f, -0.2752149f, 0.9493264 },
	{ -0.5343058f, -0.5594884f, 0.6336324 },
	{ -0.9227477f, -0.1243654f, 0.3647878 },
	{ -0.9137639f, -0.3176999f, -0.2531843 },
	{ 0.4282694f, -0.02752976f, 0.9032317 },
	{ 0.8152075f, -0.0009064535f, 0.5791682f }
};

static const int faces[] =
{
	-1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1,
	0, 8, 3, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1,
	0, 1, 9, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1,
	1, 8, 3, 9, 8, 1, -1, -1, -1, -1, -1, -1, -1, -1, -1,
	1, 2, 11, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1,
	0, 8, 3, 1, 2, 11, -1, -1, -1, -1, -1, -1, -1, -1, -1,
	9, 2, 11, 0, 2, 9, -1, -1, -1, -1, -1, -1, -1, -1, -1,
	2, 8, 3, 2, 11, 8, 11, 9, 8, -1, -1, -1, -1, -1, -1,
	3, 10, 2, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1,
	0, 10, 2, 8, 10, 0, -1, -1, -1, -1, -1, -1, -1, -1, -1,
	1, 9, 0, 2, 3, 10, -1, -1, -1, -1, -1, -1, -1, -1, -1,
	1, 10, 2, 1, 9, 10, 9, 8, 10, -1, -1, -1, -1, -1, -1,
	3, 11, 1, 10, 11, 3, -1, -1, -1, -1, -1, -1, -1, -1, -1,
	0, 11, 1, 0, 8, 11, 8, 10, 11, -1, -1, -1, -1, -1, -1,
	3, 9, 0, 3, 10, 9, 10, 11, 9, -1, -1, -1, -1, -1, -1,
	9, 8, 11, 11, 8, 10, -1, -1, -1, -1, -1, -1, -1, -1, -1,
	4, 7, 8, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1,
	4, 3, 0, 7, 3, 4, -1, -1, -1, -1, -1, -1, -1, -1, -1,
	0, 1, 9, 8, 4, 7, -1, -1, -1, -1, -1, -1, -1, -1, -1,
	4, 1, 9, 4, 7, 1, 7, 3, 1, -1, -1, -1, -1, -1, -1,
	1, 2, 11, 8, 4, 7, -1, -1, -1, -1, -1, -1, -1, -1, -1,
	3, 4, 7, 3, 0, 4, 1, 2, 11, -1, -1, -1, -1, -1, -1,
	9, 2, 11, 9, 0, 2, 8, 4, 7, -1, -1, -1, -1, -1, -1,
	2, 11, 9, 2, 9, 7, 2, 7, 3, 7, 9, 4, -1, -1, -1,
	8, 4, 7, 3, 10, 2, -1, -1, -1, -1, -1, -1, -1, -1, -1,
	10, 4, 7, 10, 2, 4, 2, 0, 4, -1, -1, -1, -1, -1, -1,
	9, 0, 1, 8, 4, 7, 2, 3, 10, -1, -1, -1, -1, -1, -1,
	4, 7, 10, 9, 4, 10, 9, 10, 2, 9, 2, 1, -1, -1, -1,
	3, 11, 1, 3, 10, 11, 7, 8, 4, -1, -1, -1, -1, -1, -1,
	1, 10, 11, 1, 4, 10, 1, 0, 4, 7, 10, 4, -1, -1, -1,
	4, 7, 8, 9, 0, 10, 9, 10, 11, 10, 0, 3, -1, -1, -1,
	4, 7, 10, 4, 10, 9, 9, 10, 11, -1, -1, -1, -1, -1, -1,
	9, 5, 4, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1,
	9, 5, 4, 0, 8, 3, -1, -1, -1, -1, -1, -1, -1, -1, -1,
	0, 5, 4, 1, 5, 0, -1, -1, -1, -1, -1, -1, -1, -1, -1,
	8, 5, 4, 8, 3, 5, 3, 1, 5, -1, -1, -1, -1, -1, -1,
	1, 2, 11, 9, 5, 4, -1, -1, -1, -1, -1, -1, -1, -1, -1,
	3, 0, 8, 1, 2, 11, 4, 9, 5, -1, -1, -1, -1, -1, -1,
	5, 2, 11, 5, 4, 2, 4, 0, 2, -1, -1, -1, -1, -1, -1,
	2, 11, 5, 3, 2, 5, 3, 5, 4, 3, 4, 8, -1, -1, -1,
	9, 5, 4, 2, 3, 10, -1, -1, -1, -1, -1, -1, -1, -1, -1,
	0, 10, 2, 0, 8, 10, 4, 9, 5, -1, -1, -1, -1, -1, -1,
	0, 5, 4, 0, 1, 5, 2, 3, 10, -1, -1, -1, -1, -1, -1,
	2, 1, 5, 2, 5, 8, 2, 8, 10, 4, 8, 5, -1, -1, -1,
	11, 3, 10, 11, 1, 3, 9, 5, 4, -1, -1, -1, -1, -1, -1,
	4, 9, 5, 0, 8, 1, 8, 11, 1, 8, 10, 11, -1, -1, -1,
	5, 4, 0, 5, 0, 10, 5, 10, 11, 10, 0, 3, -1, -1, -1,
	5, 4, 8, 5, 8, 11, 11, 8, 10, -1, -1, -1, -1, -1, -1,
	9, 7, 8, 5, 7, 9, -1, -1, -1, -1, -1, -1, -1, -1, -1,
	9, 3, 0, 9, 5, 3, 5, 7, 3, -1, -1, -1, -1, -1, -1,
	0, 7, 8, 0, 1, 7, 1, 5, 7, -1, -1, -1, -1, -1, -1,
	1, 5, 3, 3, 5, 7, -1, -1, -1, -1, -1, -1, -1, -1, -1,
	9, 7, 8, 9, 5, 7, 11, 1, 2, -1, -1, -1, -1, -1, -1,
	11, 1, 2, 9, 5, 0, 5, 3, 0, 5, 7, 3, -1, -1, -1,
	8, 0, 2, 8, 2, 5, 8, 5, 7, 11, 5, 2, -1, -1, -1,
	2, 11, 5, 2, 5, 3, 3, 5, 7, -1, -1, -1, -1, -1, -1,
	7, 9, 5, 7, 8, 9, 3, 10, 2, -1, -1, -1, -1, -1, -1,
	9, 5, 7, 9, 7, 2, 9, 2, 0, 2, 7, 10, -1, -1, -1,
	2, 3, 10, 0, 1, 8, 1, 7, 8, 1, 5, 7, -1, -1, -1,
	10, 2, 1, 10, 1, 7, 7, 1, 5, -1, -1, -1, -1, -1, -1,
	9, 5, 8, 8, 5, 7, 11, 1, 3, 11, 3, 10, -1, -1, -1,
	5, 7, 0, 5, 0, 9, 7, 10, 0, 1, 0, 11, 10, 11, 0,
	10, 11, 0, 10, 0, 3, 11, 5, 0, 8, 0, 7, 5, 7, 0,
	10, 11, 5, 7, 10, 5, -1, -1, -1, -1, -1, -1, -1, -1, -1,
	11, 6, 5, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1,
	0, 8, 3, 5, 11, 6, -1, -1, -1, -1, -1, -1, -1, -1, -1,
	9, 0, 1, 5, 11, 6, -1, -1, -1, -1, -1, -1, -1, -1, -1,
	1, 8, 3, 1, 9, 8, 5, 11, 6, -1, -1, -1, -1, -1, -1,
	1, 6, 5, 2, 6, 1, -1, -1, -1, -1, -1, -1, -1, -1, -1,
	1, 6, 5, 1, 2, 6, 3, 0, 8, -1, -1, -1, -1, -1, -1,
	9, 6, 5, 9, 0, 6, 0, 2, 6, -1, -1, -1, -1, -1, -1,
	5, 9, 8, 5, 8, 2, 5, 2, 6, 3, 2, 8, -1, -1, -1,
	2, 3, 10, 11, 6, 5, -1, -1, -1, -1, -1, -1, -1, -1, -1,
	10, 0, 8, 10, 2, 0, 11, 6, 5, -1, -1, -1, -1, -1, -1,
	0, 1, 9, 2, 3, 10, 5, 11, 6, -1, -1, -1, -1, -1, -1,
	5, 11, 6, 1, 9, 2, 9, 10, 2, 9, 8, 10, -1, -1, -1,
	6, 3, 10, 6, 5, 3, 5, 1, 3, -1, -1, -1, -1, -1, -1,
	0, 8, 10, 0, 10, 5, 0, 5, 1, 5, 10, 6, -1, -1, -1,
	3, 10, 6, 0, 3, 6, 0, 6, 5, 0, 5, 9, -1, -1, -1,
	6, 5, 9, 6, 9, 10, 10, 9, 8, -1, -1, -1, -1, -1, -1,
	5, 11, 6, 4, 7, 8, -1, -1, -1, -1, -1, -1, -1, -1, -1,
	4, 3, 0, 4, 7, 3, 6, 5, 11, -1, -1, -1, -1, -1, -1,
	1, 9, 0, 5, 11, 6, 8, 4, 7, -1, -1, -1, -1, -1, -1,
	11, 6, 5, 1, 9, 7, 1, 7, 3, 7, 9, 4, -1, -1, -1,
	6, 1, 2, 6, 5, 1, 4, 7, 8, -1, -1, -1, -1, -1, -1,
	1, 2, 5, 5, 2, 6, 3, 0, 4, 3, 4, 7, -1, -1, -1,
	8, 4, 7, 9, 0, 5, 0, 6, 5, 0, 2, 6, -1, -1, -1,
	7, 3, 9, 7, 9, 4, 3, 2, 9, 5, 9, 6, 2, 6, 9,
	3, 10, 2, 7, 8, 4, 11, 6, 5, -1, -1, -1, -1, -1, -1,
	5, 11, 6, 4, 7, 2, 4, 2, 0, 2, 7, 10, -1, -1, -1,
	0, 1, 9, 4, 7, 8, 2, 3, 10, 5, 11, 6, -1, -1, -1,
	9, 2, 1, 9, 10, 2, 9, 4, 10, 7, 10, 4, 5, 11, 6,
	8, 4, 7, 3, 10, 5, 3, 5, 1, 5, 10, 6, -1, -1, -1,
	5, 1, 10, 5, 10, 6, 1, 0, 10, 7, 10, 4, 0, 4, 10,
	0, 5, 9, 0, 6, 5, 0, 3, 6, 10, 6, 3, 8, 4, 7,
	6, 5, 9, 6, 9, 10, 4, 7, 9, 7, 10, 9, -1, -1, -1,
	11, 4, 9, 6, 4, 11, -1, -1, -1, -1, -1, -1, -1, -1, -1,
	4, 11, 6, 4, 9, 11, 0, 8, 3, -1, -1, -1, -1, -1, -1,
	11, 0, 1, 11, 6, 0, 6, 4, 0, -1, -1, -1, -1, -1, -1,
	8, 3, 1, 8, 1, 6, 8, 6, 4, 6, 1, 11, -1, -1, -1,
	1, 4, 9, 1, 2, 4, 2, 6, 4, -1, -1, -1, -1, -1, -1,
	3, 0, 8, 1, 2, 9, 2, 4, 9, 2, 6, 4, -1, -1, -1,
	0, 2, 4, 4, 2, 6, -1, -1, -1, -1, -1, -1, -1, -1, -1,
	8, 3, 2, 8, 2, 4, 4, 2, 6, -1, -1, -1, -1, -1, -1,
	11, 4, 9, 11, 6, 4, 10, 2, 3, -1, -1, -1, -1, -1, -1,
	0, 8, 2, 2, 8, 10, 4, 9, 11, 4, 11, 6, -1, -1, -1,
	3, 10, 2, 0, 1, 6, 0, 6, 4, 6, 1, 11, -1, -1, -1,
	6, 4, 1, 6, 1, 11, 4, 8, 1, 2, 1, 10, 8, 10, 1,
	9, 6, 4, 9, 3, 6, 9, 1, 3, 10, 6, 3, -1, -1, -1,
	8, 10, 1, 8, 1, 0, 10, 6, 1, 9, 1, 4, 6, 4, 1,
	3, 10, 6, 3, 6, 0, 0, 6, 4, -1, -1, -1, -1, -1, -1,
	6, 4, 8, 10, 6, 8, -1, -1, -1, -1, -1, -1, -1, -1, -1,
	7, 11, 6, 7, 8, 11, 8, 9, 11, -1, -1, -1, -1, -1, -1,
	0, 7, 3, 0, 11, 7, 0, 9, 11, 6, 7, 11, -1, -1, -1,
	11, 6, 7, 1, 11, 7, 1, 7, 8, 1, 8, 0, -1, -1, -1,
	11, 6, 7, 11, 7, 1, 1, 7, 3, -1, -1, -1, -1, -1, -1,
	1, 2, 6, 1, 6, 8, 1, 8, 9, 8, 6, 7, -1, -1, -1,
	2, 6, 9, 2, 9, 1, 6, 7, 9, 0, 9, 3, 7, 3, 9,
	7, 8, 0, 7, 0, 6, 6, 0, 2, -1, -1, -1, -1, -1, -1,
	7, 3, 2, 6, 7, 2, -1, -1, -1, -1, -1, -1, -1, -1, -1,
	2, 3, 10, 11, 6, 8, 11, 8, 9, 8, 6, 7, -1, -1, -1,
	2, 0, 7, 2, 7, 10, 0, 9, 7, 6, 7, 11, 9, 11, 7,
	1, 8, 0, 1, 7, 8, 1, 11, 7, 6, 7, 11, 2, 3, 10,
	10, 2, 1, 10, 1, 7, 11, 6, 1, 6, 7, 1, -1, -1, -1,
	8, 9, 6, 8, 6, 7, 9, 1, 6, 10, 6, 3, 1, 3, 6,
	0, 9, 1, 10, 6, 7, -1, -1, -1, -1, -1, -1, -1, -1, -1,
	7, 8, 0, 7, 0, 6, 3, 10, 0, 10, 6, 0, -1, -1, -1,
	7, 10, 6, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1,
	7, 6, 10, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1,
	3, 0, 8, 10, 7, 6, -1, -1, -1, -1, -1, -1, -1, -1, -1,
	0, 1, 9, 10, 7, 6, -1, -1, -1, -1, -1, -1, -1, -1, -1,
	8, 1, 9, 8, 3, 1, 10, 7, 6, -1, -1, -1, -1, -1, -1,
	11, 1, 2, 6, 10, 7, -1, -1, -1, -1, -1, -1, -1, -1, -1,
	1, 2, 11, 3, 0, 8, 6, 10, 7, -1, -1, -1, -1, -1, -1,
	2, 9, 0, 2, 11, 9, 6, 10, 7, -1, -1, -1, -1, -1, -1,
	6, 10, 7, 2, 11, 3, 11, 8, 3, 11, 9, 8, -1, -1, -1,
	7, 2, 3, 6, 2, 7, -1, -1, -1, -1, -1, -1, -1, -1, -1,
	7, 0, 8, 7, 6, 0, 6, 2, 0, -1, -1, -1, -1, -1, -1,
	2, 7, 6, 2, 3, 7, 0, 1, 9, -1, -1, -1, -1, -1, -1,
	1, 6, 2, 1, 8, 6, 1, 9, 8, 8, 7, 6, -1, -1, -1,
	11, 7, 6, 11, 1, 7, 1, 3, 7, -1, -1, -1, -1, -1, -1,
	11, 7, 6, 1, 7, 11, 1, 8, 7, 1, 0, 8, -1, -1, -1,
	0, 3, 7, 0, 7, 11, 0, 11, 9, 6, 11, 7, -1, -1, -1,
	7, 6, 11, 7, 11, 8, 8, 11, 9, -1, -1, -1, -1, -1, -1,
	6, 8, 4, 10, 8, 6, -1, -1, -1, -1, -1, -1, -1, -1, -1,
	3, 6, 10, 3, 0, 6, 0, 4, 6, -1, -1, -1, -1, -1, -1,
	8, 6, 10, 8, 4, 6, 9, 0, 1, -1, -1, -1, -1, -1, -1,
	9, 4, 6, 9, 6, 3, 9, 3, 1, 10, 3, 6, -1, -1, -1,
	6, 8, 4, 6, 10, 8, 2, 11, 1, -1, -1, -1, -1, -1, -1,
	1, 2, 11, 3, 0, 10, 0, 6, 10, 0, 4, 6, -1, -1, -1,
	4, 10, 8, 4, 6, 10, 0, 2, 9, 2, 11, 9, -1, -1, -1,
	11, 9, 3, 11, 3, 2, 9, 4, 3, 10, 3, 6, 4, 6, 3,
	8, 2, 3, 8, 4, 2, 4, 6, 2, -1, -1, -1, -1, -1, -1,
	0, 4, 2, 4, 6, 2, -1, -1, -1, -1, -1, -1, -1, -1, -1,
	1, 9, 0, 2, 3, 4, 2, 4, 6, 4, 3, 8, -1, -1, -1,
	1, 9, 4, 1, 4, 2, 2, 4, 6, -1, -1, -1, -1, -1, -1,
	8, 1, 3, 8, 6, 1, 8, 4, 6, 6, 11, 1, -1, -1, -1,
	11, 1, 0, 11, 0, 6, 6, 0, 4, -1, -1, -1, -1, -1, -1,
	4, 6, 3, 4, 3, 8, 6, 11, 3, 0, 3, 9, 11, 9, 3,
	11, 9, 4, 6, 11, 4, -1, -1, -1, -1, -1, -1, -1, -1, -1,
	4, 9, 5, 7, 6, 10, -1, -1, -1, -1, -1, -1, -1, -1, -1,
	0, 8, 3, 4, 9, 5, 10, 7, 6, -1, -1, -1, -1, -1, -1,
	5, 0, 1, 5, 4, 0, 7, 6, 10, -1, -1, -1, -1, -1, -1,
	10, 7, 6, 8, 3, 4, 3, 5, 4, 3, 1, 5, -1, -1, -1,
	9, 5, 4, 11, 1, 2, 7, 6, 10, -1, -1, -1, -1, -1, -1,
	6, 10, 7, 1, 2, 11, 0, 8, 3, 4, 9, 5, -1, -1, -1,
	7, 6, 10, 5, 4, 11, 4, 2, 11, 4, 0, 2, -1, -1, -1,
	3, 4, 8, 3, 5, 4, 3, 2, 5, 11, 5, 2, 10, 7, 6,
	7, 2, 3, 7, 6, 2, 5, 4, 9, -1, -1, -1, -1, -1, -1,
	9, 5, 4, 0, 8, 6, 0, 6, 2, 6, 8, 7, -1, -1, -1,
	3, 6, 2, 3, 7, 6, 1, 5, 0, 5, 4, 0, -1, -1, -1,
	6, 2, 8, 6, 8, 7, 2, 1, 8, 4, 8, 5, 1, 5, 8,
	9, 5, 4, 11, 1, 6, 1, 7, 6, 1, 3, 7, -1, -1, -1,
	1, 6, 11, 1, 7, 6, 1, 0, 7, 8, 7, 0, 9, 5, 4,
	4, 0, 11, 4, 11, 5, 0, 3, 11, 6, 11, 7, 3, 7, 11,
	7, 6, 11, 7, 11, 8, 5, 4, 11, 4, 8, 11, -1, -1, -1,
	6, 9, 5, 6, 10, 9, 10, 8, 9, -1, -1, -1, -1, -1, -1,
	3, 6, 10, 0, 6, 3, 0, 5, 6, 0, 9, 5, -1, -1, -1,
	0, 10, 8, 0, 5, 10, 0, 1, 5, 5, 6, 10, -1, -1, -1,
	6, 10, 3, 6, 3, 5, 5, 3, 1, -1, -1, -1, -1, -1, -1,
	1, 2, 11, 9, 5, 10, 9, 10, 8, 10, 5, 6, -1, -1, -1,
	0, 10, 3, 0, 6, 10, 0, 9, 6, 5, 6, 9, 1, 2, 11,
	10, 8, 5, 10, 5, 6, 8, 0, 5, 11, 5, 2, 0, 2, 5,
	6, 10, 3, 6, 3, 5, 2, 11, 3, 11, 5, 3, -1, -1, -1,
	5, 8, 9, 5, 2, 8, 5, 6, 2, 3, 8, 2, -1, -1, -1,
	9, 5, 6, 9, 6, 0, 0, 6, 2, -1, -1, -1, -1, -1, -1,
	1, 5, 8, 1, 8, 0, 5, 6, 8, 3, 8, 2, 6, 2, 8,
	1, 5, 6, 2, 1, 6, -1, -1, -1, -1, -1, -1, -1, -1, -1,
	1, 3, 6, 1, 6, 11, 3, 8, 6, 5, 6, 9, 8, 9, 6,
	11, 1, 0, 11, 0, 6, 9, 5, 0, 5, 6, 0, -1, -1, -1,
	0, 3, 8, 5, 6, 11, -1, -1, -1, -1, -1, -1, -1, -1, -1,
	11, 5, 6, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1,
	10, 5, 11, 7, 5, 10, -1, -1, -1, -1, -1, -1, -1, -1, -1,
	10, 5, 11, 10, 7, 5, 8, 3, 0, -1, -1, -1, -1, -1, -1,
	5, 10, 7, 5, 11, 10, 1, 9, 0, -1, -1, -1, -1, -1, -1,
	11, 7, 5, 11, 10, 7, 9, 8, 1, 8, 3, 1, -1, -1, -1,
	10, 1, 2, 10, 7, 1, 7, 5, 1, -1, -1, -1, -1, -1, -1,
	0, 8, 3, 1, 2, 7, 1, 7, 5, 7, 2, 10, -1, -1, -1,
	9, 7, 5, 9, 2, 7, 9, 0, 2, 2, 10, 7, -1, -1, -1,
	7, 5, 2, 7, 2, 10, 5, 9, 2, 3, 2, 8, 9, 8, 2,
	2, 5, 11, 2, 3, 5, 3, 7, 5, -1, -1, -1, -1, -1, -1,
	8, 2, 0, 8, 5, 2, 8, 7, 5, 11, 2, 5, -1, -1, -1,
	9, 0, 1, 5, 11, 3, 5, 3, 7, 3, 11, 2, -1, -1, -1,
	9, 8, 2, 9, 2, 1, 8, 7, 2, 11, 2, 5, 7, 5, 2,
	1, 3, 5, 3, 7, 5, -1, -1, -1, -1, -1, -1, -1, -1, -1,
	0, 8, 7, 0, 7, 1, 1, 7, 5, -1, -1, -1, -1, -1, -1,
	9, 0, 3, 9, 3, 5, 5, 3, 7, -1, -1, -1, -1, -1, -1,
	9, 8, 7, 5, 9, 7, -1, -1, -1, -1, -1, -1, -1, -1, -1,
	5, 8, 4, 5, 11, 8, 11, 10, 8, -1, -1, -1, -1, -1, -1,
	5, 0, 4, 5, 10, 0, 5, 11, 10, 10, 3, 0, -1, -1, -1,
	0, 1, 9, 8, 4, 11, 8, 11, 10, 11, 4, 5, -1, -1, -1,
	11, 10, 4, 11, 4, 5, 10, 3, 4, 9, 4, 1, 3, 1, 4,
	2, 5, 1, 2, 8, 5, 2, 10, 8, 4, 5, 8, -1, -1, -1,
	0, 4, 10, 0, 10, 3, 4, 5, 10, 2, 10, 1, 5, 1, 10,
	0, 2, 5, 0, 5, 9, 2, 10, 5, 4, 5, 8, 10, 8, 5,
	9, 4, 5, 2, 10, 3, -1, -1, -1, -1, -1, -1, -1, -1, -1,
	2, 5, 11, 3, 5, 2, 3, 4, 5, 3, 8, 4, -1, -1, -1,
	5, 11, 2, 5, 2, 4, 4, 2, 0, -1, -1, -1, -1, -1, -1,
	3, 11, 2, 3, 5, 11, 3, 8, 5, 4, 5, 8, 0, 1, 9,
	5, 11, 2, 5, 2, 4, 1, 9, 2, 9, 4, 2, -1, -1, -1,
	8, 4, 5, 8, 5, 3, 3, 5, 1, -1, -1, -1, -1, -1, -1,
	0, 4, 5, 1, 0, 5, -1, -1, -1, -1, -1, -1, -1, -1, -1,
	8, 4, 5, 8, 5, 3, 9, 0, 5, 0, 3, 5, -1, -1, -1,
	9, 4, 5, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1,
	4, 10, 7, 4, 9, 10, 9, 11, 10, -1, -1, -1, -1, -1, -1,
	0, 8, 3, 4, 9, 7, 9, 10, 7, 9, 11, 10, -1, -1, -1,
	1, 11, 10, 1, 10, 4, 1, 4, 0, 7, 4, 10, -1, -1, -1,
	3, 1, 4, 3, 4, 8, 1, 11, 4, 7, 4, 10, 11, 10, 4,
	4, 10, 7, 9, 10, 4, 9, 2, 10, 9, 1, 2, -1, -1, -1,
	9, 7, 4, 9, 10, 7, 9, 1, 10, 2, 10, 1, 0, 8, 3,
	10, 7, 4, 10, 4, 2, 2, 4, 0, -1, -1, -1, -1, -1, -1,
	10, 7, 4, 10, 4, 2, 8, 3, 4, 3, 2, 4, -1, -1, -1,
	2, 9, 11, 2, 7, 9, 2, 3, 7, 7, 4, 9, -1, -1, -1,
	9, 11, 7, 9, 7, 4, 11, 2, 7, 8, 7, 0, 2, 0, 7,
	3, 7, 11, 3, 11, 2, 7, 4, 11, 1, 11, 0, 4, 0, 11,
	1, 11, 2, 8, 7, 4, -1, -1, -1, -1, -1, -1, -1, -1, -1,
	4, 9, 1, 4, 1, 7, 7, 1, 3, -1, -1, -1, -1, -1, -1,
	4, 9, 1, 4, 1, 7, 0, 8, 1, 8, 7, 1, -1, -1, -1,
	4, 0, 3, 7, 4, 3, -1, -1, -1, -1, -1, -1, -1, -1, -1,
	4, 8, 7, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1,
	9, 11, 8, 11, 10, 8, -1, -1, -1, -1, -1, -1, -1, -1, -1,
	3, 0, 9, 3, 9, 10, 10, 9, 11, -1, -1, -1, -1, -1, -1,
	0, 1, 11, 0, 11, 8, 8, 11, 10, -1, -1, -1, -1, -1, -1,
	3, 1, 11, 10, 3, 11, -1, -1, -1, -1, -1, -1, -1, -1, -1,
	1, 2, 10, 1, 10, 9, 9, 10, 8, -1, -1, -1, -1, -1, -1,
	3, 0, 9, 3, 9, 10, 1, 2, 9, 2, 10, 9, -1, -1, -1,
	0, 2, 10, 8, 0, 10, -1, -1, -1, -1, -1, -1, -1, -1, -1,
	3, 2, 10, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1,
	2, 3, 8, 2, 8, 11, 11, 8, 9, -1, -1, -1, -1, -1, -1,
	9, 11, 2, 0, 9, 2, -1, -1, -1, -1, -1, -1, -1, -1, -1,
	2, 3, 8, 2, 8, 11, 0, 1, 8, 1, 11, 8, -1, -1, -1,
	1, 11, 2, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1,
	1, 3, 8, 9, 1, 8, -1, -1, -1, -1, -1, -1, -1, -1, -1,
	0, 9, 1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1,
	0, 3, 8, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1,
	-1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1
};

static const int voxel_indices[][2] =
{
	{ 0, 1 },
	{ 1, 2 },
	{ 2, 3 },
	{ 3, 0 },
	{ 4, 5 },
	{ 5, 6 },
	{ 6, 7 },
	{ 7, 4 },
	{ 0, 4 },
	{ 1, 5 },
	{ 3, 7 },
	{ 2, 6 }
};

cbuffer buffer : register(b0)
{
	int3 Size : packoffset(c0);
	float AmbientRayWidth : packoffset(c0.w);
	int3 NearestSize : packoffset(c1);
	int AmbientSamplesCount : packoffset(c1.w);
	int Seed : packoffset(c2);
	int PrefixSize : packoffset(c2.y);
	int PrefixN : packoffset(c2.z);
	int PrefixArrayLength : packoffset(c2.w);
}

SamplerState SamplerDefault
{
	Filter = MIN_MAG_MIP_LINEAR;
	AddressU = Wrap;
	AddressV = Wrap;
	AddressW = Wrap;
};

XorwowState XorwowSequence(XorwowState state)
{
	uint t = state.x ^ (state.x >> 2);

	state.x = state.y;
	state.y = state.z;
	state.z = state.w;
	state.w = state.v;
	state.v = (state.v ^ (state.v << 4)) ^ (t ^ (t << 1));
	state.d += 362437;

	return state;
}

uint XorwowValue(XorwowState state)
{
	return state.d + state.v;
}

float XorwowRandom(uint seed, uint sequence)
{
	uint s0 = seed ^ 0xaad26b49UL;
	uint s1 = (seed >> 32) ^ 0xf7dcefddUL;
	uint t0 = 1099087573UL * s0;
	uint t1 = 2591861531UL * s1;

	XorwowState state;
	state.d = 6615241 + t1 + t0;
	state.x = 123456789UL + t0;
	state.y = 362436069UL ^ t0;
	state.z = 521288629UL + t1;
	state.w = 88675123UL ^ t1;
	state.v = 5783321UL + t0;

	for(int i = 0; i < sequence; i++)
	{
		state = XorwowSequence(state);
	}

	return (XorwowValue(state) / UINT_MAX) * 2.0f - 1.0f;
}

inline uint scan1Inclusive(uint idata, uint size, int idx)
{
	uint pos = 2 * idx - (idx & (size - 1));
	prefixShared[pos] = 0;

	pos += size;
	prefixShared[pos] = idata;

	for (uint offset = 1; offset < size; offset <<= 1)
	{
		GroupMemoryBarrierWithGroupSync();
		uint t = prefixShared[pos] + prefixShared[pos - offset];

		GroupMemoryBarrierWithGroupSync();
		prefixShared[pos] = t;
	}

	return prefixShared[pos];
}

inline uint scan1Exclusive(uint idata, uint size, int idx)
{
	return scan1Inclusive(idata, size, idx) - idata;
}

inline uint4 scan4Inclusive(uint4 idata4, uint size, int idx)
{
	idata4.y += idata4.x;
	idata4.z += idata4.y;
	idata4.w += idata4.z;

	uint oval = scan1Exclusive(idata4.w, size / 4, idx);

	idata4.x += oval;
	idata4.y += oval;
	idata4.z += oval;
	idata4.w += oval;

	return idata4;
}

inline uint4 scan4Exclusive(uint4 idata4, uint size, int idx)
{
	uint4 odata4 = scan4Inclusive(idata4, size, idx);

	odata4.x -= idata4.x;
	odata4.y -= idata4.y;
	odata4.z -= idata4.z;
	odata4.w -= idata4.w;

	return odata4;
}

[numthreads(THREADBLOCK_SIZE, 1, 1)]
void ScanExclusiveShared(int3 threadGroupID : SV_GroupThreadID, int3 threadID : SV_DispatchThreadID)
{
	uint4 idata4 = uint4(triangleCounts[threadID.x * 4], triangleCounts[threadID.x * 4 + 1], triangleCounts[threadID.x * 4 + 2], triangleCounts[threadID.x * 4 + 3]);
	uint4 odata4 = scan4Exclusive(idata4, PrefixSize, threadGroupID.x);

	prefixSums[threadID.x * 4] = odata4.x;
	prefixSums[threadID.x * 4 + 1] = odata4.y;
	prefixSums[threadID.x * 4 + 2] = odata4.z;
	prefixSums[threadID.x * 4 + 3] = odata4.w;
}

[numthreads(THREADBLOCK_SIZE, 1, 1)]
void ScanExclusiveShared2(int3 threadGroupID : SV_GroupThreadID, int3 threadID : SV_DispatchThreadID)
{
	uint idata = 0;

	if (threadID.x < PrefixN)
	{
		idata = prefixSums[(4 * THREADBLOCK_SIZE) - 1 + (4 * THREADBLOCK_SIZE) * threadID.x] + triangleCounts[(4 * THREADBLOCK_SIZE) - 1 + (4 * THREADBLOCK_SIZE) * threadID.x];
	}

	uint odata = scan1Exclusive(idata, PrefixArrayLength, threadGroupID.x);

	if (threadID.x < PrefixN)
	{
		prefixSumsBuffer[threadID.x] = odata;
	}
}

[numthreads(THREADBLOCK_SIZE, 1, 1)]
void UniformUpdate(int3 threadGroupID : SV_GroupThreadID, int3 threadID : SV_DispatchThreadID, int3 groupID : SV_GroupID)
{
	if (threadGroupID.x == 0)
	{
		prefixBuffer = prefixSumsBuffer[groupID.x];
	}

	GroupMemoryBarrierWithGroupSync();

	prefixSums[threadID.x * 4] = prefixSums[threadID.x * 4] + prefixBuffer;
	prefixSums[threadID.x * 4 + 1] = prefixSums[threadID.x * 4 + 1] + prefixBuffer;
	prefixSums[threadID.x * 4 + 2] = prefixSums[threadID.x * 4 + 2] + prefixBuffer;
	prefixSums[threadID.x * 4 + 3] = prefixSums[threadID.x * 4 + 3] + prefixBuffer;
}

[numthreads(BLOCK_DIM_X, BLOCK_DIM_Y, BLOCK_DIM_Z)]
void FillNoiseTexture(int3 threadID : SV_DispatchThreadID)
{
	int index = threadID.x + threadID.y * Size.x + threadID.z * Size.x * Size.y;

	if(threadID.x < Size.x && threadID.y < Size.y && threadID.z < Size.z)
	{
		noiseTextureRW[threadID.xyz] = XorwowRandom(Seed, index);
	}
}

[numthreads(BLOCK_DIM_X, BLOCK_DIM_Y, BLOCK_DIM_Z)]
void PositionWeightNoiseCube(int3 threadID : SV_DispatchThreadID)
{
	int index = threadID.x + threadID.y * Size.x + threadID.z * Size.x * Size.y;
	int cy = Size.y / 2;

	if(threadID.x < Size.x && threadID.y < Size.y && threadID.z < Size.z)
	{
		voxels[index].Position = threadID.xyz;

		voxels[index].Weight = cy - threadID.y;
		voxels[index].Weight += noiseTexture.SampleLevel(SamplerDefault, float3(threadID.x / 256.04f, threadID.y / 256.01f, threadID.z / 255.97f), 0) * 64.0f;
		voxels[index].Weight += noiseTexture.SampleLevel(SamplerDefault, float3(threadID.x / 128.01f, threadID.y / 127.96f, threadID.z / 127.98f), 0) * 4.0f;
		voxels[index].Weight += noiseTexture.SampleLevel(SamplerDefault, float3(threadID.x / 64.01f, threadID.y / 64.04f, threadID.z / 63.96f), 0) * 2.0f;
		voxels[index].Weight += noiseTexture.SampleLevel(SamplerDefault, float3(threadID.x / 32.02f, threadID.y / 31.98f, threadID.z / 31.97f), 0) * 1.0f;
	}
}

[numthreads(BLOCK_DIM_X, BLOCK_DIM_Y, BLOCK_DIM_Z)]
void PositionWeightNoiseCubeWarp(int3 threadID : SV_DispatchThreadID)
{
	int index = threadID.x + threadID.y * Size.x + threadID.z * Size.x * Size.y;
	int cy = Size.y / 2;

	if(threadID.x < Size.x && threadID.y < Size.y && threadID.z < Size.z)
	{
		float warp = noiseTexture.SampleLevel(SamplerDefault, float3(threadID.x * 0.004f, threadID.y * 0.004f, threadID.z * 0.004f), 0);
		float wx = threadID.x + warp * 8;
		float wy = threadID.y + warp * 8;
		float wz = threadID.z + warp * 8;

		voxels[index].Position = threadID.xyz;

		voxels[index].Weight = cy - threadID.y;
		voxels[index].Weight += noiseTexture.SampleLevel(SamplerDefault, float3(wx / 256.04f, wy / 256.01f, wz / 255.97f), 0) * 64.0f;
		voxels[index].Weight += noiseTexture.SampleLevel(SamplerDefault, float3(wx / 128.01f, wy / 127.96f, wz / 127.98f), 0) * 4.0f;
		voxels[index].Weight += noiseTexture.SampleLevel(SamplerDefault, float3(wx / 64.01f, wy / 64.04f, wz / 63.96f), 0) * 2.0f;
		voxels[index].Weight += noiseTexture.SampleLevel(SamplerDefault, float3(wx / 32.02f, wy / 31.98f, wz / 31.97f), 0) * 1.0f;
	}
}

[numthreads(BLOCK_DIM_X, BLOCK_DIM_Y, BLOCK_DIM_Z)]
void PositionWeightFormula(int3 threadID : SV_DispatchThreadID)
{
	int index = threadID.x + threadID.y * Size.x + threadID.z * Size.x * Size.y;

	float area = sqrt(Size.x * Size.z);
	float3 center = float3(Size.x / 2.0f, Size.y / 2.0f, Size.z / 2.0f);
	float3 pillars[3] =
	{
		float3(Size.x / 4.0f, 0, Size.z / 4.0f),
		float3(Size.x * 3.0f / 4.0f, 0, Size.z * 3.0f / 4.0f),
		float3(Size.x * 2.0f / 4.0f, 0, Size.z / 4.0f)
	};

	if(threadID.x < Size.x && threadID.y < Size.y && threadID.z < Size.z)
	{
		float weight = 0;

		float distanceFromCenter = distance(threadID.xz, center.xz);
		distanceFromCenter = distanceFromCenter < 0.1f ? 0.1f : distanceFromCenter;

        for(int k = 0; k < 3; k++)
        {
            float dist = distance(threadID.xz, pillars[k].xz);
            dist = dist < 0.1f ? 0.1f : dist;
            weight += area / dist;
        }

		weight -= area / distanceFromCenter;

		weight -= pow(distanceFromCenter, 3) / pow(area, 1.5f);

		float coordinate = 3 * PI * threadID.y / Size.y;
		float2 helix = float2(cos(coordinate), sin(coordinate));
		weight += helix.x * (threadID.x - center.x) + helix.y * (threadID.z - center.z);

		weight += 10.0f * cos(coordinate * 4.0f / 3.0f);

		weight += noiseTexture.SampleLevel(SamplerDefault, float3(threadID.x / 256.04f, threadID.y / 256.01f, threadID.z / 255.97f), 0) * 8.0f;
		weight += noiseTexture.SampleLevel(SamplerDefault, float3(threadID.x / 128.01f, threadID.y / 127.96f, threadID.z / 127.98f), 0) * 4.0f;
		weight += noiseTexture.SampleLevel(SamplerDefault, float3(threadID.x / 64.01f, threadID.y / 64.04f, threadID.z / 63.96f), 0) * 2.0f;
		weight += noiseTexture.SampleLevel(SamplerDefault, float3(threadID.x / 32.02f, threadID.y / 31.98f, threadID.z / 31.97f), 0) * 1.0f;

		voxels[index].Position = threadID.xyz;
		voxels[index].Weight = weight;
	}
}

[numthreads(BLOCK_DIM_X, BLOCK_DIM_Y, BLOCK_DIM_Z)]
void NormalAmbient(int3 threadID : SV_DispatchThreadID)
{
	int index = threadID.x + threadID.y * Size.x + threadID.z * Size.x * Size.y;

	if(threadID.x < Size.x && threadID.y < Size.y && threadID.z < Size.z)
	{
		int xii = min(Size.x - 1, threadID.x + 1) + threadID.y * Size.x + threadID.z * Size.x * Size.y;
		int xdi = max(0, threadID.x - 1) + threadID.y * Size.x + threadID.z * Size.x * Size.y;

		int yii = threadID.x + min(Size.y - 1, threadID.y + 1) * Size.x + threadID.z * Size.x * Size.y;
		int ydi = threadID.x + max(0, threadID.y - 1) * Size.x + threadID.z * Size.x * Size.y;

		int zii = threadID.x + threadID.y * Size.x + min(Size.z - 1, threadID.z + 1) * Size.x * Size.y;
		int zdi = threadID.x + threadID.y * Size.x + max(0, threadID.z - 1) * Size.x * Size.y;

		voxels[index].Normal = normalize(float3(voxels[xdi].Weight - voxels[xii].Weight, voxels[ydi].Weight - voxels[yii].Weight, voxels[zdi].Weight - voxels[zii].Weight));

		float stepLength = (Size.x * AmbientRayWidth / 100.0f) / AmbientSamplesCount;
		float ambient = 0;

		for (int k = 0; k < POISSON_DISC_LEN; k++)
		{
			float sample = 0;

			for (int j = 0; j < AmbientSamplesCount; j++)
			{
				int stepNumber = j + 2;

				int cx = (int)max(0, min(Size.x - 1, threadID.x + stepNumber * stepLength * poissonDisc[k][0]));
				int cy = (int)max(0, min(Size.y - 1, threadID.y + stepNumber * stepLength * poissonDisc[k][1]));
				int cz = (int)max(0, min(Size.z - 1, threadID.z + stepNumber * stepLength * poissonDisc[k][2]));

				int ci = cx + cy * Size.x + cz * Size.x * Size.y;

				sample += voxels[ci].Weight > 0 ? 0 : 1;
			}

			ambient += sample / AmbientSamplesCount;
		}

		voxels[index].Ambient = ambient / POISSON_DISC_LEN;
	}
}

[numthreads(BLOCK_DIM_X, BLOCK_DIM_Y, BLOCK_DIM_Z)]
void MarchingCubesCases(int3 threadID : SV_DispatchThreadID)
{
	int index = threadID.x + threadID.y * Size.x + threadID.z * Size.x * Size.y;
	int3 SizeD = Size - 1;
	int indexD = threadID.x + threadID.y * SizeD.x + threadID.z * SizeD.x * SizeD.y;
	int indexN = threadID.x + threadID.y * NearestSize.x + threadID.z * NearestSize.x * NearestSize.y;

	if(threadID.x < SizeD.x && threadID.y < SizeD.y && threadID.z < SizeD.z)
	{
		int indices[8];

		indices[0] = index;
		indices[1] = threadID.x + threadID.y * Size.x + (threadID.z + 1) * Size.x * Size.y;
		indices[2] = (threadID.x + 1) + threadID.y * Size.x + (threadID.z + 1) * Size.x * Size.y;
		indices[3] = (threadID.x + 1) + threadID.y * Size.x + threadID.z * Size.x * Size.y;
		indices[4] = threadID.x + (threadID.y + 1) * Size.x + threadID.z * Size.x * Size.y;
		indices[5] = threadID.x + (threadID.y + 1) * Size.x + (threadID.z + 1) * Size.x * Size.y;
		indices[6] = (threadID.x + 1) + (threadID.y + 1) * Size.x + (threadID.z + 1) * Size.x * Size.y;
		indices[7] = (threadID.x + 1) + (threadID.y + 1) * Size.x + threadID.z * Size.x * Size.y;

		int caseNumber = 0;
		for(int k = -1; ++k < 8; caseNumber += voxels[indices[k]].Weight > 0 ? 1 << k : 0);

		int offset = (255 - caseNumber) * 15;
		offsets[indexD] = offset;

		int trisCount = 0;
		[allow_uav_condition]
		for(int k = 0; k < 5; k++, offset += 3)
		{
			if (faces[offset] != -1)        
				trisCount++;
			else
				break;
		}

		triangleCounts[indexN] = trisCount;
	}
}

[numthreads(BLOCK_DIM_X, BLOCK_DIM_Y, BLOCK_DIM_Z)]
void MarchingCubesVertices(int3 threadID : SV_DispatchThreadID)
{
	int index = threadID.x + threadID.y * Size.x + threadID.z * Size.x * Size.y;
	int3 SizeD = Size - 1;
	int indexD = threadID.x + threadID.y * SizeD.x + threadID.z * SizeD.x * SizeD.y;
	int indexN = threadID.x + threadID.y * NearestSize.x + threadID.z * NearestSize.x * NearestSize.y;

	if(threadID.x < SizeD.x && threadID.y < SizeD.y && threadID.z < SizeD.z)
	{
		int indices[8];

		indices[0] = index;
		indices[1] = threadID.x + threadID.y * Size.x + (threadID.z + 1) * Size.x * Size.y;
		indices[2] = (threadID.x + 1) + threadID.y * Size.x + (threadID.z + 1) * Size.x * Size.y;
		indices[3] = (threadID.x + 1) + threadID.y * Size.x + threadID.z * Size.x * Size.y;
		indices[4] = threadID.x + (threadID.y + 1) * Size.x + threadID.z * Size.x * Size.y;
		indices[5] = threadID.x + (threadID.y + 1) * Size.x + (threadID.z + 1) * Size.x * Size.y;
		indices[6] = (threadID.x + 1) + (threadID.y + 1) * Size.x + (threadID.z + 1) * Size.x * Size.y;
		indices[7] = (threadID.x + 1) + (threadID.y + 1) * Size.x + threadID.z * Size.x * Size.y;

		bool interpolatedFilled[12] = { false, false, false, false, false, false, false, false, false, false, false, false };
		VoxelMeshVertex interpolatedVertices[12];

		[allow_uav_condition]
		for(int k = 0; k < 15; k++)
		{
			int face = faces[offsets[indexD] + k];

			if(face == -1)
				break;

			if(!interpolatedFilled[face])
			{
				Voxel v1 = voxels[indices[voxel_indices[face][0]]];
				Voxel v2 = voxels[indices[voxel_indices[face][1]]];

				float interpolation = -v1.Weight / (v2.Weight - v1.Weight);

				interpolatedVertices[face].Ambient = lerp(v1.Ambient, v2.Ambient, interpolation);
				interpolatedVertices[face].Position = lerp(v1.Position, v2.Position, interpolation);
				interpolatedVertices[face].Normal = lerp(v1.Normal, v2.Normal, interpolation);

				interpolatedFilled[face] = true;
			}
		}

		int offset = offsets[indexD];

		[allow_uav_condition]
		for(int k = 0; k < 5; k++, offset += 3)
		{
			if(faces[offset] == -1)
				break;

			vertices[(prefixSums[indexN] + k) * 3] = interpolatedVertices[faces[offset]];
			vertices[(prefixSums[indexN] + k) * 3 + 1] = interpolatedVertices[faces[offset + 1]];
			vertices[(prefixSums[indexN] + k) * 3 + 2] = interpolatedVertices[faces[offset + 2]];
		}
	}
}