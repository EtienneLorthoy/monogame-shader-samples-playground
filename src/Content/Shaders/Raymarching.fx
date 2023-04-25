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
// Global parameters
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

uniform float iTime;
uniform float2 iResolution;
uniform Texture2D iChannel0;
uniform float4 iMouse;

SamplerState samplerState
{
	Filter = Linear; // Use trilinear filtering
	AddressU = WRAP;             // Wrap texture coordinates in the U direction
	AddressV = WRAP;             // Wrap texture coordinates in the V direction
	AddressW = WRAP;             // Wrap texture coordinates in the W direction
};

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
};


//==============================================================================
// Vertex shader
//==============================================================================

VertexOut VS(in VertexIn input)
{
    VertexOut output;
    output.Position = mul(float4(input.Position, 1), WorldViewProjection);
    return output;
}


//==============================================================================
// Pixel shader
//==============================================================================

// ray marching
const int max_iterations = 128;
const float stop_threshold = 0.001;
const float grad_step = 0.02;
const float clip_far = 1000.0;

// math
const float DEG_TO_RAD = 3.14159265359 / 180.0;

// iq's distance function
float sdSphere( float3 pos, float r ) {
	return length( pos ) - r;
}

float sdBox( float3 p, float3 b ) {
  float3 d = abs(p) - b;
  return min(max(d.x,max(d.y,d.z)),0.0) + length(max(d,0.0));
}


float sdUnion( float d0, float d1 ) {
    return min( d0, d1 );
}

float sdInter( float d0, float d1 ) {
    return max( d0, d1 );
}

float sdSub( float d0, float d1 ) {
    return max( d0, -d1 );
}

float sdUnion_s( float a, float b, float k ) {
    float h = clamp( 0.5+0.5*(b-a)/k, 0.0, 1.0 );
    return lerp( b, a, h ) - k*h*(1.0-h);
}

float sfDisp( float3 p ) {
    return sin(p.x)*sin(p.y)*sin(p.z) ;
}

float3 sdTwist( float3 p, float a ) {
    float c = cos(a*p.y);
    float s = sin(a*p.y);
    float2  m = float2(c,-s);
    return float3(m*p.xz,p.y);
}

float3 sdRep( float3 p, float3 c ) {
    return fmod(p,c)-0.5*c;
}

// get distance in the world
float dist_field( float3 p ) {
//  p = sdRep( p, float3( 4.0,4.0, 4.0 ) );
    p = sdTwist( p, 3.0 );
    
    float d1 = sdSphere( p, 0.5 );
    float d0 = sdBox( p, float3(0.5, 0.5, 0.5) );
    
    float d = sdInter( d1, d0 );

    return d;
    //return d + sfDisp( p * 2.5 );
    //return sdUnion_s( d + sfDisp( p * 2.5 * sin( iTime * 1.01 ) ), d1, 0.1 );
}

// get gradient in the world
float3 gradient( float3 pos ) {
	const float3 dx = float3( grad_step, 0.0, 0.0 );
	const float3 dy = float3( 0.0, grad_step, 0.0 );
	const float3 dz = float3( 0.0, 0.0, grad_step );
	return normalize (
		float3(
			dist_field( pos + dx ) - dist_field( pos - dx ),
			dist_field( pos + dy ) - dist_field( pos - dy ),
			dist_field( pos + dz ) - dist_field( pos - dz )			
		)
	);
}

float3 fresnel( float3 F0, float3 h, float3 l ) {
	return F0 + ( 1.0 - F0 ) * pow( clamp( 1.0 - dot( h, l ), 0.0, 1.0 ), 5.0 );
}

