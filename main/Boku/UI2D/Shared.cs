// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.


using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
#if !NETFX_CORE
using System.Drawing;
#endif

using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Audio;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using Microsoft.Xna.Framework.Storage;

using Boku.Common;
using Boku.Common.Localization;

namespace Boku.UI2D
{
    /// <summary>
    /// A class with static instances of shared UI bits including renderTargets and fonts.
    /// You should never hold on to a reference to these since they may change with a 
    /// device reset.
    /// </summary>
    public class Shared
    {
        #region Members

        private static SpriteBatch batch = null;

        private static FontWrapper gameFont10;
        private static FontWrapper gameFont13_5;
        private static FontWrapper gameFont15_75;
        private static FontWrapper gameFont18Bold;
        private static FontWrapper gameFont20;
        private static FontWrapper gameFont24;
        private static FontWrapper gameFont24Bold;
        private static FontWrapper gameFont30Bold;
        private static FontWrapper cardLabel;

        // Always use SpriteFonts for these.
        private static SpriteFont gameFontLineNumbers = null;
        private static SpriteFont segoeUI20 = null;
        private static SpriteFont segoeUI24 = null;
        private static SpriteFont segoeUI30 = null;

        private static RenderTarget2D renderTargetDepthStencil1024_768 = null;
        private static RenderTarget2D renderTargetDepthStencil1280_720 = null;
        private static RenderTarget2D renderTarget1920_540 = null;      // Used by VirtualKeyboard.
        private static RenderTarget2D renderTarget1024_768 = null;
        private static RenderTarget2D renderTarget512_512 = null;       // Used by ModularMenus.
        private static RenderTarget2D renderTarget512_302 = null;       // Used by Message Dialogs and ToolTips.
        private static RenderTarget2D renderTarget256_256 = null;       // Used by thought balloons.
        private static RenderTarget2D renderTarget128_128 = null;       // Used by CardSpace for tiles.
        private static RenderTarget2D renderTarget64_64 = null;         // Used by reflex handles.

        private static Texture2D blackButtonTexture = null;
        private static Texture2D upDownArrowsTexture = null;

        private static BlendState blendStateColorWriteRGB = null;       // Write to RGB channels and NOT alpha.
        private static RasterizerState rasterStateWireframe = null;     // Set rendering to wireframe.

        private static IndexBuffer quadIndexBuffer = null;              // Index buffer to replace all the TriangleFan occurances.
        private static short[] quadIndices = { 0, 1, 2, 0, 2, 3 };

        #endregion

        #region Accesssors

        /// <summary>
        /// Font used for most of the UI.
        /// </summary>
        private static string UIFontName
        {
            get
            {
                string fontName = "Calibri";

                // Apply any language-specific swaps.
                if (Localizer.LocalLanguage.StartsWith("ZH", StringComparison.OrdinalIgnoreCase))
                {
                    fontName = "Microsoft JhengHei";
                }
                else if (Localizer.LocalLanguage.StartsWith("KO", StringComparison.OrdinalIgnoreCase))
                {
                    fontName = "Malgeun Gothic";
                }
                else if (Localizer.LocalLanguage.StartsWith("AR", StringComparison.OrdinalIgnoreCase))
                {
                    fontName = "Arial";
                }


                return fontName;
            }
        }

        /// <summary>
        /// Font used for labelling programming tiles.
        /// </summary>
        private static string CardLabelFontName
        {
            get
            {
                string fontName = "Arial";

                // Apply any language-specific swaps.
                if (Localizer.LocalLanguage.StartsWith("ZH", StringComparison.OrdinalIgnoreCase))
                {
                    fontName = "Microsoft JhengHei";
                }
                else if (Localizer.LocalLanguage.StartsWith("KO", StringComparison.OrdinalIgnoreCase))
                {
                    fontName = "Malgeun Gothic";
                }

                return fontName;
            }
        }

