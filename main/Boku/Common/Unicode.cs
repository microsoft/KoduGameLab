
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Globalization;
using System.Text;

namespace Boku.Common
{
    /// <summary>
    /// Static class with helper functions for dealing with Unicode characters and bidi processing.
    /// </summary>
    public static class Unicode
    {
        #region Members

        public class UnicodeCharData
        {
            public char c = char.MinValue;
            public string name;
            public string type;                 // The bidi category for this character.
            public string decompositionMapping; // We use this for Arabic contextual forms.
            public char baseForm;               // If this character is a contextual form, this is it's base form.
            public bool mirrored;               // Is this character mirrored?
            public char mirroredChar;           // If so, here's its mirror.
        }

        /// <summary>
        /// Data needed for resolving contextual forms
        /// </summary>
        public class DecompMappingData
        {
            public char c;                      // Base character.

            public char isolated;               // How the character should appear when isolated.
            public char initial;                // How the character should appear at the beginning of a word.
            public char medial;                 // How the character should appear in the middle of a word.
            public char final;                  // How the character should appear at the end of a word.
        }

        /// <summary>
        /// Data needed for handling ligatures.
        /// </summary>
        public class LigatureData
        {
            public char c;                      // First character of ligature.
            public List<LigatureEntry> ligatures;
        }

        public class LigatureEntry : IComparable
        {
            public char[] chars;                // Rest of the characters that make up the ligature.
            public char joined;                 // Resulting character.

            #region IComparable Members

            // Note we're sorting these to get the longest ones first.
            public int CompareTo(object obj)
            {
                if (obj is LigatureEntry)
                {
                    LigatureEntry le = (LigatureEntry)obj;
                    return -chars.Length.CompareTo(le.chars.Length);
                }

                Debug.Assert(false, "Object is not a LigatureEntry");
                return 0;
            }

            #endregion
        }

        static Dictionary<char, UnicodeCharData> charDict = new Dictionary<char, UnicodeCharData>(65536);
        static Dictionary<char, char> mirrorDict = new Dictionary<char, char>();
        static Dictionary<char, DecompMappingData> decompMappingDict = new Dictionary<char, DecompMappingData>();
        static Dictionary<char, LigatureData> ligatureDict = new Dictionary<char, LigatureData>();

        #endregion

        #region Accessors
        #endregion

        #region Public

