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
using Boku.Fx;
using Boku.Common;
using Boku.UI;
using Boku.UI2D;
using Boku.Input;
using Boku.SimWorld;
using Boku.SimWorld.Terra;
using Boku.Programming;
using Boku.Common.Gesture;

namespace Boku
{
    public class EditWorldParameters : INeedsDeviceReset
    {
        private Camera camera = null;
        private Matrix worldGrid = Matrix.Identity;

        private UIGrid grid;

        private UIGridModularHelpSquare helpSquare = null;
        private UIGridModularOKSquare okSquare = null;

        private enum EditMode
        {
            ChangeSettingMode,
            ProgrammingTileMode,
        };

        private EditMode editMode;

        // Controls appear in the order of this enum
        public enum Control
        {
            ChangeHistory,
            GlassWalls,
            CameraMode,
            StartingCamera,
            CameraSpringStrength,
            ShowCompass,
            ShowResourceMeter,
            EnableResourceLimiting,
            WaveHeight,
            WaterStrength,
            Sky,
            LightRig,
            WindMin,
            WindMax,
            PreGame,
            DebugPathFollow,
            DebugDisplayCollisions,
            DebugDisplayLinesOfPerception,
            DebugDisplayCurrentPage,
            FoleyVolume,
            MusicVolume,
            ShowVirtualController,
            NextLevel,
            FirstTouchGUIButton,
            LastTouchGUIButton = FirstTouchGUIButton + ((int)Classification.ColorInfo.Count-1),
            FirstScoreType,
            LastScoreType = FirstScoreType + ((int)Classification.ColorInfo.Count-1),
            FirstScorePersistFlag,
            LastScorePersistFlag = FirstScorePersistFlag + ((int)Classification.ColorInfo.Count - 1),
            SIZEOF,
        }

        private enum ControlSetup
        {
            Initialize,
            AddToGridReflexData,    // Single parameter edit in programming tiles
            AddToGridEditWorld,     // Regular world parameter editing
        };

        private const float kWaveHeightToUI = 100.0f;
        private const float kUIToWaveHeight = 1.0f / kWaveHeightToUI;

        private const int kMaxScoreLabelLength = 260;//Value is in pixels
        
        private List<UIGridElement> gridElements = new List<UIGridElement>();

        private UIGridModularButtonElement changeHistory;
        private UIGridModularCheckboxElement glassWalls;
        private UIGridModularCameraModeElement cameraMode;
        private UIGridModularCheckboxElement startingCamera;
        private UIGridModularFloatSliderElement cameraSpringStrength;
        private UIGridModularCheckboxElement showCompass;
        private UIGridModularCheckboxElement showResourceMeter;
        private UIGridModularCheckboxElement enableResourceLimiting;
        private UIGridModularFloatSliderElement waveHeight;
        private UIGridModularFloatSliderElement waterStrength;
        private UIGridModularPictureListElement sky;
        private UIGridModularPictureListElement lightRig;
        private UIGridModularFloatSliderElement windMin;
        private UIGridModularFloatSliderElement windMax;
        private UIGridModularRadioBoxElement preGame;
        private UIGridModularCheckboxElement debugPathFollow;
        private UIGridModularCheckboxElement debugDisplayCollisions;
        private UIGridModularCheckboxElement debugDisplayLinesOfPerception;
        private UIGridModularCheckboxElement debugDisplayCurrentPage;
        private UIGridModularFloatSliderElement musicVolume;
        private UIGridModularFloatSliderElement foleyVolume;
        private UIGridModularRadioBoxElement[] scoreTypes;
        private UIGridModularRadioBoxElement[] scorePersistFlags;
        private UIGridModularCheckboxElement showVirtualController;
        private UIGridModularRadioBoxElement[] touchGuiButtons;

        private UIGridModularNextLevelElement nextLevel;


        private CommandMap commandMap = new CommandMap("EditWorldParameters");     // Placeholder for stack.

        private bool active = false;

        private GameActor actor = null;     // The actor we're editing.

        private ReflexData reflexData = null;

        private Control editTypeForActor = Control.SIZEOF;

        private static bool cameraSetMode = false;  // Used to indicate we're only temporarily exiting so we can let the user set the camera position.
        private int initialCameraMode = -1;         // This is the camera mode when activated.  We need to keep this around to see if it has changed when we exit. 

        private static bool nextLevelMode = false;  // Used to indicate we're only temporarily exiting so we can let the user set the next level

        // A place to store away the current camera settings in case the user
        // selects the Fixed or FixedOffset camera without doing a SetCamera.
        private Vector3 cameraFrom;
        private Vector3 cameraAt;
        private float cameraRotation;
        private float cameraPitch;
        private float cameraDistance;
        // Flags to let us know if the user has call SetCamera for either of these modes.
        private bool fixedCameraSet = false;
        private bool fixedOffsetCameraSet = false;

        private Point lastSelectionIndex = new Point(0,0);

        //Allows button preview when editing
        private GUIButton editingButton = null;

        #region Accessors
        public bool Active
        {
            get { return active; }
        }

        /// <summary>
        /// Used to indicate we're only temporarily exiting so we can let the user set the camer position.
        /// </summary>
        public static bool CameraSetMode
        {
            get { return cameraSetMode; }
            set { cameraSetMode = value; }
        }

        /// <summary>
        /// Used to indicate we're only temporarily exiting so we can let the user set the next level.
        /// </summary>
        public static bool NextLevelMode
        {
            get { return nextLevelMode; }
            set { nextLevelMode = value; }
        }
        #endregion

        // c'tor
        public EditWorldParameters()
        {
            SetupControl(ControlSetup.Initialize, null);
        }

        public bool IsInProgrammingTileMode()
        {
            return (editMode == EditMode.ProgrammingTileMode);
        }

        public void Update()
        {
            if (active)
            {
                // Rendering goes directly to backbuffer.
                camera.Resolution = new Point((int)BokuGame.ScreenSize.X, (int)BokuGame.ScreenSize.Y);
                // If we're in portrait mode (or getting close), need to increase the FOV angle.
                if (camera.AspectRatio < 1.3f)
                {
                    camera.Fov = 1.3f / camera.AspectRatio;
                }
                camera.Update();

                if (InGame.inGame.shared.smallTextDisplay.Active ||
                    InGame.inGame.shared.scrollableTextDisplay.Active)
                {
                    InGame.inGame.shared.smallTextDisplay.Update(camera);
                    InGame.inGame.shared.scrollableTextDisplay.Update(camera);

                }
                else
                {
                    UIGridElement prevE = grid.SelectionElement;
                    
                    HandleTouchInput();
                    HandleMouseInput();
                    HandleGamepadInput();

                    grid.Update(ref worldGrid);

                    // If the Update deactived us, bail.
                    if (!active)
                        return;

                    // Update help square's positioning to line up with current selection.
                    Vector3 selectionElementOffset = grid.SelectionElement.Position - grid.ScrollOffset;
                    helpSquare.Position = new Vector2(helpSquare.Position.X, selectionElementOffset.Y);

                    helpSquare.Update();
                    if (prevE != grid.SelectionElement)
                    {
                        helpSquare.Show();
                    }

                    //Update OK Button - (B Cancel).
                    okSquare.Position = new Vector2(okSquare.Position.X, selectionElementOffset.Y);
                    okSquare.Update();

                    if( (prevE != grid.SelectionElement && GamePadInput.ActiveMode == GamePadInput.InputMode.Touch) ||
                        (GamePadInput.ActiveMode == GamePadInput.InputMode.Touch && okSquare.Hidden) )
                    {
                        okSquare.Show();
                    }
                    else if (GamePadInput.ActiveMode != GamePadInput.InputMode.Touch)
                    {
                        okSquare.Hide();
                    }
                }

                // For each element in the grid, calc it's screen space Y position
                // and give it a slight twist around the Y axis based on this.
                // Note this assumes that this grid is 1d vertical.
                for (int j = 0; j < grid.ActualDimensions.Y; j++)
                {
                    UIGridElement e = grid.Get(0, j);
                    Vector3 pos = Vector3.Transform(e.Position, grid.WorldMatrix);
                    Vector3 rot = Vector3.Zero;
                    float rotationScaling = 0.2f;
                    rot.Y = -rotationScaling * pos.Y;
                    e.Rotation = rot;
                }
            }   // end of if active

        }   // end of EditWorldParameters Update()

