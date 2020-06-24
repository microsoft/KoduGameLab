
using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;

using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;

using KoiX;
using KoiX.Geometry;
using KoiX.Input;
using KoiX.Managers;
using KoiX.Text;
using KoiX.UI;

namespace KoiX.UI.Dialogs
{
    abstract public class BasePieMenuDialog : BaseDialog
    {
        #region Members

        protected BasePieMenuDialog parent = null;      // If we are a spawned sub-menu, this is our parent.
        protected BasePieMenuDialog child = null;       // If we have a spawned sub-menu, this is it.

        SpriteCamera localCamera;
        Twitchable<Vector2> cameraPosition;

        Vector2 offset;                     // Offset from parent position.

        bool dirtyPlacement = true;         // Do we need to define/position elements?

        #endregion

        #region Accessors

        public SpriteCamera LocalCamera
        {
            get { return localCamera; }
        }

        /// <summary>
        /// Camera position used to move dialog.
        /// </summary>
        public Vector2 CameraPosition
        {
            get { return cameraPosition.Value; }
            set { cameraPosition.Value = value; }
        }

        #endregion

        #region Public

        public BasePieMenuDialog(ThemeSet theme = null)
        {
            RenderBaseTile = false;
            BackdropColor = Color.Transparent;

            if (theme == null)
            {
                theme = Theme.CurrentThemeSet;
            }

            localCamera = new SpriteCamera();

            cameraPosition = new Twitchable<Vector2>(0.2f, TwitchCurve.Shape.EaseOut);
            cameraPosition.Value = Vector2.Zero;
        }   // end of c'tor

        public override void Update(SpriteCamera camera)
        {
            localCamera.Position = cameraPosition.Value;
            localCamera.Zoom = camera.Zoom;
            localCamera.Update();

            if (child != null)
            {
                // Offset relative to child.
                cameraPosition.Value = child.offset;
            }
            else
            {
                // Return to center.
                cameraPosition.Value = Vector2.Zero;
            }

            base.Update(localCamera);
        }   // end of Udpate()

        public override void Render(SpriteCamera camera)
        {
            // Base Render is called first.  Since we're doing all the rendering
            // here, this only has an effect if RenderBackdrop is true.
            base.Render(localCamera);

            // Render self first, then child on top if it exists.
            // Render all shadows first.            
            foreach (PieMenuElement e in widgets)
            {
                e.RenderShadow(localCamera);
            }

            foreach (PieMenuElement e in widgets)
            {
                e.Render(localCamera);
            }

        }   // end of Render()

        public void AddElement(PieMenuElement element)
        {
            AddWidget(element);

            dirtyPlacement = true;
        }   // end of AddElement()

        /// <summary>
        /// Values which need to be set before showing this menu.
        /// </summary>
        /// <param name="parent"></param>
        /// <param name="offset">Offset from parent's centr</param>
        public void SetParams(BasePieMenuDialog parent, Vector2 offset)
        {
            this.parent = parent;
            this.offset = offset;
        }   // end of SetParams()

        public override void Activate(params object[] args)
        {
            // Prevent any slice from being in focus at startup.
            PrevFocusWidget = null;

            if (dirtyPlacement)
            {
                PlaceElements();
            }

            // Start camera at offset.
            cameraPosition.TargetValue = -offset;

            base.Activate(args);
        }   // end of Activate()

        public override void Deactivate()
        {
            // Clear all parent/child refs.
            if (parent != null)
            {
                parent.child = null;    // Disconnect parent from us.
                parent = null;          // Disconnect us from parent.
            }
            child = null;               // Shouldn't be needed...

            base.Deactivate();
        }   // end of Deactivate()

        #endregion

        #region InputEventHandler

        public override void RegisterForInputEvents()
        {
            // Call base.Register first.  This ensures that the below registrations
            // end up in the same input set since a new set is pushed during the
            // base call.
            base.RegisterForInputEvents();

            // Events used to cancel this dialog.
            KoiLibrary.InputEventManager.RegisterForEvent(this, InputEventManager.Event.MouseLeftDown);
            KoiLibrary.InputEventManager.RegisterForEvent(this, InputEventManager.Event.Keyboard);
            KoiLibrary.InputEventManager.RegisterForEvent(this, InputEventManager.Event.GamePad);
            KoiLibrary.InputEventManager.RegisterForEvent(this, InputEventManager.Event.Tap);
        }   // end of RegisterForInputEvents()

