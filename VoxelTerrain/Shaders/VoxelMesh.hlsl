// Display matrices.
float4x4 xWorld;
float4x4 xView;
float4x4 xProjection;

// Vector with geometry tessellation settings.
// X = minimum tessellation; Y = maximum tessellation; Z = minimum distance; W = maximum distance
float4 xTessellationFactor;

// Defines intensity of each light used in a scene.
// X = first light; Y = second light; Z = third light
float3 xDiffuseIntensity;

// Defines specular intensity of each light used in a scene.
// X = first light; Y = second light; Z = third light
float3 xSpecularIntensity;

// Defines specular range of each light used in a scene.
// X = first light; Y = second light; Z = third light
float3 xSpecularRange;

// Contains settings of bump power used in a shader.
// X = first texture; Y = second texture; Z = third texture; W = detail texture
float4 xBumpPower;

// Defines density of each texture pack's coordinates.
// X = first texture; Y = second texture; Z = third texture; W = detail texture
float4 xTextureCoordinates;

// Defines displacement power for each texture pack.
// X = first texture; Y = second texture; Z = third texture
float3 xDisplacementPower;

// Directional lights used in a scene.
float3 xDiffuseLight1;
float3 xDiffuseLight2;
float3 xDiffuseLight3;

// Scene camera position.
float3 xCameraPosition;

// Scene camera frustum planes used in frustum culling algorithm.
float4 xFrustumPlanes[4];

// First texture pack.
texture2D xTexture1Color;
texture2D xTexture1Bump;
texture2D xTexture1Disp;

// Second texture pack.
texture2D xTexture2Color;
texture2D xTexture2Bump;
texture2D xTexture2Disp;

// Third texture pack.
texture2D xTexture3Color;
texture2D xTexture3Bump;
texture2D xTexture3Disp;

// Detail texture pack.
texture2D xTextureDetailColor;
texture2D xTextureDetailBump;

// Texture which is used to slightly colorize terrain to make it more varied.
texture3D xColorizationTexture;

// Texture with rendered fog. It needs to be updated every frame.
texture2D xFogTexture;

// Contains linear fog settings.
// X = minimum fog distance; Y = maximum fog distance
float2 xFogSettings;

// Screen space width and height.
float2 xViewport;

// Computes distance between plane and a point in 3D space.
// position : point in 3D space
// planeEquation : eqation of a plane
// returns : distance as a float
float DistanceFromPlane(float3 position, float4 planeEquation)
{
	return dot(float4(position, 1), planeEquation);
}

// Checks if a triangle is in within a camera frustum.
// edgePosition1 : first triangle edge
// edgePosition2 : second triangle edge
// edgePosition3 : third triangle edge
// frustumPlanes : left, right, top and bottom planes equations
// epsilon : distance epsilon
// returns : true if a triangle is in a frustum
bool ViewFrustumCull(float3 edgePosition1, float3 edgePosition2, float3 edgePosition3, float4 frustumPlanes[4], float epsilon)
{    
	float4 planeTests;
	
	// Left clip plane.
	planeTests.x =	((DistanceFromPlane(edgePosition1, frustumPlanes[0]) > -epsilon) ? 1 : 0) +
					((DistanceFromPlane(edgePosition2, frustumPlanes[0]) > -epsilon) ? 1 : 0) +
					((DistanceFromPlane(edgePosition3, frustumPlanes[0]) > -epsilon) ? 1 : 0);

	// Right clip plane.
	planeTests.y =	((DistanceFromPlane(edgePosition1, frustumPlanes[1]) > -epsilon) ? 1 : 0) +
					((DistanceFromPlane(edgePosition2, frustumPlanes[1]) > -epsilon) ? 1 : 0) +
					((DistanceFromPlane(edgePosition3, frustumPlanes[1]) > -epsilon) ? 1 : 0);

	// Top clip plane.
	planeTests.z =	((DistanceFromPlane(edgePosition1, frustumPlanes[2]) > -epsilon) ? 1 : 0) +
					((DistanceFromPlane(edgePosition2, frustumPlanes[2]) > -epsilon) ? 1 : 0) +
					((DistanceFromPlane(edgePosition3, frustumPlanes[2]) > -epsilon) ? 1 : 0);

	// Bottom clip plane.
	planeTests.w =	((DistanceFromPlane(edgePosition1, frustumPlanes[3]) > -epsilon) ? 1 : 0) +
					((DistanceFromPlane(edgePosition2, frustumPlanes[3]) > -epsilon) ? 1 : 0) +
					((DistanceFromPlane(edgePosition3, frustumPlanes[3]) > -epsilon) ? 1 : 0);
	
	return !all(planeTests);
}

