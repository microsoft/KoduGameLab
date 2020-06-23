using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;
using System.IO;

using Microsoft.Xna.Framework.Content;

namespace Boku.Common
{
#if !TEST

    #region Public

    /// <summary>
    /// Provides a mechanism for marking out words specified as forbidden as well as automatically detecting common variants thereof. 
    /// </summary>
    public static class Censor
    {
        #region Public

        /// <summary>
        /// Checks the input string for forbidden terms. If any are found, then the output string contains a copy of the input with the
        /// terms marked out with asterisks. If no terms were found, then the output string is unchanged.
        /// </summary>
        /// <param name="input">The string to be checked.</param>
        /// <param name="output">If any terms were found, contains the marked-out version of the input string.
        /// Otherwise output string is unchanged.</param>
        /// <returns>Returns true if any terms were found.</returns>
        public static bool Scrub(string input, ref string output)
        {
            if (input == null)
            {
                return false;
            }
            else
            {
                return CensorContent.Scrub(input, ref output);
            }
        }

        #endregion Public

        #region Private

        static CensorContent _CensorContent = null;
        static CensorContent CensorContent
        {
            get 
            {
                if (_CensorContent == null)
                {
                    _CensorContent = ContentLoader.ContentManager.Load<CensorContent>(BokuGame.Settings.MediaPath + @"Text\Censor\Profanity");
                }
                return _CensorContent; 
            }
        }

        #endregion Private
    }

    #endregion Public

    #region Internal

    public class CensorContentReader : ContentTypeReader<CensorContent>
    {
        protected override CensorContent Read(ContentReader input, CensorContent existingInstance)
        {
            CensorContent censorContent = new CensorContent();
            censorContent.ReadBinaryRepresentation(input);
            return censorContent;
        }
    }

    #endregion Internal

#endif


    #region Internal

    // This chunk of the CensorContent class must match the copy in BokuContentProcessors/CensorContent.cs
    // (We duplicate this portion of the class to avoid unnecessary full content rebuilds).
    public partial class CensorContent
    {
        const int ContainsMatch = 1 << 31;
        const int StartsWithMatch = 1 << 30;
        const int EndsWithMatch = 1 << 29;
        const int MatchMask = ContainsMatch | StartsWithMatch | EndsWithMatch;
        const int StateMask = ~MatchMask;

        char[] lookupTable;
        int[,] transitionTable;

        private static char TranslateChar(char c)
        {
            switch (c)
            {
                /*
                case '0': return 'O';
                case '1': return 'I';
                case '2': return 'S';
                case '3': return 'E';
                case '4': return 'A';
                case '7': return 'L';
                */
                default: return Char.ToUpper(c);
            }
        }
    }

    /// <summary>
    /// Application-side-only chunk of the Censor class. This piece does the work of finding and
    /// censoring words in a string.
    /// </summary>
    public partial class CensorContent
    {
        public void ReadBinaryRepresentation(BinaryReader reader)
        {
            int numSymbols = reader.ReadInt32();
            int maxStates = reader.ReadInt32();
            transitionTable = new int[numSymbols, maxStates];
            for (int i = 0; i < numSymbols; ++i)
                for (int j = 0; j < maxStates; ++j)
                    transitionTable[i, j] = reader.ReadInt32();

            int lookupTableLength = reader.ReadInt32();
            lookupTable = new char[lookupTableLength];
            reader.Read(lookupTable, 0, lookupTableLength);
        }

        public bool Scrub(string input, ref string output)
        {
            bool result = false;

            result |= ScrubInternal(input, ref output, false);
            result |= ScrubInternal(output ?? input, ref output, true);

            return result;
        }

