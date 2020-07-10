// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.


//
//	Shadow helper function used by x-file (3d model) rendering.
//

#define SHADOW_EPSILON 0.01f


//
// CalcShadow2x2()	Calculate the shadow's attenuation using
//					2x2 percentage closest filtering.
//
float CalcShadow2x2( float4 pos )
{
    pos.xyz /= pos.w;
    
    // Calc UV coords for sampling the shadow map.
    float2 shadowUV = 0.5f * pos.xy + 0.5f;
    shadowUV.y = 1.0f - shadowUV.y;
    
    // Convert to texels.
    float2 texelpos = ShadowMapSize.x * shadowUV;
    
    // Get lerp amounts (bilinear weights).
    float2 lerps = frac( texelpos );
    
    float samples[ 4 ];
    samples[ 0 ] = tex2D( ShadowMapSampler, shadowUV ).r + SHADOW_EPSILON < pos.z ? 0.0f : 1.0f;
    samples[ 1 ] = tex2D( ShadowMapSampler, shadowUV + float2( ShadowMapSize.y, 0.0f ) ).r + SHADOW_EPSILON < pos.z ? 0.0f : 1.0f;
    samples[ 2 ] = tex2D( ShadowMapSampler, shadowUV + float2( 0.0f, ShadowMapSize.y ) ).r + SHADOW_EPSILON < pos.z ? 0.0f : 1.0f;
    samples[ 3 ] = tex2D( ShadowMapSampler, shadowUV + float2( ShadowMapSize.y, ShadowMapSize.y ) ).r + SHADOW_EPSILON < pos.z ? 0.0f : 1.0f;
    
    float attenuation = lerp( lerp( samples[ 0 ], samples[ 1 ], lerps.x ), 
							  lerp( samples[ 2 ], samples[ 3 ], lerps.x ), 	
							  lerps.y );
    
    return attenuation * ShadowAttenuation + (1 - ShadowAttenuation);
}	// end of CalcShadow2x2()


//
// CalcShadow4x4()	Calculate the shadow's attenuation using
//					4x4 percentage closest filtering.
//
float CalcShadow4x4( float4 pos )
{
    pos.xyz /= pos.w;
    
    // Calc UV coords for sampling the shadow map.
    float2 shadowUV = 0.5f * pos.xy + 0.5f;
    shadowUV.y = 1.0f - shadowUV.y;
    
    // Convert to texels.
    float2 texelpos = ShadowMapSize.x * shadowUV;
    
    // Get lerp amounts (bilinear weights).
    float2 lerps = frac( texelpos );
    
    float texel = ShadowMapSize.y;
    float texel2 = texel + texel;
    
    float samples[ 16 ];
    samples[  0 ] = tex2D( ShadowMapSampler, shadowUV + float2( -texel, -texel ) ).x + SHADOW_EPSILON < pos.z ? 0.0f : 1.0f;
    samples[  1 ] = tex2D( ShadowMapSampler, shadowUV + float2( 0,      -texel ) ).x + SHADOW_EPSILON < pos.z ? 0.0f : 1.0f;
    samples[  2 ] = tex2D( ShadowMapSampler, shadowUV + float2( texel,  -texel ) ).x + SHADOW_EPSILON < pos.z ? 0.0f : 1.0f;
    samples[  3 ] = tex2D( ShadowMapSampler, shadowUV + float2( texel2, -texel ) ).x + SHADOW_EPSILON < pos.z ? 0.0f : 1.0f;

    samples[  4 ] = tex2D( ShadowMapSampler, shadowUV + float2( -texel, 0 ) ).x + SHADOW_EPSILON < pos.z ? 0.0f : 1.0f;
    samples[  5 ] = tex2D( ShadowMapSampler, shadowUV + float2( 0,      0 ) ).x + SHADOW_EPSILON < pos.z ? 0.0f : 1.0f;
    samples[  6 ] = tex2D( ShadowMapSampler, shadowUV + float2( texel,  0 ) ).x + SHADOW_EPSILON < pos.z ? 0.0f : 1.0f;
    samples[  7 ] = tex2D( ShadowMapSampler, shadowUV + float2( texel2, 0 ) ).x + SHADOW_EPSILON < pos.z ? 0.0f : 1.0f;

    samples[  8 ] = tex2D( ShadowMapSampler, shadowUV + float2( -texel, texel ) ).x + SHADOW_EPSILON < pos.z ? 0.0f : 1.0f;
    samples[  9 ] = tex2D( ShadowMapSampler, shadowUV + float2( 0,      texel ) ).x + SHADOW_EPSILON < pos.z ? 0.0f : 1.0f;
    samples[ 10 ] = tex2D( ShadowMapSampler, shadowUV + float2( texel,  texel ) ).x + SHADOW_EPSILON < pos.z ? 0.0f : 1.0f;
    samples[ 11 ] = tex2D( ShadowMapSampler, shadowUV + float2( texel2, texel ) ).x + SHADOW_EPSILON < pos.z ? 0.0f : 1.0f;

    samples[ 12 ] = tex2D( ShadowMapSampler, shadowUV + float2( -texel, texel2 ) ).x + SHADOW_EPSILON < pos.z ? 0.0f : 1.0f;
    samples[ 13 ] = tex2D( ShadowMapSampler, shadowUV + float2( 0,      texel2 ) ).x + SHADOW_EPSILON < pos.z ? 0.0f : 1.0f;
    samples[ 14 ] = tex2D( ShadowMapSampler, shadowUV + float2( texel,  texel2 ) ).x + SHADOW_EPSILON < pos.z ? 0.0f : 1.0f;
    samples[ 15 ] = tex2D( ShadowMapSampler, shadowUV + float2( texel2, texel2 ) ).x + SHADOW_EPSILON < pos.z ? 0.0f : 1.0f;

	float attenuation = 0.0f;

	float dx = lerps.x;
	float dy = lerps.y;
	float invdx = 1.0f - dx;
	float invdy = 1.0f - dy;

	attenuation += samples[  0 ] * invdx * invdy;
	attenuation += samples[  1 ] * invdy;
	attenuation += samples[  2 ] * invdy;
	attenuation += samples[  3 ] * dx * invdy;
	attenuation += samples[  4 ] * invdx;
	attenuation += samples[  5 ];
	attenuation += samples[  6 ];
	attenuation += samples[  7 ] * dx;
	attenuation += samples[  8 ] * invdx;
	attenuation += samples[  9 ];
	attenuation += samples[ 10 ];
	attenuation += samples[ 11 ] * dx;
	attenuation += samples[ 12 ] * invdx * dy;
	attenuation += samples[ 13 ] * dy;
	attenuation += samples[ 14 ] * dy;
	attenuation += samples[ 15 ] * dx * dy;
	
	attenuation /= 9.0f;
	
    return attenuation * ShadowAttenuation + (1 - ShadowAttenuation);
}	// end of CalcShadow4x4()


