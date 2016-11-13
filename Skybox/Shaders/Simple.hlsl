#include "Common.hlsl"

float4 PSMain(PixelShaderInput pixel): SV_Target
{
	return pixel.Diffuse;
}