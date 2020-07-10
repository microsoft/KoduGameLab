// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;

using Microsoft.Xna.Framework;

/// Purely internal class, convenient for stepping through the grid on
/// a ray trace. Abstract class contains two real classes, one to step
/// with the x direction major, and the other stepping y primary. No
/// further extension anticipated.

namespace Boku.SimWorld.Terra
{
    public partial class VirtualMap
    {
        /// <summary>
        /// Helper class for rasterizing through virtual heightmap.
        /// </summary>
        private abstract class Stepper
        {
            #region Members
            /// <summary>
            /// Integer coordinates.
            /// </summary>
            private Point start;
            private Point end;
            private Point coord;
            private Point last;
            private int del;

            /// <summary>
            /// Floating point coords (NOT world space).
            /// </summary>
            private Vector2 startPos;
            private Vector2 step = Vector2.Zero;

            /// <summary>
            /// These are in world space, and just kept to recover
            /// the exact hit position.
            /// </summary>
            private Vector3 p0;
            private Vector3 p1;
            private float cubeSize;
            private Vector2 min;

            private Vector3 normMajor;
            private Vector3 normMinor;

            private static StepperX stepperX = new StepperX();
            private static StepperY stepperY = new StepperY();

            #endregion Members

            #region Accessors

            /// <summary>
            /// Current position
            /// </summary>
            public Point Coord
            {
                get { return coord; }
            }
            /// <summary>
            /// Current x
            /// </summary>
            public int X
            {
                get { return coord.X; }
            }
            /// <summary>
            /// Current y
            /// </summary>
            public int Y
            {
                get { return coord.Y; }
            }
            /// <summary>
            /// Normal at boundaries crossed in a major step
            /// </summary>
            public Vector3 NormMajor
            {
                get { return normMajor; }
            }
            /// <summary>
            /// Normal at boundaries crossed in a minor step
            /// </summary>
            public Vector3 NormMinor
            {
                get { return normMinor; }
            }
            /// <summary>
            /// Coordinate from a minor step.
            /// </summary>
            public abstract Point MinorCoord
            {
                get;
            }
            /// <summary>
            /// Position of crossing a boundary in major direction.
            /// </summary>
            public abstract Vector3 HitMajor
            {
                get;
            }
            /// <summary>
            /// Position of crossing a boundary in minor direction.
            /// </summary>
            public abstract Vector3 HitMinor
            {
                get;
            }
            /// <summary>
            /// Last coordinate to evaluate.
            /// </summary>
            public Point End
            {
                get { return end; }
            }

            #endregion Accessors

            #region Subclasses
            /// <summary>
            /// Variant for stepping x direction major.
            /// </summary>
            private class StepperX : Stepper
            {
                /// <summary>
                /// Coordinate from a minor step.
                /// </summary>
                public override Point MinorCoord
                {
                    get { return new Point(last.X, coord.Y); }
                }
                /// <summary>
                /// Position of crossing a boundary in major direction.
                /// </summary>
                public override Vector3 HitMajor
                {
                    get
                    {
                        int icoord = coord.X;
                        if (del < 0)
                            ++icoord;
                        float x = min.X + icoord * cubeSize;
                        float t = (x - p0.X) / (p1.X - p0.X);
                        float y = p0.Y + t * (p1.Y - p0.Y);
                        float z = p0.Z + t * (p1.Z - p0.Z);

                        return new Vector3(x, y, z);
                    }
                }
                /// <summary>
                /// Position of crossing a boundary in minor direction.
                /// </summary>
                public override Vector3 HitMinor
                {
                    get
                    {
                        int jcoord = coord.Y;
                        if (step.Y * del < 0)
                            ++jcoord;
                        float y = min.Y + jcoord * cubeSize;
                        float t = (y - p0.Y) / (p1.Y - p0.Y);
                        float x = p0.X + t * (p1.X - p0.X);
                        float z = p0.Z + t * (p1.Z - p0.Z);

                        return new Vector3(x, y, z);
                    }
                }

