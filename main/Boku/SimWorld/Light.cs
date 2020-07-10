// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.


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
using Boku.Common.ParticleSystem;
using Boku.SimWorld;
using Boku.SimWorld.Chassis;
using Boku.SimWorld.Terra;
using Boku.Programming;
using Boku.Common.Xml;
using Boku.Fx;

namespace Boku
{
    public class Light : GameActor
    {
        #region ChildClasses
        private class LuzAttach : GameActor.Attachment
        {
            #region Members
            private Luz luz = null;
            private Vector3 position = Vector3.Zero;
            private Vector3 color = Vector3.One;
            private float radius = Light.DefaultRange; // radius > 0
            private float strength = 1.0f; // [0..1]
            private float scale = 1.0f;
            private bool on = true;
            #endregion Members

            #region Accessors
            /// <summary>
            /// Whether currently illuminating.
            /// </summary>
            public bool On
            {
                get { return on; }
                set { on = value; }
            }
            /// <summary>
            /// Final color is Strength * Color
            /// </summary>
            public float Strength
            {
                get { return strength; }
                set { strength = value; }
            }
            /// <summary>
            /// Effective radius of illumination.
            /// </summary>
            public float Radius
            {
                get { return radius; }
                set { radius = value; }
            }
            /// <summary>
            /// Radius scale value
            /// </summary>
            public float Scale
            {
                get { return scale; }
            }
            /// <summary>
            /// Don't use this for anything but init, it doesn't have the twitch to 
            /// smoothly transition. Final color is Color * Strength.
            /// </summary>
            public Vector3 Color
            {
                get { return color; }
                set { color = value; }
            }
            /// <summary>
            /// Where in space
            /// </summary>
            public Vector3 Position
            {
                get { return position; }
                set { position = value; }
            }
            /// <summary>
            /// Color modulated by whether it's on and how strongly it's on.
            /// </summary>
            public Vector3 FinalColor
            {
                get { return On ? Color * Strength : Vector3.Zero; }
            }
            #endregion Accessors

            #region Public
            /// <summary>
            /// Constructor, with zero offset
            /// </summary>
            /// <param name="offset"></param>
            public LuzAttach()
                : base(Vector3.Zero)
            {
            }

            /// <summary>
            /// Do any once a frame update. Return false to indicate you're done and
            /// can be disposed.
            /// </summary>
            /// <param name="local"></param>
            /// <returns></returns>
            public override bool Update(Matrix local, float scale)
            {
                Position = local.Translation;
                this.scale = scale;
                UpdateLuz();

                return true;
            }
            /// <summary>
            /// Get ready to go. If start is true, then actually begin animating,
            /// else go into paused but ready state.
            /// </summary>
            /// <param name="start"></param>
            public override void Enable(bool start)
            {
                if (luz == null)
                {
                    luz = Luz.Acquire(Luz.LuzGroup.Obj);
                    Debug.Assert(luz != null, "Do we have more lights than we can afford?");
                    UpdateLuz();
                }
            }
            /// <summary>
            /// Stop doing whatever you do. If hard is false, finish out your animation,
            /// but if hard is true, remove all signs of your existence from the scene.
            /// </summary>
            /// <param name="hard"></param>
            public override void Disable(bool hard)
            {
                if (luz != null)
                {
                    luz = luz.Release();
                }
            }
            /// <summary>
            /// This should be covered by the ActorFactory recycling the owning light.
            /// </summary>
            public override void Release()
            {
            }
            /// <summary>
            /// Whatever you're attached to has just teleported, so adjust accordingly.
            /// </summary>
            /// <param name="pos"></param>
            public override void ResetPosition(Vector3 pos)
            {
            }

            /// <summary>
            /// Set the color for this light. Use this rather than the accessor, to
            /// get a smooth twitch.
            /// </summary>
            /// <param name="dstCol4"></param>
            public void SetLightColor(Vector4 dstCol4)
            {
                Color = new Vector3(dstCol4.X, dstCol4.Y, dstCol4.Z);
                if (luz != null)
                {
                    TwitchManager.Set<Vector3> set = delegate(Vector3 val, Object param) 
                    {
                        if (luz != null)
                        {
                            luz.Color = val;
                        }
                    };
                    TwitchManager.CreateTwitch<Vector3>(
                        luz.Color,
                        Color,
                        set,
                        0.1f,
                        TwitchCurve.Shape.EaseInOut);
                }
            }

            #endregion Public

            #region Internal
            /// <summary>
            /// Pass my state to the underlying Luz's.
            /// </summary>
            private void UpdateLuz()
            {
                if (luz != null)
                {
                    luz.Radius = Radius * Scale;
                    luz.Color = FinalColor;
                    luz.Position = Position;
                }
            }
            #endregion Internal
        }
        #endregion ChildClasses

