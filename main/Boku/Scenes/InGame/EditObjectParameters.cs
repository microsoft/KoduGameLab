
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
using Boku.SimWorld.Chassis;
using Boku.Programming;
using Boku.Common.Gesture;

namespace Boku
{
    public class EditObjectParameters : INeedsDeviceReset
    {
        #region Members 

        public const float k_ObjectMinScale = 0.2f;
        public const float k_ObjectMaxScale = 4.0f;

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

        public
        // Controls appear in the order of this enum
        enum Control
        {
            PushRange,
            PushStrength,

            LightRange,
            MovementSpeedModifier,
            TurningSpeedModifier,
            LinearAccelerationModifier,
            TurningAccelerationModifier,
            VerticalSpeedModifier,
            VerticalAccelerationModifier,
            Rename,
            Immobile,
            Invulnerable,
            ShowHitPoints,
            MaxHitPoints,
            Creatable,
            MaxCreated,
            StayAboveWater,
            ReScale,
            HoldDistance,
            Bounciness,
            Friction,
            
            BlipDamage,
            BlipReloadTime,
            BlipRange,
            BlipSpeed,
            BlipsInAir,
            
            MissileDamage,
            MissileReloadTime,
            MissileRange,
            MissileSpeed,
            MissilesInAir,
            MissileTrails,

            ShieldEffects,
            Invisible,
            Ignored,
            Camouflaged,

            Mute,
            Hearing,
            NearByDistance,
            FarAwayDistance,
            KickStrength,
            KickRate,
            GlowAmt,
            GlowLights,
            GlowEmission,
            DisplayLOS,
            DisplayLOP,
            DisplayCurrentPage,
            //RenderSensors,

            SIZEOF
        };
        
        private enum ControlSetup
        {
            Initialize,             
            AddToGridReflexData,    // Single parameter edit in programming tiles, eg size or hold distance.
            AddToGridEditObject,    // Regular object parameter editing.
        };

        private List<UIGridElement> gridElements = new List<UIGridElement>();

        UIGridModularFloatSliderElement pushrange;
        UIGridModularFloatSliderElement pushstrength;

        UIGridModularButtonElement rename;

        UIGridModularFloatSliderElement lightrange;
        UIGridModularCheckboxElement immobile;
        UIGridModularCheckboxElement invulnerable;
        UIGridModularCheckboxElement creatable;
        UIGridModularIntegerSliderElement maxCreated;
        UIGridModularCheckboxElement mute;
        UIGridModularFloatSliderElement hearing;
        //UIGridModularCheckboxElement renderSensors;
        UIGridModularCheckboxElement showHitPoints;
        UIGridModularIntegerSliderElement maxHitPoints;

        UIGridModularIntegerSliderElement blipDamage;
        UIGridModularFloatSliderElement blipRange;
        UIGridModularFloatSliderElement blipReloadTime;
        UIGridModularIntegerSliderElement blipSpeed;
        UIGridModularIntegerSliderElement blipsInAir;

        UIGridModularIntegerSliderElement missileDamage;
        UIGridModularFloatSliderElement missileRange;
        UIGridModularFloatSliderElement missileReloadTime;
        UIGridModularIntegerSliderElement missileSpeed;
        UIGridModularIntegerSliderElement missilesInAir;
        UIGridModularCheckboxElement missileTrails;

        UIGridModularCheckboxElement shieldEffects;
        UIGridModularCheckboxElement invisible;
        UIGridModularCheckboxElement ignored;
        UIGridModularCheckboxElement camouflaged;

        UIGridModularFloatSliderElement kickStrength;
        UIGridModularFloatSliderElement kickRate;
        UIGridModularFloatSliderElement rescale;
        UIGridModularFloatSliderElement holdDistanceMultiplier;
        UIGridModularFloatSliderElement movementSpeedModifier;
        UIGridModularFloatSliderElement turningSpeedModifier;
        UIGridModularFloatSliderElement linearAccelerationModifier;
        UIGridModularFloatSliderElement turningAccelerationModifier;
        UIGridModularFloatSliderElement verticalSpeedModifier;
        UIGridModularFloatSliderElement verticalAccelerationModifier;
        UIGridModularFloatSliderElement bounciness;
        UIGridModularFloatSliderElement friction;
        UIGridModularCheckboxElement stayAboveWater;
        UIGridModularFloatSliderElement glowAmt;
        UIGridModularFloatSliderElement glowLights;
        UIGridModularFloatSliderElement glowEmission;
        UIGridModularCheckboxElement displayLOS;
        UIGridModularCheckboxElement displayLOP;
        UIGridModularCheckboxElement displayCurrentPage;

        UIGridModularFloatSliderElement nearByDistance;
        UIGridModularFloatSliderElement farAwayDistance;


        private CommandMap commandMap = new CommandMap("EditObjectParameters");     // Placeholder for stack.

        private bool active = false;

        private GameActor actor = null;     // The actor we're editing.

        private ReflexData reflexData = null;

        #endregion

        #region Accessors
        public bool Active
        {
            get { return active; }
        }

        /// <summary>
        /// The GameActor whose settings are being edited.
        /// </summary>
        public GameActor Actor
        {
            get { return actor; }
            set { actor = value; }
        }
        #endregion

        #region Public

        // c'tor
        public EditObjectParameters()
        {
            SetupControl( ControlSetup.Initialize, null );
        }

        public bool IsInProgrammingTileMode()
        {
            return (editMode == EditMode.ProgrammingTileMode);
        }

        //
        public void Update()
        {
            if (active)
            {
                //Text edit dialog
                if (InGame.inGame.shared.textLineDialog.Active)
                {
                    InGame.inGame.shared.textLineDialog.Update();
                    return; // Don't let anything under us take input.
                }

                bool textDisplaying = InGame.inGame.shared.smallTextDisplay.Active ||
                                      InGame.inGame.shared.scrollableTextDisplay.Active;

                // If in focus element has help available, get it.
                UIGridElement e = grid.SelectionElement;
                string helpID = e.HelpID;
                string helpText = TweakScreenHelp.GetHelp(helpID);

                // Rendering goes directly to backbuffer.
                camera.Resolution = new Point((int)BokuGame.ScreenSize.X, (int)BokuGame.ScreenSize.Y);
                // If we're in portrait mode (or getting close), need to increase the FOV angle.
                if (camera.AspectRatio < 1.3f)
                {
                    camera.Fov = 1.3f / camera.AspectRatio;
                }
                camera.Update();

                if (GamePadInput.ActiveMode == GamePadInput.InputMode.Touch)
                {
                    if (!textDisplaying)
                    {

                        // Check for help tile.                        
                        Vector2 hitUV = Vector2.Zero;

                        TouchContact touch = TouchInput.GetOldestTouch();

                        if (null != touch)
                        {

                            Matrix mat = Matrix.CreateTranslation(-helpSquare.Position.X, -helpSquare.Position.Y, 0);
                            // Check hitting help square
                            hitUV = TouchInput.GetHitUV(touch.position, camera, ref mat, helpSquare.Size, helpSquare.Size, useRtCoords: false);
                            if (hitUV.X >= 0 && hitUV.X < 1 && hitUV.Y >= 0 && hitUV.Y < 1)
                            {
                                if (TouchInput.WasTouched)
                                {
                                    touch.TouchedObject= helpSquare;
                                }
                                if (TouchInput.WasReleased && touch.TouchedObject == helpSquare)
                                {
                                    ShowHelp(helpText);
                                }
                            }

                            mat = Matrix.CreateTranslation(-okSquare.Position.X, -okSquare.Position.Y, 0);
                            // Check hitting OK square
                            hitUV = TouchInput.GetHitUV(touch.position, camera, ref mat, okSquare.Size, okSquare.Size, useRtCoords: false);
                            if (hitUV.X >= 0 && hitUV.X < 1 && hitUV.Y >= 0 && hitUV.Y < 1)
                            {
                                if (TouchInput.WasTouched)
                                {
                                    touch.TouchedObject = okSquare;
                                }
                                if (TouchInput.WasReleased && touch.TouchedObject == okSquare)
                                {
                                    Deactivate();
                                }
                            }

                            // Check if mouse hitting current selection object.  Or should this be done in the object?
                            mat = Matrix.Invert(e.WorldMatrix);
                            hitUV = TouchInput.GetHitUV(touch.position, camera, ref mat, e.Size.X, e.Size.Y, useRtCoords: false);

                            bool focusElementHit = false;
                            if (hitUV.X >= 0 && hitUV.X < 1 && hitUV.Y >= 0 && hitUV.Y < 1)
                            {
                                if (touch.phase == TouchPhase.Began)
                                {
                                    touch.TouchedObject = e;
                                }
                                e.HandleTouchInput(touch, hitUV);
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

                                    e = grid.Get(0, i);
                                    mat = Matrix.Invert(e.WorldMatrix);
                                    hitUV = TouchInput.GetHitUV(touch.position, camera, ref mat, e.Size.X, e.Size.Y, useRtCoords: false);

                                    if (hitUV.X >= 0 && hitUV.X < 1 && hitUV.Y >= 0 && hitUV.Y < 1)
                                    {
                                        // We hit an element, so bring it into focus.
                                        grid.SelectionIndex = new Point(0, i);
                                        break;
                                    }

                                }
                            }

                            if (touch.TouchedObject != e)
                            {
                                grid.HandleTouchInput(camera);
                            }

                            // Allow right click or left click on nothing to exit.
//                             if (!hitAnything && TouchInput.TapGesture.WasTapped())
//                             {
//                                 Deactivate();
//                             }
                        }
                    }
                }
                else if (GamePadInput.ActiveMode == GamePadInput.InputMode.KeyboardMouse)
                {
                    // Mouse input.
                    // Don't do anything if help is being displayed.
                    if (!textDisplaying)
                    {
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
                                Deactivate();
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
//                         if (MouseInput.Right.WasPressed || (!hitAnything && MouseInput.Left.WasPressed))
//                         {
//                             Deactivate();
//                         }
                    }
                }


                GamePadInput pad = GamePadInput.GetGamePad0();

                UIGridElement prevE = grid.SelectionElement;

                // Update the grid.  Note this is done only if the text is not displaying
                // since that should have priority over the input.
                if (!textDisplaying)
                {
                    grid.Update(ref worldGrid);
                }

                // If the Update deactived us, bail.
                if(!active)
                    return;

                SetupForCreatable(actor);

                // For each element in the grid, calc it's screen space Y position
                // and give it a slight twist around the Y axis based on this.
                // Note this assumes that this grid is 1d vertical.
                for (int j = 0; j < grid.ActualDimensions.Y; j++)
                {
                    e = grid.Get(0, j);
                    Vector3 pos = Vector3.Transform(e.Position, grid.WorldMatrix);
                    Vector3 rot = Vector3.Zero;
                    float rotationScaling = 0.2f;
                    rot.Y = -rotationScaling * pos.Y;
                    e.Rotation = rot;
                }

                if (!textDisplaying)
                {
                    if (helpText != null && Actions.Help.WasPressed)
                    {
                        ShowHelp(helpText);
                    }
                    else if (Actions.A.WasPressed)
                    {
                        Deactivate();
                    }
                }
                else
                {
                    InGame.inGame.shared.smallTextDisplay.Update(camera);
                    InGame.inGame.shared.scrollableTextDisplay.Update(camera);
                }

                helpSquare.Update();
                if (prevE != grid.SelectionElement)
                {
                    helpSquare.Show();
                }

                okSquare.Update();
                if (prevE != grid.SelectionElement)
                {
                    okSquare.Show();
                }

            }   // end of if active

        }   // end of EditObjectParameters Update()

