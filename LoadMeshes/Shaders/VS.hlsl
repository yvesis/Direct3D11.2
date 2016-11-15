#include "Common.hlsl"
bool drawSkybox : register(b3);

void SkinVertex(float4 weights, uint4 bones, inout float4 position, inout float3 normal)
{
	// If there are skin weights, apply vertex skinning
	if(weights.x!=0)
	{
		// Calculate skin transform
		float4x4 skinTransform= Bones[bones.x]*weights.x+ 
								Bones[bones.y]*weights.y+
								Bones[bones.z]*weights.z+ 
								Bones[bones.w]*weights.w;

		// Apply skinning to vertex and normal
		position = mul(position,skinTransform);

		normal = mul(normal,(float3x3)skinTransform);
	}
}
PixelShaderInput VSMain(VertexShaderInput vertex)
{
	PixelShaderInput result = (PixelShaderInput)0;

	// Apply vertex skining if any
	SkinVertex(vertex.SkinWeights,vertex.Skin,vertex.Position,vertex.Normal);
	// Apply MVP
	result.Position = mul(vertex.Position, MVP);
	result.Diffuse = vertex.Color* MaterialDiffuse;

	//Apply material UV transformation
	result.TextureUV = mul(float4(vertex.TextureUV.x, vertex.TextureUV.y, 0, 1), (float4x2)UVTransform).xy;

	// Transform normal to world space
	result.WorldNormal = mul(vertex.Normal, ITM);
	if (drawSkybox)
	result.WorldPosition = vertex.Position;// mul(-CameraPosition + vertex.Position, M).xyz;
	else
		result.WorldPosition = mul(vertex.Position, M).xyz;

	return result;
}
