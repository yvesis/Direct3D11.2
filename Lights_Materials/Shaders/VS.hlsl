#include "Common.hlsl"

PixelShaderInput VSMain(VertexShaderInput vertex)
{
	PixelShaderInput result = (PixelShaderInput)0;

	// Apply MVP
	result.Position = mul(vertex.Position, MVP);
	result.Diffuse = vertex.Color* MaterialDiffuse;

	//Apply material UV transformation
	result.TextureUV = mul(float4(vertex.TextureUV.x, vertex.TextureUV.y, 0, 1), (float4x2)UVTransform).xy;

	// Transform normal to world space
	result.WorldNormal = mul(vertex.Normal, ITM);
	result.WorldPosition = mul(vertex.Position, M).xyz;

	return result;
}