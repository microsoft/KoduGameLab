// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Net;

#if NETFX_CORE
using Windows.System;
#endif

using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Audio;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using Microsoft.Xna.Framework.Storage;


using Boku.Base;
using Boku.Common;
using Boku.Common.Xml;
using Boku.Fx;
using Boku.UI;
using Boku.UI2D;
using Boku.Input;
using Boku.SimWorld;

using BokuShared;
using Boku.Common.Gesture;
using Boku.Common.Localization;
using Boku.Web;

namespace Boku
{
    public class OptionsMenu : INeedsDeviceReset
    {
        #region Members

        private bool active = false;

        private UIGrid grid = null;
        private Matrix worldGrid = Matrix.Identity;

        private UIGridModularHelpSquare helpSquare = null;

        private Camera camera = new PerspectiveUICamera();
        private CommandMap commandMap = new CommandMap("OptionsMenu");  // Placeholder for stack.

        private UIGridModularCheckboxElement showToolTips = null;
        private UIGridModularCheckboxElement showHints = null;
        private UIGridModularCheckboxElement restoreDisabledHints = null;
        private UIGridModularCheckboxElement showFramerate = null;
        // private UIGridModularRadioBoxElement helpLevel = null;
        private UIGridModularRadioBoxElement language = null;
        private UIGridModularCheckboxElement invertYAxis = null;
        private UIGridModularCheckboxElement invertXAxis = null;
        private UIGridModularCheckboxElement invertCamY = null;
        private UIGridModularCheckboxElement invertCamX = null;
        private UIGridModularCheckboxElement modalToolMenu = null;
        private UIGridModularFloatSliderElement terrainSpeed;
        private UIGridModularFloatSliderElement uiVolume;
        private UIGridModularFloatSliderElement foleyVolume;
        private UIGridModularFloatSliderElement musicVolume;
        private UIGridModularCheckboxElement checkForUpdates;
        private UIGridModularCheckboxElement sendInstrumentation;
        private UIGridModularButtonElement showCodeOfConduct;
        private UIGridModularButtonElement showPrivacyStatement;
        private UIGridModularButtonElement showEULA;
        private UIGridModularCheckboxElement showIntroVideo = null;
        private UIGridModularCheckboxElement showTutorialDebug = null;
        private UIGridModularButtonElement showVersion;

        private ModularMessageDialog changeLanguageMessage = null;
        private string prevLanguage = "";

        #endregion

        #region Accessors
        
        public bool Active
        {
            get { return active; }
        }

        #endregion

        #region Public

        // c'tor
        public OptionsMenu()
        {
            InitGrid();

            helpSquare = new UIGridModularHelpSquare();
            helpSquare.Size = 0.95f;
            helpSquare.Position = new Vector2(3.5f, 0.0f);

            // Since we're rendering to a 1280*720 rendertarget.
            camera.Resolution = new Point(1280, 720);

            // changeLanguageMessage -- tell the user they need to restart Kodu if they change the language.
            {
                ModularMessageDialog.ButtonHandler handlerA = delegate(ModularMessageDialog dialog)
                {
                    // User chose "No, continue"
                    // User has decided not to restart at this time so clear this.
                    prevLanguage = XmlOptionsData.Language;

                    // Deactivate dialog.
                    dialog.Deactivate();
                };
                ModularMessageDialog.ButtonHandler handlerB = delegate(ModularMessageDialog dialog)
                {
                    // User chose "Yes, quit Kodu"

                    // Deactivate dialog.
                    dialog.Deactivate();

#if NETFX_CORE
                    Windows.UI.Xaml.Application.Current.Exit();
#else
                    BokuGame.bokuGame.Exit();
#endif
                };
                changeLanguageMessage = new ModularMessageDialog(
                    Strings.Localize("optionsParams.changeLanguageMessage"),
                    handlerA, Strings.Localize("optionsParams.noContinue"),
                    handlerB, Strings.Localize("optionsParams.yesQuitKodu"),
                    null, null,
                    null, null
                    );

            }


        }   // end of c'tor

