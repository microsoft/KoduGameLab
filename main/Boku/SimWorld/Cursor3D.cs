
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

using KoiX;

using Boku.Base;
using Boku.Common;
using Boku.Fx;
using Boku.SimWorld;
using Boku.SimWorld.Chassis;
using Boku.SimWorld.Terra;
using Boku.Common.Xml;

namespace Boku
{
    public class Cursor3D : GameThing
    {
        public class RenderObj : RenderObject, INeedsDeviceReset
        {
            private Cursor3D parent = null;

            private static VertexBuffer[] vbuf = new VertexBuffer[2] { null, null };
            private static IndexBuffer[] ibuf = new IndexBuffer[2] { null, null };
            private static Texture2D texture = null;
            private static Effect effect = null;

            private Vector4 diffuse;            // Local override for diffuse color.
            private static int[] numTriangles = new int[2] { 0, 0 };
            private static int[] numVertices = new int[2] { 0, 0 };

            private static BlendState inverseBlendState = null;

            #region Parameter Caching
            private enum EffectParams
            {
                WorldViewProjMatrix,
                WorldMatrix,
                LocalToModel,
                DiffuseColor,
                SpecularColor,
                EmissiveColor,
                SpecularPower,
            };
            private static EffectCache effectCache = new EffectCache<EffectParams>();
            private static EffectParameter Parameter(EffectParams param)
            {
                return effectCache.Parameter((int)param);
            }
            private static EffectTechnique Technique(InGame.RenderEffect pass, bool textured)
            {
                return effectCache.Technique(pass, textured);
            }
            #endregion Parameter Caching

            public struct Vertex : IVertexType
            {
                private Vector3 position;
                private Vector3 normal;
                private Vector2 tex;

                static VertexDeclaration decl = null;
                static VertexElement[] elements = new VertexElement[]
                {
                    new VertexElement(0, VertexElementFormat.Vector3, VertexElementUsage.Position, 0),
                    new VertexElement(12, VertexElementFormat.Vector3, VertexElementUsage.Normal, 0),
                    new VertexElement(24, VertexElementFormat.Vector2, VertexElementUsage.TextureCoordinate, 0),
                    // size == 32
                };

                public Vertex(Vector3 position, Vector3 normal, Vector2 tex)
                {
                    this.position = position;
                    this.normal = normal;
                    this.tex = tex;
                }   // end of Vertex c'tor

                /// <summary>
                /// This c'tor assumes that the cursor spans [-.5,.5] in
                /// both X and Y and maps the texture coords based on that.
                /// </summary>
                public Vertex(Vector3 position, Vector3 normal)
                {
                    this.position = position;
                    this.normal = normal;
                    this.tex = new Vector2(position.X + 0.5f, position.Y + 0.5f);
                }   // end of Vertex c'tor

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

            }   // end of Vertex
            

            public RenderObj(Cursor3D parent, Vector4 diffuse)
            {
                this.parent = parent;
                this.diffuse = diffuse;

                inverseBlendState = new BlendState();
                inverseBlendState.ColorBlendFunction = BlendFunction.Subtract;
                inverseBlendState.ColorSourceBlend = Blend.One;
                inverseBlendState.ColorDestinationBlend = Blend.One;
                if (BokuSettings.Settings.PreferReach)
                {
                    // In Read, AlphaBlend must be same as ColorBlend
                    inverseBlendState.AlphaDestinationBlend = Blend.One;
                    inverseBlendState.AlphaBlendFunction = BlendFunction.Subtract;
                }
                inverseBlendState.ColorWriteChannels = ColorWriteChannels.Red
                                                        | ColorWriteChannels.Green
                                                        | ColorWriteChannels.Blue;

            }   // end of RenderObj c'tor

