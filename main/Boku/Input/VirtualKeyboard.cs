
using System;
using System.Xml;
using System.Xml.Serialization;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;

using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Audio;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using Microsoft.Xna.Framework.Storage;

using KoiX;
using KoiX.Text;

using Boku.Base;
using Boku.Common;
using Boku.Common.Gesture;
using Boku.UI2D;

namespace Boku.Input
{
    public static partial class VirtualKeyboard
    {
        public delegate void OnKeyDelegate(Key key);

        const int largeBackspaceWidth = 294;
        const int largeEnterWidth = 255;
        const int largeSpaceWidth = 910;
        const int largeGapWidth = 14;
        const int largeKeyHeight = 105;
        const int largeKeyWidth = 140;

        const int smallBackspaceWidth = 205;
        const int smallEnterWidth = 178;
        const int smallSpaceWidth = 636;
        const int smallGapWidth = 10;
        const int smallKeyHeight = 74;
        const int smallKeyWidth = 98;

        #region Members

        static List<KeySet> keySets = new List<KeySet>();
        static KeySet curKeySet = null;

        static bool active = false;     // Accepting input.
        static bool visible = false;    // Visible on screen.

        static AABB2D hitBox = new AABB2D();

        static int height = 0;              // Size of renderTarget.
        static int width = 0;
        static Vector2 position;            // Position as if fully scaled.  Will be different in snapped modes.
        static Vector2 renderPosition;      // Actual position we render in due to snap.
        static Vector2 renderSize;          // Actual size we render in.
        static float renderScale = 1.0f;    // Ratio of renderSize / rt size 

        static int backspaceWidth = largeBackspaceWidth;
        static int enterWidth = largeEnterWidth;
        static int spaceWidth = largeSpaceWidth;
        static int gapWidth = largeGapWidth;
        static int keyHeight = largeKeyHeight;
        static int keyWidth = largeKeyWidth;

        static bool dirty = true;   // Do we need to refresh the renderTarget?
        static RenderTarget2D rt = null;

        static Color bkgColor = new Color(0, 0, 0);
        static Color lightKeyColor = new Color(100, 100, 100);
        static Color darkKeyColor = new Color(60, 60, 60);
        static Color textColor = new Color(230, 230, 230);
        static Color dimmedTextColor = new Color(160, 160, 160);
        static Color latchedKeyColor = new Color(235, 200, 40);

        static GetSpriteFont Font = null;

        static Texture2D whiteTexture = null;  // Used as blank texture for keycaps.

        static Texture2D circleTexture = null;
        static Texture2D backspaceTexture = null;
        static Texture2D closeTexture = null;
        static Texture2D enterTexture = null;
        static Texture2D shiftTexture = null;
        static Texture2D leftTexture = null;
        static Texture2D rightTexture = null;
        static Texture2D leftCircleTexture = null;
        static Texture2D rightCircleTexture = null;


        #endregion

        #region Accessors

        /// <summary>
        /// Is the virtual keyboard currently looking for input?
        /// </summary>
        public static bool Active
        {
            get { return active; }
        }

        public static bool Visible
        {
            get { return visible; }
        }

        public static AABB2D HitBox
        {
            get { return hitBox; }
        }

        public static Vector2 Position
        {
            get { return position; }
        }

        public static int Height
        {
            get { return height; }
        }

        #endregion

        #region Public

