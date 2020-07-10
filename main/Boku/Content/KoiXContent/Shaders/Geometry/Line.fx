// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.


//
// Line shader
//
// Renders anti-aliased lines segments.  All info is put into each vertex
// so we don't need any shader parameters.  This allows us to batch up
// multiple line segments into a single call.
//

#include "Code\Line.fx"

technique LineSegment
{
    pass Pass1
    {
        // Premultiplied alpha blending.
        AlphaBlendEnable = true;
        SrcBlend = One;
        DestBlend = InvSrcAlpha;

        ZEnable = false;
        ZFunc = LessEqual;
        ZWriteEnable = false;

        CullMode = NONE;

        VertexShader = compile vs_2_0 VS();
        PixelShader = compile ps_2_0 PS();
    }
}
