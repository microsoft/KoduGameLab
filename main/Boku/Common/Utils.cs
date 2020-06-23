
using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Reflection;
using System.IO;
using System.Xml.Serialization;
using System.Runtime.Serialization;

using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Audio;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using Microsoft.Xna.Framework.Storage;

using Boku.Common.ParticleSystem;
using Boku.Fx;

namespace Boku.Common
{
    /// <summary>
    /// A static collection of basic graphics helper functions primarily
    /// usefull for debugging.
    /// </summary>
    public class Utils
    {
        private static Effect effect = null;
        private static Texture2D ramps = null;
        public static Texture2D white = null;

        private struct UtilsVertex : IVertexType
        {
            public Vector3 position;
            public Vector4 color;

            static VertexDeclaration decl;
            static VertexElement[] elements = new VertexElement[]
            {
                new VertexElement(0, VertexElementFormat.Vector3, VertexElementUsage.Position, 0),
                new VertexElement(12, VertexElementFormat.Vector4, VertexElementUsage.Color, 0),
                // Total == 28 bytes
            };

            public UtilsVertex(Vector3 position, Vector4 color)
            {
                this.position = position;
                this.color = color;
            }   // end of UtilsVertex c'tor

            public VertexDeclaration VertexDeclaration
            {
                get 
                {
                    if (decl == null || decl.IsDisposed)
                    {
                        decl = new VertexDeclaration(elements);
                    }
                    return decl; 
                }
            }
        }   // end of UtilsVertex

        
        public static void Init(GraphicsDevice device)
        {
            // Init the effect.
            effect = BokuGame.Load<Effect>(BokuGame.Settings.MediaPath + @"Shaders\Utils");
            ShaderGlobals.RegisterEffect("Utils", effect);

            ramps = BokuGame.Load<Texture2D>(BokuGame.Settings.MediaPath + @"Textures\Ramps");
            white = BokuGame.Load<Texture2D>(BokuGame.Settings.MediaPath + @"Textures\White");

        }   // end of Utils Init()

        public static void DrawLine(Camera camera, Vector3 p0, Vector3 p1, Color color)
        {
            DrawLine(camera, p0, p1, color.ToVector4());
        }

        public static void DrawLine(Camera camera, Vector3 p0, Vector3 p1, Vector4 color)
        {
            Matrix worldMatrix = Matrix.Identity;
            DrawLine(camera, p0, p1, color, ref worldMatrix);
        }   // end of Utils DrawLine()

        public static void DrawLine(Camera camera, Vector3 p0, Vector3 p1, Color color, ref Matrix worldMatrix)
        {
            DrawLine(camera, p0, p1, color.ToVector4(), ref worldMatrix);
        }

        public static void DrawLine(Camera camera, Vector3 p0, Vector3 p1, Vector4 color, ref Matrix worldMatrix)
        {
            Matrix viewMatrix = camera.ViewMatrix;
            Matrix projMatrix = camera.ProjectionMatrix;

            Matrix worldViewProjMatrix = worldMatrix * viewMatrix * projMatrix;
            effect.Parameters["WorldViewProjMatrix"].SetValue(worldViewProjMatrix);
            effect.Parameters["WorldMatrix"].SetValue(worldMatrix);

            // Create the vertices.
            UtilsVertex[] verts = new UtilsVertex[2];
            verts[0] = new UtilsVertex(p0, color);
            verts[1] = new UtilsVertex(p1, color);

            effect.CurrentTechnique = effect.Techniques["NoTexture"];

            GraphicsDevice device = BokuGame.bokuGame.GraphicsDevice;

            // Render all passes.
            for (int i = 0; i < effect.CurrentTechnique.Passes.Count; i++)
            {
                EffectPass pass = effect.CurrentTechnique.Passes[i];
                pass.Apply();
                device.DrawUserPrimitives(PrimitiveType.LineList, verts, 0, 1);
            }

        }   // end of Utils DrawLine()

