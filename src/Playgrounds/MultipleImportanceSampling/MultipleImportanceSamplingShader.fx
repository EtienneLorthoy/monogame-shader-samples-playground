﻿#define VS_SHADERMODEL vs_5_0
#define PS_SHADERMODEL ps_5_0

//==============================================================================
// Global parameters
//==============================================================================

uniform float4x4 WorldViewProjection;
uniform float2 ViewportSize;
uniform float3 CameraPosition;
uniform float3 CameraTarget;
uniform float iTime;
uniform float LerpBalance;

uniform Texture2D iChannel0;
uniform sampler2D iChannel0Sampler = sampler_state
{
    Texture = <iChannel0>;
    MinFilter = linear;
    MagFilter = linear;
    MipFilter = linear;
};

/*Scene Objects*/
#define N_QUADS 5
#define N_BOXES 50

//==============================================================================
// Interstage structures
//==============================================================================

struct BufferAVertexIn
{
	float3 WorldPosition : POSITION;
};

struct BufferAVertexOut
{
	float4 ScreenPosition : SV_POSITION;
	float3 Ray : NORMAL1;
};

//==============================================================================
// Vertex shader
//==============================================================================

BufferAVertexOut VSBufferA(BufferAVertexIn input)
{
	BufferAVertexOut vout = (BufferAVertexOut)0;

	vout.ScreenPosition = mul(float4(input.WorldPosition, 1.0f), WorldViewProjection);		
	vout.Ray = input.WorldPosition - CameraPosition;

	return vout;
}


//==============================================================================
// Pixel shader
//==============================================================================

//// Common functions

//----------Some For Option Math Constant Define---------
#define MY_E 					2.7182818
#define ONE_OVER_E				0.3678794
#define PI 						3.1415926
#define TWO_PI 					6.2831852
#define FOUR_PI 				12.566370
#define INVERSE_PI 				0.3183099
#define ONE_OVER_TWO_PI 		0.1591549
#define ONE_OVER_FOUR_PI 		0.0795775
#define OFFSET_COEF 			0.0001
#define INFINITY         		1000000.0

//Monte Carlo by Importance Samplering Methods
/*
 	F(N) ≈ (1/N) * SUM( f(Xi)/pdf(Xi) ) <1,N>
*/
//----------------------Math Function---------------------
float seed;
float GetRandom(){return frac(sin(seed++)*43758.5453123);}
#if __VERSION__ >= 300
float FastSqrt(float x){return intBitsToFloat(532483686 + (floatBitsToInt(x) >> 1));}
#endif
float POW2(float x){ return x*x;}
float POW3(float x){ return x*x*x;}
float POW4(float x){ float x2 = x*x; return x2*x2;}
float POW5(float x){ float x2 = x*x; return x2*x2*x;}

float Complement(float coeff){ return 1.0 - coeff; }
float cosTheta(float3 w) { return w.z; }
float cosTheta2(float3 w) { return w.z*w.z; }
float cosTheta4(float3 w) { float cos2Theta = cosTheta2(w); return cos2Theta*cos2Theta;}
float absCosTheta(float3 w) { return abs(w.z); }
float sinTheta2(float3 w) { return Complement(w.z*w.z); }
float sinTheta(float3 w) { return sqrt(clamp(sinTheta2(w),0.,1.)); }
float tanTheta2(float3 w) { return sinTheta2(w) / cosTheta2(w); }
float tanTheta(float3 w) { return sinTheta(w) / cosTheta(w); }

//http://orbit.dtu.dk/files/126824972/onb_frisvad_jgt2012_v2.pdf
void frisvad(in float3 n, out float3 f, out float3 r){
    if(n.z < -0.999999) {
        f = float3(0.,-1,0);
        r = float3(-1, 0, 0);
    } else {
    	float a = 1./(1.+n.z);
    	float b = -n.x*n.y*a;
    	f = float3(1. - n.x*n.x*a, b, -n.x);
    	r = float3(b, 1. - n.y*n.y*a , -n.y);
    }
}

float3x3 CoordBase(float3 n){
	float3 x,y;
    frisvad(n,x,y);
    return float3x3(x,y,n);
}

float3 ToOtherSpaceCoord(float3x3 otherSpaceCoord,float3 vector_){
	return mul(vector_,otherSpaceCoord);
}

float3 RotVector(float3x3 otherSpaceCoord,float3 vector_){
	return mul(otherSpaceCoord, vector_);
}