        /// <summary>
        /// Note:  Do not hold on to this reference for more than the
        /// current frame since a device reset could invalidate it.
        /// </summary>
        public static SpriteBatch SpriteBatch
        {
            get
            {
                if (batch == null || batch.IsDisposed || batch.GraphicsDevice.IsDisposed)
                {
                    batch = new SpriteBatch(BokuGame.bokuGame.GraphicsDevice);
                }
                return batch;
            }
            set { batch = value; }
        }

        public static FontWrapper GameFont10
        {
            get
            {
                if (gameFont10 == null)
                {
                    gameFont10 = new FontWrapper();
#if !NETFX_CORE
                    gameFont10.systemFont = SysFont.GetSystemFont(UIFontName, 10.0f, FontStyle.Regular);
#endif
                    gameFont10.spriteFont = BokuGame.Load<SpriteFont>(BokuGame.Settings.MediaPath + @"Fonts\Calibri10");
                }
                return gameFont10;
            }
        }
        public static FontWrapper GameFont13_5
        {
            get
            {
                if (gameFont13_5 == null)
                {
                    gameFont13_5 = new FontWrapper();
#if !NETFX_CORE
                    gameFont13_5.systemFont = SysFont.GetSystemFont(UIFontName, 13.5f, FontStyle.Regular);
#endif
                    gameFont13_5.spriteFont = BokuGame.Load<SpriteFont>(BokuGame.Settings.MediaPath + @"Fonts\Calibri13_5");
                }
                return gameFont13_5;
            }
        }
        public static FontWrapper GameFont15_75
        {
            get
            {
                if (gameFont15_75 == null)
                {
                    gameFont15_75 = new FontWrapper();
#if !NETFX_CORE
                    gameFont15_75.systemFont = SysFont.GetSystemFont(UIFontName, 15.75f, FontStyle.Regular);
#endif
                    gameFont15_75.spriteFont = BokuGame.Load<SpriteFont>(BokuGame.Settings.MediaPath + @"Fonts\Calibri15_75");
                }
                return gameFont15_75;
            }
        }
        public static FontWrapper GameFont18Bold
        {
            get
            {
                if (gameFont18Bold == null)
                {
                    gameFont18Bold = new FontWrapper();
#if !NETFX_CORE
                    gameFont18Bold.systemFont = SysFont.GetSystemFont(UIFontName, 18.0f, FontStyle.Bold);
#endif
                    gameFont18Bold.spriteFont = BokuGame.Load<SpriteFont>(BokuGame.Settings.MediaPath + @"Fonts\Calibri18Bold");
                }
                return gameFont18Bold;
            }
        }
        public static FontWrapper GameFont20
        {
            get
            {
                if (gameFont20 == null)
                {
                    gameFont20 = new FontWrapper();
#if !NETFX_CORE
                    gameFont20.systemFont = SysFont.GetSystemFont(UIFontName, 20.0f, FontStyle.Regular);
#endif
                    gameFont20.spriteFont = BokuGame.Load<SpriteFont>(BokuGame.Settings.MediaPath + @"Fonts\Calibri20");
                }
                return gameFont20;
            }
        }
        public static FontWrapper GameFont24
        {
            get
            {
                if (gameFont24 == null)
                {
                    gameFont24 = new FontWrapper();
#if !NETFX_CORE
                    gameFont24.systemFont = SysFont.GetSystemFont(UIFontName, 24.0f, FontStyle.Regular);
#endif
                    gameFont24.spriteFont = BokuGame.Load<SpriteFont>(BokuGame.Settings.MediaPath + @"Fonts\Calibri24");
                }
                return gameFont24;
            }
        }
        public static FontWrapper GameFont24Bold
        {
            get
            {
                if (gameFont24Bold == null)
                {
                    gameFont24Bold = new FontWrapper();
#if !NETFX_CORE
                    gameFont24Bold.systemFont = SysFont.GetSystemFont(UIFontName, 24.0f, FontStyle.Bold);
#endif
                    gameFont24Bold.spriteFont = BokuGame.Load<SpriteFont>(BokuGame.Settings.MediaPath + @"Fonts\Calibri24Bold");
                }
                return gameFont24Bold;
            }
        }
        public static FontWrapper GameFont30Bold
        {
            get
            {
                if (gameFont30Bold == null)
                {
                    gameFont30Bold = new FontWrapper();
#if !NETFX_CORE
                    gameFont30Bold.systemFont = SysFont.GetSystemFont(UIFontName, 30.0f, FontStyle.Bold);
#endif
                    gameFont30Bold.spriteFont = BokuGame.Load<SpriteFont>(BokuGame.Settings.MediaPath + @"Fonts\Calibri30Bold");
                }
                return gameFont30Bold;
            }
        }
        public static FontWrapper CardLabel
        {
            get
            {
                if (cardLabel == null)
                {
                    cardLabel = new FontWrapper();
#if !NETFX_CORE
                    cardLabel.systemFont = SysFont.GetSystemFont("Arial", 20.0f, FontStyle.Bold);
#endif
                    cardLabel.spriteFont = BokuGame.Load<SpriteFont>(BokuGame.Settings.MediaPath + @"Fonts\CardLabel");
                }
                return cardLabel;
            }
        }

