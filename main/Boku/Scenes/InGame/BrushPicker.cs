// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.


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
using Microsoft.Xna.Framework.Storage;

using Boku.Audio;
using Boku.Base;
using Boku.Common;
using Boku.UI;
using Boku.UI2D;
using Boku.Input;
using Boku.SimWorld;
using Boku.SimWorld.Terra;
using Boku.Fx;

namespace Boku
{
    /// <summary>
    /// Menu of available brushes.  This is a generic element that 
    /// should be able to be used over the top of any tool.
    /// Tools can pick from various sets of brushes so at construction
    /// time we create a list with all the brushes in it.  The tool
    /// using the BrushPicker can then choose which brush set to 
    /// use at which time we pull the appropriate brushes from the
    /// full list and put them into the grid.
    /// 
    /// Tools which want to use the BrushPicker should set Active to true
    /// when they start up and back to false when they exit.  Note that
    /// setting Active to true just has the pikcer listen for being made
    /// visible.  The picker only displays when Visible is set to true.
    /// </summary>
    public class BrushPicker : BasePicker
    {
        private List<UIGrid2DBrushElement> brushList = null;

        //Touch related variables.
        private int m_FingerID = -1;
        private bool m_bDragging = false;
        private bool m_bDraggable = false;
        private Vector2 m_DragDelta = Vector2.Zero;

        #region Accessors
        public Brush2DManager.BrushType BrushSet
        {
            set
            {
                int[] set = Brush2DManager.GetBrushSet(value);

                // Try and maintain the curent focus index.
                Point focus = grid.SelectionIndex;
                
                // Clear the existing brush set.
                // Don't unload the brushes since brushList is holding them.
                grid.ClearNoUnload();

                // Add new set.
                for (int i = 0; i < set.Length; i++)
                {
                    grid.Add(brushList[set[i]], i, 0);
                }

                // Look at the current edit brush.  If that brush is 
                // in our set then choose that brush to be in focus.
                int curIndex = InGame.inGame.shared.editBrushIndex;
                grid.SelectionIndex = new Point(0, 0);  // Default to first element.
                for (int i = 0; i < set.Length; i++)
                {
                    if (set[i] == curIndex)
                    {
                        grid.SelectionIndex = new Point(i, 0);
                        break;
                    }
                }
                // Whichever brush we've decided is in focs, ensure that
                // the edit brush matches.
                UIGrid2DBrushElement e = (UIGrid2DBrushElement)grid.SelectionElement;
                InGame.inGame.shared.editBrushIndex = e.BrushIndex;
            }
        }
        private float Alpha
        {
            get { return alpha; }
            set
            {
                if (alpha != value)
                {
                    alpha = value;
                    // Since the alpha value has changed, update all the elements.
                    for (int i = 0; i < grid.ActualDimensions.X; i++)
                    {
                        UIGrid2DTextureElement e = (UIGrid2DTextureElement)grid.Get(i, 0);
                        Vector4 color = e.BaseColor;
                        color.W = alpha;
                        e.BaseColor = color;
                    }
                }
            }
        }
        #endregion

        // c'tor
        public BrushPicker()
            : base(null, null)
        {
            helpOverlay = @"BrushPicker";
            altHelpOverlay = @"BrushPickerMaterial";

            // Create brush elements for the grid.
            // Start with a blob of common parameters.
            UIGridElement.ParamBlob blob = new UIGridElement.ParamBlob();
            blob.width = 1.0f;
            blob.height = 1.0f;
            blob.edgeSize = 0.1f;
            blob.selectedColor = Color.Transparent;
            blob.unselectedColor = Color.Transparent;
            blob.normalMapName = @"QuarterRound4NormalMap";
            blob.altShader = true;
            blob.ignorePowerOf2 = true;

            // Create and fill the list.
            int maxBrushes = Brush2DManager.NumBrushes;
            brushList = new List<UIGrid2DBrushElement>(maxBrushes);

            UIGrid2DBrushElement e = null;
            for (int i = 0; i < maxBrushes; i++)
            {
                Brush2DManager.Brush2D brush = Brush2DManager.GetBrush(i);
                e = new UIGrid2DBrushElement(blob, brush.TileTextureName, i);
                e.NoZ = true;
                e.Tag = brush.HelpOverlay;
                brushList.Add(e);
            }

            // Create and populate grid.  By default we'll start with the full set of brushes.
            grid = new BrushPickerUIGrid(OnSelect, OnCancel, new Point(maxBrushes, 1), "TerrainEdit.BrushPicker");

            for (int i = 0; i < maxBrushes; i++)
            {
                grid.Add(brushList[i], i, 0);
            }

            // Set grid properties.
            //grid.AlwaysReadInput = true;
            grid.Scrolling = false;
            grid.IgnoreInput = true;
            grid.Spacing = new Vector2(0.25f, 0.0f);
            grid.UseLeftStick = false;
            grid.UseTriggers = true;
            grid.Wrap = false;
            grid.LocalMatrix = Matrix.CreateTranslation(0.0f, -2.2f, -10.0f);
            
        }   // end of BrushPicker c'tor

