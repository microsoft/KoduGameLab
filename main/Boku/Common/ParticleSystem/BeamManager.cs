// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System;
using System.Collections.Generic;
using System.Diagnostics;

using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

using Boku.Base;
using Boku.Common;
using Boku.Fx;
using Boku.SimWorld.Collision;
using Boku.SimWorld.Terra;

namespace Boku.Common.ParticleSystem
{
    public class BeamManager : BaseSharedEmitter
    {
        #region Child Data Types
        internal struct Beam
        {
            public GameActor TargetActor;
            /// <summary>
            /// Current position. Render position.z will vary from this. 
            /// </summary>
            public Vector3 Position; 
            /// <summary>
            /// Constant velocity
            /// </summary>
            public Vector3 Velocity;  
            /// <summary>
            /// How high above position we start life.
            /// </summary>
            public float StartHeight; 
            /// <summary>
            /// time left to live
            /// </summary>
            public float TTL; 
            /// <summary>
            /// total time to live
            /// </summary>
            public float Life; 
            /// <summary>
            /// color we are fired as
            /// </summary>
            public Color Color; 
            /// <summary>
            /// When this Beam will hit the terrain (same as Life if never).
            /// </summary>
            public float TTTerraHit;
        }

        private struct BeamVertex : IVertexType
        {
            /// <summary>
            /// where in world space, updated as it moves through world.
            /// </summary>
            public Vector3 Position;
            /// <summary>
            /// constant color
            /// </summary>
            public UInt32  Color;
            /// <summary>
            /// uv.xy, normalized age, starting height. Only TexCoord.z (age) changes.
            /// </summary>
            public Vector4 TexCoord;

            static VertexDeclaration decl = null;
            static VertexElement[] elements = new VertexElement[]
            {
                new VertexElement(0, VertexElementFormat.Vector3, VertexElementUsage.Position, 0),
                new VertexElement(12, VertexElementFormat.Color, VertexElementUsage.Color, 0),
                new VertexElement(16, VertexElementFormat.Vector4, VertexElementUsage.TextureCoordinate, 0)
            };

            public VertexDeclaration VertexDeclaration
            {
                get
                {
                    if (decl == null || decl.IsDisposed)
                    {
                        decl = new VertexDeclaration(elements);
                    }
                    return decl;
                }
            }

        };

        // used to raise the beam target to avoid some terrain that might be blocking the rock
        private float heightOffSet = 1.0f;

        /// <summary>
        /// Packet of data for storage of Beam state. Don't be poking around in here,
        /// it's strictly black box to the rest of the world.
        /// </summary>
        public class BeamData
        {
            internal List<Beam> Beams = new List<Beam>();

            public void Clear() { Beams.Clear(); }
        }
        #endregion Child Data Types

        #region Members
        private List<GameActor> shooters = new List<GameActor>();
        private int numActiveBeams = 0;

        /// <summary>
        /// The most Beams active at once. Firing fails after this number is reached
        /// until some die off.
        /// </summary>
        private const int kMaxBeams = 2000;
        /// <summary>
        /// Max number of vertices.
        /// </summary>
        private const int kMaxVerts = kMaxBeams * 4;
        /// <summary>
        /// The shared local vertex array.
        /// </summary>
        private BeamVertex[] verts = new BeamVertex[kMaxVerts];
        /// <summary>
        /// Dynamic vertex buffer actually used for rendering.
        /// </summary>
        private DynamicVertexBuffer vbuf = null;

        /// <summary>
        /// Texture2D for the Beams.
        /// </summary>
        private Texture2D texture = null;

        /// <summary>
        /// Effect used in rendering.
        /// </summary>
        private Effect effect = null;
        #endregion Members

        #region Accessors
        /// <summary>
        /// Which actors currently have birds in the air.
        /// </summary>
        private List<GameActor> Shooters
        {
            get { return shooters; }
        }
        #endregion Accessors

        #region Public

        /// <summary>
        /// Make us.
        /// </summary>
        /// <param name="manager"></param>
        public BeamManager(ParticleSystemManager manager)
            : base(manager)
        {
            maxParticles = kMaxBeams;
        }

        /// <summary>
        /// Throw away all shooters, because we've just unloaded a level or something.
        /// </summary>
        public void Clear()
        {
            for (int i = 0; i < shooters.Count; ++i)
            {
                shooters[i].ClearBeams();
            }
            shooters.Clear();
        }

