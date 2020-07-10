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
using Boku.UI2D;
using Boku.Input;
using Boku.Fx;

namespace Boku.UI2D
{
    /// <summary>
    /// Specialization of the standard UIGrid for the tool menu.  We need to have a custom
    /// Refresh() method to handle the rotation and scaling of the tiles and we also need a 
    /// custom Render() method to render the tiles from the outside-in so that the drop
    /// shadows and transparency layer correctly.
    /// </summary>
    public class ToolMenuUIGrid : UIGrid
    {
        /// <summary>
        /// Internal class to hold hard-coded tile orientations.
        /// </summary>
        #region Members

        const int kMaxTiles = 20;                           // Max number of tiles we're ready for.
        const float kEllipseRatio = 0.8f;                   // Ellipse aspect ratio.
        const float kXRadius = 6.5f;                        // X radius of ellipse.
        const float kYRadius = kXRadius * kEllipseRatio;    // Y radius of ellipse.
        const float kDeltaTheta = 0.19f;                    // Angular gap between tiles on ellipse.
        const float kTipBackAngle = -0.3f;                  // How far tipped back the tiles are.

        private Vector3[] positions;    // Positions of tiles in space starting with the in-focus tile and moving to the left.
                                        // For the tiles on the right just negate the X coordiate.

        protected Texture2D reticuleTexture = null;

        #endregion

        #region Public


        public ToolMenuUIGrid(
            UIGridEvent onSelect,
            UIGridEvent onCancel,
            Point maxDimensions,
            string uiMode)
            : base(onSelect, onCancel, maxDimensions, uiMode)
        {
            positions = new Vector3[kMaxTiles];

            float theta = 0.0f;
            for (int i = 0; i < kMaxTiles; i++)
            {
                float s = (float)Math.Sin(theta);
                float c = (float)Math.Cos(theta);
                positions[i] = new Vector3(-s * kXRadius, 0.0f, kYRadius + c * kYRadius);

                theta += kDeltaTheta;
            }

        }

        public override void Refresh()
        {
            //base.Refresh();
        }   // end of Refresh()

        public override void Update(ref Matrix parentMatrix)
        {
            int focus = SelectionIndex.X;
            bool wasDirty = dirty;

            // Normally the B button will allow a user to back out of any grid.
            // For the tool menu though we don't want this to happen so we 
            // clear any input on that button before calling the base Update().
            GamePadInput pad = GamePadInput.GetGamePad0();
            pad.ButtonB.ClearAllWasPressedState();

            base.Update(ref parentMatrix);

            // Did something change?  If so, update the transforms for the tiles.
            if (wasDirty || focus != SelectionIndex.X)
            {
                float twitchTime = Time.FrameRate > 20.0f ? 0.2f : 0.0f;
                Vector3 negativeX = new Vector3(-1.0f, 1.0f, 1.0f);
                Vector3 rotation = new Vector3(kTipBackAngle, 0.0f, 0.0f);

                for(int i=0; i<ActualDimensions.X; i++)
                {
                    UIGridElement e = grid[i, 0];
                    e.Rotation = rotation;

                    int index = focusIndex.X - i;
                    if(index >= 0)
                    {
                        //e.Position = positions[index];
                        e.TwitchPosition(positions[index], twitchTime, TwitchCurve.Shape.OvershootOut);
                    }
                    else
                    {
                        Vector3 position = positions[-index];
                        position.X = -position.X;
                        //e.Position = position;
                        e.TwitchPosition(position, twitchTime, TwitchCurve.Shape.OvershootOut);
                    }
                }
            }

            // Update Help Overlay.
            UIGrid2DTextureElement selection = SelectionElement as UIGrid2DTextureElement;
            if (active && selection != null)
            {
                HelpOverlay.ToolIcon = selection.DiffuseTexture;
            }

            HelpOverlay.Pop();
            switch((InGame.UpdateMode)SelectionElement.Tag)
            {
                case InGame.UpdateMode.RunSim:
                    HelpOverlay.Push(@"ToolMenuRunSim");
                    break;
                case InGame.UpdateMode.MiniHub:
                    HelpOverlay.Push(@"ToolMenuHomeMenu");
                    break;
                case InGame.UpdateMode.EditObject:
                    HelpOverlay.Push(@"ToolMenuObjectEdit");
                    break;
                case InGame.UpdateMode.TweakObject:
                    HelpOverlay.Push(@"ToolMenuObjectTweak");
                    break;
                case InGame.UpdateMode.TerrainFlatten:
                    HelpOverlay.Push(@"ToolMenuTerrainSmoothLevel");
                    break;
                case InGame.UpdateMode.TerrainMaterial:
                    HelpOverlay.Push(@"ToolMenuTerrainMaterial");
                    break;
                case InGame.UpdateMode.TerrainRoughHill:
                    HelpOverlay.Push(@"ToolMenuTerrainSpikeyHilly");
                    break;
                case InGame.UpdateMode.TerrainUpDown:
                    HelpOverlay.Push(@"ToolMenuTerrainUpDown");
                    break;
                case InGame.UpdateMode.TerrainWater:
                    HelpOverlay.Push(@"ToolMenuTerrainWater");
                    break;
                case InGame.UpdateMode.DeleteObjects:
                    HelpOverlay.Push(@"ToolMenuDeleteObjects");
                    break;
                case InGame.UpdateMode.EditWorldParameters:
                    HelpOverlay.Push(@"ToolMenuWorldSettings");
                    break;
                default:
                    Debug.Assert(false);
                    break;
            }

        }   // end of Update();


