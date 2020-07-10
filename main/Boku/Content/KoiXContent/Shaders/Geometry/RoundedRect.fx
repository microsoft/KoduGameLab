// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.


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
// For SM2 shaders where we have both a SlantBevel and a shadow we have to do the 
// shadow as a separate pass.
//

#include "Code\UILighting.fx"
#include "Code\RoundedRect.fx"

//
//
// Techniques
//
//

technique OuterShadowOnly
{
    pass Pass1
    {
        VertexShader = compile vs_2_0 VS();
        PixelShader = compile ps_2_0 OuterShadowOnlyPS();
    }
}

technique InnerShadowOnly
{
    pass Pass1
    {
        VertexShader = compile vs_2_0 VS();
        PixelShader = compile ps_2_0 InnerShadowOnlyPS();
    }
}

//
// No shadow, no Bevel versions.
//

technique NoShadow
{
    pass Pass1
    {
        VertexShader = compile vs_2_0 VS();
        PixelShader = compile ps_2_0 NoShadowPS();
    }
}

technique OutlineNoShadow
{
    pass Pass1
    {
        VertexShader = compile vs_2_0 VS();
        PixelShader = compile ps_2_0 OutlineNoShadowPS();
    }
}

technique OutlineAltNoShadow
{
    pass Pass1
    {
        VertexShader = compile vs_2_0 VS();
        PixelShader = compile ps_2_0 OutlineAltNoShadowPS();
    }
}

technique OuterShadow
{
    pass Pass1
    {
        VertexShader = compile vs_2_0 VS();
        PixelShader = compile ps_2_0 OuterShadowPS();
    }
}

technique InnerShadow
{
    pass Pass1
    {
        VertexShader = compile vs_2_0 VS();
        PixelShader = compile ps_2_0 InnerShadowPS();
    }
}

technique OutlineOuterShadow
{
    pass Pass1
    {
        VertexShader = compile vs_2_0 VS();
        PixelShader = compile ps_2_0 OutlineOuterShadowPS();
    }
}

technique OutlineAltOuterShadow
{
    pass Pass1
    {
        VertexShader = compile vs_2_0 VS();
        PixelShader = compile ps_2_0 OutlineAltOuterShadowPS();
    }
}

technique OutlineInnerShadow
{
    pass Pass1
    {
        VertexShader = compile vs_2_0 VS();
        PixelShader = compile ps_2_0 OutlineInnerShadowPS();
    }
}

technique OutlineAltInnerShadow
{
    pass Pass1
    {
        VertexShader = compile vs_2_0 VS();
        PixelShader = compile ps_2_0 OutlineAltInnerShadowPS();
    }
}

//
// Textured versions.
//

technique NoShadowTexture
{
    pass Pass1
    {
        VertexShader = compile vs_2_0 VS();
        PixelShader = compile ps_2_0 NoShadowTexturePS();
    }
}

technique OutlineNoShadowTexture
{
    pass Pass1
    {
        VertexShader = compile vs_2_0 VS();
        PixelShader = compile ps_2_0 OutlineNoShadowTexturePS();
    }
}

technique OutlineAltNoShadowTexture
{
    pass Pass1
    {
        VertexShader = compile vs_2_0 VS();
        PixelShader = compile ps_2_0 OutlineAltNoShadowTexturePS();
    }
}

technique OuterShadowTexture
{
    pass Pass1
    {
        VertexShader = compile vs_2_0 VS();
        PixelShader = compile ps_2_0 OuterShadowTexturePS();
    }
}

technique InnerShadowTexture
{
    pass Pass1
    {
        VertexShader = compile vs_2_0 VS();
        PixelShader = compile ps_2_0 InnerShadowTexturePS();
    }
}

technique OutlineOuterShadowTexture
{
    pass Pass1
    {
        VertexShader = compile vs_2_0 VS();
        PixelShader = compile ps_2_0 OutlineOuterShadowTexturePS();
    }
}

technique OutlineAltOuterShadowTexture
{
    pass Pass1
    {
        VertexShader = compile vs_2_0 VS();
        PixelShader = compile ps_2_0 OutlineAltOuterShadowTexturePS();
    }
}

technique OutlineInnerShadowTexture
{
    pass Pass1
    {
        VertexShader = compile vs_2_0 VS();
        PixelShader = compile ps_2_0 OutlineInnerShadowTexturePS();
    }
}

technique OutlineAltInnerShadowTexture
{
    pass Pass1
    {
        VertexShader = compile vs_2_0 VS();
        PixelShader = compile ps_2_0 OutlineAltInnerShadowTexturePS();
    }
}


//
// Non-textured, with SlantBevel.
//

technique NoShadowSlantBevel
{
    pass Pass1
    {
        VertexShader = compile vs_2_0 VS();
        PixelShader = compile ps_2_0 NoShadowSlantBevelPS();
    }
}

technique OutlineNoShadowSlantBevel
{
    pass Pass1
    {
        VertexShader = compile vs_2_0 VS();
        PixelShader = compile ps_2_0 OutlineNoShadowSlantBevelPS();
    }
}

technique OutlineAltNoShadowSlantBevel
{
    pass Pass1
    {
        VertexShader = compile vs_2_0 VS();
        PixelShader = compile ps_2_0 OutlineAltNoShadowSlantBevelPS();
    }
}

technique OuterShadowSlantBevel
{
    pass ShadowPass
    {
        VertexShader = compile vs_2_0 VS();
        PixelShader = compile ps_2_0 OuterShadowOnlyPS();
    }
    pass ShapePass
    {
        VertexShader = compile vs_2_0 VS();
        PixelShader = compile ps_2_0 NoShadowSlantBevelPS();
    }
}

technique InnerShadowSlantBevel
{
    pass ShapePass
    {
        VertexShader = compile vs_2_0 VS();
        PixelShader = compile ps_2_0 NoShadowSlantBevelPS();
    }
    pass ShadowPass
    {
        VertexShader = compile vs_2_0 VS();
        PixelShader = compile ps_2_0 InnerShadowOnlyPS();
    }
}

