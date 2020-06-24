
//
// Pie Slice shaders
//

// The world view and projection matrices
float4x4 WorldViewProjMatrix;

float2 Center;          // Center of whole menu.
float3 InnerRadius;     // x: Inner radius for menu slice. yz: minus and plus EdgeBlend
float3 OuterRadius;     // x: Outer radius for menu slice.  (not counting stacked layers) yz: minus and plus EdgeBlend
float2 EdgeNormal0;     // Edge normal for one side of the slice.
float2 EdgeNormal1;     // Other edge normal.
float2 EdgeIntersect;   // Point where edges intersect.

float4 BodyColor;               
float2 EdgeBlend;           // x: Fractional pixel amount for antiliased edge, y: 2X the amount

float4 OutlineColor;
float3 OutlineWidth;        // x: width, y: width - edgeBlend, z: width + edgeBlend

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
    float2 UV           : TEXCOORD0;
    float2 Pixels	    : TEXCOORD1;
};

//
// Standard VS used for all options.
//
VertexShaderOutput VS(VertexShaderInput input)
{
    VertexShaderOutput output;

    output.Position = mul(float4(input.Position, 0, 1), WorldViewProjMatrix);

    output.UV = input.UV;
    output.Pixels = input.Position;

    // We don't need all of these for all versions.  Think about
    // breaking this VS up into specific versions for each technique.
    /*
    CalcRadiusVS(output);
    CalcOutlineVS(output);
    CalcShadowVS(output);
    CalcBevelVS(output);
    */

    return output;
}   // end of VS()

//
// For the given input, calculates the u and v distances from the nearest edge.
// We give the input position explicitely so this can be used for shadows as
// well as solid areas.
//
// In the result x: distance to edge0
//               y: distance to edge1
//               z: distance to inner radius
//               w: distance to outer radius
//
// Positive distances are inside the shape.
// Negative distances are outside the shape.
//
float4 CalcEdgeDistances(VertexShaderOutput input, float2 pixels)
{
    float4 result;

    float2 deltaCenter = pixels - Center;
    float2 deltaEdgeIntersect = pixels - EdgeIntersect;
    float radius = length(deltaCenter);

    // Inner radius.
    result.z = radius - InnerRadius.x;
    // Outer radius
    result.w = OuterRadius.x - radius;

    // Edge0
    result.x = dot(deltaEdgeIntersect, EdgeNormal0);
    // Egde1 
    result.y = dot(deltaEdgeIntersect, EdgeNormal1);

    return result;
}   // end of CalcEdgeDistances()

float4 CalcSolidColor(VertexShaderOutput input, float4 distances)
{
    float4 result = BodyColor;

    // Find min distance.
    float dist = min(distances.x, distances.y);
    dist = min(distances.z, dist);
    dist = min(distances.w, dist);

    float alpha = saturate((dist + EdgeBlend.x) / EdgeBlend.y);
    result *= alpha;

    return result;
}   // end of CalcSolidColor()

float4 CalcOutlineColor(VertexShaderOutput input, float4 distances)
{
    // Find min distance.
    float minDistXY = min(distances.x, distances.y);
    float minDistZW = min(distances.z, distances.w);
    float dist = min(minDistXY, minDistZW);

    // Calc outline vs body.
    float outlineBodyBlend = saturate((dist - OutlineWidth.x + EdgeBlend.x) / EdgeBlend.y);
    float4 result = lerp(OutlineColor, BodyColor, outlineBodyBlend);

    // Calc antialiased edge.
    float alpha = saturate((dist + EdgeBlend.x) / EdgeBlend.y);
    result *= alpha;

    return result;
}   // end of CalcSolidColor()

//
// Gets the texture color, blends it with the bodyColor and returns
// the result.
//
// Blending is done assume texture is pre-multiplied.
//
float4 ApplyTextureColor(VertexShaderOutput input, float4 bodyColor)
{
    float4 textureColor = tex2D(DiffuseTextureSampler, input.UV);

#if HiDef
	// Clamp to full transparent border color.  We have to do it like this 
	// since the address mode has been deprecated.
	if (input.UV.x <= 0 || input.UV.x >= 1 || input.UV.y <= 0 || input.UV.y >= 1)
	{
		textureColor = float4(0, 0, 0, 0);
	}
#endif

    float4 result;
    result.rgb = textureColor.rgb * bodyColor.a + (1 - textureColor.a) * bodyColor.rgb;
    result.a = bodyColor.a;

    return result;
}   // end of ApplyTextureColor()

