// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.


//
// Line2D techniques
//

#include "Code\Line2D.fx"

technique Line2D
{
    pass Pass1
    {
        //CullMode = NONE;

        VertexShader = compile vs_2_0 VS();
        PixelShader = compile ps_2_0 PS();
    }
}
