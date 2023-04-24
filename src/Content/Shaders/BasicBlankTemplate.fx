#if OPENGL
	#define SV_POSITION POSITION
	#define VS_SHADERMODEL vs_3_0
	#define PS_SHADERMODEL ps_3_0
#else
	#define VS_SHADERMODEL vs_4_0
	#define PS_SHADERMODEL ps_4_0
#endif

//==============================================================================
// Global parameters
//==============================================================================


float4x4 WorldViewProjection;

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
// Vertex shader
//==============================================================================

VertexOut VS(in VertexIn input)
{
    VertexOut output;
    
    output.Position = mul(float4(input.Position, 1), WorldViewProjection);
	
    return output;
}

//==============================================================================
// Pixel shader
//==============================================================================

float4 PSMain(float4 position : SV_POSITION, float3 normal : NORMAL) : SV_TARGET
{
    float3 lightDirection = normalize(float3(1, 1, 1));
    float3 viewDirection = normalize(float3(0, 0, 1));
    float3 halfVector = normalize(lightDirection + viewDirection);

    float NdotL = max(dot(normal, lightDirection), 0);
    float NdotH = max(dot(normal, halfVector), 0);

    float3 diffuse = float3(0.5, 0.5, 0.5) * NdotL;
    float3 specular = pow(NdotH, 100);

    return float4(diffuse + specular, 1);
}

float4 PS(VertexOut input) : SV_TARGET
{
	float2 pos = input.Position.xy;
	float4 color = Test(pos);
	return color;
}

//==============================================================================
// Techniques
//==============================================================================
technique Technique0
{
	pass P0
	{
		VertexShader = compile VS_SHADERMODEL VS();
		PixelShader = compile PS_SHADERMODEL PS();
	}
};