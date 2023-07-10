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

//==============================================================================
// Interstage structures
//==============================================================================

struct VertexIn
{
	float3 Position : POSITION;
};

struct VertexOut
{
	float4 Position : SV_POSITION;
};

//==============================================================================
// Vertex shader
//==============================================================================

VertexOut VS(VertexIn input)
{
	VertexOut vout = (VertexOut)0;

	vout.Position = mul(float4(input.Position, 1.0f), WorldViewProjection);		

	return vout;
}


//==============================================================================
// Pixel shader
//==============================================================================
	
float3 LightDirection = float3(0.5f,0.5f,0.2f);
float3 LightPosition = float3(5.0f,5.0f,2.0f);
const float4 DiffuseColor = float4(1.0f,1.0f,0.8f,0.0f);
const float DiffuseIntensity = 1.0f;
const float4 SpecColor = float4(1.0f,1.0f,0.8f,0.0f);
const float SpecIntensity = 1.0f;

const float MaxRaymarchDist = 100.0f;
const int MaxRaymarchStep = 64;
const float DistToSurfaceThreshold = 0.05f;

float BoxSignedDistance(float3 position, float3 boxSize)
{
	return length(max(abs(position)-boxSize, .0));
}

float SphereSignedDistance(float3 position, float radius)
{
	return length(position) - radius;
}

float GetDist(float3 pointPosition)
{
	// Plane distance is equal to Y coordinate (the plane is horizontal and at 0)
	float3 dist = pointPosition.y;

	// Spheres distance
	for (int i = -1; i < 2; i++)
	for (int j = -1; j < 2; j++)
	for (int k = 0; k < 3; k++)
	{
		float3 spherePos = float3(-2.0 * i, 2.0 * k + 0.5, -2.0 * j);
		float sphereDist = SphereSignedDistance(pointPosition - spherePos, 0.5);
		dist = min(dist, sphereDist);
	}

	return dist;
}

float RayMarch(float3 rayOrigin, float3 rayDirection)
{
	float dist = 0.;

	for (int i = 0; i < MaxRaymarchStep; i++)
	{
		float3 currentPoint = rayOrigin + rayDirection * dist;
		dist += GetDist(currentPoint);

		if (dist < DistToSurfaceThreshold || dist > MaxRaymarchDist) break;
	}

	return dist; 
}

float3 GetNormal(float3 p)
{
	float3 e = float3(0.01, 0, 0);
	return normalize(float3(
		GetDist(p + e.xyy),
		GetDist(p + e.yxy),
		GetDist(p + e.yyx)));
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
	/////////////////
	// RayMarching //
	/////////////////
	float3 rayOrigin = CameraPosition;
	float3 rayTarget = CameraTarget;

	// Prim ray in projection space (PS)
	float fieldOfView = 90.0;
	float2 pri_xy = input.Position/ScreenSize * 2 - 1.0;
	// Flip Y, screen space is top left, but we want bottom left
	pri_xy.y = -pri_xy.y;
	// We need to scale the Y coordinate by the aspect ratio to account for non-square pixels
	pri_xy.y *= ScreenSize.y / ScreenSize.x;

	// Z component should be "ScreenSize.y / tan(radians(fieldOfView) / 2.0);" but since
	// ScreenSize.y is 1 normalized and tan(radians(90)/2.0) is a 1, we can just use 1.0.
    float pri_z = 1.0; 
    float3 primRayDirection_PS = normalize(float3(pri_xy, pri_z));

	// Prim ray in world space (WS)
	float3 rayDir = mul(WorldViewProjection,primRayDirection_PS).xyz;

	float dist = RayMarch(rayOrigin, rayDir);
	float3 _point = rayOrigin + rayDir * dist;
	float shadow = GetLight(_point);
 
	return float4(shadow, shadow, shadow, 1.0f);
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