        /// <summary>
        /// Change which element is in focus by incrementing the selection index.
        /// </summary>
        protected override void IncrementFocus()
        {
            Point selection = grid.SelectionIndex;
            if (selection.X < grid.ActualDimensions.X - 1)
            {
                selection.X = (selection.X + 1) % grid.ActualDimensions.X;
                grid.SelectionIndex = selection;
                grid.Dirty = true;
                UpdateIndex();
            }
        }   // end of IncrementFocus()

        /// <summary>
        /// Change which element is in focus by decrementing the selection index.
        /// </summary>
        protected override void DecrementFocus()
        {
            Point selection = grid.SelectionIndex;
            if (selection.X > 0)
            {
                selection.X = (selection.X - 1 + grid.ActualDimensions.X) % grid.ActualDimensions.X;
                grid.SelectionIndex = selection;
                grid.Dirty = true;
                UpdateIndex();
            }
        }   // end of DecrementFocus()

        protected override void UpdateIndex()
        {
            // Handy references.
            Boku.InGame inGame = Boku.InGame.inGame;
            Boku.InGame.Shared shared = inGame.shared;

            UIGrid2DBrushElement e = (UIGrid2DBrushElement)grid.SelectionElement;
            shared.editBrushIndex = e.BrushIndex;

            lastChangedTime = Time.WallClockTotalSeconds;

        }   // end of BrushPicker UpdateIndex()

        public bool TouchIsOverBrushSelection(TouchContact touch, Camera camera)
        {
            float width;
            float height;
            Matrix invMat;

            // Test in-focus element for hit.
            UIGridElement e = grid.SelectionElement;
            if (e != null)
            {
                GetElementInfo(e, out invMat, out width, out height);
                
                Vector2 hitUV = TouchInput.GetHitUV(touch.position, camera, ref invMat, width, height, useRtCoords: false);

                if (hitUV.X > 0 && hitUV.X < 1 && hitUV.Y > 0 && hitUV.Y < 1)
                {
                    return true;
                }                
            }

            return false;
        }