        /// <summary>
        /// Note:  Do not hold on to this reference for more than the
        /// current frame since a device reset could invalidate it.
        /// </summary>
        public static SpriteFont GameFontLineNumbers
        {
            get
            {
                if (gameFontLineNumbers == null)
                {
                    gameFontLineNumbers = BokuGame.Load<SpriteFont>(BokuGame.Settings.MediaPath + @"Fonts\LineNumbers");
                }
                return gameFontLineNumbers;
            }
        }


        /// <summary>
        /// Note:  Do not hold on to this reference for more than the
        /// current frame since a device reset could invalidate it.
        /// </summary>
        public static SpriteFont SegoeUI20
        {
            get
            {
                if (segoeUI20 == null)
                {
                    segoeUI20 = BokuGame.Load<SpriteFont>(BokuGame.Settings.MediaPath + @"Fonts\SegoeUI20");
                }
                return segoeUI20;
            }
        }
        /// <summary>
        /// Note:  Do not hold on to this reference for more than the
        /// current frame since a device reset could invalidate it.
        /// </summary>
        public static SpriteFont SegoeUI24
        {
            get
            {
                if (segoeUI24 == null)
                {
                    segoeUI24 = BokuGame.Load<SpriteFont>(BokuGame.Settings.MediaPath + @"Fonts\SegoeUI24");
                }
                return segoeUI24;
            }
        }
        /// <summary>
        /// Note:  Do not hold on to this reference for more than the
        /// current frame since a device reset could invalidate it.
        /// </summary>
        public static SpriteFont SegoeUI30
        {
            get
            {
                if (segoeUI30 == null)
                {
                    segoeUI30 = BokuGame.Load<SpriteFont>(BokuGame.Settings.MediaPath + @"Fonts\SegoeUI30");
                }
                return segoeUI30;
            }
        }

