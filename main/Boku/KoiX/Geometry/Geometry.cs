
using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Windows.Forms;
using System.IO;

using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

using KoiX;

namespace KoiX.Geometry
{
    public enum ShadowStyle
    {
        None,
        Inner,
        Outer,
    }

    public enum BevelStyle
    {
        None,
        Slant,
        RoundedSlant,   // Half way betwee round and slant.
        Round,
    }

    /// <summary>
    /// A place to hang enums, values, and other stuff shared among the Geometry objects.
    /// Used to have normal map textures here for UI before they all went into the shaders.
    /// </summary>
    public class Geometry
    {
        public const float DefaultEdgeBlend = 0.5f;

        public static void LoadContent()
        {
        }
    }

}   // end of namespace KoiX.Geometry
