#include "Common.hlsl"

PixelShaderInput VSMain(VertexShaderInput vertex)
{
	PixelShaderInput result = (PixelShaderInput)0;

	// Apply MVP
	result.Position = mul(vertex.Position, MVP);
	result.Diffuse = vertex.Color;
	result.TextureUV = vertex.TextureUV;

	// Transform normal to world space
	result.WorldNormal = mul(vertex.Normal, ITM);
	result.WorldPosition = mul(vertex.Position, M).xyz;

	return result;
}