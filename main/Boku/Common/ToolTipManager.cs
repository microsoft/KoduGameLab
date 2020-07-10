// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.


using System;
using System.Collections;
using System.Collections.Generic;
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
using Boku.Fx;
using Boku.Common.Xml;

namespace Boku.Common
{
    /// <summary>
    /// Provides a single entry point for creating ToolTips.  
    /// </summary>
    public class ToolTipManager
    {

        private enum Mode
        {
            None,       // No current tip.
            PreDelay,   // We have a tip but we're still in the "pause" phase before displaying it.
            Visible,    // The tip is visible.  (may be fading in or fully opaque)
            FadingOut,  // The tip is being removed.  Actually this mode may overlap with PreDelay so
                        // we need to look at the startFadeTime to see if we're fading.
        }

        private static double preDelay = 1.0;   // # seconds of "hover" before tip is activated.
        private static double fadeIn = 0.1;
        private static double fadeOut = 0.1;

        private static double startShowTime = 0;    // Time when ShowTip was called.
        private static double startFadeTime = 0;    // Time when we transitioned to Fading Out.

        private static Mode mode = Mode.None;

        private static string curTip = null;
        private static TextBlob blob = null;
        private static float alpha = 0.0f;
        private static Vector2 curPosition;         // The position we're currently displaying at.
        private static Vector2 pendingPosition;     // The position of the pending tool tip.
        private static Vector2 originalPosition;    // Unmodified version of the above used to tell when the position 
                                                    // of the tooltip hs been changed even though the content hasn't.
        private static bool useAdd = false;         // Use the "Add" string to label the <A> button rather than "Change"

        private static Texture2D texture = null;
        private static Texture2D background = null;

        private static bool dirty = true;           // Texture2D needs refreshing.

        private static int margin = 20;             // Margin for text in pixels.
        private static Vector2 size;                // Size to render at in pixels.
        private static bool showButtons = false;    // Show the A and Y buttons at the bottom?

        private static Vector2 mousePos;            // Keep track of mouse position, clear tooltip when moved.
        private static float mouseRadius = 8.0f;    // Radius needed to "move" mouse.

        #region Accessors
        #endregion

        // c'tor
        private ToolTipManager()
        {
        }

        public static void Init()
        {
        }   // end of c'tor

        public static void Update()
        {
            double secs = Time.WallClockTotalSeconds;

            alpha = 0.0f;

            // Fading out?
            if (secs - startFadeTime < fadeOut)
            {
                alpha = (float)(1 - (secs - startFadeTime) / fadeOut);
            }
            else if (mode == Mode.Visible)
            {
                alpha = 1.0f;
            }
            else if(mode == Mode.PreDelay)
            {
                if (secs - startShowTime < preDelay)
                {
                    // Do nothing, still in pre-delay
                }
                else
                {
                    if (dirty)
                    {
                        RefreshTexture();
                    }
                    curPosition = pendingPosition;
                    alpha = (float)((secs - startShowTime - preDelay) / fadeIn);
                    if (alpha > 1.0f)
                    {
                        alpha = 1.0f;
                        mode = Mode.Visible;
                    }
                }
            }

            if (GamePadInput.ActiveMode == GamePadInput.InputMode.KeyboardMouse)
            {
                Vector2 cur = new Vector2(MouseInput.Position.X, MouseInput.Position.Y);
                float dist = (cur - mousePos).Length();

                if (dist > mouseRadius)
                {
                    Clear();
                }
            }
        }   // end of ToolTipManager Update()

        public static void Render(Camera camera)
        {
            if (alpha > 0.0f)
            {
                ScreenSpaceQuad ssquad = ScreenSpaceQuad.GetInstance();

                Vector4 color = new Vector4(1, 1, 1, alpha);
                ssquad.Render(texture, color, curPosition, size, "TexturedRegularAlphaNoZ");
            }

        }   // end of ToolTipManager Render()

        /// <summary>
        /// Signals the tool tip manager to create a new tool tip.  The tip is not displayed
        /// immediately, rather the pre-delay timer is started.  When this timer expires the
        /// tool tip is then displayed.  This also gives any previous tool tip time to be
        /// gracefully faded out.
        /// </summary>
        /// <param name="name">The name to be displayed at the top of the tool tip</param>
        /// <param name="desc">The description string to be displayed.  This may contain 
        /// button icons.  If the description is too long to fit onto the tool tip it will
        /// be truncated and the final line changed to end in elipses.</param>
        /// <param name="position">Position in pixels for the upper left hand corner of the tool tip.</param>
        /// <param name="showButtons">If true, display button(s) at the bottom of the tool tip:  A accept  Y Examples</param>
        public static void ShowTip(string name, string desc, Vector2 position, bool showButtons)
        {
            ShowTip(name, desc, position, showButtons, false);
        }   // end of ShowTip