        /// <summary>
        /// Fire a missile from the given shooter's position at the target position.
        /// </summary>
        /// <param name="shooter"></param>
        /// <param name="targetPos"></param>
        /// <param name="color"></param>
        /// <param name="verbPayload"></param>
        /// <returns></returns>
        public bool Fire(GameActor shooter, 
            GameActor target,
            Vector3 targetPos, 
            Classification.Colors color)
        {
            if (numActiveBeams < kMaxBeams)
            {
                float speed = shooter.BeamSpeed;
                float time2Live = shooter.BeamDist / speed;
                Beam Beam = new Beam();
                Beam.Position = shooter.WorldCollisionCenter;
                Beam.StartHeight = shooter.WorldCollisionCenter.Z;
                Beam.Position.Z = shooter.WorldCollisionCenter.Z;
                // Save out the actor 
                Beam.TargetActor = target;
                // Raise the height of the target pos slightly to avoid terrain hits
                targetPos.Z += heightOffSet;
                Beam.Velocity = Vector3.Normalize(targetPos - Beam.Position) * speed;
                float vdotv = Vector3.Dot(Beam.Velocity, shooter.Movement.Velocity);
                if (vdotv > 0)
                {
                    /// If the bullet is moving the same direction as the shooter,
                    /// add in the shooter's speed in the bullet's direction.
                    /// This will only speed up the bullet, not change it's firing
                    /// direction.
                    vdotv /= Beam.Velocity.LengthSquared();
                    Beam.Velocity += Beam.Velocity * vdotv;
                }
                Beam.Life = Beam.TTL = time2Live;

                Vector4 color4 = Classification.ColorVector4(color);
                /// Brighten up the color a bit.
                color4.X = MyMath.SmoothStep(0.0f, 1.0f, color4.X);
                color4.Y = MyMath.SmoothStep(0.0f, 1.0f, color4.Y);
                color4.Z = MyMath.SmoothStep(0.0f, 1.0f, color4.Z);
                Beam.Color = new Color(color4);

                Beam.TTTerraHit = CheckTerrainHit(Beam, speed);

                if (shooter.ActiveBeams.Beams.Count == 0)
                {
                    Shooters.Add(shooter);
                }
                shooter.ActiveBeams.Beams.Add(Beam);

                return true;
            }
            return false;
        }

        /// <summary>
        /// Update all birds in the air.
        /// </summary>
        public override void Update()
        {
            numActiveBeams = 0;
            int vtxIdx = 0;
            if (Shooters.Count > 0)
            {
                float dt = Time.GameTimeFrameSeconds;

                for(int shooterIdx = Shooters.Count - 1; shooterIdx >= 0; --shooterIdx)
                {
                    GameActor shooter = Shooters[shooterIdx];

                    List<Beam> Beams = shooter.ActiveBeams.Beams;

                    for (int i = Beams.Count-1; i >= 0; --i)
                    {
                        Beam Beam = Beams[i];
                        Beam.TTL -= dt;
                        if (Beam.TTL <= 0)
                        {
                            OnExpire(shooter);
                            RemoveParticle(shooter, Beams, i);
                        }
                        else if (HitSomething(shooter, Beam))
                        {
                            RemoveParticle(shooter, Beams, i);
                        }
                        else
                        {
                            Beam.Position.X += Beam.Velocity.X * dt;
                            Beam.Position.Y += Beam.Velocity.Y * dt;
                            Beam.Position.Z += Beam.Velocity.Z * dt;

                            Beams[i] = Beam;

                            BeamVertex vert = new BeamVertex();
                            vert.Position = Beam.Position;
                            vert.Color = Beam.Color.PackedValue;
                            vert.TexCoord = new Vector4(
                                0.0f, 0.0f,
                                1.0f - Beam.TTL / Beam.Life,
                                Beam.StartHeight);

                            verts[vtxIdx++] = vert;

                            vert.TexCoord.X = 1.0f;
                            verts[vtxIdx++] = vert;
                            vert.TexCoord.Y = 1.0f;
                            verts[vtxIdx++] = vert;
                            vert.TexCoord.X = 0.0f;
                            verts[vtxIdx++] = vert;

                        }
                    }

                    Debug.Assert(shooter.ActiveBeams.Beams == Beams);
                    if (Beams.Count == 0)
                    {
                        Shooters.RemoveAt(shooterIdx);
                    }
                    else
                    {
                        numActiveBeams += Beams.Count;
                    }
                }
            }
            if (numActiveBeams > 0)
            {
                UpdateBuffer(numActiveBeams);                
            }
        }

