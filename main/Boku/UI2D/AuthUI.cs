// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.


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

using Boku.Base;
using Boku.Common;
using Boku.Fx;
using Boku.UI2D;
using Boku.Input;

using BokuShared;

namespace Boku.UI2D
{
    /// <summary>
    /// Static class for managing the UI dialogs related to auth.
    /// </summary>
    public static class AuthUI
    {
        public static Color FocusColor = new Color(50, 255, 50);    // Rave Green.
        public static Color ErrorColor = new Color(255, 255, 0);    // Yellow.

        #region Members

        private static AuthStatusDialog statusDialog;
        private static AuthSignInDialog signInDialog;
        private static AuthSignOutDialog signOutDialog;

        #endregion

        #region Accessors

        /// <summary>
        /// Is there a model dialog active.  If so, everything
        /// else should not look for input.
        /// </summary>
        public static bool IsModalActive
        {
            get { return !statusDialog.Active && (signInDialog.Active || signOutDialog.Active); }
        }

        #endregion

        #region Public

        public static void Update()
        {
            // Update is ignored if not active.
            AuthUI.statusDialog.Update();
            AuthUI.signInDialog.Update();
            AuthUI.signOutDialog.Update();
        }   // end of Update()

        public static void Render()
        {
            // Render is ignored if not active.
            AuthUI.statusDialog.Render();
            AuthUI.signInDialog.Render();
            AuthUI.signOutDialog.Render();
        }   // end of Render()

        public static void Init()
        {
            AuthUI.statusDialog = new AuthStatusDialog();
            AuthUI.signInDialog = new AuthSignInDialog();
            AuthUI.signOutDialog = new AuthSignOutDialog();
        }   // end of Init()

        /// <summary>
        /// Starts displaying the status dialog.  Ignored if one of
        /// the other dialogs are active.
        /// Note this is set up to be called every frame without
        /// problem.  For instance it can be called in the MainMenu
        /// update loop to make sure that the status dialog is always
        /// displayed except when the signInDialog is up.
        /// </summary>
        public static void ShowStatusDialog()
        {
            if (!AuthUI.statusDialog.Active && !AuthUI.signInDialog.Active && !AuthUI.signOutDialog.Active)
            {
                AuthUI.statusDialog.Active = true;
            }
        }   // end of ShowStatusDialog()

        /// <summary>
        /// Hides the status dialog, for instance when we go into
        /// RunSim mode.  Note that this is set up to be called 
        /// every frame without problem.
        /// </summary>
        public static void HideStatusDialog()
        {
            if (AuthUI.statusDialog.Active)
            {
                AuthUI.statusDialog.Active = false;
            }
        }   // end of HideStatusDialog()

        /// <summary>
        /// Starts displaying the ShowSignInDialog.
        /// Hides the StatusDialog.
        /// </summary>
        public static void ShowSignInDialog()
        {
            Debug.Assert(!AuthUI.signInDialog.Active, "Already active, should we be calling this again?  Not sure...");

            if (!AuthUI.signInDialog.Active)
            {
                // Disable other dialogs.
                HideAllDialogs();

                // Enable this.
                AuthUI.signInDialog.Active = true;
            }
        }   // end of ShowSignInDialog()

        /// <summary>
        /// Should only be called when a user clicks on status dialog.
        /// </summary>
        public static void ShowSignOutDialog()
        {
            Debug.Assert(!AuthUI.signOutDialog.Active, "Already active, should we be calling this again?  Not sure...");

            // If not signed in, must be guest so go straight to the sign in dialog.
            if (!Auth.IsSignedIn)
            {
                ShowSignInDialog();
                return;
            }

            if (!AuthUI.signOutDialog.Active)
            {
                // Disable other dialogs.
                HideAllDialogs();

                // Enable this.
                AuthUI.signOutDialog.Active = true;
            }
        }   // end of ShowSignOutDialog()

        /// <summary>
        /// Shortcut to disable all Auth dialogs.
        /// </summary>
        public static void HideAllDialogs()
        {
            AuthUI.statusDialog.Active = false;
            AuthUI.signInDialog.Active = false;
            AuthUI.signOutDialog.Active = false;
        }

        /// <summary>
        /// Renders the texture to a given rect.  Tries to dice it up so
        /// that it doesn't distort the edges.
        /// </summary>
        /// <param name="texture"></param>
        /// <param name="rect"></param>
        public static void RenderTile(Texture2D texture, Rectangle rect)
        {
            RenderTile(texture, rect, Color.White);
        }