        /// <summary>
        /// Note this is made private an is now called from LoadContent() to
        /// ensure that the textures are available before Init().
        /// </summary>
        static void Init()
        {
            int rightEdge = 0;
            GetFont BigFont = null;
            GetFont SmallFont = null;

            position = new Vector2(0, BokuGame.ScreenSize.Y);

            int closeOffset = 0;    // Vertical offset for close button.

            // Based on current screen size, pick keyboard layout size.
            if (BokuGame.ScreenSize.X >= 1920)
            {
                // Large version
                width = 1920;
                height = 540;
                rightEdge = 1920 - 30;
                closeOffset = 0;

                backspaceWidth = largeBackspaceWidth;
                enterWidth = largeEnterWidth;
                spaceWidth = largeSpaceWidth;
                gapWidth = largeGapWidth;
                keyHeight = largeKeyHeight;
                keyWidth = largeKeyWidth;

                BigFont = SharedX.GetGameFont30Bold;
                SmallFont = SharedX.GetGameFont24Bold;
            }
            else
            {
                // Small version
                width = 1366;
                height = 378;
                rightEdge = 1366 - 21;
                closeOffset = -12;

                backspaceWidth = smallBackspaceWidth;
                enterWidth = smallEnterWidth;
                spaceWidth = smallSpaceWidth;
                gapWidth = smallGapWidth;
                keyHeight = smallKeyHeight;
                keyWidth = smallKeyWidth;

                BigFont = SharedX.GetGameFont24Bold;
                SmallFont = SharedX.GetGameFont20;
            }

            Vector2 stdSize = new Vector2(keyWidth, keyHeight);
            Vector2 pos;

            //
            // Set up KeySets.
            //

            #region LowerCaseKeySet
            {
                KeySet ks = new KeySet("LowerCase");
                keySets.Add(ks);
                // Set this as the default KeySet.
                curKeySet = ks;

                // Start with close icon.  Should be same for all keysets.
                pos = new Vector2(rightEdge - keyWidth / 2.0f, closeOffset);
                ks.Keys.Add(new Key(ks, pos, new Vector2(64, 64), closeTexture, textColor, bkgColor, OnClose));

                // Top row of keys, right to left.
                pos.Y = height - 4 * gapWidth - 4 * keyHeight;
                pos.X = rightEdge;
                // For each key, offset pos by key and gap size then add key.
                pos.X -= backspaceWidth + gapWidth;
                ks.Keys.Add(new Key(ks, pos, new Vector2(backspaceWidth, keyHeight), backspaceTexture, textColor, lightKeyColor, Keys.Back));
                pos.X -= keyWidth + gapWidth;
                ks.Keys.Add(new Key(ks, pos, new Vector2(keyWidth, keyHeight), "p", BigFont, textColor, lightKeyColor, "p"));
                pos.X -= keyWidth + gapWidth;
                ks.Keys.Add(new Key(ks, pos, new Vector2(keyWidth, keyHeight), "o", BigFont, textColor, lightKeyColor, "o"));
                pos.X -= keyWidth + gapWidth;
                ks.Keys.Add(new Key(ks, pos, new Vector2(keyWidth, keyHeight), "i", BigFont, textColor, lightKeyColor, "i"));
                pos.X -= keyWidth + gapWidth;
                ks.Keys.Add(new Key(ks, pos, new Vector2(keyWidth, keyHeight), "u", BigFont, textColor, lightKeyColor, "u"));
                pos.X -= keyWidth + gapWidth;
                ks.Keys.Add(new Key(ks, pos, new Vector2(keyWidth, keyHeight), "y", BigFont, textColor, lightKeyColor, "y"));
                pos.X -= keyWidth + gapWidth;
                ks.Keys.Add(new Key(ks, pos, new Vector2(keyWidth, keyHeight), "t", BigFont, textColor, lightKeyColor, "t"));
                pos.X -= keyWidth + gapWidth;
                ks.Keys.Add(new Key(ks, pos, new Vector2(keyWidth, keyHeight), "r", BigFont, textColor, lightKeyColor, "r"));
                pos.X -= keyWidth + gapWidth;
                ks.Keys.Add(new Key(ks, pos, new Vector2(keyWidth, keyHeight), "e", BigFont, textColor, lightKeyColor, "e"));
                pos.X -= keyWidth + gapWidth;
                ks.Keys.Add(new Key(ks, pos, new Vector2(keyWidth, keyHeight), "w", BigFont, textColor, lightKeyColor, "w"));
                pos.X -= keyWidth + gapWidth;
                ks.Keys.Add(new Key(ks, pos, new Vector2(keyWidth, keyHeight), "q", BigFont, textColor, lightKeyColor, "q"));

                // Second row of keys, right to left.
                pos.Y = height - 3 * gapWidth - 3 * keyHeight;
                pos.X = rightEdge;
                // For each key, offset pos by key and gap size then add key.
                pos.X -= enterWidth + gapWidth;
                ks.Keys.Add(new Key(ks, pos, new Vector2(enterWidth, keyHeight), "Enter", SmallFont, textColor, lightKeyColor, Keys.Enter));
                pos.X -= keyWidth + gapWidth;
                ks.Keys.Add(new Key(ks, pos, new Vector2(keyWidth, keyHeight), "'", BigFont, textColor, lightKeyColor, "'"));
                pos.X -= keyWidth + gapWidth;
                ks.Keys.Add(new Key(ks, pos, new Vector2(keyWidth, keyHeight), "l", BigFont, textColor, lightKeyColor, "l"));
                pos.X -= keyWidth + gapWidth;
                ks.Keys.Add(new Key(ks, pos, new Vector2(keyWidth, keyHeight), "k", BigFont, textColor, lightKeyColor, "k"));
                pos.X -= keyWidth + gapWidth;
                ks.Keys.Add(new Key(ks, pos, new Vector2(keyWidth, keyHeight), "j", BigFont, textColor, lightKeyColor, "j"));
                pos.X -= keyWidth + gapWidth;
                ks.Keys.Add(new Key(ks, pos, new Vector2(keyWidth, keyHeight), "h", BigFont, textColor, lightKeyColor, "h"));
                pos.X -= keyWidth + gapWidth;
                ks.Keys.Add(new Key(ks, pos, new Vector2(keyWidth, keyHeight), "g", BigFont, textColor, lightKeyColor, "g"));
                pos.X -= keyWidth + gapWidth;
                ks.Keys.Add(new Key(ks, pos, new Vector2(keyWidth, keyHeight), "f", BigFont, textColor, lightKeyColor, "f"));
                pos.X -= keyWidth + gapWidth;
                ks.Keys.Add(new Key(ks, pos, new Vector2(keyWidth, keyHeight), "d", BigFont, textColor, lightKeyColor, "d"));
                pos.X -= keyWidth + gapWidth;
                ks.Keys.Add(new Key(ks, pos, new Vector2(keyWidth, keyHeight), "s", BigFont, textColor, lightKeyColor, "s"));
                pos.X -= keyWidth + gapWidth;
                ks.Keys.Add(new Key(ks, pos, new Vector2(keyWidth, keyHeight), "a", BigFont, textColor, lightKeyColor, "a"));

                // Third row of keys, right to left.
                pos.Y = height - 2 * gapWidth - 2 * keyHeight;
                pos.X = rightEdge;
                // For each key, offset pos by key and gap size then add key.
                pos.X -= keyWidth + gapWidth;
                ks.Keys.Add(new Key(ks, pos, new Vector2(keyWidth, keyHeight), shiftTexture, textColor, darkKeyColor, OnShift));
                pos.X -= keyWidth + gapWidth;
                ks.Keys.Add(new Key(ks, pos, new Vector2(keyWidth, keyHeight), "?", BigFont, textColor, lightKeyColor, "?"));
                pos.X -= keyWidth + gapWidth;
                ks.Keys.Add(new Key(ks, pos, new Vector2(keyWidth, keyHeight), ".", BigFont, textColor, lightKeyColor, "."));
                pos.X -= keyWidth + gapWidth;
                ks.Keys.Add(new Key(ks, pos, new Vector2(keyWidth, keyHeight), ",", BigFont, textColor, lightKeyColor, ","));
                pos.X -= keyWidth + gapWidth;
                ks.Keys.Add(new Key(ks, pos, new Vector2(keyWidth, keyHeight), "m", BigFont, textColor, lightKeyColor, "m"));
                pos.X -= keyWidth + gapWidth;
                ks.Keys.Add(new Key(ks, pos, new Vector2(keyWidth, keyHeight), "n", BigFont, textColor, lightKeyColor, "n"));
                pos.X -= keyWidth + gapWidth;
                ks.Keys.Add(new Key(ks, pos, new Vector2(keyWidth, keyHeight), "b", BigFont, textColor, lightKeyColor, "b"));
                pos.X -= keyWidth + gapWidth;
                ks.Keys.Add(new Key(ks, pos, new Vector2(keyWidth, keyHeight), "v", BigFont, textColor, lightKeyColor, "v"));
                pos.X -= keyWidth + gapWidth;
                ks.Keys.Add(new Key(ks, pos, new Vector2(keyWidth, keyHeight), "c", BigFont, textColor, lightKeyColor, "c"));
                pos.X -= keyWidth + gapWidth;
                ks.Keys.Add(new Key(ks, pos, new Vector2(keyWidth, keyHeight), "x", BigFont, textColor, lightKeyColor, "x"));
                pos.X -= keyWidth + gapWidth;
                ks.Keys.Add(new Key(ks, pos, new Vector2(keyWidth, keyHeight), "z", BigFont, textColor, lightKeyColor, "z"));
                pos.X -= keyWidth + gapWidth;
                ks.Keys.Add(new Key(ks, pos, new Vector2(keyWidth, keyHeight), shiftTexture, textColor, darkKeyColor, OnShift));

                // Fourth row of keys, right to left.
                pos.Y = height - 1 * gapWidth - 1 * keyHeight;
                pos.X = rightEdge;
                // For each key, offset pos by key and gap size then add key.
                pos.X -= keyWidth + gapWidth;
                ks.Keys.Add(new Key(ks, pos, new Vector2(keyWidth, keyHeight), null, textColor, darkKeyColor, ""));     // Blank where keyboard is.
                pos.X -= keyWidth + gapWidth;
                ks.Keys.Add(new Key(ks, pos, new Vector2(keyWidth, keyHeight), rightTexture, textColor, darkKeyColor, Keys.Right));
                pos.X -= keyWidth + gapWidth;
                ks.Keys.Add(new Key(ks, pos, new Vector2(keyWidth, keyHeight), leftTexture, textColor, darkKeyColor, Keys.Left));
                pos.X -= spaceWidth + gapWidth;
                ks.Keys.Add(new Key(ks, pos, new Vector2(spaceWidth, keyHeight), "", BigFont, textColor, lightKeyColor, " "));
                pos.X -= keyWidth + gapWidth;
                ks.Keys.Add(new Key(ks, pos, new Vector2(keyWidth, keyHeight), "<kodu>", BigFont, textColor, darkKeyColor, OnSymbols0));
                pos.X -= keyWidth + gapWidth;
                ks.Keys.Add(new Key(ks, pos, new Vector2(keyWidth, keyHeight), "Ctrl", SmallFont, dimmedTextColor, darkKeyColor, OnCtrl));
                pos.X -= keyWidth + gapWidth;
                ks.Keys.Add(new Key(ks, pos, new Vector2(keyWidth, keyHeight), "&123", SmallFont, textColor, darkKeyColor, OnNumpad0));

            }
            #endregion

            #region UpperCaseKeySet
            {
                KeySet ks = new KeySet("UpperCase");
                keySets.Add(ks);

                // Start with close icon.  Should be same for all keysets.
                pos = new Vector2(rightEdge - keyWidth / 2.0f, closeOffset);
                ks.Keys.Add(new Key(ks, pos, new Vector2(64, 64), closeTexture, textColor, bkgColor, OnClose));

                // Top row of keys, right to left.
                pos.Y = height - 4 * gapWidth - 4 * keyHeight;
                pos.X = rightEdge;
                // For each key, offset pos by key and gap size then add key.
                pos.X -= backspaceWidth + gapWidth;
                ks.Keys.Add(new Key(ks, pos, new Vector2(backspaceWidth, keyHeight), backspaceTexture, textColor, lightKeyColor, Keys.Back));
                pos.X -= keyWidth + gapWidth;
                ks.Keys.Add(new Key(ks, pos, new Vector2(keyWidth, keyHeight), "P", BigFont, textColor, lightKeyColor, "P"));
                pos.X -= keyWidth + gapWidth;
                ks.Keys.Add(new Key(ks, pos, new Vector2(keyWidth, keyHeight), "O", BigFont, textColor, lightKeyColor, "O"));
                pos.X -= keyWidth + gapWidth;
                ks.Keys.Add(new Key(ks, pos, new Vector2(keyWidth, keyHeight), "I", BigFont, textColor, lightKeyColor, "I"));
                pos.X -= keyWidth + gapWidth;
                ks.Keys.Add(new Key(ks, pos, new Vector2(keyWidth, keyHeight), "U", BigFont, textColor, lightKeyColor, "U"));
                pos.X -= keyWidth + gapWidth;
                ks.Keys.Add(new Key(ks, pos, new Vector2(keyWidth, keyHeight), "Y", BigFont, textColor, lightKeyColor, "Y"));
                pos.X -= keyWidth + gapWidth;
                ks.Keys.Add(new Key(ks, pos, new Vector2(keyWidth, keyHeight), "T", BigFont, textColor, lightKeyColor, "T"));
                pos.X -= keyWidth + gapWidth;
                ks.Keys.Add(new Key(ks, pos, new Vector2(keyWidth, keyHeight), "R", BigFont, textColor, lightKeyColor, "R"));
                pos.X -= keyWidth + gapWidth;
                ks.Keys.Add(new Key(ks, pos, new Vector2(keyWidth, keyHeight), "E", BigFont, textColor, lightKeyColor, "E"));
                pos.X -= keyWidth + gapWidth;
                ks.Keys.Add(new Key(ks, pos, new Vector2(keyWidth, keyHeight), "W", BigFont, textColor, lightKeyColor, "W"));
                pos.X -= keyWidth + gapWidth;
                ks.Keys.Add(new Key(ks, pos, new Vector2(keyWidth, keyHeight), "Q", BigFont, textColor, lightKeyColor, "Q"));

                // Second row of keys, right to left.
                pos.Y = height - 3 * gapWidth - 3 * keyHeight;
                pos.X = rightEdge;
                // For each key, offset pos by key and gap size then add key.
                pos.X -= enterWidth + gapWidth;
                ks.Keys.Add(new Key(ks, pos, new Vector2(enterWidth, keyHeight), "Enter", SmallFont, textColor, lightKeyColor, Keys.Enter));
                pos.X -= keyWidth + gapWidth;
                ks.Keys.Add(new Key(ks, pos, new Vector2(keyWidth, keyHeight), "\"", BigFont, textColor, lightKeyColor, "\""));
                pos.X -= keyWidth + gapWidth;
                ks.Keys.Add(new Key(ks, pos, new Vector2(keyWidth, keyHeight), "L", BigFont, textColor, lightKeyColor, "L"));
                pos.X -= keyWidth + gapWidth;
                ks.Keys.Add(new Key(ks, pos, new Vector2(keyWidth, keyHeight), "K", BigFont, textColor, lightKeyColor, "K"));
                pos.X -= keyWidth + gapWidth;
                ks.Keys.Add(new Key(ks, pos, new Vector2(keyWidth, keyHeight), "J", BigFont, textColor, lightKeyColor, "J"));
                pos.X -= keyWidth + gapWidth;
                ks.Keys.Add(new Key(ks, pos, new Vector2(keyWidth, keyHeight), "H", BigFont, textColor, lightKeyColor, "H"));
                pos.X -= keyWidth + gapWidth;
                ks.Keys.Add(new Key(ks, pos, new Vector2(keyWidth, keyHeight), "G", BigFont, textColor, lightKeyColor, "G"));
                pos.X -= keyWidth + gapWidth;
                ks.Keys.Add(new Key(ks, pos, new Vector2(keyWidth, keyHeight), "F", BigFont, textColor, lightKeyColor, "F"));
                pos.X -= keyWidth + gapWidth;
                ks.Keys.Add(new Key(ks, pos, new Vector2(keyWidth, keyHeight), "D", BigFont, textColor, lightKeyColor, "D"));
                pos.X -= keyWidth + gapWidth;
                ks.Keys.Add(new Key(ks, pos, new Vector2(keyWidth, keyHeight), "S", BigFont, textColor, lightKeyColor, "S"));
                pos.X -= keyWidth + gapWidth;
                ks.Keys.Add(new Key(ks, pos, new Vector2(keyWidth, keyHeight), "A", BigFont, textColor, lightKeyColor, "A"));

                // Third row of keys, right to left.
                pos.Y = height - 2 * gapWidth - 2 * keyHeight;
                pos.X = rightEdge;
                // For each key, offset pos by key and gap size then add key.
                pos.X -= keyWidth + gapWidth;
                ks.Keys.Add(new Key(ks, pos, new Vector2(keyWidth, keyHeight), shiftTexture, textColor, latchedKeyColor, OnShift));
                pos.X -= keyWidth + gapWidth;
                ks.Keys.Add(new Key(ks, pos, new Vector2(keyWidth, keyHeight), "!", BigFont, textColor, lightKeyColor, "!"));
                pos.X -= keyWidth + gapWidth;
                ks.Keys.Add(new Key(ks, pos, new Vector2(keyWidth, keyHeight), ":", BigFont, textColor, lightKeyColor, ":"));
                pos.X -= keyWidth + gapWidth;
                ks.Keys.Add(new Key(ks, pos, new Vector2(keyWidth, keyHeight), ";", BigFont, textColor, lightKeyColor, ";"));
                pos.X -= keyWidth + gapWidth;
                ks.Keys.Add(new Key(ks, pos, new Vector2(keyWidth, keyHeight), "M", BigFont, textColor, lightKeyColor, "M"));
                pos.X -= keyWidth + gapWidth;
                ks.Keys.Add(new Key(ks, pos, new Vector2(keyWidth, keyHeight), "N", BigFont, textColor, lightKeyColor, "N"));
                pos.X -= keyWidth + gapWidth;
                ks.Keys.Add(new Key(ks, pos, new Vector2(keyWidth, keyHeight), "B", BigFont, textColor, lightKeyColor, "B"));
                pos.X -= keyWidth + gapWidth;
                ks.Keys.Add(new Key(ks, pos, new Vector2(keyWidth, keyHeight), "V", BigFont, textColor, lightKeyColor, "V"));
                pos.X -= keyWidth + gapWidth;
                ks.Keys.Add(new Key(ks, pos, new Vector2(keyWidth, keyHeight), "C", BigFont, textColor, lightKeyColor, "C"));
                pos.X -= keyWidth + gapWidth;
                ks.Keys.Add(new Key(ks, pos, new Vector2(keyWidth, keyHeight), "X", BigFont, textColor, lightKeyColor, "X"));
                pos.X -= keyWidth + gapWidth;
                ks.Keys.Add(new Key(ks, pos, new Vector2(keyWidth, keyHeight), "Z", BigFont, textColor, lightKeyColor, "Z"));
                pos.X -= keyWidth + gapWidth;
                ks.Keys.Add(new Key(ks, pos, new Vector2(keyWidth, keyHeight), shiftTexture, textColor, latchedKeyColor, OnShift));

                // Fourth row of keys, right to left.
                pos.Y = height - 1 * gapWidth - 1 * keyHeight;
                pos.X = rightEdge;
                // For each key, offset pos by key and gap size then add key.
                pos.X -= keyWidth + gapWidth;
                ks.Keys.Add(new Key(ks, pos, new Vector2(keyWidth, keyHeight), null, textColor, darkKeyColor, ""));     // Blank where keyboard is.
                pos.X -= keyWidth + gapWidth;
                ks.Keys.Add(new Key(ks, pos, new Vector2(keyWidth, keyHeight), rightTexture, textColor, darkKeyColor, Keys.Right));
                pos.X -= keyWidth + gapWidth;
                ks.Keys.Add(new Key(ks, pos, new Vector2(keyWidth, keyHeight), leftTexture, textColor, darkKeyColor, Keys.Left));
                pos.X -= spaceWidth + gapWidth;
                ks.Keys.Add(new Key(ks, pos, new Vector2(spaceWidth, keyHeight), "", BigFont, textColor, lightKeyColor, " "));
                pos.X -= keyWidth + gapWidth;
                ks.Keys.Add(new Key(ks, pos, new Vector2(keyWidth, keyHeight), "<Kodu>", BigFont, textColor, darkKeyColor, OnSymbols0));
                pos.X -= keyWidth + gapWidth;
                ks.Keys.Add(new Key(ks, pos, new Vector2(keyWidth, keyHeight), "Ctrl", SmallFont, dimmedTextColor, darkKeyColor, OnCtrl));
                pos.X -= keyWidth + gapWidth;
                ks.Keys.Add(new Key(ks, pos, new Vector2(keyWidth, keyHeight), "&123", SmallFont, textColor, darkKeyColor, OnNumpad0));

            }
            #endregion

            #region CtrlKeySet
            {
                KeySet ks = new KeySet("Ctrl");
                keySets.Add(ks);
                // Set this as the default KeySet.
                curKeySet = ks;

                // Start with close icon.  Should be same for all keysets.
                pos = new Vector2(rightEdge - keyWidth / 2.0f, closeOffset);
                ks.Keys.Add(new Key(ks, pos, new Vector2(64, 64), closeTexture, textColor, bkgColor, OnClose));

                // Top row of keys, right to left.
                pos.Y = height - 4 * gapWidth - 4 * keyHeight;
                pos.X = rightEdge;
                // For each key, offset pos by key and gap size then add key.
                pos.X -= backspaceWidth + gapWidth;
                ks.Keys.Add(new Key(ks, pos, new Vector2(backspaceWidth, keyHeight), backspaceTexture, textColor, lightKeyColor, Keys.Back));
                pos.X -= keyWidth + gapWidth;
                ks.Keys.Add(new Key(ks, pos, new Vector2(keyWidth, keyHeight), "p", BigFont, textColor, lightKeyColor, "p"));
                pos.X -= keyWidth + gapWidth;
                ks.Keys.Add(new Key(ks, pos, new Vector2(keyWidth, keyHeight), "o", BigFont, textColor, lightKeyColor, "o"));
                pos.X -= keyWidth + gapWidth;
                ks.Keys.Add(new Key(ks, pos, new Vector2(keyWidth, keyHeight), "i", BigFont, textColor, lightKeyColor, "i"));
                pos.X -= keyWidth + gapWidth;
                ks.Keys.Add(new Key(ks, pos, new Vector2(keyWidth, keyHeight), "u", BigFont, textColor, lightKeyColor, "u"));
                pos.X -= keyWidth + gapWidth;
                ks.Keys.Add(new Key(ks, pos, new Vector2(keyWidth, keyHeight), "y", BigFont, textColor, lightKeyColor, "y"));
                pos.X -= keyWidth + gapWidth;
                ks.Keys.Add(new Key(ks, pos, new Vector2(keyWidth, keyHeight), "t", BigFont, textColor, lightKeyColor, "t"));
                pos.X -= keyWidth + gapWidth;
                ks.Keys.Add(new Key(ks, pos, new Vector2(keyWidth, keyHeight), "r", BigFont, textColor, lightKeyColor, "r"));
                pos.X -= keyWidth + gapWidth;
                ks.Keys.Add(new Key(ks, pos, new Vector2(keyWidth, keyHeight), "e", BigFont, textColor, lightKeyColor, "e"));
                pos.X -= keyWidth + gapWidth;
                ks.Keys.Add(new Key(ks, pos, new Vector2(keyWidth, keyHeight), "w", BigFont, textColor, lightKeyColor, "w"));
                pos.X -= keyWidth + gapWidth;
                ks.Keys.Add(new Key(ks, pos, new Vector2(keyWidth, keyHeight), "q", BigFont, textColor, lightKeyColor, "q"));

                // Second row of keys, right to left.
                pos.Y = height - 3 * gapWidth - 3 * keyHeight;
                pos.X = rightEdge;
                // For each key, offset pos by key and gap size then add key.
                pos.X -= enterWidth + gapWidth;
                ks.Keys.Add(new Key(ks, pos, new Vector2(enterWidth, keyHeight), "Enter", SmallFont, textColor, lightKeyColor, Keys.Enter));
                pos.X -= keyWidth + gapWidth;
                ks.Keys.Add(new Key(ks, pos, new Vector2(keyWidth, keyHeight), "'", BigFont, textColor, lightKeyColor, "'"));
                pos.X -= keyWidth + gapWidth;
                ks.Keys.Add(new Key(ks, pos, new Vector2(keyWidth, keyHeight), "l", BigFont, textColor, lightKeyColor, "l"));
                pos.X -= keyWidth + gapWidth;
                ks.Keys.Add(new Key(ks, pos, new Vector2(keyWidth, keyHeight), "k", BigFont, textColor, lightKeyColor, "k"));
                pos.X -= keyWidth + gapWidth;
                ks.Keys.Add(new Key(ks, pos, new Vector2(keyWidth, keyHeight), "j", BigFont, textColor, lightKeyColor, "j"));
                pos.X -= keyWidth + gapWidth;
                ks.Keys.Add(new Key(ks, pos, new Vector2(keyWidth, keyHeight), "h", BigFont, textColor, lightKeyColor, "h"));
                pos.X -= keyWidth + gapWidth;
                ks.Keys.Add(new Key(ks, pos, new Vector2(keyWidth, keyHeight), "g", BigFont, textColor, lightKeyColor, "g"));
                pos.X -= keyWidth + gapWidth;
                ks.Keys.Add(new Key(ks, pos, new Vector2(keyWidth, keyHeight), "f", BigFont, textColor, lightKeyColor, "f"));
                pos.X -= keyWidth + gapWidth;
                ks.Keys.Add(new Key(ks, pos, new Vector2(keyWidth, keyHeight), "d", BigFont, textColor, lightKeyColor, "d"));
                pos.X -= keyWidth + gapWidth;
                ks.Keys.Add(new Key(ks, pos, new Vector2(keyWidth, keyHeight), "s", BigFont, textColor, lightKeyColor, "s"));
                pos.X -= keyWidth + gapWidth;
                ks.Keys.Add(new Key(ks, pos, new Vector2(keyWidth, keyHeight), "a", BigFont, textColor, lightKeyColor, "a"));

                // Third row of keys, right to left.
                pos.Y = height - 2 * gapWidth - 2 * keyHeight;
                pos.X = rightEdge;
                // For each key, offset pos by key and gap size then add key.
                pos.X -= keyWidth + gapWidth;
                ks.Keys.Add(new Key(ks, pos, new Vector2(keyWidth, keyHeight), shiftTexture, dimmedTextColor, darkKeyColor, OnShift));
                pos.X -= keyWidth + gapWidth;
                ks.Keys.Add(new Key(ks, pos, new Vector2(keyWidth, keyHeight), "?", BigFont, dimmedTextColor, lightKeyColor, "?"));
                pos.X -= keyWidth + gapWidth;
                ks.Keys.Add(new Key(ks, pos, new Vector2(keyWidth, keyHeight), ".", BigFont, textColor, lightKeyColor, "."));
                pos.X -= keyWidth + gapWidth;
                ks.Keys.Add(new Key(ks, pos, new Vector2(keyWidth, keyHeight), ",", BigFont, textColor, lightKeyColor, ","));
                pos.X -= keyWidth + gapWidth;
                ks.Keys.Add(new Key(ks, pos, new Vector2(keyWidth, keyHeight), "m", BigFont, textColor, lightKeyColor, "m"));
                pos.X -= keyWidth + gapWidth;
                ks.Keys.Add(new Key(ks, pos, new Vector2(keyWidth, keyHeight), "n", BigFont, textColor, lightKeyColor, "n"));
                pos.X -= keyWidth + gapWidth;
                ks.Keys.Add(new Key(ks, pos, new Vector2(keyWidth, keyHeight), "b", BigFont, textColor, lightKeyColor, "b"));
                pos.X -= keyWidth + gapWidth;
                ks.Keys.Add(new Key(ks, pos, new Vector2(keyWidth, keyHeight), "v", BigFont, textColor, lightKeyColor, "v"));
                pos.X -= keyWidth + gapWidth;
                ks.Keys.Add(new Key(ks, pos, new Vector2(keyWidth, keyHeight), "c", BigFont, textColor, lightKeyColor, "c"));
                pos.X -= keyWidth + gapWidth;
                ks.Keys.Add(new Key(ks, pos, new Vector2(keyWidth, keyHeight), "x", BigFont, textColor, lightKeyColor, "x"));
                pos.X -= keyWidth + gapWidth;
                ks.Keys.Add(new Key(ks, pos, new Vector2(keyWidth, keyHeight), "z", BigFont, textColor, lightKeyColor, "z"));
                pos.X -= keyWidth + gapWidth;
                ks.Keys.Add(new Key(ks, pos, new Vector2(keyWidth, keyHeight), shiftTexture, dimmedTextColor, darkKeyColor, OnShift));

                // Fourth row of keys, right to left.
                pos.Y = height - 1 * gapWidth - 1 * keyHeight;
                pos.X = rightEdge;
                // For each key, offset pos by key and gap size then add key.
                pos.X -= keyWidth + gapWidth;
                ks.Keys.Add(new Key(ks, pos, new Vector2(keyWidth, keyHeight), null, textColor, darkKeyColor, ""));     // Blank where keyboard is.
                pos.X -= keyWidth + gapWidth;
                ks.Keys.Add(new Key(ks, pos, new Vector2(keyWidth, keyHeight), rightTexture, textColor, darkKeyColor, Keys.Right));
                pos.X -= keyWidth + gapWidth;
                ks.Keys.Add(new Key(ks, pos, new Vector2(keyWidth, keyHeight), leftTexture, textColor, darkKeyColor, Keys.Left));
                pos.X -= spaceWidth + gapWidth;
                ks.Keys.Add(new Key(ks, pos, new Vector2(spaceWidth, keyHeight), "", BigFont, textColor, lightKeyColor, " "));
                pos.X -= keyWidth + gapWidth;
                ks.Keys.Add(new Key(ks, pos, new Vector2(keyWidth, keyHeight), "<Kodu>", BigFont, textColor, darkKeyColor, OnSymbols0));
                pos.X -= keyWidth + gapWidth;
                ks.Keys.Add(new Key(ks, pos, new Vector2(keyWidth, keyHeight), "Ctrl", SmallFont, textColor, latchedKeyColor, OnCtrl));
                pos.X -= keyWidth + gapWidth;
                ks.Keys.Add(new Key(ks, pos, new Vector2(keyWidth, keyHeight), "&123", SmallFont, textColor, darkKeyColor, OnNumpad0));

            }
            #endregion

            #region Numpad0KeySet
            {
                KeySet ks = new KeySet("Numpad0");
                keySets.Add(ks);

                // Start with close icon.  Should be same for all keysets.
                pos = new Vector2(rightEdge - keyWidth / 2.0f, closeOffset);
                ks.Keys.Add(new Key(ks, pos, new Vector2(64, 64), closeTexture, textColor, bkgColor, OnClose));

                // Top row of keys, right to left.
                pos.Y = height - 4 * gapWidth - 4 * keyHeight;
                pos.X = rightEdge;
                // For each key, offset pos by key and gap size then add key.
                pos.X -= keyWidth + gapWidth;
                ks.Keys.Add(new Key(ks, pos, new Vector2(keyWidth, keyHeight), backspaceTexture, textColor, lightKeyColor, Keys.Back));
                pos.X -= keyWidth / 2.0f;   // Space between sections.
                pos.X -= keyWidth + gapWidth;
                ks.Keys.Add(new Key(ks, pos, new Vector2(keyWidth, keyHeight), "3", BigFont, textColor, lightKeyColor, "3"));
                pos.X -= keyWidth + gapWidth;
                ks.Keys.Add(new Key(ks, pos, new Vector2(keyWidth, keyHeight), "2", BigFont, textColor, lightKeyColor, "2"));
                pos.X -= keyWidth + gapWidth;
                ks.Keys.Add(new Key(ks, pos, new Vector2(keyWidth, keyHeight), "1", BigFont, textColor, lightKeyColor, "1"));
                pos.X -= keyWidth / 2.0f;   // Space between sections.
                pos.X -= keyWidth + gapWidth;
                ks.Keys.Add(new Key(ks, pos, new Vector2(keyWidth, keyHeight), "&", BigFont, textColor, lightKeyColor, "&"));
                pos.X -= keyWidth + gapWidth;
                ks.Keys.Add(new Key(ks, pos, new Vector2(keyWidth, keyHeight), "%", BigFont, textColor, lightKeyColor, "%"));
                pos.X -= keyWidth + gapWidth;
                ks.Keys.Add(new Key(ks, pos, new Vector2(keyWidth, keyHeight), "$", BigFont, textColor, lightKeyColor, "$"));
                pos.X -= keyWidth + gapWidth;
                ks.Keys.Add(new Key(ks, pos, new Vector2(keyWidth, keyHeight), "#", BigFont, textColor, lightKeyColor, "#"));
                pos.X -= keyWidth + gapWidth;
                ks.Keys.Add(new Key(ks, pos, new Vector2(keyWidth, keyHeight), "@", BigFont, textColor, lightKeyColor, "@"));
                pos.X -= keyWidth + gapWidth;
                ks.Keys.Add(new Key(ks, pos, new Vector2(keyWidth, keyHeight), "!", BigFont, textColor, lightKeyColor, "!"));
                pos.X -= keyWidth + gapWidth;
                ks.Keys.Add(new Key(ks, pos, new Vector2(keyWidth, keyHeight), "Tab", SmallFont, textColor, darkKeyColor, "\t"));

                // Second row of keys, right to left.
                pos.Y = height - 3 * gapWidth - 3 * keyHeight;
                pos.X = rightEdge;
                // For each key, offset pos by key and gap size then add key.
                pos.X -= keyWidth + gapWidth;
                ks.Keys.Add(new Key(ks, pos, new Vector2(keyWidth, 2 * keyHeight + gapWidth), enterTexture, textColor, lightKeyColor, Keys.Enter));
                pos.X -= keyWidth / 2.0f;   // Space between sections.
                pos.X -= keyWidth + gapWidth;
                ks.Keys.Add(new Key(ks, pos, new Vector2(keyWidth, keyHeight), "6", BigFont, textColor, lightKeyColor, "6"));
                pos.X -= keyWidth + gapWidth;
                ks.Keys.Add(new Key(ks, pos, new Vector2(keyWidth, keyHeight), "5", BigFont, textColor, lightKeyColor, "5"));
                pos.X -= keyWidth + gapWidth;
                ks.Keys.Add(new Key(ks, pos, new Vector2(keyWidth, keyHeight), "4", BigFont, textColor, lightKeyColor, "4"));
                pos.X -= keyWidth / 2.0f;   // Space between sections.
                pos.X -= keyWidth + gapWidth;
                ks.Keys.Add(new Key(ks, pos, new Vector2(keyWidth, keyHeight), "+", BigFont, textColor, lightKeyColor, "+"));
                pos.X -= keyWidth + gapWidth;
                ks.Keys.Add(new Key(ks, pos, new Vector2(keyWidth, keyHeight), "=", BigFont, textColor, lightKeyColor, "="));
                pos.X -= keyWidth + gapWidth;
                ks.Keys.Add(new Key(ks, pos, new Vector2(keyWidth, keyHeight), "_", BigFont, textColor, lightKeyColor, "_"));
                pos.X -= keyWidth + gapWidth;
                ks.Keys.Add(new Key(ks, pos, new Vector2(keyWidth, keyHeight), "-", BigFont, textColor, lightKeyColor, "-"));
                pos.X -= keyWidth + gapWidth;
                ks.Keys.Add(new Key(ks, pos, new Vector2(keyWidth, keyHeight), ")", BigFont, textColor, lightKeyColor, ")"));
                pos.X -= keyWidth + gapWidth;
                ks.Keys.Add(new Key(ks, pos, new Vector2(keyWidth, keyHeight), "(", BigFont, textColor, lightKeyColor, "("));
                pos.X -= keyWidth + gapWidth;
                ks.Keys.Add(new Key(ks, pos, new Vector2(keyWidth, keyHeight), leftCircleTexture, dimmedTextColor, darkKeyColor, OnNumpad0));

                // Third row of keys, right to left.
                pos.Y = height - 2 * gapWidth - 2 * keyHeight;
                pos.X = rightEdge;
                // For each key, offset pos by key and gap size then add key.
                pos.X -= keyWidth + gapWidth;
                // Skip enter key since it"s 2 rows tall.
                //ks.Keys.Add(new Key(ks, pos, new Vector2(keyWidth, 2 * keyHeight + gapWidth), enterTexture, textColor, lightKeyColor, Keys.Enter));
                pos.X -= keyWidth / 2.0f;   // Space between sections.
                pos.X -= keyWidth + gapWidth;
                ks.Keys.Add(new Key(ks, pos, new Vector2(keyWidth, keyHeight), "9", BigFont, textColor, lightKeyColor, "9"));
                pos.X -= keyWidth + gapWidth;
                ks.Keys.Add(new Key(ks, pos, new Vector2(keyWidth, keyHeight), "8", BigFont, textColor, lightKeyColor, "8"));
                pos.X -= keyWidth + gapWidth;
                ks.Keys.Add(new Key(ks, pos, new Vector2(keyWidth, keyHeight), "7", BigFont, textColor, lightKeyColor, "7"));
                pos.X -= keyWidth / 2.0f;   // Space between sections.
                pos.X -= keyWidth + gapWidth;
                ks.Keys.Add(new Key(ks, pos, new Vector2(keyWidth, keyHeight), "/", BigFont, textColor, lightKeyColor, "/"));
                pos.X -= keyWidth + gapWidth;
                ks.Keys.Add(new Key(ks, pos, new Vector2(keyWidth, keyHeight), "*", BigFont, textColor, lightKeyColor, "*"));
                pos.X -= keyWidth + gapWidth;
                ks.Keys.Add(new Key(ks, pos, new Vector2(keyWidth, keyHeight), "\"", BigFont, textColor, lightKeyColor, "\""));
                pos.X -= keyWidth + gapWidth;
                ks.Keys.Add(new Key(ks, pos, new Vector2(keyWidth, keyHeight), ":", BigFont, textColor, lightKeyColor, ":"));
                pos.X -= keyWidth + gapWidth;
                ks.Keys.Add(new Key(ks, pos, new Vector2(keyWidth, keyHeight), ";", BigFont, textColor, lightKeyColor, ";"));
                pos.X -= keyWidth + gapWidth;
                ks.Keys.Add(new Key(ks, pos, new Vector2(keyWidth, keyHeight), "\\", BigFont, textColor, lightKeyColor, "\\"));
                pos.X -= keyWidth + gapWidth;
                ks.Keys.Add(new Key(ks, pos, new Vector2(keyWidth, keyHeight), rightCircleTexture, textColor, darkKeyColor, OnNumpad1));

                // Fourth row of keys, right to left.
                pos.Y = height - 1 * gapWidth - 1 * keyHeight;
                pos.X = rightEdge;
                // For each key, offset pos by key and gap size then add key.
                pos.X -= keyWidth + gapWidth;
                ks.Keys.Add(new Key(ks, pos, new Vector2(keyWidth, keyHeight), null, textColor, darkKeyColor, ""));     // Blank where keyboard is.
                pos.X -= keyWidth / 2.0f;   // Space between sections.
                pos.X -= keyWidth + gapWidth;
                ks.Keys.Add(new Key(ks, pos, new Vector2(keyWidth, keyHeight), ".", BigFont, textColor, lightKeyColor, "."));
                pos.X -= 2.0f * (keyWidth + gapWidth);
                ks.Keys.Add(new Key(ks, pos, new Vector2(2.0f * keyWidth + gapWidth, keyHeight), "0", BigFont, textColor, lightKeyColor, "0"));
                pos.X -= keyWidth / 2.0f;   // Space between sections.
                pos.X -= 2.0f * (keyWidth + gapWidth);
                ks.Keys.Add(new Key(ks, pos, new Vector2(2.0f * keyWidth + gapWidth, keyHeight), "Space", SmallFont, textColor, lightKeyColor, " "));
                pos.X -= keyWidth + gapWidth;
                ks.Keys.Add(new Key(ks, pos, new Vector2(keyWidth, keyHeight), rightTexture, textColor, lightKeyColor, Keys.Right));
                pos.X -= keyWidth + gapWidth;
                ks.Keys.Add(new Key(ks, pos, new Vector2(keyWidth, keyHeight), leftTexture, textColor, lightKeyColor, Keys.Left));
                pos.X -= keyWidth + gapWidth;
                ks.Keys.Add(new Key(ks, pos, new Vector2(keyWidth, keyHeight), "<Kodu>", BigFont, textColor, darkKeyColor, OnSymbols0));
                pos.X -= keyWidth + gapWidth;
                ks.Keys.Add(new Key(ks, pos, new Vector2(keyWidth, keyHeight), "Ctrl", SmallFont, dimmedTextColor, darkKeyColor, OnCtrl));
                pos.X -= keyWidth + gapWidth;
                ks.Keys.Add(new Key(ks, pos, new Vector2(keyWidth, keyHeight), "&123", SmallFont, textColor, latchedKeyColor, OnNumpad0));

            }
            #endregion

            #region Numpad1KeySet
            {
                KeySet ks = new KeySet("Numpad1");
                keySets.Add(ks);

                // Start with close icon.  Should be same for all keysets.
                pos = new Vector2(rightEdge - keyWidth / 2.0f, closeOffset);
                ks.Keys.Add(new Key(ks, pos, new Vector2(64, 64), closeTexture, textColor, bkgColor, OnClose));

                // Top row of keys, right to left.
                pos.Y = height - 4 * gapWidth - 4 * keyHeight;
                pos.X = rightEdge;
                // For each key, offset pos by key and gap size then add key.
                pos.X -= keyWidth + gapWidth;
                ks.Keys.Add(new Key(ks, pos, new Vector2(keyWidth, keyHeight), backspaceTexture, textColor, lightKeyColor, Keys.Back));
                pos.X -= keyWidth / 2.0f;   // Space between sections.
                pos.X -= keyWidth + gapWidth;
                ks.Keys.Add(new Key(ks, pos, new Vector2(keyWidth, keyHeight), "3", BigFont, textColor, lightKeyColor, "3"));
                pos.X -= keyWidth + gapWidth;
                ks.Keys.Add(new Key(ks, pos, new Vector2(keyWidth, keyHeight), "2", BigFont, textColor, lightKeyColor, "2"));
                pos.X -= keyWidth + gapWidth;
                ks.Keys.Add(new Key(ks, pos, new Vector2(keyWidth, keyHeight), "1", BigFont, textColor, lightKeyColor, "1"));
                pos.X -= keyWidth / 2.0f;   // Space between sections.
                pos.X -= keyWidth + gapWidth;
                ks.Keys.Add(new Key(ks, pos, new Vector2(keyWidth, keyHeight), "½", BigFont, textColor, lightKeyColor, "½"));
                pos.X -= keyWidth + gapWidth;
                ks.Keys.Add(new Key(ks, pos, new Vector2(keyWidth, keyHeight), "µ", BigFont, textColor, lightKeyColor, "µ"));
                pos.X -= keyWidth + gapWidth;
                ks.Keys.Add(new Key(ks, pos, new Vector2(keyWidth, keyHeight), "£", BigFont, textColor, lightKeyColor, "£"));
                pos.X -= keyWidth + gapWidth;
                ks.Keys.Add(new Key(ks, pos, new Vector2(keyWidth, keyHeight), "€", BigFont, textColor, lightKeyColor, "€"));
                pos.X -= keyWidth + gapWidth;
                ks.Keys.Add(new Key(ks, pos, new Vector2(keyWidth, keyHeight), "©", BigFont, textColor, lightKeyColor, "©"));
                pos.X -= keyWidth + gapWidth;
                ks.Keys.Add(new Key(ks, pos, new Vector2(keyWidth, keyHeight), "•", BigFont, textColor, lightKeyColor, "•"));
                pos.X -= keyWidth + gapWidth;
                ks.Keys.Add(new Key(ks, pos, new Vector2(keyWidth, keyHeight), "Tab", SmallFont, textColor, darkKeyColor, "\t"));

                // Second row of keys, right to left.
                pos.Y = height - 3 * gapWidth - 3 * keyHeight;
                pos.X = rightEdge;
                // For each key, offset pos by key and gap size then add key.
                pos.X -= keyWidth + gapWidth;
                ks.Keys.Add(new Key(ks, pos, new Vector2(keyWidth, 2 * keyHeight + gapWidth), enterTexture, textColor, lightKeyColor, Keys.Enter));
                pos.X -= keyWidth / 2.0f;   // Space between sections.
                pos.X -= keyWidth + gapWidth;
                ks.Keys.Add(new Key(ks, pos, new Vector2(keyWidth, keyHeight), "6", BigFont, textColor, lightKeyColor, "6"));
                pos.X -= keyWidth + gapWidth;
                ks.Keys.Add(new Key(ks, pos, new Vector2(keyWidth, keyHeight), "5", BigFont, textColor, lightKeyColor, "5"));
                pos.X -= keyWidth + gapWidth;
                ks.Keys.Add(new Key(ks, pos, new Vector2(keyWidth, keyHeight), "4", BigFont, textColor, lightKeyColor, "4"));
                pos.X -= keyWidth / 2.0f;   // Space between sections.
                pos.X -= keyWidth + gapWidth;
                ks.Keys.Add(new Key(ks, pos, new Vector2(keyWidth, keyHeight), "}", BigFont, textColor, lightKeyColor, "}"));
                pos.X -= keyWidth + gapWidth;
                ks.Keys.Add(new Key(ks, pos, new Vector2(keyWidth, keyHeight), "{", BigFont, textColor, lightKeyColor, "{"));
                pos.X -= keyWidth + gapWidth;
                ks.Keys.Add(new Key(ks, pos, new Vector2(keyWidth, keyHeight), "]", BigFont, textColor, lightKeyColor, "]"));
                pos.X -= keyWidth + gapWidth;
                ks.Keys.Add(new Key(ks, pos, new Vector2(keyWidth, keyHeight), "[", BigFont, textColor, lightKeyColor, "["));
                pos.X -= keyWidth + gapWidth;
                ks.Keys.Add(new Key(ks, pos, new Vector2(keyWidth, keyHeight), ">", BigFont, textColor, lightKeyColor, ">"));
                pos.X -= keyWidth + gapWidth;
                ks.Keys.Add(new Key(ks, pos, new Vector2(keyWidth, keyHeight), "<", BigFont, textColor, lightKeyColor, "<"));
                pos.X -= keyWidth + gapWidth;
                ks.Keys.Add(new Key(ks, pos, new Vector2(keyWidth, keyHeight), leftCircleTexture, textColor, darkKeyColor, OnNumpad0));

                // Third row of keys, right to left.
                pos.Y = height - 2 * gapWidth - 2 * keyHeight;
                pos.X = rightEdge;
                // For each key, offset pos by key and gap size then add key.
                pos.X -= keyWidth + gapWidth;
                // Skip enter key since it"s 2 rows tall.
                //ks.Keys.Add(new Key(ks, pos, new Vector2(keyWidth, 2 * keyHeight + gapWidth), enterTexture, textColor, lightKeyColor, Keys.Enter));
                pos.X -= keyWidth / 2.0f;   // Space between sections.
                pos.X -= keyWidth + gapWidth;
                ks.Keys.Add(new Key(ks, pos, new Vector2(keyWidth, keyHeight), "9", BigFont, textColor, lightKeyColor, "9"));
                pos.X -= keyWidth + gapWidth;
                ks.Keys.Add(new Key(ks, pos, new Vector2(keyWidth, keyHeight), "8", BigFont, textColor, lightKeyColor, "8"));
                pos.X -= keyWidth + gapWidth;
                ks.Keys.Add(new Key(ks, pos, new Vector2(keyWidth, keyHeight), "7", BigFont, textColor, lightKeyColor, "7"));
                pos.X -= keyWidth / 2.0f;   // Space between sections.
                pos.X -= keyWidth + gapWidth;
                ks.Keys.Add(new Key(ks, pos, new Vector2(keyWidth, keyHeight), "^", BigFont, textColor, lightKeyColor, "^"));
                pos.X -= keyWidth + gapWidth;
                ks.Keys.Add(new Key(ks, pos, new Vector2(keyWidth, keyHeight), "~", BigFont, textColor, lightKeyColor, "~"));
                pos.X -= keyWidth + gapWidth;
                ks.Keys.Add(new Key(ks, pos, new Vector2(keyWidth, keyHeight), "°", BigFont, textColor, lightKeyColor, "°"));
                pos.X -= keyWidth + gapWidth;
                ks.Keys.Add(new Key(ks, pos, new Vector2(keyWidth, keyHeight), "¶", BigFont, textColor, lightKeyColor, "¶"));
                pos.X -= keyWidth + gapWidth;
                ks.Keys.Add(new Key(ks, pos, new Vector2(keyWidth, keyHeight), "¦", BigFont, textColor, lightKeyColor, "¦"));
                pos.X -= keyWidth + gapWidth;
                ks.Keys.Add(new Key(ks, pos, new Vector2(keyWidth, keyHeight), "|", BigFont, textColor, lightKeyColor, "|"));
                pos.X -= keyWidth + gapWidth;
                ks.Keys.Add(new Key(ks, pos, new Vector2(keyWidth, keyHeight), rightCircleTexture, dimmedTextColor, darkKeyColor, OnNumpad1));

                // Fourth row of keys, right to left.
                pos.Y = height - 1 * gapWidth - 1 * keyHeight;
                pos.X = rightEdge;
                // For each key, offset pos by key and gap size then add key.
                pos.X -= keyWidth + gapWidth;
                ks.Keys.Add(new Key(ks, pos, new Vector2(keyWidth, keyHeight), null, textColor, darkKeyColor, ""));     // Blank where keyboard is.
                pos.X -= keyWidth / 2.0f;   // Space between sections.
                pos.X -= keyWidth + gapWidth;
                ks.Keys.Add(new Key(ks, pos, new Vector2(keyWidth, keyHeight), ".", BigFont, textColor, lightKeyColor, "."));
                pos.X -= 2.0f * (keyWidth + gapWidth);
                ks.Keys.Add(new Key(ks, pos, new Vector2(2.0f * keyWidth + gapWidth, keyHeight), "0", BigFont, textColor, lightKeyColor, "0"));
                pos.X -= keyWidth / 2.0f;   // Space between sections.
                pos.X -= 2.0f * (keyWidth + gapWidth);
                ks.Keys.Add(new Key(ks, pos, new Vector2(2.0f * keyWidth + gapWidth, keyHeight), "Space", SmallFont, textColor, lightKeyColor, " "));
                pos.X -= keyWidth + gapWidth;
                ks.Keys.Add(new Key(ks, pos, new Vector2(keyWidth, keyHeight), rightTexture, textColor, lightKeyColor, Keys.Right));
                pos.X -= keyWidth + gapWidth;
                ks.Keys.Add(new Key(ks, pos, new Vector2(keyWidth, keyHeight), leftTexture, textColor, lightKeyColor, Keys.Left));
                pos.X -= keyWidth + gapWidth;
                ks.Keys.Add(new Key(ks, pos, new Vector2(keyWidth, keyHeight), "<Kodu>", BigFont, textColor, darkKeyColor, OnSymbols0));
                pos.X -= keyWidth + gapWidth;
                ks.Keys.Add(new Key(ks, pos, new Vector2(keyWidth, keyHeight), "Ctrl", SmallFont, dimmedTextColor, darkKeyColor, OnCtrl));
                pos.X -= keyWidth + gapWidth;
                ks.Keys.Add(new Key(ks, pos, new Vector2(keyWidth, keyHeight), "&123", SmallFont, textColor, latchedKeyColor, OnNumpad0));

            }
            #endregion

            #region Symbols0KeySet
            {
                KeySet ks = new KeySet("Symbols0");
                keySets.Add(ks);

                // Start with close icon.  Should be same for all keysets.
                pos = new Vector2(rightEdge - keyWidth / 2.0f, closeOffset);
                ks.Keys.Add(new Key(ks, pos, new Vector2(64, 64), closeTexture, textColor, bkgColor, OnClose));

                // Top row of keys, right to left.
                pos.Y = height - 4 * gapWidth - 4 * keyHeight;
                pos.X = rightEdge;
                // For each key, offset pos by key and gap size then add key.
                pos.X -= keyWidth + gapWidth;
                ks.Keys.Add(new Key(ks, pos, new Vector2(keyWidth, keyHeight), backspaceTexture, textColor, darkKeyColor, Keys.Back));
                pos.X -= keyWidth + gapWidth;
                ks.Keys.Add(new Key(ks, pos, new Vector2(keyWidth, keyHeight), "<Balloon>", BigFont, textColor, lightKeyColor, "<Balloon>"));
                pos.X -= keyWidth + gapWidth;
                ks.Keys.Add(new Key(ks, pos, new Vector2(keyWidth, keyHeight), "<Blimp>", BigFont, textColor, lightKeyColor, "<Blimp>"));
                pos.X -= keyWidth + gapWidth;
                ks.Keys.Add(new Key(ks, pos, new Vector2(keyWidth, keyHeight), "<Boat>", BigFont, textColor, lightKeyColor, "<Boat>"));
                pos.X -= keyWidth + gapWidth;
                ks.Keys.Add(new Key(ks, pos, new Vector2(keyWidth, keyHeight), "<Kodu>", BigFont, textColor, lightKeyColor, "<Kodu>"));
                pos.X -= keyWidth + gapWidth;
                ks.Keys.Add(new Key(ks, pos, new Vector2(keyWidth, keyHeight), "<InkJet>", BigFont, textColor, lightKeyColor, "<InkJet>"));
                pos.X -= keyWidth + gapWidth;
                ks.Keys.Add(new Key(ks, pos, new Vector2(keyWidth, keyHeight), "<Jet>", BigFont, textColor, lightKeyColor, "<Jet>"));
                pos.X -= keyWidth + gapWidth;
                ks.Keys.Add(new Key(ks, pos, new Vector2(keyWidth, keyHeight), "<Light>", BigFont, textColor, lightKeyColor, "<Light>"));
                pos.X -= keyWidth + gapWidth;
                ks.Keys.Add(new Key(ks, pos, new Vector2(keyWidth, keyHeight), "<Mine>", BigFont, textColor, lightKeyColor, "<Mine>"));
                pos.X -= keyWidth + gapWidth;
                ks.Keys.Add(new Key(ks, pos, new Vector2(keyWidth, keyHeight), "<Octopus>", BigFont, textColor, lightKeyColor, "<Octopus>"));
                pos.X -= keyWidth + gapWidth;
                ks.Keys.Add(new Key(ks, pos, new Vector2(keyWidth, keyHeight), "<Pad>", BigFont, textColor, lightKeyColor, "<Pad>"));
                pos.X -= keyWidth + gapWidth;
                ks.Keys.Add(new Key(ks, pos, new Vector2(keyWidth, keyHeight), "Tab", SmallFont, textColor, darkKeyColor, "\t"));

                // Second row of keys, right to left.
                pos.Y = height - 3 * gapWidth - 3 * keyHeight;
                pos.X = rightEdge;
                // For each key, offset pos by key and gap size then add key.
                pos.X -= keyWidth + gapWidth;
                ks.Keys.Add(new Key(ks, pos, new Vector2(keyWidth, 2.0f * keyHeight + gapWidth), enterTexture, textColor, darkKeyColor, Keys.Enter));
                pos.X -= keyWidth + gapWidth;
                ks.Keys.Add(new Key(ks, pos, new Vector2(keyWidth, keyHeight), "<Puck>", BigFont, textColor, lightKeyColor, "<Puck>"));
                pos.X -= keyWidth + gapWidth;
                ks.Keys.Add(new Key(ks, pos, new Vector2(keyWidth, keyHeight), "<Rover>", BigFont, textColor, lightKeyColor, "<Rover>"));
                pos.X -= keyWidth + gapWidth;
                ks.Keys.Add(new Key(ks, pos, new Vector2(keyWidth, keyHeight), "<Satellite>", BigFont, textColor, lightKeyColor, "<Satellite>"));
                pos.X -= keyWidth + gapWidth;
                ks.Keys.Add(new Key(ks, pos, new Vector2(keyWidth, keyHeight), "<Saucer>", BigFont, textColor, lightKeyColor, "<Saucer>"));
                pos.X -= keyWidth + gapWidth;
                ks.Keys.Add(new Key(ks, pos, new Vector2(keyWidth, keyHeight), "<Starfish>", BigFont, textColor, lightKeyColor, "<Starfish>"));
                pos.X -= keyWidth + gapWidth;
                ks.Keys.Add(new Key(ks, pos, new Vector2(keyWidth, keyHeight), "<Stick>", BigFont, textColor, lightKeyColor, "<Stick>"));
                pos.X -= keyWidth + gapWidth;
                ks.Keys.Add(new Key(ks, pos, new Vector2(keyWidth, keyHeight), "<Sub>", BigFont, textColor, lightKeyColor, "<Sub>"));
                pos.X -= keyWidth + gapWidth;
                ks.Keys.Add(new Key(ks, pos, new Vector2(keyWidth, keyHeight), "<SwimFish>", BigFont, textColor, lightKeyColor, "<SwimFish>"));
                pos.X -= keyWidth + gapWidth;
                ks.Keys.Add(new Key(ks, pos, new Vector2(keyWidth, keyHeight), "<TerraCannon>", BigFont, textColor, lightKeyColor, "<TerraCannon>"));
                pos.X -= keyWidth + gapWidth;
                ks.Keys.Add(new Key(ks, pos, new Vector2(keyWidth, keyHeight), "<Turtle>", BigFont, textColor, lightKeyColor, "<Turtle>"));
                pos.X -= keyWidth + gapWidth;
                ks.Keys.Add(new Key(ks, pos, new Vector2(keyWidth, keyHeight), leftCircleTexture, dimmedTextColor, darkKeyColor, OnSymbols0));

                // Third row of keys, right to left.
                pos.Y = height - 2 * gapWidth - 2 * keyHeight;
                pos.X = rightEdge;
                // For each key, offset pos by key and gap size then add key.
                pos.X -= keyWidth + gapWidth;
                //ks.Keys.Add(new Key(ks, pos, new Vector2(enterWidth, 2.0f * keyHeight + gapWidth), enterTexture, textColor, darkKeyColor, Keys.Enter));
                pos.X -= keyWidth + gapWidth;
                //ks.Keys.Add(new Key(ks, pos, new Vector2(keyWidth, keyHeight), "<>", BigFont, textColor, lightKeyColor, "<>"));
                pos.X -= keyWidth + gapWidth;
                //ks.Keys.Add(new Key(ks, pos, new Vector2(keyWidth, keyHeight), "<>", BigFont, textColor, lightKeyColor, "<>"));
                pos.X -= keyWidth + gapWidth;
                //ks.Keys.Add(new Key(ks, pos, new Vector2(keyWidth, keyHeight), "<>", BigFont, textColor, lightKeyColor, "<>"));
                pos.X -= keyWidth + gapWidth;
                ks.Keys.Add(new Key(ks, pos, new Vector2(keyWidth, keyHeight), "<Wisp>", BigFont, textColor, lightKeyColor, "<Wisp>"));
                pos.X -= keyWidth + gapWidth;
                ks.Keys.Add(new Key(ks, pos, new Vector2(keyWidth, keyHeight), "<Drum>", BigFont, textColor, lightKeyColor, "<Drum>"));
                pos.X -= keyWidth + gapWidth;
                ks.Keys.Add(new Key(ks, pos, new Vector2(keyWidth, keyHeight), "<Fan>", BigFont, textColor, lightKeyColor, "<Fan>"));
                pos.X -= keyWidth + gapWidth;
                ks.Keys.Add(new Key(ks, pos, new Vector2(keyWidth, keyHeight), "<Fastbot>", BigFont, textColor, lightKeyColor, "<Fastbot>"));
                pos.X -= keyWidth + gapWidth;
                ks.Keys.Add(new Key(ks, pos, new Vector2(keyWidth, keyHeight), "<FlyFish>", BigFont, textColor, lightKeyColor, "<FlyFish>"));
                pos.X -= keyWidth + gapWidth;
                //ks.Keys.Add(new Key(ks, pos, new Vector2(keyWidth, keyHeight), "<>", BigFont, textColor, lightKeyColor, "<>"));
                pos.X -= keyWidth + gapWidth;
                //ks.Keys.Add(new Key(ks, pos, new Vector2(keyWidth, keyHeight), "<>", BigFont, textColor, lightKeyColor, "<>"));
                pos.X -= keyWidth + gapWidth;
                ks.Keys.Add(new Key(ks, pos, new Vector2(keyWidth, keyHeight), rightCircleTexture, textColor, darkKeyColor, OnSymbols1));

                // Fourth row of keys, right to left.
                pos.Y = height - 1 * gapWidth - 1 * keyHeight;
                pos.X = rightEdge;
                // For each key, offset pos by key and gap size then add key.
                pos.X -= keyWidth + gapWidth;
                ks.Keys.Add(new Key(ks, pos, new Vector2(keyWidth, keyHeight), null, textColor, darkKeyColor, ""));     // Blank where keyboard is.
                pos.X -= 2.0f * (keyWidth + gapWidth);
                ks.Keys.Add(new Key(ks, pos, new Vector2(2.0f * keyWidth + gapWidth, keyHeight), "Space", SmallFont, textColor, lightKeyColor, " "));
                pos.X -= keyWidth + gapWidth;
                //ks.Keys.Add(new Key(ks, pos, new Vector2(keyWidth, keyHeight), "", BigFont, dimmedTextColor, darkKeyColor, circleTexture, ""));
                pos.X -= keyWidth + gapWidth;
                //ks.Keys.Add(new Key(ks, pos, new Vector2(keyWidth, keyHeight), "", BigFont, dimmedTextColor, darkKeyColor, circleTexture, ""));
                pos.X -= keyWidth + gapWidth;
                //ks.Keys.Add(new Key(ks, pos, new Vector2(keyWidth, keyHeight), "", BigFont, dimmedTextColor, darkKeyColor, circleTexture, ""));
                pos.X -= keyWidth + gapWidth;
                //ks.Keys.Add(new Key(ks, pos, new Vector2(keyWidth, keyHeight), "", BigFont, dimmedTextColor, darkKeyColor, circleTexture, ""));
                pos.X -= keyWidth + gapWidth;
                ks.Keys.Add(new Key(ks, pos, new Vector2(keyWidth, keyHeight), "<Gamepad>", BigFont, textColor, darkKeyColor, circleTexture, OnSymbols2));
                pos.X -= keyWidth + gapWidth;
                ks.Keys.Add(new Key(ks, pos, new Vector2(keyWidth, keyHeight), "<Apple>", BigFont, textColor, darkKeyColor, circleTexture, OnSymbols1));
                pos.X -= keyWidth + gapWidth;
                ks.Keys.Add(new Key(ks, pos, new Vector2(keyWidth, keyHeight), "<Kodu>", BigFont, textColor, latchedKeyColor, circleTexture, OnSymbols0));
                pos.X -= keyWidth + gapWidth;
                ks.Keys.Add(new Key(ks, pos, new Vector2(keyWidth, keyHeight), "Ctrl", SmallFont, dimmedTextColor, darkKeyColor, OnCtrl));
                pos.X -= keyWidth + gapWidth;
                ks.Keys.Add(new Key(ks, pos, new Vector2(keyWidth, keyHeight), "&123", SmallFont, textColor, darkKeyColor, OnNumpad0));

            }
            #endregion

            #region Symbols1KeySet
            {
                KeySet ks = new KeySet("Symbols1");
                keySets.Add(ks);

                // Start with close icon.  Should be same for all keysets.
                pos = new Vector2(rightEdge - keyWidth / 2.0f, closeOffset);
                ks.Keys.Add(new Key(ks, pos, new Vector2(64, 64), closeTexture, textColor, bkgColor, OnClose));

                // Top row of keys, right to left.
                pos.Y = height - 4 * gapWidth - 4 * keyHeight;
                pos.X = rightEdge;
                // For each key, offset pos by key and gap size then add key.
                pos.X -= keyWidth + gapWidth;
                ks.Keys.Add(new Key(ks, pos, new Vector2(keyWidth, keyHeight), backspaceTexture, textColor, darkKeyColor, Keys.Back));
                pos.X -= keyWidth + gapWidth;
                //ks.Keys.Add(new Key(ks, pos, new Vector2(keyWidth, keyHeight), "<>", BigFont, textColor, lightKeyColor, "<>"));
                pos.X -= keyWidth + gapWidth;
                ks.Keys.Add(new Key(ks, pos, new Vector2(keyWidth, keyHeight), "<Apple>", BigFont, textColor, lightKeyColor, "<Apple>"));
                pos.X -= keyWidth + gapWidth;
                ks.Keys.Add(new Key(ks, pos, new Vector2(keyWidth, keyHeight), "<Ball>", BigFont, textColor, lightKeyColor, "<Ball>"));
                pos.X -= keyWidth + gapWidth;
                ks.Keys.Add(new Key(ks, pos, new Vector2(keyWidth, keyHeight), "<Bullet>", BigFont, textColor, lightKeyColor, "<Bullet>"));
                pos.X -= keyWidth + gapWidth;
                ks.Keys.Add(new Key(ks, pos, new Vector2(keyWidth, keyHeight), "<Castle>", BigFont, textColor, lightKeyColor, "<Castle>"));
                pos.X -= keyWidth + gapWidth;
                ks.Keys.Add(new Key(ks, pos, new Vector2(keyWidth, keyHeight), "<Clam>", BigFont, textColor, lightKeyColor, "<Clam>"));
                pos.X -= keyWidth + gapWidth;
                ks.Keys.Add(new Key(ks, pos, new Vector2(keyWidth, keyHeight), "<Cloud>", BigFont, textColor, lightKeyColor, "<Cloud>"));
                pos.X -= keyWidth + gapWidth;
                ks.Keys.Add(new Key(ks, pos, new Vector2(keyWidth, keyHeight), "<Coin>", BigFont, textColor, lightKeyColor, "<Coin>"));
                pos.X -= keyWidth + gapWidth;
                ks.Keys.Add(new Key(ks, pos, new Vector2(keyWidth, keyHeight), "<Factory>", BigFont, textColor, lightKeyColor, "<Factory>"));
                pos.X -= keyWidth + gapWidth;
                //ks.Keys.Add(new Key(ks, pos, new Vector2(keyWidth, keyHeight), "<>", BigFont, textColor, lightKeyColor, "<>"));
                pos.X -= keyWidth + gapWidth;
                ks.Keys.Add(new Key(ks, pos, new Vector2(keyWidth, keyHeight), "Tab", SmallFont, textColor, darkKeyColor, "\t"));

                // Second row of keys, right to left.
                pos.Y = height - 3 * gapWidth - 3 * keyHeight;
                pos.X = rightEdge;
                // For each key, offset pos by key and gap size then add key.
                pos.X -= keyWidth + gapWidth;
                ks.Keys.Add(new Key(ks, pos, new Vector2(keyWidth, 2.0f * keyHeight + gapWidth), enterTexture, textColor, darkKeyColor, Keys.Enter));
                pos.X -= keyWidth + gapWidth;
                //ks.Keys.Add(new Key(ks, pos, new Vector2(keyWidth, keyHeight), "<>", BigFont, textColor, lightKeyColor, "<>"));
                pos.X -= keyWidth + gapWidth;
                ks.Keys.Add(new Key(ks, pos, new Vector2(keyWidth, keyHeight), "<Heart>", BigFont, textColor, lightKeyColor, "<Heart>"));
                pos.X -= keyWidth + gapWidth;
                ks.Keys.Add(new Key(ks, pos, new Vector2(keyWidth, keyHeight), "<Hut>", BigFont, textColor, lightKeyColor, "<Hut>"));
                pos.X -= keyWidth + gapWidth;
                ks.Keys.Add(new Key(ks, pos, new Vector2(keyWidth, keyHeight), "<Lilypad>", BigFont, textColor, lightKeyColor, "<Lilypad>"));
                pos.X -= keyWidth + gapWidth;
                ks.Keys.Add(new Key(ks, pos, new Vector2(keyWidth, keyHeight), "<Missile>", BigFont, textColor, lightKeyColor, "<Missile>"));
                pos.X -= keyWidth + gapWidth;
                ks.Keys.Add(new Key(ks, pos, new Vector2(keyWidth, keyHeight), "<Pipe>", BigFont, textColor, lightKeyColor, "<Pipe>"));
                pos.X -= keyWidth + gapWidth;
                ks.Keys.Add(new Key(ks, pos, new Vector2(keyWidth, keyHeight), "<Seagrass>", BigFont, textColor, lightKeyColor, "<Seagrass>"));
                pos.X -= keyWidth + gapWidth;
                ks.Keys.Add(new Key(ks, pos, new Vector2(keyWidth, keyHeight), "<Star>", BigFont, textColor, lightKeyColor, "<Star>"));
                pos.X -= keyWidth + gapWidth;
                ks.Keys.Add(new Key(ks, pos, new Vector2(keyWidth, keyHeight), "<Tree>", BigFont, textColor, lightKeyColor, "<Tree>"));
                pos.X -= keyWidth + gapWidth;
                //ks.Keys.Add(new Key(ks, pos, new Vector2(keyWidth, keyHeight), "<>", BigFont, textColor, lightKeyColor, "<>"));
                pos.X -= keyWidth + gapWidth;
                ks.Keys.Add(new Key(ks, pos, new Vector2(keyWidth, keyHeight), leftCircleTexture, textColor, darkKeyColor, OnSymbols0));

                // Third row of keys, right to left.
                pos.Y = height - 2 * gapWidth - 2 * keyHeight;
                pos.X = rightEdge;
                // For each key, offset pos by key and gap size then add key.
                pos.X -= keyWidth + gapWidth;
                //ks.Keys.Add(new Key(ks, pos, new Vector2(enterWidth, 2.0f * keyHeight + gapWidth), enterTexture, textColor, darkKeyColor, Keys.Enter));
                pos.X -= keyWidth + gapWidth;
                //ks.Keys.Add(new Key(ks, pos, new Vector2(keyWidth, keyHeight), "<>", BigFont, textColor, lightKeyColor, "<>"));
                pos.X -= keyWidth + gapWidth;
                //ks.Keys.Add(new Key(ks, pos, new Vector2(keyWidth, keyHeight), "<>", BigFont, textColor, lightKeyColor, "<>"));
                pos.X -= keyWidth + gapWidth;
                ks.Keys.Add(new Key(ks, pos, new Vector2(keyWidth, keyHeight), "<Iceberg>", BigFont, textColor, lightKeyColor, "<iceberg>"));
                pos.X -= keyWidth + gapWidth;
                ks.Keys.Add(new Key(ks, pos, new Vector2(keyWidth, keyHeight), "<RockLowValueUnknown>", BigFont, textColor, lightKeyColor, "<RockLowValueUnknown>"));
                pos.X -= keyWidth + gapWidth;
                ks.Keys.Add(new Key(ks, pos, new Vector2(keyWidth, keyHeight), "<RockHighValueUnknown>", BigFont, textColor, lightKeyColor, "<RockHighValueUnknown>"));
                pos.X -= keyWidth + gapWidth;
                ks.Keys.Add(new Key(ks, pos, new Vector2(keyWidth, keyHeight), "<RockLowValue>", BigFont, textColor, lightKeyColor, "<RockLowValue>"));
                pos.X -= keyWidth + gapWidth;
                ks.Keys.Add(new Key(ks, pos, new Vector2(keyWidth, keyHeight), "<RockHighValue>", BigFont, textColor, lightKeyColor, "<RockHighValue>"));
                pos.X -= keyWidth + gapWidth;
                ks.Keys.Add(new Key(ks, pos, new Vector2(keyWidth, keyHeight), "<Rock>", BigFont, textColor, lightKeyColor, "<Rock>"));
                pos.X -= keyWidth + gapWidth;
                //ks.Keys.Add(new Key(ks, pos, new Vector2(keyWidth, keyHeight), "<>", BigFont, textColor, lightKeyColor, "<>"));
                pos.X -= keyWidth + gapWidth;
                //ks.Keys.Add(new Key(ks, pos, new Vector2(keyWidth, keyHeight), "<>", BigFont, textColor, lightKeyColor, "<>"));
                pos.X -= keyWidth + gapWidth;
                ks.Keys.Add(new Key(ks, pos, new Vector2(keyWidth, keyHeight), rightCircleTexture, textColor, darkKeyColor, OnSymbols2));

                // Fourth row of keys, right to left.
                pos.Y = height - 1 * gapWidth - 1 * keyHeight;
                pos.X = rightEdge;
                // For each key, offset pos by key and gap size then add key.
                pos.X -= keyWidth + gapWidth;
                ks.Keys.Add(new Key(ks, pos, new Vector2(keyWidth, keyHeight), null, textColor, darkKeyColor, ""));     // Blank where keyboard is.
                pos.X -= 2.0f * (keyWidth + gapWidth);
                ks.Keys.Add(new Key(ks, pos, new Vector2(2.0f * keyWidth + gapWidth, keyHeight), "Space", SmallFont, textColor, lightKeyColor, " "));
                pos.X -= keyWidth + gapWidth;
                //ks.Keys.Add(new Key(ks, pos, new Vector2(keyWidth, keyHeight), "", BigFont, dimmedTextColor, darkKeyColor, circleTexture, ""));
                pos.X -= keyWidth + gapWidth;
                //ks.Keys.Add(new Key(ks, pos, new Vector2(keyWidth, keyHeight), "", BigFont, dimmedTextColor, darkKeyColor, circleTexture, ""));
                pos.X -= keyWidth + gapWidth;
                //ks.Keys.Add(new Key(ks, pos, new Vector2(keyWidth, keyHeight), "", BigFont, dimmedTextColor, darkKeyColor, circleTexture, ""));
                pos.X -= keyWidth + gapWidth;
                //ks.Keys.Add(new Key(ks, pos, new Vector2(keyWidth, keyHeight), "", BigFont, dimmedTextColor, darkKeyColor, circleTexture, ""));
                pos.X -= keyWidth + gapWidth;
                ks.Keys.Add(new Key(ks, pos, new Vector2(keyWidth, keyHeight), "<Gamepad>", BigFont, textColor, darkKeyColor, circleTexture, OnSymbols2));
                pos.X -= keyWidth + gapWidth;
                ks.Keys.Add(new Key(ks, pos, new Vector2(keyWidth, keyHeight), "<Apple>", BigFont, textColor, latchedKeyColor, circleTexture, OnSymbols1));
                pos.X -= keyWidth + gapWidth;
                ks.Keys.Add(new Key(ks, pos, new Vector2(keyWidth, keyHeight), "<Kodu>", BigFont, textColor, darkKeyColor, circleTexture, OnSymbols0));
                pos.X -= keyWidth + gapWidth;
                ks.Keys.Add(new Key(ks, pos, new Vector2(keyWidth, keyHeight), "Ctrl", SmallFont, dimmedTextColor, darkKeyColor, OnCtrl));
                pos.X -= keyWidth + gapWidth;
                ks.Keys.Add(new Key(ks, pos, new Vector2(keyWidth, keyHeight), "&123", SmallFont, textColor, darkKeyColor, OnNumpad0));

            }
            #endregion

            #region Symbols2KeySet
            {
                KeySet ks = new KeySet("Symbols2");
                keySets.Add(ks);

                // Start with close icon.  Should be same for all keysets.
                pos = new Vector2(rightEdge - keyWidth / 2.0f, closeOffset);
                ks.Keys.Add(new Key(ks, pos, new Vector2(64, 64), closeTexture, textColor, bkgColor, OnClose));

                // Top row of keys, right to left.
                pos.Y = height - 4 * gapWidth - 4 * keyHeight;
                pos.X = rightEdge;
                // For each key, offset pos by key and gap size then add key.
                pos.X -= keyWidth + gapWidth;
                ks.Keys.Add(new Key(ks, pos, new Vector2(keyWidth, keyHeight), backspaceTexture, textColor, darkKeyColor, Keys.Back));
                pos.X -= keyWidth + gapWidth;
                ks.Keys.Add(new Key(ks, pos, new Vector2(keyWidth, keyHeight), "<DoubleTap>", BigFont, textColor, lightKeyColor, "<DoubleTap>"));
                pos.X -= keyWidth + gapWidth;
                ks.Keys.Add(new Key(ks, pos, new Vector2(keyWidth, keyHeight), "<Tap>", BigFont, textColor, lightKeyColor, "<Tap>"));
                pos.X -= keyWidth + gapWidth;
                ks.Keys.Add(new Key(ks, pos, new Vector2(keyWidth, keyHeight), "<MiddleMouse>", BigFont, textColor, lightKeyColor, "<MiddleMouse>"));
                pos.X -= keyWidth + gapWidth;
                ks.Keys.Add(new Key(ks, pos, new Vector2(keyWidth, keyHeight), "<Mouse>", BigFont, textColor, lightKeyColor, "<Mouse>"));
                pos.X -= keyWidth + gapWidth;
                ks.Keys.Add(new Key(ks, pos, new Vector2(keyWidth, keyHeight), "<DpadDown>", BigFont, textColor, lightKeyColor, "<DpadDown>"));
                pos.X -= keyWidth + gapWidth;
                ks.Keys.Add(new Key(ks, pos, new Vector2(keyWidth, keyHeight), "<DpadUp>", BigFont, textColor, lightKeyColor, "<DpadUp>"));
                pos.X -= keyWidth + gapWidth;
                ks.Keys.Add(new Key(ks, pos, new Vector2(keyWidth, keyHeight), "<RightStick>", BigFont, textColor, lightKeyColor, "<RightStick>"));
                pos.X -= keyWidth + gapWidth;
                ks.Keys.Add(new Key(ks, pos, new Vector2(keyWidth, keyHeight), "<LeftStick>", BigFont, textColor, lightKeyColor, "<LeftStick>"));
                pos.X -= keyWidth + gapWidth;
                ks.Keys.Add(new Key(ks, pos, new Vector2(keyWidth, keyHeight), "<Keyboard>", BigFont, textColor, lightKeyColor, "<Keyboard>"));
                pos.X -= keyWidth + gapWidth;
                ks.Keys.Add(new Key(ks, pos, new Vector2(keyWidth, keyHeight), "<Gamepad>", BigFont, textColor, lightKeyColor, "<Gamepad>"));
                pos.X -= keyWidth + gapWidth;
                ks.Keys.Add(new Key(ks, pos, new Vector2(keyWidth, keyHeight), "Tab", SmallFont, textColor, darkKeyColor, "\t"));

                // Second row of keys, right to left.
                pos.Y = height - 3 * gapWidth - 3 * keyHeight;
                pos.X = rightEdge;
                // For each key, offset pos by key and gap size then add key.
                pos.X -= keyWidth + gapWidth;
                ks.Keys.Add(new Key(ks, pos, new Vector2(keyWidth, 2.0f * keyHeight + gapWidth), enterTexture, textColor, darkKeyColor, Keys.Enter));
                pos.X -= keyWidth + gapWidth;
                ks.Keys.Add(new Key(ks, pos, new Vector2(keyWidth, keyHeight), "<DoubleDrag>", BigFont, textColor, lightKeyColor, "<DoubleDrag>"));
                pos.X -= keyWidth + gapWidth;
                ks.Keys.Add(new Key(ks, pos, new Vector2(keyWidth, keyHeight), "<Drag>", BigFont, textColor, lightKeyColor, "<Drag>"));
                pos.X -= keyWidth + gapWidth;
                ks.Keys.Add(new Key(ks, pos, new Vector2(keyWidth, keyHeight), "<RightMouse>", BigFont, textColor, lightKeyColor, "<RightMouse>"));
                pos.X -= keyWidth + gapWidth;
                ks.Keys.Add(new Key(ks, pos, new Vector2(keyWidth, keyHeight), "<LeftMouse>", BigFont, textColor, lightKeyColor, "<LeftMouse>"));
                pos.X -= keyWidth + gapWidth;
                ks.Keys.Add(new Key(ks, pos, new Vector2(keyWidth, keyHeight), "<DpadRight>", BigFont, textColor, lightKeyColor, "<DpadRight>"));
                pos.X -= keyWidth + gapWidth;
                ks.Keys.Add(new Key(ks, pos, new Vector2(keyWidth, keyHeight), "<DpadLeft>", BigFont, textColor, lightKeyColor, "<DpadLeft>"));
                pos.X -= keyWidth + gapWidth;
                ks.Keys.Add(new Key(ks, pos, new Vector2(keyWidth, keyHeight), "<RightTrigger>", BigFont, textColor, lightKeyColor, "<RightTrigger>"));
                pos.X -= keyWidth + gapWidth;
                ks.Keys.Add(new Key(ks, pos, new Vector2(keyWidth, keyHeight), "<LeftTrigger>", BigFont, textColor, lightKeyColor, "<LeftTrigger>"));
                pos.X -= keyWidth + gapWidth;
                ks.Keys.Add(new Key(ks, pos, new Vector2(keyWidth, keyHeight), "<b>", BigFont, textColor, lightKeyColor, "<b>"));
                pos.X -= keyWidth + gapWidth;
                ks.Keys.Add(new Key(ks, pos, new Vector2(keyWidth, keyHeight), "<a>", BigFont, textColor, lightKeyColor, "<a>"));
                pos.X -= keyWidth + gapWidth;
                ks.Keys.Add(new Key(ks, pos, new Vector2(keyWidth, keyHeight), leftCircleTexture, textColor, darkKeyColor, OnSymbols1));

                // Third row of keys, right to left.
                pos.Y = height - 2 * gapWidth - 2 * keyHeight;
                pos.X = rightEdge;
                // For each key, offset pos by key and gap size then add key.
                pos.X -= keyWidth + gapWidth;
                //ks.Keys.Add(new Key(ks, pos, new Vector2(enterWidth, 2.0f * keyHeight + gapWidth), enterTexture, textColor, darkKeyColor, Keys.Enter));
                pos.X -= keyWidth + gapWidth;
                ks.Keys.Add(new Key(ks, pos, new Vector2(keyWidth, keyHeight), "<Rotate>", BigFont, textColor, lightKeyColor, "<Rotate>"));
                pos.X -= keyWidth + gapWidth;
                ks.Keys.Add(new Key(ks, pos, new Vector2(keyWidth, keyHeight), "<Pinch>", BigFont, textColor, lightKeyColor, "<Pinch>"));
                pos.X -= keyWidth + gapWidth;
                ks.Keys.Add(new Key(ks, pos, new Vector2(keyWidth, keyHeight), "<TouchHold>", BigFont, textColor, lightKeyColor, "<TouchHold>"));
                pos.X -= keyWidth + gapWidth;
                ks.Keys.Add(new Key(ks, pos, new Vector2(keyWidth, keyHeight), "<Cursor>", BigFont, textColor, lightKeyColor, "<Cursor>"));
                pos.X -= keyWidth + gapWidth;
                ks.Keys.Add(new Key(ks, pos, new Vector2(keyWidth, keyHeight), "<Start>", BigFont, textColor, lightKeyColor, "<Start>"));
                pos.X -= keyWidth + gapWidth;
                ks.Keys.Add(new Key(ks, pos, new Vector2(keyWidth, keyHeight), "<Back>", BigFont, textColor, lightKeyColor, "<Back>"));
                pos.X -= keyWidth + gapWidth;
                ks.Keys.Add(new Key(ks, pos, new Vector2(keyWidth, keyHeight), "<RightShoulder>", BigFont, textColor, lightKeyColor, "<RightShoulder>"));
                pos.X -= keyWidth + gapWidth;
                ks.Keys.Add(new Key(ks, pos, new Vector2(keyWidth, keyHeight), "<LeftShoulder>", BigFont, textColor, lightKeyColor, "<LeftShoulder>"));
                pos.X -= keyWidth + gapWidth;
                ks.Keys.Add(new Key(ks, pos, new Vector2(keyWidth, keyHeight), "<y>", BigFont, textColor, lightKeyColor, "<y>"));
                pos.X -= keyWidth + gapWidth;
                ks.Keys.Add(new Key(ks, pos, new Vector2(keyWidth, keyHeight), "<x>", BigFont, textColor, lightKeyColor, "<x>"));
                pos.X -= keyWidth + gapWidth;
                ks.Keys.Add(new Key(ks, pos, new Vector2(keyWidth, keyHeight), rightCircleTexture, dimmedTextColor, darkKeyColor, OnSymbols1));

                // Fourth row of keys, right to left.
                pos.Y = height - 1 * gapWidth - 1 * keyHeight;
                pos.X = rightEdge;
                // For each key, offset pos by key and gap size then add key.
                pos.X -= keyWidth + gapWidth;
                ks.Keys.Add(new Key(ks, pos, new Vector2(keyWidth, keyHeight), null, textColor, darkKeyColor, ""));     // Blank where keyboard is.
                pos.X -= 2.0f * (keyWidth + gapWidth);
                ks.Keys.Add(new Key(ks, pos, new Vector2(2.0f * keyWidth + gapWidth, keyHeight), "Space", SmallFont, textColor, lightKeyColor, " "));
                pos.X -= keyWidth + gapWidth;
                //ks.Keys.Add(new Key(ks, pos, new Vector2(keyWidth, keyHeight), "", BigFont, dimmedTextColor, darkKeyColor, circleTexture, ""));
                pos.X -= keyWidth + gapWidth;
                //ks.Keys.Add(new Key(ks, pos, new Vector2(keyWidth, keyHeight), "", BigFont, dimmedTextColor, darkKeyColor, circleTexture, ""));
                pos.X -= keyWidth + gapWidth;
                //ks.Keys.Add(new Key(ks, pos, new Vector2(keyWidth, keyHeight), "", BigFont, dimmedTextColor, darkKeyColor, circleTexture, ""));
                pos.X -= keyWidth + gapWidth;
                //ks.Keys.Add(new Key(ks, pos, new Vector2(keyWidth, keyHeight), "", BigFont, dimmedTextColor, darkKeyColor, circleTexture, ""));
                pos.X -= keyWidth + gapWidth;
                ks.Keys.Add(new Key(ks, pos, new Vector2(keyWidth, keyHeight), "<Gamepad>", BigFont, textColor, latchedKeyColor, circleTexture, OnSymbols2));
                pos.X -= keyWidth + gapWidth;
                ks.Keys.Add(new Key(ks, pos, new Vector2(keyWidth, keyHeight), "<Apple>", BigFont, textColor, darkKeyColor, circleTexture, OnSymbols1));
                pos.X -= keyWidth + gapWidth;
                ks.Keys.Add(new Key(ks, pos, new Vector2(keyWidth, keyHeight), "<Kodu>", BigFont, textColor, darkKeyColor, circleTexture, OnSymbols0));
                pos.X -= keyWidth + gapWidth;
                ks.Keys.Add(new Key(ks, pos, new Vector2(keyWidth, keyHeight), "Ctrl", SmallFont, dimmedTextColor, darkKeyColor, OnCtrl));
                pos.X -= keyWidth + gapWidth;
                ks.Keys.Add(new Key(ks, pos, new Vector2(keyWidth, keyHeight), "&123", SmallFont, textColor, darkKeyColor, OnNumpad0));

            }
            #endregion

        }   // end of c'tor

