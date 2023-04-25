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

uniform float4x4 WorldViewProjection;
uniform float3 CameraPosition;


//==============================================================================
// Interstage structures
//==============================================================================

struct VertexIn
{
	float3 Position : POSITION0;
    float2 TexCoord : TEXCOORD0;
    float3 Normal : NORMAL0;
};

struct VertexOut
{
	float4 Position : SV_POSITION;
    float2 TexCoord : TEXCOORD0;
    float3 Normal : NORMAL0;
};


//==============================================================================
// Vertex shader
//==============================================================================

VertexOut VS(in VertexIn input)
{
	VertexOut vout = (VertexOut)0;

	vout.Position = mul(float4(input.Position, 1.0f), WorldViewProjection);		
	vout.TexCoord = input.TexCoord;
    vout.Normal = input.Normal;

	return vout;
}


//==============================================================================
// Pixel shader
//==============================================================================

// ShaderToy export based on https://www.shadertoy.com/view/XlcBR7

uniform float iTime;
uniform Texture2D iChannel0;
SamplerState samplerState
{
	Filter = Linear; // Use trilinear filtering
	AddressU = WRAP;             // Wrap texture coordinates in the U direction
	AddressV = WRAP;             // Wrap texture coordinates in the V direction
	AddressW = WRAP;             // Wrap texture coordinates in the W direction
};

const float iridStrength = 0.5;
const float iridSaturation = 0.9;
const float fresnelStrength = 1.5;
const float3 lightPos = float3(2.0, 2.0, -5.0);
const float3 lightCol = float3(.2, .7, .2);

const int iter = 1;
const float far = 1000.;
#define EPSILON 0.00001

//iq's signed-box distance function
float sdBox( float3 p, float3 b )
{
  float3 d = abs(p) - b;
  return min(max(d.x,max(d.y,d.z)),0.0) + length(max(d,0.0));
}

float3 normalBox(float3 p, float3 b) {
    float dx = sdBox(float3(p.x + EPSILON, p.y, p.z), b) - sdBox(float3(p.x - EPSILON, p.y, p.z), b);
    float dy = sdBox(float3(p.x, p.y + EPSILON, p.z), b) - sdBox(float3(p.x, p.y - EPSILON, p.z), b);
    float dz = sdBox(float3(p.x, p.y, p.z + EPSILON), b) - sdBox(float3(p.x, p.y, p.z - EPSILON), b);
    return float3(dx, dy, dz);
}

//Color palette function taken from iq's shader @ https://www.shadertoy.com/view/ll2GD3
#define  pal(t) ( .5 + .5* cos( 6.283*( t + float4(0,1,2,0)/3.) ) )

//rgb to grey scale
float3 greyScale(float3 color, float lerpVal) {
    float greyCol = 0.3 * color.r + 0.59 * color.g + 0.11 * color.b;
    float3 grey = float3(greyCol, greyCol, greyCol);
    float3 newColor = lerp(color, grey, lerpVal);
    return newColor;
}

float4 mainImage(float2 fragCoord, float3 faceNormal)
{
    float2 uv = fragCoord;
    uv -= float2(0.5, 0.5);
    uv *= 1.0;

    //float3 camPos = normalize(CameraPosition) * 10.;
    float3 camPos = float3(0.0,0.0,-15);
    float3 screen = float3(uv.x, uv.y, -6);
    float3 rayDir = normalize(screen - camPos);
    float3 box = float3(1.0 , 1.0, 1.0);
    
    float depth = 0.;
    float3 tpc = camPos + rayDir * depth;
    depth += sdBox(tpc, box);
    
    float3 pc = camPos + rayDir * depth;
    float c = sdBox(pc, box);
    float3 nc = normalize(normalBox(pc, box));
  	c = smoothstep(1.,.07, c);

  	float3 up; 
    //calculating up and right surface vectors for texturing
    if(abs(dot(float3(0., 0., 1.), nc)) > 1. - EPSILON 
       || abs(dot(float3(0., 0., -1.), nc)) > 1. - EPSILON ) 
    {
        up = float3(0., 1., 0.) ;
    }
    else  up = normalize(cross(float3(0., 0., 1.), nc));

    float3 right;
  	if(abs(dot(up, nc)) > 1. - EPSILON 
      || abs(dot(-up, nc)) > 1. - EPSILON ) 
    {
        right = float3(1., 0., 0.);//* -sign(nc.y) ;
    }
    else right = normalize(cross(nc, up));

    float3 rpco = (pc - box * (up + right))/(box*2.);
    float dRight = (dot((rpco), right));//right surface vector
    float dUp = dot(rpco, up);//up surface vector
    
    //lights
    float3 lightDir = normalize(-lightPos);
    float ldc = dot(lightDir, -nc);
    float3 rflct = reflect(normalize(pc - lightPos), nc);
    float spec = dot(rflct, normalize(CameraPosition - pc));
   
    float2 uvm = (abs(float2(dRight, dUp))); //texture uv
	float4 tex = iChannel0.Sample(samplerState, uvm);

    float4 greyTex = float4(greyScale(tex.rgb, 1.), 1.);

    float3 pcWorld = CameraPosition + rayDir * depth;
    float3 ncWorld = normalize(normalBox(pcWorld, box));
    float fres =  dot(ncWorld, normalize(CameraPosition) - normalize(faceNormal));
    fres *= fresnelStrength;
    float4 irid = pal((c)+(fres * greyTex)) ; //iridescence
    float3 col = ((.4 + .3* ldc + pow(spec, 2.) * 0.3) * lightCol) * .3 * c;
    float3 colGrey = greyScale(irid.rgb , 1. - iridSaturation) * c * iridStrength;
    col = col + colGrey;
    float4 fragColor = float4(col,1.0);
	return fragColor;
}

float4 PS(VertexOut input) : SV_TARGET
{
	float4 baseColor = mainImage(input.TexCoord, input.Normal);
	return baseColor;
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