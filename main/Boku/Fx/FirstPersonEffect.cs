
/// Relocated from Common namespace

using System;
using System.Collections.Generic;

using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

using KoiX;

using Boku.Base;
using Boku.Common;
using Boku.SimWorld;

namespace Boku.Fx
{
    public partial class FirstPersonEffectMgr
    {
        public abstract class FirstPersonEffect : INeedsDeviceReset
        {
            #region Members
            private float priority = 0.0f;

            protected Effect effect = null;

            struct Vertex : IVertexType
            {
                Vector2 pos;

                static VertexDeclaration decl = null;
                static VertexElement[] elements = new VertexElement[]
            {
                new VertexElement(0, VertexElementFormat.Vector2, VertexElementUsage.Position, 0),
                // Total = 8 bytes
            };

                public Vertex(Vector2 uv)
                {
                    this.pos = uv;
                }

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

            }

            private static Vertex[] verts = new Vertex[4]
                {
                    new Vertex(new Vector2(-1.0f, -1.0f)),
                    new Vertex(new Vector2( 1.0f, -1.0f)),
                    new Vertex(new Vector2(-1.0f,  1.0f)),
                    new Vertex(new Vector2( 1.0f,  1.0f))
                };

            #endregion Members

            #region Accessors
            /// <summary>
            /// Control for layering of effects, lower priority draws first
            /// </summary>
            public float Priority
            {
                get { return priority; }
                protected set { priority = value; }
            }
            public Effect Effect
            {
                get { return effect; }
            }
            #endregion Accessors

            #region Public

            /// <summary>
            /// Do any once a frame update. Return false when time to die.
            /// </summary>
            /// <returns></returns>
            public virtual bool Update()
            {
                return true;
            }

            /// <summary>
            /// Render the effect to screen.
            /// </summary>
            /// <param name="camera"></param>
            public virtual void Render(Camera camera)
            {
                GraphicsDevice device = KoiLibrary.GraphicsDevice;
                if (SetupEffect(camera))
                {
                    for (int i = 0; i < Effect.CurrentTechnique.Passes.Count; ++i)
                    {
                        EffectPass pass = Effect.CurrentTechnique.Passes[i];

                        pass.Apply();

                        device.DrawUserPrimitives<Vertex>(
                            PrimitiveType.TriangleStrip,
                            verts,
                            0,
                            2);

                    }
                }

            }

            #endregion Public

            #region Internal

            /// <summary>
            /// Called to tell an object to load any device dependent parts of itself.
            /// </summary>
            public virtual void LoadContent(bool immediate)
            {
            }

            /// <summary>
            /// Called to load anything requiring the device.
            /// </summary>
            /// <param name="graphics"></param>
            public virtual void InitDeviceResources(GraphicsDevice device)
            {
            }

            /// <summary>
            /// Called to tell an object to remove/delete any device dependent parts.
            /// </summary>
            public virtual void UnloadContent()
            {
            }

            /// <summary>
            /// Recreate render targets.
            /// </summary>
            public virtual void DeviceReset(GraphicsDevice device)
            {
            }

            /// <summary>
            /// Load parameters into the effect for rendereing.
            /// </summary>
            /// <param name="camera"></param>
            /// <returns></returns>
            protected abstract bool SetupEffect(Camera camera);

            #endregion Internal
        }
    }
}
