//
//  Edit -- Used for stamping one texture into another.
//

//
// Variables
//
texture SourceTexture;
float4  Color;  // Color with 1's in channels to paint to, 0's otherwise.
float   Alpha;

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

    /*    
    // We need to adjust the brush texture coordinates so
    // that the brush is positioned and scaled correctly on
    // the rendertarget.
    
    // Offset
    tex += Offset - float2(0.5f, 0.5f);
    
    // Scale
    tex -= float2(0.5f, 0.5f);
    tex *= Scale;
    tex += float2(0.5f, 0.5f);
    */
    
    Output.textureUV = tex;
    
    return Output;
}   // end of VS()

//
// PS
//
float4
PS( VS_OUTPUT In ) : COLOR0
{
    float4 brush = tex2D( SourceTextureSampler, In.textureUV );

    float4 result = Color.rgba * brush.r * Alpha;

    return result;
}   // end of PS()

//
// Techniques
//
technique Edit
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
        SrcBlend = One; // SrcAlpha;
        DestBlend = InvSrcAlpha;

        CullMode = None;

        ZEnable = false;
        ZFunc = Less;
        ZWriteEnable = false;
    }
}

