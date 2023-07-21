#if OPENGL
	#define SV_POSITION POSITION
	#define VS_SHADERMODEL vs_3_0
	#define PS_SHADERMODEL ps_3_0
#else
	#define VS_SHADERMODEL vs_5_0
	#define PS_SHADERMODEL ps_5_0
#endif


//==============================================================================
// Global parameters
//==============================================================================

uniform float4x4 View;
uniform float4x4 WorldViewProjection;
uniform float2 ScreenSize;
uniform float3 CameraPosition;
uniform float3 CameraTarget;

uniform Texture3D<float4> voxelData;

//==============================================================================
// Interstage structures
//==============================================================================

struct VertexIn
{
	float3 WorldSpacePosition : POSITION;
};

struct VertexOut
{
	float4 ScreenSpacePosition : SV_POSITION;
	float3 Ray : NORMAL0;
};

//==============================================================================
// Vertex shader
//==============================================================================

VertexOut VS(VertexIn input)
{
	VertexOut vout = (VertexOut)0;

	vout.ScreenSpacePosition = mul(float4(input.WorldSpacePosition, 1.0f), WorldViewProjection);		
	vout.Ray = input.WorldSpacePosition - CameraPosition;

	return vout;
}


//==============================================================================
// Pixel shader
//==============================================================================
	
const float4 AmbientColor = float4(0.2f,0.3f,0.1f,1.0f);
const float AmbientIntensity = 0.05f;
float3 LightPosition = float3(5.0f,5.0f,2.0f);
float3 BackLightPosition = normalize(float3(-1.0f,-1.0f,-2.0f));
const float4 DiffuseColor = float4(1.0f,1.0f,0.8f,0.0f);
const float DiffuseIntensity = 1.0f;
const float4 SpecColor = float4(1.0f,1.0f,0.8f,0.0f);
const float SpecIntensity = 1.0f;

const int maxIter = 512;

// Voxel Hit returns true if the voxel at pos should be filled.
bool voxelHit(float3 pos) 
{
	if (pos.x >= 0. && pos.y >= 0. && pos.z >= 0.
	 	&& pos.x < 128. && pos.y < 128. && pos.z < 128.)
	{
		// uint3 voxelPos = uint3(round(pos));
		float4 voxelPoint = voxelData[pos];

		// Alpha 0 is the signal that the voxel is empty
		return voxelPoint.w != 0;
	}
	else return false;
}

// Voxel Color returns the color at pos with normal vector norm.
float4 voxelColor(float3 pos) 
{
	return voxelData[pos];
}

float castRay(float3 eye, float3 ray, out float dist, out float3 norm) {
    float3 pos = floor(eye);
    float3 ri = 1.0 / ray;
    float3 rs = sign(ray);
    float3 ris = ri * rs;
    float3 dis = (pos - eye + 0.5 + rs * 0.5) * ri;
    
    float3 dim = float3(.0,.0,.0);
    for (int i = 0; i < maxIter; ++i) {
        if (voxelHit(pos)) {
            dist = dot(dis - ris, dim);
            norm = normalize(-dim * rs);
            return dist;
        }
    
        dim = step(dis, dis.yzx);
		dim *= (1.0 - dim.zxy);
        
        dis += dim * ris;
        pos += dim * rs;
    }

	return 100000.;
}

float4 PS(VertexOut input) : SV_TARGET
{
	float3 ray = normalize(input.Ray);
    float dist;
    float3 norm;

	// Raymarch
    float hit = castRay(CameraPosition, ray, dist, norm);
	if (hit >= 100000.) discard;

	// Pointcolor
    float3 currentpoint = CameraPosition + dist * ray;
    float4 color = voxelColor(currentpoint - norm * 0.1);
	float4 result = color;
	// return float4(norm, 1.0); // for debug
	// return float4(color, 1.0); // for debug

	// Diffuse
	float3 currentLightDirection = normalize(LightPosition - currentpoint);
	float dflf = saturate(dot(norm, currentLightDirection)); // diffuse from light
	float dfblf = saturate(dot(norm, BackLightPosition))*.1f; // diffuse from back light
	float diffuseFactor = max(dflf, dfblf);
	result.xyz *= diffuseFactor;
	// return float4(diffuseFactor, diffuseFactor, diffuseFactor, 1.0); // for debug

	// Ambient
	result.xyz += AmbientColor.xyz*AmbientIntensity;
	// return result; // for debug

	// Specular
	// float3 reflect = normalize(color.xyz*norm - currentLightDirection);
	// float3 specular = pow(saturate(dot(reflect, CameraPosition - CameraTarget)),15);
	// result.xyz += SpecColor*SpecIntensity*float4(specular, 1.0f);

	// Raymarch shadows
    float pathToLight = castRay(currentpoint + 0.001 * norm, currentLightDirection, dist, norm);
	float distToLight = length(LightPosition - currentpoint);
	float illuminated = pathToLight < distToLight ? 0.1 : 2.;
	float dieOffFactor = 1. - saturate(distToLight / 1000.);
	result.xyz *= illuminated * dieOffFactor;
	// return float4(illuminated, illuminated, illuminated, 1.0); // for debug
	// return float4(dieOffFactor, dieOffFactor, dieOffFactor, 1.0); // for debug
    
	return result;
}


//==============================================================================
// Techniques
//==============================================================================
// #if SHADEREDITOR
technique Technique0
{
	pass P0
	{
		VertexShader = compile VS_SHADERMODEL VS();
		PixelShader = compile PS_SHADERMODEL PS();
	}
}; 
// #endif