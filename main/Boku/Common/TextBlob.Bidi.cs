// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.


using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Text;

namespace Boku.Common
{
    public partial class TextBlob
    {
        /// <summary>
        /// Static class for handling BiDi text formatting.
        /// </summary>
        public class BiDi
        {
            /// <summary>
            /// Directional override status.
            /// </summary>
            protected enum OverrideStatus
            {
                Neutral,        // None.
                RightToLeft,    // Forcing RtoL.
                LeftToRight     // Forcing LtoR.
            }

            protected struct Level
            {
                public int level;
                public OverrideStatus status;

                public Level(int level, OverrideStatus status)
                {
                    this.level = level;
                    this.status = status;
                }
            }

            #region Members

            static Stack<Level> stack = new Stack<Level>(61);     // Paragraph's current embedding level and override status.

            // Used when processing runs.  Pulled to this level of visibilty so the 
            // Sub() methods don't have to have everything passed to them.
            static int start = 0;
            static int end = 0;
            static string sor = "L";
            static string eor = "R";

            static List<FatChar> localFatChars;     // Local ref so we don't need to keep passing the real one around.

            #endregion



            /// <summary>
            /// Takes the input string which is expected to be in logical 
            /// ordering and returns a string in visual ordering.
            /// 
            /// Logical ordering is the ordering the string should be stored
            /// and edited in.  Visual ordering is the order the characters
            /// are displayed.  Since this is a many-to-one conversion there
            /// is no VisualToLogical equivalent.
            /// </summary>
            /// <param name="str">Input string</param>
            public static void LogicalToVisualPass1(List<FatChar> fatChars)
            {
                localFatChars = fatChars;

                //
                // 3.3.1 Calc paragraph CEL by looking for the first strong character.
                //
                stack.Clear();
                OverrideStatus status = OverrideStatus.Neutral;
                
                foreach (FatChar fc in fatChars)
                {
                    if (fc.type == "L")
                    {
                        stack.Push(new Level(0, status));
                        break;
                    }

                    if (fc.type == "R" || fc.type == "AL")
                    {
                        stack.Push(new Level(1, status));
                        break;
                    }
                }
                // Bail if no strong chars found.  IE treat as L-to-R.
                if (stack.Count == 0)
                {
                    return;
                }
                
                // Just assume "L".  Not sure why but this seems to give behaviour 
                // matching what I see in Word, etc.  On the other hand it breaks
                // things when just typing R.  In particular, any white space or
                // punctuation gets displayed at the wrong place until it is surrounded
                // by R.  This is because having eor=="L" forces neutrals to wrongly
                // be treated as "L".
                // stack.Push(new Level(0, status));

                // Assign the default level based on the current culture.
                // This is to try and match the behaviour of Word.
                /*
                string culture = CultureInfo.CurrentCulture.TwoLetterISOLanguageName;
                if (culture == "he" || culture == "ar")
                {
                    stack.Pop();
                    stack.Push(new Level(1, status));
                }
                else
                {
                    stack.Pop();
                    stack.Push(new Level(0, status));
                }
                */

                // X1  Set all character levels to paragraph level.
                foreach (FatChar fc in fatChars)
                {
                    fc.level = stack.Peek().level;
                }

                // Dump();

                //
                // 3.3.2 Explicit levels and directions.
                //
                foreach (FatChar fc in fatChars)
                {
                    int level;
                    switch (fc.type)
                    {
                        case "RLE":
                            // X2
                            // Push next higher odd level.
                            level = stack.Peek().level + 1;
                            if ((level & 0x01) == 0)
                            {
                                ++level;
                            }
                            stack.Push(new Level(level, status));
                            status = OverrideStatus.Neutral;
                            break;

                        case "LRE":
                            // X3
                            // Push next higher even level.
                            level = stack.Peek().level + 1;
                            if ((level & 0x01) == 1)
                            {
                                ++level;
                            }
                            stack.Push(new Level(level, status));
                            status = OverrideStatus.Neutral;
                            break;

                        case "RLO":
                            // X4
                            // Push next higher odd level.
                            level = stack.Peek().level + 1;
                            if ((level & 0x01) == 0)
                            {
                                ++level;
                            }
                            stack.Push(new Level(level, status));
                            status = OverrideStatus.RightToLeft;
                            break;

                        case "LRO":
                            // X5
                            // Push next higher even level.
                            level = stack.Peek().level + 1;
                            if ((level & 0x01) == 1)
                            {
                                ++level;
                            }
                            stack.Push(new Level(level, status));
                            status = OverrideStatus.LeftToRight;
                            break;

                        case "BN":
                            // Do nothing
                            break;

                        case "PDF":
                            // X7
                            stack.Pop();
                            break;

                        default:
                            // X6
                            // Everything else.
                            fc.level = stack.Peek().level;
                            if (stack.Peek().status != OverrideStatus.Neutral)
                            {
                                if (stack.Peek().status == OverrideStatus.LeftToRight)
                                {
                                    fc.type = "L";
                                }
                                else
                                {
                                    fc.type = "R";
                                }
                            }
                            break;

                    }   // end of switch on fc.type
                }   // end of loop over all fatChars.

                // X8, X9
                // Remove all RLE, LRE, RLO, LRO, PDF and BN codes.
                for (int i = 0; i < fatChars.Count; i++)
                {
                    if (GetType(i) == "RLE" || GetType(i) == "LRE" || GetType(i) == "RLO" || GetType(i) == "LRO" || GetType(i) == "PDF" || GetType(i) == "BN")
                    {
                        fatChars.RemoveAt(i);
                        --i;
                    }
                }

                // Bail if there's nothing left.
                if (fatChars.Count == 0)
                {
                    return;
                }

                // end of 3.3.2

                // Dump();

                // X10
                // Calc sor and eor for runs.

                // Loop over each run.
                start = 0;
                end = 0;
                sor = fatChars[start].type;
                eor = "R";
                while (true)
                {
                    // First character of run is at start.
                    // Break out of loop if this is beyond length of array.
                    if (start >= fatChars.Count)
                    {
                        break;
                    }

                    // Find the end of the run.
                    end = start;
                    while (end + 1 < fatChars.Count && fatChars[start].level == fatChars[end + 1].level)
                    {
                        ++end;
                    }

                    // Get eor from whichever level is higher at boundary.
                    eor = (fatChars[end].level & 0x01) == 1 ? "R" : "L";
                    if (end + 1 < fatChars.Count && fatChars[end].level < fatChars[end + 1].level)
                    {
                        eor = (fatChars[end + 1].level & 0x01) == 1 ? "R" : "L";
                    }
                    else
                    {
                        // At end of run.  Take eor from last strong character.
                        // This is not according to spec but makes things work better
                        // when we have text that starts R and ends L (or vice versa).
                        int e = end;
                        while (e > start)
                        {
                            if (fatChars[e].type == "L")
                            {
                                eor = "L";
                                break;
                            }
                            if (fatChars[e].type == "R")
                            {
                                eor = "R";
                                break;
                            }
                            --e;
                        }
                    }

                    //Debug.Print("run {0},{1} sor={2} eor={3}", start, end, sor, eor);


                    //
                    // 3.3.3 Resolving Weak Types.
                    //

                    // W1 Process NSMs.
                    // AL  NSM  NSM -> AL  AL  AL
                    // sor NSM      -> sor R

                    if (fatChars[start].type == "NSM")
                    {
                        fatChars[start].type = sor;
                    }
                    for (int i = start + 1; i <= end; i++)
                    {
                        if (fatChars[i - 1].type == "AL" && GetType(i) == "NSM")
                        {
                            fatChars[start].type = "AL";
                        }
                    }

                    // W2 Convert ENs to ANs if needed.
                    // AL EN    -> AL AN
                    // AL N  EN -> AL N  AN
                    for (int i = start + 1; i <= end; i++)
                    {
                        if (GetType(i) == "EN")
                        {
                            // Look backwards to try and find AL.  
                            for (int j = i - 1; j >= start; j--)
                            {
                                if (GetType(j) == "AL")
                                {
                                    fatChars[i].type = "AL";
                                    break;
                                }

                                if (GetType(j) == "R" || GetType(j) == "L")
                                {
                                    break;
                                }
                            }
                        }
                    }

                    // W3 Change all ALs to Rs
                    // AL -> R
                    for (int i = start; i <= end; i++)
                    {
                        if (GetType(i) == "AL")
                        {
                            fatChars[i].type = "R";
                        }
                    }

                    // W4 Convert European and common seperators.
                    // EN ES EN -> EN EN EN
                    // EN CS EN -> EN EN EN
                    // AN CS AN -> AN AN AN
                    for (int i = start + 1; i < end; i++)
                    {
                        if (GetType(i) == "ES" && GetType(i - 1) == "EN" && GetType(i + 1) == "EN")
                        {
                            fatChars[i].type = "EN";
                        }
                        if (GetType(i) == "CS" && GetType(i - 1) == "EN" && GetType(i + 1) == "EN")
                        {
                            fatChars[i].type = "EN";
                        }
                        if (GetType(i) == "CS" && GetType(i - 1) == "AN" && GetType(i + 1) == "AN")
                        {
                            fatChars[i].type = "AN";
                        }
                    }

                    // W5 Convert European terminators to European numbers if adjacent (either direction) to a European number.
                    // Note that these transformations need to go both directions so we do two passes.
                    // ET ET EN -> EN EN EN
                    // EN ET ET -> EN EN EN 
                    // AN ET EN -> AN EN EN
                    // left to right pass
                    for (int i = start + 1; i <= end; i++)
                    {
                        if (GetType(i) == "ET" && GetType(i - 1) == "EN")
                        {
                            fatChars[i].type = "EN";
                        }
                    }
                    // right to left pass
                    for (int i = end - 1; i >= start; i--)
                    {
                        if (GetType(i) == "ET" && GetType(i + 1) == "EN")
                        {
                            fatChars[i].type = "EN";
                        }
                    }

                    // Dump();

                    // W6 Other separators and terminators.
                    // AN ET    -> AN ON
                    // ET AN    -> ON AN
                    // L  ES EN -> L  ON EN
                    // EN CS AN -> EN ON AN
                    for (int i = start; i <= end; i++)
                    {
                        if (GetType(i) == "AN" && GetType(i + 1) == "ET")
                        {
                            fatChars[i + 1].type = "ON";
                        }
                        if (GetType(i) == "ET" && GetType(i + 1) == "AN")
                        {
                            fatChars[i].type = "ON";
                        }
                        //if (i > start)
                        {
                            //if (GetType(i - 1) == "L" && GetType(i) == "ES" && GetType(i + 1) == "EN")
                            if (GetType(i) == "ES")
                            {
                                fatChars[i].type = "ON";
                            }
                            //if (GetType(i - 1) == "EN" && GetType(i) == "CS" && GetType(i + 1) == "AN")
                            if (GetType(i) == "CS")
                            {
                                fatChars[i].type = "ON";
                            }
                        }
                    }

                    // Dump();

                    // W7 Convert Europen numbers to L if preceeded by L
                    // L N EN -> L N L
                    // R N EN -> R N EN
                    for (int i = start + 1; i <= end; i++)
                    {
                        if (GetType(i) == "EN")
                        {
                            // Scan backwards until L or R is found.  If L change EN to L.
                            for (int j = i - 1; j >= start; j--)
                            {
                                if (GetType(j) == "L")
                                {
                                    fatChars[i].type = "L";
                                    break;
                                }
                                if (/* GetType(j) == "R" || */ !IsNeutral(j))
                                {
                                    break;
                                }
                            }
                        }
                    }

                    // End of 3.3.3

                    // Dump();

                    // 3.3.4 Resolving Neutral Types.

                    // N1 Resolve neutrals based on surrounding text.  Use sor and eor at ends of the run (built into GetType()).
                    // NOTE In these cases N represents a run of 1 or more neutrals.  Not just a single, isolated character.
                    // L  N L  -> L  L L
                    // R  N R  -> R  R R
                    // R  N AN -> R  R AN
                    // R  N EN -> R  R EN 
                    // AN N R  -> AN R R
                    // AN N AN -> AN R AN
                    // AN N EN -> AN R EN
                    // EN N R  -> EN R R
                    // EN N AN -> EN R AN
                    // EN N EN -> EN R EN

                    // TODO Probably lots of opportunity for optimization here.
                    for (int i = start; i <= end; i++)
                    {
                        int len = 0;

                        // L  N L  -> L  L L
                        if (GetType(i - 1) == "L" && IsNeutralRun(i, out len) && GetType(i + len) == "L")
                        {
                            fatChars[i].type = "L";
                        }
                        // R  N R  -> R  R R
                        if (GetType(i - 1) == "R" && IsNeutralRun(i, out len) && GetType(i + len) == "R")
                        {
                            fatChars[i].type = "R";
                        }
                        // R  N AN -> R  R AN
                        if (GetType(i - 1) == "R" && IsNeutralRun(i, out len) && GetType(i + len) == "AN")
                        {
                            fatChars[i].type = "R";
                        }
                        // R  N EN -> R  R EN 
                        if (GetType(i - 1) == "R" && IsNeutralRun(i, out len) && GetType(i + len) == "EN")
                        {
                            fatChars[i].type = "R";
                        }
                        // AN N R  -> AN R R
                        if (GetType(i - 1) == "AN" && IsNeutralRun(i, out len) && GetType(i + len) == "R")
                        {
                            fatChars[i].type = "R";
                        }
                        // AN N AN -> AN R AN
                        if (GetType(i - 1) == "AN" && IsNeutralRun(i, out len) && GetType(i + len) == "AN")
                        {
                            fatChars[i].type = "R";
                        }
                        // AN N EN -> AN R EN
                        if (GetType(i - 1) == "AN" && IsNeutralRun(i, out len) && GetType(i + len) == "EN")
                        {
                            fatChars[i].type = "R";
                        }
                        // EN N R  -> EN R R
                        if (GetType(i - 1) == "EN" && IsNeutralRun(i, out len) && GetType(i + len) == "R")
                        {
                            fatChars[i].type = "R";
                        }
                        // EN N AN -> EN R AN
                        if (GetType(i - 1) == "EN" && IsNeutralRun(i, out len) && GetType(i + len) == "AN")
                        {
                            fatChars[i].type = "R";
                        }
                        // EN N EN -> EN R EN
                        if (GetType(i - 1) == "EN" && IsNeutralRun(i, out len) && GetType(i + len) == "EN")
                        {
                            fatChars[i].type = "R";
                        }

                    }

                    // Dump();

                    // N2 Handle any remaining neutrals.
                    // Set based on level.  Odd -> "R", Even -> "L".
                    for (int i = start; i <= end; i++)
                    {
                        if (GetType(i) == "B" || GetType(i) == "S" || GetType(i) == "WS")
                        {
                            fatChars[i].type = (fatChars[i].level & 0x01) == 1 ? "R" : "L";
                        }
                    }

                    // Handle ONs
                    for (int i = start; i <= end; i++)
                    {
                        int len = 0;

                        // R ON L -> R R L
                        if (GetType(i - 1) == "R" && IsOtherNeutralRun(i, out len) && GetType(i + len) == "L")
                        {
                            fatChars[i].type = "R";
                        }

                        // L ON R -> L L R
                        if (GetType(i - 1) == "L" && IsOtherNeutralRun(i, out len) && GetType(i + len) == "R")
                        {
                            fatChars[i].type = "L";
                        }
                    }

                    // Dump();

                    // End of 3.3.4

                    // 3.3.5 Resolving Implicit Levels.

                    // I1, I2
                    // Bump levels based on type and current levels.
                    for (int i = start; i <= end; i++)
                    {
                        switch (GetType(i))
                        {
                            case "L":
                                fatChars[i].level += (fatChars[i].level & 0x01) == 0 ? 0 : 1;
                                break;
                            case "R":
                                fatChars[i].level += (fatChars[i].level & 0x01) == 0 ? 1 : 0;
                                break;
                            case "AN":
                                fatChars[i].level += (fatChars[i].level & 0x01) == 0 ? 2 : 1;
                                break;
                            case "EN":
                                fatChars[i].level += (fatChars[i].level & 0x01) == 0 ? 2 : 1;
                                break;
                            default:
                                //Debug.Assert(false);
                                break;
                        }
                    }

                    // end of 3.3.5

                    // Dump();

                    // Move start to beginning of next run.
                    start = end + 1;
                    sor = eor;

                }   // end of loop over runs.

                // Dump();

            }   // end of LogicalToVisualPass1()

