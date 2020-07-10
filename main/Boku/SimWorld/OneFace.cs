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
    public class OneFace : Face
    {
        #region Members
        protected Texture2D faceEyesOpen = null;
        protected Texture2D faceEyesSquint = null;
        protected Texture2D faceBrowsUp = null;
        protected Texture2D faceBrowsNormal = null;
        protected Texture2D faceBrowsDown = null;

        #region Effect Caching
        protected enum EffectParams
        {
            FaceBkg,
            PupilScale,
            PupilOffset,
            BrowScale,
            BrowOffset,
            EyeShapeLeftTexture,
            EyePupilLeftTexture,
            EyeBrowLeftTexture,
            EyeShapeRightTexture,
            EyePupilRightTexture,
            EyeBrowRightTexture,
        };
        protected EffectCache effectCache = null;
        protected EffectParameter Parameter(EffectParams param)
        {
            return effectCache.Parameter((int)param);
        }
        #endregion Effect Caching
        #endregion Members

        #region Accessors
        protected Texture2D ShapeLeftTexture()
        {
            return eyeShapeLeft == EyeShape.Open ? faceEyesOpen : faceEyesSquint;
        }
        protected Texture2D ShapeRightTexture()
        {
            return eyeShapeRight == EyeShape.Open ? faceEyesOpen : faceEyesSquint;
        }
        protected Texture2D PupilLeftTexture()
        {
            return emotionalState == FaceState.Dead ? faceEyesPupilsCross : faceEyesPupils;
        }
        protected Texture2D PupilRightTexture()
        {
            return emotionalState == FaceState.Dead ? faceEyesPupilsCross : faceEyesPupils;
        }
        protected Texture2D BrowLeftTexture()
        {
            return browPositionLeft == BrowPosition.Normal ? faceBrowsNormal : browPositionLeft == BrowPosition.Up ? faceBrowsUp : faceBrowsDown;
        }
        protected Texture2D BrowRightTexture()
        {
            return browPositionRight == BrowPosition.Normal ? faceBrowsNormal : browPositionRight == BrowPosition.Up ? faceBrowsUp : faceBrowsDown;
        }
        #endregion Accessors

        #region Public
        public OneFace(GetModelInstance model)
            : base(model)
        {
        }

        public override void SetupForRender(FBXModel model)
        {
            Parameter(EffectParams.FaceBkg).SetValue(BackgroundColor);

            float pupilScaleLeft = 1.0f / (pupilSizeLeft * PupilSize);
            Vector2 pupilOffLeft = new Vector2(
                (pupilOffsetLeft.X - PupilCenter.X - 0.5f) * pupilScaleLeft + 0.5f,
                (pupilOffsetLeft.Y - PupilCenter.Y - 0.5f) * pupilScaleLeft + 0.5f);
            float pupilScaleRight = 1.0f / (pupilSizeRight * PupilSize);
            Vector2 pupilOffRight = new Vector2(
                (pupilOffsetRight.X + PupilCenter.X - 0.5f) * pupilScaleRight + 0.5f,
                (pupilOffsetRight.Y - PupilCenter.Y - 0.5f) * pupilScaleRight + 0.5f);
            Parameter(EffectParams.PupilScale).SetValue(new Vector4(
                pupilScaleLeft, pupilScaleLeft, pupilScaleRight, pupilScaleRight));
            Parameter(EffectParams.PupilOffset).SetValue(new Vector4(
                pupilOffLeft.X, pupilOffLeft.Y, pupilOffRight.X, pupilOffRight.Y));

            float browScaleLeft = 1.0f / BrowSize;
            Vector2 browOffLeft = new Vector2(
                (browOffsetLeft.X - BrowCenter.X - 0.5f) * browScaleLeft + 0.5f,
                (browOffsetLeft.Y - BrowCenter.Y - 0.5f) * browScaleLeft + 0.5f);
            float browScaleRight = 1.0f / BrowSize;
            Vector2 browOffRight = new Vector2(
                (browOffsetRight.X - BrowCenter.X - 0.5f) * browScaleRight + 0.5f,
                (browOffsetRight.Y - BrowCenter.Y - 0.5f) * browScaleRight + 0.5f);
            Parameter(EffectParams.BrowScale).SetValue(new Vector4(
                browScaleLeft, browScaleLeft, browScaleRight, browScaleRight));
            Parameter(EffectParams.BrowOffset).SetValue(new Vector4(
                browOffLeft.X, browOffLeft.Y, browOffRight.X, browOffRight.Y));

            Parameter(EffectParams.EyeShapeLeftTexture).SetValue(ShapeLeftTexture());
            Parameter(EffectParams.EyePupilLeftTexture).SetValue(PupilLeftTexture());
            Parameter(EffectParams.EyeBrowLeftTexture).SetValue(BrowLeftTexture());
            Parameter(EffectParams.EyeShapeRightTexture).SetValue(ShapeRightTexture());
            Parameter(EffectParams.EyePupilRightTexture).SetValue(PupilRightTexture());
            Parameter(EffectParams.EyeBrowRightTexture).SetValue(BrowRightTexture());
        }
        #endregion Public

        #region Internal
        public override void LoadContent(bool immediate)
        {
            // Load the face textures.
            if (faceEyesOpen == null)
            {
                faceEyesOpen = BokuGame.Load<Texture2D>(BokuGame.Settings.MediaPath + @"Textures\BokuFaceEyeWide");
            }
            if (faceEyesSquint == null)
            {
                faceEyesSquint = BokuGame.Load<Texture2D>(BokuGame.Settings.MediaPath + @"Textures\BokuFaceEyeSquint");
            }
            if (faceBrowsUp == null)
            {
                faceBrowsUp = BokuGame.Load<Texture2D>(BokuGame.Settings.MediaPath + @"Textures\BokuFaceBrowUp");
            }
            if (faceBrowsNormal == null)
            {
                faceBrowsNormal = BokuGame.Load<Texture2D>(BokuGame.Settings.MediaPath + @"Textures\BokuFaceBrowNormal");
            }
            if (faceBrowsDown == null)
            {
                faceBrowsDown = BokuGame.Load<Texture2D>(BokuGame.Settings.MediaPath + @"Textures\BokuFaceBrowDown");
            }
            base.LoadContent(immediate);
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
            BokuGame.Release(ref faceEyesOpen);
            BokuGame.Release(ref faceEyesSquint);
            BokuGame.Release(ref faceEyesPupils);
            BokuGame.Release(ref faceEyesPupilsCross);
            BokuGame.Release(ref faceBrowsUp);
            BokuGame.Release(ref faceBrowsNormal);
            BokuGame.Release(ref faceBrowsDown);


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