        /// <summary>
        /// Draw a single line connecting the input points.
        /// </summary>
        /// <param name="camera"></param>
        /// <param name="pts"></param>
        /// <param name="color"></param>
        /// <param name="localToWorld"></param>
        public static void DrawLine(Camera camera, List<Vector3> pts, Vector4 color)
        {
            Matrix ident = Matrix.Identity;
            DrawLine(camera, pts, color, ref ident);
        }
        /// <summary>
        /// Draw a single line connecting the input points.
        /// </summary>
        /// <param name="camera"></param>
        /// <param name="pts"></param>
        /// <param name="color"></param>
        /// <param name="localToWorld"></param>
        public static void DrawLine(Camera camera, List<Vector3> pts, Vector4 color, ref Matrix localToWorld)
        {
            if (pts.Count > 0)
            {
                Matrix viewMatrix = camera.ViewMatrix;
                Matrix projMatrix = camera.ProjectionMatrix;

                Matrix worldViewProjMatrix = localToWorld * viewMatrix * projMatrix;
                effect.Parameters["WorldViewProjMatrix"].SetValue(worldViewProjMatrix);
                effect.Parameters["WorldMatrix"].SetValue(localToWorld);

                // Create the vertices.
                if (vertsScratch.Length < pts.Count)
                {
                    vertsScratch = new UtilsVertex[pts.Count];
                }

                for (int i = 0; i < pts.Count; ++i)
                {
                    ///UtilsVertex is a struct, so no real new here.
                    vertsScratch[i] = new UtilsVertex(pts[i], color);
                }

                effect.CurrentTechnique = effect.Techniques["NoTexture"];

                GraphicsDevice device = BokuGame.bokuGame.GraphicsDevice;

                // Render all passes.
                for (int i = 0; i < effect.CurrentTechnique.Passes.Count; i++)
                {
                    EffectPass pass = effect.CurrentTechnique.Passes[i];
                    pass.Apply();
                    device.DrawUserPrimitives(PrimitiveType.LineStrip, vertsScratch, 0, pts.Count - 1);
                }
            }
        }
        /// <summary>
        /// Draw a series of line segments. That means pts must have even number positions.
        /// </summary>
        /// <param name="camera"></param>
        /// <param name="pts"></param>
        /// <param name="color"></param>
        /// <param name="localToWorld"></param>
        public static void DrawLines(Camera camera, List<Vector3> pts, Vector4 color)
        {
            Matrix ident = Matrix.Identity;
            DrawLines(camera, pts, color, ref ident);
        }
        /// <summary>
        /// Draw a series of line segments. That means pts must have even number positions.
        /// </summary>
        /// <param name="camera"></param>
        /// <param name="pts"></param>
        /// <param name="color"></param>
        /// <param name="localToWorld"></param>
        public static void DrawLines(Camera camera, List<Vector3> pts, Vector4 color, ref Matrix localToWorld)
        {
            if (pts.Count > 0)
            {
                Matrix viewMatrix = camera.ViewMatrix;
                Matrix projMatrix = camera.ProjectionMatrix;

                Matrix worldViewProjMatrix = localToWorld * viewMatrix * projMatrix;
                effect.Parameters["WorldViewProjMatrix"].SetValue(worldViewProjMatrix);
                effect.Parameters["WorldMatrix"].SetValue(localToWorld);

                // Create the vertices.
                if (vertsScratch.Length < pts.Count)
                {
                    vertsScratch = new UtilsVertex[pts.Count];
                }

                for (int i = 0; i < pts.Count; ++i)
                {
                    ///UtilsVertex is a struct, so no real new here.
                    vertsScratch[i] = new UtilsVertex(pts[i], color);
                }

                effect.CurrentTechnique = effect.Techniques["NoTexture"];

                GraphicsDevice device = BokuGame.bokuGame.GraphicsDevice;

                // Render all passes.
                for (int i = 0; i < effect.CurrentTechnique.Passes.Count; i++)
                {
                    EffectPass pass = effect.CurrentTechnique.Passes[i];
                    pass.Apply();
                    device.DrawUserPrimitives(PrimitiveType.LineList, vertsScratch, 0, pts.Count / 2);
                }
            }
        }
        private static UtilsVertex[] vertsScratch = new UtilsVertex[10];

