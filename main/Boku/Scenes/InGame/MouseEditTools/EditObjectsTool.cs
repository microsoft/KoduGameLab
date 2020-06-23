
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

using Boku;
using Boku.Audio;
using Boku.Base;
using Boku.Common;
using Boku.Common.Xml;
using Boku.Fx;
using Boku.UI;
using Boku.UI2D;
using Boku.Input;
using Boku.SimWorld;
using Boku.SimWorld.Terra;
using Boku.Common.Gesture;

namespace Boku.Scenes.InGame.MouseEditTools
{
    public class EditObjectsTool : BaseMouseEditTool, INeedsDeviceReset
    {
        #region Members

        private static EditObjectsTool instance = null;

        private MouseMenu noActorMenu = new MouseMenu();
        private MouseMenu actorMenu = new MouseMenu();

        // Actor under the cursor.
        private GameActor focusActor = null;        // Under cursor.
        private GameActor cachedActor = null;       // Actor whose creatables list has been cached.
        private int focusColorIndex;                // Color index of focus actor's color in ColorPalette.
        private GameActor selectedActor = null;     // Dragging.
        private GameActor menuActor = null;         // Actor under cursor when menu activated.
        private Vector3 menuCursorPosition;         // Position of cursor when menu activated.

        private string pasteMenuString = null;      // string used by paste option.  Is dynamic based on what is in the paste buffer.

        private Texture2D closeSquareTexture = null;        // Current texture we're using.
        private Texture2D closeSquareLitTexture = null;     // Selected version.
        private Texture2D closeSquareUnlitTexture = null;   // Unselected version.
        private Vector2 closePosition;
        private Vector2 closeSize;

        private UIGridModularFloatSliderElement slider = null;
        private bool sliderActive = false;
        private UIGridElement.ParamBlob blob = new UIGridElement.ParamBlob();

        /// <summary>
        /// When set, we don't process input for this scene. This is used to ensure that on the
        /// same frame that the tool is activated we don't end up responding to events which
        /// activated the tool.
        /// </summary>
        private bool ignoreInput= false;



        #endregion Members

        #region Accessors

        public GameActor FocusActor
        {
            get { return focusActor; }
            set
            {
                if (focusActor != value)
                {
                    focusActor = value;
                    if (focusActor != null && focusActor != cachedActor)
                    {
                        cachedActor = focusActor;
                        cachedActor.CacheCreatables();
                    }
                }
            }
        }
        public GameActor SelectedActor
        {
            get { return selectedActor; }
        }

        public MouseMenu ActorMenu
        {
            get { return actorMenu; }
        }

        public MouseMenu NoActorMenu
        {
            get { return noActorMenu; }
        }

        /// <summary>
        /// Are any of the menus active?
        /// </summary>
        public bool MenusActive
        {
            get { return ActorMenu.Active || NoActorMenu.Active || inGame.shared.textLineDialog.Active; }
        }

        /// <summary>
        /// Are one of the sliders for tweaking bots active?
        /// </summary>
        public bool SliderActive
        {
            get { return sliderActive; }
        }

        /// <summary>
        /// Are we currently dragging an actor around?
        /// </summary>
        public bool DraggingObject
        {
            get { return selectedActor != null; }
        }

        #endregion

        #region Public

        // c'tor
        public EditObjectsTool()
        {
            HelpOverlayID = @"EditObjects";

            // We don't want to see any brush rendered for this tool.
            prevBrushIndex = -1;

            // Get references.
            inGame = Boku.InGame.inGame;
            shared = inGame.shared;

            SetUpMenus();

            // Set up blob for slider.
            blob.width = 512.0f / 96.0f;
            blob.height = blob.width / 5.0f;
            blob.edgeSize = 0.06f;
            blob.Font = UI2D.Shared.GetGameFont24Bold;
            blob.textColor = new Color(20, 20, 20);
            blob.normalMapName = @"Slant0Smoothed5NormalMap";
            blob.justify = UIGridElement.Justification.Center;

            slider = new UIGridModularFloatSliderElement(blob, "To Be Replaced");
            slider.OnChange = SliderOnChange;
            Matrix mat = Matrix.CreateTranslation(new Vector3(0.0f, -4.0f, 0.0f));
            slider.WorldMatrix = mat;

        }   // end of c'tor

        /// <summary>
        /// Callback called by slider whenever current value changes.
        /// </summary>
        /// <param name="value"></param>
        public void SliderOnChange(float value)
        {
            // Figure out which value to update.
            if (slider.Label == Strings.Localize("mouseEdit.sizeValue"))
            {
                menuActor.ReScale = value;
            }
            else if (slider.Label == Strings.Localize("mouseEdit.rotationValue"))
            {
                menuActor.Movement.RotationZ = MathHelper.ToRadians(value);
            }
            else if (slider.Label == Strings.Localize("mouseEdit.heightValue"))
            {
                menuActor.HeightOffset = value - menuActor.DefaultEditHeight;
                //menuActor.Movement.Altitude = menuActor.ReScale * menuActor.GetPreferredAltitude();
                menuActor.Movement.Altitude = menuActor.GetPreferredAltitude();
            }
            else
            {
                Debug.Assert(false, "Should never get here.");
            }

            Boku.InGame.IsLevelDirty = true;

        }   // end of SliderOnChange()

