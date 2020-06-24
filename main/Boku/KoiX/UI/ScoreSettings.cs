
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
using Boku.Base;
using Boku.Common;
using Boku.Common.Xml;

namespace KoiX.UI
{
    /// <summary>
    /// A widget combo container that holds a blob showing the score color, an edit box for teh score name,
    /// a drop-down menu for setting teh score visibility, and a checkbox for setting the persistence of the score.
    /// 
    /// Note that the Label and Level Name are actually implemented by a TextBox so that it may be multi-line.
    /// 
    /// </summary>
    public class ScoreSettings : WidgetSet
    {
        #region Members

        static ScoreDisplayDialog scoreDisplayDialog;

        WidgetSet bodySet;              // Contains everything except the color box.
        SingleLineInputHelp scoreName;
        ButtonLabelHelp displayButton;  // Will this work without a label?
        CheckBoxLabelHelp persistence;

        Classification.Colors color;
        Color colorRGB = Color.White;

        // Width of entire container.
        float width;
        float colorWidth = 80;      // Space left for color block on left side.

        string labelHelpId = "";

        SpriteCamera camera;        // Local ref.

        #endregion

        #region Accessors
        #endregion

        #region Public

        public ScoreSettings(BaseDialog parentDialog, GetFont Font, float width, Classification.Colors color, Callback OnChange = null, ThemeSet theme = null)
            : base(parentDialog, RectangleF.EmptyRect, orientation: Orientation.None, horizontalJustification: Justification.Full, verticalJustification: Justification.Top)
        {
#if DEBUG
            _name = "ScoreSettings " + color.ToString();
#endif

            this.color = color;
            colorRGB = Classification.XnaColor(color);

            int margin = 8;
            OutlinePadding = new Padding(margin, margin, margin, margin);
            RenderOutline = true;

            if (theme == null)
            {
                theme = Theme.CurrentThemeSet.Clone() as ThemeSet;
            }

            // Allocate shared dialog.
            if (scoreDisplayDialog == null)
            {
                scoreDisplayDialog = new ScoreDisplayDialog(new RectangleF(0, 0, 400, 260), "editWorldParams.display", theme: theme);
            }

            // Shift set to right to allow for color box.
            this.width = width - colorWidth;

            // Will require manual placement of widgets.
            this.Orientation = UI.Orientation.None;

            bodySet = new WidgetSet(parentDialog, new RectangleF(colorWidth, 0, this.width, 0), UI.Orientation.Vertical, verticalJustification: Justification.Top);
            AddWidget(bodySet);

            {
                Callback onChange = delegate(BaseWidget w)
                {
                    Scoreboard.Score scoreObj = Scoreboard.GetScoreboardScore(color);

                    string label = CleanScoreLabel(scoreName.Text);

                    if (string.IsNullOrWhiteSpace(label))
                    {
                        scoreObj.Labeled = false;
                        scoreObj.Label = null;
                    }
                    else
                    {
                        scoreObj.Labeled = true;
                        scoreObj.Label = label;
                    }
                    
                };
                scoreName = new SingleLineInputHelp(parentDialog, Font, "editWorldParams.label", "ScoreName", this.width, OnChange: onChange, theme: theme);
                bodySet.AddWidget(scoreName);
            }

            {
                BaseDialog.Callback onDeactivate = delegate(BaseDialog d)
                {
                    string str = Strings.Localize("editWorldParams.display") + ":";
                    Scoreboard.Score scoreObj = Scoreboard.GetScoreboardScore(color);
                    switch (scoreObj.Visibility)
                    {
                        case ScoreVisibility.Loud:
                            str += Strings.Localize("editWorldParams.loud");
                            break;
                        case ScoreVisibility.Quiet:
                            str += Strings.Localize("editWorldParams.quiet");
                            break;
                        case ScoreVisibility.Off:
                            str += Strings.Localize("editWorldParams.off");
                            break;
                    }

                    displayButton.Label.DisplayText = str;

                };
                Callback onChange = delegate(BaseWidget w)
                {
                    scoreDisplayDialog.OnDeactivate = onDeactivate;
                    // Tell the dialog which color to affect.
                    scoreDisplayDialog.ClassificationColor = color;
                    // Position the dialog near the color it is editing.
                    // The "0.8f *" just moves it a bit toward the center of the screen.
                    // TODO (scoy) Maybe hide this in an accessor.
                    RectangleF rect = scoreDisplayDialog.Rectangle;
                    rect.Position = 0.8f * (bodySet.Position + camera.ScreenToCamera(bodySet.ParentPosition));
                    scoreDisplayDialog.Rectangle = rect;
                    DialogManagerX.ShowDialog(scoreDisplayDialog, camera);
                };
                displayButton = new ButtonLabelHelp(parentDialog, Font, "editWorldParams.display", "ScoreVisibility", this.width, OnChange: onChange, theme: theme);

                bodySet.AddWidget(displayButton);
            }

            {
                Callback onChange = delegate(BaseWidget w)
                {
                    Scoreboard.Score scoreObj = Scoreboard.GetScoreboardScore(color);
                    scoreObj.PersistFlag = persistence.Checked;
                };
                persistence = new CheckBoxLabelHelp(parentDialog, Font, "editWorldParams.persistence", "ScorePersistence", this.width, OnChange: onChange, theme: theme);

                bodySet.AddWidget(persistence);
            }

            // Tighten vertical arrangement.  By default, these normaly have a top and bottom margin of 8.
            displayButton.Margin = new UI.Padding(0);
            persistence.Margin = new UI.Padding(0);

            // Force recalc so we know the size.
            bodySet.Recalc(Vector2.Zero);

            // Set fixed positions.  Since we're using Orientation.None, no layout gets applied.
            // This also means that Margin and Padding are ignored.
            bodySet.Position = new Vector2(colorWidth, 0);
            localRect.Size = new Vector2(width, bodySet.LocalRect.Height);
        }   // end of c'tor

