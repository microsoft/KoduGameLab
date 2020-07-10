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
using KoiX.Text;
using KoiX.UI;

using Boku;
using Boku.Base;
using Boku.Common;

namespace KoiX.UI.Dialogs
{
    public class AddItemPieMenuDialog : BasePieMenuDialog
    {
        #region Members

        Vector2 addPosition;

        #endregion

        #region Accessors
        #endregion

        #region Public

        public AddItemPieMenuDialog()
        {
        }   // end of c'tor

        /// <summary>
        /// 
        /// </summary>
        /// <param name="parent"></param>
        /// <param name="offset">Offset in UI space used when adding submenus.</param>
        /// <param name="addPosition">World position to add new object when selected.</param>
        public void SetParams(BasePieMenuDialog parent, Vector2 offset, Vector2 addPosition)
        {
            this.addPosition = addPosition;

            base.SetParams(parent, offset);
        }   // end of SetParams()

        #endregion

        #region Internal

        override protected void OnSelect(PieMenuElement e)
        {
            // If we have a child, bring up the submenu.
            child = e.Child as AddItemPieMenuDialog;
            if (child != null)
            {
                // Calc center of selected slice as offset.
                Vector2 offset = Vector2.Zero;
                offset = (e.InnerRadius + e.OuterRadius) / 2.0f * new Vector2((float)Math.Cos(e.CenterAngle), (float)Math.Sin(e.CenterAngle));

                // Launch sub-menu.
                (child as AddItemPieMenuDialog).SetParams(parent: this, offset: offset, addPosition: addPosition);
                DialogManagerX.ShowDialog(child);
            }
            else if (e.StaticActor != null)
            {
                // if we have a StaticActor, create that bot.
                // Kill full chain of submenus.
                AddItemPieMenuDialog d = this;
                do
                {
                    AddItemPieMenuDialog p = d.parent as AddItemPieMenuDialog;
                    DialogManagerX.KillDialog(d);
                    d = p;
                } while (d != null);

                // Add the selected actor at the right location.
                GameActor actor = InGame.inGame.AddActor(ActorFactory.Create(e.StaticActor),
                                                        new Vector3(addPosition, float.MaxValue),
                                                        InGame.inGame.shared.camera.Rotation);

                InGame.IsLevelDirty = true;
            }
            else
            {
                // Right now this is where we end up when doing paths.  Need to figure out
                // what to set and where to set it...
            }

        }   // end of OnSelect()

        #endregion

    }   // end of class AddItemPieMenuDialog
}   // end of namespace KoiX.UI.Dialogs
