// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.


//
// Depth of Field
//

#include "Globals.fx"

//
// Variables
//
texture FullTexture;
texture BlurTexture;
texture BloomTexture;
texture GlowTexture;
texture DepthTexture;     // R channel is amount of DOF needed.
texture DistortTexture0;
texture DistortTexture1;

float2  PixelSize;
float4  FullOffset;
float2  ScreenScale;	// Adjust for tutorial mode.

float2 DepthSampleOffset;
float2 DepthSampleScale;

float2  DOF_MinBlur = float2(0.0f, 1.0f);

//
// Texture samplers
//
sampler2D FullTextureSampler =
sampler_state
{
    Texture = <FullTexture>;
    MipFilter = Linear;
    MinFilter = Linear;
    MagFilter = Linear;

    AddressU = Clamp;
    AddressV = Clamp;
};

sampler2D BlurTextureSampler =
sampler_state
{
    Texture = <BlurTexture>;
    MipFilter = Linear;
    MinFilter = Linear;
    MagFilter = Linear;

    AddressU = Clamp;
    AddressV = Clamp;
};

sampler2D DistortTextureSampler0 =
sampler_state
{
    Texture = <DistortTexture0>;
    MipFilter = Linear;
    MinFilter = Linear;
    MagFilter = Linear;

    AddressU = Clamp;
    AddressV = Clamp;
};

sampler2D DistortTextureSampler1 =
sampler_state
{
    Texture = <DistortTexture1>;
    MipFilter = Linear;
    MinFilter = Linear;
    MagFilter = Linear;

    AddressU = Clamp;
    AddressV = Clamp;
};

sampler2D BloomTextureSampler =
sampler_state
{
    Texture = <BloomTexture>;
    MipFilter = Linear;
    MinFilter = Linear;
    MagFilter = Linear;

    AddressU = Clamp;
    AddressV = Clamp;
};

sampler2D GlowTextureSampler =
sampler_state
{
    Texture = <GlowTexture>;
    MipFilter = Linear;
    MinFilter = Linear;
    MagFilter = Linear;

    AddressU = Clamp;
    AddressV = Clamp;
};

sampler2D DepthTextureSampler =
sampler_state
{
    Texture = <DepthTexture>;
    MipFilter = Linear;
    MinFilter = Linear;
    MagFilter = Linear;

    AddressU = Clamp;
    AddressV = Clamp;
};

//
// Pixel shader function for lookup of distortion.
// Input UV's are modified (for refraction).
// Return value is:
//		Reflected color as .rgb
//		Additional blur as .a
//
float4 Distort(inout float2 uv)
{
	float4 distort0 = tex2D( DistortTextureSampler0, uv );

	float4 distort1 = tex2D( DistortTextureSampler1, uv );

	uv += -distort1.xy + distort1.zw;
	
	return distort0;
}

//
// Vertex shader output structure
//
struct VS_OUTPUT
{
    float4 position     : POSITION;     // vertex position
    float2 textureUV    : TEXCOORD0;    // vertex texture coords, 
	float2 textureUVFull : TEXCOORD1;	// vertex texture, full width image
};

#include "QuadUvToPos.fx"

//
// Vertex shader
//
VS_OUTPUT
VS( float2 tex : TEXCOORD0 )
{
    VS_OUTPUT   Output;

    Output.position = QuadUvToPos(tex, 0.0f);
    Output.textureUV = tex;
	Output.textureUVFull = ScreenScale * tex;

    return Output;
}   // end of VS()

float4 SampleFull(float2 uv)
{
	return (tex2D( FullTextureSampler, uv + FullOffset.xy)
			+ tex2D( FullTextureSampler, uv + FullOffset.xw)
			+ tex2D( FullTextureSampler, uv + FullOffset.zy)
			+ tex2D( FullTextureSampler, uv + FullOffset.zw)) * 0.25f;
}