            public static void LogicalToVisualPass2(List<FatChar> fatChars)
            {
                localFatChars = fatChars;

                // Set start and end values to be correct since functions
                // like GetOriginaltype() need them to match the range of
                // the string we're working on.
                start = 0;
                end = fatChars.Count;

                // Dump();

                // Back to working on full string.

                // 3.4  Reordering Resolved Levels

                // L1 Adjust level of separators and whitespace to match paragraph level.
                // Note this needs the original types due to 3.3.4, N2.
                // Applies to:
                //  all S
                //  all B
                //  any WS preceeding S or B
                //  any WS at end of line
                int pLevel = fatChars[0].level;
                // First, get end of line case
                /*
                for (int i = fatChars.Count - 1; i >= 0; i--)
                {
                    if (GetOriginalType(i) == "WS")
                    {
                        fatChars[i].level = pLevel;
                    }
                    else
                    {
                        break;
                    }
                }
                */

                // Dump();

                // Now, get the rest.
                bool ws = false;
                for (int i = fatChars.Count - 1; i >= 0; i--)
                {
                    if (GetOriginalType(i) == "S" || GetOriginalType(i) == "B")
                    {
                        fatChars[i].level = pLevel;
                        ws = true;
                    }
                    else
                    {
                        if (ws)
                        {
                            if (GetOriginalType(i) == "WS")
                            {
                                fatChars[i].level = pLevel;
                            }
                            else
                            {
                                ws = false;
                            }
                        }
                    }
                }

                // Dump();

                if(BokuSettings.Settings.UseSystemFontRendering)
                {
                    // Set display order for this line.
                    // R-to-L runs will have their ordering reversed below.
                    int dispIndex = fatChars[0].displayOrderIndex;
                    for (int i = 0; i < fatChars.Count; i++)
                    {
                        fatChars[i].displayOrderIndex = dispIndex;
                        ++dispIndex;
                    }
                }        
                
                // L2  From highest level to lowest odd level, reverse 
                // any contiguous set of characters at that level or higher.
                {
                    // Find highest level.
                    int level = 0;
                    for (int i = 0; i < fatChars.Count; i++)
                    {
                        level = Math.Max(level, fatChars[i].level);
                    }

                    // Perform a pass at each level.
                    for (; level > 0; level--)
                    {
                        // Find each run at the current or higher level.
                        start = -1;
                        end = 0;
                        while (true)
                        {
                            // Increment start to find next run.
                            while (start < fatChars.Count - 1)
                            {
                                ++start;
                                if (fatChars[start].level >= level)
                                {
                                    break;
                                }
                            }

                            // No run found at this level.
                            if (start >= fatChars.Count)
                            {
                                break;
                            }

                            // Find the end of the run.
                            end = start;
                            while (end + 1 < fatChars.Count && fatChars[end + 1].level >= level)
                            {
                                ++end;
                            }

                            // Found the run, reverse it.
                            int l = start;
                            int r = end;
                            while (l < r)
                            {
                                if(!BokuSettings.Settings.UseSystemFontRendering)
                                {
                                    // Swap chars
                                    FatChar tmp = fatChars[l];
                                    fatChars[l] = fatChars[r];
                                    fatChars[r] = tmp;
                                }
                                else
                                {
                                    // Swap display order.
                                    int tmpi = fatChars[l].displayOrderIndex;
                                    fatChars[l].displayOrderIndex = fatChars[r].displayOrderIndex;
                                    fatChars[r].displayOrderIndex = tmpi;
                                }
                                ++l;
                                --r;
                            }

                            // Set start for next run.
                            start = end + 1;

                        }   // end of loop over scans.

                    }   // end of loop over levels.

                }   // end of L2


                

                // L3 Combining marks?

                // L4 Mirroring.
                foreach (FatChar fc in fatChars)
                {
                    if (fc.mirrored && fc.type == "R")
                    {
                        fc.c = fc.mirroredChar;
                    }
                }

                // Dump();

                // TODO (****) Why is this only when using system font rendering?
                if(!BokuSettings.Settings.UseSystemFontRendering)
                {
                    // Finally, fill in the new display order indices.
                    for (int i = 0; i < fatChars.Count; i++)
                    {
                        fatChars[i].displayOrderIndex = i;
                    }
                }

                // Dump();

                // Done.

            }   // end of LogicalToVisualPass2()