        /// <summary>
        /// Renders the texture to a given rect.  Tries to dice it up so
        /// that it doesn't distort the edges.
        /// </summary>
        /// <param name="texture"></param>
        /// <param name="rect"></param>
        /// <param name="color">Attenuate tile with this color.</param>
        public static void RenderTile(Texture2D texture, Rectangle rect, Color color)
        {
            SpriteBatch batch = UI2D.Shared.SpriteBatch;

            // If either dimension of the rect is smaller than the texture 
            // we need to scale everything down to prevent distortion.
            // But we want to clamp scaling at 1.
            Vector2 scale = new Vector2(Math.Min(rect.Width / (float)texture.Width, 1.0f), Math.Min(rect.Height / (float)texture.Height, 1.0f));
            float minScale = Math.Min(scale.X, scale.Y);

            Rectangle srcRect;
            Rectangle dstRect;
            batch.Begin(SpriteSortMode.Deferred, BlendState.NonPremultiplied);
            {
                Point cornerSize = new Point((int)(minScale * texture.Width / 2.0f), (int)(minScale * texture.Height / 2.0f));
                
                // Upper left hand corner.
                srcRect = new Rectangle(0, 0, texture.Width / 2, texture.Height / 2);
                dstRect = new Rectangle(rect.X, rect.Y, cornerSize.X, cornerSize.Y);
                batch.Draw(texture, dstRect, srcRect, color);
                // Lower left hand corner.
                srcRect = new Rectangle(0, texture.Height / 2, texture.Width / 2, texture.Height / 2);
                dstRect = new Rectangle(rect.X, rect.Bottom - cornerSize.Y, cornerSize.X, cornerSize.Y);
                batch.Draw(texture, dstRect, srcRect, color);
                // Left edge gap, if any.
                int verticalGapSize = (rect.Bottom - cornerSize.Y) - (rect.Y + cornerSize.Y) + 1;
                if (verticalGapSize > 1)
                {
                    srcRect = new Rectangle(0, texture.Height / 2, texture.Width / 2, 0);
                    dstRect = new Rectangle(rect.X, rect.Y + cornerSize.Y, cornerSize.X, verticalGapSize);
                    batch.Draw(texture, dstRect, srcRect, color);
                }

                // Upper right hand corner.
                srcRect = new Rectangle(texture.Width / 2, 0, texture.Width / 2, texture.Height / 2);
                dstRect = new Rectangle(rect.Right - cornerSize.X, rect.Y, cornerSize.X, cornerSize.Y);
                batch.Draw(texture, dstRect, srcRect, color);
                // Lower right hand corner.
                srcRect = new Rectangle(texture.Width / 2, texture.Height / 2, texture.Width / 2, texture.Height / 2);
                dstRect = new Rectangle(rect.Right - cornerSize.X, rect.Bottom - cornerSize.Y, cornerSize.X, cornerSize.Y);
                batch.Draw(texture, dstRect, srcRect, color);
                // Right edge gap, if any.
                if (verticalGapSize > 1)
                {
                    srcRect = new Rectangle(texture.Width / 2, texture.Height / 2, texture.Width / 2, 0);
                    dstRect = new Rectangle(rect.Right - cornerSize.X, rect.Y + cornerSize.Y, cornerSize.X, verticalGapSize);
                    batch.Draw(texture, dstRect, srcRect, color);
                }

                // Now calc center gap, if any.
                int horizontalGapSize = (rect.Right - cornerSize.X) - (rect.X + cornerSize.X);
                if (horizontalGapSize > 0)
                {
                    // If shrinking, fill this in as a single rect.
                    if (scale.Y < 1.0f)
                    {
                        srcRect = new Rectangle(texture.Width / 2, 0, 0, texture.Height);
                        dstRect = new Rectangle(rect.X + cornerSize.X, rect.Y, horizontalGapSize, rect.Height);
                        batch.Draw(texture, dstRect, srcRect, color);
                    }
                    else
                    {
                        // Not shrinking, so use 3 rects.
                        // Top
                        srcRect = new Rectangle(texture.Width / 2, 0, 0, texture.Height / 2);
                        dstRect = new Rectangle(rect.X + cornerSize.X, rect.Y, horizontalGapSize, cornerSize.Y);
                        batch.Draw(texture, dstRect, srcRect, color);
                        // Middle
                        if (verticalGapSize > 1)
                        {
                            srcRect = new Rectangle(texture.Width / 2, texture.Height / 2, 0, 0);
                            dstRect = new Rectangle(rect.X + cornerSize.X, rect.Y + cornerSize.Y, horizontalGapSize, verticalGapSize);
                            batch.Draw(texture, dstRect, srcRect, color);
                        }
                        // Bottom
                        srcRect = new Rectangle(texture.Width / 2, texture.Height / 2, 0, texture.Height / 2);
                        dstRect = new Rectangle(rect.X + cornerSize.X, rect.Bottom - cornerSize.Y, horizontalGapSize, cornerSize.Y);
                        batch.Draw(texture, dstRect, srcRect, color);
                    }
                }

            }
            batch.End();

        }   // end of RenderTile()

        #endregion

        #region Internal

        public static void LoadContent(bool immediate)
        {
            BokuGame.Load(AuthUI.statusDialog);
            BokuGame.Load(AuthUI.signInDialog);
            BokuGame.Load(AuthUI.signOutDialog);
        }

        public static void InitDeviceResources(GraphicsDevice device)
        {
            BokuGame.InitDeviceResources(AuthUI.statusDialog, device);
            BokuGame.InitDeviceResources(AuthUI.signInDialog, device);
            BokuGame.InitDeviceResources(AuthUI.signOutDialog, device);
        }

        public static void UnloadContent()
        {
            BokuGame.Unload(AuthUI.statusDialog);
            BokuGame.Unload(AuthUI.signInDialog);
            BokuGame.Unload(AuthUI.signOutDialog);
        }

        public static void DeviceReset(GraphicsDevice device)
        {
        }

        #endregion
    }   // end of class AuthUI

}   // end of namespace Boku.UI2D
