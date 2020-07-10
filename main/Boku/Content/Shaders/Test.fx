// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

//
// Test shaders -- Just a grab bag of stuff to do quick testing with.
//

//
// Shared Globals.
//
// Light 0 is the key light, which shadows are based off of.
shared float4   LightDirection0;  // Direction light is travelling.
shared float4   LightColor0;

shared float4   EyeLocation;

shared texture  EnvironmentMap;

//
// Locals.
//

// The world view and projection matrices
float4x4 WorldViewProjMatrix;
float4x4 WorldMatrix;

int     NumShadows;
float4  Shadow[16];

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
    float4 position     : POSITION;     // vertex position
    float3 pos          : TEXCOORD0;    // in world coords
    float2 textureUV    : TEXCOORD1;    // vertex texture coords
};

//
// Vertex Shader
//
VS_OUTPUT PosTex_VS(
            float3 position : POSITION,
            float2 tex      : TEXCOORD0 )
{
    VS_OUTPUT   Output;

    // Transform our position.
    float4 pos;
    pos.xyz = position;
    pos.w = 1.0f;
    Output.position = mul( pos, WorldViewProjMatrix );

    Output.pos = position;
    
    Output.textureUV = tex;

    return Output;
}

float SmoothStep(float a, float b, float t)
{
    if (t <= a)
    {
        return 0.0f;
    }
    else if (t >= b)
    {
        return 1.0f;
    }
    else
    {
        t = (t - a) / (b - a);
        return -2.0f * t * t * t + 3.0f * t * t;
    }
}   // SmoothStep()


float BlobShadow(float3 position, float4 shadow)
{
    float attenuation = 1.0f;
    
    float dz = position.z - shadow.z;
    float d = dz / LightDirection0.z;
    
    // Calculate the projected position of the blob
    // based on the height and light direction.
    float3 projectedPosition = shadow.xyz + LightDirection0.xyz * d;
    
    float dx = projectedPosition.x - position.x;
    float dy = projectedPosition.y - position.y;
    
    float dist = sqrt( dx * dx + dy * dy );
    
    attenuation = SmoothStep(shadow.w - d, shadow.w + d, dist);
    
    return attenuation;
}

//
// Pixel shader
//
float4 PosTex_PS( VS_OUTPUT In ) : COLOR0
{
    // Sample the texture.
    float4 diffuseColor = tex2D( DiffuseTextureSampler, In.textureUV );

    float attenuation = 1.0f;    
    
    for ( int i = 0; i < NumShadows; i++ )
    {
        attenuation = min( attenuation, BlobShadow( In.pos, Shadow[ i ] ) );
    }

    return diffuseColor * attenuation;

}   // end of PosTex_PS()


//
// Position and texture.  No lighting.
//
technique PosTex
{
    pass P0
    {
        VertexShader = compile vs_3_0 PosTex_VS();
        PixelShader  = compile ps_3_0 PosTex_PS();

        // Alpha test
        AlphaRef = 1;
        AlphaTestEnable = false;
        AlphaFunc = GreaterEqual;

        // Alpha blending
        AlphaBlendEnable = false;
        SrcBlend = SrcAlpha;
        DestBlend = InvSrcAlpha;

        CullMode = None;

        ZEnable = true;
        ZFunc = LessEqual;
        ZWriteEnable = true;
    }
}

