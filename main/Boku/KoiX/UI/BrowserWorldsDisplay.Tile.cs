
using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;

using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Audio;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using Microsoft.Xna.Framework.Input.Touch;
using Microsoft.Xna.Framework.Storage;

using KoiX;
using KoiX.Geometry;
using KoiX.Input;
using KoiX.Managers;
using KoiX.Text;
using KoiX.UI;
using KoiX.UI.Dialogs;

using Boku;
using Boku.Common;
using Boku.Common.Xml;
namespace KoiX.UI
{
    public partial class BrowserWorldsDisplay : WidgetSet, IDeviceResetX
    {
        /// <summary>
        /// Internal class to hold hard-coded tile orientations.
        /// </summary>
        public class Transform
        {
            public Vector2 position;
            public float scale;
            public float rotation;

            // c'tors
            public Transform()
            {
                position = Vector2.Zero;
                scale = 1.0f;
                rotation = 0.0f;
            }

            public Transform(Vector2 position, float scale, float rotation)
            {
                this.position = position;
                this.scale = scale;
                this.rotation = rotation;
            }

            public Transform(Transform o)
            {
                this.position = o.position;
                this.scale = o.scale;
                this.rotation = o.rotation;
            }

        }   // end of class Orientation

        public class Tile : BaseButton
        {
            #region Members

            LevelMetadata level;
            
            Transform transform = new Transform();          // Actual values used for display.
            Transform twitchTransform = new Transform();    // Values we're twitching toward.

            Texture2D thumbnail;
            RenderTarget2D rt;

            GetFont Font;           // Delegate which will return the font to use for this UI element.  This font returned
                                    // by this should not be held onto since a device reset may change it.

            Matrix worldMatrix = Matrix.Identity;
            Matrix inverseWorldMatrix = Matrix.Identity;

            SpriteCamera camera;

            bool rtDirty = true;

            static Texture2D downloadInProgressTexture;
            static Texture2D downloadCompleteTexture;
            static Texture2D downloadFailedTexture;
            static Texture2D reportAbuseTexture;

            #endregion

            #region Accessors

            public LevelMetadata Level
            {
                get { return level; }
                set { level = value; rtDirty = true; }
            }

            public Texture2D Thumbnail
            {
                get { return thumbnail; }
                set { thumbnail = value; }
            }

            public string Title
            {
                get
                {
                    if (Level != null)
                    {
                        return TextHelper.FilterInvalidCharacters(Level.Name);
                    }
                    else
                    {
                        return String.Empty;
                    }
                }
            }

            public bool RtDirty
            {
                get { return rtDirty; }
                set { rtDirty = value; }
            }

            #endregion

            #region Public

            public Tile(BaseDialog parentDialog, Callback onSelect)
                : base(parentDialog, onSelect)
            {
                ThemeSet theme = Theme.CurrentThemeSet;

                SystemFont font = new SystemFont(theme.TextFontFamily, theme.TextBaseFontSize, System.Drawing.FontStyle.Bold);
                FontWrapper wrapper = new FontWrapper(null, font);
                Font = delegate() { return wrapper; };

                // Note, we don't transform this.  We just use it for hit testing.
                LocalRect = new RectangleF(0, 0, 256, 256);
            }   // end of c'tor

            public override void Update(SpriteCamera camera, Vector2 parentPosition)
            {
                this.camera = camera;
                // Convert parentPosition from screen coords to camera coords since
                // that the coordinate system we render them in.
                this.parentPosition = parentPosition - camera.ScreenSize / 2.0f / camera.Zoom;

                // Build local matrix based on orientation.
                worldMatrix = Matrix.CreateRotationZ(transform.rotation)
                    * Matrix.CreateScale(transform.scale)
                    * Matrix.CreateTranslation(new Vector3(transform.position + this.parentPosition, 0));

                inverseWorldMatrix = Matrix.CreateTranslation(new Vector3(-transform.position, 0))
                    * Matrix.CreateScale(1.0f / transform.scale)
                    * Matrix.CreateRotationZ(-transform.rotation);

                if (rtDirty || rt.IsContentLost)
                {
                    RenderToTexture();
                }

            }   // end of Update()

