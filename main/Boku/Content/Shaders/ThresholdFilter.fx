// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

//
//  Threshold -- Allow anything above the threshold to pass through, 
//               anything below is clamped to 0.
//

//
// Variables
//
texture SourceTexture;
float   ThresholdValue;

//
// Texture samplers
//
sampler2D SourceTextureSampler =
sampler_state
{
    Texture = <SourceTexture>;
    MipFilter = Linear;
    MinFilter = Linear;
    MagFilter = Linear;

    AddressU = Clamp;
    AddressV = Clamp;
};

//
// Vertex shader output structure
//
struct VS_OUTPUT
{
    float4 position     : POSITION;     // vertex position
    float2 textureUV    : TEXCOORD0;    // vertex texture coords
};

#include "QuadUvToPos.fx"

//
// Vertex shader
//
VS_OUTPUT
VS( float2 tex : TEXCOORD0 )
{
    VS_OUTPUT   Output;

    Output.position = QuadUvToPos(tex, 0.0f);
    Output.textureUV = tex;
    
    return Output;
}   // end of VS()

//
// Copy Pixel shader
//
float4
PS( VS_OUTPUT In ) : COLOR0
{
    float4 result;

    result = tex2D( SourceTextureSampler, In.textureUV );
    float manual = 1.0f - result.a;
    float3 over = result.rgb >= ThresholdValue;
    result.rgb = dot(over, 1.0f) > 0.0f ? result.rgb : result.rgb * manual;

    return result;
}   // end of PS()

//
// Threshold an image.
//
technique Threshold
{
    pass P0
    {
        VertexShader = compile vs_2_0 VS();
        PixelShader  = compile ps_2_0 PS();

        /* // Alpha test
        AlphaRef = 1;
        AlphaTestEnable = false;
        AlphaFunc = GreaterEqual; */

        // Alpha blending
        AlphaBlendEnable = false;
        SrcBlend = SrcAlpha;
        DestBlend = InvSrcAlpha;

        CullMode = None;

        ZEnable = false;
        ZFunc = Less;
        ZWriteEnable = false;
    }
}

