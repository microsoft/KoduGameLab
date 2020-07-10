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
using Boku.Fx;
using Boku.Programming;

namespace Boku.Common
{
    /// <summary>
    /// A generic textured quad.
    /// </summary>
    public class Billboard : RenderObject, ITransform, IBounding, INeedsDeviceReset
    {
        private static Effect effect = null;
        private static VertexBuffer vbuf = null;
        private static EffectTechnique technique = null;

        private Texture2D texture = null;
        private string textureFilename = null;

        protected Matrix worldMatrix = Matrix.Identity;
        protected Transform localTransform = new Transform();

        private Vector2 size;        // Width and height.
        private Object parent;

        private bool premultipliedAlpha = false;    // Not sure why we bother.  This never changes.

        #region EFFECT_CACHE
        protected enum EffectParams
        {
            WorldViewProjMatrix,
            WorldMatrix,
            DiffuseTexture,
        };
        private static EffectCache effectCache = new EffectCache<EffectParams>();
        protected static EffectParameter Parameter(EffectParams param)
        {
            return effectCache.Parameter((int)param);
        }
        #endregion EFFECT_CACHE

        BoundingBox IBounding.BoundingBox
        {
            get
            {
                // BUGBUG this box is not transformed!
                BoundingBox box = new BoundingBox(new Vector3(Size.X, Size.Y, -0.0001f),
                    new Vector3(Size.X, Size.Y, 0.0001f));
                return box;
            }
        }
        BoundingSphere IBounding.BoundingSphere
        {
            get
            {
                // transform the sphere
                Vector3 center = Vector3.Transform(Vector3.Zero, localTransform.Matrix);
                Vector3 radius = Vector3.TransformNormal(new Vector3(size.Length() * 0.5f, 0.0f, 0.0f), localTransform.Matrix);

                BoundingSphere sphere = new BoundingSphere(center, radius.Length());

                return sphere;
            }
        }

        Transform ITransform.Local
        {
            get
            {
                return localTransform;
            }
            set
            {
                localTransform = value;
            }
        }
        Matrix ITransform.World
        {
            get
            {
                return worldMatrix;
            }
        }
        bool ITransform.Compose()
        {
            bool changed = this.localTransform.Compose();
            if (changed)
            {
                RecalcMatrix();
            }
            return changed;
        }
        void ITransform.Recalc(ref Matrix parentMatrix)
        {
            worldMatrix = this.localTransform.Matrix * parentMatrix;
        }
        ITransform ITransform.Parent
        {
            get
            {
                return this.parent as ITransform;
            }
            set
            {
                this.parent = value;
            }
        }
        protected void RecalcMatrix()
        {
            ITransform transformParent = this.parent as ITransform;
            Matrix parentMatrix = Matrix.Identity;
            if (transformParent != null)
            {
                parentMatrix = transformParent.World;
            }
            ITransform transformThis = this as ITransform;

            transformThis.Recalc(ref parentMatrix);
        }

