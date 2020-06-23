
#ifndef FOG_FX
#define FOG_FX

//
// Fog
//

// TODO this can be optimized by pre-calulating the end-start value.

float   CalcFog( float dist )
{
    float result = saturate(dist * FogVector.x + FogVector.y);
    return min(result, FogVector.z);
}   // end of CalcFog()

#endif // FOG_FX
