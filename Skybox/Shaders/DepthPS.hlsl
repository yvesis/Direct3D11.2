#include "Common.hlsl"

float4 PSMain(PixelShaderInput pixel):SV_Target
{
	// Items close to the near clip plane will be darker

	float4 output = float4(pixel.Position.z, 0, 0, 1);
	return output;
}