        public static void DrawLines(Camera camera, List<Vector3> pts, List<Vector4> col)
        {
            Debug.Assert((pts.Count & 1) == 0, "Even number of points for line segments");
            Debug.Assert(col.Count * 2 == pts.Count, "Mismatch between color and segment counts");
            int numSegs = col.Count;

            if (vertsScratch.Length < pts.Count)
            {
                vertsScratch = new UtilsVertex[pts.Count];
            }

            for (int i = 0; i < col.Count; ++i)
            {
                vertsScratch[i * 2].position = pts[i * 2];
                vertsScratch[i * 2].color = col[i];

                vertsScratch[i * 2 + 1].position = pts[i * 2 + 1];
                vertsScratch[i * 2 + 1].color = col[i];
            }

            Matrix viewMatrix = camera.ViewMatrix;
            Matrix projMatrix = camera.ProjectionMatrix;

            Matrix worldViewProjMatrix = viewMatrix * projMatrix;
            effect.Parameters["WorldViewProjMatrix"].SetValue(worldViewProjMatrix);
            effect.Parameters["WorldMatrix"].SetValue(Matrix.Identity);

            effect.CurrentTechnique = effect.Techniques["VtxColor"];

            GraphicsDevice device = BokuGame.bokuGame.GraphicsDevice;

            // Render all passes.
            for (int i = 0; i < effect.CurrentTechnique.Passes.Count; i++)
            {
                EffectPass pass = effect.CurrentTechnique.Passes[i];
                pass.Apply();
                device.DrawUserPrimitives(PrimitiveType.LineList, vertsScratch, 0, numSegs);
            }

        }