        public static BaseMouseEditTool GetInstance()
        {
            if (instance == null)
            {
                instance = new EditObjectsTool();
            }
            return instance;
        }   // end of GetInstance()

        Vector3 actorOffset;

        private void HandleSliderInput(Camera uicamera)
        {
            if (sliderActive)
            {
                Vector2 dragPos;
                TouchContact touch = null;
                if (GamePadInput.ActiveMode == GamePadInput.InputMode.Touch)
                {
                    touch = TouchInput.GetOldestTouch();
                    if (touch != null)
                        dragPos = touch.position;
                    else
                        return;
                }
                else
                {
                    dragPos = new Vector2(MouseInput.Position.X, MouseInput.Position.Y);
                }


                // Convert mouse hit into UV coords. (GetHitUV for MouseInput and TouchInput have same implementation...)
                Matrix worldMat = Matrix.Invert(slider.WorldMatrix);
                Vector2 hitUV = TouchInput.GetHitUV(dragPos, uicamera, ref worldMat, slider.Size.X, slider.Size.Y, useRtCoords: false); ;

                bool outside = true;
                if (hitUV.X >= 0 && hitUV.X < 1 && hitUV.Y >= 0 && hitUV.Y < 1)
                {
                    /// \TODO : add touch input for sliders
                    if (GamePadInput.ActiveMode == GamePadInput.InputMode.Touch)
                        slider.HandleTouchInput(touch, hitUV);
                    else
                        slider.HandleMouseInput(hitUV);
                    outside = false;
                }

                Matrix identity = Matrix.Identity;
                slider.Update(ref identity);

                //Get Close position matrix.
                worldMat = slider.WorldMatrix;
                worldMat.Translation = new Vector3(closePosition.X, closePosition.Y, worldMat.Translation.Z);
                worldMat = Matrix.Invert(worldMat);

                hitUV = TouchInput.GetHitUV(dragPos, uicamera, ref worldMat, closeSize.X, closeSize.Y, useRtCoords: false);

                if (hitUV.X >= 0 && hitUV.X < 1 && hitUV.Y >= 0 && hitUV.Y < 1)
                {
                    if (GamePadInput.ActiveMode == GamePadInput.InputMode.Touch)
                    {
                        if (touch.phase == TouchPhase.Began)
                        {
                            touch.TouchedObject = this;
                        }
                        if (touch.phase == TouchPhase.Ended && touch.TouchedObject == this)
                        {
                            sliderActive = false;
                        }

                        if (touch.TouchedObject == this)
                        {
                            closeSquareTexture = closeSquareLitTexture;
                        }

                    }
                    else
                    {
                        if (MouseInput.Left.WasPressed)
                        {
                            MouseInput.ClickedOnObject = this;
                        }
                        if (MouseInput.Left.WasReleased && MouseInput.ClickedOnObject == this)
                        {
                            sliderActive = false;
                        }

                        if (MouseInput.ClickedOnObject == this)
                        {
                            closeSquareTexture = closeSquareLitTexture;
                        }
                    }
                }
                else
                {
                    closeSquareTexture = closeSquareUnlitTexture;
                    if (GamePadInput.ActiveMode == GamePadInput.InputMode.Touch)
                    {
                        // If user TOUCHED outside of slide and close box, close slider.
                        if (TouchInput.WasTouched && outside)
                        {
                            sliderActive = false;
                        }
                    }
                    else
                    {
                        // If user clicked outside of slide and close box, close slider.
                        if (MouseInput.Left.WasPressed && outside)
                        {
                            MouseInput.Left.ClearAllWasPressedState();
                            sliderActive = false;
                        }
                    }
                }
                if (Actions.Cancel.WasPressed)
                {
                    Actions.Cancel.ClearAllWasPressedState();
                    sliderActive = false;
                }
            }
        }

