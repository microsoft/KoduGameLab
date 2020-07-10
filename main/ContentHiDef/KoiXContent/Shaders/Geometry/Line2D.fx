// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.


//
// Line2D techniques, HiDef
//

#include "..\..\..\..\Boku\Content\KoiXContent\Shaders\Geometry\Code\Line2D.fx"

technique Line2D
{
    pass Pass1
    {
        VertexShader = compile vs_3_0 VS();
        PixelShader = compile ps_3_0 PS();
    }
}