technique OutlineOuterShadowSlantBevel
{
    pass ShadowPass
    {
        VertexShader = compile vs_2_0 VS();
        PixelShader = compile ps_2_0 OuterShadowOnlyPS();
    }
    pass ShapePass
    {
        VertexShader = compile vs_2_0 VS();
        PixelShader = compile ps_2_0 OutlineNoShadowSlantBevelPS();
    }
}

technique OutlineAltOuterShadowSlantBevel
{
    pass ShadowPass
    {
        VertexShader = compile vs_2_0 VS();
        PixelShader = compile ps_2_0 OuterShadowOnlyPS();
    }
    pass ShapePass
    {
        VertexShader = compile vs_2_0 VS();
        PixelShader = compile ps_2_0 OutlineAltNoShadowSlantBevelPS();
    }
}

technique OutlineInnerShadowSlantBevel
{
    pass ShapePass
    {
        VertexShader = compile vs_2_0 VS();
        PixelShader = compile ps_2_0 OutlineNoShadowSlantBevelPS();
    }
    pass ShadowPass
    {
        VertexShader = compile vs_2_0 VS();
        PixelShader = compile ps_2_0 InnerShadowOnlyPS();
    }
}

technique OutlineAltInnerShadowSlantBevel
{
    pass ShapePass
    {
        VertexShader = compile vs_2_0 VS();
        PixelShader = compile ps_2_0 OutlineAltNoShadowSlantBevelPS();
    }
    pass ShadowPass
    {
        VertexShader = compile vs_2_0 VS();
        PixelShader = compile ps_2_0 InnerShadowOnlyPS();
    }
}


//
// Textured with SlantBevel.
//

technique NoShadowTextureSlantBevel
{
    pass Pass1
    {
        VertexShader = compile vs_2_0 VS();
        PixelShader = compile ps_2_0 NoShadowTextureSlantBevelPS();
    }
}

technique OutlineNoShadowTextureSlantBevel
{
    pass ShapePass
    {
        VertexShader = compile vs_2_0 VS();
        PixelShader = compile ps_2_0 OutlineNoShadowTextureSlantBevelPS();
    }
}

technique OutlineAltNoShadowTextureSlantBevel
{
    pass ShapePass
    {
        VertexShader = compile vs_2_0 VS();
        PixelShader = compile ps_2_0 OutlineAltNoShadowTextureSlantBevelPS();
    }
}

technique OuterShadowTextureSlantBevel
{
    pass ShadowPass
    {
        VertexShader = compile vs_2_0 VS();
        PixelShader = compile ps_2_0 OuterShadowOnlyPS();
    }
    pass ShapePass
    {
        VertexShader = compile vs_2_0 VS();
        PixelShader = compile ps_2_0 NoShadowTextureSlantBevelPS();
    }
}

technique InnerShadowTextureSlantBevel
{
    pass ShapePass
    {
        VertexShader = compile vs_2_0 VS();
        PixelShader = compile ps_2_0 NoShadowTextureSlantBevelPS();
    }
    pass ShadowPass
    {
        VertexShader = compile vs_2_0 VS();
        PixelShader = compile ps_2_0 InnerShadowOnlyPS();
    }
}

technique OutlineOuterShadowTextureSlantBevel
{
    pass ShadowPass
    {
        VertexShader = compile vs_2_0 VS();
        PixelShader = compile ps_2_0 OuterShadowOnlyPS();
    }
    pass ShapePass
    {
        VertexShader = compile vs_2_0 VS();
        PixelShader = compile ps_2_0 OutlineNoShadowTextureSlantBevelPS();
    }
}

technique OutlineAltOuterShadowTextureSlantBevel
{
    pass ShadowPass
    {
        VertexShader = compile vs_2_0 VS();
        PixelShader = compile ps_2_0 OuterShadowOnlyPS();
    }
    pass ShapePass
    {
        VertexShader = compile vs_2_0 VS();
        PixelShader = compile ps_2_0 OutlineAltNoShadowTextureSlantBevelPS();
    }
}

technique OutlineInnerShadowTextureSlantBevel
{
    pass ShapePass
    {
        VertexShader = compile vs_2_0 VS();
        PixelShader = compile ps_2_0 OutlineNoShadowTextureSlantBevelPS();
    }
    pass ShadowPass
    {
        VertexShader = compile vs_2_0 VS();
        PixelShader = compile ps_2_0 InnerShadowOnlyPS();
    }
}

technique OutlineAltInnerShadowTextureSlantBevel
{
    pass ShapePass
    {
        VertexShader = compile vs_2_0 VS();
        PixelShader = compile ps_2_0 OutlineAltNoShadowTextureSlantBevelPS();
    }
    pass ShadowPass
    {
        VertexShader = compile vs_2_0 VS();
        PixelShader = compile ps_2_0 InnerShadowOnlyPS();
    }
}

//
// Non-textured, with RoundedSlantBevel.
//

technique NoShadowRoundedSlantBevel
{
    pass Pass1
    {
        VertexShader = compile vs_2_0 VS();
        PixelShader = compile ps_2_0 NoShadowRoundedSlantBevelPS();
    }
}

technique OutlineNoShadowRoundedSlantBevel
{
    pass Pass1
    {
        VertexShader = compile vs_2_0 VS();
        PixelShader = compile ps_2_0 OutlineNoShadowRoundedSlantBevelPS();
    }
}

technique OutlineAltNoShadowRoundedSlantBevel
{
    pass Pass1
    {
        VertexShader = compile vs_2_0 VS();
        PixelShader = compile ps_2_0 OutlineAltNoShadowRoundedSlantBevelPS();
    }
}

technique OuterShadowRoundedSlantBevel
{
    pass ShadowPass
    {
        VertexShader = compile vs_2_0 VS();
        PixelShader = compile ps_2_0 OuterShadowOnlyPS();
    }
    pass ShapePass
    {
        VertexShader = compile vs_2_0 VS();
        PixelShader = compile ps_2_0 NoShadowRoundedSlantBevelPS();
    }
}