// 
// Calculates a slant bevel and applies the lighting.
//
float3 CalcSlantBevel(VertexShaderOutput input, float4 distances, float4 bodyColor)
{
    // Find min distance and calc overall alpha for shape.
    float minDistXY = min(distances.x, distances.y);
    float minDistZW = min(distances.z, distances.w);
    float dist = min(minDistXY, minDistZW);

    // Calc alpha for whole shape.  This is the outer edge with sharp corners.
    float alpha = saturate((dist + EdgeBlend.x) / EdgeBlend.y);

    // Face Normal.
    float3 faceNormal = float3(0, 0, 1);

    // Calc bevel UV on inner and outer side.
    float2 deltaCenter = input.Pixels - Center;
    float radius = length(deltaCenter);
    deltaCenter /= radius;

    // Calc radiusNormal.  This is the normal on the 
    // inner or outer edge.
    float3 radiusNormal = float3(deltaCenter.x, deltaCenter.y, 0);
    if(distances.z < distances.w)
    {
        radiusNormal = -radiusNormal;
    }
    radiusNormal.z = 1; // Always face the viewer.

    // Start with face normal.
    float3 normal = faceNormal;

    // Calc weight for radius normal.
    float radiusNormalWeight = 0;
    float unsaturatedRadiusNormalWeight = 0;
    if (minDistZW < BevelWidth.x + EdgeBlend.x)
    {
        unsaturatedRadiusNormalWeight = (minDistZW - BevelWidth.x + EdgeBlend.x) / EdgeBlend.y;
        radiusNormalWeight = 1 - saturate(unsaturatedRadiusNormalWeight);
    }

    // Calc edge normal.
    float3 edgeNormal = float3(-EdgeNormal0.x, -EdgeNormal0.y, 1);
    if(distances.x > distances.y)
    {
        edgeNormal.xy = -EdgeNormal1;
    }

    // Calc weight for edge normal
    float edgeNormalWeight = 0;
    float unsaturatedEdgeNormalWeight = 0;
    if (minDistXY < BevelWidth.x + EdgeBlend.x)
    {
        unsaturatedEdgeNormalWeight = (minDistXY - BevelWidth.x + EdgeBlend.x) / EdgeBlend.y;
        edgeNormalWeight = 1 - saturate(unsaturatedEdgeNormalWeight);
    }

    // Blend normals.
    float bevelWeight = max(radiusNormalWeight, edgeNormalWeight);

    // Calc weight between radius and edge normals.   pixel blend hard coded...
    float blendWidth = 1.5;     // Bigger number gives softer edge.  This is just big enough so
                                // that specular right on edge looks smooth rather than ropey.
    // When minDistXY - minDistZW == 0 we're right on the edge between the two normals.  We
    // then add half of our blend width, divide by the full blend width and then saturate so
    // we end up with a nice weight in the 0..1 range.
    float w = saturate((minDistXY - minDistZW + 0.5 * blendWidth) / blendWidth);
    float3 bevelNormal = lerp(edgeNormal, radiusNormal, w);

    // Get final normal.
    normal = lerp(faceNormal, bevelNormal, bevelWeight);
    normal = normalize(normal);

    return normal;
}   // end of CalcSlantBevel()