        /// <summary>
        /// Note:  Do not hold on to this reference for more than the
        /// current frame since a device reset could invalidate it.
        /// </summary>
        public static RenderTarget2D RenderTargetDepthStencil1024_768
        {
            get
            {
                if (renderTargetDepthStencil1024_768 == null || renderTargetDepthStencil1024_768.IsDisposed || renderTargetDepthStencil1024_768.GraphicsDevice.IsDisposed)
                {
                    int width = 1024;
                    int height = 768;
                    renderTargetDepthStencil1024_768 = new RenderTarget2D(
                        BokuGame.bokuGame.GraphicsDevice,
                        width, height, false,
                        SurfaceFormat.Color, 
                        DepthFormat.Depth24Stencil8, 0, 
                        RenderTargetUsage.PlatformContents);
                    InGame.GetRT("UI2D.Shared:renderTargetDepthStencil1024_768", renderTargetDepthStencil1024_768);
                }
                return renderTargetDepthStencil1024_768;
            }
        }
        /// <summary>
        /// Note:  Do not hold on to this reference for more than the
        /// current frame since a device reset could invalidate it.
        /// </summary>
        public static RenderTarget2D RenderTargetDepthStencil1280_720
        {
            get
            {
                if (renderTargetDepthStencil1280_720 == null || renderTargetDepthStencil1280_720.IsDisposed || renderTargetDepthStencil1280_720.GraphicsDevice.IsDisposed)
                {
                    int width = 1280;
                    int height = 720;
                    int numSamples = BokuSettings.Settings.AntiAlias ? 8 : 1;
                    renderTargetDepthStencil1280_720 = new RenderTarget2D(
                        BokuGame.bokuGame.GraphicsDevice,
                        width, height, false,
                        SurfaceFormat.Color,
                        DepthFormat.Depth24Stencil8, numSamples,
                        RenderTargetUsage.PlatformContents);
                    InGame.GetRT("UI2D.Shared:renderTargetDepthStencil1280_720", renderTargetDepthStencil1280_720);
                }
                return renderTargetDepthStencil1280_720;
            }
        }
        /// <summary>
        /// Note:  Do not hold on to this reference for more than the
        /// current frame since a device reset could invalidate it.
        /// </summary>
        public static RenderTarget2D RenderTarget1920_540
        {
            get
            {
                if (renderTarget1920_540 == null || renderTarget1920_540.IsDisposed || renderTarget1920_540.GraphicsDevice.IsDisposed)
                {
                    int width = 1920;
                    int height = 540;
                    renderTarget1920_540 = new RenderTarget2D(
                        BokuGame.bokuGame.GraphicsDevice,
                        width, height, false,
                        SurfaceFormat.Color,
                        DepthFormat.None, 0,
                        RenderTargetUsage.PlatformContents);
                    InGame.GetRT("UI2D.Shared:renderTarget1920_540", renderTarget1920_540);
                }
                return renderTarget1920_540;
            }
        }
        /// <summary>
        /// Note:  Do not hold on to this reference for more than the
        /// current frame since a device reset could invalidate it.
        /// </summary>
        public static RenderTarget2D RenderTarget1024_768
        {
            get 
            {
                if (renderTarget1024_768 == null || renderTarget1024_768.IsDisposed || renderTarget1024_768.GraphicsDevice.IsDisposed)
                {
                    int width = 1024;
                    int height = 768;
                    renderTarget1024_768 = new RenderTarget2D(
                        BokuGame.bokuGame.GraphicsDevice,
                        width, height, false,
                        SurfaceFormat.Color,
                        DepthFormat.None, 0,
                        RenderTargetUsage.PlatformContents);
                    InGame.GetRT("UI2D.Shared:renderTarget1024_768", renderTarget1024_768);
                }
                return renderTarget1024_768; 
            }
        }
        /// <summary>
        /// Note:  Do not hold on to this reference for more than the
        /// current frame since a device reset could invalidate it.
        /// </summary>
        public static RenderTarget2D RenderTarget512_512
        {
            get
            {
                if (renderTarget512_512 == null || renderTarget512_512.IsDisposed || renderTarget512_512.GraphicsDevice.IsDisposed)
                {
                    int width = 512;
                    int height = 512;
                    renderTarget512_512 = new RenderTarget2D(
                        BokuGame.bokuGame.GraphicsDevice,
                        width, height, false,
                        SurfaceFormat.Color,
                        DepthFormat.None, 0,
                        RenderTargetUsage.PlatformContents);
                    InGame.GetRT("UI2D.Shared:renderTarget512_512", renderTarget512_512);
                }
                return renderTarget512_512;
            }
        }
        /// <summary>
        /// Note:  Do not hold on to this reference for more than the
        /// current frame since a device reset could invalidate it.
        /// </summary>
        public static RenderTarget2D RenderTarget512_302
        {
            get 
            {
                if (renderTarget512_302 == null || renderTarget512_302.IsDisposed || renderTarget512_302.GraphicsDevice.IsDisposed)
                {
                    int width = 512;
                    int height = 302;
                    renderTarget512_302 = new RenderTarget2D(
                        BokuGame.bokuGame.GraphicsDevice,
                        width, height, false,
                        SurfaceFormat.Color,
                        DepthFormat.None, 0,
                        RenderTargetUsage.PlatformContents);
                    InGame.GetRT("UI2D.Shared:renderTarget512_302", renderTarget512_302);
                }
                return renderTarget512_302; 
            }
        }
        /// <summary>
        /// Note:  Do not hold on to this reference for more than the
        /// current frame since a device reset could invalidate it.
        /// </summary>
        public static RenderTarget2D RenderTarget256_256
        {
            get
            {
                if (renderTarget256_256 == null || renderTarget256_256.IsDisposed || renderTarget256_256.GraphicsDevice.IsDisposed)
                {
                    int width = 256;
                    int height = 256;
                    renderTarget256_256 = new RenderTarget2D(
                        BokuGame.bokuGame.GraphicsDevice,
                        width, height, false,
                        SurfaceFormat.Color,
                        DepthFormat.None, 0,
                        RenderTargetUsage.PlatformContents);
                    InGame.GetRT("UI2D.Shared:renderTarget256_256", renderTarget256_256);
                }
                return renderTarget256_256;
            }
        }
        /// Note:  Do not hold on to this reference for more than the
        /// current frame since a device reset could invalidate it.
        /// </summary>
        public static RenderTarget2D RenderTarget128_128
        {
            get
            {
                if (renderTarget128_128 == null || renderTarget128_128.IsDisposed || renderTarget128_128.GraphicsDevice.IsDisposed)
                {
                    int width = 128;
                    int height = 128;
                    renderTarget128_128 = new RenderTarget2D(
                        BokuGame.bokuGame.GraphicsDevice,
                        width, height, false,
                        SurfaceFormat.Color,
                        DepthFormat.None, 0,
                        RenderTargetUsage.PlatformContents);
                    InGame.GetRT("UI2D.Shared:renderTarget128_128", renderTarget128_128);
                }
                return renderTarget128_128;
            }
        }
        /// <summary>
        /// Note:  Do not hold on to this reference for more than the
        /// current frame since a device reset could invalidate it.
        /// </summary>
        public static RenderTarget2D RenderTarget64_64
        {
            get
            {
                if (renderTarget64_64 == null || renderTarget64_64.IsDisposed || renderTarget64_64.GraphicsDevice.IsDisposed)
                {
                    int width = 64;
                    int height = 64;
                    renderTarget64_64 = new RenderTarget2D(
                        BokuGame.bokuGame.GraphicsDevice,
                        width, height, false,
                        SurfaceFormat.Color,
                        DepthFormat.None, 0,
                        RenderTargetUsage.PlatformContents);
                    InGame.GetRT("UI2D.Shared:renderTarget64_64", renderTarget64_64);
                }
                return renderTarget64_64;
            }
        }