technique InnerShadowRoundedSlantBevel
{
    pass ShapePass
    {
        VertexShader = compile vs_2_0 VS();
        PixelShader = compile ps_2_0 NoShadowRoundedSlantBevelPS();
    }
    pass ShadowPass
    {
        VertexShader = compile vs_2_0 VS();
        PixelShader = compile ps_2_0 InnerShadowOnlyPS();
    }
}

technique OutlineOuterShadowRoundedSlantBevel
{
    pass ShadowPass
    {
        VertexShader = compile vs_2_0 VS();
        PixelShader = compile ps_2_0 OuterShadowOnlyPS();
    }
    pass ShapePass
    {
        VertexShader = compile vs_2_0 VS();
        PixelShader = compile ps_2_0 OutlineNoShadowRoundedSlantBevelPS();
    }
}

technique OutlineAltOuterShadowRoundedSlantBevel
{
    pass ShadowPass
    {
        VertexShader = compile vs_2_0 VS();
        PixelShader = compile ps_2_0 OuterShadowOnlyPS();
    }
    pass ShapePass
    {
        VertexShader = compile vs_2_0 VS();
        PixelShader = compile ps_2_0 OutlineAltNoShadowRoundedSlantBevelPS();
    }
}

technique OutlineInnerShadowRoundedSlantBevel
{
    pass ShapePass
    {
        VertexShader = compile vs_2_0 VS();
        PixelShader = compile ps_2_0 OutlineNoShadowRoundedSlantBevelPS();
    }
    pass ShadowPass
    {
        VertexShader = compile vs_2_0 VS();
        PixelShader = compile ps_2_0 InnerShadowOnlyPS();
    }
}

technique OutlineAltInnerShadowRoundedSlantBevel
{
    pass ShapePass
    {
        VertexShader = compile vs_2_0 VS();
        PixelShader = compile ps_2_0 OutlineAltNoShadowRoundedSlantBevelPS();
    }
    pass ShadowPass
    {
        VertexShader = compile vs_2_0 VS();
        PixelShader = compile ps_2_0 InnerShadowOnlyPS();
    }
}


//
// Textured with RoundedSlantBevel.
//

technique NoShadowTextureRoundedSlantBevel
{
    pass Pass1
    {
        VertexShader = compile vs_2_0 VS();
        PixelShader = compile ps_2_0 NoShadowTextureRoundedSlantBevelPS();
    }
}

technique OutlineNoShadowTextureRoundedSlantBevel
{
    pass ShapePass
    {
        VertexShader = compile vs_2_0 VS();
        PixelShader = compile ps_2_0 OutlineNoShadowTextureRoundedSlantBevelPS();
    }
}

technique OutlineAltNoShadowTextureRoundedSlantBevel
{
    pass ShapePass
    {
        VertexShader = compile vs_2_0 VS();
        PixelShader = compile ps_2_0 OutlineAltNoShadowTextureRoundedSlantBevelPS();
    }
}

technique OuterShadowTextureRoundedSlantBevel
{
    pass ShadowPass
    {
        VertexShader = compile vs_2_0 VS();
        PixelShader = compile ps_2_0 OuterShadowOnlyPS();
    }
    pass ShapePass
    {
        VertexShader = compile vs_2_0 VS();
        PixelShader = compile ps_2_0 NoShadowTextureRoundedSlantBevelPS();
    }
}

technique InnerShadowTextureRoundedSlantBevel
{
    pass ShapePass
    {
        VertexShader = compile vs_2_0 VS();
        PixelShader = compile ps_2_0 NoShadowTextureRoundedSlantBevelPS();
    }
    pass ShadowPass
    {
        VertexShader = compile vs_2_0 VS();
        PixelShader = compile ps_2_0 InnerShadowOnlyPS();
    }
}

technique OutlineOuterShadowTextureRoundedSlantBevel
{
    pass ShadowPass
    {
        VertexShader = compile vs_2_0 VS();
        PixelShader = compile ps_2_0 OuterShadowOnlyPS();
    }
    pass ShapePass
    {
        VertexShader = compile vs_2_0 VS();
        PixelShader = compile ps_2_0 OutlineNoShadowTextureRoundedSlantBevelPS();
    }
}

technique OutlineAltOuterShadowTextureRoundedSlantBevel
{
    pass ShadowPass
    {
        VertexShader = compile vs_2_0 VS();
        PixelShader = compile ps_2_0 OuterShadowOnlyPS();
    }
    pass ShapePass
    {
        VertexShader = compile vs_2_0 VS();
        PixelShader = compile ps_2_0 OutlineAltNoShadowTextureRoundedSlantBevelPS();
    }
}

technique OutlineInnerShadowTextureRoundedSlantBevel
{
    pass ShapePass
    {
        VertexShader = compile vs_2_0 VS();
        PixelShader = compile ps_2_0 OutlineNoShadowTextureRoundedSlantBevelPS();
    }
    pass ShadowPass
    {
        VertexShader = compile vs_2_0 VS();
        PixelShader = compile ps_2_0 InnerShadowOnlyPS();
    }
}

technique OutlineAltInnerShadowTextureRoundedSlantBevel
{
    pass ShapePass
    {
        VertexShader = compile vs_2_0 VS();
        PixelShader = compile ps_2_0 OutlineAltNoShadowTextureRoundedSlantBevelPS();
    }
    pass ShadowPass
    {
        VertexShader = compile vs_2_0 VS();
        PixelShader = compile ps_2_0 InnerShadowOnlyPS();
    }
}

//
// No-textured, with RoundBevel.
//

technique NoShadowRoundBevel
{
    pass Pass1
    {
        VertexShader = compile vs_2_0 VS();
        PixelShader = compile ps_2_0 NoShadowRoundBevelPS();
    }
}

technique OutlineNoShadowRoundBevel
{
    pass Pass1
    {
        VertexShader = compile vs_2_0 VS();
        PixelShader = compile ps_2_0 OutlineNoShadowRoundBevelPS();
    }
}

technique OutlineAltNoShadowRoundBevel
{
    pass Pass1
    {
        VertexShader = compile vs_2_0 VS();
        PixelShader = compile ps_2_0 OutlineAltNoShadowRoundBevelPS();
    }
}

