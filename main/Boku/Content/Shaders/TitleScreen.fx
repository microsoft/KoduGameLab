//
// TitleScreen shaders
//

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
};

//
// Vertex Shader
//
VS_OUTPUT VS(
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

    return Output;
}


//
// Pixel shader
//
float4 PS( VS_OUTPUT In ) : COLOR0
{
    // Sample the texture.
    float4 diffuseColor = tex2D( DiffuseTextureSampler, In.textureUV );

    return diffuseColor;

}   // end of PS()


//
// Default technique
//
technique Default
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
        ZFunc = LessEqual;
        ZWriteEnable = false;
    }
}