        #region Accessors
        public Texture2D Texture
        {
            get
            {
                return this.texture;
            }
            set
            {
                this.texture = value;
            }
        }
        public Object Parent
        {
            get
            {
                return this.parent;
            }
            set
            {
                this.parent = value;
                RecalcMatrix();
            }
        }
        public Vector2 Size
        {
            get { return size; }
            set { size = value; InitDeviceResources(BokuGame.bokuGame.GraphicsDevice); }
        }
        public float Width
        {
            get { return size.X; }
            set { size.X = value; InitDeviceResources(BokuGame.bokuGame.GraphicsDevice); }
        }
        public float Height
        {
            get { return size.Y; }
            set { size.Y = value; InitDeviceResources(BokuGame.bokuGame.GraphicsDevice); }
        }
        public bool TextureIsFromCardSpace
        {
            get { return !textureFilename.Contains(@"\"); }
        }

        public string TextureFilename
        {
            get { return textureFilename; }
        }

        #endregion


        public struct Vertex : IVertexType
        {
            private Vector3 position;
            private Vector2 texCoord;

            // Declare the vertex structure we'll use for the Billboard.
            static private VertexDeclaration decl = null;
            static private VertexElement[] elements = new VertexElement[]
            {
                new VertexElement(0, VertexElementFormat.Vector3, VertexElementUsage.Position, 0),
                new VertexElement(12, VertexElementFormat.Vector2, VertexElementUsage.TextureCoordinate, 0),
                // size == 20
            };

            public Vertex(Vector3 pos, Vector2 tex)
            {
                position = pos;
                texCoord = tex;
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

        // c'tors
        public Billboard( Object parent, string textureFilename, Vector2 size)
        {
            this.parent = parent;
            this.textureFilename = textureFilename;
            this.size = size;

            InitDeviceResources(BokuGame.bokuGame.GraphicsDevice);
            
        }   // end of Billboard c'tor

        public override void Render(Camera camera)
        {
            GraphicsDevice device = BokuGame.bokuGame.GraphicsDevice;

            // Is the texture bad?  If so, try and reload it.
            // Can happen due to device reset, machine going to sleep, etc.
            RenderTarget2D tex = texture as RenderTarget2D;
            if (texture == null || texture.IsDisposed || texture.GraphicsDevice.IsDisposed || (tex != null && tex.IsContentLost))
            {
                BokuGame.Release(ref texture);

                InitDeviceResources(device);
            }

            Matrix viewMatrix = camera.ViewMatrix;
            Matrix projMatrix = camera.ProjectionMatrix;

            ITransform transformThis = this as ITransform;

            Matrix worldViewProjMatrix = transformThis.World * viewMatrix * projMatrix;
            Parameter(EffectParams.WorldViewProjMatrix).SetValue(worldViewProjMatrix);
            Parameter(EffectParams.WorldMatrix).SetValue(transformThis.World);

            device.SetVertexBuffer(vbuf);
            device.Indices = UI2D.Shared.QuadIndexBuff;

            // Render all passes.
            Parameter(EffectParams.DiffuseTexture).SetValue(texture);
            effect.CurrentTechnique = technique;

            for (int i = 0; i < effect.CurrentTechnique.Passes.Count; i++)
            {
                EffectPass pass = effect.CurrentTechnique.Passes[i];
                pass.Apply();
                device.DrawIndexedPrimitives(PrimitiveType.TriangleList, 0, 0, 4, 0, 2);
            }

        }   // end of Billboard Render()


        override public void Activate()
        {
        }
        
        override public void Deactivate()
        {
        }
        
        public void LoadContent(bool immediate)
        {
        }

        /// <summary>
        /// This Init function also doubles as an update function
        /// in case the position or size of the billboard changes.
        /// </summary>
        public void InitDeviceResources(GraphicsDevice device)
        {
            // Init the vertex buffer.
            if (vbuf == null)
            {
                vbuf = new VertexBuffer(device, typeof(Vertex), 4, BufferUsage.WriteOnly);

                // Create local vertices.
                Vertex[] localVerts = new Vertex[4];
                {
                    localVerts[0] = new Vertex(new Vector3(-size.X / 2.0f, size.Y / 2.0f, 0.0f), new Vector2(0, 0));
                    localVerts[1] = new Vertex(new Vector3(size.X / 2.0f, size.Y / 2.0f, 0.0f), new Vector2(1, 0));
                    localVerts[2] = new Vertex(new Vector3(size.X / 2.0f, -size.Y / 2.0f, 0.0f), new Vector2(1, 1));
                    localVerts[3] = new Vertex(new Vector3(-size.X / 2.0f, -size.Y / 2.0f, 0.0f), new Vector2(0, 1));
                }

                // Copy to vertex buffer.
                vbuf.SetData<Vertex>(localVerts);
            }

            // Init the effect.
            if (effect == null)
            {
                effect = BokuGame.Load<Effect>(BokuGame.Settings.MediaPath + @"Shaders\Billboard");
                ShaderGlobals.RegisterEffect("Billboard", effect);
                effectCache.Load(effect);
                technique = premultipliedAlpha
                    ? effect.Techniques["PremultipliedAlphaColorPass"]
                    : effect.Techniques["NormalAlphaColorPass"];
            }

            // Load the texture.
            if (textureFilename != null)
            {
                // First try and load it as a programming tile.
                texture = CardSpace.Cards.CardFaceTexture(textureFilename);

                // If that didn't work, treat it as an asset name.
                if (texture == null)
                {
                    texture = BokuGame.Load<Texture2D>(BokuGame.Settings.MediaPath + textureFilename);
                }
            }
            if (texture != null && size == Vector2.Zero)
            {
                UiCamera c = new UiCamera();
                size.X = texture.Width / c.Dpi;  // 96 DPI
                size.Y = texture.Height / c.Dpi;
            }

        }   // end of InitDeviceResources()

        public void UnloadContent()
        {
            BokuGame.Release(ref effect);
            BokuGame.Release(ref vbuf);

            // don't release a texture we don't control
            if (this.textureFilename != null)
            {
                BokuGame.Release(ref this.texture);
            }
        }

        public void DeviceReset(GraphicsDevice device)
        {
        }

    }   // end of class Billboard

}   // end of namespace Boku
