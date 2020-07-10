// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.


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
        public class GlowLuz : GameActor.Attachment
        {
            #region Members
            private Vector3 tint = Vector3.Zero;

            private float strength = 1.0f;
            private float radius = 1.0f;
            private Vector3 color = Vector3.One;

            private Luz luz = null;

            private static List<GlowLuz> available = new List<GlowLuz>();
            private static List<GlowLuz> ready = new List<GlowLuz>();

            private static Random rnd = new Random();
            #endregion Members

            #region Accessors
            public Vector3 Tint
            {
                get { return tint; }
                set { tint = value; }
            }
            public float Radius
            {
                get { return radius; }
                set { radius = value; }
            }
            public float Strength
            {
                get { return strength; }
                set { strength = value; }
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
            public static GlowLuz Acquire(string boneName, Matrix offset, float strength)
            {
                return Acquire(boneName, offset, 16.0f, strength);
            }
            public static GlowLuz Acquire(string boneName, Matrix offset, float radius, float strength)
            {
                GlowLuz glowLuz = null;
                Luz luz = Luz.Acquire(Luz.LuzGroup.Glow);
                if (luz != null)
                {
                    glowLuz = Get(boneName, offset);

                    glowLuz.Tint = Vector3.Zero;
                    glowLuz.Luz = luz;
                    glowLuz.Radius = radius;
                    glowLuz.Strength = strength;
                }

                return glowLuz;
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
                double deviate = 0.01125;
                float flutter = (float)((rnd.NextDouble() * 2.0 - 1.0) * deviate + 1.0);
                flutter *= Strength;

                luz.Color = Tint * flutter;
                luz.Position = Concat(local, scale).Translation;
                luz.Radius = Radius * flutter;

                return true;
            }
            public override void Enable(bool start)
            {
            }
            public override void Disable(bool hard)
            {
            }
            public override void ResetPosition(Vector3 pos)
            {
            }

            #endregion Public

            #region Internal
            private GlowLuz(string boneName, Matrix offset)
                : base(boneName, offset)
            {
            }

            protected void Reset()
            {
                if (luz != null)
                {
                    luz.Color = color * Tint;
                    luz.Radius = radius;
                }
            }
            private static GlowLuz Get(string boneName, Matrix offset)
            {
                GlowLuz glowLuz = null;
                if (available.Count == 0)
                {
                    glowLuz = new GlowLuz(boneName, offset);
                    ready.Add(glowLuz);
                }
                else
                {
                    glowLuz = available[available.Count - 1];
                    available.RemoveAt(available.Count - 1);
                    ready.Add(glowLuz);

                    glowLuz.BoneName = boneName;
                    glowLuz.Offset = offset;
                }
                return glowLuz;
            }

            #endregion Internal
        }
    }
}
