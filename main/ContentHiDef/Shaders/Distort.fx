
#ifndef DISTORT_H
#define DISTORT_H

#include "PrepXform.fx"

texture BumpTexture;
texture DepthTexture;     // R channel is amount of DOF needed. G is W / DOF_FarPlane

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

float4 BumpScroll = 0.0f;
float4 BumpScale = 1.0f;
float BumpStrength = 1.0f;
float4 BumpTransU = 0.0f;
float4 BumpTransV = 0.0f;
float4 BumpTint = float4(0.0f, 1.0f, 1.0f, 1.0f);

// Distortion parameters
float4x4	PreWorld = float4x4(1.f, 0.f, 0.f, 0.f,
								0.f, 1.f, 0.f, 0.f,
								0.f, 0.f, 1.f, 0.f,
								0.f, 0.f, 0.f, 1.f); // Override transform pre-multiplied to local to world
// Opacity.[offsetscale, 0.f, glint scale, blurscale].
float4		Opacity = 1.f;

#include "Flex.fx"
#include "Skin.fx"

struct DISTORT_VS_OUTPUT
{
	float4		position : POSITION;
	float4		worldPosition	: TEXCOORD0;
	float4		worldNormal		: TEXCOORD1;
	float4		viewNormal		: TEXCOORD2;
	float4		screenPos		: TEXCOORD3;
	float2		bumpUV			: TEXCOORD4;
};

// Transform our coordinates into world space
DISTORT_VS_OUTPUT DistortVS(
                        float3 localPos		: POSITION,
                        float3 localNorm	: NORMAL)
{
    DISTORT_VS_OUTPUT   Output;

    // Transform our position.
    Output.position = mul( float4(localPos, 1.f), mul( PreWorld, WorldViewProjMatrix ) );
    
    // Record it in a useful form
    Output.screenPos = Output.position;
    Output.screenPos.xy /= Output.position.w;
    Output.screenPos = Output.screenPos * float4(0.5f, -0.5f, 1.f, 1.f / DOF_FarPlane) 
										+ float4(0.5f, 0.5f, 0.f, 0.f);

    // Transform the normals into world coordinates and normalize.
    float4x4 NewWorld = mul(PreWorld, WorldMatrix);
    // Don't bother normalizing, we'll need to normalize in pixel shader anyway.
    Output.worldNormal = mul( localNorm.xyz, NewWorld );
    // We'll assume (hah!) that WorldToCamera is a well behaved camera transform,
    // i.e. no scale (or at least uniform scale)
    Output.viewNormal = mul( Output.worldNormal, WorldToCamera );

    // Transform the position into world coordinates for calculating the eye vector.
    float4 worldPosition = mul( float4(localPos, 1.f), NewWorld );
    Output.worldPosition = worldPosition;
    
    Output.bumpUV.x = dot(worldPosition.xyzw, BumpTransU.xyzw);
    Output.bumpUV.y = dot(worldPosition.xyzw, BumpTransV.xyzw);

    return Output;
}

DISTORT_VS_OUTPUT DistortVSSimple(
                        float3 localPos		: POSITION,
                        float3 localNorm	: NORMAL)
{
    localPos = PrepPosition(localPos);
    localNorm = PrepNormal(localNorm);

    return DistortVS(localPos, localNorm);
}

// Transform our coordinates into world space
DISTORT_VS_OUTPUT DistortVSFlex(
                        float3 localPos		: POSITION,
                        float3 localNorm	: NORMAL)
{
    localPos = PrepPosition(localPos);
    localNorm = PrepNormal(localNorm);

    float3 pos = ApplyFlex(localPos, localNorm);
	
	return DistortVS(pos, localNorm);
}

DISTORT_VS_OUTPUT DistortVSSkinning(in SKIN_VS_INPUT input)
{
    input.position = PrepPosition(input.position);
    input.normal = PrepNormal(input.normal);
    SKIN_OUTPUT skin = Skin4(input);
    
    return DistortVS(skin.position, skin.normal);
}

