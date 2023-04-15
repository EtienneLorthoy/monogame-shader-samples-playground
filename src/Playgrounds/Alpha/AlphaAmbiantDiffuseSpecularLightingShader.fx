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
float3 CameraPosition;

//==============================================================================
// Interstage structures
//==============================================================================
struct VertexIn
{
	float3 Position : POSITION;
	float3 Normal : NORMAL;
};

struct VertexOut
{
	float4 Position : SV_POSITION;
	float3 Normal : NORMAL;
	float3 View : NORMAL2;
};

//==============================================================================
// Vertex shader
//==============================================================================

VertexOut VS(VertexIn input)
{
	VertexOut vout = (VertexOut)0;

	vout.Position = mul(float4(input.Position, 1.0f), WorldViewProjection);		
	vout.Normal = normalize(input.Normal);
	vout.View = normalize(CameraPosition - input.Position.xyz);

	return vout;
}

//==============================================================================
// Pixel shader
//==============================================================================

const float4 AmbientColor = float4(0.0f,0.5f,0.0f,1.0f);
const float AmbientIntensity = 1.0f;
	
float3 LightDirection = float3(0.5f,0.5f,0.2f);
const float4 DiffuseColor = float4(1.0f,1.0f,0.8f,1.0f);
const float DiffuseIntensity = 1.0f;
const float4 SpecColor = float4(1.0f,1.0f,0.8f,1.0f);
const float SpecIntensity = 1.0f;

float4 PS(VertexOut input) : SV_TARGET
{
	// Ambient
	float4 result = AmbientColor*AmbientIntensity;

	// Diffuse
	float3 diffuse = saturate(dot(input.Normal, -LightDirection));
	result += DiffuseColor*DiffuseIntensity*float4(diffuse, 0.2f);

	// Specular
	float3 reflect = normalize(2*diffuse*input.Normal - LightDirection);
	float3 specular = pow(saturate(dot(reflect,input.View)),15);
	result += SpecColor*SpecIntensity*float4(specular, 0.2f);

	result.a = 0.2f;
 
	return result;
}

float4 PSOpaque(VertexOut input) : SV_TARGET
{
	float4 result = PS(input);
	result.a = 1.0f;
	return result;
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
technique TechniqueOpaque
{
	pass P0
	{
		VertexShader = compile VS_SHADERMODEL VS();
		PixelShader = compile PS_SHADERMODEL PSOpaque();
	}
};