        private void ShowHelp(string helpText)
        {
            if (!string.IsNullOrEmpty(helpText))
            {
                InGame.inGame.shared.smallTextDisplay.Activate(null, helpText, UIGridElement.Justification.Center, false, useRtCoords: false);
                if (InGame.inGame.shared.smallTextDisplay.Overflow)
                {
                    InGame.inGame.shared.smallTextDisplay.Deactivate();
                    InGame.inGame.shared.scrollableTextDisplay.Activate(null, helpText, UIGridElement.Justification.Center, false, useRtCoords: false);
                }
            }
            else
            {
                Debug.Assert(false, "Shouldn't there be something here???");
            }
        }   // end of ShowHelp()

        public void Render()
        {
            if (active)
            {
                // Render menu using local camera.
                Fx.ShaderGlobals.SetCamera(camera);

                grid.Render(camera);

                helpSquare.Render(camera);

                okSquare.Render(camera);

                ToolTipManager.Render(camera);

                //Text edit dialog
                if (InGame.inGame.shared.textLineDialog.Active)
                {
                    InGame.inGame.shared.textLineDialog.Render();
                }

            }

        }   // end of EditObjectParameters Render()

        public void OnSelect(UIGrid grid)
        {
            // Normally the grid wil deactivate itself when a selection is made.
            // In the options/settings case there are some elements that ignore 
            // the Select action letting it get to the grid which then deactivates
            // itself.  We don't want that to happen so set the grid active here.
            grid.Active = true;

            //
            Deactivate();
        }   // end of OnSelect()

        public void OnCancel(UIGrid grid)
        {
            Deactivate();
        }   // end of OnCancel()

        #endregion

        #region Internal

