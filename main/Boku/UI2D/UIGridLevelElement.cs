
using System;
using System.Text;
using System.Collections;
using System.Diagnostics;

using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Audio;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using Microsoft.Xna.Framework.Storage;

using KoiX;
using KoiX.Text;

using Boku.Common;
using Boku.Common.Sharing;
using Boku.Base;
using Boku.Fx;

namespace Boku.UI2D
{
    /// <summary>
    /// Internal class to hold hard-coded tile orientations.
    /// </summary>
    public class Orientation
    {
        public Vector3 position;
        public float scale;
        public float rotation;

        // c'tors
        public Orientation()
        {
            position = Vector3.Zero;
            scale = 1.0f;
            rotation = 0.0f;
        }

        public Orientation(Vector3 position, float scale, float rotation)
        {
            this.position = position;
            this.scale = scale;
            this.rotation = rotation;
        }

        public Orientation(Orientation o)
        {
            this.position = o.position;
            this.scale = o.scale;
            this.rotation = o.rotation;
        }
    }

    /// <summary>
    /// An instance of UIElement that uses a 9-grid element for its geometry.
    /// This one is specialized for displaying tiles for the load level menu.
    /// 
    /// The tiles display the thumbnail from the level with the title 
    /// overlayed at the bottom of the tile.  If the title is too long
    /// to fit it should fade out at the end to indicate this.
    /// </summary>
    public class UIGridLevelElement : UIGridElement
    {
        #region Members

        private static Texture2D downloadQueuedIcon;
        private static Texture2D downloadInProgressIcon;
        private static Texture2D downloadCompleteIcon;
        private static Texture2D downloadFailedIcon;
        private static Texture2D levelSharedIcon;
        private static Texture2D levelSharedAlphaMap;
        private static Texture2D levelTileShadow;
        private static Texture2D levelTileAlphaMap;
        private static Texture2D downloadQueuedAlphaMap;
        private static Texture2D downloadCompleteAlphaMap;
        private static Texture2D downloadInProgressAlphaMap;
        private static Texture2D downloadFailedAlphaMap;
        private static Texture2D statusIconShadow;
        private static Texture2D favoriteBox;
        private static Texture2D reportAbuse;

        private static Effect effect = null;

        private RenderTarget2D rt = null;
        private Orientation orientation = new Orientation();        // Actual values used for display.
        private Orientation twitchOrientation = new Orientation();  // Values we're twitching toward.

        // Properties for the underlying 9-grid geometry.
        private float width;
        private float height;

        private bool selected = false;

        private Vector4 specularColor = new Vector4(1.0f, 1.0f, 1.0f, 1.0f);
        private float specularPower = 16.0f;

        private Color textColor;

        private LevelMetadata level;
        private LevelMetadata.DownloadStates prevDownloadState = LevelMetadata.DownloadStates.None;
        private float downloadStateIconAlpha;
        private float downloadInProgressAnimOffset;

        const float kDownloadInProgressIconOffset = -0.2f;
        const float kDownloadInProgressIconTwitchTime = 0.35f;
        const float kDownloadStateIconAlphaTwitchTime = 0.15f;

        #endregion

        #region Accessors

        public Orientation Orientation
        {
            get { return orientation; }
        }

        public override bool Selected
        {
            get { return selected; }
            set { selected = value; }
        }
        public Color SpecularColor
        {
            set { specularColor = value.ToVector4(); }
        }
        public float SpecularPower
        {
            set { specularPower = value; }
        }
        public Color TextColor
        {
            get { return textColor; }
        }
        public float Width
        {
            get { return width; }
        }
        public float Height
        {
            get { return height; }
        }
        public Texture2D Thumbnail
        {
            get
            {
                if (Level != null)
                    return Level.Thumbnail.Texture;
                else
                    return null;
            }
        }
        public string Title
        {
            get
            {
                if (Level != null)
                    return TextHelper.FilterInvalidCharacters(Level.Name);
                else
                    return String.Empty;
            }
        }
        public LevelMetadata Level
        {
            get { return level; }
            set { level = value; dirty = true; }
        }

        public override Vector2 Size
        {
            get { return new Vector2(width, height); }
            set { /* do nothing, should be removed from base class */ }
        }

        #endregion

