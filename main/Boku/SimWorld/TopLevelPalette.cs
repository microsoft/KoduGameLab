// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.


using System;
using System.Collections;
using System.Diagnostics;
using System.IO;
using System.Xml.Serialization;

using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Audio;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using Microsoft.Xna.Framework.Storage;

using Boku.Base;
using Boku.SimWorld;
using Boku.Common;
using Boku.Programming;
using Boku.UI;
using Boku.UI2D;
using Boku.Input;

namespace Boku
{
    public class TopLevelPalette : INeedsDeviceReset
    {
        private bool active = true;
        private bool open = false;
        private InGame.UpdateMode select = InGame.UpdateMode.RunSim;
        private int selectIndex = 0;

        private const int numIcons = 4;

        private struct Vertex
        {
            private Vector2 position;
            private Vector2 uvScreen;
            private Vector2 uvFrame;
            private Vector2 uvIcons;

            public Vertex(Vector2 position, Vector2 uvScreen, Vector2 uvFrame, Vector2 uvIcons)
            {
                this.position = position;
                this.uvScreen = uvScreen;
                this.uvFrame = uvFrame;
                this.uvIcons = uvIcons;

            }   // end of Vertex c'tor
        }   // end of Vertex

        // Declare the vertex structure we'll use.
        static private VertexElement[] elements = new VertexElement[]
        {
            new VertexElement(0, 0, VertexElementFormat.Vector2, VertexElementMethod.Default, VertexElementUsage.Position, 0),
            new VertexElement(0, 8, VertexElementFormat.Vector2, VertexElementMethod.Default, VertexElementUsage.TextureCoordinate, 0),     // Frame UV coords
            new VertexElement(0, 16, VertexElementFormat.Vector2, VertexElementMethod.Default, VertexElementUsage.TextureCoordinate, 1),    // Screen UV coords
            new VertexElement(0, 24, VertexElementFormat.Vector2, VertexElementMethod.Default, VertexElementUsage.TextureCoordinate, 2),    // Icon UV coords
            // size == 32
        };

        // unused 1/10/2008 mattmac // private int stride = 32;
        private Vertex[] vertices = new Vertex[8];

        private Effect effect = null;
        static private VertexDeclaration decl = null;

        private Vector2 position = new Vector2(-30.0f, 20.0f);      // Top left corner of palette in pixels.

        private const float fullWidth = (numIcons - 1) * 96.0f;     // Width of center section when fully open in pixels.
        private const float fullFrameWidth = 9.0f;                  // Extra width of frame when palette is open.
        private const float iconCenterBase = 0.5f;                  // U coord for icon texture at center of palette when palette is open.
        
        private float centerWidth = 0.0f;               // Width of center section.  Collapses to 0 when in badge mode.
        private float frameWidth = 0.0f;                // Current frame extra width.  This is 0 when palette is in badge mode.
        private float iconCenter = iconCenterBase;      // Move to center over current selection when palette closes into badge mode.

        private Texture2D iconTexture = null;
        private Texture2D screenTexture = null;
        private Texture2D frameTexture = null;

        private const float badgeAlpha = 0.75f;         // Blending for badge (closed) mode.
        private float alpha = 1.0f;                     // Overall blending for whole palette.
        private Vector4 selectedColor = new Vector4(1.0f, 0.9f, 0.467f, 1.0f);
        private Vector4 unselectedColor = new Vector4(0.9f, 0.9f, 0.9f, 1.0f);

        private Vector4[] color = new Vector4[(int)InGame.UpdateMode.NumModesInPalette];

        private float scale = 1.0f; // Used to compensate for low resolution screens.

        // Timing constants for animation, all in seconds.
        private float openCloseTime = 0.3f;
        private float changeColorTime = 0.2f;
        private float fadeInOutTime = 0.3f;

