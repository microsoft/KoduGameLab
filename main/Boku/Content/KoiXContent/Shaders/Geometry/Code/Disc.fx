// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.


//
// Disc shaders
//

// The world view and projection matrices
float4x4 WorldViewProjMatrix;

float2 Center;              // Center of disc.
float Radius;               // Radius of disc.
float4 BodyColor;               
float EdgeBlend;            // Fractional pixel amount for antiliased edge.

float4 OutlineColor;
float OutlineWidth;

float2 ShadowOffset;        // Offset relative to shape in pixels.
float ShadowSize;           // Pixel amount for blended edge of shadow. (actually half of it)
float ShadowAttenuation;    // 1 is full strength shadow, <1 is softer version.

float3 BevelWidth;          // x: width, y: width - edgeBlend, z: width + edgeBlend

Texture2D DiffuseTexture;

sampler DiffuseTextureSampler =
sampler_state
{
    Texture = <DiffuseTexture>;
    MipFilter = LINEAR;
    MinFilter = LINEAR;
    MagFilter = LINEAR;
    AddressU = Clamp;
    AddressV = Clamp;
};

//
// VS input/output structures.
//
struct VertexShaderInput
{
    float2 Position		: POSITION0;    // Position in pixels.
    float2 UV           : TEXCOORD0;    // TextureCoordinates.
};

struct VertexShaderOutput
{
    float4 Position     : POSITION0;
    float2 UV           : TEXCOORD0;        // Texture Coordinates.
    float3 Pixels	    : TEXCOORD1;		// xy pixel coords with 0,0 at center of shape, total shadow blend width
    float2 Radius	    : TEXCOORD2;		// innerRadius2, outerRadius2
    float2 Outline      : TEXCOORD3;        // innerOutlineRadius2, outerOutlineRadius2
    float4 Shadow       : TEXCOORD4;        // innerShadowRadius2, outerShadowRadius2, widthShadow, heightShadow
    float2 Bevel        : TEXCOORD5;        // innerBevelRadius, outerBevelRadius
};

//
// Vertex shader helper functions.
//

void CalcRadiusVS(inout VertexShaderOutput output)
{
    // Precalc corner radius limits.  This is the edge between the body and the background.
    float innerRadius = max(Radius - EdgeBlend, 0);
    float innerRadius2 = innerRadius * innerRadius;
    output.Radius.x = innerRadius2;

    float outerRadius = (Radius + EdgeBlend);
    float outerRadius2 = outerRadius * outerRadius;
    output.Radius.y = outerRadius2;
}   // end of CalcRadiusVS()

void CalcOutlineVS(inout VertexShaderOutput output)
{
    // Precalc outline radius limits.  This is the edge between the outline and body.
    float innerOutlineRadius = max(Radius - OutlineWidth - EdgeBlend, 0);
    float innerOutlineRadius2 = innerOutlineRadius * innerOutlineRadius;
    output.Outline.x = innerOutlineRadius2;
    float outerOutlineRadius = max(Radius - OutlineWidth + EdgeBlend, 0);
    float outerOutlineRadius2 = outerOutlineRadius * outerOutlineRadius;
    output.Outline.y = outerOutlineRadius2;
}   // end of CalcOutlineVS()

void CalcShadowVS(inout VertexShaderOutput output)
{
    // Precalc Shadow radius limits.  This is the inner and outer radii where
    // the shadow transitions from full to nothing.
    float outerShadowRadius = max(Radius + ShadowSize + EdgeBlend, 0);
    float outerShadowRadius2 = outerShadowRadius * outerShadowRadius;
    output.Shadow.y = outerShadowRadius2;

    float innerShadowRadius = max(Radius - ShadowSize - EdgeBlend, 0);
    float innerShadowRadius2 = innerShadowRadius * innerShadowRadius;
    output.Shadow.x = innerShadowRadius2;

    float radius = max(ShadowSize, Radius);
    output.Pixels.z = radius;

    float2 Size = float2(1, 1);
    float widthShadow = Size.x - 2 * radius;
    float heightShadow = Size.y - 2 * radius;
    output.Shadow.z = widthShadow;
    output.Shadow.w = heightShadow;

}   // end of CalcShadowVS()

