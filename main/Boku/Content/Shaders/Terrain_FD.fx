// =====================================================
//Terrain: FewerDraws effect
// The FewerDraws (FD) algorithm designed for PCs 
// without hardware vertex acceleration (like 
// netbooks with Intel 965 chipsets) minimizes 
// the number of draw calls (and by extension, 
// the number of vertices that need to be 
// processed) by rendering all the side faces 
// together in one call and the top face in a 
// separate call (a total of two calls (per 
// material) as compared to the LowMemory 
// algorithm's five calls (per material)). To 
// accomplish this, the FewerDraws algorithm uses 
// four vertices per visible cube face and thus 
// usually uses more memory than the LowMemory 
// algorithm (potentially 5x more, but in practice 
// it usually uses between 2-3x more (one terrain 
// cube will average three visible faces for hilly 
// terrain and only one visible face for completely 
// flat terrain). Because the FewerDraws algorithm 
// has a separate vertex buffer for the top face, 
// we are able to do optimizations for flat terrain 
// that are not possible with the LowMemory 
// algorithm. Thus, large flat terrain areas tend 
// to render much faster (and consume less memory) 
// when rendered with the FewerDraws algorithm as 
// compared to the LowMemory algorithm.
// This algorithm should be used on systems with 
// poor vertex processing capabilities (i.e., PCs 
// with lower end or non-existent dedicated 
// graphics chips, i.e., laptops/netbooks/old-PCs) 
// or systems with plenty of memory (1gb+, i.e., 
// anything but the Xbox.)
// =====================================================

//#include "Terrain.fx" //Included in Terrain_FA

// -----------------------------------------------------
// Fabric render method
// -----------------------------------------------------

#include "Terrain_FA.fx"

// -----------------------------------------------------
// FewerDraws specific parameters
// -----------------------------------------------------
// Note:
//   These arrays (Inversion, Normals, 
//   BotUVWToUV, TopUVWToUV, BumpToWorld) are
//   index by the face they apply to. 
// Indices map:
//	 - [0] for top face
//	 - [1] for front face
//	 - [2] for back face
//	 - [3] for left face
//	 - [4] for right face

float Inversion[5];
float3 Normals[5];

float4x4 BotUVWToUV[5];
float4x4 TopUVWToUV[5];

float4x4 BumpToWorld[5];

// -----------------------------------------------------
// Helper functions
// -----------------------------------------------------
int AsIndex(float index)
{
	return (int)(index + 0.01);//We add 0.01 because that seems to help floats act as indices better
}

float3 MakeUVW(float3 pos, float zDiff)
{
	return float3(pos.x, pos.y, zDiff) * InvCubeSize.y;
}

float2 CubeCenter(float2 pos)
{
	return pos - fmod(pos, InvCubeSize.x) + float2(pos.x > 0 ? InvCubeSize.z : -InvCubeSize.z, pos.y > 0 ? InvCubeSize.z : -InvCubeSize.z);
}

float3 EditInvert(float2 center, float3 result, float face)
{
	float4 brush = SampleEditBrush(center);
    
    float inv = brush.r * Inversion[AsIndex(face)];
    
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
    float  face			: TEXCOORD3;
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
    float  face         : TEXCOORD5;
};

// -----------------------------------------------------
// Color-pass vertex shaders
// -----------------------------------------------------
COLOR_VS_OUTPUT_SM2 ColorVSCommon_SM2( float3 position, float zDiff, float face )
{
	COLOR_VS_OUTPUT_SM2   Output;
    
    Output.position = mul( float4(position.xyz, 1.0f), WorldViewProjMatrix );
    Output.face = face;
    
	float3 uvw = MakeUVW(position, zDiff);
    Output.UV.xy = mul(float4(uvw.xyz, 1.0f), BotUVWToUV[face]);
    Output.UV.zw = mul(float4(uvw.xyz, 1.0f), TopUVWToUV[face]);
	
    Output.shadowUV = ShadowCoord(position.xy);
    
    Output.diffuse = DiffuseLightPrimary(Normals[face], 1.0f, 1.0f.xxx, LightWrap);//float4(0,0,0,0);//

    float4 eyeDist = EyeDist(position.xyz);
    Output.fog = CalcFog(eyeDist.w);

	float4 gloss = GlossVtx(1.0f, eyeDist.xyz, Normals[face], SpecularPower);
    Output.diffuse.a = gloss.a;
    Output.diffuse.rgb += gloss.rgb * LightColor0.rgb;
    
    return Output;
}
COLOR_VS_OUTPUT_SM3 ColorVSCommon_SM3(float3 position, float zDiff, float face )
{
    COLOR_VS_OUTPUT_SM3   Output;

    // Transform our position.
    Output.position = mul( float4(position.xyz, 1.0f), WorldViewProjMatrix );
    Output.worldPos = position.xyz;
    Output.face = face;

    Output.perturb = PerturbNormal(position);
    
	float3 uvw = MakeUVW(position, zDiff);
    Output.UV.xy = mul(float4(uvw.xyz, 1.0f), BotUVWToUV[face]);
    Output.UV.zw = mul(float4(uvw.xyz, 1.0f), TopUVWToUV[face]);

    AverageLuz(position, Normals[face], Output.luzPos, Output.luzCol);

    // Calc fog and DOF contributions.
    float4 eyeDist = EyeDist(Output.worldPos.xyz);
    Output.tint.rgb = float3(1.0f, 1.0f, 1.0f);
    Output.tint.a = CalcFog(eyeDist.w);
    Output.eye = eyeDist.xyz;
    
    return Output;
}

