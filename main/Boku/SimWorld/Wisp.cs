
using System;
using System.Collections;
using System.Diagnostics;

using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Audio;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using Microsoft.Xna.Framework.Storage;

using KoiX;

using Boku.Base;
using Boku.Common;
using Boku.Common.ParticleSystem;
using Boku.SimWorld;
using Boku.SimWorld.Chassis;
using Boku.SimWorld.Terra;
using Boku.Programming;
using Boku.Common.Xml;

namespace Boku
{
    public class Wisp : GameActor
    {
        private PlasmaEmitter tail = null;

        public override Classification.Colors ClassColor
        {
            get { return base.ClassColor; }
            set 
            { 
                base.ClassColor = value;
                if (tail != null)
                {
                    tail.Color = Classification.ColorVector4(Classification.Color);
                }
            }
        }

        public Wisp(string classificationName, BaseChassis chassis, GetModelInstance getModelInstance, StaticActor staticActor)
            : base(classificationName, classificationName, chassis, getModelInstance, getModelInstance, staticActor)
        {
        }   // end of Wisp c'tor

        protected override void XmlConstruct()
        {
            tail = new PlasmaEmitter(InGame.inGame.ParticleSystemManager);   // Starts in inactive state.
            AddEmitter(tail, new Vector3(0f, 0f, 0f));

            base.XmlConstruct();
        }

        /// <summary>
        /// Reinitialize members that can change at runtime back to their initial
        /// values. DO NOT ALLOCATE ANYTHING IN THIS FUNCTION. This function exists
        /// so that ActorFactory can reinitialize recycled actors. ActorFactory
        /// exists so that we can avoid allocating new memory when the need for a
        /// new actor arises. Allocating memory in this function would defeat the
        /// entire reason for ActorFactory's existence.
        /// </summary>
        public override void InitDefaults(bool revivingDeadActor)
        {
            // WARNING: NO CODE MAY APPEAR BEFORE CALL TO BASE. PUT YOUR ADDITIONS
            //          AT THE END OF THIS FUNCTION.

            // base must be called BEFORE any changes are made at this call-level.
            // This is because the chassis may be reset to default values. We need
            // to apply our bot-specific chassis customizations after this happens.
            base.InitDefaults(revivingDeadActor);

            /// Set up the particle system, but don't set emitting or active.
            /// Those get set up when the bot is activated.
            tail.Color = new Vector4(1f, .5f, .5f, 1.0f); // ok to allocate here, as it is a struct.
            tail.PositionJitter = 0.03f;
            tail.StartRadius = 0.5f;
            tail.EndRadius = 0.05f;
            tail.StartAlpha = 1.0f;
            tail.EndAlpha = 0.0f;
            tail.MinLifetime = 0.5f;
            tail.MaxLifetime = 1.5f;
            tail.EmissionRate = 100.0f;
            tail.Usage = BaseEmitter.Use.Regular;
        }

        public override void SetColor(Classification.Colors color)
        {
            base.SetColor(color);

            // Apply a twitch to transition the RGBA color to match.
            // colorRGBA = ColorVector4(color);
            TwitchManager.Set<Vector4> set = delegate(Vector4 val, Object param) { tail.Color = val; };
            TwitchManager.CreateTwitch(tail.Color, Classification.ColorVector4(color), set, 0.1f, TwitchCurve.Shape.EaseInOut);
        }

    }   // end of class Wisp

}   // end of namespace Boku