void CalcBevelVS(inout VertexShaderOutput output)
{
    // Precalc outline radius limits.  This is the edge between the flat surface and the bevel.
    float innerBevelRadius = max(Radius - BevelWidth - EdgeBlend, 0);
    output.Bevel.x = innerBevelRadius;

    float outerBevelRadius = max(Radius - BevelWidth + EdgeBlend, 0);
    output.Bevel.y = outerBevelRadius;
}   // end of CalcOutlineVS()

//
// Standard VS used for all options.
//
VertexShaderOutput VS(VertexShaderInput input)
{
    VertexShaderOutput output;

    output.Position = mul(float4(input.Position, 0, 1), WorldViewProjMatrix);

    // Shift position so 0, 0 is at center of shape.
    output.Pixels.xy = input.Position - Center;
    output.UV = input.UV;

    // We don't need all of these for all versions.  Think about
    // breaking this VS up into specific versions for each technique.
    CalcRadiusVS(output);
    CalcOutlineVS(output);
    CalcShadowVS(output);
    CalcBevelVS(output);

    return output;
}   // end of VS()

//
// Pixel shader helper functions
//

// 
// Calculates the color and alpha for a solid shape.
//
float4 CalcSolidColor(VertexShaderOutput input)
{
    float innerRadius2 = input.Radius.x;
    float outerRadius2 = input.Radius.y;

    float2 pos = input.Pixels.xy;

    float dx = pos.x;
    float dy = pos.y;
    float dist2 = dx*dx + dy*dy;

    // Calc blended edge.
    float alpha = (outerRadius2 - dist2) / (outerRadius2 - innerRadius2);
    alpha = clamp(alpha, 0, 1);

    float4 result = BodyColor * alpha;

    return result;
}   // end of CalcSolidColor()

//
// Calculates the color and alpha for an outlined shape.
//
float4 CalcOutlineColor(VertexShaderOutput input)
{
    float innerRadius2 = input.Radius.x;
    float outerRadius2 = input.Radius.y;
    float innerOutlineRadius2 = input.Outline.x;
    float outerOutlineRadius2 = input.Outline.y;

    float2 pos = input.Pixels.xy;

    float dx = pos.x;
    float dy = pos.y;
    float dist2 = dx*dx + dy*dy;

    // Calc color body vs outline.
    float outlineBodyBlend = (outerOutlineRadius2 - dist2) / (outerOutlineRadius2 - innerOutlineRadius2);
    outlineBodyBlend = clamp(outlineBodyBlend, 0, 1);

    // Calc blended edge.
    float alpha = (outerRadius2 - dist2) / (outerRadius2 - innerRadius2);
    alpha = clamp(alpha, 0, 1);

    float4 result = lerp(OutlineColor, BodyColor, outlineBodyBlend) * alpha;
    
    return result;
}   // end of CalcOutlineColor()

//
// Gets the texture color, blends it with the bodyColor and returns
// the result.
//
// Blending is done assume texture is pre-multiplied.
//
float4 ApplyTextureColor(VertexShaderOutput input, float4 bodyColor)
{
    float4 textureColor = tex2D(DiffuseTextureSampler, input.UV);
    float4 result;
    result.rgb = textureColor.rgb * bodyColor.a + (1 - textureColor.a) * bodyColor.rgb;
    result.a = bodyColor.a;

    return result;
}   // end of ApplyTextureColor()

//
// Calculate normal for slant bevel.
//
float3 CalcSlantBevel(VertexShaderOutput input, float4 bodyColor)
{
    float innerBevelRadius = input.Bevel.x;
    float outerBevelRadius = input.Bevel.y;

    float2 pos = input.Pixels.xy;

    float dx = pos.x;
    float dy = pos.y;
    float dist2 = dx*dx + dy*dy;

    float dist = sqrt(dist2);

    float3 faceNormal = float3(0, 0, 1);
    float3 bevelNormal = normalize( float3(dx / dist, dy / dist, 1) );

    // Calc blend between body and bevel.
    float bevelBlend = saturate((outerBevelRadius - dist) / (outerBevelRadius - innerBevelRadius));

    float3 normal = lerp(bevelNormal, faceNormal, bevelBlend);
    normal = normalize(normal);

    return normal;
}   // end of CalcSlantBevel()

