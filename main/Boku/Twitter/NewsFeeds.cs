using System;
using System.Collections.Generic;
using System.Text;

using System.IO;
using System.Net;
using System.Threading;

using System.Xml;
using System.Xml.Serialization;
using System.Diagnostics;

#if NETFX_CORE
    using Windows.Data;
    using Windows.Web;
    using System.Runtime.Serialization.Json;
#else
    using System.Data;
    using System.Web;
#endif

using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Audio;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using Microsoft.Xna.Framework.Storage;

using Boku.Base;
using Boku.Common;
using Boku.UI2D;
using Boku.Fx;
using Boku.Common.Localization;


namespace Boku
{
    public class NewsFeeds
    {
        public enum OpState
        {
            Idle,
            Retrieving,
            Retrieved,
            Failed
        }

        private string rawGetData = "";
        private int feedFetchCount = 20;

        private float GET_TIMEOUT_SECS = 20.0f;

        private OpState currentState = OpState.Idle;
        private IAsyncResult getFeedResult = null;
        private double opStartTime = 0;
        
        /// <summary>
        ///  unprocesses string data representing each news item
        /// </summary>
        //private List<String> rawFeedList = null;

        /// <summary>
        /// List of FeedMs (ScrollContainer's) items
        /// </summary>
        //private List<FeedMs> newsClips = null;

        public OpState CurrentState
        {
            get { return currentState; }
        }

        public static void Init()
        {

        }

        
        //static bool getFeedComplete = false;
        public void BeginFetchNews()
        {
            try
            {
                //getFeedComplete = false;
                int itemCount = feedFetchCount;
                string baseUrl =  Program2.SiteOptions.KGLUrl + "/API/GetLatestNews?tag=client";
                string paramUrl = "&region=" + GetLangRegion();
#if !NETFX_CORE
                // TODO (****) This doesn't do anything anyway.  Should it be removed?
                string paramRegionUrl = Thread.CurrentThread.CurrentCulture.Name;

                paramRegionUrl = "-" + paramRegionUrl;
#endif
                Uri feedUri = CreateUpdateFeedURI(baseUrl + paramUrl, itemCount);
                opStartTime = Time.GameTimeTotalSeconds + GET_TIMEOUT_SECS;
                currentState = OpState.Retrieving;
                RequestFeed(feedUri.ToString());

/*                int timeSpent = 0; // opStartTime = 0;GET_TIMEOUT_SECS
                if ( RequestFeed( feedUri.ToString() ) )
                {
                    while (!getFeedComplete && timeSpent < 30 * 1000)
                    {
                        System.Threading.Thread.Sleep(10);
                        timeSpent += 10;
                    }
                }
 */
            }
            catch
            {
                rawGetData = "";
                currentState = OpState.Failed;
                //getFeedComplete = true;
            }
        }

        private string GetLangRegion()
        {
            string paramLang = Localizer.LocalLanguage;
            string paramRegion = "en-US";

#if NETFX_CORE
            var preferredLanguages = Windows.Globalization.ApplicationLanguages.Languages;
            if (preferredLanguages.Count > 0)
            {
                paramRegion = preferredLanguages[0];
            }

            // If no legit region is found, use a default.
            if (string.IsNullOrEmpty(paramRegion))
            {
                paramRegion = "en-US";
            }
#else
            paramRegion = Thread.CurrentThread.CurrentCulture.Name; // Should get "en-US" for local starts...
#endif

            if (BokuSettings.Settings.Language != null && BokuSettings.Settings.Language.Length == 2)
            {
                paramLang = BokuSettings.Settings.Language;
                paramRegion = paramLang;
            }
/*          // combine known boku language with systems cultureinfo region
            else
            {
                try
                {
                    int pos = paramRegion.IndexOf("-");
                    if (pos > 0 && (pos < paramRegion.Length - 1))
                    {
                        paramRegion = paramRegion.Substring(pos + 1, paramRegion.Length - pos - 1);
                    }
                    else
                    {
                        paramRegion = "US";
                    }
                }
                catch
                {
                    paramRegion = "US";
                }
                paramRegion = paramLang + "-" + paramRegion;
            }
*/

            return paramRegion;
        }

