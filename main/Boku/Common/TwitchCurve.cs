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

using Boku.Base;

namespace Boku.Common
{
    /// <summary>
    /// Controls the animation curve used by Twitches.
    /// </summary>
    public class TwitchCurve
    {
        public enum Shape
        {
            Linear,
            EaseIn,
            EaseOut,
            EaseInOut,
            OvershootIn,
            OvershootOut,
            OvershootInOut
        };

        // TODO we should probably also have some parameters on
        // this to control how much ease there is.  Right now it's
        // using a simple SmoothStep (cubic) for the curve.

        // Private c'tor since we never need to be instantiated.
        private TwitchCurve()
        {
        }   // end of TwitchCurve c'tor

        /// <summary>
        /// Applies the curve to the paramter t.  Assumes that t is
        /// in the range [0, 1].
        /// </summary>
        public static float Apply(float t, Shape shape)
        {
            const float overshootAmplitutde = 0.1f;
            float result = t;

            switch (shape)
            {
                case Shape.Linear:
                    result = t;
                    break;
                case Shape.EaseIn:
                    result = 2.0f * MyMath.SmoothStep(0.0f, 2.0f, t);
                    break;
                case Shape.EaseOut:
                    result = 2.0f * MyMath.SmoothStep(-1.0f, 1.0f, t) - 1.0f;
                    break;
                case Shape.EaseInOut:
                    result = MyMath.SmoothStep(0.0f, 1.0f, t);
                    break;
                case Shape.OvershootIn:
                    {
                        t *= 0.5f;
                        float smooth = MyMath.SmoothStep(0.0f, 1.0f, t);
                        result = smooth - overshootAmplitutde * (float)Math.Cos((t - 0.25f) * MathHelper.TwoPi);
                        // Blend toward smooth at tail.
                        if (t < 0.1f)
                        {
                            result = MyMath.Lerp(smooth, result, t * 10.0f);
                        }
                        result *= 2.0f;
                    }
                    break;
                case Shape.OvershootOut:
                    {
                        t = 0.5f + t * 0.5f;
                        float smooth = MyMath.SmoothStep(0.0f, 1.0f, t);
                        result = smooth - overshootAmplitutde * (float)Math.Cos((t - 0.25f) * MathHelper.TwoPi);
                        // Blend toward smooth at tail.
                        if (t > 0.9f)
                        {
                            result = MyMath.Lerp(smooth, result, (1.0f - t) * 10.0f);
                        }
                        result = (result - 0.5f) * 2.0f;
                    }
                    break;
                case Shape.OvershootInOut:
                    {
                        float smooth = MyMath.SmoothStep(0.0f, 1.0f, t);
                        result = smooth - overshootAmplitutde * (float)Math.Cos((t - 0.25f) * MathHelper.TwoPi);
                        // Blend toward smooth at tails.
                        if (t < 0.1f)
                        {
                            result = MyMath.Lerp(smooth, result, t * 10.0f);
                        }
                        else if (t > 0.9f)
                        {
                            result = MyMath.Lerp(smooth, result, (1.0f - t) * 10.0f);
                        }
                    }
                    break;
            }
            
            return result;
        }   // TwitchCurve Apply()

    }   // end of class TwitchCurve

}   // end of namespace Boku.Common