        private bool ScrubInternal(string input, ref string output, bool coalesce)
        {
            if (Scrub_working.Length < input.Length)
                Scrub_working = new char[input.Length];
            input.CopyTo(0, Scrub_working, 0, input.Length);

            int matchCount = 0;

            // When coasting is true, the start index will be remembered once we
            // find a cell with transition state, marking the beginning of a sequence.
            bool coast = true;

            int start = 0;
            int currState = 0;
            char currChar = ' ';

            for (int i = 0; i < input.Length; ++i)
            {
                char c = TranslateChar(Scrub_working[i]);

                // Skip non-alphanumeric chars
                if (ShouldIgnoreChar(c))
                    continue;

                int iSymbol = lookupTable[c];
                if (iSymbol == 0)
                {
                    // Symbol does not appear in any sequence, reset the traversal.
                    currState = 0;
                    start = i;
                    coast = true;
                    continue;
                }

                // Symbols are one-based (a symbol of zero has a special meaning) but we index
                // into the transition table from zero, so subtract one to get the index value.
                iSymbol -= 1;

                int nextState = transitionTable[iSymbol, currState & StateMask];

                if (coalesce && c == currChar && nextState == 0)
                {
                    // Optionally coalesce repeating characters, unless the next character is in the sequence.
                    continue;
                }
                currChar = c;

                int prevState = currState;
                currState = nextState;

                if (prevState != 0 && currState == 0)
                {
                    // This can occur when sequences overlap. Reset the traversal on the current position.
                    currState = transitionTable[iSymbol, 0];
                    start = i;
                    coast = true;
                }

                if (coast && (currState & StateMask) != 0)
                {
                    // If we're coasting and we see transition info, then it looks like we've hit
                    // the start of a sequence, so remember this position start the start of it.
                    start = i;
                    coast = false;
                }
                else if (currState == 0)
                {
                    // No sequence here, continue floating forward.
                    start = i;
                    coast = true;
                    continue;
                }

                if ((currState & MatchMask) == 0)
                {
                    // This is not a terminus, continue traversing.
                    continue;
                }

                //// From here on we're working with a matched sequence.

                if ((currState & StartsWithMatch) != 0)
                {
                    // Ensure the matched sequence is at the beginning of a word.
                    if (start > 0 && !IsWhiteSpace(Scrub_working[start - 1]))
                        continue;
                }

                if ((currState & EndsWithMatch) != 0)
                {
                    if (coalesce)
                    {
                        // If coalescing, skip past chars matching the terminus.
                        while (i < (input.Length - 1) && Char.ToUpper(Scrub_working[i + 1]) == currChar)
                            i += 1;
                    }

                    // Ensure the matched sequence is at the end of a word.
                    if (i < (input.Length - 1) && !IsWhiteSpace(Scrub_working[i + 1]))
                        continue;
                }

                // Expand the match to encompass the whole word.
                while (start > 0 && !IsWhiteSpace(Scrub_working[start - 1]))
                    start -= 1;
                while (i < (input.Length - 1) && !IsWhiteSpace(Scrub_working[i + 1]))
                    i += 1;

                if (!coalesce || (coalesce && (i - start) > 2))
                {
                    // Replace matched sequence with * characters.
                    MarkOutText(Scrub_working, start, i);
                    matchCount += 1;
                }

                // Start matching the next sequence.
                currState = 0;
                start = i;
                coast = true;
            }

            if (matchCount > 0)
                output = new String(Scrub_working, 0, input.Length);

            return (matchCount > 0);
        }
        private char[] Scrub_working = new char[0];

        private static bool ShouldIgnoreChar(char c)
        {
            return !Char.IsLetterOrDigit(c);
        }

        private static bool IsWhiteSpace(char c)
        {
            return (Char.IsWhiteSpace(c) || Char.IsPunctuation(c));
        }

        private static void MarkOutText(char[] arr, int startIndex, int endIndex)
        {
            while (startIndex <= endIndex)
            {
                if (!IsWhiteSpace(arr[startIndex]))
                {
                    arr[startIndex] = '*';
                }
                startIndex += 1;
            }
        }
    }

    #endregion Internal
}