//
// Calculate normal for rounded slant bevel.
//
float3 CalcRoundedSlantBevel(VertexShaderOutput input, float4 bodyColor)
{
    float2 pos = input.Pixels.xy;

    float dx = pos.x;
    float dy = pos.y;
    float dist2 = dx*dx + dy*dy;

    float dist = sqrt(dist2);

    float3 faceNormal = float3(0, 0, 1);
    float3 bevelNormal = float3(dx / dist, dy / dist, 0);

    // Calc blend between body and bevel.
    float bevelBlend = saturate((Radius - dist) / BevelWidth.x);

    float3 normal = lerp(bevelNormal, faceNormal, bevelBlend);
    normal = normalize(normal);

    return normal;
}   // end of CalcRounedSlantBevel()

//
// Calculate normal for round bevel.
//
float3 CalcRoundBevel(VertexShaderOutput input, float4 bodyColor)
{
    float2 pos = input.Pixels.xy;

    float dx = pos.x;
    float dy = pos.y;
    float dist2 = dx*dx + dy*dy;

    float dist = sqrt(dist2);

    float3 faceNormal = float3(0, 0, 1);
    float3 bevelNormal = float3(dx / dist, dy / dist, 0);

    // Calc blend between body and bevel.
    float bevelBlend = 1 - saturate((Radius - dist) / BevelWidth.x);
    // Make rounded.
    bevelBlend *= bevelBlend;

    float3 normal = lerp(faceNormal, bevelNormal, bevelBlend);
    normal = normalize(normal);

    return normal;
}   // end of CalcRoundBevel()

//
// Calculates the weight of the shadow at this position.
//
float CalcShadowWeight(VertexShaderOutput input)
{
    float innerRadius2 = input.Shadow.x;
    float outerRadius2 = input.Shadow.y;

    float shadowWeight = 0;

    // Set inital pos.  
    float2 pos = input.Pixels.xy;
    pos -= ShadowOffset;

    // Calc radius.
    float dx = pos.x;
    float dy = pos.y;
    float dist2 = dx*dx + dy*dy;

    shadowWeight = (outerRadius2 - dist2) / (outerRadius2 - innerRadius2);
    shadowWeight = clamp(shadowWeight, 0, 1);

    return shadowWeight;
}   // end of CalcShadowWeight();

float4 ApplyInnerShadow(VertexShaderOutput input, float4 bodyColor)
{
    float shadowWeight = CalcShadowWeight(input);

    // Invert since this is an inner shadow.
    shadowWeight = 1 - shadowWeight;
    // Apply attenuation.
    shadowWeight *= ShadowAttenuation;
    // Adjust from linear to quadratic falloff.  Give the appearence of a softer, feathered edge.
    shadowWeight *= shadowWeight;

    // Calc final result, blending shadow into color.
    // Note the careful use of bodyColor vs BodyColor.a (just for the overall alpha)
    float4 result = lerp(bodyColor.rgba, float4(0, 0, 0, BodyColor.a), shadowWeight) * bodyColor.a;

    return result;
}   // end of ApplyInnerShadow()

float4 ApplyOuterShadow(VertexShaderOutput input, float4 bodyColor)
{
    float shadowWeight = CalcShadowWeight(input);

    // Apply attenuation.
    shadowWeight *= ShadowAttenuation;
    // Adjust from linear to quadratic falloff.  Give the appearence of a softer, feathered edge.
    shadowWeight *= shadowWeight;

    // Calc final result.
    float4 result = bodyColor;
    // Calc how much shadow there is in this pixel.
    float shadowContribution = (1 - result.a) * shadowWeight;
    // Add that to the alpha which just then pulls it toward black since we're not adding any color in.
    result.a += shadowContribution;

    return result;
}   // end of ApplyInnerShadow()


//
//
// Pixel shaders
//
//

float4 OuterShadowOnlyPS(VertexShaderOutput input) : COLOR0
{
    float4 result = ApplyOuterShadow(input, float4(0, 0, 0, 0));

    return result;
}	// end of OuterShadowPS()