technique OuterShadowRoundBevel
{
    pass ShadowPass
    {
        VertexShader = compile vs_2_0 VS();
        PixelShader = compile ps_2_0 OuterShadowOnlyPS();
    }
    pass ShapePass
    {
        VertexShader = compile vs_2_0 VS();
        PixelShader = compile ps_2_0 NoShadowRoundBevelPS();
    }
}

technique InnerShadowRoundBevel
{
    pass ShapePass
    {
        VertexShader = compile vs_2_0 VS();
        PixelShader = compile ps_2_0 NoShadowRoundBevelPS();
    }
    pass ShadowPass
    {
        VertexShader = compile vs_2_0 VS();
        PixelShader = compile ps_2_0 InnerShadowOnlyPS();
    }
}

technique OutlineOuterShadowRoundBevel
{
    pass ShadowPass
    {
        VertexShader = compile vs_2_0 VS();
        PixelShader = compile ps_2_0 OuterShadowOnlyPS();
    }
    pass ShapePass
    {
        VertexShader = compile vs_2_0 VS();
        PixelShader = compile ps_2_0 OutlineNoShadowRoundBevelPS();
    }
}

technique OutlineAltOuterShadowRoundBevel
{
    pass ShadowPass
    {
        VertexShader = compile vs_2_0 VS();
        PixelShader = compile ps_2_0 OuterShadowOnlyPS();
    }
    pass ShapePass
    {
        VertexShader = compile vs_2_0 VS();
        PixelShader = compile ps_2_0 OutlineAltNoShadowRoundBevelPS();
    }
}

technique OutlineInnerShadowRoundBevel
{
    pass ShapePass
    {
        VertexShader = compile vs_2_0 VS();
        PixelShader = compile ps_2_0 OutlineNoShadowRoundBevelPS();
    }
    pass ShadowPass
    {
        VertexShader = compile vs_2_0 VS();
        PixelShader = compile ps_2_0 InnerShadowOnlyPS();
    }
}

technique OutlineAltInnerShadowRoundBevel
{
    pass ShapePass
    {
        VertexShader = compile vs_2_0 VS();
        PixelShader = compile ps_2_0 OutlineAltNoShadowRoundBevelPS();
    }
    pass ShadowPass
    {
        VertexShader = compile vs_2_0 VS();
        PixelShader = compile ps_2_0 InnerShadowOnlyPS();
    }
}


//
// Textured with RoundBevel.
//

technique NoShadowTextureRoundBevel
{
    pass Pass1
    {
        VertexShader = compile vs_2_0 VS();
        PixelShader = compile ps_2_0 NoShadowTextureRoundBevelPS();
    }
}

technique OutlineNoShadowTextureRoundBevel
{
    pass ShapePass
    {
        VertexShader = compile vs_2_0 VS();
        PixelShader = compile ps_2_0 OutlineNoShadowTextureRoundBevelPS();
    }
}

technique OutlineAltNoShadowTextureRoundBevel
{
    pass ShapePass
    {
        VertexShader = compile vs_2_0 VS();
        PixelShader = compile ps_2_0 OutlineAltNoShadowTextureRoundBevelPS();
    }
}

technique OuterShadowTextureRoundBevel
{
    pass ShadowPass
    {
        VertexShader = compile vs_2_0 VS();
        PixelShader = compile ps_2_0 OuterShadowOnlyPS();
    }
    pass ShapePass
    {
        VertexShader = compile vs_2_0 VS();
        PixelShader = compile ps_2_0 NoShadowTextureRoundBevelPS();
    }
}

technique InnerShadowTextureRoundBevel
{
    pass ShapePass
    {
        VertexShader = compile vs_2_0 VS();
        PixelShader = compile ps_2_0 NoShadowTextureRoundBevelPS();
    }
    pass ShadowPass
    {
        VertexShader = compile vs_2_0 VS();
        PixelShader = compile ps_2_0 InnerShadowOnlyPS();
    }
}

technique OutlineOuterShadowTextureRoundBevel
{
    pass ShadowPass
    {
        VertexShader = compile vs_2_0 VS();
        PixelShader = compile ps_2_0 OuterShadowOnlyPS();
    }
    pass ShapePass
    {
        VertexShader = compile vs_2_0 VS();
        PixelShader = compile ps_2_0 OutlineNoShadowTextureRoundBevelPS();
    }
}

technique OutlineAltOuterShadowTextureRoundBevel
{
    pass ShadowPass
    {
        VertexShader = compile vs_2_0 VS();
        PixelShader = compile ps_2_0 OuterShadowOnlyPS();
    }
    pass ShapePass
    {
        VertexShader = compile vs_2_0 VS();
        PixelShader = compile ps_2_0 OutlineAltNoShadowTextureRoundBevelPS();
    }
}

technique OutlineInnerShadowTextureRoundBevel
{
    pass ShapePass
    {
        VertexShader = compile vs_2_0 VS();
        PixelShader = compile ps_2_0 OutlineNoShadowTextureRoundBevelPS();
    }
    pass ShadowPass
    {
        VertexShader = compile vs_2_0 VS();
        PixelShader = compile ps_2_0 InnerShadowOnlyPS();
    }
}

technique OutlineAltInnerShadowTextureRoundBevel
{
    pass ShapePass
    {
        VertexShader = compile vs_2_0 VS();
        PixelShader = compile ps_2_0 OutlineAltNoShadowTextureRoundBevelPS();
    }
    pass ShadowPass
    {
        VertexShader = compile vs_2_0 VS();
        PixelShader = compile ps_2_0 InnerShadowOnlyPS();
    }
}

//
//
// Same as all of the above but with TwoTone
//
//

//
// No shadow, no Bevel versions.
//

technique TwoToneNoShadow
{
    pass Pass1
    {
        VertexShader = compile vs_2_0 VS();
        PixelShader = compile ps_2_0 TwoToneNoShadowPS();
    }
}

technique OutlineTwoToneNoShadow
{
    pass Pass1
    {
        VertexShader = compile vs_2_0 VS();
        PixelShader = compile ps_2_0 OutlineTwoToneNoShadowPS();
    }
}

