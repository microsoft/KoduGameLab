// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.


//
// TopLevelPalette
//

//
// Variables
//

// Tint colors for up to 5 icons.
float4 Color0;
float4 Color1;
float4 Color2;
float4 Color3;
float4 Color4;

float Alpha;
float NumIcons;

texture FrameTexture;
texture ScreenTexture;
texture IconTexture;

//
// Texture samplers
//
sampler2D FrameTextureSampler =
sampler_state
{
    Texture = <FrameTexture>;
    MipFilter = Linear;
    MinFilter = Linear;
    MagFilter = Linear;

    AddressU = Clamp;
    AddressV = Clamp;
};
sampler2D ScreenTextureSampler =
sampler_state
{
    Texture = <ScreenTexture>;
    MipFilter = Linear;
    MinFilter = Linear;
    MagFilter = Linear;

    AddressU = Clamp;
    AddressV = Clamp;
};
sampler2D IconTextureSampler =
sampler_state
{
    Texture = <IconTexture>;
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
    float4 position : POSITION;     // vertex position
    float2 frameUV  : TEXCOORD0;    // frame texture coords
    float2 screenUV : TEXCOORD1;    // screen texture coords
    float2 iconUV   : TEXCOORD2;    // icon texture coords
};

//
// Vertex shader
//
VS_OUTPUT
VS( float2 pos          : POSITION0,
    float2 frameTex     : TEXCOORD0,
    float2 screenTex    : TEXCOORD1,
    float2 iconTex      : TEXCOORD2)
{
    VS_OUTPUT   Output;

    Output.position = float4( pos.x, pos.y, 0.0f, 1.0f );
    Output.frameUV = frameTex;
    Output.screenUV = screenTex;
    Output.iconUV = iconTex;

    return Output;
}   // end of VS()

//
// Pixel shaders
//
float4
PS( VS_OUTPUT In ) : COLOR0
{
    float4 frame = tex2D( FrameTextureSampler, In.frameUV );
    float4 screen = tex2D( ScreenTextureSampler, In.screenUV );
    float4 icon = tex2D( IconTextureSampler, In.iconUV );

    // Add selected/unselected color to icon.
    float width = 1.0f / NumIcons;
    if ( In.iconUV.x < width )
    {
        icon *= Color0;
    } 
    else if ( In.iconUV.x < width * 2 )
    {
        icon *= Color1;
    }
    else if ( In.iconUV.x < width * 3 )
    {
        icon *= Color2;
    }
    else if ( In.iconUV.x < width * 4 )
    {
        icon *= Color3;
    }
    else
    {
		icon *= Color4;
    }
    
    float4 result;
    result = lerp( screen, icon, icon.a );
    result = lerp( frame, result, screen.a );

    result.a = frame.a * Alpha;
    
    return result;
}   // end of TexturedPS()


//
// Normal
//
technique Normal
{
    pass P0
    {
        VertexShader = compile vs_2_0 VS();
        PixelShader  = compile ps_2_0 PS();

        // Alpha test
        AlphaRef = 1;
        AlphaTestEnable = true;
        AlphaFunc = GreaterEqual;

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
