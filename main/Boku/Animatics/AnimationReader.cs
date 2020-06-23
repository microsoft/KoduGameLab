using System;
using System.Collections.Generic;
using System.Diagnostics;

using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;

namespace Boku.Animatics
{
    internal class AnimationReader : ContentTypeReader<Animation.Dict>
    {
        /// <summary>
        /// Pass input stream off to animation dictionary for construct/load.
        /// </summary>
        /// <param name="input"></param>
        /// <param name="existingInstance"></param>
        /// <returns></returns>
        protected override Animation.Dict Read(ContentReader input, Animation.Dict existingInstance)
        {
            return new Animation.Dict(input);
        }
    }
}