        public override void Update(SpriteCamera camera, Vector2 parentPosition)
        {
            this.camera = camera;

            base.Update(camera, parentPosition);
        }   // end of Update()

        public override void Render(SpriteCamera camera, Vector2 parentPosition)
        {
            base.Render(camera, parentPosition);

            // Render color blob over top of base render.
            RectangleF rect = new RectangleF(parentPosition + Position, new Vector2(colorWidth - 16, bodySet.LocalRect.Height));
            RoundedRect.Render(camera, rect, radius: 6.0f, color: colorRGB,
                                outlineWidth: 2.0f, outlineColor: Color.Black
                                //bevelStyle: BevelStyle.Round, bevelWidth: 16.0f
                                );

        }   // end of Render()

        public override void RegisterForInputEvents()
        {
            // Call base register first.  By putting the child widgets on the input stacks
            // first we can then put oursleves on and have priority.
            base.RegisterForInputEvents();
        }   // end of RegisterForInputEvents()

        public override void Activate(params object[] args)
        {
            // Update the label on the button to reflect the current state.
            string str = Strings.Localize("editWorldParams.display") + ":";
            Scoreboard.Score scoreObj = Scoreboard.GetScoreboardScore(color);
            switch(scoreObj.Visibility)
            {
                case ScoreVisibility.Loud:
                    str += Strings.Localize("editWorldParams.loud");
                    break;
                case ScoreVisibility.Quiet:
                    str += Strings.Localize("editWorldParams.quiet");
                    break;
                case ScoreVisibility.Off:
                    str += Strings.Localize("editWorldParams.off");
                    break;
            }

            displayButton.Label.DisplayText = str;

            base.Activate(args);
        }   // end of Activate()

        public override void SetOnChange(Callback onChange)
        {
            this.onChange = onChange;
        }

        #endregion

        #region InputEventHandler
        #endregion

        #region Internal

        void OnHelp(BaseWidget w)
        {
            TextDialog helpDialog = SharedX.TextDialog;

            Debug.Assert(helpDialog.Active == false);

            helpDialog.TitleId = "mainMenu.help";
            helpDialog.BodyText = TweakScreenHelp.GetHelp(labelHelpId);
            DialogManagerX.ShowDialog(helpDialog);
        }   // end of OnHelp()

        string CleanScoreLabel(string scoreLabel)
        {
            scoreLabel = scoreLabel.Replace("\n", "");
            scoreLabel = scoreLabel.Replace("\r", "");
            scoreLabel = scoreLabel.Trim();

            scoreLabel = TextHelper.FilterURLs(scoreLabel);
            scoreLabel = TextHelper.FilterEmail(scoreLabel);

            return scoreLabel;
        }   // end of CleanScoreLabel()

        #endregion

    }   // end of class ScoreSettings

}   // end of namespace KoiX.UI
