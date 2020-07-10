// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.


using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using System.Runtime.Serialization.Formatters.Binary;

using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using Microsoft.Xna.Framework.Input.Touch;

using KoiX;
using KoiX.Geometry;
using KoiX.Input;
using KoiX.Managers;
using KoiX.Text;
using KoiX.UI.Dialogs;

using Boku;
using Boku.Common;
using Boku.Common.Xml;

namespace KoiX.UI
{
    /// <summary>
    /// A widget combo container that holds a title, a level thumbnail, a level name, a clear button, and the Help Button.
    /// If there is no valid level the thumbnail and level name will idicate so.
    /// The clear button is optional and only appears if there is a valid link.
    /// 
    /// Note that the Label and Level Name are actually implemented by a TextBox so that it may be multi-line.
    /// 
    /// </summary>
    public class LinkedLevelLabelHelp : WidgetSet
    {
        #region Members

        TextBox label;
        TextBox levelTitle;
        HelpButton helpButton;
        GraphicButton thumbnail;
        ButtonLabelHelp clear;

        string helpId = "NextLevel";
        string labelId = "editWorldParams.nextLevel";
        string noLevelmessageId = "editWorldParams.noLevelSelected";
        string clearId = "editWorldParams.clearNextLevel";

        // Width of entire container.
        float width;

        #endregion

        #region Accessors

        /// <summary>
        /// The current linked level title.
        /// </summary>
        public string Title
        {
            get { return levelTitle.ScrubbedText; }
            set
            {
                if (value != levelTitle.ScrubbedText)
                {
                    if (value == null)
                    {
                        levelTitle.DisplayText = Strings.Localize(noLevelmessageId);
                    }
                    else
                    {
                        levelTitle.DisplayText = value;
                    }
                }
            }
        }

        public Texture2D ThumbnailTexture
        {
            get { return thumbnail.Texture; }
            set { thumbnail.Texture = value; }
        }

        public GraphicButton Button
        {
            get { return thumbnail; }
        }

        #endregion

        #region Public