        public override bool ProcessMouseLeftDownEvent(MouseInput input)
        {
            Debug.Assert(Active);

            if (KoiLibrary.InputEventManager.MouseFocusObject == null)
            {
                PieMenuElement e = GetFocusElement();
                if (e != null && KoiLibrary.InputEventManager.MouseHitObject == e)
                {
                    OnSelect(e);
                }
                else
                {
                    // If we clicked off a slice, exit.
                    DialogManagerX.KillDialog(this);
                }

                return true;
            }

            return base.ProcessMouseLeftDownEvent(input);
        }   // end of ProcessMouseLeftDownEvent()

        public override bool ProcessKeyboardEvent(KeyInput input)
        {
            Debug.Assert(Active);

            // Exit out of menu.
            if (input.Key == Keys.Escape)
            {
                DialogManagerX.KillDialog(this);
                return true;
            }

            // Accept in-focus element.
            if (input.Key == Keys.Enter)
            {
                PieMenuElement e = GetFocusElement();
                if (e != null)
                {
                    OnSelect(e);
                }
                else
                {
                    // Set focus on first widget.
                    widgets[0].SetFocus();
                }

                return true;
            }
            
            // Use up/down, right/left arrow keys to cycle focus through slices.
            int index = GetFocusIndex();
            if (index == -1)
            {
                // Set focus on first widget.
                widgets[0].SetFocus();
            }
            else
            {
                if (input.Key == Keys.Up || input.Key == Keys.Right)
                {
                    int i = (index + 1) % widgets.Count;
                    widgets[i].SetFocus();
                    return true;
                }
                if (input.Key == Keys.Down || input.Key == Keys.Left)
                {
                    int i = (index + widgets.Count - 1) % widgets.Count;
                    widgets[i].SetFocus();
                    return true;
                }
            }

            return base.ProcessKeyboardEvent(input);
        }   // end of ProcessKeyboardEvent()

        public override bool ProcessGamePadEvent(GamePadInput pad)
        {
            Debug.Assert(Active);

            if (pad.ButtonA.WasPressed)
            {
                PieMenuElement e = GetFocusElement();
                if (e != null)
                {
                    OnSelect(e);
                }
                return true;
            }

            if (pad.ButtonB.WasPressed || pad.Back.WasPressed)
            {
                DialogManagerX.KillDialog(this);
                return true;
            }

            // Use leftStick to select element to focus on.
            Vector2 dir = pad.LeftStick;

            float magnitude = dir.Length();

            // If the position is outside our dead area.
            if (magnitude > 0.80f)
            {
                int focusIndex = GetFocusIndex();

                dir.Normalize();

                // Calc the angle of the stick.  
                // Need to do this in camera coordinates.
                // 0 is to the right and the angle grows clockwise.
                double angle = Math.Acos(dir.X);
                if (dir.Y > 0.0f)
                {
                    angle = MathHelper.TwoPi - angle;
                }

                // Calc the arc covered by each menu item.
                double arcItem = (MathHelper.TwoPi / (double)widgets.Count);

                if (focusIndex == -1)
                {
                    // Set focus on tile which most closely matches stick direction.
                    PieMenuElement best = null;
                    double bestAngle = float.MaxValue;
                    foreach (PieMenuElement e in widgets)
                    {
                        double selectedAngle = e.CenterAngle;

                        // Calc the stick angle relative to this angle.  Positive is clockwise...
                        double relative = angle - selectedAngle;
                        if (relative > MathHelper.Pi)
                        {
                            relative = relative - MathHelper.TwoPi;
                        }
                        else if (relative < -MathHelper.Pi)
                        {
                            relative = relative + MathHelper.TwoPi;
                        }

                        relative = Math.Abs(relative);

                        if (relative < bestAngle)
                        {
                            bestAngle = relative;
                            best = e;
                        }
                    }

                    best.SetFocus();
                }
                else
                {
                    // We already have a selected item, so we want to see if the selection has changed.

                    // Calc angle for center of selected item.
                    double selectedAngle = GetFocusElement().CenterAngle;

                    // Calc the stick angle relative to this angle.  Positive is clockwise...
                    double relative = angle - selectedAngle;
                    if (relative > MathHelper.Pi)
                    {
                        relative = relative - MathHelper.TwoPi;
                    }
                    else if (relative < -MathHelper.Pi)
                    {
                        relative = relative + MathHelper.TwoPi;
                    }

                    // Calc max relative angle we need before switching to the 
                    // next item.  Use half the width of the pie segment plus 
                    // a little extra to provide some hysteresis.
                    double maxAngle = arcItem / 2.0f;
                    // Only apply hysteresis when we have more that 4 items in the
                    // pie.  Use 1/3 of the width of the segment.
                    if (widgets.Count > 4)
                    {
                        maxAngle += arcItem / 3.0f;
                    }

                    // Calc new focusIndex.
                    if (relative > maxAngle)
                    {
                        focusIndex = (focusIndex + 1) % widgets.Count;
                    }
                    else if (relative < -maxAngle)
                    {
                        focusIndex = (focusIndex - 1 + widgets.Count) % widgets.Count;
                    }
                    // Set focus.
                    widgets[focusIndex].SetFocus();
                }
            }   // end if stick is pushed over far enough.

            return base.ProcessGamePadEvent(pad);
        }   // end of ProcessGamePadEvent()

