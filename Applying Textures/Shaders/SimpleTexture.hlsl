//Type	Register Description
//b	Constant buffer
//t	Texture and texture buffer
//c	Buffer offset
//s	Sampler
//u	Unordered Access View

//Constant buffer by application frame

cbuffer PerObject:register(b0)
{
	//MVP

	float4x4 MVP;
};

// Global for texture sampling
Texture2D shaderTexture:register(t0);
SamplerState Sampler:register(s0);

// VS input structure

struct VertexShaderInput
{
	float4 Position: SV_Position;
	float2 TextureUV: TEXCOORD0;
};

//VS output structure

struct VertexShaderOutput
{
	float4 Position: SV_Position;
	float2 TextureUV : TEXCOORD0;
};

//VS main function

VertexShaderOutput VSMain(VertexShaderInput input)
{
	VertexShaderOutput output= (VertexShaderOutput)0;

	// Transform position homogeneous projection space
	output.Position = mul(input.Position, MVP);

	// Pass through the color
	output.TextureUV = input.TextureUV;

	return output;
}

//PS shader main function
float4 PSMain(VertexShaderOutput input) : SV_Target
{
	return shaderTexture.Sample(Sampler,input.TextureUV);
}