
//
// RoundedRect shaders
//
// Basically all these variants including simple, outline and shadow are just
// variations on the same algorithm for rendering a rounded rect.  We take the
// input pixel position and "collapse" the rect down to a disk.  Then we calc
// the radius and find the right colors and edges.  Each edge has an inner and
// outer radius.  In between these values we blend to produce smooth edges.
// If the radii are close (1.5 pixels) then this produces a nice antialiased edge.
// If the radii are further apart we can use this for soft shadows.
//
// Note "Alt" shaders are used for the the following cases:
// -- shadows are rendered and ShadowSize > CornerRadius.
// -- outlining is used and OutlineWidth > CornerRadius
//

// The world view and projection matrices
float4x4 WorldViewProjMatrix;

float2 Size;                // Size in pixels of shape not counting outer shadow.
float CornerRadius;
float EdgeBlend;            // Fractional pixel amount for antiliased edge.
float4 BodyColor;

float4 OutlineColor;
float OutlineWidth;  

bool TwoToneHorizontalSplit;// If true, split the shape horizontally, else split vertically.
float4 TwoToneSecondColor;	// Second color for two-tone.  BodyColor is used for the upper/left part.
float TwoToneSplitPosition;	// Position where split between colors happens.       

float2 ShadowOffset;        // Offset relative to shape in pixels.
float ShadowSize;           // Pixel amount for blended edge of shadow. (actually half of it)
float ShadowAttenuation;    // 1 is full strength shadow, <1 is softer version.
float ShadowRadius;         // Radius used for shadow shape.  This is most max of ShadowSize and CornerRadius.

float3 BevelWidth;          // x: width, y: width - edgeBlend, z: width + edgeBlend

texture DiffuseTexture;

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
    float2 Position		: POSITION0;
    float2 UV           : TEXCOORD0;    
    float2 Coords		: TEXCOORD1;	
};

struct VertexShaderOutput
{
    float4 Position     : POSITION0;
    float2 UV           : TEXCOORD0;
    float2 Coords	    : TEXCOORD1;		// xy based pixel coords with 0,0 at center of shape
    float4 Radius	    : TEXCOORD2;		// innerRadius2, outerRadius2, width, height
    float2 Outline      : TEXCOORD3;        // innerOutlineRadius2, outerOutlineRadius2
    float4 OutlineAlt   : TEXCOORD4;        // halfWidthInner, halfHeightInner, halfWidthOuter, halfHeightOuter (inner and outer bounderies for the BodyColor section of the rect)
    float4 Shadow       : TEXCOORD5;        // innerShadowRadius2, outerShadowRadius2, widthShadow, heightShadow
    float2 Bevel        : TEXCOORD6;        // innerBevelRadius, outerBevelRadius
};


//
// Shared functions
//

//
// Vertex shader methods.
//

void CalcCornersVS(inout VertexShaderOutput output)
{
    // Precalc corner radius limits.  This is the edge between the body and the background.
    float innerRadius = max(CornerRadius - EdgeBlend, 0);
    float innerRadius2 = innerRadius * innerRadius;
    output.Radius.x = innerRadius2;

    float outerRadius = (CornerRadius + EdgeBlend);
    float outerRadius2 = outerRadius * outerRadius;
    output.Radius.y = outerRadius2;

    float width = Size.x - 2 * CornerRadius;
    float height = Size.y - 2 * CornerRadius;
    output.Radius.z = width;
    output.Radius.w = height;
}

void CalcOutlineVS(inout VertexShaderOutput output)
{
    // Precalc outline radius limits.  This is the edge between the outline and body.
    float innerOutlineRadius = max(CornerRadius - OutlineWidth - EdgeBlend, 0);
    float innerOutlineRadius2 = innerOutlineRadius * innerOutlineRadius;
    output.Outline.x = innerOutlineRadius2;

    float outerOutlineRadius = max(CornerRadius - OutlineWidth + EdgeBlend, 0);
    float outerOutlineRadius2 = outerOutlineRadius * outerOutlineRadius;
    output.Outline.y = outerOutlineRadius2;

    float halfBodyWidth = 0.5 * (Size.x - 2 * OutlineWidth);
    float halfBodyHeight = 0.5 * (Size.y - 2 * OutlineWidth);
    output.OutlineAlt.x = halfBodyWidth - EdgeBlend;
    output.OutlineAlt.y = halfBodyHeight - EdgeBlend;
    output.OutlineAlt.z = halfBodyWidth + EdgeBlend;
    output.OutlineAlt.w = halfBodyHeight + EdgeBlend;
}

void CalcShadowVS(inout VertexShaderOutput output)
{
    // Precalc Shadow radius limits.  This is the inner and outer radii where
    // the shadow transitions from full to nothing.
    float outerShadowRadius = max(ShadowRadius + ShadowSize + EdgeBlend, 0);
    float outerShadowRadius2 = outerShadowRadius * outerShadowRadius;
    output.Shadow.y = outerShadowRadius2;

    float innerShadowRadius = max(ShadowRadius - ShadowSize - EdgeBlend, 0);
    float innerShadowRadius2 = innerShadowRadius * innerShadowRadius;
    output.Shadow.x = innerShadowRadius2;

    // We've already got these values in the cornerRadius output.
    // Use this to calc the width and height needed for version
    // where CornerRadius < ShadowSize.
    float widthShadow = Size.x - 2 * ShadowRadius;
    float heightShadow = Size.y - 2 * ShadowRadius;
    output.Shadow.z = widthShadow;
    output.Shadow.w = heightShadow;

}   // end of CalcShadowVS()

void CalcBevelVS(inout VertexShaderOutput output)
{
    // Precalc outline radius limits.  This is the edge between the flat surface and the bevel.
    float innerBevelRadius = max(CornerRadius - BevelWidth - EdgeBlend, 0);
    output.Bevel.x = innerBevelRadius;

    float outerBevelRadius = max(CornerRadius - BevelWidth + EdgeBlend, 0);
    output.Bevel.y = outerBevelRadius;
}   // end of CalcOutlineVS()

//
// Shadow PS methods
//

//
// Calculates the weight of the shadow at this position.
// Normal version where CornerRadius >= ShadowSize.
// Shadow shape follows curve of rounded rect.
//
float CalcShadowWeight(VertexShaderOutput input)
{
    float innerRadius2 = input.Shadow.x;
    float outerRadius2 = input.Shadow.y;
    float width = input.Shadow.z;
    float height = input.Shadow.w;

    float shadowWeight = 0;

    // Normal version where CornerRadius >= ShadowSize
    // Set inital pos.  
    float2 pos = input.Coords.xy;
    pos -= ShadowOffset;

    float radius = ShadowRadius;
    if (pos.x > radius)
    {
        pos.x = max(pos.x - width, radius);
    }
    if (pos.y > radius)
    {
        pos.y = max(pos.y - height, radius);
    }

    // Calc normal radius.
    float dx = pos.x - radius;
    float dy = pos.y - radius;
    float dist2 = dx*dx + dy*dy;

    shadowWeight = (outerRadius2 - dist2) / (outerRadius2 - innerRadius2);
    shadowWeight = clamp(shadowWeight, 0, 1);

    shadowWeight *= ShadowAttenuation;

    return shadowWeight;
}   // end of CalcShadowWeight();

//
// Main shape PS method.
//

float4 CalcSolidColor(VertexShaderOutput input, out float dist2, out float dx, out float dy)
{
    float innerRadius2 = input.Radius.x;
    float outerRadius2 = input.Radius.y;
    float width = input.Radius.z;
    float height = input.Radius.w;

    // Cal shape of rounded rect.
    float2 pos = input.Coords.xy;

    if (pos.x > CornerRadius)
    {
        pos.x = max(pos.x - width, CornerRadius);
    }
    if (pos.y > CornerRadius)
    {
        pos.y = max(pos.y - height, CornerRadius);
    }

    dx = pos.x - CornerRadius;
    dy = pos.y - CornerRadius;
    dist2 = dx*dx + dy*dy;

    // Calc blended edge.
    float alpha = (outerRadius2 - dist2) / (outerRadius2 - innerRadius2);
    alpha = clamp(alpha, 0, 1);

    float4 result = BodyColor * alpha;

    return result;
}   // end of CalcSolidColor()

float4 CalcTwoToneColor(VertexShaderOutput input, out float dist2, out float dx, out float dy)
{
    float innerRadius2 = input.Radius.x;
    float outerRadius2 = input.Radius.y;
    float width = input.Radius.z;
    float height = input.Radius.w;

    // Cal shape of rounded rect.
    float2 pos = input.Coords.xy;

    if (pos.x > CornerRadius)
    {
        pos.x = max(pos.x - width, CornerRadius);
    }
    if (pos.y > CornerRadius)
    {
        pos.y = max(pos.y - height, CornerRadius);
    }

    dx = pos.x - CornerRadius;
    dy = pos.y - CornerRadius;
    dist2 = dx*dx + dy*dy;

    // Calc blended edge.
    float alpha = (outerRadius2 - dist2) / (outerRadius2 - innerRadius2);
    alpha = clamp(alpha, 0, 1);

    float4 result = BodyColor;
	if (TwoToneHorizontalSplit)
	{
		// Horizontal.
		if (input.Coords.xy.y > TwoToneSplitPosition)
		{
			result = TwoToneSecondColor;
		}
	}
	else
	{
		// Vertical.
		if (input.Coords.xy.x > TwoToneSplitPosition)
		{
			result = TwoToneSecondColor;
		}
	}
	result *= alpha;

    return result;
}   // end of CalcTwoToneColor()

float4 CalcOutlineColor(VertexShaderOutput input, out float dist2, out float dx, out float dy)
{
    float innerRadius2 = input.Radius.x;
    float outerRadius2 = input.Radius.y;
    float width = input.Radius.z;
    float height = input.Radius.w;
    float innerOutlineRadius2 = input.Outline.x;
    float outerOutlineRadius2 = input.Outline.y;

    float2 pos = input.Coords.xy;

    // Collapse point down to disc.
    if (pos.x > CornerRadius)
    {
        pos.x = max(pos.x - width, CornerRadius);
    }
    if (pos.y > CornerRadius)
    {
        pos.y = max(pos.y - height, CornerRadius);
    }

    dx = pos.x - CornerRadius;
    dy = pos.y - CornerRadius;
    dist2 = dx*dx + dy*dy;

    // Calc color body vs outline.
    float outlineBodyBlend = (outerOutlineRadius2 - dist2) / (outerOutlineRadius2 - innerOutlineRadius2);
    outlineBodyBlend = clamp(outlineBodyBlend, 0, 1);

    // Calc blended edge.
    float alpha = (outerRadius2 - dist2) / (outerRadius2 - innerRadius2);
    alpha = clamp(alpha, 0, 1);

    float4 result = lerp(OutlineColor, BodyColor, outlineBodyBlend);
    result *= alpha;

    return result;
}   // end of CalcOutlineColor()

float4 CalcOutlineTwoToneColor(VertexShaderOutput input, out float dist2, out float dx, out float dy)
{
    float innerRadius2 = input.Radius.x;
    float outerRadius2 = input.Radius.y;
    float width = input.Radius.z;
    float height = input.Radius.w;
    float innerOutlineRadius2 = input.Outline.x;
    float outerOutlineRadius2 = input.Outline.y;

    float2 pos = input.Coords.xy;

    // Collapse point down to disc.
    if (pos.x > CornerRadius)
    {
        pos.x = max(pos.x - width, CornerRadius);
    }
    if (pos.y > CornerRadius)
    {
        pos.y = max(pos.y - height, CornerRadius);
    }

    dx = pos.x - CornerRadius;
    dy = pos.y - CornerRadius;
    dist2 = dx*dx + dy*dy;

    // Calc color body vs outline.
    float outlineBodyBlend = (outerOutlineRadius2 - dist2) / (outerOutlineRadius2 - innerOutlineRadius2);
    outlineBodyBlend = clamp(outlineBodyBlend, 0, 1);

    // Calc blended edge.
    float alpha = (outerRadius2 - dist2) / (outerRadius2 - innerRadius2);
    alpha = clamp(alpha, 0, 1);

    float4 result = BodyColor;
	if (TwoToneHorizontalSplit)
	{
		// Horizontal.
		if (input.Coords.xy.y > TwoToneSplitPosition)
		{
			result = TwoToneSecondColor;
		}
	}
	else
	{
		// Vertical.
		if (input.Coords.xy.x > TwoToneSplitPosition)
		{
			result = TwoToneSecondColor;
		}
	}

    result = lerp(OutlineColor, result, outlineBodyBlend);
    result *= alpha;

    return result;
}   // end of CalcOutlineTwoToneColor()