            public void Render(SpriteCamera camera, LevelBrowserType browserType)
            {
                if (level != null)
                {
                    ThemeSet theme = Theme.CurrentThemeSet;

                    RoundedRect.Render(camera, LocalRect, theme.BaseCornerRadius, Color.White,
                                        texture: rt,
                                        shadowStyle: ShadowStyle.Outer, shadowSize: 18.0f, shadowOffset: new Vector2(5, 5), shadowAttenuation: 0.9f,
                                        worldMatrix: worldMatrix);

                    // Need to know whether we're in local or community browser
                    // since we only render the "decorations" while in community.
                    if(browserType == LevelBrowserType.Community)
                    {
                        SpriteBatch batch = KoiLibrary.SpriteBatch;
                        Matrix mat = Matrix.CreateTranslation(192, -8, 0);
                        mat = mat * worldMatrix * camera.ViewMatrix;

                        Texture2D icon = null;
                        Vector2 offset = Vector2.Zero;

                        // Downloaded level?
                        switch (level.DownloadState)
                        {
                            case LevelMetadata.DownloadStates.Queued:
                                // Do nothing, left over from slow Xbox days.
                                break;
                            case LevelMetadata.DownloadStates.InProgress:
                                icon = downloadInProgressTexture;
                                offset = new Vector2(0, 20.0f * (float)Math.Sin(Time.WallClockTotalSeconds));
                                break;
                            case LevelMetadata.DownloadStates.Complete:
                                icon = downloadCompleteTexture;
                                break;
                            case LevelMetadata.DownloadStates.Failed:
                                icon = downloadFailedTexture;
                                break;
                        }

                        // Render ReporetAbuse texture?
                        if (level.FlaggedByMe)
                        {
                            icon = reportAbuseTexture;
                        }

                        if (icon != null)
                        {
                            batch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, SamplerState.LinearClamp, DepthStencilState.None, RasterizerState.CullNone, null, mat);
                            batch.Draw(icon, offset, Color.White);
                            batch.End();
                        }
                    }
                }
            }   // end of Render()

            /// <summary>
            /// Set the orientation without a twitch.
            /// </summary>
            /// <param name="o"></param>
            public void SetOrientation(Transform o)
            {
                // If setting explicitly, create a new instance.  This avoids
                // having lingering twitches wrongly change the value.
                transform = new Transform(o);
                twitchTransform = new Transform(transform);
            }   // end of SetOrientation()

            /// <summary>
            /// Smoothly changes the orientation from the current values to the new.
            /// </summary>
            /// <param name="o"></param>
            public void TwitchOrientation(Transform o)
            {
                TwitchCurve.Shape shape = TwitchCurve.Shape.OvershootOut;
                float twitchTime = 0.25f;

                if (Level == null)
                    return;

                Transform cur = transform;

                // Rotation
                if (twitchTransform.rotation != o.rotation)
                {
                    twitchTransform.rotation = o.rotation;
                    TwitchManager.Set<float> set = delegate(float val, Object param) { cur.rotation = val; };
                    TwitchManager.CreateTwitch<float>(cur.rotation, o.rotation, set, twitchTime, shape);
                }
                // Scale
                if (twitchTransform.scale != o.scale)
                {
                    twitchTransform.scale = o.scale;
                    TwitchManager.Set<float> set = delegate(float val, Object param) { cur.scale = val; };
                    TwitchManager.CreateTwitch<float>(cur.scale, o.scale, set, twitchTime, shape);
                }
                // Position
                if (twitchTransform.position != o.position)
                {
                    twitchTransform.position = o.position;
                    TwitchManager.Set<Vector2> set = delegate(Vector2 val, Object param) { cur.position = val; };
                    TwitchManager.CreateTwitch<Vector2>(cur.position, o.position, set, twitchTime, shape);
                }

            }   // TwitchedOrientation()