        private void HandleTouchInput()
        {
            if (GamePadInput.ActiveMode != GamePadInput.InputMode.Touch) { return; }
            if (TouchInput.TouchCount == 0) { return; }

            TouchContact touch = TouchInput.GetOldestTouch();

            // If in focus element has help available, get it.
            UIGridElement focusElement = grid.SelectionElement;
            string helpID = focusElement.HelpID;
            string helpText = TweakScreenHelp.GetHelp(helpID);

            if (touch != null)
            {
                // Check for help tile.
                Matrix mat = Matrix.CreateTranslation(-helpSquare.Position.X, -helpSquare.Position.Y, 0);

                Vector2 hitHelpUV = Vector2.Zero;
                hitHelpUV = TouchInput.GetHitUV(touch.position, camera, ref mat, helpSquare.Size,
                    helpSquare.Size, useRtCoords: false);

                if (hitHelpUV.X >= 0 && hitHelpUV.X < 1 && hitHelpUV.Y >= 0 && hitHelpUV.Y < 1)
                {
                    if (TouchInput.WasTouched)
                    {
                        touch.TouchedObject = helpSquare;
                    }
                    if (TouchInput.WasReleased && touch.TouchedObject == helpSquare)
                    {
                        ShowHelp(helpText);
                    }
                }

                // Check for ok tile.
                mat = Matrix.CreateTranslation(-okSquare.Position.X, -okSquare.Position.Y, 0);

                hitHelpUV = Vector2.Zero;
                hitHelpUV = TouchInput.GetHitUV(touch.position, camera, ref mat, okSquare.Size,
                    okSquare.Size, useRtCoords: false);

                if (hitHelpUV.X >= 0 && hitHelpUV.X < 1 && hitHelpUV.Y >= 0 && hitHelpUV.Y < 1)
                {
                    if (TouchInput.WasTouched)
                    {
                        touch.TouchedObject = okSquare;
                    }
                    if (TouchInput.WasReleased && touch.TouchedObject == okSquare)
                    {
                        Deactivate(false);
                    }
                }

                // Check if mouse hitting current selection object.  Or should this be done in the object?
                mat = Matrix.Invert(focusElement.WorldMatrix);
                Vector2 hitFocusUV = TouchInput.GetHitUV(touch.position, camera, ref mat, focusElement.Size.X,
                    focusElement.Size.Y, useRtCoords: false);
                bool focusElementHit = false;

                if (hitFocusUV.X >= 0 && hitFocusUV.X < 1 && hitFocusUV.Y >= 0 && hitFocusUV.Y < 1)
                {
                    if (touch.phase == TouchPhase.Began)
                    {
                        touch.TouchedObject = focusElement;
                    }
                    focusElement.HandleTouchInput(touch, hitFocusUV);
                    focusElementHit = true;
                }

                // If we didn't hit the focus object, see if we hit any of the others.
                // If so, bring them into focus.
                if (!focusElementHit && TouchGestureManager.Get().TapGesture.WasTapped())
                {
                    for (int i = 0; i < grid.ActualDimensions.Y; i++)
                    {
                        if (i == grid.SelectionIndex.Y)
                            continue;

                        UIGridElement e = grid.Get(0, i);
                        mat = Matrix.Invert(e.WorldMatrix);
                        Vector2 hitUV = TouchInput.GetHitUV(touch.position, camera, ref mat, e.Size.X,
                            e.Size.Y, useRtCoords: false);

                        if (hitUV.X >= 0 && hitUV.X < 1 && hitUV.Y >= 0 && hitUV.Y < 1)
                        {
                            // We hit an element, so bring it into focus.
                            grid.SelectionIndex = new Point(0, i);
                            break;
                        }
                    }
                }

//                 if ((hitFocusUV.X >= 0) && (hitFocusUV.X < 1))
//                 {
//                     hitMenu = true;
//                 }
//                 if (!hitMenu && TouchInput.TapGesture.WasTapped())
//                 {
//                     Deactivate(false);
//                 }

                // Handle free-form scrolling
                if (touch.TouchedObject != focusElement)
                {
                    grid.HandleTouchInput(camera);
                }
            }   // end of touch input
        }

        private void HandleMouseInput()
        {
            if (GamePadInput.ActiveMode != GamePadInput.InputMode.KeyboardMouse) { return; }

            // If in focus element has help available, get it.
            UIGridElement e = grid.SelectionElement;
            string helpID = e.HelpID;
            string helpText = TweakScreenHelp.GetHelp(helpID);

            // Check for help tile.
            Matrix mat = Matrix.CreateTranslation(-helpSquare.Position.X, -helpSquare.Position.Y, 0);
            Vector2 hitUV = MouseInput.GetHitUV(camera, ref mat, helpSquare.Size, helpSquare.Size, useRtCoords: false);

            if (hitUV.X >= 0 && hitUV.X < 1 && hitUV.Y >= 0 && hitUV.Y < 1)
            {
                if (MouseInput.Left.WasPressed)
                {
                    MouseInput.ClickedOnObject = helpSquare;
                }
                if (MouseInput.Left.WasReleased && MouseInput.ClickedOnObject == helpSquare)
                {
                    ShowHelp(helpText);
                }
            }

            // Check for ok tile.
            mat = Matrix.CreateTranslation(-okSquare.Position.X, -okSquare.Position.Y, 0);
            hitUV = MouseInput.GetHitUV(camera, ref mat, okSquare.Size, okSquare.Size, useRtCoords: false);

            if (hitUV.X >= 0 && hitUV.X < 1 && hitUV.Y >= 0 && hitUV.Y < 1)
            {
                if (MouseInput.Left.WasPressed)
                {
                    MouseInput.ClickedOnObject = okSquare;
                }
                if (MouseInput.Left.WasReleased && MouseInput.ClickedOnObject == okSquare)
                {
                    Deactivate(false);
                }
            }

            // Check if mouse hitting current selection object.  Or should this be done in the object?
            mat = Matrix.Invert(e.WorldMatrix);
            hitUV = MouseInput.GetHitUV(camera, ref mat, e.Size.X, e.Size.Y, useRtCoords: false);

            bool focusElementHit = false;
            if (hitUV.X >= 0 && hitUV.X < 1 && hitUV.Y >= 0 && hitUV.Y < 1)
            {
                e.HandleMouseInput(hitUV);
                focusElementHit = true;
            }

            // If we didn't hit the focus object, see if we hit any of the others.
            // If so, bring them into focus.
            if (!focusElementHit && MouseInput.Left.WasPressed)
            {
                for (int i = 0; i < grid.ActualDimensions.Y; i++)
                {
                    if (i == grid.SelectionIndex.Y)
                        continue;

                    e = grid.Get(0, i);
                    mat = Matrix.Invert(e.WorldMatrix);
                    hitUV = MouseInput.GetHitUV(camera, ref mat, e.Size.X, e.Size.Y, useRtCoords: false);

                    if (hitUV.X >= 0 && hitUV.X < 1 && hitUV.Y >= 0 && hitUV.Y < 1)
                    {
                        // We hit an element, so bring it into focus.
                        grid.SelectionIndex = new Point(0, i);
                        break;
                    }

                }
            }

            // Check for edges of screen.
            if (MouseInput.AtWindowTop())
            {
                grid.MoveUp();
            }
            if (MouseInput.AtWindowBottom())
            {
                grid.MoveDown();
            }

            // Allow right click or left click on nothing to exit.
//             if (MouseInput.Right.WasPressed || (!hitAnything && MouseInput.Left.WasPressed))
//             {
//                 Deactivate(false);
//             }
        }

        private void HandleGamepadInput()
        {
            GamePadInput pad = GamePadInput.GetGamePad0();

            UIGridElement e = grid.SelectionElement;
            string helpID = e.HelpID;
            string helpText = TweakScreenHelp.GetHelp(helpID);

            if (helpText != null && Actions.Help.WasPressed)
            {
                ShowHelp(helpText);
            }
        }

        private void ShowHelp(string helpText)
        {
            InGame.inGame.shared.smallTextDisplay.Activate(null, helpText, UIGridElement.Justification.Center, false, useRtCoords: false);
            if (InGame.inGame.shared.smallTextDisplay.Overflow)
            {
                InGame.inGame.shared.smallTextDisplay.Deactivate();
                InGame.inGame.shared.scrollableTextDisplay.Activate(null, helpText, UIGridElement.Justification.Center, false, useRtCoords: false);
            }
        }   // end of ShowHelp()

