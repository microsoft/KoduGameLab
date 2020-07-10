// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.


using System;
using System.Collections;
using System.Diagnostics;

using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Audio;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using Microsoft.Xna.Framework.Storage;

namespace ExtensionMethods
{
    /// <summary>
    /// Random collection of class extension methods.
    /// </summary>
    public static class Extensions
    {
        /// <summary>
        /// Rounds the elements of a Vector
        /// </summary>
        /// <param name="v"></param>
        /// <returns></returns>
        public static Vector2 Round(this Vector2 v)
        {
            return new Vector2((float)Math.Round(v.X), (float)Math.Round(v.Y));
        }

        /// <summary>
        /// Truncates the elements of a Vector
        /// </summary>
        /// <param name="v"></param>
        /// <returns></returns>
        public static Vector2 Truncate(this Vector2 v)
        {
            return new Vector2((int)v.X, (int)v.Y);
        }

        /// <summary>
        /// Returns a Rectangle numPixels smaller all around.
        /// </summary>
        /// <param name="r"></param>
        /// <returns></returns>
        public static Rectangle Shrink(this Rectangle r, int numPixels)
        {
            return new Rectangle(r.X + numPixels, r.Y + numPixels, r.Width - 2 * numPixels, r.Height - 2 * numPixels);
        }

    }   // end of class Extensions

}   // end of namespace Boku.Common
