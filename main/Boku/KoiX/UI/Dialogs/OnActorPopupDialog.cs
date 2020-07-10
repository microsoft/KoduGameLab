// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.


using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;

using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;

using KoiX;
using KoiX.Input;
using KoiX.Managers;
using KoiX.Scenes;
using KoiX.Text;
using KoiX.UI;

using Boku;
using Boku.Base;
using Boku.Common;
using Boku.Programming;

namespace KoiX.UI.Dialogs
{
    /// <summary>
    /// Dialog with actor options which appears when
    /// user right-clicks on an actor in edit mode.
    /// </summary>
    public class OnActorPopupDialog : BasePopupMenuDialog
    {
        #region Members

        Button programButton;
        Button objectSettingsButton;
        Button renameButton;
        Button cutButton;
        Button copyButton;
        Button sizeButton;
        Button rotateButton;
        Button heightButton;

        SinglelineInputDialog renameDialog;

        GameActor focusActor = null;    // The actor under the menu when activated.
                                        // Must be set before showing this dialog.

        #endregion

        #region Accessors

        /// <summary>
        /// The actor under the cursor when this menu was launched.  This
        /// must be set before calling Show on this dialog.
        /// </summary>
        public GameActor FocusActor
        {
            get { return focusActor; }
            set { focusActor = value; }
        }

        #endregion

        #region Public

        public OnActorPopupDialog()
        {
#if DEBUG
            _name = "OnActorMenu";
#endif

            programButton = new Button(this, RectangleF.EmptyRect, labelId: "mouseEdit.program", OnChange: OnProgram, theme: theme);
            set.AddWidget(programButton);

            objectSettingsButton = new Button(this, RectangleF.EmptyRect, labelId: "mouseEdit.objectTweak", OnChange: OnObjectSettings, theme: theme);
            set.AddWidget(objectSettingsButton);

            renameButton = new Button(this, RectangleF.EmptyRect, labelId: "mouseEdit.rename", OnChange: OnRename, theme: theme);
            set.AddWidget(renameButton);

            cutButton = new Button(this, RectangleF.EmptyRect, labelId: "mouseEdit.cut", OnChange: OnCut, theme: theme);
            set.AddWidget(cutButton);

            copyButton = new Button(this, RectangleF.EmptyRect, labelId: "mouseEdit.copy", OnChange: OnCopy, theme: theme);
            set.AddWidget(copyButton);

            sizeButton = new Button(this, RectangleF.EmptyRect, labelId: "mouseEdit.size", OnChange: OnSize, theme: theme);
            set.AddWidget(sizeButton);

            rotateButton = new Button(this, RectangleF.EmptyRect, labelId: "mouseEdit.rotate", OnChange: OnRotate, theme: theme);
            set.AddWidget(rotateButton);

            heightButton = new Button(this, RectangleF.EmptyRect, labelId: "mouseEdit.height", OnChange: OnHeight, theme: theme);
            set.AddWidget(heightButton);

            // Set size to max of all buttons.
            Vector2 max = Vector2.Zero;
            foreach (BaseWidget w in set.Widgets)
            {
                max = MyMath.Max(max, w.CalcMinSize());
            }
            foreach (BaseWidget w in set.Widgets)
            {
                w.Size = max;
            }

            // Positioning will happen on launch.
            Rectangle = new RectangleF(0, 0, max.X, set.Widgets.Count * max.Y);

            // Connect navigation links.
            CreateTabList();

            // Dpad nav includes all widgets so webSiteButton will be there.
            CreateDPadLinks();

        }   // end of c'tor

        void OnProgram(BaseWidget w)
        {
            DialogManagerX.KillDialog(this);

            Debug.Assert(false, "Not impl");
        }   // end of OnProgram()

        void OnObjectSettings(BaseWidget w)
        {
            DialogManagerX.KillDialog(this);

            if (FocusActor != null)
            {
                ObjectSettingsMenuScene.Actor = FocusActor;
                SceneManager.SwitchToScene("ObjectSettingsMenuScene");
            }
        }   // end of OnObjectSettings()

        void OnRename(BaseWidget w)
        {
            DialogManagerX.KillDialog(this);

            renameDialog = new SinglelineInputDialog(labelId: "mouseEdit.rename", prefilledBoxText: focusActor.DisplayName, OnAccept: OnRenameAccept, OnCancel: OnRenameCancel);
            DialogManagerX.ShowDialog(renameDialog);
        }   // end of OnRename()