//------>Fresnel Term<------
float F_Schlick(float F0, float3 L,float3 H){
    return F0 + (1.0 - F0)*POW5(1.0-dot(L,H));
}

//------------Multiple Importance Sample Weight-----------
/*
	heuristic
	here βis 2;so power of coeff
*/
float MISWeight(float a,float b){
	float a2 = a*a;
	float b2 = b*b;
	return a2/(a2+b2);
}
float MISWeight(float coffe_a,float aPDF,float coffe_b,float bPDF){
    return MISWeight(coffe_a * aPDF,coffe_b*bPDF);
}

//--------------Probability Density Function and Sample--------------
/*---------------------------------
	Whether it’s solid angle or spherical coordinate. 
	What we have so far is the pdf for half-vector,a transformation is necessary.
---> P(θ) = Ph(θ)*(dWh)/(dWi) = Ph(θ)/(4*Wo*wh)
*/
float PDF_h2theta(float pdf_h,float3 wi,float3 wh){
	return 0.25*pdf_h/dot(wi,wh);//return pdf_h/(4.0*dot(wo,wh));
}

float3 DiffuseUnitSphereRay(float x1,float x2){
	float theta = acos(sqrt(1.0-x1));
    float phi   = TWO_PI * x2;
    float sinTheta = sin(theta);
    return float3(sinTheta*cos(phi),sinTheta*sin(phi),cos(theta));
}

/*
Form https://www.shadertoy.com/view/4ssXRX
	 https://www.shadertoy.com/view/MslGR8
	 https://www.loopit.dk/banding_in_games.pdf
*/
float nrand( float2 n ){
	return frac(sin(dot(n.xy, float2(12.9898, 78.233)))* 43758.5453);
}

float TriangularNoise(float2 n,float time){
    float t = frac(time);
	float nrnd0 = nrand( n + 0.07*t );
	float nrnd1 = nrand( n + 0.11*t );
	return (nrnd0+nrnd1) / 2.0;
}

float2 TriangularNoise2DShereRay(float2 n,float time){
	float theta = TWO_PI*GetRandom();
    float r = TriangularNoise(n,time);
    return float2(cos(theta),sin(theta))*(1.-r);
}

