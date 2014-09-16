// Display matrices.
float4x4 xWorld;
float4x4 xView;
float4x4 xProjection;

// Table with noise textures.
texture3D xNoiseTexture[4];

SamplerState SamplerDefault
{
	Filter = MIN_MAG_MIP_LINEAR;
	AddressU = Wrap;
	AddressV = Wrap;
	AddressW = Wrap;
};

struct VS_OUTPUT_GS_INPUT
{
	float4 Position : POSITION;
};

struct GS_OUTPUT_PS_INPUT
{
	float4 Position : SV_POSITION;
	float3 UV: UV;
};

// Draws dummy point.
VS_OUTPUT_GS_INPUT SkyCubeVS(uint id : SV_VertexID)
{
	VS_OUTPUT_GS_INPUT result;

	result.Position = float4(0, 0, 0, 1);

	return result;
}

// Generates cube using geometry shader.
[maxvertexcount(24)]
void SkyCubeGS(point VS_OUTPUT_GS_INPUT input[1], inout TriangleStream<GS_OUTPUT_PS_INPUT> outputStream)
{	
	GS_OUTPUT_PS_INPUT output;

	float4x4 ViewProjection = mul(xView, xProjection);

	output.UV = float3(-1, -1, -1);
	output.Position = mul(mul(float4(output.UV, 1), xWorld), ViewProjection);
	outputStream.Append(output);
	output.UV = float3(-1, -1, 1);
	output.Position = mul(mul(float4(output.UV, 1), xWorld), ViewProjection);
	outputStream.Append(output);
	output.UV = float3(-1, 1, -1);
	output.Position = mul(mul(float4(output.UV, 1), xWorld), ViewProjection);
	outputStream.Append(output);
	output.UV = float3(-1, 1, 1);
	output.Position = mul(mul(float4(output.UV, 1), xWorld), ViewProjection);
	outputStream.Append(output);
	outputStream.RestartStrip();

	output.UV = float3(1, -1, -1);
	output.Position = mul(mul(float4(output.UV, 1), xWorld), ViewProjection);
	outputStream.Append(output);
	output.UV = float3(1, -1, 1);
	output.Position = mul(mul(float4(output.UV, 1), xWorld), ViewProjection);
	outputStream.Append(output);
	output.UV = float3(1, 1, -1);
	output.Position = mul(mul(float4(output.UV, 1), xWorld), ViewProjection);
	outputStream.Append(output);
	output.UV = float3(1, 1, 1);
	output.Position = mul(mul(float4(output.UV, 1), xWorld), ViewProjection);
	outputStream.Append(output);
	outputStream.RestartStrip();

	output.UV = float3(-1, -1, -1);
	output.Position = mul(mul(float4(output.UV, 1), xWorld), ViewProjection);
	outputStream.Append(output);
	output.UV = float3(-1, -1, 1);
	output.Position = mul(mul(float4(output.UV, 1), xWorld), ViewProjection);
	outputStream.Append(output);
	output.UV = float3(1, -1, -1);
	output.Position = mul(mul(float4(output.UV, 1), xWorld), ViewProjection);
	outputStream.Append(output);
	output.UV = float3(1, -1, 1);
	output.Position = mul(mul(float4(output.UV, 1), xWorld), ViewProjection);
	outputStream.Append(output);
	outputStream.RestartStrip();

	output.UV = float3(-1, 1, -1);
	output.Position = mul(mul(float4(output.UV, 1), xWorld), ViewProjection);
	outputStream.Append(output);
	output.UV = float3(-1, 1, 1);
	output.Position = mul(mul(float4(output.UV, 1), xWorld), ViewProjection);
	outputStream.Append(output);
	output.UV = float3(1, 1, -1);
	output.Position = mul(mul(float4(output.UV, 1), xWorld), ViewProjection);
	outputStream.Append(output);
	output.UV = float3(1, 1, 1);
	output.Position = mul(mul(float4(output.UV, 1), xWorld), ViewProjection);
	outputStream.Append(output);
	outputStream.RestartStrip();

	output.UV = float3(-1, -1, -1);
	output.Position = mul(mul(float4(output.UV, 1), xWorld), ViewProjection);
	outputStream.Append(output);
	output.UV = float3(-1, 1, -1);
	output.Position = mul(mul(float4(output.UV, 1), xWorld), ViewProjection);
	outputStream.Append(output);
	output.UV = float3(1, -1, -1);
	output.Position = mul(mul(float4(output.UV, 1), xWorld), ViewProjection);
	outputStream.Append(output);
	output.UV = float3(1, 1, -1);
	output.Position = mul(mul(float4(output.UV, 1), xWorld), ViewProjection);
	outputStream.Append(output);
	outputStream.RestartStrip();

	output.UV = float3(-1, -1, 1);
	output.Position = mul(mul(float4(output.UV, 1), xWorld), ViewProjection);
	outputStream.Append(output);
	output.UV = float3(-1, 1, 1);
	output.Position = mul(mul(float4(output.UV, 1), xWorld), ViewProjection);
	outputStream.Append(output);
	output.UV = float3(1, -1, 1);
	output.Position = mul(mul(float4(output.UV, 1), xWorld), ViewProjection);
	outputStream.Append(output);
	output.UV = float3(1, 1, 1);
	output.Position = mul(mul(float4(output.UV, 1), xWorld), ViewProjection);
	outputStream.Append(output);
	outputStream.RestartStrip();
}