        public static Texture2D BlackButtonTexture
        {
            get 
            {
                if (blackButtonTexture == null || blackButtonTexture.IsDisposed || blackButtonTexture.GraphicsDevice.IsDisposed)
                {
                    blackButtonTexture = BokuGame.Load<Texture2D>(BokuGame.Settings.MediaPath + @"Textures\GridElements\BlackTextTile");
                }

                return blackButtonTexture; 
            }
        }

        public static Texture2D UpDownArrowsTexture
        {
            get 
            {
                if (upDownArrowsTexture == null || upDownArrowsTexture.IsDisposed || upDownArrowsTexture.GraphicsDevice.IsDisposed)
                {
                    upDownArrowsTexture = BokuGame.Load<Texture2D>(BokuGame.Settings.MediaPath + @"Textures\HelpCard\UpDownArrows");
                }

                return upDownArrowsTexture; 
            }
        }

        /// <summary>
        /// BlendState which prevents rendering to the alpha channel.
        /// Note this also assumes normal alpha blending.
        /// </summary>
        public static BlendState BlendStateColorWriteRGB
        {
            get
            {
                if (blendStateColorWriteRGB == null || blendStateColorWriteRGB.IsDisposed || blendStateColorWriteRGB.GraphicsDevice == null || blendStateColorWriteRGB.GraphicsDevice.IsDisposed)
                {
                    blendStateColorWriteRGB = new BlendState();
                    blendStateColorWriteRGB.ColorWriteChannels = ColorWriteChannels.Red | ColorWriteChannels.Green | ColorWriteChannels.Blue;
                    // Set for normal alpha.
                    blendStateColorWriteRGB.AlphaSourceBlend = Blend.SourceAlpha;
                    blendStateColorWriteRGB.AlphaDestinationBlend = Blend.InverseSourceAlpha;
                    blendStateColorWriteRGB.ColorSourceBlend = Blend.SourceAlpha;
                    blendStateColorWriteRGB.ColorDestinationBlend = Blend.InverseSourceAlpha;
                }

                return blendStateColorWriteRGB;
            }
        }