float4 InnerShadowOnlyPS(VertexShaderOutput input) : COLOR0
{
    // We need to calc this so we know where the edge of the shape is.
    float4 result = CalcSolidColor(input);
    result.rgb = result.a;  // Convert base color to white.
    result = ApplyInnerShadow(input, result);
    // Now take darkness of result and make the reset transparent.
    result = float4(0, 0, 0, (1-result.r) * result.a);

    return result;
}	// end of InnerShadowPS()

//
// Untextured versions
//

float4 NoShadowPS(VertexShaderOutput input) : COLOR0
{
    float4 result = CalcSolidColor(input);

    return result;
}	// end of NoShadowPS()

float4 OutlineNoShadowPS(VertexShaderOutput input) : COLOR0
{
    float4 result = CalcOutlineColor(input);

    return result;
}	// end of OutlineNoShadowPS()

float4 InnerShadowPS(VertexShaderOutput input) : COLOR0
{
    float4 result = CalcSolidColor(input);
    result = ApplyInnerShadow(input, result);

    return result;
}	// end of InnerShadowPS()

float4 OuterShadowPS(VertexShaderOutput input) : COLOR0
{
    float4 result = CalcSolidColor(input);
    result = ApplyOuterShadow(input, result);

    return result;
}	// end of OuterShadowPS()

float4 OutlineInnerShadowPS(VertexShaderOutput input) : COLOR0
{
    float4 result = CalcOutlineColor(input);
    result = ApplyInnerShadow(input, result);

    return result;
}	// end of OutlineInnerShadowPS()

float4 OutlineOuterShadowPS(VertexShaderOutput input) : COLOR0
{
    float4 result = CalcOutlineColor(input);
    result = ApplyOuterShadow(input, result);

    return result;
}	// end of OutlineOuterShadowPS()

//
// Untextured versions with bevel
//

float4 NoShadowSlantBevelPS(VertexShaderOutput input) : COLOR0
{
    float4 bodyColor = CalcSolidColor(input);
    float3 normal = CalcSlantBevel(input, bodyColor);
    float4 result = CalcDiffuse(bodyColor, normal);
    result += CalcSpecular(bodyColor.a, normal);

    return result;
}	// end of NoShadowSlantBevelPS()
float4 NoShadowRoundedSlantBevelPS(VertexShaderOutput input) : COLOR0
{
    float4 bodyColor = CalcSolidColor(input);
    float3 normal = CalcRoundedSlantBevel(input, bodyColor);
    float4 result = CalcDiffuse(bodyColor, normal);
    result += CalcSpecular(bodyColor.a, normal);

    return result;
}	// end of NoShadowRoundedSlantBevelPS()
float4 NoShadowRoundBevelPS(VertexShaderOutput input) : COLOR0
{
    float4 bodyColor = CalcSolidColor(input);
    float3 normal = CalcRoundBevel(input, bodyColor);
    float4 result = CalcDiffuse(bodyColor, normal);
    result += CalcSpecular(bodyColor.a, normal);

    return result;
}	// end of NoShadowRoundBevelPS()

float4 OutlineNoShadowSlantBevelPS(VertexShaderOutput input) : COLOR0
{
    float4 bodyColor = CalcOutlineColor(input);
    float3 normal = CalcSlantBevel(input, bodyColor);
    float4 result = CalcDiffuse(bodyColor, normal);
    result += CalcSpecular(bodyColor.a, normal);

    return result;
}	// end of OutlineNoShadowSlantBevelPS()
float4 OutlineNoShadowRoundedSlantBevelPS(VertexShaderOutput input) : COLOR0
{
    float4 bodyColor = CalcOutlineColor(input);
    float3 normal = CalcRoundedSlantBevel(input, bodyColor);
    float4 result = CalcDiffuse(bodyColor, normal);
    result += CalcSpecular(bodyColor.a, normal);

    return result;
}	// end of OutlineNoShadowRoundedSlantBevelPS()
float4 OutlineNoShadowRoundBevelPS(VertexShaderOutput input) : COLOR0
{
    float4 bodyColor = CalcOutlineColor(input);
    float3 normal = CalcRoundBevel(input, bodyColor);
    float4 result = CalcDiffuse(bodyColor, normal);
    result += CalcSpecular(bodyColor.a, normal);

    return result;
}	// end of OutlineNoShadowRoundBevelPS()