//
// Pixel shader
//
float4
SingleSamplePS( VS_OUTPUT In ) : COLOR0
{
    float4 result;
    
    float4 full = SampleFull(In.textureUV);
    
    float4 blur = tex2D( BlurTextureSampler, In.textureUV );
    float4 depth = tex2D( DepthTextureSampler, In.textureUV );
    depth.r = saturate(depth.r * DOF_MinBlur.y + DOF_MinBlur.x);
    
    result = lerp( full, blur, depth.r );   
        
    result.a = 1.0f;
    
    return result;
    
}   // end of SingleSamplePS()

//
// Pixel shader
//
float4
DistortSingleSamplePS( VS_OUTPUT In ) : COLOR0
{
    float4 result;
    
	float4 distort0 = Distort(In.textureUV);
//return distort0;

    float4 full = SampleFull(In.textureUV);
    
    float4 blur = tex2D( BlurTextureSampler, In.textureUV );
    float4 depth = tex2D( DepthTextureSampler, In.textureUV );
    depth.r = saturate(depth.r * DOF_MinBlur.y + DOF_MinBlur.x);
    
    result = lerp( full, blur, saturate(depth.r + distort0.a) );   
        
    result.rgb += distort0.rgb;
//result.rgb = lerp(result.rgb, distort0.rgb, distort0.aaa);
    
    result.a = 1.0f;
    
    return result;
    
}   // end of DistortSingleSamplePS()

//
// Pixel shader
//
float4
BloomDistortSingleSamplePS( VS_OUTPUT In ) : COLOR0
{
    float4 result;
    
	float4 distort0 = Distort(In.textureUVFull);

    float4 full = SampleFull(In.textureUVFull);

    float4 blur = tex2D( BlurTextureSampler, In.textureUVFull );

	// Need to fudge the depth sampling to get ti to line up with the glow texture.
	float2 uv = In.textureUVFull;
	uv = ( uv  + DepthSampleOffset ) * DepthSampleScale;
    float4 depth = tex2D( DepthTextureSampler, uv );

    depth.r = saturate(depth.r * DOF_MinBlur.y + DOF_MinBlur.x);
    
    result = lerp( full, blur, saturate(depth.r + distort0.a) );   
        
    // Add on bloom and glow.
    //float4 bloom = tex2D( BloomTextureSampler, In.textureUVFull );

    float4 glow = tex2D( GlowTextureSampler, In.textureUVFull );
    glow *= 1.0f - depth.b * blur.a;
    
	//result += (bloom + glow) * BloomStrength;
	result += glow;
    
    result.rgb += distort0.rgb;
	//result.rgb = lerp(result.rgb, distort0.rgb, distort0.aaa);
    
    result.a = 1.0f;
    
    return result;
    
}   // end of BloomDistortSingleSamplePS()



//
// Do depth of field compositing along with blur.
// Single sample version.
//
technique DOF_Composite_Single_Sample
{
    pass P0
    {
        VertexShader = compile vs_2_0 VS();
        PixelShader  = compile ps_2_0 SingleSamplePS();

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
        ZFunc = Less;
        ZWriteEnable = false;
    }
}

//
// Do depth of field compositing along with blur.
// Single sample version. Once again with No Bloom
//
technique DOF_CompositeDistort_Single_Sample
{
    pass P0
    {
        VertexShader = compile vs_2_0 VS();
        PixelShader  = compile ps_2_0 DistortSingleSamplePS();

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
        ZFunc = Less;
        ZWriteEnable = false;
    }
}

//
// Do depth of field compositing along with blur.
// Single sample version. Once again with No Bloom
//
technique DOF_CompositeBloomDistort_Single_Sample
{
    pass P0
    {
        VertexShader = compile vs_2_0 VS();
        PixelShader  = compile ps_2_0 BloomDistortSingleSamplePS();

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
        ZFunc = Less;
        ZWriteEnable = false;
    }
}