        private void HandleTouchInput(Camera uicamera)
        {
            //TODO: this method is now extremely unwieldly - now that we have the basics down, refactor and consolidate the various checks
            if (GamePadInput.ActiveMode != GamePadInput.InputMode.Touch) { return; }

            // If the mouse took over from the touch, it should clear any
            // highlights the touch had going.
            Boku.InGame.inGame.MouseEdit.Clear();

            Camera camera = Boku.InGame.inGame.shared.camera;
            TouchEdit touchEdit = Boku.InGame.inGame.TouchEdit;
            TouchEdit.TouchHitInfo hitInfo = null;

            //keep track of previous focus actor for comparisons this frame
            GameActor previousFocusActor = FocusActor;

            hitInfo = TouchEdit.HitInfo;

            //Check for color pallet hits
            if (Boku.InGame.ColorPalette.Active && (FocusActor != null) && !actorMenu.Active && !noActorMenu.Active)
            {
                Classification.Colors touchColor = Boku.InGame.ColorPalette.GetColorFromTouch();
                if ((touchColor != Classification.Colors.None) && (FocusActor.ClassColor != touchColor))
                {
                    FocusActor.ClassColor = touchColor;
                    focusColorIndex = ColorPalette.GetIndexFromColor(touchColor);
                    Foley.PlayColorChange();
                    Boku.InGame.IsLevelDirty = true;
                }
                // For the duration of the color palette handling touch, all touch inputs are deferred.
                if (Boku.InGame.ColorPalette.HandlingTouch)
                {
                    return;
                }            
            }

            bool hasNonUITouch = touchEdit.HasNonUITouch();
            bool hasValidTap = TouchGestureManager.Get().TapGesture.WasValidEditObjectTap;

            touchEdit.DoObject(camera);

            //check for tap to adjust hit actor
            if (hasValidTap || 
                TouchGestureManager.Get().DoubleTapGesture.WasRecognized ||
                TouchGestureManager.Get().TouchHoldGesture.WasRecognized ||
                TouchGestureManager.Get().TouchHoldGesture.SlightHoldMade ||
                (TouchGestureManager.Get().DragGesture.IsDragging && TouchInput.InitialActorHit!=null))
            {
                if (hasNonUITouch && TouchGestureManager.Get().DragGesture.IsDragging && TouchInput.InitialActorHit != null)
                {
                    FocusActor = TouchInput.InitialActorHit;
                    focusColorIndex = ColorPalette.GetIndexFromColor(FocusActor.Classification.Color);
                    Boku.InGame.ColorPalette.Active = true;
                }
                else if (hasNonUITouch && hitInfo.HaveActor)
                {
                    FocusActor = hitInfo.ActorHit;
                    focusColorIndex = ColorPalette.GetIndexFromColor(FocusActor.Classification.Color);
                    Boku.InGame.ColorPalette.Active = true;
                }
                else
                {
                    FocusActor = null;
                    Boku.InGame.ColorPalette.Active = false;
                }
            }

            //check for double tap on terrain to bring up add actor
            if (hasNonUITouch && 
                TouchGestureManager.Get().DoubleTapGesture.WasRecognized &&
                FocusActor == null &&
                !MenusActive && !SliderActive && inGame.editObjectUpdateObj.newItemSelectorShim.State != UIShim.States.Active)
            {
                // No actor in focus so activate AddItem menu.
                Vector2 position = new Vector2(hitInfo.TerrainPosition.X, hitInfo.TerrainPosition.Y);
                inGame.editObjectUpdateObj.ActivateNewItemSelector(position, true);
            }

            //handle dragging an actor
            if (TouchGestureManager.Get().DragGesture.IsDragging && TouchInput.InitialActorHit!=null && FocusActor != null)
            {
                //clear out menu if up
                actorMenu.Deactivate();
                noActorMenu.Deactivate();

                //select the focus actor when we start dragging
                if (selectedActor == null)
                {


                    // Start draggin if over actor.
                    selectedActor = FocusActor;
                    actorOffset = selectedActor.Movement.Position - hitInfo.TerrainPosition;
                }

                Vector3 position = hitInfo.TerrainPosition + actorOffset;
                selectedActor.Movement.Position = Boku.InGame.SnapPosition(position);

                // Try and keep the bot directly under the mouse cursor while still being at the correct height.
                // A possible alternative would be to use the cursor's 2d position for the bot and just have the
                // bot float at the appropriate height over the cursor.  This would allow more exact placement of
                // bots over terrain but it would mean a visual disconnect between where the cursor is and where
                // the bot is.  There would also be a jump when the bot is first clicked on since the terrain
                // position of the cursor is most likely further back than the bot's current position.
                if (hitInfo.VerticalOffset == 0.0f)
                {
                    Vector3 terrainToCameraDir = hitInfo.TerrainPosition - camera.From;
                    terrainToCameraDir.Normalize();
                    position = hitInfo.TerrainPosition + terrainToCameraDir * (selectedActor.EditHeight / terrainToCameraDir.Z);
                    selectedActor.Movement.Position = Boku.InGame.SnapPosition(position);
                }

                // If the actor is supposed to stay above water, try to enforce that.
                // This can have some strange visual effects since it forces the actor to 
                // float above where the mouse cursor is but the alternative is to have
                // actor get dragged under water.
                if (selectedActor.StayAboveWater)
                {
                    float waterAlt = Terrain.GetWaterBase(position);
                    if (waterAlt != 0)
                    {
                        position.Z = waterAlt + selectedActor.EditHeight;
                        selectedActor.Movement.Position = Boku.InGame.SnapPosition(position);
                    }
                }

                Boku.InGame.IsLevelDirty = true;
            }
            else
            {
                selectedActor = null;


                //rules for context menus:
                // tap + hold -> always bring up menu (terrain or actor accordingly) 
                // double tap -> bring up menu if over an actor (actor only)
                // single tap -> bring up menu if over an actor that was already selected (actor only)
                if (hasNonUITouch && 
                    (TouchGestureManager.Get().TouchHoldGesture.WasRecognized ||
                    (FocusActor != null && TouchGestureManager.Get().DoubleTapGesture.WasRecognized) ||
                    (FocusActor != null && hasValidTap && FocusActor == previousFocusActor))) 
                {
                    menuActor = FocusActor;
                    menuCursorPosition = hitInfo.TerrainPosition;

                    // We need to do this repeatedly since the Paste option will
                    // change depending on what's in the cut/paste buffer.
                    SetUpMenus();

                    if (FocusActor == null)
                    {
                        actorMenu.Deactivate();
                        noActorMenu.Activate(TouchInput.GetOldestTouch().position);
                    }
                    else
                    {
                        noActorMenu.Deactivate();
                        actorMenu.Activate(TouchInput.GetOldestTouch().position);
                        // Turn off any thought balloons so they don't clutter the menu.
                        ThoughtBalloonManager.RemoveThoughts(FocusActor);
                    }
                }

                // Handle two finger actions. Only enabled when not dragging.
                if (hasNonUITouch && TouchInput.TouchCount == 2 && selectedActor == null)
                {


                    PinchGestureRecognizer pinchGesture = TouchGestureManager.Get().GetActiveGesture( TouchGestureType.Pinch, TouchGestureType.Rotate ) as PinchGestureRecognizer;
                    if ( pinchGesture != null && pinchGesture.IsPinching )
                    {
                        //Debug.WriteLine("Pinching... Scale: "+ pinchGesture.Scale );
                        DoScaleActor( pinchGesture.DeltaScale, FocusActor );
                    }

                    RotationGestureRecognizer rotationGesture = TouchGestureManager.Get().GetActiveGesture(TouchGestureType.Rotate, TouchGestureType.Pinch) as RotationGestureRecognizer;
                    if (null != rotationGesture && rotationGesture.IsRotating)
                    {
                        DoRotateActor(rotationGesture.RotationDelta, FocusActor );
                    }
                }
            }

            if (TouchGestureManager.Get().RotateGesture.IsValidated || 
                TouchGestureManager.Get().PinchGesture.IsValidated || 
                TouchGestureManager.Get().DoubleDragGesture.IsValidated)
            {
                //turn off menu if rotating, pinching or double dragging (i.e. terrain manipulation)
                actorMenu.Deactivate();
                noActorMenu.Deactivate();
            }

            noActorMenu.Update();
            actorMenu.Update();

            // Support for changing tree types via up/down arrow keys.
            if (FocusActor != null && FocusActor.Classification.name == "tree")
            {
                inGame.editObjectUpdateObj.MakeTreeChange(FocusActor);
            }

            //
            // Figure out help overlay mode.
            //
            if (inGame.editObjectUpdateObj.newItemSelectorShim.State == UIShim.States.Active)
            {
                // The pie menu is active.
                HelpOverlay.ReplaceTop("MouseEditAddItemMenu");
            }
            else if (hitInfo != null && hitInfo.HaveActor)
            {
                // We have an actor in focus.
                if (FocusActor != null && FocusActor.Classification.name == "tree")
                {
                    HelpOverlay.ReplaceTop("MouseEditEditObjectFocusTree");
                }
                else
                {
                    HelpOverlay.ReplaceTop("MouseEditEditObjectFocus");
                }
            }            
        }

