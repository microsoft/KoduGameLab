// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

//===========================================================================
// HealthBar Shader
//===

#include "Globals.fx"

#include "Fog.fx"
#include "DOF.fx"

//===========================================================================
// Variables
//===

float4x4 WorldViewProj;

texture BackTexture;
texture LifeTexture;

float4 LifeTint;
float LifePct;

float2 BackSize;
float2 LifeSize;


//===========================================================================
// Texture samplers
//===
sampler2D BackTextureSampler =
sampler_state
{
    Texture = <BackTexture>;
    MipFilter = Linear;
    MinFilter = Linear;
    MagFilter = Linear;

    AddressU = Clamp;
    AddressV = Clamp;
};

sampler2D LifeTextureSampler =
sampler_state
{
    Texture = <LifeTexture>;
    MipFilter = Linear;
    MinFilter = Linear;
    MagFilter = Linear;

    AddressU = Clamp;
    AddressV = Clamp;
};


//===========================================================================
// Vertex shader output structure
//===
struct VS_OUTPUT
{
    float4 position     : POSITION;     // vertex position
    float2 textureUV    : TEXCOORD0;    // vertex texture coords
};


VS_OUTPUT VS(float3 pos : POSITION, float2 tex : TEXCOORD0)
{
    VS_OUTPUT   Output;

	pos.x = -tex.x * BackSize.x + BackSize.x / 2;
	pos.y = -tex.y * BackSize.y + BackSize.y / 2;
    Output.position = mul(float4(pos.x, pos.y, pos.z, 1.0f), WorldViewProj);
    Output.textureUV = tex;

    return Output;
}   // end of VS()


float4 PS(VS_OUTPUT In) : COLOR0
{
    float4 result = tex2D(BackTextureSampler, In.textureUV);
    float4 life = LifeTint * tex2D(LifeTextureSampler, In.textureUV);
    if(result.a < 0.9 && result.a > 0.1 
        && LifePct > In.textureUV.x 
        && In.textureUV.x > 0.025 && In.textureUV.x < 0.975
        && In.textureUV.y > 0.15 && In.textureUV.y < 0.85)
    {
        result.rgb = lerp(result.rgb, life.rgb, life.a);
        result.a = 1;   // Make color part of bar opaque.
    }

    return result;
}   // end of PS()

//===========================================================================
// Technique
//===
technique T0
{
    // Combined pass.
    pass P0
    {
        // Alpha blending
        AlphaBlendEnable = true;
        SrcBlend = SrcAlpha;
        DestBlend = InvSrcAlpha;

        CullMode = None;

        ZEnable = true;
        ZFunc = LessEqual;
        ZWriteEnable = true;

        VertexShader = compile vs_2_0 VS();
        PixelShader = compile ps_2_0 PS();
    }

}