                /// <summary>
                /// Setup before first step.
                /// </summary>
                /// <param name="vMap"></param>
                /// <param name="p0"></param>
                /// <param name="p1"></param>
                public override void Init(VirtualMap vMap, Vector3 p0, Vector3 p1)
                {
                    base.Init(vMap, p0, p1);
                    /// We'll just punt on the degenerate case of start == end, because
                    /// in that case we do the first point, and immediately exit the loop.
                    /// So leaving step, and xMajor at defaults works fine.
                    if (start != end)
                    {
                        del = p1.X > p0.X ? 1 : -1;

                        step.X = del;
                        Debug.Assert(!float.IsNaN(step.X));

                        step.Y = (p1.Y - p0.Y) / (p1.X - p0.X);

                        normMajor = new Vector3(-step.X, 0.0f, 0.0f);
                        normMinor = new Vector3(
                            0.0f,
                            step.Y * del > 0.0f ? -1.0f : 1.0f,
                            0.0f);
                    }
                    else
                    {
                        normMajor = normMinor = Vector3.UnitZ;
                    }
                }
                /// <summary>
                /// Step to first even boundary.
                /// </summary>
                /// <returns></returns>
                public override bool StepFirst()
                {
                    last = coord;
                    if (start.X == end.X)
                    {
                        /// Pathological little start and end right on top of each other.
                        coord = end;
                        return start != end;
                    }
                    float firstStep = del > 0
                        ? ++start.X - startPos.X
                        : start.X-- - startPos.X;
                    if (Math.Abs(firstStep) > 0.0f)
                    {
                        startPos.X = start.X;
                        startPos.Y += firstStep * step.Y;
                        coord = new Point(
                            (int)startPos.X,
                            (int)startPos.Y);
                        return true;
                    }
                    else if (start != end)
                    {
                        coord = end;
                        return true;
                    }
                    return false;
                }
                /// <summary>
                /// Need to step in the minor direction?
                /// </summary>
                /// <returns></returns>
                public override bool StepMinor()
                {
                    return last.Y != coord.Y;
                }
                /// <summary>
                /// Take a step in the major direction, unless we're done.
                /// Return whether we're done.
                /// </summary>
                /// <returns></returns>
                public override bool StepMajor()
                {
                    last = coord;
                    if (coord.X == end.X)
                    {
                        return false;
                    }
                    coord.X += del;

                    /// Do this multiply instead of accumulating to keep down error.
                    coord.Y = (int)(startPos.Y + (coord.X - start.X) * step.Y);

                    return true;
                }
            };
            /// <summary>
            /// Variant to step in the y major direction.
            /// </summary>
            private class StepperY : Stepper
            {
                /// <summary>
                /// Coordinate from a minor step.
                /// </summary>
                public override Point MinorCoord
                {
                    get { return new Point(coord.X, last.Y); }
                }
                /// <summary>
                /// Position of crossing a boundary in major direction.
                /// </summary>
                public override Vector3 HitMajor
                {
                    get
                    {
                        int jcoord = coord.Y;
                        if (del < 0)
                            ++jcoord;
                        float y = min.Y + jcoord * cubeSize;
                        float t = (y - p0.Y) / (p1.Y - p0.Y);
                        float x = p0.X + t * (p1.X - p0.X);
                        float z = p0.Z + t * (p1.Z - p0.Z);

                        return new Vector3(x, y, z);
                    }
                }
                /// <summary>
                /// Position of crossing a boundary in minor direction.
                /// </summary>
                public override Vector3 HitMinor
                {
                    get
                    {
                        int icoord = coord.X;
                        if (step.X * del < 0.0f)
                            ++icoord;
                        float x = min.X + icoord * cubeSize;
                        float t = (x - p0.X) / (p1.X - p0.X);
                        float y = p0.Y + t * (p1.Y - p0.Y);
                        float z = p0.Z + t * (p1.Z - p0.Z);

                        return new Vector3(x, y, z);
                    }
                }
                /// <summary>
                /// Setup before first step.
                /// </summary>
                /// <param name="vMap"></param>
                /// <param name="p0"></param>
                /// <param name="p1"></param>
                public override void Init(VirtualMap vMap, Vector3 p0, Vector3 p1)
                {
                    base.Init(vMap, p0, p1);

                    /// We'll just punt on the degenerate case of start == end, because
                    /// in that case we do the first point, and immediately exit the loop.
                    /// So leaving step, and xMajor at defaults works fine.
                    if (start != end)
                    {
                        del = p1.Y > p0.Y ? 1 : -1;

                        step.Y = del;

                        step.X = (p1.X - p0.X) / (p1.Y - p0.Y);
                        Debug.Assert(!float.IsNaN(step.X));

                        normMajor = new Vector3(0.0f, -step.Y, 0.0f);
                        normMinor = new Vector3(
                            step.X * del > 0.0f ? -1.0f : 1.0f,
                            0.0f,
                            0.0f);
                    }
                    else
                    {
                        normMajor = normMinor = Vector3.UnitZ;
                    }
                }
                /// <summary>
                /// Step to first even boundary.
                /// </summary>
                /// <returns></returns>
                public override bool StepFirst()
                {
                    last = coord;
                    if (start.Y == end.Y)
                    {
                        /// Pathological little baby half step.
                        coord = end;
                        return start != end;
                    }
                    float firstStep = del > 0
                        ? ++start.Y - startPos.Y
                        : start.Y-- - startPos.Y;
                    if (Math.Abs(firstStep) > 0.0f)
                    {
                        startPos.Y = start.Y;
                        startPos.X += firstStep * step.X;
                        coord = new Point(
                            (int)startPos.X,
                            (int)startPos.Y);
                        return true;
                    }
                    else if (start != end)
                    {
                        coord = end;
                        return true;
                    }
                    return false;
                }
                /// <summary>
                /// Need to step in the minor direction?
                /// </summary>
                /// <returns></returns>
                public override bool StepMinor()
                {
                    return last.X != coord.X;
                }
                /// <summary>
                /// Take a step in the major direction, unless we're done.
                /// Return whether we're done.
                /// </summary>
                /// <returns></returns>
                public override bool StepMajor()
                {
                    last = coord;
                    if (coord.Y == end.Y)
                    {
                        return false;
                    }

                    coord.Y += del;

                    /// Do this multiply instead of accumulating to keep down error.
                    coord.X = (int)(startPos.X + (coord.Y - start.Y) * step.X);

                    return true;
                }
            }
            #endregion Subclasses