        private void DoScaleActor( float deltaScale, GameActor actorToScale )
        {
            if (actorToScale != null)
            {
                actorToScale.ReScale = MathHelper.Clamp(actorToScale.ReScale + deltaScale, EditObjectParameters.k_ObjectMinScale, EditObjectParameters.k_ObjectMaxScale);

                Boku.InGame.IsLevelDirty = true;
            }
        }

        private void DoRotateActor( float rotationDelta, GameActor actorToRotate )
        {
            if (actorToRotate != null)
            {
                actorToRotate.Movement.RotationZ -= rotationDelta;
                Boku.InGame.IsLevelDirty = true;
            }
        }

        private void HandleMouseInput(Camera uicamera)
        {
            if (GamePadInput.ActiveMode != GamePadInput.InputMode.KeyboardMouse) { return; }

            // If the mouse took over from the touch, it should clear any
            // highlights the touch had going.
            Boku.InGame.inGame.TouchEdit.Clear();

            Camera camera = Boku.InGame.inGame.shared.camera;
            MouseEdit mouseEdit = Boku.InGame.inGame.MouseEdit;
            MouseEdit.MouseHitInfo hitInfo = null;

            hitInfo = MouseEdit.HitInfo;
            mouseEdit.DoObject(camera);

            if (hitInfo.HaveActor)
            {
                FocusActor = hitInfo.ActorHit;
                focusColorIndex = ColorPalette.GetIndexFromColor(FocusActor.Classification.Color);
                Boku.InGame.ColorPalette.Active = true;
            }
            else
            {
                FocusActor = null;
                Boku.InGame.ColorPalette.Active = false;
            }

            if (MouseInput.Left.WasReleased)
            {
                selectedActor = null;
            }

            //don't add if we detected a paste
            if (MouseInput.Left.WasPressed && !MenusActive && (mouseEdit.MiddleAction != true))
            {
                MouseInput.Left.ClearAllWasPressedState();

                if (FocusActor != null)
                {
                    // Start draggin if over actor.
                    selectedActor = FocusActor;
                    actorOffset = selectedActor.Movement.Position - hitInfo.TerrainPosition;
                }
                else if (!MenusActive && !SliderActive && inGame.editObjectUpdateObj.newItemSelectorShim.State != UIShim.States.Active)
                {
                    // No actor in focus so activate AddItem menu.
                    Vector2 position = new Vector2(hitInfo.TerrainPosition.X, hitInfo.TerrainPosition.Y);
                    inGame.editObjectUpdateObj.ActivateNewItemSelector(position, true);
                }
            }

            if (MouseInput.Left.IsPressed &&
                (selectedActor != null) &&
                (hitInfo != null))
            {
                Vector3 position = hitInfo.TerrainPosition + actorOffset;
                selectedActor.Movement.Position = Boku.InGame.SnapPosition(position);

                // Try and keep the bot directly under the mouse cursor while still being at the correct height.
                // A possible alternative would be to use the cursor's 2d position for the bot and just have the
                // bot float at the appropriate height over the cursor.  This would allow more exact placement of
                // bots over terrain but it would mean a visual disconnect between where the cursor is and where
                // the bot is.  There would also be a jump when the bot is first clicked on since the terrain
                // position of the cursor is most likely further back than the bot's current position.
                if (hitInfo.VerticalOffset == 0.0f)
                {
                    Vector3 terrainToCameraDir = hitInfo.TerrainPosition - camera.From;
                    terrainToCameraDir.Normalize();
                    position = hitInfo.TerrainPosition + terrainToCameraDir * (selectedActor.EditHeight / terrainToCameraDir.Z);
                    selectedActor.Movement.Position = Boku.InGame.SnapPosition(position);
                }

                // If the actor is supposed to stay above water, try to enforce that.
                // This can have some strange visual effects since it forces the actor to 
                // float above where the mouse cursor is but the alternative is to have
                // actor get dragged under water.
                if (selectedActor.StayAboveWater)
                {
                    float waterAlt = Terrain.GetWaterBase(position);
                    if (waterAlt != 0)
                    {
                        position.Z = waterAlt + selectedActor.EditHeight;
                        selectedActor.Movement.Position = Boku.InGame.SnapPosition(position);
                    }
                }

                Boku.InGame.IsLevelDirty = true;
            }

            if (MouseInput.Right.WasReleased)
            {
                menuActor = FocusActor;
                menuCursorPosition = hitInfo.TerrainPosition;

                // We need to do this repeatedly since the Paste option will
                // change depending on what's in the cut/paste buffer.
                SetUpMenus();

                if (FocusActor == null)
                {
                    actorMenu.Deactivate();
                    noActorMenu.Activate(new Vector2(MouseInput.Position.X, MouseInput.Position.Y));
                }
                else
                {
                    noActorMenu.Deactivate();
                    actorMenu.Activate(new Vector2(MouseInput.Position.X, MouseInput.Position.Y));
                    // Turn off any thought balloons so they don't clutter the menu.
                    ThoughtBalloonManager.RemoveThoughts(FocusActor);
                }
            }

            noActorMenu.Update();
            actorMenu.Update();

            // Support for changing tree types via up/down arrow keys.
            if (FocusActor != null && FocusActor.Classification.name == "tree")
            {
                inGame.editObjectUpdateObj.MakeTreeChange(FocusActor);
            }

            // Color palette support.
            if (FocusActor != null && !MenusActive && !sliderActive)
            {
                int numColors = Boku.InGame.ColorPalette.NumEntries;
                if (Actions.Left.WasPressedOrRepeat)
                {
                    focusColorIndex = (focusColorIndex + numColors - 1) % numColors;
                    shared.curObjectColor = focusColorIndex;
                    FocusActor.ClassColor = ColorPalette.GetColorFromIndex(focusColorIndex);
                    Foley.PlayColorChange();
                    Boku.InGame.IsLevelDirty = true;
                }
                if (Actions.Right.WasPressedOrRepeat)
                {
                    focusColorIndex = (focusColorIndex + 1) % numColors;
                    shared.curObjectColor = focusColorIndex;
                    FocusActor.ClassColor = ColorPalette.GetColorFromIndex(focusColorIndex);
                    Foley.PlayColorChange();
                    Boku.InGame.IsLevelDirty = true;
                }
            }

            // Align NSEW
            if (FocusActor != null && !MenusActive && !sliderActive)
            {
                if (Actions.Up.WasPressedOrRepeat)
                {
                    // Rotate clockwise.
                    float targetRotation = MathHelper.PiOver2 * (int)((FocusActor.Movement.RotationZ + MathHelper.TwoPi - 0.0001f) / MathHelper.PiOver2);
                    FocusActor.Movement.RotationZ = targetRotation;
                    Foley.PlayClickUp();
                    Boku.InGame.IsLevelDirty = true;
                }
                if (Actions.Down.WasPressedOrRepeat)
                {
                    // Rotate counter-clockwise.
                    float targetRotation = MathHelper.PiOver2 * (int)((FocusActor.Movement.RotationZ + MathHelper.PiOver2 + 0.0001f)/MathHelper.PiOver2);
                    FocusActor.Movement.RotationZ = targetRotation;
                    Foley.PlayClickDown();
                    Boku.InGame.IsLevelDirty = true;
                }
            }

            // Cut/Copy/Paste via keyboard.
            if (Actions.Cut.WasPressed && FocusActor != null)
            {
                inGame.editObjectUpdateObj.CutAction(FocusActor);
            }
            if (Actions.Copy.WasPressed && FocusActor != null)
            {
                inGame.editObjectUpdateObj.CopyAction(FocusActor);
            }
            if (Actions.Paste.WasPressed)
            {
                inGame.editObjectUpdateObj.PasteAction(null, hitInfo.TerrainPosition);
            }

            //
            // Figure out help overlay mode.
            //
            if (inGame.editObjectUpdateObj.newItemSelectorShim.State == UIShim.States.Active)
            {
                // The pie menu is active.
                HelpOverlay.ReplaceTop("MouseObjectEditAddItemMenu");
            }
            else if (hitInfo != null && hitInfo.HaveActor)
            {
                // We have an actor in focus.
                if (FocusActor != null && FocusActor.Classification.name == "tree")
                {
                    HelpOverlay.ReplaceTop("MouseEditEditObjectFocusTree");
                }
                else
                {
                    HelpOverlay.ReplaceTop("MouseEditEditObjectFocus");
                }
            }
        }