        private void SetupControl( ControlSetup setupType, Control? controlType )
        {
            UIGridElement.ParamBlob blob = null;

            if (setupType == ControlSetup.Initialize)
            {
                // The UI is rendered directly to backbuffer.
                camera = new PerspectiveUICamera();
                camera.Resolution = new Point((int)BokuGame.ScreenSize.X, (int)BokuGame.ScreenSize.Y);

                grid = new UIGrid(OnSelect, OnCancel, new Point(1, (int)Control.SIZEOF), "EditWorldParameters");
                grid.RenderEndsIn = true;
                grid.UseMouseScrollWheel = true;
                grid.LocalMatrix = Matrix.CreateTranslation(0.25f / 96.0f, 0.25f / 96.0f, 0.0f);

                // Create a blob of common parameters.
                blob = new UIGridElement.ParamBlob();
                //blob.width = 5.0f;
                //blob.height = 1.0f;
                blob.width = 512.0f / 96.0f;
                blob.height = blob.width / 5.0f;
                blob.edgeSize = 0.06f;
                blob.Font = UI2D.Shared.GetGameFont24Bold;
                blob.textColor = Color.White;
                blob.dropShadowColor = Color.Black;
                blob.useDropShadow = true;
                blob.invertDropShadow = false;
                blob.unselectedColor = new Color(new Vector3(4, 100, 90) / 255.0f);
                blob.selectedColor = new Color(new Vector3(5, 180, 160) / 255.0f);
                blob.normalMapName = @"Slant0Smoothed5NormalMap";
                blob.justify = UIGridModularCheckboxElement.Justification.Left;
            }

            //
            // Create elements here.
            //

            #region ChangeHistory
            {
                if (setupType == ControlSetup.Initialize)
                {
                    UIGridModularButtonElement.UIButtonElementEvent onA = delegate()
                    {
                        // Build the text blob to display.
                        string text = Strings.Localize("editWorldParams.changeHistoryTitle") + " : " + InGame.XmlWorldData.name + "\n\n";

                        if (InGame.XmlWorldData.changeHistory.Count == 0)
                        {
                            text += Strings.Localize("editWorldParams.noHistory");
                        }
                        else
                        {
                            for (int i = 0; i < InGame.XmlWorldData.changeHistory.Count; i++)
                            {
#if NETFX_CORE
                                text += InGame.XmlWorldData.changeHistory[i].time.ToString() + " ";
                                text += InGame.XmlWorldData.changeHistory[i].time.ToString() + " ";
#else
                                text += InGame.XmlWorldData.changeHistory[i].time.ToShortDateString() + " ";
                                text += InGame.XmlWorldData.changeHistory[i].time.ToShortTimeString() + " ";
#endif
                                text += InGame.XmlWorldData.changeHistory[i].gamertag + "\n";
                            }
                        }

                        InGame.inGame.shared.smallTextDisplay.Activate(null, text, UIGridElement.Justification.Center, false, false);
                        if (InGame.inGame.shared.smallTextDisplay.Overflow)
                        {
                            InGame.inGame.shared.smallTextDisplay.Deactivate();
                            InGame.inGame.shared.scrollableTextDisplay.Activate(null, text, UIGridElement.Justification.Center, false, false);
                        }
                    };

                    UIGridModularButtonElement.UIButtonElementEvent onX = delegate()
                    {
                        // TODO Maybe have a dialog box for confirmation?
                        InGame.XmlWorldData.changeHistory.Clear();
                        InGame.IsLevelDirty = true;
                    };

                    changeHistory = new UIGridModularButtonElement(blob, Strings.Localize("editWorldParams.changeHistoryTitle"),
                                                                    Strings.Localize("editWorldParams.viewHistory"), onA,
                                                                    Strings.Localize("editWorldParams.clearHistory"), onX);
                    changeHistory.HelpID = "ChangeHistory";

                    gridElements.Add(changeHistory);
                }
                else if (setupType == ControlSetup.AddToGridEditWorld && controlType == Control.ChangeHistory)
                {
                    // Add to grid.
                    grid.Add(changeHistory, 0, (int)Control.ChangeHistory);
                }
            }
            #endregion

            #region GlassWall

            if (setupType == ControlSetup.Initialize)
            {
                glassWalls = new UIGridModularCheckboxElement(blob, Strings.Localize("editWorldParams.glassWallsCheckbox"));
                glassWalls.OnCheck = delegate() { Terrain.Current.GlassWalls = true; InGame.IsLevelDirty = true; };
                glassWalls.OnClear = delegate() { Terrain.Current.GlassWalls = false; InGame.IsLevelDirty = true; };
                glassWalls.HelpID = "GlassWalls";
                gridElements.Add(glassWalls);
            }
            else if (setupType == ControlSetup.AddToGridEditWorld && controlType == Control.GlassWalls)
            {
                // Add to grid.
                grid.Add(glassWalls, 0, (int)Control.GlassWalls);
            }
            #endregion

            #region CameraMode
            {
                if (setupType == ControlSetup.Initialize)
                {
                    blob.height = 1.25f;
                    cameraMode = new UIGridModularCameraModeElement(blob, Strings.Localize("editWorldParams.cameraMode"));
                    cameraMode.OnSetCamera = delegate(int index)
                    {
                        Terrain.Current.FixedCamera = false;
                        Terrain.Current.FixedOffsetCamera = false;

                        switch (index)
                        {
                            case 0:
                                // Fixed Position
                                InGame.inGame.SaveFixedCamera();
                                break;

                            case 1:
                                // Fixed Offset
                                Terrain.Current.FixedOffsetCamera = true;
                                Terrain.Current.FixedOffset = InGame.inGame.Camera.EyeOffset;
                                break;

                            case 2:
                                // Free
                                // Nothing to see here, move along.
                                break;
                        }

                        InGame.IsLevelDirty = true;
                    };
                    cameraMode.HelpID = "CameraMode";
                    gridElements.Add(cameraMode);

                    // Set up the X button.
                    UIGridModularCameraModeElement.UIModularEvent onX = delegate(int index)
                    {
                        // If we already have valid settings for this camera mode, restore them.
                        // This makes it easier for the user to do subtle tweaks.
                        switch (index)
                        {
                            case 0:
                                // Fixed position.
                                InGame.inGame.RestoreFixedCamera();
                                break;
                            case 1:
                                // Fixed offset.
                                InGame.inGame.RestoreFixedCamera();
                                break;
                            case 2:
                                // Free
                                // do nothing
                                break;
                            default:
                                // This space intentionally left blank.
                                break;
                        }

                        CameraSetMode = true;
                        Deactivate(false);
                    };
                    cameraMode.SetXButton(onX, Strings.Localize("editWorldParams.setCamera"));

                    // Restore default blob height.
                    blob.height = 1.0f;
                }
                else if (setupType == ControlSetup.AddToGridEditWorld && controlType == Control.CameraMode)
                {
                    // Add to grid.
                    grid.Add(cameraMode, 0, (int)Control.CameraMode);
                }
            }

            #endregion

            #region StartingCamera
            {
                if (setupType == ControlSetup.Initialize)
                {
                    startingCamera = new UIGridModularCheckboxElement(blob, Strings.Localize("editWorldParams.startingCameraCheckbox"));
                    startingCamera.OnCheck = delegate()
                        {
                            InGame.inGame.SaveStartingCamera();
                        };
                    startingCamera.OnClear = delegate()
                        {
                            InGame.StartingCamera = false;
                            InGame.IsLevelDirty = true;
                        };
                    startingCamera.HelpID = "StartingCameraPosition";
                    gridElements.Add(startingCamera);

                    // Set up the X button.
                    UIGridModularCheckboxElement.UICheckboxEvent onX = delegate()
                        {
                            startingCamera.Check = true;    // If the user is setting the camera position they must want this to be true.
                            CameraSetMode = true;
                            Deactivate(false);
                        };
                    startingCamera.SetXButton(onX, Strings.Localize("editWorldParams.setCamera"));
                }
                else if (setupType == ControlSetup.AddToGridEditWorld && controlType == Control.StartingCamera)
                {
                    // Add to grid.
                    grid.Add(startingCamera, 0, (int)Control.StartingCamera);
                }
            }
            #endregion

            #region CameraSpringStrength
            {
                if (setupType == ControlSetup.Initialize)
                {
                    // Restore default.
                    blob.height = blob.width / 5.0f;
                    cameraSpringStrength = new UIGridModularFloatSliderElement(blob, Strings.Localize("editWorldParams.cameraSpringStrength"));
                    cameraSpringStrength.MinValue = 0.0f;
                    cameraSpringStrength.MaxValue = 1.0f;
                    cameraSpringStrength.IncrementByAmount = 0.1f;
                    cameraSpringStrength.NumberOfDecimalPlaces = 1;
                    cameraSpringStrength.OnChange = delegate(float strength) { InGame.CameraSpringStrength = strength; };
                    cameraSpringStrength.HelpID = "CameraSpringStrength";
                    gridElements.Add(cameraSpringStrength);
                }
                else if (setupType == ControlSetup.AddToGridEditWorld && controlType == Control.CameraSpringStrength)
                {
                    grid.Add(cameraSpringStrength, 0, (int)Control.CameraSpringStrength);
                }
            }
            #endregion

            #region ShowCompass
            {
                if (setupType == ControlSetup.Initialize)
                {
                    showCompass = new UIGridModularCheckboxElement(blob, Strings.Localize("editWorldParams.showCompassCheckbox"));
                    showCompass.OnCheck = delegate() { Terrain.Current.ShowCompass = true; InGame.IsLevelDirty = true; };
                    showCompass.OnClear = delegate() { Terrain.Current.ShowCompass = false; InGame.IsLevelDirty = true; };
                    showCompass.HelpID = "ShowCompass";
                    gridElements.Add(showCompass);
                }
                else if (setupType == ControlSetup.AddToGridEditWorld && controlType == Control.ShowCompass)
                {
                    // Add to grid.
                    grid.Add(showCompass, 0, (int)Control.ShowCompass);
                }
            }
            #endregion

            #region ShowResourceMeter
            {
                if (setupType == ControlSetup.Initialize)
                {
                    showResourceMeter = new UIGridModularCheckboxElement(blob, Strings.Localize("editWorldParams.showResourceMeterCheckbox"));
                    showResourceMeter.OnCheck = delegate() { Terrain.Current.ShowResourceMeter = true; InGame.IsLevelDirty = true; };
                    showResourceMeter.OnClear = delegate() { Terrain.Current.ShowResourceMeter = false; InGame.IsLevelDirty = true; };
                    showResourceMeter.HelpID = "ShowResourceMeter";
                    gridElements.Add(showResourceMeter);
                }
                else if (setupType == ControlSetup.AddToGridEditWorld && controlType == Control.ShowResourceMeter)
                {
                    // Add to grid.
                    grid.Add(showResourceMeter, 0, (int)Control.ShowResourceMeter);
                }
            }
            #endregion

            #region ResourceLimiting
            {
                if (setupType == ControlSetup.Initialize)
                {
                    enableResourceLimiting = new UIGridModularCheckboxElement(blob, Strings.Localize("editWorldParams.enableResourceLimitingCheckbox"));
                    enableResourceLimiting.OnCheck = delegate() { Terrain.Current.EnableResourceLimiting = true; InGame.IsLevelDirty = true; };
                    enableResourceLimiting.OnClear = delegate() { Terrain.Current.EnableResourceLimiting = false; InGame.IsLevelDirty = true; };
                    enableResourceLimiting.HelpID = "EnableResourceLimiting";
                    gridElements.Add(enableResourceLimiting);
                }
                else if (setupType == ControlSetup.AddToGridEditWorld && controlType == Control.EnableResourceLimiting)
                {
                    // Add to grid.
                    grid.Add(enableResourceLimiting, 0, (int)Control.EnableResourceLimiting);
                }
            }
            #endregion

            #region SkyPicture
            {
                if (setupType == ControlSetup.Initialize)
                {
                    blob.height = 1.25f;
                    sky = new UIGridModularPictureListElement(blob, Strings.Localize("editWorldParams.skyPictureList"));

                    // Note that the first color is what you see when looking straight 
                    // down and the last is what you see looking straight up.  The alpha
                    // channel is used to define where in the gradient range each color
                    // appears.  0.0 = straight down, 1.0 = straight up.
                    /// The last Vector4 is the tint for particles when this skydome is up.

                    // Default green grass/blue sky.
                    sky.AddGradientTile(0, Strings.Localize("skyGradientNames.grassAndSky"));

                    // All black.  Space.
                    sky.AddGradientTile(1, Strings.Localize("skyGradientNames.space"));

                    // Simple black to white ramp.
                    sky.AddGradientTile(2, Strings.Localize("skyGradientNames.rampBW"));

                    // Purple to Pink
                    sky.AddGradientTile(3, Strings.Localize("skyGradientNames.pink"));

                    // Xbox green to black.
                    sky.AddGradientTile(4, Strings.Localize("skyGradientNames.venus"));

                    // Sunset.
                    sky.AddGradientTile(5, Strings.Localize("skyGradientNames.sunset"));

                    // Mars?
                    sky.AddGradientTile(6, Strings.Localize("skyGradientNames.mars"));

                    // I've got the blues.
                    sky.AddGradientTile(7, Strings.Localize("skyGradientNames.blues"));

                    // Mars 2
                    sky.AddGradientTile(8, Strings.Localize("skyGradientNames.mars2"));

                    // Twilight
                    sky.AddGradientTile(9, Strings.Localize("skyGradientNames.twilight"));

                    sky.AddGradientTile(10, Strings.Localize("skyGradientNames.G1"));
                    sky.AddGradientTile(11, Strings.Localize("skyGradientNames.G2"));
                    sky.AddGradientTile(12, Strings.Localize("skyGradientNames.G3"));
                    sky.AddGradientTile(13, Strings.Localize("skyGradientNames.G4"));
                    sky.AddGradientTile(14, Strings.Localize("skyGradientNames.G5"));
                    sky.AddGradientTile(15, Strings.Localize("skyGradientNames.G6"));
                    sky.AddGradientTile(16, Strings.Localize("skyGradientNames.G7"));
                    sky.AddGradientTile(17, Strings.Localize("skyGradientNames.G8"));
                    sky.AddGradientTile(18, Strings.Localize("skyGradientNames.G9"));
                    sky.AddGradientTile(19, Strings.Localize("skyGradientNames.G10"));
                    sky.AddGradientTile(20, Strings.Localize("skyGradientNames.G11"));

                    sky.HelpID = "Sky";
                    gridElements.Add(sky);
                }
                else if (setupType == ControlSetup.AddToGridEditWorld && controlType == Control.Sky)
                {
                    sky.OnChange = delegate(int index)
                    {
                        Terrain.SkyIndex = sky.GetGradient(index);
                        InGame.IsLevelDirty = true;
                        InGame.RefreshThumbnail = true;
                    };

                    // Add to grid.
                    grid.Add(sky, 0, (int)Control.Sky);
                }
                else if (setupType == ControlSetup.AddToGridReflexData && controlType == Control.Sky)
                {
                    sky.OnChange = delegate(int index)
                    {
                        reflexData.WorldSkyChangeEnabled = true;
                        reflexData.WorldSkyChangeIndex = sky.GetGradient(index);
                    };

                    // Add to grid.
                    grid.Add(sky, 0, (int)Control.Sky);
                }
            }
            #endregion

            #region LightRigPicture
            {
                if (setupType == ControlSetup.Initialize)
                {
                    blob.height = 1.25f;
                    lightRig = new UIGridModularPictureListElement(blob, Strings.Localize("editWorldParams.lightRigPictureList"));

                    lightRig.AddPicture(@"IconDayRig", Strings.Localize("lightRigNames.day"));
                    lightRig.AddPicture(@"IconNightRig", Strings.Localize("lightRigNames.night"));
                    lightRig.AddPicture(@"IconSpaceRig", Strings.Localize("lightRigNames.space"));
                    lightRig.AddPicture(@"IconDreamRig", Strings.Localize("lightRigNames.dream"));
                    lightRig.AddPicture(@"IconVenusRig", Strings.Localize("lightRigNames.venus"));
                    lightRig.AddPicture(@"IconMarsRig", Strings.Localize("lightRigNames.mars"));
                    lightRig.AddPicture(@"IconDarkRig", Strings.Localize("lightRigNames.dark"));
                    lightRig.AddPicture(@"IconReallyDarkRig", Strings.Localize("lightRigNames.realdark"));

                    lightRig.HelpID = "Lighting";
                    gridElements.Add(lightRig);
                }
                else if (setupType == ControlSetup.AddToGridEditWorld && controlType == Control.LightRig)
                {
                    lightRig.OnChange = delegate(int index)
                    {
                        InGame.LightRig = ShaderGlobals.RigNames[index];
                    };

                    // Add to grid.
                    grid.Add(lightRig, 0, (int)Control.LightRig);
                }
                else if (setupType == ControlSetup.AddToGridReflexData && controlType == Control.LightRig)
                {
                    lightRig.OnChange = delegate(int index)
                    {
                        reflexData.WorldLightChangeEnabled = true;
                        reflexData.WorldLightChangeIndex = index;
                    };

                    // Add to grid.
                    grid.Add(lightRig, 0, (int)Control.LightRig);
                }
            }
            #endregion

            #region WaveHeight
            {
                if (setupType == ControlSetup.Initialize)
                {
                    // Restore default.
                    blob.height = blob.width / 5.0f;
                    waveHeight = new UIGridModularFloatSliderElement(blob, Strings.Localize("editWorldParams.waveHeight"));
                    waveHeight.MinValue = 0.0f;
                    waveHeight.MaxValue = 100.0f;
                    waveHeight.IncrementByAmount = 1.0f;
                    waveHeight.NumberOfDecimalPlaces = 0;
                    waveHeight.OnChange = delegate(float height) { Terrain.WaveHeight = height * kUIToWaveHeight; };
                    waveHeight.HelpID = "WaveHeight";
                    gridElements.Add(waveHeight);
                }
                else if (setupType == ControlSetup.AddToGridEditWorld && controlType == Control.WaveHeight)
                {
                    grid.Add(waveHeight, 0, (int)Control.WaveHeight);
                }
            }
            #endregion

            #region WaterStrength
            {
                if (setupType == ControlSetup.Initialize)
                {
                    // Restore default.
                    blob.height = blob.width / 5.0f;
                    waterStrength = new UIGridModularFloatSliderElement(blob, Strings.Localize("editWorldParams.waterStrength"));
                    waterStrength.MinValue = 0.0f;
                    waterStrength.MaxValue = 100.0f;
                    waterStrength.IncrementByAmount = 5.0f;
                    waterStrength.NumberOfDecimalPlaces = 0;
                    waterStrength.OnChange = delegate(float height)
                    {
                        Terrain.WaterStrength = height * kUIToWaveHeight;
                    };
                    waterStrength.HelpID = "WaterStrength";
                    gridElements.Add(waterStrength);
                }
                else if (setupType == ControlSetup.AddToGridEditWorld && controlType == Control.WaterStrength)
                {
                    grid.Add(waterStrength, 0, (int)Control.WaterStrength);
                }
            }
            #endregion

            #region WindStrength
            {
                if (setupType == ControlSetup.Initialize)
                {
                    // Restore default.
                    blob.height = blob.width / 5.0f;
                    windMin = new UIGridModularFloatSliderElement(blob, Strings.Localize("editWorldParams.windMin"));
                    windMin.MinValue = 0.0f;
                    windMin.MaxValue = 100.0f;
                    windMin.IncrementByAmount = 5.0f;
                    windMin.NumberOfDecimalPlaces = 0;
                    windMin.OnChange = delegate(float min)
                    {
                        min = MathHelper.Clamp(min, 0.0f, 100.0f);
                        if (min > windMax.CurrentValue)
                            windMax.CurrentValue = min;
                        InGame.WindMin = min * 0.01f;
                    };
                    windMin.HelpID = "MinBreeze";
                    gridElements.Add(windMin);
                }
                else  if (setupType == ControlSetup.AddToGridEditWorld && controlType == Control.WindMin)
                {                    
                    grid.Add(windMin, 0, (int)Control.WindMin);
                }

                if (setupType == ControlSetup.Initialize)
                {
                    windMax = new UIGridModularFloatSliderElement(blob, Strings.Localize("editWorldParams.windMax"));
                    windMax.MinValue = 0.0f;
                    windMax.MaxValue = 100.0f;
                    windMax.IncrementByAmount = 5.0f;
                    windMax.NumberOfDecimalPlaces = 0;
                    windMax.OnChange = delegate(float max)
                    {
                        max = MathHelper.Clamp(max, 0.0f, 100.0f);
                        if (max < windMin.CurrentValue)
                            windMin.CurrentValue = max;
                        InGame.WindMax = max * 0.01f;
                    };
                    windMax.HelpID = "MaxBreeze";
                    gridElements.Add(windMax);
                }
                else if (setupType == ControlSetup.AddToGridEditWorld && controlType == Control.WindMax)
                {
                    grid.Add(windMax, 0, (int)Control.WindMax);
                }
            }
            #endregion

            #region PreGame
            {
                if (setupType == ControlSetup.Initialize)
                {
                    blob.height = 0.4f;     // This will grow as needed.
                    preGame = new UIGridModularRadioBoxElement(blob, Strings.Localize("editWorldParams.preGame"));
                    preGame.OnChange = delegate(UIGridModularRadioBoxElement.ListEntry entry)
                    {
                        // Need to set the value based on the localized text.
                        if (entry.Text == Strings.Localize("editWorldParams.nothing"))
                        {
                            InGame.XmlWorldData.preGame = "";
                        }
                        else if (entry.Text == Strings.Localize("editWorldParams.levelTitle"))
                        {
                            InGame.XmlWorldData.preGame = "World Title";
                        }
                        else if (entry.Text == Strings.Localize("editWorldParams.levelDesc"))
                        {
                            InGame.XmlWorldData.preGame = "World Description";
                        }
                        else if (entry.Text == Strings.Localize("editWorldParams.countdown"))
                        {
                            InGame.XmlWorldData.preGame = "Countdown";
                        }
                        else if (entry.Text == Strings.Localize("editWorldParams.countdownWithDesc"))
                        {
                            InGame.XmlWorldData.preGame = "Description with Countdown";
                        }

                        InGame.IsLevelDirty = true;
                    };
                    preGame.AddText(Strings.Localize("editWorldParams.nothing"));
                    preGame.AddText(Strings.Localize("editWorldParams.levelTitle"));
                    preGame.AddText(Strings.Localize("editWorldParams.levelDesc"));
                    preGame.AddText(Strings.Localize("editWorldParams.countdown"));
                    preGame.AddText(Strings.Localize("editWorldParams.countdownWithDesc"));

                    preGame.HelpID = "PreGame";
                    gridElements.Add(preGame);
                }
                else if (setupType == ControlSetup.AddToGridEditWorld && controlType == Control.PreGame)
                {
                    grid.Add(preGame, 0, (int)Control.PreGame);
                }
            }
            #endregion

            #region DebugPathFollow
            {
                if (setupType == ControlSetup.Initialize)
                {
                    blob.height = 1.0f;
                    debugPathFollow = new UIGridModularCheckboxElement(blob, Strings.Localize("editWorldParams.debugPathFollow"));
                    debugPathFollow.OnCheck = delegate() { InGame.DebugPathFollow = true; };
                    debugPathFollow.OnClear = delegate() { InGame.DebugPathFollow = false; };
                    debugPathFollow.HelpID = "Debug:PathFollowing";
                    gridElements.Add(debugPathFollow);
                }
                else if (setupType == ControlSetup.AddToGridEditWorld && controlType == Control.DebugPathFollow)
                {
                    grid.Add(debugPathFollow, 0, (int)Control.DebugPathFollow);
                }
            }
            #endregion

            #region DebugDisplayCollisions
            {
                if (setupType == ControlSetup.Initialize)
                {
                    debugDisplayCollisions = new UIGridModularCheckboxElement(blob, Strings.Localize("editWorldParams.debugDisplayCollisions"));
                    debugDisplayCollisions.OnCheck = delegate() { InGame.DebugDisplayCollisions = true; };
                    debugDisplayCollisions.OnClear = delegate() { InGame.DebugDisplayCollisions = false; };
                    debugDisplayCollisions.HelpID = "Debug:DisplayCollisions";
                    gridElements.Add(debugDisplayCollisions);
                }
                else if (setupType == ControlSetup.AddToGridEditWorld && controlType == Control.DebugDisplayCollisions)
                {
                    grid.Add(debugDisplayCollisions, 0, (int)Control.DebugDisplayCollisions);
                }
            }
            #endregion

            #region DebugDisplayLOP
            {
                if (setupType == ControlSetup.Initialize)
                {
                    debugDisplayLinesOfPerception = new UIGridModularCheckboxElement(blob, Strings.Localize("editWorldParams.debugDisplayLinesOfPerception"));
                    debugDisplayLinesOfPerception.OnCheck = delegate() { InGame.DebugDisplayLinesOfPerception = true; };
                    debugDisplayLinesOfPerception.OnClear = delegate() { InGame.DebugDisplayLinesOfPerception = false; };
                    debugDisplayLinesOfPerception.HelpID = "Debug:ShowLinesOfPerception";
                    gridElements.Add(debugDisplayLinesOfPerception);
                }
                else if (setupType == ControlSetup.AddToGridEditWorld && controlType == Control.DebugDisplayLinesOfPerception)
                {
                    grid.Add(debugDisplayLinesOfPerception, 0, (int)Control.DebugDisplayLinesOfPerception);
                }
            }
            #endregion

            #region DebugDisplayCurrentPage
            {
                if (setupType == ControlSetup.Initialize)
                {
                    debugDisplayCurrentPage = new UIGridModularCheckboxElement(blob, Strings.Localize("editWorldParams.debugDisplayCurrentPage"));
                    debugDisplayCurrentPage.OnCheck = delegate() { InGame.DebugDisplayCurrentPage = true; };
                    debugDisplayCurrentPage.OnClear = delegate() { InGame.DebugDisplayCurrentPage = false; };
                    debugDisplayCurrentPage.HelpID = "Debug:ShowCurrentPage";
                    gridElements.Add(debugDisplayCurrentPage);
                }
                else if (setupType == ControlSetup.AddToGridEditWorld && controlType == Control.DebugDisplayCurrentPage)
                {
                    grid.Add(debugDisplayCurrentPage, 0, (int)Control.DebugDisplayCurrentPage);
                }
            }
            #endregion

            #region FoleyVolume
            {
                if (setupType == ControlSetup.Initialize)
                {
                    // Restore default.
                    blob.height = blob.width / 5.0f;
                    foleyVolume = new UIGridModularFloatSliderElement(blob, Strings.Localize("editWorldParams.foleyVolume"));
                    foleyVolume.MinValue = 0.0f;
                    foleyVolume.MaxValue = 100.0f;
                    foleyVolume.IncrementByAmount = 5.0f;
                    foleyVolume.NumberOfDecimalPlaces = 0;
                    foleyVolume.OnChange = delegate(float volume) { InGame.LevelFoleyVolume = volume * 0.01f; };
                    foleyVolume.HelpID = "EffectsVolume";
                    gridElements.Add(foleyVolume);
                }
                else if (setupType == ControlSetup.AddToGridEditWorld && controlType == Control.FoleyVolume)
                {
                    grid.Add(foleyVolume, 0, (int)Control.FoleyVolume);
                }
            }
            #endregion

            #region MusicVolume
            {
                if (setupType == ControlSetup.Initialize)
                {
                    musicVolume = new UIGridModularFloatSliderElement(blob, Strings.Localize("editWorldParams.musicVolume"));
                    musicVolume.MinValue = 0.0f;
                    musicVolume.MaxValue = 100.0f;
                    musicVolume.IncrementByAmount = 5.0f;
                    musicVolume.NumberOfDecimalPlaces = 0;
                    musicVolume.OnChange = delegate(float volume) { InGame.LevelMusicVolume = volume * 0.01f; };
                    musicVolume.HelpID = "MusicVolume";
                    gridElements.Add(musicVolume);
                }
                else if (setupType == ControlSetup.AddToGridEditWorld && controlType == Control.MusicVolume)
                { 
                    grid.Add(musicVolume, 0, (int)Control.MusicVolume); 
                }
            }
            #endregion

            #region Temporary score visibility settings for try-out
            {
                if (setupType == ControlSetup.Initialize)
                {
                    scoreTypes = new UIGridModularRadioBoxElement[(int)Classification.ColorInfo.Count];
                    for (int i = 0; i < (int)Classification.ColorInfo.Count; ++i)
                    {
                        Classification.Colors scoreColor = (Classification.Colors)((int)Classification.ColorInfo.First + i);
                        Control scoreCtlId = (Control)((int)Control.FirstScoreType + i);

                        blob.height = 0.4f;     // This will grow as needed.
                        UIGridModularRadioBoxElement ctl = scoreTypes[i] = new UIGridModularRadioBoxElement(blob, Strings.Localize("editWorldParams.scoreVisibility") + Strings.Localize("colorNames." + scoreColor));
                        ctl.OnSelection = delegate(UIGridModularRadioBoxElement.ListEntry entry)
                        {
                            Scoreboard.Score scoreObj = Scoreboard.GetScoreboardScore(scoreColor);
                            Debug.Assert(null != scoreObj);
                            
                            // Note:  The text we get back is localized so we
                            // can't just use Utils.EnumParse to get back the enum value.
                            if(entry.Text == Strings.Localize("editWorldParams.loudLabel"))
                            {
                                TextLineDialog.OnDialogDone callback = delegate(bool canceled, string newText)
                                {
                                    if (!canceled)
                                    {
                                        //clean the label for better display
                                        newText = CleanScoreLabel(newText);

                                        scoreObj.Labeled = !canceled && newText.Length > 0;
                                        scoreObj.Label = scoreObj.Labeled ? newText : "";

                                        if (!scoreObj.Labeled)
                                        {
                                            ctl.SetValue(Strings.Localize("editWorldParams.loud"));
                                        }
                                    }
                                };
                                TextLineEditor.ValidateText validateCallback = delegate(TextBlob textBlob)
                                {
                                    //Deterimine if text will fit.
                                    var font =Scoreboard.GetFont();
                                    var width = font().MeasureString(textBlob.RawText).X;
                                    bool valid = width <= kMaxScoreLabelLength;
                                    return valid;
                                };
                                scoreObj.Visibility = ScoreVisibility.Loud;
                                InGame.inGame.shared.textLineDialog.Activate(callback, scoreObj.Label, validateCallback);
                            }
                            else if (entry.Text == Strings.Localize("editWorldParams.quietLabel"))
                            {
                                TextLineDialog.OnDialogDone callback = delegate(bool canceled, string newText)
                                {
                                    if (!canceled)
                                    {
                                        //clean the label for better display
                                        newText = CleanScoreLabel(newText);

                                        scoreObj.Labeled = !canceled && newText.Length > 0;
                                        scoreObj.Label = scoreObj.Labeled ? newText : "";

                                        if (!scoreObj.Labeled)
                                        {
                                            ctl.SetValue(Strings.Localize("editWorldParams.quiet"));
                                        }
                                    }
                                };
                                TextLineEditor.ValidateText validateCallback = delegate(TextBlob textBlob)
                                {
                                    //Deterimine if text will fit.
                                    var font = Scoreboard.GetFont();
                                    var width = font().MeasureString(textBlob.RawText).X;
                                    bool valid = width <= kMaxScoreLabelLength;
                                    return valid;
                                };
                                scoreObj.Visibility = ScoreVisibility.Quiet;
                                InGame.inGame.shared.textLineDialog.Activate(callback, scoreObj.Label, validateCallback);
                            }
                            else if (entry.Text == Strings.Localize("editWorldParams.quiet"))
                            {
                                scoreObj.Visibility = ScoreVisibility.Quiet;
                            }
                            else if (entry.Text == Strings.Localize("editWorldParams.off"))
                            {
                                scoreObj.Visibility = ScoreVisibility.Off;
                            }
                            else
                            {
                                scoreObj.Visibility = ScoreVisibility.Loud;
                            }

                            InGame.IsLevelDirty = true;
                        };

                        ctl.AddText(Strings.Localize("editWorldParams.loudLabel"));
                        ctl.AddText(Strings.Localize("editWorldParams.loud"));
                        ctl.AddText(Strings.Localize("editWorldParams.quietLabel"));
                        ctl.AddText(Strings.Localize("editWorldParams.quiet"));
                        ctl.AddText(Strings.Localize("editWorldParams.off"));
                        ctl.HelpID = "ScoreVisibility";
                        gridElements.Add(ctl);

                        // Restore default.
                        blob.height = blob.width / 5.0f;
                    }
                }
                else if (setupType == ControlSetup.AddToGridEditWorld && controlType == Control.FirstScoreType)
                {
                    int i = 0;
                    foreach (UIGridModularRadioBoxElement ct in scoreTypes)
                    {
                        Control scoreCtlId = (Control)((int)Control.FirstScoreType + i);
                        grid.Add(ct, 0, (int)scoreCtlId);
                        i++;
                    }
                }
            }
            #endregion

            #region score persistence settings 
            {
                if (setupType == ControlSetup.Initialize)
                {
                    scorePersistFlags = new UIGridModularRadioBoxElement[(int)Classification.ColorInfo.Count];
                    for (int i = 0; i < (int)Classification.ColorInfo.Count; ++i)
                    {
                        Classification.Colors scoreColor = (Classification.Colors)((int)Classification.ColorInfo.First + i);
                        Control scoreCtlId = (Control)((int)Control.FirstScorePersistFlag + i);

                        blob.height = 0.4f;     // This will grow as needed.
                        UIGridModularRadioBoxElement ctl = scorePersistFlags[i] = new UIGridModularRadioBoxElement(blob, Strings.Localize("editWorldParams.scorePersistence") + Strings.Localize("colorNames." + scoreColor));
                        ctl.OnChange = delegate(UIGridModularRadioBoxElement.ListEntry entry)
                        {
                            // Note:  The text we get back is localized so we
                            // can't just use Utils.EnumParse to get back the enum value.
                            bool persistence = false;
                            if (entry.Text == Strings.Localize("editWorldParams.on"))
                            {
                                persistence = true;
                            }

                            Scoreboard.SetPersistFlag(scoreColor, persistence);
                            InGame.IsLevelDirty = true;
                        };
                        ctl.AddText(Strings.Localize("editWorldParams.on"));
                        ctl.AddText(Strings.Localize("editWorldParams.off"));
                        ctl.HelpID = "ScorePersistence";
                        gridElements.Add(ctl);

                        // Restore default.
                        blob.height = blob.width / 5.0f;
                    }
                }
                else if (setupType == ControlSetup.AddToGridEditWorld && controlType == Control.FirstScorePersistFlag)
                {
                    int i = 0;
                    foreach (UIGridModularRadioBoxElement ct in scorePersistFlags)
                    {
                        Control scoreCtlId = (Control)((int)Control.FirstScorePersistFlag + i);
                        grid.Add(ct, 0, (int)scoreCtlId);
                        i++;
                    }
                }
            }
            #endregion

            
            #region Touch GUI Button Visibility
            {
                if (setupType == ControlSetup.Initialize)
                {
                    touchGuiButtons = new UIGridModularRadioBoxElement[(int)Classification.ColorInfo.Count];
                    for (int i = 0; i < (int)Classification.ColorInfo.Count; ++i)
                    {
                        Classification.Colors color = (Classification.Colors)((int)Classification.ColorInfo.First + i);
                        Control ctrlID = (Control)((int)Control.FirstTouchGUIButton + i);

                        blob.height = 0.4f;     // This will grow as needed.
                        UIGridModularRadioBoxElement ctl = touchGuiButtons[i] = new UIGridModularRadioBoxElement(blob, Strings.Localize("editWorldParams.guiButtonVisibility") + Strings.Localize("colorNames." + color));
                        ctl.OnSelection = delegate(UIGridModularRadioBoxElement.ListEntry entry)
                        {
                            GUIButton button = GUIButtonManager.GetButton(color);
                            Debug.Assert(null != button);

                            // Note:  The text we get back is localized so we
                            if( Strings.Localize("editWorldParams.labeledButton") == entry.Text )
                            {
                                TextLineDialog.OnDialogDone callback = delegate( bool canceled, string newText )
                                {
                                    editingButton = null;

                                    //clean the label for better display
                                    newText = CleanButtonLabel(newText);

                                    if( !canceled && newText.Length > 0 )
                                    {
                                        Debug.Assert(null != button);
                                        button.Label = newText;
                                    }
                                    else
                                    {
                                        ctl.SetValue( Strings.Localize("editWorldParams.solidButton") );
                                    }
                                };
                                TextLineEditor.ValidateText validateCallback = delegate(TextBlob textBlob)
                                {
                                    //Deterimine if text will fit on the button.
                                    bool valid = GUIButtonManager.TestLabelFit(textBlob.RawText);
                                    return valid;
                                }; 
                                editingButton = button;
                                InGame.inGame.shared.textLineDialog.Activate(callback, button.Label, validateCallback);
                            }
                            else
                            {
                            }
                            
                            
                            InGame.IsLevelDirty = true;
                        };
                        ctl.AddText(Strings.Localize("editWorldParams.labeledButton"));
                        ctl.AddText(Strings.Localize("editWorldParams.solidButton"));
                        ctl.HelpID = "GuiButtonVisibility";
                        gridElements.Add(ctl);

                        // Restore default.
                        blob.height = blob.width / 5.0f;
                    }
                }
                else if (setupType == ControlSetup.AddToGridEditWorld && controlType == Control.FirstTouchGUIButton)
                {
                    int i = 0;
                    foreach (UIGridModularRadioBoxElement ct in touchGuiButtons)
                    {
                        Control ctrlID = (Control)((int)Control.FirstTouchGUIButton + i);
                        grid.Add(ct, 0, (int)ctrlID);
                        i++;
                    }
                }
            }
            #endregion


            #region Virtual Controller Visibility
            {
                if (setupType == ControlSetup.Initialize)
                {
                    showVirtualController = new UIGridModularCheckboxElement(blob, Strings.Localize("editWorldParams.showVirtualController"));
                    showVirtualController.OnCheck = delegate() { InGame.ShowVirtualController = true; };
                    showVirtualController.OnClear = delegate() { InGame.ShowVirtualController = false; };
                    showVirtualController.HelpID = "ShowVirtualController";
                    gridElements.Add(showVirtualController);
                }
                else if (setupType == ControlSetup.AddToGridEditWorld && controlType == Control.ShowVirtualController)
                {
                    grid.Add(showVirtualController, 0, (int)Control.ShowVirtualController);
                }
            }
            #endregion

            #region NextLevel
            {
                if (setupType == ControlSetup.Initialize)
                {
                    nextLevel = new UIGridModularNextLevelElement(blob);

                    nextLevel.OnClear = delegate() { if (InGame.XmlWorldData != null) InGame.XmlWorldData.LinkedToLevel = null;  };
                    nextLevel.HelpID = "NextLevel";
                    gridElements.Add(nextLevel);
                }
                else if (setupType == ControlSetup.AddToGridEditWorld && controlType == Control.NextLevel)
                {

                    nextLevel.OnSetNextLevel = delegate()
                    {
                        Deactivate(false);
                        NextLevelMode = true;

                        BokuGame.bokuGame.loadLevelMenu.ReturnToMenu = LoadLevelMenu.ReturnTo.EditWorldParameters;
                        BokuGame.bokuGame.loadLevelMenu.ActivateAttaching();
                    };

                    // Add to grid.
                    grid.Add(nextLevel, 0, (int)Control.NextLevel);
                }
                else if (setupType == ControlSetup.AddToGridReflexData && controlType == Control.NextLevel)
                {

                    nextLevel.OnSetNextLevel = delegate()
                    {
                        Deactivate(true);
                        NextLevelMode = true;

                        BokuGame.bokuGame.loadLevelMenu.ReturnToMenu = LoadLevelMenu.ReturnTo.Editor;
                        BokuGame.bokuGame.loadLevelMenu.ActivateAttaching();
                    };

                    // Add to grid.
                    grid.Add(nextLevel, 0, (int)Control.NextLevel);
                }
            }
            #endregion


            if (setupType == ControlSetup.Initialize)
            {
                // Set grid properties.
                grid.Spacing = new Vector2(0.0f, 0.1f);     // The first number doesn't really matter since we're doing a 1d column.
                grid.Scrolling = true;
                grid.Wrap = false;
                grid.LocalMatrix = Matrix.Identity;

                helpSquare = new UIGridModularHelpSquare();
                helpSquare.Size = 0.95f;
                helpSquare.Position = new Vector2(3.5f, 0.0f);

                okSquare = new UIGridModularOKSquare();
                okSquare.Size = 0.95f;
                okSquare.Position = new Vector2(4.5f, 0.0f);
            }

        }   // end of SetupControl