// phong shading
float3 shading( float3 v, float3 n, float3 dir, float3 eye ) {
	// ...add lights here...
	
	float shininess = 16.0;
	
	float3 final = float3( 0.0, 0.0, 0.0 );
	
	float3 ref = reflect( dir, n );
    
    float3 Ks = float3( 0.5, 0.5, 0.5 );
    float3 Kd = float3( 1.0, 1.0, 1.0 );
	
	// light 0
	{
		float3 light_pos   = float3( 20.0, 20.0, 20.0 );
		float3 light_color = float3( 1.0, 0.7, 0.7 );
	
		float3 vl = normalize( light_pos - v );
	
		float dl = max( 0.0, dot( vl, n ) );
		float3 diffuse  = Kd * float3( dl, dl, dl );
		float sl =  max( 0.0, dot( vl, ref ) );
		float3 specular = float3( sl, sl, sl);
		
        float3 F = fresnel( Ks, normalize( vl - dir ), vl );
		specular = pow( specular, float3( shininess, shininess, shininess ) );
		
		final += light_color * lerp( diffuse, specular, F ); 
	}
	
	// light 1
	{
		float3 light_pos   = float3( -20.0, -20.0, -30.0 );
		float3 light_color = float3( 0.5, 0.7, 1.0 );
	
		float3 vl = normalize( light_pos - v );
	
		float dl = max( 0.0, dot( vl, n ) );
		float3 diffuse  = Kd * float3( dl, dl, dl );
		float sl =  max( 0.0, dot( vl, ref ) );
		float3 specular = float3( sl, sl, sl);
        
        float3 F = fresnel( Ks, normalize( vl - dir ), vl );
		specular = pow( specular, float3( shininess, shininess, shininess ) );
		
		final += light_color * lerp( diffuse, specular, F );
	}

	float4 texSample = iChannel0.Sample(samplerState, ref);
    final += texSample.xyz * fresnel( Ks, n, -dir );
    
	return final;
}


bool ray_vs_aabb(float3 o, float3 dir, float3 bmin, float3 bmax, inout float2 e ) {
    float3 a = ( bmin - o ) / dir;
    float3 b = ( bmax - o ) / dir;
    
    float3 s = min( a, b );
    float3 t = max( a, b );
    
    e.x = max( max( s.x, s.y ), max( s.z, e.x ) );
    e.y = max( min( t.x, t.y ), max( t.z, e.y ) );
    
    return e.x < e.y;
}

// ray marching
bool ray_marching( float3 o, float3 dir, inout float depth, inout float3 n ) {
	float t = 0.0;
    float d = 10000.0;
    float dt = 0.0;
    for ( int i = 0; i < 128; i++ ) {
        float3 v = o + dir * t;
        d = dist_field( v );
        if ( d < 0.001 ) {
            break;
        }
        dt = min( abs(d), 0.1 );
        t += dt;
        if ( t > depth ) {
            break;
        }
    }
    
    if ( d >= 0.001 ) {
        return false;
    }
    
    t -= dt;
    for ( int i = 0; i < 4; i++ ) {
        dt *= 0.5;
        
        float3 v = o + dir * ( t + dt );
        if ( dist_field( v ) >= 0.001 ) {
            t += dt;
        }
    }
    
    depth = t;
    n = normalize( gradient( o + dir * t ) );
    return true;
    
    return true;
}

// get ray direction
float3 ray_dir( float fov, float2 size, float2 pos ) {
	float2 xy = pos - size * 0.5;

	float cot_half_fov = tan( ( 90.0 - fov * 0.5 ) * DEG_TO_RAD );	
	float z = size.y * 0.5 * cot_half_fov;
	
	return normalize( float3( xy, -z ) );
}

// camera rotation : pitch, yaw
float3x3 rotationXY( float2 angle ) {
	float2 c = cos( angle );
	float2 s = sin( angle );
	
	return float3x3(
		c.y      ,  0.0, -s.y,
		s.y * s.x,  c.x,  c.y * s.x,
		s.y * c.x, -s.x,  c.y * c.x
	);
}

void mainImage( out float4 fragColor, in float2 fragCoord )
{
	// default ray dir
	float3 dir = ray_dir( 45.0, iResolution.xy, fragCoord.xy );
	
	// default ray origin
	float3 eye = float3( 0.0, 0.0, 3.5 );

	// rotate camera
	float3x3 rot = rotationXY( ( iMouse.xy - iResolution.xy * 0.5 ).yx * float2( 0.01, -0.01 ) );
	dir = mul(rot, dir);
	eye = mul(rot, eye);
	
	// ray marching
    float depth = clip_far;
    float3 n = float3( 0.0, 0.0, 0.0 );
	if ( !ray_marching( eye, dir, depth, n ) ) {
		fragColor = iChannel0.Sample(samplerState, dir);
        return;
	}
	
	// shading
	float3 pos = eye + dir * depth;
    
    float3 color = shading( pos, n, dir, eye );
	fragColor = float4( pow( color, float3(1.0/1.2, 1.0/1.2, 1.0/1.2) ), 1.0 );
}

float4 PS(VertexOut input) : SV_TARGET
{
	float4 colorIn = float4(0, 0, 0, 1);
	float2 pos = input.Position.xy;
	mainImage(colorIn, pos);
	return colorIn;
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