
// Vertex shader input structure

struct VertexShaderInput
{
	float4 Position:SV_Position;
	float3 Normal:NORMAL;
	float4 Color:COLOR;
	float2 TextureUV:TEXCOORD;
};

// Pixel shader input structure
struct PixelShaderInput
{
	float4 Position:SV_Position;
	float4 Diffuse:COLOR;
	float2 TextureUV:TEXCOORD;
	// Normal and Position in wordl space for lighting computation
	float3 WorldNormal:NORMAL;
	float3 WorldPosition:WORLDPOS;
};

// Simple directional light
struct Light
{
	float4 Color;
	float3 Direction;
	bool On;
};

// Constant buffer updated per object
cbuffer PerObjet:register(b0)
{
	float4x4 MVP;
	float4x4 M;// World matrix for light computation in world space

	// Inverse transpose of M used for bringing normals into world
	// space, especially where non-uniform scaling has been applied
	float4x4 ITM; // Mat Normales


};

// Constant buffer updated per frame
cbuffer PerFrame:register(b1)
{
	//Light Lights[3]; //0: Directional, 1:Point light , 2: Spot
	Light Light0;
	Light Light1;
	Light Light2;

	float3 CameraPosition;

}

// Constant buffer updated per material
cbuffer PerMaterial : register(b2)
{
	float4 MaterialAmbient;
	float4 MaterialDiffuse;
	float4 MaterialSpecular;
	float Shininess;
	bool HasTexture;
	float4 MaterialEmissive;
	float4x4 UVTransform;
}

// Functions definition
float3 Lambert(float4 pixelDiffuse, float3 normal, float3 toLight)
{
	// Calculate diffuse color
	//float distance = length(toLight);
	float diffuse = saturate(dot(normal, toLight)); // saturate = clamp within 0 - 1
	return pixelDiffuse.rgb*diffuse;
}

float3 SpecularPhong(float3 normal, float3 toLight, float3 toEye)
{
	float3 r = normalize(reflect(-toLight, normal));
	float rdotEye = saturate(dot(toEye, r));
	float specular = pow(rdotEye, max(Shininess, 0.0001f));

	return MaterialSpecular.rgb*specular;
}

float3 BlinnPhongSpecular(float3 normal, float3 toLight, float3 toEye)
{
	float3 hv = normalize(toEye + toLight);
	float ndothv = saturate(dot(normal, hv));
	float specular = pow(ndothv, max(Shininess, 0.0001f));

	return MaterialSpecular.rgb*specular;
}

float3 SpotLight(float3 normal, float3 toLight, float3 toEye, float3 spotDir, float spotCutoff, float spotExponent)
{
	float ndotl = saturate(dot(normal, toLight));
	float cosAngle = dot(spotDir, toLight);
	float angleSpot = acos(cosAngle);

	float spotAttenuation = 0;
	if (cosAngle > cos(radians(spotCutoff)))
		spotAttenuation = pow(cosAngle, spotExponent);

	return spotAttenuation*ndotl;
}
void GetLights(out Light Lights[3])
{
	Lights[0] = Light0;
	Lights[1] = Light1;
	Lights[2] = Light2;

}