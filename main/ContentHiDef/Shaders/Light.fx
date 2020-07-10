// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

#ifndef LIGHT_H
#define LIGHT_H

#include "Globals.fx"
#include "EyeDist.fx"

float3 GlowEmissiveColor = float3(0, 0, 0);

sampler2D ShadowTextureSampler =
sampler_state
{
    Texture = <ShadowTexture>;
    MipFilter = Linear;
    MinFilter = Linear;
    MagFilter = Linear;

    AddressU = Clamp;
    AddressV = Clamp;
};

sampler2D ShadowMaskSampler =
sampler_state
{
    Texture = <ShadowMask>;
    MipFilter = Linear;
    MinFilter = Linear;
    MagFilter = Linear;

    AddressU = Clamp;
    AddressV = Clamp;
};


shared texture  EnvironmentMap;

samplerCUBE EnvMapSampler =
sampler_state
{
    Texture = <EnvironmentMap>;
    MipFilter = Linear;
    MinFilter = Linear;
    MagFilter = Linear;

    AddressU = WRAP;
    AddressV = WRAP;
};

float4 ShadowCoord(float2 pos)
{
    float4 coord;
    coord.xy = pos.xy * ShadowTextureOffsetScale.zw + ShadowTextureOffsetScale.xy;
    coord.zw = pos.xy * ShadowMaskOffsetScale.zw + ShadowMaskOffsetScale.xy;
    return coord;
}

float2 ShadowAtten(float4 coord)
{
    return 1.0f - 
		ShadowAttenuation 
		* tex2D( ShadowTextureSampler, coord.xy ).rg
		* tex2D (ShadowMaskSampler, coord.zw ).rg;
}

float2 Shadow(float2 pos)
{
    return ShadowAtten(ShadowCoord(pos));
}

float3 PerturbNormal(float3 position)
{
	float3 toPos = position - WarpCenter.xyz;
	toPos *= WarpCenter.w;
//	toPos *= toPos;
	return toPos;
}

float4 DiffuseLight(float3 normal, float shadow, float3 tint, float3 wrap)
{
    float4 accum;
    accum.rgb = max(wrap.y * -dot(LightDirection0.xyz, normal) + wrap.z, 0) * shadow * LightColor0;
#if 1
    accum.rgb += max(wrap.y * -dot(LightDirection1.xyz, normal) + wrap.z, 0) * LightColor1;
    accum.rgb += max(wrap.y * -dot(LightDirection2.xyz, normal) + wrap.z, 0) * LightColor2;
    accum.rgb += max(wrap.y * -dot(LightDirection3.xyz, normal) + wrap.z, 0) * LightColor3;
#endif

//    accum.rgb += saturate(dot(LightDirection1.xyz, normal)) * LightColor1;
//    accum.rgb += saturate(dot(LightDirection2.xyz, normal)) * LightColor2;
//    accum.rgb += saturate(dot(LightDirection3.xyz, normal)) * LightColor3;

	accum.rgb += GlowEmissiveColor;
    
    return float4(tint * accum, 1.0f);    
}   
float4 DiffuseLightPrimary(float3 normal, float shadow, float3 tint, float3 wrap)
{
    float4 accum;
    accum.rgb = max(wrap.y * -dot(LightDirection0.xyz, normal) + wrap.z, 0) * shadow * LightColor0;
	accum.rgb += GlowEmissiveColor;
    
    return float4(tint * accum, 1.0f);    
}   

float4 DiffuseLight(float3 normal)
{
    float3 accum;
    accum.rgb = max(-dot(LightDirection0.xyz, normal), 0) * LightColor0;
#if 1
    accum.rgb += max(-dot(LightDirection1.xyz, normal), 0) * LightColor1;
    accum.rgb += max(-dot(LightDirection2.xyz, normal), 0) * LightColor2;
    accum.rgb += max(-dot(LightDirection3.xyz, normal), 0) * LightColor3;
#endif

	accum.rgb += GlowEmissiveColor;
    
    return float4(accum.rgb, 1.0f);    
}

// Sample the environment.
float3 Env(float3 reflection)
{
//return float3(0.0f, 0.0f, 0.0f);
    return texCUBE( EnvMapSampler, reflection ) * LightColor0.rgb * 0.3f;
}

float4 GlossReflect(float gloss, float3 eye, float3 normal, float specPow, out float3 reflection)
{
    // Add in the gloss.
    // Create a reflection vector
    float EdotN = max( dot( eye, normal ), 0 );
    reflection = normalize( 2 * EdotN * normal - eye );
    float LdotR = dot(reflection, LightDirection0.xyz) * LightDirection0.a;
    float specular = pow( max( -LdotR, 0.00001f ), specPow );
    float fres = 1.0f - abs( dot( normal, eye ) );

    return float4((gloss * fres).xxx, specular);
}

float4 GlossReflectAniso(float gloss, float3 eye, float3 normal, float2 aniso, float specPow, out float3 reflection)
{
    // Add in the gloss.
    // Create a reflection vector
    float EdotN = max( dot( eye, normal ), 0 );
    reflection = normalize( 2 * EdotN * normal - eye );

    float fres = 1.0f - abs( dot( normal, eye ) );

    float3 axis0 = normalize(float3(-normal.y, normal.x, 0.0f));
    float3 axis1 = normalize(float3(-normal.x * normal.z, 
                                    -normal.y * normal.z,
                                    dot(normal.xy, normal.xy)));

    float2 del = float2(dot(reflection, axis0) + dot(LightDirection0.xyz, axis0),
                        dot(reflection, axis1) + dot(LightDirection0.xyz, axis1));
    del *= aniso.xy;

    float specular = pow(max(1.0f - length(del), 0.00001f), specPow);

    return float4((gloss * fres).xxx, specular);
}