//---------------VNDF-----------------
/*
	https://hal.inria.fr/hal-00942452v1/document
	https://hal.archives-ouvertes.fr/hal-01509746/document
	https://hal.inria.fr/file/index/docid/996995/filename/article.pdf
	and some reference https://www.shadertoy.com/view/llfyRj
  	alpha_x/alpha_y is anisotropic roughness; U1/U2 is uniform random numbers
*/
float3 sampleGGXVNDF(float3 V_, float alpha_x, float alpha_y, float U1, float U2){
	// stretch view
	float3 V = normalize(float3(alpha_x * V_.x, alpha_y * V_.y, V_.z));
	// orthonormal basis
	float3 T1 = (V.z < 0.9999) ? normalize(cross(V, float3(0,0,1))) : float3(1,0,0);
	float3 T2 = cross(T1, V);
	// sample point with polar coordinates (r, phi)
	float a = 1.0 / (1.0 + V.z);
	float r = sqrt(U1);
	float phi = (U2<a) ? U2/a * PI : PI + (U2-a)/(1.0-a) * PI;
	float P1 = r*cos(phi);
	float P2 = r*sin(phi)*((U2<a) ? 1.0 : V.z);
	// compute normal
	float3 N = P1*T1 + P2*T2 + sqrt(max(0.0, 1.0 - P1*P1 - P2*P2))*V;
	// unstretch
	N = normalize(float3(alpha_x*N.x, alpha_y*N.y, max(0.0, N.z)));
	return N;
}
/*
------>GGX Distribution
*/
float GGX_Distribution(float3 wh, float alpha_x, float alpha_y) {
    float tan2Theta = tanTheta2(wh);
    if(alpha_x == alpha_y){
    	//------when alpha_x == alpha_y so
    	float c = alpha_x + tan2Theta/alpha_x;
    	return 1.0/(PI*cosTheta4(wh)*c*c);
	}else{
		float alpha_xy = alpha_x * alpha_y;
		float e_add_1 = 1. + tan2Theta / alpha_xy;
    	return 1.0 / (PI * alpha_xy * cosTheta4(wh) * e_add_1 * e_add_1);
	}
}
/*
    	Λ(ω) = (-1+sign(a)sqrt(1+1/(a*a))/2 where a = 1/(ai*tan(theta_i))   	<0,π>
	so  Λ(ω) = (-1+sqrt(1+ai^2*tan^2(θ)))/2		<0,π/2>
*/
float lambda(float3 w, float alpha_x, float alpha_y){
	return 0.5*(-1.0 + sqrt(1.0 + alpha_x*alpha_y*tanTheta2(w)));
}
/*
	  G (wo,wi) = G1(wo)G1(wi) 
	  G1(ωo,ωm) = 1/(1+Λ(ωo)) 
*/
float GGX_G1(float3 w, float alpha_x, float alpha_y) {
    return 1.0 / (1.0 + lambda(w, alpha_x, alpha_y));
}
/*
	https://jo.dreggn.org/home/2016_microfacets.pdf
	G2(ωi, ωo, ωm) is masking-shadowing
	G2(ωi, ωo, ωm) = G1(ωi, ωm)G1(ωo, ωm)  [Walter et al.2007]
		<If ωi and ωo are on the same side of the microsurface (i.e. reflection),it has the closed for>
	G2(ωi, ωo) = 1/[1+Λ(ωi)+Λ(ωo)]

------>GGX Geometry
*/
float GGX_G2(float3 wo, float3 wi, float alpha_x, float alpha_y) {
    return 1.0 / (1.0 + lambda(wo, alpha_x, alpha_y) + lambda(wi, alpha_x, alpha_y));
}
/*
	https://hal.inria.fr/hal-00996995v1/document
	PDF(ωm·ωg)D(ωm)	 previous
	PDF Dωi(ωm)		   now this
		Dωi(ωm) = G1(ωi,ωm)|ωi·ωm|D(ωm)/|ωi·ωg|
*/
float GGX_PDF(float3 wi, float3 wh, float alpha_x, float alpha_y) {
    return GGX_Distribution(wh, alpha_x, alpha_y) * GGX_G1(wi, alpha_x, alpha_y) * abs(dot(wi, wh)) / abs(wi.z);
}
//For Diffuse Lambert Sample
float Diffuse_PDF(float3 L){
	return INVERSE_PI*cosTheta(L);
}
//For Reflect Micro-fact Sample
float Specular_PDF(float3 wi, float3 wh, float alpha_x, float alpha_y){
	return PDF_h2theta(GGX_PDF(wi,wh,alpha_x,alpha_y),wi,wh); 
}
//--------------------Post Processing---------------------
float3 GammaCorrect(float3 col,float coeff){
	return pow(col,float3(coeff,coeff,coeff));
}
float3 GammaCorrect(float3 col,float3 coeff){
	return pow(col,coeff);
}
float3 ExposureCorrect(float3 col, float linfac, float logfac){
	return linfac*(1.0 - exp(col*logfac));
}

/*
	An almost-perfect approximation from http://chilliant.blogspot.com.au/2012/08/srgb-approximations-for-hlsl.html?m=1
*/
float Luminance(float3 linearRGB){
    return dot(linearRGB, float3(0.2126729,  0.7151522, 0.0721750));
}

/*
	https://www.shadertoy.com/view/MdyyRt from 834144373
	Or you can easy find from wiki
*/
#define _Strength 10.
float4 FXAA(sampler2D _Tex,float2 uv,float2 RenderSize){
    float3 e = float3(1./RenderSize,0.);

    float reducemul = 0.125;// 1. / 8.;
    float reducemin = 0.0078125;// 1. / 128.;

    float4 Or = tex2D(_Tex,uv); //P
    float4 LD = tex2D(_Tex,uv - e.xy); //左下
    float4 RD = tex2D(_Tex,uv + float2( e.x,-e.y)); //右下
    float4 LT = tex2D(_Tex,uv + float2(-e.x, e.y)); //左上
    float4 RT = tex2D(_Tex,uv + e.xy); // 右上

    float Or_Lum = Luminance(Or.rgb);
    float LD_Lum = Luminance(LD.rgb);
    float RD_Lum = Luminance(RD.rgb);
    float LT_Lum = Luminance(LT.rgb);
    float RT_Lum = Luminance(RT.rgb);

    float min_Lum = min(Or_Lum,min(min(LD_Lum,RD_Lum),min(LT_Lum,RT_Lum)));
    float max_Lum = max(Or_Lum,max(max(LD_Lum,RD_Lum),max(LT_Lum,RT_Lum)));

    //x direction,-y direction
    float2 dir = float2((LT_Lum+RT_Lum)-(LD_Lum+RD_Lum),(LD_Lum+LT_Lum)-(RD_Lum+RT_Lum));
    float dir_reduce = max((LD_Lum+RD_Lum+LT_Lum+RT_Lum)*reducemul*0.25,reducemin);
    float dir_min = 1./(min(abs(dir.x),abs(dir.y))+dir_reduce);
    dir = min(float2(_Strength,_Strength),max(-float2(_Strength, _Strength),dir*dir_min)) * e.xy;

    //------
    float4 resultA = 0.5*(tex2D(_Tex,uv-0.166667*dir)+tex2D(_Tex,uv+0.166667*dir));
    float4 resultB = resultA*0.5+0.25*(tex2D(_Tex,uv-0.5*dir)+tex2D(_Tex,uv+0.5*dir));
    float B_Lum = Luminance(resultB.rgb);

    //return resultA;
    if(B_Lum < min_Lum || B_Lum > max_Lum)
        return resultA;
    else 
        return resultB;
}
//// Common functions

