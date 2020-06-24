
/// Relocated from Scenes namespace

using System;
using System.Collections.Generic;
using System.Diagnostics;

using Boku.Common;
using Boku.SimWorld.Terra;

using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

using KoiX;

namespace Boku.Fx
{
    public class Luz
    {
        #region Members
        private Vector3 position;
        private Vector3 color;
        private float radius;

        public enum LuzGroup
        {
            Obj,
            Glow,
            Fire,

            UnAssigned
        };
        private const int kNumGroups = (int)LuzGroup.UnAssigned + 1;
        private LuzGroup group;

        private const int kMaxLights = 10; /// Must match NUM_LIGHTS in Globals.fx
        private static List<Luz> active = new List<Luz>(kMaxLights);
        private static List<Luz> ready = new List<Luz>(kMaxLights);

        private static Vector4[] positions = new Vector4[kMaxLights];
        private static Vector4[] colors = new Vector4[kMaxLights];

        private static int[] counts = new int[kNumGroups];

        #region Parameter Caching
        enum EffectParams
        {
            LightPosition,
            LightColor,
        };
        static EffectCache effectCache = new EffectCache<EffectParams>();
        private static EffectParameter Parameter(EffectParams param)
        {
            return effectCache.Parameter((int)param);
        }
        #endregion Parameter Caching

        #endregion Members

        #region Accessors
        public static int Count
        {
            get { return active.Count; }
        }
        public Vector3 Position
        {
            get { return position; }
            set { position = value; }
        }
        public float Radius
        {
            get { return radius; }
            set { radius = value; }
        }
        public Vector3 Color
        {
            get { return color; }
            set { color = value; }
        }
        public LuzGroup Group
        {
            get { return group; }
            private set { group = value; }
        }
        #endregion Accessors

        #region Public
        public static void Init(Effect effect)
        {
            Luz.effectCache.Load(effect);
            int numReadyNeeded = kMaxLights - (active.Count + ready.Count);
            for (int i = 0; i < numReadyNeeded; ++i)
            {
                ready.Add(new Luz(LuzGroup.UnAssigned));
            }
        }

        public static void DeInit()
        {
            Luz.effectCache.UnLoad();
        }

        public static void DebugDrawLuz(Camera camera)
        {
            //if (Terrain.Current != null)
            //{
            //    for (int i = 0; i < active.Count; ++i)
            //    {
            //        Utils.DrawSphere(camera, active[i].Position, active[i].Radius);
            //        Utils.DrawAxis(camera, active[i].Position);
            //    }
            //}
        }

        /// <summary>
        /// Send off light parameters to the shader registers. If disabled,
        /// set all lights to be black.
        /// </summary>
        /// <param name="disabled"></param>
        public static void SetToEffect(Effect effect, bool disabled)
        {
            int count = disabled ? 0 : active.Count;
            for (int i = 0; i < count; ++i)
            {
                positions[i] = new Vector4(active[i].position, 1.0f / active[i].radius);
                colors[i] = new Vector4(active[i].color, 0.0f);
            }
            for (int i = count; i < kMaxLights; ++i)
            {
                positions[i] = Vector4.Zero;
                colors[i] = Vector4.Zero;
            }

            if (effect.Parameters["LightPosition"] != null)
            {
                effect.Parameters["LightPosition"].SetValue(positions);
                effect.Parameters["LightColor"].SetValue(colors);
            }
        }

        public static Luz Acquire(LuzGroup group)
        {
            Luz luz = null;
            if (ready.Count > 0)
            {
                int idx = ready.Count-1;
                luz = ready[idx];
                ready.RemoveAt(idx);

                AddLuz(luz, group);
            }
            return luz;
        }

        private static void AddLuz(Luz luz, LuzGroup group)
        {
            Debug.Assert(luz.Group == LuzGroup.UnAssigned);

            counts[(int)group] += 1;
            luz.Group = group;
            active.Add(luz);
            Debug.Assert(active.Count <= kMaxLights);
        }

        public Luz Release()
        {
            counts[(int)Group] -= 1;
            active.Remove(this);

            Group = LuzGroup.UnAssigned;
            ready.Add(this);

            return null;
        }
        #endregion Public

        #region Internal
        private Luz(LuzGroup group)
        {
            this.group = group;
        }

        #endregion Internal
    }
}
