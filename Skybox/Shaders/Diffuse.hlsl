#include "Common.hlsl"

// global for textures
Texture2D Texture0:register(t0);
TextureCube CubeMap : register(t1);
SamplerState Sampler:register(s0);


bool drawSkybox : register(b3);

float4 PSMain(PixelShaderInput pixel) : SV_Target
{
	if (drawSkybox)
	{
		return CubeMap.Sample(Sampler, pixel.WorldPosition);
	}
	// Sample texture

	float4 sample = (float4)1.0f;
	if (HasTexture)
		sample = Texture0.Sample(Sampler, pixel.TextureUV);

	float3 ambient = MaterialAmbient.rgb;
	float3 emissive = MaterialEmissive.rgb;

	// Normalize normal
	float3 normal = normalize(pixel.WorldNormal);
	// Compute toEye direction
	float3 toEye = normalize(CameraPosition - pixel.WorldPosition);
	float3 color = (float3)0;

	float4 ambientCol = (float4)0;
	float3 diffuse = (float3)0;

	// Diffuse lights contributions

	// Directional contribution
	float3 dirCol = (float3)0;
	if (Light0.On)
	{
		float3 toLight = normalize(-Light0.Direction.xyz);
		diffuse = Lambert(pixel.Diffuse, normal, toLight);
		dirCol = saturate((ambient + diffuse)*sample.rgb)*Light0.Color.rgb;
	}

	// Point light contribution
	float3 pointCol = (float3)0;
	if (Light1.On)
	{
		float3 toLight = normalize(Light1.Direction.xyz - pixel.WorldPosition);
		diffuse = Lambert(pixel.Diffuse, normal, toLight);
		pointCol = saturate((ambient + diffuse)*sample.rgb)*Light1.Color.rgb;
	}

	// Spot contribution
	float3 spotCol = (float3)0;
	if (Light2.On)
	{
		float3 toLight = normalize(Light2.Direction.xyz - pixel.WorldPosition);
		diffuse = Lambert(pixel.Diffuse, normal, toLight);
		float3 spotDir = normalize(float3(Light2.Direction.x, Light2.Direction.y, Light2.Direction.z));
		float spotCutoff = 30;
		float spotExponent = 0.5;
		float attenuation = SpotLight(normal, toLight, toEye, spotDir, spotCutoff, spotExponent);
		spotCol = saturate(( diffuse* attenuation)*sample.rgb)*Light2.Color.rgb;
	}


	////final color. we saturate, but do not for HDR rendering
	////color = saturate((ambient + diffuse)*sample.rgb)*(dirCol+pointCol) + emissive ;
	color = (dirCol + pointCol + spotCol) + emissive;


	//// clamp color
	color = saturate(color);
	//// final alpha
	float alpha = pixel.Diffuse.a*sample.a;


	return float4(color, alpha);
}