// Generates clouds using perlin noise algorithm.
float4 SkyCubePS(GS_OUTPUT_PS_INPUT input) : SV_Target
{
	input.UV = normalize(input.UV);

	float clouds = 0;
	clouds += xNoiseTexture[0].Sample(SamplerDefault, input.UV * 256.0f).b * 0.015625f;
	clouds += xNoiseTexture[1].Sample(SamplerDefault, input.UV * 128.0f).b * 0.03125f;
	clouds += xNoiseTexture[2].Sample(SamplerDefault, input.UV * 64.0f).b * 0.0625f;
	clouds += xNoiseTexture[3].Sample(SamplerDefault, input.UV * 32.0f).b * 0.125f;
	clouds += xNoiseTexture[0].Sample(SamplerDefault, input.UV * 16.0f).r * 0.25f;
	clouds += xNoiseTexture[1].Sample(SamplerDefault, input.UV * 8.0f).r * 0.5f;
	clouds += xNoiseTexture[2].Sample(SamplerDefault, input.UV * 4.0f).r * 1.0f;
	clouds += xNoiseTexture[3].Sample(SamplerDefault, input.UV * 2.0f).r * 2.0f;
	clouds += xNoiseTexture[0].Sample(SamplerDefault, input.UV * 1.0f).g * 4.0f;
	clouds += xNoiseTexture[1].Sample(SamplerDefault, input.UV * 0.5f).g * 8.0f;
	clouds += xNoiseTexture[2].Sample(SamplerDefault, input.UV * 0.25f).g * 16.0f;
	clouds += xNoiseTexture[3].Sample(SamplerDefault, input.UV * 0.125f).g * 32.0f;
	clouds /= 32.0f + 16.0f + 8.0f + 4.0f + 2.0f + 1.0f + 0.5f + 0.25f + 0.125f + 0.0625f + 0.03125f + 0.015625f;
	clouds = pow(clouds, 2) + 1;

	float dome = pow(cos(input.UV.y), 2.0f);

	float4 background = xNoiseTexture[0].Sample(SamplerDefault, input.UV / 64.0f);

	float4 result = clouds * background * dome;
	result.a = 1;

	return result;
}

technique11 Default
{
	pass P0
	{
		SetHullShader(0);
		SetDomainShader(0);
		SetGeometryShader(CompileShader(gs_5_0, SkyCubeGS()));
		SetVertexShader(CompileShader(vs_5_0, SkyCubeVS()));
		SetPixelShader(CompileShader(ps_5_0, SkyCubePS()));
	}
}