            private Vector4 targetColor = Color.White.ToVector4();
            public Vector4 DiffuseColor
            {
                set 
                {
                    if (targetColor != value)
                    {
                        // diffuse = value;
                        targetColor = value;
                        TwitchManager.Set<Vector4> set = delegate(Vector4 val, Object param) { diffuse = val; };
                        TwitchManager.CreateTwitch(DiffuseColor, targetColor, set, 0.2f, TwitchCurve.Shape.EaseInOut);
                    }
                }
                get { return diffuse; }
            }
            public override void Render(Camera camera)
            {
                if (!parent.Hidden)
                {
                    switch (parent.Rep)
                    {
                        case Visual.Edit:
                            Render(camera, parent.Movement.LocalMatrix, false, diffuse);

                            // If there's water, also render the cursor at the surface.
                            float waterHeight = Terrain.GetWaterHeight(parent.Movement.Position);

                            if (waterHeight > 0)
                            {
                                Matrix mat = parent.Movement.LocalMatrix;
                                Vector3 pos = mat.Translation;
                                pos.Z = waterHeight + 0.1f;
                                mat.Translation = pos;
                                Render(camera, mat, false, diffuse);
                            }

                            break;
                        case Visual.Pointy:
                            /// Put the pointy cursor down at the alt-position, which may
                            /// be different from regular position if we're using the mouse.
                            Matrix l2w = parent.movement.LocalMatrix;
                            l2w.Translation = parent.AltPosition;
                            Render(camera, l2w, true, diffuse);
                            /// If the alt position is different, render the RunSim cursor
                            /// at screen center for a rotation reference point.
                            /*
                            if (Vector3.DistanceSquared(parent.movement.Position, parent.AltPosition) > 0.01f)
                            {
                                RenderRunSim(camera, parent.Movement.LocalMatrix, diffuse);
                            }
                            */
                            break;
                        case Visual.RunSim:
                            RenderRunSim(camera, parent.Movement.LocalMatrix, diffuse);
                            break;
                    }
                }
                /// Debug code to draw the cone cursor where the system thinks the 
                /// mouse LOS hits the terrain.
                #if false 
                {
                    Matrix l2w = Matrix.Identity;
                    l2w.Translation = MouseEdit.MouseTouchHitInfo.TerrainPosition;
                    Render(camera, l2w, true, 
                        MouseEdit.MouseTouchHitInfo.TerrainHit 
                            ? diffuse
                            : Vector4.UnitW);
                }
                #endif /// false
            }   // end of RenderObj Render()

            /// <summary>
            /// Render the runtime offline authored cursor model.
            /// </summary>
            /// <param name="camera"></param>
            /// <param name="l2w"></param>
            /// <param name="diffuse"></param>
            private static void RenderRunSim(Camera camera, Matrix l2w, Vector4 diffuse)
            {
                RunSimCursor model = RunSimCursor.GetInstance();

                model.RenderColor = diffuse;
                model.Render(camera, ref l2w, null);
            }