SamplerState SamplerDefault
{
	Filter = MIN_MAG_MIP_LINEAR;
	AddressU = Wrap;
	AddressV = Wrap;
	AddressW = Wrap;
};

struct VS_INPUT
{
	float3 Position : POSITION;
	float3 Normal : NORMAL;
	float Ambient : AMBIENT;
};

struct VS_OUTPUT_HS_INPUT
{
	float3 Position : POSITION;
	float3 Normal : NORMAL;
	float3 UV : UV;
	float Ambient : AMBIENT;
	float3 ViewDirection : VIEWDIRECTION;
	float DistanceFactor : DISTANCEFACTOR;
	float FogFactor : FOGFACTOR;
};

struct HS_CONTROL_POINT_OUTPUT_DS_INPUT
{
	float3 Position : POSITION;
	float3 Normal : NORMAL;
	float3 UV : UV;
	float Ambient : AMBIENT;
	float3 ViewDirection : VIEWDIRECTION;
	float FogFactor : FOGFACTOR;
};

struct HS_CONSTANT_DATA_OUTPUT_DS_INPUT
{
	float Edges[3] : SV_TessFactor;
	float Inside : SV_InsideTessFactor;
};

struct DS_OUTPUT_GS_INPUT
{
	float3 Position : POSITION;
	float3 Normal : NORMAL;
	float3 UV : UV;
	float Ambient : AMBIENT;
	float3 ViewDirection : VIEWDIRECTION;
	float FogFactor : FOGFACTOR;
	float3 BlendWeights : BLENDWEIGHTS;
};

struct GS_OUTPUT_PS_INPUT
{
	float4 Position : SV_POSITION;
	float3 Normal : NORMAL;
	float3 UV : UV;
	float Ambient : AMBIENT;
	float3 ViewDirection : VIEWDIRECTION;
	float FogFactor : FOGFACTOR;
	float3 BlendWeights : BLENDWEIGHTS;
};

// Vertex shader.
VS_OUTPUT_HS_INPUT VoxelMeshVS(VS_INPUT input)
{
	VS_OUTPUT_HS_INPUT output;

	output.UV = input.Position;
	output.Position = mul(input.Position, xWorld);
	output.Normal = mul(input.Normal, xWorld);

	// Distance factor used as a tessellation factor.
	output.DistanceFactor = 1.0 - clamp(((distance(output.Position, xCameraPosition) - xTessellationFactor.z) / (xTessellationFactor.w - xTessellationFactor.z)), 0.0, 1.0 - xTessellationFactor.x / xTessellationFactor.y);
	
	output.Ambient = input.Ambient;

	// View direction used in specular lighting computation.
	output.ViewDirection = normalize(xCameraPosition - output.Position);

	// Linear fog factor.
	output.FogFactor = saturate((xFogSettings.y - distance(output.Position, xCameraPosition)) / (xFogSettings.y - xFogSettings.x));

	return output;
}

