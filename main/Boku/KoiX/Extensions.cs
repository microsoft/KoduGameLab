// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.


using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace KoiX
{
    /// <summary>
    /// A collection of extensions that make live a bit easier.
    /// Add on as needed.
    /// </summary>
    public static class Extensions
    {
        /// <summary>
        /// Convert Point to Vector2
        /// </summary>
        /// <param name="p"></param>
        /// <returns></returns>
        public static Vector2 ToVector2(this Point p)
        {
            return new Vector2(p.X, p.Y);
        }

        /// <summary>
        /// Converts a Vector2 to a Point via truncation.
        /// </summary>
        /// <param name="v"></param>
        /// <returns></returns>
        public static Point TruncateToPoint(this Vector2 v)
        {
            return new Point((int)v.X, (int)v.Y);
        }

        /// <summary>
        /// Converts a Vector2 to a Point via rounding.
        /// </summary>
        /// <param name="v"></param>
        /// <returns></returns>
        public static Point RoundToPoint(this Vector2 v)
        {
            return new Point((int)Math.Round(v.X), (int)Math.Round(v.Y));
        }



        /// <summary>
        /// Returns a trucated version of the input vector.
        /// </summary>
        /// <param name="v"></param>
        /// <returns></returns>
        public static Vector2 Truncate(this Vector2 v)
        {
            return new Vector2((int)v.X, (int)v.Y);
        }

        /// <summary>
        /// Returns a rounded version of the input vector.
        /// </summary>
        /// <param name="v"></param>
        /// <returns></returns>
        public static Vector2 Round(this Vector2 v)
        {
            return new Vector2((float)Math.Round(v.X), (float)Math.Round(v.Y));
        }

        /// <summary>
        /// Rounds each element up.
        /// </summary>
        /// <param name="v"></param>
        /// <returns></returns>
        public static Vector2 Ceiling(this Vector2 v)
        {
            return new Vector2((float)Math.Ceiling(v.X), (float)Math.Ceiling(v.Y));
        }

        /// <summary>
        /// Returns a trucated version of the input vector.
        /// </summary>
        /// <param name="v"></param>
        /// <returns></returns>
        public static Vector3 Truncate(this Vector3 v)
        {
            return new Vector3((int)v.X, (int)v.Y, (int)v.Z);
        }

        /// <summary>
        /// Returns a rounded version of the input vector.
        /// </summary>
        /// <param name="v"></param>
        /// <returns></returns>
        public static Vector3 Round(this Vector3 v)
        {
            return new Vector3((float)Math.Round(v.X), (float)Math.Round(v.Y), (float)Math.Round(v.Z));
        }

        //
        // Swizzle style operators on Vector3.  Wish these could be accessors.
        // TODO (****) Flesh these out fully?
        //
        public static Vector2 XY(this Vector3 v)
        {
            return new Vector2(v.X, v.Y);
        }
        public static Vector2 XZ(this Vector3 v)
        {
            return new Vector2(v.X, v.Z);
        }
        public static Vector2 YZ(this Vector3 v)
        {
            return new Vector2(v.Y, v.Z);
        }


        /// <summary>
        /// Does this Rectangle contain the given Vector2.
        /// </summary>
        /// <param name="r"></param>
        /// <param name="v"></param>
        /// <returns></returns>
        public static bool Contains(this Rectangle r, Vector2 v)
        {
            bool inside = v.X >= r.Left && v.X <= r.Right && v.Y >= r.Top && v.Y <= r.Bottom;

            return inside;
        }

        /// <summary>
        /// Get the Size of a Rectangle.
        /// </summary>
        /// <param name="r"></param>
        /// <returns></returns>
        public static Point GetSize(this Rectangle r)
        {
            return new Point(r.Width, r.Height);
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

        /// <summary>
        /// Extension to GraphicsDevice which forces all the device
        /// texture references to null.  This prevents the errors where
        /// the system still thinks that a Vector2 texture is still bound
        /// to the device while non-Point sampling is being set up.
        /// 
        /// This should be called after at the end of any render call
        /// which uses Vector* based textures.
        /// </summary>
        /// <param name="device"></param>
        public static void ClearTextures(this GraphicsDevice device)
        {
            for (int i = 0; i < 15; i++)
            {
                device.Textures[i] = null;
            }
            for (int i = 0; i < 4; i++)
            {
                device.VertexTextures[i] = null;
            }
        }

    }   // end of class Extensions
}   // end of namespace KoiX