        #region Public

        // c'tor
        /// <summary>
        /// Simple c'tor using a blob to hold the common data.
        /// </summary>
        /// <param name="blob"></param>
        /// <param name="label"></param>
        public UIGridLevelElement(ParamBlob blob)
        {
            this.width = blob.width;
            this.height = blob.height;

            this.Font = blob.Font;
            this.textColor = blob.textColor;
        }

        /// <summary>
        /// Set the orientation without a twitch.
        /// </summary>
        /// <param name="o"></param>
        public void SetOrientation(Orientation o)
        {
            // If setting explicitly, create a new instance.  This avoids
            // having lingering twitches wrongly change the value.
            orientation = new Orientation(o);
            twitchOrientation = new Orientation(orientation);
        }   // end of SetOrientation()

        /// <summary>
        /// Smoothly changes the orientation from the current values to the new.
        /// </summary>
        /// <param name="o"></param>
        public void TwitchOrientation(Orientation o)
        {
            TwitchCurve.Shape shape = TwitchCurve.Shape.OvershootOut;
            float twitchTime = 0.25f;

            if (Level == null)
                return;

            Orientation cur = orientation;

            // Rotation
            if (twitchOrientation.rotation != o.rotation)
            {
                twitchOrientation.rotation = o.rotation;
                TwitchManager.Set<float> set = delegate(float val, Object param) { cur.rotation = val; };
                TwitchManager.CreateTwitch<float>(cur.rotation, o.rotation, set, twitchTime, shape);
            }
            // Scale
            if (twitchOrientation.scale != o.scale)
            {
                twitchOrientation.scale = o.scale;
                TwitchManager.Set<float> set = delegate(float val, Object param) { cur.scale = val; };
                TwitchManager.CreateTwitch<float>(cur.scale, o.scale, set, twitchTime, shape);
            }
            // Position
            if (twitchOrientation.position != o.position)
            {
                twitchOrientation.position = o.position;
                TwitchManager.Set<Vector3> set = delegate(Vector3 val, Object param) { cur.position = val; };
                TwitchManager.CreateTwitch<Vector3>(cur.position, o.position, set, twitchTime, shape);
            }

        }   // TwitchedOrientation()


        public void RenderToTexture()
        {
            dirty = false;

            GraphicsDevice device = KoiLibrary.GraphicsDevice;

            InGame.SetRenderTarget(rt);
            InGame.Clear(Color.Black);

            ScreenSpaceQuad quad = ScreenSpaceQuad.GetInstance();

            // Render the thumbnail.
            if (Thumbnail != null)
            {
                // Figure out how much of the thumbnail to crop off since the tiles are square.
                int crop = (Thumbnail.Width - Thumbnail.Height) / 2;

                try
                {
                    quad.Render(Thumbnail, new Vector2(-crop, 0.0f), new Vector2(rt.Width + crop, rt.Height), @"TexturedNoAlpha");
                }
                catch 
                {
                    // At this point what has probably happened is that the thumbnail data has been lost or corrupted
                    // so we need to force it to reload and then set the dirty flag on this element so the rt is redone.

                    // Note:  for now setting dirty to true is commented out since it won't cause the thumbnail texture to
                    // reload and just causes perf to die.  Probably related to bug #2221

                    //dirty = true;
                }
            }

            // Render the title.
            {
                const int kTextMargin = 10;
                const int kTextPosY = 150;

                string title = TextHelper.AddEllipsis(Font, Title, rt.Width - kTextMargin * 2);
                Vector2 titleSize = Font().MeasureString(title);

                // Render the title background.
                quad.Render(
                    new Vector4(0, 0, 0, 0.25f), 
                    new Vector2(0, kTextPosY), 
                    new Vector2(rt.Width, titleSize.Y));

                // Render the title text.
                SpriteBatch batch = KoiLibrary.SpriteBatch;
                batch.Begin();
                TextHelper.DrawString(Font, title, new Vector2((rt.Width - titleSize.X) / 2f, kTextPosY), textColor);
                batch.End();
            }

            InGame.RestoreRenderTarget();

        }   // end of UIGridLevelElement RenderToTexture()