        public static void Activate()
        {
            if (!active)
            {
                rt = SharedX.RenderTarget1920_540;

                // Twitch to move keyboard offscreen.
                {
                    Vector2 targetPosition = new Vector2(0, BokuGame.ScreenSize.Y - height);
                    TwitchManager.Set<Vector2> set = delegate(Vector2 val, Object param) { position = val; };
                    TwitchManager.CreateTwitch<Vector2>(position, targetPosition, set, 0.25f, TwitchCurve.Shape.EaseInOut);
                }

                // Reset default.
                SwitchToKeySet("LowerCase");

                visible = true;
                active = true;
                dirty = true;
            }
        }   // end of Activate()

        public static void Deactivate()
        {
            if (active)
            {
                if (visible)
                {
                    // Twitch to move keyboard offscreen.
                    {
                        Vector2 targetPosition = new Vector2(0, BokuGame.ScreenSize.Y);
                        TwitchManager.Set<Vector2> set = delegate(Vector2 val, Object param) { position = val; };
                        TwitchManager.CreateTwitch<Vector2>(position, targetPosition, set, 0.25f, TwitchCurve.Shape.EaseInOut, null, OnDeactivateComplete);
                    }
                }
                active = false;
            }
        }   // end of Deactivate()

        static void OnDeactivateComplete(object param)
        {
            visible = false;
        }