        public override void Render(Camera camera)
        {
            if (active || renderWhenInactive)
            {
                bool focusChanged = prevFocus != focusIndex;

                if (grid[focusIndex.X, focusIndex.Y] != null && (focusChanged || grid[focusIndex.X, focusIndex.Y].Selected == false))
                {
                    // Unselect the previously infocus element before selecting
                    // the new one.  This helps keep the help overlay stack coherent.
                    if (prevFocus.X != -1 && prevFocus.Y != -1)
                    {
                        if (grid[prevFocus.X, prevFocus.Y] != null)
                        {
                            grid[prevFocus.X, prevFocus.Y].Selected = false;
                        }
                    }
                    grid[focusIndex.X, focusIndex.Y].Selected = true;
                }

                prevFocus = focusIndex;


                // Render reticule around selection.
                ScreenSpaceQuad quad = ScreenSpaceQuad.GetInstance();
                UIGrid2DTextureElement e = (UIGrid2DTextureElement)SelectionElement;

                Vector3 pos3D = Vector3.Transform(SelectionElement.Position, LocalMatrix);
                Point pos = camera.WorldToScreenCoords(pos3D);
                Vector2 position = new Vector2(pos.X, pos.Y);

                // First, render from the right edge, inward.
                for (int i = actualDimensions.X - 1; i > focusIndex.X; i--)
                {
                    if (grid[i, 0] != null && grid[i, 0].Visible)
                    {
                        grid[i, 0].Render(camera);
                    }
                }
                // The from left edge to focus object.
                for (int i = 0; i <= focusIndex.X; i++)
                {
                    if (grid[i, 0] != null && grid[i, 0].Visible)
                    {
                        grid[i, 0].Render(camera);
                    }
                }

            }   // end of if active.

        }   // end of Render()

        #endregion

        #region Internal

        public override void LoadContent(bool immediate)
        {
            if (reticuleTexture == null)
            {
                reticuleTexture = BokuGame.Load<Texture2D>(BokuGame.Settings.MediaPath + @"Textures\UI2D\Tools\SelectionReticule");
            }

            base.LoadContent(immediate);

            // If we've had device reset we need to refresh the tool icon we've given to the help overlay.  
            // We let the base.LoadContent call go first to ensure that the new texture is good to go.
            if (HelpOverlay.ToolIcon != null && HelpOverlay.ToolIcon.GraphicsDevice.IsDisposed)
            {
                UIGrid2DTextureElement selection = SelectionElement as UIGrid2DTextureElement;
                if (selection != null)
                {
                    HelpOverlay.ToolIcon = selection.DiffuseTexture;
                }
            }

        }   // end of LoadContent()

        public override void UnloadContent()
        {
            BokuGame.Release(ref reticuleTexture);

            base.UnloadContent();
        }   // end of LoadContent()

        #endregion


    }   // end of class ToolMenuUIGrid

}   // end of namespace Boku.Ui2d
