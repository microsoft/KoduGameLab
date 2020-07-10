// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System;
using System.Collections.Generic;
using System.Text;

using System.Xml;
using System.Xml.Serialization;
using System.Diagnostics;

using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Audio;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using Microsoft.Xna.Framework.Storage;

using Boku.Base;
using Boku.Common;
//using Boku.Common.Gesture;
using Boku.Fx;
using Boku.UI2D;

namespace Boku
{
    public enum HyperlinkType
    {
        HashTag,
        Mention,
        URL,
        None
    }

    public class Hyperlink
    {
        #region Members

        HyperlinkType linkType = HyperlinkType.URL;
        string text;
        string linkUrl;

        TextBlob blob;
        Color textColor = Color.Black;

        AABB2D hitBox;

        public bool IsClickFocus = false;
        public bool WasClicked = false;

        #endregion

        #region Accessors

        public HyperlinkType LinkType
        {
            get { return linkType; }
        }

        public string LinkText
        {
            get { return text; }
        }

        public string LinkUrl
        {
            get { return linkUrl; }
        }

        public AABB2D HitBox
        {
            get { return hitBox; }
            set { hitBox = value; }
        }

        public Color TextColor
        {
            get { return textColor; }
            set { textColor = value; }
        }

        #endregion

        #region Public

        public Hyperlink(string text, string linkUrl)
        {
            linkType = HyperlinkType.URL;
            this.text = text;
            this.linkUrl = linkUrl;

            blob = new TextBlob(Shared.GetGameFont10, text, 390);

            // Need to set once we know the correct position.
            hitBox = new AABB2D();
        }

        public void Draw(Vector2 position)
        {
            blob.RenderWithButtons(position, TextColor);

            // Set hitbox.  Note that the coordinates for the hitbox are relative
            // to the upper left hand corner of the rendertarget we're currently
            // drawing to, not the screen.  Need to adjust when hit testing.
            Vector2 size = new Vector2(blob.Font().MeasureString(blob.ScrubbedText).X, blob.TotalSpacing);
            hitBox.Set(position, position + size);

            /*
            // Debug highlight of hyperlink hitbox.
            ScreenSpaceQuad ssquad = ScreenSpaceQuad.GetInstance();
            ssquad.Render(new Vector4(1, 0, 0, 0.5f), position, size);
            */
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="pos">Needs to be relative to rt where link was rendered.</param>
        /// <param name="clickType"></param>
        /// <returns></returns>
        public bool HitTest(Vector2 pos, ClickType clickType)
        {
            if (HitBox.Contains(pos))
            {
                if (GamePadInput.ActiveMode == GamePadInput.InputMode.Touch)
                {
                    // In touch mode, we don't hover first, count it as clicked right away.
                    if (clickType == ClickType.WasPressed || clickType == ClickType.WasReleased)
                    {
                        IsClickFocus = true;
                        WasClicked = true;
                        return true;
                    }
                }
                else
                {
                    if (clickType == ClickType.WasPressed)
                    {
                        IsClickFocus = true;
                    }
                    else if (clickType == ClickType.WasReleased)
                    {
                        if (IsClickFocus)
                        {
                            WasClicked = true;
                        }
                    }
                    return true;
                }
            }

            IsClickFocus = false;
            WasClicked = false;
            
            return false;

        }   // end of HitTest()

        #endregion

        #region Internal
        #endregion
    }   // end of class Hyperlink

    
}
