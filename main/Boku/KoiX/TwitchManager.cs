// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.


using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;

using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Audio;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using Microsoft.Xna.Framework.Storage;

using KoiX.UI;

using Boku.Base;

namespace KoiX
{
    // Prototype used for Twitch Completed events.
    public delegate void TwitchCompleteEvent(Object param);

    /// <summary>
    /// This is a singleton class which keeps track of animations.
    /// </summary>
    public class TwitchManager
    {
        //
        //
        // Generics based twitches.
        //
        //

        // Set delegate.
        public delegate void Set<T>(T value, Object param);

        // 
        // CreateTwitch methods.  These should be the only public interface...
        //

        /// <summary>
        /// Creates and starts a twitch.
        /// </summary>
        /// <typeparam name="T">The type of twitch.</typeparam>
        /// <param name="startValue"></param>
        /// <param name="targetValue"></param>
        /// <param name="set">Set delegate called each frame to set the current value.</param>
        /// <param name="duration">Time for twitch to take.</param>
        /// <param name="shape">Curve shape.</param>
        /// <param name="param">User defined param used w/ onComplete</param>
        /// <param name="onComplete">Event handler triggered when twitch completes.</param>
        /// <param name="onTerminate">Event handler triggered when twitch is terminated.</param>
        /// <param name="useGameTime">By default twitches used WallClockTime since they're generally used for UI.  Set this to true if you want the twitch to run off of GameTime</param>
        public static int CreateTwitch<T>(T startValue, T targetValue, Set<T> set, double duration, TwitchCurve.Shape shape, Object param = null, TwitchCompleteEvent onComplete = null, TwitchCompleteEvent onTerminate = null, bool useGameTime = false) where T : struct
        {
            return Twitch<T>.CreateTwitch(startValue, targetValue, set, duration, shape, param, onComplete, onTerminate, useGameTime);
        }

        /// <summary>
        /// Kill a twitch that has this handle id.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="handle"></param>
        public static void KillTwitch<T>(int handle) where T : struct
        {
            Twitch<T>.KillTwitch(handle);
        }

        public class Twitch<T> where T : struct
        {
            private static List<Twitch<T>> activeList = null;
            private static List<Twitch<T>> freeList = null;

            protected enum State
            {
                Starting,
                Running,
                Paused,
                Stopped,
            }
            protected State state = State.Stopped;

            protected Object param = null;              // Anything the user needs.  Generaly used in conjunction Event.
            protected event TwitchCompleteEvent Completed = null;
            protected event TwitchCompleteEvent Terminated = null;
            protected bool useGameTime = false;
            protected TwitchCurve.Shape shape = TwitchCurve.Shape.Linear;

            protected double startTime = 0.0;
            protected double duration = 1.0;

            protected float t = 0.0f;                   // How far through the animation we are.  In range [0, 1)
            // This is pre-curve t.

            protected int handle = 0; //unique handle to identify this twitch
            protected bool terminate = false;

            protected T startValue;
            protected T targetValue;
            protected Set<T> set;

            static AbstractGeneric<T> typeMath;

            static Twitch()
            {
                typeMath = GetMathForMyType();
            }

            // c'tor
            private Twitch()
            {
            }   // end of Twitch c'tor

            public static void Init()
            {
                // Create the lists.
                if (freeList == null)
                {
                    freeList = new List<Twitch<T>>();
                }

                if (activeList == null)
                {
                    activeList = new List<Twitch<T>>();
                }

            }   // end of Twitch init

            private static int gTwitchHandle = 0;

            public static int CreateTwitch(T startValue, T targetValue, Set<T> set, double duration, TwitchCurve.Shape shape, Object param, TwitchCompleteEvent onComplete, TwitchCompleteEvent onTerminate, bool useGameTime)
            {
                // Get the next free twitch.  
                // If none exists, create it.
                Twitch<T> twitch = null;
                if (freeList.Count > 0)
                {
                    twitch = freeList[freeList.Count - 1];
                    freeList.RemoveAt(freeList.Count - 1);
                }
                else
                {
                    twitch = new Twitch<T>();
                }

                twitch.startValue = startValue;
                twitch.targetValue = targetValue;
                twitch.set = set;
                twitch.duration = duration;
                twitch.shape = shape;
                twitch.param = param;
                twitch.Completed = onComplete;
                twitch.Terminated = onTerminate;
                twitch.useGameTime = useGameTime;
                twitch.handle = gTwitchHandle++;
                twitch.terminate = false;

                twitch.startTime = useGameTime ? Time.GameTimeTotalSeconds : Time.WallClockTotalSeconds;

                // Add the twitch to the active list.
                activeList.Add(twitch);

                return twitch.handle;
            }   // end of CreateTwitch()