COLOR_VS_OUTPUT_SM2 ColorL10VS_SM2( float4 positionAndZDiff : POSITION, float4 face : TEXCOORD1 )
{
	float3 position = positionAndZDiff.xyz;
	float zDiff = positionAndZDiff.w;
	
    COLOR_VS_OUTPUT_SM2 Output = ColorVSCommon_SM2(position, zDiff, face.x);
    
	Output.diffuse.rgb += PointLights10(position, Normals[face.x]);

    return Output;
}

COLOR_VS_OUTPUT_SM2 ColorL6VS_SM2( float4 positionAndZDiff : POSITION, float4 face : TEXCOORD1 )
{
	float3 position = positionAndZDiff.xyz;
	float zDiff = positionAndZDiff.w; 
	
    COLOR_VS_OUTPUT_SM2 Output = ColorVSCommon_SM2(position, zDiff, face.x);
    
	Output.diffuse.rgb += PointLights6(position, Normals[face.x]);

    return Output;
}
COLOR_VS_OUTPUT_SM2 ColorL4VS_SM2( float4 positionAndZDiff : POSITION, float4 face : TEXCOORD1 )
{
	float3 position = positionAndZDiff.xyz;
	float zDiff = positionAndZDiff.w; 
	
    COLOR_VS_OUTPUT_SM2 Output = ColorVSCommon_SM2(position, zDiff, face.x);
    
	Output.diffuse.rgb += PointLights4(position, Normals[face.x]);

    return Output;
}
COLOR_VS_OUTPUT_SM2 ColorL2VS_SM2( float4 positionAndZDiff : POSITION, float4 face : TEXCOORD1 )
{
	float3 position = positionAndZDiff.xyz;
	float zDiff = positionAndZDiff.w; 
	
    COLOR_VS_OUTPUT_SM2 Output = ColorVSCommon_SM2(position, zDiff, face.x);
    
	Output.diffuse.rgb += PointLights2(position, Normals[face.x]);

    return Output;
}
COLOR_VS_OUTPUT_SM2 ColorL0VS_SM2( float4 positionAndZDiff : POSITION, float4 face : TEXCOORD1 )
{
	float3 position = positionAndZDiff.xyz;
	float zDiff = positionAndZDiff.w;
	
    COLOR_VS_OUTPUT_SM2 Output = ColorVSCommon_SM2(position, zDiff, face.x);

    return Output;
}

COLOR_VS_OUTPUT_SM3 ColorVS_SM3( float4 positionAndZDiff : POSITION, float4 face : TEXCOORD1 )
{
	float3 position = positionAndZDiff.xyz;
	float zDiff = positionAndZDiff.w;
	
    COLOR_VS_OUTPUT_SM3 Output = ColorVSCommon_SM3(position, zDiff, face.x);

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
	normal = mul(float4(normal, 1.0f), BumpToWorld[AsIndex(In.face)]);
	normal += In.perturb;
	normal = normalize(normal);
    
    float2 shadow = Shadow(In.worldPos.xy);

    float3 result = DiffuseLight(normal, shadow.g, BotColor, LightWrap);

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
	
	topNorm.rgb = mul(float4(topNorm.rgb, 1.0f), BumpToWorld[AsIndex(In.face)]);
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
struct DEPTH_VS_OUTPUT
{
    float4 position     : POSITION;     // vertex position
    float4 color        : COLOR00;      // depth values
};

// -----------------------------------------------------
// Depth-pass vertex shaders
// -----------------------------------------------------
DEPTH_VS_OUTPUT TerrainDepthVS( float4 position	: POSITION )
{
    DEPTH_VS_OUTPUT   Output;

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
    brush.rgb *= (Inversion[0] * 0.5f) + 0.5f; //Inversion[0] is for the top face
    
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
        ZFunc = LessEqual;
        ZWriteEnable = false;
	}
}