            #region Public

            /// <summary>
            /// Determine the proper stepping direction, and return a stepper
            /// of appropriate type.
            /// </summary>
            /// <param name="vMap"></param>
            /// <param name="p0"></param>
            /// <param name="p1"></param>
            /// <returns></returns>
            public static Stepper Select(VirtualMap vMap, Vector3 p0, Vector3 p1)
            {
                bool xMajor = Math.Abs(p0.X - p1.X) > Math.Abs(p0.Y - p1.Y);

                Stepper stepper = null;
                if (xMajor)
                    stepper = stepperX;
                else
                    stepper = stepperY;
                stepper.Init(vMap, p0, p1);

                return stepper;
            }

            /// <summary>
            /// Initialize before first step.
            /// </summary>
            /// <param name="vMap"></param>
            /// <param name="p0"></param>
            /// <param name="p1"></param>
            public virtual void Init(VirtualMap vMap, Vector3 p0, Vector3 p1)
            {
                this.p0 = p0;
                this.p1 = p1;
                this.cubeSize = vMap.CubeSize;
                this.min = vMap.Min;

                startPos = new Vector2(
                    (p0.X - vMap.Min.X) / vMap.CubeSize,
                    (p0.Y - vMap.Min.Y) / vMap.CubeSize);

                start = new Point((int)startPos.X, (int)startPos.Y);
                end = new Point(
                    (int)((p1.X - vMap.Min.X) / vMap.CubeSize),
                    (int)((p1.Y - vMap.Min.Y) / vMap.CubeSize));

                coord = start;
            }

            /// <summary>
            /// Step up to first boundary.
            /// </summary>
            /// <returns></returns>
            public abstract bool StepFirst();
            /// <summary>
            /// Step in minor direction.
            /// </summary>
            /// <returns></returns>
            public abstract bool StepMinor();
            /// <summary>
            /// Step in major direction.
            /// </summary>
            /// <returns></returns>
            public abstract bool StepMajor();
            /// <summary>
            /// Did we process the end coordinate?
            /// </summary>
            /// <returns></returns>
            public bool DidEnd()
            {
                return last == end;
            }
            /// <summary>
            /// Set current coordinate to end point.
            /// </summary>
            public void SetToEnd()
            {
                coord = end;
            }
            /// <summary>
            /// Is there nothing to do?
            /// </summary>
            /// <returns></returns>
            public bool Empty()
            {
                return start == end;
            }

            #endregion Public
        }

    }
}
