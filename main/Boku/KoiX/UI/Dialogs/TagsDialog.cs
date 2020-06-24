
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

using KoiX;
using KoiX.Input;
using KoiX.Managers;
using KoiX.Text;
using KoiX.UI;

using Boku;
using Boku.Common;

using BokuShared;

namespace KoiX.UI.Dialogs
{
    public class TagsDialog : BaseDialogWithTitle
    {
        #region Members

        Genres genres;

        Button okButton;

        WidgetSet column1;
        WidgetSet column2;
        Vector2 columnSize = new Vector2(400, 400);
        //float columnGutter = 25.0f;     // Gap between columns.

        CheckBoxLabelHelp actionCheckBox;
        CheckBoxLabelHelp adventureCheckBox;
        CheckBoxLabelHelp puzzleCheckBox;
        CheckBoxLabelHelp racingCheckBox;
        CheckBoxLabelHelp rpgCheckBox;
        CheckBoxLabelHelp shooterCheckBox;
        CheckBoxLabelHelp sportsCheckBox;
        CheckBoxLabelHelp strategyCheckBox;
        CheckBoxLabelHelp multiplayerCheckBox;
        CheckBoxLabelHelp lessonsCheckBox;
        CheckBoxLabelHelp sampleWorldsCheckBox;
        CheckBoxLabelHelp finishedCheckBox;
        CheckBoxLabelHelp favoriteCheckBox;
        CheckBoxLabelHelp keyboardMouseCheckBox;
        CheckBoxLabelHelp xboxControllerCheckBox;
        CheckBoxLabelHelp touchCheckBox;

        #endregion

        #region Accessors

        /// <summary>
        /// The genres (tags) currently editing.
        /// </summary>
        public Genres Genres
        {
            get { return genres; }
            set { genres = value; }
        }

        #endregion

        #region Public