float4 InnerShadowSlantBevelPS(VertexShaderOutput input) : COLOR0
{
    float4 bodyColor = CalcSolidColor(input);
    float3 normal = CalcSlantBevel(input, bodyColor);
    float4 result = CalcDiffuse(bodyColor, normal);
    result += CalcSpecular(bodyColor.a, normal);
    result = ApplyInnerShadow(input, result);

    return result;
}	// end of InnerShadowSlantBevelPS()
float4 InnerShadowRoundedSlantBevelPS(VertexShaderOutput input) : COLOR0
{
    float4 bodyColor = CalcSolidColor(input);
    float3 normal = CalcRoundedSlantBevel(input, bodyColor);
    float4 result = CalcDiffuse(bodyColor, normal);
    result += CalcSpecular(bodyColor.a, normal);
    result = ApplyInnerShadow(input, result);

    return result;
}	// end of InnerShadowRoundedSlantBevelPS()
float4 InnerShadowRoundBevelPS(VertexShaderOutput input) : COLOR0
{
    float4 bodyColor = CalcSolidColor(input);
    float3 normal = CalcRoundBevel(input, bodyColor);
    float4 result = CalcDiffuse(bodyColor, normal);
    result += CalcSpecular(bodyColor.a, normal);
    result = ApplyInnerShadow(input, result);

    return result;
}	// end of InnerShadowRoundBevelPS()

float4 OuterShadowSlantBevelPS(VertexShaderOutput input) : COLOR0
{
    float4 bodyColor = CalcSolidColor(input);
    float3 normal = CalcSlantBevel(input, bodyColor);
    float4 result = CalcDiffuse(bodyColor, normal);
    result += CalcSpecular(bodyColor.a, normal);
    result = ApplyOuterShadow(input, result);

    return result;
}	// end of OuterShadowSlantBevelPS()
float4 OuterShadowRoundedSlantBevelPS(VertexShaderOutput input) : COLOR0
{
    float4 bodyColor = CalcSolidColor(input);
    float3 normal = CalcRoundedSlantBevel(input, bodyColor);
    float4 result = CalcDiffuse(bodyColor, normal);
    result += CalcSpecular(bodyColor.a, normal);
    result = ApplyOuterShadow(input, result);

    return result;
}	// end of OuterShadowRoundedSlantBevelPS()
float4 OuterShadowRoundBevelPS(VertexShaderOutput input) : COLOR0
{
    float4 bodyColor = CalcSolidColor(input);
    float3 normal = CalcRoundBevel(input, bodyColor);
    float4 result = CalcDiffuse(bodyColor, normal);
    result += CalcSpecular(bodyColor.a, normal);
    result = ApplyOuterShadow(input, result);

    return result;
}	// end of OuterShadowRoundBevelPS()

float4 OutlineInnerShadowSlantBevelPS(VertexShaderOutput input) : COLOR0
{
    float4 bodyColor = CalcOutlineColor(input);
    float3 normal = CalcSlantBevel(input, bodyColor);
    float4 result = CalcDiffuse(bodyColor, normal);
    result += CalcSpecular(bodyColor.a, normal);
    result = ApplyInnerShadow(input, result);

    return result;
}	// end of OutlineInnerShadowSlantBevelPS()
float4 OutlineInnerShadowRoundedSlantBevelPS(VertexShaderOutput input) : COLOR0
{
    float4 bodyColor = CalcOutlineColor(input);
    float3 normal = CalcRoundedSlantBevel(input, bodyColor);
    float4 result = CalcDiffuse(bodyColor, normal);
    result += CalcSpecular(bodyColor.a, normal);
    result = ApplyInnerShadow(input, result);

    return result;
}	// end of OutlineInnerShadowRoundedSlantBevelPS()
float4 OutlineInnerShadowRoundBevelPS(VertexShaderOutput input) : COLOR0
{
    float4 bodyColor = CalcOutlineColor(input);
    float3 normal = CalcRoundBevel(input, bodyColor);
    float4 result = CalcDiffuse(bodyColor, normal);
    result += CalcSpecular(bodyColor.a, normal);
    result = ApplyInnerShadow(input, result);

    return result;
}	// end of OutlineInnerShadowRoundBevelPS()

