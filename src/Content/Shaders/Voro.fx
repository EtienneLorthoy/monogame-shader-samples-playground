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


//==============================================================================
// Interstage structures
//==============================================================================
struct VertexIn
{
	float3 Position : POSITION0;
};

struct VertexOut
{
	float4 Position : SV_POSITION;
    float3 TexCoord : TEXCOORD0;
};


//==============================================================================
// Vertex shader
//==============================================================================

VertexOut VS(in VertexIn input)
{
    VertexOut output;
    
    output.Position = mul(float4(input.Position, 1), WorldViewProjection);
    output.TexCoord = input.Position.xyz + 0.5;
	
    return output;
}


//==============================================================================
// Pixel shader 
//==============================================================================
/*
	Rounded Voronoi Borders
	-----------------------

	Fabrice came up with an interesting formula to produce more evenly distributed Voronoi values. 
	I'm sure there are more interesting ways to use it, but I like the fact that it facilitates 
	the creation of more rounded looking borders. I'm sure there are more sophisticated ways to 
	produce more accurate borders, but Fabrice's version is simple and elegant.

	The process is explained below. The link to the original is below also.

	I didn't want to cloud the example with too much window dressing, so just for fun, I tried 
	to pretty it up by using as little code as possible.

	// 2D version
	2D trabeculum - FabriceNeyret2
	https://www.shadertoy.com/view/4dKSDV

	// 3D version
	hypertexture - trabeculum - FabriceNeyret2
	https://www.shadertoy.com/view/ltj3Dc

	// Straight borders - accurate geometric solution.
	Voronoi - distances - iq
	https://www.shadertoy.com/view/ldl3W8

*/

uniform float iTime;
uniform float2 iResolution; 

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

// float2 to float2 hash.
float2 hash22(float2 p) { 

    // Faster, but doesn't disperse things quite as nicely as other combinations. :)
    float n = sin(dot(p, float2(41, 289)));
    //return fract(float2(262144, 32768)*n)*.75 + .25; 
    
    // Animated.
    p = frac(float2(262144, 32768)*n); 
    return sin( p*6.2831853 + iTime )*.35 + .65; 
    
}

// IQ's polynomial-based smooth minimum function.
float smin( float a, float b, float k ){

    float h = clamp(.5 + .5*(b - a)/k, 0., 1.);
    return lerp(b, a, h) - k*h*(1. - h);
}

// 2D 3rd-order Voronoi: This is just a rehash of Fabrice Neyret's version, which is in
// turn based on IQ's original. I've simplified it slightly, and tidied up the "if-statements,"
// but the clever bit at the end came from Fabrice.
//
// Using a bit of science and art, Fabrice came up with the following formula to produce a more 
// rounded, evenly distributed, cellular value:

// d1, d2, d3 - First, second and third closest points (nodes).
// val = 1./(1./(d2 - d1) + 1./(d3 - d1));
//
float Voronoi(in float2 p){
	float2 g = floor(p), o; p -= g;
	
	float3 d = float3(1,1,1); // 1.4, etc.
    
    float r = 0.;
    
	for(int y = -1; y <= 1; y++){
		for(int x = -1; x <= 1; x++){
            
			o = float2(x, y);
            o += hash22(g + o) - p;
            
			r = dot(o, o);
            
            // 1st, 2nd and 3rd nearest squared distances.
            d.z = max(d.x, max(d.y, min(d.z, r))); // 3rd.
            d.y = max(d.x, min(d.y, r)); // 2nd.
            d.x = min(d.x, r); // Closest.
                       
		}
	}
    
	d = sqrt(d); // Squared distance to distance.
    
    // Fabrice's formula.
    return min(2./(1./max(d.y - d.x, .001) + 1./max(d.z - d.x, .001)), 1.);
    // Dr2's variation - See "Voronoi Of The Week": https://www.shadertoy.com/view/lsjBz1
    //return min(smin(d.z, d.y, .2) - d.x, 1.);
}

float2 hMap(float2 uv){
    // Plain Voronoi value. We're saving it and returning it to use when coloring.
    // It's a little less tidy, but saves the need for recalculation later.
    float h = Voronoi(uv*6.);
    
    // Adding some bordering and returning the result as the height map value.
    float c = smoothstep(0., fwidth(h)*2., h - .09)*h;
    c += (1.-smoothstep(0., fwidth(h)*3., h - .22))*c*.5; 
    
    // Returning the rounded border Voronoi, and the straight Voronoi values.
    return float2(c, h);
}

float4 Test(float2 fragCoord)
{
	// Moving screen coordinates.
	float2 uv = fragCoord/iResolution + float2(-.1, .025)*iTime;
	
	// Obtain the height map (rounded Voronoi border) value, then another nearby. 
	float2 c = hMap(uv);
	float2 c2 = hMap(uv + .004);
	
	// Take a factored difference of the values above to obtain a very, very basic gradient value.
	// Ie. - a measurement of the bumpiness, or bump value.
	float b = max(c2.x - c.x, 0.)*16.;
	
	// Use the height map value to produce some color. It's all made up on the spot, so don't pay it
	// too much attention.
	float3 col = float3(1, .05, .25)*c.x; // Red base.
	float sv = Voronoi(uv*16. + c.y)*.66 + (1.-Voronoi(uv*48. + c.y*2.))*.34; // Finer overlay pattern.
	col = col*.85 + float3(1, .7, .5)*sv*sqrt(sv)*.3; // Mix in a little of the overlay.
	col += (1. - col)*(1.-smoothstep(0., fwidth(c.y)*3., c.y - .22))*c.x; // Highlighting the border.
	col *= col; // Ramping up the contrast, simply because the deeper color seems to look better.
	
	// Taking a pattern sample a little off to the right, ramping it up, then combining a bit of it
	// with the color above. The result is the flecks of yellowy orange that you see. There's no physics
	// behind it, but the offset tricks your eyes into believing something's happening. :)
	sv = col.x*Voronoi(uv*6. + .5);
	col += float3(.7, 1, .3)*pow(sv, 4.)*8.;
	
	// Apply the bump - or a powered variation of it - to the color for a bit of highlighting.
	col += float3(.5, .7, 1)*(b*b*.5 + b*b*b*b*.5);
	
	// Basic gamma correction
	return float4(sqrt(clamp(col, 0., 1.)), 1);
}

float4 PS(VertexOut input) : SV_TARGET
{
	float2 pos = input.Position.xy;
	float4 color = Test(pos);
	return color;
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