////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

//// Buffer A
/*
	"Fast PathTracing[Lab]!" by 834144373(恬纳微晰)
	This shader url : https://www.shadertoy.com/view/XdVfRm
	Licence: CC3.0 署名（BY）-非商业性使用（NC）-相同方式共享（SA）
	Also 834144373 is 恬纳微晰 or TNWX or 祝元洪
*/
/*
--------------------Main Reference Knowledge-------------------
MIS: [0]https://graphics.stanford.edu/courses/cs348b-03/papers/veach-chapter9.pdf
     [1]https://hal.inria.fr/hal-00942452v1/document
     [2]Importance Sampling Microfacet-Based BSDFs using the Distribution of Visible Normals. Eric Heitz, Eugene D’Eon
     [3]Understanding the Masking-Shadowing Function in Microfacet-Based BRDFs. Eric Heitz
     [4]Microfacet Models for Refraction through Rough Surfaces. Bruce Walter, Stephen R. Marschner, Hongsong Li, Kenneth E. Torrance
	Note: More details about theory and math you can easy find on "Common" shader.
*/
#define GI_DEPTH 3


/*Type*/
#define LIGHT 0
#define DIFF 1
#define REFR 2
#define SPEC 3

const float3 BACKGROUD_COL = float3(0.,0.,0.);

const float3 ZERO = float3(0.,0.,0.);
const float3 ONE  = float3(1.,1.,1.);
const float3 UP   = float3(0.,1.,0.);

struct Ray { float3 origin; float3 direction; };
struct Quad { float3 normal; float3 v0; float3 v1; float3 v2; float3 v3; float3 emission; float3 color; float roughness; int type; };
struct Box { float3 minCorner; float3 maxCorner; float3 emission; float3 color; float roughness; int type; };
struct Intersection {float distance;float2 uv; float3 normal; float3 emission; float3 color; float roughness; int type; };

Ray NewRay(float3 origin, float3 direction){
	Ray r;
	r.origin = origin;
	r.direction = direction;
	return r;
}

Quad NewQuad(float3 normal, float3 v0, float3 v1, float3 v2, float3 v3, float3 emission, float3 color, float roughness, int type){
	Quad q;
	q.v0 = v0;
	q.v1 = v1;
	q.v2 = v2;
	q.v3 = v3;
	q.emission = emission;
	q.color = color;
	q.roughness = roughness;
	q.type = type;
	q.normal = normal;
	return q;
}

Box NewBox(float3 minCorner, float3 maxCorner, float3 emission, float3 color, float roughness, int type){
	Box b;
	b.minCorner = minCorner;
	b.maxCorner = maxCorner;
	b.emission = emission;
	b.color = color;
	b.roughness = roughness;
	b.type = type;
	return b;
}

Intersection NewIntersection(float distance, float2 uv, float3 normal, float3 emission, float3 color, float roughness, int type){
	Intersection i;
	i.distance = distance;
	i.uv = uv;
	i.normal = normal;
	i.emission = emission;
	i.color = color;
	i.roughness = roughness;
	i.type = type;
	return i;
}

struct Material{float id;float2 uv;float3 normal;float3 specular;float3 diffuse;float roughness;int type;};     
static Quad quads[N_QUADS];
static Box boxes[N_BOXES];