float4 OutlineOuterShadowSlantBevelPS(VertexShaderOutput input) : COLOR0
{
    float4 bodyColor = CalcOutlineColor(input);
    float3 normal = CalcSlantBevel(input, bodyColor);
    float4 result = CalcDiffuse(bodyColor, normal);
    result += CalcSpecular(bodyColor.a, normal);
    result = ApplyOuterShadow(input, result);

    return result;
}	// end of OutlineOuterShadowSlantBevelPS()
float4 OutlineOuterShadowRoundedSlantBevelPS(VertexShaderOutput input) : COLOR0
{
    float4 bodyColor = CalcOutlineColor(input);
    float3 normal = CalcRoundedSlantBevel(input, bodyColor);
    float4 result = CalcDiffuse(bodyColor, normal);
    result += CalcSpecular(bodyColor.a, normal);
    result = ApplyOuterShadow(input, result);

    return result;
}	// end of OutlineOuterShadowRoundedSlantBevelPS()
float4 OutlineOuterShadowRoundBevelPS(VertexShaderOutput input) : COLOR0
{
    float4 bodyColor = CalcOutlineColor(input);
    float3 normal = CalcRoundBevel(input, bodyColor);
    float4 result = CalcDiffuse(bodyColor, normal);
    result += CalcSpecular(bodyColor.a, normal);
    result = ApplyOuterShadow(input, result);

    return result;
}	// end of OutlineOuterShadowRoundBevelPS()

//
// Textured versions
//

float4 NoShadowTexturePS(VertexShaderOutput input) : COLOR0
{
    float4 result = CalcSolidColor(input);
    result = ApplyTextureColor(input, result);

    return result;
}	// end of NoShadowTexturePS()

float4 OutlineNoShadowTexturePS(VertexShaderOutput input) : COLOR0
{
    float4 result = CalcOutlineColor(input);
    result = ApplyTextureColor(input, result);

    return result;
}	// end of OutlineNoShadowTexturePS()

float4 InnerShadowTexturePS(VertexShaderOutput input) : COLOR0
{
    float4 result = CalcSolidColor(input);
    result = ApplyTextureColor(input, result);
    result = ApplyInnerShadow(input, result);

    return result;
}	// end of InnerShadowTexturePS()

float4 OuterShadowTexturePS(VertexShaderOutput input) : COLOR0
{
    float4 result = CalcSolidColor(input);
    result = ApplyTextureColor(input, result);
    result = ApplyOuterShadow(input, result);

    return result;
}	// end of OuterShadowTexturePS()

float4 OutlineInnerShadowTexturePS(VertexShaderOutput input) : COLOR0
{
    float4 result = CalcOutlineColor(input);
    result = ApplyTextureColor(input, result);
    result = ApplyInnerShadow(input, result);

    return result;
}	// end of OutlineInnerShadowTexturePS()

float4 OutlineOuterShadowTexturePS(VertexShaderOutput input) : COLOR0
{
    float4 result = CalcOutlineColor(input);
    result = ApplyTextureColor(input, result);
    result = ApplyOuterShadow(input, result);

    return result;
}	// end of OutlineOuterShadowTexturePS()


//
// Textured with bevel versions
//

float4 NoShadowTextureSlantBevelPS(VertexShaderOutput input) : COLOR0
{
    float4 bodyColor = CalcSolidColor(input);
    bodyColor = ApplyTextureColor(input, bodyColor);
    float3 normal = CalcSlantBevel(input, bodyColor);
    float4 result = CalcDiffuse(bodyColor, normal);
    result += CalcSpecular(bodyColor.a, normal);

    return result;
}	// end of NoShadowTextureSlantBevelPS()
float4 NoShadowTextureRoundedSlantBevelPS(VertexShaderOutput input) : COLOR0
{
    float4 bodyColor = CalcSolidColor(input);
    bodyColor = ApplyTextureColor(input, bodyColor);
    float3 normal = CalcRoundedSlantBevel(input, bodyColor);
    float4 result = CalcDiffuse(bodyColor, normal);
    result += CalcSpecular(bodyColor.a, normal);

    return result;
}	// end of NoShadowTextureRoundedSlantBevelPS()
float4 NoShadowTextureRoundBevelPS(VertexShaderOutput input) : COLOR0
{
    float4 bodyColor = CalcSolidColor(input);
    bodyColor = ApplyTextureColor(input, bodyColor);
    float3 normal = CalcRoundBevel(input, bodyColor);
    float4 result = CalcDiffuse(bodyColor, normal);
    result += CalcSpecular(bodyColor.a, normal);

    return result;
}	// end of NoShadowTextureRoundBevelPS()