        public void Update()
        {
            changeLanguageMessage.Update(camera);

            if (active && CommandStack.Peek() == grid.CommandMap)
            {

                UIGridElement prevE = grid.SelectionElement;

                HandleTouchInput();
                HandleMouseInput();
                HandleGamepadInput();

                // Update the grid.
                grid.Update(ref worldGrid);

                // If the Update deactived us, bail.
                if (!active)
                    return;

                // Update help square's positioning to line up with current selection.
                Vector3 selectionElementOffset = grid.SelectionElement.Position - grid.ScrollOffset;
                helpSquare.Position = new Vector2(helpSquare.Position.X, selectionElementOffset.Y);
 
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

                if (prevE != grid.SelectionElement)
                {
                    if (grid.SelectionElement.ShowHelpButton)
                    {
                        helpSquare.Show();
                    }
                    else
                    {
                        helpSquare.Hide();
                    }
                }
                helpSquare.Update();

                GamePadInput.ClearAllWasPressedState();
            }   // end of if active and have input focus.

            // Update the text displays.  Internally they check if they're active before doing anything.
            if (active)
            {
                InGame.inGame.shared.smallTextDisplay.Update(camera);
                InGame.inGame.shared.scrollableTextDisplay.Update(camera);
            }

        }   // end of Update()

        private void HandleTouchInput()
        {
            if (GamePadInput.ActiveMode != GamePadInput.InputMode.Touch) { return; }
            if (TouchInput.TouchCount == 0) { return; }

            bool hitMenu = false;

            TouchContact touch = TouchInput.GetOldestTouch();

            // If in focus element has help available, get it.
            UIGridElement focusElement = grid.SelectionElement;
            string helpID = focusElement.HelpID;
            string helpText = TweakScreenHelp.GetHelp(helpID);

            // Check for help tile.
            Matrix mat = Matrix.CreateTranslation(-helpSquare.Position.X, -helpSquare.Position.Y, 0);
            if (touch != null)
            {
                Vector2 hitHelpUV = Vector2.Zero;
                hitHelpUV = TouchInput.GetHitUV(touch.position, camera, ref mat, helpSquare.Size, 
                    helpSquare.Size, true);

                if (grid.SelectionElement.ShowHelpButton)
                {
                    if (hitHelpUV.X >= 0 && hitHelpUV.X < 1 && hitHelpUV.Y >= 0 && hitHelpUV.Y < 1)
                    {
                        if (TouchInput.WasTouched)
                        {
                            touch.TouchedObject = helpSquare;
                        }
                        if (TouchInput.WasReleased && touch.TouchedObject == helpSquare)
                        {
                            if (helpText != null)
                            {
                                ShowHelp(helpText);
                            }
                        }
                        hitMenu = true;
                    }
                }

                // Check if mouse hitting current selection object.  Or should this be done in the object?
                mat = Matrix.Invert(focusElement.WorldMatrix);
                Vector2 hitFocusUV = TouchInput.GetHitUV(touch.position, camera, ref mat, focusElement.Size.X, 
                    focusElement.Size.Y, true);
                bool focusElementHit = false;

                if (hitFocusUV.X >= 0 && hitFocusUV.X < 1 && hitFocusUV.Y >= 0 && hitFocusUV.Y < 1)
                {
                    if(touch.phase == TouchPhase.Began)
                    {
                        touch.TouchedObject = focusElement;
                    }
                    focusElement.HandleTouchInput(touch, hitFocusUV);
                    focusElementHit = true;
                    hitMenu = true;
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
                            e.Size.Y, true);

                        if (hitUV.X >= 0 && hitUV.X < 1 && hitUV.Y >= 0 && hitUV.Y < 1)
                        {
                            // We hit an element, so bring it into focus.
                            grid.SelectionIndex = new Point(0, i);
                            hitMenu = true;
                            break;
                        }
                    }
                }

                if ((hitFocusUV.X >= 0) && (hitFocusUV.X < 1))
                {
                    hitMenu = true;
                }
                if (!hitMenu && TouchGestureManager.Get().TapGesture.WasTapped())
                {
                    Deactivate();
                }

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
            bool hitAnything = false;

