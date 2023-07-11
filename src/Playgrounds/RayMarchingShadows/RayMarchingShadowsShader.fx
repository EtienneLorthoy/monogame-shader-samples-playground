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

uniform float4x4 View;
uniform float4x4 WorldViewProjection;
uniform float2 ViewportSize;
uniform float3 CameraPosition;
uniform float3 CameraTarget;

uniform texture2D ColorMap;
uniform sampler2D ColorMapSampler = sampler_state
{
    Texture = <ColorMap>;
    MinFilter = linear;
    MagFilter = linear;
    MipFilter = linear;
};

//==============================================================================
// Interstage structures
//==============================================================================

struct VertexIn
{
	float3 WorldPosition : POSITION;
    float2 TexCoord : TEXCOORD0;
	float3 Normal : NORMAL;
};

struct VertexOut
{
	float4 ScreenPosition : SV_POSITION;
    float2 TexCoord : TEXCOORD0;
	float3 Ray : NORMAL1;
	float3 Normal : NORMAL2;
};

//==============================================================================
// Vertex shader
//==============================================================================

VertexOut VS(VertexIn input)
{
	VertexOut vout = (VertexOut)0;

	vout.ScreenPosition = mul(float4(input.WorldPosition, 1.0f), WorldViewProjection);		
	vout.TexCoord = input.TexCoord;
	// vout.Ray = normalize(input.WorldPosition - CameraPosition);
	vout.Ray = input.WorldPosition - CameraPosition;
	vout.Normal = input.Normal;

	return vout;
}


//==============================================================================
// Pixel shader
//==============================================================================

const float4 AmbientColor = float4(1.0f,1.0f,1.0f,0.0f);
const float AmbientIntensity = 0.8f;
	
float3 LightDirection = float3(0.5f,0.5f,0.2f);
float3 LightPosition = float3(5.0f,5.0f,2.0f);
const float4 DiffuseColor = float4(1.0f,1.0f,0.8f,0.0f);
const float DiffuseIntensity = 1.0f;
const float4 SpecColor = float4(1.0f,1.0f,0.7f,0.0f);
const float SpecIntensity = 1.0f;

const float MaxRaymarchDist = 100.0f;
const int MaxRaymarchStep = 50;
const float DistToSurfaceThreshold = 0.01f;

const float3 BoxPosition = float3(0.0f, 0.0f, 0.0f);
const float3 BoxSize = float3(0.5f, 0.5f, 0.5f);

float BoxSignedDistance(float3 position, float3 boxSize)
{
    return length(max(abs(position)-boxSize, 0.));
}

float SceneSignedDistance(float3 position)
{
	float dist = 100000000.;
	for (int i = -1; i < 2; i++)
	for (int j = -1; j < 2; j++)
	for (int k = -1; k < 2; k++)
	{
		dist = min(dist, BoxSignedDistance(position - float3(2.0f * i, 2.0f * j, 2.0f * k) - BoxSize, BoxSize));
	}
	return dist;
}

float RayMarch(float3 rayOrigin, float3 rayDirection)
{
	float dist = 0.;

	for (int i = 0; i < MaxRaymarchStep; i++)
	{
		float3 currentPoint = rayOrigin + rayDirection * dist;
		dist += SceneSignedDistance(currentPoint);

		if (dist < DistToSurfaceThreshold || dist > MaxRaymarchDist) break;
	}

	return dist; 
}

float GetLight(float3 p, float3 normal)
{
	float3 lightPos = LightPosition;
	float3 lightDir = normalize(lightPos - p);
	float diffuse = clamp(max(dot(normal, lightDir), 0.0), 0.0f, 1.0f);

	float d = RayMarch(p + normal * DistToSurfaceThreshold * 2, lightDir);
	if (d < length(lightPos - p)) diffuse *= 0.1;

	return diffuse;
}

float4 PS(VertexOut input) : SV_TARGET
{
	float4 baseColor = tex2D(ColorMapSampler, input.TexCoord);
    float4 result = float4(0.0f, 0.0f, 0.0f, 1.0f);

	// Ambient stage
	result = baseColor*float4((AmbientColor*AmbientIntensity).xyz, 1.0f);

	// RayMarching stage
	float3 ray = normalize(input.Ray);
	float dist = RayMarch(CameraPosition, ray);
	
	if (dist > MaxRaymarchDist) discard;

	// Shadow stage
	float3 _point = CameraPosition + ray * dist;
	float shadow = GetLight(_point, input.Normal);
	result.xyz *= max(shadow,0.1f);

	// Debug each stage
	// result = float4(baseColor);
	// result = float4(ray, 1.0f);
	// result = float4(dist/6,dist/6,dist/6, 1.0f);
	// result = float4(input.Normal, 1.0f);
	// result = float4(shadow,shadow,shadow, 1.0f);

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