        public static void DrawFatLine(Camera camera, List<Vector3> pts, Vector4 color)
        {
            Matrix ident = Matrix.Identity;
            DrawFatLine(camera, pts, color, ref ident);
        }
        public static void DrawFatLine(Camera camera, List<Vector3> pts, Vector4 color, ref Matrix localToWorld)
        {
            if (pts.Count > 0)
            {
                Matrix viewMatrix = camera.ViewMatrix;
                Matrix projMatrix = camera.ProjectionMatrix;

                Matrix worldViewProjMatrix = localToWorld * viewMatrix * projMatrix;
                effect.Parameters["WorldViewProjMatrix"].SetValue(worldViewProjMatrix);
                effect.Parameters["WorldMatrix"].SetValue(localToWorld);

                int numSegs = pts.Count / 2;
                int numTris = numSegs * 2;

                SetupRunwayPos(pts, color);

                effect.CurrentTechnique = effect.Techniques["RunwayAlphaBack"];

                effect.Parameters["RunCount"].SetValue(5.0f);
                effect.Parameters["RunPhase"].SetValue(0.0f);
                effect.Parameters["RunEndColor"].SetValue(Vector4.UnitW);
                effect.Parameters["Ramps"].SetValue(white);

                Vector2 runSkinny = new Vector2(
                    1.0f / camera.Resolution.X,
                    1.0f / camera.Resolution.Y) * 1.0f;
                effect.Parameters["RunWidth"].SetValue(runSkinny);

                ShaderGlobals.FixExplicitBloom(0.0f);

                DrawPrimRunwayVerts(numTris);

                effect.CurrentTechnique = effect.Techniques["RunwayAlpha"];

                effect.Parameters["RunCount"].SetValue(1.666f);
                effect.Parameters["RunPhase"].SetValue(0.0f);
                effect.Parameters["RunEndColor"].SetValue(color);
                effect.Parameters["Ramps"].SetValue(white);

                Vector2 runWidth = new Vector2(
                    1.0f / camera.Resolution.X,
                    1.0f / camera.Resolution.Y) * 4.0f;
                effect.Parameters["RunWidth"].SetValue(runWidth);

                DrawPrimRunwayVerts(numTris);

                ShaderGlobals.ReleaseExplicitBloom();
            }
        }
        /// <summary>
        /// Draw stripped lines connecting the input dots.
        /// </summary>
        /// <param name="camera"></param>
        /// <param name="pts"></param>
        /// <param name="col"></param>
        /// <param name="colorOff"></param>
        /// <param name="phase"></param>
        public static void DrawRunway(Camera camera, List<Vector3> pts, List<Vector4> col, Vector4 colorOff, float phase, float bloom)
        {
            Debug.Assert((pts.Count & 1) == 0, "Even number of points for line segments");
            Debug.Assert(col.Count * 2 == pts.Count, "Mismatch between color and segment counts");
            int numSegs = col.Count;
            int numTris = numSegs * 2;
            int numVerts = numTris * 3;

            SetupRunwayVerts(pts, col);

            Matrix viewMatrix = camera.ViewMatrix;
            Matrix projMatrix = camera.ProjectionMatrix;

            Matrix worldViewProjMatrix = viewMatrix * projMatrix;
            effect.Parameters["WorldViewProjMatrix"].SetValue(worldViewProjMatrix);
            effect.Parameters["WorldMatrix"].SetValue(Matrix.Identity);

            effect.Parameters["RunCount"].SetValue(0.666f);
            effect.Parameters["RunPhase"].SetValue(phase);
            effect.Parameters["RunEndColor"].SetValue(colorOff);
            effect.Parameters["Ramps"].SetValue(ramps);

            DrawPrimRunwayVerts(numTris);

            effect.CurrentTechnique = effect.Techniques["RunwayAlpha"];

            ShaderGlobals.FixExplicitBloom(bloom);

            Vector2 runWidth = new Vector2(
                1.0f / camera.Resolution.X,
                1.0f / camera.Resolution.Y) * 4.0f;
            effect.Parameters["RunWidth"].SetValue(runWidth);

            DrawPrimRunwayVerts(numTris);

            ShaderGlobals.ReleaseExplicitBloom();
        }
        private static void DrawPrimRunwayVerts(int numTris)
        {
            GraphicsDevice device = BokuGame.bokuGame.GraphicsDevice;

            // Render all passes.
            for (int i = 0; i < effect.CurrentTechnique.Passes.Count; i++)
            {
                EffectPass pass = effect.CurrentTechnique.Passes[i];
                pass.Apply();
                device.DrawUserPrimitives<RunwayVertex>(PrimitiveType.TriangleList, runwayScratch, 0, numTris);
            }
        }
        private static void SetupRunwayPos(List<Vector3> pts, Vector4 col)
        {
            int numSegs = pts.Count / 2;
            int numTris = numSegs * 2;
            int numVerts = numTris * 3;

            if (runwayScratch.Length < numVerts)
            {
                runwayScratch = new RunwayVertex[numVerts];
            }

            for (int i = 0; i < numSegs; ++i)
            {
                int vertBase = i * 6;
                runwayScratch[vertBase + 0].position = pts[i * 2];
                runwayScratch[vertBase + 0].other = new Vector4(pts[i * 2 + 1], -1.0f);

                Vector4 onColor = new Vector4(col.X, col.Y, col.Z, 0.0f);
                runwayScratch[vertBase + 0].toEnd = new Color(onColor).PackedValue;

                runwayScratch[vertBase + 1] = runwayScratch[vertBase + 0];
                runwayScratch[vertBase + 1].other.W = -runwayScratch[vertBase + 1].other.W;


                runwayScratch[vertBase + 2].position = pts[i * 2 + 1];
                runwayScratch[vertBase + 2].other = new Vector4(pts[i * 2], 1.0f);

                Vector4 offColor = new Vector4(col.X, col.Y, col.Z, 1.0f);
                runwayScratch[vertBase + 2].toEnd = new Color(offColor).PackedValue;

                runwayScratch[vertBase + 3] = runwayScratch[vertBase + 2];
                runwayScratch[vertBase + 4] = runwayScratch[vertBase + 1];

                runwayScratch[vertBase + 5] = runwayScratch[vertBase + 2];
                runwayScratch[vertBase + 5].other.W = -runwayScratch[vertBase + 5].other.W;
            }

        }
        private static void SetupRunwayVerts(List<Vector3> pts, List<Vector4> col)
        {
            int numSegs = col.Count;
            int numTris = numSegs * 2;
            int numVerts = numTris * 3;

            if (runwayScratch.Length < numVerts)
            {
                runwayScratch = new RunwayVertex[numVerts];
            }

            for (int i = 0; i < numSegs; ++i)
            {
                int vertBase = i * 6;
                runwayScratch[vertBase + 0].position = pts[i * 2];
                runwayScratch[vertBase + 0].other = new Vector4(pts[i * 2 + 1], -1.0f);

                Vector4 onColor = new Vector4(col[i].X, col[i].Y, col[i].Z, 0.0f);
                runwayScratch[vertBase + 0].toEnd = new Color(onColor).PackedValue;

                runwayScratch[vertBase + 1] = runwayScratch[vertBase + 0];
                runwayScratch[vertBase + 1].other.W = -runwayScratch[vertBase + 1].other.W;


                runwayScratch[vertBase + 2].position = pts[i * 2 + 1];
                runwayScratch[vertBase + 2].other = new Vector4(pts[i * 2], 1.0f);

                Vector4 offColor = new Vector4(col[i].X, col[i].Y, col[i].Z, 1.0f);
                runwayScratch[vertBase + 2].toEnd = new Color(offColor).PackedValue;

                runwayScratch[vertBase + 3] = runwayScratch[vertBase + 2];
                runwayScratch[vertBase + 4] = runwayScratch[vertBase + 1];

                runwayScratch[vertBase + 5] = runwayScratch[vertBase + 2];
                runwayScratch[vertBase + 5].other.W = -runwayScratch[vertBase + 5].other.W;
            }

        }