DISTORT_VS_OUTPUT DistortVSWind(in SKIN_VS_INPUT input)
{
    input.position = PrepPosition(input.position);
    input.normal = PrepNormal(input.normal);
    SKIN_OUTPUT skin = Skin8(input);
    
    return DistortVS(skin.position, skin.normal);
}

struct DISTORT_PS_OUTPUT
{
	float4	rt0	: COLOR0;
	float4	rt1 : COLOR1;
};

DISTORT_PS_OUTPUT DistortPS(DISTORT_VS_OUTPUT In) : COLOR0
{
	DISTORT_PS_OUTPUT Output;
	
    float4 depthTex = tex2D( DepthTextureSampler, In.screenPos.xy );
    float depth = depthTex.g - In.screenPos.w;
	depth = depth > 0.0f ? saturate(depth + 0.25) : 0.0f;  
//	float depth = depthTex.g >= In.screenPos.w ? 1.0f : 0.0f;

	float3 worldNorm = normalize(In.worldNormal.xyz);
	
	float3 viewNorm = normalize(In.viewNormal.xyz);

	// rt0.a is blurriness
	Output.rt0.a = smoothstep(0.f, 1.f, 1.f - viewNorm.z * viewNorm.z) * Opacity.w;

//Output.rt0 = tex2D( BumpSampler, In.bumpUV.xy * BumpScale.xy + BumpScroll.xy );
//Output.rt1 = 0.0f;
//return Output;

	float4 bumpNorm0 = tex2D( BumpSampler, In.bumpUV.xy * BumpScale.xy + BumpScroll.xy ) * 2.0f - 1.0f; 
	float4 bumpNorm1 = tex2D( BumpSampler, In.bumpUV.xy * BumpScale.xy + BumpScroll.zw ) * 2.0f - 1.0f;

	float4 bumpNorm;
	bumpNorm.rgb = normalize(bumpNorm0.rgb + bumpNorm1.rgb) * BumpStrength;

	bumpNorm.a = max(bumpNorm0.a, bumpNorm1.a);
	bumpNorm.a = depthTex.g > In.screenPos.w ? bumpNorm.a : 0.f;
//bumpNorm.a = bumpNorm0.a * bumpNorm1.a;

	Output.rt0.rgb = saturate(1.f - 0.75f * viewNorm.zzz) * saturate(bumpNorm.aaa * BumpTint.rgb) * Opacity.zzz;
	
	viewNorm += bumpNorm.rgb;
	viewNorm = normalize(viewNorm);

	// TODO, combine glint and env map into rt0 color
//	Output.rt0.rgb = saturate(viewNorm.yyy + bumpNorm.aaa * BumpTint.rgb) * Opacity.zzz;
//Output.rt0.rgb = saturate(bumpNorm.aaa * BumpTint.rgb) * Opacity.zzz;
//Output.rt0.rgb = viewNorm.rgb;
//Output.rt0.rgb = normalize(bumpNorm);
//Output.rt0.rgb = normalize(bumpNorm0.rgb + bumpNorm1.rgb);
//Output.rt0.a = 1.f;

//Output.rt0.rgb = 0.f;
//Output.rt0.rgb = viewNorm;
//Output.rt0.a = 1.f;
	
	viewNorm.xy = -viewNorm.xy * depth * Opacity.x;
//viewNorm.xy = 0.f;
	
	Output.rt1 = saturate(float4(-viewNorm.x, -viewNorm.y, viewNorm.x, viewNorm.y));
	
//Output.rt0.xyz = Output.rt0.a;
//Output.rt0.a = 1.0f;
	
	return Output;
}

//
// Technique
//
technique DistortionPass
{
    pass P0
    {
        VertexShader = compile vs_2_0 DistortVSSimple();
        PixelShader  = compile ps_2_0 DistortPS();

        /* // Alpha test
        AlphaRef = 1;
        AlphaTestEnable = false;
        AlphaFunc = GreaterEqual; */

        // Alpha blending
        AlphaBlendEnable = true;
        SrcBlend = One;
        DestBlend = One;

        CullMode = CCW;

        ZEnable = false;
        ZFunc = LessEqual;
        ZWriteEnable = true;
    }
}

