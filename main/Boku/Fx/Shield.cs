
using System;
using System.Collections.Generic;
using System.Diagnostics;

using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

using Boku.Base;
using Boku.Common;
using Boku.Common.ParticleSystem;

namespace Boku.Fx
{
    public class Shield
    {
        #region ChildClasses
        private class Impact
        {
            #region Members
            #endregion Members

            #region Accessors
            public float Age { get; private set; }
            public float Life { get; set; }
            public float Strength { get; set; }
            public float Radius { get; set; }
            public Vector3 Tint { get; set; }

            public Vector3 WorldCenter { get; private set; }

            private GameActor Actor { get; set; }

            private double Birth { get; set; }
            private Vector3 TexU { get; set; }
            private Vector3 TexV { get; set; }
            private Vector3 TexW { get; set; }
            #endregion Accessors

            #region Public
            public Impact()
            {
            }

            public void Set(GameActor actor, Vector3 worldPosition)
            {
                this.Actor = actor;
                this.TexW = Vector3.Normalize(worldPosition - WorldCenter);
                this.Birth = Time.GameTimeTotalSeconds;

                /// Pick out an "V" vector. It doesn't matter what it is,
                /// as long as it is ortho to Position to form a basis set.
                /// We'll use whichever of Cross(Pos, X) and Cross(Z, Pos),
                /// because Pos might be collinear with either, but can't be
                /// collinear with both. Our U vector is then Cross(V, Pos).
                Vector3 PosCrossX = new Vector3(0, TexW.Z, -TexW.Y);
                Vector3 ZCrossPos = new Vector3(-TexW.Y, TexW.X, 0);
                TexV = PosCrossX.LengthSquared() > ZCrossPos.LengthSquared()
                    ? PosCrossX
                    : ZCrossPos;
                TexV.Normalize();
                TexU = Vector3.Cross(TexV, TexW);
                TexV.Normalize();

                WorldCenter = actor.WorldCollisionCenter;
            }

            public void Render(Camera camera, Effect effect, EffectCache cache)
            {
                BoundingSphere bound = new BoundingSphere(WorldCenter, Radius);
                if (Frustum.CullResult.TotallyOutside != camera.Frustum.CullTest(bound))
                {
                    float bloom = Age;
                    bloom *= bloom * bloom;
                    bloom = 1.0f - bloom;
                    bloom *= 0.2f;
                    ShaderGlobals.FixExplicitBloom(bloom);


                    GraphicsDevice device = BokuGame.bokuGame.GraphicsDevice;

                    Sphere sphere = Sphere.GetInstance();

                    sphere.PreDraw(device);

                    /// Set effect parameters here.
                    float ageParm = 2.0f - 2.0f / (Age + 1.0f);
                    Parameter(EffectParams.Age).SetValue(ageParm);
                    Parameter(EffectParams.Center).SetValue(new Vector4(WorldCenter, Radius));
                    Parameter(EffectParams.TexU).SetValue(TexU);
                    Parameter(EffectParams.TexV).SetValue(TexV);
                    Parameter(EffectParams.TexW).SetValue(TexW);

                    float colorParm = 1.0f - Age;
                    colorParm *= colorParm * colorParm;
                    colorParm = 1.0f - colorParm;
                    Vector4 tint0 = new Vector4(0.5f * Tint.X, 1.0f * Tint.Y, 0.6f * Tint.Z, 1.0f);
                    Vector4 tint1 = new Vector4(Tint, 1.0f);
                    Vector4 tint = tint0 + (tint1 - tint0) * colorParm;
                    Parameter(EffectParams.Tint0).SetValue(tint);
                    Parameter(EffectParams.Tint1).SetValue(tint);

                    sphere.DrawPrim(effect);
                }
            }

            public static void RenderActive(Camera camera, Effect effect, EffectCache cache)
            {
                for (int i = 0; i < _actives.Count; ++i)
                {
                    _actives[i].Render(camera, effect, cache);
                }
            }

            public static void UpdateActive()
            {
                double t = Time.GameTimeTotalSeconds;
                int good = 0;
                for(int i = 0; i < _actives.Count; ++i)
                {
                    if (_actives[i].Update(t))
                    {
                        _actives[good++] = _actives[i];
                    }
                    else
                    {
                        _avails.Add(_actives[i]);
                    }
                }
                _actives.RemoveRange(good, _actives.Count - good);
            }

            public static void ClearActive()
            {
                _avails.AddRange(_actives);
                _actives.Clear();
            }
            #endregion Public

            #region Internal

            private bool Update(double t)
            {
                Age = (float)(t - Birth) / Life;

                AddSparks();

                if(Actor != null)
                {
                    if (Actor.CurrentState == GameThing.State.Inactive)
                    {
                        Actor = null;
                    }
                    else
                    {
                        WorldCenter = Actor.WorldCollisionCenter;
                    }
                }

                return Age < 1.0f;
            }