        public override bool HandleTouchInput(Camera camera)
        {
            float scaleY = Math.Min(BokuGame.ScreenSize.Y / 576.0f, 1.0f);


            AABB2D box = new AABB2D(new Vector2(0, (BokuGame.ScreenSize.Y - (300.0f) * scaleY)), BokuGame.ScreenSize);

            TouchContact focusedTouch = null;

            //Finger ID is focused finger
            if (m_FingerID < 0)
            {
                TouchContact[] touches = TouchInput.Touches;

                for (int i = 0; i < touches.Length; ++i)
                {
                    if (TouchPhase.Began == touches[i].phase)
                    {
                        m_FingerID = touches[i].fingerId;
                        m_bDraggable = box.Contains(touches[i].position);
                        break;
                    }
                }
            }
            else
            {
                focusedTouch = TouchInput.GetTouchContactByFingerId(m_FingerID);

                if (null != focusedTouch && TouchPhase.Ended != focusedTouch.phase)
                {
                    if (TouchPhase.Moved == focusedTouch.phase)
                    {
                        m_DragDelta += focusedTouch.deltaPosition;
                    }

                    if (!m_bDragging && m_bDraggable)
                    {
                        //Check to see if the dragging threshold has been exceeded to change material.
                        Vector2 distance = focusedTouch.position - focusedTouch.startPosition;

                        //Did we start a drag?
                        if (Math.Abs(distance.X) >= 20.0f)
                        {
                            m_bDragging = true;
                            m_DragDelta = distance;
                        }
                    }

                    if (m_bDragging)
                    {
                        while (Math.Abs(m_DragDelta.X) >= 200.0f)
                        {
                            if (m_DragDelta.X >= 200.0f)
                            {
                                DecrementFocus();
                                m_DragDelta.X -= 200.0f;
                            }
                            else if (m_DragDelta.X <= -200.0f)
                            {
                                IncrementFocus();
                                m_DragDelta.X += 200.0f;
                            }
                        }
                    }
                }
                else
                {
                    //When the focused touch is ended we check for selection
                    if (!m_bDragging && null != focusedTouch )
                    {

                        bool bSelectedMat = TouchHitElement(grid.SelectionElement, camera, focusedTouch.position);

                        if (bSelectedMat)
                        {
                            SelectCurrentChoice();
                            Foley.PlayPressA();
                        }
                        else
                        {
                            //Didn't select the center piece.  Check if we picked a material on the sides so we can go to it.
                            for (int i = 0; (!bSelectedMat && i < grid.SelectionIndex.X); i++)
                            {
                                if (TouchHitElement( grid.Get(i, 0), camera, focusedTouch.position))
                                {
                                    int steps = grid.SelectionIndex.X - i;
                                    while (steps > 0)
                                    {
                                        DecrementFocus();
                                        --steps;
                                    }
                                    bSelectedMat = true;
                                    break;
                                }
                            }

                            for (int i = grid.SelectionIndex.X + 1; (!bSelectedMat && i < grid.ActualDimensions.X); i++)
                            {
                                if (TouchHitElement( grid.Get(i, 0), camera, focusedTouch.position))
                                {
                                    int steps = i - grid.SelectionIndex.X;
                                    while (steps > 0)
                                    {
                                        IncrementFocus();
                                        --steps;
                                    }
                                    bSelectedMat = true;
                                    break;
                                }
                            }

                            //Cancel this menu.
                            if (!bSelectedMat &&
                                !box.Contains(focusedTouch.position) &&
                                (Time.WallClockTotalSeconds - focusedTouch.startTime <= 0.3))
                            {
                                //If we didn't select anything and we are outside the drag area + didn't move a lot then cancel.
                                RestorePreviousChoice();
                                Foley.PlayBack();
                            }
                        }
                    }

                    m_bDraggable = false;
                    m_bDragging = false;
                    m_FingerID = -1;
                }
            }

            return true;
        }   // end of HandleTouchInput()

        private bool TouchHitElement( UIGridElement element, Camera camera, Vector2 screenPos )
        {
            float width, height;
            Matrix invMat;

            GetElementInfo(element, out invMat, out width, out height);
            Vector2 hitUV = TouchInput.GetHitUV(screenPos, camera, ref invMat, width, height, useRtCoords: false);

            return !( (hitUV.X < 0 || hitUV.X > 1) ||
                      (hitUV.Y < 0 || hitUV.Y > 1) );
        }