            #region Internal

            /// <summary>
            /// Get the type from fatChars for the i'th character.
            /// If the character is outside of the current run it
            /// then returns sor or eor as appropriate.
            /// </summary>
            /// <param name="i"></param>
            /// <returns></returns>
            static string GetType(int i)
            {
                if (i < start)
                    return sor;
                else if (i > end)
                    return eor;
                else
                    return localFatChars[i].type;
            }

            static string GetOriginalType(int i)
            {
                if (i < start)
                    return sor;
                else if (i > end)
                    return eor;
                else
                    return localFatChars[i].originalType;
            }

            /// <summary>
            /// Gets the type of char i and returns true if it's neutral.
            /// </summary>
            /// <param name="?"></param>
            /// <returns></returns>
            static bool IsNeutral(int i)
            {
                bool result = GetType(i) == "B" || GetType(i) == "S" || GetType(i) == "WS" || GetType(i) == "ON";
                return result;
            }

            /// <summary>
            /// Determines if there's a run of 1 or more neutral characters at this location.
            /// </summary>
            /// <param name="i">Index to start run.</param>
            /// <param name="len">If run is found, returns length of run.</param>
            /// <returns>True if run is found, false otherwise.</returns>
            static bool IsNeutralRun(int i, out int len)
            {
                len = 0;
                // Is current character neutral?
                bool result = IsNeutral(i);
                if (result)
                {
                    len = 1;
                    while (len < end - start)
                    {
                        ++i;
                        // Is next character neutral?
                        if (IsNeutral(i))
                        {
                            ++len;
                        }
                        else
                        {
                            break;
                        }
                    }
                }

                return result;
            }

