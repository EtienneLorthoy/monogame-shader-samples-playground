#define VS_SHADERMODEL vs_5_0
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

uniform Texture3D<float4> voxelData;

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

//// Voxel
const float4 AmbientColor = float4(0.2f,0.3f,0.1f,1.0f);
const float AmbientIntensity = 0.05f;
float LightIntensity = 10.0f;
float3 LightPosition = float3(5.0f,5.0f,2.0f);
float3 BackLightPosition = normalize(float3(-1.0f,-1.0f,-2.0f));
const float4 DiffuseColor = float4(1.0f,1.0f,0.8f,0.0f);
const float DiffuseIntensity = 1.0f;
const float4 SpecColor = float4(1.0f,1.0f,0.8f,0.0f);
const float SpecIntensity = 1.0f;

const int maxIter = 256;
const float voxelSize = 0.01f;

bool voxelHit(float3 pos) 
{
	if (pos.x >= 0. && pos.y >= 0. && pos.z >= 0.
	 	&& pos.x < 128. && pos.y < 128. && pos.z < 128.)
	{
		float4 voxelPoint = voxelData[pos];

		// Alpha 0 is the signal that the voxel is empty
		return voxelPoint.w != 0;
	}
	else return false;
}

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
	--------------------Main Reference Knowledge-------------------
	"Fast PathTracing[Lab]!" by 834144373(恬纳微晰) url : https://www.shadertoy.com/view/XdVfRm
*/

#define GI_DEPTH 1

/*Type*/
#define LIGHT 0
#define DIFF 1
#define REFR 2
#define SPEC 3

const float3 ZERO = float3(0.,0.,0.);
const float3 ONE  = float3(1.,1.,1.);
const float3 UP   = float3(0.,1.,0.);

struct Ray { float3 origin; float3 direction; };
struct LightQuad { float3 normal; float3 v0; float3 v1; float3 v2; float3 v3; float3 emission; float3 color; float roughness; int type; };
struct Intersection {float distance;float2 uv; float3 normal; float3 emission; float3 color; float roughness; int type; };

Ray NewRay(float3 origin, float3 direction)
{
	Ray r;
	r.origin = origin;
	r.direction = direction;
	return r;
}

LightQuad NewLightQuad(float3 normal, float3 v0, float3 v1, float3 v2, float3 v3, float3 emission, float3 color, float roughness)
{
	LightQuad q;
	q.v0 = v0;
	q.v1 = v1;
	q.v2 = v2;
	q.v3 = v3;
	q.emission = emission;
	q.color = color;
	q.roughness = roughness;
	q.type = LIGHT;
	q.normal = normal;
	return q;
}