            /// <summary>
            /// Render the edit mode runtime generated cursors.
            /// </summary>
            /// <param name="camera"></param>
            /// <param name="l2w"></param>
            /// <param name="cone"></param>
            /// <param name="diffuse"></param>
            internal static void Render(Camera camera, Matrix l2w, bool cone, Vector4 diffuse)
            {
                GraphicsDevice device = KoiLibrary.GraphicsDevice;

                Matrix viewMatrix = camera.ViewMatrix;
                Matrix projMatrix = camera.ProjectionMatrix;

                Matrix worldViewProjMatrix = l2w * viewMatrix * projMatrix;
                Parameter(EffectParams.WorldViewProjMatrix).SetValue(worldViewProjMatrix);
                Parameter(EffectParams.WorldMatrix).SetValue(l2w);
                Parameter(EffectParams.LocalToModel).SetValue(Matrix.Identity);

                //Parameter(EffectParams.DiffuseTexture).SetValue(texture);

                Parameter(EffectParams.DiffuseColor).SetValue(diffuse);
                Parameter(EffectParams.SpecularColor).SetValue(Color.White.ToVector4());
                Parameter(EffectParams.EmissiveColor).SetValue(Color.Black.ToVector4());
                Parameter(EffectParams.SpecularPower).SetValue(32.0f);

                int cursorIndex = cone ? 1 : 0;

                device.SetVertexBuffer(vbuf[cursorIndex]);
                device.Indices = ibuf[cursorIndex];

                // Render all passes.
                effect.CurrentTechnique = effectCache.Technique(InGame.inGame.renderEffects, false);

                for (int i = 0; i < effect.CurrentTechnique.Passes.Count; i++)
                {
                    EffectPass pass = effect.CurrentTechnique.Passes[i];
                    pass.Apply();

                    if (!cone)
                    {
                        InvertColorMode(device);
                    }

                    device.DrawIndexedPrimitives(PrimitiveType.TriangleList,
                        0, 0,
                        numVertices[cursorIndex],
                        0, numTriangles[cursorIndex]);
                }

                ResetColorMode(device);
            }

            /// <summary>
            /// Turn on a blend mode which is Source - Dest, where Source is
            /// the color of the cursor, and dest is whatever's been rendered before it.
            /// This guarantees that the cursor will stand out against whatever background
            /// is there.
            /// </summary>
            /// <param name="device"></param>
            private static void InvertColorMode(GraphicsDevice device)
            {
                device.BlendState = inverseBlendState;
            }

            /// <summary>
            /// Reset the renderstates that are assumed to be at their defaults by
            /// other effects.
            /// </summary>
            /// <param name="device"></param>
            private static void ResetColorMode(GraphicsDevice device)
            {
                device.BlendState = BlendState.AlphaBlend;
            }
            
            public override void Activate()
            {

            }

            public override void Deactivate()
            {

            }

            public void LoadContent(bool immediate)
            {
                // Init the effect.
                if (effect == null)
                {
                    effect = KoiLibrary.LoadEffect(@"Shaders\Standard");
                    ShaderGlobals.RegisterEffect("Standard", effect);
                    effectCache.Load(effect, "");
                }

                // Load the texture.
                if (texture == null)
                {
                    texture = KoiLibrary.LoadTexture2D(@"Textures\Cursor3D");
                }

                RunSimCursor.GetInstance().LoadContent(immediate);
            }   // end of Cursor3D RenderObj LoadContent()

            public void InitDeviceResources(GraphicsDevice device)
            {
                //
                // Generate the geometry.
                //
                MakeRing(device);
                MakeCone(device);

                RunSimCursor.GetInstance().InitDeviceResources(device);
            }   // end of RenderObj Init()


            public void UnloadContent()
            {
                effectCache.UnLoad();
                DeviceResetX.Release(ref effect);
                DeviceResetX.Release(ref texture);
                DeviceResetX.Release(ref ibuf[0]);
                DeviceResetX.Release(ref vbuf[0]);
                DeviceResetX.Release(ref ibuf[1]);
                DeviceResetX.Release(ref vbuf[1]);

                RunSimCursor.GetInstance().UnloadContent();
            }   // end of Cursor3D RenderObj UnloadContent()

            /// <summary>
            /// Recreate render targets
            /// </summary>
            /// <param name="graphics"></param>
            public void DeviceReset(GraphicsDevice device)
            {
            }

