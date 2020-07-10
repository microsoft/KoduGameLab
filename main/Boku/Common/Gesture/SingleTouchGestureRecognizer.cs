// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Xna.Framework;

namespace Boku.Common.Gesture
{
    /// <summary>
    /// Base class used by most common gestures that can be performed with an arbitrary number of touch contacts. Such as drag, tap, swipe...
    /// The position of the touch contacts are averaged and stored in the StartPosition and Position properties
    /// </summary>
    public abstract class SingleTouchGestureRecognizer : GestureRecognizer
    {
        /// <summary>
        /// The exact number of fingers required for the gesture to be recognized
        /// </summary>
        public int RequiredFingerCount = 1;

        Vector2 startPos = Vector2.Zero;
        Vector2 pos = Vector2.Zero;

        protected override int GetRequiredTouchCount()
        {
            return RequiredFingerCount;
        }

        /// <summary>
        /// Initial touch contact(s) position
        /// </summary>
        public Vector2 StartPosition
        {
            get { return startPos; }
            protected set { startPos = value; }
        }

        /// <summary>
        /// Current touch contact(s) position
        /// </summary>
        public Vector2 Position
        {
            get { return pos; }
            protected set { pos = value; }
        }
    }
}