            // Check for help tile.
            Matrix mat = Matrix.CreateTranslation(-helpSquare.Position.X, -helpSquare.Position.Y, 0);
            Vector2 hitUV = MouseInput.GetHitUV(camera, ref mat, helpSquare.Size, helpSquare.Size, true);

            if (grid.SelectionElement.ShowHelpButton)
            {
                if (hitUV.X >= 0 && hitUV.X < 1 && hitUV.Y >= 0 && hitUV.Y < 1)
                {
                    if (MouseInput.Left.WasPressed)
                    {
                        MouseInput.ClickedOnObject = helpSquare;
                    }
                    if (MouseInput.Left.WasReleased && MouseInput.ClickedOnObject == helpSquare)
                    {
                        if (helpText != null)
                        {
                            ShowHelp(helpText);
                        }
                    }

                    hitAnything = true;
                }
            }

            // Check if mouse hitting current selection object.  Or should this be done in the object?
            mat = Matrix.Invert(e.WorldMatrix);
            hitUV = MouseInput.GetHitUV(camera, ref mat, e.Size.X, e.Size.Y, true);

            bool focusElementHit = false;
            if (hitUV.X >= 0 && hitUV.X < 1 && hitUV.Y >= 0 && hitUV.Y < 1)
            {
                e.HandleMouseInput(hitUV);
                focusElementHit = true;

                hitAnything = true;
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
                    hitUV = MouseInput.GetHitUV(camera, ref mat, e.Size.X, e.Size.Y, true);

                    if (hitUV.X >= 0 && hitUV.X < 1 && hitUV.Y >= 0 && hitUV.Y < 1)
                    {
                        // We hit an element, so bring it into focus.
                        grid.SelectionIndex = new Point(0, i);

                        hitAnything = true;
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
            if (MouseInput.Right.WasPressed || (!hitAnything && MouseInput.Left.WasPressed))
            {
                Deactivate();
            }
        }

        private void HandleGamepadInput()
        {
            GamePadInput pad = GamePadInput.GetGamePad0();

            UIGridElement e = grid.SelectionElement;
            UpdateInvertAxis();
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

        public void Render()
        {
            // Render menu using local camera.
            Fx.ShaderGlobals.SetCamera(camera);

            grid.Render(camera);

            helpSquare.Render(camera);

            // Moved to MainMenu so they don't end up on the RT.
            //InGame.inGame.shared.smallTextDisplay.Render();
            //InGame.inGame.shared.scrollableTextDisplay.Render();

            ToolTipManager.Render(camera);

            changeLanguageMessage.Render();

        }   // end of Render()

        public void Activate()
        {
            if (!Active)
            {
                CommandStack.Push(commandMap);
                HelpOverlay.Push("OptionsMenu");

                grid.Active = true;

                // Force the selected element to "reselect" itself so that
                // the help overlay gets updated properly.
                grid.SelectionElement.Selected = false;
                grid.SelectionElement.Selected = true;

                active = true;

                // Set initial state.
                showToolTips.Check = XmlOptionsData.ShowToolTips;
                showHints.Check = XmlOptionsData.ShowHints;
                restoreDisabledHints.Check = XmlOptionsData.DisabledHintIDs.Count == 0;
                showFramerate.Check = XmlOptionsData.ShowFramerate;
                //helpLevel.CurIndex = XmlOptionsData.HelpLevel;
                modalToolMenu.Check = XmlOptionsData.ModalToolMenu;

                terrainSpeed.CurrentValue = XmlOptionsData.TerrainSpeed;

                uiVolume.CurrentValue = XmlOptionsData.UIVolume * 100.0f;
                foleyVolume.CurrentValue = XmlOptionsData.FoleyVolume * 100.0f;
                musicVolume.CurrentValue = XmlOptionsData.MusicVolume * 100.0f;

                checkForUpdates.Check = XmlOptionsData.CheckForUpdates;
                sendInstrumentation.Check = XmlOptionsData.SendInstrumentation;
                showIntroVideo.Check = false;
                showTutorialDebug.Check = XmlOptionsData.ShowTutorialDebug;

                language.SetValueByKey(XmlOptionsData.Language);
                prevLanguage = XmlOptionsData.Language;

                // Force an update to ensure everything is ready.
                Update();
            }

        }   // end of Activate()

        public void Deactivate()
        {
            if (Active)
            {
                if (prevLanguage != XmlOptionsData.Language)
                {
                    changeLanguageMessage.Activate(useRtCoords: true);
                    grid.Active = true; // Keep grid active.
                    return;
                }

                grid.Active = false;
                HelpOverlay.Pop();      // Pop off the help overlay for the current options element.

                CommandStack.Pop(commandMap);
                HelpOverlay.Pop();      // Pop off the help for the options menu.

                // Prevent the button pressed from leaking into runtime.
                GamePadInput.IgnoreUntilReleased(Buttons.B);

                active = false;

                touched = (PlayerIndex)(-1);

                GamePadInput.ClearAllWasPressedState();

                MainMenu.Instance.LiveFeedDirty = true;

            }
        }   // end of Deactivate()

        public void OnSelect(UIGrid grid)
        {
            // Normally the grid wil deactivate itself when a selection is made.
            // In the options/settings case there are some elements that ignore 
            // the Select action letting it get to the grid which then deactivates
            // itself.  We don't want that to happen so set the grid active here.
            grid.Active = true;
        }   // end of OnSelect()

        public void OnCancel(UIGrid grid)
        {
            Deactivate();
        }   // end of OnCancel()

        #endregion

        #region Internal

        private void UpdateInvertAxis()
        {
            PlayerIndex lastTouched = GamePadInput.RealToLogical(GamePadInput.LastTouched);
            if (lastTouched != touched)
            {
                string gamerTag = GamePadInput.GetGamerTag(lastTouched);

                invertYAxis.Check = XmlOptionsData.GetInvertYAxis(gamerTag,
                    GamePadInput.InvertYAxis(lastTouched));
                invertYAxis.Label = Strings.Localize("optionsParams.invertYAxis") + gamerTag;

                invertXAxis.Check = XmlOptionsData.GetInvertXAxis(gamerTag,
                    GamePadInput.InvertXAxis(lastTouched));
                invertXAxis.Label = Strings.Localize("optionsParams.invertXAxis") + gamerTag;

                invertCamY.Check = XmlOptionsData.GetInvertCamY(gamerTag,
                    GamePadInput.InvertCamY());
                invertCamY.Label = Strings.Localize("optionsParams.invertCamY") + gamerTag;

                invertCamX.Check = XmlOptionsData.GetInvertCamX(gamerTag,
                    GamePadInput.InvertCamX());
                invertCamX.Label = Strings.Localize("optionsParams.invertCamX") + gamerTag;

                grid.Dirty = true;

                touched = lastTouched;
            }
        }
        private static PlayerIndex touched = (PlayerIndex)(-1);

        /// <summary>
        /// Comparison used when sorting languages.
        /// Note this assumes we never get a null input.
        /// </summary>
        /// <param name="a"></param>
        /// <param name="b"></param>
        /// <returns></returns>
        private int LanguageSortComp(LocalizationResourceManager.SupportedLanguage a, LocalizationResourceManager.SupportedLanguage b)
        {
            return a.NameInEnglish.CompareTo(b.NameInEnglish);
        }

        private void InitGrid()
        {
            grid = new UIGrid(OnSelect, OnCancel, new Point(1, 10), "OptionsMenuGrid");
            grid.LocalMatrix = Matrix.CreateTranslation(0.25f / 96.0f, 0.25f / 96.0f, 0.0f);
            grid.RenderEndsIn = true;
            grid.UseMouseScrollWheel = true;

            // Create a blob of common parameters.
            UIGridElement.ParamBlob blob = new UIGridElement.ParamBlob();
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


            //
            // Create elements here.
            //

            int index = 0;

            {
                showToolTips = new UIGridModularCheckboxElement(blob, Strings.Localize("optionsParams.showToolTips"));
                showToolTips.OnCheck = delegate() { XmlOptionsData.ShowToolTips = true; };
                showToolTips.OnClear = delegate() { XmlOptionsData.ShowToolTips = false; };
                showToolTips.HelpID = "ShowToolTips";
                // Add to grid.
                grid.Add(showToolTips, 0, index++);
            }

            {
                showHints = new UIGridModularCheckboxElement(blob, Strings.Localize("optionsParams.showHints"));
                showHints.OnCheck = delegate() { XmlOptionsData.ShowHints = true; };
                showHints.OnClear = delegate() { XmlOptionsData.ShowHints = false; };
                showHints.HelpID = "ShowHints";
                // Add to grid.
                grid.Add(showHints, 0, index++);
            }

            {
                restoreDisabledHints = new UIGridModularCheckboxElement(blob, Strings.Localize("optionsParams.restoreDisabledHints"));
                restoreDisabledHints.OnCheck = delegate() { XmlOptionsData.RestoreDisabledHints(); };
                restoreDisabledHints.OnClear = delegate() { restoreDisabledHints.Check = true; };
                restoreDisabledHints.HelpID = "RestoreDisabledHints";
                // Add to grid.
                grid.Add(restoreDisabledHints, 0, index++);
            }

            {
                showFramerate = new UIGridModularCheckboxElement(blob, Strings.Localize("optionsParams.showFramerate"));
                showFramerate.OnCheck = delegate() { XmlOptionsData.ShowFramerate = true; };
                showFramerate.OnClear = delegate() { XmlOptionsData.ShowFramerate = false; };
                showFramerate.HelpID = "ShowFramerate";
                // Add to grid.
                grid.Add(showFramerate, 0, index++);
            }

            /*
            {
                helpLevel = new UIGridModularRadioBoxElement(blob, Strings.Localize("optionsParams.helpLevel"));
                helpLevel.AddText(Strings.Localize("optionsParams.lowHelp"));
                helpLevel.AddText(Strings.Localize("optionsParams.midHelp"));
                helpLevel.AddText(Strings.Localize("optionsParams.highHelp"));
                helpLevel.CurIndex = XmlOptionsData.HelpLevel;
                helpLevel.OnChange = delegate(UIGridModularRadioBoxElement.ListEntry entry)
                {
                    XmlOptionsData.HelpLevel = helpLevel.CurIndex;
                };
                helpLevel.HelpID = "HelpOverlayAmount";
                // Add to grid.
                grid.Add(helpLevel, 0, index++);
            }
            */

            {
                float oldWidth = blob.width;
                blob.width += 0.5f;
                language = new UIGridModularRadioBoxElement(blob, Strings.Localize("optionsParams.language"));
                blob.width = oldWidth;
                IEnumerable<LocalizationResourceManager.SupportedLanguage> langs = LocalizationResourceManager.SupportedLanguages;

                // Copy to a List so we can sort.
                List<LocalizationResourceManager.SupportedLanguage> languageList = new List<LocalizationResourceManager.SupportedLanguage>();
                foreach (LocalizationResourceManager.SupportedLanguage lang in langs)
                {
                    languageList.Add(lang);
                }
                languageList.Sort(LanguageSortComp);

                // Add the sorted languages to the UI element.
                foreach(LocalizationResourceManager.SupportedLanguage lang in languageList)
                {
#if NETFX_CORE
                    if(lang.NameInEnglish.Equals("hebrew", StringComparison.OrdinalIgnoreCase))
#else
                    if (lang.NameInEnglish.Equals("hebrew", StringComparison.InvariantCultureIgnoreCase))
#endif
                    {
                        // RtoL code seems to have trouble with NSM characters 0x05b0 and 0x05b4.
                        // Strip them out.
                        string native = "";
                        char[] a = lang.NameInNative.ToCharArray();
                        foreach (char c in a)
                        {
                            if (c != 0x05b0 && c != 0x05b4)
                            {
                                native += c;
                            }
                        }
                        
                        language.AddText(lang.NameInEnglish + " : " + native, lang.Language);
                    }
                    else
                    {
                        language.AddText(lang.NameInEnglish + " : " + lang.NameInNative, lang.Language);
                    }
                }
                language.NumColumns = 2;
                language.SetValueByKey(XmlOptionsData.Language);

                language.OnChange = delegate(UIGridModularRadioBoxElement.ListEntry entry)
                {
                    // Note we can only get away with this since the language won't change for real until restart.
                    XmlOptionsData.Language = language.CurKey;
                };
                language.HelpID = "Language";
                // Add to grid.
                grid.Add(language, 0, index++);
            }

            {
                modalToolMenu = new UIGridModularCheckboxElement(blob, Strings.Localize("optionsParams.ModalToolMenu"));
                modalToolMenu.OnCheck = delegate() { XmlOptionsData.ModalToolMenu = true; };
                modalToolMenu.OnClear = delegate() { XmlOptionsData.ModalToolMenu = false; };
                modalToolMenu.HelpID = "ModalToolMenu";
                // Add to grid.
                grid.Add(modalToolMenu, 0, index++);
            }

            #region Stick Inverting
            {
                invertYAxis = new UIGridModularCheckboxElement(blob, Strings.Localize("optionsParams.invertYAxis"));
                invertYAxis.OnCheck = delegate()
                {
                    PlayerIndex lastTouched = GamePadInput.RealToLogical(GamePadInput.LastTouched);
                    GamePadInput.SetInvertYAxis(lastTouched, true);
                };
                invertYAxis.OnClear = delegate()
                {
                    PlayerIndex lastTouched = GamePadInput.RealToLogical(GamePadInput.LastTouched);
                    GamePadInput.SetInvertYAxis(lastTouched, false);
                };
                invertYAxis.HelpID = "InvertYAxis";
                grid.Add(invertYAxis, 0, index++);
            }

            {
                invertXAxis = new UIGridModularCheckboxElement(blob, Strings.Localize("optionsParams.invertXAxis"));
                invertXAxis.OnCheck = delegate()
                {
                    PlayerIndex lastTouched = GamePadInput.RealToLogical(GamePadInput.LastTouched);
                    GamePadInput.SetInvertXAxis(lastTouched, true);
                };
                invertXAxis.OnClear = delegate()
                {
                    PlayerIndex lastTouched = GamePadInput.RealToLogical(GamePadInput.LastTouched);
                    GamePadInput.SetInvertXAxis(lastTouched, false);
                };
                invertXAxis.HelpID = "InvertXAxis";
                grid.Add(invertXAxis, 0, index++);
            }
            {
                invertCamY = new UIGridModularCheckboxElement(blob, Strings.Localize("optionsParams.invertCamY"));
                invertCamY.OnCheck = delegate()
                {
                    PlayerIndex lastTouched = GamePadInput.RealToLogical(GamePadInput.LastTouched);
                    GamePadInput.SetInvertCamY(lastTouched, true);
                };
                invertCamY.OnClear = delegate()
                {
                    PlayerIndex lastTouched = GamePadInput.RealToLogical(GamePadInput.LastTouched);
                    GamePadInput.SetInvertCamY(lastTouched, false);
                };
                invertCamY.HelpID = "InvertCamY";
                grid.Add(invertCamY, 0, index++);
            }

            {
                invertCamX = new UIGridModularCheckboxElement(blob, Strings.Localize("optionsParams.invertCamX"));
                invertCamX.OnCheck = delegate()
                {
                    PlayerIndex lastTouched = GamePadInput.RealToLogical(GamePadInput.LastTouched);
                    GamePadInput.SetInvertCamX(lastTouched, true);
                };
                invertCamX.OnClear = delegate()
                {
                    PlayerIndex lastTouched = GamePadInput.RealToLogical(GamePadInput.LastTouched);
                    GamePadInput.SetInvertCamX(lastTouched, false);
                };
                invertCamX.HelpID = "InvertCamX";
                grid.Add(invertCamX, 0, index++);
            }
            #endregion Stick Inverting

            #region Terrain Edit Speed
            {
                // Restore default.
                blob.height = blob.width / 5.0f;
                terrainSpeed = new UIGridModularFloatSliderElement(blob, Strings.Localize("optionsParams.terrainSpeed"));
                terrainSpeed.MinValue = 0.25f;
                terrainSpeed.MaxValue = 4.0f;
                terrainSpeed.IncrementByAmount = 0.25f;
                terrainSpeed.NumberOfDecimalPlaces = 2;
                terrainSpeed.OnChange = delegate(float speed) { XmlOptionsData.TerrainSpeed = speed; };
                terrainSpeed.HelpID = "TerrainSpeed";
                grid.Add(terrainSpeed, 0, index++);
            }
            #endregion Terrain Edit Speed

            #region Audio Volumes
            {
                // Restore default.
                blob.height = blob.width / 5.0f;
                uiVolume = new UIGridModularFloatSliderElement(blob, Strings.Localize("optionsParams.uiVolume"));
                uiVolume.MinValue = 0.0f;
                uiVolume.MaxValue = 100.0f;
                uiVolume.IncrementByAmount = 5.0f;
                uiVolume.NumberOfDecimalPlaces = 0;
                uiVolume.OnChange = delegate(float volume) { XmlOptionsData.UIVolume = volume * 0.01f; };
                uiVolume.HelpID = "UIVolume";
                grid.Add(uiVolume, 0, index++);
            }
            {
                foleyVolume = new UIGridModularFloatSliderElement(blob, Strings.Localize("optionsParams.foleyVolume"));
                foleyVolume.MinValue = 0.0f;
                foleyVolume.MaxValue = 100.0f;
                foleyVolume.IncrementByAmount = 5.0f;
                foleyVolume.NumberOfDecimalPlaces = 0;
                foleyVolume.OnChange = delegate(float volume) { XmlOptionsData.FoleyVolume = volume * 0.01f; };
                foleyVolume.HelpID = "EffectsVolume";
                grid.Add(foleyVolume, 0, index++);
            }
            {
                musicVolume = new UIGridModularFloatSliderElement(blob, Strings.Localize("optionsParams.musicVolume"));
                musicVolume.MinValue = 0.0f;
                musicVolume.MaxValue = 100.0f;
                musicVolume.IncrementByAmount = 5.0f;
                musicVolume.NumberOfDecimalPlaces = 0;
                musicVolume.OnChange = delegate(float volume) { XmlOptionsData.MusicVolume = volume * 0.01f; };
                musicVolume.HelpID = "MusicVolume";
                grid.Add(musicVolume, 0, index++);
            }
            #endregion Audio Volumes

            #region Privacy Settings
            {
                checkForUpdates = new UIGridModularCheckboxElement(blob, Strings.Localize("optionsParams.checkForUpdates"));
                checkForUpdates.OnCheck = delegate() { XmlOptionsData.CheckForUpdates = true; };
                checkForUpdates.OnClear = delegate() { XmlOptionsData.CheckForUpdates = false; };
                checkForUpdates.HelpID = "CheckForUpdates";
                // Add to grid.
                grid.Add(checkForUpdates, 0, index++);
            }
            {
                sendInstrumentation = new UIGridModularCheckboxElement(blob, Strings.Localize("optionsParams.sendInstrumentation"));
                sendInstrumentation.OnCheck = delegate() { XmlOptionsData.SendInstrumentation = true; };
                sendInstrumentation.OnClear = delegate() { XmlOptionsData.SendInstrumentation = false; };
                sendInstrumentation.HelpID = "SendInstrumentation";
                // Add to grid.
                grid.Add(sendInstrumentation, 0, index++);
            }
            {
                UIGridModularButtonElement.UIButtonElementEvent onA = delegate()
                {
                    Stream stream = Storage4.OpenRead(BokuGame.Settings.MediaPath + @"Text\Kodu_Game_Lab_Code_of_Conduct.txt", StorageSource.TitleSpace);
                    StreamReader reader = new StreamReader(stream);
                    string content = reader.ReadToEnd();
                    reader.Close();
                    InGame.inGame.shared.scrollableTextDisplay.Activate(null, content, UIGridElement.Justification.Left, false, false, false);
                };

                showCodeOfConduct = new UIGridModularButtonElement(blob, Strings.Localize("optionsParams.viewCodeOfConduct"), Strings.Localize("optionsParams.viewButtonLabel"), onA, null, null);
                showCodeOfConduct.HelpID = "ShowCodeOfConduct";
                grid.Add(showCodeOfConduct, 0, index++);
            }
            {
                UIGridModularButtonElement.UIButtonElementEvent onA = delegate()
                {
#if NETFX_CORE
                    Launcher.LaunchUriAsync(new Uri(Program2.SiteOptions.KGLUrl + @"/Link/PrivacyStatement"));
#else
                    Process.Start(Program2.SiteOptions.KGLUrl + @"/Link/PrivacyStatement");
#endif
                };

                showPrivacyStatement = new UIGridModularButtonElement(blob, Strings.Localize("optionsParams.viewPrivacyStatement"), Strings.Localize("optionsParams.viewButtonLabel"), onA, null, null);
                showPrivacyStatement.HelpID = "ShowPrivacyStatement";
                grid.Add(showPrivacyStatement, 0, index++);
            }
            {
                UIGridModularButtonElement.UIButtonElementEvent onA = delegate()
                {
                    Stream stream = Storage4.OpenRead(BokuGame.Settings.MediaPath + @"Text\Kodu_Game_Lab_EULA.txt", StorageSource.TitleSpace);
                    StreamReader reader = new StreamReader(stream);
                    string content = reader.ReadToEnd();
                    reader.Close();
                    InGame.inGame.shared.scrollableTextDisplay.Activate(null, content, UIGridElement.Justification.Left, false, false, false);
                };

                showEULA = new UIGridModularButtonElement(blob, Strings.Localize("optionsParams.viewEULA"), Strings.Localize("optionsParams.viewButtonLabel"), onA, null, null);
                showEULA.HelpID = "ShowEULA";
                grid.Add(showEULA, 0, index++);
            }
            #endregion

            #region ShowIntroVideo
            {
                showIntroVideo = new UIGridModularCheckboxElement(blob, Strings.Localize("optionsParams.showIntroVideo"));
                showIntroVideo.OnCheck = delegate() { XmlOptionsData.ShowIntroVideo = true; };
                showIntroVideo.OnClear = delegate() { XmlOptionsData.ShowIntroVideo = false; };
                showIntroVideo.HelpID = "ShowIntroVideo";
                // Add to grid.
                grid.Add(showIntroVideo, 0, index++);
            }
            #endregion

            #region ShowTutorialDebug
            {
                showTutorialDebug = new UIGridModularCheckboxElement(blob, Strings.Localize("optionsParams.showTutorialDebug"));
                showTutorialDebug.OnCheck = delegate() { XmlOptionsData.ShowTutorialDebug = true; };
                showTutorialDebug.OnClear = delegate() { XmlOptionsData.ShowTutorialDebug = false; };
                showTutorialDebug.HelpID = "ShowTutorialDebug";
                // Add to grid.
                grid.Add(showTutorialDebug, 0, index++);
            }
            #endregion


            showVersion = new UIGridModularButtonElement(blob, Strings.Localize("shareHub.appName") + " (" + Program2.ThisVersion.ToString() + ", " + Program2.SiteOptions.Product + ")", null, null, null, null);
            showVersion.HelpID = "Version";
            grid.Add(showVersion, 0, index++);


            //
            // Set grid properties.
            //
            grid.Spacing = new Vector2(0.0f, 0.1f);     // The first number doesn't really matter since we're doing a 1d column.
            grid.Scrolling = true;
            grid.Wrap = false;
            grid.LocalMatrix = Matrix.Identity;

            // Loop over al the elements in the grid.  For any that have 
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

        }   // end of InitGrid

        public void LoadContent(bool immediate)
        {
            grid.LoadContent(immediate);
        }

        public void InitDeviceResources(GraphicsDevice device)
        {
            grid.InitDeviceResources(device);
        }

        public void UnloadContent()
        {
            grid.UnloadContent();
        }

        /// <summary>
        /// Recreate render targets
        /// </summary>
        /// <param name="graphics"></param>
        public void DeviceReset(GraphicsDevice device)
        {
            BokuGame.DeviceReset(grid, device);
        }

        #endregion

    }   // end of class OptionsMenu


}   // end of namespace Boku.Scenes
