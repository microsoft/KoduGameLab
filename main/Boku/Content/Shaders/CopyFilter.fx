// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

//
//  Copy -- straight pixel copy with attenuation.
//

//
// Variables
//
texture SourceTexture;
float   Attenuation;
float4  Scale;

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

    result = Attenuation * tex2D( SourceTextureSampler, In.textureUV );

    return result;
}   // end of PS()

//
// Just copy the image
//
technique Copy
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

//
// Just copy the image
//
technique Add
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
        AlphaBlendEnable = true;
        SrcBlend = One;
        DestBlend = One;

        CullMode = None;

        ZEnable = false;
        ZFunc = Less;
        ZWriteEnable = false;
    }
}