        #region Accessors
        /// <summary>
        /// By default this is always on.  May be used to 
        /// turn the palette off for screenshots or ???
        /// </summary>
        public bool Active
        {
            get { return active; }
            set { active = value; }
        }
        public bool Open
        {
            get { return open; }
            set 
            {
                if (open != value)
                {
                    open = value;

                    if (open)
                    {
                        Alpha = 1.0f;
                        {
                            // frameWidth = fullFrameWidth;
                            TwitchManager.GetFloat get = delegate(Object param) { return frameWidth; };
                            TwitchManager.SetFloat set = delegate(float val, Object param) { frameWidth = val; };
                            TwitchManager.FloatTwitch twitch = new TwitchManager.FloatTwitch(get, set, fullFrameWidth, openCloseTime, TwitchCurve.Shape.EaseInOut);
                            twitch.Start();
                        }
                        {
                            // centerWidth = fullWidth;
                            TwitchManager.GetFloat get = delegate(Object param) { return centerWidth; };
                            TwitchManager.SetFloat set = delegate(float val, Object param) { centerWidth = val; };
                            TwitchManager.FloatTwitch twitch = new TwitchManager.FloatTwitch(get, set, fullWidth, openCloseTime, TwitchCurve.Shape.EaseInOut);
                            twitch.Start();
                        }
                        {
                            // iconCenter = iconCenterBase;
                            TwitchManager.GetFloat get = delegate(Object param) { return iconCenter; };
                            TwitchManager.SetFloat set = delegate(float val, Object param) { iconCenter = val; };
                            TwitchManager.FloatTwitch twitch = new TwitchManager.FloatTwitch(get, set, iconCenterBase, openCloseTime, TwitchCurve.Shape.EaseInOut);
                            twitch.Start();
                        }
                    }
                    else
                    {
                        Alpha = badgeAlpha;
                        {
                            // frameWidth = 0;
                            TwitchManager.GetFloat get = delegate(Object param) { return frameWidth; };
                            TwitchManager.SetFloat set = delegate(float val, Object param) { frameWidth = val; };
                            TwitchManager.FloatTwitch twitch = new TwitchManager.FloatTwitch(get, set, 0.0f, openCloseTime, TwitchCurve.Shape.EaseInOut);
                            twitch.Start();
                        }
                        {
                            // centerWidth = 0;
                            TwitchManager.GetFloat get = delegate(Object param) { return centerWidth; };
                            TwitchManager.SetFloat set = delegate(float val, Object param) { centerWidth = val; };
                            TwitchManager.FloatTwitch twitch = new TwitchManager.FloatTwitch(get, set, 0.0f, openCloseTime, TwitchCurve.Shape.EaseInOut);
                            twitch.Start();
                        }
                        {
                            // iconCenter = centered over selected icon
                            float offsetUV = 1.0f / numIcons;
                            float baseUV = offsetUV / 2.0f;
                            float newCenter = baseUV + offsetUV * selectIndex;
                            TwitchManager.GetFloat get = delegate(Object param) { return iconCenter; };
                            TwitchManager.SetFloat set = delegate(float val, Object param) { iconCenter = val; };
                            TwitchManager.FloatTwitch twitch = new TwitchManager.FloatTwitch(get, set, newCenter, openCloseTime, TwitchCurve.Shape.EaseInOut);
                            twitch.Start();
                        }
                    }
                }
            }
        }
        /// <summary>
        /// Sets which item on the palette is currently hot.
        /// </summary>
        public InGame.UpdateMode Select
        {
            set
            {
                if (select != value)
                {
                    {
                        // Change previous selection to unselected color.
                        int oldIndex = selectIndex;
                        TwitchManager.GetVector4 get = delegate(Object param) { return color[oldIndex]; };
                        TwitchManager.SetVector4 set = delegate(Vector4 val, Object param) { color[oldIndex] = val; };
                        TwitchManager.Vector4Twitch twitch = new TwitchManager.Vector4Twitch(get, set, unselectedColor, changeColorTime, TwitchCurve.Shape.EaseInOut);
                        twitch.Start();
                    }
                    select = value;
                    selectIndex = (int)select;
                    {
                        // Change new selection to selected color.
                        int newIndex = selectIndex;
                        TwitchManager.GetVector4 get = delegate(Object param) { return color[newIndex]; };
                        TwitchManager.SetVector4 set = delegate(Vector4 val, Object param) { color[newIndex] = val; };
                        TwitchManager.Vector4Twitch twitch = new TwitchManager.Vector4Twitch(get, set, selectedColor, changeColorTime, TwitchCurve.Shape.EaseInOut);
                        twitch.Start();
                    }
                    {
                        // Move icon center.  We shouldn't see this changing when the palette in in "badge" mode 
                        // but just in case we decide to use it this way, make sure the right icon is showing.
                        // iconCenter = centered over selected icon
                        float offsetUV = 1.0f / numIcons;
                        float baseUV = offsetUV / 2.0f;
                        float newCenter = Open ? iconCenterBase : baseUV + offsetUV * selectIndex;
                        TwitchManager.GetFloat get = delegate(Object param) { return iconCenter; };
                        TwitchManager.SetFloat set = delegate(float val, Object param) { iconCenter = val; };
                        TwitchManager.FloatTwitch twitch = new TwitchManager.FloatTwitch(get, set, newCenter, openCloseTime, TwitchCurve.Shape.EaseInOut);
                        twitch.Start();
                    }
                }
            }
        }
        /// <summary>
        /// Sets which item on the palette is currently hot.
        /// </summary>
        public int SelectionIndex
        {
            set
            {
                if (selectIndex != value)
                {
                    // Defer to the Select accessor for setting the twitches.
                    Select = (InGame.UpdateMode)value;
                }
            }
        }
        public float Alpha
        {
            get { return alpha; }
            set
            {
                if (alpha != value)
                {
                    {
                        // alpha = value;
                        TwitchManager.GetFloat get = delegate(Object param) { return alpha; };
                        TwitchManager.SetFloat set = delegate(float val, Object param) { alpha = val; };
                        TwitchManager.FloatTwitch twitch = new TwitchManager.FloatTwitch(get, set, value, fadeInOutTime, TwitchCurve.Shape.EaseInOut);
                        twitch.Start();
                    }
                }
            }
        }