float4 OutlineNoShadowTextureSlantBevelPS(VertexShaderOutput input) : COLOR0
{
    float4 bodyColor = CalcOutlineColor(input);
    bodyColor = ApplyTextureColor(input, bodyColor);
    float3 normal = CalcSlantBevel(input, bodyColor);
    float4 result = CalcDiffuse(bodyColor, normal);
    result += CalcSpecular(bodyColor.a, normal);

    return result;
}	// end of OutlineNoShadowTextureSlantBevelPS()
float4 OutlineNoShadowTextureRoundedSlantBevelPS(VertexShaderOutput input) : COLOR0
{
    float4 bodyColor = CalcOutlineColor(input);
    bodyColor = ApplyTextureColor(input, bodyColor);
    float3 normal = CalcRoundedSlantBevel(input, bodyColor);
    float4 result = CalcDiffuse(bodyColor, normal);
    result += CalcSpecular(bodyColor.a, normal);

    return result;
}	// end of OutlineNoShadowTextureRoundedSlantBevelPS()
float4 OutlineNoShadowTextureRoundBevelPS(VertexShaderOutput input) : COLOR0
{
    float4 bodyColor = CalcOutlineColor(input);
    bodyColor = ApplyTextureColor(input, bodyColor);
    float3 normal = CalcRoundBevel(input, bodyColor);
    float4 result = CalcDiffuse(bodyColor, normal);
    result += CalcSpecular(bodyColor.a, normal);

    return result;
}	// end of OutlineNoShadowTextureRoundBevelPS()

float4 InnerShadowTextureSlantBevelPS(VertexShaderOutput input) : COLOR0
{
    float4 bodyColor = CalcSolidColor(input);
    bodyColor = ApplyTextureColor(input, bodyColor);
    float3 normal = CalcSlantBevel(input, bodyColor);
    float4 result = CalcDiffuse(bodyColor, normal);
    result += CalcSpecular(bodyColor.a, normal);
    result = ApplyInnerShadow(input, result);

    return result;
}	// end of InnerShadowTextureSlantBevelPS()
float4 InnerShadowTextureRoundedSlantBevelPS(VertexShaderOutput input) : COLOR0
{
    float4 bodyColor = CalcSolidColor(input);
    bodyColor = ApplyTextureColor(input, bodyColor);
    float3 normal = CalcRoundedSlantBevel(input, bodyColor);
    float4 result = CalcDiffuse(bodyColor, normal);
    result += CalcSpecular(bodyColor.a, normal);
    result = ApplyInnerShadow(input, result);

    return result;
}	// end of InnerShadowTextureRoundedSlantBevelPS()
float4 InnerShadowTextureRoundBevelPS(VertexShaderOutput input) : COLOR0
{
    float4 bodyColor = CalcSolidColor(input);
    bodyColor = ApplyTextureColor(input, bodyColor);
    float3 normal = CalcRoundBevel(input, bodyColor);
    float4 result = CalcDiffuse(bodyColor, normal);
    result += CalcSpecular(bodyColor.a, normal);
    result = ApplyInnerShadow(input, result);

    return result;
}	// end of InnerShadowTextureRoundBevelPS()