float4 CalcOutlineAltColor(VertexShaderOutput input, out float dist2, out float dx, out float dy)
{
    float innerRadius2 = input.Radius.x;
    float outerRadius2 = input.Radius.y;
    float width = input.Radius.z;
    float height = input.Radius.w;
    float innerOutlineRadius2 = input.Outline.x;
    float outerOutlineRadius2 = input.Outline.y;

    float halfWidthInner = input.OutlineAlt.x;
    float halfHeightInner = input.OutlineAlt.y;
    float halfWidthOuter = input.OutlineAlt.z;
    float halfHeightOuter = input.OutlineAlt.w;

    //
    // Calc outline color params.  Since this is the OutlineAlt shader
    // we know that the bodyColor area will just be the rectangle in 
    // the center of the shape.
    //

    // Transform pos to coord system where 0, 0 is at center of shape.
    float2 pos = input.Coords.xy;
    pos = abs(pos - Size/2 - EdgeBlend);

    // Calc blend values along both axes.
    float2 blends = (pos - input.OutlineAlt.xy) / (2.0 * EdgeBlend);
    // Pick max one to use.
    float outlineBodyBlend = max(blends.x, blends.y);
    outlineBodyBlend = clamp(1 - outlineBodyBlend, 0, 1);

    //
    // Calc corner params.
    //
    pos = input.Coords.xy;

    // Collapse point down to disc.
    if (pos.x > CornerRadius)
    {
        pos.x = max(pos.x - width, CornerRadius);
    }
    if (pos.y > CornerRadius)
    {
        pos.y = max(pos.y - height, CornerRadius);
    }

    dx = pos.x - CornerRadius;
    dy = pos.y - CornerRadius;
    dist2 = dx*dx + dy*dy;

    // Calc blended edge.
    float alpha = (outerRadius2 - dist2) / (outerRadius2 - innerRadius2);
    alpha = clamp(alpha, 0, 1);

    float4 result = lerp(OutlineColor, BodyColor, outlineBodyBlend);
    result *= alpha;

    return result;
}   // end of CalcOutlineAltColor()

float4 CalcOutlineTwoToneAltColor(VertexShaderOutput input, out float dist2, out float dx, out float dy)
{
    float innerRadius2 = input.Radius.x;
    float outerRadius2 = input.Radius.y;
    float width = input.Radius.z;
    float height = input.Radius.w;
    float innerOutlineRadius2 = input.Outline.x;
    float outerOutlineRadius2 = input.Outline.y;

    float halfWidthInner = input.OutlineAlt.x;
    float halfHeightInner = input.OutlineAlt.y;
    float halfWidthOuter = input.OutlineAlt.z;
    float halfHeightOuter = input.OutlineAlt.w;

    //
    // Calc outline color params.  Since this is the OutlineAlt shader
    // we know that the bodyColor area will just be the rectangle in 
    // the center of the shape.
    //

    // Transform pos to coord system where 0, 0 is at center of shape.
    float2 pos = input.Coords.xy;
    pos = abs(pos - Size/2 - EdgeBlend);

    // Calc blend values along both axes.
    float2 blends = (pos - input.OutlineAlt.xy) / (2.0 * EdgeBlend);
    // Pick max one to use.
    float outlineBodyBlend = max(blends.x, blends.y);
    outlineBodyBlend = clamp(1 - outlineBodyBlend, 0, 1);

    //
    // Calc corner params.
    //
    pos = input.Coords.xy;

    // Collapse point down to disc.
    if (pos.x > CornerRadius)
    {
        pos.x = max(pos.x - width, CornerRadius);
    }
    if (pos.y > CornerRadius)
    {
        pos.y = max(pos.y - height, CornerRadius);
    }

    dx = pos.x - CornerRadius;
    dy = pos.y - CornerRadius;
    dist2 = dx*dx + dy*dy;

    // Calc blended edge.
    float alpha = (outerRadius2 - dist2) / (outerRadius2 - innerRadius2);
    alpha = clamp(alpha, 0, 1);

    float4 result = BodyColor;
	if (TwoToneHorizontalSplit)
	{
		// Horizontal.
		if (input.Coords.xy.y > TwoToneSplitPosition)
		{
			result = TwoToneSecondColor;
		}
	}
	else
	{
		// Vertical.
		if (input.Coords.xy.x > TwoToneSplitPosition)
		{
			result = TwoToneSecondColor;
		}
	}

    result = lerp(OutlineColor, result, outlineBodyBlend);
    result *= alpha;

    return result;
}   // end of CalcOutlineTwoToneAltColor()

//
// Calculate normal for slant bevel.
//
float3 CalcSlantBevel(VertexShaderOutput input, float4 bodyColor, float dist2, float dx, float dy)
{
    float innerBevelRadius = input.Bevel.x;
    float outerBevelRadius = input.Bevel.y;

    // Dist is how far away from the radius center we are.  Note this
    // assumes that the bevel width is less than the corner radius.
    float dist = sqrt(dist2);

    float3 faceNormal = float3(0, 0, 1);
    float3 bevelNormal = normalize( float3(dx / dist, dy / dist, 1) );

    // Calc blend between body and bevel.
    //float bevelBlend = saturate((CornerRadius - dist) / (2 * EdgeBlend));
    float bevelBlend = saturate((outerBevelRadius - dist) / (outerBevelRadius - innerBevelRadius));

    float3 normal = lerp(bevelNormal, faceNormal, bevelBlend);
    normal = normalize(normal);

    return normal;

}   // end of CalcSlantBevel()

//
// Calculate normal for rounded slant bevel.
//
float3 CalcRoundedSlantBevel(VertexShaderOutput input, float4 bodyColor, float dist2, float dx, float dy)
{
    float innerBevelRadius = input.Bevel.x;
    float outerBevelRadius = input.Bevel.y;

    // Dist is how far away from the radius center we are.  Note this
    // assumes that the bevel width is less than the corner radius.
    float dist = sqrt(dist2);

    float3 faceNormal = float3(0, 0, 1);
    float3 bevelNormal = float3(dx / dist, dy / dist, 0);

    // Calc blend between body and bevel.
    float bevelBlend = saturate((CornerRadius - dist) / BevelWidth.x);

    float3 normal = lerp(bevelNormal, faceNormal, bevelBlend);
    normal = normalize(normal);

    return normal;

}   // end of CalcRoundedSlantBevel()

//
// Calculate normal for round bevel.
//
float3 CalcRoundBevel(VertexShaderOutput input, float4 bodyColor, float dist2, float dx, float dy)
{
    float innerBevelRadius = input.Bevel.x;
    float outerBevelRadius = input.Bevel.y;

    // Dist is how far away from the radius center we are.  Note this
    // assumes that the bevel width is less than the corner radius.
    float dist = sqrt(dist2);

    float3 faceNormal = float3(0, 0, 1);
    float3 bevelNormal = float3(dx / dist, dy / dist, 0);

    // Calc blend between body and bevel.
    float bevelBlend = 1 - saturate((CornerRadius - dist) / BevelWidth.x);
    // Make rounded.
    bevelBlend *= bevelBlend;

    float3 normal = lerp(faceNormal, bevelNormal, bevelBlend);
    normal = normalize(normal);

    return normal;

}   // end of CalcRoundBevel()


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
}   // end of ApplyOuterShadow()

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

//
// Standard VS used for all options.
//
VertexShaderOutput VS(VertexShaderInput input)
{
    VertexShaderOutput output;

    output.Position = mul(float4(input.Position, 0, 1), WorldViewProjMatrix);
    output.Coords.xy = input.Coords;

    output.UV = input.UV;

    // We don't need all of these for all versions.  Think about
    // breaking this VS up instead specific versions.
    CalcCornersVS(output);
    CalcOutlineVS(output);
    CalcShadowVS(output);
    CalcBevelVS(output);

    return output;
}   // end of VS()

//
// Alt version of VS used for AltShadow shaders.
//
VertexShaderOutput AltVS(VertexShaderInput input)
{
    VertexShaderOutput output;

    output.Position = mul(float4(input.Position, 0, 1), WorldViewProjMatrix);
    output.Coords.xy = input.Coords;

    output.UV = input.UV;

    // We don't need all of these for all versions.  Think about
    // breaking this VS up instead specific versions.
    CalcCornersVS(output);
    CalcOutlineVS(output);
    CalcShadowVS(output);
    CalcBevelVS(output);

    return output;
}   // end of AltVS()


//
//
// Pixel shaders.
//
//


float4 OuterShadowOnlyPS(VertexShaderOutput input) : COLOR0
{
    float4 result = ApplyOuterShadow(input, float4(0, 0, 0, 0));

    return result;
}   // end of OuterShadowOnlyPS()

float4 InnerShadowOnlyPS(VertexShaderOutput input) : COLOR0
{
    float dist2, dx, dy;

    // We need to calc this so we know where the edge of the shape is.
    float4 result = CalcSolidColor(input, dist2, dx, dy);
    result.rgb = result.a;  // Convert base color to white.
    result = ApplyInnerShadow(input, result);
    // Now take darkness of result and make the reset transparent.
    result = float4(0, 0, 0, (1-result.r) * result.a);

    return result;
}   // end of InnerShadowOnlyPS()


//
// No texture, no bevel versions.
//

float4 NoShadowPS(VertexShaderOutput input) : COLOR0
{
    float dist2, dx, dy;
    float4 result = CalcSolidColor(input, dist2, dx, dy);

    return result;
}	// end of NoShadowPS()

float4 OutlineNoShadowPS(VertexShaderOutput input) : COLOR0
{
    float dist2, dx, dy;
    float4 result = CalcOutlineColor(input, dist2, dx, dy);

    return result;
}	// end of OutlineNoShadowPS()

float4 OutlineAltNoShadowPS(VertexShaderOutput input) : COLOR0
{
    float dist2, dx, dy;
    float4 result = CalcOutlineAltColor(input, dist2, dx, dy);

    return result;
}	// end of OutlineAltNoShadowPS()

float4 OuterShadowPS(VertexShaderOutput input) : COLOR0
{   
    float dist2, dx, dy;
    float4 result = CalcSolidColor(input, dist2, dx, dy);
    result = ApplyOuterShadow(input, result);

    return result;
}	// end of OuterShadowPS()

float4 InnerShadowPS(VertexShaderOutput input) : COLOR0
{
    float dist2, dx, dy;
    float4 result = CalcSolidColor(input, dist2, dx, dy);
    result = ApplyInnerShadow(input, result);

    return result;
}	// end of InnerShadowPS()

float4 OutlineOuterShadowPS(VertexShaderOutput input) : COLOR0
{   
    float dist2, dx, dy;
    float4 result = CalcOutlineColor(input, dist2, dx, dy);
    result = ApplyOuterShadow(input, result);

    return result;
}	// end of OutlineOuterShadowPS()

float4 OutlineInnerShadowPS(VertexShaderOutput input) : COLOR0
{
    float dist2, dx, dy;
    float4 result = CalcOutlineColor(input, dist2, dx, dy);
    result = ApplyInnerShadow(input, result);

    return result;
}	// end of OutlineInnerShadowPS()

float4 OutlineAltOuterShadowPS(VertexShaderOutput input) : COLOR0
{   
    float dist2, dx, dy;
    float4 result = CalcOutlineAltColor(input, dist2, dx, dy);
    result = ApplyOuterShadow(input, result);

    return result;
}	// end of OutlineAltOuterShadowPS()

float4 OutlineAltInnerShadowPS(VertexShaderOutput input) : COLOR0
{
    float dist2, dx, dy;
    float4 result = CalcOutlineAltColor(input, dist2, dx, dy);
    result = ApplyInnerShadow(input, result);

    return result;
}	// end of OutlineAltInnerShadowPS()

//
// Texture, no bevel versions.
//