// 
// Calculates a rounded-slant bevel.
//
float3 CalcRoundedSlantBevel(VertexShaderOutput input, float4 distances, float4 bodyColor)
{
    // Find min distance and calc overall alpha for shape.
    float minDistXY = min(distances.x, distances.y);
    float minDistZW = min(distances.z, distances.w);
    float dist = min(minDistXY, minDistZW);

    // Calc alpha for whole shape.  This is the outer edge with sharp corners.
    float alpha = saturate((dist + EdgeBlend.x) / EdgeBlend.y);

    // Face Normal.
    float3 faceNormal = float3(0, 0, 1);

    // Calc bevel UV on inner and outer side.
    float2 deltaCenter = input.Pixels - Center;
    float radius = length(deltaCenter);
    deltaCenter /= radius;

    // Calc radiusNormal.  This is the normal on the 
    // inner or outer edge.
    float3 radiusNormal = float3(deltaCenter.x, deltaCenter.y, 0);
    if(distances.z < distances.w)
    {
        radiusNormal = -radiusNormal;
    }
    radiusNormal.z = 0; // Always face the viewer.

    // Start with face normal.
    float3 normal = faceNormal;

    // Calc weight for radius normal.
    float radiusNormalWeight = 0;
    if (minDistZW < BevelWidth.x + EdgeBlend.x)
    {
        radiusNormalWeight = 1 - minDistZW / BevelWidth.x;
    }

    // Calc edge normal.
    float3 edgeNormal = float3(-EdgeNormal0.x, -EdgeNormal0.y, 0);
    if(distances.x > distances.y)
    {
        edgeNormal.xy = -EdgeNormal1;
    }

    // Calc weight for edge normal
    float edgeNormalWeight = 0;
    if (minDistXY < BevelWidth.x + EdgeBlend.x)
    {
        edgeNormalWeight = 1 - minDistXY / BevelWidth.x;
    }

    // Blend normals.
    float bevelWeight = max(radiusNormalWeight, edgeNormalWeight);
    float3 bevelNormal = radiusNormal;

    // Calc weight between radius and edge normals.   pixel blend hard coded...
    float blendWidth = 2.5;     // Bigger number gives softer edge.  This is just big enough so
                                // that specular right on edge looks smooth rather than ropey.
    // When minDistXY - minDistZW == 0 we're right on the edge between the two normals.  We
    // then add half of our blend width, divide by the full blend width and then saturate so
    // we end up with a nice weight in the 0..1 range.
    float w = saturate((minDistXY - minDistZW + 0.5 * blendWidth) / blendWidth);
    bevelNormal = lerp(edgeNormal, radiusNormal, w);

    // Get final normal.
    normal = lerp(faceNormal, bevelNormal, bevelWeight);

    normal = normalize(normal);

    return normal;
}   // end of CalcRoundedSlantBevel()

// 
// Calculates a rounded bevel and applies the lighting.
//
float3 CalcRoundBevel(VertexShaderOutput input, float4 distances, float4 bodyColor)
{
    // Find min distance and calc overall alpha for shape.
    float minDistXY = min(distances.x, distances.y);
    float minDistZW = min(distances.z, distances.w);
    float dist = min(minDistXY, minDistZW);

    // Calc alpha for whole shape.  This is the outer edge with sharp corners.
    float alpha = saturate((dist + EdgeBlend.x) / EdgeBlend.y);

    // Face Normal.
    float3 faceNormal = float3(0, 0, 1);

    // Calc bevel UV on inner and outer side.
    float2 deltaCenter = input.Pixels - Center;
    float radius = length(deltaCenter);
    deltaCenter /= radius;

    // Calc radiusNormal.  This is the normal on the 
    // inner or outer edge.
    float3 radiusNormal = float3(deltaCenter.x, deltaCenter.y, 0);
    if(distances.z < distances.w)
    {
        radiusNormal = -radiusNormal;
    }
    radiusNormal.z = 0; // Always face the viewer.

    // Start with face normal.
    float3 normal = faceNormal;

    // Calc weight for radius normal.
    float radiusNormalWeight = 0;
    if (minDistZW < BevelWidth.x + EdgeBlend.x)
    {
        radiusNormalWeight = 1 - minDistZW / BevelWidth.x;
        radiusNormalWeight *= radiusNormalWeight;   // Make full round.
    }

    // Calc edge normal.
    float3 edgeNormal = float3(-EdgeNormal0.x, -EdgeNormal0.y, 0);
    if(distances.x > distances.y)
    {
        edgeNormal.xy = -EdgeNormal1;
    }

    // Calc weight for edge normal
    float edgeNormalWeight = 0;
    if (minDistXY < BevelWidth.x + EdgeBlend.x)
    {
        edgeNormalWeight = 1 - minDistXY / BevelWidth.x;
        edgeNormalWeight *= edgeNormalWeight;   // Make full round.
    }

    // Blend normals.
    float bevelWeight = max(radiusNormalWeight, edgeNormalWeight);
    float3 bevelNormal = radiusNormal;

    // Calc weight between radius and edge normals.   pixel blend hard coded...
    float blendWidth = 2.5;     // Bigger number gives softer edge.  This is just big enough so
                                // that specular right on edge looks smooth rather than ropey.
    // When minDistXY - minDistZW == 0 we're right on the edge between the two normals.  We
    // then add half of our blend width, divide by the full blend width and then saturate so
    // we end up with a nice weight in the 0..1 range.
    float w = saturate((minDistXY - minDistZW + 0.5 * blendWidth) / blendWidth);
    bevelNormal = lerp(edgeNormal, radiusNormal, w);

    // Get final normal.
    normal = lerp(faceNormal, bevelNormal, bevelWeight);

    normal = normalize(normal);

    return normal;   
}   // end of CalcRoundBevel()