        // Should be called after all UI elements are setup on the grid
        private void SetupGridHelp()
        {
            // Loop over all the elements in the grid.  For any that have 
            // help, set the flag so they display Y button for help.
            for (int i = 0; i < grid.ActualDimensions.Y; i++)
            {
                UIGridElement e = grid.Get(0, i);
                string helpID = e.HelpID;
                string helpText = TweakScreenHelp.GetHelp(helpID);
                if (helpText != null)
                {
                    e.ShowHelpButton = true;
                }
            }
        }

        private string CleanScoreLabel(string scoreLabel)
        {
            scoreLabel = scoreLabel.Replace("\n", "");
            scoreLabel = scoreLabel.Replace("\r", "");
            scoreLabel = scoreLabel.Trim();

            scoreLabel = TextHelper.FilterURLs(scoreLabel);
            scoreLabel = TextHelper.FilterEmail(scoreLabel);

            return scoreLabel;
        }

        private string CleanButtonLabel(string buttonLabel)
        {
            buttonLabel = buttonLabel.Replace("\r", "");

            //Buttons used to allow a single new line. 
            //Now they autowrap via text blob so replace
            //old \n with space.
            buttonLabel = buttonLabel.Replace("\r", " ");

            buttonLabel = buttonLabel.Trim();

            buttonLabel = TextHelper.FilterURLs(buttonLabel);
            buttonLabel = TextHelper.FilterEmail(buttonLabel);

            return buttonLabel;
        }