technique OutlineTwoToneAltNoShadow
{
    pass Pass1
    {
        VertexShader = compile vs_2_0 VS();
        PixelShader = compile ps_2_0 OutlineTwoToneAltNoShadowPS();
    }
}

technique TwoToneOuterShadow
{
    pass Pass1
    {
        VertexShader = compile vs_2_0 VS();
        PixelShader = compile ps_2_0 TwoToneOuterShadowPS();
    }
}

technique TwoToneInnerShadow
{
    pass Pass1
    {
        VertexShader = compile vs_2_0 VS();
        PixelShader = compile ps_2_0 TwoToneInnerShadowPS();
    }
}

technique OutlineTwoToneOuterShadow
{
    pass Pass1
    {
        VertexShader = compile vs_2_0 VS();
        PixelShader = compile ps_2_0 OutlineTwoToneOuterShadowPS();
    }
}

technique OutlineTwoToneAltOuterShadow
{
    pass Pass1
    {
        VertexShader = compile vs_2_0 VS();
        PixelShader = compile ps_2_0 OutlineTwoToneAltOuterShadowPS();
    }
}

technique OutlineTwoToneInnerShadow
{
    pass Pass1
    {
        VertexShader = compile vs_2_0 VS();
        PixelShader = compile ps_2_0 OutlineTwoToneInnerShadowPS();
    }
}

technique OutlineTwoToneAltInnerShadow
{
    pass Pass1
    {
        VertexShader = compile vs_2_0 VS();
        PixelShader = compile ps_2_0 OutlineTwoToneAltInnerShadowPS();
    }
}

//
// Textured versions.
//

technique TwoToneNoShadowTexture
{
    pass Pass1
    {
        VertexShader = compile vs_2_0 VS();
        PixelShader = compile ps_2_0 TwoToneNoShadowTexturePS();
    }
}

technique OutlineTwoToneNoShadowTexture
{
    pass Pass1
    {
        VertexShader = compile vs_2_0 VS();
        PixelShader = compile ps_2_0 OutlineTwoToneNoShadowTexturePS();
    }
}

technique OutlineTwoToneAltNoShadowTexture
{
    pass Pass1
    {
        VertexShader = compile vs_2_0 VS();
        PixelShader = compile ps_2_0 OutlineTwoToneAltNoShadowTexturePS();
    }
}

technique TwoToneOuterShadowTexture
{
    pass Pass1
    {
        VertexShader = compile vs_2_0 VS();
        PixelShader = compile ps_2_0 TwoToneOuterShadowTexturePS();
    }
}

technique TwoToneInnerShadowTexture
{
    pass Pass1
    {
        VertexShader = compile vs_2_0 VS();
        PixelShader = compile ps_2_0 TwoToneInnerShadowTexturePS();
    }
}

technique OutlineTwoToneOuterShadowTexture
{
    pass Pass1
    {
        VertexShader = compile vs_2_0 VS();
        PixelShader = compile ps_2_0 OutlineTwoToneOuterShadowTexturePS();
    }
}

technique OutlineTwoToneAltOuterShadowTexture
{
    pass Pass1
    {
        VertexShader = compile vs_2_0 VS();
        PixelShader = compile ps_2_0 OutlineTwoToneAltOuterShadowTexturePS();
    }
}

technique OutlineTwoToneInnerShadowTexture
{
    pass Pass1
    {
        VertexShader = compile vs_2_0 VS();
        PixelShader = compile ps_2_0 OutlineTwoToneInnerShadowTexturePS();
    }
}

technique OutlineTwoToneAltInnerShadowTexture
{
    pass Pass1
    {
        VertexShader = compile vs_2_0 VS();
        PixelShader = compile ps_2_0 OutlineTwoToneAltInnerShadowTexturePS();
    }
}


//
// Non-textured, with SlantBevel.
//

technique TwoToneNoShadowSlantBevel
{
    pass Pass1
    {
        VertexShader = compile vs_2_0 VS();
        PixelShader = compile ps_2_0 TwoToneNoShadowSlantBevelPS();
    }
}

technique OutlineTwoToneNoShadowSlantBevel
{
    pass Pass1
    {
        VertexShader = compile vs_2_0 VS();
        PixelShader = compile ps_2_0 OutlineTwoToneNoShadowSlantBevelPS();
    }
}

technique OutlineTwoToneAltNoShadowSlantBevel
{
    pass Pass1
    {
        VertexShader = compile vs_2_0 VS();
        PixelShader = compile ps_2_0 OutlineTwoToneAltNoShadowSlantBevelPS();
    }
}

technique TwoToneOuterShadowSlantBevel
{
    pass ShadowPass
    {
        VertexShader = compile vs_2_0 VS();
        PixelShader = compile ps_2_0 OuterShadowOnlyPS();
    }
    pass ShapePass
    {
        VertexShader = compile vs_2_0 VS();
        PixelShader = compile ps_2_0 TwoToneNoShadowSlantBevelPS();
    }
}

technique TwoToneInnerShadowSlantBevel
{
    pass ShapePass
    {
        VertexShader = compile vs_2_0 VS();
        PixelShader = compile ps_2_0 TwoToneNoShadowSlantBevelPS();
    }
    pass ShadowPass
    {
        VertexShader = compile vs_2_0 VS();
        PixelShader = compile ps_2_0 InnerShadowOnlyPS();
    }
}

technique OutlineTwoToneOuterShadowSlantBevel
{
    pass ShadowPass
    {
        VertexShader = compile vs_2_0 VS();
        PixelShader = compile ps_2_0 OuterShadowOnlyPS();
    }
    pass ShapePass
    {
        VertexShader = compile vs_2_0 VS();
        PixelShader = compile ps_2_0 OutlineTwoToneNoShadowSlantBevelPS();
    }
}

technique OutlineTwoToneAltOuterShadowSlantBevel
{
    pass ShadowPass
    {
        VertexShader = compile vs_2_0 VS();
        PixelShader = compile ps_2_0 OuterShadowOnlyPS();
    }
    pass ShapePass
    {
        VertexShader = compile vs_2_0 VS();
        PixelShader = compile ps_2_0 OutlineTwoToneAltNoShadowSlantBevelPS();
    }
}