            private void MakeRing(GraphicsDevice device)
            {
                // Ring
                const int numSegments = 32;
                int numTriangles = numSegments * 8;
                int numVertices = numSegments * 8;

                const float innerRadius = 0.25f;
                const float outerRadius = 0.5f;
                const float thickness = 0.05f;

                // Init the vertex buffer.
                if (vbuf[0] == null)
                {
                    vbuf[0] = new VertexBuffer(device, typeof(Vertex), numVertices, BufferUsage.WriteOnly);
                }

                // Create local vertices.
                Vertex[] localVerts = new Vertex[numVertices];

                int index = 0;

                // Upper surface.
                Vector3 normal = new Vector3(0.0f, 0.0f, 1.0f);

                for (int i = 0; i < numSegments; i++)
                {
                    float t = i * (2.0f * (float)Math.PI) / numSegments;
                    float s = (float)Math.Sin(t);
                    float c = (float)Math.Cos(t);

                    localVerts[index++] = new Vertex(new Vector3(c * outerRadius, s * outerRadius, thickness / 2.0f), normal);
                    localVerts[index++] = new Vertex(new Vector3(c * innerRadius, s * innerRadius, thickness / 2.0f), normal);
                }

                // Lower surface.
                normal = new Vector3(0.0f, 0.0f, -1.0f);

                for (int i = 0; i < numSegments; i++)
                {
                    float t = i * (2.0f * (float)Math.PI) / numSegments;
                    float s = (float)Math.Sin(t);
                    float c = (float)Math.Cos(t);

                    localVerts[index++] = new Vertex(new Vector3(c * outerRadius, s * outerRadius, -thickness / 2.0f), normal);
                    localVerts[index++] = new Vertex(new Vector3(c * innerRadius, s * innerRadius, -thickness / 2.0f), normal);
                }

                // Outer edge.
                for (int i = 0; i < numSegments; i++)
                {
                    float t = i * (2.0f * (float)Math.PI) / numSegments;
                    float s = (float)Math.Sin(t);
                    float c = (float)Math.Cos(t);
                    normal = new Vector3(c, s, 0.0f);

                    localVerts[index++] = new Vertex(new Vector3(c * outerRadius, s * outerRadius, thickness / 2.0f), normal);
                    localVerts[index++] = new Vertex(new Vector3(c * outerRadius, s * outerRadius, -thickness / 2.0f), normal);
                }

                // Inner edge.
                for (int i = 0; i < numSegments; i++)
                {
                    float t = i * (2.0f * (float)Math.PI) / numSegments;
                    float s = (float)Math.Sin(t);
                    float c = (float)Math.Cos(t);
                    normal = new Vector3(-c, -s, 0.0f);

                    localVerts[index++] = new Vertex(new Vector3(c * innerRadius, s * innerRadius, thickness / 2.0f), normal);
                    localVerts[index++] = new Vertex(new Vector3(c * innerRadius, s * innerRadius, -thickness / 2.0f), normal);
                }

                // Copy to vertex buffer.
                vbuf[0].SetData<Vertex>(localVerts);


                // Create index buffer.
                if (ibuf[0] == null)
                {
                    ibuf[0] = new IndexBuffer(device, IndexElementSize.SixteenBits, numTriangles * 3, BufferUsage.WriteOnly);
                }

                // Generate the local copy of the data.
                ushort[] localIBuf = new ushort[numTriangles * 3];

                index = 0;
                // Upper surface.
                for (int i = 0; i < numSegments; i++)
                {
                    int baseVertex = 0;
                    localIBuf[index++] = (ushort)(baseVertex + (0 + i * 2) % (numSegments * 2));
                    localIBuf[index++] = (ushort)(baseVertex + (1 + i * 2) % (numSegments * 2));
                    localIBuf[index++] = (ushort)(baseVertex + (2 + i * 2) % (numSegments * 2));
                    localIBuf[index++] = (ushort)(baseVertex + (2 + i * 2) % (numSegments * 2));
                    localIBuf[index++] = (ushort)(baseVertex + (1 + i * 2) % (numSegments * 2));
                    localIBuf[index++] = (ushort)(baseVertex + (3 + i * 2) % (numSegments * 2));
                }
                // Lower surface.
                for (int i = 0; i < numSegments; i++)
                {
                    int baseVertex = numSegments * 2;
                    localIBuf[index++] = (ushort)(baseVertex + (0 + i * 2) % (numSegments * 2));
                    localIBuf[index++] = (ushort)(baseVertex + (2 + i * 2) % (numSegments * 2));
                    localIBuf[index++] = (ushort)(baseVertex + (1 + i * 2) % (numSegments * 2));
                    localIBuf[index++] = (ushort)(baseVertex + (2 + i * 2) % (numSegments * 2));
                    localIBuf[index++] = (ushort)(baseVertex + (3 + i * 2) % (numSegments * 2));
                    localIBuf[index++] = (ushort)(baseVertex + (1 + i * 2) % (numSegments * 2));
                }
                // Outer ring.
                for (int i = 0; i < numSegments; i++)
                {
                    int baseVertex = numSegments * 4;
                    localIBuf[index++] = (ushort)(baseVertex + (0 + i * 2) % (numSegments * 2));
                    localIBuf[index++] = (ushort)(baseVertex + (2 + i * 2) % (numSegments * 2));
                    localIBuf[index++] = (ushort)(baseVertex + (1 + i * 2) % (numSegments * 2));
                    localIBuf[index++] = (ushort)(baseVertex + (2 + i * 2) % (numSegments * 2));
                    localIBuf[index++] = (ushort)(baseVertex + (3 + i * 2) % (numSegments * 2));
                    localIBuf[index++] = (ushort)(baseVertex + (1 + i * 2) % (numSegments * 2));
                }
                // Inner ring.
                for (int i = 0; i < numSegments; i++)
                {
                    int baseVertex = numSegments * 6;
                    localIBuf[index++] = (ushort)(baseVertex + (0 + i * 2) % (numSegments * 2));
                    localIBuf[index++] = (ushort)(baseVertex + (1 + i * 2) % (numSegments * 2));
                    localIBuf[index++] = (ushort)(baseVertex + (2 + i * 2) % (numSegments * 2));
                    localIBuf[index++] = (ushort)(baseVertex + (2 + i * 2) % (numSegments * 2));
                    localIBuf[index++] = (ushort)(baseVertex + (1 + i * 2) % (numSegments * 2));
                    localIBuf[index++] = (ushort)(baseVertex + (3 + i * 2) % (numSegments * 2));
                }

                // Copy it to the index buffer.
                ibuf[0].SetData<ushort>(localIBuf);

                RenderObj.numTriangles[0] = numTriangles;
                RenderObj.numVertices[0] = numVertices;
            }

