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
	//float3 diffuse = Lambert(pixel.Diffuse, normal, toLight);
	//float3 specular = SpecularPhong(normal, toLight, toEye);

	// Diffuse lights contributions

	// Directional contribution
	float3 dirCol = (float3)0;
	if (Light0.On)
	{
		float3 toLight = normalize(-Light0.Direction.xyz);
		float3 diffuse = Lambert(pixel.Diffuse, normal, toLight);
		float3 specular = SpecularPhong(normal, toLight, toEye);
		dirCol = saturate((ambient + diffuse)*sample.rgb + specular)*Light0.Color.rgb;
	}

	// Point light contribution
	float3 pointCol = (float3)0;
	if (Light1.On)
	{
		float3 toLight = normalize(Light1.Direction.xyz - pixel.WorldPosition);
		float3 diffuse = Lambert(pixel.Diffuse, normal, toLight);
		float3 specular = SpecularPhong(normal, toLight, toEye);
		pointCol = saturate((ambient + diffuse)*sample.rgb + specular)*Light1.Color.rgb;
	}

	// Spot contribution
	float3 spotCol = (float3)0;
	if (Light2.On)
	{
		float3 toLight = normalize(Light2.Direction.xyz - pixel.WorldPosition);
		float3 diffuse = Lambert(pixel.Diffuse, normal, toLight);
		float3 specular = SpecularPhong(normal, toLight, toEye);

		float3 spotDir = normalize(float3(Light2.Direction.x, Light2.Direction.y, Light2.Direction.z));
		float spotCutoff = 30;
		float spotExponent = 0.000001;
		float attenuation = SpotLight(normal, toLight, toEye, spotDir, spotCutoff, spotExponent);
		spotCol = saturate((ambient + diffuse)*sample.rgb+ specular)*Light2.Color.rgb* attenuation;
	}


	//final color. we saturate, but do not for HDR rendering
	float3 color = (dirCol + pointCol + spotCol) + emissive;

	//float3 color = saturate((ambient + diffuse)*sample.rgb + specular)*Light0.Color.rgb + emissive;

	// final alpha
	float alpha = pixel.Diffuse.a*sample.a;
	return float4(color, alpha); 
}