float4 NoShadowTexturePS(VertexShaderOutput input) : COLOR0
{
    float dist2, dx, dy;
    float4 result = CalcSolidColor(input, dist2, dx, dy);
    result = ApplyTextureColor(input, result);

    return result;
}	// end of NoShadowTexturePS()

float4 OutlineNoShadowTexturePS(VertexShaderOutput input) : COLOR0
{
    float dist2, dx, dy;
    float4 result = CalcOutlineColor(input, dist2, dx, dy);
    result = ApplyTextureColor(input, result);

    return result;
}	// end of OutlineNoShadowTexturePS()

float4 OutlineAltNoShadowTexturePS(VertexShaderOutput input) : COLOR0
{
    float dist2, dx, dy;
    float4 result = CalcOutlineAltColor(input, dist2, dx, dy);
    result = ApplyTextureColor(input, result);

    return result;
}	// end of OutlineAltNoShadowTexturePS()

float4 OuterShadowTexturePS(VertexShaderOutput input) : COLOR0
{   
    float dist2, dx, dy;
    float4 result = CalcSolidColor(input, dist2, dx, dy);
    result = ApplyTextureColor(input, result);
    result = ApplyOuterShadow(input, result);

    return result;
}	// end of OuterShadowTexturePS()

float4 InnerShadowTexturePS(VertexShaderOutput input) : COLOR0
{
    float dist2, dx, dy;
    float4 result = CalcSolidColor(input, dist2, dx, dy);
    result = ApplyTextureColor(input, result);
    result = ApplyInnerShadow(input, result);

    return result;
}	// end of InnerShadowTexturePS()

float4 OutlineOuterShadowTexturePS(VertexShaderOutput input) : COLOR0
{   
    float dist2, dx, dy;
    float4 result = CalcOutlineColor(input, dist2, dx, dy);
    result = ApplyTextureColor(input, result);
    result = ApplyOuterShadow(input, result);

    return result;
}	// end of OutlineOuterShadowTexturePS()

float4 OutlineInnerShadowTexturePS(VertexShaderOutput input) : COLOR0
{
    float dist2, dx, dy;
    float4 result = CalcOutlineColor(input, dist2, dx, dy);
    result = ApplyTextureColor(input, result);
    result = ApplyInnerShadow(input, result);

    return result;
}	// end of OutlineInnerShadowTexturePS()

float4 OutlineAltOuterShadowTexturePS(VertexShaderOutput input) : COLOR0
{   
    float dist2, dx, dy;
    float4 result = CalcOutlineAltColor(input, dist2, dx, dy);
    result = ApplyTextureColor(input, result);
    result = ApplyOuterShadow(input, result);

    return result;
}	// end of OutlineAltOuterShadowTexturePS()

float4 OutlineAltInnerShadowTexturePS(VertexShaderOutput input) : COLOR0
{
    float dist2, dx, dy;
    float4 result = CalcOutlineAltColor(input, dist2, dx, dy);
    result = ApplyTextureColor(input, result);
    result = ApplyInnerShadow(input, result);

    return result;
}	// end of OutlineAltInnerShadowTexturePS()

//
//
// Beveled, no texture
//
//
float4 NoShadowSlantBevelPS(VertexShaderOutput input) : COLOR0
{
    float dist2, dx, dy;
    float4 bodyColor = CalcSolidColor(input, dist2, dx, dy);
    float3 normal = CalcSlantBevel(input, bodyColor, dist2, dx, dy);
    float4 result = CalcDiffuse(bodyColor, normal);
    result += CalcSpecular(bodyColor.a, normal);

    return result;
}	// end of NoShadowSlantBevelPS()
float4 NoShadowRoundedSlantBevelPS(VertexShaderOutput input) : COLOR0
{
    float dist2, dx, dy;
    float4 bodyColor = CalcSolidColor(input, dist2, dx, dy);
    float3 normal = CalcRoundedSlantBevel(input, bodyColor, dist2, dx, dy);
    float4 result = CalcDiffuse(bodyColor, normal);
    result += CalcSpecular(bodyColor.a, normal);

    return result;
}	// end of NoShadowRoundedSlantBevelPS()
float4 NoShadowRoundBevelPS(VertexShaderOutput input) : COLOR0
{
    float dist2, dx, dy;
    float4 bodyColor = CalcSolidColor(input, dist2, dx, dy);
    float3 normal = CalcRoundBevel(input, bodyColor, dist2, dx, dy);
    float4 result = CalcDiffuse(bodyColor, normal);
    result += CalcSpecular(bodyColor.a, normal);

    return result;
}	// end of NoShadowRoundBevelPS()

float4 OutlineNoShadowSlantBevelPS(VertexShaderOutput input) : COLOR0
{
    float dist2, dx, dy;
    float4 bodyColor = CalcOutlineColor(input, dist2, dx, dy);
    float3 normal = CalcSlantBevel(input, bodyColor, dist2, dx, dy);
    float4 result = CalcDiffuse(bodyColor, normal);
    result += CalcSpecular(bodyColor.a, normal);

    return result;
}	// end of OutlineNoShadowSlantBevelPS()
float4 OutlineNoShadowRoundedSlantBevelPS(VertexShaderOutput input) : COLOR0
{
    float dist2, dx, dy;
    float4 bodyColor = CalcOutlineColor(input, dist2, dx, dy);
    float3 normal = CalcRoundedSlantBevel(input, bodyColor, dist2, dx, dy);
    float4 result = CalcDiffuse(bodyColor, normal);
    result += CalcSpecular(bodyColor.a, normal);

    return result;
}	// end of OutlineNoShadowRoundedSlantBevelPS()
float4 OutlineNoShadowRoundBevelPS(VertexShaderOutput input) : COLOR0
{
    float dist2, dx, dy;
    float4 bodyColor = CalcOutlineColor(input, dist2, dx, dy);
    float3 normal = CalcRoundBevel(input, bodyColor, dist2, dx, dy);
    float4 result = CalcDiffuse(bodyColor, normal);
    result += CalcSpecular(bodyColor.a, normal);

    return result;
}	// end of OutlineNoShadowRoundBevelPS()

float4 OutlineAltNoShadowSlantBevelPS(VertexShaderOutput input) : COLOR0
{
    float dist2, dx, dy;
    float4 bodyColor = CalcOutlineAltColor(input, dist2, dx, dy);
    float3 normal = CalcSlantBevel(input, bodyColor, dist2, dx, dy);
    float4 result = CalcDiffuse(bodyColor, normal);
    result += CalcSpecular(bodyColor.a, normal);

    return result;
}	// end of OutlineAltNoShadowSlantBevelPS()
float4 OutlineAltNoShadowRoundedSlantBevelPS(VertexShaderOutput input) : COLOR0
{
    float dist2, dx, dy;
    float4 bodyColor = CalcOutlineAltColor(input, dist2, dx, dy);
    float3 normal = CalcRoundedSlantBevel(input, bodyColor, dist2, dx, dy);
    float4 result = CalcDiffuse(bodyColor, normal);
    result += CalcSpecular(bodyColor.a, normal);

    return result;
}	// end of OutlineAltNoShadowRoundedSlantBevelPS()
float4 OutlineAltNoShadowRoundBevelPS(VertexShaderOutput input) : COLOR0
{
    float dist2, dx, dy;
    float4 bodyColor = CalcOutlineAltColor(input, dist2, dx, dy);
    float3 normal = CalcRoundBevel(input, bodyColor, dist2, dx, dy);
    float4 result = CalcDiffuse(bodyColor, normal);
    result += CalcSpecular(bodyColor.a, normal);

    return result;
}	// end of OutlineAltNoShadowRoundBevelPS()

//
// Outer shadow with bevel.
//
float4 OuterShadowSlantBevelPS(VertexShaderOutput input) : COLOR0
{
    float dist2, dx, dy;
    float4 bodyColor = CalcSolidColor(input, dist2, dx, dy);
    float3 normal = CalcSlantBevel(input, bodyColor, dist2, dx, dy);
    float4 result = CalcDiffuse(bodyColor, normal);
    result += CalcSpecular(bodyColor.a, normal);
    result = ApplyOuterShadow(input, result);

    return result;
}	// end of OuterShadowSlantBevelPS()
float4 OuterShadowRoundedSlantBevelPS(VertexShaderOutput input) : COLOR0
{
    float dist2, dx, dy;
    float4 bodyColor = CalcSolidColor(input, dist2, dx, dy);
    float3 normal = CalcRoundedSlantBevel(input, bodyColor, dist2, dx, dy);
    float4 result = CalcDiffuse(bodyColor, normal);
    result += CalcSpecular(bodyColor.a, normal);
    result = ApplyOuterShadow(input, result);

    return result;
}	// end of OuterShadowRoundedSlantBevelPS()
float4 OuterShadowRoundBevelPS(VertexShaderOutput input) : COLOR0
{
    float dist2, dx, dy;
    float4 bodyColor = CalcSolidColor(input, dist2, dx, dy);
    float3 normal = CalcRoundBevel(input, bodyColor, dist2, dx, dy);
    float4 result = CalcDiffuse(bodyColor, normal);
    result += CalcSpecular(bodyColor.a, normal);
    result = ApplyOuterShadow(input, result);

    return result;
}	// end of OuterShadowRoundBevelPS()

float4 OutlineOuterShadowSlantBevelPS(VertexShaderOutput input) : COLOR0
{
    float dist2, dx, dy;
    float4 bodyColor = CalcOutlineColor(input, dist2, dx, dy);
    float3 normal = CalcSlantBevel(input, bodyColor, dist2, dx, dy);
    float4 result = CalcDiffuse(bodyColor, normal);
    result += CalcSpecular(bodyColor.a, normal);
    result = ApplyOuterShadow(input, result);

    return result;
}	// end of OutlineOuterShadowSlantBevelPS()
float4 OutlineOuterShadowRoundedSlantBevelPS(VertexShaderOutput input) : COLOR0
{
    float dist2, dx, dy;
    float4 bodyColor = CalcOutlineColor(input, dist2, dx, dy);
    float3 normal = CalcRoundedSlantBevel(input, bodyColor, dist2, dx, dy);
    float4 result = CalcDiffuse(bodyColor, normal);
    result += CalcSpecular(bodyColor.a, normal);
    result = ApplyOuterShadow(input, result);

    return result;
}	// end of OutlineOuterShadowRoundedSlantBevelPS()
float4 OutlineOuterShadowRoundBevelPS(VertexShaderOutput input) : COLOR0
{
    float dist2, dx, dy;
    float4 bodyColor = CalcOutlineColor(input, dist2, dx, dy);
    float3 normal = CalcRoundBevel(input, bodyColor, dist2, dx, dy);
    float4 result = CalcDiffuse(bodyColor, normal);
    result += CalcSpecular(bodyColor.a, normal);
    result = ApplyOuterShadow(input, result);

    return result;
}	// end of OutlineOuterShadowRoundBevelPS()

float4 OutlineAltOuterShadowSlantBevelPS(VertexShaderOutput input) : COLOR0
{
    float dist2, dx, dy;
    float4 bodyColor = CalcOutlineAltColor(input, dist2, dx, dy);
    float3 normal = CalcSlantBevel(input, bodyColor, dist2, dx, dy);
    float4 result = CalcDiffuse(bodyColor, normal);
    result += CalcSpecular(bodyColor.a, normal);
    result = ApplyOuterShadow(input, result);

    return result;
}	// end of OutlineAltOuterShadowSlantBevelPS()
float4 OutlineAltOuterShadowRoundedSlantBevelPS(VertexShaderOutput input) : COLOR0
{
    float dist2, dx, dy;
    float4 bodyColor = CalcOutlineAltColor(input, dist2, dx, dy);
    float3 normal = CalcRoundedSlantBevel(input, bodyColor, dist2, dx, dy);
    float4 result = CalcDiffuse(bodyColor, normal);
    result += CalcSpecular(bodyColor.a, normal);
    result = ApplyOuterShadow(input, result);

    return result;
}	// end of OutlineAltOuterShadowRoundedSlantBevelPS()
float4 OutlineAltOuterShadowRoundBevelPS(VertexShaderOutput input) : COLOR0
{
    float dist2, dx, dy;
    float4 bodyColor = CalcOutlineAltColor(input, dist2, dx, dy);
    float3 normal = CalcRoundBevel(input, bodyColor, dist2, dx, dy);
    float4 result = CalcDiffuse(bodyColor, normal);
    result += CalcSpecular(bodyColor.a, normal);
    result = ApplyOuterShadow(input, result);

    return result;
}	// end of OutlineAltOuterShadowRoundBevelPS()

