// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.


#include "Globals.fx"

#include "Fog.fx"
#include "DOF.fx"
#include "EyeDist.fx"

float4   Color;			// Tint of the water
float	 TextureTile;	// 
float2	 ToNeighbor;		// Vector from this center to the next.
float4   NeighborSelect; // Dot with this to pull out the relevant neighbor info.
float	 NeighborCutoff; // Decide whether 0.5 counts as 0 or 1
float3	 UVToX;
float3   UVToY;
float	 IsTop; // 2.0f if it's the top face, else zero.
float	 HalfSize; // half the size of a cube.
float4x4 BumpToWorld;
float3   LightDir;
float2   Fresnel;
float    Shininess;
float3   Emissive;

float4x4 WorldToNDC;	// World to screen transform

texture BumpMap;

sampler2D BumpMapSampler =
sampler_state
{
    Texture = <BumpMap>;
    MipFilter = Linear;
    MinFilter = Linear;
    MagFilter = Linear;

    AddressU = WRAP;
    AddressV = WRAP;
};

shared texture  EnvironmentMap;

samplerCUBE EnvMapSampler =
sampler_state
{
    Texture = <EnvironmentMap>;
    MipFilter = Linear;
    MinFilter = Linear;
    MagFilter = Linear;

    AddressU = WRAP;
    AddressV = WRAP;
};


//
//
//  Water
//
//

//
// Vertex shader output structure
//
struct COLOR_VS_OUTPUT
{
    float4 position     : POSITION;     // vertex position
    float3 normal       : TEXCOORD0;    // vertex normal
    float4 eye			: TEXCOORD1;	// vector to eye from point
    float2 textureUV    : TEXCOORD2;    // vertex texture coords
};

//#define DOUBLE_WAVES

#include "WaterHeight.fx"

//
// Vertex Shader
//
COLOR_VS_OUTPUT ColorVS(
                        float3 center		: POSITION0,
                        float4 neighbors	: COLOR0,
                        float2 uv           : TEXCOORD0
                        )
{
    COLOR_VS_OUTPUT	Output;

	float3 worldPos;
	worldPos.x = center.x + dot(uv, UVToX.xy) + UVToX.z;
	worldPos.y = center.y + dot(uv, UVToY.xy) + UVToY.z;
	HeightAndNormal(WaveCenter, InverseWaveLength, center, worldPos.z, Output.normal);
#ifdef DOUBLE_WAVES
	float4 norm2;
	HeightAndNormal(WaveCenter.yx, InverseWaveLength * 1.3f, center, norm2.w, norm2.xyz);	
	Output.normal += norm2.xyz;
	worldPos.z += norm2.w;
	worldPos.z -= BaseHeight;
#endif // DOUBLE_WAVES
	
	float neighborHeight = Height(WaveCenter, InverseWaveLength, center + ToNeighbor);
#ifdef DOUBLE_WAVES
	neighborHeight += Height(WaveCenter.yx, InverseWaveLength * 1.3f, center + ToNeighbor);
	neighborHeight -= BaseHeight;
#endif // DOUBLE_WAVES
	
	neighbors = neighbors > NeighborCutoff.xxxx ? 1.0f.xxxx : 0.0f.xxxx;
	worldPos.z = uv.y + IsTop >= 0.0f 
		? worldPos.z
		: neighborHeight * dot(NeighborSelect, neighbors);
		
	worldPos.z = max(worldPos.z, center.z);

    // Transform our position.
    Output.position = mul( float4(worldPos, 1.0f), WorldToNDC );

    Output.textureUV = worldPos.xy * TextureTile;

    // Calc fog and DOF contributions.
    // Calc the eye vector.  This is the direction from the point to the eye.
    // Note this assumes that the WorldMatrix is identity;
    float4 eyeDist = EyeDist(worldPos.xyz);

    Output.eye.xyz = eyeDist.xyz;
    Output.eye.w = CalcFog( eyeDist.w );

    return Output;
}   // end of ColorVS()