            private void MakeCone(GraphicsDevice device)
            {
                // Ring
                const int numSegments = 32;
                int numTriangles = numSegments * 2;
                int numVertices = numSegments * 2 + 2;

                const float length = 1.0f;
                const float radius = 0.25f;
                const float depth = -0.2f;

                // Init the vertex buffer.
                if (vbuf[1] == null)
                {
                    vbuf[1] = new VertexBuffer(device, typeof(Vertex), numVertices, BufferUsage.WriteOnly);
                }

                // Create local vertices.
                Vertex[] localVerts = new Vertex[numVertices];

                /// arrangement is:
                /// Ring verts attached to base
                /// Base
                /// Ring verts attached to upper center
                /// Upper center
                for (int i = 0; i < numSegments; i++)
                {
                    float t = i * (2.0f * (float)Math.PI) / numSegments;
                    float s = (float)Math.Sin(t);
                    float c = (float)Math.Cos(t);

                    Vector3 normal = new Vector3(c, s, -radius / (length - depth));
                    normal.Normalize();

                    localVerts[i] = new Vertex(new Vector3(c * radius, s * radius, length), normal);
                    localVerts[i + numSegments + 1] = new Vertex(new Vector3(c * radius, s * radius, length), Vector3.UnitZ);
                }

                localVerts[numSegments] = new Vertex(new Vector3(Vector2.Zero, depth), -Vector3.UnitZ);
                localVerts[numSegments * 2 + 1] = new Vertex(new Vector3(0.0f, 0.0f, length), Vector3.UnitZ);

                // Copy to vertex buffer.
                vbuf[1].SetData<Vertex>(localVerts);


                // Create index buffer.
                if (ibuf[1] == null)
                {
                    ibuf[1] = new IndexBuffer(device, IndexElementSize.SixteenBits, numTriangles * 3, BufferUsage.WriteOnly);
                }

                // Generate the local copy of the data.
                ushort[] localIBuf = new ushort[numTriangles * 3];

                int vtxOff = numSegments + 1;
                int idxOff = numSegments * 3;

                // Upper surface.
                for (int i = 0; i < numSegments - 1; ++i)
                {
                    localIBuf[i * 3 + 0] = (ushort)(numSegments);
                    localIBuf[i * 3 + 1] = (ushort)(i);
                    localIBuf[i * 3 + 2] = (ushort)(i + 1);

                    localIBuf[i * 3 + idxOff + 0] = (ushort)(numSegments * 2 + 1);
                    localIBuf[i * 3 + idxOff + 2] = (ushort)(i + vtxOff + 0);
                    localIBuf[i * 3 + idxOff + 1] = (ushort)(i + vtxOff + 1);
                }

                int last = numSegments - 1;

                localIBuf[last * 3 + 0] = (ushort)(numSegments);
                localIBuf[last * 3 + 1] = (ushort)(last);
                localIBuf[last * 3 + 2] = (ushort)(0);

                localIBuf[last * 3 + idxOff + 0] = (ushort)(numSegments * 2 + 1);
                localIBuf[last * 3 + idxOff + 2] = (ushort)(last + vtxOff + 0);
                localIBuf[last * 3 + idxOff + 1] = (ushort)(0 + vtxOff + 0);

                // Copy it to the index buffer.
                ibuf[1].SetData<ushort>(localIBuf);

                RenderObj.numTriangles[1] = numTriangles;
                RenderObj.numVertices[1] = numVertices;
            }



        }   // end of class RenderObj


