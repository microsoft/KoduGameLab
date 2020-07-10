// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.


// Uncomment this to have rendertarget allocation info printed to output.
//#define PRINT_RT_DEBUG

#if DEBUG
#define INSTRUMENT_RTS
#endif // DEBUG

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

using KoiX;
using KoiX.Text;
using KoiX.UI.Dialogs;

using Boku.Common;
using Boku.Common.Localization;


namespace KoiX
{
    /// <summary>
    /// A class with static instances of shared UI bits including renderTargets and fonts.
    /// You should never hold on to a reference to these since they may change with a 
    /// device reset.
    /// </summary>
    public class SharedX
    {
        #region Members

        static FontWrapper gameFont10;
        static FontWrapper gameFont13_5;
        static FontWrapper gameFont15_75;
        static FontWrapper gameFont18Bold;
        static FontWrapper gameFont20;
        static FontWrapper gameFont24;
        static FontWrapper gameFont24Bold;
        static FontWrapper gameFont30Bold;
        static FontWrapper gameFont42;
        static FontWrapper cardLabel;

        // Always use SpriteFonts for these.
        static SpriteFont gameFontLineNumbers = null;
        static SpriteFont segoeUI20 = null;
        static SpriteFont segoeUI24 = null;
        static SpriteFont segoeUI30 = null;

        static RenderTarget2D renderTargetDepthStencil1024_768 = null;
        static RenderTarget2D renderTargetDepthStencil1280_720 = null;
        static RenderTarget2D renderTarget1920_540 = null;      // Used by VirtualKeyboard.
        static RenderTarget2D renderTarget1024_768 = null;
        static RenderTarget2D renderTarget512_512 = null;       // Used by ModularMenus.
        static RenderTarget2D renderTarget512_302 = null;       // Used by Message Dialogs and ToolTips.
        static RenderTarget2D renderTarget256_256 = null;       // Used by thought balloons.
        static RenderTarget2D renderTarget128_128 = null;       // Used by CardSpace for tiles.
        static RenderTarget2D renderTarget64_64 = null;         // Used by reflex handles.

        static Texture2D whiteTexture = null;
        static Texture2D blackButtonTexture = null;
        static Texture2D upDownArrowsTexture = null;

        static BlendState blendStateColorWriteRGB = null;       // Write to RGB channels and NOT alpha.
        static RasterizerState rasterStateWireframe = null;     // Set rendering to wireframe.

        static IndexBuffer quadIndexBuffer = null;              // Index buffer to replace all the TriangleFan occurances.
        static short[] quadIndices = { 0, 1, 2, 0, 2, 3 };

        static TextDialog textDialog;                           // Shared text dialog.

        #endregion

        #region Accesssors

        /// <summary>
        /// Font used for most of the UI.
        /// </summary>
        static string UIFontName
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

