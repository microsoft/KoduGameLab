// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

//===========================================================================
// ScoreEffect Shader
//===


//===========================================================================
// Local Variables
//===

float4x4 WorldViewProj;
texture ScoreTexture;
float2 ScoreSize;
float ScoreAlpha;
float4 ScoreColor;
float4 ScoreColorDarken;


//===========================================================================
// Texture sampler
//===

sampler2D ScoreTextureSampler =
sampler_state
{
    Texture = <ScoreTexture>;
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


//===========================================================================
// Shaders
//===

VS_OUTPUT ColorVS(float3 pos : POSITION, float2 tex : TEXCOORD0)
{
    VS_OUTPUT   Output;

	pos.x = -tex.x * ScoreSize.x + ScoreSize.x / 2;
	pos.y = -tex.y * ScoreSize.y + ScoreSize.y / 2;
	pos.z = 0;
    Output.position = mul(float4(pos.x, pos.y, pos.z, 1.0f), WorldViewProj);
    Output.textureUV = tex;

    return Output;
}   // end of VS()


float4 ColorPS(VS_OUTPUT In) : COLOR0
{
    float4 result = tex2D(ScoreTextureSampler, In.textureUV);
    
    //result.rgb = ScoreColor.rgb;
    result.rgb *= ScoreColor.rgb;
	result.a *= ScoreAlpha;
	result *= ScoreColorDarken;

    return result;
}   // end of PS()


//===========================================================================
// Techniques
//===

technique T0
{
    pass P0
    {
        /* // Alpha test
        AlphaRef = 1;
        AlphaTestEnable = true;
        AlphaFunc = GreaterEqual; */

        // Alpha blending
        AlphaBlendEnable = true;
        SrcBlend = SrcAlpha;
        DestBlend = InvSrcAlpha;

        CullMode = None;

        ZEnable = false;
        ZWriteEnable = false;

        VertexShader = compile vs_2_0 ColorVS();
        PixelShader = compile ps_2_0 ColorPS();
    }
}
