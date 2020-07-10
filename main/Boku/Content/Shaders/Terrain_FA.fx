// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

#define Debug_UseWhiteTexture;

// =====================================================
//Terrain: Fabric render method
//TODO: Add description
// =====================================================

#include "Terrain.fx"

#ifndef FA_INCLUDED
#define FA_INCLUDED

// -----------------------------------------------------
// Helper functions
// -----------------------------------------------------

float3 EditInvert_FA(float2 center, float3 result)
{
	float4 brush = SampleEditBrush(center);
    
    float inv = brush.r * Inversion_FA;
    
	return (1.0f - inv) * result.rgb + inv * (1.0f - result.rgb);
}

float3 MakeUVW_FA(float3 pos)
{
	return float3(pos.x, pos.y, 0) * InvCubeSize.y;
}

float2 CubeCenter_FA(float2 pos)
{
	return pos - fmod(pos, InvCubeSize.x) + float2(pos.x > 0 ? InvCubeSize.z : -InvCubeSize.z, pos.y > 0 ? InvCubeSize.z : -InvCubeSize.z);
}

// =====================================================
// Color-pass
// =====================================================
// -----------------------------------------------------
// Color-pass VS output
// -----------------------------------------------------
struct COLOR_VS_OUTPUT_FA_SM2
{
    float4 position     : POSITION;     // vertex position
    float4 diffuse      : COLOR0;       // tint from water, fog strength in 'a'
    float4 UV		    : TEXCOORD0;	// vertex texture coords for textures 0 and 1
    float4 shadowUV		: TEXCOORD1;    // shadowuv
    float  fog          : TEXCOORD2;    // 
};
struct COLOR_VS_OUTPUT_FA_SM3
{
    float4 position     : POSITION;     // vertex position
    float3 worldPos     : TEXCOORD0;    // in world coords
    float4 tint         : COLOR0;       // tint from water, fog strength in 'a'
    float3 luzCol		: COLOR1;		// Average color of point lights
    float4 UV		    : TEXCOORD1;	// vertex texture coords for textures 0 and 1
    float3 luzPos		: TEXCOORD2;	// Average direction of point lights
    float3 eye          : TEXCOORD3;    // vector to eye from point
    float3 perturb		: TEXCOORD4;	// perturbation of the normal
};



// =====================================================
// Edit mode
// =====================================================
// -----------------------------------------------------
// Edit mode vertex shader output
// -----------------------------------------------------
struct COLOR_VS_EDIT_OUTPUT_FA_SM2
{
    COLOR_VS_OUTPUT_FA_SM2 base;

    float2 worldPos     : TEXCOORD4;    // pos in world coords
};

struct COLOR_VS_EDIT_OUTPUT_FA_SM3
{
    COLOR_VS_OUTPUT_FA_SM3 base;
};

// -----------------------------------------------------
// Color-pass vertex shaders
// -----------------------------------------------------
COLOR_VS_OUTPUT_FA_SM2 ColorVSCommon_FA_SM2( float3 position, float3 normal )
{
	COLOR_VS_OUTPUT_FA_SM2   Output;
    
    Output.position = mul( float4(position.xyz, 1.0f), WorldViewProjMatrix );
    
	float3 uvw = MakeUVW_FA(position);
    Output.UV.xy = mul(float4(uvw.xyz, 1.0f), BotUVWToUV_FA);
    Output.UV.zw = mul(float4(uvw.xyz, 1.0f), TopUVWToUV_FA);
	
    Output.shadowUV = ShadowCoord(position.xy);
    
    Output.diffuse = DiffuseLightPrimary(normal, 1.0f, 1.0f.xxx, LightWrap);

    float4 eyeDist = EyeDist(position.xyz);
    Output.fog = CalcFog(eyeDist.w);

	float4 gloss = GlossVtx(1.0f, eyeDist.xyz, normal, SpecularPower);
    Output.diffuse.a = gloss.a;
    Output.diffuse.rgb += gloss.rgb * LightColor0.rgb;
    
    return Output;
}
COLOR_VS_OUTPUT_FA_SM3 ColorVSCommon_FA_SM3( float3 position, float3 normal )
{
    COLOR_VS_OUTPUT_FA_SM3   Output;

    // Transform our position.
    Output.position = mul( float4(position.xyz, 1.0f), WorldViewProjMatrix );
    Output.worldPos = position.xyz;

    Output.perturb = normal;
    
	float3 uvw = MakeUVW_FA(position);
	Output.UV.xy = mul(float4(uvw.xyz, 1.0f), BotUVWToUV_FA);
    Output.UV.zw = mul(float4(uvw.xyz, 1.0f), TopUVWToUV_FA);

    AverageLuz(position, normal, Output.luzPos, Output.luzCol);

    // Calc fog and DOF contributions.
    float4 eyeDist = EyeDist(Output.worldPos.xyz);
    Output.tint.rgb = float3(1.0f, 1.0f, 1.0f);
    Output.tint.a = CalcFog(eyeDist.w);
    Output.eye = eyeDist.xyz;
    
    return Output;
}

