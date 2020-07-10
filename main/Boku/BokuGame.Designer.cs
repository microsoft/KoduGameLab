// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.


using System;
using System.Diagnostics;

using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Audio;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using Microsoft.Xna.Framework.Storage;


using Boku.Common.Sharing;

namespace Boku
{
    partial class BokuGame
    {
        public static bool syncRefresh = false;
        public static PresentInterval presentInterval = PresentInterval.Two;

        /// <summary>
        /// Prevent the system from clearing the backbuffer each frame.
        /// </summary>
        public void PreparingDeviceSettingsHandler(object sender, EventArgs e)
        {
            PreparingDeviceSettingsEventArgs args = e as PreparingDeviceSettingsEventArgs;
            if (args != null)
            {
                Microsoft.Xna.Framework.GraphicsDeviceInformation info = args.GraphicsDeviceInformation;
                info.PresentationParameters.RenderTargetUsage = RenderTargetUsage.PlatformContents;

                /*
                // PresentInterval.Two only works in full screen mode.
                if (BokuSettings.Settings.FullScreen)
                {
                    info.PresentationParameters.PresentationInterval = presentInterval;
                }
                */

            }
        }

        /// <summary>
        /// Required method for Designer support - do not modify
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            //
            // Graphics 
            //

#if NETFX_CORE
            graphics = new Microsoft.Xna.Framework.GraphicsDeviceManager(this);

            graphics.PreparingDeviceSettings += PreparingDeviceSettingsHandler;
#endif

            // Determine if HiDef is supported.
            // Find the default adapter and check if it supports Reach and Hidef.
            // TODO (****) What do we do if Reach isn't supported???
            foreach (GraphicsAdapter ga in GraphicsAdapter.Adapters)
            {
#if NETFX_CORE
                Debug.Assert(false, "Waiting on MG");
                // Assume Reach for now.
                hwSupportsReach = true;
                break;
#else
                if (ga.IsDefaultAdapter)
                {
                    if (ga.IsProfileSupported(GraphicsProfile.Reach))
                    {
                        hwSupportsReach = true;
                    }
                    if (ga.IsProfileSupported(GraphicsProfile.HiDef))
                    {
                        hwSupportsHiDef = true;
                    }

                    break;
                }
#endif
            }

            // Set HiDef iff HW supports AND user doesn't prefer Reach.
            hidef = false;
            if (hwSupportsHiDef && !BokuSettings.Settings.PreferReach)
            {
                hidef = true;
            }
            else
            {
                BokuSettings.ConstrainToReach();
            }

            Debug.Assert(false, "Should we even be here?");
            
            // Select right profile.
            graphics.GraphicsProfile = BokuGame.HiDefProfile ? GraphicsProfile.HiDef : GraphicsProfile.Reach;

            //graphics.PreferredBackBufferFormat = SurfaceFormat.Color;
            graphics.PreferredDepthStencilFormat = DepthFormat.Depth24Stencil8;

#if NETFX_CORE
            // For Win8 always force fullscreen and use full device resolution.
            graphics.IsFullScreen = BokuSettings.Settings.FullScreen = true;
            BokuSettings.Settings.ResolutionX = graphics.PreferredBackBufferWidth;
            BokuSettings.Settings.ResolutionY = graphics.PreferredBackBufferHeight;
#endif

            // <<<<<<<<<<<<<<<<<<<<<<<<<<<<<< FULL SCREEN WINDOWED MODE FIX
            
#if !NETFX_CORE
            // Always start windowed.
            graphics.IsFullScreen = false;
#endif
            
            // FULL SCREEN WINDOWED MODE FIX >>>>>>>>>>>>>>>>>>>>>>>>>>>>>

            graphics.SynchronizeWithVerticalRetrace = syncRefresh;
            graphics.PreferMultiSampling = BokuSettings.Settings.AntiAlias;

            //
            // Game
            //
#if NETFX_CORE
            IsFixedTimeStep = false;
            Window.AllowUserResizing = false;
#endif
            IsMouseVisible = true;

        }   // end of BokuGame InitializeComponent()

#if NETFX_CORE
        static GraphicsDeviceManager graphics = null;
#else
        GraphicsDeviceManager graphics = null;
#endif

    }   // end of partial class BokuGame

}   // end of namespace Boku
