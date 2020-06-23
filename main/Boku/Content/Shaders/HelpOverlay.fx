
//
//  Help Overlay shader
//

//
// Variables
//

float   Alpha;      // Used to fade overlays.
texture Texture;

//
// Texture sampler
//
sampler2D TextureSampler =
sampler_state
{
    Texture = <Texture>;
    MipFilter = None;       // Help overlay textures should be 1-to-1 with pixels on the
    MinFilter = Point;      // screen so no filtering should be needed.
    MagFilter = Point;

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

//
// Vertex shader
//
VS_OUTPUT
VS( float2 pos : POSITION0,
    float2 tex : TEXCOORD0 )
{
    VS_OUTPUT   Output;

    Output.position = float4( pos.x, pos.y, 0.0f, 1.0f );
    Output.textureUV = tex;

    return Output;
}   // end of VS()

//
// Pixel shader
//
float4
PS( VS_OUTPUT In ) : COLOR0
{
    float4 result = tex2D( TextureSampler, In.textureUV );
    result.a *= Alpha;

    return result;
}   // end of PS()

//
// Technique
//
technique Overlay
{
    pass P0
    {
        VertexShader = compile vs_2_0 VS();
        PixelShader  = compile ps_2_0 PS();

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