        private struct RunwayVertex : IVertexType
        {
            public Vector3 position;
            public Vector4 other;
            public UInt32 toEnd;

            static VertexDeclaration decl = null;
            static VertexElement[] elements = new VertexElement[]
            { 
                new VertexElement(0, VertexElementFormat.Vector3, VertexElementUsage.Position, 0),
                new VertexElement(12, VertexElementFormat.Vector4, VertexElementUsage.TextureCoordinate, 0),
                new VertexElement(28, VertexElementFormat.Color, VertexElementUsage.Color, 0),
            // Total = 40 bytes
            };

            public VertexDeclaration VertexDeclaration
            {
                get
                {
                    if (decl == null || decl.IsDisposed)
                    {
                        decl = new VertexDeclaration(elements);
                    }
                    return decl;
                }
            }

        };
        private static RunwayVertex[] runwayScratch = new RunwayVertex[10];

        public static void DrawAxis(Camera camera, Vector3 position)
        {
            DrawLine(camera, position, position + new Vector3(1.0f, 0.0f, 0.0f), Color.Red);
            DrawLine(camera, position, position + new Vector3(0.0f, 1.0f, 0.0f), Color.Green);
            DrawLine(camera, position, position + new Vector3(0.0f, 0.0f, 1.0f), Color.Blue);
        }   // end of Utils.DrawAxis()

        public static void DrawAxes(Camera camera, Matrix transform)
        {
            Vector3 position = transform.Translation;
            Vector3 right = Vector3.Normalize(transform.Right);
            Vector3 up = Vector3.Normalize(transform.Up);
            Vector3 dir = Vector3.Normalize(transform.Backward);

            DrawLine(camera, position, position + right, Color.Red);
            DrawLine(camera, position, position + up, Color.Green);
            DrawLine(camera, position, position + dir, Color.Blue);
        }

