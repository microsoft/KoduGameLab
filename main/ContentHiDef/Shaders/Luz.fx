// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.


#ifndef LUZ_FX
#define LUZ_FX

//#define DISABLE_LUZ

/// Thoughts/Questions:
/// 1)
/// When we average lights, what do we use as the weighting factor?
///		Distance? Doesn't take into account the light's falloff
///		Attenuated distance (e.g. 1.0 - distance/radius)? Doesn't take into account surface normal.
///			Of course, that's partly the point, we don't know the surface normal, because we haven't 
///			evaluated any bump maps yet. But if we assume bump maps will only perturb the normal, then the
///			vertex normal is still a good aproximation.
///		Attenuated distance * normal dot (liPos - pos)/distance? (Final /distance is normalizing direction to light)
///			Note that this simplifies to:
///				(1.0f / distance - 1.0f / radius) * dot(normal, liPos-pos)
///			or
///				pos2light = liPos-pos;
///				(rsqrt(dot(pos2light, pos2light) - 1.0f / radius) * dot(normal, pos2light)
/// 2)
/// It's more intuitive to average radii, but it's cheaper to average 1.0/radius, because we never use the radius
/// proper, we just want to use one over the radius.
/// 3) 
/// We need to be vigilant about divides by zero here. In the vertex shader, it's much more reasonably priced to
/// have a couple of conditionals, as opposed to the pixel shader.
/// 4) 
/// Note that the weighting function doesn't actually need to match whatever lighting function we're eventually going to use
/// to compute illuminance, but it's kind of reassuring if it does.
#ifndef DISABLE_LUZ
void AvgLuz(
	in float3 pos, 
	in float3 norm, 
	in float4 liPos, 
	in float4 liColor, 
	inout float3 avgPos, 
	inout float3 avgColor, 
	inout float avgWgt)
{
	float3 pos2light = liPos.xyz - pos;
    float invLen = rsqrt(dot(pos2light, pos2light));
	float dist = 1.0f / invLen;
    pos2light *= invLen;
	float wgt = saturate(1.0f - dist * liPos.w);
    wgt *= dot(liColor.rgb, liColor.rgb);
	
	avgPos += pos2light * wgt;
	avgColor += liColor * wgt;
	avgWgt += wgt;
}
#endif // DISABLE_LUZ

void AverageLuz(in float3 pos, in float3 norm, out float3 avgDir, out float3 avgColor)
{
#ifndef DISABLE_LUZ
	float3 accDir = float3(0.0f, 0.0f, 0.0f);
	float3 accColor = float3(0.0f, 0.0f, 0.0f);
	
	float accWgt = 0.0f;

	int i;
	for(i = 0; i < NUM_LUZ; ++i)
//	for(i = 0; i < 1; ++i)
	{
		AvgLuz(pos, norm, LightPosition[i], LightColor[i], accDir, accColor, accWgt);
	}
	accWgt = accWgt > 0.0f ? 1.0f / accWgt : 1.0f;
	
	avgDir = accDir * accWgt;
	avgColor = accColor;
#else // DISABLE_LUZ
	avgDir = float3(0.0f, 0.0f, 0.0f);
	avgColor = float3(0.0f, 0.0f, 0.0f);
#endif // DISABLE_LUZ
}

float3 ApplyLuz(
	in float3 pos,
	in float3 norm, 
	in float3 luzDir,
	in float3 luzCol,
	in float3 diffuse)
{
#ifndef DISABLE_LUZ
    float atten = saturate(dot(normalize(luzDir.xyz), norm));
	return luzCol.rgb * diffuse * atten.xxx;
#else // DISABLE_LUZ
	return float3(0.0f, 0.0f, 0.0f);
#endif // DISABLE_LUZ
}

#endif // LUZ_FX
