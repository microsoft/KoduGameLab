using System;
using System.Collections.Generic;
using System.Text;

using System.Xml;
using System.Xml.Serialization;
using System.Diagnostics;

#if NETFX_CORE
#else
    using System.Data;
#endif

using System.IO;

using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Audio;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using Microsoft.Xna.Framework.Storage;

using Boku.Base;
using Boku.Common;
using Boku.UI2D;

#if false //replaced all this by parsing JSON in GetFeedList. 

namespace Boku
{
    public class ParserMSFeed
    {
        private enum FeedSection
        {
            keyNone,
            keyTitle,
            keyText,
            keyLink,
            keyDate,
            keyTwitter,
        };

        /// <summary>
        ///  Keys for custom parsing on news feeds
        /// </summary>
        private string keyTitle ="Title";
        private string keyText ="Text";
        private string keyLink ="URL";
        private string keyDate ="Date";
        private string keyTwitter ="TwitterMessage";
        private FeedSection currentFeedSection;
       
        private DataSet rssData = new DataSet();
        public List<String> ParseItems(string rawData)
        {
            List<String> items = new List<string>();
            char[] itemDelimiterChars = { '{','}' };
            string[] sections = rawData.Split(itemDelimiterChars);

            foreach (string item in sections)  // for each entry
            {
                if (item.Length > 2)
                {
                    items.Add(item);
                }
            }
            return items;
        }


        public bool AddFeed(string rawData, ref List<FeedMs> msFeed, int width, Shared.GetFont tFont, Shared.GetFont dFont, Shared.GetFont bFont)
        {
            char lastChar;
            char nextChar;

            TextBlobHL TextBlobHL = new TextBlobHL(bFont,"label",width);
            FeedMs feedItem = null;
            
            currentFeedSection =  FeedSection.keyNone;
            if (rawData.StartsWith("\"" + keyTitle + "\""))
            {
                feedItem = new FeedMs(new Vector2(24, 0), TextBlobHL, tFont, dFont, bFont);
                string[] delimiterChars = { "\":\"", "\",\"" };
                string[] fields = rawData.Split(delimiterChars, StringSplitOptions.None);
                foreach (string field in fields)
                {
                    nextChar = field[field.Length-1];
                    if ( currentFeedSection != FeedSection.keyNone )
                    {
                        ProcessKey(ref feedItem, field, currentFeedSection);
                        currentFeedSection =  FeedSection.keyNone;
                    }
                    else if ( field.Contains( "\""+keyTitle ) )
                    {
                        currentFeedSection = FeedSection.keyTitle;
                    }
                    else if ( field.Contains( keyText ) )
                    {
                        currentFeedSection = FeedSection.keyText;
                    }
                    else if ( field.Contains( keyLink ) )
                    {
                        currentFeedSection = FeedSection.keyLink;
                    }
                    else if ( field.Contains( keyTwitter) )
                    {                            
                        currentFeedSection = FeedSection.keyTwitter;
                    }
                    else if ( field.Contains( keyDate ) )
                    {
                        currentFeedSection = FeedSection.keyDate;
                    }                        
                    lastChar = field[0];
                }
                msFeed.Add(feedItem);
            }

          
            return true;
        }

        private void ProcessKey(ref FeedMs newsItem, string data, FeedSection key)
        {
            switch (key)
            {
                case FeedSection.keyTitle:
                    newsItem.Title = data;
                    ; break;
                case FeedSection.keyText:
                    newsItem.Body = data; 
                    break;
                case FeedSection.keyDate:
                    newsItem.DateString = data; 
                    break;
                case FeedSection.keyTwitter: ; 
                    break;
                case FeedSection.keyLink:
                    newsItem.CreateHyperlink(HyperlinkType.URL, "\nRead more Here!", data); 
                    break;
            }
        }
    }
}
#endif