        public Uri CreateUpdateFeedURI(string baseUrl, int items)
        {
            int num = items;
            // &count=2&include_entities=true
            string htmlURL = baseUrl;// +"&count=" + items;
            Uri uri = new Uri(htmlURL);
            return uri; 
        }

        bool RequestFeed(string url)
        {
            bool sent = false;
            try
            {
                HttpWebRequest request =
                    (HttpWebRequest)HttpWebRequest.Create(new Uri(url));

                request.ContentType = "application/json";
                request.BeginGetResponse(new AsyncCallback(ReadFeedCallback), request);
                
                sent = true;                
            }
            catch (Exception ex)
            {
                string failedMsg = ex.Message;
            }
            return sent;
        }

        private void ReadFeedCallback(IAsyncResult asynchronousResult)
        {
            try
            {
                getFeedResult = asynchronousResult;
                HttpWebRequest request =
                  (HttpWebRequest)asynchronousResult.AsyncState;
                HttpWebResponse response =
                  (HttpWebResponse)request.EndGetResponse(asynchronousResult);

                using (StreamReader streamReader1 =
                  new StreamReader(response.GetResponseStream()))
                {
                    string resultString = streamReader1.ReadToEnd();
                    //Handle single quote. 
                    //Not sure why this is needed since quotes are handled correctly.
                    resultString = resultString.Replace("\\u0027", "'");
                    rawGetData = resultString;

                    //ParserMSFeed feedParser = new ParserMSFeed();
                    //rawFeedList = feedParser.ParseItems(rawGetData);
                    currentState = OpState.Retrieved;
                    //getFeedComplete = true;
                }
            }
            catch (Exception)
            {
                currentState = OpState.Failed;
            }

        }

        public List<FeedMs> GetFeedList(int width, Shared.GetFont titleFont, Shared.GetFont dateFont, Shared.GetFont bodyFont)
        {
            TextBlob textBlob = new TextBlob(bodyFont, "label", width);

            //parse JSON string into List of Dictionaries.
            List<FeedMs> allFeeds = new List<FeedMs>();
            try
            {
#if NETFX_CORE
                // For some reason the WinRT Json Serializer doesn't want to 
                // deserialize our objects so we'll just have to do it manually.
                //var items = Deserialize<List<Dictionary<string, string>>>(rawGetData);

                List<Dictionary<string, string>> items = HackDeserialize(rawGetData);
#else
                var js = new System.Web.Script.Serialization.JavaScriptSerializer();
                var items = js.Deserialize<List<Dictionary<string, string>>>(rawGetData);
#endif

                //build news feed.
                foreach (var item in items)
                {
                    FeedMs feedItem = new FeedMs(new Vector2(24, 0), textBlob, titleFont, dateFont, bodyFont);
                    feedItem.Title = item["Title"];
                    feedItem.Body = item["Text"];
                    feedItem.DateString = item["Date"];
                    feedItem.CreateHyperlink(HyperlinkType.URL, Strings.Localize("mainMenu.readMoreHere"), item["URL"]);
                    allFeeds.Add(feedItem);
                }
            }
            catch { }
            return allFeeds;
        }

#if NETFX_CORE
        T Deserialize<T>(string json)
        {
            var bytes = Encoding.Unicode.GetBytes(json);
            using (MemoryStream stream = new MemoryStream(bytes))
            {
                var serializer = new DataContractJsonSerializer(typeof(T));
                return (T)serializer.ReadObject(stream);
            }
        }

        List<Dictionary<string, string>> HackDeserialize(string data)
        {
            List<Dictionary<string, string>> items = new List<Dictionary<string, string>>();

            int prevItemIndex = 0;
            while (true)
            {
                int openBraceIndex = data.IndexOf('{', prevItemIndex);
                if (openBraceIndex == -1)
                {
                    break;
                }
                int closeBraceIndex = data.IndexOf('}', openBraceIndex);

                prevItemIndex = closeBraceIndex;

                items.Add(ParseItem(data.Substring(openBraceIndex + 1, closeBraceIndex - openBraceIndex - 1)));
            }

            return items;
        }