        public override void Update(Camera uicamera)
        {
            if (Active)
            {
                // We need to update our child objects first so they have first shot at grabbing any input.
                inGame.shared.textLineDialog.Update();
                if (inGame.shared.textLineDialog.Active)
                {
                    // Don't let unused input leak under.
                    return;
                }

                inGame.shared.addItemHelpCard.Update();

                HandleSliderInput(uicamera);

                // If the slider is still active, we're done.  
                // We don't want any other input acted upon.
                if (sliderActive)
                {
                    return;
                }
                // If pie menu is active, we don't want any input acted on.
                else if (inGame.editObjectUpdateObj.newItemSelectorShim.State == UIShim.States.Active)
                {
                    return;
                }

                if (!ignoreInput)
                {
                    HandleTouchInput(uicamera);
                    HandleMouseInput(uicamera);
                }

                ignoreInput = false;
            }   // end if active.

            // This tool is odd enough compared to the standard 
            // tools that we don't want to call the base update.
            //base.Update();

        }   // end of Update()

        public void Render(Camera camera)
        {
            if (Active)
            {
                Boku.InGame.RenderColorMenu(focusColorIndex);

                if (inGame.shared.textLineDialog.Active)
                {
                    inGame.shared.textLineDialog.Render();
                }

                if (sliderActive)
                {
                    // Render menu using local camera.
                    Fx.ShaderGlobals.SetCamera(camera);

                    // Fit to bottom of screen adjusting for tutorial mode.

                    float tutScale = 1.0f;
                    if (BokuGame.ScreenSize.X > BokuGame.ScreenSize.Y)
                    {
                        float smallestRes = Math.Min(camera.Resolution.X, camera.Resolution.Y);
                        tutScale = (smallestRes > 0) ? BokuGame.ScreenSize.Y / smallestRes : 1.0f;
                    }
                    else
                    {
                        float biggestRes = Math.Max(camera.Resolution.X, camera.Resolution.Y);
                        tutScale = (biggestRes > 0) ? BokuGame.ScreenSize.X / biggestRes : 1.0f;
                    }

                    // Note 7.5 is the default vertical height for the UI camera.
                    float y = -7.5f / 2.0f + tutScale * slider.Size.Y;

                    slider.WorldMatrix = Matrix.CreateScale(tutScale) * Matrix.CreateTranslation(new Vector3(0.0f, y, 0.0f));
                    slider.position = slider.WorldMatrix.Translation;
                    slider.Render(camera);

                    // Render the CloseBox.
                    CameraSpaceQuad csquad = CameraSpaceQuad.GetInstance();
                    float scale = 0.33f;
                    closeSize = tutScale * scale * new Vector2(slider.Height, slider.Height);
                    // Calc position to have top even with top of slider.
                    closePosition = tutScale * new Vector2(slider.WorldMatrix.Translation.X + slider.Width / 2.0f + closeSize.X / 2.0f, slider.WorldMatrix.Translation.Y + (1.0f - scale) * slider.Height / 2.0f);
                    closePosition = new Vector2(slider.WorldMatrix.Translation.X, slider.WorldMatrix.Translation.Y);
                    closePosition.X += tutScale * slider.Size.X / 2.0f + closeSize.X / 2.0f;
                    closePosition.Y += tutScale * slider.Size.Y / 2.0f - closeSize.Y / 2.0f;
                    // Hack adjust for issues with art.
                    closePosition += tutScale * new Vector2(-0.04f, -0.03f);
                    csquad.Render(camera, closeSquareTexture, closePosition, closeSize, "TexturedRegularAlpha");
                }
            }
        }   // end of Render()

