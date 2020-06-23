//
// Billboard shader
//
#include "Globals.fx"
#include "Fog.fx"

//
// Shared Globals.
//

//
// Locals.
//

// The world view and projection matrices
float4x4 WorldViewProjMatrix;
float4x4 WorldMatrix;

// Textures
texture DiffuseTexture;

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

//
// Vertex shader output structure
//
struct VS_OUTPUT
{
    float4 position     : POSITION;      // vertex position
    float2 textureUV    : TEXCOORD0;     // vertex texture coords
    float  fog              : TEXCOORD4;    // fog strength in x
};

//
// Color Pass Vertex Shader
//
VS_OUTPUT ColorVS(
            float3 position : POSITION,
            float2 tex      : TEXCOORD0 )
{
    VS_OUTPUT   Output;

    // Transform our position.
    float4 pos;
    pos.xyz = position;
    pos.w = 1.0f;
    Output.position = mul( pos, WorldViewProjMatrix );

    Output.textureUV = tex;

    Output.fog = CalcFog( Output.position.w );

    return Output;
}


//
// Color Pass Pixel Shader
//
float4 ColorPS( VS_OUTPUT In ) : COLOR0
{
    // Sample the texture.
    float4 diffuseColor = tex2D( DiffuseTextureSampler, In.textureUV );

    diffuseColor.rgb = lerp(diffuseColor, FogColor, In.fog);

    return diffuseColor;

}   // end of PS()


//
// NormalAlphaColorPass technique
//
technique NormalAlphaColorPass
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
        SrcBlend = SrcAlpha;
        DestBlend = InvSrcAlpha;

        CullMode = None;

        ZEnable = true;
        ZFunc = LessEqual;
        ZWriteEnable = false;
    }
}

//
// NormalAlphaColorPassNoZ technique
//
technique NormalAlphaColorPassNoZ
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
        SrcBlend = SrcAlpha;
        DestBlend = InvSrcAlpha;

        CullMode = None;

        ZEnable = false;
        ZFunc = LessEqual;
        ZWriteEnable = false;
    }
}

//
// PremultipliedAlphaColorPass technique
//
technique PremultipliedAlphaColorPass
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
        DestBlend = InvSrcAlpha;

        CullMode = None;

        ZEnable = true;
        ZFunc = LessEqual;
        ZWriteEnable = true;
    }
}

//
// Vertex shader output structure
//
struct DEPTH_VS_OUTPUT
{
    float4 position         : POSITION;     // vertex position
    float4 color            : TEXCOORD0;    // depth values
};

// Transform our coordinates into world space
DEPTH_VS_OUTPUT DepthVS(
            float3 position : POSITION,
            float2 tex      : TEXCOORD0 )
{
    DEPTH_VS_OUTPUT   Output;

    // Transform our position.
    float4 pos;
    pos.xyz = position;
    pos.w = 1.0f;
    Output.position = mul( pos, WorldViewProjMatrix );

    Output.color = 0.0f;
    Output.color.r = 0.0f;  // No DOF blur for billboard

    return Output;
}

//
// Pixel shader
//
float4 DepthPS( DEPTH_VS_OUTPUT In ) : COLOR0
{
    float4 result = In.color;
    
    return result;
}

//
// DepthPass technique
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
        DestBlend = One;

        CullMode = None;

        ZEnable = true;
        ZFunc = LessEqual;
        ZWriteEnable = true;
    }
}