        private void ClearGrid()
        {
            grid.ClearNoUnload();
        }

        private void SetupControlsForEditObject()
        {
            ClearGrid();

            SetupControl( ControlSetup.AddToGridEditWorld, Control.ChangeHistory );
            SetupControl( ControlSetup.AddToGridEditWorld, Control.GlassWalls );
            SetupControl( ControlSetup.AddToGridEditWorld, Control.CameraMode );
            SetupControl( ControlSetup.AddToGridEditWorld, Control.StartingCamera );
            SetupControl( ControlSetup.AddToGridEditWorld, Control.CameraSpringStrength );
            SetupControl( ControlSetup.AddToGridEditWorld, Control.ShowCompass );
            SetupControl( ControlSetup.AddToGridEditWorld, Control.ShowResourceMeter );
            SetupControl( ControlSetup.AddToGridEditWorld, Control.EnableResourceLimiting );
            SetupControl( ControlSetup.AddToGridEditWorld, Control.WaveHeight );
            SetupControl( ControlSetup.AddToGridEditWorld, Control.WaterStrength );
            SetupControl( ControlSetup.AddToGridEditWorld, Control.Sky );
            SetupControl( ControlSetup.AddToGridEditWorld, Control.LightRig );
            SetupControl( ControlSetup.AddToGridEditWorld, Control.WindMin );
            SetupControl( ControlSetup.AddToGridEditWorld, Control.WindMax );
            SetupControl( ControlSetup.AddToGridEditWorld, Control.PreGame );
            SetupControl( ControlSetup.AddToGridEditWorld, Control.DebugPathFollow );
            SetupControl( ControlSetup.AddToGridEditWorld, Control.DebugDisplayCollisions );
            SetupControl(ControlSetup.AddToGridEditWorld, Control.DebugDisplayLinesOfPerception);
            SetupControl(ControlSetup.AddToGridEditWorld, Control.DebugDisplayCurrentPage);
            SetupControl(ControlSetup.AddToGridEditWorld, Control.FoleyVolume);
            SetupControl( ControlSetup.AddToGridEditWorld, Control.MusicVolume );
            SetupControl( ControlSetup.AddToGridEditWorld, Control.ShowVirtualController );
            SetupControl( ControlSetup.AddToGridEditWorld, Control.NextLevel);
            SetupControl( ControlSetup.AddToGridEditWorld, Control.FirstTouchGUIButton);
            SetupControl( ControlSetup.AddToGridEditWorld, Control.FirstScoreType );
            SetupControl( ControlSetup.AddToGridEditWorld, Control.FirstScorePersistFlag );
            
            SetupGridHelp();
        }