        public static void DrawSphere(Camera camera, Vector3 center, float radius)
        {
            Matrix world = Matrix.Identity;
            DrawSphere(camera, center, radius, ref world);
        }   // end of Utils.DrawSphere()

        public static void DrawSphere(Camera camera, Vector3 center, float radius, ref Matrix worldMatrix)
        {
            const int numSegments = 32;
            float dt = MathHelper.TwoPi / numSegments;
            Vector3 p0;
            Vector3 p1;
            float t;

            // X==0 plane
            t = 0.0f;
            for (int i = 0; i < numSegments; i++)
            {
                p0 = center + new Vector3(0.0f, radius * (float)Math.Cos(t), radius * (float)Math.Sin(t));
                p1 = center + new Vector3(0.0f, radius * (float)Math.Cos(t + dt), radius * (float)Math.Sin(t + dt));
                DrawLine(camera, p0, p1, Color.Red, ref worldMatrix);
                t += dt;
            }

            // Y==0 plane
            t = 0.0f;
            for (int i = 0; i < numSegments; i++)
            {
                p0 = center + new Vector3(radius * (float)Math.Cos(t), 0.0f, radius * (float)Math.Sin(t));
                p1 = center + new Vector3(radius * (float)Math.Cos(t + dt), 0.0f, radius * (float)Math.Sin(t + dt));
                DrawLine(camera, p0, p1, Color.Green, ref worldMatrix);
                t += dt;
            }

            // Z==0 plane
            t = 0.0f;
            for (int i = 0; i < numSegments; i++)
            {
                p0 = center + new Vector3(radius * (float)Math.Cos(t), radius * (float)Math.Sin(t), 0.0f);
                p1 = center + new Vector3(radius * (float)Math.Cos(t + dt), radius * (float)Math.Sin(t + dt), 0.0f);
                DrawLine(camera, p0, p1, Color.Blue, ref worldMatrix);
                t += dt;
            }

        }   // end of Utils.DrawSphere()

        public static void DrawSolidSphere(Camera camera, Vector3 position, float radius, Vector4 color)
        {
            DrawSolidEllipsoid(camera, position, new Vector3(radius), color);
        }   // end of DrawSolidSphere()

        public static void DrawSolidEllipsoid(Camera camera, Vector3 position, Vector3 radii, Vector4 color)
        {
            Sphere sphere = Sphere.GetInstance();

            // Get the effect we need.  Borrow it from the particle system manager.
            ParticleSystemManager manager = InGame.inGame.ParticleSystemManager;

            // Set up common rendering values.
            Effect effect = manager.Effect3d;
            effect.CurrentTechnique = color.W > 0.99f
                ? manager.Technique(ParticleSystemManager.EffectTech3d.OpaqueColorPass)
                : manager.Technique(ParticleSystemManager.EffectTech3d.TransparentColorPass);

            // Set parameters.
            manager.Parameter(ParticleSystemManager.EffectParams3d.Radius).SetValue(1.0f);
            manager.Parameter(ParticleSystemManager.EffectParams3d.DiffuseColor).SetValue(color);
            manager.Parameter(ParticleSystemManager.EffectParams3d.EmissiveColor).SetValue(Vector4.Zero);
            manager.Parameter(ParticleSystemManager.EffectParams3d.SpecularColor).SetValue(new Vector4(0.9f));
            manager.Parameter(ParticleSystemManager.EffectParams3d.SpecularPower).SetValue(16.0f);
            manager.Parameter(ParticleSystemManager.EffectParams3d.Alpha).SetValue(color.W);

            manager.Parameter(ParticleSystemManager.EffectParams3d.Shininess).SetValue(0.4f);

            // Set up world matrix.
            Matrix worldMatrix = Matrix.CreateScale(radii);

            // Set radius and translation.
            worldMatrix.Translation = position;
            Matrix worldViewProjMatrix = worldMatrix * camera.ViewProjectionMatrix;

            // Render
            sphere.Render(camera, ref worldMatrix, effect);

        }   // end of Node Render()