        public override void Update(ref Matrix parentMatrix)
        {
            if (dirty || rt.IsContentLost)
            {
                RenderToTexture();
            }

            // Calc new local matrix from orientation instead of calling base.Update().
            localMatrix = Matrix.CreateRotationZ(orientation.rotation) * Matrix.CreateScale(orientation.scale);
            localMatrix.Translation = orientation.position;

            worldMatrix = localMatrix * parentMatrix;

        }   // end of UIGridLevelElement Update()

        private void DownloadStateChanged()
        {
            StartDownloadStateIconAlphaTwitch(null);
            MaybeRestartDownloadInProgressTwitch(null);
        }

        private void LevelSharedChanged()
        {
            StartDownloadStateIconAlphaTwitch(null);
        }

        private void StartDownloadStateIconAlphaTwitch(object unused)
        {
            downloadStateIconAlpha = 0;
            TwitchManager.Set<float> set = delegate(float value, object param) { downloadStateIconAlpha = value; };
            TwitchManager.CreateTwitch(downloadStateIconAlpha, 1, set, kDownloadStateIconAlphaTwitchTime, TwitchCurve.Shape.Linear);
        }

        private void MaybeRestartDownloadInProgressTwitch(object unused)
        {
            downloadInProgressAnimOffset = kDownloadInProgressIconOffset;
            if (Level != null && Level.DownloadState == LevelMetadata.DownloadStates.InProgress)
            {
                TwitchManager.Set<float> set = delegate(float value, object param) { downloadInProgressAnimOffset = value; };
                TwitchManager.CreateTwitch(downloadInProgressAnimOffset, 0, set, kDownloadInProgressIconTwitchTime, TwitchCurve.Shape.EaseInOut, null, StartDownloadInProgressTwitchBack);
            }
        }

        private void StartDownloadInProgressTwitchBack(object unused)
        {
            TwitchManager.Set<float> set = delegate(float value, object param) { downloadInProgressAnimOffset = value; };
            TwitchManager.CreateTwitch(downloadInProgressAnimOffset, kDownloadInProgressIconOffset, set, kDownloadInProgressIconTwitchTime, TwitchCurve.Shape.EaseInOut, null, MaybeRestartDownloadInProgressTwitch);
        }

        public override void HandleMouseInput(Vector2 hitUV)
        {
        }   // end of HandleMouseInput()


        public override void HandleTouchInput(TouchContact touch, Vector2 hitUV)
        {
        }   // end of HandleTouchInput()


