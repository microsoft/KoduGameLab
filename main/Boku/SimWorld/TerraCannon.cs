
using System;
using System.Collections;
using System.Diagnostics;

using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Audio;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using Microsoft.Xna.Framework.Storage;

using Boku.Base;
using Boku.Common;
using Boku.SimWorld;
using Boku.SimWorld.Chassis;
using Boku.SimWorld.Terra;
using Boku.Programming;
using Boku.Common.Xml;

namespace Boku
{
    public class TerraCannon : GameActor
    {
        public TerraCannon(string classificationName, BaseChassis chassis, GetModelInstance getModelInstance, StaticActor staticActor)
            : base(classificationName, classificationName, chassis, getModelInstance, getModelInstance, staticActor)
        {
        }

        public override void CreatedItemVelocity(ref Vector3 velocity)
        {
            Random rnd = BokuGame.bokuGame.rnd;
            velocity = Vector3.UnitX;       // Forward.
            velocity.Z += 0.4f;             // Add some loft.
            velocity *= 4.0f;               // And power.
            // And a little randomness.
            velocity += 0.1f * new Vector3((float)rnd.NextDouble() - (float)rnd.NextDouble(), (float)rnd.NextDouble() - (float)rnd.NextDouble(), (float)rnd.NextDouble() - (float)rnd.NextDouble());

            // Transform into bot's space.
            velocity = Vector3.TransformNormal(velocity, Movement.LocalMatrix);
        }
    }
}