technique OutlineTwoToneInnerShadowSlantBevel
{
    pass ShapePass
    {
        VertexShader = compile vs_2_0 VS();
        PixelShader = compile ps_2_0 OutlineTwoToneNoShadowSlantBevelPS();
    }
    pass ShadowPass
    {
        VertexShader = compile vs_2_0 VS();
        PixelShader = compile ps_2_0 InnerShadowOnlyPS();
    }
}

technique OutlineTwoToneAltInnerShadowSlantBevel
{
    pass ShapePass
    {
        VertexShader = compile vs_2_0 VS();
        PixelShader = compile ps_2_0 OutlineTwoToneAltNoShadowSlantBevelPS();
    }
    pass ShadowPass
    {
        VertexShader = compile vs_2_0 VS();
        PixelShader = compile ps_2_0 InnerShadowOnlyPS();
    }
}


//
// Textured with SlantBevel.
//

technique TwoToneNoShadowTextureSlantBevel
{
    pass Pass1
    {
        VertexShader = compile vs_2_0 VS();
        PixelShader = compile ps_2_0 TwoToneNoShadowTextureSlantBevelPS();
    }
}

technique OutlineTwoToneNoShadowTextureSlantBevel
{
    pass ShapePass
    {
        VertexShader = compile vs_2_0 VS();
        PixelShader = compile ps_2_0 OutlineTwoToneNoShadowTextureSlantBevelPS();
    }
}

technique OutlineTwoToneAltNoShadowTextureSlantBevel
{
    pass ShapePass
    {
        VertexShader = compile vs_2_0 VS();
        PixelShader = compile ps_2_0 OutlineTwoToneAltNoShadowTextureSlantBevelPS();
    }
}

technique TwoToneOuterShadowTextureSlantBevel
{
    pass ShadowPass
    {
        VertexShader = compile vs_2_0 VS();
        PixelShader = compile ps_2_0 OuterShadowOnlyPS();
    }
    pass ShapePass
    {
        VertexShader = compile vs_2_0 VS();
        PixelShader = compile ps_2_0 TwoToneNoShadowTextureSlantBevelPS();
    }
}

technique TwoToneInnerShadowTextureSlantBevel
{
    pass ShapePass
    {
        VertexShader = compile vs_2_0 VS();
        PixelShader = compile ps_2_0 TwoToneNoShadowTextureSlantBevelPS();
    }
    pass ShadowPass
    {
        VertexShader = compile vs_2_0 VS();
        PixelShader = compile ps_2_0 InnerShadowOnlyPS();
    }
}

technique OutlineTwoToneOuterShadowTextureSlantBevel
{
    pass ShadowPass
    {
        VertexShader = compile vs_2_0 VS();
        PixelShader = compile ps_2_0 OuterShadowOnlyPS();
    }
    pass ShapePass
    {
        VertexShader = compile vs_2_0 VS();
        PixelShader = compile ps_2_0 OutlineTwoToneNoShadowTextureSlantBevelPS();
    }
}

technique OutlineTwoToneAltOuterShadowTextureSlantBevel
{
    pass ShadowPass
    {
        VertexShader = compile vs_2_0 VS();
        PixelShader = compile ps_2_0 OuterShadowOnlyPS();
    }
    pass ShapePass
    {
        VertexShader = compile vs_2_0 VS();
        PixelShader = compile ps_2_0 OutlineTwoToneAltNoShadowTextureSlantBevelPS();
    }
}

technique OutlineTwoToneInnerShadowTextureSlantBevel
{
    pass ShapePass
    {
        VertexShader = compile vs_2_0 VS();
        PixelShader = compile ps_2_0 OutlineTwoToneNoShadowTextureSlantBevelPS();
    }
    pass ShadowPass
    {
        VertexShader = compile vs_2_0 VS();
        PixelShader = compile ps_2_0 InnerShadowOnlyPS();
    }
}

technique OutlineTwoToneAltInnerShadowTextureSlantBevel
{
    pass ShapePass
    {
        VertexShader = compile vs_2_0 VS();
        PixelShader = compile ps_2_0 OutlineTwoToneAltNoShadowTextureSlantBevelPS();
    }
    pass ShadowPass
    {
        VertexShader = compile vs_2_0 VS();
        PixelShader = compile ps_2_0 InnerShadowOnlyPS();
    }
}

//
// Non-textured, with RoundedSlantBevel.
//

technique TwoToneNoShadowRoundedSlantBevel
{
    pass Pass1
    {
        VertexShader = compile vs_2_0 VS();
        PixelShader = compile ps_2_0 TwoToneNoShadowRoundedSlantBevelPS();
    }
}

technique OutlineTwoToneNoShadowRoundedSlantBevel
{
    pass Pass1
    {
        VertexShader = compile vs_2_0 VS();
        PixelShader = compile ps_2_0 OutlineTwoToneNoShadowRoundedSlantBevelPS();
    }
}

technique OutlineTwoToneAltNoShadowRoundedSlantBevel
{
    pass Pass1
    {
        VertexShader = compile vs_2_0 VS();
        PixelShader = compile ps_2_0 OutlineTwoToneAltNoShadowRoundedSlantBevelPS();
    }
}

technique TwoToneOuterShadowRoundedSlantBevel
{
    pass ShadowPass
    {
        VertexShader = compile vs_2_0 VS();
        PixelShader = compile ps_2_0 OuterShadowOnlyPS();
    }
    pass ShapePass
    {
        VertexShader = compile vs_2_0 VS();
        PixelShader = compile ps_2_0 TwoToneNoShadowRoundedSlantBevelPS();
    }
}

technique TwoToneInnerShadowRoundedSlantBevel
{
    pass ShapePass
    {
        VertexShader = compile vs_2_0 VS();
        PixelShader = compile ps_2_0 TwoToneNoShadowRoundedSlantBevelPS();
    }
    pass ShadowPass
    {
        VertexShader = compile vs_2_0 VS();
        PixelShader = compile ps_2_0 InnerShadowOnlyPS();
    }
}

