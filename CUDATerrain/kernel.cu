#include <cuda.h> 
#include <cuda_runtime.h>
#include <device_launch_parameters.h>
#include <texture_fetch_functions.h>
#include <cuda_texture_types.h>
#include <builtin_types.h>
#include <vector_functions.h> 
#include <float.h>
#include <curand.h>
#include <curand_kernel.h>
#include <math_functions.h>
#include <math_constants.h>

#define POISSON_DISC_LEN 32
#define AMBIGOUS_LEN 60
#define THREADBLOCK_SIZE 256

typedef unsigned int uint;

__device__ float poissonDisc[POISSON_DISC_LEN][3] =
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

__device__ int faces[] =
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

__device__ int voxel_indices[][2] =
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

texture<float, 3, cudaReadModeElementType> noiseTexture;

extern "C"
{
	inline __device__ uint scan1Inclusive(uint idata, volatile uint *s_Data, uint size)
	{
		uint pos = 2 * threadIdx.x - (threadIdx.x & (size - 1));
		s_Data[pos] = 0;

		pos += size;
		s_Data[pos] = idata;

		for (uint offset = 1; offset < size; offset <<= 1)
		{
			__syncthreads();
			uint t = s_Data[pos] + s_Data[pos - offset];

			__syncthreads();
			s_Data[pos] = t;
		}

		return s_Data[pos];
	}

	inline __device__ uint scan1Exclusive(uint idata, volatile uint *s_Data, uint size)
	{
		return scan1Inclusive(idata, s_Data, size) - idata;
	}

	inline __device__ uint4 scan4Inclusive(uint4 idata4, volatile uint *s_Data, uint size)
	{
		idata4.y += idata4.x;
		idata4.z += idata4.y;
		idata4.w += idata4.z;

		uint oval = scan1Exclusive(idata4.w, s_Data, size / 4);

		idata4.x += oval;
		idata4.y += oval;
		idata4.z += oval;
		idata4.w += oval;

		return idata4;
	}

	inline __device__ uint4 scan4Exclusive(uint4 idata4, volatile uint *s_Data, uint size)
	{
		uint4 odata4 = scan4Inclusive(idata4, s_Data, size);

		odata4.x -= idata4.x;
		odata4.y -= idata4.y;
		odata4.z -= idata4.z;
		odata4.w -= idata4.w;

		return odata4;
	}

	__global__ void scanExclusiveShared(uint4 *d_Dst, uint4 *d_Src, uint size)
	{
		__shared__ uint s_Data[2 * THREADBLOCK_SIZE];

		uint pos = blockIdx.x * blockDim.x + threadIdx.x;
		uint4 idata4 = d_Src[pos];
		uint4 odata4 = scan4Exclusive(idata4, s_Data, size);

		d_Dst[pos] = odata4;
	}

	__global__ void scanExclusiveShared2(uint *d_Buf, uint *d_Dst, uint *d_Src, uint N, uint arrayLength)
	{
		__shared__ uint s_Data[2 * THREADBLOCK_SIZE];

		uint pos = blockIdx.x * blockDim.x + threadIdx.x;
		uint idata = 0;

		if (pos < N)
		{
			idata = d_Dst[(4 * THREADBLOCK_SIZE) - 1 + (4 * THREADBLOCK_SIZE) * pos] + d_Src[(4 * THREADBLOCK_SIZE) - 1 + (4 * THREADBLOCK_SIZE) * pos];
		}

		uint odata = scan1Exclusive(idata, s_Data, arrayLength);

		if (pos < N)
		{
			d_Buf[pos] = odata;
		}
	}

	__global__ void uniformUpdate(uint4 *d_Data, uint *d_Buffer)
	{
		__shared__ uint buf;

		uint pos = blockIdx.x * blockDim.x + threadIdx.x;

		if (threadIdx.x == 0)
		{
			buf = d_Buffer[blockIdx.x];
		}

		__syncthreads();

		uint4 data4 = d_Data[pos];

		data4.x += buf;
		data4.y += buf;
		data4.z += buf;
		data4.w += buf;

		d_Data[pos] = data4;
	}

	__global__ void position_weight_noise_cube(Voxel* v, int w, int h, int d)
	{
		int x = threadIdx.x + blockDim.x * blockIdx.x;
		int y = threadIdx.y + blockDim.y * blockIdx.y;
		int z = threadIdx.z + blockDim.z * blockIdx.z;
		int i = x + y * w + z * w * h;
		int cy = h / 2;

		if(x < w && y < h && z < d)
		{
			v[i].Weight = cy - y;
			v[i].Weight += (tex3D(noiseTexture, x / 256.04f, y / 256.01f, z / 255.97f) * 2.0f - 1.0f) * 64.0f;
			v[i].Weight += (tex3D(noiseTexture, x / 128.01f, y / 127.96f, z / 127.98f) * 2.0f - 1.0f) * 4.0f;
			v[i].Weight += (tex3D(noiseTexture, x / 64.01f, y / 64.04f, z / 63.96f) * 2.0f - 1.0f) * 2.0f;
			v[i].Weight += (tex3D(noiseTexture, x / 32.02f, y / 31.98f, z / 31.97f) * 2.0f - 1.0f) * 1.0f;

			v[i].Position.x = x;
			v[i].Position.y = y;
			v[i].Position.z = z;
		}
	}

	__global__ void position_weight_formula(Voxel* v, int w, int h, int d)
	{
		int x = threadIdx.x + blockDim.x * blockIdx.x;
		int y = threadIdx.y + blockDim.y * blockIdx.y;
		int z = threadIdx.z + blockDim.z * blockIdx.z;
		int i = x + y * w + z * w * h;
		
		float area = sqrtf(w * d);
		float3 center = make_float3(w / 2.0f, h / 2.0f, d / 2.0f);
		float3 pillars[3] =
		{
			make_float3(w / 4.0f, 0, d / 4.0f),
			make_float3(w * 3.0f / 4.0f, 0, d * 3.0f / 4.0f),
			make_float3(w * 2.0f / 4.0f, 0, d / 4.0f)
		};

		if(x < w && y < h && z < d)
		{	
			float weight = 0;

			float distanceFromCenter = sqrtf(powf(x - center.x, 2) + powf(z - center.z, 2));
			distanceFromCenter = distanceFromCenter < 0.1f ? 0.1f : distanceFromCenter;

            for(int k = 0; k < 3; k++)
            {
                float distance = sqrtf(powf(x - pillars[k].x, 2) + powf(z - pillars[k].z, 2));
                distance = distance < 0.1f ? 0.1f : distance;
                weight += area / distance;
            }

			weight -= area / distanceFromCenter;

			weight -= powf(distanceFromCenter, 3) / powf(area, 1.5f);

			double coordinate = 3 * CUDART_PI * y / h;
			float2 helix = make_float2(cosf(coordinate), sinf(coordinate));
			weight += helix.x * (x - center.x) + helix.y * (z - center.z);

			weight += 10 * cosf(coordinate * 4 / 3);

			weight += (tex3D(noiseTexture, x / 256.04f, y / 256.01f, z / 255.97f) * 2.0f - 1.0f) * 8.0f;
			weight += (tex3D(noiseTexture, x / 128.01f, y / 127.96f, z / 127.98f) * 2.0f - 1.0f) * 4.0f;
			weight += (tex3D(noiseTexture, x / 64.01f, y / 64.04f, z / 63.96f) * 2.0f - 1.0f) * 2.0f;
			weight += (tex3D(noiseTexture, x / 32.02f, y / 31.98f, z / 31.97f) * 2.0f - 1.0f) * 1.0f;

			v[i].Position.x = x;
			v[i].Position.y = y;
			v[i].Position.z = z;
			v[i].Weight = weight;
		}
	}

	__global__ void position_weight_noise_cube_warp(Voxel* v, int w, int h, int d)
	{
		int x = threadIdx.x + blockDim.x * blockIdx.x;
		int y = threadIdx.y + blockDim.y * blockIdx.y;
		int z = threadIdx.z + blockDim.z * blockIdx.z;
		int i = x + y * w + z * w * h;
		int cy = h / 2;

		if(x < w && y < h && z < d)
		{
			float warp = tex3D(noiseTexture, x * 0.004, y * 0.004, z * 0.004);
            float wx = x + warp * 8;
            float wy = y + warp * 8;
            float wz = z + warp * 8;

			v[i].Weight = cy - y;
			v[i].Weight += (tex3D(noiseTexture, wx / 256.04f, wy / 256.01f, wz / 255.97f) * 2.0f - 1.0f) * 64.0f;
			v[i].Weight += (tex3D(noiseTexture, wx / 128.01f, wy / 127.96f, wz / 127.98f) * 2.0f - 1.0f) * 4.0f;
			v[i].Weight += (tex3D(noiseTexture, wx / 64.01f, wy / 64.04f, wz / 63.96f) * 2.0f - 1.0f) * 2.0f;
			v[i].Weight += (tex3D(noiseTexture, wx / 32.02f, wy / 31.98f, wz / 31.97f) * 2.0f - 1.0f) * 1.0f;

			v[i].Position.x = x;
			v[i].Position.y = y;
			v[i].Position.z = z;
		}
	}

	__global__ void normal_ambient(Voxel* v, int w, int h, int d, float ambientRayWidth, int ambientSamplesCount)
	{
		int x = threadIdx.x + blockDim.x * blockIdx.x;
		int y = threadIdx.y + blockDim.y * blockIdx.y;
		int z = threadIdx.z + blockDim.z * blockIdx.z;
		int i = x + y * w + z * w * h;

		if(x < w && y < h && z < d)
		{
			int xii = min(w - 1, x + 1) + y * w + z * w * h;
			int xdi = max(0, x - 1) + y * w + z * w * h;

			int yii = x + min(h - 1, y + 1) * w + z * w * h;
			int ydi = x + max(0, y - 1) * w + z * w * h;

			int zii = x + y * w + min(d - 1, z + 1) * w * h;
			int zdi = x + y * w + max(0, z - 1) * w * h;

			v[i].Normal.x = v[xdi].Weight - v[xii].Weight;
			v[i].Normal.y = v[ydi].Weight - v[yii].Weight;
			v[i].Normal.z = v[zdi].Weight - v[zii].Weight;

			float len = sqrtf(powf(v[i].Normal.x, 2) + powf(v[i].Normal.y, 2) + powf(v[i].Normal.z, 2));

			v[i].Normal.x /= len;
			v[i].Normal.y /= len;
			v[i].Normal.z /= len;

			float stepLength = (w * ambientRayWidth / 100.0f) / ambientSamplesCount;
			float ambient = 0;

			for (int k = 0; k < POISSON_DISC_LEN; k++)
			{
				float sample = 0;

				for (int j = 0; j < ambientSamplesCount; j++)
				{
					int stepNumber = j + 2;

					int cx = (int)fmaxf(0, fminf(w - 1, x + stepNumber * stepLength * poissonDisc[k][0]));
					int cy = (int)fmaxf(0, fminf(h - 1, y + stepNumber * stepLength * poissonDisc[k][1]));
					int cz = (int)fmaxf(0, fminf(d - 1, z + stepNumber * stepLength * poissonDisc[k][2]));

					int ci = cx + cy * w + cz * w * h;

					sample += v[ci].Weight > 0 ? 0 : 1;
				}

				ambient += sample / ambientSamplesCount;
			}

			v[i].Ambient = ambient / POISSON_DISC_LEN;
		}
	}

	__global__ void marching_cubes_cases(Voxel* v, int w, int h, int d, int* offsets, int* triangleCounts, int nW, int nH, int nD)
	{
		int x = threadIdx.x + blockDim.x * blockIdx.x;
		int y = threadIdx.y + blockDim.y * blockIdx.y;
		int z = threadIdx.z + blockDim.z * blockIdx.z;
		int i = x + y * w + z * w * h;
		int wd = w - 1;
		int hd = h - 1;
		int dd = d - 1;
		int id = x + y * wd + z * wd * hd;
		int in = x + y * nW + z * nW * nH;

		if(x < wd && y < hd && z < dd)
		{
			int indices[8];

			indices[0] = i;
			indices[1] = x + y * w + (z + 1) * w * h;
			indices[2] = (x + 1) + y * w + (z + 1) * w * h;
			indices[3] = (x + 1) + y * w + z * w * h;
			indices[4] = x + (y + 1) * w + z * w * h;
			indices[5] = x + (y + 1) * w + (z + 1) * w * h;
			indices[6] = (x + 1) + (y + 1) * w + (z + 1) * w * h;
			indices[7] = (x + 1) + (y + 1) * w + z * w * h;

			int caseNumber = 0;
			for(int k = -1; ++k < 8; caseNumber += v[indices[k]].Weight > 0 ? 1 << k : 0);

			int offset = (255 - caseNumber) * 15;
			offsets[id] = offset;

			int trisCount = 0;
			for (int k = 0; k < 5; k++, offset += 3)
			{
				if (faces[offset] != -1)        
					trisCount++;
				else
					break;
			}

			triangleCounts[in] = trisCount;
		}
	}

	__global__ void marching_cubes_vertices(VoxelMeshVertex* vertices, Voxel* voxels, int* prefixSums, int* offsets, int w, int h, int d, int nW, int nH, int nD)
	{
		int x = threadIdx.x + blockDim.x * blockIdx.x;
		int y = threadIdx.y + blockDim.y * blockIdx.y;
		int z = threadIdx.z + blockDim.z * blockIdx.z;
		int i = x + y * w + z * w * h;
		int wd = w - 1;
		int hd = h - 1;
		int dd = d - 1;
		int id = x + y * wd + z * wd * hd;
		int in = x + y * nW + z * nW * nH;

		if(x < wd && y < hd && z < dd)
		{
			int indices[8];

			indices[0] = i;
			indices[1] = x + y * w + (z + 1) * w * h;
			indices[2] = (x + 1) + y * w + (z + 1) * w * h;
			indices[3] = (x + 1) + y * w + z * w * h;
			indices[4] = x + (y + 1) * w + z * w * h;
			indices[5] = x + (y + 1) * w + (z + 1) * w * h;
			indices[6] = (x + 1) + (y + 1) * w + (z + 1) * w * h;
			indices[7] = (x + 1) + (y + 1) * w + z * w * h;

			bool interpolatedFilled[12] = { false };
			VoxelMeshVertex interpolatedVertices[12];

			for(int k = 0; k < 15; k++)
			{
				int index = faces[offsets[id] + k];

				if(index == -1)
					break;

				if(!interpolatedFilled[index])
				{
					Voxel v1 = voxels[indices[voxel_indices[index][0]]];
					Voxel v2 = voxels[indices[voxel_indices[index][1]]];

					float interpolation = -v1.Weight / (v2.Weight - v1.Weight);

					interpolatedVertices[index].Ambient = v1.Ambient + (v2.Ambient - v1.Ambient) * interpolation;
					interpolatedVertices[index].Position.x = v1.Position.x + (v2.Position.x - v1.Position.x) * interpolation;
					interpolatedVertices[index].Position.y = v1.Position.y + (v2.Position.y - v1.Position.y) * interpolation;
					interpolatedVertices[index].Position.z = v1.Position.z + (v2.Position.z - v1.Position.z) * interpolation;
					interpolatedVertices[index].Normal.x = v1.Normal.x + (v2.Normal.x - v1.Normal.x) * interpolation;
					interpolatedVertices[index].Normal.y = v1.Normal.y + (v2.Normal.y - v1.Normal.y) * interpolation;
					interpolatedVertices[index].Normal.z = v1.Normal.z + (v2.Normal.z - v1.Normal.z) * interpolation;

					interpolatedFilled[index] = true;
				}
			}

			int offset = offsets[id];

			for(int k = 0; k < 5; k++, offset += 3)
			{
				if(faces[offset] == -1)
					break;

				vertices[(prefixSums[in] + k) * 3] = interpolatedVertices[faces[offset]];
				vertices[(prefixSums[in] + k) * 3 + 1] = interpolatedVertices[faces[offset + 1]];
				vertices[(prefixSums[in] + k) * 3 + 2] = interpolatedVertices[faces[offset + 2]];
			}
		}
	}
}