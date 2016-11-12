//Constant buffer by application frame

cbuffer PerObject:register(b0)
{
	//MVP

	float4x4 MVP;
};

// VS input structure

struct VertexShaderInput
{
	float4 Position: SV_Position;
	float4 Color: COLOR;
};

//VS output structure

struct VertexShaderOutput
{
	float4 Position: SV_Position;
	float4 Color : COLOR;
};

//VS main function

VertexShaderOutput VSMain(VertexShaderInput input)
{
	VertexShaderOutput output= (VertexShaderOutput)0;

	// Transform position homogeneous projection space
	output.Position = mul(input.Position, MVP);

	// Pass through the color
	output.Color = input.Color;

	return output;
}

//PS shader main function
float4 PSMain(VertexShaderOutput input) : SV_Target
{
	return input.Color;
}