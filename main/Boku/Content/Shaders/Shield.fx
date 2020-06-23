
//
// Particle3D -- A collection of shaders for non-textured, 3D particles.
//

#include "Globals.fx"

#include "Fog.fx"
#include "DOF.fx"
#include "StandardLight.fx"

//
// Locals.
//

// The world view and projection matrices
float4x4    WorldViewProjMatrix;
float4x4    WorldMatrix;

// Sphere info.
float4		Center; // Radius is .w
float3		TexU;
float3		TexV;
float		Age;
float3		TexW;
float4		Tint0;
float4		Tint1;

/// Texture section
texture AxialTexture;
texture CrossTexture;

sampler2D AxialSampler =
sampler_state
{
    Texture = <AxialTexture>;
    MipFilter = Linear;
    MinFilter = Linear;
    MagFilter = Linear;

    AddressU = Clamp;
    AddressV = Clamp;
};

sampler2D CrossSampler =
sampler_state
{
    Texture = <CrossTexture>;
    MipFilter = Linear;
    MinFilter = Linear;
    MagFilter = Linear;

    AddressU = Clamp;
    AddressV = Clamp;
};


//
// Vertex shader output structure
//
struct COLOR_VS_OUTPUT
{
    float4 position         : POSITION;     // vertex position
    float4 color			: COLOR;
    float3 normal           : TEXCOORD0;    // normal in world space after transform
    float3 worldPosition	: TEXCOORD1;
    float4 uvw				: TEXCOORD2;
    float3 eye				: TEXCOORD3;
    float  fog              : TEXCOORD4;    // fog strength in x
};

// Transform our coordinates into world space
COLOR_VS_OUTPUT ColorVS(float3 position : POSITION)
{
    COLOR_VS_OUTPUT   Output;

    // Transform our position.
    float4 worldPosition = float4(position * Center.w + Center, 1.0f);
    Output.position = mul( worldPosition, WorldViewProjMatrix );

    // Transform the normal into world coordinates.
    Output.normal = position.xyz;

    // Transform the position into world coordinates for calculating the eye vector.
    Output.worldPosition = worldPosition;

    // Calc the eye vector.  This is the direction from the point to the eye.
    float3 eyeDir = EyeLocation - worldPosition.xyz / worldPosition.w;
    float eyeDist = length(eyeDir);
    Output.eye = eyeDir / eyeDist;  // Normalize
    
//    Output.uvw.x = dot(position, TexU) / (Age + 0.1f) * 0.5f + 0.5f;
//    Output.uvw.y = dot(position, TexV) / (Age + 0.1f) * 0.5f + 0.5f;

	float len = dot(position, TexW);
	Output.uvw.x = dot(position, TexU);
	Output.uvw.y = dot(position, TexV);
	Output.uvw.xy = normalize(Output.uvw.xy);
	Output.uvw.xy *= (1.0f - len) * 0.95f / (Age + 0.1f) * 0.5f;
	Output.uvw.xy = Output.uvw.xy * 0.5f + 0.5f;
	Output.uvw.z = len > -0.95f ? 1.0f : 0.0f;
    Output.uvw.w = 1.0f - Age;
    Output.uvw.z *= Output.uvw.w;
    
    Output.color = lerp(Tint0, Tint1, Age);

    Output.fog = CalcFog( eyeDist );

    return Output;
}

//
// Basic Pixel shader
//
float4 ColorPS( COLOR_VS_OUTPUT In ) : COLOR0
{
    // Normalize our vectors.
    float3 normal = normalize( In.normal );
    float3 eye = normalize( In.eye );
    
    float4 result = tex2D(CrossSampler, In.uvw.xy);
    result.xyz *= In.uvw.z;
    result.xyz *= In.color.xyz;
    
//    result *= tex2D(AxialSampler, float2(In.uvw.z, 0.5f));
    
//    result.xyz = float3(0.2f, 0.2f, 0.4f);
//    result.w = saturate(dot(-eye, normal) + 1.0f);
//result.xy = In.uvw.xy;
//result.xyz = (In.uvw.z < 0.1f) && (In.uvw.z > 0.0f) ? 1.0f.xxx : 0.0f.xxx;
//result.w = 1.0f;
//result.z = 0.0f;
//result.xy = In.uvw.yy;
result.w = result.x;
    
    // Return the combined color
    return result;
}   // end of BasicPS()

//
// Technique - basic, full color
//
technique TexturedColorPass
{
    pass P0
    {
        VertexShader = compile vs_2_0 ColorVS();
        PixelShader  = compile ps_2_0 ColorPS();

        /* // Alpha test
        AlphaRef = 1;
        AlphaTestEnable = true;
        AlphaFunc = GreaterEqual; */

        // Alpha blending
        AlphaBlendEnable = true;
        SrcBlend = One;
        DestBlend = One;

        CullMode = None;

        ZEnable = true;
        ZFunc = LessEqual;
        ZWriteEnable = false;
    }
}

//
//
//  Depth passes.
//
//

//
// Vertex shader output structure
//
struct DEPTH_VS_OUTPUT
{
    float4 position         : POSITION;     // vertex position
    float3 normal			: TEXCOORD0;
    float4 eye				: TEXCOORD1;
};

// Transform our coordinates into world space
DEPTH_VS_OUTPUT DepthVS(float3 position : POSITION)
{
	DEPTH_VS_OUTPUT Output;
	
    float4 worldPosition = float4(position * Center.w + Center, 1.0f);
    Output.position = mul( worldPosition, WorldViewProjMatrix );

    // Transform the normal into world coordinates.
    Output.normal = position.xyz;

    // Calc the eye vector.  This is the direction from the point to the eye.
    float3 eyeDir = EyeLocation - worldPosition.xyz;
    float eyeDist = length(eyeDir);
    Output.eye.xyz = eyeDir / eyeDist;
    
	float len = dot(position, TexW);
	Output.eye.w = 1.0f - (1.0f - len) * 0.5f / (Age + 0.1f);
    Output.eye.w *= smoothstep(0.5f, 0.75f, Output.eye.w);

    return Output;
}

//
// Pixel shader
//
float4 DepthPS( DEPTH_VS_OUTPUT In ) : COLOR0
{
    float4 result = float4(1.0f, 0.0f, 0.0f, 0.0f);
    float strength = dot(normalize(In.normal), normalize(In.eye.xyz));
    strength = -strength * strength + 1.0f;
    strength *= In.eye.w;
    result.x *= strength;
    
    return result;
}

//
// Technique
//
technique DepthPass
{
    pass P0
    {
        VertexShader = compile vs_2_0 DepthVS();
        PixelShader  = compile ps_2_0 DepthPS();

        /* // Alpha test
        AlphaRef = 1;
        AlphaTestEnable = false;
        AlphaFunc = GreaterEqual; */

        // Alpha blending
        AlphaBlendEnable = true;
        SrcBlend = One;
        DestBlend = One;

        CullMode = None;

        ZEnable = true;
        ZFunc = LessEqual;
        ZWriteEnable = false;
    }
}