//
// Inner shadow, with bevel.
//
float4 InnerShadowSlantBevelPS(VertexShaderOutput input) : COLOR0
{
    float dist2, dx, dy;
    float4 bodyColor = CalcSolidColor(input, dist2, dx, dy);
    float3 normal = CalcSlantBevel(input, bodyColor, dist2, dx, dy);
    float4 result = CalcDiffuse(bodyColor, normal);
    result += CalcSpecular(bodyColor.a, normal);
    result = ApplyInnerShadow(input, result);

    return result;
}	// end of InnerShadowSlantBevelPS()
float4 InnerShadowRoundedSlantBevelPS(VertexShaderOutput input) : COLOR0
{
    float dist2, dx, dy;
    float4 bodyColor = CalcSolidColor(input, dist2, dx, dy);
    float3 normal = CalcRoundedSlantBevel(input, bodyColor, dist2, dx, dy);
    float4 result = CalcDiffuse(bodyColor, normal);
    result += CalcSpecular(bodyColor.a, normal);
    result = ApplyInnerShadow(input, result);

    return result;
}	// end of InnerShadowRoundedSlantBevelPS()
float4 InnerShadowRoundBevelPS(VertexShaderOutput input) : COLOR0
{
    float dist2, dx, dy;
    float4 bodyColor = CalcSolidColor(input, dist2, dx, dy);
    float3 normal = CalcRoundBevel(input, bodyColor, dist2, dx, dy);
    float4 result = CalcDiffuse(bodyColor, normal);
    result += CalcSpecular(bodyColor.a, normal);
    result = ApplyInnerShadow(input, result);

    return result;
}	// end of InnerShadowRoundBevelPS()

float4 OutlineInnerShadowSlantBevelPS(VertexShaderOutput input) : COLOR0
{
    float dist2, dx, dy;
    float4 bodyColor = CalcOutlineColor(input, dist2, dx, dy);
    float3 normal = CalcSlantBevel(input, bodyColor, dist2, dx, dy);
    float4 result = CalcDiffuse(bodyColor, normal);
    result += CalcSpecular(bodyColor.a, normal);
    result = ApplyInnerShadow(input, result);

    return result;
}	// end of OutlineInnerShadowSlantBevelPS()
float4 OutlineInnerShadowRoundedSlantBevelPS(VertexShaderOutput input) : COLOR0
{
    float dist2, dx, dy;
    float4 bodyColor = CalcOutlineColor(input, dist2, dx, dy);
    float3 normal = CalcRoundedSlantBevel(input, bodyColor, dist2, dx, dy);
    float4 result = CalcDiffuse(bodyColor, normal);
    result += CalcSpecular(bodyColor.a, normal);
    result = ApplyInnerShadow(input, result);

    return result;
}	// end of OutlineInnerShadowRoundedSlantBevelPS()
float4 OutlineInnerShadowRoundBevelPS(VertexShaderOutput input) : COLOR0
{
    float dist2, dx, dy;
    float4 bodyColor = CalcOutlineColor(input, dist2, dx, dy);
    float3 normal = CalcRoundBevel(input, bodyColor, dist2, dx, dy);
    float4 result = CalcDiffuse(bodyColor, normal);
    result += CalcSpecular(bodyColor.a, normal);
    result = ApplyInnerShadow(input, result);

    return result;
}	// end of OutlineInnerShadowRoundBevelPS()

float4 OutlineAltInnerShadowSlantBevelPS(VertexShaderOutput input) : COLOR0
{
    float dist2, dx, dy;
    float4 bodyColor = CalcOutlineAltColor(input, dist2, dx, dy);
    float3 normal = CalcSlantBevel(input, bodyColor, dist2, dx, dy);
    float4 result = CalcDiffuse(bodyColor, normal);
    result += CalcSpecular(bodyColor.a, normal);
    result = ApplyInnerShadow(input, result);

    return result;
}	// end of OutlineAltInnerShadowSlantBevelPS()
float4 OutlineAltInnerShadowRoundedSlantBevelPS(VertexShaderOutput input) : COLOR0
{
    float dist2, dx, dy;
    float4 bodyColor = CalcOutlineAltColor(input, dist2, dx, dy);
    float3 normal = CalcRoundedSlantBevel(input, bodyColor, dist2, dx, dy);
    float4 result = CalcDiffuse(bodyColor, normal);
    result += CalcSpecular(bodyColor.a, normal);
    result = ApplyInnerShadow(input, result);

    return result;
}	// end of OutlineAltInnerShadowRoundedSlantBevelPS()
float4 OutlineAltInnerShadowRoundBevelPS(VertexShaderOutput input) : COLOR0
{
    float dist2, dx, dy;
    float4 bodyColor = CalcOutlineAltColor(input, dist2, dx, dy);
    float3 normal = CalcRoundBevel(input, bodyColor, dist2, dx, dy);
    float4 result = CalcDiffuse(bodyColor, normal);
    result += CalcSpecular(bodyColor.a, normal);
    result = ApplyInnerShadow(input, result);

    return result;
}	// end of OutlineAltInnerShadowRoundBevelPS()

//
//
// Beveled with texture
//
//
float4 NoShadowTextureSlantBevelPS(VertexShaderOutput input) : COLOR0
{
    float dist2, dx, dy;
    float4 bodyColor = CalcSolidColor(input, dist2, dx, dy);
    bodyColor = ApplyTextureColor(input, bodyColor);
    float3 normal = CalcSlantBevel(input, bodyColor, dist2, dx, dy);
    float4 result = CalcDiffuse(bodyColor, normal);
    result += CalcSpecular(bodyColor.a, normal);

    return result;
}	// end of NoShadowTextureSlantBevelPS()
float4 NoShadowTextureRoundedSlantBevelPS(VertexShaderOutput input) : COLOR0
{
    float dist2, dx, dy;
    float4 bodyColor = CalcSolidColor(input, dist2, dx, dy);
    bodyColor = ApplyTextureColor(input, bodyColor);
    float3 normal = CalcRoundedSlantBevel(input, bodyColor, dist2, dx, dy);
    float4 result = CalcDiffuse(bodyColor, normal);
    result += CalcSpecular(bodyColor.a, normal);

    return result;
}	// end of NoShadowTextureRoundedSlantBevelPS()
float4 NoShadowTextureRoundBevelPS(VertexShaderOutput input) : COLOR0
{
    float dist2, dx, dy;
    float4 bodyColor = CalcSolidColor(input, dist2, dx, dy);
    bodyColor = ApplyTextureColor(input, bodyColor);
    float3 normal = CalcRoundBevel(input, bodyColor, dist2, dx, dy);
    float4 result = CalcDiffuse(bodyColor, normal);
    result += CalcSpecular(bodyColor.a, normal);

    return result;
}	// end of NoShadowTextureRoundBevelPS()

float4 OutlineNoShadowTextureSlantBevelPS(VertexShaderOutput input) : COLOR0
{
    float dist2, dx, dy;
    float4 bodyColor = CalcOutlineColor(input, dist2, dx, dy);
    bodyColor = ApplyTextureColor(input, bodyColor);
    float3 normal = CalcSlantBevel(input, bodyColor, dist2, dx, dy);
    float4 result = CalcDiffuse(bodyColor, normal);
    result += CalcSpecular(bodyColor.a, normal);

    return result;
}	// end of OutlineNoShadowTextureSlantBevelPS()
float4 OutlineNoShadowTextureRoundedSlantBevelPS(VertexShaderOutput input) : COLOR0
{
    float dist2, dx, dy;
    float4 bodyColor = CalcOutlineColor(input, dist2, dx, dy);
    bodyColor = ApplyTextureColor(input, bodyColor);
    float3 normal = CalcRoundedSlantBevel(input, bodyColor, dist2, dx, dy);
    float4 result = CalcDiffuse(bodyColor, normal);
    result += CalcSpecular(bodyColor.a, normal);

    return result;
}	// end of OutlineNoShadowTextureRoundedSlantBevelPS()
float4 OutlineNoShadowTextureRoundBevelPS(VertexShaderOutput input) : COLOR0
{
    float dist2, dx, dy;
    float4 bodyColor = CalcOutlineColor(input, dist2, dx, dy);
    bodyColor = ApplyTextureColor(input, bodyColor);
    float3 normal = CalcRoundBevel(input, bodyColor, dist2, dx, dy);
    float4 result = CalcDiffuse(bodyColor, normal);
    result += CalcSpecular(bodyColor.a, normal);

    return result;
}	// end of OutlineNoShadowTextureRoundBevelPS()

float4 OutlineAltNoShadowTextureSlantBevelPS(VertexShaderOutput input) : COLOR0
{
    float dist2, dx, dy;
    float4 bodyColor = CalcOutlineAltColor(input, dist2, dx, dy);
    bodyColor = ApplyTextureColor(input, bodyColor);
    float3 normal = CalcSlantBevel(input, bodyColor, dist2, dx, dy);
    float4 result = CalcDiffuse(bodyColor, normal);
    result += CalcSpecular(bodyColor.a, normal);

    return result;
}	// end of OutlineAltNoShadowTextureSlantBevelPS()
float4 OutlineAltNoShadowTextureRoundedSlantBevelPS(VertexShaderOutput input) : COLOR0
{
    float dist2, dx, dy;
    float4 bodyColor = CalcOutlineAltColor(input, dist2, dx, dy);
    bodyColor = ApplyTextureColor(input, bodyColor);
    float3 normal = CalcRoundedSlantBevel(input, bodyColor, dist2, dx, dy);
    float4 result = CalcDiffuse(bodyColor, normal);
    result += CalcSpecular(bodyColor.a, normal);

    return result;
}	// end of OutlineAltNoShadowTextureRoundedSlantBevelPS()
float4 OutlineAltNoShadowTextureRoundBevelPS(VertexShaderOutput input) : COLOR0
{
    float dist2, dx, dy;
    float4 bodyColor = CalcOutlineAltColor(input, dist2, dx, dy);
    bodyColor = ApplyTextureColor(input, bodyColor);
    float3 normal = CalcRoundBevel(input, bodyColor, dist2, dx, dy);
    float4 result = CalcDiffuse(bodyColor, normal);
    result += CalcSpecular(bodyColor.a, normal);

    return result;
}	// end of OutlineAltNoShadowTextureRoundBevelPS()

//
// Outer ShadowTexture with bevel.
//
float4 OuterShadowTextureSlantBevelPS(VertexShaderOutput input) : COLOR0
{
    float dist2, dx, dy;
    float4 bodyColor = CalcSolidColor(input, dist2, dx, dy);
    bodyColor = ApplyTextureColor(input, bodyColor);
    float3 normal = CalcSlantBevel(input, bodyColor, dist2, dx, dy);
    float4 result = CalcDiffuse(bodyColor, normal);
    result += CalcSpecular(bodyColor.a, normal);
    result = ApplyOuterShadow(input, result);

    return result;
}	// end of OuterShadowTextureSlantBevelPS()
float4 OuterShadowTextureRoundedSlantBevelPS(VertexShaderOutput input) : COLOR0
{
    float dist2, dx, dy;
    float4 bodyColor = CalcSolidColor(input, dist2, dx, dy);
    bodyColor = ApplyTextureColor(input, bodyColor);
    float3 normal = CalcRoundedSlantBevel(input, bodyColor, dist2, dx, dy);
    float4 result = CalcDiffuse(bodyColor, normal);
    result += CalcSpecular(bodyColor.a, normal);
    result = ApplyOuterShadow(input, result);

    return result;
}	// end of OuterShadowTextureRoundedSlantBevelPS()
float4 OuterShadowTextureRoundBevelPS(VertexShaderOutput input) : COLOR0
{
    float dist2, dx, dy;
    float4 bodyColor = CalcSolidColor(input, dist2, dx, dy);
    bodyColor = ApplyTextureColor(input, bodyColor);
    float3 normal = CalcRoundBevel(input, bodyColor, dist2, dx, dy);
    float4 result = CalcDiffuse(bodyColor, normal);
    result += CalcSpecular(bodyColor.a, normal);
    result = ApplyOuterShadow(input, result);

    return result;
}	// end of OuterShadowTextureRoundBevelPS()