        #endregion Public

        #region Internal

        private void SetUpMenus()
        {
            //
            // NoActorMenu
            //

            noActorMenu.DeleteAll();
            noActorMenu.AddText(Strings.Localize("mouseEdit.addObject"));
            noActorMenu.AddText(Strings.Localize("mouseEdit.worldTweak"));
            // TODO (mouse) Only if paste buffer contains something.  Note this means calling SetUpMenus if this changes.
            // Also: Try and get bot's name and tack it on to the end of the paste.

            pasteMenuString = Strings.Localize("mouseEdit.paste");
            GameActor actor = inGame.editObjectUpdateObj.CutPasteObject as GameActor;
            if (actor == null)
            {
                pasteMenuString += " (" + Strings.Localize("mouseEdit.empty") + ")";
            }
            else
            {
                pasteMenuString += " (" + actor.DisplayNameNumber + ")";
            }

            noActorMenu.AddText(pasteMenuString);
            noActorMenu.OnSelect = NoActorOnSelect;
            noActorMenu.OnCancel = OnCancel;

            //
            // ActorMenu
            //

            actorMenu.DeleteAll();
            actorMenu.AddText(Strings.Localize("mouseEdit.program"));
            actorMenu.AddText(Strings.Localize("mouseEdit.objectTweak"));
            actorMenu.AddText(Strings.Localize("mouseEdit.rename"));
            actorMenu.AddText(Strings.Localize("mouseEdit.cut"));
            actorMenu.AddText(Strings.Localize("mouseEdit.copy"));
            actorMenu.AddText(Strings.Localize("mouseEdit.size"));
            actorMenu.AddText(Strings.Localize("mouseEdit.rotate"));
            actorMenu.AddText(Strings.Localize("mouseEdit.height"));
            actorMenu.OnSelect = ActorOnSelect;
            actorMenu.OnCancel = OnCancel;

        }   // end of SetUpMenus();

