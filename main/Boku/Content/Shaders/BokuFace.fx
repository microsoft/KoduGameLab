//
// BokuFace - Shader for compositing Boku's face.
//

//
// Variables
//
float4  PupilOffset;        // UV offset for pupils.
float4  PupilSize;          // X is scale factor for pupil size.
texture EyeShapeTexture;
texture EyePupilTexture;
texture EyeBrowTexture;

//
// Texture samplers
//
sampler2D EyeShapeTextureSampler =
sampler_state
{
    Texture = <EyeShapeTexture>;
    MipFilter = Linear;
    MinFilter = Linear;
    MagFilter = Linear;

    AddressU = Clamp;
    AddressV = Clamp;
};

sampler2D EyePupilTextureSampler =
sampler_state
{
    Texture = <EyePupilTexture>;
    MipFilter = Linear;
    MinFilter = Linear;
    MagFilter = Linear;

    AddressU = Clamp;
    AddressV = Clamp;
};

sampler2D EyeBrowTextureSampler =
sampler_state
{
    Texture = <EyeBrowTexture>;
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
    float2 pupilUV		: TEXCOORD1;	// pupil texture coords
};

//
// Vertex shader
//
VS_OUTPUT
VS( float2 pos : POSITION,
    float2 tex : TEXCOORD0 )
{
    VS_OUTPUT   Output;

    Output.position = float4( pos.x, pos.y, 1.0f, 1.0f );
    Output.textureUV = tex;
    Output.pupilUV = ( tex - float2(0.5f, 0.5f) + PupilOffset) / PupilSize.x + float2( 0.5f, 0.5f );

    return Output;
}   // end of VS()

//
// Pixel shader
//
float4
PS( VS_OUTPUT In ) : COLOR0
{
    float4 result;

    float4 shape = tex2D( EyeShapeTextureSampler, In.textureUV );
    float4 pupil = tex2D( EyePupilTextureSampler, In.pupilUV );
    float4 brow  = tex2D( EyeBrowTextureSampler, In.textureUV );

    result.rgb = shape.rgb - pupil.a;
    result.a = max( shape.a, brow.a );
    
    return result;
}   // end of PS()

//
// Composite the face elements together
//
technique BokuFace
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
        ZFunc = Less;
        ZWriteEnable = false;
    }
}

