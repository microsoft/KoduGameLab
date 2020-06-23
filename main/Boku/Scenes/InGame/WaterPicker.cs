
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
    /// Menu of available materials.  This is a generic element that 
    /// should be able to be used over the top of any tool.
    /// 
    /// Tools which want to use the MaterialPicker should set Active to true
    /// when they start up and back to false when they exit.  Note that
    /// setting Active to true just has the pikcer listen for being made
    /// visible.  The picker only displays when Visible is set to true.

    /// </summary>
    public class WaterPicker : BasePicker
    {
        private int curIndex = 0;           // The index of the material in focus.
        private int numMaterials = 0;       // Count of materials in the list.

        //Touch related variables.
        private int m_FingerID = -1;
        private bool m_bDragging = false;
        private bool m_bDraggable = false;
        private Vector2 m_DragDelta = Vector2.Zero;

        private const float kHitSphereTestSize = 1.5f;


        #region Accessors
        #endregion

        // c'tor
        public WaterPicker(OnSetMaterial onSet, OnGetMaterial onGet)
            : base(onSet, onGet)
        {
            helpOverlay = @"WaterPicker";

            // Create material elements for the grid.
            // Start with a blob of common parameters.
            UIGridElement.ParamBlob blob = new UIGridElement.ParamBlob();
            blob.width = 1.0f;
            blob.height = 1.0f;
            blob.edgeSize = 0.1f;
            blob.selectedColor = Color.White;
            Vector4 transparentWhite = new Vector4(1.0f, 1.0f, 1.0f, 0.5f);
            blob.unselectedColor = new Color(transparentWhite);
            blob.normalMapName = @"QuarterRound4NormalMap";

            // Create and populate grid.
            int maxMaterials = Water.Types.Count;
            grid = new UIGrid(OnSelect, OnCancel, new Point(maxMaterials, 1), "TerrainEdit.WaterPicker");

            numMaterials = 0;

            UIGridWaterElement e = null;
            for (int i = 0; i < maxMaterials; i++)
            {
                e = new UIGridWaterElement(i);
                e.SelectedScale = 0.8f;
                e.UnselectedScale = 0.5f;
                grid.Add(e, numMaterials++, 0);
            }

            // Set grid properties.
            grid.IgnoreInput = true;    // We'll control the grid selection from here instead of internally.
            grid.Spacing = new Vector2(0.25f, 0.0f);
            grid.Scrolling = true;
            grid.Wrap = false;
            grid.LocalMatrix = Matrix.CreateTranslation(0.0f, -2.5f, 0.0f);

            OnSampleType = SampleType;

        }   // end of WaterPicker c'tor

        /// <summary>
        /// Sample the water type under the cursor. Return -1 if no water there.
        /// </summary>
        /// <returns></returns>
        public int SampleType()
        {
            Vector2 pos = InGame.inGame.Cursor3D.Position2d;

            int t = Terrain.GetWaterType(pos);
            return t < Water.Types.Count ? t : -1;
        }

        //
        // TODO (****) Can these next functions be refactored so that I don't need to override them?
        //

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
                curIndex = selection.X;

                RefreshPositions();
            }
        }   // end of WaterPicker IncrementFocus()

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
                curIndex = selection.X;

                RefreshPositions();
            }
        }   // end of WaterPicker DecrementFocus()

        private void RefreshPositions()
        {
            // Handy references.
            Boku.InGame inGame = Boku.InGame.inGame;
            Boku.InGame.Shared shared = inGame.shared;

            OnSetType(curIndex);

            lastChangedTime = Time.WallClockTotalSeconds;

        }   // end of WaterPicker RefreshPositions()

        protected override void SetDefaultSelection()
        {
            // Set the current selection as the default.
            curIndex = OnGetType();
            grid.SelectionIndex = new Point(curIndex, 0);
        }

        public override bool HandleTouchInput(Camera camera)
        {
            float scaleY = Math.Min(BokuGame.ScreenSize.Y / 576.0f, 1.0f);

            AABB2D box = new AABB2D(new Vector2(0, (BokuGame.ScreenSize.Y - (200.0f) * scaleY)), BokuGame.ScreenSize);

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
                    if (!m_bDragging && null != focusedTouch)
                    {
                        Ray ray = new Ray(camera.From, camera.ScreenToWorldCoords(focusedTouch.position));
                        Matrix m = (grid.SelectionElement as UIGridMaterialElement).HitWorldMatrix;

                        bool bSelectedMat = (null != ray.Intersects(new BoundingSphere(m.Translation, kHitSphereTestSize * m.M33)));

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
                                m = (grid.Get(i, 0) as UIGridMaterialElement).HitWorldMatrix;
                                if (null != ray.Intersects(new BoundingSphere(m.Translation, kHitSphereTestSize * m.M33)))
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
                                m = (grid.Get(i, 0) as UIGridMaterialElement).HitWorldMatrix;
                                if (null != ray.Intersects(new BoundingSphere(m.Translation, kHitSphereTestSize * m.M33)))
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
                                return false;
                            }
                        }
                    }

                    m_bDraggable = false;
                    m_bDragging = false;
                    m_FingerID = -1;
                }
            }

            return true;
        }   // end of HandleTouchInput();

        public override bool HandleMouseInput(Camera camera)
        {
            Matrix mat;

            BoundingSphere sphere;  // Sphere we wrap around elements for hit testing.
            float? dist = null;     // Dist to sphere bounds hit.

            bool handled = false;

            // Test in-focus element for hit.
            UIGridMaterialElement e = grid.SelectionElement as UIGridWaterElement;
            if (e != null)
            {
                Vector2 mouseHit = new Vector2(MouseInput.Position.X, MouseInput.Position.Y);
                Ray ray = new Ray(camera.From, camera.ScreenToWorldCoords(mouseHit));

                mat = e.HitWorldMatrix;
                sphere = new BoundingSphere(mat.Translation, 1.25f * mat.M33);
                dist = ray.Intersects(sphere);

                if (dist != null)
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
                    e = grid.Get(i, 0) as UIGridWaterElement;

                    mat = e.HitWorldMatrix;
                    sphere = new BoundingSphere(mat.Translation, 1.25f * mat.M33);
                    dist = ray.Intersects(sphere);

                    if (dist != null)
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
                    e = grid.Get(i, 0) as UIGridWaterElement;

                    mat = e.HitWorldMatrix;
                    sphere = new BoundingSphere(mat.Translation, 1.25f * mat.M33);
                    dist = ray.Intersects(sphere);

                    if (dist != null)
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
        }   // end of HandleMouseInput();

        public override void Render(Camera camera)
        {
            if (active && !hidden)
            {
                // Render reticule around selected material.
                CameraSpaceQuad quad = CameraSpaceQuad.GetInstance();
                UIGridWaterElement e = (UIGridWaterElement)grid.SelectionElement;
                Vector2 position = new Vector2(e.Position.X, e.Position.Y);
                position.X += grid.WorldMatrix.Translation.X;
                position.Y += grid.WorldMatrix.Translation.Y;
                Vector2 size = 2.0f * new Vector2(e.Size.X, e.Size.Y);
                quad.Render(camera, reticuleTexture, position, size, @"AdditiveBlend");

                // Trigger icons?
                double curTime = Time.WallClockTotalSeconds;
                double dTime = curTime - lastChangedTime;
                if (GamePadInput.ActiveMode == GamePadInput.InputMode.GamePad && dTime > kPreFadeTime)
                {
                    dTime -= kPreFadeTime;

                    float alpha = Math.Min((float)(dTime / kFadeTime), 1.0f);
                    Vector2 offset = size * 0.4f;
                    size *= 0.4f;
                    // Note the 12/64 in the positioning accounts for the fact that the 
                    // button textures only use the upper 40x40 out of the 64x64 space they allocate.
                    // The 12 is actually (64-40)/2.
                    quad.Render(camera, ButtonTextures.RightTrigger, alpha, position + offset + size * 12.0f / 64.0f, size, @"TexturedRegularAlpha");
                    offset.X = -offset.X;
                    quad.Render(camera, ButtonTextures.LeftTrigger, alpha, position + offset + size * 12.0f / 64.0f, size, @"TexturedRegularAlpha");
                }

            }

            BokuGame.bokuGame.GraphicsDevice.DepthStencilState = DepthStencilState.None;
            base.Render(camera);

            /*
            // Debug code to show spherical bounding hits.
            Vector4 red = new Vector4(1, 0, 0, 0.5f);
            if (active && !hidden)
            {
                for (int i = 0; i < grid.ActualDimensions.X; i++)
                {
                    UIGridWaterElement e = grid.Get(i, 0) as UIGridWaterElement;

                    // Don't bother if offscreen.
                    Vector3 position = e.WorldMatrix.Translation;
                    float radius = 2.0f;
                    Frustum.CullResult cullResult = camera.Frustum.CullTest(position, radius);
                    if (cullResult == Frustum.CullResult.TotallyOutside)
                        continue;

                    Utils.DrawSolidSphere(camera, e.HitWorldMatrix.Translation, 1.25f * e.HitWorldMatrix.M33, red);
                }
            }
            */
        }   // end of WaterPicker Render()

    }   // end of class WaterPicker

}   // end of namespace Boku

