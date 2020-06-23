// =====================================================
//Terrain: LowMemory effect
// The LowMemory (LM) algorithm designed by Mark Finch
// for the XBox uses a minimal number of vertices with
// a highly compressed vertex structure so as
// to use as little video memory as possible.
// This alogirthm uses 4 vertices per terrain cube and
// and transforms the vertices into their proper
// position depending on which face is being rendered.
// All faces are rendered seprately (therefore this
// algorithm also uses more draw calls). The LowMemory
// algorithm uses about half as much memory as the
// FastShaders algorithm.
// This algorithm should be used on systems with that
// can run vertex shaders quickly (a la powerful 
// dedicated graphics cards with hardware vertex
// acceleration) but have limited memory (e.g., the
// XBox360).
// =====================================================

#include "Terrain.fx" //Included in Terrain_FA

// -----------------------------------------------------
// Fabric render method
// -----------------------------------------------------

//#include "Terrain_FA.fx"

// -----------------------------------------------------
// LowMemory specific parameters
// -----------------------------------------------------
float Inversion;
float3 Normal;

float4x4 BotUVWToUV;
float4x4 TopUVWToUV;

float4x4 BumpToWorld;

float4 DecodePosition[4]; //Per-corner
float DecodeZ;
float3 HeightSelects[4]; //Per-corner

// -----------------------------------------------------
// Helper functions
// -----------------------------------------------------
float3 MakePosition(float2 h01, float2 h23, float2 xy)
{
	float3 pos;
	pos.xy = xy.xy * DecodePosition[h23.y].xy + DecodePosition[h23.y].zw;
	
	pos.z = dot(float3(h01.x, h01.y, h23.x), HeightSelects[h23.y].xyz);

	return pos;
}

float3 MakeUVW(float3 pos, float zDiff)
{
	return float3(pos.x, pos.y, zDiff) * InvCubeSize.y;
}

float2 CubeCenter(float2 xy)
{
	float2 pos;
	pos.xy = xy.xy * InvCubeSize.xx + InvCubeSize.zz;
	
	return pos;
}

float3 EditInvert(float2 center, float3 result)
{
	float4 brush = SampleEditBrush(center);
    
    float inv = brush.r * Inversion;
    
	return (1.0f - inv) * result.rgb + inv * (1.0f - result.rgb);
}

// =====================================================
// Color-pass
// =====================================================
// -----------------------------------------------------
// Color-pass VS output
// -----------------------------------------------------
struct COLOR_VS_OUTPUT_SM2
{
    float4 position     : POSITION;     // vertex position
    float4 diffuse      : COLOR0;       // tint from water, fog strength in 'a'
    float4 UV		    : TEXCOORD0;	// vertex texture coords for textures 0 and 1
    float4 shadowUV		: TEXCOORD1;    // shadowuv
    float  fog          : TEXCOORD2;    // 
};
struct COLOR_VS_OUTPUT_SM3
{
    float4 position     : POSITION;     // vertex position
    float3 worldPos     : TEXCOORD0;    // in world coords
    float4 tint         : COLOR0;       // tint from water, fog strength in 'a'
    float3 luzCol		: COLOR1;		// Average color of point lights
    float4 UV		    : TEXCOORD1;	// vertex texture coords for textures 0 and 1
    float3 luzPos		: TEXCOORD2;	// Average direction of point lights
    float3 eye          : TEXCOORD3;    // vector to eye from point
    float3 perturb		: TEXCOORD4;	// perturbation of the normal
    float2 center		: TEXCOORD5;
};