        /// <summary>
        /// Sets up all the common stuff needed for rendering.  This includes
        /// setting the technique and any parameters that don't change from
        /// one batch to the next.
        /// </summary>
        public override void PreRender(Camera camera)
        {
            if (InGame.inGame.renderEffects == InGame.RenderEffect.Normal)
            {
                if (numActiveBeams > 0)
                {
                    GraphicsDevice device = BokuGame.bokuGame.GraphicsDevice;

                    effect.CurrentTechnique = effect.Techniques[@"BeamPass"];

                    // Set up common rendering values.
                    effect.Parameters["CurrentTime"].SetValue((float)Time.GameTimeTotalSeconds);

                    // Set up world matrix.
                    Matrix worldMatrix = Matrix.Identity;
                    Matrix worldViewProjMatrix = worldMatrix * camera.ViewProjectionMatrix;

                    effect.Parameters["DiffuseTexture"].SetValue(texture);
                    effect.Parameters["EyeLocation"].SetValue(new Vector4(camera.ActualFrom, 1.0f));
                    effect.Parameters["CameraUp"].SetValue(new Vector4(camera.ViewUp, 1.0f));
                    effect.Parameters["WorldMatrix"].SetValue(worldMatrix);
                    effect.Parameters["WorldViewProjMatrix"].SetValue(worldViewProjMatrix);

                    float radius = 0.2f;
                    float minPix = 3.0f;
                    float maxPix = 6.0f;
                    Vector4 pixelLimit = ShaderGlobals.MakeParticleSizeLimit(radius, minPix, maxPix);
                    effect.Parameters["ParticleRadius"].SetValue(pixelLimit);

                    device.Indices = ibuf;

                    device.SetVertexBuffer(vbuf);
                }
            }

        }

        /// <summary>
        /// Any post-render stuff that needs to be restored after rendering a batch.
        /// </summary>
        public override void PostRender()
        {
        } 

        /// <summary>
        /// Render the currently active Beams. Currently only renders on normal pass.
        /// </summary>
        /// <param name="camera"></param>
        public override void Render(Camera camera)
        {
            if (InGame.inGame.renderEffects == InGame.RenderEffect.Normal)
            {
                GraphicsDevice device = BokuGame.bokuGame.GraphicsDevice;

                int numVerts = numActiveBeams * 4;
                int numTris = numActiveBeams * 2;
                if (numTris > 0)
                {
                    // Render all passes.
                    for (int i = 0; i < effect.CurrentTechnique.Passes.Count; i++)
                    {
                        EffectPass pass = effect.CurrentTechnique.Passes[i];
                        pass.Apply();
                        device.DrawIndexedPrimitives(
                            PrimitiveType.TriangleList, 
                            0, 0, 
                            numVerts, 
                            0, numTris);
                    }
                }
            }
        }
        #endregion Public

        #region Internal

        /// <summary>
        /// Update the vertex buffer from the local vertex data.
        /// </summary>
        /// <param name="numActive"></param>
        private void UpdateBuffer(int numActive)
        {
            int numVerts = numActive * 4;
            vbuf.SetData<BeamVertex>(verts, 0, numVerts, SetDataOptions.NoOverwrite);
        }

        /// <summary>
        /// Take care of necessary callbacks when a beam hits a thing.
        /// </summary>
        /// <param name="shooter"></param>
        /// <param name="hitThing"></param>
        /// <param name="beam"></param>
        private void OnHit(GameActor shooter, GameThing hitThing, Vector3 hitPos, Beam beam)
        {
            Debug.Assert(shooter != null);

            shooter.OnBeamHit(hitThing, hitPos);
        }
        private void OnExpire(GameActor shooter)
        {
            shooter.OnBeamExpire();
        }

        /// <summary>
        /// Remove a beam from the list.
        /// </summary>
        /// <param name="shooter"></param>
        /// <param name="beams"></param>
        /// <param name="particle"></param>
        private void RemoveParticle(GameActor shooter, List<Beam> beams, int particle)
        {
            Debug.Assert(particle < beams.Count);
            int last = beams.Count - 1;
            if (particle != last)
            {
                beams[particle] = beams[last];
            }
            beams.RemoveAt(last);
        }

        /// <summary>
        /// Look ahead over the life of this beam (since it's strictly inertial)
        /// for when (if ever) it would strike terrain or path. We do the single
        /// LOS check with background on firing, but checks against other objects
        /// (which are probably moving) every frame.
        /// </summary>
        /// <param name="beam"></param>
        /// <param name="speed"></param>
        /// <returns></returns>
        private float CheckTerrainHit(Beam beam, float speed)
        {
            Vector3 start = beam.Position;
            Vector3 end = start + beam.Velocity * beam.TTL;

            Vector3 terrainHitPos = Vector3.Zero;
            float ttTerraHit = beam.Life * 2;
            if( SimWorld.Terra.Terrain.LOSCheckTerrainAndPath(start, end, ref terrainHitPos))
            {
                Vector3 total = end - start;
                float invRangeSq = beam.Life / total.LengthSquared();
                
                Vector3 toTerraHit = terrainHitPos - start;

                ttTerraHit = Vector3.Dot(toTerraHit, total) * invRangeSq;
            }
            return ttTerraHit;
        }


