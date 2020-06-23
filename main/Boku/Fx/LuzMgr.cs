
/// Relocated from Scenes namespace

using System;
using System.Collections.Generic;
using System.Text;

using Microsoft.Xna.Framework;

using Boku.Common;
using Boku.Base;

namespace Boku.Fx
{
    public partial class LuzMgr
    {
        public class MuzzleLuz : GameActor.Attachment
        {
            #region Members
            private float age = 0.0f;
            private float life = 2.0f;

            private Vector3 tint = Vector3.One;

            private float[] radii = new float[3]
            {
                0.125f,
                1.0f,
                0.0125f
            };
            private Vector3[] colors = new Vector3[3]
            { 
                new Vector3(0.4f, 0.4f, 0.4f),
                new Vector3(1.0f, 1.0f, 1.0f),
                new Vector3(0.5f, 0.5f, 0.5f)
            };
            private const float mid = 0.15f;

            private Luz luz = null;

            private static List<MuzzleLuz> available = new List<MuzzleLuz>();
            private static List<MuzzleLuz> ready = new List<MuzzleLuz>();

            private static Random rnd = new Random();
            #endregion Members

            #region Accessors
            public Vector3 Tint
            {
                get { return tint; }
                set { tint = value; }
            }
            public float Life
            {
                get { return life; }
                set { life = value; }
            }
            protected Luz Luz
            {
                get { return luz; }
                private set 
                { 
                    luz = value;
                    Reset();
                }
            }
            #endregion Accessors

            #region Public
            public static MuzzleLuz Acquire(Matrix offset)
            {
                return Acquire(offset, 2.0f, 8.0f);
            }
            public static MuzzleLuz Acquire(Matrix offset, float life, float scale)
            {
                MuzzleLuz muzzle = null;
                Luz luz = Luz.Acquire(Luz.LuzGroup.Fire);
                if (luz != null)
                {
                    muzzle = Get(offset);

                    muzzle.age = 0;
                    muzzle.Luz = luz;
                    muzzle.Life = life;
                    muzzle.InitRadii();
                    muzzle.ScaleRadii(scale);
                }

                return muzzle;
            }

            public override void Release()
            {
                ready.Remove(this);
                available.Add(this);
                if (luz != null)
                {
                    luz = luz.Release();
                }
            }

            public override bool Update(Matrix local, float scale)
            {
                float dt = Time.GameTimeFrameSeconds;
                age += dt;
                if (age >= life)
                {
                    luz = luz.Release();
                    return false;
                }
                int lo = 0;
                int hi = 1;
                float t = age;
                float midLife = mid * life;
                if (t < midLife)
                {
                    t /= midLife;
                }
                else
                {
                    t = (life - t) / (life - midLife);
                    lo = 2;
                }
                /// Smooth it.
                t = t * (3.0f * t - 2.0f * t * t);

                double deviate = 0.03125;
                float flutter = (float)((rnd.NextDouble() * 2.0 - 1.0) * deviate + 1.0);

                luz.Color = colors[lo] + t * (colors[hi] - colors[lo]);
                luz.Color = luz.Color * Tint * flutter;
                luz.Position = Matrix.Multiply(Offset, local).Translation;
                luz.Radius = radii[lo] + t * (radii[hi] - radii[lo]);
                luz.Radius *= scale;

                return true;
            }
            public override void Enable(bool start)
            {
            }
            public override void Disable(bool hard)
            {
                age = life;
            }
            public override void ResetPosition(Vector3 pos)
            {
            }

            #endregion Public

            #region Internal
            private MuzzleLuz(Matrix offset)
                : base(offset)
            {
            }

            private MuzzleLuz(Vector3 offset)
                : base(offset)
            {
            }

            protected void Reset()
            {
                if (luz != null)
                {
                    luz.Color = colors[0] * Tint;
                    luz.Radius = radii[0];
                }
            }

            protected void ScaleRadii(float scale)
            {
                for (int i = 0; i < radii.Length; ++i)
                {
                    radii[i] *= scale;
                }
            }

            private void InitRadii()
            {
                radii[0] = 0.125f;
                radii[1] = 1.0f;
                radii[2] = 0.0125f;
            }


            private static MuzzleLuz Get(Matrix offset)
            {
                MuzzleLuz muzzle = null;
                if (available.Count == 0)
                {
                    muzzle = new MuzzleLuz(offset);
                    ready.Add(muzzle);
                }
                else
                {
                    muzzle = available[available.Count - 1];
                    available.RemoveAt(available.Count - 1);
                    ready.Add(muzzle);

                    muzzle.Offset = offset;
                }
                return muzzle;
            }

            #endregion Internal
        }
    }
}