        /// <summary>
        /// Updates the state of the virutal keyboard.
        /// </summary>
        /// <returns>string of input characters.</returns>
        public static Key Update()
        {
            Key result = null;

            //Time.DebugString = "screenSize = " + BokuGame.ScreenSize.ToString() + "  position = " + position.ToString() + " renderPosition = " + renderPosition.ToString() + " scale = " + renderScale.ToString();

            if (active)
            {
                hitBox.Set(position, position + new Vector2(width, height));

                TouchContact touch = TouchInput.GetOldestTouch();
                if(touch != null && touch.phase == TouchPhase.Began)
                {
                    Vector2 hitPosition;
                    if (renderScale == 1.0f)
                    {
                        // Keyboard is fullsize, just need to adjust for position.
                        hitPosition = touch.position - renderPosition;
                    }
                    else
                    {
                        // Keyboard is being shrunk.
                        hitPosition = (touch.position - renderPosition) / renderScale;
                    }

                    result = curKeySet.HitTest(hitPosition);
                }

                Key autorepeatKey = curKeySet.Update();

                if (result == null)
                {
                    result = autorepeatKey;
                }
            }

            if (visible && dirty)
            {
                RefreshTexture();
            }

            return result;
        }   // end of Update()

        public static void Render()
        {
            // Debug only.  Force dirty flag on so we re-render each frame.
            //dirty = true;
            if (visible)
            {
                // Render the rt texture at the current position.
                SpriteBatch batch = KoiLibrary.SpriteBatch;
                batch.Begin(SpriteSortMode.Deferred, BlendState.NonPremultiplied);

                // Calc actual size and position to render in.  Save these away because
                // we need to use them to adjust the touch locations to match.
                renderPosition = position;
                renderSize = new Vector2(width, height);

                // Adjust if screen is not expected size.
                renderScale = 1.0f;
                if (BokuGame.ScreenSize.X > width)
                {
                    // Center keyboard on wider screen.
                    renderPosition.X = (BokuGame.ScreenSize.X - width) / 2.0f;
                }
                else if (BokuGame.ScreenSize.X < width)
                {
                    renderScale = BokuGame.ScreenSize.X / width;

                    // Shrink keyboard to fit.
                    renderSize.X = BokuGame.ScreenSize.X;
                    renderSize.Y *= renderScale;
                    renderPosition.Y += (1 - renderScale) * height;
                }

                Rectangle srcRect = new Rectangle(0, 0, width, height);
                Rectangle dstRect = new Rectangle((int)renderPosition.X, (int)renderPosition.Y, (int)renderSize.X, (int)renderSize.Y);
                batch.Draw(rt, dstRect, srcRect, Color.White);
                batch.End();
            }
        }   // end of Render()

