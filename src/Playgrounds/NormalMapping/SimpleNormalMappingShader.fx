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

uniform float4x4 World;
uniform float4x4 WorldViewProjection;
uniform float3 CameraPosition;

uniform texture2D ColorMap;
uniform sampler2D ColorMapSampler = sampler_state
{
    Texture = <ColorMap>;
    MinFilter = linear;
    MagFilter = linear;
    MipFilter = linear;
};
 
uniform texture2D NormalMap;
uniform sampler2D NormalMapSampler = sampler_state
{
    Texture = <NormalMap>;
    MinFilter = linear;
    MagFilter = linear;
    MipFilter = linear;
};


//==============================================================================
// Interstage structures
//==============================================================================

struct VertexIn
{
	float3 Position : POSITION;
	float3 Normal : NORMAL;
    float2 TexCoord : TEXCOORD0;
    float3 Tangent : TANGENT0;
    float3 Binormal : BINORMAL0;
};

struct VertexOut
{
	float4 Position : SV_POSITION;
    float2 TexCoord : TEXCOORD0;
	float3 View : NORMAL2;
    float3x3 WorldToTangentSpace : TEXCOORD2;
	float3 Normal: NORMAL;
};

//==============================================================================
// Vertex shader
//==============================================================================

VertexOut VS(VertexIn input)
{
	VertexOut vout = (VertexOut)0;

	vout.Position = mul(float4(input.Position, 1.0f), WorldViewProjection);		
	vout.TexCoord = input.TexCoord;
	
	vout.WorldToTangentSpace[0] = mul(normalize(input.Tangent), World);
    vout.WorldToTangentSpace[1] = mul(normalize(input.Binormal), World);
    vout.WorldToTangentSpace[2] = mul(normalize(input.Normal), World);

	vout.View = normalize(CameraPosition - input.Position.xyz);

	vout.Normal = input.Normal;

	return vout;
}


//==============================================================================
// Pixel shader
//==============================================================================

const float4 AmbientColor = float4(1.0f,1.0f,1.0f,1.0f);
const float AmbientIntensity = 0.8f;
	
float3 LightDirection = float3(2.0f,2.0f,2.0f);
const float4 DiffuseColor = float4(1.0f,1.0f,0.8f,1.0f);
const float DiffuseIntensity = 1.0f;
const float4 SpecColor = float4(1.0f,1.0f,0.8f,1.0f);
const float SpecIntensity = 1.0f;

float4 PS(VertexOut input) : SV_TARGET
{
	// Base color
	float4 baseColor = tex2D(ColorMapSampler, input.TexCoord);

	// Ambient stage (discarding the alpha channel)
	float4 result = baseColor * float4((AmbientColor*AmbientIntensity).xyz, 1.0f);
	
	// New precalculated normal for the specular and diffuse stage
	float3 normalMap = tex2D(NormalMapSampler, input.TexCoord).xyz*2.0f - 1.0f;
	normalMap = normalize(mul(input.WorldToTangentSpace, normalMap));
	float4 normal = float4(normalMap, 1.0f);

	// Diffuse stage
	float3 diffuse = saturate(dot(normal, -LightDirection));
	result += baseColor*DiffuseColor*DiffuseIntensity*float4(diffuse, 1.0f);

	// Specular stage
	float3 reflect = normalize(2*diffuse*normal - LightDirection);
	float3 specular = pow(saturate(dot(reflect,input.View)),15);
	result += baseColor*SpecColor*SpecIntensity*float4(specular, 1.0f);

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