        public void Render()
        {
            if (active)
            {
                // Render menu using local camera.
                ShaderGlobals.SetCamera(camera);

                grid.Render(camera);

                helpSquare.Render(camera);
                okSquare.Render(camera);

                ToolTipManager.Render(camera);

                //Text edit dialog
                if (InGame.inGame.shared.textLineDialog.Active)
                {
                    InGame.inGame.shared.textLineDialog.Render();
                    if (editingButton != null)
                    {
                        //Draw button preview
                        var buttonCenter = BokuGame.ScreenSize/2;
                        buttonCenter.X -= BokuGame.ScreenSize.X / 4;
                        GUIButtonManager.RenderButtonPreview(editingButton, buttonCenter, InGame.inGame.shared.textLineDialog.GetText());
                    }
                }

            }

        }   // end of EditWorldParameters Render()

        public void OnSelect(UIGrid grid)
        {
            // Normally the grid wil deactivate itself when a selection is made.
            // In the options/settings case there are some elements that ignore 
            // the Select action letting it get to the grid which then deactivates
            // itself.  We don't want that to happen so set the grid active here.
            grid.Active = true;

            //
            Deactivate(false);

        }   // end of OnSelect()

        public void OnCancel(UIGrid grid)
        {
            Deactivate(false);
        }   // end of OnCancel()