        public static void Init()
        {
            //
            // Mirroring data.
            //

            string[] mirrorData = Storage4.ReadAllLines(@"Content\Xml\Unicode\BidiMirroring.txt", StorageSource.TitleSpace);

            if (mirrorData != null)
            {
                for (int i = 0; i < mirrorData.Length; i++)
                {
                    if (!String.IsNullOrEmpty(mirrorData[i]) && !mirrorData[i].StartsWith("#"))
                    {
                        string[] fields = mirrorData[i].Split(new char[] { ';', '#' });
                        // Note, this will throw when it tries to parse the lines in the header.
                        // Feel free to ignore those lines.
                        try
                        {
                            int code = int.Parse(fields[0], NumberStyles.HexNumber);
                            int mirror = int.Parse(fields[1], NumberStyles.HexNumber);

                            mirrorDict.Add((char)code, (char)mirror);
                        }
                        catch
                        {
                        }
                    }
                }   // end of loop over each line.
            }

            string[] unicodeData = Storage4.ReadAllLines(@"Content\Xml\Unicode\UnicodeData.txt", StorageSource.TitleSpace);

            if (unicodeData != null)
            {
                for (int i = 0; i < unicodeData.Length; i++)
                {
                    string[] fields = unicodeData[i].Split(';');
                    int code = 0;

                    // If we throw trying to parse the code, just skip this glyph.
                    try
                    {
                        code = int.Parse(fields[0], NumberStyles.HexNumber);
                    }
                    catch
                    {
                        continue;
                    }

                    UnicodeCharData data = new UnicodeCharData();
                    data.c = (char)code;
                    data.name = fields[1];
                    data.type = fields[4];
                    data.decompositionMapping = fields[5];
                    data.mirrored = fields[9] == "Y";

                    // Ignore Arabic contextual forms for ligatures.  This was causing too much breakage.
                    if (data.name.Contains("LIGATURE") && data.name.Contains("ARABIC"))
                    {
                        continue;
                    }

                    if (data.decompositionMapping.StartsWith("<isolated>"))
                    {
                        // Parse may throw.  If it does, just ignore this line.
                        try
                        {
                            data.baseForm = (char)int.Parse(fields[5].Substring(fields[5].Length - 4), NumberStyles.HexNumber);

                            DecompMappingData dm = null;
                            decompMappingDict.TryGetValue(data.baseForm, out dm);
                            if (dm == null)
                            {
                                dm = new DecompMappingData();
                                decompMappingDict.Add(data.baseForm, dm);
                                dm.c = data.baseForm;
                            }
                            dm.isolated = data.c;
                        }
                        catch { }
                    }
                    else if (data.decompositionMapping.StartsWith("<initial>"))
                    {
                        // Parse may throw.  If it does, just ignore this line.
                        try
                        {
                            data.baseForm = (char)int.Parse(fields[5].Substring(fields[5].Length - 4), NumberStyles.HexNumber);

                            DecompMappingData dm = null;
                            decompMappingDict.TryGetValue(data.baseForm, out dm);
                            if (dm == null)
                            {
                                dm = new DecompMappingData();
                                decompMappingDict.Add(data.baseForm, dm);
                                dm.c = data.baseForm;
                            }
                            dm.initial = data.c;
                        }
                        catch { }
                    }
                    else if (data.decompositionMapping.StartsWith("<medial>"))
                    {
                        // Parse may throw.  If it does, just ignore this line.
                        try
                        {
                            data.baseForm = (char)int.Parse(fields[5].Substring(fields[5].Length - 4), NumberStyles.HexNumber);

                            DecompMappingData dm = null;
                            decompMappingDict.TryGetValue(data.baseForm, out dm);
                            if (dm == null)
                            {
                                dm = new DecompMappingData();
                                decompMappingDict.Add(data.baseForm, dm);
                                dm.c = data.baseForm;
                            }
                            dm.medial = data.c;
                        }
                        catch { }
                    }
                    else if (data.decompositionMapping.StartsWith("<final>"))
                    {
                        // Parse may throw.  If it does, just ignore this line.
                        try
                        {
                            data.baseForm = (char)int.Parse(fields[5].Substring(fields[5].Length - 4), NumberStyles.HexNumber);

                            DecompMappingData dm = null;
                            decompMappingDict.TryGetValue(data.baseForm, out dm);
                            if (dm == null)
                            {
                                dm = new DecompMappingData();
                                decompMappingDict.Add(data.baseForm, dm);
                                dm.c = data.baseForm;
                            }
                            dm.final = data.c;
                        }
                        catch { }
                    }

                    // Get ligature data from Decompostion Mapping field.  Note that we're filtering
                    // on only getting Arabic ligatures and ignoring other languages.  This is because
                    // it's too easy to find ligatures with no matching glyphs.
                    if (data.name.Contains("ARABIC LIGATURE") && !string.IsNullOrEmpty(data.decompositionMapping))
                    {
                        // HACK Finding too many missing characters so only look for the one required ligature.
                        if (data.c != 0xfefb)
                            continue;

                        // Parse out the chars that combine to form this ligature.
                        int index = data.decompositionMapping.LastIndexOf('>');
                        string[] args = data.decompositionMapping.Substring(index + 1).Trim().Split(' ');

                        // Parse may throw.  If it does, just ignore this ligature.
                        try
                        {
                            char c0 = (char)int.Parse(args[0], NumberStyles.HexNumber);
                            char[] chars = new char[args.Length - 1];
                            for (int c = 0; c < args.Length - 1; c++)
                            {
                                chars[c] = (char)int.Parse(args[c + 1], NumberStyles.HexNumber);
                            }

                            // Create or add to the ligature entry.  Note that the entry
                            // uses the first character of the pair for the key.
                            LigatureData ld = null;
                            ligatureDict.TryGetValue(c0, out ld);

                            // Create new dictionary entry if needed.
                            if (ld == null)
                            {
                                ld = new LigatureData();
                                ld.c = c0;
                                ld.ligatures = new List<LigatureEntry>();

                                ligatureDict.Add(c0, ld);
                            }
                            // Add new entry.
                            LigatureEntry le = new LigatureEntry();
                            le.chars = chars;
                            le.joined = data.c;
                            ld.ligatures.Add(le);
                        }
                        catch { }
                    }

                    if (data.mirrored)
                    {
                        mirrorDict.TryGetValue(data.c, out data.mirroredChar);
                    }

                    if (code >= 0 && code <= 0xFFFF)  //UTF-16 BMP code points only 
                    {
                        bool inRange = data.name.EndsWith(", First>");
                        if (inRange)   // Add all characters within a specified range.
                        {
                            data.name.Replace(", First", String.Empty);     // Remove range indicator from name .
                            fields = unicodeData[++i].Split(';');

                            // Parse may throw.  If it does, just ignore this range?
                            // Not totally sure if this is the right thing to do...
                            try
                            {
                                int endCharCode = int.Parse(fields[0], NumberStyles.HexNumber);

                                if (!fields[1].EndsWith(", Last>"))
                                {
                                    throw new Exception("Expected end-of-range indicator.");
                                }

                                for (int codeInRange = code; codeInRange <= endCharCode; codeInRange++)
                                {
                                    charDict.Add((char)codeInRange, data);
                                }
                            }
                            catch { }
                        }
                        else
                        {
                            charDict.Add((char)code, data);
                        }
                    }

                }   // end of loop over each line.
            }

            // For ligatures, we need to be greedy and try to apply longer conversions
            // before shorter ones.  So, sort all the ligatures in the dictionary by
            // the number of characters putting the longest first.
            foreach (LigatureData ld in ligatureDict.Values)
            {
                ld.ligatures.Sort();
            }

        }   // end of Init()

        public static Unicode.UnicodeCharData GetCharInfo(char c)
        {
            Debug.Assert(charDict != null, "Looks like we need to init this sooner.");

            Unicode.UnicodeCharData result = null;
            charDict.TryGetValue(c, out result);

            return result;
        }   // end of GetCharInfo()

        public static Unicode.LigatureData GetLigatureData(char c)
        {
            Debug.Assert(ligatureDict != null, "Looks like we need to init this sooner.");

            LigatureData ld = null;
            ligatureDict.TryGetValue(c, out ld);

            return ld;
        }   // end of GetLigatureData()

        public static Unicode.DecompMappingData GetDecompMappingData(char c)
        {
            Debug.Assert(decompMappingDict != null, "Looks like we need to init this sooner.");

            DecompMappingData dmd = null;
            decompMappingDict.TryGetValue(c, out dmd);

            return dmd;
        }   // end of GetDecompMappingData()

        #endregion

        #region Internal
        #endregion


    }   // end of class Unicode

}   // end of namespace Boku.Common
