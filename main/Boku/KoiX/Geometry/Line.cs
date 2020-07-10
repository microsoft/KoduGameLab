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
    /// <summary>
    /// Class for drawing antialiased lines.
    /// 
    /// We put all the information need to render the line segment into each vertex.  This
    /// means that we don't need any shader parameters which means that we can then easily
    /// batch up a bunch of lines and render them with a single draw call.
    /// 
    /// TODO (****) Changing the edgeBlend in the middle of a path doesn't work.  Fix it or just don't worry about it?
    /// TODO (****) EdgeBlend doesn't work right with camera zoom.  Should we adjust for this in the shader?
    /// </summary>
    public class Line
    {
        static Effect effect;

        static VertexElement[] elements = new VertexElement[]
        {
            new VertexElement(0, VertexElementFormat.Vector2, VertexElementUsage.Position, 0),              // Position of vertex in pixel coords.
            new VertexElement(8, VertexElementFormat.Vector4, VertexElementUsage.TextureCoordinate, 0),     // point0
            new VertexElement(24, VertexElementFormat.Vector4, VertexElementUsage.TextureCoordinate, 1),    // point1
            new VertexElement(40, VertexElementFormat.Vector4, VertexElementUsage.Color, 0),                // color
            new VertexElement(56, VertexElementFormat.Single, VertexElementUsage.TextureCoordinate, 2),     // edgeBlend
            // Total = 60 bytes
        };

        struct Vertex : IVertexType
        {
            public Vector2 pixels;  // Vertex position in pixel coords.
            public Vector4 point0;  // X, Y = position in pixels, Z = radius, W = capped (1 if true, 0 if false)
            public Vector4 point1;
            public Vector4 color;
            public float edgeBlend;

            // c'tor
            public Vertex(Vector2 pixels, Vector4 point0, Vector4 point1, Color color, float edgeBlend)
            {
                this.pixels = pixels;
                this.point0 = point0;
                this.point1 = point1;
                this.color = color.ToVector4();
                this.edgeBlend = edgeBlend;
            }   // end of Vertex c'tor

            public Vertex(Vertex v)
            {
                this.pixels = v.pixels;
                this.point0 = v.point0;
                this.point1 = v.point1;
                this.color = v.color;
                this.edgeBlend = v.edgeBlend;
            }

            public VertexDeclaration VertexDeclaration
            {
                get
                {
                    if (decl == null || decl.IsDisposed)
                    {
                        DeviceResetX.Release(ref decl);
                        decl = new VertexDeclaration(elements);
                    }
                    return decl;
                }
            }
        }   // end of Vertex

        /// <summary>
        /// For batched rendering, the EndPoints store the raw data.
        /// </summary>
        class EndPoint
        {
            public Vector2 point;
            public Color color;
            public float radius;
            public float edgeBlend;

            public EndPoint(Vector2 point, Color color, float radius, float edgeBlend)
            {
                this.point = point;
                this.color = color;
                this.radius = radius;
                this.edgeBlend = edgeBlend;
            }
        }

        enum Action
        {
            MoveTo,
            DrawTo,
            CloseLoop,

            DrawDot,
        }

        class Command
        {
            public Action action;
            // Endpoints action is applied to.
            //  MoveTo : only ep0 is valid.
            //  DrawTo : ep0 is where we start, ep1 is the dest index.
            //  CloseLoop : ep0 is where we start, ep1 is the most recent DrawTo index.
            //  DrawDot : only ep0 is valid.
            public int ep0;
            public int ep1;

            public Command(Action action, int ep0, int ep1)
            {
                this.action = action;
                this.ep0 = ep0;
                this.ep1 = ep1;
            }
        }

        static VertexDeclaration decl;
        static short[] quadIndices = { 0, 1, 3, 0, 3, 2 };
        static Vertex[] localVerts = new Vertex[4];

        #region Members

        bool dirty = true;
        short[] indices;
        Vertex[] verts;

        int numVerts = 0;
        int numPrims = 0;

        List<EndPoint> points = new List<EndPoint>();
        List<Command> commands = new List<Command>();

        Matrix worldMatrix = Matrix.Identity;

        #endregion

        #region Accessors

        /// <summary>
        /// World matrix for line batch.  
        /// Note that scaling from this matrix is not taken into account
        /// when fixing up the size of the blendEdge.  So, for best results
        /// only use this for rotation and translation.
        /// </summary>
        public Matrix WorldMatrix
        {
            get { return worldMatrix; }
            set { worldMatrix = value; }
        }

        public int NumPrims
        {
            get { return numPrims; }
        }

        #endregion

        #region Public

        /// <summary>
        /// An immediate, static call which doesn't batch up the line.  Useful if you only need 
        /// a couple of line segments but definitely not efficient for anything more than that.
        /// 
        /// Capped at both ends.
        /// </summary>
        /// <param name="point0"></param>
        /// <param name="point1"></param>
        /// <param name="color0"></param>
        /// <param name="color1"></param>
        /// <param name="width0">width of line in pixels at this end</param>
        /// <param name="width1"></param>
        /// <param name="edgeBlend"></param>
        public static void DrawLine(SpriteCamera camera, Vector2 point0, Vector2 point1, Color color0, Color color1, float width0, float width1, float edgeBlend = Geometry.DefaultEdgeBlend)
        {
            Debug.Assert(camera != null);
            Debug.Assert(effect != null);

            SetVertices(camera, point0, point1, color0, color1, width0 * 0.5f, width1 * 0.5f, capped0: true, capped1: true, edgeBlend: edgeBlend);

            GraphicsDevice device = KoiLibrary.GraphicsDevice;

            effect.CurrentTechnique = effect.Techniques["LineSegment"];

            effect.Parameters["WorldViewProjMatrix"].SetValue(camera.ViewProjMatrix);

            // Render all passes.
            for (int i = 0; i < effect.CurrentTechnique.Passes.Count; i++)
            {
                EffectPass pass = effect.CurrentTechnique.Passes[i];
                pass.Apply();
                device.DrawUserIndexedPrimitives<Vertex>(PrimitiveType.TriangleList, localVerts, 0, 4, quadIndices, 0, 2);
            }

        }

        public static void DrawDot(SpriteCamera camera, Vector2 point, Color color, float diameter, float edgeBlend = Geometry.DefaultEdgeBlend)
        {
            DrawLine(camera, point, point, color, color, diameter, diameter, edgeBlend);
        }

        /// <summary>
        /// c'tor for creating a batched set of lines and dots.
        /// </summary>
        public Line()
        {
        }   // end of c'tor

        public void MoveTo(Vector2 point, Color color, float width, float edgeBlend = Geometry.DefaultEdgeBlend)
        {
            EndPoint p = new EndPoint(point, color, width * 0.5f, edgeBlend);
            points.Add(p);
            commands.Add(new Command(Action.MoveTo, points.Count - 1, -1));

            dirty = true;
        }

        public void DrawTo(Vector2 point, Color color, float width, float edgeBlend = Geometry.DefaultEdgeBlend)
        {
            EndPoint p = new EndPoint(point, color, width * 0.5f, edgeBlend);
            points.Add(p);
            commands.Add(new Command(Action.DrawTo, points.Count - 2, points.Count - 1));

            numVerts += 4;
            numPrims += 2;

            dirty = true;
        }

        /// <summary>
        /// Draws a dot at the given point.
        /// 
        /// TODO (****) Should we instead require a MoveTo(), DrawDot() sequence.  This
        /// would allow more easily drawing dots at each intersection.  (do I care?)
        /// </summary>
        /// <param name="point"></param>
        /// <param name="color"></param>
        /// <param name="diameter"></param>
        /// <param name="edgeBlend"></param>
        public void DrawDot(Vector2 point, Color color, float diameter, float edgeBlend = Geometry.DefaultEdgeBlend)
        {
            EndPoint p = new EndPoint(point, color, diameter * 0.5f, edgeBlend);
            points.Add(p);
            commands.Add(new Command(Action.DrawDot, points.Count - 1, -1));

            numVerts += 4;
            numPrims += 2;

            dirty = true;
        }

        /// <summary>
        /// Used for closed-loop figures.  Same as doing a DrawTo() back to the most recent MoveTo() vertex
        /// except the overlap at the join is rendered correctly.
        /// 
        /// For instance, to create a batch to render a square:
        /// Line line = new Line();
        /// line.MoveTo(new Vector2( 0,  0), ...
        /// line.DrawTo(new Vector2(10,  0), ...
        /// line.DrawTo(new Vector2(10, 10), ...
        /// line.DrawTo(new Vector2( 0, 10), ...
        /// line.CloseLoop();
        /// 
        /// Then to actually render it:
        /// line.Render(camera);
        /// </summary>
        public void CloseLoop()
        {
            int ep0 = points.Count - 1;
            int ep1 = -1;
            for (int i = commands.Count - 1; i >= 0; i--)
            {
                if (commands[i].action == Action.MoveTo)
                {
                    ep1 = commands[i].ep0;
                }
            }

            Debug.Assert(ep1 != -1, "No valid MoveTo found to close this loop.");

            commands.Add(new Command(Action.CloseLoop, ep0, ep1));

            numVerts += 4;
            numPrims += 2;

            dirty = true;
        }

        /// <summary>
        /// Renders the batch up line segments and dots.
        /// </summary>
        /// <param name="camera"></param>
        public void Render(SpriteCamera camera)
        {
            if (dirty)
            {
                CalcVertices(camera);
                dirty = false;
            }

            if (numVerts == 0)
            {
                // Nothing to draw.
                return;
            }

            GraphicsDevice device = KoiLibrary.GraphicsDevice;

            effect.CurrentTechnique = effect.Techniques["LineSegment"];

            Matrix worldViewProjMatrix = worldMatrix * camera.ViewProjMatrix;
            effect.Parameters["WorldViewProjMatrix"].SetValue(worldViewProjMatrix);

            // Render all passes.
            for (int i = 0; i < effect.CurrentTechnique.Passes.Count; i++)
            {
                EffectPass pass = effect.CurrentTechnique.Passes[i];
                pass.Apply();
                device.DrawUserIndexedPrimitives<Vertex>(PrimitiveType.TriangleList, verts, 0, numVerts, indices, 0, numPrims);
            }

        }   // end of Render()

        #endregion

        #region Internal

        /// <summary>
        /// Sets vertices for static calls.
        /// </summary>
        /// <param name="localVerts"></param>
        /// <param name="camera"></param>
        /// <param name="point0"></param>
        /// <param name="point1"></param>
        /// <param name="color0"></param>
        /// <param name="color1"></param>
        /// <param name="radius0"></param>
        /// <param name="radius1"></param>
        /// <param name="capped0"></param>
        /// <param name="capped1"></param>
        /// <param name="edgeBlend"></param>
        static void SetVertices(SpriteCamera camera, Vector2 point0, Vector2 point1, Color color0, Color color1, float radius0, float radius1, bool capped0, bool capped1, float edgeBlend)
        {
            float zoom = camera != null ? camera.Zoom : 1.0f;
            // Adjust edgeBlend to handle zoom.
            edgeBlend /= zoom;

            // Fill in the simple stuff.
            for (int i = 0; i < 4; i++)
            {
                localVerts[i].point0 = new Vector4(point0.X, point0.Y, radius0, 1);
                localVerts[i].point1 = new Vector4(point1.X, point1.Y, radius1, 1);

                localVerts[i].edgeBlend = edgeBlend;
            }
            localVerts[0].color = localVerts[1].color = color0.ToVector4();
            localVerts[2].color = localVerts[3].color = color1.ToVector4();

            // Calc bounding quad in pixels.
            Vector2 axis = point1 - point0;
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

            // Inflate radii to cover for blurred edge.
            radius0 += edgeBlend;
            radius1 += edgeBlend;

            Vector2 right = new Vector2(normalizedAxis.Y, -normalizedAxis.X);   // Normalized vector at right angle to axis.

            // At this point we need to calculate the vertex positions.  The case when the line has differing radii
            // is a bit more complex but since it's rearely used we can just fake it by using the larger radius.  This
            // then also covers the case when the radii are equal.
            float radius = Math.Max(radius0, radius1);
            localVerts[0].pixels = point0 - radius * normalizedAxis - right * radius;
            localVerts[1].pixels = point0 - radius * normalizedAxis + right * radius;
            localVerts[2].pixels = point1 + radius * normalizedAxis - right * radius;
            localVerts[3].pixels = point1 + radius * normalizedAxis + right * radius;

        }   // end of SetVertices()

        /// <summary>
        /// Calculates vertices for batched calls.
        /// Accumulate all the info in lists and then transfer them to arrays.
        /// </summary>
        void CalcVertices(SpriteCamera camera)
        {
            // Ensure arrays are allocated and correctly sized.
            if (verts == null || verts.Length != numVerts)
            {
                verts = new Vertex[numVerts];
            }
            if (indices == null || indices.Length != numPrims * 3)
            {
                indices = new short[numPrims * 3];
            }

            // Indices for inserting data in the arrays.
            int vertIndex = 0;
            int indexIndex = 0;
            EndPoint moveToEndPoint = null; // EndPoint at the time MoveTo was called.
            int moveToVertIndex = -1;       // Index of vertex added in first segment.
            foreach (Command cmd in commands)
            {
                switch (cmd.action)
                {
                    case Action.MoveTo:
                        {
                            moveToEndPoint = points[cmd.ep0];
                            moveToVertIndex = vertIndex;
                        }
                        break;

                    case Action.DrawTo:
                    case Action.CloseLoop:
                        {
                            EndPoint e0 = points[cmd.ep0];
                            EndPoint e1 = points[cmd.ep1];

                            if (cmd.action == Action.CloseLoop)
                            {
                                e1 = moveToEndPoint;
                            }

                            // Use SetVertices to calc the results and
                            // then just plug them into the arrays.
                            SetVertices(
                                camera,
                                e0.point,
                                e1.point,
                                e0.color,
                                e1.color,
                                e0.radius,
                                e1.radius,
                                true, true,
                                e0.edgeBlend);

                            int baseIndex = vertIndex;
                            verts[vertIndex++] = localVerts[0];
                            verts[vertIndex++] = localVerts[1];
                            verts[vertIndex++] = localVerts[2];
                            verts[vertIndex++] = localVerts[3];

                            indices[indexIndex++] = (short)(baseIndex + 0);
                            indices[indexIndex++] = (short)(baseIndex + 1);
                            indices[indexIndex++] = (short)(baseIndex + 3);
                            indices[indexIndex++] = (short)(baseIndex + 0);
                            indices[indexIndex++] = (short)(baseIndex + 3);
                            indices[indexIndex++] = (short)(baseIndex + 2);

                            // If this move didn't start at the MoveTo point fix 
                            // up joint between this segment and the previous one.
                            if (e0 != moveToEndPoint)
                            {
                                FixJoint(vertIndex - 8, vertIndex - 4);
                            }
                            // If CloseLoop then also fix up joint between this segment and first one.
                            if (cmd.action == Action.CloseLoop)
                            {
                                FixJoint(vertIndex - 4, moveToVertIndex);
                            }
                        }
                        break;

                    case Action.DrawDot:
                        {
                            // Use SetVertices to calc the results and
                            // then just plug them into the arrays.
                            SetVertices(
                                camera,
                                points[cmd.ep0].point,
                                points[cmd.ep0].point,
                                points[cmd.ep0].color,
                                points[cmd.ep0].color,
                                points[cmd.ep0].radius,
                                points[cmd.ep0].radius,
                                true, true,
                                points[cmd.ep0].edgeBlend);

                            int baseIndex = vertIndex;
                            verts[vertIndex++] = localVerts[0];
                            verts[vertIndex++] = localVerts[1];
                            verts[vertIndex++] = localVerts[2];
                            verts[vertIndex++] = localVerts[3];

                            indices[indexIndex++] = (short)(baseIndex + 0);
                            indices[indexIndex++] = (short)(baseIndex + 1);
                            indices[indexIndex++] = (short)(baseIndex + 3);
                            indices[indexIndex++] = (short)(baseIndex + 0);
                            indices[indexIndex++] = (short)(baseIndex + 3);
                            indices[indexIndex++] = (short)(baseIndex + 2);
                        }
                        break;
                }
            }

        }   // end of CalcVertices()

        /// <summary>
        /// Takes an overlapped joint and adjusts the vertices to make a non-overlapping connection.
        /// 
        /// v0 is the first vertex index from the first segment.
        /// v4 is the first vertex index from the connected segment.
        /// We pass in the indices rather than the vertices since Vertex is a struct and would get passed by value.
        /// 
        /// After the fix, v0.pixels should be the same as v2.pixels.
        /// Same goes for v1.pixels and v3.pixels.
        /// </summary>
        /// <param name="v0"></param>
        /// <param name="v4"></param>
        void FixJoint(int v0, int v4)
        {
            // Vertex indices.
            // Segment0 - v0, v1, v2, v3
            // Segment1 - v4, v5, v6, v7
            // v2 should be at the same position as v4
            // v3 should be at the same position as v5
            int v1 = v0 + 1;
            int v2 = v0 + 2;
            int v3 = v0 + 3;
            int v5 = v4 + 1;
            int v6 = v4 + 2;
            int v7 = v4 + 3;

            // Calc the splitting line between the segments.
            // For now, assuming that the edges of the quads are parallel with the axis of the line segment.
            // This is always true for constant thickness lines.  This is true for varying thickness lines
            // iff we create a rectangular bounding quad rather than tighter fit (which we currently do).
            // Actually, no it's not.  On the thick end, extending along the line axis rather than along the 
            // quad edge can cause clipping.  So don't do this.

            // Axis vector are calculated pointing inward from the joint.
            Vector2 axis01 = new Vector2(verts[v2].point1.X, verts[v2].point1.Y) - new Vector2(verts[v2].point0.X, verts[v2].point0.Y);
            Vector2 axis23 = new Vector2(verts[v4].point0.X, verts[v4].point0.Y) - new Vector2(verts[v4].point1.X, verts[v4].point1.Y);

            axis01.Normalize();
            axis23.Normalize();

            // Calculate the vector defining the split between the segments.
            Vector2 split = axis01 + axis23;
            // Adjust for colinear case.
            if (split.LengthSquared() == 0)
            {
                split = new Vector2(axis01.Y, -axis01.X);
            }

            float t = 0;

            // Figure out which direction has the biggest radius.
            float s0Radius = Math.Max(verts[v0].point0.Z, verts[v0].point1.Z);
            float s1Radius = Math.Max(verts[v4].point0.Z, verts[v4].point1.Z);

            if (s0Radius >= s1Radius)
            {
                // Calc first new point.
                Vector2 edge = verts[v0].pixels - verts[v2].pixels;
                MyMath.LineLineIntersect(verts[v2].pixels, verts[v2].pixels + edge, new Vector2(verts[v2].point1.X, verts[v2].point1.Y), new Vector2(verts[v2].point1.X, verts[v2].point1.Y) + split, out t);
                Vector2 newPoint = verts[v2].pixels + t * edge;

                verts[v2].pixels = newPoint;
                verts[v4].pixels = newPoint;

                // Calc other new point.
                edge = verts[v1].pixels - verts[v3].pixels;
                MyMath.LineLineIntersect(verts[v3].pixels, verts[v3].pixels + edge, new Vector2(verts[v3].point1.X, verts[v3].point1.Y), new Vector2(verts[v3].point1.X, verts[v3].point1.Y) + split, out t);
                newPoint = verts[v3].pixels + t * edge;
                verts[v3].pixels = newPoint;
                verts[v5].pixels = newPoint;
            }
            else
            {
                // Calc first new point.
                Vector2 edge = verts[v6].pixels - verts[v4].pixels;
                MyMath.LineLineIntersect(verts[v4].pixels, verts[v4].pixels + edge, new Vector2(verts[v4].point0.X, verts[v4].point0.Y), new Vector2(verts[v4].point0.X, verts[v4].point0.Y) + split, out t);
                Vector2 newPoint = verts[v4].pixels + t * edge;

                verts[v2].pixels = newPoint;
                verts[v4].pixels = newPoint;

                // Calc other new point.
                edge = verts[v7].pixels - verts[v5].pixels;
                MyMath.LineLineIntersect(verts[v5].pixels, verts[v5].pixels + edge, new Vector2(verts[v5].point0.X, verts[v5].point0.Y), new Vector2(verts[v5].point0.X, verts[v5].point0.Y) + split, out t);
                newPoint = verts[v5].pixels + t * edge;

                verts[v3].pixels = newPoint;
                verts[v5].pixels = newPoint;
            }

            // Flop color so we can see split for debug.
            /*
            verts[v2].color.X = 1.0f - verts[v2].color.X;
            verts[v3].color.X = 1.0f - verts[v3].color.X;
            */
        }   // end of FixJoint()

        public static void LoadContent()
        {
            if (DeviceResetX.NeedsLoad(effect))
            {
                effect = KoiLibrary.LoadEffect(@"KoiXContent\Shaders\Geometry\Line");
            }
        }

        public static void UnloadContent()
        {
            DeviceResetX.Release(ref effect);
        }

        public static void DeviceResetHandler(object sender, EventArgs e)
        {
            throw new NotImplementedException();
        }

        #endregion

    }   // end of class Line

}   // end of namespace KoiX.Geometry
