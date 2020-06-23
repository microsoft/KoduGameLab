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
    public class BleepManager : BaseSharedEmitter
    {
        #region Child Data Types
        internal struct Bleep
        {
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
            /// The number of hit-points this missile takes from its target
            /// </summary>
            public int Damage;
            /// <summary>
            /// Payload to deliver on impact.
            /// </summary>
            public GameThing.Verbs VerbPayload; 
            /// <summary>
            /// When this bleep will hit the terrain (same as Life if never).
            /// </summary>
            public float TTTerraHit;
        }
        private struct BleepVertex : IVertexType
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

        /// <summary>
        /// Packet of data for storage of bleep state. Don't be poking around in here,
        /// it's strictly black box to the rest of the world.
        /// </summary>
        public class BleepData
        {
            internal List<Bleep> Bleeps = new List<Bleep>();

            public void Clear() { Bleeps.Clear(); }
        }
        #endregion Child Data Types

        #region Members
        private List<GameActor> shooters = new List<GameActor>();

        /// <summary>
        /// Number of active blips.  This really needs to be refactored.  What this
        /// is doing isd counting the number of blips updated during the frame.  This
        /// number is then used to prevent further firing of blips.  This required
        /// that this system is updated BEFORE any brains are updated.
        /// </summary>
        private int numActiveBleeps = 0;

        /// <summary>
        /// The most bleeps active at once. Firing fails after this number is reached
        /// until some die off.
        /// This is the max that will be rendered per frame.  Any extras will be updated
        /// but not rendered.
        /// </summary>
        private const int kMaxBleeps = 2000;
        /// <summary>
        /// Max number of vertices.
        /// </summary>
        private const int kMaxVerts = kMaxBleeps * 4;
        /// <summary>
        /// The shared local vertex array.
        /// </summary>
        private BleepVertex[] verts = new BleepVertex[kMaxVerts];
        /// <summary>
        /// Dynamic vertex buffer actually used for rendering.
        /// </summary>
        private DynamicVertexBuffer vbuf = null;

        /// <summary>
        /// Texture2D for the bleeps.
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
        public BleepManager(ParticleSystemManager manager)
            : base(manager)
        {
            maxParticles = kMaxBleeps;
        }

        /// <summary>
        /// Throw away all shooters, because we've just unloaded a level or something.
        /// </summary>
        public void Clear()
        {
            for (int i = 0; i < shooters.Count; ++i)
            {
                shooters[i].ClearBleeps();
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
            Vector3 targetPos, 
            Classification.Colors color,
            GameThing.Verbs verbPayload,
            int damage)
        {
            if (numActiveBleeps < kMaxBleeps)
            {
                float speed = shooter.BlipSpeed;
                float time2Live = shooter.BlipRange / speed;
                Bleep bleep = new Bleep();
                bleep.Position = shooter.WorldGlowPosition;
                bleep.StartHeight = bleep.Position.Z;
                bleep.Position.Z = shooter.WorldCollisionCenter.Z;
                bleep.Velocity = Vector3.Normalize(targetPos - bleep.Position) * speed;
                float vdotv = Vector3.Dot(bleep.Velocity, shooter.Movement.Velocity);
                if (vdotv > 0)
                {
                    /// If the bullet is moving the same direction as the shooter,
                    /// add in the shooter's speed in the bullet's direction.
                    /// This will only speed up the bullet, not change it's firing
                    /// direction.
                    vdotv /= bleep.Velocity.LengthSquared();
                    bleep.Velocity += bleep.Velocity * vdotv;
                }
                bleep.Life = bleep.TTL = time2Live;
                bleep.VerbPayload = verbPayload;
                bleep.Damage = damage;

                Vector4 color4 = Classification.ColorVector4(color);
                /// Brighten up the color a bit.
                color4.X = MyMath.SmoothStep(0.0f, 1.0f, color4.X);
                color4.Y = MyMath.SmoothStep(0.0f, 1.0f, color4.Y);
                color4.Z = MyMath.SmoothStep(0.0f, 1.0f, color4.Z);
                bleep.Color = new Color(color4);

                bleep.TTTerraHit = CheckTerrainHit(bleep, speed);

                if (shooter.ActiveBleeps.Bleeps.Count == 0)
                {
                    Shooters.Add(shooter);
                }
                shooter.ActiveBleeps.Bleeps.Add(bleep);

                // Increment the number of active bleeps.  Without this, on the
                // current frame we can end up shooting enough new bleeps to 
                // exceed the max.
                ++numActiveBleeps;

                return true;
            }
            return false;
        }

        /// <summary>
        /// Update all birds in the air.
        /// </summary>
        public override void Update()
        {
            numActiveBleeps = 0;
            int vtxIdx = 0;
            if (Shooters.Count > 0)
            {
                float dt = Time.GameTimeFrameSeconds;

                for(int shooterIdx = Shooters.Count - 1; shooterIdx >= 0; --shooterIdx)
                {
                    GameActor shooter = Shooters[shooterIdx];

                    List<Bleep> bleeps = shooter.ActiveBleeps.Bleeps;

                    for (int i = bleeps.Count-1; i >= 0; --i)
                    {
                        Bleep bleep = bleeps[i];
                        bleep.TTL -= dt;
                        if (bleep.TTL <= 0)
                        {
                            OnExpire(shooter);
                            RemoveParticle(shooter, bleeps, i);
                        }
                        else if (HitSomething(shooter, bleep))
                        {
                            RemoveParticle(shooter, bleeps, i);
                        }
                        else
                        {
                            // Update the current blip.
                            bleep.Position.X += bleep.Velocity.X * dt;
                            bleep.Position.Y += bleep.Velocity.Y * dt;
                            bleep.Position.Z += bleep.Velocity.Z * dt;

                            bleeps[i] = bleep;

                            // If we have room in the buffer to render it, do so.
                            if (numActiveBleeps < kMaxBleeps)
                            {
                                BleepVertex vert = new BleepVertex();
                                vert.Position = bleep.Position;
                                vert.Color = bleep.Color.PackedValue;
                                vert.TexCoord = new Vector4(
                                    0.0f, 0.0f,
                                    1.0f - bleep.TTL / bleep.Life,
                                    bleep.StartHeight);

                                verts[vtxIdx++] = vert;

                                vert.TexCoord.X = 1.0f;
                                verts[vtxIdx++] = vert;
                                vert.TexCoord.Y = 1.0f;
                                verts[vtxIdx++] = vert;
                                vert.TexCoord.X = 0.0f;
                                verts[vtxIdx++] = vert;

                                ++numActiveBleeps;
                            }
                            else
                            {
                                Debug.Assert(false, "We should never get here.  If we do it means that we're allowing Fire() to happen when we're already maxed out.");
                            }
                        }
                    }

                    Debug.Assert(shooter.ActiveBleeps.Bleeps == bleeps);
                    if (bleeps.Count == 0)
                    {
                        Shooters.RemoveAt(shooterIdx);
                    }
                }
            }
            if (numActiveBleeps > 0)
            {
                UpdateBuffer(numActiveBleeps);                
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
                if (numActiveBleeps > 0)
                {
                    GraphicsDevice device = BokuGame.bokuGame.GraphicsDevice;

                    effect.CurrentTechnique = effect.Techniques[@"BleepPass"];

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
        /// Render the currently active bleeps. Currently only renders on normal pass.
        /// </summary>
        /// <param name="camera"></param>
        public override void Render(Camera camera)
        {
            if (InGame.inGame.renderEffects == InGame.RenderEffect.Normal)
            {
                GraphicsDevice device = BokuGame.bokuGame.GraphicsDevice;

                int numVerts = numActiveBleeps * 4;
                int numTris = numActiveBleeps * 2;
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
            vbuf.SetData<BleepVertex>(verts, 0, numVerts, SetDataOptions.NoOverwrite);
        }

        /// <summary>
        /// Take care of necessary callbacks when a Bleep hits a thing.
        /// </summary>
        /// <param name="shooter"></param>
        /// <param name="hitThing"></param>
        /// <param name="bleep"></param>
        private void OnHit(GameActor shooter, GameThing hitThing, Vector3 hitPos, Bleep bleep)
        {
            Debug.Assert(shooter != null);

            shooter.OnBlipHit(hitThing, hitPos, bleep.VerbPayload, bleep.Damage);
        }
        private void OnExpire(GameActor shooter)
        {
            shooter.OnBlipExpire();
        }

        /// <summary>
        /// Remove a bleep from the list.
        /// </summary>
        /// <param name="shooter"></param>
        /// <param name="bleeps"></param>
        /// <param name="particle"></param>
        private void RemoveParticle(GameActor shooter, List<Bleep> bleeps, int particle)
        {
            Debug.Assert(particle < bleeps.Count);
            int last = bleeps.Count - 1;
            if (particle != last)
            {
                bleeps[particle] = bleeps[last];
            }
            bleeps.RemoveAt(last);
        }

        /// <summary>
        /// Look ahead over the life of this bleep (since it's strictly inertial)
        /// for when (if ever) it would strike terrain or path. We do the single
        /// LOS check with background on firing, but checks against other objects
        /// (which are probably moving) every frame.
        /// </summary>
        /// <param name="bleep"></param>
        /// <param name="speed"></param>
        /// <returns></returns>
        private float CheckTerrainHit(Bleep bleep, float speed)
        {
            Vector3 start = bleep.Position;
            Vector3 end = start + bleep.Velocity * bleep.TTL;

            Vector3 terrainHitPos = Vector3.Zero;
            float ttTerraHit = bleep.Life * 2;
            if( SimWorld.Terra.Terrain.LOSCheckTerrainAndPath(start, end, ref terrainHitPos))
            {
                Vector3 total = end - start;
                float invRangeSq = bleep.Life / total.LengthSquared();
                
                Vector3 toTerraHit = terrainHitPos - start;

                ttTerraHit = Vector3.Dot(toTerraHit, total) * invRangeSq;
            }
            return ttTerraHit;
        }


        /// <summary>
        /// Test to see if a bleep hit anything, object or terrain.
        /// </summary>
        /// <param name="shooter"></param>
        /// <param name="bleeps"></param>
        /// <param name="particle"></param>
        /// <returns></returns>
        private bool HitSomething(GameActor shooter, Bleep bleep)
        {
            Vector3 start = bleep.Position;
            Vector3 end = start + bleep.Velocity * Time.GameTimeFrameSeconds;

            Vector3 terrainHit = end;
            bool hitTerrain = bleep.Life - bleep.TTL >= bleep.TTTerraHit;
            if (hitTerrain)
            {
                /// terrain hit is current position 
                /// - what we've travelled already (vel * (life - ttl))
                /// + time from beginning to terrain hit (vel * ttTerraHit)
                /// We use end as current position because we've already updated TTL
                /// but haven't yet advanced .Position. When we do advance .Position,
                /// it will be to "end".
                terrainHit = end + bleep.Velocity * (bleep.TTTerraHit - (bleep.Life - bleep.TTL));
            }

            const float kBleepRadius = 0.25f;
            GameActor hitThing = null;
            Vector3 hitPoint = terrainHit;
            if (CollSys.TestAll(
                start,
                end,
                kBleepRadius,
                _scratchHitInfo))
            {
                for (int i = 0; i < _scratchHitInfo.Count; ++i)
                {
                    GameActor other = _scratchHitInfo[i].Other;
                    if (!ExcludedHitThing(shooter, other))
                    {
                        hitThing = other;
                        hitPoint = _scratchHitInfo[i].Contact;
                        break;
                    }
                }
                _scratchHitInfo.Clear();
            }

            if (hitTerrain && (hitThing != null))
            {

                if (Vector3.DistanceSquared(start, terrainHit) < Vector3.DistanceSquared(start, hitPoint))
                {
                    hitThing = null;
                    hitPoint = terrainHit;
                }
            }

            if (hitThing != null)
            {
                OnHit(shooter, hitThing, hitPoint, bleep);
            }
            else if (hitTerrain)
            {
                OnHit(shooter, null, hitPoint, bleep);
                ExplosionManager.CreateSpark(hitPoint, 3, 0.4f, 1.0f);
            }

            return hitTerrain || (hitThing != null); ;
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
            return (thing == null)                              // Exclude null.
                || (thing == shooter)                           // Don't allow shooting self.
                || (thing.ActorHoldingThis == shooter);         // Don't allow shooting things we are holding.
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
                texture = BokuGame.Load<Texture2D>(BokuGame.Settings.MediaPath + @"Textures\ShadowMask");
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
                vbuf = new DynamicVertexBuffer(device, typeof(BleepVertex), 4 * maxParticles, BufferUsage.WriteOnly);
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
