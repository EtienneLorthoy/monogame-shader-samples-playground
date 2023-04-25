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
 
uniform texture2D HeightMap;
uniform sampler2D HeightMapSampler = sampler_state
{
    Texture = <HeightMap>;
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
    float3 tangentSpaceViewDir : TEXCOORD1;
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

	vout.tangentSpaceViewDir = mul(normalize(CameraPosition - input.Position), vout.WorldToTangentSpace);
	vout.View = normalize(CameraPosition - input.Position.xyz);

	vout.Normal = input.Normal;

	return vout;
}


//==============================================================================
// Pixel shader
//==============================================================================

const float4 AmbientColor = float4(1.0f,1.0f,1.0f,0.0f);
const float AmbientIntensity = 0.8f;
	
float3 LightDirection = float3(0.5f,0.5f,0.2f);
const float4 DiffuseColor = float4(1.0f,1.0f,0.8f,0.0f);
const float DiffuseIntensity = 1.0f;
const float4 SpecColor = float4(1.0f,1.0f,0.8f,0.0f);
const float SpecIntensity = 1.0f;

float parallaxScale = 0.01;
float parallaxBias = 0.00;

float4 parallax_mapPS(VertexOut IN,
			float3 SurfaceColor,
			float PhongExp,
			float3 SpecColor,
			float3 AmbiColor
)
{
    // view and light directions
    float3 Vn = normalize(IN.Position);
    float3 Ln = normalize(LightDirection);
    float2 uv = IN.TexCoord;
    // parallax code
    float3x3 tbnXf = IN.WorldToTangentSpace;
    float4 reliefTex = tex2D(HeightMapSampler,uv);
    float height = reliefTex.w * 0.06 - 0.03;
    uv += height * mul(tbnXf,Vn).xy;
    // normal map
    float3 tNorm = reliefTex.xyz - float3(0.5,0.5,0.5);
    // transform tNorm to world space
    tNorm = normalize(tNorm.x*IN.WorldToTangentSpace[0] -
		      tNorm.y*IN.WorldToTangentSpace[1] + 
		      tNorm.z*IN.WorldToTangentSpace[2]);
    float3 texCol = tex2D(ColorMapSampler,uv).xyz;
    // compute diffuse and specular terms
    float att = saturate(dot(Ln,IN.Normal));
    float diff = saturate(dot(Ln,tNorm));
    float spec = saturate(dot(normalize(Ln-Vn),tNorm));
    spec = pow(spec,PhongExp);
    // compute final color
    float3 finalcolor = AmbiColor*texCol +
	    att*(texCol*SurfaceColor.xyz*diff+SpecColor*spec);
    return float4(finalcolor.rgb,1.0);
}

float4 PS(VertexOut input) : SV_TARGET
{
	// float4 result = parallax_mapPS(input, float3(1.0f,1.0f,1.0f), 2.0f, SpecColor, AmbientColor);

	float4 diffuseTex = tex2D(ColorMapSampler, input.TexCoord);
	float height = tex2D(HeightMapSampler, input.TexCoord).r;

    float2 texcoordOffset = input.tangentSpaceViewDir.xy * height * parallaxScale;

    float2 newTexcoord = input.TexCoord + texcoordOffset;
	float depth = diffuseTex.g;
    depth = depth * parallaxScale - parallaxBias;
    float2 newOffset = (texcoordOffset * depth);
    newTexcoord -= newOffset;
    float4 result = tex2D(ColorMapSampler, newTexcoord) * diffuseTex.g;
	result.w = 1.0f;

	// Base color
	float4 baseColor = result;

	// Normal
	float3 normalMap = tex2D(NormalMapSampler, input.TexCoord).xyz*2.0f - 1.0f;
	normalMap = normalize(mul(input.WorldToTangentSpace, normalMap));
	float4 normal = float4(normalMap, 1.0f);
	// normal = float4(input.Normal, 1.0f);

	// Ambient stage
	result = baseColor*float4((AmbientColor*AmbientIntensity).xyz, 1.0f);

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