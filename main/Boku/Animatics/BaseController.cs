// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.


using System;
using System.Collections.Generic;
using System.Diagnostics;

using Microsoft.Xna.Framework;

namespace Boku.Animatics
{
    /// <summary>
    /// Base class for animation controllers. These are the handles by which animation
    /// is controlled, through time and weights.
    /// </summary>
    public abstract class BaseController
    {
        #region Members
        /// <summary>
        /// Current time into the animation in ticks.
        /// </summary>
        private long currentTicks = 0;
        /// <summary>
        /// Current weight of this animation if blended.
        /// </summary>
        private float weight = 1.0f;

        protected const float kWeightTol = 1.0e-3f;
        #endregion Members

        #region Accessors
        /// <summary>
        /// A weight for when they are blended.
        /// </summary>
        public float Weight
        {
            get { return weight; }
            set 
            {
                Debug.Assert((weight >= -kWeightTol) && (weight <= 1 + kWeightTol));
                weight = value; 
            }
        }
        /// <summary>
        /// Current time into the animation in ticks.
        /// </summary>
        public long CurrentTicks
        {
            get { return currentTicks; }
            set { currentTicks = value; }
        }
        #endregion Accessors

        #region Public
        /// <summary>
        /// Convert input seconds into tick counts.
        /// </summary>
        /// <param name="secs"></param>
        /// <returns></returns>
        public static long SecsToTicks(double secs)
        {
            return (long)(secs * TimeSpan.TicksPerSecond);
        }

        /// <summary>
        /// Update timing info by advancing by the input number of seconds.
        /// </summary>
        /// <param name="deltaSecs"></param>
        public void Update(float deltaSecs)
        {
            Update(SecsToTicks(deltaSecs));
        }

        /// <summary>
        /// Advance the clock by ticks. No heavy lifting, just update internal clock state.
        /// </summary>
        /// <param name="advTicks"></param>
        public abstract void Update(long advTicks);

        /// <summary>
        /// Compute the localToParent values we know about, leaving the rest alone.
        /// localToParent has already been populated with default values.
        /// </summary>
        /// <param name="inst"></param>
        /// <param name="localToParent"></param>
        internal abstract void GetTransforms(AnimationInstance inst, Matrix[] localToParent);
        #endregion Public

        #region Internal
        #endregion Internal

    }
}
