
/// Relocated from Common namespace

using System;
using System.Collections.Generic;

using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

using Boku.Base;
using Boku.Common;

namespace Boku.Fx
{
    public partial class FirstPersonEffectMgr
    {
        #region Members
        private static List<FirstPersonEffect> effects = new List<FirstPersonEffect>();
        #endregion Members

        #region Accessors
        private static List<FirstPersonEffect> Effects
        {
            get { return effects; }
        }

        protected static Random rnd = new Random();

        #endregion Accessors

        #region Public
        public static void Update()
        {
            for (int i = Effects.Count - 1; i >= 0; --i)
            {
                FirstPersonEffect fpe = Effects[i];
                if (!fpe.Update())
                {
                    Effects.RemoveAt(i);
                }
            }
        }

        public static void Render(Camera camera)
        {
            for (int i = 0; i < Effects.Count; ++i)
            {
                FirstPersonEffect fpe = Effects[i];
                fpe.Render(camera);
            }
        }

        public static void LoadEffects()
        {
            FPEGlow glow = new FPEGlow();
            Effects.Add(glow);

            FPEDistort distort = new FPEDistort();
            Effects.Add(distort);

            Effects.Sort(CompareFPEs);
        }

        public static void LoadContent(bool immediate)
        {
            if (Effects.Count == 0)
            {
                LoadEffects();
            }

            for (int i = 0; i < Effects.Count; ++i)
            {
                FirstPersonEffect fpe = Effects[i];
                fpe.LoadContent(immediate);
            }
        }

        public static void InitDeviceResources(GraphicsDevice device)
        {
            for (int i = 0; i < Effects.Count; ++i)
            {
                FirstPersonEffect fpe = Effects[i];
                fpe.InitDeviceResources(device);
            }
        }

        public static void UnloadContent()
        {
            for (int i = 0; i < Effects.Count; ++i)
            {
                FirstPersonEffect fpe = Effects[i];
                fpe.UnloadContent();
            }
        }

        public static void DeviceReset(GraphicsDevice device)
        {
            for (int i = 0; i < Effects.Count; ++i)
            {
                FirstPersonEffect fpe = Effects[i];
                BokuGame.DeviceReset(fpe, device);
            }
        }

        #endregion Public

        #region Internal
        private static int CompareFPEs(FirstPersonEffect lhs, FirstPersonEffect rhs)
        {
            if (lhs.Priority < rhs.Priority)
                return -1;
            if (lhs.Priority > rhs.Priority)
                return 1;
            return 0;
        }
        #endregion Internal
    }
}
