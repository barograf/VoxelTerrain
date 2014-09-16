// Input texture.
texture2D xTexture;

// Rendering scale factor.
float xScale;

SamplerState SamplerDefault
{
	Filter = MIN_MAG_MIP_LINEAR;
	AddressU = Wrap;
	AddressV = Wrap;
	AddressW = Wrap;
};

struct VS_OUTPUT_PS_INPUT
{
	float4 Position : SV_POSITION;
	float2 UV: TEXCOORD0;
};

// Renders quad on entire screen.
VS_OUTPUT_PS_INPUT QuadVS(uint id : SV_VertexID)
{
	VS_OUTPUT_PS_INPUT result;

	result.UV = float2(id & 1, (id & 2) >> 1);
	result.Position = float4(result.UV * float2(2.0f, -2.0f) + float2(-1.0f, 1.0f), 0.0f, 1.0f);

	return result;
}

// Samples input texture onto rendered quad.
float4 QuadPS(VS_OUTPUT_PS_INPUT input) : SV_Target
{
	return xTexture.Sample(SamplerDefault, input.UV / xScale);
}

technique11 Default
{
	pass P0
	{
		SetHullShader(0);
		SetDomainShader(0);
		SetGeometryShader(0);
		SetVertexShader(CompileShader(vs_5_0, QuadVS()));
		SetPixelShader(CompileShader(ps_5_0, QuadPS()));
	}
}