        public void NoActorOnSelect(MouseMenu menu)
        {
            if (menu.CurString == Strings.Localize("mouseEdit.addObject"))
            {
                Vector2 position = new Vector2(menuCursorPosition.X, menuCursorPosition.Y);
                inGame.editObjectUpdateObj.ActivateNewItemSelector(position, true);
            }
            else if (menu.CurString == Strings.Localize("mouseEdit.worldTweak"))
            {
                inGame.CurrentUpdateMode = Boku.InGame.UpdateMode.EditWorldParameters;
                // Set return mode so we come back to ObjectEdit when done.
                inGame.mouseEditUpdateObj.ReturnMode = Boku.InGame.BaseEditUpdateObj.ToolMode.EditObject;
                inGame.touchEditUpdateObj.ReturnMode = Boku.InGame.BaseEditUpdateObj.ToolMode.EditObject;
            }
            else if (menu.CurString == pasteMenuString)
            {
                inGame.editObjectUpdateObj.PasteAction(null, menuCursorPosition);
            }
            else
            {
                Debug.Assert(false, "Should never get here.");
            }
        }   // end of NoActorOnSelect()

        public void ActorOnSelect(MouseMenu menu)
        {
            if (menu.CurString == Strings.Localize("mouseEdit.rename"))
            {
                if (menuActor != null)
                {
                    TextLineDialog.OnDialogDone callback = delegate(bool canceled, string newText)
                    {
                        if (!canceled && newText.Length > 0)
                        {
                            newText = TextHelper.FilterURLs(newText);
                            newText = TextHelper.FilterEmail(newText);

                            menuActor.DisplayName = newText;
                            Programming.NamedFilter.RegisterInCardSpace(menuActor);
                            Boku.InGame.IsLevelDirty = true;
                        }
                    };

                    TextLineEditor.ValidateText validateCallback = delegate(TextBlob textBlob)
                    {
                        // Deterimine if name is valid.
                        string name = textBlob.ScrubbedText;
                        name = name.Trim();
                        bool valid = !String.IsNullOrWhiteSpace(name);
                        return valid;
                    };

                    inGame.shared.textLineDialog.Activate(callback, menuActor.DisplayName, validateCallback);
                }
            }
            else if (menu.CurString == Strings.Localize("mouseEdit.program"))
            {
                inGame.ShowEditor(menuActor);
            }
            else if (menu.CurString == Strings.Localize("mouseEdit.objectTweak"))
            {
                if (menuActor != null)
                {
                    inGame.shared.editObjectParameters.Actor = menuActor;
                    inGame.CurrentUpdateMode = Boku.InGame.UpdateMode.EditObjectParameters;
                    // Set return mode so we come back to ObjectEdit when done.
                    inGame.mouseEditUpdateObj.ReturnMode = Boku.InGame.BaseEditUpdateObj.ToolMode.EditObject;
                    inGame.touchEditUpdateObj.ReturnMode = Boku.InGame.BaseEditUpdateObj.ToolMode.EditObject;
                }
            }
            else if (menu.CurString == Strings.Localize("mouseEdit.cut"))
            {
                inGame.editObjectUpdateObj.CutAction(menuActor);
            }
            else if (menu.CurString == Strings.Localize("mouseEdit.copy"))
           {
                inGame.editObjectUpdateObj.CopyAction(menuActor);
            }
            else if (menu.CurString == Strings.Localize("mouseEdit.size"))
            {
                slider.Label = Strings.Localize("mouseEdit.sizeValue");
                slider.MinValue = 0.2f;
                slider.MaxValue = 4.0f;
                slider.NumberOfDecimalPlaces = 1;
                slider.IncrementByAmount = 0.1f;

                slider.CurrentValueImmediate = menuActor.ReScale;

                slider.Selected = true;

                slider.RefreshTexture();

                sliderActive = true;
            }
            else if (menu.CurString == Strings.Localize("mouseEdit.rotate"))
            {
                slider.Label = Strings.Localize("mouseEdit.rotationValue");
                slider.MinValue = 0.0f;
                slider.MaxValue = 360.0f;
                slider.NumberOfDecimalPlaces = 0;
                slider.IncrementByAmount = 1.0f;

                slider.CurrentValueImmediate = MathHelper.ToDegrees(menuActor.Movement.RotationZ);

                slider.Selected = true;

                slider.RefreshTexture();

                sliderActive = true;
            }
            else if (menu.CurString == Strings.Localize("mouseEdit.height"))
            {
                slider.Label = Strings.Localize("mouseEdit.heightValue");
                slider.MinValue = menuActor.MinHeight;
                slider.MaxValue = 30.0f;
                slider.NumberOfDecimalPlaces = 2;
                slider.IncrementByAmount = 0.01f;

                slider.CurrentValueImmediate = menuActor.HeightOffset + menuActor.DefaultEditHeight;

                slider.Selected = true;

                slider.RefreshTexture();

                sliderActive = true;
            }
            else
            {
                Debug.Assert(false, "Should never get here.");
            }
        }   // end of ActorOnSelect()

