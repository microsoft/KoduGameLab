// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.


texture Mask;
texture BumpTexture;

float4 PosToUV;
float4 BumpTint;

float4 BumpScroll = 0.0f;
float4 BumpScale = 1.0f;


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

sampler2D BumpSampler =
sampler_state
{
    Texture = <BumpTexture>;
    MipFilter = Linear;
    MinFilter = Linear;
    MagFilter = Linear;

    AddressU = WRAP;
    AddressV = WRAP;
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

struct PS_OUTPUT
{
	float4	rt0	: COLOR0;
	float4	rt1 : COLOR1;
};


PS_INPUT ColorVS(VS_INPUT input)
{
    PS_INPUT output;

    output.position.xy = input.position.xy;
    output.position.z = 0.0f;
    output.position.w = 1.0f;

	output.texcoord = input.position;

    return output;
}

PS_OUTPUT ColorPS(PS_INPUT input)
{
	PS_OUTPUT output;
	
	float len = length(input.texcoord);
	input.texcoord *= len;
	input.texcoord = input.texcoord * PosToUV.xz + PosToUV.yw;

	float4 color = tex2D(MaskSampler, input.texcoord);

	color.a *= BumpTint.a;

	output.rt0.a = color.a;

	float4 bumpNorm0 = tex2D( BumpSampler, input.texcoord.xy * BumpScale.xy + BumpScroll.xy ) * 2.0f - 1.0f; 
	float4 bumpNorm1 = tex2D( BumpSampler, input.texcoord.xy * BumpScale.zw + BumpScroll.zw ) * 2.0f - 1.0f;

	float4 bumpNorm;
	bumpNorm.rgb = normalize(bumpNorm0.rgb + bumpNorm1.rgb) * color.a;

	bumpNorm.a = max(bumpNorm0.a, bumpNorm1.a) * color.a;

	output.rt0.rgb = saturate(bumpNorm.aaa * BumpTint.rgb);

	output.rt1 = saturate(float4(-bumpNorm.x, -bumpNorm.y, bumpNorm.x, bumpNorm.y));

	return output;
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
        DestBlend = One;

        CullMode = None;

        ZEnable = false;
        ZFunc = Less;
        ZWriteEnable = false;
    }
}