float QuadIntersect( float3 v0, float3 v1, float3 v2, float3 v3, float3 normal, Ray r ){
	float3 u, v, n;    // triangle vectors
	float3 w0, w, x;   // ray and intersection vectors
	float rt, a, b;  // params to calc ray-plane intersect
	
	// get first triangle edge vectors and plane normal
	v = v2 - v0;
	u = v1 - v0; // switched u and v names to save calculation later below
	//n = cross(v, u); // switched u and v names to save calculation later below
	n = -normal; // can avoid cross product if normal is already known
	    
	w0 = r.origin - v0;
	a = -dot(n,w0);
	b = dot(n, r.direction);
	if (b < 0.0001)   // ray is parallel to quad plane
		return INFINITY;

	// get intersect point of ray with quad plane
	rt = a / b;
	if (rt < 0.0)          // ray goes away from quad
		return INFINITY;   // => no intersect
	    
	x = r.origin + rt * r.direction; // intersect point of ray and plane

	// is x inside first Triangle?
	float uu, uv, vv, wu, wv, D;
	uu = dot(u,u);
	uv = dot(u,v);
	vv = dot(v,v);
	w = x - v0;
	wu = dot(w,u);
	wv = dot(w,v);
	D = 1.0 / (uv * uv - uu * vv);

	// get and test parametric coords
	float s, t;
	s = (uv * wv - vv * wu) * D;
	if (s >= 0.0 && s <= 1.0)
	{
		t = (uv * wu - uu * wv) * D;
		if (t >= 0.0 && (s + t) <= 1.0)
		{
			return rt;
		}
	}
	
	// is x inside second Triangle?
	u = v3 - v0;
	///v = v2 - v0;  //optimization - already calculated above

	uu = dot(u,u);
	uv = dot(u,v);
	///vv = dot(v,v);//optimization - already calculated above
	///w = x - v0;   //optimization - already calculated above
	wu = dot(w,u);
	///wv = dot(w,v);//optimization - already calculated above
	D = 1.0 / (uv * uv - uu * vv);

	// get and test parametric coords
	s = (uv * wv - vv * wu) * D;
	if (s >= 0.0 && s <= 1.0)
	{
		t = (uv * wu - uu * wv) * D;
		if (t >= 0.0 && (s + t) <= 1.0)
		{
			return rt;
		}
	}


	return INFINITY;
}

float BoxIntersect( float3 minCorner, float3 maxCorner, Ray r, out float3 normal ){
	float3 invDir = 1.0 / r.direction;
	float3 tmin = (minCorner - r.origin) * invDir;
	float3 tmax = (maxCorner - r.origin) * invDir;
	
	float3 real_min = min(tmin, tmax);
	float3 real_max = max(tmin, tmax);
	
	float minmax = min( min(real_max.x, real_max.y), real_max.z);
	float maxmin = max( max(real_min.x, real_min.y), real_min.z);
	
	if (minmax > maxmin)
	{
		
		if (maxmin > 0.0) // if we are outside the box
		{
			normal = -sign(r.direction) * step(real_min.yzx, real_min) * step(real_min.zxy, real_min);
			return maxmin;	
		}
		
		else if (minmax > 0.0) // else if we are inside the box
		{
			normal = -sign(r.direction) * step(real_max, real_max.yzx) * step(real_max, real_max.zxy);
			return minmax;
		}
				
	}
	
	return INFINITY;
}

// I = ΔΦ/4π
float3 GetLightIntensity(){
	return 50.* ONE;
}

float3 GetLightIntensityFrom(float3 col){
	return GetLightIntensity() * col;
}

float SceneIntersect( Ray r, inout Intersection intersec ){
    float d = INFINITY;	
    float t = 0.;
    float3 normal = float3(0.,0.,0.);

	intersec.distance = INFINITY;
	intersec.uv = float2(0.,0.);
	intersec.normal = float3(0.,0.,0.);
	intersec.emission = float3(0.,0.,0.);
	intersec.color = float3(0.,0.,0.);
	intersec.roughness = 0.;
	intersec.type = 0;
   
    for(int i=0;i<N_QUADS;i++)
	{
        t = QuadIntersect( quads[i].v0, quads[i].v1, quads[i].v2, quads[i].v3, quads[i].normal, r );
        if (t < d){
        	d = t;
            intersec.normal = normalize(quads[i].normal);
            intersec.emission = quads[i].emission;
            intersec.color = quads[i].color;
            intersec.roughness = quads[i].roughness;
            intersec.type = quads[i].type;
        }
    }
	
    for(int i=0;i<N_BOXES;i++)
	{
    	t = BoxIntersect(boxes[i].minCorner,boxes[i].maxCorner,r,normal);
        if(t < d){
        	d = t;
            intersec.normal = normalize(normal);
            intersec.emission = boxes[i].emission;
            intersec.color = boxes[i].color;
            intersec.roughness = boxes[i].roughness;
            intersec.type = boxes[i].type;
        }
    }
    intersec.distance = d;
    return d;
}