            /// <summary>
            /// Kill the twitch with this handle id
            /// </summary>
            /// <param name="handle"></param>
            public static void KillTwitch(int handle)
            {
                for (int i = 0; i < activeList.Count; i++)
                {
                    Twitch<T> twitch = activeList[i];
                    if (twitch != null && twitch.handle == handle)
                    {
                        twitch.terminate = true;
                        break;
                    }
                }
            }

            /// <summary>
            /// Forces all active twitches to terminated and proceed
            /// immediately to final value.
            /// </summary>
            public static void KillAllTwitches()
            {
                for (int i = 0; i < activeList.Count; i++)
                {
                    Twitch<T> twitch = activeList[i];
                    twitch.terminate = true;
                }
                UpdateActiveList();
            }

            /// <summary>
            /// Clean any references this twitch may be holding.
            /// </summary>
            private void Clean()
            {
                set = null;
                param = null;
                Completed = null;
                terminate = false;
            }

            public static void UpdateActiveList()
            {
                for (int i = 0; i < activeList.Count; /* this space intentinally left blank */)
                {
                    Twitch<T> twitch = activeList[i];
                    //PV: this is a bug. The code should be:  if ( twitch!=null && twitch.Update()==false )
                    //
                    if (twitch == null || twitch.Update() == false)
                    {
                        // The twitch has indicated that it is no longer active.
                        // Move it to the free list.
                        activeList.RemoveAt(i);
                        twitch.Clean();
                        freeList.Add(twitch);
                    }
                    else
                    {
                        // Only increment the index if we didn't delete the current twitch.
                        i++;
                    }
                }
            }   // end of UpdateList()

            /// <summary>
            /// Updates the twitch's target based on the current time.
            /// </summary>
            /// <returns>True if still active, false if complete</returns>
            public bool Update()
            {
                double totalSeconds = useGameTime ? Time.GameTimeTotalSeconds : Time.WallClockTotalSeconds;

                // If this is the first call since adding to the list, adjust the start time.
                if (this.state == State.Starting)
                {
                    startTime = totalSeconds - t * duration;
                    state = State.Running;
                }

                bool active = true;

                // Recalc t.
                double elapsedTime = totalSeconds - startTime;
                t = (duration > 0.0) ? (float)(elapsedTime / duration) : 1.0f;

                // Check progress.
                active = (t < 1.0f) && (terminate == false);

                if (active)
                {
                    t = TwitchCurve.Apply(t, shape);

                    T value = typeMath.Lerp(startValue, targetValue, t);
                    set(value, param);
                }
                else
                {

                    //Note: implemented this way to preserve old behaviour
                    // if note termination delegate, then the completed will fire as before, setting the final value first
                    // this does allow new callers to specify a termination delegate for cases where they don't want a quick 
                    // snap to the final value.
                    if (terminate && Terminated != null)
                    {
                        Terminated(param);
                    }
                    else
                    {
                        // Ensure that we've gotten to the final value.
                        set(targetValue, param);

                        // Trigger any Completed events.

                        if (Completed != null)
                        {
                            Completed(param);
                        }
                    }
                }

                return active;
            }   // end of BaseTwitch Update()

            private static AbstractGeneric<T> GetMathForMyType()
            {
                if (typeof(T) == typeof(float))
                {
                    return GenericFloat.Instance as AbstractGeneric<T>;
                }
                if (typeof(T) == typeof(int))
                {
                    return GenericInt.Instance as AbstractGeneric<T>;
                }
                if (typeof(T) == typeof(Vector2))
                {
                    return GenericVector2.Instance as AbstractGeneric<T>;
                }
                if (typeof(T) == typeof(Vector3))
                {
                    return GenericVector3.Instance as AbstractGeneric<T>;
                }
                if (typeof(T) == typeof(Vector4))
                {
                    return GenericVector4.Instance as AbstractGeneric<T>;
                }
                if (typeof(T) == typeof(Matrix))
                {
                    return GenericMatrix.Instance as AbstractGeneric<T>;
                }
                if (typeof(T) == typeof(Rectangle))
                {
                    return GenericRectangle.Instance as AbstractGeneric<T>;
                }
                if (typeof(T) == typeof(RectangleF))
                {
                    return GenericRectangleF.Instance as AbstractGeneric<T>;
                }
                if (typeof(T) == typeof(Point))
                {
                    return GenericPoint.Instance as AbstractGeneric<T>;
                }
                if (typeof(T) == typeof(Color))
                {
                    return GenericColor.Instance as AbstractGeneric<T>;
                }
                if (typeof(T) == typeof(Padding))
                {
                    return GenericPadding.Instance as AbstractGeneric<T>;
                }

                throw new Exception("Unsupported twitch datatype");
            }

        }   // end of class Twitch

        //
        // Warning, bizarre code ahead.  This is to get around C#'s reluctance
        // to allow you use regular operators on generic types even if you
        // know that they are supported.  The alternative is boxing and 
        // unboxing so this is actually kind of efficient.
        //
        abstract class AbstractGeneric<T>
        {
            protected AbstractGeneric() { }

            public abstract T Lerp(T a, T b, float t);
        }