        /// <summary>
        /// Signals the tool tip manager to create a new tool tip.  The tip is not displayed
        /// immediately, rather the pre-delay timer is started.  When this timer expires the
        /// tool tip is then displayed.  This also gives any previous tool tip time to be
        /// gracefully faded out.
        /// </summary>
        /// <param name="name">The name to be displayed at the top of the tool tip</param>
        /// <param name="desc">The description string to be displayed.  This may contain 
        /// button icons.  If the description is too long to fit onto the tool tip it will
        /// be truncated and the final line changed to end in elipses.</param>
        /// <param name="position">Position in pixels for the upper left hand corner of the tool tip.</param>
        /// <param name="showButtons">If true, display button(s) at the bottom of the tool tip:  A Change  Y Examples</param>
        /// <param name="useAdd">For the A button, used the "Add" label instead of the "change" one.</param>
        public static void ShowTip(string name, string desc, Vector2 position, bool showButtons, bool useAdd)
        {
            if (!XmlOptionsData.ShowToolTips)
                return;

            ToolTipManager.showButtons = showButtons;

            // Capitalize the name.
            string betterName = null;
            if (name != null && name.Length > 0)
            {
                betterName = char.ToUpper(name[0]) + name.Substring(1);
                betterName = TextHelper.FilterInvalidCharacters(betterName);
            }

            // Trim white space off string, filter any invalid characters.
            if (desc != null)
            {
                desc = desc.Trim();
                desc = TextHelper.FilterInvalidCharacters(desc);
            }

            // Already showing this one...
            if (betterName == curTip && position == originalPosition)
            {
                return;
            }
            originalPosition = position;

            // If a tip is already active, deactivate it.
            if (mode == Mode.Visible)
            {
                mode = Mode.None;
                startFadeTime = Time.WallClockTotalSeconds;
            }

            // Check if this tip is valid.
            if (betterName == null || desc == null || desc == "")
            {
                // If not valid, set current tip to null.
                curTip = null;
                blob = null;
                mode = Mode.None;
            }
            else
            {
                // If valid, set as current tip and start delay timer .
                curTip = betterName;
                if (desc != null)
                {
                    desc = desc.Trim();
                    blob = new TextBlob(UI2D.Shared.GetGameFont24, desc, 512 - margin * 2);
                }
                else
                {
                    blob = null;
                }
                pendingPosition = position;
                mode = Mode.PreDelay;
                startShowTime = Time.WallClockTotalSeconds;
                ToolTipManager.useAdd = useAdd;
                dirty = true;

                // Grab mouse position so we know if it moved.
                mousePos = new Vector2(MouseInput.Position.X, MouseInput.Position.Y);
            }

        }   // end of ShowTip()

        /// <summary>
        /// Removes any acitve tooltip.
        /// </summary>
        public static void Clear()
        {
            // If a tip is active but not yet visible, just get rid of it.
            if (mode == Mode.PreDelay)
            {
                mode = Mode.None;
                curTip = null;
                blob = null;
            }
            else if (mode == Mode.Visible)
            {
                mode = Mode.None;
                curTip = null;
                blob = null;
                startFadeTime = Time.WallClockTotalSeconds;
            }
        }   // end of Clear()

