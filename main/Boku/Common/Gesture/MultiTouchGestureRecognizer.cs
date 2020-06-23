using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Xna.Framework;

namespace Boku.Common.Gesture
{
    /// <summary>
    /// Base class for multi-touch gestures that require individual access to each finger position
    /// </summary>
    public abstract class MultiTouchGestureRecognizer : GestureRecognizer
    {
        Vector2[] startPos;
        protected Vector2[] StartPosition
        {
            get { return startPos; }
            set { startPos = value; }
        }

        Vector2[] pos;
        protected Vector2[] Position
        {
            get { return pos; }
            set { pos = value; }
        }

        public MultiTouchGestureRecognizer()
        {
            OnTouchCountChanged(GetRequiredTouchCount());
        }

        protected void OnTouchCountChanged(int touchCount)
        {
            StartPosition = new Vector2[touchCount];
            Position = new Vector2[touchCount];
        }

        public int RequiredTouchCount
        {
            get { return GetRequiredTouchCount(); }
        }

        /// <summary>
        /// Get the position of the touch contact at the given index
        /// </summary>
        /// <param name="index">hopefully you've given a valid index!</param>
        /// <returns>The position of the touch contact, or Vector2.Zero if you've requested an invalid index</returns>
        public Vector2 GetPosition(int index)
        {
            if( index < pos.Length  && index > -1)
            {
                return pos[index];
            }
            return Vector2.Zero;
        }

        /// <summary>
        /// Get the start position of the touch contact for the given index
        /// </summary>
        /// <param name="index">hopefully you've given a valid index!</param>
        /// <returns>The start position of the touch contact, or Vector2.Zero if you've requested an invalid index</returns>
        public Vector2 GetStartPosition(int index)
        {
            if (index < startPos.Length && index > -1)
            {
                return startPos[index];
            }
            return Vector2.Zero;
        }
    }
}
