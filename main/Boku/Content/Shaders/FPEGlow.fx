
texture Mask;
float4 PosToUV;
float4 GlowColor;

sampler2D MaskSampler =
sampler_state
{
    Texture = <Mask>;
    MipFilter = Linear;
    MinFilter = Linear;
    MagFilter = Linear;

    AddressU = Clamp;
    AddressV = Clamp;
};


struct VS_INPUT
{
    float2 position : POSITION0;

};

struct PS_INPUT
{
    float4 position : POSITION0;
	float2 texcoord : TEXCOORD0;
};

PS_INPUT ColorVS(VS_INPUT input)
{
    PS_INPUT output;

    output.position.xy = input.position.xy;
    output.position.z = 0.0f;
    output.position.w = 1.0f;

	output.texcoord = input.position * PosToUV.xz + PosToUV.yw;

    return output;
}

float4 ColorPS(PS_INPUT input) : COLOR0
{
	float4 color = tex2D(MaskSampler, input.texcoord);

	color.a *= GlowColor.a;
	color.rgb *= GlowColor * color.a;
	
	return color;
}

technique Technique1
{
    pass Pass1
    {

        VertexShader = compile vs_2_0 ColorVS();
        PixelShader = compile ps_2_0 ColorPS();

        /* // Alpha test
        AlphaRef = 1;
        AlphaTestEnable = false;
        AlphaFunc = GreaterEqual; */

        // Alpha blending
        AlphaBlendEnable = true;
        SrcBlend = One;
        DestBlend = InvSrcAlpha;

        CullMode = None;

        ZEnable = false;
        ZFunc = Less;
        ZWriteEnable = false;
    }
}
