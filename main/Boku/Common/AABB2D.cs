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

using Boku.Common.Xml;

namespace Boku.Common
{
    /// <summary>
    /// Axis Aligned Bounding Box, 2D version.
    /// </summary>
    public class AABB2D
    {
        private Vector2 min;
        private Vector2 max;

        #region Accessors

        public Vector2 Min
        {
            get { return min; }
        }
        public Vector2 Max
        {
            get { return max; }
        }
        public Vector2 Size
        {
            get { return max - min; }
        }
        public float Width
        {
            get { return max.X - min.X; }
        }
        public float Height
        {
            get { return max.Y - min.Y; }
        }

        public Rectangle Rectangle
        {
            get { return new Rectangle((int)min.X, (int)min.Y, (int)(max.X - min.X), (int)(max.Y - min.Y)); }
        }

        #endregion

        // c'tor
        public AABB2D(Vector2 min, Vector2 max)
        {
            this.min = Vector2.Min(min, max);
            this.max = Vector2.Max(min, max);
        }   // end of AABB2D c'tor

        public AABB2D(AABB2D src)
        {
            this.min = src.min;
            this.max = src.max;
        }

        public AABB2D()
        {
            this.min = Vector2.Zero;
            this.max = Vector2.Zero;
        }

        /// <summary>
        /// Create an empty box
        /// </summary>
        /// <returns></returns>
        public static AABB2D EmptyBox()
        {
            AABB2D box = new AABB2D();
            box.min = new Vector2(Single.MaxValue);
            box.max = new Vector2(Single.MinValue);
            return box;
        }

        /// <summary>
        /// Empty box signified by min being greater than max.
        /// </summary>
        /// <returns></returns>
        public bool Empty()
        {
            return (Min.X > Max.X)
                || (Min.Y > Max.Y);
        }

        /// <summary>
        /// Set the AABB2Ds size.
        /// </summary>
        /// <param name="min"></param>
        /// <param name="max"></param>
        public void Set(Vector2 min, Vector2 max)
        {
            this.min = Vector2.Min(min, max);
            this.max = Vector2.Max(min, max);
        }   // end of Set

        /// <summary>
        /// Initialize from another bounding box.
        /// </summary>
        /// <param name="src"></param>
        public void Set(AABB2D src)
        {
            this.min = src.Min;
            this.max = src.Max;
        }

        /// <summary>
        /// Init from a Rectangle.
        /// </summary>
        /// <param name="rect"></param>
        public void Set(Rectangle rect)
        {
            this.min = new Vector2(rect.X, rect.Y);
            this.max = new Vector2(rect.Right, rect.Bottom);
        }

        /// <summary>
        /// Test an AABB2D against a point.
        /// </summary>
        /// <param name="pos"></param>
        /// <returns>True if AABB2D contains pos.  False otherwise.</returns>
        public bool Contains(Vector2 pos)
        {
            return (pos.X >= min.X) && (pos.X <= max.X)
                && (pos.Y >= min.Y) && (pos.Y <= max.Y);
        }

        /// <summary>
        /// Union self with a point in space.
        /// </summary>
        /// <param name="pos"></param>
        /// <returns></returns>
        public AABB2D Union(Vector2 pos)
        {
            min = Vector2.Min(min, pos);
            max = Vector2.Max(max, pos);
            return this;
        }

        /// <summary>
        /// Union self with other box.
        /// </summary>
        /// <param name="other"></param>
        /// <returns></returns>
        public AABB2D Union(AABB2D other)
        {
            min = Vector2.Min(min, other.Min);
            max = Vector2.Max(max, other.Max);
            return this;
        }

        /// <summary>
        /// Test two AABB2Ds against each other.
        /// </summary>
        /// <param name="box"></param>
        /// <returns>True if they intersect.  False otherwise.</returns>
        public bool Intersect(AABB2D box)
        {
            bool result = true;

            if ((min.X > box.max.X || max.X < box.min.X) ||
                 (min.Y > box.max.Y || max.Y < box.min.Y))
            {
                result = false;
            }

            return result;
        }   // end of AABB2D Intersect()

        /// <summary>
        /// Encapsulates a commonly used pattern when comparing a touch
        /// to a bounding box.  On press, the box is set as the touchedObject.
        /// On release, if this box is stil the touchedObject then this function
        /// returns true.
        /// </summary>
        /// <param name="touch">the touch potentially contacting this object</param>
        /// <param name="adjustedTouchPos">camera relative adjusted position of the touch</param>
        /// <returns></returns>
        public bool Touched( TouchContact touch, Vector2 adjustedTouchPos )
        {
            return Touched(touch, adjustedTouchPos, true);
        }   // end of Touched()

        public bool Touched(TouchContact touch, Vector2 adjustedTouchPos, bool confirm)
        {
            bool result = false;

            if (Contains(adjustedTouchPos))
            {
                if (touch.phase == TouchPhase.Began)
                {
                    // touch down - store object touched
                    touch.TouchedObject = this;
                }
                else if (touch.phase == TouchPhase.Ended)
                {
                    if (confirm)
                    {
                        AABB2D storedHitBox = touch.TouchedObject as AABB2D;
                        if (storedHitBox != null)
                        {
                            if (storedHitBox == touch.TouchedObject)
                            {
                                // touch up/released on the same object
                                result = true;
                                touch.TouchedObject = null;
                            }
                        }
                    }
                    else
                    {
                        result = true;
                        touch.TouchedObject = null;
                    }
                }
            }

            return result;
        }   // end of Touched()

        /// <summary>
        /// Encapsulates a commonly used pattern when comparing a mouse hit
        /// to a bounding box.  On left down, the box is set as the ClickedOnObject.
        /// On left up, if this box is stil the ClickedOnObject then this function
        /// returns true.
        /// </summary>
        /// <param name="mouseHit">Mouse position adjusted for overscan if needed.</param>
        /// <returns></returns>
        public bool LeftPressed(Vector2 mouseHit)
        {
            bool result = false;

            if (Contains(mouseHit))
            {
                MouseInput.OverButton = true;

                if (MouseInput.Left.WasPressed)
                {
                    MouseInput.Left.IgnoreUntilReleased = true;
                    MouseInput.ClickedOnObject = this;
                }
                if (MouseInput.Left.WasReleased && MouseInput.ClickedOnObject == this)
                {
                    result = true;
                }
            }

            return result;
        }   // end of LeftPressed()

        /// <summary>
        /// Encapsulates a commonly used pattern when comparing a mouse hit
        /// to a bounding box.  On right down, the box is set as the ClickedOnObject.
        /// On right up, if this box is stil the ClickedOnObject then this function
        /// returns true.
        /// </summary>
        /// <param name="mouseHit">Mouse position adjusted for overscan if needed.</param>
        /// <returns></returns>
        public bool RightPressed(Vector2 mouseHit)
        {
            bool result = false;

            if (Contains(mouseHit))
            {
                MouseInput.OverButton = true;

                if (MouseInput.Right.WasPressed)
                {
                    MouseInput.Right.IgnoreUntilReleased = true;
                    MouseInput.ClickedOnObject = this;
                }
                if (MouseInput.Right.WasReleased && MouseInput.ClickedOnObject == this)
                {
                    result = true;
                }
            }

            return result;
        }   // end of RightPressed()

    }   // end of class AABB2D

}   // end of namespace Boku.Common


