
#ifndef DOF_FX
#define DOF_FX

//
// Depth of Field
//

//
// TODO Tune this.  Why store near when what we want is focal - near?
//                  Why store far when what we want is far - focal?
//

//
// Returns amount of blur.  0 -- none, 1.0 -- full
//
float4 CalcDOF( float depth ) // depth is in view space
{
    float4 result = float4(0.0f, 0.0f, 1.0f, 1.0f);

	result.g = depth / DOF_FarPlane;
	
	result.r = DOF_MaxBlur * saturate(abs(1.0f - DOF_FocalPlane / depth));

    return result;
}     // end of CalcDOF()

#endif // DOF_FX