        public LinkedLevelLabelHelp(BaseDialog parentDialog, GetFont Font, float width, Callback OnChange = null, ThemeSet theme = null)
            : base(parentDialog, RectangleF.EmptyRect, orientation: Orientation.None, horizontalJustification: Justification.Full, verticalJustification: Justification.Top)
        {
            int margin = 8;

            if (theme == null)
            {
                theme = Theme.CurrentThemeSet.Clone() as ThemeSet;
            }

            this.width = width;
            
            // Turn off orientation for this set.  This means that all elements must be manually placed.
            Orientation = Orientation.None;

            // Help button.
            if (helpId != null)
            {
                helpButton = new HelpButton(parentDialog, OnHelp, data: data);
            }

            // Title.
            label = new TextBox(parentDialog, Font, theme.DarkTextColor, textId: labelId);
            // Calc width of label.
            float textWidth = width;
            textWidth -= helpId != null ? helpButton.LocalRect.Width : 0;
            textWidth -= 3 * margin;
            label.Width = (int)textWidth;
            label.Size = label.CalcMinSize();
            AddWidget(label);

            // Thumbnail for linking.
            Callback onLinkLevel = delegate(BaseWidget w) 
            { 
                // Launch LoadLevelScene to pick level for linking.
                SceneManager.SwitchToScene("LoadLevelAttachingScene");
            };
            thumbnail = new GraphicButton(parentDialog, new RectangleF(0, 0, 256, 256), @"Textures\GridElements\NoNextLevel", onLinkLevel);
            AddWidget(thumbnail);

            // LevelTitle
            levelTitle = new TextBox(parentDialog, Font, theme.DarkTextColor, textId: noLevelmessageId);
            levelTitle.Size = levelTitle.CalcMinSize();
            AddWidget(levelTitle);

            // Clear button.
            Callback onClear = delegate(BaseWidget w) 
            {
                if (InGame.XmlWorldData != null && InGame.XmlWorldData.LinkedToLevel.HasValue)
                {
                    Guid target = InGame.XmlWorldData.LinkedToLevel.Value;

                    // Update this world.  Note that we don't save it, we just mark it as dirty so
                    // it can either be autosaved or saved for real.
                    InGame.XmlWorldData.LinkedToLevel = null;
                    InGame.IsLevelDirty = true;

                    // Update the target.
                    {
                        string filename = Path.Combine(Storage4.UserLocation, BokuGame.Settings.MediaPath, BokuGame.MyWorldsPath, target.ToString() + ".Xml");
                        XmlWorldData level = XmlWorldData.Load(filename, XnaStorageHelper.Instance);
                        if (level != null && level.LinkedFromLevel != null)
                        {
                            level.LinkedFromLevel = null;
                            level.Save(filename, XnaStorageHelper.Instance);
                        }
                    }

                    // Update thumbnail texture.
                    thumbnail.Texture = KoiLibrary.LoadTexture2D(@"Textures\GridElements\NoNextLevel");
                    // Update the title.
                    levelTitle.DisplayText = Strings.Localize(noLevelmessageId);
                }
            };
            clear = new ButtonLabelHelp(parentDialog, Font, clearId, helpId = null, width - thumbnail.LocalRect.Width - 16, OnChange: onClear, theme: theme);
            AddWidget(clear);

            localRect.Width = width;
            localRect.Height = label.NumLines * label.TotalSpacing;

            if (helpButton != null)
            {
                AddWidget(helpButton);
            }

            // Set fixed positions.  Since we're using Orientation.None, no layout gets applied.
            // This also means that Margin and Padding are ignored.
            label.Position = new Vector2(margin, 0);

            // Right justify help.
            if (helpButton != null)
            {
                helpButton.Position = new Vector2(width - helpButton.LocalRect.Width - margin, 0);
            }

            thumbnail.Position = new Vector2(0, label.LocalRect.Height + margin);

            levelTitle.Position = thumbnail.Position + new Vector2(thumbnail.LocalRect.Width, 0) + new Vector2(margin, margin);
            levelTitle.Width = (int)(width - levelTitle.Position.X);
            clear.Position = levelTitle.Position + new Vector2(0, levelTitle.LocalRect.Height + margin);

            localRect.Size = new Vector2(width, label.LocalRect.Height + margin + thumbnail.Size.Y);

        }   // end of c'tor

        public override void Update(SpriteCamera camera, Vector2 parentPosition)
        {
            // Keeps things in sync depending on whether or not we have a valid level to link to.
            /*
            bool validLevel = false;
            if (InGame.XmlWorldData != null && InGame.XmlWorldData.LinkedToLevel != null)
            {
                validLevel = true;
            }
            */

            // TODO (****)  Figure out how to properly do this.
            //
            // Original intent was to have clear button go
            // away when not needed.  This would require the dPad links
            // to be recalculated.  So, for now, just leave it.
            /*
            if (validLevel)
            {
                // Note that these are both no-ops if already activated and visible.
                clear.Activate();
                clear.Alpha = 1;
            }
            else
            {
                if (clear.Active)
                {
                    // We ned to set focus on the thumbnail here.  Otherwise the
                    // focus is still on the clear button even though it is
                    // deactivated.
                    thumbnail.SetFocus();
                    clear.Deactivate();
                }
                clear.Alpha = 0;
            }
            */

            base.Update(camera, parentPosition);
        }   // end of Update()

        public override void Render(SpriteCamera camera, Vector2 parentPosition)
        {
            /*
            // If in focus, render a box underneath to indicate this.
            if (button.InFocus)
            {
                RectangleF rect = LocalRect;
                rect.Position += parentPosition;
                rect.Inflate(2);
                Geometry.RoundedRect.Render(camera, rect, theme.ButtonCornerRadius, theme.FocusColor);
            }
            */

            base.Render(camera, parentPosition);
        }   // end of Render()