COLOR_VS_OUTPUT_FA_SM2 ColorL10VS_FA_SM2( float4 positionAndNormalZ : POSITION, float2 normalXY : NORMAL )
{
	float3 position = positionAndNormalZ.xyz;
	float3 normal = float3(normalXY.x, normalXY.y, positionAndNormalZ.w);
	
    COLOR_VS_OUTPUT_FA_SM2 Output = ColorVSCommon_FA_SM2(position, normal);
    
	Output.diffuse.rgb += PointLights10(position, normal);

    return Output;
}
COLOR_VS_OUTPUT_FA_SM2 ColorL6VS_FA_SM2( float4 positionAndNormalZ : POSITION, float2 normalXY : NORMAL )
{
	float3 position = positionAndNormalZ.xyz;
	float3 normal = float3(normalXY.x, normalXY.y, positionAndNormalZ.w);
	
    COLOR_VS_OUTPUT_FA_SM2 Output = ColorVSCommon_FA_SM2(position, normal);
    
	Output.diffuse.rgb += PointLights6(position, normal);

    return Output;
}
COLOR_VS_OUTPUT_FA_SM2 ColorL4VS_FA_SM2( float4 positionAndNormalZ : POSITION, float2 normalXY : NORMAL )
{
	float3 position = positionAndNormalZ.xyz;
	float3 normal = float3(normalXY.x, normalXY.y, positionAndNormalZ.w);
	
    COLOR_VS_OUTPUT_FA_SM2 Output = ColorVSCommon_FA_SM2(position, normal);
    
	Output.diffuse.rgb += PointLights4(position, normal);

    return Output;
}
COLOR_VS_OUTPUT_FA_SM2 ColorL2VS_FA_SM2( float4 positionAndNormalZ : POSITION, float2 normalXY : NORMAL )
{
	float3 position = positionAndNormalZ.xyz;
	float3 normal = float3(normalXY.x, normalXY.y, positionAndNormalZ.w);
	
    COLOR_VS_OUTPUT_FA_SM2 Output = ColorVSCommon_FA_SM2(position, normal);
    
	Output.diffuse.rgb += PointLights2(position, normal);

    return Output;
}
COLOR_VS_OUTPUT_FA_SM2 ColorL0VS_FA_SM2( float4 positionAndNormalZ : POSITION, float2 normalXY : NORMAL )
{
	float3 position = positionAndNormalZ.xyz;
	float3 normal = float3(normalXY.x, normalXY.y, positionAndNormalZ.w);
	
    COLOR_VS_OUTPUT_FA_SM2 Output = ColorVSCommon_FA_SM2(position, normal);
    
    return Output;
}

COLOR_VS_OUTPUT_FA_SM3 ColorVS_FA_SM3( float4 positionAndNormalZ : POSITION, float2 normalXY : NORMAL )
{
	float3 position = positionAndNormalZ.xyz;
	float3 normal = float3(normalXY.x, normalXY.y, positionAndNormalZ.w);
	
    COLOR_VS_OUTPUT_FA_SM3 Output = ColorVSCommon_FA_SM3(position, normal);

    return Output;
}

// -----------------------------------------------------
// Color-pass pixel shaders
// -----------------------------------------------------
float4 ColorPS_FA_SM2( COLOR_VS_OUTPUT_FA_SM2 In ) : COLOR0
{
	float4 botTex = tex2D( BotSampler, In.UV.xy );
    
    float2 shadow = ShadowAtten(In.shadowUV);

	float3 result = (BotColor * In.diffuse.rgb * shadow.g
					+ In.diffuse.aaa * BotGloss
					+ BotEmissive.rgb)
					* botTex.rgb;

    result.rgb = lerp(result, FogColor, In.fog);

    return float4(result, BotEmissive.a);
}

float4 Color2PS_FA_SM2( COLOR_VS_OUTPUT_FA_SM2 In ) : COLOR0
{
	float4 botTex = tex2D( BotSampler, In.UV.xy );
	float4 topTex = tex2D( TopSampler, In.UV.zw );
    
    float2 shadow = ShadowAtten(In.shadowUV);
    //In.diffuse.rgb *= shadow.g;

	float3 bot = (BotColor * In.diffuse.rgb
				 + BotGloss * In.diffuse.aaa
				 + BotEmissive)
				 * botTex.rgb;
	float3 top = (TopColor * In.diffuse.rgb
				 + TopGloss * In.diffuse.aaa
				 + TopEmissive)
				 * topTex.rgb;

    float3 result = lerp(bot, top, topTex.a);
    //result = bot;

    result.rgb = lerp(result, FogColor, In.fog);

    return float4(result, BotEmissive.a);
}