//
// Calculates the weight of the shadow at this position.
//
float CalcShadowWeight(VertexShaderOutput input)
{
    float4 distances = CalcEdgeDistances(input, input.Pixels - ShadowOffset);

    // "Normalize" distances into transsition range.
    distances = saturate((distances + ShadowSize + EdgeBlend.x) / (2 * ShadowSize + EdgeBlend.y));

    // To get final shadow weight, multiply all weights together.  
    // This has the effect of rounding off the corners.  For sharp
    // corners, use min value.
    float shadowWeight = distances.x * distances.y * distances.z * distances.w;

    return shadowWeight;
}   // end of CalcShadowWeight()

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
// Pixel Shaders
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
    float4 distances = CalcEdgeDistances(input, input.Pixels);    
    float4 result = CalcSolidColor(input, distances);
    result.rgb = result.a;  // Convert base color to white.
    result = ApplyInnerShadow(input, result);
    // Now take darkness of result and make the reset transparent.
    result = float4(0, 0, 0, (1-result.r) * result.a);

    return result;
}	// end of InnerShadowPS()

float4 SpecularOnlyPS(VertexShaderOutput input) : COLOR0
{
    float4 distances = CalcEdgeDistances(input, input.Pixels);    
    float4 result = CalcSolidColor(input, distances);

    return result;
}

float4 NoShadowPS(VertexShaderOutput input) : COLOR0
{
    float4 distances = CalcEdgeDistances(input, input.Pixels);    
    float4 result = CalcSolidColor(input, distances);

    return result;
}	// end of NoShadowPS()

float4 OutlineNoShadowPS(VertexShaderOutput input) : COLOR0
{
    float4 distances = CalcEdgeDistances(input, input.Pixels);    
    float4 result = CalcOutlineColor(input, distances);

    return result;
}	// end of OutlineNoShadowPS()

//
// Bevel, no texture, no outline
//

float4 NoShadowSlantBevelPS(VertexShaderOutput input) : COLOR0
{
    float4 distances = CalcEdgeDistances(input, input.Pixels);
    float4 bodyColor = CalcSolidColor(input, distances);  
    float3 normal = CalcSlantBevel(input, distances, bodyColor);
    float4 result = CalcDiffuse(bodyColor, normal);
#if HiDef
    result += CalcSpecular(bodyColor.a, normal);
#endif
    return result;
}	// end of NoShadowSlantBevelPS()

float4 NoShadowRoundedSlantBevelPS(VertexShaderOutput input) : COLOR0
{
    float4 distances = CalcEdgeDistances(input, input.Pixels);    
    float4 bodyColor = CalcSolidColor(input, distances);  
    float3 normal = CalcRoundedSlantBevel(input, distances, bodyColor);
    float4 result = CalcDiffuse(bodyColor, normal);
#if HiDef
    result += CalcSpecular(bodyColor.a, normal);
#endif

    return result;
}	// end of NoShadowRoundedSlantBevelPS()