        /// <summary>
        /// Test to see if a beam hit anything, object or terrain.
        /// </summary>
        /// <param name="shooter"></param>
        /// <param name="beams"></param>
        /// <param name="particle"></param>
        /// <returns></returns>
        private bool HitSomething(GameActor shooter, Beam beam)
        {
            Vector3 start = beam.Position;
            Vector3 end = start + beam.Velocity * Time.GameTimeFrameSeconds;

            Vector3 terrainHit = end;
            bool hitTerrain = beam.Life - beam.TTL >= beam.TTTerraHit;
            if (hitTerrain)
            {
                /// terrain hit is current position 
                /// - what we've travelled already (vel * (life - ttl))
                /// + time from beginning to terrain hit (vel * ttTerraHit)
                /// We use end as current position because we've already updated TTL
                /// but haven't yet advanced .Position. When we do advance .Position,
                /// it will be to "end".
                terrainHit = end + beam.Velocity * (beam.TTTerraHit - (beam.Life - beam.TTL));
            }

            Vector3 hitPoint = terrainHit;
            GameActor hitThing = beam.TargetActor;
            if (hitThing != null)
            {
                // never hit terrain if we have a valid target actor
                hitTerrain = false;
                hitPoint = beam.TargetActor.WorldCollisionCenter;
            }

            // used the actor to determine if we've hit the target
            Vector3 beamVector = end - hitPoint;
            float beamLength = beamVector.LengthSquared();
            bool hitTarget = beamLength < 0.25f;

            if (hitTerrain && hitTarget)
            {
                float beamToTerrain = Vector3.DistanceSquared(start, terrainHit);
                float beamToHit = Vector3.DistanceSquared(start, hitPoint);
                if ((beamToHit - beamToTerrain) > 0.25f)
                {
                    hitThing = null;
                    hitPoint = terrainHit;
                }
            }

            if (hitTarget)
            {
                OnHit(shooter, hitThing, hitPoint, beam);
            }
            else if (hitTerrain)
            {
                OnHit(shooter, null, terrainHit, beam);
                ExplosionManager.CreateSpark(terrainHit, 3, 0.4f, 1.0f);
            }

            return hitTerrain || hitTarget;
        }
        /// <summary>
        /// Internal scratch list, don't mess with this!!!
        /// </summary>
        private static List<HitInfo> _scratchHitInfo = new List<HitInfo>();

        /// <summary>
        /// Returns true if this thing should be excluded from being hit.
        /// </summary>
        /// <param name="thing"></param>
        /// <returns></returns>
        private bool ExcludedHitThing(GameActor shooter, GameThing thing)
        {
            return (thing == null)
                || (thing as CruiseMissile != null)
                || (thing == shooter)
                || (thing.ActorHoldingThis == shooter)
                || ((thing.CurrentState != GameThing.State.Active)
                    &&(thing.ActorHoldingThis == null));
        }

        /// <summary>
        /// Load our shader and texture, then on to base load.
        /// </summary>
        /// <param name="immediate"></param>
        public override void LoadContent(bool immediate)
        {
            if (effect == null)
            {
                effect = BokuGame.Load<Effect>(BokuGame.Settings.MediaPath + @"Shaders\SharedParticle2D");
            }

            if (texture == null)
            {
                texture = BokuGame.Load<Texture2D>(BokuGame.Settings.MediaPath + @"Textures\Beam");
            }

            base.LoadContent(immediate);
        }   // end of SharedSmokeEmitter LoadContent()

        /// <summary>
        /// Device specifics created, vertex buffer and decl.
        /// </summary>
        /// <param name="graphics"></param>
        public override void InitDeviceResources(GraphicsDevice device)
        {
            if (vbuf == null)
            {
                vbuf = new DynamicVertexBuffer(device, typeof(BeamVertex), 4 * maxParticles, BufferUsage.WriteOnly);
            }

            base.InitDeviceResources(device);
        }

        /// <summary>
        /// Dump our stuff and then on to base.
        /// </summary>
        public override void UnloadContent()
        {
            BokuGame.Release(ref effect);
            BokuGame.Release(ref vbuf);
            BokuGame.Release(ref texture);

            base.UnloadContent();
        }   // end of SharedSmokeEmitter UnloadContent()
        #endregion Internal
    }
}
