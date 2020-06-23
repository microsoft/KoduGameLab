
///
/// This was originally in Common, but migrated here when the new animation
/// system was written.
/// 


using System;
using System.Collections.Generic;
using System.Diagnostics;

using Boku.SimWorld;
using Boku.Common;

using Microsoft.Xna.Framework;

using Boku.Animatics;

namespace Boku.Animatics
{
    public class AnimatorList
    {
        #region Members
        List<AnimationInstance> animList = null;
        List<Matrix[]> restPalettes = null;
        List<int> lastUpdates = null;

        private static AnimatorList emptyList = new AnimatorList();
        #endregion Members

        #region Accessors
        public AnimationInstance Sample
        {
            get 
            {
                AnimationInstance anim = null;
                if (NotEmpty)
                {
                    int lod = Math.Max(Count - 1, 0);
                    anim = this[lod];
                }
                return anim;
            }
        }
        public AnimationInstance this[int lod]
        {
            get
            {
                AnimationInstance ret = null;
                if (lod < Count)
                {
                    ret = animList[lod];
                }
                return ret;
            }
        }
        public bool NotEmpty
        {
            get { return Count > 0; }
        }
        public bool Empty
        {
            get { return Count == 0; }
        }
        public int Count
        {
            get { return animList != null ? animList.Count : 0; }
        }
        public List<Matrix[]> RestPalettes
        {
            get { return restPalettes; }
        }
        public Matrix[] RestPalette(int which)
        {
            return restPalettes[which];
        }
        public static AnimatorList EmptyList
        {
            get { return emptyList; }
        }
        #endregion Accessors

        #region Public
        public AnimatorList(FBXModel model)
        {
            /// Note these are only setting up capacity, they're still empty.
            animList = new List<AnimationInstance>(model.NumLODs);
            restPalettes = new List<Matrix[]>(model.NumLODs);
            lastUpdates = new List<int>(model.NumLODs);

            for (int lod = 0; lod < model.NumLODs; ++lod)
            {
                AnimationInstance animator = model.MakeAnimator(lod);
                if (animator != null)
                {

                    animList.Add(animator);
                    lastUpdates.Add(0);

                    Matrix[] animPalette = animList[lod].Palette;
                    int boneCount = animPalette.Length;

                    Matrix[] palette = new Matrix[boneCount];
                    for (int bone = 0; bone < boneCount; ++bone)
                    {
                        palette[bone] = animPalette[bone];
                    }
                    restPalettes.Add(palette);
                }
            }
        }

        public void ApplyController(BaseController activeController)
        {
            for (int lod = 0; lod < animList.Count; ++lod)
            {
                AnimationInstance animator = animList[lod];
                animator.SetAnimation(activeController);

                SetRestPalette(lod);
            }
        }
        #endregion Public

        #region Internal
        private AnimatorList()
        {
        }

        private void SetRestPalette(int lod)
        {
            Matrix[] animPalette = animList[lod].Palette;
            int boneCount = animPalette.Length;
            for (int bone = 0; bone < boneCount; ++bone)
            {
                restPalettes[lod][bone] = animPalette[bone];
            }
        }

        #endregion Internal
    }
}