void SetupScene()
{ 
	// Walls // struct Quad { float3 normal; float3 v0; float3 v1; float3 v2; float3 v3; float3 emission; float3 color; float roughness; int type; };
	float3 emission = float3(0.2f,0.2f,0.1f);
	float3 color = float3(1.0,0.0,0.0);
	float zD  = int(sqrt(N_BOXES)) * 0.1 * 1.1 * -1.0; // distFromFirstCubeRow
	zD = min(zD, -1.0);
	quads[0] = NewQuad(float3(0.,0.,1.),float3(-.5,0.,zD), float3(-.5,0.5,zD), float3(.5 ,0.5 ,zD), float3(.5,0.,zD), emission, color,0.4, LIGHT);
	// quads[1] = NewQuad(float3(-1.,0.,0.),float3(1.1,0.,-1.), float3(1.1,.5,-1.), float3(1.1,.5,1. ), float3(1.1,0.,1.), emission, color,0.4, LIGHT);

    // Floor
	quads[2] = NewQuad(float3(0.,1.,0.),float3(-1.0,0.,-1.), float3(1.0,0.,-1.), float3(1.0 ,0. ,1. ), float3(-1.0,0.,1.), float3(0.8,0.8,0.3), float3(.2,.2,.2),0.8, SPEC);
    
	// Cube color palette
    float3 currentBoxSize = float3(0.1,0.1,0.1);
    int boxesSizeCount = int(sqrt(N_BOXES));
    for (int i=0;i<boxesSizeCount;i++)
    for (int j=0;j<boxesSizeCount;j++)
    {
        float floati = (i) * 0.2 - (boxesSizeCount) * 0.5 * 0.2;
        float floatj = (j) * 0.2 - (boxesSizeCount) * 0.5 * 0.2;
        float3 currentPos = float3(floati, 0.001, floatj);
		int currentIndex = i*boxesSizeCount + j;
		float currentIndexN = (float(currentIndex) / float(boxesSizeCount * boxesSizeCount));
		float3 boxColor = float3(sin(currentIndexN * TWO_PI) / 2 + 0.5, cos(currentIndexN * TWO_PI) / 2 + 0.5, tan(currentIndexN * TWO_PI) / 2 + 0.5);

		int type = SPEC;
		if ((int(iTime/500.) - currentIndex) % (boxesSizeCount * boxesSizeCount) == 0) type = LIGHT;
        boxes[j + i*boxesSizeCount] = NewBox(currentPos, currentPos + currentBoxSize, boxColor, boxColor, 0.8, type);
    }

	// Moving cube
	//boxes[0] = NewBox(LightPosition + float3(0.,1.,0.), LightPosition + currentBoxSize * 5, float3(1.,1.,1.)*10, float3(0.6,0.6,0.6), 0.2, LIGHT);
}

#define time iTime*0.1

Material GetMaterial(Intersection _intersec){
	Material mat;
    mat.type = _intersec.type;
    mat.uv = _intersec.uv;
    mat.normal = _intersec.normal;
    mat.diffuse = _intersec.color;
    mat.specular = _intersec.emission;
    mat.roughness = _intersec.roughness;
    return mat;
}

float isSameHemishere(float cosA,float cosB){
	return cosA*cosB;
} 

//pdf area to solid angle
float PDF_Area2Angle(float pdf,float dist,float costhe){
	return pdf*dist*dist/costhe;
}

