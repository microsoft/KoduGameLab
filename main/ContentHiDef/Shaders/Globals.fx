
#ifndef GLOBALS_H
#define GLOBALS_H

//
// Shared Globals.
//

// UI lights
shared float3   UILightDirection0;  // Direction light is travelling.
shared float3   UILightColor0;
shared float3   UILightDirection1;  // Direction light is travelling.
shared float3   UILightColor1;
shared float3   UILightDirection2;  // Direction light is travelling.
shared float3   UILightColor2;

shared float4	FogVector;			// Scale, Offset, Max, unused
shared float3   FogColor;

shared float4   EyeLocation;		// These separate values for camera,
shared float4	CameraDir;			// position, direction etc., are 
shared float4   CameraUp;			// redundant with the WorldToCamera transform
shared float4x4	WorldToCamera;		// which we also need. Could replace with functions?

shared float    BloomThreshold;     // Limit above which blooms happens.
shared float    BloomStrength;      // Multiplier for amount of bloom to add.

shared float    DOF_NearPlane;      // Near distance at which blur is max.
shared float    DOF_FocalPlane;     // Distance at which everything is in focus.
shared float    DOF_FarPlane;       // Far distance at which blur is max.
shared float    DOF_MaxBlur;        // Max amount of blur, only applies to far plane.

shared float4	BloomColor;		// Constant color to write to bloom map.

#define NUM_LUZ (10) /// Must match kMaxLights in Luz.cs
shared float4	LightPosition[NUM_LUZ]; /// position.xyz, 1/radius in .w
shared float4	LightColor[NUM_LUZ]; /// color.rgb, wrap in .w

shared float3   LightWrap = float3(1.0f, 0.5f, 0.5f);	// 0 == no wrap, 1 == full, spherical lighting.
														// y is 1.0f / (1.0f + Wrap.x);
														// z is Wrap.x / (1.0f + Wrap.x);

// From Light.fx
// Light 0 is the key light, which shadows are based off of.
shared float4   LightDirection0;    // Direction light is travelling.
shared float3   LightColor0;
shared float4   LightDirection1;    // Direction light is travelling.
shared float3   LightColor1;
shared float4   LightDirection2;    // Direction light is travelling.
shared float3   LightColor2;
shared float4   LightDirection3;    // Direction light is travelling.
shared float3   LightColor3;

shared float4	WarpCenter;

shared texture ShadowTexture;
shared texture ShadowMask;
shared float4 ShadowTextureOffsetScale;    // Offset (x,y) and Scale (z,w) values used to translate 
                                    // the XY coord of the pixel into shadow UV coords.
shared float4 ShadowMaskOffsetScale; // same, but for the shadowmask
shared float    ShadowAttenuation;  // How dark should the shadows be.


#endif // GLOBALS_H