        public void Activate(ReflexData _data, GameActor _actor, Control _editType)
        {
            if (!active)
            {
                editMode = EditMode.ProgrammingTileMode;

                reflexData = _data;
                actor = _actor;
                editTypeForActor = _editType;

                // Do stack handling here.  If we do it in the update object we have no
                // clue which order things get pushed and popped and madness ensues.
                CommandStack.Push(commandMap);
                active = true;
                grid.Active = true;
                grid.RenderWhenInactive = false;

                /// We are going to read in the object's settings and set our own to mirror them.
                /// This won't dirty the level, but setting our settings will think it is dirtying
                /// the level. So we let the dirty flag get set, then just reset it after we've
                /// initialized (end of this activate function).
                bool wasDirty = InGame.IsLevelDirty;

                ClearGrid();

                // We are only supporting few edit types here
                switch (_editType)
                {
                    case Control.LightRig:
                    {
                        SetupControl(ControlSetup.AddToGridReflexData, Control.LightRig);

                        if (reflexData.WorldLightChangeEnabled)
                        {                            
                            lightRig.SetValue(reflexData.WorldLightChangeIndex);
                        }
                        else
                        {
                            // Take the current light rig name, find it's index value, and set the current
                            // selection for the lightRig picture list to match.
                            int lightRigIndex = 0;
                            for (int i = 0; i < ShaderGlobals.RigNames.Length; i++)
                            {
                                if (InGame.LightRig == ShaderGlobals.RigNames[i])
                                {
                                    lightRigIndex = i;
                                    break;
                                }
                            }
                            lightRig.SetValue(lightRigIndex);
                        }
                    }
                    break;

                    case Control.Sky:
                    {
                        SetupControl(ControlSetup.AddToGridReflexData, Control.Sky);

                        if (reflexData.WorldSkyChangeEnabled)
                        {
                            sky.SetValue(reflexData.WorldSkyChangeIndex);
                        }
                        else
                        {
                            sky.SetValue(Terrain.SkyIndex);
                        }
                    }
                    break;

                    case Control.NextLevel:
                    {
                        SetupControl(ControlSetup.AddToGridReflexData, Control.NextLevel);

                        if (InGame.XmlWorldData.LinkedToLevel != null)
                        {
                            nextLevel.NextLevel = XmlDataHelper.LoadMetadataUnknownGenre((Guid)InGame.XmlWorldData.LinkedToLevel);
                        }
                        else
                        {
                            //make sure the old value doesn't stick around
                            nextLevel.NextLevel = null;
                        }
                    }
                    break;
                }

                // Get rid of empty elements
                grid.RemoveAllEmptyAndCollapse();

                // Then setup grid for help
                SetupGridHelp();

                //
                InGame.inGame.RenderWorldAsThumbnail = true;
                InGame.IsLevelDirty = wasDirty;

                Foley.PlayMenuLoop();
            }
        }