        /// <summary>
        /// Returns the position of the closed palette in pixel coordinates.
        /// Useful for anything that want to try and line up with the palette.
        /// </summary>
        public Vector2 CenterOfClosedPalette
        {
            get
            {
                Vector2 center = position + scale * new Vector2(128.0f, 64.0f);
                return center;
            }
        }
        #endregion

        // c'tor
        public TopLevelPalette()
        {
            for (int i = 0; i < (int)InGame.UpdateMode.NumModesInPalette; i++)
            {
                color[i] = unselectedColor;
            }

            // Make sure everything is gets set correctly.
            SelectionIndex = 3;
            SelectionIndex = 0;
            Open = true;
            Open = false;

            // Make scale relative to screen height.  Assume that 720p is max 
            // size we want to adjust for.
            float height = BokuGame.bokuGame.GraphicsDevice.Viewport.Height;
            height = MathHelper.Min(height, 720.0f);
            scale = height / 720.0f;
        }   // end of TopLevelPalette c'tor

        public void Update()
        {
            // Transform position and size to homogeneous coordinates.
            Vector2 viewport = new Vector2((float)BokuGame.bokuGame.GraphicsDevice.Viewport.Width, (float)BokuGame.bokuGame.GraphicsDevice.Viewport.Height);

            Vector2 pos = 2.0f * position / viewport - new Vector2(1.0f, 1.0f);
            float width = 2.0f * centerWidth / viewport.X;
            Vector2 size = 2.0f * new Vector2(128.0f, 128.0f) / viewport;   // Size of end tiles.

            // Adjust for low res display.
            width *= scale;
            size *= scale;

            float frameOffset = frameWidth / 128.0f;
            pos.X += frameOffset;

            // Invert for homogeneous coords.
            pos.Y = -pos.Y;
            size.Y = -size.Y;

            // This is how wide the center section of the 
            // screen is in UV coords for the icon texture.
            float widthU = (numIcons - 1.0f) / numIcons * frameWidth / fullFrameWidth;
            widthU *= 0.5f; // We only need half.

            // Fill in the local vertex data in homogeneous coords.
            float foo = 0.325f; // foo is a correction factor for the UVs for the icon texture.  It is used
                                // to adjust for the scaling in U at the ends of the palette shape.  Right
                                // now it's ad hoc.  If numIcons changes then foo needs to be adjusted to
                                // Make sure the icons at either end of the palette maintain their correct
                                // aspect ratio.  
                                // for numIcons==4 foo should be 0.325, for numIcons==5 foo should be 0.255
            vertices[0] = new Vertex(new Vector2(pos.X, pos.Y + size.Y),                            new Vector2(-0.25f + frameOffset, 1.0f), new Vector2(-0.25f, 1.0f), new Vector2(iconCenter - widthU - foo, 1.0f));
            vertices[1] = new Vertex(new Vector2(pos.X, pos.Y),                                     new Vector2(-0.25f + frameOffset, 0.0f), new Vector2(-0.25f, 0.0f), new Vector2(iconCenter - widthU - foo, 0.0f));
            vertices[2] = new Vertex(new Vector2(pos.X + size.X, pos.Y + size.Y),                   new Vector2( 0.25f + frameOffset, 1.0f), new Vector2( 0.25f, 1.0f), new Vector2(iconCenter - widthU, 1.0f));
            vertices[3] = new Vertex(new Vector2(pos.X + size.X, pos.Y),                            new Vector2( 0.25f + frameOffset, 0.0f), new Vector2( 0.25f, 0.0f), new Vector2(iconCenter - widthU, 0.0f));
            vertices[4] = new Vertex(new Vector2(pos.X + size.X + width, pos.Y + size.Y),           new Vector2( 0.75f - frameOffset, 1.0f), new Vector2( 0.75f, 1.0f), new Vector2(iconCenter + widthU, 1.0f));
            vertices[5] = new Vertex(new Vector2(pos.X + size.X + width, pos.Y),                    new Vector2( 0.75f - frameOffset, 0.0f), new Vector2( 0.75f, 0.0f), new Vector2(iconCenter + widthU, 0.0f));
            vertices[6] = new Vertex(new Vector2(pos.X + size.X + width + size.X, pos.Y + size.Y),  new Vector2(1.25f - frameOffset, 1.0f),  new Vector2(1.25f, 1.0f),  new Vector2(iconCenter + widthU + foo, 1.0f));
            vertices[7] = new Vertex(new Vector2(pos.X + size.X + width + size.X, pos.Y),           new Vector2(1.25f - frameOffset, 0.0f),  new Vector2(1.25f, 0.0f),  new Vector2(iconCenter + widthU + foo, 0.0f));

        }   // end of TopLevelPalette Update()

