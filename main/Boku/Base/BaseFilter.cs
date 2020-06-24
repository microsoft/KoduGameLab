
using System;
using System.Collections.Generic;
using System.Diagnostics;

using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Audio;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using Microsoft.Xna.Framework.Storage;

using KoiX;

namespace Boku.Base
{
    public class BaseFilter : INeedsDeviceReset
    {
        static protected VertexBuffer vbuf = null;
        protected Effect effect = null;

        private struct FilterVertex : IVertexType
        {
            public Vector2 uv;

            static VertexDeclaration decl;
            static VertexElement[] elements = new VertexElement[]
            {
                new VertexElement(0, VertexElementFormat.Vector2, VertexElementUsage.TextureCoordinate, 0),
                // Total == 8 bytes
            };

            public FilterVertex(Vector2 uv)
            {
                this.uv = uv;
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
        }   // end of FilterVertex

        static FilterVertex[] localVerts = new FilterVertex[4]
            {
                new FilterVertex(new Vector2(0.0f, 0.0f)),
                new FilterVertex(new Vector2(1.0f, 0.0f)),
                new FilterVertex(new Vector2(0.0f, 1.0f)),
                new FilterVertex(new Vector2(1.0f, 1.0f))
            };


        // c'tor
        protected BaseFilter()
        {
        }

        protected void SetUvToPos()
        {
            GraphicsDevice device = KoiLibrary.GraphicsDevice;

            int width = device.Viewport.Width;
            int height = device.Viewport.Height;

            float pixelWidth = 1.0f / width;
            float pixelHeight = 1.0f / height;

            Vector4 uvToPos = new Vector4(
                2.0f,                   // x scale
                -2.0f,                  // y scale
                -1.0f - pixelWidth,     // x offset
                1.0f + pixelHeight);    // y offset

            effect.Parameters["UvToPos"].SetValue(uvToPos);

        }   // end of BaseFilter SetUvToPos()


        public virtual void LoadContent(bool immediate)
        {
        }   // end of BaseFilter LoadContent()

        public virtual void InitDeviceResources(GraphicsDevice device)
        {
            // Init the vertex buffer.
            if (vbuf == null)
            {
                vbuf = new VertexBuffer(device, typeof(FilterVertex), 4, BufferUsage.WriteOnly);
                
                vbuf.SetData<FilterVertex>(localVerts);
            }

        }

        public virtual void UnloadContent()
        {
            DeviceResetX.Release(ref effect);       // a little odd that this is released in this class since it's not allocated here. shouldn't we do this in the subclass that allocates?
            DeviceResetX.Release(ref vbuf);
        }   // end of BaseFilter UnloadContent()

        public virtual void DeviceReset(GraphicsDevice device)
        {
        }

    }   // end of class BaseFilter

}   // end of namespace Boku.Common


