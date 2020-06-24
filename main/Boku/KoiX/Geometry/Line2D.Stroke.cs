
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
        /// <summary>
        /// Base class used internally by Line2D
        /// </summary>
        protected class Stroke
        {
            public Vertex[] verts;

            /// <summary>
            /// Protected c'tor.  Should not be instanciated.
            /// </summary>
            protected Stroke()
            {
            }

            // For lines, dots, and caps.
            public virtual void CalcVertices() { Debug.Assert(false, "Something's wrong.  We shouldn't be calling this.");  }  
            // For joints.
            public virtual void CalcVertices(Line line0, Line line1) { Debug.Assert(false, "Something's wrong.  We shouldn't be calling this."); }

            static public void CalcVertices(List<Stroke> strokes)
            {
                // Do lines dots, and caps first.
                foreach(Stroke stroke in strokes)
                {
                    if(stroke is Cap || stroke is Line || stroke is Dot)
                    {
                        stroke.CalcVertices();
                    }
                }

                Line loopBegin = null;

                for (int i = 0; i < strokes.Count; i++)
                {
                    Stroke stroke = strokes[i];

                    Line l = stroke as Line;
                    if (l != null)
                    {
                        if (l.p0.action == Action.StartLoop)
                        {
                            loopBegin = l;
                        }
                    }

                    if (stroke is Sharp || stroke is Arc || stroke is Fill)
                    {
                        Line prevLine = null;
                        Line nextLine = null;
                        if (i > 0)
                        {
                            prevLine = strokes[i - 1] as Line;
                        }
                        if (i < strokes.Count - 2)
                        {
                            nextLine = strokes[i + 1] as Line;
                        }
                        // Decide if we need to loop back.
                        // if at end of strokes list
                        // or next stroke is not a line with p0.action == AddPoint
                        bool loopBack = loopBegin != null;
                        bool atEnd = i == strokes.Count - 1;
                        if (!atEnd)
                        {
                            Line tmp = strokes[i + 1] as Line;
                            loopBack = tmp == null || tmp.p0.action != Action.AddPoint;
                        }
                        if (loopBack)
                        {
                            // Need to loop back to see if we're in a loop or a path.
                            // If it's a loop set nextLine
                            // to the first segment of the loop.
                            nextLine = loopBegin;
                            loopBegin = null;
                        }
                        stroke.CalcVertices(prevLine, nextLine);
                    }
                }
            }

        }   // end of class Stroke

        protected class Line : Stroke
        {
            public PathPoint p0;
            public PathPoint p1;

            public Line(PathPoint p0, PathPoint p1)
            {
                this.p0 = p0;
                this.p1 = p1;

                verts = new Vertex[4];
            }

            override public void CalcVertices()
            {
                // Fill in the simple stuff.
                for (int i = 0; i < 4; i++)
                {
                    verts[i].point0 = new Vector3(p0.point, p0.strokeRadius);
                    verts[i].point1 = new Vector3(p1.point, p1.strokeRadius);
                    verts[i].prim = (int)Prim.Line;
                }
                verts[0].color = verts[1].color = p0.color.ToVector4();
                verts[2].color = verts[3].color = p1.color.ToVector4();
                verts[0].edgeBlend = verts[1].edgeBlend = p0.edgeBlend;
                verts[2].edgeBlend = verts[3].edgeBlend = p1.edgeBlend;

                // Calc bounding quad in pixels.
                Vector2 axis = p1.point - p0.point;
                float axisLength = axis.Length();
                Vector2 normalizedAxis = axis;
                if (axisLength > 0)
                {
                    normalizedAxis /= axisLength;
                }
                else
                {
                    // This option is needed when drawing dots.
                    normalizedAxis = Vector2.UnitX;
                }

                // Calc radius inflated to cover blurred edge.
                float radius0 = p0.strokeRadius + p0.edgeBlend;
                float radius1 = p1.strokeRadius + p1.edgeBlend;

                Vector2 right = new Vector2(normalizedAxis.Y, -normalizedAxis.X);   // Normalized vector at right angle to axis.

                // At this point we need to calculate the vertex positions.  The case when the line has differing radii
                // is a bit more complex but since it's rarely used we can just fake it by using the larger radius.  This
                // then also covers the case when the radii are equal.
                // TODO (****) This still creates crappy looking joints.  Need to decide whether or not it's worth fixing.
                /*
                float radius = Math.Max(radius0, radius1);
                verts[0].pixels = p0.point - right * radius;
                verts[1].pixels = p0.point + right * radius;
                verts[2].pixels = p1.point - right * radius;
                verts[3].pixels = p1.point + right * radius;
                */
                verts[0].pixels = p0.point - right * radius0;
                verts[1].pixels = p0.point + right * radius0;
                verts[2].pixels = p1.point - right * radius1;
                verts[3].pixels = p1.point + right * radius1;

            }   // end of CalcVertices()

        }   // end of class Line

        /// <summary>
        /// Smoothed joint where arcRadius greater than or equal to strokeRadius.
        /// </summary>
        protected class Arc : Stroke
        {
            public PathPoint p;

            public Arc(PathPoint p)
            {
                this.p = p;

                verts = new Vertex[5];
            }

            override public void CalcVertices(Line line0, Line line1)
            {
                // Need to find the new vertex position which will be on the outside of
                // the corner.
                float t = 0;
                float arcRadius = line0.p1.arcRadius;

                // "This" side.
                // Calc t.
                MyMath.LineLineIntersect(line0.verts[2].pixels, line0.verts[0].pixels, line1.verts[0].pixels, line1.verts[2].pixels, out t);
                // Calc intersection point from t.
                Vector2 intersection = line0.verts[0].pixels - line0.verts[2].pixels;
                intersection = line0.verts[2].pixels + t * intersection;
                // Flip if on wrong side (ie inside when we want outside).
                bool flipped = t > 0;
                if (flipped)
                {
                    // Reflect across center point to get intersection on other side.
                    intersection = 2.0f * line0.p1.point - intersection;
                }

                // Calc normalize axis of incoming line segment.
                Vector2 axis = line0.p0.point - line0.p1.point;
                axis.Normalize();
                // ...and its perpendicular.
                Vector2 right = new Vector2(axis.Y, -axis.X);

                // Calc arc center.
                Vector2 dirToCenter = line0.p1.point - intersection;
                dirToCenter.Normalize();
                // Project right onto dirToCenter.  This lets us know how much to scale our vector to get the right radius.
                float dot = Vector2.Dot(right, dirToCenter);

                Vector2 arcCenter = flipped ? line0.p1.point + arcRadius / dot * dirToCenter : line0.p1.point - arcRadius / dot * dirToCenter; 

                // Project arcCenter onto line segments and move vertices back.  Amount is same on both sides.
                float shortenAmount = (arcRadius / dot) * Vector2.Dot(dirToCenter, axis);

                if (!flipped)
                {
                    shortenAmount = -shortenAmount;
                }

                line0.verts[2].pixels += shortenAmount * axis;
                line0.verts[3].pixels += shortenAmount * axis;

                // Now for other line segment.
                axis = line1.p0.point - line1.p1.point;
                axis.Normalize();
                line1.verts[0].pixels -= shortenAmount * axis;
                line1.verts[1].pixels -= shortenAmount * axis;

                // Now that we have moved the line segment vertices to make room for the arc
                // and have calculated the position of the extra vertex we need we can finally
                // generate the vertices and triangles.

                // Start by copying the existing line vertices.
                if (flipped)
                {
                    verts[0] = new Vertex(line0.verts[2]);
                    verts[1] = new Vertex(line0.verts[3]);
                    verts[2] = new Vertex(line1.verts[0]);
                    verts[3] = new Vertex(line1.verts[1]);
                }
                else
                {
                    verts[0] = new Vertex(line1.verts[1]);
                    verts[1] = new Vertex(line1.verts[0]);
                    verts[2] = new Vertex(line0.verts[3]);
                    verts[3] = new Vertex(line0.verts[2]);
                }

                verts[4] = new Vertex(line0.verts[2]);
                verts[4].pixels = intersection;

                // Set the arc-specific info.
                for (int i = 0; i < 5; i++)
                {
                    verts[i].prim = (int)Prim.Arc;
                    verts[i].point0.X = arcCenter.X;
                    verts[i].point0.Y = arcCenter.Y;
                    verts[i].point1.Z = arcRadius;
                }

            }   // end of CalcVertices()

        }   // end of class Arc

        /// <summary>
        /// Smoothed joint where arcRadius less than strokeRadius.
        /// not implemented!
        /// </summary>
        protected class Fill : Stroke
        {
            public PathPoint p;

            public Fill(PathPoint p)
            {
                Debug.Assert(false, "Not implemented!");
                this.p = p;

                verts = new Vertex[4];
            }

            override public void CalcVertices(Line line0, Line line1)
            {
            }   // end of CalcVertices()

        }   // end of class Fill

        /// <summary>
        /// Produces a sharp joint.  Note that this
        /// just affects the 2 lines coming into the
        /// joint.  It doesn't add any new vertices.
        /// </summary>
        protected class Sharp : Stroke
        {
            public PathPoint p;

            public Sharp(PathPoint p)
            {
                this.p = p;
            }

            override public void CalcVertices(Line line0, Line line1)
            {
                // TODO (****) Note that this gives kind of ugly results when
                // the line segments coming into this joint don't have a constant 
                // radius.  The result is kind of a shear across the joint.
                // Possible fix: Calc the max radius needed for the whole path or loop
                // and use that for all the segments regardless of actual radius.
                // Just tried it.  Nope, still getting crappy looking results.  Sadly
                // it looks like I may have to _think_ about this one...

                // For Sharps joints we just need to project the edges of the 
                // bounding box for line0 and line1 to intersect.
                float t = 0;

                // "This" side.
                // Calc t.
                MyMath.LineLineIntersect(line0.verts[2].pixels, line0.verts[0].pixels, line1.verts[0].pixels, line1.verts[2].pixels, out t);
                // Calc intersection point from t.
                Vector2 intersection = line0.verts[0].pixels - line0.verts[2].pixels;
                intersection = line0.verts[2].pixels + t * intersection;
                // Set vertices to match.
                line0.verts[2].pixels = intersection;
                line1.verts[0].pixels = intersection;

                // "That" side.
                // Reflect across center point to get intersection on other side.
                intersection = 2.0f * line0.p1.point - intersection;
                // Set vertices to match.
                line0.verts[3].pixels = intersection;
                line1.verts[1].pixels = intersection;
                
            }   // end of CalcVertices()

        }   // end of class Sharp

        protected class Cap : Stroke
        {
            public PathPoint p0;    // Point where the cap needs to go.
            public PathPoint p1;

            public Cap(PathPoint p0, PathPoint p1)
            {
                this.p0 = p0;
                this.p1 = p1;

                verts = new Vertex[4];
            }

            override public void CalcVertices()
            {
                // Fill in the simple stuff.
                for (int i = 0; i < 4; i++)
                {
                    verts[i].point0 = new Vector3(p0.point, p0.strokeRadius);
                    verts[i].point1 = Vector3.Zero;    // Not used.
                    verts[i].color = p0.color.ToVector4();
                    verts[i].edgeBlend = p0.edgeBlend;
                    verts[i].prim = (int)Prim.Dot;
                }

                // Calc pixel extent including blur.
                float radius = p0.strokeRadius + p0.edgeBlend;

                Vector2 axis = p0.point - p1.point;
                axis.Normalize();
                axis *= radius;
                Vector2 right = new Vector2(axis.Y, -axis.X);

                // At this point we need to calculate the vertex positions.
                verts[0].pixels = p0.point + axis - right;
                verts[1].pixels = p0.point - right;
                verts[2].pixels = p0.point + axis + right;
                verts[3].pixels = p0.point + right; 

            }   // end of CalcVertices()

        }   // end of class Cap

        protected class Dot : Stroke
        {
            public PathPoint p;

            public Dot(PathPoint p)
            {
                this.p = p;

                verts = new Vertex[4];
            }

            override public void CalcVertices()
            {
                // Fill in the simple stuff.
                for (int i = 0; i < 4; i++)
                {
                    verts[i].point0 = new Vector3(p.point, p.strokeRadius);
                    verts[i].point1 = Vector3.Zero;    // Not used.
                    verts[i].color = p.color.ToVector4();
                    verts[i].edgeBlend = p.edgeBlend;
                    verts[i].prim = (int)Prim.Dot;
                }

                // Calc pixel extent.
                float radius = p.strokeRadius;

                // Inflate radius to cover for blurred edge.
                radius += p.edgeBlend;

                // At this point we need to calculate the vertex positions.
                verts[0].pixels = p.point + new Vector2(radius, -radius);
                verts[1].pixels = p.point + new Vector2(radius, radius);
                verts[2].pixels = p.point + new Vector2(-radius, -radius);
                verts[3].pixels = p.point + new Vector2(-radius, radius);

            }   // end of CalcVertices()

        }   // end of class Dot

    }   // end of class Line2D

}   // end of namespace KoiX.Geometry
