using System;
using System.Collections.Generic;
using System.Text;

using System.Xml;
using System.Xml.Serialization;
using System.Diagnostics;
using System.Text.RegularExpressions;

#if NETFX_CORE
    using Windows.Foundation;
    using Windows.System;
#endif

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
    public class FeedMs : ScrollContainer
    {
        private static Color defaultColor = Color.Cyan;
        private static Color hoverColor = Color.YellowGreen;
        private static Color clickColor = Color.Orange;
        private static Color bodyTextColor = new Color(0.2f, 0.2f, 0.2f, 1.0f);
        private Shared.GetFont titleFont = UI2D.Shared.GetGameFont15_75;
        private Shared.GetFont dateFont = UI2D.Shared.GetGameFont10;
        private Shared.GetFont bodyFont = UI2D.Shared.GetGameFont10;

        private List<Hyperlink> hyperlinkList = new List<Hyperlink>();
        private TextBlob textBlob  = null;
        private TextBlob titleBlob = null;
        private TextBlob dateBlob = null;
        private string title = "";
        private DateTime postDateDT = DateTime.Now;
        private string postDate = "";
        private Vector2 drawPos = Vector2.Zero;
        private int focusIndex = 0;
        private bool useFocus = false;

        public List<Hyperlink> HyperlinkList
        {
            get { return hyperlinkList; }
        }

        public DateTime DateDT
        {
            get { return postDateDT; }
            set { postDateDT = value; }
        }

        public string DateString
        {
            get { return postDate; }
            set
            {
                try
                {
                    postDateDT = DateTime.ParseExact(value, "M/d/yyyy", null);                                  
                }
                catch
                {
                }
                postDate = value;
                dateBlob = new TextBlob(dateFont, postDate, 160);                
            }
        }

        public string Title
        {
            get {
                if (titleBlob.Width < (int)(base.Width * 0.8f))
                {
                    titleBlob = new TextBlob(titleFont, titleBlob.RawText, (int)(base.Width * 0.8f));
                }
                return title; }
            set
            {
                int titleWidth = 160;
                if (base.Width * 0.8f > titleWidth)
                    titleWidth = (int)(base.Width * 0.8f);
                title = value;
                titleBlob = new TextBlob(titleFont, title, titleWidth);
                
            }
        }

        public string Body
        {
            set {
                string pattern = "\\\\\\\"";
                string replacement = "\"";
                Regex rgx = new Regex(pattern);
                string result = rgx.Replace(value, replacement);

                textBlob = new TextBlob(bodyFont, result, 150);
                textBlob.Justification = UIGridElement.Justification.Left;
            }
            get
            {
                if (textBlob == null)
                {
                    return "";
                }
                else
                {
                    return textBlob.RawText;
                }
            }
        }




        public FeedMs(Vector2 size, TextBlob tBlob, Shared.GetFont titleFont, Shared.GetFont dateFont, Shared.GetFont bodyFont)
          : base(size)
        {
            this.titleFont = titleFont;
            this.dateFont = dateFont;
            this.bodyFont = bodyFont;

            this.textBlob = tBlob;

            titleBlob = new TextBlob(titleFont, "", 160);
            dateBlob = new TextBlob(dateFont, "", 160);

            size.Y = (tBlob.NumLines - 1 + 1) * tBlob.TotalSpacing;
            size.Y += bodyFont().LineSpacing;
            if (titleBlob != null)
                size.Y += titleBlob.TotalSpacing + titleFont().LineSpacing;
            if (dateBlob != null)
                size.Y += dateBlob.TotalSpacing + dateFont().LineSpacing;
            base.Height = size.Y;

            titleBlob.Justification = UIGridElement.Justification.Left;
            dateBlob.Justification = UIGridElement.Justification.Left;
        }

        public FeedMs(Vector2 size, string text, Shared.GetFont tFont, Shared.GetFont dFont, Shared.GetFont bFont)
          : base(size)
        {

            titleFont = tFont;
            dateFont = dFont;
            bodyFont = bFont;

            textBlob = new TextBlob(bodyFont, text, 150);
            textBlob.Width = (int)size.X;

            titleBlob = new TextBlob(titleFont, "", 160);
            dateBlob = new TextBlob(dateFont, "", 160);

            size.Y = (textBlob.NumLines - 1) * textBlob.TotalSpacing;
            size.Y += bodyFont().LineSpacing;
            size.Y += GetTitleHeightOffset(); 
            size.Y += GetDateHeightOffset();
            base.Height = size.Y;
        }

        public void CreateHyperlink(HyperlinkType linkType, string urlText, string url)
        {
            if (linkType != HyperlinkType.URL)
            {
                Debug.Assert(false, "We only support URLs");
                return;
            }

            Hyperlink hyperlink = new Hyperlink(urlText, url);
            hyperlinkList.Add(hyperlink);

        }

        override public void ResetWidth()
        {

            dateBlob.Width = (int)Width;
            titleBlob.Width = (int)Width;
            textBlob.Width = (int)Width;

            float h = (textBlob.NumLines - 1) * textBlob.TotalSpacing;
            h += bodyFont().LineSpacing;
            h += GetDateHeightOffset();
            h += GetTitleHeightOffset();
            Height = h;
        }

        private float GetDateHeightOffset()
        {
            if (dateBlob == null)
                return 0.0f;
            return (dateBlob.TotalSpacing);// *0.5f;

        }

        private float GetTitleHeightOffset()
        {
            float h = 0.0f;
            if (titleBlob == null)
                return h;
            if (titleBlob.NumLines > 1)
                h = (titleBlob.NumLines-1) * titleBlob.TotalSpacing;
            return titleBlob.TotalSpacing + h;// * 0.65f;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="pos">Appears to be position on RT to render this news item.  Different for each item.</param>
        override public void Render(Vector2 pos)
        {
            base.Render(pos);
            drawPos = pos;
            // drawOffset seems just to be an offset accumulated as each section of 
            // the news item is rendered.
            Vector2 drawOffset = Vector2.Zero;

            dateBlob.RenderWithButtons(
               drawPos,
               Color.Black,
               maxLines: 1);
            drawOffset.Y += GetDateHeightOffset();

            titleBlob.RenderWithButtons(
                drawPos + drawOffset,
                Color.Black,
                maxLines: 1);
            drawOffset.Y += GetTitleHeightOffset();

            // Draw the body of the news item.
            textBlob.RenderWithButtons(drawPos + drawOffset, bodyTextColor);
            drawOffset.Y += textBlob.NumLines * textBlob.TotalSpacing;

            // Draw any attatched hyperlinks.
            foreach (Hyperlink link in hyperlinkList)
            {
                link.Draw(drawPos + drawOffset);
            }

        }

        override public void Press()
        {
            if (useFocus)
            {
                if (focusIndex < HyperlinkList.Count)
                {
                    string urlString = CreateURLFromTwitterLink(HyperlinkList[focusIndex]);
#if NETFX_CORE
                    Uri uri = new Uri(urlString);
                    IAsyncOperation<bool> op = Launcher.LaunchUriAsync(uri);
                    op.AsTask<bool>().Wait();
                    bool result = op.GetResults();
#else
                    Process.Start(urlString);
#endif
                }
            }
        }

        override public void Hover(Vector2 pos)
        {
            foreach (Hyperlink link in HyperlinkList)
            {
                if (link.HitTest(pos, ClickType.None))
                {
                    link.TextColor = hoverColor;
                }
                else
                {
                    link.TextColor = defaultColor;
                }
            }
        }

        override public bool Click(Vector2 pos, out Object obj, ClickType clickType)
        {
            foreach (Hyperlink link in HyperlinkList)
            {
                if (link.HitTest(pos, clickType))
                {
                    if (link.WasClicked)
                    {
                        string urlString = CreateURLFromTwitterLink(link);
                        obj = urlString as Object;
                            //make sure url starts with http://
                        if (!urlString.StartsWith("http://") && !urlString.StartsWith("https://"))
                            urlString = "http://" + urlString;
#if NETFX_CORE
                        Uri uri = new Uri(urlString);
                        IAsyncOperation<bool> op = Launcher.LaunchUriAsync(uri);
                        op.AsTask<bool>().Wait();
                        bool result = op.GetResults();
#else
                        Process.Start(urlString);
#endif
                        return true;
                    }
                    else if (link.IsClickFocus)
                    {
                        link.TextColor = clickColor;
                    }
                    else
                    {
                        link.TextColor = hoverColor;
                    }
                }
            }
            obj = null;
            return false;
        }

        public string CreateURLFromTwitterLink(Hyperlink link)
        {
            if (link.LinkType == HyperlinkType.Mention)
            {
                return "https://www.twitter.com/#!/" + link.LinkText.Substring(1);
            }
            else if (link.LinkType == HyperlinkType.URL)
            {
                return link.LinkUrl;
            }
            else if (link.LinkType == HyperlinkType.HashTag)
            {
                return "https://twitter.com/#!/search/" + link.LinkText.Substring(1);
            }
            // Unknown type.
            return link.LinkText;
        }

        override public void DisableFocus()
        {
            useFocus = false;
        }

        override public void EnableFocus()
        {
            useFocus = true;
        }

        override public void ResetFocus()
        {
            focusIndex = 0;
        }

        override public bool IsFocusEnabled()
        {
            return useFocus || (GetNumFocus() == 0);
        }

        override public bool SetFocusLast()
        {
            if (HyperlinkList.Count > 0)
            {
                focusIndex = (HyperlinkList.Count - 1);
                useFocus = true;
                return true;
            }
            else
            {
                return false;
            }
        }

        override public bool SetPrevFocus() 
        {
            if (!useFocus)
            {
                return false;
            }
            else if (focusIndex > 0)
            {
                focusIndex--;
                return true;
            }
            else
            {
                useFocus = false;
                return false;
            }
        }

        override public bool SetNextFocus() 
        {
            if (!useFocus)
            {
                return false;
            }
            else if (focusIndex < (HyperlinkList.Count - 1))
            {
                focusIndex++;
                return true;
            }
            else
            {
                useFocus = false;
                return false;
            }
        }

        override public int GetNumFocus()
        {
            return HyperlinkList.Count;
        }

        /*
        public static int CompareLinksbyStartIndex(Hyperlink x, Hyperlink y)
        {
            if (x.startIndex < y.startIndex)
            {
                return -1;
            }
            else if (x.startIndex > y.startIndex)
            {
                return 1;
            }
            else
            {
                return 0;
            }
        }
        */
 

    }
}