        public static void DrawSolidCone(Camera camera, Vector3 pos, Vector3 dir, float scale, Vector4 color)
        {
            Matrix localToWorld = Matrix.Identity;
            localToWorld.Up = Vector3.UnitY;
            localToWorld.Backward = -Vector3.Normalize(dir) * scale;
            localToWorld.Right = Vector3.Normalize(Vector3.Cross(Vector3.UnitY, localToWorld.Backward)) * scale;
            localToWorld.Up = Vector3.Normalize(Vector3.Cross(localToWorld.Backward, localToWorld.Right)) * scale;
            localToWorld.Translation = pos;

            Cursor3D.Render(camera, localToWorld, true, color);
        }

        //
        //
        //  2D Stuff
        //
        //


        /// <summary>
        /// Draw a line in screenspace using pixel coords.
        /// </summary>
        /// <param name="p0"></param>
        /// <param name="p1"></param>
        /// <param name="color"></param>
        public static void Draw2DLine(Vector2 p0, Vector2 p1, Vector4 color)
        {
            // Add a half pixel offset to get things to line up cleanly.
            p0 += new Vector2(0.5f, 0.5f);
            p1 += new Vector2(0.5f, 0.5f);

            // Convert from pixel coords to homogeneous coords.
            p0.X = p0.X / BokuGame.bokuGame.GraphicsDevice.Viewport.Width * 2.0f - 1.0f;
            p0.Y = -(p0.Y / BokuGame.bokuGame.GraphicsDevice.Viewport.Height * 2.0f - 1.0f);
            p1.X = p1.X / BokuGame.bokuGame.GraphicsDevice.Viewport.Width * 2.0f - 1.0f;
            p1.Y = -(p1.Y / BokuGame.bokuGame.GraphicsDevice.Viewport.Height * 2.0f - 1.0f);

            // Create the vertices.
            UtilsVertex[] verts = new UtilsVertex[2];
            verts[0] = new UtilsVertex(new Vector3(p0, 0.0f), color);
            verts[1] = new UtilsVertex(new Vector3(p1, 0.0f), color);

            effect.CurrentTechnique = effect.Techniques["Screenspace2D"];

            GraphicsDevice device = BokuGame.bokuGame.GraphicsDevice;

            // Render all passes.
            for (int i = 0; i < effect.CurrentTechnique.Passes.Count; i++)
            {
                EffectPass pass = effect.CurrentTechnique.Passes[i];
                pass.Apply();
                try
                {
                    device.DrawUserPrimitives(PrimitiveType.LineList, verts, 0, 1);
                }
                catch
                {
                }
            }

        }   // end of Draw2DLine()

        /// <summary>
        /// Draw a debug cross hair
        /// </summary>
        /// <param name="screenPos">position to draw at</param>
        /// <param name="color">color of crosshairs</param>
        public static void Draw2DCrossHairs(Vector2 screenPos, Vector4 color)
        {
            Vector2 Point0 = new Vector2();
            Vector2 Point1 = new Vector2();
            float lineSize = 30.0f;

            Point0 = screenPos;
            Point0.X -= lineSize;

            Point1 = screenPos;
            Point1.X += lineSize;

            Draw2DLine(Point0, Point1, color);

            Point0 = screenPos;
            Point0.Y -= lineSize;

            Point1 = screenPos;
            Point1.Y += lineSize;

            Draw2DLine(Point0, Point1, color);
        }

        /// <summary>
        /// This Xbox .Net CF does not implement Enum.Parse, so we have this drop-in workaround.
        /// </summary>
        /// <param name="enumType"></param>
        /// <param name="str"></param>
        /// <returns></returns>
        public static Object EnumParse(Type enumType, string str)
        {
            return EnumParse(enumType, str, true);
        }

        public static Object EnumParse(Type enumType, string str, bool ignoreCase)
        {
#if !NETFX_CORE
            Debug.Assert(enumType.IsEnum);
#endif
            return Enum.Parse(enumType, str, ignoreCase);
        }

