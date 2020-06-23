using System;
using System.Collections.Generic;

using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

using Boku.Base;
using Boku.Fx;
using Boku.Common;

namespace Boku.SimWorld
{
    public class TwoFace : BotFace
    {
        #region Members
        #endregion Members

        #region Accessors
        #endregion Accessors

        #region Public
        public TwoFace(GetModelInstance model)
            : base(model)
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
            SetLid(new Vector2(leftCenter.X, leftCenter.Y + leftAbove.X),
                new Vector2(1.0f, leftCenter.Y + leftAbove.Y),
                false,
                EffectParams.UpperLidLeft);

            SetLid(new Vector2(leftCenter.X, leftCenter.Y + leftBelow.X),
                new Vector2(1.0f, leftCenter.Y + leftBelow.Y),
                true,
                EffectParams.LowerLidLeft);

            SetLid(new Vector2(rightCenter.X, rightCenter.Y + rightAbove.X),
                new Vector2(0.0f, rightCenter.Y + rightAbove.Y),
                true,
                EffectParams.UpperLidRight);

            SetLid(new Vector2(rightCenter.X, rightCenter.Y + rightBelow.X),
                new Vector2(0.0f, rightCenter.Y + rightBelow.Y),
                false,
                EffectParams.LowerLidRight);

        }
        #endregion Internal
    }
}