float4 OutlineOuterShadowTextureSlantBevelPS(VertexShaderOutput input) : COLOR0
{
    float dist2, dx, dy;
    float4 bodyColor = CalcOutlineColor(input, dist2, dx, dy);
    bodyColor = ApplyTextureColor(input, bodyColor);
    float3 normal = CalcSlantBevel(input, bodyColor, dist2, dx, dy);
    float4 result = CalcDiffuse(bodyColor, normal);
    result += CalcSpecular(bodyColor.a, normal);
    result = ApplyOuterShadow(input, result);

    return result;
}	// end of OutlineOuterShadowTextureSlantBevelPS()
float4 OutlineOuterShadowTextureRoundedSlantBevelPS(VertexShaderOutput input) : COLOR0
{
    float dist2, dx, dy;
    float4 bodyColor = CalcOutlineColor(input, dist2, dx, dy);
    bodyColor = ApplyTextureColor(input, bodyColor);
    float3 normal = CalcRoundedSlantBevel(input, bodyColor, dist2, dx, dy);
    float4 result = CalcDiffuse(bodyColor, normal);
    result += CalcSpecular(bodyColor.a, normal);
    result = ApplyOuterShadow(input, result);

    return result;
}	// end of OutlineOuterShadowTextureRoundedSlantBevelPS()
float4 OutlineOuterShadowTextureRoundBevelPS(VertexShaderOutput input) : COLOR0
{
    float dist2, dx, dy;
    float4 bodyColor = CalcOutlineColor(input, dist2, dx, dy);
    bodyColor = ApplyTextureColor(input, bodyColor);
    float3 normal = CalcRoundBevel(input, bodyColor, dist2, dx, dy);
    float4 result = CalcDiffuse(bodyColor, normal);
    result += CalcSpecular(bodyColor.a, normal);
    result = ApplyOuterShadow(input, result);

    return result;
}	// end of OutlineOuterShadowTextureRoundBevelPS()

float4 OutlineAltOuterShadowTextureSlantBevelPS(VertexShaderOutput input) : COLOR0
{
    float dist2, dx, dy;
    float4 bodyColor = CalcOutlineAltColor(input, dist2, dx, dy);
    bodyColor = ApplyTextureColor(input, bodyColor);
    float3 normal = CalcSlantBevel(input, bodyColor, dist2, dx, dy);
    float4 result = CalcDiffuse(bodyColor, normal);
    result += CalcSpecular(bodyColor.a, normal);
    result = ApplyOuterShadow(input, result);

    return result;
}	// end of OutlineAltOuterShadowTextureSlantBevelPS()
float4 OutlineAltOuterShadowTextureRoundedSlantBevelPS(VertexShaderOutput input) : COLOR0
{
    float dist2, dx, dy;
    float4 bodyColor = CalcOutlineAltColor(input, dist2, dx, dy);
    bodyColor = ApplyTextureColor(input, bodyColor);
    float3 normal = CalcRoundedSlantBevel(input, bodyColor, dist2, dx, dy);
    float4 result = CalcDiffuse(bodyColor, normal);
    result += CalcSpecular(bodyColor.a, normal);
    result = ApplyOuterShadow(input, result);

    return result;
}	// end of OutlineAltOuterShadowTextureRoundedSlantBevelPS()
float4 OutlineAltOuterShadowTextureRoundBevelPS(VertexShaderOutput input) : COLOR0
{
    float dist2, dx, dy;
    float4 bodyColor = CalcOutlineAltColor(input, dist2, dx, dy);
    bodyColor = ApplyTextureColor(input, bodyColor);
    float3 normal = CalcRoundBevel(input, bodyColor, dist2, dx, dy);
    float4 result = CalcDiffuse(bodyColor, normal);
    result += CalcSpecular(bodyColor.a, normal);
    result = ApplyOuterShadow(input, result);

    return result;
}	// end of OutlineAltOuterShadowTextureRoundBevelPS()

//
// Inner ShadowTexture, with bevel.
//
float4 InnerShadowTextureSlantBevelPS(VertexShaderOutput input) : COLOR0
{
    float dist2, dx, dy;
    float4 bodyColor = CalcSolidColor(input, dist2, dx, dy);
    bodyColor = ApplyTextureColor(input, bodyColor);
    float3 normal = CalcSlantBevel(input, bodyColor, dist2, dx, dy);
    float4 result = CalcDiffuse(bodyColor, normal);
    result += CalcSpecular(bodyColor.a, normal);
    result = ApplyInnerShadow(input, result);

    return result;
}	// end of InnerShadowTextureSlantBevelPS()
float4 InnerShadowTextureRoundedSlantBevelPS(VertexShaderOutput input) : COLOR0
{
    float dist2, dx, dy;
    float4 bodyColor = CalcSolidColor(input, dist2, dx, dy);
    bodyColor = ApplyTextureColor(input, bodyColor);
    float3 normal = CalcRoundedSlantBevel(input, bodyColor, dist2, dx, dy);
    float4 result = CalcDiffuse(bodyColor, normal);
    result += CalcSpecular(bodyColor.a, normal);
    result = ApplyInnerShadow(input, result);

    return result;
}	// end of InnerShadowTextureRoundedSlantBevelPS()
float4 InnerShadowTextureRoundBevelPS(VertexShaderOutput input) : COLOR0
{
    float dist2, dx, dy;
    float4 bodyColor = CalcSolidColor(input, dist2, dx, dy);
    bodyColor = ApplyTextureColor(input, bodyColor);
    float3 normal = CalcRoundBevel(input, bodyColor, dist2, dx, dy);
    float4 result = CalcDiffuse(bodyColor, normal);
    result += CalcSpecular(bodyColor.a, normal);
    result = ApplyInnerShadow(input, result);

    return result;
}	// end of InnerShadowTextureRoundBevelPS()

float4 OutlineInnerShadowTextureSlantBevelPS(VertexShaderOutput input) : COLOR0
{
    float dist2, dx, dy;
    float4 bodyColor = CalcOutlineColor(input, dist2, dx, dy);
    bodyColor = ApplyTextureColor(input, bodyColor);
    float3 normal = CalcSlantBevel(input, bodyColor, dist2, dx, dy);
    float4 result = CalcDiffuse(bodyColor, normal);
    result += CalcSpecular(bodyColor.a, normal);
    result = ApplyInnerShadow(input, result);

    return result;
}	// end of OutlineInnerShadowTextureSlantBevelPS()
float4 OutlineInnerShadowTextureRoundedSlantBevelPS(VertexShaderOutput input) : COLOR0
{
    float dist2, dx, dy;
    float4 bodyColor = CalcOutlineColor(input, dist2, dx, dy);
    bodyColor = ApplyTextureColor(input, bodyColor);
    float3 normal = CalcRoundedSlantBevel(input, bodyColor, dist2, dx, dy);
    float4 result = CalcDiffuse(bodyColor, normal);
    result += CalcSpecular(bodyColor.a, normal);
    result = ApplyInnerShadow(input, result);

    return result;
}	// end of OutlineInnerShadowTextureRoundedSlantBevelPS()
float4 OutlineInnerShadowTextureRoundBevelPS(VertexShaderOutput input) : COLOR0
{
    float dist2, dx, dy;
    float4 bodyColor = CalcOutlineColor(input, dist2, dx, dy);
    bodyColor = ApplyTextureColor(input, bodyColor);
    float3 normal = CalcRoundBevel(input, bodyColor, dist2, dx, dy);
    float4 result = CalcDiffuse(bodyColor, normal);
    result += CalcSpecular(bodyColor.a, normal);
    result = ApplyInnerShadow(input, result);

    return result;
}	// end of OutlineInnerShadowTextureRoundBevelPS()

float4 OutlineAltInnerShadowTextureSlantBevelPS(VertexShaderOutput input) : COLOR0
{
    float dist2, dx, dy;
    float4 bodyColor = CalcOutlineAltColor(input, dist2, dx, dy);
    bodyColor = ApplyTextureColor(input, bodyColor);
    float3 normal = CalcSlantBevel(input, bodyColor, dist2, dx, dy);
    float4 result = CalcDiffuse(bodyColor, normal);
    result += CalcSpecular(bodyColor.a, normal);
    result = ApplyInnerShadow(input, result);

    return result;
}	// end of OutlineAltInnerShadowTextureSlantBevelPS()
float4 OutlineAltInnerShadowTextureRoundedSlantBevelPS(VertexShaderOutput input) : COLOR0
{
    float dist2, dx, dy;
    float4 bodyColor = CalcOutlineAltColor(input, dist2, dx, dy);
    bodyColor = ApplyTextureColor(input, bodyColor);
    float3 normal = CalcRoundedSlantBevel(input, bodyColor, dist2, dx, dy);
    float4 result = CalcDiffuse(bodyColor, normal);
    result += CalcSpecular(bodyColor.a, normal);
    result = ApplyInnerShadow(input, result);

    return result;
}	// end of OutlineAltInnerShadowTextureRoundedSlantBevelPS()
float4 OutlineAltInnerShadowTextureRoundBevelPS(VertexShaderOutput input) : COLOR0
{
    float dist2, dx, dy;
    float4 bodyColor = CalcOutlineAltColor(input, dist2, dx, dy);
    bodyColor = ApplyTextureColor(input, bodyColor);
    float3 normal = CalcRoundBevel(input, bodyColor, dist2, dx, dy);
    float4 result = CalcDiffuse(bodyColor, normal);
    result += CalcSpecular(bodyColor.a, normal);
    result = ApplyInnerShadow(input, result);

    return result;
}	// end of OutlineAltInnerShadowTextureRoundBevelPS()

//
//
//	Same as all of the above but with TwoTone.
//
//

//
// No texture, no bevel versions.
//

float4 TwoToneNoShadowPS(VertexShaderOutput input) : COLOR0
{
    float dist2, dx, dy;
    float4 result = CalcTwoToneColor(input, dist2, dx, dy);

    return result;
}	// end of TwoToneNoShadowPS()

float4 OutlineTwoToneNoShadowPS(VertexShaderOutput input) : COLOR0
{
    float dist2, dx, dy;
    float4 result = CalcOutlineTwoToneColor(input, dist2, dx, dy);

    return result;
}	// end of OutlineTwoToneNoShadowPS()

float4 OutlineTwoToneAltNoShadowPS(VertexShaderOutput input) : COLOR0
{
    float dist2, dx, dy;
    float4 result = CalcOutlineTwoToneAltColor(input, dist2, dx, dy);

    return result;
}	// end of OutlineTwoToneAltNoShadowPS()

float4 TwoToneOuterShadowPS(VertexShaderOutput input) : COLOR0
{   
    float dist2, dx, dy;
    float4 result = CalcTwoToneColor(input, dist2, dx, dy);
    result = ApplyOuterShadow(input, result);

    return result;
}	// end of TwoToneOuterShadowPS()

float4 TwoToneInnerShadowPS(VertexShaderOutput input) : COLOR0
{
    float dist2, dx, dy;
    float4 result = CalcTwoToneColor(input, dist2, dx, dy);
    result = ApplyInnerShadow(input, result);

    return result;
}	// end of TwoToneInnerShadowPS()

float4 OutlineTwoToneOuterShadowPS(VertexShaderOutput input) : COLOR0
{   
    float dist2, dx, dy;
    float4 result = CalcOutlineTwoToneColor(input, dist2, dx, dy);
    result = ApplyOuterShadow(input, result);

    return result;
}	// end of OutlineTwoToneOuterShadowPS()

float4 OutlineTwoToneInnerShadowPS(VertexShaderOutput input) : COLOR0
{
    float dist2, dx, dy;
    float4 result = CalcOutlineTwoToneColor(input, dist2, dx, dy);
    result = ApplyInnerShadow(input, result);

    return result;
}	// end of OutlineTwoToneInnerShadowPS()

float4 OutlineTwoToneAltOuterShadowPS(VertexShaderOutput input) : COLOR0
{   
    float dist2, dx, dy;
    float4 result = CalcOutlineTwoToneAltColor(input, dist2, dx, dy);
    result = ApplyOuterShadow(input, result);

    return result;
}	// end of OutlineTwoToneAltOuterShadowPS()