        public TagsDialog(ThemeSet theme = null)
            : base(RectangleF.EmptyRect, titleId: "loadLevelMenu.editTags", theme: theme)
        {
#if DEBUG
            _name = "TagsDialog";
#endif

            // Change ref to what base class has set.
            // Allows us to use theme rather than this.Theme.
            theme = this.ThemeSet;

            // Set size of dialog.
            rect.Size = new Vector2(900, 600);
            // Center dialog on screen.
            rect.Position = -rect.Size / 2.0f;

            column1 = new WidgetSet(this, new RectangleF(new Vector2(150, 100), columnSize), Orientation.Vertical, horizontalJustification: Justification.Left, verticalJustification: Justification.Top);
            column2 = new WidgetSet(this, new RectangleF(new Vector2(900, 100), columnSize), Orientation.Vertical, horizontalJustification: Justification.Left, verticalJustification: Justification.Top);
            bodySet.Orientation = Orientation.Horizontal;
            bodySet.VerticalJustification = Justification.Top;
            bodySet.HorizontalJustification = Justification.Center;
            bodySet.AddWidget(column1);
            bodySet.AddWidget(column2);
            column1.Margin = new Padding(16, 0, 16, 0);
            column2.Margin = new Padding(16, 0, 16, 0);

            SystemFont font = new SystemFont(theme.TextFontFamily, theme.TextBaseFontSize * 1.2f, System.Drawing.FontStyle.Regular);
            FontWrapper wrapper = new FontWrapper(null, font);
            GetFont Font = delegate() { return wrapper; };

            actionCheckBox = new CheckBoxLabelHelp(this, Font, labelId: "genres.action", helpId: null, width: columnSize.X);
            column1.AddWidget(actionCheckBox);
            adventureCheckBox = new CheckBoxLabelHelp(this, Font, labelId: "genres.adventure", helpId: null, width: columnSize.X);
            column1.AddWidget(adventureCheckBox);
            puzzleCheckBox = new CheckBoxLabelHelp(this, Font, labelId: "genres.puzzle", helpId: null, width: columnSize.X);
            column1.AddWidget(puzzleCheckBox);
            racingCheckBox = new CheckBoxLabelHelp(this, Font, labelId: "genres.racing", helpId: null, width: columnSize.X);
            column1.AddWidget(racingCheckBox);
            rpgCheckBox = new CheckBoxLabelHelp(this, Font, labelId: "genres.rpg", helpId: null, width: columnSize.X);
            column1.AddWidget(rpgCheckBox);
            shooterCheckBox = new CheckBoxLabelHelp(this, Font, labelId: "genres.shooter", helpId: null, width: columnSize.X);
            column1.AddWidget(shooterCheckBox);
            sportsCheckBox = new CheckBoxLabelHelp(this, Font, labelId: "genres.sports", helpId: null, width: columnSize.X);
            column1.AddWidget(sportsCheckBox);
            strategyCheckBox = new CheckBoxLabelHelp(this, Font, labelId: "genres.strategy", helpId: null, width: columnSize.X);
            column1.AddWidget(strategyCheckBox);

            multiplayerCheckBox = new CheckBoxLabelHelp(this, Font, labelId: "genres.multiplayer", helpId: null, width: columnSize.X);
            column2.AddWidget(multiplayerCheckBox);
            lessonsCheckBox = new CheckBoxLabelHelp(this, Font, labelId: "genres.lessons", helpId: null, width: columnSize.X);
            column2.AddWidget(lessonsCheckBox);
            sampleWorldsCheckBox = new CheckBoxLabelHelp(this, Font, labelId: "genres.sampleWorlds", helpId: null, width: columnSize.X);
            column2.AddWidget(sampleWorldsCheckBox);
            finishedCheckBox = new CheckBoxLabelHelp(this, Font, labelId: "genres.finishedWorlds", helpId: null, width: columnSize.X);
            column2.AddWidget(finishedCheckBox);
            favoriteCheckBox = new CheckBoxLabelHelp(this, Font, labelId: "genres.favorite", helpId: null, width: columnSize.X);
            column2.AddWidget(favoriteCheckBox);
            keyboardMouseCheckBox = new CheckBoxLabelHelp(this, Font, labelId: "genres.keyboard", helpId: null, width: columnSize.X);
            column2.AddWidget(keyboardMouseCheckBox);
            xboxControllerCheckBox = new CheckBoxLabelHelp(this, Font, labelId: "genres.controller", helpId: null, width: columnSize.X);
            column2.AddWidget(xboxControllerCheckBox);
            touchCheckBox = new CheckBoxLabelHelp(this, Font, labelId: "genres.touch", helpId: null, width: columnSize.X);
            column2.AddWidget(touchCheckBox);

            buttonSet.Padding = new Padding(32, 16, 32, 16);

            okButton = new Button(this, new RectangleF(), labelId: "auth.ok", OnChange: null, theme: this.theme);
            okButton.KillParentDialogOnSelect = true;
            okButton.Size = okButton.CalcMinSize() + new Vector2(8, 0);     // Match button size to label, with a bit of margin.
            okButton.Label.Size = okButton.Size;                            // Make label same size so it gets centered correctly.
            buttonSet.AddWidget(okButton);

            /*
            bodySet.RenderDebugOutline = true;
            column1.RenderDebugOutline = true;
            column2.RenderDebugOutline = true;
            */

            CreateTabList();
            CreateDPadLinks();

        }   // end of c'tor