float4 NoShadowRoundBevelPS(VertexShaderOutput input) : COLOR0
{
    float4 distances = CalcEdgeDistances(input, input.Pixels);    
    float4 bodyColor = CalcSolidColor(input, distances);  
    float3 normal = CalcRoundBevel(input, distances, bodyColor);
    float4 result = CalcDiffuse(bodyColor, normal);
#if HiDef
    result += CalcSpecular(bodyColor.a, normal);
#endif

    return result;
}	// end of NoShadowRoundBevelPS()

float4 SpecularOnlyNoShadowSlantBevelPS(VertexShaderOutput input) : COLOR0
{
    float4 distances = CalcEdgeDistances(input, input.Pixels);
    float4 bodyColor = CalcSolidColor(input, distances);  
    float3 normal = CalcSlantBevel(input, distances, bodyColor);
    float4 result = CalcSpecular(bodyColor.a, normal);

    return result;
}	// end of SpecularOnlyNoShadowSlantBevelPS()

float4 SpecularOnlyNoShadowRoundedSlantBevelPS(VertexShaderOutput input) : COLOR0
{
    float4 distances = CalcEdgeDistances(input, input.Pixels);
    float4 bodyColor = CalcSolidColor(input, distances);  
    float3 normal = CalcRoundedSlantBevel(input, distances, bodyColor);
    float4 result = CalcSpecular(bodyColor.a, normal);

    return result;
}	// end of SpecularOnlyNoShadowRoundedSlantBevelPS()

float4 SpecularOnlyNoShadowRoundBevelPS(VertexShaderOutput input) : COLOR0
{
    float4 distances = CalcEdgeDistances(input, input.Pixels);
    float4 bodyColor = CalcSolidColor(input, distances);  
    float3 normal = CalcRoundBevel(input, distances, bodyColor);
    float4 result = CalcSpecular(bodyColor.a, normal);

    return result;
}	// end of SpecularOnlyNoShadowRoundBevelPS()

//
// Textured, no bevel.
//

float4 NoShadowTexturePS(VertexShaderOutput input) : COLOR0
{
    float4 distances = CalcEdgeDistances(input, input.Pixels);    
    float4 result = CalcSolidColor(input, distances);
    result = ApplyTextureColor(input, result);

    return result;
}	// end of NoShadowTexturePS()

float4 OutlineNoShadowTexturePS(VertexShaderOutput input) : COLOR0
{
    float4 distances = CalcEdgeDistances(input, input.Pixels);    
    float4 result = CalcOutlineColor(input, distances);
    result = ApplyTextureColor(input, result);

    return result;
}	// end of OutlineNoShadowTexturePS()

//
// Bevel with outline, no texture
//

float4 OutlineNoShadowSlantBevelPS(VertexShaderOutput input) : COLOR0
{
    float4 distances = CalcEdgeDistances(input, input.Pixels);
    float4 bodyColor = CalcOutlineColor(input, distances);  
    float3 normal = CalcSlantBevel(input, distances, bodyColor);
    float4 result = CalcDiffuse(bodyColor, normal);
#if HiDef
    result += CalcSpecular(bodyColor.a, normal);
#endif

    return result;
}   // end of OutlineNoShadowSlantBevelPS()

float4 OutlineNoShadowRoundedSlantBevelPS(VertexShaderOutput input) : COLOR0
{
    float4 distances = CalcEdgeDistances(input, input.Pixels);
    float4 bodyColor = CalcOutlineColor(input, distances);  
    float3 normal = CalcRoundedSlantBevel(input, distances, bodyColor);
    float4 result = CalcDiffuse(bodyColor, normal);
#if HiDef
    result += CalcSpecular(bodyColor.a, normal);
#endif

    return result;
}   // end of OutlineNoShadowRounedSlantBevelPS()

