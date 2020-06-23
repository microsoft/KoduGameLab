//
//  Copy -- straight pixel copy with attenuation.
//

#include "Globals.fx"

#include "WaterHeight.fx"

float3 NearPlaneToCamera;
float4x4 CameraToWorld;

//
// Variables
//
texture SourceTexture;
float   Attenuation;
float4  Scale;
float3  CubeSize; // .x == cubesize, .y == 1.0f / cubesize, .z == cubesize/2

// Distortion specifics
texture Bump;
float BumpStrength = 1.0f;
float4 BumpScroll = float4(0.0f, 0.0f, 0.0f, 0.0f);
float4 BumpScale = float4(1.0f, 1.0f, 1.0f, 1.0f);
float3 WaterColor = float3(0.2f, 0.5f, 0.6f);

sampler2D DistortBumpSampler =
sampler_state
{
    Texture = <Bump>;
    MipFilter = Linear;
    MinFilter = Linear;
    MagFilter = Linear;

    AddressU = WRAP;
    AddressV = WRAP;
};

//
// Vertex shader output structure
//
struct VS_OUTPUT
{
    float4 position     : POSITION;     // vertex position
    float2 textureUV    : TEXCOORD0;    // vertex texture coords
    float3 worldPos		: TEXCOORD1;	// Position in world space
};

struct DISTORT_PS_OUTPUT
{
	float4		rt0 : COLOR0;
	float4		rt1 : COLOR1;
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
    
    // Establish world position
    Output.worldPos = Output.position.xyw * NearPlaneToCamera;
    Output.worldPos = mul(float4(Output.worldPos, 1.0f), CameraToWorld).xyz;
    
    return Output;
}   // end of VS()

//
// Copy Pixel shader
//
DISTORT_PS_OUTPUT PS( VS_OUTPUT In )
{
	DISTORT_PS_OUTPUT Out;
	
	// Quantize the world position.xy to cube center
	In.worldPos.xy *= CubeSize.yy; // times 1/cubesize
	In.worldPos.xy = floor(In.worldPos.xy);
	In.worldPos.xy *= CubeSize.xx; // times cubesize
	In.worldPos.xy += CubeSize.zz; // + cubesize / 2
	
	// Find wave height here
	float waveHeight = Height(WaveCenter, InverseWaveLength, In.worldPos.xy);
	float under = waveHeight > In.worldPos.z ? 1.0f : 0.0f;

	float3 norm = tex2D( DistortBumpSampler, In.textureUV * BumpScale.xy + BumpScroll.xy ) 
					* 2.0f - 1.0f;
	norm += tex2D( DistortBumpSampler, In.textureUV * BumpScale.xy + BumpScroll.zw )
					* 2.0f - 1.0f;
	norm = normalize(norm);
	
	float4 offset = saturate(float4(-norm.x, -norm.y, norm.x, norm.y));
	offset *= under * BumpStrength;

	Out.rt0.rgb = under * WaterColor * (1.f - smoothstep(0.85f, 1.0f, norm.z));
	Out.rt0.a = under * 0.1f;
	
	
	Out.rt1 = offset;
    
    return Out;
}   // end of PS()

//
// Just copy the image
//
technique Distort
{
    pass P0
    {
        VertexShader = compile vs_2_0 VS();
        PixelShader  = compile ps_2_0 PS();

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