                return fontName;
            }
        }

        /// <summary>
        /// Font used for labelling programming tiles.
        /// </summary>
        static string CardLabelFontName
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

        public static FontWrapper GameFont10
        {
            get
            {
                if (gameFont10 == null)
                {
                    gameFont10 = new FontWrapper();
#if !NETFX_CORE
                    gameFont10.systemFont = SysFont.GetSystemFont("Calibri", 10.0f, FontStyle.Regular);
#endif
                    gameFont10.spriteFont = KoiLibrary.LoadSpriteFont(@"Fonts\Calibri10");
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
                    gameFont13_5.systemFont = SysFont.GetSystemFont("Calibri", 13.5f, FontStyle.Regular);
#endif
                    gameFont13_5.spriteFont = KoiLibrary.LoadSpriteFont(@"Fonts\Calibri13_5");
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
                    gameFont15_75.systemFont = SysFont.GetSystemFont("Calibri", 15.75f, FontStyle.Regular);
#endif
                    gameFont15_75.spriteFont = KoiLibrary.LoadSpriteFont(@"Fonts\Calibri15_75");
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
                    gameFont18Bold.systemFont = SysFont.GetSystemFont("Calibri", 18.0f, FontStyle.Bold);
#endif
                    gameFont18Bold.spriteFont = KoiLibrary.LoadSpriteFont(@"Fonts\Calibri18Bold");
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
                    gameFont20.systemFont = SysFont.GetSystemFont("Calibri", 20.0f, FontStyle.Regular);
#endif
                    gameFont20.spriteFont = KoiLibrary.LoadSpriteFont(@"Fonts\Calibri20");
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
                    gameFont24.systemFont = SysFont.GetSystemFont("Calibri", 24.0f, FontStyle.Regular);
#endif
                    gameFont24.spriteFont = KoiLibrary.LoadSpriteFont(@"Fonts\Calibri24");
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
                    gameFont24Bold.systemFont = SysFont.GetSystemFont("Calibri", 24.0f, FontStyle.Bold);
#endif
                    gameFont24Bold.spriteFont = KoiLibrary.LoadSpriteFont(@"Fonts\Calibri24Bold");
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
                    gameFont30Bold.systemFont = SysFont.GetSystemFont("Calibri", 30.0f, FontStyle.Bold);
#endif
                    gameFont30Bold.spriteFont = KoiLibrary.LoadSpriteFont(@"Fonts\Calibri30Bold");
                }
                return gameFont30Bold;
            }
        }
        public static FontWrapper GameFont42
        {
            get
            {
                if (gameFont42 == null)
                {
                    gameFont42 = new FontWrapper();
#if !NETFX_CORE
                    gameFont42.systemFont = SysFont.GetSystemFont("Calibri", 42.0f, FontStyle.Regular);
#endif
                    gameFont42.spriteFont = KoiLibrary.LoadSpriteFont(@"Fonts\Calibri30Bold");
                }
                return gameFont42;
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
                    cardLabel.spriteFont = KoiLibrary.LoadSpriteFont(@"Fonts\CardLabel");
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
                    gameFontLineNumbers = KoiLibrary.LoadSpriteFont(@"Fonts\LineNumbers");
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
                    segoeUI20 = KoiLibrary.LoadSpriteFont(@"Fonts\SegoeUI20");
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
                    segoeUI24 = KoiLibrary.LoadSpriteFont(@"Fonts\SegoeUI24");
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
                    segoeUI30 = KoiLibrary.LoadSpriteFont(@"Fonts\SegoeUI30");
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
                        KoiLibrary.GraphicsDevice,
                        width, height, false,
                        SurfaceFormat.Color,
                        DepthFormat.Depth24Stencil8, 0,
                        RenderTargetUsage.PlatformContents);
                    SharedX.GetRT("UI2D.Shared:renderTargetDepthStencil1024_768", renderTargetDepthStencil1024_768);
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
                    int numSamples = Boku.BokuSettings.Settings.AntiAlias ? 8 : 1;
                    renderTargetDepthStencil1280_720 = new RenderTarget2D(
                        KoiLibrary.GraphicsDevice,
                        width, height, false,
                        SurfaceFormat.Color,
                        DepthFormat.Depth24Stencil8, numSamples,
                        RenderTargetUsage.PlatformContents);
                    SharedX.GetRT("UI2D.Shared:renderTargetDepthStencil1280_720", renderTargetDepthStencil1280_720);
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
                        KoiLibrary.GraphicsDevice,
                        width, height, false,
                        SurfaceFormat.Color,
                        DepthFormat.None, 0,
                        RenderTargetUsage.PlatformContents);
                    SharedX.GetRT("UI2D.Shared:renderTarget1920_540", renderTarget1920_540);
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
                        KoiLibrary.GraphicsDevice,
                        width, height, false,
                        SurfaceFormat.Color,
                        DepthFormat.None, 0,
                        RenderTargetUsage.PlatformContents);
                    SharedX.GetRT("UI2D.Shared:renderTarget1024_768", renderTarget1024_768);
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
                        KoiLibrary.GraphicsDevice,
                        width, height, false,
                        SurfaceFormat.Color,
                        DepthFormat.None, 0,
                        RenderTargetUsage.PlatformContents);
                    SharedX.GetRT("UI2D.Shared:renderTarget512_512", renderTarget512_512);
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
                        KoiLibrary.GraphicsDevice,
                        width, height, false,
                        SurfaceFormat.Color,
                        DepthFormat.None, 0,
                        RenderTargetUsage.PlatformContents);
                    SharedX.GetRT("UI2D.Shared:renderTarget512_302", renderTarget512_302);
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
                        KoiLibrary.GraphicsDevice,
                        width, height, false,
                        SurfaceFormat.Color,
                        DepthFormat.None, 0,
                        RenderTargetUsage.PlatformContents);
                    SharedX.GetRT("UI2D.Shared:renderTarget256_256", renderTarget256_256);
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
                        KoiLibrary.GraphicsDevice,
                        width, height, false,
                        SurfaceFormat.Color,
                        DepthFormat.None, 0,
                        RenderTargetUsage.PlatformContents);
                    SharedX.GetRT("UI2D.Shared:renderTarget128_128", renderTarget128_128);
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
                        KoiLibrary.GraphicsDevice,
                        width, height, false,
                        SurfaceFormat.Color,
                        DepthFormat.None, 0,
                        RenderTargetUsage.PlatformContents);
                    SharedX.GetRT("UI2D.Shared:renderTarget64_64", renderTarget64_64);
                }
                return renderTarget64_64;
            }
        }

        public static Texture2D WhiteTexture
        {
            get
            {
                if (whiteTexture == null || whiteTexture.IsDisposed || whiteTexture.GraphicsDevice.IsDisposed)
                {
                    whiteTexture = KoiLibrary.LoadTexture2D(@"KoiXContent\Textures\White");
                }

                return whiteTexture;
            }
        }

        public static Texture2D BlackButtonTexture
        {
            get
            {
                if (blackButtonTexture == null || blackButtonTexture.IsDisposed || blackButtonTexture.GraphicsDevice.IsDisposed)
                {
                    blackButtonTexture = KoiLibrary.LoadTexture2D(@"Textures\GridElements\BlackTextTile");
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
                    upDownArrowsTexture = KoiLibrary.LoadTexture2D(@"Textures\HelpCard\UpDownArrows");
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
                    quadIndexBuffer = new IndexBuffer(KoiLibrary.GraphicsDevice, IndexElementSize.SixteenBits, 6, BufferUsage.WriteOnly);
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

        /// <summary>
        /// Shared TextDialog useful for anyplace that a blob of text needs to be handled.
        /// Use:  
        ///     First, be sure it's not already active.  If it is you may have a case where
        ///     you either need to create a second dialog OR (better) release the previous usage
        ///     and restore it if needed.
        /// </summary>
        public static TextDialog TextDialog
        {
            get
            {
                if (textDialog == null)
                {
                    textDialog = new TextDialog(titleText: "Title", bodyText: "This space intentionally left blank.");
                }

                return textDialog;
            }
        }

        #endregion

        #region Public

        /// <summary>
        /// These delegates are used to provide a level of indirection for objects that
        /// need to hold onto a reference to a font.  Since a device reset may change the
        /// underlying font we instead give the objects a delegate which returns the 
        /// correct font.
        /// </summary>
        public static GetFont GetGameFont10 = delegate() { return GameFont10; };
        public static GetFont GetGameFont13_5 = delegate() { return GameFont13_5; };
        public static GetFont GetGameFont15_75 = delegate() { return GameFont15_75; };
        public static GetFont GetGameFont18Bold = delegate() { return GameFont18Bold; };
        public static GetFont GetGameFont20 = delegate() { return GameFont20; };
        public static GetFont GetGameFont24 = delegate() { return GameFont24; };
        public static GetFont GetGameFont24Bold = delegate() { return GameFont24Bold; };
        public static GetFont GetGameFont30Bold = delegate() { return GameFont30Bold; };
        public static GetFont GetGameFont42 = delegate() { return GameFont42; };
        public static GetFont GetCardLabel = delegate() { return CardLabel; };

        public static GetSpriteFont GetGameFontLineNumbers = delegate() { return GameFontLineNumbers; };
        public static GetSpriteFont GetSegoeUI20 = delegate() { return SegoeUI20; };
        public static GetSpriteFont GetSegoeUI24 = delegate() { return SegoeUI24; };
        public static GetSpriteFont GetSegoeUI30 = delegate() { return SegoeUI30; };

        #endregion

        #region Internal

        public static void InitDeviceResources(GraphicsDevice device)
        {
            segoeUI20 = KoiLibrary.LoadSpriteFont(@"Fonts\SegoeUI20");
            segoeUI24 = KoiLibrary.LoadSpriteFont(@"Fonts\SegoeUI24");
            segoeUI30 = KoiLibrary.LoadSpriteFont(@"Fonts\SegoeUI30");

            CreateRenderTargets(device);

            if (whiteTexture == null)
            {
                whiteTexture = KoiLibrary.LoadTexture2D(@"KoiXContent\Textures\White");
            }

            if (blackButtonTexture == null)
            {
                blackButtonTexture = KoiLibrary.LoadTexture2D(@"Textures\GridElements\BlackTextTile");
            }

            if (upDownArrowsTexture == null)
            {
                upDownArrowsTexture = KoiLibrary.LoadTexture2D(@"Textures\HelpCard\UpDownArrows");
            }


        } // end of InitDeviceResources()

        public static void LoadContent(bool immediate)
        {
        }   // end of LoadContent()

        public static void UnloadContent()
        {
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

            DeviceResetX.Release(ref whiteTexture);
            DeviceResetX.Release(ref blackButtonTexture);
            DeviceResetX.Release(ref upDownArrowsTexture);

            DeviceResetX.Release(ref blendStateColorWriteRGB);
            DeviceResetX.Release(ref rasterStateWireframe);

            DeviceResetX.Release(ref quadIndexBuffer);

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
        }

        static void CreateRenderTargets(GraphicsDevice device)
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
            SharedX.GetRT("UI2D.Shared:renderTargetDepth1024_768", renderTargetDepthStencil1024_768);

            width = 1280;
            height = 720;
            if (renderTargetDepthStencil1280_720 == null)
            {
                int numSamples = Boku.BokuSettings.Settings.AntiAlias ? 8 : 1;
#if NETFX_CORE
                numSamples = 0;
#endif
                renderTargetDepthStencil1280_720 = new RenderTarget2D(
                    device,
                    width, height, false,
                    SurfaceFormat.Color,
                    DepthFormat.Depth24Stencil8, numSamples,
                    RenderTargetUsage.PlatformContents);
                SharedX.GetRT("UI2D.Shared:renderTargetDepthStencil1280_720", renderTargetDepthStencil1280_720);
            }

            width = 1920;
            height = 540;
            if (renderTarget1920_540 == null)
            {
                renderTarget1920_540 = new RenderTarget2D(
                    KoiLibrary.GraphicsDevice,
                    width, height, false,
                    SurfaceFormat.Color,
                    DepthFormat.None, 0,
                    RenderTargetUsage.PlatformContents);
                SharedX.GetRT("UI2D.Shared:renderTarget1920_540", renderTarget1920_540);
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
            SharedX.GetRT("UI2D.Shared:renderTarget1024_768", renderTarget1024_768);

            width = 512;
            height = 512;
            renderTarget512_512 = new RenderTarget2D(
                device,
                width, height, false,
                SurfaceFormat.Color,
                DepthFormat.None, 0,
                RenderTargetUsage.PlatformContents);
            SharedX.GetRT("UI2D.Shared:renderTarget512_512", renderTarget512_512);

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
                SharedX.GetRT("UI2D.Shared:renderTarget512_302", renderTarget512_302);
            }

            width = 256;
            height = 256;
            renderTarget256_256 = new RenderTarget2D(
                device,
                width, height, false,
                SurfaceFormat.Color,
                DepthFormat.None, 0,
                RenderTargetUsage.PlatformContents);
            SharedX.GetRT("UI2D.Shared:renderTarget256_256", renderTarget256_256);

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
                SharedX.GetRT("UI2D.Shared:renderTarget128_128", renderTarget128_128);
            }

            width = 64;
            height = 64;
            renderTarget64_64 = new RenderTarget2D(
                device,
                width, height, false,
                SurfaceFormat.Color,
                DepthFormat.None, 0,
                RenderTargetUsage.PlatformContents);
            SharedX.GetRT("UI2D.Shared:renderTarget64_64", renderTarget64_64);

        }

        static void ReleaseRenderTargets()
        {
            SharedX.RelRT("UI2D.Shared:renderTargetDepthStencil1280_720", renderTargetDepthStencil1280_720);
            SharedX.RelRT("UI2D.Shared:renderTargetDepthStencil1024_768", renderTargetDepthStencil1024_768);
            SharedX.RelRT("UI2D.Shared:renderTarget512_512", renderTarget512_512);
            SharedX.RelRT("UI2D.Shared:renderTarget512_302", renderTarget512_302);
            SharedX.RelRT("UI2D.Shared:renderTarget256_256", renderTarget256_256);
            SharedX.RelRT("UI2D.Shared:renderTarget128_128", renderTarget128_128);
            SharedX.RelRT("UI2D.Shared:renderTarget64_64", renderTarget64_64);
            DeviceResetX.Release(ref renderTargetDepthStencil1280_720);
            DeviceResetX.Release(ref renderTarget1920_540);
            DeviceResetX.Release(ref renderTarget1024_768);
            DeviceResetX.Release(ref renderTarget512_512);
            DeviceResetX.Release(ref renderTarget512_302);
            DeviceResetX.Release(ref renderTarget256_256);
            DeviceResetX.Release(ref renderTarget128_128);
            DeviceResetX.Release(ref renderTarget64_64);

        }   // endof UnloadContent()

        #endregion

#if INSTRUMENT_RTS
        /// <summary>
        /// Record the creation of a w x h rendertarget, with label.
        /// </summary>
        /// <param name="label"></param>
        /// <param name="w"></param>
        /// <param name="h"></param>
        /// <returns></returns>
        public static void GetRT(string label, int w, int h)
        {
            totalRT += w * h * 4;
            if (rtUsages.ContainsKey(label))
            {
                rtUsages[label] += w * h * 4;
            }
            else
            {
                rtUsages.Add(label, w * h * 4);
            }
        }
        /// <summary>
        /// Record the creation of renderTarg with label.
        /// </summary>
        /// <param name="label"></param>
        /// <param name="renderTarg"></param>
        /// <returns></returns>
        public static void GetRT(string label, RenderTarget2D renderTarg)
        {
            if (renderTarg != null)
            {
                GetRT(label, renderTarg.Width, renderTarg.Height);
            }
        }
        /// <summary>
        /// Record the release of an w x h rendertarget, with label.
        /// </summary>
        /// <param name="label"></param>
        /// <param name="w"></param>
        /// <param name="h"></param>
        /// <returns></returns>
        public static void RelRT(string label, int w, int h)
        {
            totalRT -= w * h * 4;
            //Debug.WriteLine("RELEASING RT VVV " + w * h * 4 + " B : " + label);
            rtUsages[label] -= w * h * 4;
        }
        /// <summary>
        /// Record the release of renderTarg with label.
        /// </summary>
        /// <param name="label"></param>
        /// <param name="renderTarg"></param>
        /// <returns></returns>
        public static void RelRT(string label, RenderTarget2D renderTarg)
        {
            if (renderTarg != null)
            {
                RelRT(label, renderTarg.Width, renderTarg.Height);
            }
        }
        /// <summary>
        /// Dump current memory usages for all render targets.
        /// </summary>
        public static void DumpRT()
        {
#if PRINT_RT_DEBUG
            Debug.WriteLine("VVVVVVVVVVVVVVVVVVVVVVVVVVV");
            foreach (KeyValuePair<string, Int64> pair in rtUsages)
            {
                Debug.WriteLine(pair.Key + ": " + pair.Value);
            }
            Debug.WriteLine("AAAAAAAAAAAAAAAAAAAAAAAAAAA");
            Debug.WriteLine("VVVVVVVVVVVVVVVVVVVVVVVVVVV");
            Debug.WriteLine("Total: " + totalRT + " Bytes");
            Debug.WriteLine("AAAAAAAAAAAAAAAAAAAAAAAAAAA");
#endif
        }
        private static Int64 totalRT = 0;
        private static Dictionary<string, Int64> rtUsages = new Dictionary<string, Int64>();
#else // INSTRUMENT_RTS
        /// <summary>
        /// Noop, see define INSTRUMENT_RTS
        /// </summary>
        /// <param name="label"></param>
        /// <param name="w"></param>
        /// <param name="h"></param>
        /// <returns></returns>
        public static void GetRT(string label, int w, int h)
        {
        }
        /// <summary>
        /// Noop, see define INSTRUMENT_RTS
        /// </summary>
        /// <param name="label"></param>
        /// <param name="renderTarg"></param>
        /// <returns></returns>
        public static void GetRT(string label, RenderTarget2D renderTarg)
        {
        }
        /// <summary>
        /// Noop, see define INSTRUMENT_RTS
        /// </summary>
        /// <param name="label"></param>
        /// <param name="w"></param>
        /// <param name="h"></param>
        /// <returns></returns>
        public static void RelRT(string label, int w, int h)
        {
        }
        /// <summary>
        /// Noop, see define INSTRUMENT_RTS
        /// </summary>
        /// <param name="label"></param>
        /// <param name="renderTarg"></param>
        /// <returns></returns>
        public static void RelRT(string label, RenderTarget2D renderTarg)
        {
        }
        /// <summary>
        /// Noop, see define INSTRUMENT_RTS
        /// </summary>
        public static void DumpRT()
        {
        }
#endif // INSTRUMENT_RTS


    }   // end of class Shared

}   // end of namespace KoiX