        public void Activate()
        {
            if (!active)
            {                
                editMode = EditMode.ChangeSettingMode;

                //make sure we clean up the data
                reflexData = null;
                actor = null;
                editTypeForActor = Control.SIZEOF;

                // Do stack handling here.  If we do it in the update object we have no
                // clue which order things get pushed and popped and madness ensues.
                CommandStack.Push(commandMap);
                active = true;
                grid.Active = true;
                grid.RenderWhenInactive = false;

                SetupControlsForEditObject();

                //scroll to previous index
                grid.SelectionIndex = lastSelectionIndex;

                // If we're coming back from camera set mode, grab the new camera values as needed.
                if (CameraSetMode)
                {
                    UIGridElement e = grid.SelectionElement;

                    if (e == startingCamera)
                    {
                        InGame.inGame.SaveStartingCamera();
                    }
                    else if (e == cameraMode)
                    {
                        cameraMode.OnSetCamera(cameraMode.CurIndex);
                        if (cameraMode.CurIndex == 0)
                        {
                            fixedCameraSet = true;
                        }
                        else if (cameraMode.CurIndex == 1)
                        {
                            fixedOffsetCameraSet = true;
                        }
                    }

                    CameraSetMode = false;
                }
                else
                {
                    // Not coming from CameraSet so store away the current camera.
                    cameraFrom = InGame.inGame.Camera.From;
                    cameraAt = InGame.inGame.Camera.At;
                    cameraRotation = InGame.inGame.Camera.Rotation;
                    cameraPitch = InGame.inGame.Camera.Pitch;
                    cameraDistance = InGame.inGame.Camera.Distance;

                    fixedCameraSet = false;
                    fixedOffsetCameraSet = false;
                }

                //clear next level mode flag
                NextLevelMode = false;

                /// We are going to read in the object's settings and set our own to mirror them.
                /// This won't dirty the level, but setting our settings will think it is dirtying
                /// the level. So we let the dirty flag get set, then just reset it after we've
                /// initialized (end of this activate function).
                bool wasDirty = InGame.IsLevelDirty;

                // Set initial values.
                glassWalls.Check = Terrain.Current.GlassWalls;
                if (Terrain.Current.FixedCamera)
                {
                    cameraMode.CurIndex = 0;
                }
                else if (Terrain.Current.FixedOffsetCamera)
                {
                    cameraMode.CurIndex = 1;
                }
                else
                {
                    cameraMode.CurIndex = 2;
                }
                initialCameraMode = cameraMode.CurIndex;
                startingCamera.Check = InGame.StartingCamera;
                cameraSpringStrength.CurrentValue = InGame.CameraSpringStrength;
                showCompass.Check = Terrain.Current.ShowCompass;
                showResourceMeter.Check = Terrain.Current.ShowResourceMeter;
                enableResourceLimiting.Check = Terrain.Current.EnableResourceLimiting;
                waveHeight.CurrentValue = Terrain.WaveHeight * kWaveHeightToUI;
                waterStrength.CurrentValue = Terrain.WaterStrength * kWaveHeightToUI;
                sky.SetValue(Terrain.SkyIndex);

                preGame.SetValue(Terrain.Current.XmlWorldData.preGame);

                debugPathFollow.Check = InGame.DebugPathFollow;
                debugDisplayCollisions.Check = InGame.DebugDisplayCollisions;
                debugDisplayLinesOfPerception.Check = InGame.DebugDisplayLinesOfPerception;
                debugDisplayCurrentPage.Check = InGame.DebugDisplayCurrentPage;

                // Take the current light rig name, find it's index value, and set the current
                // selection for the lightRig picture list to match.
                int lightRigIndex = 0;
                for (int i = 0; i < ShaderGlobals.RigNames.Length; i++)
                {
                    if (InGame.LightRig == ShaderGlobals.RigNames[i])
                    {
                        lightRigIndex = i;
                        break;
                    }
                }
                lightRig.SetValue(lightRigIndex);

                windMin.CurrentValue = InGame.WindMin * 100.0f;
                windMax.CurrentValue = InGame.WindMax * 100.0f;

                foleyVolume.CurrentValue = InGame.LevelFoleyVolume * 100.0f;
                musicVolume.CurrentValue = InGame.LevelMusicVolume * 100.0f;

                for (int i = 0; i < (int)Classification.ColorInfo.Count; ++i)
                {
                    //Score settings by color.
                    
                    Classification.Colors color = (Classification.Colors)((int)Classification.ColorInfo.First + i);
                    Scoreboard.Score scoreObj = Scoreboard.GetScoreboardScore(color);


                    //Kinda a weird way to set current setting.  Grabs Enum actual sring and creates loc key from it.  
                    //If ENUM chagnes this breaks :s.  Should consider re-implementing.
                    
                    //make sure we localize the value - the odd way this is implemented means if we don't, the values
                    //won't persist between languages!!
                    string locKey = "editWorldParams." + scoreObj.Visibility.ToString().ToLower();
                    if(scoreObj.Labeled && scoreObj.Label.Length > 0 && scoreObj.Visibility!=ScoreVisibility.Off)
                    {
                        locKey += "Label";
                    }
                    
                    scoreTypes[i].SetValue(Strings.Localize(locKey));

                    //initialize the persistance values per score
                    bool persistence = Scoreboard.GetPersistFlag(color);
                    if (persistence)
                    {
                        scorePersistFlags[i].SetValue(Strings.Localize("editWorldParams.on"));
                    }
                    else
                    {
                        scorePersistFlags[i].SetValue(Strings.Localize("editWorldParams.off"));
                    }

                    // Touch GUI Buttons by color.
                    GUIButton button = GUIButtonManager.GetButton(color);
                    Debug.Assert(null != button);

                    if (string.IsNullOrEmpty(button.Label))
                    {
                        touchGuiButtons[i].SetValue(Strings.Localize("editWorldParams.solidButton"));
                    }
                    else
                    {
                        touchGuiButtons[i].SetValue(Strings.Localize("editWorldParams.labeledButton"));
                    }
                }



                showVirtualController.Check = InGame.ShowVirtualController;
                if (InGame.XmlWorldData.LinkedToLevel != null)
                {
                    nextLevel.NextLevel = XmlDataHelper.LoadMetadataUnknownGenre((Guid)InGame.XmlWorldData.LinkedToLevel);
                }
                else
                {
                    //make sure the old value doesn't stick around
                    nextLevel.NextLevel = null;
                }

                InGame.inGame.RenderWorldAsThumbnail = true;
                InGame.IsLevelDirty = wasDirty;

                Foley.PlayMenuLoop();
            }
        }

        public void Deactivate(bool maintainActorInfo)
        {
            if (active)
            {
                lastSelectionIndex = grid.SelectionIndex;
                grid.Active = false;

                if (!maintainActorInfo)
                {
                    nextLevelMode = false;
                    actor = null;
                    reflexData = null;
                    editTypeForActor = Control.SIZEOF;
                }

                // Do stack handling here.  If we do it in the update object we have no
                // clue which order things get pushed and popped and madness ensues.
                CommandStack.Pop(commandMap);
                active = false;

                if (!IsInProgrammingTileMode())
                {
                    InGame.inGame.RenderWorldAsThumbnail = false;
                }

                // If we have a selected element, we want to deselect it on exiting so
                // that it removes any help overlay associated with it.
                if (grid.SelectionElement != null)
                {
                    grid.SelectionElement.Selected = false;
                }

                ToolTipManager.Clear();

                // Programming tile mode did not initialize camera values, so ignore
                if (editMode == EditMode.ChangeSettingMode)
                {
                    //
                    // Copy any values we need back out into the world.
                    //

                    // Camera mode, but only save if really exiting.  If
                    // going into CameraSet, just leave it alone.  Also, don't
                    // do anything if the mode hasn't changed.
                    if (CameraSetMode != true && initialCameraMode != cameraMode.CurIndex)
                    {
                        Terrain.Current.FixedCamera = false;
                        Terrain.Current.FixedOffsetCamera = false;
                        switch (cameraMode.CurIndex)
                        {
                            case 0:
                                // Fixed Position Camera
                                Terrain.Current.FixedCamera = true;
                                // If the user didn't go through the CameraSet use the existing values.
                                if (!fixedCameraSet)
                                {
                                    Terrain.Current.FixedCameraFrom = cameraFrom;
                                    Terrain.Current.FixedCameraAt = cameraAt;
                                    Terrain.Current.FixedCameraDistance = cameraDistance;
                                    Terrain.Current.FixedCameraPitch = cameraPitch;
                                    Terrain.Current.FixedCameraRotation = cameraRotation;
                                }
                                InGame.IsLevelDirty = true;
                                break;
                            case 1:
                                // FixedOffset Camera
                                Terrain.Current.FixedOffsetCamera = true;
                                // If the user didn't go through the CameraSet use the existing values.
                                if (!fixedOffsetCameraSet)
                                {
                                    Terrain.Current.FixedOffset = InGame.inGame.Camera.EyeOffset;
                                    Terrain.Current.FixedCameraFrom = cameraFrom;
                                    Terrain.Current.FixedCameraAt = cameraAt;
                                    Terrain.Current.FixedCameraDistance = cameraDistance;
                                    Terrain.Current.FixedCameraPitch = cameraPitch;
                                    Terrain.Current.FixedCameraRotation = cameraRotation;
                                }
                                InGame.IsLevelDirty = true;
                                break;
                            case 3:
                                // Free
                                // do nothing
                                break;
                        }

                    }
                }

                Foley.StopMenuLoop();
            }
        }

        public void LoadContent(bool immediate)
        {
            // grid is no longer preloaded with UI, this can dynamically change based on what controls are added to grid during runtime
            //            BokuGame.Load(grid, immediate);

            //
            foreach (UIGridElement element in gridElements)
            {
                BokuGame.Load(element, immediate);
            }
        }   // end of EditWorldParameters LoadContent()

        public void InitDeviceResources(GraphicsDevice device)
        {
        }

        public void UnloadContent()
        {
            foreach (UIGridElement element in gridElements)
            {
                BokuGame.Unload(element);
            }

            BokuGame.Unload(grid);
        }   // end of EditWorldParameters UnloadContent()

        /// <summary>
        /// Recreate render targets
        /// </summary>
        /// <param name="graphics"></param>
        public void DeviceReset(GraphicsDevice device)
        {
            BokuGame.DeviceReset(grid, device);
        }

    }   // end of class EditWorldParameters

}


