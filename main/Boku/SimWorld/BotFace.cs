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
    public abstract class BotFace : Face
    {
        #region SpeedBlend
        /// <summary>
        /// Constant velocity interpolation.
        /// </summary>
        protected struct SpeedBlend
        {
            #region Members
            private float target;
            private float current;
            private float speed;
            #endregion Members

            #region Accessors
            public float Target
            {
                get { return target; }
                set { target = value; }
            }
            public float Current
            {
                get { return current; }
            }
            public float Speed
            {
                get { return speed; }
                set { speed = value; }
            }
            #endregion Accessors

            #region Public
            public SpeedBlend(float init, float speed)
            {
                this.speed = speed;
                this.target = init;
                this.current = init;
            }
            public void Update(float dt, float speedScale)
            {
                float del = target - current;
                float maxStep = dt * speed * speedScale;
                del = MathHelper.Clamp(del, -maxStep, maxStep);
                current += del;
            }
            public void Reset()
            {
                Reset(target);
            }
            public void Reset(float val)
            {
                target = val;
                current = val;
            }
            #endregion Public

        }
        #endregion SpeedBlend

        #region Members
        private SpeedBlend leftAboveCenter = new SpeedBlend(0.0f, 0.8f);
        private SpeedBlend leftAboveEdge = new SpeedBlend(0.0f, 0.75f);
        private SpeedBlend leftBelowCenter = new SpeedBlend(0.0f, 0.8f);
        private SpeedBlend leftBelowEdge = new SpeedBlend(0.0f, 0.75f);

        private SpeedBlend rightAboveCenter = new SpeedBlend(0.0f, 0.8f);
        private SpeedBlend rightAboveEdge = new SpeedBlend(0.0f, 0.75f);
        private SpeedBlend rightBelowCenter = new SpeedBlend(0.0f, 0.8f);
        private SpeedBlend rightBelowEdge = new SpeedBlend(0.0f, 0.75f);

        private const float kBlinkAvgDuration = 0.3f;
        private float blinkNext = 0.0f;
        private float blinkTime = 0.0f;
        private float blinkDuration = kBlinkAvgDuration;
        private Random rnd = new Random();

        private const float kAsymDuration = 0.5f;
        private float asymTime = 0.0f;
        private float asymNext = 0.0f;
        private Vector2 asymGoal = Vector2.One;
        private Vector2 asymLast = Vector2.One;
        private Vector2 asymmetry = Vector2.One;

        private bool twoBlink = false;

        #region Effect Caching
        protected enum EffectParams
        {
            FaceBkg,
            PupilScale,
            PupilOffset,
            UpperLidLeft,
            LowerLidLeft,
            UpperLidRight,
            LowerLidRight,
            EyePupilLeftTexture,
            EyePupilRightTexture,
        };
        protected EffectCache effectCache = null;
        protected EffectParameter Parameter(EffectParams param)
        {
            return effectCache.Parameter((int)param);
        }
        #endregion Effect Caching
        #endregion Members

        #region Accessors
        protected Texture2D PupilLeftTexture()
        {
            return emotionalState == FaceState.Dead ? faceEyesPupilsCross : faceEyesPupils;
        }
        protected Texture2D PupilRightTexture()
        {
            return emotionalState == FaceState.Dead ? faceEyesPupilsCross : faceEyesPupils;
        }
        public Vector2 Asymmetry
        {
            get { return asymmetry; }
        }
        #endregion Accessors

        #region Public
        public BotFace(GetModelInstance getModel)
            : base(getModel)
        {
        }
        public override void SetupForRender(FBXModel model)
        {
            Parameter(EffectParams.FaceBkg).SetValue(BackgroundColor);

            float pupilSize = PupilSize;
            if (emotionalState == FaceState.Dead)
                pupilSize *= 2.0f;
            float pupilScaleLeft = 1.0f / (pupilSizeLeft * pupilSize * Asymmetry.X);
            Vector2 pupilOffLeft = new Vector2(
                (pupilOffsetLeft.X - PupilCenter.X - 0.5f) * pupilScaleLeft + 0.5f,
                (pupilOffsetLeft.Y - PupilCenter.Y - 0.5f) * pupilScaleLeft + 0.5f);
            float pupilScaleRight = 1.0f / (pupilSizeRight * pupilSize * Asymmetry.Y);
            Vector2 pupilOffRight = new Vector2(
                (pupilOffsetRight.X + PupilCenter.X - 0.5f) * pupilScaleRight + 0.5f,
                (pupilOffsetRight.Y - PupilCenter.Y - 0.5f) * pupilScaleRight + 0.5f);
            Parameter(EffectParams.PupilScale).SetValue(new Vector4(
                pupilScaleLeft, pupilScaleLeft, pupilScaleRight, pupilScaleRight));
            Parameter(EffectParams.PupilOffset).SetValue(new Vector4(
                pupilOffLeft.X, pupilOffLeft.Y, pupilOffRight.X, pupilOffRight.Y));

            /// Transform 0.5,0.5 from texture space to uv space, to find
            /// where the center of the pupil is on the face.
            Vector2 leftCenter = new Vector2(
                (0.5f - pupilOffLeft.X) / pupilScaleLeft,
                (0.5f - pupilOffLeft.Y) / pupilScaleLeft);
            Vector2 rightCenter = new Vector2(
                (0.5f - pupilOffRight.X) / pupilScaleRight,
                (0.5f - pupilOffRight.Y) / pupilScaleRight);
            SetLids(leftCenter, rightCenter);

            Parameter(EffectParams.EyePupilLeftTexture).SetValue(PupilLeftTexture());
            Parameter(EffectParams.EyePupilRightTexture).SetValue(PupilRightTexture());
        }

        #endregion Public

        #region Internal
        protected void SetLids(Vector2 leftCenter, Vector2 rightCenter)
        {
            switch (browPositionLeft)
            {
                case BrowPosition.Up:
                    leftAboveCenter.Target = 0.25f;
                    leftAboveEdge.Target = 0.0f;
                    leftBelowCenter.Target = -0.1f;
                    leftBelowEdge.Target = -0.15f;
                    break;
                case BrowPosition.Normal:
                    leftAboveCenter.Target = 0.15f + LidDistance;
                    leftAboveEdge.Target = 0.1f + LidDistance;
                    leftBelowCenter.Target = -0.15f - LidDistance;
                    leftBelowEdge.Target = -0.1f - LidDistance;
                    break;
                case BrowPosition.Down:
                    leftAboveCenter.Target = 0.1f;
                    leftAboveEdge.Target = 0.15f;
                    leftBelowCenter.Target = -0.1f;
                    leftBelowEdge.Target = -0.15f;
                    break;
            };
            switch (browPositionRight)
            {
                case BrowPosition.Up:
                    rightAboveCenter.Target = 0.25f;
                    rightAboveEdge.Target = 0.0f;
                    rightBelowCenter.Target = -0.1f;
                    rightBelowEdge.Target = -0.15f;
                    break;
                case BrowPosition.Normal:
                    rightAboveCenter.Target = 0.15f + LidDistance;
                    rightAboveEdge.Target = 0.1f + LidDistance;
                    rightBelowCenter.Target = -0.15f - LidDistance;
                    rightBelowEdge.Target = -0.1f - LidDistance;
                    break;
                case BrowPosition.Down:
                    rightAboveCenter.Target = 0.1f;
                    rightAboveEdge.Target = 0.15f;
                    rightBelowCenter.Target = -0.1f;
                    rightBelowEdge.Target = -0.15f;
                    break;
            };

            float blink = 1.0f;
            if (InGame.RenderEffect.Normal == InGame.inGame.renderEffects)
            {
                float dt = Time.GameTimeFrameSeconds;
                leftAboveCenter.Update(dt, EyeSpeed);
                rightAboveCenter.Update(dt, EyeSpeed);
                leftAboveEdge.Update(dt, EyeSpeed);
                rightAboveEdge.Update(dt, EyeSpeed);
                leftBelowCenter.Update(dt, EyeSpeed);
                rightBelowCenter.Update(dt, EyeSpeed);
                leftBelowEdge.Update(dt, EyeSpeed);
                rightBelowEdge.Update(dt, EyeSpeed);

                blink = UpdateBlink(dt);
                UpdateAsymmetry(dt);
            }

            /// Flip the signs, because 0,0 is upper left corner, but I can't
            /// help thinking in terms of positive is up.
            PositionLids(
                leftCenter,
                rightCenter,
                new Vector2(-leftAboveCenter.Current * blink * Asymmetry.X, 
                            -leftAboveEdge.Current * blink * Asymmetry.X),
                new Vector2(-rightAboveCenter.Current * blink * Asymmetry.Y, 
                            -rightAboveEdge.Current * blink * Asymmetry.Y),
                new Vector2(-leftBelowCenter.Current * blink * Asymmetry.X, 
                            -leftBelowEdge.Current * blink * Asymmetry.X),
                new Vector2(-rightBelowCenter.Current * blink * Asymmetry.Y, 
                            -rightBelowEdge.Current * blink * Asymmetry.Y));
        }

        protected abstract void PositionLids(
            Vector2 leftCenter,
            Vector2 rightCenter,
            Vector2 leftAbove,
            Vector2 rightAbove,
            Vector2 leftBelow,
            Vector2 rightBelow);

        protected void SetLid(Vector2 center, Vector2 edge, bool flip, EffectParams param)
        {
            Vector2 norm = new Vector2(edge.Y - center.Y, -(edge.X - center.X));
            if (flip)
                norm = -norm;

            float dist = Vector2.Dot(norm, center);

            Vector4 plane = new Vector4(norm.X, norm.Y, dist, center.X);

            Parameter(param).SetValue(plane);
        }

        protected void UpdateAsymmetry(float dt)
        {
            if (emotionalState != FaceState.Dead)
            {
                if (asymTime > 0)
                {
                    asymTime -= dt;
                    if (asymTime < 0)
                    {
                        asymmetry = asymGoal;
                        asymNext = BlinkRange.X + (float)(rnd.NextDouble() * (BlinkRange.Y - BlinkRange.X));
                        return;
                    }
                    float parm = asymTime / kAsymDuration;
                    parm = 1.0f - MyMath.SmoothStep(0.0f, 1.0f, parm);
                    asymmetry.X = MyMath.Lerp(asymLast.X, asymGoal.X, parm);
                    asymmetry.Y = MyMath.Lerp(asymLast.Y, asymGoal.Y, parm);
                    return;
                }

                asymNext -= dt;
                if (asymNext <= 0)
                {
                    asymLast = asymGoal;
                    asymTime = kAsymDuration;
                    float rndAsym = MaxAsymmetry * (float)(0.5 + rnd.NextDouble() * 0.5f);
                    bool bigX = asymGoal.X > asymGoal.Y;
                    if (rnd.NextDouble() > 0.65)
                    {
                        bigX = !bigX;
                    }
                    if (!bigX)
                    {
                        asymGoal.Y = 1.0f + rndAsym;
                        asymGoal.X = 1.0f / asymGoal.Y;
                    }
                    else
                    {
                        asymGoal.X = 1.0f + rndAsym;
                        asymGoal.Y = 1.0f / asymGoal.X;
                    }
                }
            }
        }

        protected float UpdateBlink(float dt)
        {
            if (emotionalState != FaceState.Dead)
            {
                if (blinkTime > 0)
                {
                    blinkTime -= dt;
                    if (blinkTime <= 0)
                    {
                        if (twoBlink)
                        {
                            twoBlink = false;
                            blinkTime = blinkDuration;
                        }
                        else
                        {
                            blinkNext = BlinkRange.X + (float)(rnd.NextDouble() * (BlinkRange.Y - BlinkRange.X));
                        }
                        return 1.0f;
                    }
                    float parm = 1.0f - blinkTime / blinkDuration;
                    if (parm < 0.25f)
                    {
                        /// On the way to closing fast
                        parm = 1.0f - MyMath.SmoothStep(0.0f, 0.25f, parm);
                    }
                    else
                    {
                        /// on the way to open slow
                        parm = MyMath.SmoothStep(0.25f, 1.0f, parm);
                    }
                    return parm;
                }

                blinkNext -= dt;
                if (blinkNext <= 0)
                {
                    blinkDuration = kBlinkAvgDuration / EyeSpeed * (float)(rnd.NextDouble() * 0.1f + 0.9f);
                    blinkTime = blinkDuration;
                    twoBlink = emotionalState == FaceState.Sad;
                }
            }
            return 1.0f;
        }

        public override void InitDeviceResources(GraphicsDevice device)
        {
            if (effectCache == null)
            {
                effectCache = new EffectCache<EffectParams>();
                effectCache.Load(BaseModel.Effect, "", 0);
            }

            base.InitDeviceResources(device);
        }

        public override void UnloadContent()
        {
            if (effectCache != null)
            {
                effectCache.UnLoad();
                effectCache = null;
            }

            base.UnloadContent();
        }

        #endregion Internal
    }
}
