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
uniform float4x4 View;
uniform float4x4 WorldViewProjection;
uniform float2 ScreenSize;
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
	float3 Position : POSITION;
    float2 TexCoord : TEXCOORD0;
};

struct VertexOut
{
	float4 Position : SV_POSITION;
    float2 TexCoord : TEXCOORD0;
	float3 View : NORMAL2;
};

//==============================================================================
// Vertex shader
//==============================================================================

VertexOut VS(VertexIn input)
{
	VertexOut vout = (VertexOut)0;

	vout.Position = mul(float4(input.Position, 1.0f), WorldViewProjection);		
	vout.TexCoord = input.TexCoord;
	vout.View = normalize(CameraPosition - input.Position.xyz);

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
const int MaxRaymarchStep = 100;
const float DistToSurfaceThreshold = 0.01f;

const float3 BoxPosition = float3(-0.25, -0.25, -0.25);
const float3 BoxSize = float3(0.5f, 0.5f, 0.5f);

float BoxSignedDistance(float3 position, float3 boxSize)
{
    return length(max(abs(position)-boxSize, 0.));
}

float RayMarch(float3 rayOrigin, float3 rayDirection)
{
	float dist = 0.;

	for (int i = 0; i < MaxRaymarchStep; i++)
	{
		float3 currentPoint = rayOrigin + rayDirection * dist;
		dist += BoxSignedDistance(currentPoint - BoxPosition, BoxSize);

		if (dist < DistToSurfaceThreshold || dist > MaxRaymarchDist) break;
	}

	return dist; 
}

float3 GetNormal(float3 p) {
	float d = BoxSignedDistance(p - BoxPosition, BoxSize);
    float2 e = float2(.001, 0);
    
    float3 n = d - float3(
         BoxSignedDistance(p-e.xyy - BoxPosition, BoxSize),
         BoxSignedDistance(p-e.yxy - BoxPosition, BoxSize),
         BoxSignedDistance(p-e.yyx - BoxPosition, BoxSize));
    
    return normalize(n);
}

float GetLight(float3 p)
{
	float3 lightPos = LightPosition;
	float3 lightDir = normalize(lightPos - p);
	float3 normal = GetNormal(p);
	float diffuse = clamp(max(dot(normal, lightDir), 0.0), 0.0f, 1.0f);

	float d = RayMarch(p + normal * DistToSurfaceThreshold * 2, lightDir);
	if (d < length(lightPos - p)) diffuse *= 0.1;

	return diffuse;
}

float4 PS(VertexOut input) : SV_TARGET
{
	float4 diffuseTex = tex2D(ColorMapSampler, input.TexCoord);
    float4 result = diffuseTex;
	result.w = 1.0f;

	// Base color
	float4 baseColor = result;

	// Ambient stage
	result = baseColor*float4((AmbientColor*AmbientIntensity).xyz, 1.0f);

	// RayMarching 
	float3 rayOrigin = CameraPosition;
	float3 rayTarget = CameraTarget;

	// Prim ray in projection space (PS)
	float2 pri_xy = input.Position/ScreenSize * 2 - 1.0;
	// Flip Y, screen space is top left, but we want bottom left
	pri_xy.y = -pri_xy.y;
	// We need to scale the Y coordinate by the aspect ratio to account for non-square pixels
	pri_xy.y *= ScreenSize.y / ScreenSize.x;
	pri_xy.y *= 0.55;

	float fieldOfView = 90.0;
    float pri_z = ScreenSize.y / tan(radians(fieldOfView) / 2.0); 
    float3 primRayDirection_PS = normalize(float3(pri_xy, pri_z));

	// Prim ray in world space (WS)
	float3 rayDir = mul(WorldViewProjection,primRayDirection_PS).xyz;

	float dist = RayMarch(rayOrigin, rayDir);
	//if (dist > MaxRaymarchDist) discard;

	float3 _point = rayOrigin + rayDir * dist;
	float shadow = GetLight(_point);
	result = float4(baseColor.xyz * shadow, 1.0f);

	float3 normal = GetNormal(_point);
	result = float4(normal, 1.0f);
 
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