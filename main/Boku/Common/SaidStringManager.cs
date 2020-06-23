
using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Xml.Serialization;

using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Audio;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using Microsoft.Xna.Framework.Storage;

using Boku.Base;

namespace Boku.Common
{
    /// <summary>
    /// Static class which keeps track of strings "said" by the actors this frame.
    /// 
    /// When an actor "says" a line of text displayed in a thought balloon it will
    /// actually get put into the list twice:  once at the beginning of the thought
    /// balloon's life and once at the end.  The "said" filter can then look for either.
    /// 
    /// When this is used for messaging via the <tag > construct, it is assumed to always
    /// be at the beginning.
    /// </summary>
    public class SaidStringManager
    {
        public class SaidStringEntry
        {
            public GameActor actor = null;      // The actor that spoke.
            public string text = null;          // This text that was spoken.
            public bool atBeginning = true;     // True if this entry is from the beginning of the thought balloon's life.
                                                // False if at the end of the thought balloon's life.

            public SaidStringEntry(GameActor actor, string text, bool atBeginning)
            {
                this.actor = actor;
                this.text = TextHelper.WhitespaceCompress(text);
                this.atBeginning = atBeginning;
            }
        }

        // List of strings for this frame.  This is the list that we are building this frame
        // which contains all the stirngs spoken this frame.
        static List<SaidStringEntry> curFrameEntries = new List<SaidStringEntry>();

        // List for string for the previous frame.  This is the list that the "said" filter is testing against.
        static List<SaidStringEntry> prevFrameEntries = new List<SaidStringEntry>();

        public static void Update()
        {
            // Swap and clear cur.
            List<SaidStringEntry> tmp = curFrameEntries;
            curFrameEntries = prevFrameEntries;
            prevFrameEntries = tmp;
            curFrameEntries.Clear();

        }   // end of Update()

        public static void AddEntry(GameActor actor, string text, bool atBeginning)
        {
            SaidStringEntry entry = new SaidStringEntry(actor, text, atBeginning);
            curFrameEntries.Add(entry);
        }   // end of AddEntry()

        /// <summary>
        /// Looks for the target text as a substring of the entries for this frame.
        /// </summary>
        /// <param name="thinker">bot which is sending message</param>
        /// <param name="targetText">substring to look for</param>
        /// <param name="atBeginning">math atBeginning or atEnd?</param>
        /// <returns></returns>
        public static bool MatchText(GameActor thinker, string targetText, bool atBeginning)
        {
            bool result = false;
            string txt = TextHelper.WhitespaceCompress(targetText);

            for (int i = 0; i < prevFrameEntries.Count; i++)
            {
                if (prevFrameEntries[i].actor == thinker && prevFrameEntries[i].atBeginning == atBeginning && prevFrameEntries[i].text.Contains(txt))
                {
                    result = true;
                    break;
                }
            }

            return result;
        }   // end of MatchText()

    }   // end of class SaidStringManager

}   // end of namespace Boku.Common