        /// <summary>
        /// RasterState for wireframe rendering.
        /// </summary>
        public static RasterizerState RasterStateWireframe
        {
            get
            {
                if (rasterStateWireframe == null || rasterStateWireframe.IsDisposed || rasterStateWireframe.GraphicsDevice.IsDisposed)
                {
                    rasterStateWireframe = new RasterizerState();
                    rasterStateWireframe.FillMode = FillMode.WireFrame;
                    rasterStateWireframe.CullMode = CullMode.None;
                }

                return rasterStateWireframe;
            }
        }

        /// <summary>
        /// Index buffer to be used when converting old code which used triangle fans.
        /// </summary>
        public static IndexBuffer QuadIndexBuff
        {
            get
            {
                if (quadIndexBuffer == null || quadIndexBuffer.IsDisposed || quadIndexBuffer.GraphicsDevice.IsDisposed)
                {
                    quadIndexBuffer = new IndexBuffer(BokuGame.bokuGame.GraphicsDevice, IndexElementSize.SixteenBits, 6, BufferUsage.WriteOnly);
                    quadIndexBuffer.SetData<short>(quadIndices);
                }

                return quadIndexBuffer;
            }
        }

        /// <summary>
        /// Indices to be used with DrawUserIndexedPrimitives()
        /// </summary>
        public static short[] QuadIndices
        {
            get { return quadIndices; }
        }


        #endregion

        #region Public

        public delegate FontWrapper GetFont();
        public delegate SpriteFont GetSpriteFont();

        /// <summary>
        /// These delegates are used to provide a level of indirection for objects that
        /// need to hold onto a reference to a font.  Since a device reset may change the
        /// underlying font we instead give the objects a delegate which returns the 
        /// correct font.
        /// </summary>
        public static GetFont GetGameFont10 = delegate() { return UI2D.Shared.GameFont10; };
        public static GetFont GetGameFont13_5 = delegate() { return UI2D.Shared.GameFont13_5; };
        public static GetFont GetGameFont15_75 = delegate() { return UI2D.Shared.GameFont15_75; };
        public static GetFont GetGameFont18Bold = delegate() { return UI2D.Shared.GameFont18Bold; };
        public static GetFont GetGameFont20 = delegate() { return UI2D.Shared.GameFont20; };
        public static GetFont GetGameFont24 = delegate() { return UI2D.Shared.GameFont24; };
        public static GetFont GetGameFont24Bold = delegate() { return UI2D.Shared.GameFont24Bold; };
        public static GetFont GetGameFont30Bold = delegate() { return UI2D.Shared.GameFont30Bold; };
        public static GetFont GetCardLabel = delegate() { return UI2D.Shared.CardLabel; };