        #region Members
        private PlasmaEmitter tail = null;

        private LuzAttach luzAtt = new LuzAttach();

        private static int numInScene = 0;
        #endregion Members

        #region Accessors

        /// <summary>
        /// Hijack setting my color to pass it on to the light.
        /// </summary>
        public override Classification.Colors ClassColor
        {
            get { return base.ClassColor; }
            set 
            { 
                base.ClassColor = value;
                if (tail != null)
                {
                    Vector4 color = Classification.ColorVector4(Classification.Color);
                    tail.Color = color;
                    LuzAtt.SetLightColor(color);
                }
            }
        }

        /// <summary>
        /// Whether currently illuminating.
        /// </summary>
        public bool On
        {
            get { return LuzAtt.On; }
            set { LuzAtt.On = value; }
        }
        /// <summary>
        /// Final color is Strength * Color
        /// </summary>
        public float Strength
        {
            get { return LuzAtt.Strength; }
            set { LuzAtt.Strength = value; }
        }
        /// <summary>
        /// Effective radius of illumination.
        /// </summary>
        public override float LightRange
        {
            get { return base.LightRange; }
            set { base.LightRange = value; LuzAtt.Radius = value; }
        }

        /// <summary>
        /// Accessor for my attachment. It's set at constructor and never changed.
        /// </summary>
        private LuzAttach LuzAtt
        {
            get { return luzAtt; }
        }

        /// <summary>
        /// The default radius for newly created lights.
        /// </summary>
        public static float DefaultRange
        {
            get { return 10.0f; }
        }
        #endregion

        #region Public
        //
        //  Light
        //

        public Light(string classificationName, BaseChassis chassis, GetModelInstance getModelInstance, StaticActor staticActor)
            : base(classificationName, classificationName, chassis, getModelInstance, getModelInstance, staticActor)
        {
        }   // end of Light c'tor

        protected override void XmlConstruct()
        {
            tail = new PlasmaEmitter(InGame.inGame.ParticleSystemManager);   // Starts in inactive state.
            AddEmitter(tail, new Vector3(0f, 0f, 0f));


            AddAttachment(luzAtt);

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
            tail.Color = new Vector4(1f, .5f, .5f, 1.0f);
            tail.PositionJitter = 0.0f;
            tail.StartRadius = 0.15f;
            tail.EndRadius = 0.05f;
            tail.StartAlpha = 1.0f;
            tail.EndAlpha = 0.0f;
            tail.MinLifetime = 0.25f;
            tail.MaxLifetime = 0.25f;
            tail.EmissionRate = 100.0f;
            tail.Usage = BaseEmitter.Use.Regular;
        }

        /// <summary>
        /// Do a smooth transitioned color change.
        /// </summary>
        /// <param name="color"></param>
        public override void SetColor(Classification.Colors color)
        {
            base.SetColor(color);

            SetColor(Classification.ColorVector4(color));
        }

        /// <summary>
        /// Keep track of how many of us there are, start failing when too many.
        /// </summary>
        /// <returns></returns>
        public override bool EnterScene()
        {
            if (base.EnterScene())
            {
                const int kMaxInScene = 8;
                Debug.Assert(numInScene <= kMaxInScene);

                /// Take this opportunity to sync down our light range to our
                /// luz attachment.
                luzAtt.Radius = LightRange;

                if (numInScene < kMaxInScene)
                {
                    ++numInScene;
                    return true;
                }
            }

            return false;
        }

        /// <summary>
        /// Keep track of how many of us are left.
        /// </summary>
        public override void ExitScene()
        {
            base.ExitScene();
            Debug.Assert(numInScene > 0);
            --numInScene;
        }

        #endregion Public

        #region Internal

        protected override void MakeGlow(bool on)
        {
            LuzAtt.On = on;
            if (on)
            {
                SetColor(glowEmitter.Color);
            }
        }

        /// <summary>
        /// Make a smooth transition to the new color.
        /// </summary>
        /// <param name="color"></param>
        protected void SetColor(Vector4 color)
        {
            // Apply a twitch to transition the RGBA color to match.
            // colorRGBA = ColorVector4(color);
            TwitchManager.Set<Vector4> set = delegate(Vector4 val, Object param) { tail.Color = val; };
            TwitchManager.CreateTwitch<Vector4>(
                tail.Color,
                color,
                set,
                0.1f,
                TwitchCurve.Shape.EaseInOut);

            LuzAtt.SetLightColor(color);
        }

        public override void Activate()
        {
            base.Activate();

            //MakeGlow(glowing = true);
        }

        public override void Deactivate()
        {
            //MakeGlow(glowing = false);

            base.Deactivate();
        }

       #endregion Internal


    }   // end of class Light

}   // end of namespace Boku
