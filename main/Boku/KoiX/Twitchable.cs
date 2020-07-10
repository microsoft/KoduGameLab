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

namespace KoiX
{
    /// <summary>
    /// Generic class for creating twitchable types.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public class Twitchable<T> where T:struct, IEquatable<T>
    {
        T curValue;
        T targetValue;
        int twitchHandle = -1;

        float twitchTime;
        TwitchCurve.Shape curve;
        IDirty parent;
        bool useGameTime;       // Use game time for animation rather than the default, wall clock time.
        bool firstTime = true;  // Allows us to force immediate change the first time the value is set.

        /// <summary>
        /// This is the parent who's dirty flag is set whenever this Twitchable changes value.
        /// </summary>
        public IDirty Parent
        {
            get { return parent; }
            set { parent = value; }
        }

        public Twitchable(float twitchTime, TwitchCurve.Shape curve, IDirty parent = null, T startingValue = default(T), bool useGameTime = false)
        {
            Debug.Assert(parent == null || parent is IDirty);

            this.twitchTime = twitchTime;
            this.curve = curve;
            this.parent = parent;
            this.curValue = this.targetValue = startingValue;
            this.useGameTime = useGameTime;
        }

        public T Value
        {
            get { return curValue; }
            set
            {
                if (firstTime)
                {
                    TargetValue = value;
                    firstTime = false;
                }
                else if (!value.Equals(targetValue))
                {
                    targetValue = value;
                    TwitchManager.Set<T> set = delegate(T val, object param) 
                    { 
                        curValue = val;

                        if (parent != null)
                        {
                            parent.Dirty = true;
                        }
                    };
                    twitchHandle = TwitchManager.CreateTwitch<T>(curValue, targetValue, set, twitchTime, curve, param: parent, useGameTime: useGameTime);
                }
            }
        }

        /// <summary>
        /// The value we're twitching toward.  
        /// Only set this when reseting the object.
        /// </summary>
        public T TargetValue
        {
            get { return targetValue; }
            set
            {
                TwitchManager.KillTwitch<T>(twitchHandle);
                targetValue = curValue = value;
                if (parent != null)
                {
                    parent.Dirty = true;
                }
            }
        }

    }   // end of class Twitchable

}   // end of namespace KoiX
