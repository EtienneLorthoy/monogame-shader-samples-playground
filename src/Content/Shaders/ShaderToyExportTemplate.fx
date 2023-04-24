#if OPENGL
	#define SV_POSITION POSITION
	#define VS_SHADERMODEL vs_3_0
	#define PS_SHADERMODEL ps_3_0
#else
	#define VS_SHADERMODEL vs_4_0
	#define PS_SHADERMODEL ps_4_0
#endif

///=============================================================================
/// Shadertoy shader template to port to HLSL with all usable parameters and such
///=============================================================================

//==============================================================================
// ShaderToy export 
//==============================================================================

// Helper parameters used in shadertoy shaders
// uniform vec3 iResolution;
// uniform float iTime;
// uniform float iTimeDelta;
// uniform float iFrame;
// uniform float iChannelTime[4];
// uniform vec4 iMouse;
// uniform vec4 iDate;
// uniform float iSampleRate;
// uniform vec3 iChannelResolution[4];
// uniform samplerXX iChanneli;

float4 mainImage(float2 fragCoord)
{
	// similar entry point than shadertoy void mainImage( out vec4 fragColor, in vec2 fragCoord )

	return float4(0.0, 1.0, 0.0, 1.0);
}

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
    float2 TexCoord : TEXCOORD0;
};

struct VertexOut
{
	float4 Position : SV_POSITION;
    float2 TexCoord : TEXCOORD0;
};

//==============================================================================
// Vertex shader
//==============================================================================

VertexOut VS(in VertexIn input)
{
	VertexOut vout = (VertexOut)0;

	vout.Position = mul(float4(input.Position, 1.0f), WorldViewProjection);		
	vout.TexCoord = input.TexCoord;

	return vout;
}

//==============================================================================
// Pixel shader
//==============================================================================

float4 PS(VertexOut input) : SV_TARGET
{
   	// We just use the texture coordinates but with the internal rendering resolution.
	// We could remove the *800f and the iResolution variable and just use the texture coordinates
	// But I wanted to show with little to no modifications with the original Shadertoy shader.
	// (beside converting it from GLSL to HLSL)
	float4 baseColor = mainImage(input.TexCoord*800.0f);
	return baseColor;

	// OR you can render on screen coordinates instead of object space coordinates
	// float4 baseColor = mainImage(input.Position.xy);
	// return baseColor;
}

//==============================================================================
// Techniques
//==============================================================================
technique Technique0
{
	pass P0
	{r
		VertexShader = compile VS_SHADERMODEL VS();
		PixelShader = compile PS_SHADERMODEL PS();
	}
};