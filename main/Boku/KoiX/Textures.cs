// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;

using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Storage;

using KoiX.Input;

namespace KoiX
{
    /// <summary>
    /// Static class with a bunch of standard textures.
    /// Id strings are made up of the group name plus the individual name with a seperating space.
    /// For instance "GamePad LeftStick" will get the LeftStick texture.
    /// The id is case sensitive.  
    /// 
    /// GamePad:
    ///     A, B, X, Y, Start, Back
    ///     DPad, DpadUp, DpadDown, DpadLeft, DpadRight
    ///     LeftStick, RightStick
    ///     LeftBumper, RightBumper
    ///     LeftTrigger, RightTrigger
    /// </summary>
    public static class Textures
    {
        static Dictionary<string, Texture2D> textures;

        class TextureData
        {
            public string id;
            public string assetName;

            public TextureData(string id, string assetName)
            {
                this.id = id;
                this.assetName = assetName;
            }
        }

        static TextureData[] data = 
        {
            new TextureData("White", @"KoiXContent\Textures\White"),
            new TextureData("QuestionMark", @"KoiXContent\Textures\QuestionMark64"),

            new TextureData("GamePad A", @"KoiXContent\Textures\GamePad\A"),
            new TextureData("GamePad B", @"KoiXContent\Textures\GamePad\B"),
            new TextureData("GamePad X", @"KoiXContent\Textures\GamePad\X"),
            new TextureData("GamePad Y", @"KoiXContent\Textures\GamePad\Y"),
            new TextureData("GamePad Start", @"KoiXContent\Textures\GamePad\Start"),
            new TextureData("GamePad Back", @"KoiXContent\Textures\GamePad\Back"),
            new TextureData("GamePad DPad", @"KoiXContent\Textures\GamePad\DPad"),
            new TextureData("GamePad DPadUp", @"KoiXContent\Textures\GamePad\DPadUp"),
            new TextureData("GamePad DPadDown", @"KoiXContent\Textures\GamePad\DPadDown"),
            new TextureData("GamePad DPadRight", @"KoiXContent\Textures\GamePad\DPadRight"),
            new TextureData("GamePad DPadLeft", @"KoiXContent\Textures\GamePad\DPadLeft"),
            new TextureData("GamePad LeftStick", @"KoiXContent\Textures\GamePad\LeftStick"),
            new TextureData("GamePad RightStick", @"KoiXContent\Textures\GamePad\RightStick"),
            new TextureData("GamePad LeftBumper", @"KoiXContent\Textures\GamePad\LeftBumper"),
            new TextureData("GamePad RightBumper", @"KoiXContent\Textures\GamePad\RightBumper"),
            new TextureData("GamePad LeftTrigger", @"KoiXContent\Textures\GamePad\LeftTrigger"),
            new TextureData("GamePad RightTrigger", @"KoiXContent\Textures\GamePad\RightTrigger"),

        };

        /// <summary>
        /// Init the cached textures.  This may be called multiple times if either
        /// the textures or the device for the textures is disposed.
        /// </summary>
        public static void Init()
        {
            if (textures != null)
            {
                textures.Clear();
            }
            else
            {
                textures = new Dictionary<string, Texture2D>();
            }

            foreach (TextureData td in data)
            {
                Texture2D texture = KoiLibrary.LoadTexture2D(td.assetName);

                Debug.Assert(texture != null, "Missing texture");

                textures.Add(td.id, texture);
            }
        }   // end of Init()

        /// <summary>
        /// Given a texture id, returns the matching texture, null if not found.
        /// id is case sensitive.
        /// 
        /// TODO (****) Right now the default size of the button textures is 64x64.
        /// Should we go higher so zooming looks better?
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        public static Texture2D Get(string id)
        {
            if (textures == null)
            {
                Init();
            }

            Texture2D result = null;

            if (!textures.TryGetValue(id, out result))
            {
                Debug.Assert(false, "Invalid id");
            }

            if (result.IsDisposed || result.GraphicsDevice.IsDisposed)
            {
                Init();

                // One more time!
                if (!textures.TryGetValue(id, out result))
                {
                    Debug.Assert(false, "Invalid id");
                }
            }

            Debug.Assert(result != null);
            Debug.Assert(!result.IsDisposed);
            Debug.Assert(!result.GraphicsDevice.IsDisposed);

            return result;
        }   // end of Get()

        /// <summary>
        /// Gets the gamepad icon associated with the input element.
        /// </summary>
        /// <param name="element"></param>
        /// <returns>null if not found or texture is not valid.</returns>
        public static Texture2D Get(GamePadInput.Element element)
        {
            if (textures == null)
            {
                Init();
            }

            Texture2D result = null;

            // TODO (****) Should we change this around so that the dictionary is keyed
            // to the enum?  We could just remove the stirng version although then this
            // wouldn't work as well with White, etc.  With string interning either way
            // should be as fast so maybe remove this Get based on enums.

            switch(element)
            {
                case GamePadInput.Element.AButton: result = Get("GamePad A"); break;
                case GamePadInput.Element.BButton: result = Get("GamePad B"); break;
                case GamePadInput.Element.XButton: result = Get("GamePad X"); break;
                case GamePadInput.Element.YButton: result = Get("GamePad Y"); break;
                case GamePadInput.Element.Start: result = Get("GamePad Start"); break;
                case GamePadInput.Element.Back: result = Get("GamePad Back"); break;
                case GamePadInput.Element.DPad: result = Get("GamePad DPad"); break;
                case GamePadInput.Element.LeftStick: result = Get("GamePad LeftStick"); break;
                case GamePadInput.Element.RightStick: result = Get("GamePad RightStick"); break;
                case GamePadInput.Element.LeftBumper: result = Get("GamePad LeftBumper"); break;
                case GamePadInput.Element.RightBumper: result = Get("GamePad RightBumper"); break;
                case GamePadInput.Element.LeftTrigger: result = Get("GamePad LeftTrigger"); break;
                case GamePadInput.Element.RightTrigger: result = Get("GamePad RightTrigger"); break;
            }

            Debug.Assert(result != null || element == GamePadInput.Element.None);
            Debug.Assert(result == null || !result.GraphicsDevice.IsDisposed);

            // If the result isn't usable, return null.
            if (result == null || result.IsDisposed || result.GraphicsDevice.IsDisposed)
            {
                result = null;
            }

            return result;
        }   // end of Get()

    }   // end of class Textures

}   // end of namespace KoiX