        public override bool HandleMouseInput(Camera camera)
        {
            float width;
            float height;
            Matrix invMat;

            bool handled = false;

            // Test in-focus element for hit.
            UIGridElement e = grid.SelectionElement;
            if (e != null)
            {
                GetElementInfo(e, out invMat, out width, out height);
                Vector2 hitUV = MouseInput.GetHitUV(camera, ref invMat, width, height, useRtCoords: false);

                if (hitUV.X > 0 && hitUV.X < 1 && hitUV.Y > 0 && hitUV.Y < 1)
                {
                    if (MouseInput.Left.WasPressed)
                    {
                        MouseInput.ClickedOnObject = e;
                        handled = true;
                    }
                    if (MouseInput.Left.WasReleased && MouseInput.ClickedOnObject == e)
                    {
                        SelectCurrentChoice();
                        Foley.PlayPressA();
                        return true;
                    }
                }

                // Test elements not in focus.  If one is hit, bring it to the fore.
                // Need to test inside out so that if any overlap we test the front
                // ones first and react to them.  If we find a UV hit with a tile
                // we break to skip testing the rest.
                // Left side first.
                for (int i = grid.SelectionIndex.X - 1; i >= 0; i--)
                {
                    e = grid.Get(i, 0);
                    GetElementInfo(e, out invMat, out width, out height);
                    hitUV = MouseInput.GetHitUV(camera, ref invMat, width, height, useRtCoords: false);

                    if (hitUV.X > 0 && hitUV.X < 1 && hitUV.Y > 0 && hitUV.Y < 1)
                    {
                        if (MouseInput.Left.WasPressed)
                        {
                            MouseInput.ClickedOnObject = e;
                            handled = true;
                        }
                        if (MouseInput.Left.WasReleased && MouseInput.ClickedOnObject == e)
                        {
                            int steps = grid.SelectionIndex.X - i;
                            while (steps > 0)
                            {
                                DecrementFocus();
                                --steps;
                            }
                            handled = true;
                        }
                        break;
                    }
                }
                // Now the right side.
                for (int i = grid.SelectionIndex.X + 1; i < grid.ActualDimensions.X; i++)
                {
                    e = grid.Get(i, 0);
                    GetElementInfo(e, out invMat, out width, out height);
                    hitUV = MouseInput.GetHitUV(camera, ref invMat, width, height, useRtCoords: false);

                    if (hitUV.X > 0 && hitUV.X < 1 && hitUV.Y > 0 && hitUV.Y < 1)
                    {
                        if (MouseInput.Left.WasPressed)
                        {
                            MouseInput.ClickedOnObject = e;
                            handled = true;
                        }
                        if (MouseInput.Left.WasReleased && MouseInput.ClickedOnObject == e)
                        {
                            int steps = i - grid.SelectionIndex.X;
                            while (steps > 0)
                            {
                                IncrementFocus();
                                --steps;
                            }
                            handled = true;
                        }
                        break;
                    }
                }
            }
            
            return handled;
        }   // end of HandleMouseInput()

        /// <summary>
        /// Helper function to cut down on the cut/paste.
        /// </summary>
        /// <param name="e">The element we want info about.</param>
        /// <param name="invMat">Inverse world matrix of the element.</param>
        /// <param name="height">Height of the element.</param>
        /// <param name="width">Width of the element.</param>
        private void GetElementInfo(UIGridElement e, out Matrix invMat, out float height, out float width)
        {
            invMat = e.InvWorldMatrix;
            UIGrid2DTextureElement te = e as UIGrid2DTextureElement;
            width = te.Width;
            height = te.Height;
        }   // end of GetElementInfo()

        public override void Render(Camera camera)
        {
            if (active && alpha > 0.0f)
            {
                // Render reticule around selected brush.
                CameraSpaceQuad quad = CameraSpaceQuad.GetInstance();
                UIGrid2DTextureElement e = (UIGrid2DTextureElement)grid.SelectionElement;
                Vector2 position = new Vector2(e.Position.X, e.Position.Y);
                position.X += grid.WorldMatrix.Translation.X;
                position.Y += grid.WorldMatrix.Translation.Y;
                position.Y -= 0.14f;    // No clue.  Nedd to figure this out.
                Vector2 size = 2.0f * new Vector2(e.Size.X, e.Size.Y);
                quad.Render(camera, reticuleTexture, alpha, position, size, @"AdditiveBlend");

                // Trigger icons?
                double curTime = Time.WallClockTotalSeconds;
                double dTime = curTime - lastChangedTime;
                if (GamePadInput.ActiveMode == GamePadInput.InputMode.GamePad && dTime > kPreFadeTime)
                {
                    dTime -= kPreFadeTime;

                    float triggerAlpha = Math.Min((float)(dTime / kFadeTime), 1.0f);
                    Vector2 offset = size * 0.4f;
                    size *= 0.4f;
                    // Note the 12/64 in the positioning accounts for the fact that the 
                    // button textures only use the upper 40x40 out of the 64x64 space they allocate.
                    // The 12 is actually (64-40)/2.
                    quad.Render(camera, ButtonTextures.RightTrigger, triggerAlpha, position + offset + size * 12.0f / 64.0f, size, @"TexturedRegularAlpha");
                    offset.X = -offset.X;
                    quad.Render(camera, ButtonTextures.LeftTrigger, triggerAlpha, position + offset + size * 12.0f / 64.0f, size, @"TexturedRegularAlpha");
                }

            }

            base.Render(camera);
        }   // end of BrushPicker Render()

    }   // end of class BrushPicker

}   // end of namespace Boku


