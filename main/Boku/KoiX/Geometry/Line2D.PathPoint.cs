// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.


using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Windows.Forms;
using System.IO;

using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

using KoiX;

namespace KoiX.Geometry
{
 	public partial class Line2D
	{
        protected enum Action
        {
            StartPath,
            StartLoop,
            AddPoint,

            DrawDot,
        }

        protected enum Prim
        {
            None = 0,
            Dot,
            Line,
            Arc,
        }

        /// <summary>
        /// For batched rendering, the PathPoints store the raw data.
        /// </summary>
        protected class PathPoint
        {
            public Action action;   // Action associated with this point.
            public Vector2 point;
            public Color color;
            public float strokeRadius;
            public float arcRadius;
            public bool sharp = false;  // If true, no fill on the corner.  Just a sharp join.
            public float edgeBlend;

            /*
            public bool needCap;
            public Vector2 capAxis; // For points at the end of the path, this is a normalized vector from the point torward the cap.
            public Vector2 arcCenter;
            // The shorten value is how much the lines connecting to this point are effected.
            // In the case where arcRadius <= stroke Radius this is the amount that the inner vertex must be moved to avoid overlap.
            // In the case of a wider arc radius, this is the amount that both vertices must be mvoed back.
            public float shorten;
            public int indexPrevP = -1; // For AddPoint calls, this is the index of the previous point that we're drawing the line from.
            */

            public PathPoint(Action action, Vector2 point, Color color, float strokeRadius, float arcRadius, bool sharp, float edgeBlend)
            {
                this.action = action;
                this.point = point;
                this.color = color;
                this.strokeRadius = strokeRadius;
                this.arcRadius = arcRadius;
                this.sharp = sharp;
                this.edgeBlend = edgeBlend;
            }

        }   // end of class PathPoint

    }   // end of class Line2D

}   // end of namespace KoiX.Geometry