        //
        // Key delegates.
        //
        public static void OnClose(Key key)
        {
            Deactivate();
        }

        public static void OnShift(Key key)
        {
            if (curKeySet.Name == "LowerCase")
            {
                SwitchToKeySet("UpperCase");
            }
            else if (curKeySet.Name == "UpperCase")
            {
                SwitchToKeySet("LowerCase");
            }
            else
            {
                Debug.Assert(false);
            }
        }

        public static void OnCtrl(Key key)
        {
            if (curKeySet.Name == "Ctrl")
            {
                SwitchToKeySet("LowerCase");
            }
            else
            {
                SwitchToKeySet("Ctrl");
            }
        }

        public static void OnSymbols0(Key key)
        {
            if (curKeySet.Name == "Symbols0")
            {
                SwitchToKeySet("LowerCase");
            }
            else
            {
                SwitchToKeySet("Symbols0");
            }
        }

        public static void OnSymbols1(Key key)
        {
            if (curKeySet.Name == "Symbols1")
            {
                SwitchToKeySet("LowerCase");
            }
            else
            {
                SwitchToKeySet("Symbols1");
            }
        }

        public static void OnSymbols2(Key key)
        {
            if (curKeySet.Name == "Symbols2")
            {
                SwitchToKeySet("LowerCase");
            }
            else
            {
                SwitchToKeySet("Symbols2");
            }
        }