float4 OuterShadowTextureSlantBevelPS(VertexShaderOutput input) : COLOR0
{
    float4 bodyColor = CalcSolidColor(input);
    bodyColor = ApplyTextureColor(input, bodyColor);
    float3 normal = CalcSlantBevel(input, bodyColor);
    float4 result = CalcDiffuse(bodyColor, normal);
    result += CalcSpecular(bodyColor.a, normal);
    result = ApplyOuterShadow(input, result);

    return result;
}	// end of OuterShadowTextureSlantBevelPS()
float4 OuterShadowTextureRoundedSlantBevelPS(VertexShaderOutput input) : COLOR0
{
    float4 bodyColor = CalcSolidColor(input);
    bodyColor = ApplyTextureColor(input, bodyColor);
    float3 normal = CalcRoundedSlantBevel(input, bodyColor);
    float4 result = CalcDiffuse(bodyColor, normal);
    result += CalcSpecular(bodyColor.a, normal);
    result = ApplyOuterShadow(input, result);

    return result;
}	// end of OuterShadowTextureRoundedSlantBevelPS()
float4 OuterShadowTextureRoundBevelPS(VertexShaderOutput input) : COLOR0
{
    float4 bodyColor = CalcSolidColor(input);
    bodyColor = ApplyTextureColor(input, bodyColor);
    float3 normal = CalcRoundBevel(input, bodyColor);
    float4 result = CalcDiffuse(bodyColor, normal);
    result += CalcSpecular(bodyColor.a, normal);
    result = ApplyOuterShadow(input, result);

    return result;
}	// end of OuterShadowTextureRoundBevelPS()

float4 OutlineInnerShadowTextureSlantBevelPS(VertexShaderOutput input) : COLOR0
{
    float4 bodyColor = CalcOutlineColor(input);
    bodyColor = ApplyTextureColor(input, bodyColor);
    float3 normal = CalcSlantBevel(input, bodyColor);
    float4 result = CalcDiffuse(bodyColor, normal);
    result += CalcSpecular(bodyColor.a, normal);
    result = ApplyInnerShadow(input, result);

    return result;
}	// end of OutlineInnerShadowTextureSlantBevelPS()
float4 OutlineInnerShadowTextureRoundedSlantBevelPS(VertexShaderOutput input) : COLOR0
{
    float4 bodyColor = CalcOutlineColor(input);
    bodyColor = ApplyTextureColor(input, bodyColor);
    float3 normal = CalcRoundedSlantBevel(input, bodyColor);
    float4 result = CalcDiffuse(bodyColor, normal);
    result += CalcSpecular(bodyColor.a, normal);
    result = ApplyInnerShadow(input, result);

    return result;
}	// end of OutlineInnerShadowTextureRoundedSlantBevelPS()
float4 OutlineInnerShadowTextureRoundBevelPS(VertexShaderOutput input) : COLOR0
{
    float4 bodyColor = CalcOutlineColor(input);
    bodyColor = ApplyTextureColor(input, bodyColor);
    float3 normal = CalcRoundBevel(input, bodyColor);
    float4 result = CalcDiffuse(bodyColor, normal);
    result += CalcSpecular(bodyColor.a, normal);
    result = ApplyInnerShadow(input, result);

    return result;
}	// end of OutlineInnerShadowTextureRoundBevelPS()

float4 OutlineOuterShadowTextureSlantBevelPS(VertexShaderOutput input) : COLOR0
{
    float4 bodyColor = CalcOutlineColor(input);
    bodyColor = ApplyTextureColor(input, bodyColor);
    float3 normal = CalcSlantBevel(input, bodyColor);
    float4 result = CalcDiffuse(bodyColor, normal);
    result += CalcSpecular(bodyColor.a, normal);
    result = ApplyOuterShadow(input, result);

    return result;
}	// end of OutlineOuterShadowTextureSlantBevelPS()
float4 OutlineOuterShadowTextureRoundedSlantBevelPS(VertexShaderOutput input) : COLOR0
{
    float4 bodyColor = CalcOutlineColor(input);
    bodyColor = ApplyTextureColor(input, bodyColor);
    float3 normal = CalcRoundedSlantBevel(input, bodyColor);
    float4 result = CalcDiffuse(bodyColor, normal);
    result += CalcSpecular(bodyColor.a, normal);
    result = ApplyOuterShadow(input, result);

    return result;
}	// end of OutlineOuterShadowTextureRoundedSlantBevelPS()
float4 OutlineOuterShadowTextureRoundBevelPS(VertexShaderOutput input) : COLOR0
{
    float4 bodyColor = CalcOutlineColor(input);
    bodyColor = ApplyTextureColor(input, bodyColor);
    float3 normal = CalcRoundBevel(input, bodyColor);
    float4 result = CalcDiffuse(bodyColor, normal);
    result += CalcSpecular(bodyColor.a, normal);
    result = ApplyOuterShadow(input, result);

    return result;
}	// end of OutlineOuterShadowTextureRoundBevelPS()
