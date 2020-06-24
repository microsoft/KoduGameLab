
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
    /// Class for drawing antialiased lines with optional arcs at the joints.
    /// 
    /// We put all the information need to render the line segment into each vertex.  This
    /// means that we don't need any shader parameters which means that we can then easily
    /// batch up a bunch of lines and render them with a single draw call.
    /// </summary>

    public partial class Line2D
    {
        static Effect effect;

        static VertexElement[] elements = new VertexElement[]
        {
            new VertexElement(0, VertexElementFormat.Vector2, VertexElementUsage.Position, 0),              // Position of vertex in pixel coords.
            new VertexElement(8, VertexElementFormat.Vector3, VertexElementUsage.TextureCoordinate, 0),     // line(xy: point0, z: radius0(stroke)) dot(xy: point0, z: radius0(stroke)) arc(xy: center, z:radius(stroke))
            new VertexElement(20, VertexElementFormat.Vector3, VertexElementUsage.TextureCoordinate, 1),    // line(xy: point1, z: radius1(stroke)) dot() arc(z: radius(arc))
            new VertexElement(32, VertexElementFormat.Vector4, VertexElementUsage.Color, 0),                // color
            new VertexElement(48, VertexElementFormat.Single, VertexElementUsage.TextureCoordinate, 2),    // x: 0 = none, 1 = dot, 2 = line segment, 3 = arc
            new VertexElement(52, VertexElementFormat.Single, VertexElementUsage.TextureCoordinate, 3),    // x: edgeBlend
            // Total = 56 bytes
        };

        protected struct Vertex : IVertexType
        {
            public Vector2 pixels;
            public Vector3 point0;
            public Vector3 point1;
            public Vector4 color;
            public float prim;
            public float edgeBlend;

            // c'tor
            public Vertex(Vector2 pixels, Vector3 point0, Vector3 point1, Color color, Prim prim, float edgeBlend)
            {
                this.pixels = pixels;
                this.point0 = point0;
                this.point1 = point1;
                this.color = color.ToVector4();
                this.prim = (int)prim;
                this.edgeBlend = edgeBlend;
            }   // end of Vertex c'tor

            public Vertex(Vertex v)
            {
                this.pixels = v.pixels;
                this.point0 = v.point0;
                this.point1 = v.point1;
                this.color = v.color;
                this.prim = v.prim;
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

        static VertexDeclaration decl;
        static short[] quadIndices = { 0, 1, 3, 0, 3, 2 };
        static Vertex[] localVerts = new Vertex[5];

        #region Members

        bool dirty = true;

        List<PathPoint> points = new List<PathPoint>();
        int startIndex = -1;    // Index of most recent start conmmand.

        List<Stroke> strokes = new List<Stroke>();

        Matrix worldMatrix = Matrix.Identity;

        short[] indices;
        Vertex[] verts;

        int numVerts = 0;
        int numPrims = 0;

        #endregion

        #region Accessors

        /// <summary>
        /// World matrix for line batch.  
        /// Note that scaling from this matrix is not taken into account
        /// when fixing up the size of the blendEdge.  So, for best results
        /// only use this for rotation and translation.
        /// TODO (****) Fix this?  Can we pass in the current camera zoom and adjust in the shader?
        /// </summary>
        public Matrix WorldMatrix
        {
            get { return worldMatrix; }
            set { worldMatrix = value; }
        }

        #endregion

        #region Public

        public Line2D()
        {
        }   // end of c'tor

        public void StartPath(Vector2 point, Color color, float strokeWidth, float arcRadius = 0, bool sharp = false, float edgeBlend = Geometry.DefaultEdgeBlend)
        {
            if (arcRadius == 0)
            {
                sharp = true;
            }

            PathPoint p = new PathPoint(Action.StartPath, point, color, strokeWidth / 2.0f, arcRadius, sharp, edgeBlend);
            startIndex = points.Count;
            points.Add(p);

            dirty = true;
        }   // end of StartPath()

        public void StartLoop(Vector2 point, Color color, float strokeWidth, float arcRadius = 0, bool sharp = false, float edgeBlend = Geometry.DefaultEdgeBlend)
        {
            if (arcRadius == 0)
            {
                sharp = true;
            }

            PathPoint p = new PathPoint(Action.StartLoop, point, color, strokeWidth / 2.0f, arcRadius, sharp, edgeBlend);
            startIndex = points.Count;
            points.Add(p);

            dirty = true;
        }   // end of StartLoop()

        /// <summary>
        /// Add a point to the current path or loop.  Other than point all params are optional.
        /// If left off, the value from the Start*() call will be used.
        /// </summary>
        /// <param name="point"></param>
        /// <param name="color"></param>
        /// <param name="strokeWidth"></param>
        /// <param name="arcRadius">Instead of coming to a sharp "kink" at this point, apply a radius</param>
        public void AddPoint(Vector2 point, Color? color = null, float? strokeWidth = null, float? arcRadius = null, bool? sharp = null, float? edgeBlend = null)
        {
            Debug.Assert(startIndex != -1, "Need to start a path or loop before adding points.");

            // Fill in default values from Start if needed.
            if (!color.HasValue)
            {
                color = points[startIndex].color;
            }
            if (!strokeWidth.HasValue)
            {
                strokeWidth = 2.0f * points[startIndex].strokeRadius;
            }
            if (!arcRadius.HasValue)
            {
                arcRadius = points[startIndex].arcRadius;
            }
            if (!sharp.HasValue)
            {
                sharp = points[startIndex].sharp;
            }
            if (!edgeBlend.HasValue)
            {
                edgeBlend = points[startIndex].edgeBlend;
            }

            // For curved paths or loops we only support using a fixed strokeWidth.
            Debug.Assert(sharp.Value || strokeWidth.Value == 2.0f * points[startIndex].strokeRadius, "Currently we don't support changing the stroke width with radius corners.");

            Debug.Assert(sharp.Value || arcRadius.Value >= strokeWidth.Value / 2.0f, "For non-sharp joints, arcRadius must be at least as big as the radius of the line.  (strokeWidth / 2)");

            PathPoint p = new PathPoint(Action.AddPoint, point, color.Value, strokeWidth.Value / 2.0f, arcRadius.Value, sharp.Value, edgeBlend.Value);
            points.Add(p);

            dirty = true;
        }   // end of AddPoint()

        /// <summary>
        /// Ends the current loop or path.
        /// </summary>
        public void End()
        {
            Debug.Assert(points.Count > 1, "Zero length paths or loops not supported.");

            // Convert current path or loop into strokes.

            if (points[0].action == Action.StartPath)
            {
                // Handle Path case.

                // Initial cap.
                strokes.Add(new Cap(points[0], points[1]));
                numVerts += 4;
                numPrims += 2;

                // Lines with Joints.
                for (int i = 0; i < points.Count - 1; i++)
                {
                    strokes.Add(new Line2D.Line(points[i], points[i+1]));
                    numVerts += 4;
                    numPrims += 2;

                    // Do we have enough points for a joint?
                    if (i < points.Count - 2)
                    {
                        if (points[i].sharp)
                        {
                            // Sharp.
                            strokes.Add(new Sharp(points[i]));
                        }
                        else
                        {
                            // Which rounded option?
                            if (points[i].arcRadius >= points[i].strokeRadius)
                            {
                                strokes.Add(new Arc(points[i]));
                                numVerts += 5;
                                numPrims += 3;
                            }
                            else
                            {
                                strokes.Add(new Fill(points[i]));
                                numVerts += 4;
                                numPrims += 2;
                            }
                        }
                    }   // if enough points.
                }

                // We've now got an extra joint at the end.  Get rid of it.
                if (strokes[strokes.Count - 2] is Line)
                {
                    strokes.RemoveAt(strokes.Count - 1);
                    numVerts -= 4;
                    numPrims -= 2;
                }

                // Final cap.
                strokes.Add(new Cap(points[points.Count - 1], points[points.Count - 2]));
                numVerts += 4;
                numPrims += 2;
            }
            else if (points[0].action == Action.StartLoop)
            {
                // A loop is a collection of an equal number of lines and joints.
                for (int i = 0; i < points.Count; i++)
                {
                    // Line.
                    strokes.Add(new Line2D.Line(points[i], points[(i+1) % points.Count]));
                    numVerts += 4;
                    numPrims += 2;

                    // Joint.
                    if (points[i].sharp)
                    {
                        // Sharp.
                        strokes.Add(new Sharp(points[i]));
                    }
                    else
                    {
                        // Which rounded option?
                        if (points[i].arcRadius >= points[i].strokeRadius)
                        {
                            strokes.Add(new Arc(points[i]));
                            numVerts += 5;
                            numPrims += 3;
                        }
                        else
                        {
                            strokes.Add(new Fill(points[i]));
                            numVerts += 4;
                            numPrims += 2;
                        }
                    }
                }
            }
            else
            {
                Debug.Assert(false, "Not sure how we got here.");
            }

            // Get ready for next set.
            points.Clear();
        }   // end of End()

        public void AddDot(Vector2 point, Color color, float radius, float edgeBlend = Geometry.DefaultEdgeBlend)
        {
            PathPoint p = new PathPoint(Action.DrawDot, point, color, radius, 0, false, edgeBlend);
            strokes.Add(new Dot(p));

            numVerts += 4;
            numPrims += 2;

            dirty = true;
        }   // end of AddDot()

        public void Render(SpriteCamera camera)
        {
            if (numPrims == 0)
            {
                return;
            }

            if (dirty)
            {
                Stroke.CalcVertices(strokes);

                // Ensure arrays are allocated and correctly sized.
                if (verts == null || verts.Length != numVerts)
                {
                    verts = new Vertex[numVerts];
                }
                if (indices == null || indices.Length != numPrims * 3)
                {
                    indices = new short[numPrims * 3];
                }

                // Copy vertices from strokes into vertex array.
                // Note that Sharps don't have vertices.
                int v = 0;
                int i = 0;
                foreach (Stroke stroke in strokes)
                {
                    if (stroke.verts != null)
                    {
                        CopyVerts(ref v, ref i, stroke.verts);
                    }
                }

                dirty = false;
            }

            GraphicsDevice device = KoiLibrary.GraphicsDevice;

            effect.CurrentTechnique = effect.Techniques["Line2D"];

            Matrix worldViewProjMatrix = worldMatrix * camera.ViewProjMatrix;
            effect.Parameters["WorldViewProjMatrix"].SetValue(worldViewProjMatrix);
            effect.Parameters["Zoom"].SetValue(camera.Zoom);

            // Set renderstate we care about.  Pre-mult alpha blending and no Z.
            device.DepthStencilState = DepthStencilState.None;
            device.BlendState = BlendState.AlphaBlend;
            device.RasterizerState = RasterizerState.CullCounterClockwise;
            //device.RasterizerState = RasterizerState.CullNone;

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

        void CopyVerts(ref int vertIndex, ref int indexIndex, Vertex[] srcVerts)
        {
            int count = srcVerts.Length;
            int baseIndex = vertIndex;

            for (int i = 0; i < count; i++)
            {
                verts[vertIndex++] = srcVerts[i];
            }

            if (count == 4)
            {
                indices[indexIndex++] = (short)(baseIndex + 0);
                indices[indexIndex++] = (short)(baseIndex + 1);
                indices[indexIndex++] = (short)(baseIndex + 3);
                indices[indexIndex++] = (short)(baseIndex + 0);
                indices[indexIndex++] = (short)(baseIndex + 3);
                indices[indexIndex++] = (short)(baseIndex + 2);
            }
            else if (count == 5)
            {
                indices[indexIndex++] = (short)(baseIndex + 0);
                indices[indexIndex++] = (short)(baseIndex + 1);
                indices[indexIndex++] = (short)(baseIndex + 4);
                indices[indexIndex++] = (short)(baseIndex + 4);
                indices[indexIndex++] = (short)(baseIndex + 2);
                indices[indexIndex++] = (short)(baseIndex + 0);
                indices[indexIndex++] = (short)(baseIndex + 4);
                indices[indexIndex++] = (short)(baseIndex + 3);
                indices[indexIndex++] = (short)(baseIndex + 2);
            }
            else
            {
                Debug.Assert(false);
            }
        }   // end CopyVerts()

        public static void LoadContent()
        {
            if (DeviceResetX.NeedsLoad(effect))
            {
                effect = KoiLibrary.LoadEffect(@"KoiXContent\Shaders\Geometry\Line2D");
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

    }   // end of class Line2D

}   // end of namespace KoiX.Geometry