        void OnRenameAccept(BaseWidget w)
        {
            DialogManagerX.KillDialog(renameDialog);

            string newText = renameDialog.CurrentText;

            if (newText.Length > 0)
            {
                newText = TextHelper.FilterURLs(newText);
                newText = TextHelper.FilterEmail(newText);

                focusActor.DisplayName = newText;
                Boku.Programming.NamedFilter.RegisterInCardSpace(focusActor);
                Boku.InGame.IsLevelDirty = true;
            }

            // Since we're creating it on the fly, get rid of it when we no longer need it.
            renameDialog.UnloadContent();
            renameDialog = null;

        }   // end of OnRenameAccept()

        void OnRenameCancel(BaseWidget w)
        {
            DialogManagerX.KillDialog(renameDialog);

            // Since we're creating it on the fly, get rid of it when we no longer need it.
            renameDialog.UnloadContent();
            renameDialog = null;

        }   // end of OnRenameCancel()

        void OnCut(BaseWidget w)
        {
            DialogManagerX.KillDialog(this);

            InGame.inGame.editObjectUpdateObj.CutAction(focusActor);
        }   // end of OnCut()

        void OnCopy(BaseWidget w)
        {
            DialogManagerX.KillDialog(this);

            InGame.inGame.editObjectUpdateObj.CopyAction(focusActor);
        }   // end of OnCopy()

        void OnSize(BaseWidget w)
        {
            DialogManagerX.KillDialog(this);

            BaseWidget.Callback OnChange = delegate(BaseWidget widget)
            {
                Slider s = widget as Slider; if (s != null)
                {
                    focusActor.ReScale = s.CurValue; InGame.IsLevelDirty = true;
                }
            };

            SliderDialog dialog = new SliderDialog(position: Rectangle.Position, labelId: "mouseEdit.size", helpId: null, minValue: 0.2f, maxValue: 4.0f, increment: 0.1f, numDecimals: 1, curValue: focusActor.ReScale, OnChange: OnChange);
            DialogManagerX.ShowDialog(dialog);

        }   // end of OnSize()

        void OnRotate(BaseWidget w)
        {
            DialogManagerX.KillDialog(this);

            BaseWidget.Callback OnChange = delegate(BaseWidget widget)
            {
                Slider s = widget as Slider; if (s != null)
                {
                    focusActor.Movement.RotationZ = MathHelper.ToRadians(s.CurValue); InGame.IsLevelDirty = true;
                }
            };

            SliderDialog dialog = new SliderDialog(position: Rectangle.Position, labelId: "mouseEdit.rotate", helpId: null, minValue: 0.0f, maxValue: 360.0f, increment: 1.0f, numDecimals: 0, curValue: MathHelper.ToDegrees(focusActor.Movement.RotationZ), OnChange: OnChange);
            DialogManagerX.ShowDialog(dialog);
        
        }   // end of OnRotate()

        void OnHeight(BaseWidget w)
        {
            DialogManagerX.KillDialog(this);

            BaseWidget.Callback OnChange = delegate(BaseWidget widget)
            {
                Slider s = widget as Slider; if (s != null)
                {
                    focusActor.HeightOffset = s.CurValue - focusActor.DefaultEditHeight;
                    focusActor.Movement.Altitude = focusActor.GetPreferredAltitude();
                    InGame.IsLevelDirty = true;
                }
            };

            SliderDialog dialog = new SliderDialog(position: Rectangle.Position, labelId: "mouseEdit.height", helpId: null, minValue: focusActor.MinHeight, maxValue: 30.0f, increment: 0.01f, numDecimals: 2, curValue: focusActor.HeightOffset + focusActor.DefaultEditHeight, OnChange: OnChange);
            DialogManagerX.ShowDialog(dialog);

        }   // end of OnHeight()


        public override void Activate(params object[] args)
        {
            // TODO (****)  Would be better to plumb args through DialogManager.
            Debug.Assert(focusActor != null, "This must be set with the actor under the cursor before calling show.");

            base.Activate(args);
        }   // end of Activate()

        #endregion

        #region InputEventHandler

        #endregion

        #region Iternal
        #endregion

    }   // end of class OnActorPopupDialog

}   // end of namespace KoiX.UI.Dialogs
