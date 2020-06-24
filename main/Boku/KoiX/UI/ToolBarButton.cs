
using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using System.Runtime.Serialization.Formatters.Binary;

using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

using KoiX;
using KoiX.Geometry;
using KoiX.Input;
using KoiX.UI.Dialogs;

using Boku.Input;

namespace KoiX.UI
{
    /// <summary>
    /// A specialization of GraphicButton which is used for the edit mode toolbar.
    /// Acts like a radio button but also responds to mouse-over.
    /// </summary>
    public class ToolBarButton : GraphicButton
    {
        static public int SmallSize = 64;
        static public int LargeSize = 96;

        static Texture2D reticule;

        #region Members

        List<ToolBarButton> siblings;

        EditModeTools tool;

        float curSize = SmallSize;
        float _curSize = SmallSize;     // Twitch target for curSize.

        #endregion

        #region Accessors

        public override bool Selected
        {
            get
            {
                return base.Selected;
            }
            set
            {
                base.Selected = value;
                // If this one is selected, need to unselect all siblings.
                if (Selected)
                {
                    foreach (ToolBarButton b in siblings)
                    {
                        if (b != this)
                        {
                            b.Selected = false;
                        }
                    }
                }
            }
        }

        public float CurSize
        {
            get { return curSize; }
            set
            {
                if (value != _curSize)
                {
                    _curSize = value;
                    TwitchManager.Set<float> set = delegate(float size, Object param) { curSize = size; localRect.SetSize(new Vector2(curSize, curSize)); };
                    TwitchManager.CreateTwitch<float>(curSize, _curSize, set, 0.2f, TwitchCurve.Shape.EaseOut);
                }
            }
        }

        /// <summary>
        /// The tool type represented by this button.
        /// </summary>
        public EditModeTools Tool
        {
            get { return tool; }
        }

        #endregion

        #region Public

        public ToolBarButton(BaseDialog parentDialog, List<ToolBarButton> siblings, RectangleF rect, string textureName, Callback onSelect, EditModeTools tool, string id = null)
            : base(parentDialog: parentDialog, rect: rect, textureName: textureName, onSelect: onSelect, id: id)
        {
            this.siblings = siblings;
            this.tool = tool;

            // Add self.
            siblings.Add(this);

        }   // end of c'tor

        public override void Update(SpriteCamera camera, Vector2 parentPosition)
        {
            
            base.Update(camera, parentPosition);
        }   // end of Update()

        public override void Render(SpriteCamera camera, Vector2 parentPosition)
        {
            // Reticule?
            if (Selected)
            {
                SpriteBatch batch = KoiLibrary.SpriteBatch;
                Rectangle rect = LocalRect.ToRectangle();
                rect.X += (int)parentPosition.X;
                rect.Y += (int)parentPosition.Y;
                rect.Inflate(8, 8);
                batch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, null, null, null, null, camera.ViewMatrix);
                {
                    batch.Draw(reticule, rect, Color.White);
                }
                batch.End();
            }

            base.Render(camera, parentPosition);
        }   // end of Render()

        #endregion

        #region Internal

        public override void LoadContent()
        {
            if (DeviceResetX.NeedsLoad(reticule))
            {
                reticule = KoiLibrary.LoadTexture2D(@"Textures\UI2D\Tools\SelectionReticule2");
            }
            
            base.LoadContent();
        }   // end of LoadContent()

        public override void UnloadContent()
        {
            DeviceResetX.Release(ref reticule);

            base.UnloadContent();
        }   // end of UnloadContent()

        #endregion

    }   // end of class ToolBarButton

}   // end of namespace KoiX.UI
