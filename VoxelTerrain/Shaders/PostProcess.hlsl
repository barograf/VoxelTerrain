// Input and output textures.
Texture2D<float4> Input0 : register(t0);
Texture2D<float4> Input1 : register(t1);
RWTexture2D<float4> Output;

// Pixel weights used in blur effect.
static const float BlurWeights[13] =
{
	0.002216,
	0.008764,
	0.026995,
	0.064759,
	0.120985,
	0.176033,
	0.199471,
	0.176033,
	0.120985,
	0.064759,
	0.026995,
	0.008764,
	0.002216,
};

// Pixel positions used in blur effect.
static const uint2 PixelKernel[13] =
{
	{-6, 0},
	{-5, 0},
	{-4, 0},
	{-3, 0},
	{-2, 0},
	{-1, 0},
	{0, 0},
	{1, 0},
	{2, 0},
	{3, 0},
	{4, 0},
	{5, 0},
	{6, 0},
};

// Pixel positions used in down sample effect.
static const float2 PixelCoordsDownFilter[16] =
{
	{1.5, -1.5},
	{1.5, -0.5},
	{1.5, 0.5},
	{1.5, 1.5},
	{0.5, -1.5},
	{0.5, -0.5},
	{0.5, 0.5},
	{0.5, 1.5},
	{-0.5, -1.5},
	{-0.5, -0.5},
	{-0.5, 0.5},
	{-0.5, 1.5},
	{-1.5, -1.5},
	{-1.5, -0.5},
	{-1.5, 0.5},
	{-1.5, 1.5},
};

// Constant buffer with input variables.
cbuffer buffer
{
	// Screen space width and height.
	// X = Width; Y = Height
	float4 xViewport;

	// Bloom effect settings.
	// X = Luminance; Y = MiddleGray; Z = WhiteCutoff; W = GlowPower
	float4 xBloomSettings;
}

SamplerState SamplerDefault
{
	Filter = MIN_MAG_MIP_LINEAR;
	AddressU = Wrap;
	AddressV = Wrap;
	AddressW = Wrap;
};

// Down samples a texture 4 times averaging color values.
[numthreads(32, 32, 1)]
void DownSample4x(uint3 threadID : SV_DispatchThreadID)
{
	float4 Color = 0;

	[unroll]
	for (int i = 0; i < 16; i++)
	{
		Color += Input0[(threadID.xy + PixelCoordsDownFilter[i]) * 4.0f];
	}

	Color /= 16;
	Color.a = 1;

	Output[threadID.xy] = Color;
}

// Blurs texture vertically.
[numthreads(32, 32, 1)]
void BlurV(uint3 threadID : SV_DispatchThreadID)
{
	float4 color = 0;

	[unroll]
	for(int i = 0; i < 13; i++)
	{
		color += Input0[threadID.xy + PixelKernel[i].xy] * BlurWeights[i];
	}

	color *= xBloomSettings.w;
	color.a = 1;

	Output[threadID.xy] = color;
}

// Blurs texture horizontally.
[numthreads(32, 32, 1)]
void BlurH(uint3 threadID : SV_DispatchThreadID)
{
	float4 color = 0;

	[unroll]
	for(int i = 0; i < 13; i++)
	{
		color += Input0[threadID.xy + PixelKernel[i].yx] * BlurWeights[i];
	}

	color *= xBloomSettings.w;
	color.a = 1;

	Output[threadID.xy] = color;
}

// Brightens bright areas of a texture and darkens rest of the image.
[numthreads(32, 32, 1)]
void BrightPass(uint3 threadID : SV_DispatchThreadID)
{
	float4 color = Input0[threadID.xy];

	color *= xBloomSettings.y / (xBloomSettings.x + 0.001f);
	color *= (1.0f + (color / (xBloomSettings.z * xBloomSettings.z)));
	color -= 5.0f;
	color = max(color, 0.0f);
	color /= (10.0f + color);
	color = float4(color.xyz, 1.0f);

	Output[threadID.xy] = color;
}

// Up samples texture 4 times.
[numthreads(32, 32, 1)]
void UpSample4x(uint3 threadID : SV_DispatchThreadID)
{
	float4 result = Input0.SampleLevel(SamplerDefault, 4.0f * threadID.xy / xViewport.xy, 0);
	Output[threadID.xy] = result;
}

// Up samples texture 4 times and adds it to the second one.
[numthreads(32, 32, 1)]
void UpSample4xCombine(uint3 threadID : SV_DispatchThreadID)
{
	float4 result = Input0[threadID.xy];
	result += Input1.SampleLevel(SamplerDefault, threadID.xy / xViewport.xy, 0);
	result.a = 1;
	Output[threadID.xy] = result;
}

// Adds fog texture to an input texture.
[numthreads(32, 32, 1)]
void AddFogTexture(uint3 threadID : SV_DispatchThreadID)
{
	float4 result = 0;
	float4 c = Input1[threadID.xy];

	if(c.r == 0 && c.g == 0 && c.b == 0)
		result += Input0[threadID.xy];
	else
		result += c;

	result.a = 1;

	Output[threadID.xy] = result;
}