        /// <summary>
        /// Gets the storage folder based on the Genre flags.  Note that
        /// there are only three possible folders, MyWorlds, BuiltInWorlds
        /// and Downloads.  So this is expected to be used with the flag
        /// set from a real file.  Asking for Genres.All will not get you
        /// all.  Asking for Genres.Action will get you MyWorlds.
        /// </summary>
        /// <param name="flags"></param>
        /// <returns></returns>
        public static string FolderNameFromFlags(BokuShared.Genres flags)
        {
            if (0 != (flags & BokuShared.Genres.MyWorlds))
                return BokuGame.MyWorldsPath;
            if (0 != (flags & BokuShared.Genres.BuiltInWorlds))
                return BokuGame.BuiltInWorldsPath;
            if (0 != (flags & BokuShared.Genres.Downloads))
                return BokuGame.DownloadsPath;
            return null;
        }

        public static bool Vector2FromString(string s, out Vector2 v, Vector2 defaultValue)
        {
            v = defaultValue;

            try
            {
                string[] work = s.Split('{', '}', ',');

                int field = 0;
                for (int i = 0; i < work.Length; ++i)
                {
                    if (String.IsNullOrEmpty(work[i]))
                        continue;

                    if (field == 0)
                    {
                        v.X = float.Parse(work[i], System.Globalization.CultureInfo.InvariantCulture);
                        field += 1;
                        continue;
                    }

                    if (field == 1)
                    {
                        v.Y = float.Parse(work[i], System.Globalization.CultureInfo.InvariantCulture);
                        break;
                    }
                }

                return true;
            }
            catch
            {
                return false;
            }
        }

    }   // end of class Utils

#if !NETFX_CORE
    [SerializableAttribute]
#endif
    public class XmlSerializableDictionary<TKey, TValue> : Dictionary<TKey, TValue>, IXmlSerializable
    {
        #region Constructors

        public XmlSerializableDictionary()
            : base()
        {
        }

        #endregion


        #region Constants

        private const string ITEM = "item";

        private const string KEY = "key";
        
        private const string VALUE = "value";

        #endregion
 

        #region IXmlSerializable Members

        public System.Xml.Schema.XmlSchema GetSchema()
        {
            return null;
        }
 

        public void ReadXml(System.Xml.XmlReader reader)
        {
            XmlSerializer keySerializer = new XmlSerializer(typeof(TKey));

            XmlSerializer valueSerializer = new XmlSerializer(typeof(TValue));

            bool wasEmpty = reader.IsEmptyElement;

            reader.Read();

            if (wasEmpty)
                return;

            while (reader.NodeType != System.Xml.XmlNodeType.EndElement)
            {
                reader.ReadStartElement(ITEM);

                reader.ReadStartElement(KEY);

                TKey key = (TKey)keySerializer.Deserialize(reader);

                reader.ReadEndElement();

                reader.ReadStartElement(VALUE);

                TValue value = (TValue)valueSerializer.Deserialize(reader);

                reader.ReadEndElement();

                this.Add(key, value);

                reader.ReadEndElement();

                reader.MoveToContent();
            }

            reader.ReadEndElement();
        }

 
        public void WriteXml(System.Xml.XmlWriter writer)
        {
            XmlSerializer keySerializer = new XmlSerializer(typeof(TKey));

            XmlSerializer valueSerializer = new XmlSerializer(typeof(TValue));

            foreach (TKey key in this.Keys)
            {
                writer.WriteStartElement(ITEM);

                writer.WriteStartElement(KEY);

                keySerializer.Serialize(writer, key);

                writer.WriteEndElement();

                writer.WriteStartElement(VALUE);

                TValue value = this[key];

                valueSerializer.Serialize(writer, value);

                writer.WriteEndElement();

                writer.WriteEndElement();
            }
        }

        #endregion
    }



}   // end of namespace Boku.Common