float4 GlossVtx(float gloss, float3 eye, float3 normal, float specPow)
{
    float3 reflection;
    return GlossReflect(gloss, eye, normal, specPow, reflection);
}

float4 GlossEnvAniso(float gloss, float3 eye, float3 normal, float2 aniso, float specPow)
{
    float3 reflection;
    float4 result = GlossReflectAniso(gloss, eye, normal, aniso, specPow, reflection);
    result.rgb *= Env(reflection);

    return result;
}
			
float4 GlossEnv(float gloss, float3 eye, float3 normal, float specPow)
{
    float3 reflection;
    float4 result = GlossReflect(gloss, eye, normal, specPow, reflection);
    result.rgb *= Env(reflection);

    return result;
}

float3 PointLight(float3 pos, float3 normal, float4 lightPos, float4 lightCol)
{
	/// Get ray from pos to light
	pos = lightPos.xyz - pos;
	
	/// Get 1/distance_to_light
	float invLen = rsqrt(dot(pos, pos));
	
	/// Want 
	/// saturate(dot(normalized(lightPos - pos), normal))	/// Angular attenuation
	/// * saturate(1 - dist/range)							/// Distance attenuation
	/// So we notice that normalized(lightPos - pos) == (lightPos - pos) / dist,
	/// so we take that 1/dist and push it from angular to distance attenuation.
	/// That gives us
	///		max(dot(lightPos - pos, normal), 0)
	///		* max(1/dist - 1/range, 0)
	/// Notice that we also turned the saturates into clamp positive, because it's
	/// half the instructions and we aren't concerned with it being too big, just 
	/// don't want to let it go negative.
	/// 1/range is passed in as a constant, and 1/dist is the cheaper version of
	/// dist because it can use the rsqrt builtin.
	float NdotL = max(dot(pos, normal), 0) * max(invLen - lightPos.w, 0);
	
	/// Finally attenuate by light color.
	return NdotL * lightCol.rgb;
}

float3 PointLights(float3 pos, float3 normal)
{
#if 1
#if 1
	return PointLight(pos, normal, LightPosition[0], LightColor[0])
		 + PointLight(pos, normal, LightPosition[1], LightColor[1])
		 + PointLight(pos, normal, LightPosition[2], LightColor[2])
		 + PointLight(pos, normal, LightPosition[3], LightColor[3])
		 + PointLight(pos, normal, LightPosition[4], LightColor[4])
		 + PointLight(pos, normal, LightPosition[5], LightColor[5])
		 + PointLight(pos, normal, LightPosition[6], LightColor[6])
		 + PointLight(pos, normal, LightPosition[7], LightColor[7])
		 + PointLight(pos, normal, LightPosition[8], LightColor[8])
		 + PointLight(pos, normal, LightPosition[9], LightColor[9]);
#else
	float3 result = PointLight(pos, normal, LightPosition[0], LightColor[0]);
	for(int i = 1; i < 10; ++i)
	{
		result += PointLight(pos, normal, LightPosition[i], LightColor[i]);
	}
	return result;
#endif		 
#else
	return float3(0, 0, 0);
#endif
}

float3 PointLights10(float3 pos, float3 normal)
{
	return PointLight(pos, normal, LightPosition[0], LightColor[0])
		 + PointLight(pos, normal, LightPosition[1], LightColor[1])
		 + PointLight(pos, normal, LightPosition[2], LightColor[2])
		 + PointLight(pos, normal, LightPosition[3], LightColor[3])
		 + PointLight(pos, normal, LightPosition[4], LightColor[4])
		 + PointLight(pos, normal, LightPosition[5], LightColor[5])
		 + PointLight(pos, normal, LightPosition[6], LightColor[6])
		 + PointLight(pos, normal, LightPosition[7], LightColor[7])
		 + PointLight(pos, normal, LightPosition[8], LightColor[8])
		 + PointLight(pos, normal, LightPosition[9], LightColor[9]);
}
float3 PointLights6(float3 pos, float3 normal)
{
	return PointLight(pos, normal, LightPosition[0], LightColor[0])
		 + PointLight(pos, normal, LightPosition[1], LightColor[1])
		 + PointLight(pos, normal, LightPosition[2], LightColor[2])
		 + PointLight(pos, normal, LightPosition[3], LightColor[3])
		 + PointLight(pos, normal, LightPosition[4], LightColor[4])
		 + PointLight(pos, normal, LightPosition[5], LightColor[5]);
}
float3 PointLights4(float3 pos, float3 normal)
{
	return PointLight(pos, normal, LightPosition[0], LightColor[0])
		 + PointLight(pos, normal, LightPosition[1], LightColor[1])
		 + PointLight(pos, normal, LightPosition[2], LightColor[2])
		 + PointLight(pos, normal, LightPosition[3], LightColor[3]);
}
float3 PointLights2(float3 pos, float3 normal)
{
	return PointLight(pos, normal, LightPosition[0], LightColor[0])
		 + PointLight(pos, normal, LightPosition[1], LightColor[1]);
}
			
#endif // LIGHT_H
