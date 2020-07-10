// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System;
using System.Collections.Generic;

using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

using Boku.Base;
using Boku.Fx;
using Boku.Common;

namespace Boku.SimWorld
{
    public class WideFace : BotFace
    {
        #region Members
        #endregion Members

        #region Accessors
        #endregion Accessors

        #region Public
        public WideFace(GetModelInstance getModel)
            : base(getModel)
        {
        }
        #endregion Public

        #region Internal
        protected override void PositionLids(
            Vector2 leftCenter,
            Vector2 rightCenter,
            Vector2 leftAbove,
            Vector2 rightAbove,
            Vector2 leftBelow,
            Vector2 rightBelow)
        {
            float centerX = (leftCenter.X + rightCenter.X) * 0.5f;

            SetLid(new Vector2(centerX, leftCenter.Y + leftAbove.X),
                new Vector2(1.0f, leftCenter.Y + leftAbove.Y),
                false,
                EffectParams.UpperLidLeft);

            SetLid(new Vector2(centerX, leftCenter.Y + leftBelow.X),
                new Vector2(1.0f, leftCenter.Y + leftBelow.Y),
                true,
                EffectParams.LowerLidLeft);

            SetLid(new Vector2(centerX, rightCenter.Y + rightAbove.X),
                new Vector2(0.0f, rightCenter.Y + rightAbove.Y),
                true,
                EffectParams.UpperLidRight);

            SetLid(new Vector2(centerX, rightCenter.Y + rightBelow.X),
                new Vector2(0.0f, rightCenter.Y + rightBelow.Y),
                false,
                EffectParams.LowerLidRight);

        }
        #endregion Internal
    }
}