Intersection NewIntersection(float distance, float2 uv, float3 normal, float3 emission, float3 color, float roughness, int type)
{
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

static LightQuad lighquad;

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

// I = ΔΦ/4π
float3 GetLightIntensity(){
	return LightIntensity * ONE;
}

float3 GetLightIntensityFrom(float3 col){
	return GetLightIntensity() * col;
}

float SceneIntersect(Ray r, inout Intersection intersec ){
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

	t = QuadIntersect( lighquad.v0, lighquad.v1, lighquad.v2, lighquad.v3, lighquad.normal, r );
	if (t < d)
	{
		d = t;
		intersec.normal = normalize(lighquad.normal);
		intersec.emission = lighquad.emission;
		intersec.color = lighquad.color;
		intersec.roughness = lighquad.roughness;
		intersec.type = lighquad.type;
		intersec.distance = d;
	}

    float dist;
    float3 norm;
    t = castRay(r.origin, r.direction, dist, norm);
	if (t < d)
	{
        d = t;
		intersec.normal = normalize(norm);
		float4 color = voxelColor(r.origin + r.direction * d - norm * 0.0001f);
		intersec.emission = color.xyz *0.3f;
		intersec.color = color.xyz;
		intersec.roughness = 0.1f;
		intersec.type = SPEC;
    	intersec.distance = d;
	}

    return d;
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
	float3 v0v1 = lighquad.v1 - lighquad.v0;
    float3 v0v3 = lighquad.v3 - lighquad.v0;
    float width  = length(v0v1);
    float height = length(v0v3);
    float3 O = lighquad.v0 + v0v1*x1 + v0v3*x2;
    wo = O - p;
    dist = length(wo);
    wo = normalize(wo);
    float costhe = dot(-wo,lighquad.normal);
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

float3 LightingDirectSample(Material mat,float3 p,float3 nDir,float3 vDir,out float pdf){
	float3 L = float3(0.,0.,0.);
    float x1 = GetRandom(),x2 = GetRandom();
    float3 wo;
    float dist;
    float3 Li = LightSample(p,x1,x2,wo,dist,pdf) * mat.specular;
    float WoDotN = dot(wo,nDir);
    if(WoDotN >= 0. && pdf > 0.0001)
	{
        float3 Lr = MicroFactEvalution(mat,nDir,wo,vDir);
        Ray shadowRay = NewRay(p,wo);
        Intersection shadow_intersc;
        float d = SceneIntersect(shadowRay,shadow_intersc);
        if(shadow_intersc.type == LIGHT) L = Lr*Li/pdf;
    }
    return L;
}

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
    if(GetRandom() < part_pdf)
	{
        //diffuse ray
        L_local = DiffuseUnitSphereRay(x1,x2);
        path_pdf = Diffuse_PDF(L_local);
    }
    else
	{
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

float3 Radiance(Ray ray,float x1)
{
    Intersection intersecNow;
    Intersection intersecNext;
    float3 col = float3(0.,0.5,0.);
    float3 Lo = float3(0.,0.,0.);
    float pathWeight = 1.;

	// Find the point
	SceneIntersect(ray,intersecNow);

	// Early out
    if(intersecNow.distance >=INFINITY) return ZERO;

	// Direct lighting
    if(intersecNow.type == LIGHT) return GetLightIntensityFrom(intersecNow.emission);

	// Indirect lighting
    for(int step=0;step<GI_DEPTH;step++)
	{
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

float4 PS(BufferAVertexOut input) : SV_TARGET
{
	float4 result = float4(0.,0.,0.,0.);
	float2 U = input.ScreenPosition.xy;
	float2 R = ViewportSize.xy;

	// Setup
	float3 ray = normalize(input.Ray);
	float3 pos = CameraPosition;
	float noise = TriangularNoise(U.x+U.y, iTime);
    seed = iTime*sin(noise) + (U.x+R.x*U.y)/(R.y);
	// Top Light
    // lighquad = NewLightQuad(float3(0.,-1.,0.5),float3(0.f,64.f,0.f), float3(0.f,64.f,128.f), float3(128.f,64.,0.f), float3(128.0f,.64f,128.f), float3(0.8f,0.8f,0.3f), float3(1.0,0.0,0.0),0.4);
    // 45° at origin Light
	lighquad = NewLightQuad(float3(0.4,-0.5,0.6),float3(-32.f,32.f,32.f), float3(-32.f,64.f,32.f), float3(32.f,64.,-32.f), float3(32.0f,.32f,-32.f), float3(0.8f,0.8f,0.3f), float3(1.0,0.0,0.0),0.4);
    
	// Render
	float2 dither = TriangularNoise2DShereRay(U,iTime);
    pos += float3(dither,0.)*0.02;
    Ray rayObj = NewRay(pos,ray);
    float3 newColor = Radiance(rayObj,GetRandom());

	// Blend
	float3 oldColor = tex2D(iChannel0Sampler,U/R).rgb;
	float lerpValue = 1. / clamp(LerpBalance, 0.5, 5.);
	result = float4(lerp(oldColor, newColor, lerpValue), 1.0);

	return result;
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
	float4 result = tex2D(bufferASampler, position.xy / ViewportSize.xy);
	// float4 result = FXAA(bufferASampler, position.xy / ViewportSize.xy,ViewportSize.xy);
    result.rgb = ExposureCorrect(result.rgb,2.1, -0.8);
 
	return result;
}

technique Composition
{
	pass P0
	{
		PixelShader = compile PS_SHADERMODEL PS2();
	}
}