float4 OutlineTwoToneAltInnerShadowPS(VertexShaderOutput input) : COLOR0
{
    float dist2, dx, dy;
    float4 result = CalcOutlineTwoToneAltColor(input, dist2, dx, dy);
    result = ApplyInnerShadow(input, result);

    return result;
}	// end of OutlineTwoToneAltInnerShadowPS()

//
// Texture, no bevel versions.
//

float4 TwoToneNoShadowTexturePS(VertexShaderOutput input) : COLOR0
{
    float dist2, dx, dy;
    float4 result = CalcTwoToneColor(input, dist2, dx, dy);
    result = ApplyTextureColor(input, result);

    return result;
}	// end of TwoToneNoShadowTexturePS()

float4 OutlineTwoToneNoShadowTexturePS(VertexShaderOutput input) : COLOR0
{
    float dist2, dx, dy;
    float4 result = CalcOutlineTwoToneColor(input, dist2, dx, dy);
    result = ApplyTextureColor(input, result);

    return result;
}	// end of OutlineTwoToneNoShadowTexturePS()

float4 OutlineTwoToneAltNoShadowTexturePS(VertexShaderOutput input) : COLOR0
{
    float dist2, dx, dy;
    float4 result = CalcOutlineTwoToneAltColor(input, dist2, dx, dy);
    result = ApplyTextureColor(input, result);

    return result;
}	// end of OutlineTwoToneAltNoShadowTexturePS()

float4 TwoToneOuterShadowTexturePS(VertexShaderOutput input) : COLOR0
{   
    float dist2, dx, dy;
    float4 result = CalcTwoToneColor(input, dist2, dx, dy);
    result = ApplyTextureColor(input, result);
    result = ApplyOuterShadow(input, result);

    return result;
}	// end of TwoToneOuterShadowTexturePS()

float4 TwoToneInnerShadowTexturePS(VertexShaderOutput input) : COLOR0
{
    float dist2, dx, dy;
    float4 result = CalcTwoToneColor(input, dist2, dx, dy);
    result = ApplyTextureColor(input, result);
    result = ApplyInnerShadow(input, result);

    return result;
}	// end of TwoToneInnerShadowTexturePS()

float4 OutlineTwoToneOuterShadowTexturePS(VertexShaderOutput input) : COLOR0
{   
    float dist2, dx, dy;
    float4 result = CalcOutlineTwoToneColor(input, dist2, dx, dy);
    result = ApplyTextureColor(input, result);
    result = ApplyOuterShadow(input, result);

    return result;
}	// end of OutlineTwoToneOuterShadowTexturePS()

float4 OutlineTwoToneInnerShadowTexturePS(VertexShaderOutput input) : COLOR0
{
    float dist2, dx, dy;
    float4 result = CalcOutlineTwoToneColor(input, dist2, dx, dy);
    result = ApplyTextureColor(input, result);
    result = ApplyInnerShadow(input, result);

    return result;
}	// end of OutlineTwoToneInnerShadowTexturePS()

float4 OutlineTwoToneAltOuterShadowTexturePS(VertexShaderOutput input) : COLOR0
{   
    float dist2, dx, dy;
    float4 result = CalcOutlineTwoToneAltColor(input, dist2, dx, dy);
    result = ApplyTextureColor(input, result);
    result = ApplyOuterShadow(input, result);

    return result;
}	// end of OutlineTwoToneAltOuterShadowTexturePS()

float4 OutlineTwoToneAltInnerShadowTexturePS(VertexShaderOutput input) : COLOR0
{
    float dist2, dx, dy;
    float4 result = CalcOutlineTwoToneAltColor(input, dist2, dx, dy);
    result = ApplyTextureColor(input, result);
    result = ApplyInnerShadow(input, result);

    return result;
}	// end of OutlineTwoToneAltInnerShadowTexturePS()

//
//
// Beveled, no texture
//
//
float4 TwoToneNoShadowSlantBevelPS(VertexShaderOutput input) : COLOR0
{
    float dist2, dx, dy;
    float4 bodyColor = CalcTwoToneColor(input, dist2, dx, dy);
    float3 normal = CalcSlantBevel(input, bodyColor, dist2, dx, dy);
    float4 result = CalcDiffuse(bodyColor, normal);
    result += CalcSpecular(bodyColor.a, normal);

    return result;
}	// end of TwoToneNoShadowSlantBevelPS()
float4 TwoToneNoShadowRoundedSlantBevelPS(VertexShaderOutput input) : COLOR0
{
    float dist2, dx, dy;
    float4 bodyColor = CalcTwoToneColor(input, dist2, dx, dy);
    float3 normal = CalcRoundedSlantBevel(input, bodyColor, dist2, dx, dy);
    float4 result = CalcDiffuse(bodyColor, normal);
    result += CalcSpecular(bodyColor.a, normal);

    return result;
}	// end of TwoToneNoShadowRoundedSlantBevelPS()
float4 TwoToneNoShadowRoundBevelPS(VertexShaderOutput input) : COLOR0
{
    float dist2, dx, dy;
    float4 bodyColor = CalcTwoToneColor(input, dist2, dx, dy);
    float3 normal = CalcRoundBevel(input, bodyColor, dist2, dx, dy);
    float4 result = CalcDiffuse(bodyColor, normal);
    result += CalcSpecular(bodyColor.a, normal);

    return result;
}	// end of TwoToneNoShadowRoundBevelPS()

float4 OutlineTwoToneNoShadowSlantBevelPS(VertexShaderOutput input) : COLOR0
{
    float dist2, dx, dy;
    float4 bodyColor = CalcOutlineTwoToneColor(input, dist2, dx, dy);
    float3 normal = CalcSlantBevel(input, bodyColor, dist2, dx, dy);
    float4 result = CalcDiffuse(bodyColor, normal);
    result += CalcSpecular(bodyColor.a, normal);

    return result;
}	// end of OutlineTwoToneNoShadowSlantBevelPS()
float4 OutlineTwoToneNoShadowRoundedSlantBevelPS(VertexShaderOutput input) : COLOR0
{
    float dist2, dx, dy;
    float4 bodyColor = CalcOutlineTwoToneColor(input, dist2, dx, dy);
    float3 normal = CalcRoundedSlantBevel(input, bodyColor, dist2, dx, dy);
    float4 result = CalcDiffuse(bodyColor, normal);
    result += CalcSpecular(bodyColor.a, normal);

    return result;
}	// end of OutlineTwoToneNoShadowRoundedSlantBevelPS()
float4 OutlineTwoToneNoShadowRoundBevelPS(VertexShaderOutput input) : COLOR0
{
    float dist2, dx, dy;
    float4 bodyColor = CalcOutlineTwoToneColor(input, dist2, dx, dy);
    float3 normal = CalcRoundBevel(input, bodyColor, dist2, dx, dy);
    float4 result = CalcDiffuse(bodyColor, normal);
    result += CalcSpecular(bodyColor.a, normal);

    return result;
}	// end of OutlineTwoToneNoShadowRoundBevelPS()

float4 OutlineTwoToneAltNoShadowSlantBevelPS(VertexShaderOutput input) : COLOR0
{
    float dist2, dx, dy;
    float4 bodyColor = CalcOutlineTwoToneAltColor(input, dist2, dx, dy);
    float3 normal = CalcSlantBevel(input, bodyColor, dist2, dx, dy);
    float4 result = CalcDiffuse(bodyColor, normal);
    result += CalcSpecular(bodyColor.a, normal);

    return result;
}	// end of OutlineTwoToneAltNoShadowSlantBevelPS()
float4 OutlineTwoToneAltNoShadowRoundedSlantBevelPS(VertexShaderOutput input) : COLOR0
{
    float dist2, dx, dy;
    float4 bodyColor = CalcOutlineTwoToneAltColor(input, dist2, dx, dy);
    float3 normal = CalcRoundedSlantBevel(input, bodyColor, dist2, dx, dy);
    float4 result = CalcDiffuse(bodyColor, normal);
    result += CalcSpecular(bodyColor.a, normal);

    return result;
}	// end of OutlineTwoToneAltNoShadowRoundedSlantBevelPS()
float4 OutlineTwoToneAltNoShadowRoundBevelPS(VertexShaderOutput input) : COLOR0
{
    float dist2, dx, dy;
    float4 bodyColor = CalcOutlineTwoToneAltColor(input, dist2, dx, dy);
    float3 normal = CalcRoundBevel(input, bodyColor, dist2, dx, dy);
    float4 result = CalcDiffuse(bodyColor, normal);
    result += CalcSpecular(bodyColor.a, normal);

    return result;
}	// end of OutlineTwoToneAltNoShadowRoundBevelPS()

//
// Outer shadow with bevel.
//
float4 TwoToneOuterShadowSlantBevelPS(VertexShaderOutput input) : COLOR0
{
    float dist2, dx, dy;
    float4 bodyColor = CalcTwoToneColor(input, dist2, dx, dy);
    float3 normal = CalcSlantBevel(input, bodyColor, dist2, dx, dy);
    float4 result = CalcDiffuse(bodyColor, normal);
    result += CalcSpecular(bodyColor.a, normal);
    result = ApplyOuterShadow(input, result);

    return result;
}	// end of TwoToneOuterShadowSlantBevelPS()
float4 TwoToneOuterShadowRoundedSlantBevelPS(VertexShaderOutput input) : COLOR0
{
    float dist2, dx, dy;
    float4 bodyColor = CalcTwoToneColor(input, dist2, dx, dy);
    float3 normal = CalcRoundedSlantBevel(input, bodyColor, dist2, dx, dy);
    float4 result = CalcDiffuse(bodyColor, normal);
    result += CalcSpecular(bodyColor.a, normal);
    result = ApplyOuterShadow(input, result);

    return result;
}	// end of TwoToneOuterShadowRoundedSlantBevelPS()
float4 TwoToneOuterShadowRoundBevelPS(VertexShaderOutput input) : COLOR0
{
    float dist2, dx, dy;
    float4 bodyColor = CalcTwoToneColor(input, dist2, dx, dy);
    float3 normal = CalcRoundBevel(input, bodyColor, dist2, dx, dy);
    float4 result = CalcDiffuse(bodyColor, normal);
    result += CalcSpecular(bodyColor.a, normal);
    result = ApplyOuterShadow(input, result);

    return result;
}	// end of TwoToneOuterShadowRoundBevelPS()

float4 OutlineTwoToneOuterShadowSlantBevelPS(VertexShaderOutput input) : COLOR0
{
    float dist2, dx, dy;
    float4 bodyColor = CalcOutlineTwoToneColor(input, dist2, dx, dy);
    float3 normal = CalcSlantBevel(input, bodyColor, dist2, dx, dy);
    float4 result = CalcDiffuse(bodyColor, normal);
    result += CalcSpecular(bodyColor.a, normal);
    result = ApplyOuterShadow(input, result);

    return result;
}	// end of OutlineTwoToneOuterShadowSlantBevelPS()
float4 OutlineTwoToneOuterShadowRoundedSlantBevelPS(VertexShaderOutput input) : COLOR0
{
    float dist2, dx, dy;
    float4 bodyColor = CalcOutlineTwoToneColor(input, dist2, dx, dy);
    float3 normal = CalcRoundedSlantBevel(input, bodyColor, dist2, dx, dy);
    float4 result = CalcDiffuse(bodyColor, normal);
    result += CalcSpecular(bodyColor.a, normal);
    result = ApplyOuterShadow(input, result);

    return result;
}	// end of OutlineTwoToneOuterShadowRoundedSlantBevelPS()
float4 OutlineTwoToneOuterShadowRoundBevelPS(VertexShaderOutput input) : COLOR0
{
    float dist2, dx, dy;
    float4 bodyColor = CalcOutlineTwoToneColor(input, dist2, dx, dy);
    float3 normal = CalcRoundBevel(input, bodyColor, dist2, dx, dy);
    float4 result = CalcDiffuse(bodyColor, normal);
    result += CalcSpecular(bodyColor.a, normal);
    result = ApplyOuterShadow(input, result);

    return result;
}	// end of OutlineTwoToneOuterShadowRoundBevelPS()