        public override void Render(Camera camera)
        {
            if (rt != null && level != null && !dirty)
            {
                // We do this check here to ensure the download state icon's alpha is reset if needed before being rendered.
                // When this check is performed in Update, we see a fully opaque icon for one frame due to order of operations.
                if (Level != null)
                {
                    if (Level.DownloadState != prevDownloadState)
                        DownloadStateChanged();
                    prevDownloadState = Level.DownloadState;
                }

                try
                {
                    SimpleTexturedQuad quad = SimpleTexturedQuad.GetInstance();

                    // Render thumbnail drop shadow.
                    {
                        float s = Scale;
                        Matrix shadowMatrix =
                            Matrix.CreateScale(3.8f + 0.25f * s * s) *
                            Matrix.CreateTranslation(0, -0.2f * s, 0) *
                            worldMatrix;
                        quad.Render(camera, levelTileShadow, ref shadowMatrix, 0.6f);
                    }

                    // Render the favorite box
                    if (0 != (level.Genres & BokuShared.Genres.Favorite))
                    {
                        Matrix tileMatrix = Matrix.CreateScale(2.13f) * worldMatrix;
                        quad.Render(camera, favoriteBox, ref tileMatrix, levelTileAlphaMap);
                    }

                    // Render the thumbnail.
                    {
                        Matrix tileMatrix = Matrix.CreateScale(2f) * worldMatrix;
                        quad.Render(camera, rt, ref tileMatrix, levelTileAlphaMap);
                    }

                    if (Level != null)
                    {
                        const float kDownloadStatusX = 0.9f;
                        const float kDownloadStatusY = 0.8f;

                        if (Level.FlaggedByMe)
                        {
                            Matrix m = Matrix.CreateTranslation(kDownloadStatusX, kDownloadStatusY, 0) * worldMatrix;
                            quad.Render(camera, reportAbuse, ref m, 1.0f);
                        }
                        else
                        {
                            switch (Level.DownloadState)
                            {
                                case LevelMetadata.DownloadStates.Queued:
                                    {
                                        Matrix s = Matrix.CreateTranslation(kDownloadStatusX, kDownloadStatusY - 0.1f, 0) * worldMatrix;
                                        quad.Render(camera, statusIconShadow, ref s, 0.75f * downloadStateIconAlpha);
                                        Matrix m = Matrix.CreateTranslation(kDownloadStatusX, kDownloadStatusY, 0) * worldMatrix;
                                        quad.Render(camera, downloadQueuedIcon, ref m, downloadQueuedAlphaMap, downloadStateIconAlpha);
                                    }
                                    break;

                                case LevelMetadata.DownloadStates.InProgress:
                                    {
                                        Matrix s = Matrix.CreateTranslation(kDownloadStatusX, kDownloadStatusY - downloadInProgressAnimOffset - 0.1f, 0) * worldMatrix;
                                        quad.Render(camera, statusIconShadow, ref s, 0.75f * downloadStateIconAlpha);
                                        Matrix m = Matrix.CreateTranslation(kDownloadStatusX, kDownloadStatusY - downloadInProgressAnimOffset, 0) * worldMatrix;
                                        quad.Render(camera, downloadInProgressIcon, ref m, downloadInProgressAlphaMap, downloadStateIconAlpha);
                                    }
                                    break;

                                case LevelMetadata.DownloadStates.Failed:
                                    {
                                        Matrix s = Matrix.CreateTranslation(kDownloadStatusX, kDownloadStatusY - 0.1f, 0) * worldMatrix;
                                        quad.Render(camera, statusIconShadow, ref s, 0.75f * downloadStateIconAlpha);
                                        Matrix m = Matrix.CreateTranslation(kDownloadStatusX, kDownloadStatusY, 0) * worldMatrix;
                                        quad.Render(camera, downloadFailedIcon, ref m, downloadFailedAlphaMap, downloadStateIconAlpha);
                                    }
                                    break;

                                case LevelMetadata.DownloadStates.Complete:
                                    {
                                        Matrix s = Matrix.CreateTranslation(kDownloadStatusX, kDownloadStatusY - 0.1f, 0) * worldMatrix;
                                        quad.Render(camera, statusIconShadow, ref s, 0.75f * downloadStateIconAlpha);
                                        Matrix m = Matrix.CreateTranslation(kDownloadStatusX, kDownloadStatusY, 0) * worldMatrix;
                                        quad.Render(camera, downloadCompleteIcon, ref m, downloadCompleteAlphaMap, downloadStateIconAlpha);
                                    }
                                    break;
                            }
                        }
                    }
                }
                catch
                {
                    // This will end up here if the rt call above throws an exception
                    // because it thinks that the rendertarget is still tied to the device.  This
                    // only occurs in some device-reset situations.  If we do nothing here then
                    // everything is ok next frame.
                }
            }
        }   // end of UIGridLevelElement Render()

        #endregion

        #region Internal