        public static GetSpriteFont GetGameFontLineNumbers = delegate() { return UI2D.Shared.GameFontLineNumbers; };
        public static GetSpriteFont GetSegoeUI20 = delegate() { return UI2D.Shared.SegoeUI20; };
        public static GetSpriteFont GetSegoeUI24 = delegate() { return UI2D.Shared.SegoeUI24; };
        public static GetSpriteFont GetSegoeUI30 = delegate() { return UI2D.Shared.SegoeUI30; };

        #endregion

        #region Internal

        public static void InitDeviceResources(GraphicsDevice device)
        {
            batch = new SpriteBatch(device);

            segoeUI20 = BokuGame.Load<SpriteFont>(BokuGame.Settings.MediaPath + @"Fonts\SegoeUI20");
            segoeUI24 = BokuGame.Load<SpriteFont>(BokuGame.Settings.MediaPath + @"Fonts\SegoeUI24");
            segoeUI30 = BokuGame.Load<SpriteFont>(BokuGame.Settings.MediaPath + @"Fonts\SegoeUI30");

            CreateRenderTargets(device);

            if (blackButtonTexture == null)
            {
                blackButtonTexture = BokuGame.Load<Texture2D>(BokuGame.Settings.MediaPath + @"Textures\GridElements\BlackTextTile");
            }

            if (upDownArrowsTexture == null)
            {
                upDownArrowsTexture = BokuGame.Load<Texture2D>(BokuGame.Settings.MediaPath + @"Textures\HelpCard\UpDownArrows");
            }


        } // end of InitDeviceResources()

        public static void LoadContent(bool immediate)
        {
        }   // end of LoadContent()

        public static void UnloadContent()
        {
            BokuGame.Release(ref batch);

            gameFontLineNumbers = null;
            gameFont10 = null;
            gameFont13_5 = null;
            gameFont15_75 = null;
            gameFont18Bold = null;
            gameFont20 = null;
            gameFont24 = null;
            gameFont24Bold = null;
            gameFont30Bold = null;
            cardLabel = null;

            segoeUI20 = null;
            segoeUI24 = null;
            segoeUI30 = null;

            ReleaseRenderTargets();

            BokuGame.Release(ref blackButtonTexture);
            BokuGame.Release(ref upDownArrowsTexture);

            BokuGame.Release(ref blendStateColorWriteRGB);
            BokuGame.Release(ref rasterStateWireframe);

            BokuGame.Release(ref quadIndexBuffer);

        }

        /// <summary>
        /// Recreate render targets.
        /// </summary>
        public static void DeviceReset(GraphicsDevice device)
        {
            /*
            ReleaseRenderTargets();
            CreateRenderTargets(device);
            */

            BokuGame.Release(ref batch);
            batch = new SpriteBatch(device);
        }