// Hull shader patch constant function.
HS_CONSTANT_DATA_OUTPUT_DS_INPUT VoxelMeshConstantHS(InputPatch<VS_OUTPUT_HS_INPUT, 3> ip, uint PatchID : SV_PrimitiveID)
{
	HS_CONSTANT_DATA_OUTPUT_DS_INPUT output;

	float4 factor;
	factor.x = 0.5 * (ip[1].DistanceFactor + ip[2].DistanceFactor);
	factor.y = 0.5 * (ip[2].DistanceFactor + ip[0].DistanceFactor);
	factor.z = 0.5 * (ip[0].DistanceFactor + ip[1].DistanceFactor);
	factor.w = factor.x;
	factor *= xTessellationFactor.y;
	
	output.Edges[0] = factor.x;
	output.Edges[1] = factor.y;
	output.Edges[2] = factor.z;
	output.Inside = factor.w;

	// View frustum culling.
	if (ViewFrustumCull(ip[0].Position, ip[1].Position, ip[2].Position, xFrustumPlanes, 1))
	{
		output.Edges[0] = 0;
		output.Edges[1] = 0;
		output.Edges[2] = 0;
		output.Inside = 0;
	}

	return output;
}

// Hull shader.
[domain("tri")]
[partitioning("fractional_even")]
[outputtopology("triangle_cw")]
[outputcontrolpoints(3)]
[patchconstantfunc("VoxelMeshConstantHS")]
HS_CONTROL_POINT_OUTPUT_DS_INPUT VoxelMeshHS(InputPatch<VS_OUTPUT_HS_INPUT, 3> ip, uint i : SV_OutputControlPointID, uint PatchID : SV_PrimitiveID)
{
	HS_CONTROL_POINT_OUTPUT_DS_INPUT output;

	output.Position = ip[i].Position;
	output.Normal = ip[i].Normal;
	output.UV = ip[i].UV;
	output.Ambient = ip[i].Ambient;
	output.ViewDirection = ip[i].ViewDirection;
	output.FogFactor = ip[i].FogFactor;

	return output;
}

// Domain shader.
[domain("tri")]
DS_OUTPUT_GS_INPUT VoxelMeshDS(HS_CONSTANT_DATA_OUTPUT_DS_INPUT input, float3 BarycentricCoordinates : SV_DomainLocation, const OutputPatch<HS_CONTROL_POINT_OUTPUT_DS_INPUT, 3> TrianglePatch)
{
	DS_OUTPUT_GS_INPUT output;

	float3 position = BarycentricCoordinates.x * TrianglePatch[0].Position + BarycentricCoordinates.y * TrianglePatch[1].Position + BarycentricCoordinates.z * TrianglePatch[2].Position;
	float3 normal = BarycentricCoordinates.x * TrianglePatch[0].Normal + BarycentricCoordinates.y * TrianglePatch[1].Normal + BarycentricCoordinates.z * TrianglePatch[2].Normal;
	float3 uv = BarycentricCoordinates.x * TrianglePatch[0].UV + BarycentricCoordinates.y * TrianglePatch[1].UV + BarycentricCoordinates.z * TrianglePatch[2].UV;
	float ambient = BarycentricCoordinates.x * TrianglePatch[0].Ambient + BarycentricCoordinates.y * TrianglePatch[1].Ambient + BarycentricCoordinates.z * TrianglePatch[2].Ambient;
	float3 viewDirection = BarycentricCoordinates.x * TrianglePatch[0].ViewDirection + BarycentricCoordinates.y * TrianglePatch[1].ViewDirection + BarycentricCoordinates.z * TrianglePatch[2].ViewDirection;
	float3 fogFactor = BarycentricCoordinates.x * TrianglePatch[0].FogFactor + BarycentricCoordinates.y * TrianglePatch[1].FogFactor + BarycentricCoordinates.z * TrianglePatch[2].FogFactor;
	
	output.Position = position;
	output.UV = uv;
	output.Normal = normal;
	output.Ambient = ambient;
	output.ViewDirection = viewDirection;
	output.FogFactor = fogFactor;

	// Blend weights used in tri-planar texturing.
	output.BlendWeights = abs(normal);
	output.BlendWeights = (output.BlendWeights - 0.2) * 7;
	output.BlendWeights = pow(output.BlendWeights, 3);
	output.BlendWeights = max(output.BlendWeights, 0);
	output.BlendWeights /= dot(output.BlendWeights, 1);

	// Samples displacement values from textures.
	float dispMapMIPLevel = clamp((distance(position, xCameraPosition) - 15.0f) / 5.0f, 0.0f, 3.0f);
	float4 disp1 = (xTexture1Disp.SampleLevel(SamplerDefault, uv.yz / xTextureCoordinates.x, dispMapMIPLevel) - 0.5) * xDisplacementPower.x;
	float4 disp2 = (xTexture2Disp.SampleLevel(SamplerDefault, uv.xz / xTextureCoordinates.y, dispMapMIPLevel) - 0.5) * xDisplacementPower.y;
	float4 disp3 = (xTexture3Disp.SampleLevel(SamplerDefault, uv.xy / xTextureCoordinates.z, dispMapMIPLevel) - 0.5) * xDisplacementPower.z;

	// Changes vertex positions using displacement values.
	output.Position += normal * (disp1 * output.BlendWeights.x + disp2 * output.BlendWeights.y + disp3 * output.BlendWeights.z);

	return output;
}

