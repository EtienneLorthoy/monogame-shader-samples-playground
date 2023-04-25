//==============================================================================
// Interstage structures
//==============================================================================

struct VertexIn
{
	float3 Position : POSITION0;
};

struct VertexOut
{
	float4 Position : SV_POSITION;
};

//==============================================================================
// Pixel shader 
//==============================================================================
float4 PS(VertexOut input) : SV_TARGET
{
	return float4(1,1,0,1);
}

//==============================================================================
// Vertex shader
//==============================================================================

VertexOut VS(in VertexIn input)
{
	VertexOut output;
    output.Position = float4(input.Position, 1);
	return output;
}

//==============================================================================
// Techniques
//==============================================================================
technique Technique0
{
	pass P0
	{
		VertexShader = compile vs_4_0 VS();
		PixelShader = compile ps_4_0 PS();
	}
};