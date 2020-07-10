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

namespace Boku.Common
{
    /// <summary>
    /// Float version of Rectangle struct.
    /// </summary>
    public struct RectangleF
    {
        #region Members

        Vector2 position;
        Vector2 size;

        #endregion

        #region Accesssors

        public Vector2 Position
        {
            get { return position; }
            set { position = value; }
        }

        public Vector2 Size
        {
            get { return size; }
            set { size = value; }
        }

        /// <summary>
        /// X position of rectangle.
        /// </summary>
        public float X
        {
            get { return position.X; }
            set { position.X = value; }
        }

        /// <summary>
        /// Y position of rectangle.
        /// </summary>
        public float Y
        {
            get { return position.Y; }
            set { position.Y = value; }
        }

        /// <summary>
        /// Width of rectangle.
        /// </summary>
        public float Width
        {
            get { return size.X; }
            set { size.X = value; }
        }

        /// <summary>
        /// Height of rectangle.
        /// </summary>
        public float Height
        {
            get { return size.Y; }
            set { size.Y = value; }
        }

        /// <summary>
        /// Gets left edge of rectangle.
        /// </summary>
        public float Left
        {
            get { return position.X; }
        }

        /// <summary>
        /// Gets right edge of rectangle.
        /// </summary>
        public float Right
        {
            get { return position.X + size.X; }
        }

        /// <summary>
        /// Gets top edge of rectangle.
        /// </summary>
        public float Top
        {
            get { return position.Y; }
        }

        /// <summary>
        /// Gets bottom edge of rectangle.
        /// </summary>
        public float Bottom
        {
            get { return position.Y + size.Y; }
        }

        /// <summary>
        /// Returns the center of the rectangle.
        /// </summary>
        public Vector2 Center
        {
            get { return position + size / 2.0f; }
            set { position = value - size / 2.0f; }
        }

        /// <summary>
        /// Returns true if the given rectangle is empty.
        /// "empty" defined as having a 0 size.
        /// </summary>
        public bool IsEmpty
        {
            get { return size == Vector2.Zero; }
        }

        #endregion

        #region Public

        public RectangleF(RectangleF rect)
        {
            this.position = rect.position;
            this.size = rect.size;
        }

        public RectangleF(Vector2 position, Vector2 size)
        {
            this.position = position;
            this.size = size;
        }

        public RectangleF(float x, float y, float width, float height)
        {
            position = new Vector2(x, y);
            size = new Vector2(width, height);
        }

        public static bool operator ==(RectangleF a, RectangleF b)
        {
            bool result = (a.position == b.position) && (a.size == b.size);

            return result;
        }

        public static bool operator !=(RectangleF a, RectangleF b)
        {
            return !(a == b);
        }

        public override bool Equals(Object obj)
        {
            // If parameter is null or wrong type return false.
            if (obj == null || !(obj is RectangleF))
            {
                return false;
            }

            // Return true if the values match.
            return this == (RectangleF)obj;
        }

        public override int GetHashCode()
        {
            return base.GetHashCode();
        }

        /// <summary>
        /// Determines whether this RectangleF contains the specified point.
        /// </summary>
        /// <param name="point"></param>
        /// <returns></returns>
        public bool Contains(Vector2 point)
        {
            bool result;
            result = point.X >= Left && point.Y >= Top && point.X <= Right && point.Y <= Bottom;
            return result;
        }

        /// <summary>
        /// Determines whether this RectangleF contains the specified point.
        /// </summary>
        /// <param name="point"></param>
        /// <returns></returns>
        public bool Contains(Point point)
        {
            bool result;
            result = point.X >= Left && point.Y >= Top && point.X <= Right && point.Y <= Bottom;
            return result;
        }

        /// <summary>
        /// Determines whether this RectangleF contains the specified RectangleF.
        /// </summary>
        /// <param name="point"></param>
        /// <returns></returns>
        public bool Contains(RectangleF rect)
        {
            bool result;
            result = rect.Right >= Right && rect.Top >= Top && rect.Right <= Right && rect.Bottom <= Bottom;
            return result;
        }

        /// <summary>
        /// Is x in the X axis extent of the bounds?
        /// </summary>
        /// <param name="x"></param>
        /// <returns></returns>
        public bool ContainsX(float x)
        {
            bool result;
            result = x >= Left && x <= Right;
            return result;
        }

