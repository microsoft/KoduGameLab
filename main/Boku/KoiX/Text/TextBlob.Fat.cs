// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;

using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Audio;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using Microsoft.Xna.Framework.Storage;

using Boku.Base;
using Boku.UI2D;
using Boku.Fx;
using Boku.Programming;

namespace KoiX.Text
{
    /// <summary>
    /// The TextBlob class wraps up a user entered text string along 
    /// with methods for navigating, editing and rendering it.
    /// 
    /// This file contains the FatChar class.  
    /// </summary>
    public partial class TextBlob
    {
        /// <summary>
        /// Heavyweight character info to support bidi processing etc.
        /// </summary>
        public class FatChar
        {
            #region Members

            public char c;                  // Character.

            public string originalType;     // Unicode "type" of character based on Unicode Standard Annex #9
            public string type;             // During bidi processing the type is changed.  This is the current value.

            public bool mirrored = false;   // Is this a bidi mirrored character?
            public char mirroredChar;       // If so, here's its mirror.

            public int level = 0;           // Embedding level, EL in the doc.

            public int rawIndex;            // Index of this char in raw string.
            public int logicalOrderIndex;   // Index of this char in logical ordered string.
            public int displayOrderIndex;   // Index of this char in display ordered string.

            // If c == U+F8FF then this FatChar represents a Kodu icon.
            // For normal icons, the label will be null and the icon will be filled in.
            // For keys, icon==key and either label will contain the string to print on the key
            //      or keyIcon will contain the icon to render on top of the key face.
            // For programming tiles the icon will contain the tile and the label will have the label.
            public List<FatChar> label = null;
            public TextHelper.ControlInputs icon = TextHelper.ControlInputs.none;
            public TextHelper.ControlInputs keyIcon = TextHelper.ControlInputs.none;

            #endregion

            #region Public

            public FatChar(char c)
            {
                this.c = c;
                Unicode.UnicodeCharData data = Unicode.GetCharInfo(c);

                this.originalType = this.type = data.type;
                this.mirrored = data.mirrored;
                this.mirroredChar = data.mirroredChar;

                this.level = 0;
            }

            #endregion

        }   // end of class FatChar

    }   // end of class TextBlob

}   // end of namespace KoiX.Text