        public void Render()
        {
            if (Active)
            {
                GraphicsDevice device = BokuGame.bokuGame.GraphicsDevice;

                device.VertexDeclaration = decl;

                // Render all passes.
                effect.Parameters["FrameTexture"].SetValue(frameTexture);
                effect.Parameters["ScreenTexture"].SetValue(screenTexture);
                effect.Parameters["IconTexture"].SetValue(iconTexture);

                effect.Parameters["Alpha"].SetValue(Alpha);

                effect.CurrentTechnique = effect.Techniques["Normal"];

                effect.Parameters["NumIcons"].SetValue((float)numIcons);

                for (int i = 0; i < numIcons; i++)
                {
                    effect.Parameters["Color" + i.ToString()].SetValue(color[i]);
                }

                effect.Begin();
                for (int i = 0; i < effect.CurrentTechnique.Passes.Count; i++)
                {
                    EffectPass pass = effect.CurrentTechnique.Passes[i];
                    pass.Begin();
                    device.DrawUserPrimitives(PrimitiveType.TriangleStrip, vertices, 0, 6);
                    pass.End();
                }
                effect.End();

            }   // end if Active

        }   // end of TopLevelPalette Render()

        public void LoadGraphicsContent(GraphicsDeviceManager graphics)
        {
            if (effect == null)
            {
                effect = BokuGame.ContentManager.Load<Effect>(BokuGame.Settings.MediaPath + @"Shaders\TopLevelPalette");
            }

            if (decl == null)
            {
                decl = new VertexDeclaration(graphics.GraphicsDevice, elements);            
            }

            if (iconTexture == null)
            {
                iconTexture = BokuGame.ContentManager.Load<Texture2D>(BokuGame.Settings.MediaPath + @"Textures\TopEditPalette\Icons");
            }

            if (screenTexture == null)
            {
                screenTexture = BokuGame.ContentManager.Load<Texture2D>(BokuGame.Settings.MediaPath + @"Textures\TopEditPalette\Screen");
            }

            if (frameTexture == null)
            {
                frameTexture = BokuGame.ContentManager.Load<Texture2D>(BokuGame.Settings.MediaPath + @"Textures\TopEditPalette\Frame");
            }

        }   // end of TopLevelPalette LoadGraphicsContent()

        public void UnloadGraphicsContent()
        {
            BokuGame.Release(ref effect);
            BokuGame.Release(ref decl);

            BokuGame.Release(ref iconTexture);
            BokuGame.Release(ref screenTexture);
            BokuGame.Release(ref frameTexture);

        }   // end of TopLevelPalette UnloadGraphicsContent()

    }   // end of class TopLevelPalette

}   // end of namespace Boku


