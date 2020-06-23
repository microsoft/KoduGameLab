//
//  Box4x4Blur -- 4 by 4 box filter, 
//      designed to be fast if used to downsample to a rendertarget that is 1/4 the size.
//

//
// Variables
//
texture	SourceTexture;
float2  PixelSize;      // Since XNA doesn't support a pretransformed position vertex decl we
                        // have to scale all out offsets by the pixel size.

//
// Texture samplers
//
sampler2D SourceTextureSampler =
sampler_state
{
    Texture = <SourceTexture>;
    MipFilter = NONE; //LINEAR;
    MinFilter = LINEAR;
    MagFilter = LINEAR;

    AddressU = Clamp;
    AddressV = Clamp;
};

float2 Offsets[ 4 ] = {
    { -0.25f, -0.25f },
    {  0.25f, -0.25f },
    {  0.25f,  0.25f },
    { -0.25f,  0.25f },
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
    Output.textureUV = QuadUvToSource(tex);
    
    return Output;
}   // end of VS()

//
// 4X4 -> 1 4x4 Box Filter Pixel shader
//
float4
PS( VS_OUTPUT In ) : COLOR0
{
    float4 result = 0;

    for ( int i = 0; i < 4; i++ )
    {
        float2 tex = In.textureUV + Offsets[ i ].xy * PixelSize;
        result += tex2D( SourceTextureSampler, tex );
    }

    return result * 0.25f;
}   // end of PS()

//
// 4x4 Box Filter
//
technique Box4x4Blur
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
        SrcBlend = One;
        DestBlend = Zero;

        CullMode = None;

        ZEnable = false;
        ZFunc = Less;
        ZWriteEnable = false;
    }
}

