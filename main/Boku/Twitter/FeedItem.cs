// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.


using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;

using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

using KoiX;
using KoiX.Text;
using KoiX.UI;

using Boku.Common;

namespace Boku
{
    /// <summary>
    /// Represents a single item in the Kodu news feed.
    /// </summary>
    public class FeedItem
    {
        #region Members

        static ThemeSet theme;

        static SystemFont dateFont;
        static SystemFont headlineFont;
        static SystemFont bodyFont;
        static SystemFont linkFont;

        TextBlob dateBlob;
        TextBlob headlineBlob;
        TextBlob bodyBlob;
        TextBlob linkBlob;

        string dateString;
        string headlineString;
        string bodyString;
        string urlString;

        RectangleF linkHitBox;
        int width;  // Width to wrap text;

        #endregion

        #region Accessors

        public int Width
        {
            get { return Width; }
            set
            {
                if (width != value)
                {
                    width = value;

                    dateBlob.Width = width;
                    headlineBlob.Width = width;
                    bodyBlob.Width = width;
                    linkBlob.Width = width;
                }
            }
        }

        /// <summary>
        /// Get the total height of this message.  Note this does
        /// not include any spacing between messages.
        /// </summary>
        public float Height
        {
            get
            {
                float height = 0;
                height += dateBlob.NumLines * dateBlob.TotalSpacing;
                height += headlineBlob.NumLines * headlineBlob.TotalSpacing;
                height += bodyBlob.NumLines * bodyBlob.TotalSpacing;
                height += linkBlob.NumLines * linkBlob.TotalSpacing;

                return height;
            }
        }

        /// <summary>
        /// Hit box for URL.
        /// </summary>
        public RectangleF LinkHitBox
        {
            get { return linkHitBox; }
        }

        public string URL
        {
            get { return urlString; }
        }

        #endregion

        #region Public

        public FeedItem(int width, string dateString, string headlineString, string bodyString, string urlString)
        {
            char[] trimChars = { ' ', '\n', '\t'};

            this.width = width;
            this.dateString = dateString.Trim(trimChars);
            this.headlineString = headlineString.Trim(trimChars);
            this.bodyString = bodyString.Trim(trimChars);
            this.urlString = urlString.Trim(trimChars);

            dateBlob = new TextBlob(DateFont, this.dateString, width);
            headlineBlob = new TextBlob(HeadlineFont, this.headlineString, width);
            bodyBlob = new TextBlob(BodyFont, this.bodyString, width);
            linkBlob = new TextBlob(LinkFont, Strings.Localize("mainMenu.readMoreHere"), width);

        }   // end of c'tor

        static public void Init(ThemeSet theme, float scale)
        {
            FeedItem.theme = theme;

            dateFont = SysFont.GetSystemFont(theme.TextFontFamily, 15 * scale, System.Drawing.FontStyle.Regular);
            headlineFont = SysFont.GetSystemFont(theme.TextFontFamily, 18 * scale, System.Drawing.FontStyle.Bold);
            bodyFont = SysFont.GetSystemFont(theme.TextFontFamily, 15 * scale, System.Drawing.FontStyle.Regular);
            linkFont = SysFont.GetSystemFont(theme.TextFontFamily, 15 * scale, System.Drawing.FontStyle.Underline);
        }

        /// <summary>
        /// Render this item starting at the given position.
        /// </summary>
        /// <param name="pos"></param>
        public void Render(SpriteCamera camera, Vector2 pos)
        {
            dateBlob.RenderText(camera, pos, theme.DarkTextColor);
            pos.Y += dateBlob.NumLines * dateBlob.TotalSpacing;

            headlineBlob.RenderText(camera, pos, theme.DarkTextColor);
            pos.Y += headlineBlob.NumLines * headlineBlob.TotalSpacing;

            bodyBlob.RenderText(camera, pos, theme.DarkTextColor);
            pos.Y += bodyBlob.NumLines * bodyBlob.TotalSpacing;

            linkBlob.RenderText(camera, pos, Color.DarkBlue);
            linkHitBox = new RectangleF(pos, new Vector2(linkBlob.GetLineWidth(0), linkBlob.TotalSpacing));
            pos.Y = linkBlob.NumLines * linkBlob.TotalSpacing;


        }   // end of Render()

        #endregion

        #region Internal

        static FontWrapper HeadlineFont()
        {
            FontWrapper wrapper = new FontWrapper(null, headlineFont);
            return wrapper;
        }

        static FontWrapper DateFont()
        {
            FontWrapper wrapper = new FontWrapper(null, dateFont);
            return wrapper;
        }

        static FontWrapper BodyFont()
        {
            FontWrapper wrapper = new FontWrapper(null, bodyFont);
            return wrapper;
        }

        static FontWrapper LinkFont()
        {
            FontWrapper wrapper = new FontWrapper(null, linkFont);
            return wrapper;
        }

        #endregion
    }   // end of class FeedItem

}   // end of namespace Boku