float4 OutlineTwoToneAltOuterShadowSlantBevelPS(VertexShaderOutput input) : COLOR0
{
    float dist2, dx, dy;
    float4 bodyColor = CalcOutlineTwoToneAltColor(input, dist2, dx, dy);
    float3 normal = CalcSlantBevel(input, bodyColor, dist2, dx, dy);
    float4 result = CalcDiffuse(bodyColor, normal);
    result += CalcSpecular(bodyColor.a, normal);
    result = ApplyOuterShadow(input, result);

    return result;
}	// end of OutlineTwoToneAltOuterShadowSlantBevelPS()
float4 OutlineTwoToneAltOuterShadowRoundedSlantBevelPS(VertexShaderOutput input) : COLOR0
{
    float dist2, dx, dy;
    float4 bodyColor = CalcOutlineTwoToneAltColor(input, dist2, dx, dy);
    float3 normal = CalcRoundedSlantBevel(input, bodyColor, dist2, dx, dy);
    float4 result = CalcDiffuse(bodyColor, normal);
    result += CalcSpecular(bodyColor.a, normal);
    result = ApplyOuterShadow(input, result);

    return result;
}	// end of OutlineTwoToneAltOuterShadowRoundedSlantBevelPS()
float4 OutlineTwoToneAltOuterShadowRoundBevelPS(VertexShaderOutput input) : COLOR0
{
    float dist2, dx, dy;
    float4 bodyColor = CalcOutlineTwoToneAltColor(input, dist2, dx, dy);
    float3 normal = CalcRoundBevel(input, bodyColor, dist2, dx, dy);
    float4 result = CalcDiffuse(bodyColor, normal);
    result += CalcSpecular(bodyColor.a, normal);
    result = ApplyOuterShadow(input, result);

    return result;
}	// end of OutlineTwoToneAltOuterShadowRoundBevelPS()

//
// Inner shadow, with bevel.
//
float4 TwoToneInnerShadowSlantBevelPS(VertexShaderOutput input) : COLOR0
{
    float dist2, dx, dy;
    float4 bodyColor = CalcTwoToneColor(input, dist2, dx, dy);
    float3 normal = CalcSlantBevel(input, bodyColor, dist2, dx, dy);
    float4 result = CalcDiffuse(bodyColor, normal);
    result += CalcSpecular(bodyColor.a, normal);
    result = ApplyInnerShadow(input, result);

    return result;
}	// end of TwoToneInnerShadowSlantBevelPS()
float4 TwoToneInnerShadowRoundedSlantBevelPS(VertexShaderOutput input) : COLOR0
{
    float dist2, dx, dy;
    float4 bodyColor = CalcTwoToneColor(input, dist2, dx, dy);
    float3 normal = CalcRoundedSlantBevel(input, bodyColor, dist2, dx, dy);
    float4 result = CalcDiffuse(bodyColor, normal);
    result += CalcSpecular(bodyColor.a, normal);
    result = ApplyInnerShadow(input, result);

    return result;
}	// end of TwoToneInnerShadowRoundedSlantBevelPS()
float4 TwoToneInnerShadowRoundBevelPS(VertexShaderOutput input) : COLOR0
{
    float dist2, dx, dy;
    float4 bodyColor = CalcTwoToneColor(input, dist2, dx, dy);
    float3 normal = CalcRoundBevel(input, bodyColor, dist2, dx, dy);
    float4 result = CalcDiffuse(bodyColor, normal);
    result += CalcSpecular(bodyColor.a, normal);
    result = ApplyInnerShadow(input, result);

    return result;
}	// end of TwoToneInnerShadowRoundBevelPS()

float4 OutlineTwoToneInnerShadowSlantBevelPS(VertexShaderOutput input) : COLOR0
{
    float dist2, dx, dy;
    float4 bodyColor = CalcOutlineTwoToneColor(input, dist2, dx, dy);
    float3 normal = CalcSlantBevel(input, bodyColor, dist2, dx, dy);
    float4 result = CalcDiffuse(bodyColor, normal);
    result += CalcSpecular(bodyColor.a, normal);
    result = ApplyInnerShadow(input, result);

    return result;
}	// end of OutlineTwoToneInnerShadowSlantBevelPS()
float4 OutlineTwoToneInnerShadowRoundedSlantBevelPS(VertexShaderOutput input) : COLOR0
{
    float dist2, dx, dy;
    float4 bodyColor = CalcOutlineTwoToneColor(input, dist2, dx, dy);
    float3 normal = CalcRoundedSlantBevel(input, bodyColor, dist2, dx, dy);
    float4 result = CalcDiffuse(bodyColor, normal);
    result += CalcSpecular(bodyColor.a, normal);
    result = ApplyInnerShadow(input, result);

    return result;
}	// end of OutlineTwoToneInnerShadowRoundedSlantBevelPS()
float4 OutlineTwoToneInnerShadowRoundBevelPS(VertexShaderOutput input) : COLOR0
{
    float dist2, dx, dy;
    float4 bodyColor = CalcOutlineTwoToneColor(input, dist2, dx, dy);
    float3 normal = CalcRoundBevel(input, bodyColor, dist2, dx, dy);
    float4 result = CalcDiffuse(bodyColor, normal);
    result += CalcSpecular(bodyColor.a, normal);
    result = ApplyInnerShadow(input, result);

    return result;
}	// end of OutlineTwoToneInnerShadowRoundBevelPS()

float4 OutlineTwoToneAltInnerShadowSlantBevelPS(VertexShaderOutput input) : COLOR0
{
    float dist2, dx, dy;
    float4 bodyColor = CalcOutlineTwoToneAltColor(input, dist2, dx, dy);
    float3 normal = CalcSlantBevel(input, bodyColor, dist2, dx, dy);
    float4 result = CalcDiffuse(bodyColor, normal);
    result += CalcSpecular(bodyColor.a, normal);
    result = ApplyInnerShadow(input, result);

    return result;
}	// end of OutlineTwoToneAltInnerShadowSlantBevelPS()
float4 OutlineTwoToneAltInnerShadowRoundedSlantBevelPS(VertexShaderOutput input) : COLOR0
{
    float dist2, dx, dy;
    float4 bodyColor = CalcOutlineTwoToneAltColor(input, dist2, dx, dy);
    float3 normal = CalcRoundedSlantBevel(input, bodyColor, dist2, dx, dy);
    float4 result = CalcDiffuse(bodyColor, normal);
    result += CalcSpecular(bodyColor.a, normal);
    result = ApplyInnerShadow(input, result);

    return result;
}	// end of OutlineTwoToneAltInnerShadowRoundedSlantBevelPS()
float4 OutlineTwoToneAltInnerShadowRoundBevelPS(VertexShaderOutput input) : COLOR0
{
    float dist2, dx, dy;
    float4 bodyColor = CalcOutlineTwoToneAltColor(input, dist2, dx, dy);
    float3 normal = CalcRoundBevel(input, bodyColor, dist2, dx, dy);
    float4 result = CalcDiffuse(bodyColor, normal);
    result += CalcSpecular(bodyColor.a, normal);
    result = ApplyInnerShadow(input, result);

    return result;
}	// end of OutlineTwoToneAltInnerShadowRoundBevelPS()

//
//
// Beveled with texture
//
//
float4 TwoToneNoShadowTextureSlantBevelPS(VertexShaderOutput input) : COLOR0
{
    float dist2, dx, dy;
    float4 bodyColor = CalcTwoToneColor(input, dist2, dx, dy);
    bodyColor = ApplyTextureColor(input, bodyColor);
    float3 normal = CalcSlantBevel(input, bodyColor, dist2, dx, dy);
    float4 result = CalcDiffuse(bodyColor, normal);
    result += CalcSpecular(bodyColor.a, normal);

    return result;
}	// end of TwoToneNoShadowTextureSlantBevelPS()
float4 TwoToneNoShadowTextureRoundedSlantBevelPS(VertexShaderOutput input) : COLOR0
{
    float dist2, dx, dy;
    float4 bodyColor = CalcTwoToneColor(input, dist2, dx, dy);
    bodyColor = ApplyTextureColor(input, bodyColor);
    float3 normal = CalcRoundedSlantBevel(input, bodyColor, dist2, dx, dy);
    float4 result = CalcDiffuse(bodyColor, normal);
    result += CalcSpecular(bodyColor.a, normal);

    return result;
}	// end of TwoToneNoShadowTextureRoundedSlantBevelPS()
float4 TwoToneNoShadowTextureRoundBevelPS(VertexShaderOutput input) : COLOR0
{
    float dist2, dx, dy;
    float4 bodyColor = CalcTwoToneColor(input, dist2, dx, dy);
    bodyColor = ApplyTextureColor(input, bodyColor);
    float3 normal = CalcRoundBevel(input, bodyColor, dist2, dx, dy);
    float4 result = CalcDiffuse(bodyColor, normal);
    result += CalcSpecular(bodyColor.a, normal);

    return result;
}	// end of TwoToneNoShadowTextureRoundBevelPS()

float4 OutlineTwoToneNoShadowTextureSlantBevelPS(VertexShaderOutput input) : COLOR0
{
    float dist2, dx, dy;
    float4 bodyColor = CalcOutlineTwoToneColor(input, dist2, dx, dy);
    bodyColor = ApplyTextureColor(input, bodyColor);
    float3 normal = CalcSlantBevel(input, bodyColor, dist2, dx, dy);
    float4 result = CalcDiffuse(bodyColor, normal);
    result += CalcSpecular(bodyColor.a, normal);

    return result;
}	// end of OutlineTwoToneNoShadowTextureSlantBevelPS()
float4 OutlineTwoToneNoShadowTextureRoundedSlantBevelPS(VertexShaderOutput input) : COLOR0
{
    float dist2, dx, dy;
    float4 bodyColor = CalcOutlineTwoToneColor(input, dist2, dx, dy);
    bodyColor = ApplyTextureColor(input, bodyColor);
    float3 normal = CalcRoundedSlantBevel(input, bodyColor, dist2, dx, dy);
    float4 result = CalcDiffuse(bodyColor, normal);
    result += CalcSpecular(bodyColor.a, normal);

    return result;
}	// end of OutlineTwoToneNoShadowTextureRoundedSlantBevelPS()
float4 OutlineTwoToneNoShadowTextureRoundBevelPS(VertexShaderOutput input) : COLOR0
{
    float dist2, dx, dy;
    float4 bodyColor = CalcOutlineTwoToneColor(input, dist2, dx, dy);
    bodyColor = ApplyTextureColor(input, bodyColor);
    float3 normal = CalcRoundBevel(input, bodyColor, dist2, dx, dy);
    float4 result = CalcDiffuse(bodyColor, normal);
    result += CalcSpecular(bodyColor.a, normal);

    return result;
}	// end of OutlineTwoToneNoShadowTextureRoundBevelPS()

float4 OutlineTwoToneAltNoShadowTextureSlantBevelPS(VertexShaderOutput input) : COLOR0
{
    float dist2, dx, dy;
    float4 bodyColor = CalcOutlineTwoToneAltColor(input, dist2, dx, dy);
    bodyColor = ApplyTextureColor(input, bodyColor);
    float3 normal = CalcSlantBevel(input, bodyColor, dist2, dx, dy);
    float4 result = CalcDiffuse(bodyColor, normal);
    result += CalcSpecular(bodyColor.a, normal);

    return result;
}	// end of OutlineTwoToneAltNoShadowTextureSlantBevelPS()
float4 OutlineTwoToneAltNoShadowTextureRoundedSlantBevelPS(VertexShaderOutput input) : COLOR0
{
    float dist2, dx, dy;
    float4 bodyColor = CalcOutlineTwoToneAltColor(input, dist2, dx, dy);
    bodyColor = ApplyTextureColor(input, bodyColor);
    float3 normal = CalcRoundedSlantBevel(input, bodyColor, dist2, dx, dy);
    float4 result = CalcDiffuse(bodyColor, normal);
    result += CalcSpecular(bodyColor.a, normal);

    return result;
}	// end of OutlineTwoToneAltNoShadowTextureRoundedSlantBevelPS()
float4 OutlineTwoToneAltNoShadowTextureRoundBevelPS(VertexShaderOutput input) : COLOR0
{
    float dist2, dx, dy;
    float4 bodyColor = CalcOutlineTwoToneAltColor(input, dist2, dx, dy);
    bodyColor = ApplyTextureColor(input, bodyColor);
    float3 normal = CalcRoundBevel(input, bodyColor, dist2, dx, dy);
    float4 result = CalcDiffuse(bodyColor, normal);
    result += CalcSpecular(bodyColor.a, normal);

    return result;
}	// end of OutlineTwoToneAltNoShadowTextureRoundBevelPS()