        public override void Activate(params object[] args)
        {
            // Copy current world settings into checkboxes.
            actionCheckBox.Checked = (genres & Genres.Action) != 0;
            adventureCheckBox.Checked = (genres & Genres.Adventure) != 0;
            puzzleCheckBox.Checked = (genres & Genres.Puzzle) != 0;
            racingCheckBox.Checked = (genres & Genres.Racing) != 0;
            rpgCheckBox.Checked = (genres & Genres.RPG) != 0;
            shooterCheckBox.Checked = (genres & Genres.Shooter) != 0;
            sportsCheckBox.Checked = (genres & Genres.Sports) != 0;
            strategyCheckBox.Checked = (genres & Genres.Strategy) != 0;
            multiplayerCheckBox.Checked = (genres & Genres.Multiplayer) != 0;
            lessonsCheckBox.Checked = (genres & Genres.Lessons) != 0;
            sampleWorldsCheckBox.Checked = (genres & Genres.SampleWorlds) != 0;
            finishedCheckBox.Checked = (genres & Genres.FinishedWorlds) != 0;
            favoriteCheckBox.Checked = (genres & Genres.Favorite) != 0;
            keyboardMouseCheckBox.Checked = (genres & Genres.Keyboard) != 0;
            xboxControllerCheckBox.Checked = (genres & Genres.Controller) != 0;
            touchCheckBox.Checked = (genres & Genres.Touch) != 0;

            base.Activate(args);

            // Note we can only set focus after activation.
            actionCheckBox.SetFocus();

        }   // end of Activate()

        public override void Deactivate()
        {
            // Copy settings back from checkboxes to current world.
            // Note we don't want to change any bits we shouldn't.
            genres = SetGenresCategory(genres, Genres.Action, actionCheckBox.Checked);
            genres = SetGenresCategory(genres, Genres.Adventure, adventureCheckBox.Checked);
            genres = SetGenresCategory(genres, Genres.Puzzle, puzzleCheckBox.Checked);
            genres = SetGenresCategory(genres, Genres.Racing, racingCheckBox.Checked);
            genres = SetGenresCategory(genres, Genres.RPG, rpgCheckBox.Checked);
            genres = SetGenresCategory(genres, Genres.Shooter, shooterCheckBox.Checked);
            genres = SetGenresCategory(genres, Genres.Sports, sportsCheckBox.Checked);
            genres = SetGenresCategory(genres, Genres.Strategy, strategyCheckBox.Checked);
            genres = SetGenresCategory(genres, Genres.Multiplayer, multiplayerCheckBox.Checked);
            genres = SetGenresCategory(genres, Genres.Lessons, lessonsCheckBox.Checked);
            genres = SetGenresCategory(genres, Genres.SampleWorlds, sampleWorldsCheckBox.Checked);
            genres = SetGenresCategory(genres, Genres.FinishedWorlds, finishedCheckBox.Checked);
            genres = SetGenresCategory(genres, Genres.Favorite, favoriteCheckBox.Checked);
            genres = SetGenresCategory(genres, Genres.Keyboard, keyboardMouseCheckBox.Checked);
            genres = SetGenresCategory(genres, Genres.Controller, xboxControllerCheckBox.Checked);
            genres = SetGenresCategory(genres, Genres.Touch, touchCheckBox.Checked);

            base.Deactivate();
        }   // end of Deactivate()

        /// <summary>
        /// Helper function for setting or clearing a single category
        /// in a genres variable.
        /// </summary>
        /// <param name="genres"></param>
        /// <param name="category"></param>
        /// <param name="set"></param>
        /// <returns></returns>
        Genres SetGenresCategory(Genres genres, Genres category, bool set)
        {
            if (set)
            {
                genres |= category;
            }
            else
            {
                genres &= ~category;
            }

            return genres;
        }   // end of SetGenresCategory()

        public override bool ProcessKeyboardEvent(KeyInput input)
        {
            Debug.Assert(Active);

            switch(input.Key)
            {
                // Allow ESC to close dialog.
                case Keys.Escape:
                    DialogManagerX.KillDialog(this);
                    return true;
            }

            return base.ProcessKeyboardEvent(input);
        }   // end of ProcessKeyboardEvent()

        #endregion

        #region Internal
        #endregion

    }   // end of class TagsDialog

}   // end of namespace KoiX.UI.Dialogs