technique OutlineTwoToneOuterShadowRoundedSlantBevel
{
    pass ShadowPass
    {
        VertexShader = compile vs_2_0 VS();
        PixelShader = compile ps_2_0 OuterShadowOnlyPS();
    }
    pass ShapePass
    {
        VertexShader = compile vs_2_0 VS();
        PixelShader = compile ps_2_0 OutlineTwoToneNoShadowRoundedSlantBevelPS();
    }
}

technique OutlineTwoToneAltOuterShadowRoundedSlantBevel
{
    pass ShadowPass
    {
        VertexShader = compile vs_2_0 VS();
        PixelShader = compile ps_2_0 OuterShadowOnlyPS();
    }
    pass ShapePass
    {
        VertexShader = compile vs_2_0 VS();
        PixelShader = compile ps_2_0 OutlineTwoToneAltNoShadowRoundedSlantBevelPS();
    }
}

technique OutlineTwoToneInnerShadowRoundedSlantBevel
{
    pass ShapePass
    {
        VertexShader = compile vs_2_0 VS();
        PixelShader = compile ps_2_0 OutlineTwoToneNoShadowRoundedSlantBevelPS();
    }
    pass ShadowPass
    {
        VertexShader = compile vs_2_0 VS();
        PixelShader = compile ps_2_0 InnerShadowOnlyPS();
    }
}

technique OutlineTwoToneAltInnerShadowRoundedSlantBevel
{
    pass ShapePass
    {
        VertexShader = compile vs_2_0 VS();
        PixelShader = compile ps_2_0 OutlineTwoToneAltNoShadowRoundedSlantBevelPS();
    }
    pass ShadowPass
    {
        VertexShader = compile vs_2_0 VS();
        PixelShader = compile ps_2_0 InnerShadowOnlyPS();
    }
}


//
// Textured with RoundedSlantBevel.
//

technique TwoToneNoShadowTextureRoundedSlantBevel
{
    pass Pass1
    {
        VertexShader = compile vs_2_0 VS();
        PixelShader = compile ps_2_0 TwoToneNoShadowTextureRoundedSlantBevelPS();
    }
}

technique OutlineTwoToneNoShadowTextureRoundedSlantBevel
{
    pass ShapePass
    {
        VertexShader = compile vs_2_0 VS();
        PixelShader = compile ps_2_0 OutlineTwoToneNoShadowTextureRoundedSlantBevelPS();
    }
}

technique OutlineTwoToneAltNoShadowTextureRoundedSlantBevel
{
    pass ShapePass
    {
        VertexShader = compile vs_2_0 VS();
        PixelShader = compile ps_2_0 OutlineTwoToneAltNoShadowTextureRoundedSlantBevelPS();
    }
}

technique TwoToneOuterShadowTextureRoundedSlantBevel
{
    pass ShadowPass
    {
        VertexShader = compile vs_2_0 VS();
        PixelShader = compile ps_2_0 OuterShadowOnlyPS();
    }
    pass ShapePass
    {
        VertexShader = compile vs_2_0 VS();
        PixelShader = compile ps_2_0 TwoToneNoShadowTextureRoundedSlantBevelPS();
    }
}

technique TwoToneInnerShadowTextureRoundedSlantBevel
{
    pass ShapePass
    {
        VertexShader = compile vs_2_0 VS();
        PixelShader = compile ps_2_0 TwoToneNoShadowTextureRoundedSlantBevelPS();
    }
    pass ShadowPass
    {
        VertexShader = compile vs_2_0 VS();
        PixelShader = compile ps_2_0 InnerShadowOnlyPS();
    }
}

technique OutlineTwoToneOuterShadowTextureRoundedSlantBevel
{
    pass ShadowPass
    {
        VertexShader = compile vs_2_0 VS();
        PixelShader = compile ps_2_0 OuterShadowOnlyPS();
    }
    pass ShapePass
    {
        VertexShader = compile vs_2_0 VS();
        PixelShader = compile ps_2_0 OutlineTwoToneNoShadowTextureRoundedSlantBevelPS();
    }
}

technique OutlineTwoToneAltOuterShadowTextureRoundedSlantBevel
{
    pass ShadowPass
    {
        VertexShader = compile vs_2_0 VS();
        PixelShader = compile ps_2_0 OuterShadowOnlyPS();
    }
    pass ShapePass
    {
        VertexShader = compile vs_2_0 VS();
        PixelShader = compile ps_2_0 OutlineTwoToneAltNoShadowTextureRoundedSlantBevelPS();
    }
}

technique OutlineTwoToneInnerShadowTextureRoundedSlantBevel
{
    pass ShapePass
    {
        VertexShader = compile vs_2_0 VS();
        PixelShader = compile ps_2_0 OutlineTwoToneNoShadowTextureRoundedSlantBevelPS();
    }
    pass ShadowPass
    {
        VertexShader = compile vs_2_0 VS();
        PixelShader = compile ps_2_0 InnerShadowOnlyPS();
    }
}

technique OutlineTwoToneAltInnerShadowTextureRoundedSlantBevel
{
    pass ShapePass
    {
        VertexShader = compile vs_2_0 VS();
        PixelShader = compile ps_2_0 OutlineTwoToneAltNoShadowTextureRoundedSlantBevelPS();
    }
    pass ShadowPass
    {
        VertexShader = compile vs_2_0 VS();
        PixelShader = compile ps_2_0 InnerShadowOnlyPS();
    }
}

//
// No-textured, with RoundBevel.
//

technique TwoToneNoShadowRoundBevel
{
    pass Pass1
    {
        VertexShader = compile vs_2_0 VS();
        PixelShader = compile ps_2_0 TwoToneNoShadowRoundBevelPS();
    }
}

technique OutlineTwoToneNoShadowRoundBevel
{
    pass Pass1
    {
        VertexShader = compile vs_2_0 VS();
        PixelShader = compile ps_2_0 OutlineTwoToneNoShadowRoundBevelPS();
    }
}