//
// Outer ShadowTexture with bevel.
//
float4 TwoToneOuterShadowTextureSlantBevelPS(VertexShaderOutput input) : COLOR0
{
    float dist2, dx, dy;
    float4 bodyColor = CalcTwoToneColor(input, dist2, dx, dy);
    bodyColor = ApplyTextureColor(input, bodyColor);
    float3 normal = CalcSlantBevel(input, bodyColor, dist2, dx, dy);
    float4 result = CalcDiffuse(bodyColor, normal);
    result += CalcSpecular(bodyColor.a, normal);
    result = ApplyOuterShadow(input, result);

    return result;
}	// end of TwoToneOuterShadowTextureSlantBevelPS()
float4 TwoToneOuterShadowTextureRoundedSlantBevelPS(VertexShaderOutput input) : COLOR0
{
    float dist2, dx, dy;
    float4 bodyColor = CalcTwoToneColor(input, dist2, dx, dy);
    bodyColor = ApplyTextureColor(input, bodyColor);
    float3 normal = CalcRoundedSlantBevel(input, bodyColor, dist2, dx, dy);
    float4 result = CalcDiffuse(bodyColor, normal);
    result += CalcSpecular(bodyColor.a, normal);
    result = ApplyOuterShadow(input, result);

    return result;
}	// end of TwoToneOuterShadowTextureRoundedSlantBevelPS()
float4 TwoToneOuterShadowTextureRoundBevelPS(VertexShaderOutput input) : COLOR0
{
    float dist2, dx, dy;
    float4 bodyColor = CalcTwoToneColor(input, dist2, dx, dy);
    bodyColor = ApplyTextureColor(input, bodyColor);
    float3 normal = CalcRoundBevel(input, bodyColor, dist2, dx, dy);
    float4 result = CalcDiffuse(bodyColor, normal);
    result += CalcSpecular(bodyColor.a, normal);
    result = ApplyOuterShadow(input, result);

    return result;
}	// end of TwoToneOuterShadowTextureRoundBevelPS()

float4 OutlineTwoToneOuterShadowTextureSlantBevelPS(VertexShaderOutput input) : COLOR0
{
    float dist2, dx, dy;
    float4 bodyColor = CalcOutlineTwoToneColor(input, dist2, dx, dy);
    bodyColor = ApplyTextureColor(input, bodyColor);
    float3 normal = CalcSlantBevel(input, bodyColor, dist2, dx, dy);
    float4 result = CalcDiffuse(bodyColor, normal);
    result += CalcSpecular(bodyColor.a, normal);
    result = ApplyOuterShadow(input, result);

    return result;
}	// end of OutlineTwoToneOuterShadowTextureSlantBevelPS()
float4 OutlineTwoToneOuterShadowTextureRoundedSlantBevelPS(VertexShaderOutput input) : COLOR0
{
    float dist2, dx, dy;
    float4 bodyColor = CalcOutlineTwoToneColor(input, dist2, dx, dy);
    bodyColor = ApplyTextureColor(input, bodyColor);
    float3 normal = CalcRoundedSlantBevel(input, bodyColor, dist2, dx, dy);
    float4 result = CalcDiffuse(bodyColor, normal);
    result += CalcSpecular(bodyColor.a, normal);
    result = ApplyOuterShadow(input, result);

    return result;
}	// end of OutlineTwoToneOuterShadowTextureRoundedSlantBevelPS()
float4 OutlineTwoToneOuterShadowTextureRoundBevelPS(VertexShaderOutput input) : COLOR0
{
    float dist2, dx, dy;
    float4 bodyColor = CalcOutlineTwoToneColor(input, dist2, dx, dy);
    bodyColor = ApplyTextureColor(input, bodyColor);
    float3 normal = CalcRoundBevel(input, bodyColor, dist2, dx, dy);
    float4 result = CalcDiffuse(bodyColor, normal);
    result += CalcSpecular(bodyColor.a, normal);
    result = ApplyOuterShadow(input, result);

    return result;
}	// end of OutlineTwoToneOuterShadowTextureRoundBevelPS()

float4 OutlineTwoToneAltOuterShadowTextureSlantBevelPS(VertexShaderOutput input) : COLOR0
{
    float dist2, dx, dy;
    float4 bodyColor = CalcOutlineTwoToneAltColor(input, dist2, dx, dy);
    bodyColor = ApplyTextureColor(input, bodyColor);
    float3 normal = CalcSlantBevel(input, bodyColor, dist2, dx, dy);
    float4 result = CalcDiffuse(bodyColor, normal);
    result += CalcSpecular(bodyColor.a, normal);
    result = ApplyOuterShadow(input, result);

    return result;
}	// end of OutlineTwoToneAltOuterShadowTextureSlantBevelPS()
float4 OutlineTwoToneAltOuterShadowTextureRoundedSlantBevelPS(VertexShaderOutput input) : COLOR0
{
    float dist2, dx, dy;
    float4 bodyColor = CalcOutlineTwoToneAltColor(input, dist2, dx, dy);
    bodyColor = ApplyTextureColor(input, bodyColor);
    float3 normal = CalcRoundedSlantBevel(input, bodyColor, dist2, dx, dy);
    float4 result = CalcDiffuse(bodyColor, normal);
    result += CalcSpecular(bodyColor.a, normal);
    result = ApplyOuterShadow(input, result);

    return result;
}	// end of OutlineTwoToneAltOuterShadowTextureRoundedSlantBevelPS()
float4 OutlineTwoToneAltOuterShadowTextureRoundBevelPS(VertexShaderOutput input) : COLOR0
{
    float dist2, dx, dy;
    float4 bodyColor = CalcOutlineTwoToneAltColor(input, dist2, dx, dy);
    bodyColor = ApplyTextureColor(input, bodyColor);
    float3 normal = CalcRoundBevel(input, bodyColor, dist2, dx, dy);
    float4 result = CalcDiffuse(bodyColor, normal);
    result += CalcSpecular(bodyColor.a, normal);
    result = ApplyOuterShadow(input, result);

    return result;
}	// end of OutlineTwoToneAltOuterShadowTextureRoundBevelPS()

//
// Inner ShadowTexture, with bevel.
//
float4 TwoToneInnerShadowTextureSlantBevelPS(VertexShaderOutput input) : COLOR0
{
    float dist2, dx, dy;
    float4 bodyColor = CalcTwoToneColor(input, dist2, dx, dy);
    bodyColor = ApplyTextureColor(input, bodyColor);
    float3 normal = CalcSlantBevel(input, bodyColor, dist2, dx, dy);
    float4 result = CalcDiffuse(bodyColor, normal);
    result += CalcSpecular(bodyColor.a, normal);
    result = ApplyInnerShadow(input, result);

    return result;
}	// end of TwoToneInnerShadowTextureSlantBevelPS()
float4 TwoToneInnerShadowTextureRoundedSlantBevelPS(VertexShaderOutput input) : COLOR0
{
    float dist2, dx, dy;
    float4 bodyColor = CalcTwoToneColor(input, dist2, dx, dy);
    bodyColor = ApplyTextureColor(input, bodyColor);
    float3 normal = CalcRoundedSlantBevel(input, bodyColor, dist2, dx, dy);
    float4 result = CalcDiffuse(bodyColor, normal);
    result += CalcSpecular(bodyColor.a, normal);
    result = ApplyInnerShadow(input, result);

    return result;
}	// end of TwoToneInnerShadowTextureRoundedSlantBevelPS()
float4 TwoToneInnerShadowTextureRoundBevelPS(VertexShaderOutput input) : COLOR0
{
    float dist2, dx, dy;
    float4 bodyColor = CalcTwoToneColor(input, dist2, dx, dy);
    bodyColor = ApplyTextureColor(input, bodyColor);
    float3 normal = CalcRoundBevel(input, bodyColor, dist2, dx, dy);
    float4 result = CalcDiffuse(bodyColor, normal);
    result += CalcSpecular(bodyColor.a, normal);
    result = ApplyInnerShadow(input, result);

    return result;
}	// end of TwoToneInnerShadowTextureRoundBevelPS()

float4 OutlineTwoToneInnerShadowTextureSlantBevelPS(VertexShaderOutput input) : COLOR0
{
    float dist2, dx, dy;
    float4 bodyColor = CalcOutlineTwoToneColor(input, dist2, dx, dy);
    bodyColor = ApplyTextureColor(input, bodyColor);
    float3 normal = CalcSlantBevel(input, bodyColor, dist2, dx, dy);
    float4 result = CalcDiffuse(bodyColor, normal);
    result += CalcSpecular(bodyColor.a, normal);
    result = ApplyInnerShadow(input, result);

    return result;
}	// end of OutlineTwoToneInnerShadowTextureSlantBevelPS()
float4 OutlineTwoToneInnerShadowTextureRoundedSlantBevelPS(VertexShaderOutput input) : COLOR0
{
    float dist2, dx, dy;
    float4 bodyColor = CalcOutlineTwoToneColor(input, dist2, dx, dy);
    bodyColor = ApplyTextureColor(input, bodyColor);
    float3 normal = CalcRoundedSlantBevel(input, bodyColor, dist2, dx, dy);
    float4 result = CalcDiffuse(bodyColor, normal);
    result += CalcSpecular(bodyColor.a, normal);
    result = ApplyInnerShadow(input, result);

    return result;
}	// end of OutlineTwoToneInnerShadowTextureRoundedSlantBevelPS()
float4 OutlineTwoToneInnerShadowTextureRoundBevelPS(VertexShaderOutput input) : COLOR0
{
    float dist2, dx, dy;
    float4 bodyColor = CalcOutlineTwoToneColor(input, dist2, dx, dy);
    bodyColor = ApplyTextureColor(input, bodyColor);
    float3 normal = CalcRoundBevel(input, bodyColor, dist2, dx, dy);
    float4 result = CalcDiffuse(bodyColor, normal);
    result += CalcSpecular(bodyColor.a, normal);
    result = ApplyInnerShadow(input, result);

    return result;
}	// end of OutlineTwoToneInnerShadowTextureRoundBevelPS()

float4 OutlineTwoToneAltInnerShadowTextureSlantBevelPS(VertexShaderOutput input) : COLOR0
{
    float dist2, dx, dy;
    float4 bodyColor = CalcOutlineTwoToneAltColor(input, dist2, dx, dy);
    bodyColor = ApplyTextureColor(input, bodyColor);
    float3 normal = CalcSlantBevel(input, bodyColor, dist2, dx, dy);
    float4 result = CalcDiffuse(bodyColor, normal);
    result += CalcSpecular(bodyColor.a, normal);
    result = ApplyInnerShadow(input, result);

    return result;
}	// end of OutlineTwoToneAltInnerShadowTextureSlantBevelPS()
float4 OutlineTwoToneAltInnerShadowTextureRoundedSlantBevelPS(VertexShaderOutput input) : COLOR0
{
    float dist2, dx, dy;
    float4 bodyColor = CalcOutlineTwoToneAltColor(input, dist2, dx, dy);
    bodyColor = ApplyTextureColor(input, bodyColor);
    float3 normal = CalcRoundedSlantBevel(input, bodyColor, dist2, dx, dy);
    float4 result = CalcDiffuse(bodyColor, normal);
    result += CalcSpecular(bodyColor.a, normal);
    result = ApplyInnerShadow(input, result);

    return result;
}	// end of OutlineTwoToneAltInnerShadowTextureRoundedSlantBevelPS()
float4 OutlineTwoToneAltInnerShadowTextureRoundBevelPS(VertexShaderOutput input) : COLOR0
{
    float dist2, dx, dy;
    float4 bodyColor = CalcOutlineTwoToneAltColor(input, dist2, dx, dy);
    bodyColor = ApplyTextureColor(input, bodyColor);
    float3 normal = CalcRoundBevel(input, bodyColor, dist2, dx, dy);
    float4 result = CalcDiffuse(bodyColor, normal);
    result += CalcSpecular(bodyColor.a, normal);
    result = ApplyInnerShadow(input, result);

    return result;
}	// end of OutlineTwoToneAltInnerShadowTextureRoundBevelPS()








