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
using Boku.Common.Gesture;
using Boku.Fx;
using Boku.UI2D;

namespace Boku
{
    public class Tweet : ScrollContainer
    {
        private static Color defaultColor = Color.Cyan;
        private static Color hoverColor = Color.YellowGreen;
        private static Color clickColor = Color.Orange;

        private List<Hyperlink> hyperlinkList = new List<Hyperlink>();
        private TextBlob tweetBlob = null;
        private Vector2 drawPos = Vector2.Zero;
        private int focusIndex = 0;
        private bool useFocus = false;

        public List<Hyperlink> HyperlinkList
        {
            get { return hyperlinkList; }
        }

        public Tweet(Vector2 size, TextBlob tweetBlob)
          : base(size)
        {
            this.tweetBlob = tweetBlob;

            size.Y = (tweetBlob.NumLines - 1) * tweetBlob.TotalSpacing;
            size.Y += UI2D.Shared.GetGameFont13_5().LineSpacing;
            base.Height = size.Y;
        }

        public Tweet(Vector2 size, string text)
          : base(size)
        {
            tweetBlob = new TextBlob(UI2D.Shared.GetGameFont13_5, text, 150);
            tweetBlob.Width = (int)size.X;
            
            size.Y = (tweetBlob.NumLines - 1) * tweetBlob.TotalSpacing;
            size.Y += UI2D.Shared.GetGameFont13_5().LineSpacing;
            base.Height = size.Y;
        }

        public void CreateHyperlink(HyperlinkType linkType, int startIndex, int endIndex)
        {
            List<TextSegment> segmentList = new List<TextSegment>();
            String linkText = tweetBlob.RawText.Substring(startIndex, endIndex - startIndex);
            tweetBlob.CreateSubstringSegments(startIndex, endIndex, ref segmentList);

            if (segmentList.Count > 0)
            {
                Hyperlink newLink = new Hyperlink(linkText, segmentList, linkType, startIndex, endIndex);
                hyperlinkList.Add(newLink);
            }

            // Sort the list after the new entry
            hyperlinkList.Sort(CompareLinksbyStartIndex);
        }

        override public void ResetWidth()
        {
            tweetBlob.Width = (int)Width;
            tweetBlob.FlowText();

            // Update the segments for all hyperlinks.
            foreach (Hyperlink link in HyperlinkList)
            {
                List<TextSegment> segmentList = new List<TextSegment>();
                tweetBlob.CreateSubstringSegments(link.startIndex, link.endIndex, ref segmentList);
                if (segmentList.Count > 0)
                {
                    link.segmentList = segmentList;
                }
            }

            float h = (tweetBlob.NumLines - 1) * tweetBlob.TotalSpacing;
            h += UI2D.Shared.GetGameFont13_5().LineSpacing;
            Height = h;
        }

        override public void Render(Vector2 pos)
        {
            base.Render(pos);
            drawPos = pos;

            tweetBlob.RenderWithButtons(
                drawPos,
                Color.White,
                false,
                UIGridElement.Justification.Left);

            SpriteBatch batch = UI2D.Shared.SpriteBatch;
            batch.Begin();

            foreach (Hyperlink link in hyperlinkList)
            {
                if (useFocus)
                {
                    if (link == hyperlinkList[focusIndex])
                    {
                        link.drawColor = hoverColor;
                    }
                }
                foreach (TextSegment segment in link.segmentList)
                {
                    batch.DrawString(
                        tweetBlob.Font(),
                        segment.Text,
                        drawPos + segment.HitBox.Min,
                        link.drawColor);
                }
                // Always reset the draw color after rendering. This ensures that
                // only items which were changed this frame have a different color.
                link.drawColor = defaultColor;
            }

            batch.End();
        }

        override public void Press()
        {
            if (useFocus)
            {
                if (focusIndex < HyperlinkList.Count)
                {
                    string urlString = CreateURLFromLink(HyperlinkList[focusIndex]);
                    Process.Start(urlString);
                }
            }
        }

        override public void Hover(Vector2 pos)
        {
            // Get a position that is local to the tweet's textblob
            Vector2 localPos = pos - drawPos;

            foreach (Hyperlink link in HyperlinkList)
            {
                if (link.HitTest(localPos, ClickType.None))
                {
                    link.drawColor = hoverColor;
                }
            }
        }

        override public bool Click(Vector2 pos, out Object obj, ClickType clickType)
        {
            // Get a position that is local to the tweet's textblob
            Vector2 localPos = pos - drawPos;

            foreach (Hyperlink link in HyperlinkList)
            {
                if (link.HitTest(localPos, clickType))
                {
                    if (link.wasClicked)
                    {
                        string urlString = CreateURLFromLink(link);
                        obj = urlString as Object;
                        Process.Start(urlString);
                        return true;
                    }
                    else if (link.isClickFocus)
                    {
                        link.drawColor = clickColor;
                    }
                    else
                    {
                        link.drawColor = hoverColor;
                    }
                }
            }
            obj = null;
            return false;
        }

        public string CreateURLFromLink(Hyperlink link)
        {
            if (link.linkType == HyperlinkType.Mention)
            {
                return "https://www.twitter.com/#!/" + link.linkText.Substring(1);
            }
            else if (link.linkType == HyperlinkType.URL)
            {
                return link.linkText;
            }
            else if (link.linkType == HyperlinkType.HashTag)
            {
                return "https://twitter.com/#!/search/" + link.linkText.Substring(1);
            }
            // Unknown type.
            return link.linkText;
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
    }
}
