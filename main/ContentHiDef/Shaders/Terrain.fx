
//
//  Terrain
//

#ifndef TERRAIN_INCLUDES
#define TERRAIN_INCLUDES

#include "Globals.fx"

#include "Fog.fx"
#include "DOF.fx"

#include "StandardLight.fx"
#include "Luz.fx"

// -----------------------------------------------------
// FewerDraws specific parameters
// -----------------------------------------------------
float Inversion_FA;
float4x4 BotUVWToUV_FA;
float4x4 TopUVWToUV_FA;
float4x4 BumpToWorld_FA;

// The world view and projection matrices
float4x4 WorldViewProjMatrix;
float4x4 WorldMatrix;

// -----------------------------------------------------
// Common parameters
// -----------------------------------------------------
int VSIndex;
int PSIndex;

texture BotTex;
texture TopTex;

float4 BotColor;
float BotGloss;
float4 TopColor;
float TopGloss;

float3 InvCubeSize; // (cubeSize, 1.0f / cubeSize, 0.5f * cubeSize)

float4 TopEmissive;
float4 BotEmissive;

float4 BotBumpStrength;
float4 TopBumpStrength;

// -----------------------------------------------------
// Texture samplers
// -----------------------------------------------------
sampler2D BotSampler : register(s0) =
sampler_state
{
    Texture = <BotTex>;
    MipFilter = Linear;
    MinFilter = Linear;
    MagFilter = Linear;

    AddressU = WRAP;
};
sampler2D TopSampler : register(s1) =
sampler_state
{
    Texture = <TopTex>;
    MipFilter = Linear;
    MinFilter = Linear;
    MagFilter = Linear;

    AddressU = WRAP;
};

// -----------------------------------------------------
// Common edit mode parameters
// -----------------------------------------------------
texture EditBrushTexture;
float   EditBrushRadius;
float2	EditBrushToParam;
float2	EditBrushStart;
float2	EditBrushStartToEnd;
float2	EditBrushScaleOff; // 0.5f / Radius, 0.5f

sampler2D EditBrushTextureSampler =
sampler_state
{
    Texture = <EditBrushTexture>;
    MipFilter = Point;
    MinFilter = Point;
    MagFilter = Point;

    AddressU = Clamp;
    AddressV = Clamp;
};

// -----------------------------------------------------
// Common helper functions
// -----------------------------------------------------
float4 SampleEditBrush(float2 center)
{
    // Calc the edit brush UV and sample.
	float t = saturate(dot(center - EditBrushStart, EditBrushToParam));
	float2 closest = t * EditBrushStartToEnd + EditBrushStart;
	float2 editUV = (center - closest) * EditBrushScaleOff.xx + EditBrushScaleOff.yy;

    return tex2D( EditBrushTextureSampler, editUV );
}

#endif // TERRAIN_INCLUDES

