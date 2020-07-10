// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

//
// LightColumn
//

#include "Globals.fx"

//#include "Fog.fx"
#include "DOF.fx"

//
// Locals.
//

// The world view and projection matrices
float4x4    WorldViewProjMatrix;
float4x4    WorldMatrix;

// Material info.
float4      Color;
float       TextureOffset;

texture     LightTexture;

//
// Texture samplers
//
sampler2D LightTextureSampler =
sampler_state
{
    Texture = <LightTexture>;
    MipFilter = Linear;
    MinFilter = Linear;
    MagFilter = Linear;
    
    AddressU = Wrap;
    AddressV = Clamp;
};

//#include "ShadowInc.fx"

//
// Vertex shader output structure
//
struct COLOR_VS_OUTPUT
{
    float4 position         : POSITION;     // vertex position
    float2 textureUV        : TEXCOORD0;    // vertex texture coords
};

// Transform our coordinates into world space
COLOR_VS_OUTPUT ColorVS(
                        float3 position : POSITION,
                        float3 normal   : NORMAL,
                        float2 tex      : TEXCOORD0)
{
    COLOR_VS_OUTPUT Output;

    // Transform our position.
    float4 pos;
    pos.xyz = position;
    pos.w = 1.0f;
    Output.position = mul( pos, WorldViewProjMatrix );

    Output.textureUV = tex;

    return Output;
}

//
// Pixel shader
//
float4 ColorPS( COLOR_VS_OUTPUT In ) : COLOR0
{
    // Sample the texture.
    float4 color = Color * ( tex2D( LightTextureSampler, In.textureUV + float2( TextureOffset, 0.0f ) ) + tex2D( LightTextureSampler, In.textureUV + float2( -TextureOffset, 0.0f ) ) );

    return color;
}   // end of ColorPS()



//
// Techniques
//
technique AdditiveTexturedColorPass
{
    pass P0
    {
        VertexShader = compile vs_2_0 ColorVS();
        PixelShader  = compile ps_2_0 ColorPS();

        // Alpha test
        AlphaRef = 1;
        AlphaTestEnable = false;
        AlphaFunc = GreaterEqual;

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
// Vertex shader output structure
//
struct EFFECTS_VS_OUTPUT
{
    float4 position         : POSITION;     // vertex position
    float4 color            : TEXCOORD0;    // effects values
};

// Transform our coordinates into world space
EFFECTS_VS_OUTPUT EffectsVS(
                        float3 position : POSITION,
                        float3 normal   : NORMAL,
                        float2 tex      : TEXCOORD0)
{
    EFFECTS_VS_OUTPUT Output;

    // Transform our position.
    float4 pos;
    pos.xyz = position;
    pos.w = 1.0f;
    Output.position = mul( pos, WorldViewProjMatrix );
    
    // Transform the position into world coordinates for calculating the eye vector.
    float4 worldPosition = mul( pos, WorldMatrix );

    // Calc the eye vector.  This is the direction from the point to the eye.
    float3 eyeDir = EyeLocation - worldPosition.xyz / worldPosition.w;
    float eyeDist = length(eyeDir);

    Output.color = CalcDOF( eyeDist );

    return Output;
}

//
// Pixel shader
//
float4 EffectsPS( EFFECTS_VS_OUTPUT In ) : COLOR0
{
    float4 result = In.color;
    
    return result;
}   // end of EffectsPS()



//
// Techniques
//
technique AdditiveTexturedEffectsPass
{
    pass P0
    {
        VertexShader = compile vs_2_0 EffectsVS();
        PixelShader  = compile ps_2_0 EffectsPS();

        // Alpha test
        AlphaRef = 1;
        AlphaTestEnable = false;
        AlphaFunc = GreaterEqual;

        // Alpha blending
        AlphaBlendEnable = false;
        SrcBlend = One;
        DestBlend = One;

        CullMode = None;

        ZEnable = true;
        ZFunc = LessEqual;
        ZWriteEnable = true;
    }
}