//float3 v0; float3 v1; float3 v2; float3 v3
//     A		B         C        D
/*  v3------v2
	|	 O	 |
	v0------v1
*/
float3 LightSample(float3 p,float x1,float x2, out float3 wo,out float dist,out float pdf){
	float3 v0v1 = quads[0].v1 - quads[0].v0;
    float3 v0v3 = quads[0].v3 - quads[0].v0;
    float width  = length(v0v1);
    float height = length(v0v3);
    float3 O = quads[0].v0 + v0v1*x1 + v0v3*x2;
    wo = O - p;
    dist = length(wo);
    wo = normalize(wo);
    float costhe = dot(-wo,quads[0].normal);
    pdf = PDF_Area2Angle(1./(width*height),dist,clamp(costhe,0.0001,1.));
    return costhe>0. ? GetLightIntensity(): float3(0.,0.,0.);
}

float3 MicroFactEvalution(Material mat,float3 nDir,float3 wo,float3 wi){
	float3x3 nDirSpace = CoordBase(nDir);
    //light dir
    float3 L_local = ToOtherSpaceCoord(nDirSpace,wo);
    //eye dir
    float3 E_local = ToOtherSpaceCoord(nDirSpace,wi);
    if(isSameHemishere(L_local.z,E_local.z) < 0.){
    	return ZERO;
    }
    if(L_local.z == 0. || E_local.z == 0.){
    	return ZERO;
    }
    float alpha = mat.roughness;
    float3 H_local = normalize(L_local + E_local);
    float D = GGX_Distribution(H_local, alpha, alpha);
    float F = F_Schlick(0.91, L_local,H_local);
    float G = GGX_G2(L_local, E_local, alpha, alpha);
    float3 specular_Col = mat.specular*0.25*D*G/clamp(L_local.z*E_local.z,0.,1.);
    float3 diffuse_Col = mat.diffuse*INVERSE_PI;
    return lerp(specular_Col,diffuse_Col,F);
}

float EnergyAbsorptionTerm(float roughness,float3 wo){
	return lerp(1.-roughness*roughness,sinTheta(wo),roughness);
}

//F function
float3 LightingDirectSample(Material mat,float3 p,float3 nDir,float3 vDir,out float pdf){
	float3 L = float3(0.,0.,0.);
    float x1 = GetRandom(),x2 = GetRandom();
    float3 wo;
    float dist;
    float3 Li = LightSample(p,x1,x2,wo,dist,pdf) * mat.specular;
    float WoDotN = dot(wo,nDir);
    if(WoDotN >= 0. && pdf > 0.0001){
        float3 Lr = MicroFactEvalution(mat,nDir,wo,vDir);
        Ray shadowRay = NewRay(p,wo);
        Intersection shadow_intersc;
        float d = SceneIntersect(shadowRay,shadow_intersc);
        if(shadow_intersc.type == LIGHT){
            L = Lr*Li/pdf;
        }
    }
    return L;
}

//G function 
float3 LightingBRDFSample(Material mat,float3 p,float3 nDir,float3 vDir,inout float _pathWeight,inout Intersection _intersec,inout Ray ray,out float pdf){
	float3 L = float3(0.,0.,0.);
    float x1 = GetRandom(),x2 = GetRandom();
    float3x3 nDirSpace = CoordBase(nDir);
    //light dir
    float3 L_local;
    //eye dir
    float3 E_local = ToOtherSpaceCoord(nDirSpace,vDir);
    float alpha = mat.roughness;
    if(E_local.z <= 0.)
        return ZERO;
    float3 N_local = sampleGGXVNDF(E_local,alpha,alpha,x1,x2);
    N_local *= sign(E_local.z * N_local.z);

    float part_pdf = 0.5;
    float path_pdf = 0.;
    if(GetRandom() < part_pdf){
        //diffuse ray
        L_local = DiffuseUnitSphereRay(x1,x2);
        path_pdf = Diffuse_PDF(L_local);
    }
    else{
        //specular ray
        L_local = reflect(-E_local, N_local);
        path_pdf = Specular_PDF(E_local,N_local,alpha,alpha);
    }
    
    float3 wo = RotVector(nDirSpace,L_local); //to world coord base
    float3 Lr = MicroFactEvalution(mat,nDir,wo,vDir);
    Ray shadowRay = NewRay(p,wo);
    float d = SceneIntersect(shadowRay,_intersec);
    ray = NewRay(p,wo);
    if(path_pdf > 0.0001){
        if(_intersec.type == LIGHT && cosTheta(L_local)>0.){
            L = GetLightIntensityFrom(_intersec.emission)/(d*d)*Lr*dot(wo,nDir)/part_pdf;
        }
        L /=path_pdf;
    }
    pdf = path_pdf;
	//(we should simple think about energy absorption)
    _pathWeight *= EnergyAbsorptionTerm(alpha,L_local);
    return L;
}