            /// <summary>
            /// Determines if there's a run of 1 or more ON characters at this location.
            /// </summary>
            /// <param name="i">Index to start run.</param>
            /// <param name="len">If run is found, returns length of run.</param>
            /// <returns>True if run is found, false otherwise.</returns>
            static bool IsOtherNeutralRun(int i, out int len)
            {
                len = 0;
                // Is current character neutral?
                bool result = GetType(i) == "ON";
                if (result)
                {
                    len = 1;
                    while (len < end - start)
                    {
                        ++i;
                        // Is next character neutral?
                        if (GetType(i) == "ON")
                        {
                            ++len;
                        }
                        else
                        {
                            break;
                        }
                    }
                }

                return result;
            }

            /// <summary>
            /// Debug helper.
            /// </summary>
            static void Dump()
            {
#if DEBUG
                if (localFatChars.Count > 50)
                    return;

#if !NETFX_CORE
                Debug.Print("===");
                Debug.Print("sor : " + sor);
                Debug.Print("eor : " + eor);
#endif

                string display = "";
                int i = 0;
                foreach (FatChar fc in localFatChars)
                {
#if !NETFX_CORE
                    //Debug.Print(String.Format("{0:d2} : {1:1} {2:d2} {3:3}", i, fc.c, fc.EL, fc.type));
                    Debug.Print(i.ToString("d3") + " : " + fc.level.ToString("d2") + " " + fc.c + " " + fc.type + "(" + fc.originalType + ")" + " " + fc.displayOrderIndex.ToString("d3"));
#endif
                    display += fc.c;
                    ++i;
                }
#if !NETFX_CORE
                Debug.Print("Display : " + display);
#endif
#endif
            }   // end of Dump()

            #endregion

        }   // end of class BiDi

    }   // end of class TextBlob
}   // end of namespace Boku.Common