        private static void CreateRenderTargets(GraphicsDevice device)
        {
            int width = 1024;
            int height = 768;
            renderTarget1024_768 = new RenderTarget2D(
                device,
                width, height, false,
                SurfaceFormat.Color,
                DepthFormat.Depth24Stencil8,
                0,
                RenderTargetUsage.PlatformContents);
            InGame.GetRT("UI2D.Shared:renderTargetDepth1024_768", renderTargetDepthStencil1024_768);

            width = 1280;
            height = 720;
            if (renderTargetDepthStencil1280_720 == null)
            {
                int numSamples = BokuSettings.Settings.AntiAlias ? 8 : 1;
#if NETFX_CORE
                numSamples = 0;
#endif
                renderTargetDepthStencil1280_720 = new RenderTarget2D(
                    device,
                    width, height, false,
                    SurfaceFormat.Color,
                    DepthFormat.Depth24Stencil8, numSamples,
                    RenderTargetUsage.PlatformContents);
                InGame.GetRT("UI2D.Shared:renderTargetDepthStencil1280_720", renderTargetDepthStencil1280_720);
            }

            width = 1920;
            height = 540;
            if (renderTarget1920_540 == null)
            {
                renderTarget1920_540 = new RenderTarget2D(
                    BokuGame.bokuGame.GraphicsDevice,
                    width, height, false,
                    SurfaceFormat.Color,
                    DepthFormat.None, 0,
                    RenderTargetUsage.PlatformContents);
                InGame.GetRT("UI2D.Shared:renderTarget1920_540", renderTarget1920_540);
            }

            width = 1024;
            height = 768;
            renderTarget1024_768 = new RenderTarget2D(
                device,
                width, height, false,
                SurfaceFormat.Color,
                DepthFormat.None,
                0,
                RenderTargetUsage.PlatformContents);
            InGame.GetRT("UI2D.Shared:renderTarget1024_768", renderTarget1024_768);

            width = 512;
            height = 512;
            renderTarget512_512 = new RenderTarget2D(
                device,
                width, height, false,
                SurfaceFormat.Color,
                DepthFormat.None, 0,
                RenderTargetUsage.PlatformContents);
            InGame.GetRT("UI2D.Shared:renderTarget512_512", renderTarget512_512);

            if (renderTarget512_302 == null)
            {
                width = 512;
                height = 302;
                renderTarget512_302 = new RenderTarget2D(
                    device,
                    width, height, false,
                    SurfaceFormat.Color,
                    DepthFormat.None, 0,
                    RenderTargetUsage.PlatformContents);
                InGame.GetRT("UI2D.Shared:renderTarget512_302", renderTarget512_302);
            }

            width = 256;
            height = 256;
            renderTarget256_256 = new RenderTarget2D(
                device,
                width, height, false,
                SurfaceFormat.Color,
                DepthFormat.None, 0,
                RenderTargetUsage.PlatformContents);
            InGame.GetRT("UI2D.Shared:renderTarget256_256", renderTarget256_256);

            if (renderTarget128_128 == null)
            {
                width = 128;
                height = 128;
                renderTarget128_128 = new RenderTarget2D(
                    device,
                    width, height, false,
                    SurfaceFormat.Color,
                    DepthFormat.None, 0,
                    RenderTargetUsage.PlatformContents);
                InGame.GetRT("UI2D.Shared:renderTarget128_128", renderTarget128_128);
            }

            width = 64;
            height = 64;
            renderTarget64_64 = new RenderTarget2D(
                device,
                width, height, false,
                SurfaceFormat.Color,
                DepthFormat.None, 0,
                RenderTargetUsage.PlatformContents);
            InGame.GetRT("UI2D.Shared:renderTarget64_64", renderTarget64_64);

        }

        private static void ReleaseRenderTargets()
        {
            InGame.RelRT("UI2D.Shared:renderTargetDepthStencil1280_720", renderTargetDepthStencil1280_720);
            InGame.RelRT("UI2D.Shared:renderTargetDepthStencil1024_768", renderTargetDepthStencil1024_768);
            InGame.RelRT("UI2D.Shared:renderTarget512_512", renderTarget512_512);
            InGame.RelRT("UI2D.Shared:renderTarget512_302", renderTarget512_302);
            InGame.RelRT("UI2D.Shared:renderTarget256_256", renderTarget256_256);
            InGame.RelRT("UI2D.Shared:renderTarget128_128", renderTarget128_128);
            InGame.RelRT("UI2D.Shared:renderTarget64_64", renderTarget64_64);
            BokuGame.Release(ref renderTargetDepthStencil1280_720);
            BokuGame.Release(ref renderTarget1920_540);
            BokuGame.Release(ref renderTarget1024_768);
            BokuGame.Release(ref renderTarget512_512);
            BokuGame.Release(ref renderTarget512_302);
            BokuGame.Release(ref renderTarget256_256);
            BokuGame.Release(ref renderTarget128_128);
            BokuGame.Release(ref renderTarget64_64);

        }   // endof UnloadContent()

        #endregion

    }   // end of class Shared

}   // end of namespace Boku.UI2D