// -----------------------------------------------------
// Color-pass vertex shaders
// -----------------------------------------------------
COLOR_VS_OUTPUT_SM2 ColorVSCommon_SM2( float3 position, float zDiff)
{
	COLOR_VS_OUTPUT_SM2   Output;
    
    Output.position = mul( float4(position.xyz, 1.0f), WorldViewProjMatrix );
    
	float3 uvw = MakeUVW(position, zDiff);
    Output.UV.xy = mul(float4(uvw.xyz, 1.0f), BotUVWToUV);
    Output.UV.zw = mul(float4(uvw.xyz, 1.0f), TopUVWToUV);
	
    Output.shadowUV = ShadowCoord(position.xy);
    
    Output.diffuse = DiffuseLightPrimary(Normal, 1.0f, 1.0f.xxx, Wrap);//float4(0,0,0,0);//

    float4 eyeDist = EyeDist(position.xyz);
    Output.fog = CalcFog(eyeDist.w);

	float4 gloss = GlossVtx(1.0f, eyeDist.xyz, Normal, SpecularPower);
    Output.diffuse.a = gloss.a;
    Output.diffuse.rgb += gloss.rgb * LightColor0.rgb;
    
    return Output;
}
COLOR_VS_OUTPUT_SM3 ColorVSCommon_SM3(float3 position, float zDiff, float2 xy)
{
    COLOR_VS_OUTPUT_SM3   Output;

    // Transform our position.
    Output.position = mul( float4(position, 1.0f), WorldViewProjMatrix );

    Output.worldPos = position;
    
    Output.perturb = PerturbNormal(position);
    
    Output.center = CubeCenter(xy);

	float3 uvw = MakeUVW(position, zDiff);	
    Output.UV.xy = mul(float4(uvw.xyz, 1.0f), BotUVWToUV);
    Output.UV.zw = mul(float4(uvw.xyz, 1.0f), TopUVWToUV);

    AverageLuz(position, Normal, Output.luzPos, Output.luzCol);

    // Calc fog and DOF contributions.
    float4 eyeDist = EyeDist(Output.worldPos.xyz);
    Output.tint.rgb = float3(1.0f, 1.0f, 1.0f);
    Output.tint.a = CalcFog(eyeDist.w);
    Output.eye = eyeDist.xyz;
    
    return Output;
}
COLOR_VS_OUTPUT_SM2 ColorL10VS_SM2(
                    float2 h01			: POSITION,
                    float2 h23			: TEXCOORD0,
                    float2 xy			: TEXCOORD1
                 )
{
	float3 position = MakePosition(h01, h23, xy);    
	float zDiff = position.z - h01.x * DecodeZ;

    COLOR_VS_OUTPUT_SM2 Output = ColorVSCommon_SM2(position, zDiff);
    
	Output.diffuse.rgb += PointLights10(position, Normal);

    return Output;
}
COLOR_VS_OUTPUT_SM2 ColorL6VS_SM2(
                    float2 h01			: POSITION,
                    float2 h23			: TEXCOORD0,
                    float2 xy			: TEXCOORD1
                 )
{
	float3 position = MakePosition(h01, h23, xy);    
	float zDiff = position.z - h01.x * DecodeZ;
	
    COLOR_VS_OUTPUT_SM2 Output = ColorVSCommon_SM2(position, zDiff);
    
	Output.diffuse.rgb += PointLights6(position, Normal);

    return Output;
}
COLOR_VS_OUTPUT_SM2 ColorL4VS_SM2(
                    float2 h01			: POSITION,
                    float2 h23			: TEXCOORD0,
                    float2 xy			: TEXCOORD1
                 )
{
	float3 position = MakePosition(h01, h23, xy);    
	float zDiff = position.z - h01.x * DecodeZ;

    COLOR_VS_OUTPUT_SM2 Output = ColorVSCommon_SM2(position, zDiff);
    
	Output.diffuse.rgb += PointLights4(position, Normal);

    return Output;
}
COLOR_VS_OUTPUT_SM2 ColorL2VS_SM2(
                    float2 h01			: POSITION,
                    float2 h23			: TEXCOORD0,
                    float2 xy			: TEXCOORD1
                 )
{
	float3 position = MakePosition(h01, h23, xy);    
	float zDiff = position.z - h01.x * DecodeZ;

    COLOR_VS_OUTPUT_SM2 Output = ColorVSCommon_SM2(position, zDiff);
    
	Output.diffuse.rgb += PointLights2(position, Normal);

    return Output;
}
COLOR_VS_OUTPUT_SM2 ColorL0VS_SM2(
                    float2 h01			: POSITION,
                    float2 h23			: TEXCOORD0,
                    float2 xy			: TEXCOORD1
                 )
{
	float3 position = MakePosition(h01, h23, xy);    
	float zDiff = position.z - h01.x * DecodeZ;
	
    COLOR_VS_OUTPUT_SM2 Output = ColorVSCommon_SM2(position, zDiff);

    return Output;
}
COLOR_VS_OUTPUT_SM3 ColorVS_SM3(
                    float2 h01			: POSITION,
                    float2 h23			: TEXCOORD0,
                    float2 xy			: TEXCOORD1
                 )
{        
    float3 position = MakePosition(h01, h23, xy);    
	float zDiff = position.z - h01.x * DecodeZ;

    COLOR_VS_OUTPUT_SM3 Output = ColorVSCommon_SM3(position, zDiff, xy);
    
    return Output;
}

// -----------------------------------------------------
// Color-pass pixel shaders
// -----------------------------------------------------
float4 ColorPS_SM2( COLOR_VS_OUTPUT_SM2 In ) : COLOR0
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

