
/// Relocated from Boku.Common namespace

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
using Boku.Common;

namespace Boku.Fx
{
    /// <summary>
    /// Simple 2d quad which can be instanced as needed.  The size 
    /// is initialized to 1x1 which can be scaled as needed.  The quad
    /// is centered on 0,0,0 and lives in the X/Y plane aka Z==0.
    /// This is set up as a singleton so only one is ever created and
    /// can be shared/rendered as needed.
    /// </summary>
    public class SimpleTexturedQuad : INeedsDeviceReset
    {
        private static SimpleTexturedQuad instance = null;

        private static Vector4 defaultTint = Vector4.One;

        private Effect effect = null;

        public struct Vertex : IVertexType
        {
            private Vector2 pos;    // Expanded to a Vector4 in the vertex shader.
            private Vector2 tex;

            static VertexDeclaration decl = null;
            static VertexElement[] elements = new VertexElement[]
            {
                new VertexElement(0, VertexElementFormat.Vector2, VertexElementUsage.Position, 0),
                new VertexElement(8, VertexElementFormat.Vector2, VertexElementUsage.TextureCoordinate, 0),
                // Total = 16 bytes
            };

            // c'tor
            public Vertex(Vector2 pos, Vector2 tex)
            {
                this.pos = pos;
                this.tex = tex;
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

        private Vertex[] localVerts = new Vertex[4];

        public static SimpleTexturedQuad GetInstance()
        {
            if (instance == null)
            {
                instance = new SimpleTexturedQuad();
            }

            return instance;
        }   // end of SimpleTexturedQuad GetInstance()

        // c'tor
        private SimpleTexturedQuad()
        {
            // Fill in the local vertex data.
            localVerts[0] = new Vertex(new Vector2(0.5f, 0.5f), new Vector2(1.0f, 0.0f));
            localVerts[1] = new Vertex(new Vector2(0.5f, -0.5f), new Vector2(1.0f, 1.0f));
            localVerts[2] = new Vertex(new Vector2(-0.5f, -0.5f), new Vector2(0.0f, 1.0f));
            localVerts[3] = new Vertex(new Vector2(-0.5f, 0.5f), new Vector2(0.0f, 0.0f));
        }   // end of SimpleTexturedQuad c'tor

        public void Render(Camera camera, Texture2D texture, ref Matrix worldMatrix, float alpha)
        {
            Render(camera, texture, ref worldMatrix, alpha, ref defaultTint);
        }

        public void Render(Camera camera, Texture2D texture, ref Matrix worldMatrix, float alpha, ref Vector4 tint)
        {
            GraphicsDevice device = BokuGame.bokuGame.GraphicsDevice;

            Matrix worldViewProjMatrix = worldMatrix * camera.ViewProjectionMatrix;
            effect.Parameters[ "WorldViewProjMatrix" ].SetValue(worldViewProjMatrix);
            effect.Parameters[ "WorldMatrix" ].SetValue(worldMatrix);
            effect.Parameters[ "Texture" ].SetValue(texture);
            effect.Parameters[ "Alpha" ].SetValue(alpha);
            effect.Parameters[ "Tint" ].SetValue(tint);

            effect.CurrentTechnique = effect.Techniques[ "TexturedNormalAlpha" ];

            device.SetVertexBuffer(null);

            // Render all passes.
            for (int i = 0; i < effect.CurrentTechnique.Passes.Count; i++)
            {
                EffectPass pass = effect.CurrentTechnique.Passes[i];
                pass.Apply();
                device.DrawUserIndexedPrimitives(PrimitiveType.TriangleList, localVerts, 0, 4, UI2D.Shared.QuadIndices, 0, 2);
            }

        }   // end of SimpleTexturedQuad Render()

        /// <summary>
        /// Render the quad using a texture and an alpha map. The alpha map may be an RGB or
        /// RGBA surface. Alpha values are sampled from the red channel of the map.
        /// </summary>
        /// <param name="camera"></param>
        /// <param name="texture"></param>
        /// <param name="worldMatrix"></param>
        /// <param name="alphaMap"></param>
        public void Render(Camera camera, Texture2D texture, ref Matrix worldMatrix, Texture2D alphaMap)
        {
            Render(camera, texture, ref worldMatrix, alphaMap, 1, ref defaultTint);
        }
        public void Render(Camera camera, Texture2D texture, ref Matrix worldMatrix, Texture2D alphaMap, float alpha)
        {
            Render(camera, texture, ref worldMatrix, alphaMap, alpha, ref defaultTint);
        }

        /// <summary>
        /// Render the quad using a texture and an alpha map. The alpha map may be an RGB or
        /// RGBA surface. Alpha values are sampled from the red channel of the map. The sampled
        /// alpha map value is multiplied by the provided global alpha value.
        /// </summary>
        /// <param name="camera"></param>
        /// <param name="texture"></param>
        /// <param name="worldMatrix"></param>
        /// <param name="alphaMap"></param>
        /// <param name="alpha"></param>
        public void Render(Camera camera, Texture2D texture, ref Matrix worldMatrix, Texture2D alphaMap, float alpha, ref Vector4 tint)
        {
            GraphicsDevice device = BokuGame.bokuGame.GraphicsDevice;

            Matrix worldViewProjMatrix = worldMatrix * camera.ViewProjectionMatrix;
            effect.Parameters["WorldViewProjMatrix"].SetValue(worldViewProjMatrix);
            effect.Parameters["WorldMatrix"].SetValue(worldMatrix);
            effect.Parameters["Texture"].SetValue(texture);
            effect.Parameters["AlphaMap"].SetValue(alphaMap);
            effect.Parameters["Alpha"].SetValue(alpha);
            effect.Parameters["Tint"].SetValue(tint);

            effect.CurrentTechnique = effect.Techniques["TexturedWithAlphaMap"];

            device.SetVertexBuffer(null);

            // Render all passes.
            for (int i = 0; i < effect.CurrentTechnique.Passes.Count; i++)
            {
                EffectPass pass = effect.CurrentTechnique.Passes[i];
                pass.Apply();
                device.DrawUserIndexedPrimitives(PrimitiveType.TriangleList, localVerts, 0, 4, UI2D.Shared.QuadIndices, 0, 2);
            }

        }   // end of SimpleTexturedQuad Render()

        public void LoadContent(bool immediate)
        {
            // Init the effect.
            if (effect == null)
            {
                effect = BokuGame.Load<Effect>(BokuGame.Settings.MediaPath + @"Shaders\SimpleQuad");
            }
        }   // end of SimpleTexturedQuad LoadContent()

        public void InitDeviceResources(GraphicsDevice device)
        {
        }

        public void UnloadContent()
        {
            BokuGame.Release(ref effect);
        }   // end of SimpleTexturedQuad UnloadContent()

        /// <summary>
        /// Recreate render targets.
        /// </summary>
        /// <param name="graphics"></param>
        public void DeviceReset(GraphicsDevice device)
        {
        }

    }   // end of class SimpleTexturedQuad

}   // end of namespace Boku.Common