//
// Pixel shader
//
float4 ColorPS( COLOR_VS_OUTPUT In ) : COLOR0
{
//return float4(1.0f, 1.0f, 0.0f, 1.0f);

    // Get the normal and transform it back into world space.
    float3 normal = tex2D( BumpMapSampler, In.textureUV );

    In.textureUV.x -= WaveCycle * 0.11f;
    In.textureUV.y -= WaveCycle * 0.13f;
    float3 normal2 = tex2D( BumpMapSampler, In.textureUV );

    In.textureUV.x += WaveCycle * 0.17f;
    In.textureUV.y += WaveCycle * 0.21f;
    float3 normal3 = tex2D( BumpMapSampler, In.textureUV );
    normal = (normal + normal2 + normal3) * 0.33f;

    // Shift from 0..1 to -1..1  Note this leaves this vector
    // approximately length 2.
    normal = normal * 2.0f - 1.0f;

    // Add in vertex normal (which is close to normalized) and normalize result.
    // We weight the vertex normal more so that it dominates.
    normal = normalize( normal + In.normal * 2.0f );
    
    normal = mul(normal, BumpToWorld);

    // Normalize our eye vector.
    float3 eye = normalize( In.eye.xyz );

    // Create a reflection vector
    float EdotN = saturate( dot( eye, normal ) );
    float3 reflection = normalize( 2 * EdotN * normal - eye );

    // Calc lighting.
	float LdotN = dot(normal, LightDir);
	float specular = pow( saturate(dot(reflection, LightDir)), 64 ) * Shininess;

    // Sample env map
    float3 env = texCUBE( EnvMapSampler, reflection );
    float fresnel = 1.0f - abs( dot( normal, eye ) );
    //fresnel = fresnel * fresnel;
    fresnel = fresnel * Fresnel.x + Fresnel.y;

    float4 result;
	result.rgb = (Color.rgb * LdotN + specular.rrr) * LightColor0.rgb + Emissive.rgb;

    result.rgb += env * fresnel * Shininess;    

//result.rgb = Color.rgb;
//result.rgb = LightColor0.rgb;

    result.a = fresnel;

    // Add in fog.
    result.rgb = lerp( result, FogColor, In.eye.w );

    // Return the combined color
    return result;

}   // end of ColorPS()


//
// Water
//
technique ColorPass
{
    pass P0
    {
        VertexShader = compile vs_2_0 ColorVS();
        PixelShader  = compile ps_2_0 ColorPS();

        /* // Alpha test
        AlphaRef = 1;
        AlphaTestEnable = false;
        AlphaFunc = GreaterEqual; */

        // Alpha blending
        AlphaBlendEnable = true;
        SrcBlend = SrcAlpha;
        DestBlend = InvSrcAlpha;

        CullMode = None;

		/// We enable and disable depth buffering in code, because
		/// we want it rendering the world, but don't want it rendering UI.
        ZFunc = LessEqual;
        ZWriteEnable = false;
    }
}


//
// Vertex shader output structure
//
struct DEPTH_VS_OUTPUT
{
    float4 position     : POSITION;     // vertex position
    float4 color        : TEXCOORD0;    // depth values
};

//
// Vertex Shader
//
DEPTH_VS_OUTPUT DepthVS(
						float3 center		: POSITION0,
						float2 uv           : TEXCOORD0,
						float4 neighbors	: COLOR0
						)
{
    DEPTH_VS_OUTPUT	Output;

	float3 worldPos;
	worldPos.x = center.x + dot(uv, UVToX.xy) + UVToX.z;
	worldPos.y = center.y + dot(uv, UVToY.xy) + UVToY.z;
	
	worldPos.z = Height(WaveCenter, InverseWaveLength, center);
#ifdef DOUBLE_WAVES
	worldPos.z += Height(WaveCenter.yx, InverseWaveLength * 1.3f, center);
	worldPos.z -= BaseHeight;
#endif // DOUBLE_WAVES
	
	float neighborHeight = Height(WaveCenter, InverseWaveLength, center + ToNeighbor);
#ifdef DOUBLE_WAVES
	neighborHeight += Height(WaveCenter.yx, InverseWaveLength * 1.3f, center + ToNeighbor);
	neighborHeight -= BaseHeight;
#endif // DOUBLE_WAVES

	worldPos.z = uv.y + IsTop >= 0.0f 
		? worldPos.z
		: neighborHeight * dot(NeighborSelect, neighbors);

	worldPos.z = max(worldPos.z, center.z);
	
    // Transform our position.
    Output.position = mul( float4(worldPos, 1.0f), WorldToNDC );

    float3  eyeDir = EyeLocation - worldPos.xyz;
    float   eyeDist = length( eyeDir );
    Output.color = CalcDOF( eyeDist );
    Output.color.b = 0.0f;

    return Output;
}   // end of DepthVS()


//
// Pixel shader
//
float4 DepthPS( DEPTH_VS_OUTPUT In ) : COLOR0
{
    float4 result = In.color;
    
    return result;
}   // end of DepthPS()


//
// Water
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
        AlphaBlendEnable = false;
        SrcBlend = SrcAlpha;
        DestBlend = InvSrcAlpha;

        CullMode = None;

        ZEnable = true;
        ZFunc = LessEqual;
        ZWriteEnable = false;
    }
}
