// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

//
//  Camera Space Quad shaders
//

//
// Variables
//

// The world view and projection matrices
float4x4    WorldViewProjMatrix;
float4x4    WorldMatrix;

float4      DiffuseColor;
float		Alpha;

texture     DiffuseTexture;
texture     ShadowMaskTexture;

//
// Texture samplers
//
sampler2D DiffuseTextureSampler =
sampler_state
{
    Texture = <DiffuseTexture>;
    MipFilter = Linear;
    MinFilter = Linear;
    MagFilter = Linear;

    AddressU = Clamp;
    AddressV = Clamp;
};
sampler2D ShadowMaskTextureSampler =
sampler_state
{
    Texture = <ShadowMaskTexture>;
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

    float4 position = QuadUvToPos(tex, 0.0f);
    Output.position = mul( position, WorldViewProjMatrix );
    Output.textureUV = tex;

    return Output;
}   // end of VS()

//
// Pixel shaders
//
float4
TexturedPS( VS_OUTPUT In ) : COLOR0
{
    float4 result = tex2D( DiffuseTextureSampler, In.textureUV );
	result.a *= Alpha;
	result *= DiffuseColor;

    return result;
}   // end of TexturedPS()

//
//  TexturedMultAlphaPS -- Pixel shader used to allow additive blending to
//						   be faded out using the Alpha param.
//
float4
TexturedMultAlphaPS( VS_OUTPUT In ) : COLOR0
{
    float4 result = tex2D( DiffuseTextureSampler, In.textureUV );
	result.rgb *= Alpha;

    return result;
}   // end of TexturedMultAlphaPS()


//
// Drop Shadow -- This requires a shadow mask texture.  This texture should
//                have the "shape" of the object in white in the RGB channels
//                and a blurred version of this in the alpha channel for the 
//                shadow.
//
float4
DropShadowPS( VS_OUTPUT In ) : COLOR0
{
    float4 diffuse = tex2D( DiffuseTextureSampler, In.textureUV );
    float4 mask = tex2D( ShadowMaskTextureSampler, In.textureUV );
    
    float4 result;
    result.rgb = diffuse.rgb * mask.rgb;
    result.a = mask.a;

    return result;
}   // end of TexturedPS()

//
// SolidColorWithDrop Shadow -- This requires a shadow mask texture.  This texture should
//                              have the "shape" of the object in white in the RGB channels
//                              and a blurred version of this in the alpha channel for the 
//                              shadow.  The RGB channels are attenuated by the DiffuseColor
//
float4
SolidColorWithDropShadowPS( VS_OUTPUT In ) : COLOR0
{
    float4 mask = tex2D( ShadowMaskTextureSampler, In.textureUV );
    
    float4 result;
    result.rgb = DiffuseColor.rgb * mask.rgb;
    result.a = max(mask.r, mask.a);

    return result;
}   // end of TexturedPS()

//
// Stencil
//
float4
StencilPS( VS_OUTPUT In ) : COLOR0
{
    float4 result;
    result.rgba = DiffuseColor.rgba;

    return result;
}   // end of StencilPS()

//
// TexturedRegularAlpha
//
technique TexturedRegularAlpha
{
    pass P0
    {
        VertexShader = compile vs_2_0 VS();
        PixelShader  = compile ps_2_0 TexturedPS();

        /* // Alpha test
        AlphaRef = 1;
        AlphaTestEnable = true;
        AlphaFunc = GreaterEqual; */

        // Alpha blending
        AlphaBlendEnable = true;
        SrcBlend = SrcAlpha;
        DestBlend = InvSrcAlpha;

        CullMode = None;

        ZEnable = true;
        ZFunc = LessEqual;
        ZWriteEnable = false;
    }
}

//
// TexturedPreMultAlpha
//
technique TexturedPreMultAlpha
{
    pass P0
    {
        VertexShader = compile vs_2_0 VS();
        PixelShader  = compile ps_2_0 TexturedPS();

        /* // Alpha test
        AlphaRef = 1;
        AlphaTestEnable = true;
        AlphaFunc = GreaterEqual; */

        // Alpha blending
        AlphaBlendEnable = true;
        SrcBlend = One;
        DestBlend = InvSrcAlpha;

        CullMode = None;

        ZEnable = true;
        ZFunc = LessEqual;
        ZWriteEnable = false;
    }
}

//
// AdditiveBlend
//
technique AdditiveBlend
{
    pass P0
    {
        VertexShader = compile vs_2_0 VS();
        PixelShader  = compile ps_2_0 TexturedMultAlphaPS();

        /* // Alpha test
        AlphaRef = 1;
        AlphaTestEnable = true;
        AlphaFunc = GreaterEqual; */

        // Alpha blending
        AlphaBlendEnable = true;
        SrcBlend = One;
        DestBlend = One;

        CullMode = None;

        ZEnable = false;
        ZFunc = LessEqual;
        ZWriteEnable = false;
    }
}

//
// DropShadow
//
technique DropShadow
{
    pass P0
    {
        VertexShader = compile vs_2_0 VS();
        PixelShader  = compile ps_2_0 DropShadowPS();

        /* // Alpha test
        AlphaRef = 1;
        AlphaTestEnable = true;
        AlphaFunc = GreaterEqual; */

        // Alpha blending
        AlphaBlendEnable = true;
        SrcBlend = SrcAlpha;
        DestBlend = InvSrcAlpha;

        CullMode = None;

        ZEnable = true;
        ZFunc = LessEqual;
        ZWriteEnable = false;
    }
}

//
// SolidColorWithDropShadow
//
technique SolidColorWithDropShadow
{
    pass P0
    {
        VertexShader = compile vs_2_0 VS();
        PixelShader  = compile ps_2_0 SolidColorWithDropShadowPS();

        /* // Alpha test
        AlphaRef = 1;
        AlphaTestEnable = true;
        AlphaFunc = GreaterEqual; */

        // Alpha blending
        AlphaBlendEnable = true;
        SrcBlend = SrcAlpha;
        DestBlend = InvSrcAlpha;

        CullMode = None;

        ZEnable = true;
        ZFunc = LessEqual;
        ZWriteEnable = false;
    }
}

//
// Stencil
//
technique Stencil
{
    pass P0
    {
        VertexShader = compile vs_2_0 VS();
        PixelShader  = compile ps_2_0 StencilPS();

        /* // Alpha test
        AlphaRef = 1;
        AlphaTestEnable = false;
        AlphaFunc = GreaterEqual; */

        // Alpha blending
        AlphaBlendEnable = true;
        SrcBlend = Zero;
        DestBlend = One;

        CullMode = None;
        
        StencilEnable = true;
        StencilFunc = Always;
        StencilPass = Replace;
        StencilRef = 1;


        ZEnable = true;
        ZFunc = LessEqual;
        ZWriteEnable = false;
    }
}