float4 Color2PS_SM2( COLOR_VS_OUTPUT_SM2 In ) : COLOR0
{
	float4 botTex = tex2D( BotSampler, In.UV.xy );
	float4 topTex = tex2D( TopSampler, In.UV.zw );
    
    float2 shadow = ShadowAtten(In.shadowUV);
    In.diffuse.rgb *= shadow.g;

	float3 bot = (BotColor * In.diffuse.rgb
				 + BotGloss * In.diffuse.aaa
				 + BotEmissive)
				 * botTex.rgb;
	float3 top = (TopColor * In.diffuse.rgb
				 + TopGloss * In.diffuse.aaa
				 + TopEmissive)
				 * topTex.rgb;

    float3 result = lerp(bot, top, topTex.a);

    result.rgb = lerp(result, FogColor, In.fog);

    return float4(result, BotEmissive.a);
}

float4 ColorPS_SM3( COLOR_VS_OUTPUT_SM3 In ) : COLOR0
{
	float3 normal = tex2D( BotSampler, In.UV.xy ) * BotBumpStrength.xxz 
													+ BotBumpStrength.yyw;
	normal = mul(float4(normal, 1.0f), BumpToWorld);
	normal += In.perturb;
	normal = normalize(normal);
    
    float2 shadow = Shadow(In.worldPos.xy);

    float3 result = DiffuseLight(normal, shadow.g, BotColor, Wrap);

	result.rgb += ApplyLuz(In.worldPos, normal, In.luzPos, In.luzCol, BotColor.rgb);
	
	In.eye = normalize( In.eye );

	float4 gloss = GlossEnv(BotGloss, In.eye, normal, SpecularPower);
	
	result.rgb += gloss.rgb;
	
	result.rgb += BotEmissive.rgb;
	
	// add in specular shine (from gloss.a)?
        
    // Add in fog.
    result.rgb = lerp(result, FogColor, In.tint.a);
    
    return float4(result, BotEmissive.a);

}

float4 Color2PS_SM3( COLOR_VS_OUTPUT_SM3 In ) : COLOR0
{
	float3 botNorm = tex2D( BotSampler, In.UV.xy ) * BotBumpStrength.xxz 
													+ BotBumpStrength.yyw;

	float4 topNorm = tex2D( TopSampler, In.UV.zw ) * float4(TopBumpStrength.xxz, 1.0f) 
													+ float4(TopBumpStrength.yyw, 0.0f);
													
	topNorm.rgb = lerp(botNorm.rgb, topNorm.rgb, topNorm.a);
	
	topNorm.rgb = mul(float4(topNorm.rgb, 1.0f), BumpToWorld);
	topNorm.rgb += In.perturb;
	topNorm.rgb = normalize(topNorm.rgb);

    float2 shadow = Shadow(In.worldPos.xy);

	In.eye = normalize( In.eye );

	float4 emissive = lerp(BotEmissive, TopEmissive, topNorm.a);
	float4 diffuse = lerp(BotColor, TopColor, topNorm.a);
	
	float4 result = emissive + DiffuseLight(topNorm.rgb, shadow.g, diffuse, Wrap);

	result.rgb += ApplyLuz(In.worldPos, topNorm, In.luzPos, In.luzCol, diffuse.rgb);
	
	float gloss = lerp(BotGloss, TopGloss, topNorm.a);
	result.rgb += GlossEnv(gloss, In.eye, topNorm.xyz, SpecularPower);
	
	// add in specular shine (from gloss.a)?
        
    // Add in fog.
    result.rgb = lerp(result, FogColor, In.tint.a);
    
    return result;

}   // end of ColorPS()


// =====================================================
// Edit mode
// =====================================================
// -----------------------------------------------------
// Edit mode vertex shader output
// -----------------------------------------------------
struct COLOR_VS_EDIT_OUTPUT_SM2
{
    COLOR_VS_OUTPUT_SM2 base;

    float2 center       : TEXCOORD3;
};

