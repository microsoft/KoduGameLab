
//
// Line2D shader
//
// Renders anti-aliased lines segments and arcs.  All info is put into
// each vertex so we don't need any shader parameters.  This allows us
// to batch up multiple line segments into a single call.
//

// The world view and projection matrices
float4x4 WorldViewProjMatrix;
float Zoom;                             // Current zoom factor.  Needed to adjust edgeBlend.

// new VertexElement(0, VertexElementFormat.Vector2, VertexElementUsage.Position, 0),              // Position of vertex in pixel coords.
// new VertexElement(8, VertexElementFormat.Vector3, VertexElementUsage.TextureCoordinate, 0),     // line(xy: point0, z: radius0(stroke)) dot(xy: point0, z: radius0(stroke)) arc(xy: center, z:radius(stroke))
// new VertexElement(20, VertexElementFormat.Vector3, VertexElementUsage.TextureCoordinate, 1),    // line(xy: point1, z: radius1(stroke)) dot() arc(z: radius(arc))
// new VertexElement(32, VertexElementFormat.Vector4, VertexElementUsage.Color, 0),                // color
// new VertexElement(48, VertexElementFormat.Single, VertexElementUsage.TextureCoordinate, 2),     // x:0 = line segment, 1 = dot, 2 = arc
// new VertexElement(48, VertexElementFormat.Single, VertexElementUsage.TextureCoordinate, 3),     // x:edgeBlend

//
// VS input/output structures.
//
struct VertexShaderInput
{
    float2 Pixels       : POSITION0;
    float3 Point0       : TEXCOORD0;
    float3 Point1       : TEXCOORD1;
    float4 Color        : COLOR0;
    float  Prim         : TEXCOORD2;
    float  EdgeBlend    : TEXCOORD3;
};

struct VertexShaderOutput
{
    float4 Position     : POSITION0;
    float2 Pixels       : TEXCOORD0;
    float3 Point0       : TEXCOORD1;
    float3 Point1       : TEXCOORD2;
    float4 Color        : COLOR0;
    float Prim          : TEXCOORD3;
    float EdgeBlend     : TEXCOORD4;

    float3 Axis         : TEXCOORD5;    // Normalized vector from Point0 to Point1, length of axis.
};

VertexShaderOutput VS(VertexShaderInput input)
{
    VertexShaderOutput output;

    output.Position = mul(float4(input.Pixels, 0, 1), WorldViewProjMatrix);

    output.Pixels = input.Pixels;
    output.Point0 = input.Point0;
    output.Point1 = input.Point1;
    output.Color = input.Color;
    output.Prim = input.Prim;
    output.EdgeBlend = input.EdgeBlend / Zoom;
    
    // Pre-calc some useful values.
    float2 axis = input.Point1 - input.Point0;
    output.Axis.z = length(axis);
    output.Axis.xy = normalize(axis);

    return output;
}   // end of VS()

float4 PS(VertexShaderOutput input) : COLOR0
{
    float edgeBlend = input.EdgeBlend;
    float4 result = float4(0, 0, 0, 1);

    uint prim = (uint)abs(input.Prim);
    [flatten]switch(prim)
    {
        case 0 : // none
            result = float4(1, 1, 1, 1);
            break;

        case 1 : // dot
            {
                float2 center = input.Point0.xy;
                float radius = input.Point0.z;
                float2 curPoint = input.Pixels;

                // TODO (scoy) Push some of this into the VS since it will be used for each pixel.
                float minRadius = radius - edgeBlend;
                float maxRadius = radius + edgeBlend;
                float curRadius = length(curPoint - center);

                float alpha = 1;
                if (curRadius < minRadius)
                {
                    // In solid region.
                    alpha = 1;
                }
                else if (curRadius > maxRadius)
                {
                    // Outside.
                    alpha = 0;
                }
                else
                {
                    // Somewhere in the blend.
                    alpha = 1 - (curRadius - minRadius) / (2 * edgeBlend);
                }

                result = input.Color * alpha;
                //result = float4(1,0,0,1);
            }
            break;

        case 2 : // line
            {
                // Vector from Point0 to current pixel.
                float2 dir = input.Pixels - input.Point0.xy;

                // Project this onto axis and scale into 0, 1 range.
                // Note this will slightly exceed that range on caps.
                float t = dot(dir, input.Axis.xy) / input.Axis.z;

                float2 pointOnLine = input.Point0 + t * input.Axis.xy * input.Axis.z;
                float curRadius = length(input.Pixels - pointOnLine);

                // Calc radius of line at this point.
                float radius = lerp(input.Point0.z, input.Point1.z, t);

                float minRadius = radius - edgeBlend;
                float maxRadius = radius + edgeBlend;

                float alpha = 0;

                // Calc alpha.
                if (curRadius < minRadius)
                {
                    // In solid region.
                    alpha = 1;
                }
                else if (curRadius > maxRadius)
                {
                    // Outside.
                    alpha = 0;
                }
                else
                {
                    // Somewhere in the blend.
                    alpha = 1 - (curRadius - minRadius) / (2 * edgeBlend);
                }

                result = input.Color * alpha;
                //result = float4(0, 1, 0, 1);
            }
            break;

        case 3 : // arc
            {
                float2 arcCenter = input.Point0.xy;
                float arcRadius = input.Point1.z;
                float strokeRadius = input.Point0.z;
                float minRadius = strokeRadius - edgeBlend;
                float maxRadius = strokeRadius + edgeBlend;

                float2 delta = arcCenter - input.Pixels;
                float radius = length(delta);
                // Convert for pure radius to distance from centerline of stroke.
                radius = abs(radius - arcRadius);

                float alpha;
                if (radius < minRadius)
                {
                    alpha = 1;
                }
                else if (radius > maxRadius)
                {
                    alpha = 0;
                }
                else
                {
                    // Somewhere in the blend.
                    alpha = 1 - (radius - minRadius) / (2 * edgeBlend);
                }

                result = input.Color * alpha;
            }
            //result = float4(0, 0, 1, 1);
            break;

        default:
            result = float4(1, 1, 0, 1);
            break;
    }

    return result;
}   // end of PS()

