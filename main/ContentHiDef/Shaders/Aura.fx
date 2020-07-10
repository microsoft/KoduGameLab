// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

////////////////////////////////////////////////////////////////////
// Bloom pass
////////////////////////////////////////////////////////////////////

//
// Vertex shader output structure for bloom pass
//
struct VS_OUTPUT_AURA
{
    float4 position     : POSITION;     // vertex position
    float4 color		: COLOR0;
};

// Transform our coordinates into world space
VS_OUTPUT_AURA AuraVS(
                                float4 position : POSITION,
                                float3 normal   : NORMAL)
{
    VS_OUTPUT_AURA Output;

    Output.position = mul( position, mul(PreWorld, WorldViewProjMatrix) );
    
    normal = normalize(mul(normal, mul(PreWorld, WorldMatrix)));

    Output.color.a = dot(normal, CameraDir);
    
    Output.color.rgb = BloomColor.rgb * Output.color.a;

    return Output;
}

VS_OUTPUT_AURA AuraSimpleVS(
                                float4 position : POSITION,
                                float3 normal   : NORMAL,
                                float2 tex      : TEXCOORD0 )   // ignored
{
    VS_OUTPUT_AURA Output;

    position = PrepPosition(position);
    normal = PrepNormal(normal);
    
    return AuraVS(position, normal);
}

VS_OUTPUT_AURA AuraWithFlexVS(
                                float4 position : POSITION,
                                float3 normal   : NORMAL,       // ignored
                                float2 tex      : TEXCOORD0 )   // ignored
{
    VS_OUTPUT_AURA Output;

    position = PrepPosition(position);
    normal = PrepNormal(normal);

	float3 pos = ApplyFlex(position, normal);
	
	return AuraVS(position, normal);
}

VS_OUTPUT_AURA AuraWithSkinningVS(in SKIN_VS_INPUT input)
{
    VS_OUTPUT_AURA Output;
    
    input.position = PrepPosition(input.position);
    input.normal = PrepNormal(input.normal);
    SKIN_OUTPUT skin = Skin4(input);
    
    return AuraVS(skin.position, skin.normal);
}

VS_OUTPUT_AURA AuraWithWindVS(in SKIN_VS_INPUT input)
{
    VS_OUTPUT_AURA Output;
    
    input.position = PrepPosition(input.position);
    input.normal = PrepNormal(input.normal);
    
    SKIN_OUTPUT skin = Skin8(input);
    
    return AuraVS(skin.position, skin.normal);
}

//
// Pixel shader for bloom pass.
//
float4 AuraPS( VS_OUTPUT_AURA In ) : COLOR0
{
//return float4(1.0f, 1.0f, 0.0f, 1.0f);
	return In.color;
}

technique Aura
{
    pass P0
    {
        VertexShader = compile vs_2_0 AuraSimpleVS();
        PixelShader  = compile ps_2_0 AuraPS();

        /* // Alpha test
        AlphaRef = 1;
        AlphaTestEnable = true;
        AlphaFunc = GreaterEqual; */

        // Alpha blending
        AlphaBlendEnable = true;
        SrcBlend = One;
        DestBlend = One;

        CullMode = CW;

        ZEnable = true;
        ZFunc = LessEqual;
        ZWriteEnable = false;
    }
}

technique AuraWithFlex
{
    pass P0
    {
        VertexShader = compile vs_2_0 AuraWithFlexVS();
        PixelShader  = compile ps_2_0 AuraPS();

        /* // Alpha test
        AlphaRef = 1;
        AlphaTestEnable = true;
        AlphaFunc = GreaterEqual; */

        // Alpha blending
        AlphaBlendEnable = true;
        SrcBlend = One;
        DestBlend = One;

        CullMode = CW;

        ZEnable = true;
        ZFunc = LessEqual;
        ZWriteEnable = false;
    }
}

technique AuraCloud
{
    pass P0
    {
        VertexShader = compile vs_2_0 AuraWithSkinningVS();
        PixelShader  = compile ps_2_0 AuraPS();

        /* // Alpha test
        AlphaRef = 1;
        AlphaTestEnable = true;
        AlphaFunc = GreaterEqual; */

        // Alpha blending
        AlphaBlendEnable = true;
        SrcBlend = One;
        DestBlend = One;

        CullMode = CW;

        ZEnable = true;
        ZFunc = LessEqual;
        ZWriteEnable = false;
    }
}

technique AuraWithSkinning
{
    pass P0
    {
        VertexShader = compile vs_2_0 AuraWithSkinningVS();
        PixelShader  = compile ps_2_0 AuraPS();

        /* // Alpha test
        AlphaRef = 1;
        AlphaTestEnable = true;
        AlphaFunc = GreaterEqual; */

        // Alpha blending
        AlphaBlendEnable = true;
        SrcBlend = One;
        DestBlend = One;

        CullMode = CW;

        ZEnable = true;
        ZFunc = LessEqual;
        ZWriteEnable = false;
    }
}

technique AuraBokuFaceWithSkinning
{
    pass P0
    {
        VertexShader = compile vs_2_0 AuraWithSkinningVS();
        PixelShader  = compile ps_2_0 AuraPS();

        /* // Alpha test
        AlphaRef = 1;
        AlphaTestEnable = true;
        AlphaFunc = GreaterEqual; */

        // Alpha blending
        AlphaBlendEnable = true;
        SrcBlend = One;
        DestBlend = One;

        CullMode = CW;

        ZEnable = true;
        ZFunc = LessEqual;
        ZWriteEnable = false;
    }
}

technique AuraWideFaceWithSkinning
{
    pass P0
    {
        VertexShader = compile vs_2_0 AuraWithSkinningVS();
        PixelShader  = compile ps_2_0 AuraPS();

        /* // Alpha test
        AlphaRef = 1;
        AlphaTestEnable = true;
        AlphaFunc = GreaterEqual; */

        // Alpha blending
        AlphaBlendEnable = true;
        SrcBlend = One;
        DestBlend = One;

        CullMode = CW;

        ZEnable = true;
        ZFunc = LessEqual;
        ZWriteEnable = false;
    }
}

technique AuraTwoFaceWithSkinning
{
    pass P0
    {
        VertexShader = compile vs_2_0 AuraWithSkinningVS();
        PixelShader  = compile ps_2_0 AuraPS();

        /* // Alpha test
        AlphaRef = 1;
        AlphaTestEnable = true;
        AlphaFunc = GreaterEqual; */

        // Alpha blending
        AlphaBlendEnable = true;
        SrcBlend = One;
        DestBlend = One;

        CullMode = CW;

        ZEnable = true;
        ZFunc = LessEqual;
        ZWriteEnable = false;
    }
}

technique AuraWithWind
{
    pass P0
    {
        VertexShader = compile vs_2_0 AuraWithWindVS();
        PixelShader  = compile ps_2_0 AuraPS();

        /* // Alpha test
        AlphaRef = 1;
        AlphaTestEnable = true;
        AlphaFunc = GreaterEqual; */

        // Alpha blending
        AlphaBlendEnable = true;
        SrcBlend = One;
        DestBlend = One;

        CullMode = CW;

        ZEnable = true;
        ZFunc = LessEqual;
        ZWriteEnable = false;
    }
}