        public override bool ProcessTouchTapEvent(TapGestureEventArgs gesture)
        {
            PieMenuElement e = gesture.HitObject as PieMenuElement;
            if (gesture.HitObject == e)
            {
                OnSelect(e);
            }
            else
            {
                // Tapped off of menu so kill.
                DialogManagerX.KillDialog(this);
            }

            return true;
        }   // end of ProcessTouchTapEvent()
        #endregion

        #region Internal

        /// <summary>
        /// Arranges elements for pie menu.  First element is positioned
        /// at the top of the circle and the rest follow clockwise.
        /// </summary>
        void PlaceElements()
        {
            // Calc radial size of each slice.
            float sliceAngle = MathHelper.TwoPi / widgets.Count;

            // Start with first slice at top.
            float rotationAngle = 0.75f * MathHelper.TwoPi - sliceAngle / 2.0f;

            float margin = 2.0f;    // 1/2 gap between pie slices.

            foreach (PieMenuElement e in widgets)
            {
                e.LoadContent();

                float angle0 = rotationAngle;
                float angle1 = (sliceAngle + rotationAngle) % MathHelper.TwoPi;

                e.CenterAngle = (angle0 + sliceAngle / 2.0f + MathHelper.TwoPi) % MathHelper.TwoPi;

                // Use smaller radius for menus with fewer elements.
                float radiusScaleFactor = 1.0f;
                if (widgets.Count < 5)
                {
                    radiusScaleFactor = 0.5f + 0.1f * widgets.Count;
                }
                e.InnerRadius = 180.0f * radiusScaleFactor;
                e.OuterRadius = 360.0f * radiusScaleFactor;

                e.EdgeNormal0 = new Vector2(-(float)Math.Sin(angle0), (float)Math.Cos(angle0));
                e.EdgeNormal1 = new Vector2((float)Math.Sin(angle1), -(float)Math.Cos(angle1));

                float midAngle = (angle0 + angle1) / 2.0f;

                // If wrapping then we need to fix midAngle.
                if (angle0 > angle1)
                {
                    midAngle -= MathHelper.Pi;
                }
                Vector2 vectorToTileCenter = new Vector2((float)Math.Cos(midAngle), (float)Math.Sin(midAngle));
                //e.EdgeIntersect = center.Value + (margin / (float)Math.Sin(sliceAngle / 2.0f)) * vectorToTileCenter;
                e.EdgeIntersect = (margin / (float)Math.Sin(sliceAngle / 2.0f)) * vectorToTileCenter;

                // Calc bounding rect.
                RectangleF rect = new RectangleF();
                // Start by adding 4 "corners"
                rect.Position = e.Center + e.InnerRadius * new Vector2((float)Math.Cos(angle0), (float)Math.Sin(angle0));
                rect.ExpandToInclude(e.Center + e.InnerRadius * new Vector2((float)Math.Cos(angle1), (float)Math.Sin(angle1)));
                rect.ExpandToInclude(e.Center + e.OuterRadius * new Vector2((float)Math.Cos(angle0), (float)Math.Sin(angle0)));
                rect.ExpandToInclude(e.Center + e.OuterRadius * new Vector2((float)Math.Cos(angle1), (float)Math.Sin(angle1)));

                // Then if arc of slice crosses an axis, add a point for that axis.
                if (angle0 < angle1)
                {
                    // Normal range. 
                    if (angle0 < MathHelper.PiOver2 && angle1 > MathHelper.PiOver2)
                    {
                        rect.ExpandToInclude(e.Center + e.OuterRadius * Vector2.UnitY);
                    }
                    if (angle0 < MathHelper.Pi && angle1 > MathHelper.Pi)
                    {
                        rect.ExpandToInclude(e.Center - e.OuterRadius * Vector2.UnitX);
                    }
                    if (angle0 < MathHelper.Pi + MathHelper.PiOver2 && angle1 > MathHelper.Pi + MathHelper.PiOver2)
                    {
                        rect.ExpandToInclude(e.Center - e.OuterRadius * Vector2.UnitY);
                    }
                }
                else if (angle0 > angle1)
                {
                    // Wraps across 0.  Implies that angle0 > Pi so we need to test starting after that.
                    float tmpAngle1 = angle1 + MathHelper.TwoPi;
                    if (angle0 < MathHelper.Pi + MathHelper.PiOver2 && tmpAngle1 > MathHelper.Pi + MathHelper.PiOver2)
                    {
                        rect.ExpandToInclude(e.Center - e.OuterRadius * Vector2.UnitY);
                    }
                    if (angle0 < MathHelper.TwoPi && tmpAngle1 > MathHelper.TwoPi)
                    {
                        rect.ExpandToInclude(e.Center + e.OuterRadius * Vector2.UnitX);
                    }
                    if (angle0 < MathHelper.TwoPi + MathHelper.PiOver2 && tmpAngle1 > MathHelper.TwoPi + MathHelper.PiOver2)
                    {
                        rect.ExpandToInclude(e.Center + e.OuterRadius * Vector2.UnitY);
                    }
                }
                else
                {
                    // Full 360, 1 tile for the whole pie. 
                    // Add all 4 axis points.
                    rect.ExpandToInclude(e.Center + e.OuterRadius * Vector2.UnitX);
                    rect.ExpandToInclude(e.Center - e.OuterRadius * Vector2.UnitX);
                    rect.ExpandToInclude(e.Center + e.OuterRadius * Vector2.UnitY);
                    rect.ExpandToInclude(e.Center - e.OuterRadius * Vector2.UnitY);

                    // Also need to special case edgeNormals.
                    // Put tile center directly up.
                    vectorToTileCenter = -Vector2.UnitY;
                    // Put edgeIntersect fully below the shape.
                    e.EdgeIntersect = e.Center + e.OuterRadius * Vector2.UnitY;
                    // Point both normals up.
                    e.EdgeNormal0 = -Vector2.UnitY;
                    e.EdgeNormal1 = -Vector2.UnitY;

                }

                // Finaly inflate to cover edgeBlend and then lock down to pixel values.
                rect.Inflate(2.0f * Geometry.Geometry.DefaultEdgeBlend);

                // Copy new value to element.
                e.Rectangle = rect;

                // Calc padding to adjust for distortion of rect dur to rotation.
                {
                    // Calc center of where we think texture should go.
                    // TODO (****) in reality this may look better shifted slightly outward toward
                    // the fatter edge of the pie slice.  Also will need to adjust for the label.
                    Vector2 textureCenter = e.Center + (e.InnerRadius + e.OuterRadius) / 2.0f * vectorToTileCenter;
                    Vector2 size = new Vector2(e.Texture.Width, e.Texture.Height);
                    float magnification = 1.0f;
                    size *= magnification;

                    // Calc rect around texture.
                    RectangleF textureRect = new RectangleF();
                    textureRect.Size = size;
                    textureRect.Position = textureCenter - size / 2.0f;

                    // Calc padding from comparing textureRect to bounding rect.
                    e.Padding = new Padding((int)(textureRect.Left - rect.Left),
                                            (int)(textureRect.Top - rect.Top),
                                            (int)(rect.Right - textureRect.Right),
                                            (int)(rect.Bottom - textureRect.Bottom));
                }

                rotationAngle = (rotationAngle + sliceAngle) % MathHelper.TwoPi;
            }   // end of loop over elements.

            
            // TODO (****) create tab list and dapd links manually.





            dirtyPlacement = false;
        }   // end of PlaceElements()

        /// <summary>
        /// Gets the index of the current infocus slice.
        /// </summary>
        /// <returns></returns>
        int GetFocusIndex()
        {
            int index = -1;
            for (int i = 0; i < widgets.Count; i++)
            {
                if (widgets[i].InFocus)
                {
                    index = i;
                    break;
                }
            }

            return index;
        }   // end of GetFocusIndex()

        PieMenuElement GetFocusElement()
        {
            int index = GetFocusIndex();
            PieMenuElement result = index == -1 ? null : widgets[GetFocusIndex()] as PieMenuElement;
            return result;
        }   // end of GetFocusElement()

        /// <summary>
        /// What happens with a menu choice is made.  Shold be implemented by
        /// the derived class.
        /// </summary>
        /// <param name="e"></param>
        abstract protected void OnSelect(PieMenuElement e);

        #endregion

    }   // end of class BasePieMenuDialog
}   // end of namespace KoiX.UI.Dialogs