technique OutlineTwoToneAltNoShadowRoundBevel
{
    pass Pass1
    {
        VertexShader = compile vs_2_0 VS();
        PixelShader = compile ps_2_0 OutlineTwoToneAltNoShadowRoundBevelPS();
    }
}

technique TwoToneOuterShadowRoundBevel
{
    pass ShadowPass
    {
        VertexShader = compile vs_2_0 VS();
        PixelShader = compile ps_2_0 OuterShadowOnlyPS();
    }
    pass ShapePass
    {
        VertexShader = compile vs_2_0 VS();
        PixelShader = compile ps_2_0 TwoToneNoShadowRoundBevelPS();
    }
}

technique TwoToneInnerShadowRoundBevel
{
    pass ShapePass
    {
        VertexShader = compile vs_2_0 VS();
        PixelShader = compile ps_2_0 TwoToneNoShadowRoundBevelPS();
    }
    pass ShadowPass
    {
        VertexShader = compile vs_2_0 VS();
        PixelShader = compile ps_2_0 InnerShadowOnlyPS();
    }
}

technique OutlineTwoToneOuterShadowRoundBevel
{
    pass ShadowPass
    {
        VertexShader = compile vs_2_0 VS();
        PixelShader = compile ps_2_0 OuterShadowOnlyPS();
    }
    pass ShapePass
    {
        VertexShader = compile vs_2_0 VS();
        PixelShader = compile ps_2_0 OutlineTwoToneNoShadowRoundBevelPS();
    }
}

technique OutlineTwoToneAltOuterShadowRoundBevel
{
    pass ShadowPass
    {
        VertexShader = compile vs_2_0 VS();
        PixelShader = compile ps_2_0 OuterShadowOnlyPS();
    }
    pass ShapePass
    {
        VertexShader = compile vs_2_0 VS();
        PixelShader = compile ps_2_0 OutlineTwoToneAltNoShadowRoundBevelPS();
    }
}

technique OutlineTwoToneInnerShadowRoundBevel
{
    pass ShapePass
    {
        VertexShader = compile vs_2_0 VS();
        PixelShader = compile ps_2_0 OutlineTwoToneNoShadowRoundBevelPS();
    }
    pass ShadowPass
    {
        VertexShader = compile vs_2_0 VS();
        PixelShader = compile ps_2_0 InnerShadowOnlyPS();
    }
}

technique OutlineTwoToneAltInnerShadowRoundBevel
{
    pass ShapePass
    {
        VertexShader = compile vs_2_0 VS();
        PixelShader = compile ps_2_0 OutlineTwoToneAltNoShadowRoundBevelPS();
    }
    pass ShadowPass
    {
        VertexShader = compile vs_2_0 VS();
        PixelShader = compile ps_2_0 InnerShadowOnlyPS();
    }
}


//
// Textured with RoundBevel.
//

technique TwoToneNoShadowTextureRoundBevel
{
    pass Pass1
    {
        VertexShader = compile vs_2_0 VS();
        PixelShader = compile ps_2_0 TwoToneNoShadowTextureRoundBevelPS();
    }
}

technique OutlineTwoToneNoShadowTextureRoundBevel
{
    pass ShapePass
    {
        VertexShader = compile vs_2_0 VS();
        PixelShader = compile ps_2_0 OutlineTwoToneNoShadowTextureRoundBevelPS();
    }
}

technique OutlineTwoToneAltNoShadowTextureRoundBevel
{
    pass ShapePass
    {
        VertexShader = compile vs_2_0 VS();
        PixelShader = compile ps_2_0 OutlineTwoToneAltNoShadowTextureRoundBevelPS();
    }
}

technique TwoToneOuterShadowTextureRoundBevel
{
    pass ShadowPass
    {
        VertexShader = compile vs_2_0 VS();
        PixelShader = compile ps_2_0 OuterShadowOnlyPS();
    }
    pass ShapePass
    {
        VertexShader = compile vs_2_0 VS();
        PixelShader = compile ps_2_0 TwoToneNoShadowTextureRoundBevelPS();
    }
}

technique TwoToneInnerShadowTextureRoundBevel
{
    pass ShapePass
    {
        VertexShader = compile vs_2_0 VS();
        PixelShader = compile ps_2_0 TwoToneNoShadowTextureRoundBevelPS();
    }
    pass ShadowPass
    {
        VertexShader = compile vs_2_0 VS();
        PixelShader = compile ps_2_0 InnerShadowOnlyPS();
    }
}

technique OutlineTwoToneOuterShadowTextureRoundBevel
{
    pass ShadowPass
    {
        VertexShader = compile vs_2_0 VS();
        PixelShader = compile ps_2_0 OuterShadowOnlyPS();
    }
    pass ShapePass
    {
        VertexShader = compile vs_2_0 VS();
        PixelShader = compile ps_2_0 OutlineTwoToneNoShadowTextureRoundBevelPS();
    }
}

technique OutlineTwoToneAltOuterShadowTextureRoundBevel
{
    pass ShadowPass
    {
        VertexShader = compile vs_2_0 VS();
        PixelShader = compile ps_2_0 OuterShadowOnlyPS();
    }
    pass ShapePass
    {
        VertexShader = compile vs_2_0 VS();
        PixelShader = compile ps_2_0 OutlineTwoToneAltNoShadowTextureRoundBevelPS();
    }
}

technique OutlineTwoToneInnerShadowTextureRoundBevel
{
    pass ShapePass
    {
        VertexShader = compile vs_2_0 VS();
        PixelShader = compile ps_2_0 OutlineTwoToneNoShadowTextureRoundBevelPS();
    }
    pass ShadowPass
    {
        VertexShader = compile vs_2_0 VS();
        PixelShader = compile ps_2_0 InnerShadowOnlyPS();
    }
}

technique OutlineTwoToneAltInnerShadowTextureRoundBevel
{
    pass ShapePass
    {
        VertexShader = compile vs_2_0 VS();
        PixelShader = compile ps_2_0 OutlineTwoToneAltNoShadowTextureRoundBevelPS();
    }
    pass ShadowPass
    {
        VertexShader = compile vs_2_0 VS();
        PixelShader = compile ps_2_0 InnerShadowOnlyPS();
    }
}


