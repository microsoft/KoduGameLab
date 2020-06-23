/*
 * EffectInstancedAnimator.cs
 * Copyright (c) 2007 David Astle
 * 
 * Permission is hereby granted, free of charge, to any person obtaining a
 * copy of this software and associated documentation files (the
 * "Software"), to deal in the Software without restriction, including
 * without limitation the rights to use, copy, modify, merge, publish,
 * distribute, sublicense, and/or sell copies of the Software, and to
 * permit persons to whom the Software is furnished to do so, subject to
 * the following conditions:
 *
 * The above copyright notice and this permission notice shall be included
 * in all copies or substantial portions of the Software.
 *
 * THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS
 * OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF
 * MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT.
 * IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY
 * CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION OF CONTRACT,
 * TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION WITH THE
 * SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.
 */
using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace Xclna.Xna.Animation
{
    /// <summary>
    /// A subclass of ModelAnimator that uses effects cloned from the model instead of
    /// directly from the model.
    /// </summary>
    public class EffectInstancedAnimator : ModelAnimator
    {
        /// <summary>
        /// Creates a new EffectInstancedAnimator.
        /// </summary>
        /// <param name="game">The game.</param>
        /// <param name="model">The model.</param>
        public EffectInstancedAnimator(Game game, Model model)
            : base(game, model)
        {

        }

        /// <summary>
        /// Creates a list of effects that are cloned from the model's current effects.
        /// </summary>
        /// <returns>A list of effects that are cloned from the model's current effects.</returns>
        protected override IList<Effect> CreateEffectList()
        {
            List<Effect> effects = new List<Effect>();
            foreach (ModelMesh mesh in base.Model.Meshes)
            {
                foreach (ModelMeshPart part in mesh.MeshParts)
                {
                    effects.Add(part.Effect.Clone(part.Effect.GraphicsDevice));
                }
            }
            return effects;
        }
    }
}