float4 OutlineNoShadowRoundBevelPS(VertexShaderOutput input) : COLOR0
{
    float4 distances = CalcEdgeDistances(input, input.Pixels);
    float4 bodyColor = CalcOutlineColor(input, distances);  
    float3 normal = CalcRoundBevel(input, distances, bodyColor);
    float4 result = CalcDiffuse(bodyColor, normal);
#if HiDef
    result += CalcSpecular(bodyColor.a, normal);
#endif

    return result;
}   // end of OutlineNoShadowRoundBevelPS()

//
// Bevel with texture, no outline
//

float4 NoShadowTextureSlantBevelPS(VertexShaderOutput input) : COLOR0
{
    float4 distances = CalcEdgeDistances(input, input.Pixels);
    float4 bodyColor = CalcSolidColor(input, distances);  
    bodyColor = ApplyTextureColor(input, bodyColor);
    float3 normal = CalcSlantBevel(input, distances, bodyColor);
    float4 result = CalcDiffuse(bodyColor, normal);
#if HiDef
    result += CalcSpecular(bodyColor.a, normal);
#endif

    return result;
}	// end of NoShadowTextureSlantBevelPS()

float4 NoShadowTextureRoundedSlantBevelPS(VertexShaderOutput input) : COLOR0
{
    float4 distances = CalcEdgeDistances(input, input.Pixels);    
    float4 bodyColor = CalcSolidColor(input, distances);  
    bodyColor = ApplyTextureColor(input, bodyColor);
    float3 normal = CalcRoundedSlantBevel(input, distances, bodyColor);
    float4 result = CalcDiffuse(bodyColor, normal);
#if HiDef
    result += CalcSpecular(bodyColor.a, normal);
#endif

    return result;
}	// end of NoShadowTextureRoundedSlantBevelPS()

float4 NoShadowTextureRoundBevelPS(VertexShaderOutput input) : COLOR0
{
    float4 distances = CalcEdgeDistances(input, input.Pixels);    
    float4 bodyColor = CalcSolidColor(input, distances);  
    bodyColor = ApplyTextureColor(input, bodyColor);
    float3 normal = CalcRoundBevel(input, distances, bodyColor);
    float4 result = CalcDiffuse(bodyColor, normal);
#if HiDef
    result += CalcSpecular(bodyColor.a, normal);
#endif

    return result;
}	// end of NoShadowTextureRoundBevelPS()

//
// Bevel with texture and outline
//

float4 OutlineNoShadowTextureSlantBevelPS(VertexShaderOutput input) : COLOR0
{
    float4 distances = CalcEdgeDistances(input, input.Pixels);
    float4 bodyColor = CalcOutlineColor(input, distances);  
    bodyColor = ApplyTextureColor(input, bodyColor);
    float3 normal = CalcSlantBevel(input, distances, bodyColor);
    float4 result = CalcDiffuse(bodyColor, normal);
#if HiDef
    result += CalcSpecular(bodyColor.a, normal);
#endif

    return result;
}	// end of OutlineNoShadowTextureSlantBevelPS()

float4 OutlineNoShadowTextureRoundedSlantBevelPS(VertexShaderOutput input) : COLOR0
{
    float4 distances = CalcEdgeDistances(input, input.Pixels);    
    float4 bodyColor = CalcOutlineColor(input, distances);  
    bodyColor = ApplyTextureColor(input, bodyColor);
    float3 normal = CalcRoundedSlantBevel(input, distances, bodyColor);
    float4 result = CalcDiffuse(bodyColor, normal);
#if HiDef
    result += CalcSpecular(bodyColor.a, normal);
#endif

    return result;
}	// end of OutlineNoShadowTextureRoundedSlantBevelPS()

float4 OutlineNoShadowTextureRoundBevelPS(VertexShaderOutput input) : COLOR0
{
    float4 distances = CalcEdgeDistances(input, input.Pixels);    
    float4 bodyColor = CalcOutlineColor(input, distances);  
    bodyColor = ApplyTextureColor(input, bodyColor);
    float3 normal = CalcRoundBevel(input, distances, bodyColor);
    float4 result = CalcDiffuse(bodyColor, normal);
#if HiDef
    result += CalcSpecular(bodyColor.a, normal);
#endif

    return result;
}	// end of OutlineNoShadowTextureRoundBevelPS()