        public static void OnNumpad0(Key key)
        {
            if (curKeySet.Name == "Numpad0")
            {
                SwitchToKeySet("LowerCase");
            }
            else
            {
                SwitchToKeySet("Numpad0");
            }
        }

        public static void OnNumpad1(Key key)
        {
            if (curKeySet.Name == "Numpad1")
            {
                SwitchToKeySet("LowerCase");
            }
            else
            {
                SwitchToKeySet("Numpad1");
            }
        }

        #endregion



        #region Internal

        static void SwitchToKeySet(string name)
        {
            curKeySet = null;
            foreach (KeySet ks in keySets)
            {
                if (ks.Name == name)
                {
                    curKeySet = ks;
                    break;
                }
            }

            Debug.Assert(curKeySet != null);

            dirty = true;

        }   // end of SwitchToKeySet()
            
        static void RefreshTexture()
        {
            // Render the keyboard into our rendertarget.
            InGame.SetRenderTarget(rt);
            InGame.Clear(bkgColor);

            curKeySet.Render();

            InGame.RestoreRenderTarget();

            dirty = false;
        }   // end of RefreshTexture()


        public static void LoadContent(bool immediate)
        {
            if (Font == null)
            {
                Font = SharedX.GetSegoeUI30;
            }
            if (whiteTexture == null)
            {
                whiteTexture = KoiLibrary.LoadTexture2D(@"Textures\White");
            }

            if (circleTexture == null)
            {
                circleTexture = KoiLibrary.LoadTexture2D(@"Textures\VirtualKeyboard\Circle");
            }
            if (backspaceTexture == null)
            {
                backspaceTexture = KoiLibrary.LoadTexture2D(@"Textures\VirtualKeyboard\Backspace");
            }
            if(closeTexture == null)
            {
                closeTexture = KoiLibrary.LoadTexture2D(@"Textures\VirtualKeyboard\Close");
            }
            if(enterTexture == null)
            {
                enterTexture = KoiLibrary.LoadTexture2D(@"Textures\VirtualKeyboard\Enter");
            }
            if (shiftTexture == null)
            {
                shiftTexture = KoiLibrary.LoadTexture2D(@"Textures\VirtualKeyboard\Shift");
            }
            if (leftTexture == null)
            {
                leftTexture = KoiLibrary.LoadTexture2D(@"Textures\VirtualKeyboard\Left");
            }
            if(rightTexture == null)
            {
                rightTexture = KoiLibrary.LoadTexture2D(@"Textures\VirtualKeyboard\Right");
            }
            if(leftCircleTexture == null)
            {
                leftCircleTexture = KoiLibrary.LoadTexture2D(@"Textures\VirtualKeyboard\LeftCircle");
            }
            if(rightCircleTexture == null)
            {
                rightCircleTexture = KoiLibrary.LoadTexture2D(@"Textures\VirtualKeyboard\RightCircle");
            }

            // Now that we have all the textures, call Init()
            Init();
        }

        public static void InitDeviceResources(GraphicsDevice device)
        {
        }

        public static void UnloadContent()
        {
            Font = null;
            DeviceResetX.Release(ref whiteTexture);

            DeviceResetX.Release(ref circleTexture);
            DeviceResetX.Release(ref backspaceTexture);
            DeviceResetX.Release(ref closeTexture);
            DeviceResetX.Release(ref enterTexture);
            DeviceResetX.Release(ref shiftTexture);
            DeviceResetX.Release(ref leftTexture);
            DeviceResetX.Release(ref rightTexture);
            DeviceResetX.Release(ref leftCircleTexture);
            DeviceResetX.Release(ref rightCircleTexture);
        }

        public static void DeviceReset(GraphicsDevice device)
        {
        }

        #endregion

    }   // end of class VirtualKeyboard

}   // end of namespace Boku.Input