float3 Radiance(Ray ray,float x1){
	float t = INFINITY;
    Intersection intersecNow;
    Intersection intersecNext;
    float3 col = float3(0.,0.5,0.);
    float3 Lo = float3(0.,0.,0.);
    float pathWeight = 1.;
    if(SceneIntersect(ray,intersecNow)>=INFINITY)
    	return BACKGROUD_COL;
    if(intersecNow.type == LIGHT){
        Lo = GetLightIntensityFrom(intersecNow.emission);
        return Lo;
    }
    for(int step=0;step<GI_DEPTH;step++){
		if(intersecNow.type == LIGHT)
            break;
   		float3 viewDir = -ray.direction;
        float3 nDir = intersecNow.normal;
        nDir = faceforward(nDir,-viewDir,nDir);
        Material mat = GetMaterial(intersecNow);
        if(dot(viewDir,nDir)<0.)
            break;
        float3 _point = ray.origin + ray.direction * intersecNow.distance +nDir*0.00001;
        float Light_pdf,BRDF_pdf;
       	float3 LIGHT_S = LightingDirectSample(mat,_point,nDir,viewDir,Light_pdf);
        float3 BRDF_S = LightingBRDFSample(mat,_point,nDir,viewDir,pathWeight,intersecNext,ray,BRDF_pdf);
        Lo += LIGHT_S*MISWeight(1.,Light_pdf,1.,BRDF_pdf)
           +  BRDF_S*MISWeight(1.,BRDF_pdf,1.,Light_pdf);
        Lo *= pathWeight;
		intersecNow = intersecNext;
    }
    return Lo/float(GI_DEPTH);
}

#define R ViewportSize.xy
const float2 FRAME_START_UV = float2(0.,0.);
float4 readValues(float2 xy){
	return tex2D(iChannel0Sampler,(xy+0.5)/R);
}

float4 PS(BufferAVertexOut input) : SV_TARGET
{
	float4 C = float4(0.,0.,0.,0.);
	float2 U = input.ScreenPosition.xy;

	// Setup
	float3 dir = normalize(input.Ray);
	float3 pos = CameraPosition;
	float noise = TriangularNoise(U.x+U.y, iTime);
    seed = iTime*sin(noise) + (U.x+R.x*U.y)/(R.y);
    SetupScene();
    
	// Render
	float2 dither = TriangularNoise2DShereRay(U,iTime);
    pos += float3(dither,0.)*0.004;
    Ray ray = NewRay(pos,dir);
    float3 newColor = Radiance(ray,GetRandom());
	newColor = clamp(newColor, 0.0, 10.0);

	// Blend
	float3 oldColor = tex2D(iChannel0Sampler,U/R).rgb;
	float lerpValue = 1. / clamp(LerpBalance, 0.5, 22.);
	C = float4(lerp (oldColor, newColor, lerpValue), 1.0);

	return C;
}

//==============================================================================
// Technique
//==============================================================================

technique BufferA
{
	pass P0
	{
		VertexShader = compile VS_SHADERMODEL VSBufferA();
		PixelShader = compile PS_SHADERMODEL PS();
	}
};


//==============================================================================
// COMPOSITION
//==============================================================================

uniform Texture2D bufferA;
uniform sampler2D bufferASampler = sampler_state
{
    Texture = <bufferA>;
    MinFilter = linear;
    MagFilter = linear;
    MipFilter = linear;
};

struct CompositionVertexIn
{
	float3 Position : POSITION;
    float2 TexCoord : TEXCOORD0;
	float3 Color : Color0;
};

struct CompositionVertexOut
{
    float2 TexCoord : TEXCOORD0;
};

CompositionVertexOut VSComposition(CompositionVertexIn input)
{
	CompositionVertexOut vout = (CompositionVertexOut)0;

	vout.TexCoord = input.TexCoord;

	return vout;
}

float4 PS2(float4 position : POSITION0) : COLOR0
{
	// float4 result = tex2D(bufferASampler, position.xy / ViewportSize.xy);
	float4 result = FXAA(bufferASampler, position.xy / ViewportSize.xy,R);
    result.rgb = ExposureCorrect(result.rgb,2.1, -0.8);
    // result.rgb = GammaToLinear(result.rgb);
 
	return result;
}

technique Composition
{
	pass P0
	{
		PixelShader = compile PS_SHADERMODEL PS2();
	}
}