//
// Technique
//
technique DistortionPassWithFlex
{
    pass P0
    {
        VertexShader = compile vs_2_0 DistortVSFlex();
        PixelShader  = compile ps_2_0 DistortPS();

        /* // Alpha test
        AlphaRef = 1;
        AlphaTestEnable = false;
        AlphaFunc = GreaterEqual; */

        // Alpha blending
        AlphaBlendEnable = true;
        SrcBlend = One;
        DestBlend = One;

        CullMode = CCW;

        ZEnable = false;
        ZFunc = LessEqual;
        ZWriteEnable = true;
    }
}

technique DistortionPassCloud
{
    pass P0
    {
        VertexShader = compile vs_2_0 DistortVSSkinning();
        PixelShader  = compile ps_2_0 DistortPS();

        /* // Alpha test
        AlphaRef = 1;
        AlphaTestEnable = false;
        AlphaFunc = GreaterEqual; */

        // Alpha blending
        AlphaBlendEnable = true;
        SrcBlend = One;
        DestBlend = One;

        CullMode = CCW;

        ZEnable = false;
        ZFunc = LessEqual;
        ZWriteEnable = true;
    }
}

technique DistortionPassWithSkinning
{
    pass P0
    {
        VertexShader = compile vs_2_0 DistortVSSkinning();
        PixelShader  = compile ps_2_0 DistortPS();

        /* // Alpha test
        AlphaRef = 1;
        AlphaTestEnable = false;
        AlphaFunc = GreaterEqual; */

        // Alpha blending
        AlphaBlendEnable = true;
        SrcBlend = One;
        DestBlend = One;

        CullMode = CCW;

        ZEnable = false;
        ZFunc = LessEqual;
        ZWriteEnable = true;
    }
}

technique DistortionPassBokuFaceWithSkinning
{
    pass P0
    {
        VertexShader = compile vs_2_0 DistortVSSkinning();
        PixelShader  = compile ps_2_0 DistortPS();

        /* // Alpha test
        AlphaRef = 1;
        AlphaTestEnable = false;
        AlphaFunc = GreaterEqual; */

        // Alpha blending
        AlphaBlendEnable = true;
        SrcBlend = One;
        DestBlend = One;

        CullMode = CCW;

        ZEnable = false;
        ZFunc = LessEqual;
        ZWriteEnable = true;
    }
}

technique DistortionPassWideFaceWithSkinning
{
    pass P0
    {
        VertexShader = compile vs_2_0 DistortVSSkinning();
        PixelShader  = compile ps_2_0 DistortPS();

        /* // Alpha test
        AlphaRef = 1;
        AlphaTestEnable = false;
        AlphaFunc = GreaterEqual; */

        // Alpha blending
        AlphaBlendEnable = true;
        SrcBlend = One;
        DestBlend = One;

        CullMode = CCW;

        ZEnable = false;
        ZFunc = LessEqual;
        ZWriteEnable = true;
    }
}

technique DistortionPassTwoFaceWithSkinning
{
    pass P0
    {
        VertexShader = compile vs_2_0 DistortVSSkinning();
        PixelShader  = compile ps_2_0 DistortPS();

        /* // Alpha test
        AlphaRef = 1;
        AlphaTestEnable = false;
        AlphaFunc = GreaterEqual; */

        // Alpha blending
        AlphaBlendEnable = true;
        SrcBlend = One;
        DestBlend = One;

        CullMode = CCW;

        ZEnable = false;
        ZFunc = LessEqual;
        ZWriteEnable = true;
    }
}

technique DistortionPassWithWind
{
    pass P0
    {
        VertexShader = compile vs_2_0 DistortVSWind();
        PixelShader  = compile ps_2_0 DistortPS();

        /* // Alpha test
        AlphaRef = 1;
        AlphaTestEnable = false;
        AlphaFunc = GreaterEqual; */

        // Alpha blending
        AlphaBlendEnable = true;
        SrcBlend = One;
        DestBlend = One;

        CullMode = CCW;

        ZEnable = false;
        ZFunc = LessEqual;
        ZWriteEnable = true;
    }
}


#endif // DISTORT_H