
using System;
using System.Collections;
using System.Diagnostics;

using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Audio;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using Microsoft.Xna.Framework.Storage;

namespace Boku.Common
{
    /// <summary>
    /// Static class designed to act as a helper class for dealing with the
    /// screen scaling and positioning due to window resizing and tutorial mode.
    /// Needs a way to "clear" this setting for when not rendering to a render target.
    /// </summary>
    public static class ScreenWarp
    {
        public static float RenderScale = 1.0f;
        public static Vector2 RenderPosition;
        public static Vector2 RenderSize;

        public static Vector2 RTSize;   // Useful for debugging.

        /// <summary>
        /// Sets values to fit the given rtSize to the current screen.
        /// </summary>
        /// <param name="rtSize"></param>
        public static void FitRtToScreen(Vector2 rtSize)
        {
            Vector2 screenSize = BokuGame.ScreenSize;
            Vector2 ratios = rtSize / screenSize;
            RenderScale = Math.Max(ratios.X, ratios.Y);
            RenderSize = rtSize / RenderScale;
            RenderPosition = (screenSize - RenderSize) / 2.0f;

            // Ensure integrer coords for cleaner rendering.
            RenderPosition.X = (int)RenderPosition.X;
            RenderPosition.Y = (int)RenderPosition.Y;
            RenderSize.X = (int)RenderSize.X;
            RenderSize.Y = (int)RenderSize.Y;

            RTSize = rtSize;
        }   // end of FitRtToScreen()

        /// <summary>
        /// Converts a screen coordinate into RenderTarget coordinates
        /// based on the current warp settings.
        /// </summary>
        /// <param name="screen"></param>
        /// <returns></returns>
        public static Vector2 ScreenToRT(Vector2 screen)
        {
            Vector2 result = screen;

            result = (result - RenderPosition) * RenderScale;

            return result;
        }   // end of ScreenToRT()

    }   // end of class ScreenWarp
}   // end of namespace Boku.Common
