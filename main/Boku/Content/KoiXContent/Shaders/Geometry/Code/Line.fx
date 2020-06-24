
//
// Line shader
//
// Renders anti-aliased lines segments.  All info is put into each vertex
// so we don't need any shader parameters.  This allows us to batch up
// multiple line segments into a single call.
//

// The world view and projection matrices
float4x4 WorldViewProjMatrix;

//
// VS input/output structures.
//
struct VertexShaderInput
{
    float2 Pixels       : POSITION0;
    float4 Point0       : TEXCOORD0;    // XY = Pixel position, Z = radius, W = capped.
    float4 Point1       : TEXCOORD1;
    float4 Color        : COLOR0;
    float EdgeBlend     : TEXCOORD2;
};

struct VertexShaderOutput
{
    float4 Position     : POSITION0;
    float2 Pixels       : TEXCOORD0;
    float4 Point0       : TEXCOORD1;
    float4 Point1       : TEXCOORD2;
    float4 Color        : COLOR0;
    float EdgeBlend     : TEXCOORD3;

    float3 Axis         : TEXCOORD4;    // Normalized vector from Point0 to Point1, length of axis.
};

VertexShaderOutput VS(VertexShaderInput input)
{
    VertexShaderOutput output;

    output.Position = mul(float4(input.Pixels, 0, 1), WorldViewProjMatrix);

    output.Pixels = input.Pixels;
    output.Point0 = input.Point0;
    output.Point1 = input.Point1;
    output.Color = input.Color;
    output.EdgeBlend = input.EdgeBlend;
    
    // Pre-calc some useful values.
    float2 axis = input.Point1 - input.Point0;
    output.Axis.z = length(axis);
    output.Axis.xy = normalize(axis);

    return output;
}   // end of VS()

//
// CalcCapAlpha -- Calculates the alpha value for pixels in the cap region.
//
// curPoint is the current pixel location.
// linePoint is the near line endpoint.
// radius is the radius of the line at this endpoint.
float CalcCapAlpha(float2 curPoint, float2 linePoint, float radius, float edgeBlend)
{
    float minRadius = radius - edgeBlend;
    float maxRadius = radius + edgeBlend;
    float curRadius = length(curPoint - linePoint);
    float alpha = 1;
    if (curRadius < minRadius)
    {
        // In solid region.
        alpha = 1;
    }
    else if(curRadius > maxRadius)
    {
        // Outside.
        alpha = 0;
    }
    else
    {
        // Somewhere in the blend.
        alpha = 1 - (curRadius - minRadius) / (2 * edgeBlend);
    }

    return alpha;
}   // end of CalcCapAlpha()

float4 PS(VertexShaderOutput input) : COLOR0
{
    // Vector from Point0 to current pixel.
    float2 dir = input.Pixels - input.Point0;
    // Project this onto axis and scale into 0, 1 range.
    // Note this will slightly exceed that range on caps.
    float t = dot(dir, input.Axis.xy) / input.Axis.z;

    float4 result = input.Color;
    float alpha = 1;

    // Figure out alpha.
    if (t < 0)
    {
        // Cap at Point0 end.
        alpha = CalcCapAlpha(input.Pixels, input.Point0.xy, input.Point0.z, input.EdgeBlend);
    }
    else if (t > 1)
    {
        // Cap at Point1 end.
        alpha = CalcCapAlpha(input.Pixels, input.Point1.xy, input.Point1.z, input.EdgeBlend);
    }
    else
    {
        // Somewhere in the line segment.  Calc where and how far from line we are.
        float2 pointOnLine = input.Point0 + t * input.Axis.xy * input.Axis.z;
        float curRadius = length(input.Pixels - pointOnLine);

        // Calc radius of line at this point.
        float radius = lerp(input.Point0.z, input.Point1.z, t);

        float minRadius = radius - input.EdgeBlend;
        float maxRadius = radius + input.EdgeBlend;

        // Calc alpha.
        if (curRadius < minRadius)
        {
            // In solid region.
            alpha = 1;
        }
        else if(curRadius > maxRadius)
        {
            // Outside.
            alpha = 0;
        }
        else
        {
            // Somewhere in the blend.
            alpha = 1 - (curRadius - minRadius) / (2 * input.EdgeBlend);
        }
    }

    // Apply pre-mult alpha.
    result.rgb *= (alpha * result.a);
    result.a *= alpha;

    // Debug : see the totally transparent pixels.
    /*
    if(result.a == 0)
    {
        result.rgba = 0.5;
    }
    */

    return result;
}   // end of PS()





