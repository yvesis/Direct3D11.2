#include "Common.hlsl"

Texture2D Texture0:register(t0);
SamplerState Sampler:register(s0);

float4 PSMain(PixelShaderInput pixel):SV_Target
{
	// Normalize normal
	float3 normal = normalize(pixel.WorldNormal);

	// Compute toEye direction
	float3 toEye = normalize(CameraPosition - pixel.WorldPosition);

	// Compute light direction (it's a directional light))
	float3 toLight = normalize(-Light0.Direction);

	// Sample texture
	float4 sample = (float4)1.0f;
	if (HasTexture)
		sample = Texture0.Sample(Sampler, pixel.TextureUV);

	float3 ambient = MaterialAmbient.rgb;
	float3 emissive = MaterialEmissive.rgb;
	float3 diffuse = Lambert(pixel.Diffuse, normal, toLight);
	float3 specular = BlinnPhongSpecular(normal, toLight, toEye);

	//final color. we saturate, but do not for HDR rendering
	float3 color = saturate((ambient + diffuse)*sample.rgb + specular)*Light0.Color.rgb + emissive;

	// final alpha
	float alpha = pixel.Diffuse.a*sample.a;


	return float4(color, alpha);
}