            /// <summary>
            /// Render the level's thumbnail and title to the rt.
            /// Note, level may be null in which case just clear
            /// the rt to transparent.
            /// </summary>
            public void RenderToTexture()
            {
                GraphicsDevice device = KoiLibrary.GraphicsDevice;
                SpriteBatch batch = KoiLibrary.SpriteBatch;

                device.SetRenderTarget(rt);

                if (level == null)
                {
                    device.Clear(Color.Transparent);
                }
                else
                {
                    Texture2D thumbnail = level.Thumbnail.Texture;
                    // Render the thumbnail.
                    if (thumbnail != null)
                    {
                        // Figure out how much of the thumbnail to crop off since the tiles are square.
                        int crop = (thumbnail.Width - thumbnail.Height) / 2;

                        batch.Begin();
                        {
                            Rectangle srcRect = new Rectangle(-crop, 0, thumbnail.Width + crop, thumbnail.Height);
                            Rectangle dstRect = new Rectangle(0, 0, 256, 256);
                            batch.Draw(thumbnail, dstRect, srcRect, Color.White);
                        }
                        batch.End();
                    }

                    // Render the title.
                    {
                        const int kTextMargin = 10;
                        const int kTextPosY = 200;

                        string title = TextHelper.AddEllipsis(Font, Title, rt.Width - kTextMargin * 2);
                        Vector2 titleSize = Font().MeasureString(title);

                        // Render the title text.
                        batch.Begin();
                        {
                            TextHelper.DrawString(Font, title, new Vector2((rt.Width - titleSize.X) / 2f, kTextPosY), Color.White,
                                                    outlineColor: Color.Black, outlineWidth: 1.0f);
                        }
                        batch.End();
                    }
                }

                InGame.RestoreRenderTarget();

                rtDirty = false;

            }   // end of RenderToTexture()

            public override void RegisterForInputEvents()
            {
                base.RegisterForInputEvents();

                // Don't register for keyboard events since we don't have any sense of being in focus.
                KoiLibrary.InputEventManager.RegisterForEvent(this, InputEventManager.Event.Keyboard);

            }   // end of RegisterForInputEvents()

            #endregion

            #region InputEventHandler

            public override InputEventHandler HitTest(Vector2 hitLocation)
            {
                InputEventHandler result = null;

                // hitLocation is in fullSet coords.  LocalRect is in camera coords.

                // Transform hitLocation into camera coords (ie tile coords).
                Vector2 hit = Vector2.Transform(hitLocation, inverseWorldMatrix);

                if (LocalRect.Contains(hit))
                {
                    result = this;
                }

                return result;
            }   // end of HitTest()

            #endregion

            #region Internal

            public override void LoadContent()
            {
                if (DeviceResetX.NeedsLoad(rt))
                {
                    rt = new RenderTarget2D(KoiLibrary.GraphicsDevice, 256, 256);
                    rtDirty = true;
                }

                if(DeviceResetX.NeedsLoad(downloadInProgressTexture))
                {
                    downloadInProgressTexture = KoiLibrary.LoadTexture2D(@"Textures\LoadLevel\DownloadInProgress");
                }
                if(DeviceResetX.NeedsLoad(downloadCompleteTexture))
                {
                    downloadCompleteTexture = KoiLibrary.LoadTexture2D(@"Textures\LoadLevel\DownloadComplete");
                }
                if(DeviceResetX.NeedsLoad(downloadFailedTexture))
                {
                    downloadFailedTexture = KoiLibrary.LoadTexture2D(@"Textures\LoadLevel\DownloadFailed");
                }
                if(DeviceResetX.NeedsLoad(reportAbuseTexture))
                {
                    reportAbuseTexture = KoiLibrary.LoadTexture2D(@"Textures\LoadLevel\ReportAbuse");
                }
            }

            public override void UnloadContent()
            {
                DeviceResetX.Release(ref rt);
            }

            public override void DeviceResetHandler(object sender, EventArgs e)
            {
            }

            #endregion

        }   // end of class Tile


    }   // end of class BrowserWorldsDisplay

}   // end of namespace KoiX.UI