        class GenericFloat : AbstractGeneric<float>
        {
            public static GenericFloat Instance = new GenericFloat();

            public override float Lerp(float a, float b, float t)
            {
                return MyMath.Lerp(a, b, t);
            }
        }
        class GenericInt : AbstractGeneric<int>
        {
            public static GenericInt Instance = new GenericInt();

            public override int Lerp(int a, int b, float t)
            {
                return (int)MyMath.Lerp(a, b, t);
            }
        }
        class GenericVector2 : AbstractGeneric<Vector2>
        {
            public static GenericVector2 Instance = new GenericVector2();

            public override Vector2 Lerp(Vector2 a, Vector2 b, float t)
            {
                return MyMath.Lerp(a, b, t);
            }
        }
        class GenericVector3 : AbstractGeneric<Vector3>
        {
            public static GenericVector3 Instance = new GenericVector3();

            public override Vector3 Lerp(Vector3 a, Vector3 b, float t)
            {
                return MyMath.Lerp(a, b, t);
            }
        }
        class GenericVector4 : AbstractGeneric<Vector4>
        {
            public static GenericVector4 Instance = new GenericVector4();

            public override Vector4 Lerp(Vector4 a, Vector4 b, float t)
            {
                return MyMath.Lerp(a, b, t);
            }
        }
        class GenericMatrix : AbstractGeneric<Matrix>
        {
            public static GenericMatrix Instance = new GenericMatrix();

            public override Matrix Lerp(Matrix a, Matrix b, float t)
            {
                return MyMath.Lerp(ref a, ref b, t);
            }
        }
        class GenericRectangle : AbstractGeneric<Rectangle>
        {
            public static GenericRectangle Instance = new GenericRectangle();

            public override Rectangle Lerp(Rectangle a, Rectangle b, float t)
            {
                return MyMath.Lerp(a, b, t);
            }
        }
        class GenericRectangleF : AbstractGeneric<RectangleF>
        {
            public static GenericRectangleF Instance = new GenericRectangleF();

            public override RectangleF Lerp(RectangleF a, RectangleF b, float t)
            {
                return MyMath.Lerp(a, b, t);
            }
        }
        class GenericPoint : AbstractGeneric<Point>
        {
            public static GenericPoint Instance = new GenericPoint();

            public override Point Lerp(Point a, Point b, float t)
            {
                return MyMath.Lerp(a, b, t);
            }
        }
        class GenericColor : AbstractGeneric<Color>
        {
            public static GenericColor Instance = new GenericColor();

            public override Color Lerp(Color a, Color b, float t)
            {
                return MyMath.Lerp(a, b, t);
            }
        }
        class GenericPadding : AbstractGeneric<Padding>
        {
            public static GenericPadding Instance = new GenericPadding();

            public override Padding Lerp(Padding a, Padding b, float t)
            {
                return MyMath.Lerp(a, b, t);
            }
        }

        //
        //  TwitchManager class
        //

        // c'tor
        private TwitchManager()
        {
        }   // end of c'tor

        public static void Init()
        {
            Twitch<float>.Init();
            Twitch<int>.Init();
            Twitch<Vector2>.Init();
            Twitch<Vector3>.Init();
            Twitch<Vector4>.Init();
            Twitch<Matrix>.Init();
            Twitch<Rectangle>.Init();
            Twitch<RectangleF>.Init();
            Twitch<Point>.Init();
            Twitch<Color>.Init();
            Twitch<Padding>.Init();

        }   // end of Init()

        public static void Update()
        {
            Twitch<float>.UpdateActiveList();
            Twitch<int>.UpdateActiveList();
            Twitch<Vector2>.UpdateActiveList();
            Twitch<Vector3>.UpdateActiveList();
            Twitch<Vector4>.UpdateActiveList();
            Twitch<Matrix>.UpdateActiveList();
            Twitch<Rectangle>.UpdateActiveList();
            Twitch<RectangleF>.UpdateActiveList();
            Twitch<Point>.UpdateActiveList();
            Twitch<Color>.UpdateActiveList();
            Twitch<Padding>.UpdateActiveList();

        }   // end of Update()

        /// <summary>
        /// Force all active twitches to immediately terminate.
        /// </summary>
        static public void KillAllTwitches()
        {
            Twitch<float>.KillAllTwitches();
            Twitch<int>.KillAllTwitches();
            Twitch<Vector2>.KillAllTwitches();
            Twitch<Vector3>.KillAllTwitches();
            Twitch<Vector4>.KillAllTwitches();
            Twitch<Matrix>.KillAllTwitches();
            Twitch<Rectangle>.KillAllTwitches();
            Twitch<RectangleF>.KillAllTwitches();
            Twitch<Point>.KillAllTwitches();
            Twitch<Color>.KillAllTwitches();
            Twitch<Padding>.KillAllTwitches();

        }

    }   // end of class TwitchManager

}   // end of namespace KoiX