// -----------------------------------------------------
// Edit mode vertex shaders
// -----------------------------------------------------
COLOR_VS_EDIT_OUTPUT_SM2 EditColorL10VS_SM2(
                    float2 h01			: POSITION,
                    float2 h23			: TEXCOORD0,
                    float2 xy			: TEXCOORD1
                 )
{
    COLOR_VS_EDIT_OUTPUT_SM2 Output;
    
    Output.base = ColorL10VS_SM2(h01, h23, xy);
    Output.center = CubeCenter(xy);

    return Output;
}
COLOR_VS_EDIT_OUTPUT_SM2 EditColorL6VS_SM2(
                    float2 h01			: POSITION,
                    float2 h23			: TEXCOORD0,
                    float2 xy			: TEXCOORD1
                 )
{
    COLOR_VS_EDIT_OUTPUT_SM2 Output;
    
    Output.base = ColorL6VS_SM2(h01, h23, xy);    
	Output.center = CubeCenter(xy);

    return Output;
}
COLOR_VS_EDIT_OUTPUT_SM2 EditColorL4VS_SM2(
                    float2 h01			: POSITION,
                    float2 h23			: TEXCOORD0,
                    float2 xy			: TEXCOORD1
                 )
{
    COLOR_VS_EDIT_OUTPUT_SM2 Output;
    
    Output.base = ColorL4VS_SM2(h01, h23, xy);
	Output.center = CubeCenter(xy);

    return Output;
}
COLOR_VS_EDIT_OUTPUT_SM2 EditColorL2VS_SM2(
                    float2 h01			: POSITION,
                    float2 h23			: TEXCOORD0,
                    float2 xy			: TEXCOORD1
                 )
{
    COLOR_VS_EDIT_OUTPUT_SM2 Output;
    
    Output.base = ColorL2VS_SM2(h01, h23, xy);
    Output.center = CubeCenter(xy);

    return Output;
}
COLOR_VS_EDIT_OUTPUT_SM2 EditColorL0VS_SM2(
                    float2 h01			: POSITION,
                    float2 h23			: TEXCOORD0,
                    float2 xy			: TEXCOORD1
                 )
{
    COLOR_VS_EDIT_OUTPUT_SM2 Output;
    
    Output.base = ColorL0VS_SM2(h01, h23, xy);
    Output.center = CubeCenter(xy);

    return Output;
}
COLOR_VS_OUTPUT_SM3 EditColorVS_SM3(
                    float2 h01			: POSITION,
                    float2 h23			: TEXCOORD0,
                    float2 xy			: TEXCOORD1
                 )
{
    return ColorVS_SM3(h01, h23, xy);
}

// =====================================================
// Depth-pass
// =====================================================
// -----------------------------------------------------
// Depth-pass vertex shader output
// -----------------------------------------------------
struct DEPTH_VS_OUTPUT
{
    float4 position     : POSITION;     // vertex position
    float4 color        : COLOR00;      // depth values
};

// -----------------------------------------------------
// Depth-pass vertex shaders
// -----------------------------------------------------
DEPTH_VS_OUTPUT TerrainDepthVS(
                    float2 h01			: POSITION,
                    float2 h23			: TEXCOORD0,
                    float2 xy			: TEXCOORD1
                 )
{
    DEPTH_VS_OUTPUT   Output;

    float3 position = MakePosition(h01, h23, xy);

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
float4 TerrainDepthPS( DEPTH_VS_OUTPUT In ) : COLOR0
{
    float4 result = In.color;
    
    return result;
}   // end of TerrainDepthPS()

// -----------------------------------------------------
// Depth-pass technique
// -----------------------------------------------------
technique TerrainDepthPass
{
    pass P0
    {
        VertexShader = compile vs_2_0 TerrainDepthVS();
        PixelShader  = compile ps_2_0 TerrainDepthPS();

        // Alpha test
        AlphaRef = 1;
        AlphaTestEnable = false;
        AlphaFunc = GreaterEqual;

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

// =====================================================
// Pre-cursor
// =====================================================
// -----------------------------------------------------
// Pre-curosr vertex shader output
// -----------------------------------------------------
struct PRECURSOR_VS_OUTPUT
{
	float4 position		: POSITION;
	float2 center		: TEXCOORD0;
	//float2 uv			: TEXCOORD1;
};

// -----------------------------------------------------
// Pre-cursor vertex shaders
// -----------------------------------------------------
PRECURSOR_VS_OUTPUT PreCursorVS(
						float3 position : POSITION0,
						float2 uv : TEXCOORD0)
{
	PRECURSOR_VS_OUTPUT Output;
	
	Output.position = mul(float4(position, 1.0f), WorldViewProjMatrix);
	Output.center = position.xy;
	//Output.uv = uv;
		
	return Output;		
}

// -----------------------------------------------------
// Pre-cursor pixel shaders
// -----------------------------------------------------
float4 PreCursorPS(PRECURSOR_VS_OUTPUT In) : COLOR0
{
	float4 brush = SampleEditBrush(In.center);

    brush.rgb *= float3(1.0f, 1.0f, 0.25f);
    brush.rgb *= (Inversion * 0.5f) + 0.5f;
    
    return brush;
}

// -----------------------------------------------------
// Pre-cursor technique
// -----------------------------------------------------
technique PreCursorPass
{
	pass P0
	{
		VertexShader = compile vs_2_0 PreCursorVS();
		PixelShader = compile ps_2_0 PreCursorPS();

        // Alpha test
        AlphaRef = 1;
        AlphaTestEnable = false;
        AlphaFunc = GreaterEqual;

        // Alpha blending
        AlphaBlendEnable = true;
        SrcBlend = One;
        DestBlend = One;

        CullMode = None;

        ZEnable = false;
        ZFunc = LessEqual;
        ZWriteEnable = false;
	}
}