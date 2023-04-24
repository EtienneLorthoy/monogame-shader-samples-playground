#if OPENGL
	#define SV_POSITION POSITION
	#define VS_SHADERMODEL vs_3_0
	#define PS_SHADERMODEL ps_3_0
#else
	#define VS_SHADERMODEL vs_4_0
	#define PS_SHADERMODEL ps_4_0
#endif

//==============================================================================
// ShaderToy export based on https://www.shadertoy.com/view/XlcBR7
//==============================================================================

// Helper parameters used in shadertoy shaders
uniform float3 iResolution;
uniform float iTime;
// uniform float iTimeDelta;
// uniform float iFrame;
// uniform float iChannelTime[4];
uniform float4 iMouse;
// uniform float4 iDate;
// uniform float iSampleRate;
// uniform float3 iChannelResolution[4];
uniform Texture2D iChannel0;
SamplerState samplerState
{
	Filter = Linear; // Use trilinear filtering
	AddressU = WRAP;             // Wrap texture coordinates in the U direction
	AddressV = WRAP;             // Wrap texture coordinates in the V direction
	AddressW = WRAP;             // Wrap texture coordinates in the W direction
};

const float iridStrength = 0.5;
const float iridSaturation = 0.7;
const float fresnelStrength = 3.;
const float3 lightCol = float3(.02, .7, .02);

float2 mouse;
#define MRX(X) float3x3(1., 0., 0. ,0., cos(X), -sin(X) ,0., sin(X), cos(X))	//x axis rotation matrix
#define MRY(X) float3x3(cos(X), 0., sin(X),0., 1., 0.,-sin(X), 0., cos(X))	//y axis rotation matrix	
#define MRZ(X) float3x3(cos(X), -sin(X), 0.	,sin(X), cos(X), 0.	,0., 0., 1.)	//z axis rotation matrix
#define MRF(X,Y,Z) MRZ(Z)*MRY(Y)*MRX(X)	//x,y,z combined rotation macro
#define ROT 0.1 //MRF(iTime * .1 + mouse.y, iTime * .2 + mouse.x, iTime * .3)
const int iter = 256;
const float far = 1000.;
#define EPSILON 0.00001

//iq's signed-box distance function
float sdBox( float3 p, float3 b )
{
  float3 d = abs(p) - b;
  return min(max(d.x,max(d.y,d.z)),0.0) + length(max(d,0.0));
}

float sdBoxR(float3 p, float3 b) {
    p = p * ROT;
    return sdBox(p, b);
}

float3 normalBoxR(float3 p, float3 b) {
     float dx = sdBoxR(float3(p.x + EPSILON, p.y, p.z), b) - sdBoxR(float3(p.x - EPSILON, p.y, p.z), b);
    float dy = sdBoxR(float3(p.x, p.y + EPSILON, p.z), b) - sdBoxR(float3(p.x, p.y - EPSILON, p.z), b);
    float dz = sdBoxR(float3(p.x, p.y, p.z + EPSILON), b) - sdBoxR(float3(p.x, p.y, p.z - EPSILON), b);
    return float3(dx, dy, dz);
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

float4 mainImage(float2 fragCoord)
{
    mouse = iMouse.xy/iResolution.xy;
    float2 uv = fragCoord/iResolution.xy;
    uv -= float2(0.5, 0.5);
    uv.x*= iResolution.x/iResolution.y;
    uv*=5.;

    float3 camPos = float3(0.0, 0.0, -20.0);
    float3 screen = float3(uv.x, uv.y, -5.0);
    float3 rayDir = normalize(screen - camPos);
    
    float3 box = float3(2. , 2., 2.);
    
    float depth = 0.;
   
    for(int i=0;i<iter;i++) {
        float3 tpc = camPos + rayDir * depth;
        tpc = tpc*ROT;
        float distc = sdBox(tpc, box);
        if(distc < EPSILON){
            break;
        }        
        if(depth > far) {            
            break;
        }
        depth+=distc;
    }
    
    float3 p = camPos + rayDir * depth;
    float3 pc = camPos + rayDir * depth;
    float3 rpc = pc*ROT;
    float c = sdBox(rpc, box);
    float3 nc = normalize(normalBox(rpc, box));
    float3 nco = normalize(normalBoxR(pc, box)); //normal for calculating fresnel
  	c = smoothstep(1.,.07, c);
    
  	float3 up; 
    //calculating up and right surface vectors for texturing
    if(abs(dot(float3(0., 0., 1.), nc)) > 1. - EPSILON 
       || abs(dot(float3(0., 0., -1.), nc)) > 1. - EPSILON ) 
    {
        up = float3(0., 1., 0.) ;
    }
    else 
    {
    	up = normalize(cross(float3(0., 0., 1.), nc));
    }
    float3 right;
  	if(abs(dot(up, nc)) > 1. - EPSILON 
      || abs(dot(-up, nc)) > 1. - EPSILON ) 
    {
        right = float3(1., 0., 0.);//* -sign(nc.y) ;
    }
    else 
    {
   		right = normalize(cross(nc, up));
    }
    float3 rpco = (rpc - box * (up + right))/(box*2.);
    float dRight = (dot((rpco), right));//right surface vector
    float dUp = dot(rpco, up);//up surface vector
    
    //lights
    float3 lightPos = float3(2., 2., -5.);
    float3 lightDir = normalize(-lightPos);
    float ldc = dot(lightDir, -nc);
    float3 rflct = reflect(normalize(pc - lightPos), nc);
    float spec = dot(rflct, normalize(camPos - pc));
   
    float2 uvm = (abs(float2(dRight, dUp))); //texture uv
	float4 tex = iChannel0.Sample(samplerState, uvm);
    float4 greyTex = float4(greyScale(tex.rgb, 1.), 1.);
    float fres = 1. - dot(nco, normalize(camPos - pc));
    fres *= fresnelStrength;
    float4 irid = pal((c)+(fres * greyTex)) ; //iridescence    
    float3 col = ((.4 + .3* ldc + pow(spec, 2.) * 0.3) * lightCol) * .3 * c;
    col += greyScale(irid.rgb , 1. - iridSaturation) * c * iridStrength;
    float fragColor = float4(col,1.0);

	return fragColor;
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
	baseColor.R = 1.0f;
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