        //
        //  Cursor3D
        //

        private RenderObj renderObj = null;
        //private UpdateObj updateObj = null;
        //public Shared shared = null;

        private bool state = false;
        private bool pendingState = false;

        private bool hidden = false;        // If true, prevents the cursor from being rendered.

        public enum Visual
        {
            Edit,
            Pointy,
            RunSim
        };
        /// <summary>
        /// Which visual representation of the cursor to use.
        /// </summary>
        private Visual visual = Visual.RunSim;

        private Vector3 altPosition = Vector3.Zero;

        public override bool Mute { get { return false; } set { } }

        public override bool Invulnerable { get { return true; } set { } }

        public Cursor3D(Vector2 position, 
                        Vector4 color)
            : base("cursor", new CursorChassis())
        {
            Position = new Vector3(position.X, position.Y, 0.0f);

            renderObj = new RenderObj(this, color);
        }   // end of Cursor3D c'tor


        public override RenderObject RenderObject
        {
            get { return renderObj; }
        }

        public static void Render(Camera camera, Matrix l2w, bool cone, Vector4 diffuse)
        {
            RenderObj.Render(camera, l2w, cone, diffuse);
        }
        /// <summary>
        /// Prevents the cursor from being rendered.
        /// </summary>
        public bool Hidden
        {
            get { return hidden; }
            set { hidden = value; }
        }
        /// <summary>
        /// Which visual representation of the cursor to use.
        /// </summary>
        public Visual Rep
        {
            get { return visual; }
            set { visual = value; }
        }
        public Vector3 Position
        {
            get { return movement.Position; }
            set 
            {
                Vector3 pos = value;

                /*
                if (InGame.inGame.SnapToGrid)
                {
                    pos = InGame.inGame.SnapPosition(pos);
                }
                */

                movement.Position = pos;
                movement.Altitude = Chassis.EditHeight + Terrain.GetTerrainAndPathHeight(
                    new Vector3(movement.Position.X,
                                movement.Position.Y,
                                float.MaxValue));
            }
        }
        /// <summary>
        /// The cursor is always center screen, but using the mouse can
        /// affect other parts of the terrain without recentering the view.
        /// This is the position the mouse is indicating.
        /// </summary>
        public Vector3 AltPosition
        {
            get { return altPosition; }
            set
            {
                Vector3 pos = value;

                if (InGame.inGame.SnapToGrid)
                {
                    pos = InGame.SnapPosition(pos);
                }

                altPosition = pos;
                altPosition.Z = Chassis.EditHeight + Terrain.GetTerrainAndPathHeight(
                    new Vector3(altPosition.X,
                                altPosition.Y,
                                float.MaxValue));
            }
        }
        public Vector2 Position2d
        {
            get { return new Vector2(Position.X, Position.Y); }
        }
        public override BoundingSphere BoundingSphere
        {
            get 
            { 
                return new BoundingSphere(movement.Position, 0.5f); 
            }
        }
        public Vector4 DiffuseColor
        {
            set { renderObj.DiffuseColor = value; }
            get { return renderObj.DiffuseColor; }
        }