        public void OnCancel(MouseMenu menu)
        {
            menuActor = null;
        }   // end of OnCancel()

        public override void OnActivate()
        {
            //base.OnActivate();
            inGame.HideCursor();

            // May have changed since last time.
            SetUpMenus();

            closeSquareTexture = closeSquareUnlitTexture;

            ignoreInput = true;
        }   // end of OnActivate()

        public override void OnDeactivate()
        {
            //base.OnDeactivate();
            // Ensure that the selection highlights are off.
            Boku.InGame.inGame.TouchEdit.Clear();
            Boku.InGame.inGame.MouseEdit.Clear();

            sliderActive = false;

            actorMenu.Deactivate();
            noActorMenu.Deactivate();

            //reset selection state on deactivation
            FocusActor = null;
            selectedActor = null;
            menuActor = null;
            //make sure the color palette doesn't stay up
            Boku.InGame.ColorPalette.Active = false;
        }   // end of OnDeactivate()
        #endregion Internal

        #region INeedsDeviceReset Members

        public void LoadContent(bool immediate)
        {
            BokuGame.Load(slider);

            if (closeSquareLitTexture == null)
            {
                closeSquareLitTexture = BokuGame.Load<Texture2D>(BokuGame.Settings.MediaPath + @"Textures\GridElements\CloseSquare");
            }
            if (closeSquareUnlitTexture == null)
            {
                closeSquareUnlitTexture = BokuGame.Load<Texture2D>(BokuGame.Settings.MediaPath + @"Textures\GridElements\CloseSquareDesat");
            }

        }

        public void InitDeviceResources(GraphicsDevice device)
        {
            slider.InitDeviceResources(device);
        }

        public void UnloadContent()
        {
            BokuGame.Unload(slider);
            BokuGame.Release(ref closeSquareLitTexture);
            BokuGame.Release(ref closeSquareUnlitTexture);
        }

        public void DeviceReset(GraphicsDevice device)
        {
            slider.DeviceReset(device);
        }

        #endregion
    }   // class EditObjectsTool

}   // end of namespace Boku.Scenes.InGame.MouseEditTools


