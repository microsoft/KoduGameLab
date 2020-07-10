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

using KoiX;
using KoiX.Input;


using Boku.Base;
using Boku.Common;
using Boku.UI2D;
using Boku.Input;
using Boku.Fx;

namespace Boku.UI2D
{
    /// <summary>
    /// Specialization of the standard UIGrid for the brush picker.  We need to have a custom
    /// Refresh() method to handle the rotation and scaling of the tiles and we also need a 
    /// custom Render() method to render the tiles from the outside-in so that the drop
    /// shadows and transparency layer correctly.
    /// TODO Code is based on the ToolMenuUIGrid so we may be able to refactor to a common base class.
    /// </summary>
    public class BrushPickerUIGrid : UIGrid
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


        public BrushPickerUIGrid(
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

            GamePadInput pad = GamePadInput.GetGamePad0();

            base.Update(ref parentMatrix);

            // Did something change?  If so, update the transforms for the tiles.
            if (wasDirty || focus != SelectionIndex.X)
            {
                float twitchTime = Time.FrameRate > 20.0f ? 0.2f : 0.0f;
                Vector3 negativeX = new Vector3(-1.0f, 1.0f, 1.0f);
                Vector3 rotation = new Vector3(kTipBackAngle, 0.0f, 0.0f);

                for(int i = 0; i<ActualDimensions.X; i++)
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

            var brushElement = (UIGrid2DBrushElement)SelectionElement;
            var overlay = (string)(brushElement.Tag);
            HelpOverlay.ReplaceTop(overlay);

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

                // First, render from the right edge, inward.
                for (int i = actualDimensions.X - 1; i > focusIndex.X; i--)
                {
                    if (grid[i, 0] != null)
                    {
                        grid[i, 0].Render(camera);
                    }
                }
                // The from left edge to focus object.
                for (int i = 0; i <= focusIndex.X; i++)
                {
                    if (grid[i, 0] != null)
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
                reticuleTexture = KoiLibrary.LoadTexture2D(@"Textures\UI2D\Tools\SelectionReticule");
            }

            base.LoadContent(immediate);

        }   // end of LoadContent()

        public override void UnloadContent()
        {
            DeviceResetX.Release(ref reticuleTexture);

            base.UnloadContent();
        }   // end of LoadContent()

        #endregion

    }   // end of class BrushPickerUIGrid

}   // end of namespace Boku.Ui2d
