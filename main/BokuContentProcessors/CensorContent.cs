using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Diagnostics;
using System.IO;

namespace Boku.Common
{
    // This chunk of the CensorContent class must match the copy in Boku/Common/Censor.cs
    // (We duplicate this portion of the class to avoid unnecessary full content rebuilds).
    public partial class CensorContent
    {
    //// The test program compiles both aspects of this class (sourcing from the BokuContentProcessors
    //// and Boku projects) so we must guard against duplicated members and methods.
#if !TEST
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
#endif
    }

    public partial class CensorContent
    {
        public class CensorContentFileEntry
        {
            public string MatchWord;
            public CensorContent.MatchType MatchType;
            public int LineNo;
        }

        public class CensorContentFile
        {
            public string Filename;
            public List<CensorContentFileEntry> Entries = new List<CensorContentFileEntry>();
        }

        public enum MatchType
        {
            None = -1,
            Contains,
            StartsWith,
            EndsWith,
            Exact,
        }

        int numSymbols;
        int numStates;
        int maxStates;

        const int MaxSymbols = 0x10000; // covers the Unicode Basic Multilingual Plane (BMP).

        public static CensorContentFile ReadSourceRepresentation(StreamReader reader, string filename, IBokuContentBuildLogger logger)
        {
            CensorContentFile file = new CensorContentFile();
            file.Filename = Path.GetFullPath(filename);
            
            int lineno = 0;

            char[] delimChars = new char[] { ',', ';' };
            while (!reader.EndOfStream)
            {
                string line = reader.ReadLine();
                lineno += 1;

                if (String.IsNullOrEmpty(line))
                    continue;

                string[] values = line.Split(delimChars, StringSplitOptions.RemoveEmptyEntries);

                if (values.Length < 2)
                    continue;

                if (values[0] == "#" || values[0] == ";")
                    continue;

                MatchType MatchType = MatchType.None;

                try { MatchType = (MatchType)Enum.Parse(typeof(MatchType), values[1]); }
                catch { }

                if (MatchType == MatchType.None)
                {
                    logger.LogMessage("Syntax error on line {0}: {1}", lineno, line);
                    continue;
                }

                CensorContentFileEntry entry = new CensorContentFileEntry();
                entry.MatchWord = values[0].ToUpper();
                entry.MatchType = MatchType;
                entry.LineNo = lineno;

                file.Entries.Add(entry);
            }

            return file;
        }

        public void WriteBinaryRepresentation(BinaryWriter writer)
        {
            writer.Write((Int32)numSymbols);
            writer.Write((Int32)maxStates);
            for (int i = 0; i < numSymbols; ++i)
                for (int j = 0; j < maxStates; ++j)
                    writer.Write((Int32)transitionTable[i, j]);

            writer.Write((Int32)lookupTable.Length);
            for (int i = 0; i < lookupTable.Length; ++i)
                writer.Write((Char)lookupTable[i]);
        }

        public void Compile(CensorContentFile input, IBokuContentBuildLogger logger)
        {
            numSymbols = 0;
            numStates = 0;
            maxStates = 0;

            // Build the lookup table.
            lookupTable = new char[MaxSymbols];
            foreach (CensorContentFileEntry entry in input.Entries)
                AddToLookupTable(entry);

            // Build the transition table.
            transitionTable = new int[numSymbols, maxStates];
            foreach (CensorContentFileEntry entry in input.Entries)
                AddToTransitionTable(entry);
        }

        private void AddToLookupTable(CensorContentFileEntry entry)
        {
            string value = entry.MatchWord;

            int strlen = 0;
            for (int i = 0; i < value.Length; ++i)
            {
                char c = TranslateChar(value[i]);

                strlen += 1;
                if (lookupTable[c] == 0)
                {
                    numSymbols += 1;
                    lookupTable[c] = (char)numSymbols;
                }
            }

            maxStates += strlen;
        }

        private void AddToTransitionTable(CensorContentFileEntry entry)
        {
            string value = entry.MatchWord;

            int currState = 0;

            for (int i = 0; i < value.Length; ++i)
            {
                char c = TranslateChar(value[i]);

                int iSymbol = lookupTable[c];

                // The symbol table is one-based, but indexes into the transition table are
                // zero-based, so we just subtract one from the symbol to get the index.
                iSymbol -= 1;

                // Each cell encodes transition information from the current state to the next.
                int nextState = transitionTable[iSymbol, currState];

                if (i < value.Length - 1)
                {
                    //// We are not yet at the last character in the word.

                    if ((nextState & MatchMask) != 0)
                    {
                        // If we detect a terminus on our transition path, then we have a sequence collision.
                        // Non fatal, but should be corrected in the dictionary file.
                        //Console.WriteLine(String.Format("{0}:({1},1): Another censor sequence masks this one: {2}, {3}.", filename, entry.LineNo, entry.MatchWord, entry.MatchType));
                    }

                    if ((nextState & StateMask) != 0)
                    {
                        // The next transition in this sequence has already been set by an
                        // earlier encoding, so just continue the traversal.
                        currState = nextState & StateMask;
                    }
                    else
                    {
                        // Add a new transition to this sequence.
                        numStates += 1;
                        transitionTable[iSymbol, currState] |= numStates;
                        currState = numStates;
                    }
                }
                else
                {
                    //// We are now at the last character in the word. Encode the terminus bits.

                    if (entry.MatchType == MatchType.Contains)
                        transitionTable[iSymbol, currState] |= ContainsMatch;
                    if (entry.MatchType == MatchType.StartsWith)
                        transitionTable[iSymbol, currState] |= StartsWithMatch;
                    if (entry.MatchType == MatchType.EndsWith)
                        transitionTable[iSymbol, currState] |= EndsWithMatch;
                    if (entry.MatchType == MatchType.Exact)
                        transitionTable[iSymbol, currState] |= (StartsWithMatch | EndsWithMatch);
                }
            }
        }
    }
}