        // Use for setting up control UI on constructor as well as adding to grid once activated
        private void SetupControl( ControlSetup setupType, Control? controlType )
        {
            UIGridElement.ParamBlob blob = null;

            if ( setupType == ControlSetup.Initialize )
            {
                // The UI is rendered directly to backbuffer so set the camera up accordingly.
                camera = new PerspectiveUICamera();
                camera.Resolution = new Point((int)BokuGame.ScreenSize.X, (int)BokuGame.ScreenSize.Y);

                grid = new UIGrid(OnSelect, OnCancel, new Point(1, (int)Control.SIZEOF), "EditObjectParameters");
                grid.LocalMatrix = Matrix.CreateTranslation(0.25f / 96.0f, 0.25f / 96.0f, 0.0f);
                grid.RenderEndsIn = true;
                grid.UseMouseScrollWheel = true;

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

            // Create elements here.
            //

            #region pushrange

            if ( setupType == ControlSetup.Initialize )
            {
                /// This region only active for pushes.
                pushrange = new UIGridModularFloatSliderElement(blob, Strings.Localize("editObjectParams.pushrange"));
                pushrange.MinValue = 1.0f;
                pushrange.MaxValue = 100.0f;
                pushrange.IncrementByAmount = 5.0f;
                pushrange.NumberOfDecimalPlaces = 0;
                pushrange.HelpID = "PushRange";
                gridElements.Add(pushrange);
            }
            else if (setupType == ControlSetup.AddToGridEditObject && controlType == Control.PushRange )
            {
                pushrange.OnChange = delegate(float range) { actor.PushRange = range; InGame.IsLevelDirty = true; };                
                grid.Add(pushrange, 0, (int)Control.PushRange);
            }
            
            #endregion pushrange

            #region pushstrength
            if (setupType == ControlSetup.Initialize)
            {
                /// This region only active for pushes.
                pushstrength = new UIGridModularFloatSliderElement(blob, Strings.Localize("editObjectParams.pushstrength"));
                pushstrength.MinValue = 1.0f;
                pushstrength.MaxValue = 150.0f;
                pushstrength.IncrementByAmount = 1.0f;
                pushstrength.NumberOfDecimalPlaces = 0;                
                pushstrength.HelpID = "PushStrength";
                gridElements.Add(pushstrength);
            }
            else if (setupType == ControlSetup.AddToGridEditObject && controlType == Control.PushStrength)
            {
                pushstrength.OnChange = delegate(float range) { actor.PushStrength = range; InGame.IsLevelDirty = true; };
                grid.Add(pushstrength, 0, (int)Control.PushStrength);
            }
            #endregion pushstrength

            #region rename
            if (setupType == ControlSetup.Initialize)
            {
                UIGridModularButtonElement.UIButtonElementEvent onA = delegate()
                {
                    TextLineDialog.OnDialogDone callback = delegate(bool canceled, string newText)
                    {
                        if (!canceled && newText.Length > 0)
                        {
                            newText = TextHelper.FilterURLs(newText);
                            newText = TextHelper.FilterEmail(newText);

                            actor.DisplayName = newText;
                            Programming.NamedFilter.RegisterInCardSpace(actor);
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

                    InGame.inGame.shared.textLineDialog.Activate(callback, actor.DisplayName, validateCallback);

                };

                rename = new UIGridModularButtonElement(blob, Strings.Localize("editObjectParams.renameTitle"),
                                                              Strings.Localize("editObjectParams.rename"), onA,
                                                              null, null);
                rename.HelpID = "Rename";

                gridElements.Add(rename);

            }
            else if (setupType == ControlSetup.AddToGridEditObject && controlType == Control.LightRange)
            {
                grid.Add(rename, 0, (int)Control.Rename);
            }
            #endregion rename

            #region lightrange

            if (setupType == ControlSetup.Initialize)
            {
                /// This region only active for lights.
                lightrange = new UIGridModularFloatSliderElement(blob, Strings.Localize("editObjectParams.lightrange"));
                lightrange.MinValue = 0.0f;
                lightrange.MaxValue = 100.0f;
                lightrange.IncrementByAmount = 5.0f;
                lightrange.NumberOfDecimalPlaces = 0;                
                lightrange.HelpID = "LightRange";
                gridElements.Add(lightrange);
                
            }
            else if (setupType == ControlSetup.AddToGridEditObject && controlType == Control.LightRange)
            {
                lightrange.OnChange = delegate(float range) { actor.LightRange = LightRangeToRadius(range); InGame.IsLevelDirty = true; };
                grid.Add(lightrange, 0, (int)Control.LightRange);
            }
            #endregion lightrange

            #region immobile
            if (setupType == ControlSetup.Initialize)
            {
                immobile = new UIGridModularCheckboxElement(blob, Strings.Localize("editObjectParams.immobile"));
                immobile.HelpID = "Immobile";
                gridElements.Add(immobile);
            }
            else if (setupType == ControlSetup.AddToGridEditObject && controlType == Control.Immobile)
            {
                immobile.OnCheck = delegate() { actor.TweakImmobile = true; InGame.IsLevelDirty = true; };
                immobile.OnClear = delegate() { actor.TweakImmobile = false; InGame.IsLevelDirty = true; };
                grid.Add(immobile, 0, (int)Control.Immobile);
            }
            #endregion

            #region invulnerable
            if (setupType == ControlSetup.Initialize)
            {
                invulnerable = new UIGridModularCheckboxElement(blob, Strings.Localize("editObjectParams.invulnerable"));
                invulnerable.HelpID = "Invulnerable";
                gridElements.Add(invulnerable);
            }
            else if (setupType == ControlSetup.AddToGridEditObject && controlType == Control.Invulnerable)
            {
                invulnerable.OnCheck = delegate() { actor.TweakInvulnerable = true; InGame.IsLevelDirty = true; };
                invulnerable.OnClear = delegate() { actor.TweakInvulnerable = false; InGame.IsLevelDirty = true; };
                grid.Add(invulnerable, 0, (int)Control.Invulnerable);
            }
            #endregion

            #region creatable
            if (setupType == ControlSetup.Initialize)
            {
                creatable = new UIGridModularCheckboxElement(blob, Strings.Localize("editObjectParams.creatable"));
                creatable.HelpID = "Creatable";
                gridElements.Add(creatable);
            }
            else if (setupType == ControlSetup.AddToGridEditObject && controlType == Control.Creatable)
            {
                creatable.OnCheck = delegate() { actor.Creatable = true; InGame.IsLevelDirty = true; Instrumentation.IncrementCounter(Instrumentation.CounterId.DefinedCreatable); };
                creatable.OnClear = delegate() { actor.Creatable = false; InGame.IsLevelDirty = true; };
                grid.Add(creatable, 0, (int)Control.Creatable);
            }
            #endregion

            #region maxcreated
            if (setupType == ControlSetup.Initialize)
            {
                maxCreated = new UIGridModularIntegerSliderElement(blob, Strings.Localize("editObjectParams.maxCreated"));
                maxCreated.MinValue = 0;
                maxCreated.MaxValue = 1000;
                maxCreated.IncrementByAmount = 1;
                maxCreated.HelpID = "MaxCreated";
                gridElements.Add(maxCreated);
            }
            else if (setupType == ControlSetup.AddToGridEditObject && controlType == Control.MaxCreated)
            {
                maxCreated.OnChange = delegate(int points) { actor.MaxCreated = points; InGame.IsLevelDirty = true; };
                grid.Add(maxCreated, 0, (int)Control.MaxCreated);
            }
            #endregion

            #region glow works as light
            if (setupType == ControlSetup.Initialize)
            {
                glowLights = new UIGridModularFloatSliderElement(blob, Strings.Localize("editObjectParams.glowLights"));
                glowLights.MinValue = 0.0f;
                glowLights.MaxValue = 10.0f;
                glowLights.IncrementByAmount = 0.1f;
                glowLights.NumberOfDecimalPlaces = 1;                
                glowLights.HelpID = "GlowStrength";
                gridElements.Add(glowLights);
            }
            else if (setupType == ControlSetup.AddToGridEditObject && controlType == Control.GlowLights)
            {
                glowLights.OnChange = delegate(float amt) { actor.GlowLights = amt / 10.0f; InGame.IsLevelDirty = true; };
                grid.Add(glowLights, 0, (int)Control.GlowLights);
            }
            #endregion

            #region scale
            if (setupType == ControlSetup.Initialize)
            {
                rescale = new UIGridModularFloatSliderElement(blob, Strings.Localize("editObjectParams.reScale"));
                rescale.MinValue = k_ObjectMinScale;
                rescale.MaxValue = k_ObjectMaxScale;
                rescale.IncrementByAmount = 0.1f;
                rescale.NumberOfDecimalPlaces = 1;               
                rescale.HelpID = "ReScale";
                gridElements.Add(rescale);                                
            }
            else if (setupType == ControlSetup.AddToGridEditObject && controlType == Control.ReScale)
            {
                rescale.OnChange = delegate(float s) { actor.ReScale = s; InGame.IsLevelDirty = true; };
                grid.Add(rescale, 0, (int)Control.ReScale);
            }
            else if (setupType == ControlSetup.AddToGridReflexData && controlType == Control.ReScale)
            {
                rescale.OnChange = delegate(float s) { reflexData.ReScale = s; reflexData.ReScaleEnabled = true; };
                grid.Add(rescale, 0, (int)Control.ReScale);
            }
            #endregion

            #region holdingPositionMultiplier
            if (setupType == ControlSetup.Initialize)
            {
                holdDistanceMultiplier = new UIGridModularFloatSliderElement(blob, Strings.Localize("editObjectParams.holdDistance"));
                holdDistanceMultiplier.MinValue = 1.0f;
                holdDistanceMultiplier.MaxValue = 5.0f;
                holdDistanceMultiplier.IncrementByAmount = 0.1f;
                holdDistanceMultiplier.NumberOfDecimalPlaces = 1;
                holdDistanceMultiplier.HelpID = "HoldDistance";
                gridElements.Add(holdDistanceMultiplier);
            }
            else if (setupType == ControlSetup.AddToGridEditObject && controlType == Control.HoldDistance)
            {
                holdDistanceMultiplier.OnChange = delegate(float s) { actor.HoldDistance = s; InGame.IsLevelDirty = true; };
                grid.Add(holdDistanceMultiplier, 0, (int)Control.HoldDistance);
            }
            else if (setupType == ControlSetup.AddToGridReflexData && controlType == Control.HoldDistance)
            {
                holdDistanceMultiplier.OnChange = delegate(float s) { reflexData.HoldDistance = s; };
                grid.Add(holdDistanceMultiplier, 0, (int)Control.HoldDistance);
            }
            #endregion

            #region bounciness
            if (setupType == ControlSetup.Initialize)
            {
                bounciness = new UIGridModularFloatSliderElement(blob, Strings.Localize("editObjectParams.bounciness"));
                bounciness.MinValue = 0.0f;
                bounciness.MaxValue = 1.0f;
                bounciness.IncrementByAmount = 0.05f;
                bounciness.NumberOfDecimalPlaces = 2;                
                bounciness.HelpID = "Bounciness";
                gridElements.Add(bounciness);
            }
            else if (setupType == ControlSetup.AddToGridEditObject && controlType == Control.Bounciness)
            {
                bounciness.OnChange = delegate(float value) { actor.CoefficientOfRestitution = value; InGame.IsLevelDirty = true; };
                grid.Add(bounciness, 0, (int)Control.Bounciness);
            }
            #endregion bounciness    
        
            #region friction
            if (setupType == ControlSetup.Initialize)
            {
                friction = new UIGridModularFloatSliderElement(blob, Strings.Localize("editObjectParams.friction"));
                friction.MinValue = 0.0f;
                friction.MaxValue = 1.0f;
                friction.IncrementByAmount = 0.05f;
                friction.NumberOfDecimalPlaces = 2;                
                friction.HelpID = "Friction";
                gridElements.Add(friction);
            }
            else if (setupType == ControlSetup.AddToGridEditObject && controlType == Control.Friction)
            {
                friction.OnChange = delegate(float value) { actor.Friction = value; InGame.IsLevelDirty = true; };
                grid.Add(friction, 0, (int)Control.Friction);
            }
            #endregion friction

            #region stayAboveWater
            if (setupType == ControlSetup.Initialize)
            {
                stayAboveWater = new UIGridModularCheckboxElement(blob, Strings.Localize("editObjectParams.stayAboveWater"));
                stayAboveWater.HelpID = "StayAboveWater";
                gridElements.Add(stayAboveWater);
            }
            else if (setupType == ControlSetup.AddToGridEditObject && controlType == Control.StayAboveWater)
            {
                stayAboveWater.OnCheck = delegate() { actor.StayAboveWater = true; InGame.IsLevelDirty = true; };
                stayAboveWater.OnClear = delegate() { actor.StayAboveWater = false; InGame.IsLevelDirty = true; };
                grid.Add(stayAboveWater, 0, (int)Control.StayAboveWater);
            }
            #endregion stayAboveWater

            #region mute
            if (setupType == ControlSetup.Initialize)
            {
                mute = new UIGridModularCheckboxElement(blob, Strings.Localize("editObjectParams.mute"));
                mute.HelpID = "Mute";
                gridElements.Add(mute);
            }
            else if (setupType == ControlSetup.AddToGridEditObject && controlType == Control.Mute)
            {
                mute.OnCheck = delegate() { actor.Mute = true; InGame.IsLevelDirty = true; };
                mute.OnClear = delegate() { actor.Mute = false; InGame.IsLevelDirty = true; };
                grid.Add(mute, 0, (int)Control.Mute);
            }
            #endregion

            #region hearing
            if (setupType == ControlSetup.Initialize)
            {
                hearing = new UIGridModularFloatSliderElement(blob, Strings.Localize("editObjectParams.hearing"));
                hearing.MinValue = 0.0f;
                hearing.MaxValue = 100.0f;
                hearing.IncrementByAmount = 0.5f;
                hearing.NumberOfDecimalPlaces = 1;              
                hearing.HelpID = "Hearing";
                gridElements.Add(hearing);
            }
            else if (setupType == ControlSetup.AddToGridEditObject && controlType == Control.Hearing)
            {
                hearing.OnChange = delegate(float hear) { actor.Hearing = hear / 100.0f; InGame.IsLevelDirty = true; };
                grid.Add(hearing, 0, (int)Control.Hearing);
            }
            else if (setupType == ControlSetup.AddToGridReflexData && controlType == Control.Hearing)
            {
                hearing.OnChange = delegate(float hear) { reflexData.ParamFloat = hear / 100.0f; InGame.IsLevelDirty = true; };
                hearing.CurrentValue = reflexData.ParamFloat * 100.0f;
                grid.Add(hearing, 0, (int)Control.Hearing);
            }

            #endregion

            #region showhitpoints
            if (setupType == ControlSetup.Initialize)
            {
                showHitPoints = new UIGridModularCheckboxElement(blob, Strings.Localize("editObjectParams.showHitPoints"));
                showHitPoints.HelpID = "ShowHitPoints";
                gridElements.Add(showHitPoints);
            }
            else if (setupType == ControlSetup.AddToGridEditObject && controlType == Control.ShowHitPoints)
            {
                showHitPoints.OnCheck = delegate() { actor.ShowHitPoints = true; InGame.IsLevelDirty = true; };
                showHitPoints.OnClear = delegate() { actor.ShowHitPoints = false; InGame.IsLevelDirty = true; };
                grid.Add(showHitPoints, 0, (int)Control.ShowHitPoints);
            }
            #endregion showhitpoints

            #region maxhitpoints
            if (setupType == ControlSetup.Initialize)
            {
                maxHitPoints = new UIGridModularIntegerSliderElement(blob, Strings.Localize("editObjectParams.maxHitPoints"));
                maxHitPoints.MinValue = 0;
                maxHitPoints.MaxValue = 1000;
                maxHitPoints.IncrementByAmount = 5;
                maxHitPoints.HelpID = "MaxHitPoints";
                gridElements.Add(maxHitPoints);
            }
            else if (setupType == ControlSetup.AddToGridEditObject && controlType == Control.MaxHitPoints)
            {
                maxHitPoints.OnChange = delegate(int points) { actor.MaxHitPoints = points; InGame.IsLevelDirty = true; };
                grid.Add(maxHitPoints, 0, (int)Control.MaxHitPoints);
            }
            else if (setupType == ControlSetup.AddToGridReflexData && controlType == Control.MaxHitPoints)
            {
                maxHitPoints.OnChange = delegate(int points) { reflexData.MaxHitpoints = points; InGame.IsLevelDirty = true; };
                maxHitPoints.CurrentValue = reflexData.MaxHitpoints;
                grid.Add(maxHitPoints, 0, (int)Control.MaxHitPoints);
            }
            #endregion

            #region blip damage
            if (setupType == ControlSetup.Initialize)
            {
                blipDamage = new UIGridModularIntegerSliderElement(blob, Strings.Localize("editObjectParams.blipDamage"));
                blipDamage.MinValue = -500;
                blipDamage.MaxValue = 500;
                blipDamage.IncrementByAmount = 1;
                blipDamage.HelpID = "BlipDamage";
                gridElements.Add(blipDamage);
            }
            else if (setupType == ControlSetup.AddToGridEditObject && controlType == Control.BlipDamage)
            {
                blipDamage.OnChange = delegate(int value) { actor.BlipDamage = value; InGame.IsLevelDirty = true; };
                grid.Add(blipDamage, 0, (int)Control.BlipDamage);
            }
            else if (setupType == ControlSetup.AddToGridReflexData && controlType == Control.BlipDamage)
            {
                blipDamage.OnChange = delegate(int points) { reflexData.ParamInt = points; InGame.IsLevelDirty = true; };
                blipDamage.CurrentValue = reflexData.ParamInt;
                grid.Add(blipDamage, 0, (int)Control.BlipDamage);
            }
            #endregion

            #region blip reload time
            if (setupType == ControlSetup.Initialize)
            {
                blipReloadTime = new UIGridModularFloatSliderElement(blob, Strings.Localize("editObjectParams.blipReloadTime"));
                blipReloadTime.MinValue = 0.05f;
                blipReloadTime.MaxValue = 3.0f;
                blipReloadTime.IncrementByAmount = 0.05f;
                blipReloadTime.NumberOfDecimalPlaces = 2;
                blipReloadTime.HelpID = "BlipReloadTime";
                gridElements.Add(blipReloadTime);
            }
            else if (setupType == ControlSetup.AddToGridEditObject && controlType == Control.BlipReloadTime)
            {
                blipReloadTime.OnChange = delegate(float t) { actor.BlipReloadTime = t; InGame.IsLevelDirty = true; };
                grid.Add(blipReloadTime, 0, (int)Control.BlipReloadTime);
            }
            else if (setupType == ControlSetup.AddToGridReflexData && controlType == Control.BlipReloadTime)
            {
                blipReloadTime.OnChange = delegate(float secs) { reflexData.ParamFloat = secs; InGame.IsLevelDirty = true; };
                blipReloadTime.CurrentValue = reflexData.ParamFloat;
                grid.Add(blipReloadTime, 0, (int)Control.BlipReloadTime);
            }

            #endregion

            #region blip speed
            if (setupType == ControlSetup.Initialize)
            {
                blipSpeed = new UIGridModularIntegerSliderElement(blob, Strings.Localize("editObjectParams.blipSpeed"));
                blipSpeed.MinValue = 5;
                blipSpeed.MaxValue = 100;
                blipSpeed.IncrementByAmount = 5;
                blipSpeed.HelpID = "BlipSpeed";
                gridElements.Add(blipSpeed);
            }
            else if (setupType == ControlSetup.AddToGridEditObject && controlType == Control.BlipSpeed)
            {
                blipSpeed.OnChange = delegate(int rate) { actor.BlipSpeed = rate; InGame.IsLevelDirty = true; };
                grid.Add(blipSpeed, 0, (int)Control.BlipSpeed);
            }
            #endregion

            #region blip range
            if (setupType == ControlSetup.Initialize)
            {
                blipRange = new UIGridModularFloatSliderElement(blob, Strings.Localize("editObjectParams.blipRange"));
                blipRange.MinValue = 10;
                blipRange.MaxValue = 100;
                blipRange.IncrementByAmount = 5;
                blipRange.NumberOfDecimalPlaces = 0;
                blipRange.HelpID = "BlipRange";
                gridElements.Add(blipRange);
            }
            else if (setupType == ControlSetup.AddToGridEditObject && controlType == Control.BlipRange)
            {
                blipRange.OnChange = delegate(float range) { actor.BlipRange = range; InGame.IsLevelDirty = true; };
                grid.Add(blipRange, 0, (int)Control.BlipRange);
            }
            else if (setupType == ControlSetup.AddToGridReflexData && controlType == Control.BlipRange)
            {
                blipRange.OnChange = delegate(float range) { reflexData.ParamFloat = range; InGame.IsLevelDirty = true; };
                blipRange.CurrentValue = reflexData.ParamFloat;
                grid.Add(blipRange, 0, (int)Control.BlipRange);
            }
            #endregion blip range

            #region blips in air
            if (setupType == ControlSetup.Initialize)
            {
                blipsInAir = new UIGridModularIntegerSliderElement(blob, Strings.Localize("editObjectParams.blipsInAir"));
                blipsInAir.MinValue = 1;
                blipsInAir.MaxValue = 200;
                blipsInAir.IncrementByAmount = 1;                
                blipsInAir.HelpID = "BlipsInAir";
                gridElements.Add(blipsInAir);
            }
            else if (setupType == ControlSetup.AddToGridEditObject && controlType == Control.BlipsInAir)
            {
                blipsInAir.OnChange = delegate(int num) { actor.BlipsInAir = num; InGame.IsLevelDirty = true; };
                grid.Add(blipsInAir, 0, (int)Control.BlipsInAir);
            }
            #endregion blips in air

            #region missile damage
            if (setupType == ControlSetup.Initialize)
            {
                missileDamage = new UIGridModularIntegerSliderElement(blob, Strings.Localize("editObjectParams.missileDamage"));
                missileDamage.MinValue = -500;
                missileDamage.MaxValue = 500;
                missileDamage.IncrementByAmount = 1;
                missileDamage.HelpID = "MissileDamage";
                gridElements.Add(missileDamage);
            }
            else if (setupType == ControlSetup.AddToGridEditObject && controlType == Control.MissileDamage)
            {
                missileDamage.OnChange = delegate(int value) { actor.MissileDamage = value; InGame.IsLevelDirty = true; };
                grid.Add(missileDamage, 0, (int)Control.MissileDamage);
            }
            else if (setupType == ControlSetup.AddToGridReflexData && controlType == Control.MissileDamage)
            {
                missileDamage.OnChange = delegate(int points) { reflexData.ParamInt = points; InGame.IsLevelDirty = true; };
                missileDamage.CurrentValue = reflexData.ParamInt;
                grid.Add(missileDamage, 0, (int)Control.MissileDamage);
            }
            #endregion

            #region missile reload time
            if (setupType == ControlSetup.Initialize)
            {
                missileReloadTime = new UIGridModularFloatSliderElement(blob, Strings.Localize("editObjectParams.missileReloadTime"));
                missileReloadTime.MinValue = 0.5f;
                missileReloadTime.MaxValue = 5.0f;
                missileReloadTime.IncrementByAmount = 0.5f;
                missileReloadTime.NumberOfDecimalPlaces = 1;
                missileReloadTime.HelpID = "MissileReloadTime";
                gridElements.Add(missileReloadTime);
            }
            else if (setupType == ControlSetup.AddToGridEditObject && controlType == Control.MissileReloadTime)
            {
                missileReloadTime.OnChange = delegate(float t) { actor.MissileReloadTime = t; InGame.IsLevelDirty = true; };
                grid.Add(missileReloadTime, 0, (int)Control.MissileReloadTime);
            }
            else if (setupType == ControlSetup.AddToGridReflexData && controlType == Control.MissileReloadTime)
            {
                missileReloadTime.OnChange = delegate(float secs) { reflexData.ParamFloat = secs; InGame.IsLevelDirty = true; };
                missileReloadTime.CurrentValue = reflexData.ParamFloat;
                grid.Add(missileReloadTime, 0, (int)Control.MissileReloadTime);
            }
            #endregion

            #region missile speed
            if (setupType == ControlSetup.Initialize)
            {
                missileSpeed = new UIGridModularIntegerSliderElement(blob, Strings.Localize("editObjectParams.missileSpeed"));
                missileSpeed.MinValue = 1;
                missileSpeed.MaxValue = 20;
                missileSpeed.IncrementByAmount = 1;
                missileSpeed.HelpID = "MissileSpeed";
                gridElements.Add(missileSpeed);
            }
            else if (setupType == ControlSetup.AddToGridEditObject && controlType == Control.MissileSpeed)
            {
                missileSpeed.OnChange = delegate(int rate) { actor.MissileSpeed = rate; InGame.IsLevelDirty = true; };
                grid.Add(missileSpeed, 0, (int)Control.MissileSpeed);
            }
            #endregion

            #region missile range
            if (setupType == ControlSetup.Initialize)
            {
                missileRange = new UIGridModularFloatSliderElement(blob, Strings.Localize("editObjectParams.missileRange"));
                missileRange.MinValue = 10;
                missileRange.MaxValue = 100;
                missileRange.IncrementByAmount = 5;
                missileRange.NumberOfDecimalPlaces = 0;
                missileRange.HelpID = "MissileRange";
                gridElements.Add(missileRange);
            }
            else if (setupType == ControlSetup.AddToGridEditObject && controlType == Control.MissileRange)
            {
                missileRange.OnChange = delegate(float range) { actor.MissileRange = range; InGame.IsLevelDirty = true; };
                grid.Add(missileRange, 0, (int)Control.MissileRange);
            }
            else if (setupType == ControlSetup.AddToGridReflexData && controlType == Control.MissileRange)
            {
                missileRange.OnChange = delegate(float range) { reflexData.ParamFloat = range; InGame.IsLevelDirty = true; };
                missileRange.CurrentValue = reflexData.ParamFloat;
                grid.Add(missileRange, 0, (int)Control.MissileRange);
            }
            #endregion missile range

            #region missiles in air
            if (setupType == ControlSetup.Initialize)
            {
                missilesInAir = new UIGridModularIntegerSliderElement(blob, Strings.Localize("editObjectParams.missilesInAir"));
                missilesInAir.MinValue = 1;
                missilesInAir.MaxValue = 10;
                missilesInAir.IncrementByAmount = 1;
                missilesInAir.HelpID = "MissilesInAir";
                gridElements.Add(missilesInAir);
            }
            else if (setupType == ControlSetup.AddToGridEditObject && controlType == Control.MissilesInAir)
            {
                missilesInAir.OnChange = delegate(int num) { actor.MissilesInAir = num; InGame.IsLevelDirty = true; };
                grid.Add(missilesInAir, 0, (int)Control.MissilesInAir);
            }
            #endregion missiles in air

            #region show missile trails
            if (setupType == ControlSetup.Initialize)
            {
                missileTrails = new UIGridModularCheckboxElement(blob, Strings.Localize("editObjectParams.missileTrails"));
                missileTrails.HelpID = "MissileSmoke";
                gridElements.Add(missileTrails);
            }
            else if (setupType == ControlSetup.AddToGridEditObject && controlType == Control.MissileTrails)
            {
                missileTrails.OnCheck = delegate() { actor.MissileTrails = true; InGame.IsLevelDirty = true; };
                missileTrails.OnClear = delegate() { actor.MissileTrails = false; InGame.IsLevelDirty = true; };
                grid.Add(missileTrails, 0, (int)Control.MissileTrails);
            }
            #endregion

            #region show shield effects
            if (setupType == ControlSetup.Initialize)
            {
                shieldEffects = new UIGridModularCheckboxElement(blob, Strings.Localize("editObjectParams.shieldEffects"));
                shieldEffects.HelpID = "ShieldEffects";
                gridElements.Add(shieldEffects);
            }
            else if (setupType == ControlSetup.AddToGridEditObject && controlType == Control.ShieldEffects)
            {
                shieldEffects.OnCheck = delegate() { actor.ShieldEffects = true; InGame.IsLevelDirty = true; };
                shieldEffects.OnClear = delegate() { actor.ShieldEffects = false; InGame.IsLevelDirty = true; };
                grid.Add(shieldEffects, 0, (int)Control.ShieldEffects);
            }
            #endregion

            #region invisible
            if (setupType == ControlSetup.Initialize)
            {
                invisible = new UIGridModularCheckboxElement(blob, Strings.Localize("editObjectParams.invisible"));
                invisible.HelpID = "Invisible";
                gridElements.Add(invisible);
            }
            else if (setupType == ControlSetup.AddToGridEditObject && controlType == Control.Invisible)
            {
                invisible.OnCheck = delegate() { actor.Invisible = true; InGame.IsLevelDirty = true; };
                invisible.OnClear = delegate() { actor.Invisible = false; InGame.IsLevelDirty = true; };
                grid.Add(invisible, 0, (int)Control.Invisible);
            }
            #endregion invisible

            #region ignored
            if (setupType == ControlSetup.Initialize)
            {
                ignored = new UIGridModularCheckboxElement(blob, Strings.Localize("editObjectParams.ignored"));
                ignored.HelpID = "Ignored";
                gridElements.Add(ignored);
            }
            else if (setupType == ControlSetup.AddToGridEditObject && controlType == Control.Ignored)
            {
                ignored.OnCheck = delegate() { actor.Ignored = true; InGame.IsLevelDirty = true; };
                ignored.OnClear = delegate() { actor.Ignored = false; InGame.IsLevelDirty = true; };
                grid.Add(ignored, 0, (int)Control.Ignored);
            }
            #endregion ignored

            #region camouflaged
            if (setupType == ControlSetup.Initialize)
            {
                camouflaged = new UIGridModularCheckboxElement(blob, Strings.Localize("editObjectParams.camouflaged"));
                camouflaged.HelpID = "Camouflaged";
                gridElements.Add(camouflaged);
            }
            else if (setupType == ControlSetup.AddToGridEditObject && controlType == Control.Camouflaged)
            {
                camouflaged.OnCheck = delegate() { actor.Camouflaged = true; InGame.IsLevelDirty = true; };
                camouflaged.OnClear = delegate() { actor.Camouflaged = false; InGame.IsLevelDirty = true; };
                grid.Add(camouflaged, 0, (int)Control.Camouflaged);
            }
            #endregion camouflaged

            #region kick strength
            if (setupType == ControlSetup.Initialize)
            {
                kickStrength = new UIGridModularFloatSliderElement(blob, Strings.Localize("editObjectParams.kickStrength"));
                kickStrength.MinValue = 1.0f;
                kickStrength.MaxValue = 20.0f;
                kickStrength.IncrementByAmount = 1.0f;
                kickStrength.NumberOfDecimalPlaces = 0;
                kickStrength.HelpID = "KickStrength";
                gridElements.Add(kickStrength);
            }
            else if (setupType == ControlSetup.AddToGridEditObject && controlType == Control.KickStrength)
            {
                kickStrength.OnChange = delegate(float Strength) { actor.KickStrength = Strength; InGame.IsLevelDirty = true; };
                grid.Add(kickStrength, 0, (int)Control.KickStrength);
            }
            #endregion

            #region kick rate
            if (setupType == ControlSetup.Initialize)
            {
                kickRate = new UIGridModularFloatSliderElement(blob, Strings.Localize("editObjectParams.kickRate"));
                kickRate.MinValue = 1.0f;
                kickRate.MaxValue = 10.0f;
                kickRate.IncrementByAmount = 1.0f;
                kickRate.NumberOfDecimalPlaces = 0;
                kickRate.HelpID = "KickRate";
                gridElements.Add(kickRate);
            }
            else if (setupType == ControlSetup.AddToGridEditObject && controlType == Control.KickRate)
            {
                kickRate.OnChange = delegate(float rate) { actor.KickRate = rate; InGame.IsLevelDirty = true; };
                grid.Add(kickRate, 0, (int)Control.KickRate);
            }
            #endregion

            #region movement speed modifier
            if (setupType == ControlSetup.Initialize)
            {
                movementSpeedModifier = new UIGridModularFloatSliderElement(blob, Strings.Localize("editObjectParams.movementSpeedMultiplier"));
                movementSpeedModifier.MinValue = 0.1f;
                movementSpeedModifier.MaxValue = 5.0f;
                movementSpeedModifier.IncrementByAmount = 0.1f;
                movementSpeedModifier.NumberOfDecimalPlaces = 1;
                movementSpeedModifier.HelpID = "SpeedMultiplier";
                gridElements.Add(movementSpeedModifier);
            }
            else if (setupType == ControlSetup.AddToGridEditObject && controlType == Control.MovementSpeedModifier)
            {
                movementSpeedModifier.OnChange = delegate(float modifier) { actor.MovementSpeedModifier = modifier; InGame.IsLevelDirty = true; };
                grid.Add(movementSpeedModifier, 0, (int)Control.MovementSpeedModifier);
            }
            else if (setupType == ControlSetup.AddToGridReflexData && controlType == Control.MovementSpeedModifier)
            {
                movementSpeedModifier.OnChange = delegate(float modifier) { reflexData.MoveSpeedTileModifier = modifier; InGame.IsLevelDirty = true; };
                movementSpeedModifier.CurrentValueImmediate = reflexData.MoveSpeedTileModifier;
                grid.Add(movementSpeedModifier, 0, (int)Control.MovementSpeedModifier);
            }
            #endregion movement speed modifier

            #region turning speed modifier
            if (setupType == ControlSetup.Initialize)
            {
                turningSpeedModifier = new UIGridModularFloatSliderElement(blob, Strings.Localize("editObjectParams.turningSpeedMultiplier"));
                turningSpeedModifier.MinValue = 0.1f;
                turningSpeedModifier.MaxValue = 5.0f;
                turningSpeedModifier.IncrementByAmount = 0.1f;
                turningSpeedModifier.NumberOfDecimalPlaces = 1;
                turningSpeedModifier.HelpID = "SpeedMultiplier";
                gridElements.Add(turningSpeedModifier);
            }
            else if (setupType == ControlSetup.AddToGridEditObject && controlType == Control.TurningSpeedModifier)
            {
                turningSpeedModifier.OnChange = delegate(float modifier) { actor.TurningSpeedModifier = modifier; InGame.IsLevelDirty = true; };
                grid.Add(turningSpeedModifier, 0, (int)Control.TurningSpeedModifier);
            }
            else if (setupType == ControlSetup.AddToGridReflexData && controlType == Control.TurningSpeedModifier)
            {
                turningSpeedModifier.OnChange = delegate(float modifier) { reflexData.TurnSpeedTileModifier = modifier; InGame.IsLevelDirty = true; };
                turningSpeedModifier.CurrentValueImmediate = reflexData.TurnSpeedTileModifier;
                grid.Add(turningSpeedModifier, 0, (int)Control.TurningSpeedModifier);
            }
            #endregion turning speed modifier

            #region linear acceleration modifier
            if (setupType == ControlSetup.Initialize)
            {
                linearAccelerationModifier = new UIGridModularFloatSliderElement(blob, Strings.Localize("editObjectParams.linearAccelerationMultiplier"));
                linearAccelerationModifier.MinValue = 0.1f;
                linearAccelerationModifier.MaxValue = 10.0f;
                linearAccelerationModifier.IncrementByAmount = 0.1f;
                linearAccelerationModifier.NumberOfDecimalPlaces = 1;
                linearAccelerationModifier.HelpID = "AccelerationMultiplier";
                gridElements.Add(linearAccelerationModifier);
            }
            else if (setupType == ControlSetup.AddToGridEditObject && controlType == Control.LinearAccelerationModifier)
            {
                linearAccelerationModifier.OnChange = delegate(float modifier) { actor.LinearAccelerationModifier = modifier; InGame.IsLevelDirty = true; };
                grid.Add(linearAccelerationModifier, 0, (int)Control.LinearAccelerationModifier);
            }
            else if (setupType == ControlSetup.AddToGridReflexData && controlType == Control.LinearAccelerationModifier)
            {
                linearAccelerationModifier.OnChange = delegate(float modifier) { actor.LinearAccelerationModifier = modifier; };
                grid.Add(linearAccelerationModifier, 0, (int)Control.LinearAccelerationModifier);
            }
            #endregion linear Acceleration modifier

            #region turning acceleration modifier
            if (setupType == ControlSetup.Initialize)
            {
                turningAccelerationModifier = new UIGridModularFloatSliderElement(blob, Strings.Localize("editObjectParams.turningAccelerationMultiplier"));
                turningAccelerationModifier.MinValue = 0.1f;
                turningAccelerationModifier.MaxValue = 5.0f;
                turningAccelerationModifier.IncrementByAmount = 0.1f;
                turningAccelerationModifier.NumberOfDecimalPlaces = 1;
                turningAccelerationModifier.HelpID = "AccelerationMultiplier";
                gridElements.Add(turningAccelerationModifier);
            }
            else if (setupType == ControlSetup.AddToGridEditObject && controlType == Control.TurningAccelerationModifier)
            {
                turningAccelerationModifier.OnChange = delegate(float modifier) { actor.TurningAccelerationModifier = modifier; InGame.IsLevelDirty = true; };
                grid.Add(turningAccelerationModifier, 0, (int)Control.TurningAccelerationModifier);
            }
            else if (setupType == ControlSetup.AddToGridReflexData && controlType == Control.TurningAccelerationModifier)
            {
                turningAccelerationModifier.OnChange = delegate(float modifier) { actor.TurningAccelerationModifier = modifier; };
                grid.Add(turningAccelerationModifier, 0, (int)Control.TurningAccelerationModifier);
            }
            #endregion turning Acceleration modifier

            #region vertical speed modifier
            if (setupType == ControlSetup.Initialize)
            {
                verticalSpeedModifier = new UIGridModularFloatSliderElement(blob, Strings.Localize("editObjectParams.verticalSpeedMultiplier"));
                verticalSpeedModifier.MinValue = 0.1f;
                verticalSpeedModifier.MaxValue = 5.0f;
                verticalSpeedModifier.IncrementByAmount = 0.1f;
                verticalSpeedModifier.NumberOfDecimalPlaces = 1;
                verticalSpeedModifier.HelpID = "SpeedMultiplier";
                gridElements.Add(verticalSpeedModifier);
            }
            else if (setupType == ControlSetup.AddToGridEditObject && controlType == Control.VerticalSpeedModifier)
            {
                verticalSpeedModifier.OnChange = delegate(float modifier) { actor.VerticalSpeedModifier = modifier; InGame.IsLevelDirty = true; };
                grid.Add(verticalSpeedModifier, 0, (int)Control.VerticalSpeedModifier);
            }
            else if (setupType == ControlSetup.AddToGridReflexData && controlType == Control.VerticalSpeedModifier)
            {
                verticalSpeedModifier.OnChange = delegate(float modifier) { actor.VerticalSpeedModifier = modifier; };
                grid.Add(verticalSpeedModifier, 0, (int)Control.VerticalSpeedModifier);
            }
            #endregion vertical speed modifier

            #region vertical acceleration modifier
            if (setupType == ControlSetup.Initialize)
            {
                verticalAccelerationModifier = new UIGridModularFloatSliderElement(blob, Strings.Localize("editObjectParams.verticalAccelerationMultiplier"));
                verticalAccelerationModifier.MinValue = 0.1f;
                verticalAccelerationModifier.MaxValue = 10.0f;
                verticalAccelerationModifier.IncrementByAmount = 0.1f;
                verticalAccelerationModifier.NumberOfDecimalPlaces = 1;
                verticalAccelerationModifier.HelpID = "AccelerationMultiplier";
                gridElements.Add(verticalAccelerationModifier);
            }
            else if (setupType == ControlSetup.AddToGridEditObject && controlType == Control.VerticalAccelerationModifier)
            {
                verticalAccelerationModifier.OnChange = delegate(float modifier) { actor.VerticalAccelerationModifier = modifier; InGame.IsLevelDirty = true; };
                grid.Add(verticalAccelerationModifier, 0, (int)Control.VerticalAccelerationModifier);
            }
            else if (setupType == ControlSetup.AddToGridReflexData && controlType == Control.VerticalAccelerationModifier)
            {
                verticalAccelerationModifier.OnChange = delegate(float modifier) { actor.VerticalAccelerationModifier = modifier; };
                grid.Add(verticalAccelerationModifier, 0, (int)Control.VerticalAccelerationModifier);
            }
            #endregion vertical acceleration modifier

            #region glow strength
            if (setupType == ControlSetup.Initialize)
            {
                glowAmt = new UIGridModularFloatSliderElement(blob, Strings.Localize("editObjectParams.glowAmt"));
                glowAmt.MinValue = 0.0f;
                glowAmt.MaxValue = 10.0f;
                glowAmt.IncrementByAmount = 0.1f;
                glowAmt.NumberOfDecimalPlaces = 1;
                glowAmt.HelpID = "GlowLightStrength";
                gridElements.Add(glowAmt);
            }
            else if (setupType == ControlSetup.AddToGridEditObject && controlType == Control.GlowAmt)
            {
                glowAmt.OnChange = delegate(float amt) { actor.GlowAmt = amt / 10.0f; InGame.IsLevelDirty = true; };
                grid.Add(glowAmt, 0, (int)Control.GlowAmt);
            }
            #endregion

            #region glow emission
            if (setupType == ControlSetup.Initialize)
            {
                glowEmission = new UIGridModularFloatSliderElement(blob, Strings.Localize("editObjectParams.glowEmission"));
                glowEmission.MinValue = 0.0f;
                glowEmission.MaxValue = 10.0f;
                glowEmission.IncrementByAmount = 0.1f;
                glowEmission.NumberOfDecimalPlaces = 1;
                glowEmission.HelpID = "GlowEmission";
                gridElements.Add(glowEmission);
            }
            else if (setupType == ControlSetup.AddToGridEditObject && controlType == Control.GlowEmission)
            {
                glowEmission.OnChange = delegate(float amt) { actor.GlowEmission = amt / 10.0f; InGame.IsLevelDirty = true; };
                grid.Add(glowEmission, 0, (int)Control.GlowEmission);
            }
            #endregion

            /*
            #region show sensors
            renderSensors = new UIGridModularCheckboxElement(blob, Strings.Localize("editObjectParams.showSensors"));
            renderSensors.OnCheck = delegate() { actor.ShowSensors = true; InGame.IsLevelDirty = true; };
            renderSensors.OnClear = delegate() { actor.ShowSensors = false; InGame.IsLevelDirty = true; };
            renderSensors.HelpID = "ShowSensors";
            // Add to grid.
            grid.Add(renderSensors, 0, (int)Control.RenderSensors);
            #endregion
            */

            #region debug displays
            if (setupType == ControlSetup.Initialize)
            {
                // Restore default.
                blob.height = blob.width / 5.0f;
                displayLOS = new UIGridModularCheckboxElement(blob, Strings.Localize("editObjectParams.displayLOS"));
                displayLOS.HelpID = "Debug:DisplayLineOfSight";
                gridElements.Add(displayLOS);
            }
            else if (setupType == ControlSetup.AddToGridEditObject && controlType == Control.DisplayLOS)
            {
                displayLOS.OnCheck = delegate() { actor.DisplayLOS = true; InGame.IsLevelDirty = true; };
                displayLOS.OnClear = delegate() { actor.DisplayLOS = false; InGame.IsLevelDirty = true; };
                grid.Add(displayLOS, 0, (int)Control.DisplayLOS);
            }

            if (setupType == ControlSetup.Initialize)
            {
                displayLOP = new UIGridModularCheckboxElement(blob, Strings.Localize("editObjectParams.displayLOP"));
                displayLOP.HelpID = "Debug:DrawLinesShowingWhatISeeAndHear";
                gridElements.Add(displayLOP);
            }
            else if (setupType == ControlSetup.AddToGridEditObject && controlType == Control.DisplayLOP)
            {
                displayLOP.OnCheck = delegate() { actor.DisplayLOP = true; InGame.IsLevelDirty = true; };
                displayLOP.OnClear = delegate() { actor.DisplayLOP = false; InGame.IsLevelDirty = true; };
                grid.Add(displayLOP, 0, (int)Control.DisplayLOP);
            }

            if (setupType == ControlSetup.Initialize)
            {
                displayCurrentPage = new UIGridModularCheckboxElement(blob, Strings.Localize("editObjectParams.displayCurrentPage"));
                displayCurrentPage.HelpID = "Debug:DrawCurrentPage";
                gridElements.Add(displayCurrentPage);
            }
            else if (setupType == ControlSetup.AddToGridEditObject && controlType == Control.DisplayCurrentPage)
            {
                displayCurrentPage.OnCheck = delegate() { actor.DisplayCurrentPage = true; InGame.IsLevelDirty = true; };
                displayCurrentPage.OnClear = delegate() { actor.DisplayCurrentPage = false; InGame.IsLevelDirty = true; };
                grid.Add(displayCurrentPage, 0, (int)Control.DisplayCurrentPage);
            }

            #endregion debug displays

            #region distance filters
            if (setupType == ControlSetup.Initialize)
            {
                nearByDistance = new UIGridModularFloatSliderElement(blob, Strings.Localize("editObjectParams.nearByDistance"));
                nearByDistance.MinValue = 0;
                nearByDistance.MaxValue = 100;
                nearByDistance.IncrementByAmount = 0.5f;
                nearByDistance.HelpID = "NearByDistance";
                gridElements.Add(nearByDistance);
            }
            else if (setupType == ControlSetup.AddToGridEditObject && controlType == Control.NearByDistance)
            {
                nearByDistance.OnChange = delegate(float num) { actor.NearByDistance = num; InGame.IsLevelDirty = true; };
                grid.Add(nearByDistance, 0, (int)Control.NearByDistance);
            }
            else if (setupType == ControlSetup.AddToGridReflexData && controlType == Control.NearByDistance)
            {
                nearByDistance.OnChange = delegate(float dist) { reflexData.ParamFloat = dist; InGame.IsLevelDirty = true; };
                nearByDistance.CurrentValue = reflexData.ParamFloat;
                grid.Add(nearByDistance, 0, (int)Control.NearByDistance);
            }

            if (setupType == ControlSetup.Initialize)
            {
                farAwayDistance = new UIGridModularFloatSliderElement(blob, Strings.Localize("editObjectParams.farAwayDistance"));
                farAwayDistance.MinValue = 0;
                farAwayDistance.MaxValue = 100;
                farAwayDistance.IncrementByAmount = 0.5f;
                farAwayDistance.HelpID = "FarAwayDistance";
                gridElements.Add(farAwayDistance);
            }
            else if (setupType == ControlSetup.AddToGridEditObject && controlType == Control.FarAwayDistance)
            {
                farAwayDistance.OnChange = delegate(float num) { actor.FarAwayDistance = num; InGame.IsLevelDirty = true; };
                grid.Add(farAwayDistance, 0, (int)Control.FarAwayDistance);
            }
            else if (setupType == ControlSetup.AddToGridReflexData && controlType == Control.FarAwayDistance)
            {
                farAwayDistance.OnChange = delegate(float dist) { reflexData.ParamFloat = dist; InGame.IsLevelDirty = true; };
                farAwayDistance.CurrentValue = reflexData.ParamFloat;
                grid.Add(farAwayDistance, 0, (int)Control.FarAwayDistance);
            }
            #endregion distance filters

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
        }

        // Should be called after all UI elements are setup on the grid
        private void SetupGridHelp()
        {
            // Loop over all the elements in the grid.  For any that have 
            // help, set the flag so they display Y button for help.
            for (int i = 0; i < grid.ActualDimensions.Y; i++)
            {
                UIGridElement e = grid.Get(0, i);
                if (e != null)
                {
                    string helpID = e.HelpID;
                    string helpText = TweakScreenHelp.GetHelp(helpID);
                    if (helpText != null)
                    {
                        e.ShowHelpButton = true;
                    }
                }                
            }
        }

        private void ClearGrid()
        {
            grid.ClearNoUnload();
        }

        private void SetupControlsForEditObject()
        {
            ClearGrid();

            SetupControl(ControlSetup.AddToGridEditObject, Control.PushRange);
            SetupControl(ControlSetup.AddToGridEditObject, Control.PushStrength);

            SetupControl(ControlSetup.AddToGridEditObject, Control.LightRange);
            SetupControl(ControlSetup.AddToGridEditObject, Control.MovementSpeedModifier);
            SetupControl(ControlSetup.AddToGridEditObject, Control.TurningSpeedModifier);
            SetupControl(ControlSetup.AddToGridEditObject, Control.LinearAccelerationModifier);
            SetupControl(ControlSetup.AddToGridEditObject, Control.TurningAccelerationModifier);
            SetupControl(ControlSetup.AddToGridEditObject, Control.VerticalSpeedModifier);
            SetupControl(ControlSetup.AddToGridEditObject, Control.VerticalAccelerationModifier);
            SetupControl(ControlSetup.AddToGridEditObject, Control.Immobile);
            SetupControl(ControlSetup.AddToGridEditObject, Control.Invulnerable);
            SetupControl(ControlSetup.AddToGridEditObject, Control.ShowHitPoints);
            SetupControl(ControlSetup.AddToGridEditObject, Control.MaxHitPoints);
            SetupControl(ControlSetup.AddToGridEditObject, Control.Creatable);
            SetupControl(ControlSetup.AddToGridEditObject, Control.MaxCreated);
            SetupControl(ControlSetup.AddToGridEditObject, Control.StayAboveWater);
            SetupControl(ControlSetup.AddToGridEditObject, Control.ReScale);
            SetupControl(ControlSetup.AddToGridEditObject, Control.HoldDistance);
            SetupControl(ControlSetup.AddToGridEditObject, Control.Bounciness);
            SetupControl(ControlSetup.AddToGridEditObject, Control.Friction);

            SetupControl(ControlSetup.AddToGridEditObject, Control.BlipDamage);
            SetupControl(ControlSetup.AddToGridEditObject, Control.BlipReloadTime);
            SetupControl(ControlSetup.AddToGridEditObject, Control.BlipRange);
            SetupControl(ControlSetup.AddToGridEditObject, Control.BlipSpeed);
            SetupControl(ControlSetup.AddToGridEditObject, Control.BlipsInAir);

            SetupControl(ControlSetup.AddToGridEditObject, Control.MissileDamage);
            SetupControl(ControlSetup.AddToGridEditObject, Control.MissileReloadTime);
            SetupControl(ControlSetup.AddToGridEditObject, Control.MissileRange);
            SetupControl(ControlSetup.AddToGridEditObject, Control.MissileSpeed);
            SetupControl(ControlSetup.AddToGridEditObject, Control.MissilesInAir);
            SetupControl(ControlSetup.AddToGridEditObject, Control.MissileTrails);

            SetupControl(ControlSetup.AddToGridEditObject, Control.ShieldEffects);
            SetupControl(ControlSetup.AddToGridEditObject, Control.Invisible);
            SetupControl(ControlSetup.AddToGridEditObject, Control.Ignored);
            SetupControl(ControlSetup.AddToGridEditObject, Control.Camouflaged);

            SetupControl(ControlSetup.AddToGridEditObject, Control.Mute);
            SetupControl(ControlSetup.AddToGridEditObject, Control.Hearing);
            SetupControl(ControlSetup.AddToGridEditObject, Control.NearByDistance);
            SetupControl(ControlSetup.AddToGridEditObject, Control.FarAwayDistance);
            SetupControl(ControlSetup.AddToGridEditObject, Control.KickStrength);
            SetupControl(ControlSetup.AddToGridEditObject, Control.KickRate);
            SetupControl(ControlSetup.AddToGridEditObject, Control.GlowAmt);
            SetupControl(ControlSetup.AddToGridEditObject, Control.GlowLights);
            SetupControl(ControlSetup.AddToGridEditObject, Control.GlowEmission);
            SetupControl(ControlSetup.AddToGridEditObject, Control.DisplayLOS);
            SetupControl(ControlSetup.AddToGridEditObject, Control.DisplayLOP);
            SetupControl(ControlSetup.AddToGridEditObject, Control.DisplayCurrentPage);

            SetupGridHelp();
        }

        private const float kMinLightRange = 5.0f;
        private const float kMaxLightRange = 300.0f;
        private float LightRangeToRadius(float range)
        {
            range *= 0.01f;
            range *= range;
            return kMinLightRange + range * (kMaxLightRange - kMinLightRange);
        }
        private float LightRadiusToRange(float radius)
        {
            radius -= kMinLightRange;
            radius /= (kMaxLightRange - kMinLightRange);
            radius = (float)Math.Sqrt(radius);
            radius *= 100.0f;
            return radius;
        }
        /// <summary>
        /// Look for the next non-disabled element, and make that the current selection.
        /// 
        /// </summary>
        /// <param name="hiding"></param>
        private void CheckSelection()
        {
            if (!grid.SelectionElement.Visible)
            {
                int x = grid.SelectionIndex.X;
                for (int y = grid.SelectionIndex.Y + 1; y != grid.SelectionIndex.Y; ++y)
                {
                    if (y >= grid.ActualDimensions.Y)
                        y = y % grid.ActualDimensions.Y;

                    UIGridElement newSel = grid.Get(x, y);
                    if ((newSel != null) && newSel.Visible)
                    {
                        grid.SelectionIndex = new Point(x, y);
                        break;
                    }
                }
            }
        }
        private void SetupForFixed(GameActor actor)
        {
            bool isFixed = (actor.Chassis == null) || actor.Chassis.FixedPosition;
            bool isDynamic = actor.Chassis is DynamicPropChassis;
            bool isSpin = actor.Chassis is SitAndSpinChassis || (actor.Chassis is DynamicPropChassis && actor.Version >= 1);
            bool noSpin = actor.Chassis is PuckChassis || actor.Chassis is SaucerChassis;
            bool vertical = actor.Chassis is FloatInAirChassis || actor.Chassis is SaucerChassis || actor.Chassis is SwimChassis || actor.Chassis is HoverSwimChassis;
            bool isAlwaysImmobile = actor.Chassis is PipeChassis;

            immobile.Visible = !isAlwaysImmobile;
            movementSpeedModifier.Visible = !isFixed && !isDynamic;
            turningSpeedModifier.Visible = (!isFixed && !isDynamic && !noSpin) || isSpin;
            linearAccelerationModifier.Visible = !isFixed && !isDynamic;
            turningAccelerationModifier.Visible = (!isFixed && !isDynamic && !noSpin) || isSpin;
            verticalSpeedModifier.Visible = vertical;
            verticalAccelerationModifier.Visible = vertical;
        }

        /// <summary>
        /// Only enable push settings for fan.
        /// </summary>
        /// <param name="actor"></param>
        private void SetupForPush(GameActor actor)
        {
            if (actor is Fan)
            {
                pushrange.Visible = true;
                pushstrength.Visible = true;
            }
            else
            {
                pushrange.Visible = false;
                pushstrength.Visible = false;
            }
        }

        private void SetupForLight(GameActor actor)
        {
            if (actor is Light)
            {
                lightrange.Visible = true;
                lightrange.CurrentValue = LightRadiusToRange(actor.LightRange);

                glowLights.Visible = false;
                glowAmt.Visible = false;
                glowEmission.Visible = false;
            }
            else
            {
                lightrange.Visible = false;
                glowLights.Visible = true;
                glowAmt.Visible = true;
                glowEmission.Visible = true;
            }
        }

        private void SetupForWater(GameActor actor)
        {
            if ((actor.Chassis is BoatChassis)
                || (actor.Chassis is CursorChassis)
                || (actor.Chassis is CycleChassis)
                || (actor.Chassis is DynamicPropChassis)
                || (actor.Chassis is StaticPropChassis)
                || (actor.Chassis is SwimChassis)
                || (actor.Chassis is RoverChassis))
            {
                stayAboveWater.Visible = false;
            }
            else
            {
                stayAboveWater.Visible = true;
            }
        }

        private void SetupForCreatable(GameActor actor)
        {
            // If the actor is a clone, do not show the creatable checkbox.
            if (actor.IsClone && creatable.Visible)
            {
                creatable.Visible = false;
                // This does not change while tweak screen is visible, so
                // no need to check selection or make the grid dirty.
            }
            else if (!actor.IsClone && !creatable.Visible)
            {
                creatable.Visible = true;
                // This does not change while tweak screen is visible, so
                // no need to check selection or make the grid dirty.
            }

            // If the actor is a creatable or a clone, show the max created checkbox.
            if ((actor.Creatable || actor.IsClone) && !maxCreated.Visible)
            {
                maxCreated.Visible = true;
                grid.Dirty = true;
                CheckSelection();
            }
            else if (!(actor.Creatable || actor.IsClone) && maxCreated.Visible)
            {
                maxCreated.Visible = false;
                grid.Dirty = true;
                CheckSelection();
            }
        }

        private void SetupForTumbles(GameActor actor)
        {
            DynamicPropChassis chassis = actor.Chassis as DynamicPropChassis;
            if (chassis != null && chassis.Tumbles && actor.Friction == 0.0f)
            {
                friction.Visible = false;
            }
            else
            {
                friction.Visible = true;
            }
        }   // end of SetupForTumbles()

        public void Activate(ReflexData _data, GameActor _actor, Control editType)
        {
            if (!active)
            {
                editMode = EditMode.ProgrammingTileMode;

//                HelpOverlay.Push("EditObjectParameters");

                actor = _actor;
                reflexData = _data;

                // Do stack handling here.  If we do it in the update object we have no
                // clue which order things get pushed and popped and madness ensues.
                CommandStack.Push(commandMap);
                active = true;
                grid.Active = true;

                //
                grid.RenderWhenInactive = false;

                // Tell InGame we're using the thumbnail so no need to do full render.
                InGame.inGame.RenderWorldAsThumbnail = true;

                /// We are going to read in the object's settings and set our own to mirror them.
                /// This won't dirty the level, but setting our settings will think it is dirtying
                /// the level. So we let the dirty flag get set, then just reset it after we've
                /// initialized (end of this activate function).
                bool wasDirty = InGame.IsLevelDirty;

                ClearGrid();

                // We are only supporting few edit types here.
                // Or that used to be the case.  This list is growing longer and longer.
                // Need to figure out a decent way to refactor this.
                switch (editType)
                {
                    case Control.ReScale:
                    {
                        SetupControl( ControlSetup.AddToGridReflexData, Control.ReScale );
                        rescale.CurrentValue = (reflexData.ReScaleEnabled) ? reflexData.ReScale : actor.ReScale;
                    }
                    break;

                    case Control.HoldDistance:
                    {
                        SetupControl(ControlSetup.AddToGridReflexData, Control.HoldDistance);
                        holdDistanceMultiplier.CurrentValue = reflexData.HoldDistance;
                    }
                    break;

                    case Control.MaxHitPoints:
                    {
                        SetupControl(ControlSetup.AddToGridReflexData, Control.MaxHitPoints);
                        maxHitPoints.CurrentValue = reflexData.MaxHitpoints;
                    }
                    break;

                    case Control.MovementSpeedModifier:
                    {
                        SetupControl(ControlSetup.AddToGridReflexData, Control.MovementSpeedModifier);
                        movementSpeedModifier.CurrentValue = reflexData.MoveSpeedTileModifier;
                    }
                    break;

                    case Control.TurningSpeedModifier:
                    {
                        SetupControl(ControlSetup.AddToGridReflexData, Control.TurningSpeedModifier);
                        turningSpeedModifier.CurrentValue = reflexData.TurnSpeedTileModifier;
                    }
                    break;

                    case Control.BlipDamage:
                    {
                        SetupControl(ControlSetup.AddToGridReflexData, Control.BlipDamage);
                        blipDamage.CurrentValue = reflexData.ParamInt;
                    }
                    break;

                    case Control.MissileDamage:
                    {
                        SetupControl(ControlSetup.AddToGridReflexData, Control.MissileDamage);
                        missileDamage.CurrentValue = reflexData.ParamInt;
                    }
                    break;

                    case Control.BlipReloadTime:
                    {
                        SetupControl(ControlSetup.AddToGridReflexData, Control.BlipReloadTime);
                        blipReloadTime.CurrentValue = reflexData.ParamFloat;
                    }
                    break;

                    case Control.BlipRange:
                    {
                        SetupControl(ControlSetup.AddToGridReflexData, Control.BlipRange);
                        blipRange.CurrentValue = reflexData.ParamFloat;
                    }
                    break;

                    case Control.MissileReloadTime:
                    {
                        SetupControl(ControlSetup.AddToGridReflexData, Control.MissileReloadTime);
                        missileReloadTime.CurrentValue = reflexData.ParamFloat;
                    }
                    break;

                    case Control.MissileRange:
                    {
                        SetupControl(ControlSetup.AddToGridReflexData, Control.MissileRange);
                        missileRange.CurrentValue = reflexData.ParamFloat;
                    }
                    break;

                    case Control.NearByDistance:
                    {
                        SetupControl(ControlSetup.AddToGridReflexData, Control.NearByDistance);
                        nearByDistance.CurrentValue = reflexData.ParamFloat;
                    }
                    break;

                    case Control.FarAwayDistance:
                    {
                        SetupControl(ControlSetup.AddToGridReflexData, Control.FarAwayDistance);
                        farAwayDistance.CurrentValue = reflexData.ParamFloat;
                    }
                    break;

                    case Control.Hearing:
                    {
                        SetupControl(ControlSetup.AddToGridReflexData, Control.Hearing);
                        hearing.CurrentValue = reflexData.ParamFloat * 100.0f;
                    }
                    break;

                }

                SetupGridHelp();

                // Get rid of empty elements
                grid.RemoveAllEmptyAndCollapse();

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

//                HelpOverlay.Push("EditObjectParameters");

                // Do stack handling here.  If we do it in the update object we have no
                // clue which order things get pushed and popped and madness ensues.
                CommandStack.Push(commandMap);
                active = true;
                grid.Active = true;
                grid.RenderWhenInactive = false;

                // Add all controls to grid for now
                SetupControlsForEditObject();

                /// We are going to read in the object's settings and set our own to mirror them.
                /// This won't dirty the level, but setting our settings will think it is dirtying
                /// the level. So we let the dirty flag get set, then just reset it after we've
                /// initialized (end of this activate function).
                bool wasDirty = InGame.IsLevelDirty;

                // Set initial values.
                pushrange.CurrentValue = actor.PushRange;
                pushstrength.CurrentValue = actor.PushStrength;
                immobile.Check = actor.TweakImmobile;
                invulnerable.Check = actor.TweakInvulnerable;
                creatable.Check = actor.Creatable;
                maxCreated.CurrentValue = actor.MaxCreated;
                mute.Check = actor.Mute;
                hearing.CurrentValue = actor.Hearing * 100.0f;
                //renderSensors.Check = actor.ShowSensors;
                showHitPoints.Check = actor.ShowHitPoints;
                maxHitPoints.CurrentValue = actor.MaxHitPoints;
                
                blipDamage.CurrentValue = actor.BlipDamage;
                blipReloadTime.CurrentValue = actor.BlipReloadTime;
                blipSpeed.CurrentValue = actor.BlipSpeed;
                blipRange.CurrentValue = actor.BlipRange;
                blipsInAir.CurrentValue = actor.BlipsInAir;

                missileDamage.CurrentValue = actor.MissileDamage;
                missileReloadTime.CurrentValue = actor.MissileReloadTime;
                missileSpeed.CurrentValue = actor.MissileSpeed;
                missileRange.CurrentValue = actor.MissileRange;
                missilesInAir.CurrentValue = actor.MissilesInAir;
                missileTrails.Check = actor.MissileTrails;

                shieldEffects.Check = actor.ShieldEffects;
                invisible.Check = actor.Invisible;
                ignored.Check = actor.Ignored;
                camouflaged.Check = actor.Camouflaged;

                bounciness.CurrentValue = actor.CoefficientOfRestitution;
                friction.CurrentValue = actor.Friction;
                stayAboveWater.Check = actor.StayAboveWater;
                glowAmt.CurrentValue = actor.GlowAmt * 10;
                glowEmission.CurrentValue = actor.GlowEmission * 10;
                glowLights.CurrentValue = actor.GlowLights * 10;
                displayLOS.Check = actor.DisplayLOS;
                displayLOP.Check = actor.DisplayLOP;
                displayCurrentPage.Check = actor.DisplayCurrentPage;

                nearByDistance.CurrentValue = actor.NearByDistance;
                farAwayDistance.CurrentValue = actor.FarAwayDistance;

                kickStrength.CurrentValue = actor.KickStrength;
                kickRate.CurrentValue = actor.KickRate;
                rescale.CurrentValue = actor.ReScale;
                holdDistanceMultiplier.CurrentValue = actor.HoldDistance;
                movementSpeedModifier.CurrentValue = actor.MovementSpeedModifier;
                turningSpeedModifier.CurrentValue = actor.TurningSpeedModifier;
                linearAccelerationModifier.CurrentValue = actor.LinearAccelerationModifier;
                turningAccelerationModifier.CurrentValue = actor.TurningAccelerationModifier;
                verticalSpeedModifier.CurrentValue = actor.VerticalSpeedModifier;
                verticalAccelerationModifier.CurrentValue = actor.VerticalAccelerationModifier;

                // For fixed objects, disable the things that don't make sense.
                // Enable them for mobile objects.
                // TODO Just hiding things causes the spacing to get out of whack.
                // We need to either fix that or remove "visible" and just add/remove
                // entries as our needs change.
                SetupForFixed(actor);

                // Only enable PushRange and PushStrength for fans.
                SetupForPush(actor);

                SetupForLight(actor);

                SetupForWater(actor);

                SetupForCreatable(actor);

                SetupForTumbles(actor);

                grid.Dirty = true;

                CheckSelection();
                
                InGame.inGame.RenderWorldAsThumbnail = true;
                InGame.IsLevelDirty = wasDirty;

                Foley.PlayMenuLoop();
            }
        }   // end of Activate()

        public void Deactivate()
        {
            if (active)
            {
                grid.Active = false;

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

                if (editMode == EditMode.ChangeSettingMode)
                {
                    // Return to the previous update mode.  This should be either the editObject or tweakObject modes.
                    InGame.inGame.CurrentUpdateMode = InGame.inGame.PreviousUpdateMode;
                }

                ToolTipManager.Clear();

                Foley.StopMenuLoop();

//                HelpOverlay.Pop();
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

        }   // end of EditObjectParameters LoadContent()

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
        }   // end of EditObjectParameters UnloadContent()

        /// <summary>
        /// Recreate render targets
        /// </summary>
        /// <param name="graphics"></param>
        public void DeviceReset(GraphicsDevice device)
        {
            BokuGame.DeviceReset(grid, device);
        }

        #endregion

    }   // end of class EditObjectParameters

}