        /// <summary>
        /// Is y in the Y axis extent of the bounds?
        /// </summary>
        /// <param name="x"></param>
        /// <returns></returns>
        public bool ContainsY(float y)
        {
            bool result;
            result = y >= Top && y <= Bottom;
            return result;
        }

        /// <summary>
        /// Does this rect intersect with the given rect.
        /// </summary>
        /// <param name="rect"></param>
        /// <returns></returns>
        public bool Intersects(RectangleF rect)
        {
            bool result = false;

            if (this.Right >= rect.Left
                && this.Left <= rect.Right
                && this.Bottom >= rect.Top
                && this.Top <= rect.Bottom)
            {
                result = true;
            }

            return result;
        }   // end of Intersects

        /// <summary>
        /// Returns a rectangle where the two given rectangles overlap.
        /// If there's no overlap, returns an empty rectangle.
        /// </summary>
        /// <param name="rect0"></param>
        /// <param name="rect1"></param>
        /// <returns></returns>
        public RectangleF Intersect(RectangleF rect0, RectangleF rect1)
        {
            RectangleF result = new RectangleF();

            result.X = MathHelper.Max(rect0.Left, rect1.Left);
            result.Y = MathHelper.Max(rect0.Top, rect1.Top);

            Vector2 point;
            point.X = MathHelper.Min(rect0.Right, rect1.Right);
            point.Y = MathHelper.Min(rect0.Bottom, rect1.Bottom);

            result.Size = point - result.Position;

            if (result.Size.X < 0 || result.Size.Y < 0)
            {
                result.Size = Vector2.Zero;
            }

            return result;
        }

        /// <summary>
        /// Returns a rectangle that exactly contains the union of the two input rectangles.
        /// </summary>
        /// <param name="rect0"></param>
        /// <param name="rect1"></param>
        /// <returns></returns>
        public RectangleF Union(RectangleF rect0, RectangleF rect1)
        {
            RectangleF result = new RectangleF();

            result.X = MathHelper.Min(rect0.Left, rect1.Left);
            result.Y = MathHelper.Min(rect0.Top, rect1.Top);

            Vector2 point;
            point.X = MathHelper.Max(rect0.Right, rect1.Right);
            point.Y = MathHelper.Max(rect0.Bottom, rect1.Bottom);

            result.Size = point - result.Position;

            return result;
        }

        /// <summary>
        /// Expands rectangle in each direction byu given amount.
        /// </summary>
        /// <param name="amount"></param>
        public void Inflate(float amount)
        {
            position.X -= amount;
            position.Y -= amount;
            size.X += 2 * amount;
            size.Y += 2 * amount;
        }   // end of Inflate()

        /// <summary>
        /// Inflates the current rectangle to include input point.
        /// </summary>
        /// <param name="point"></param>
        public void ExpandToInclude(Vector2 point)
        {
            if (point.X < Left)
            {
                size.X += Left - point.X;
                position.X = point.X;
            }
            else if (point.X > Right)
            {
                size.X += point.X - Right;
            }

            if (point.Y < Top)
            {
                size.Y += Top - point.Y;
                position.Y = point.Y;
            }
            else if (point.Y > Bottom)
            {
                size.Y += point.Y - Bottom;
            }

        }   // end of ExpandToInclude()

        /// <summary>
        /// Truncates position and size values to int values.
        /// </summary>
        public void Truncate()
        {
            position.X = (int)position.X;
            position.Y = (int)position.Y;
            size.X = (int)size.X;
            size.Y = (int)size.Y;
        }

        /// <summary>
        /// Rounds position and size values to int values.
        /// </summary>
        public void Round()
        {
            position.X = (float)Math.Round(position.X);
            position.Y = (float)Math.Round(position.Y);
            size.X = (float)Math.Round(size.X);
            size.Y = (float)Math.Round(size.Y);
        }

        /// <summary>
        /// Clamps the input vector to a position inside the RectangleF
        /// </summary>
        /// <param name="v"></param>
        /// <returns></returns>
        public Vector2 Clamp(Vector2 v)
        {
            v.X = MathHelper.Clamp(v.X, Left, Right);
            v.Y = MathHelper.Clamp(v.Y, Top, Bottom);

            return v;
        }

        public Rectangle ToRectangle()
        {
            Rectangle rect = new Rectangle((int)position.X, (int)position.Y, (int)Width, (int)Height);
            return rect;
        }

        #endregion

        #region Internal
        #endregion

    }   // end of class RectangleF

}   // end of namespace Boku.Common