            private const int kMaxSparks = 10;
            private float leftOvers = 0;
            private Vector3[] _positions = new Vector3[kMaxSparks];
            private Vector3[] _velocities = new Vector3[kMaxSparks];
            private void AddSparks()
            {
                float maxRate = 100.0f;
                float numToGen = maxRate * Time.GameTimeFrameSeconds + leftOvers;
                int numSparks = (int)numToGen;
                if (numSparks > kMaxSparks)
                    numSparks = kMaxSparks;
                leftOvers = numToGen - numSparks;

                Vector3 worldCenter = WorldCenter;
                Vector3 line = worldCenter + TexW * Radius * (1.0f - 2.0f * Age);
                float r = (float)Math.Sqrt(Radius * Radius - (line - worldCenter).LengthSquared());

                Random rnd = BokuGame.bokuGame.rnd;

                float speed = 0.5f;

                for (int i = 0; i < numSparks; ++i)
                {
                    double ang = rnd.NextDouble() * Math.PI * 2.0;
                    float sing = (float)Math.Sin(ang);
                    float cosg = (float)Math.Cos(ang);
                    _velocities[i] = TexU * cosg + TexV * sing;
                    _positions[i] = line + _velocities[i] * r;
                    _velocities[i] *= speed;
                }

                ExplosionManager.CreateSparks(numSparks, _positions, _velocities, 0.05f);
            }

            #region Bookkeeping
            private static List<Impact> _actives = new List<Impact>();
            private static List<Impact> _avails = new List<Impact>();

            public static Impact Alloc()
            {
                Impact impact = null;
                if (_avails.Count > 0)
                {
                    impact = _avails[_avails.Count - 1];
                    _avails.RemoveAt(_avails.Count - 1);
                }
                else
                {
                    impact = new Impact();
                }
                _actives.Add(impact);

                return impact;
            }
            public static Impact Free(Impact impact)
            {
                _actives.Remove(impact);
                _avails.Add(impact);

                return null;
            }
            #endregion Bookkeeping

            #endregion Internal
        }
        #endregion ChildClasses

        #region Members
        #region Parameter Caching
        private enum EffectParams
        {
            WorldViewProjMatrix,
            WorldMatrix,

            Center,
            Age,
            TexU,
            TexV,
            TexW,
            Tint0,
            Tint1,
            CrossTexture,
            AxialTexture,
        };
        private static EffectCache effectCache = new EffectCache<EffectParams>();
        private static EffectParameter Parameter(EffectParams param)
        {
            return effectCache.Parameter((int)param);
        }
        private enum EffectTechs
        {

        }
        private static EffectTechnique Technique(InGame.RenderEffect renderEffect)
        {
            return effectCache.Technique(renderEffect, true);
        }
        private static Effect effect = null;
        #endregion Parameter Caching

        private static Texture2D crossTexture = null;
        private static Texture2D axialTexture = null;
        #endregion Members

        #region Accessors
        private static Texture2D CrossTexture
        {
            get { return crossTexture; }
            set { crossTexture = value; }
        }
        private static Texture2D AxialTexture
        {
            get { return axialTexture; }
            set { axialTexture = value; }
        }
        #endregion Accessors

        #region Public

        public static void Clear()
        {
            Impact.ClearActive();
        }

        public static void Update()
        {
            Impact.UpdateActive();
        }

        public static void Render(Camera camera)
        {
            Parameter(EffectParams.WorldMatrix).SetValue(Matrix.Identity);
            Parameter(EffectParams.WorldViewProjMatrix).SetValue(camera.ViewProjectionMatrix);
            Parameter(EffectParams.CrossTexture).SetValue(CrossTexture);

            effect.CurrentTechnique = Technique(InGame.inGame.renderEffects);

            Impact.RenderActive(camera, effect, effectCache);

            ShaderGlobals.ReleaseExplicitBloom();
        }

        public static void AddImpact(
            GameActor actor,
            Vector3 worldPosition,
            float radius,
            Vector3 tint)
        {
            if (!BokuSettings.Settings.PreferReach)
            {
                double timeSinceLast = Time.GameTimeTotalSeconds - actor.LastImpact;
                double kMinTime = 0.4;
                if (timeSinceLast >= kMinTime)
                {
                    Impact impact = Impact.Alloc();
                    impact.Set(actor, worldPosition);
                    impact.Radius = radius;
                    impact.Strength = 1.0f;
                    impact.Life = 1.0f;
                    impact.Tint = tint;

                    actor.LastImpact = Time.GameTimeTotalSeconds;
                }
            }
        }

        public static void AddImpact(
            GameActor actor,
            Vector3 worldPosition,
            float radius)
        {
//            AddImpact(actor, worldPosition, radius, new Vector3(0.5f, 1.0f, 0.6f));
            AddImpact(actor, worldPosition, radius, new Vector3(1.0f, 0.3f, 0.2f));
        }

        #endregion Public

        #region Internal
        public static void Load()
        {
            if (effect == null)
            {
                effect = BokuGame.Load<Effect>(BokuGame.Settings.MediaPath + @"Shaders\Shield");
                ShaderGlobals.RegisterEffect("Shield", effect);
                effectCache.Load(effect, "");
            }

            if (crossTexture == null)
            {
                crossTexture = BokuGame.Load<Texture2D>(BokuGame.Settings.MediaPath + @"Textures\Ring");
            }
        }

        public static void Unload()
        {
            if (effect != null)
            {
                effectCache.UnLoad();
                effect = null;
            }

            BokuGame.Release(ref crossTexture);
        }
        #endregion Internal
    };
};