// Geometry shader.
[maxvertexcount(3)]
void VoxelMeshGS(triangle DS_OUTPUT_GS_INPUT input[3], inout TriangleStream<GS_OUTPUT_PS_INPUT> outputStream)
{	
	GS_OUTPUT_PS_INPUT output;

	float4x4 ViewProjection = mul(xView, xProjection);

	[unroll]
	for(int i = 0; i < 3; i++)
	{
		output.Position = mul(float4(input[i].Position, 1), ViewProjection);
		output.Normal = input[i].Normal;
		output.UV = input[i].UV;
		output.Ambient = input[i].Ambient;
		output.ViewDirection = input[i].ViewDirection;
		output.FogFactor = input[i].FogFactor;
		output.BlendWeights = input[i].BlendWeights;
	
		outputStream.Append(output);
	}
}

// Pixel shader.
float4 VoxelMeshPS(GS_OUTPUT_PS_INPUT input) : SV_Target
{
	// Detail texture bump values.
	float2 bumpFetch1 = xTextureDetailBump.Sample(SamplerDefault, input.UV.yz / xTextureCoordinates.w).xy - 0.5;  
	float2 bumpFetch2 = xTextureDetailBump.Sample(SamplerDefault, input.UV.zx / xTextureCoordinates.w).xy - 0.5;  
	float2 bumpFetch3 = xTextureDetailBump.Sample(SamplerDefault, input.UV.xy / xTextureCoordinates.w).xy - 0.5;  

	// Tangent, bitangent, normal values.
	float3 bump1 = float3(0, bumpFetch1.x, bumpFetch1.y);  
	float3 bump2 = float3(bumpFetch2.y, 0, bumpFetch2.x);  
	float3 bump3 = float3(bumpFetch3.x, bumpFetch3.y, 0); 
	
	// Computes bump vector using tri-planar texturing.
	float3 BumpVector = (bump1 * input.BlendWeights.x + bump2 * input.BlendWeights.y + bump3 * input.BlendWeights.z) * xBumpPower.w;

	// Color textures bump values.
	bumpFetch1 = (xTexture1Bump.Sample(SamplerDefault, input.UV.yz / xTextureCoordinates.x).xy - 0.5) * xBumpPower.x;
	bumpFetch2 = (xTexture2Bump.Sample(SamplerDefault, input.UV.zx / xTextureCoordinates.y).xy - 0.5) * xBumpPower.y;
	bumpFetch3 = (xTexture3Bump.Sample(SamplerDefault, input.UV.xy / xTextureCoordinates.z).xy - 0.5) * xBumpPower.z;

	// Tangent, bitangent, normal values.
	bump1 = float3(0, bumpFetch1.x, bumpFetch1.y);
	bump2 = float3(bumpFetch2.y, 0, bumpFetch2.x);
	bump3 = float3(bumpFetch3.x, bumpFetch3.y, 0);

	// Computes bump vector using tri-planar texturing.
	BumpVector += bump1 * input.BlendWeights.x + bump2 * input.BlendWeights.y + bump3 * input.BlendWeights.z;

	// Adds bump vector to normal vector.
	float3 normal = normalize(input.Normal + BumpVector);

	// Fetches textures colors.
	float4 color1 = xTexture1Color.Sample(SamplerDefault, input.UV.yz / xTextureCoordinates.x);
	float4 color2 = xTexture2Color.Sample(SamplerDefault, input.UV.xz / xTextureCoordinates.y);
	float4 color3 = xTexture3Color.Sample(SamplerDefault, input.UV.xy / xTextureCoordinates.z);

	// Fetches detail texture colors.
	float4 detail1 = xTextureDetailColor.Sample(SamplerDefault, input.UV.yz / xTextureCoordinates.w);
	float4 detail2 = xTextureDetailColor.Sample(SamplerDefault, input.UV.xz / xTextureCoordinates.w);
	float4 detail3 = xTextureDetailColor.Sample(SamplerDefault, input.UV.xy / xTextureCoordinates.w);

	color1 *= pow(detail1, 2);
	color2 *= pow(detail2, 2);
	color3 *= pow(detail3, 2);

	float4 Ambient = input.Ambient;

	// Computes color using tri-planar texturing.
	float4 Color = color1 * input.BlendWeights.x + color2 * input.BlendWeights.y + color3 * input.BlendWeights.z;

	// Computes diffuse lighting.
	float4 Diffuse = float4(1, 1, 1, 1) * saturate(dot(normal, -normalize(xDiffuseLight1))) * xDiffuseIntensity.x;
	Diffuse += float4(1, 1, 1, 1) * saturate(dot(normal, -normalize(xDiffuseLight2))) * xDiffuseIntensity.y;
	Diffuse += float4(1, 1, 1, 1) * saturate(dot(normal, -normalize(xDiffuseLight3))) * xDiffuseIntensity.z;
	Diffuse /= 3;

	// Computes specular lighting.
	float3 R = normalize(2 * dot(input.ViewDirection, normal) * normal - input.ViewDirection);
	float RL1 = saturate(dot(R, -normalize(xDiffuseLight1)));
	float RL2 = saturate(dot(R, -normalize(xDiffuseLight2)));
	float RL3 = saturate(dot(R, -normalize(xDiffuseLight3)));
	float Specular = pow(RL1, xSpecularRange.x) * xSpecularIntensity.x + pow(RL2, xSpecularRange.y) * xSpecularIntensity.y + pow(RL3, xSpecularRange.z) * xSpecularIntensity.z;
	Specular /= 3;

	// This is the final color.
	float4 Result = Ambient * (float4(1, 1, 1, 1) + Diffuse + Specular) * Color;
	
	// Add a little random color.
	float4 colorization = xColorizationTexture.Sample(SamplerDefault, input.UV.xyz / 512);
	Result *= 0.9 + 0.1 * colorization;

	// Add fog color.
	float4 Fog = xFogTexture.Sample(SamplerDefault, input.Position.xy / xViewport.xy);
	Result = input.FogFactor * Result + (1 - input.FogFactor) * Fog;
	
	return float4(Result.xyz, 1);
}

technique11 Default
{
	pass P0
	{
		SetVertexShader(CompileShader(vs_5_0, VoxelMeshVS()));
		SetHullShader(CompileShader(hs_5_0, VoxelMeshHS()));
		SetDomainShader(CompileShader(ds_5_0, VoxelMeshDS()));
		SetGeometryShader(CompileShader(gs_5_0, VoxelMeshGS()));
		SetPixelShader(CompileShader(ps_5_0, VoxelMeshPS()));
	}
}