        public override void RegisterForInputEvents()
        {
            // Call base register first.  By putting the child widgets on the input stacks
            // first we can then put oursleves on and have priority.
            base.RegisterForInputEvents();

            // Need to look for events in areas other than the child widgets.
            KoiLibrary.InputEventManager.RegisterForEvent(this, InputEventManager.Event.Keyboard);
            KoiLibrary.InputEventManager.RegisterForEvent(this, InputEventManager.Event.MouseLeftDown);
            KoiLibrary.InputEventManager.RegisterForEvent(this, InputEventManager.Event.GamePad);
            KoiLibrary.InputEventManager.RegisterForEvent(this, InputEventManager.Event.Tap);

            // Focus.
            KoiLibrary.InputEventManager.RegisterForEvent(this, InputEventManager.Event.Touch);
        }   // end of RegisterForInputEvents()

        public override void SetOnChange(Callback onChange)
        {
            this.onChange = onChange;
        }

        #endregion

        #region InputEventHandler

        public override bool ProcessKeyboardEvent(KeyInput input)
        {
            Debug.Assert(Active);

            //if (button.InFocus)
            {
                switch (input.Key)
                {
                    case Keys.F1:
                        if (helpButton != null)
                        {
                            helpButton.OnButtonSelect();
                        }
                        return true;

                    default:
                        // Do nothing here, just let fall through to base.
                        break;
                }
            }

            return base.ProcessKeyboardEvent(input);
        }   // end of ProcessKeyboardEvent()

        public override bool ProcessMouseLeftDownEvent(MouseInput input)
        {
            Debug.Assert(Active);

            if (KoiLibrary.InputEventManager.MouseFocusObject == null)
            {
                if (MouseOver)
                {
                    //button.OnSelect();

                    return true;
                }
            }

            return false;
        }   // end of ProcessMouseLeftDownEvent()

        public override bool ProcessGamePadEvent(GamePadInput pad)
        {
            Debug.Assert(Active);

            //if (button.InFocus)
            {
                if (pad.ButtonY.WasPressedOrRepeat)
                {
                    if (helpButton != null)
                    {
                        helpButton.OnButtonSelect();
                        return true;
                    }
                }
            }

            return base.ProcessGamePadEvent(pad);
        }   // end of ProcessGamePadEvent()

        public override bool ProcessTouchTapEvent(TapGestureEventArgs gesture)
        {
            Debug.Assert(Active);

            BaseWidget hitObject = gesture.HitObject as BaseWidget;
            if (hitObject != null && Widgets.Contains(hitObject))
            {
                /*
                if (button != null)
                {
                    button.SetFocus();
                    if (hitObject != helpButton)
                    {
                        button.OnSelect();
                        return true;
                    }
                }
                */
            }   // end of ProcessTouchTapEvent()

            return base.ProcessTouchTapEvent(gesture);
        }   // end of ProcessTouchTapEvent()

        public override bool ProcessTouchEvent(List<TouchSample> touchSampleList)
        {
            Debug.Assert(Active);

            // Is first TouchSample state Pressed?
            if (touchSampleList.Count > 0 && touchSampleList[0].State == TouchLocationState.Pressed)
            {
                /*
                if (KoiLibrary.InputEventManager.TouchHitObject != null
                    && (KoiLibrary.InputEventManager.TouchHitObject == button || KoiLibrary.InputEventManager.TouchHitObject == label || KoiLibrary.InputEventManager.TouchHitObject == helpButton))
                {
                    SetFocus();
                    return true;
                }
                */
            }

            return base.ProcessTouchEvent(touchSampleList);
        }   // end of ProcessTouchEvent()

        #endregion

        #region Internal

        void OnHelp(BaseWidget w)
        {
            TextDialog helpDialog = SharedX.TextDialog;

            Debug.Assert(helpDialog.Active == false);

            helpDialog.TitleId = "mainMenu.help";
            helpDialog.BodyText = TweakScreenHelp.GetHelp(helpId);
            DialogManagerX.ShowDialog(helpDialog);
        }   // end of OnHelp()

        #endregion

    }   // end of class LinkedLevelLabelHelp

}   // end of namespace KoiX.UI