        private static void RefreshTexture()
        {
            bool lores = BokuGame.ScreenSize.Y <= 480;

            UI2D.Shared.GetFont Font = UI2D.Shared.GetGameFont24Bold;
            if (lores)
            {
                Font = UI2D.Shared.GetGameFont30Bold;
            }
            blob.Font = Font;

            ScreenSpaceQuad ssquad = ScreenSpaceQuad.GetInstance();
            RenderTarget2D rt = UI2D.Shared.RenderTarget512_302;

            InGame.SetRenderTarget(rt);

            InGame.Clear(Color.Transparent);

            ssquad.Render(background, Vector2.Zero, new Vector2(512, 302), "TexturedRegularAlpha");

            // Tile name.
            SpriteBatch batch = UI2D.Shared.SpriteBatch;
            Vector2 pos = new Vector2(margin, margin);
            /*
            batch.Begin();
            TextHelper.DrawString(Font, curTip, pos, Color.Yellow);
            batch.End();
            */
            string desc = blob.RawText; // Save string we're displaying.
            blob.RawText = curTip;
            if (blob.HasRtoL)
            {
                blob.Justification = Boku.UI2D.UIGridElement.Justification.Right;
            }
            blob.RenderWithButtons(pos, Color.Yellow);
            blob.RawText = desc;        // Restore

            // We need to special case groups since they don't have any data.  For groups 
            // we just display a string that says "press <a> for more..."
            if (curTip == Strings.Localize("toolTips.group"))
            {
                blob = new TextBlob(Font, Strings.Localize("toolTips.groupDesc"), 512 - margin * 2);
                blob.Justification = Boku.UI2D.UIGridElement.Justification.Center;
                pos = new Vector2(0, (302 - Font().LineSpacing) / 2);
                blob.RenderWithButtons(pos, Color.White);
            }
            else
            {
                // Normal ToolTip.

                // Text description.
                if (blob != null)
                {
                    int maxLines = showButtons ? (lores ? 3 : 4) : (lores ? 4 : 6);

                    // Modify final line to end with ellipsis.
                    blob.AddEllipsisToLine(maxLines - 1);

                    // Move down to account for title.
                    pos.Y += Font().LineSpacing;

                    // If less than maxLines of text, center on texture.
                    int spareLines = maxLines - blob.NumLines - 1;
                    if (spareLines > 0)
                    {
                        pos.Y += spareLines * 0.5f * Font().LineSpacing;
                    }

                    // Right justify if RtoL
                    if (blob.HasRtoL)
                    {
                        blob.Justification = Boku.UI2D.UIGridElement.Justification.Right;
                    }

                    blob.RenderWithButtons(pos, Color.White, maxLines: maxLines);
                }

                // Buttons @ bottom
                if (showButtons)
                {
                    string aText = useAdd ? Strings.Localize("toolTips.add") : Strings.Localize("toolTips.change"); 

                    int buttonWidth = 40;                       // For spacing.
                    Vector2 buttonSize = new Vector2(64, 64);   // For rendering.
                    pos.Y = 302 - margin - Font().LineSpacing;
                    int aTextWidth = (int)Font().MeasureString(aText).X;
                    int yTextWidth = (int)Font().MeasureString(Strings.Localize("toolTips.examples")).X;
                    int width = 3 * buttonWidth + aTextWidth + yTextWidth;
                    pos.X = (512 - width) / 2;

                    batch.Begin();
                    
                    ssquad.Render(ButtonTextures.AButton, pos, buttonSize, "TexturedRegularAlpha");
                    pos.X += buttonWidth;
                    TextHelper.DrawString(Font, aText, pos, Color.White);

                    pos.X += buttonWidth + aTextWidth;
                    ssquad.Render(ButtonTextures.YButton, pos, buttonSize, "TexturedRegularAlpha");
                    pos.X += buttonWidth;
                    TextHelper.DrawString(Font, Strings.Localize("toolTips.examples"), pos, Color.White);
                    
                    batch.End();
                }
            }

            InGame.RestoreRenderTarget();

            //
            // Copy result to local texture.
            //
            int[] data = new int[512 * 302];
            rt.GetData<int>(data);
            texture.SetData<int>(data);

            // Scale size to 1/4 screen height.
            int w = (int)BokuGame.ScreenSize.X;
            int h = (int)BokuGame.ScreenSize.Y;
            float scale = h / 4.0f / 302.0f;
            size = new Vector2(512, 302);
            size *= scale;

            // Check position to keep on screen within 10% safe area.
            // Horizontal
            int safe = (int)(w * 0.05f);
            if (pendingPosition.X < safe)
            {
                pendingPosition.X = safe;
            }
            else if (pendingPosition.X > w - safe - size.X)
            {
                pendingPosition.X = w - safe - size.X;
            }
            // Vertical
            safe = (int)(h * 0.05f);
            if (pendingPosition.Y < safe)
            {
                pendingPosition.Y = safe;
            }
            else if (pendingPosition.Y > h - safe - size.Y)
            {
                pendingPosition.Y = h - safe - size.Y;
            }

            dirty = false;

        }   // end of RefreshTexture()

        public static void LoadContent(bool immediate)
        {
            // Load the background texture.
            if (background == null)
            {
                background = BokuGame.Load<Texture2D>(BokuGame.Settings.MediaPath + @"Textures\HelpCard\ToolTip");
            }

        }   // end of LoadContent()

        public static void InitDeviceResources(GraphicsDevice device)
        {
            // Create the texture.
            if (texture == null)
            {
                texture = new Texture2D(device, 512, 302, false, SurfaceFormat.Color);
            }

        }   // end of InitDeviceResources()

        public static void UnloadContent()
        {
            BokuGame.Release(ref texture);
            BokuGame.Release(ref background);
        }   // end of UnloadContent()

        /// <summary>
        /// Recreate render targets.
        /// </summary>
        public static void DeviceReset(GraphicsDevice device)
        {
        }

    }   // end of class ToolTipManager

}   // end of namespace Boku.Common
