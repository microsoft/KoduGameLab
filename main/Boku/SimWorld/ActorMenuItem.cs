// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.


using System;
using System.Collections;
using System.Diagnostics;

using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Audio;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using Microsoft.Xna.Framework.Storage;

using KoiX;
using KoiX.Text;

using Boku;
using Boku.Base;
using Boku.Common;
using Boku.Common.Xml;
using Boku.Fx;

namespace Boku.SimWorld
{
    public delegate GameActor MakeActorFn();

    /// <summary>
    /// A specialization of the Billboard designed to be used as a pie selector menu item.
    /// Also contains the info needed to support the help system.
    /// </summary>
    public class ActorMenuItem : Billboard
    {
        #region Members

        private string name = null;             // The friendly name of the menu item.
        private StaticActor staticActor = null;

        private Vector3 pieCenter = Vector3.Zero;   // Center of the pie menu used to position the help button.
        private bool displayHelpButton = false;

        #endregion

        #region Accessors

        /// <summary>
        /// Friendly name for this menu item.
        /// </summary>
        public string Name
        {
            get { return name; }
        }

        public StaticActor StaticActor
        {
            get { return staticActor; }
        }

        /// <summary>
        /// Controls whether or not the <Y> button is displayed on the slice.
        /// Note:  This gets cleared every frame and so needs to be set every
        /// frame by the pie menu.  This saves the hassle of trying to figure
        /// out how to unset it in the pie menu.
        /// </summary>
        public bool DisplayHelpButton
        {
            get { return displayHelpButton; }
            set { displayHelpButton = value; }
        }

        /// <summary>
        /// Tell the item where the parent's center is.
        /// </summary>
        public Vector3 PieCenter
        {
            set { pieCenter = value; }
        }

        #endregion

        #region Public

        /// <summary>
        /// Wrapper around Billboard for use as a menu item in the add item menu.
        /// For groups you should leave name, typeName and makeActorfn null.
        /// </summary>
        /// <param name="parent">Pie selector that holds this item.</param>
        /// <param name="name">Friendly (localized) name of element.  Used to add a label.  If the label is already embedded in the texture then set this to null.</param>
        /// <param name="typeName">Type name without the "Boku.".  Used for help system.</param>
        /// <param name="textureFilename">Texture2D tile to display of pie slice.</param>
        /// <param name="makeActorFn">Delegate which creates a new actor of this type.</param>
        /// <param name="size">Size to display texture on slice.</param>
        /// <param name="radialOffset">Offset away from center for texture on slice.</param>
        public ActorMenuItem(Object parent, string name, string textureFilename, StaticActor staticActor, Vector2 size, float radialOffset)
            : base(parent, textureFilename, size)
        {
            this.name = name;
            this.staticActor = staticActor;

            // We need the 0.5 offset in z to put the billboard on top of the pie slice.
            localTransform.Translation = new Vector3(radialOffset, 0, 0.5f);
            localTransform.Compose();
        }   // end of ActorMenuItem c'tor

        public override void Render(Camera camera)
        {
            base.Render(camera);

            // Add a text label if it exists and we're not getting the texture from cardspace.
            if (name != null && !TextureIsFromCardSpace)
            {
                GetFont Font = SharedX.GetCardLabel;
                SpriteBatch batch = KoiLibrary.SpriteBatch;

                Point pixelPos = camera.WorldToScreenCoords(worldMatrix.Translation);
                Vector2 pos = new Vector2(pixelPos.X, pixelPos.Y);

                name = TextHelper.FilterInvalidCharacters(name);
                pos.X -= (int)Font().MeasureString(name).X / 2;
                pos.Y += 42;

                Color newBlack = new Color(20, 20, 20);

                batch.Begin();
                TextHelper.DrawString(Font, name, pos, newBlack);
                batch.End();
            }

            // Show the Y button.
            if (DisplayHelpButton)
            {
                Vector3 loc = MyMath.Lerp(worldMatrix.Translation, pieCenter, 0.4f);
                Point pixelPos = camera.WorldToScreenCoords(loc);
                Vector2 pos = new Vector2(pixelPos.X, pixelPos.Y);
                Vector2 size = new Vector2(KoiLibrary.GraphicsDevice.Viewport.Height / 12.0f);
                // Center button.
                pos -= size * (40.0f / 64.0f) / 2.0f;

                ScreenSpaceQuad ssquad = ScreenSpaceQuad.GetInstance();
                ssquad.Render(ButtonTextures.YButton, pos, size, "TexturedRegularAlpha");

                DisplayHelpButton = false;
            }

        }   // end of ActorMenuItem Render()

        #endregion

    }   // end of class ActorMenuItem

}   // end of namespace Boku.SimWorld


