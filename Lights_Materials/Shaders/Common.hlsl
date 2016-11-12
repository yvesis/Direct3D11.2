
// Vertex shader structure

struct VertexShaderInput
{
	float4 Position:SV_Position;
	float3 Normal:NORMAL;
	float4 Color:COLOR;
	float2 TextureUV:TEXCOORD;
};

struct PixelShaderInput
{
	float4 Position:SV_Position;
	float4 Diffuse:COLOR;
	float2 TextureUV:TEXCOORD;
	// Normal and Position in wordl space for lighting computation
	float3 WorldNormal:NORMAL;
	float3 WorldPosition:WORLDPOS;
};

// Constant buffer updated per object
cbuffer PerObjet:register(b0)
{
	float4x4 MVP;
	float4x4 M;// World matrix for light computation in world space

	// Inverse transpose of M used for bringing normals into world
	// space, especially where non-uniform scaling has been applied
	float4x4 ITM // Mat Normales


};
cbuffer PerFrame:register(b1)
{
	float3 CameraPosition;
}