
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
using KoiX.Text;

using Boku.Base;
using Boku.Common;
using Boku.Common.Xml;
using Boku.Fx;
using Boku.SimWorld;

namespace Boku
{
    public class ThoughtBalloon
    {
        private Texture2D contentTexture = null;

        private GameThing thinker = null;   // The object that is spawning this thought balloon.
        private string text = null;         // What the thought balloon says.
        private string rawText = null;      // Text before any string (score) substitutions.
        private Vector4 color = new Vector4(0, 0, 0, 1);    // Color of board around throught balloon.

        private Matrix world;
        private Vector2 size;               // Size of whole balloon.

        private float alpha = 1.0f;

        private static float defaultDuration = 1.0f;    // Seconds per line to display.
        private double creationTime;
        private float fadeIn;
        private float duration;
        private float fadeOut;

        private VertexBuffer vbuf = null;
        private Vertex[] localVerts = new Vertex[4];

        private struct Vertex : IVertexType
        {
            private Vector3 position;
            private Vector2 texCoord;

            static VertexDeclaration decl = null;
            static VertexElement[] elements = new VertexElement[]
            {
                new VertexElement(0, VertexElementFormat.Vector3, VertexElementUsage.Position, 0),
                new VertexElement(12, VertexElementFormat.Vector2, VertexElementUsage.TextureCoordinate, 0),
                // Total == 20 bytes
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

        #region Accessors

        public GameThing Thinker
        {
            get { return thinker; }
        }

        public string Text
        {
            get { return text; }
            set
            {
                if (text != value)
                {
                    text = value;
                    RefreshTexture();
                }
            }
        }

        public string RawText
        {
            get { return rawText; }
            set { rawText = value; }
        }

        #endregion

        public ThoughtBalloon()
        {
            size = new Vector2(2.0f, 2.0f);
        }   // end of ThoughtBalloon c'tor

        /// <summary>
        /// Restart the timer on this thought balloon to extend 
        /// it's duration but still skip the fade in.
        /// </summary>
        public void RestartTime()
        {
            creationTime = Time.GameTimeTotalSeconds - fadeIn;
        }   // end of ThoughtBalloon RestartTime()

        /// <summary>
        /// Tell the thought balloon to shut down now rather than waiting 
        /// for its full duration.
        /// </summary>
        public void Kill()
        {
            // If not already fadin out, set the creation time so that it
            // starts fading out right now and shorten the fade time.
            double newCreationTime = Time.GameTimeTotalSeconds - duration;
            if (newCreationTime < creationTime)
            {
                creationTime = newCreationTime;
                fadeOut *= 0.5f;
            }
        }   // end of ThoughtBalloon Kill()

        public void InitDeviceResources(GraphicsDevice device)
        {
            // Init the vertex buffer.
            if (vbuf == null)
            {
                vbuf = new VertexBuffer(device, typeof(Vertex), 4, BufferUsage.WriteOnly);

                // Fill in the local vertex data.
                localVerts[0] = new Vertex(new Vector3(0.0f, size.Y, 0.0f), new Vector2(0.0f, 0.0f));
                localVerts[1] = new Vertex(new Vector3(-size.X, size.Y, 0.0f), new Vector2(1.0f, 0.0f));
                localVerts[2] = new Vertex(new Vector3(-size.X, 0.0f, 0.0f), new Vector2(1.0f, 0.99f));
                localVerts[3] = new Vertex(new Vector3(0.0f, 0.0f, 0.0f), new Vector2(0.0f, 0.99f));

                // Copy to vertex buffer.
                vbuf.SetData<Vertex>(localVerts);
            }

            // Create the texture.
            if (contentTexture == null)
            {
                contentTexture = new Texture2D(device, 256, 256, false, SurfaceFormat.Color);
            }

        }   // end of ThoughtBalloon Init()

        public void UnloadContent()
        {
            text = null;    // Force to not recycle.

            DeviceResetX.Release(ref contentTexture);
            DeviceResetX.Release(ref vbuf);

        }   // end of ThoughtBalloon UnloadContent()

        /// <summary>
        /// Recreate render targets
        /// </summary>
        /// <param name="graphics"></param>
        public void DeviceReset(GraphicsDevice device)
        {
        }
        
        public void Activate(GameThing thinker, string text, Vector4 color)
        {
            Activate(thinker, text, text, color);
        }

        public void Activate(GameThing thinker, string text, string rawText, Vector4 color)
        {
            bool recycled = this.text == text;

            this.thinker = thinker;
            this.text = text;
            this.rawText = rawText;
            this.color = color;

            creationTime = Time.GameTimeTotalSeconds;
            fadeIn = 0.15f;
            fadeOut = 0.15f;

            if (!recycled)
            {
                RefreshTexture();
            }
        }   // end of ThoughtBalloon Activate()

        public void RefreshTexture()
        {
            RenderTarget2D rt = SharedX.RenderTarget256_256;

            //
            // Render the frame and text to the rendertarget.
            //
            InGame.SetRenderTarget(rt);

            ScreenSpaceQuad ssquad = ScreenSpaceQuad.GetInstance();

            InGame.Clear(Color.Transparent);

            // Frame
            ssquad.Render(ThoughtBalloonManager.FrameTexture, Vector2.Zero, new Vector2(rt.Width, rt.Height), "TexturedRegularAlpha");

            int width = 238;
            TextBlob blob = new TextBlob(SharedX.GetGameFont30Bold, text, width);
            blob.Justification = TextHelper.Justification.Center;

            if (blob.NumLines > 3)
            {
                blob.Font = SharedX.GetGameFont24Bold;
                if (blob.NumLines > 3)
                {
                    blob.Font = SharedX.GetGameFont24Bold;
                }
            }

            duration = defaultDuration * blob.NumLines;

            int margin = 8;
            int middle = 76;    // Vertical midpoint of space for text.
            int lineSpacing = blob.Font().LineSpacing;
            int numLines = Math.Min(4, blob.NumLines);
            Vector2 pos = new Vector2(margin, middle - (numLines * 0.5f) * lineSpacing);
            blob.RenderText(null, pos, Color.Black, maxLines: numLines);

            InGame.RestoreRenderTarget();

            //
            // Copy result to local texture.
            //
            rt.GetData<int>(_scratchData);
            contentTexture.SetData<int>(_scratchData);

        }   // end of ThoughtBalloon RefreshTexture()
        private static int[] _scratchData = new int[256 * 256];

        /// <summary>
        /// Update of balloon for current frame.
        /// </summary>
        /// <param name="camera"></param>
        /// <returns>True if still alive, false if dead.</returns>
        public bool Update(Camera camera)
        {
            double curTime = Time.GameTimeTotalSeconds;
            
            float elapsedTime = (float)(curTime - creationTime);

            // First, check if time has expired.
            if (elapsedTime > fadeIn + duration + fadeOut)
            {
                // We're done.
                return false;
            }

            // Calc alpha based on time.
            if (elapsedTime < fadeIn)
            {
                // Fading in...
                alpha = 1.0f - (fadeIn - elapsedTime) / fadeIn;
            }
            else if (elapsedTime < fadeIn + duration)
            {
                // Fully visible...
                alpha = 1.0f;
            }
            else
            {
                // Fading out...
                elapsedTime -= fadeIn + duration;
                alpha = Math.Max(0.0f, (fadeOut - elapsedTime) / fadeOut);
            }

            // Orient to always face camera.
            world = Matrix.CreateBillboard(
                thinker.WorldThoughtBalloonOffset,
                camera.ActualFrom,
                camera.ViewUp,
                camera.ViewDir);

            return true;
        }   // end of ThoughtBalloon Update()
            

        public void Render(Camera camera)
        {
            GraphicsDevice device = KoiLibrary.GraphicsDevice;

            if (thinker.FirstPerson)
            {
                // In first person just render the ballon to the screen in 2D.
                ScreenSpaceQuad quad = ScreenSpaceQuad.GetInstance();

                Vector2 size = new Vector2(contentTexture.Width, contentTexture.Height);
                Vector2 position = new Vector2(camera.Resolution.X - size.X, 0);
                // Adjust for overscan.
                Vector2 res = new Vector2(camera.Resolution.X, -camera.Resolution.Y);
                position -= res * 0.5f * 5.0f / 100.0f;

                quad.Render(contentTexture, position, size, "TexturedPreMultAlpha");
            }
            else
            {
                // Cull thought balloons based on the actor's bounding sphere.  Note that
                // bounding spheres are in actor local cooords and so need to be translated
                // by the actor's position.
                if (camera.Frustum.CullTest(thinker.BoundingSphere.Center + thinker.Movement.Position, thinker.BoundingSphere.Radius) != Frustum.CullResult.TotallyOutside)
                {
                    Effect effect = ThoughtBalloonManager.Effect;

                    Matrix worldViewProjMatrix = world * camera.ViewMatrix * camera.ProjectionMatrix;
                    effect.Parameters["WorldViewProjMatrix"].SetValue(worldViewProjMatrix);
                    effect.Parameters["WorldMatrix"].SetValue(world);

                    effect.Parameters["ContentTexture"].SetValue(contentTexture);

                    effect.Parameters["Size"].SetValue(size);
                    effect.Parameters["Alpha"].SetValue(alpha);
                    effect.Parameters["BorderColor"].SetValue(color);

                    device.SetVertexBuffer(vbuf);
                    device.Indices = SharedX.QuadIndexBuff;

                    // Render all passes.
                    for (int i = 0; i < effect.CurrentTechnique.Passes.Count; i++)
                    {
                        EffectPass pass = effect.CurrentTechnique.Passes[i];
                        pass.Apply();
                        device.DrawIndexedPrimitives(PrimitiveType.TriangleList, 0, 0, 4, 0, 2);
                    }
                }
            }
        }   // end of ThoughtBalloon Render()

    }   // end of class ThoughtBalloon

}   // end of namespace Boku
