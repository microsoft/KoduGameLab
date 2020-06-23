
#ifndef FLEX_H
#define FLEX_H

float       Flex;                   // Flex amount for fish body.
float3      BoneOffset;             // FishBone translation.


float3 ApplyFlex(in float3 pos, in float3 normal)
{
    // Apply flex to position and normal.
    float theta = pos.x * Flex;
    if( pos.x > 0 )
        theta *= 0.5f;
    float s;
    float c;
    sincos( theta, s, c );
    float2 xy = float2( pos.x * c - pos.y * s, pos.x * s + pos.y * c );
    pos.xy = xy;
    xy = float2( normal.x * c - normal.y * s, normal.x * s + normal.y * c );
    normal.xy = xy;
    
    return pos;
}

#endif // FLEX_H