        public override bool Refresh(List<UpdateObject> updateList, List<RenderObject> renderList)
        {
            bool result = false;

            if (state != pendingState)
            {
                if (pendingState)
                {
                    renderList.Add(renderObj);
                    renderObj.Activate();
                    InGame.inGame.RegisterChassis(this);
                }
                else
                {
                    BokuGame.gameListManager.RemoveObject(this);
                    renderObj.Deactivate();
                    renderList.Remove(renderObj);
                    InGame.inGame.UnRegisterChassis(this);

                    result = true;
                }

                state = pendingState;
            }

            return result;
        }   // end of Cursor3D Refresh()

        override public void Activate()
        {
            if (!pendingState)
            {
                pendingState = true;
                BokuGame.objectListDirty = true;
            }
        }
        override public void Deactivate()
        {
            if (pendingState)
            {
                pendingState = false;
                BokuGame.objectListDirty = true;
            }
        }

        public override bool IsAlive()
        {
            throw new Exception("The method or operation is not implemented.");
        }

        public override void LoadContent(bool immediate)
        {
            BokuGame.Load(renderObj, immediate);
        }   // end of Cursor3D LoadContent()

        public override void InitDeviceResources(GraphicsDevice device)
        {
        }

        public override void UnloadContent()
        {
            BokuGame.Unload(renderObj);
        }   // end of Cursor3D UnloadContent()

        /// <summary>
        /// Recreate render targets
        /// </summary>
        /// <param name="graphics"></param>
        public override void DeviceReset(GraphicsDevice device)
        {
            BokuGame.DeviceReset(renderObj, device);
        }

        #region RunSimCursor
        private class RunSimCursor : FBXModel
        {
            #region Members
            private static RunSimCursor sroInstance = null;
            private static XmlGameActor xmlCursor = null;
            #endregion Members

            #region Public
            /// <summary>
            /// Returns a static, shareable instance of a hover car sro.
            /// </summary>
            public static RunSimCursor GetInstance()
            {
                if (sroInstance == null)
                {
                    sroInstance = new RunSimCursor();
                    sroInstance.XmlActor = XmlCursor;
                }

                return sroInstance;
            }   

            public static XmlGameActor XmlCursor
            {
                get
                {
                    if (xmlCursor == null)
                        xmlCursor = XmlGameActor.Deserialize("RunSimCursor");
                    return xmlCursor;
                }
            }
            #endregion Public

            #region Internal
            private RunSimCursor()
                : base(@"Models\RunSimCursor")
            {
            }   

            #endregion Internal
        }
        #endregion RunSimCursor
    }   // end of class Cursor3D

}   // end of namespace Boku