float4 ColorPS_FA_SM3( COLOR_VS_OUTPUT_FA_SM3 In ) : COLOR0
{
	float3 botNorm = tex2D( BotSampler, In.UV.xy ) * BotBumpStrength.xxz 
													+ BotBumpStrength.yyw;
	botNorm = mul(float4(botNorm, 1.0f), BumpToWorld_FA);
	botNorm += In.perturb * 0.5;
	botNorm = normalize(botNorm);
    
    float2 shadow = Shadow(In.worldPos.xy);
    
    float3 result = DiffuseLight(botNorm, shadow.g, BotColor, LightWrap);

	result.rgb += ApplyLuz(In.worldPos, botNorm, In.luzPos, In.luzCol, BotColor.rgb);
	
	In.eye = normalize( In.eye );

	float4 gloss = GlossEnv(BotGloss, In.eye, botNorm, SpecularPower);
	
	result.rgb += gloss.rgb;
	
	result.rgb += BotEmissive.rgb;
	
	// add in specular shine (from gloss.a)?
        
    // Add in fog.
    result.rgb = lerp(result, FogColor, In.tint.a);
        
    return float4(result, BotEmissive.a);
}

float4 Color2PS_FA_SM3( COLOR_VS_OUTPUT_FA_SM3 In ) : COLOR0
{
	float3 botNorm = tex2D( BotSampler, In.UV.xy ) * BotBumpStrength.xxz 
									 				+ BotBumpStrength.yyw;

	float4 topNorm = tex2D( TopSampler, In.UV.zw ) * float4(TopBumpStrength.xxz, 1.0f) 
									 				+ float4(TopBumpStrength.yyw, 0.0f);
													
	topNorm.rgb = lerp(botNorm.rgb, topNorm.rgb, topNorm.a);
	
	topNorm.rgb = mul(float4(topNorm.rgb, 1.0f), BumpToWorld_FA);
	topNorm.rgb += In.perturb;
	topNorm.rgb = normalize(topNorm.rgb);

    float2 shadow = Shadow(In.worldPos.xy);

	In.eye = normalize( In.eye );

	float4 emissive = lerp(BotEmissive, TopEmissive, topNorm.a);
	float4 diffuse = lerp(BotColor, TopColor, topNorm.a);
	
	float4 result = emissive + DiffuseLight(topNorm.rgb, shadow.g, diffuse, LightWrap);

	result.rgb += ApplyLuz(In.worldPos, topNorm, In.luzPos, In.luzCol, diffuse.rgb);
	
	float gloss = lerp(BotGloss, TopGloss, topNorm.a);
	result.rgb += GlossEnv(gloss, In.eye, topNorm.xyz, SpecularPower);
	
	// add in specular shine (from gloss.a)?
        
    // Add in fog.
    result.rgb = lerp(result, FogColor, In.tint.a);
    
    return result;

}   // end of ColorPS()


// =====================================================
// Depth-pass
// =====================================================
// -----------------------------------------------------
// Depth-pass vertex shader output
// -----------------------------------------------------
struct DEPTH_VS_OUTPUT_FA
{
    float4 position     : POSITION;     // vertex position
    float4 color        : COLOR00;      // depth values
};

// -----------------------------------------------------
// Depth-pass vertex shaders
// -----------------------------------------------------
DEPTH_VS_OUTPUT_FA TerrainDepthVS_FA( float4 position	: POSITION )
{
    DEPTH_VS_OUTPUT_FA   Output;

    // Transform our position.
    Output.position = mul( float4(position.xyz, 1.0f), WorldViewProjMatrix );

    // Calc fog and DOF contributions.
    float4 eyeDist = EyeDist(position.xyz);
    Output.color = CalcDOF(eyeDist.w);
    Output.color.b = 0.0f;

    return Output;
}

// -----------------------------------------------------
// Depth-pass pixel shaders
// -----------------------------------------------------
float4 TerrainDepthPS_FA( DEPTH_VS_OUTPUT_FA In ) : COLOR0
{
    float4 result = In.color;
    
    return result;
}   // end of TerrainDepthPS()

// -----------------------------------------------------
// Depth-pass technique
// -----------------------------------------------------
technique TerrainDepthPass_FA
{
    pass P0
    {
        VertexShader = compile vs_2_0 TerrainDepthVS_FA();
        PixelShader  = compile ps_2_0 TerrainDepthPS_FA();

        /* // Alpha test
        AlphaRef = 1;
        AlphaTestEnable = false;
        AlphaFunc = GreaterEqual; */

        // Alpha blending
        AlphaBlendEnable = false;
        SrcBlend = SrcAlpha;
        DestBlend = InvSrcAlpha;

        CullMode = CCW;

        ZEnable = true;
        ZFunc = LessEqual;
        ZWriteEnable = true;
    }
}

#endif // FA_INCLUDED
