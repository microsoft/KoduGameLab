
#ifndef FACE_H
#define FACE_H


// Face specifics
float4      UpperLidLeft; // N.x, N.y, dot(N, C), C.x
float4      LowerLidLeft; // N.x, N.y, dot(N, C), C.x
float4      UpperLidRight; // N.x, N.y, dot(N, C), C.x
float4      LowerLidRight; // N.x, N.y, dot(N, C), C.x

float4		FaceBkg;
float4		PupilScale;
float4		PupilOffset;
float4      BrowScale;
float4      BrowOffset;
texture EyeShapeLeftTexture;
texture EyePupilLeftTexture;
texture EyeBrowLeftTexture;
texture EyeShapeRightTexture;
texture EyePupilRightTexture;
texture EyeBrowRightTexture;

sampler2D EyeShapeLeftTextureSampler =
sampler_state
{
	Texture = <EyeShapeLeftTexture>;
	MipFilter = Linear;
	MinFilter = Linear;
	MagFilter = Linear;
	
	AddressU = Clamp;
	AddressV = Clamp;
};
sampler2D EyePupilLeftTextureSampler =
sampler_state
{
	Texture = <EyePupilLeftTexture>;
	MipFilter = Linear;
	MinFilter = Linear;
	MagFilter = Linear;
	
	AddressU = Clamp;
	AddressV = Clamp;
};
sampler2D EyeBrowLeftTextureSampler = 
sampler_state
{
	Texture = <EyeBrowLeftTexture>;
	MipFilter = Linear;
	MinFilter = Linear;
	MagFilter = Linear;
	
	AddressU = Clamp;
	AddressV = Clamp;
};
sampler2D EyeShapeRightTextureSampler =
sampler_state
{
	Texture = <EyeShapeRightTexture>;
	MipFilter = Linear;
	MinFilter = Linear;
	MagFilter = Linear;
	
	AddressU = Clamp;
	AddressV = Clamp;
};
sampler2D EyePupilRightTextureSampler =
sampler_state
{
	Texture = <EyePupilRightTexture>;
	MipFilter = Linear;
	MinFilter = Linear;
	MagFilter = Linear;
	
	AddressU = Clamp;
	AddressV = Clamp;
};
sampler2D EyeBrowRightTextureSampler =
sampler_state
{
	Texture = <EyeBrowRightTexture>;
	MipFilter = Linear;
	MinFilter = Linear;
	MagFilter = Linear;
	
	AddressU = Clamp;
	AddressV = Clamp;
};

float4 BokuFace(float2 textureUV)
{
	// left => .xy, right => .zw
	float4 pupilUV = textureUV.xyxy * PupilScale + PupilOffset;
    float4 browUV = textureUV.xyxy * BrowScale + BrowOffset;
	
    float4 shapeLeft = tex2D( EyeShapeLeftTextureSampler, textureUV );
    
    float4 pupilLeft = tex2D( EyePupilLeftTextureSampler, pupilUV.xy );
    float4 browLeft  = tex2D( EyeBrowLeftTextureSampler, browUV.xy );
    
    shapeLeft.rgb = lerp(shapeLeft.rgb, browLeft.rgb, browLeft.aaa);
    shapeLeft.a = max(shapeLeft.a, browLeft.a);

    float4 shapeRight = tex2D( EyeShapeRightTextureSampler, textureUV );

    float4 pupilRight = tex2D( EyePupilRightTextureSampler, pupilUV.zw );
    float4 browRight  = tex2D( EyeBrowRightTextureSampler, browUV.zw );

	shapeRight.rgb = lerp(shapeRight.rgb, browRight.rgb, browRight.aaa);
    shapeRight.a = max(shapeRight.a, browRight.a);

    float4 shape = textureUV.x < 0.5f 
        ? shapeLeft
        : shapeRight;

    shape.rgb *= pupilLeft.rgb;
    shape.rgb *= pupilRight.rgb;

    float4 diffuse = float4(lerp(FaceBkg.rgb, saturate(shape.rgb), shape.a), 1.0f);

    return diffuse;
}

float FaceAnd(float lhs, float rhs)
{
    return lhs * rhs;
}
float FaceOr(float lhs, float rhs)
{
    return 1.0f - (1.0f - lhs) * (1.0f - rhs);
}
float3 FaceOr(float3 lhs, float3 rhs)
{
    return 1.0f - (1.0f - lhs) * (1.0f - rhs);
}
float TestLid(float4 lid, float2 textureUV)
{
    return dot(lid.xy, textureUV.xy) < lid.z ? 0.0f : 1.0f;
}
float TestLidLeft(float4 lid, float2 textureUV)
{
    float test = TestLid(lid, textureUV);

    test = FaceAnd(test, (textureUV.x > lid.w ? 1.0f : 0.0f));

    return test;
}
float TestLidRight(float4 lid, float2 textureUV)
{
    float test = TestLid(lid, textureUV);

    test = FaceAnd(test, (textureUV.x < lid.w ? 1.0f : 0.0f));

    return test;
}
float4 WideFace(float2 textureUV, float4 diffuse)
{
//return TestLidLeft(LowerLidLeft, textureUV).xxxx;
//return textureUV.x > 0.0f ? textureUV.xxxx : textureUV.yyyy;
    float test = TestLidLeft(UpperLidLeft, textureUV);

    test = FaceOr(test, TestLidRight(UpperLidRight, textureUV));

//test = 0.0f;
    test = FaceOr(test, TestLidLeft(LowerLidLeft, textureUV));

    test = FaceOr(test, TestLidRight(LowerLidRight, textureUV));

	float4 pupilUV = textureUV.xyxy * PupilScale + PupilOffset;
    float4 pupilLeft = tex2D( EyePupilLeftTextureSampler, pupilUV.xy );

    float4 pupilRight = tex2D( EyePupilRightTextureSampler, pupilUV.zw );

    float4 eyeColor = FaceBkg;
    eyeColor.rgb *= pupilLeft.rgb * pupilRight.rgb;

    eyeColor = test > 0 ? diffuse : eyeColor;

    return eyeColor;
}

float4 TwoFace(float2 textureUV, float4 diffuse, float right)
{
    float test = TestLid(UpperLidLeft, textureUV) * (1.0f - right);

    test = FaceOr(test, TestLid(UpperLidRight, textureUV) * right);

//test = 0.0f;
    test = FaceOr(test, TestLid(LowerLidLeft, textureUV) * (1.0f - right));

    test = FaceOr(test, TestLid(LowerLidRight, textureUV) * right);

	float4 pupilUV = textureUV.xyxy * PupilScale + PupilOffset;
    float4 pupilLeft = tex2D( EyePupilLeftTextureSampler, pupilUV.xy );

    float4 pupilRight = tex2D( EyePupilRightTextureSampler, pupilUV.zw );

    float4 eyeColor = FaceBkg;
    eyeColor.rgb *= FaceOr(pupilLeft.rgb, right.xxx) * FaceOr(pupilRight.rgb, (1.0f - right).xxx);

    eyeColor = test > 0 ? diffuse : eyeColor;

    return eyeColor;
}

#endif // FACE_H