        Dictionary<string, string> ParseItem(string data)
        {
            Dictionary<string, string> item = new Dictionary<string, string>();

            int prevIndex = 0;
            while (true)
            {
                int q0 = NextQuote(data, prevIndex);
                if (q0 == -1)
                {
                    break;
                }
                int q1 = NextQuote(data, q0 + 1);
                if (q1 == -1)
                {
                    break;
                }
                int q2 = NextQuote(data, q1 + 1);
                if (q2 == -1)
                {
                    break;
                }
                int q3 = NextQuote(data, q2 + 1);
                if (q3 == -1)
                {
                    break;
                }

                prevIndex = q3 + 1;

                string key = data.Substring(q0 + 1, q1 - q0 - 1);
                string value = data.Substring(q2 + 1, q3 - q2 - 1);
                item.Add(key, value);
            }

            return item;
        }

        /// <summary>
        /// Finds the next doublke quote in the string but
        /// skips over escaped quotes, eg \"
        /// </summary>
        /// <param name="data"></param>
        /// <param name="startIndex"></param>
        /// <returns></returns>
        int NextQuote(string data, int startIndex)
        {
            int index = -1;

            while (true)
            {
                index = data.IndexOf('"', startIndex);
                // If quote not found or found at first character or found and is not escaped.
                if (index < 1 ||  (index > 0 && data[index - 1] != '\\'))
                {
                    break;
                }
                startIndex = index + 1;
            }

            return index;
        }
#endif


/*
        /// <summary>
        /// Once the tweets have been retrieved, this will return the list of tweets as Tweet
        /// objects with clickable hit boxes.
        /// </summary>
        /// <param name="font"></param>
        /// <param name="width"></param>
        /// <returns></returns>
        public static List<Tweet> GetTweetList(Boku.UI2D.Shared.GetFont font, int width)
        {
            if (newsClips == null)
            {
                return null;
            }

            List<Tweet> tweetList = new List<Tweet>();
            //foreach (TwitterStatus twitterStatus in newsClips)
            foreach (TwitterStatus twitterStatus in newsClips)
            {
                TextBlob tweetBlob = new TextBlob(font, twitterStatus.Text, width, TextBlob.BlobType.Simple);
                tweetBlob.FlowText();

                Tweet newTweet = new Tweet(new Vector2(tweetBlob.Width, 0), tweetBlob);

                IList<TwitterHashTag> hashes = twitterStatus.Entities.HashTags;
                foreach (var hash in hashes)
                {
                    newTweet.CreateHyperlink(
                        HyperlinkType.HashTag,
                        hash.StartIndex,
                        hash.EndIndex);
                }
                IList<TwitterMention> mentions = twitterStatus.Entities.Mentions;
                foreach (var mention in mentions)
                {
                    newTweet.CreateHyperlink(
                        HyperlinkType.Mention,
                        mention.StartIndex,
                        mention.EndIndex);
                }
                IList<TwitterUrl> urls = twitterStatus.Entities.Urls;
                foreach (var url in urls)
                {
                    newTweet.CreateHyperlink(
                        HyperlinkType.URL,
                        url.StartIndex,
                        url.EndIndex);
                }

                tweetList.Add(newTweet);
            }

            return tweetList;
        }
*/
        public void Update()
        {
            if (currentState != OpState.Retrieving) 
            {
                return; 
            }

/*            if ( getFeedResult.IsCompleted )
            {
                if (newsClips == null)
                {
                    currentState = OpState.Failed;
                }
                else
                {
                    currentState = OpState.Retrieved;
                }

            }
    
            //  newsClips = listCaller.EndInvoke(getFeedsResult);
            else if(Time.GameTimeTotalSeconds > opStartTime)
            {
                //Console.WriteLine("Tweet get failed. Operation timed out.");
                currentState = OpState.Failed;
                getFeedResult = null;
            }
 */
        }
    }
}