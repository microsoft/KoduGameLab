using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;

using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Audio;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using Microsoft.Xna.Framework.Storage;

using Boku.Base;
using Boku.Common;
using Boku.Common.ParticleSystem;
using Boku.SimWorld;
using Boku.SimWorld.Chassis;
using Boku.SimWorld.Terra;
using Boku.SimWorld.Collision;
using Boku.Programming;
using Boku.Common.Xml;

namespace Boku
{
    public partial class Drum : GameActor
    {
        public Drum(string classificationName, BaseChassis chassis, GetModelInstance getModelInstance, StaticActor staticActor)
            : base(classificationName, classificationName, chassis, getModelInstance, getModelInstance, staticActor) { }

        protected override bool SpecialBounce(GameActor other, ref MouseTouchHitInfo MouseTouchHitInfo)
        {
            Vector3 otherPos = other.WorldCollisionCenter;
            Vector3 delta = otherPos - WorldCollisionCenter;

            float length = delta.Length();

            float sinAng = delta.Z / length;

            if (sinAng > 0.8f)
            {
                // close enough to overhead.

                /// Only modify the velocity when the other is actually the other.
                /// This keeps us from doing it twice.
                if (MouseTouchHitInfo.Other == other)
                {
                    delta.Z *= 5.0f;
                    Vector3 myWorldFront = Movement.LocalMatrix.Right;
                    delta += myWorldFront * 2.0f;
                    delta.Normalize();
                    float speed = Math.Max(KickStrength, other.Movement.Velocity.Length());
                    delta *= speed * 1.5f;

                    other.Movement.Velocity = delta;
                }

                return true;
            }
            return false;
        }

    }   // end of class Drum

}   // end of namespace Boku