        public override void LoadContent(bool immediate)
        {
            // Init the effect if it's not already done.
            if (effect == null)
            {
                effect = KoiLibrary.LoadEffect(@"Shaders\UI2D");
                ShaderGlobals.RegisterEffect("UI2D", effect);
            }

            // Load the shadow texture.
            if (levelTileShadow == null)
            {
                levelTileShadow = KoiLibrary.LoadTexture2D(@"Textures\LoadLevel\TileShadow");
            }

            if (downloadQueuedIcon == null)
            {
                downloadQueuedIcon = KoiLibrary.LoadTexture2D(@"Textures\LoadLevel\DownloadQueued");
            }

            if (downloadInProgressIcon == null)
            {
                downloadInProgressIcon = KoiLibrary.LoadTexture2D(@"Textures\LoadLevel\DownloadInProgress");
            }

            if (downloadCompleteIcon == null)
            {
                downloadCompleteIcon = KoiLibrary.LoadTexture2D(@"Textures\LoadLevel\DownloadComplete");
            }

            if (downloadFailedIcon == null)
            {
                downloadFailedIcon = KoiLibrary.LoadTexture2D(@"Textures\LoadLevel\DownloadFailed");
            }

            if (levelSharedIcon == null)
            {
                levelSharedIcon = KoiLibrary.LoadTexture2D(@"Textures\LoadLevel\SharedLevel");
            }

            if (levelSharedAlphaMap == null)
            {
                levelSharedAlphaMap = KoiLibrary.LoadTexture2D(@"Textures\LoadLevel\SharedLevelAlphaMap");
            }

            if (levelTileAlphaMap == null)
            {
                levelTileAlphaMap = KoiLibrary.LoadTexture2D(@"Textures\LoadLevel\TileAlphaMap");
            }

            if (downloadQueuedAlphaMap == null)
            {
                downloadQueuedAlphaMap = KoiLibrary.LoadTexture2D(@"Textures\LoadLevel\DownloadQueuedAlphaMap");
            }

            if (downloadCompleteAlphaMap == null)
            {
                downloadCompleteAlphaMap = KoiLibrary.LoadTexture2D(@"Textures\LoadLevel\DownloadCompleteAlphaMap");
            }

            if (downloadInProgressAlphaMap == null)
            {
                downloadInProgressAlphaMap = KoiLibrary.LoadTexture2D(@"Textures\LoadLevel\DownloadInProgressAlphaMap");
            }

            if (downloadFailedAlphaMap == null)
            {
                downloadFailedAlphaMap = KoiLibrary.LoadTexture2D(@"Textures\LoadLevel\DownloadFailedAlphaMap");
            }

            if (statusIconShadow == null)
            {
                statusIconShadow = KoiLibrary.LoadTexture2D(@"Textures\LoadLevel\StatusIconShadow");
            }

            if (favoriteBox == null)
            {
                favoriteBox = KoiLibrary.LoadTexture2D(@"Textures\LoadLevel\FavoriteBox");
            }

            if (reportAbuse == null)
            {
                reportAbuse = KoiLibrary.LoadTexture2D(@"Textures\LoadLevel\ReportAbuse");
            }
        }

        public override void InitDeviceResources(GraphicsDevice device)
        {
            CreateRenderTargets(device);
        }

        public override void UnloadContent()
        {
            base.UnloadContent();

            DeviceResetX.Release(ref effect);

            ReleaseRenderTargets();

            DeviceResetX.Release(ref downloadQueuedIcon);
            DeviceResetX.Release(ref downloadInProgressIcon);
            DeviceResetX.Release(ref downloadCompleteIcon);
            DeviceResetX.Release(ref downloadFailedIcon);
            DeviceResetX.Release(ref levelSharedIcon);
            DeviceResetX.Release(ref levelSharedAlphaMap);
            DeviceResetX.Release(ref levelTileShadow);
            DeviceResetX.Release(ref levelTileAlphaMap);
            DeviceResetX.Release(ref downloadCompleteAlphaMap);
            DeviceResetX.Release(ref downloadInProgressAlphaMap);
            DeviceResetX.Release(ref downloadQueuedAlphaMap);
            DeviceResetX.Release(ref downloadFailedAlphaMap);
            DeviceResetX.Release(ref statusIconShadow);
            DeviceResetX.Release(ref favoriteBox);
            DeviceResetX.Release(ref reportAbuse);

        }   // end of UIGridLevelElement UnloadContent()

        /// <summary>
        /// Recreate render targets
        /// </summary>
        /// <param name="graphics"></param>
        public override void DeviceReset(GraphicsDevice device)
        {
            ReleaseRenderTargets();
            CreateRenderTargets(device);
        }

        private void CreateRenderTargets(GraphicsDevice device)
        {
            const int dpi = 96;
            int w = (int)(dpi * 2.0f);
            int h = (int)(dpi * 2.0f);

            rt = new RenderTarget2D(device, w, h, false, SurfaceFormat.Color, DepthFormat.None);
            SharedX.GetRT("UIGridLevelElement", rt);

            dirty = true;   // Ensure a refresh of the texture.
        }

        private void ReleaseRenderTargets()
        {
            SharedX.GetRT("UIGridLevelElement", rt);
            DeviceResetX.Release(ref rt);
        }

        #endregion

    }   // end of class UIGridLevelElement

}   // end of namespace Boku.UI2D






