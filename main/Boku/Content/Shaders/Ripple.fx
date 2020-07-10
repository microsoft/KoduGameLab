// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.


//
// Particle3D -- A collection of shaders for non-textured, 3D particles.
//

#include "Globals.fx"

#include "Fog.fx"
#include "DOF.fx"
#include "StandardLight.fx"

//
// Locals.
//

// The world view and projection matrices
float4x4    WorldViewProjMatrix;

texture RippleTex;

float CurrentTime;
float HalfCube;
float2 WaveSpeed;
float4 Tint;
float2 RotRate;

float4 UVAge;			// botScale.x, botOffset.y, topScale.z, topOffset.w
float4 UpDownAgeBot;	// upS.x, upO.y, dnS.x, dnS.y
float4 UpDownAgeMid;	// upS.x, upO.y, dnS.x, dnS.y
float4 UpDownAgeTop;	// upS.x, upO.y, dnS.x, dnS.y

float4 UvExpand = float4(10.0f, 10.0f - 1.0f, 5.0f, 5.0f - 1.0f);

#include "WaterHeight.fx"

sampler2D RippleSampler =
sampler_state
{
    Texture = <RippleTex>;
    MipFilter = Linear;
    MinFilter = Linear;
    MagFilter = Linear;

    AddressU = Clamp;
    AddressV = Clamp;
};


//
// Vertex shader output structure
//
struct COLOR_VS_OUTPUT
{
    float4 position         : POSITION;     // vertex position
    float4 color			: COLOR0;
    float2 coord0			: TEXCOORD0;	
    float2 coord1			: TEXCOORD1;
	float fog				: TEXCOORD2;
};

/// life = radius / waveSpeed, so invLife = waveSpeed / radius = waveSpeed * invRadius.
float InvLifeFromInvRadius(float invRadius)
{
	return WaveSpeed.x * invRadius;
}

float AgeFromBasis(float4 basis)
{
	float age = CurrentTime - basis.w;
	age *= InvLifeFromInvRadius(basis.z);
	
	age = saturate(age);
	
	age = 1.0f - age;
	age *= age;
	age = 1.0f - age;
	
	return age;
}

float UVAgeFromAge(float age, float2 scaleOff)
{
	return age * scaleOff.x + scaleOff.y;
}

float AlphaFromAge(float age, float4 upDown)
{
	return saturate(age * upDown.x + upDown.y)
		* saturate(age * upDown.z + upDown.w);
}

float4 RotFromAge(float age, float rate)
{
	float4 sc;
	sincos(age * rate, sc.y, sc.x);
	sc.z = -sc.y;
	sc.w = sc.x;
	
	return sc;
}

float2 CalcCoordBot(float3 pos, float4 basis, float age)
{
	float2 uv = pos.xy - basis.xy;
	uv *= basis.z;
	
	float4 rot = RotFromAge(age, RotRate.x);
	
	float uvAge = UVAgeFromAge(age, UVAge.xy);
	
	float expand = UvExpand.x / (1.0f + UvExpand.y * uvAge);
	
	uv *= expand;
	
	uv = uv.xx * rot.xy + uv.yy * rot.zw;
	
	return uv * 0.5f + 0.5f;
}

float2 CalcCoordTop(float2 position, float4 basis, float age)
{
	float2 uv = position.xy - basis.xy;
	uv *= basis.z;

	float uvAge = UVAgeFromAge(age, UVAge.zw);
	
	float expand = UvExpand.z / (1.0f + UvExpand.w * uvAge);
	
	uv *= expand;
	
	float4 rot = RotFromAge(age, RotRate.y);
	
	uv = uv.xx * rot.xy + uv.yy * rot.zw;
	
	return uv * 0.5f + 0.5f;
}


// Transform our coordinates into world space
COLOR_VS_OUTPUT ColorVS(float3 position : POSITION,
						float2 offset : TEXCOORD0,
						float4 basis : TEXCOORD1,
						float2 water : TEXCOORD2)
{
    COLOR_VS_OUTPUT   Output;

	position.z = Height(water.x, WaveCenter, InverseWaveLength, position.xy);
	position.z = max(position.z, water.y);
	position.xy += offset * HalfCube;

	
    // Transform our position.
    Output.position = mul( float4(position, 1.0f), WorldViewProjMatrix );
    
	float age = AgeFromBasis(basis);
	
	if(age >= 1.0f)
		offset.xy = 0.0f.xx;
    
    Output.coord0 = CalcCoordBot(position, basis, age);
    Output.coord1 = CalcCoordTop(position, basis, age);
    
    float updown = AlphaFromAge(age, UpDownAgeTop);
    updown = smoothstep(0.0f, 1.0f, updown);
    Output.color = float4(
		AlphaFromAge(age, UpDownAgeBot),
		AlphaFromAge(age, UpDownAgeMid),
		AlphaFromAge(age, UpDownAgeTop),
		updown);
    

    float4 eyeDist = EyeDist(position.xyz);
    Output.fog = CalcFog( eyeDist.w );

    return Output;
}

//
// Basic Pixel shader
//
float4 ColorPS( COLOR_VS_OUTPUT In ) : COLOR0
{
	float4 texBot = tex2D(RippleSampler, In.coord0);
	
	float4 texTop = tex2D(RippleSampler, In.coord1);
	
	texBot = max(texBot, texTop);
//texBot = texTop;
	texBot *= In.color.zzzz;
	
	float alpha = Tint.w * texBot.x;
	return float4(Tint.xyz * alpha, alpha);
}   // end of BasicPS()

//
// Technique - basic, full color
//
technique TexturedColorPass
{
    pass P0
    {
        VertexShader = compile vs_2_0 ColorVS();
        PixelShader  = compile ps_2_0 ColorPS();

        /* // Alpha test
        AlphaRef = 1;
        AlphaTestEnable = false;
        AlphaFunc = GreaterEqual; */

        // Alpha blending
        AlphaBlendEnable = true;
        SrcBlend = One;
        DestBlend = InvSrcAlpha;

        CullMode = None;

        ZEnable = true;
        ZFunc = LessEqual;
        ZWriteEnable = false;
    }
}

