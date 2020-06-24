
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
using KoiX.Text;
using KoiX.UI;

using Boku;
using Boku.Base;
using Boku.Common;
using Boku.Scenes.InGame.MouseEditTools;

namespace KoiX.UI.Dialogs
{
    /// <summary>
    /// Dialog which appears when user right-clicks on screen, not on an actor.
    /// </summary>
    public class OffActorPopupDialog : BasePopupMenuDialog
    {
        #region Members

        Button addObjectButton;
        Button worldSettingsButton;
        Button pasteButton;

        Vector3 worldPosition;  // Where to add or paste an object.

        #endregion

        #region Accessors

        /// <summary>
        /// Where to add or paste an object.
        /// </summary>
        public Vector3 WorldPosition
        {
            set { worldPosition = value; }
        }

        #endregion

        #region Public

        public OffActorPopupDialog()
        {
#if DEBUG
            _name = "OffActorMenu";
#endif

            addObjectButton = new Button(this, RectangleF.EmptyRect, labelId: "mouseEdit.addObject", OnChange: OnAddObject, theme: theme);
            set.AddWidget(addObjectButton);

            worldSettingsButton = new Button(this, RectangleF.EmptyRect, labelId: "mouseEdit.worldTweak", OnChange: OnWorldSettings, theme: theme);
            set.AddWidget(worldSettingsButton);

            pasteButton = new Button(this, RectangleF.EmptyRect, labelId: "mouseEdit.paste", OnChange: OnPaste, theme: theme);
            set.AddWidget(pasteButton);

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

        void OnAddObject(BaseWidget w)
        {
            DialogManagerX.KillDialog(this);

            EditObjectsTool.AddItemPieMenu.SetParams(parent: null, offset: Vector2.Zero, addPosition: worldPosition.XY());

            DialogManagerX.ShowDialog(EditObjectsTool.AddItemPieMenu);
        }   // end of OnAddObject()

        void OnWorldSettings(BaseWidget w)
        {
            DialogManagerX.KillDialog(this);

            SceneManager.SwitchToScene("WorldSettingsMenuScene");
        }   // end of OnWorldSettings()

        void OnPaste(BaseWidget w)
        {
            DialogManagerX.KillDialog(this);

            InGame.inGame.editObjectUpdateObj.PasteAction(null, worldPosition);
        }   // end of OnPaste()

        public override void Activate(params object[] args)
        {
            // Tweak text for PasteButton to reflect what we have in the paste buffer.
            GameActor actor = InGame.inGame.editObjectUpdateObj.CutPasteObject as GameActor;
            string str = Strings.Localize("mouseEdit.paste");
            if (actor == null)
            {
                str += " (" + Strings.Localize("mouseEdit.empty") + ")";
            }
            else
            {
                str += " (" + actor.DisplayNameNumber + ")";
            }
            pasteButton.Label.LabelText = str;

            base.Activate(args);
        }   // end of Activate()

        #endregion

        #region InputEventHandler

        public override InputEventHandler HitTest(Vector2 hitLocation)
        {
            return base.HitTest(hitLocation);
        }

        #endregion

        #region Iternal
        #endregion

    }   // end of class OffActorPopupDialog

}   // end of namespace KoiX.UI.Dialogs
