using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Xml.Serialization;

using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Audio;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using Microsoft.Xna.Framework.Storage;

using Boku.Base;
using Boku.Fx;
using Boku.Common.Xml;

namespace Boku.Common
{
    /// <summary>
    /// Renders health bars above actors.
    /// </summary>
    public static class HealthBarManager
    {
        #region Public Constants
        public const int kInvalidHandle = -1;

        #endregion

        #region Private Members

        #region Effect Cache
        private enum EffectParams
        {
            WorldViewProj,
            BackTexture,
            LifeTexture,
            LifeTint,
            LifePct,
            BackSize,
            LifeSize,
        }
        #endregion

        const float kLifeBarScalarX = 0.953125f;
        const float kLifeBarScalarY = 0.6875f;

        private /*const*/ static Vector2 kBackSize;
        private /*const*/ static Vector2 kLifeSize;
        private static List<GameActor> actors;
        private static List<int> freelist;

        private static Texture2D backTexture;
        private static Texture2D lifeTexture;
        private static Effect effect;

        private static EffectCache effectCache;
        private static VertexBuffer vertexBuf;

        #endregion

        #region Public Accessors
        
        public static Texture2D BackTexture
        {
            get { return backTexture; }
        }
        public static Texture2D LifeTexture
        {
            get { return lifeTexture; }
        }
        public static Effect Effect
        {
            get { return effect; }
        }

        #endregion

        #region Public Methods
        
        static HealthBarManager()
        {
            actors = new List<GameActor>();
            freelist = new List<int>();
        }

        /// <summary>
        /// Will register the actor if it wants its health bar rendered.
        /// </summary>
        /// <param name="actor"></param>
        public static void RegisterActor(GameActor actor)
        {
            // Be forgiving, let the app blindly throw actors at us for register.
            if (!actor.ShowHitPoints)
                return;

            if (actors.Contains(actor))
                return;

            if (freelist.Count == 0)
            {
                // No free slots in the array, add a new slot.
                actors.Add(actor);
                actor.HealthBarHandle = actors.Count - 1;
            }
            else
            {
                // Free slots exist, use the last one (avoids a memcpy when shrinking the freelist).
                actor.HealthBarHandle = freelist[freelist.Count - 1];
                actors[actor.HealthBarHandle] = actor;
                freelist.RemoveAt(freelist.Count - 1);
            }

        }

        /// <summary>
        /// Will unregister the actor if registered.
        /// </summary>
        /// <param name="actor"></param>
        public static void UnregisterActor(GameActor actor)
        {
            // Be forgiving, let the app blindly throw actors at us for unregister.
            if (actor.HealthBarHandle == HealthBarManager.kInvalidHandle)
                return;

            if (actors.Contains(actor))
            {
                // Recycle existing slots to avoid under-the-hood memcpys when
                // registering/unregistering actors.
                freelist.Add(actor.HealthBarHandle);

                actors[actor.HealthBarHandle] = null;
            }

            actor.HealthBarHandle = HealthBarManager.kInvalidHandle;

        }

        /// <summary>
        /// Remove all actors from the health bar manager.
        /// </summary>
        public static void UnregisterAllActors()
        {
            for (int i = 0; i < actors.Count; ++i)
            {
                GameActor actor = actors[i] as GameActor;

                if (actor == null)
                    continue;

                if (actor.HealthBarHandle == HealthBarManager.kInvalidHandle)
                    continue;

                freelist.Add(actor.HealthBarHandle);

                actors[actor.HealthBarHandle] = null;
                actor.HealthBarHandle = HealthBarManager.kInvalidHandle;
            }
        }

        /// <summary>
        /// Set whether registered or not based on parameter value.
        /// </summary>
        /// <param name="actor"></param>
        /// <param name="yes"></param>
        public static void SetRegistered(GameActor actor, bool yes)
        {
            if (yes)
                RegisterActor(actor);
            else
                UnregisterActor(actor);
        }

        private class GameActorCompare : IComparer
        {
            Camera camera;

            public GameActorCompare(Camera camera)
            {
                this.camera = camera;
            }

            public int Compare(object ox, object oy)
            {
                GameActor x = ox as GameActor;
                GameActor y = oy as GameActor;

                if (x != null && y != null)
                {
                    float distx = (x.Movement.Position - camera.ActualFrom).Length();
                    float disty = (y.Movement.Position - camera.ActualFrom).Length();
                    return distx > disty ? -1 : distx < disty ? 1 : 0;
                }
                else if (x == null && y == null)
                {
                    return 0;
                }
                else if (y == null)
                {
                    return -1;
                }
                else
                {
                    return 1;
                }
            }
        }

        /// <summary>
        /// Renders all the registered health bars.
        /// </summary>
        /// <param name="camera"></param>
        public static void Render(Camera camera)
        {
            if (actors.Count > 0)
            {
                GraphicsDevice device = BokuGame.bokuGame.GraphicsDevice;

                Vector3 forward = Vector3.Normalize(new Vector3(camera.ViewDir.X, camera.ViewDir.Y, 0));

                Parameter(EffectParams.BackTexture).SetValue(BackTexture);
                Parameter(EffectParams.LifeTexture).SetValue(LifeTexture);
                Parameter(EffectParams.BackSize).SetValue(kBackSize);
                Parameter(EffectParams.LifeSize).SetValue(kLifeSize);

                Array arr = actors.ToArray();
                Array.Sort(arr, new GameActorCompare(camera));

                Effect.CurrentTechnique = Effect.Techniques[0];

                ShaderGlobals.FixExplicitBloom(0.25f);

                GameActor firstPersonActor = null;

                for (int i = 0; i < Effect.CurrentTechnique.Passes.Count; ++i)
                {
                    EffectPass pass = Effect.CurrentTechnique.Passes[i];

                    float prevDist = float.MaxValue;

                    pass.Apply();

                    for (int j = 0; j < arr.Length; ++j)
                    {
                        GameActor actor = arr.GetValue(j) as GameActor;

                        // By design, the actor list CAN contain nulls.
                        if (actor == null)
                            continue;

                        // We'll handle first-person render separately.
                        if (actor.FirstPerson)
                        {
                            firstPersonActor = actor;
                            continue;
                        }

                        float dist = (actor.Movement.Position - camera.ActualFrom).Length();
                        Debug.Assert(dist <= prevDist);
                        prevDist = dist;

                        Debug.Assert(actor.HealthBarHandle != HealthBarManager.kInvalidHandle);

                        RenderHealthBar(actor, camera, forward);
                    }
                }

                if (firstPersonActor != null)
                {
                    RenderFirstPersonHealthBar(firstPersonActor, camera, forward);
                }

                ShaderGlobals.ReleaseExplicitBloom();
            }
        }

        #endregion

        #region Private Methods
        private static void RenderFirstPersonHealthBar(GameActor actor, Camera camera, Vector3 forward)
        {
            ScreenSpaceQuad quad = ScreenSpaceQuad.GetInstance();

            {
                Vector2 size = new Vector2(BackTexture.Width, BackTexture.Height);

                // Position at center bottom of screen.
                Vector2 position = new Vector2((camera.Resolution.X - size.X) / 2, (camera.Resolution.Y - size.Y));
                
                // Adjust for vertical overscan.
                position.Y -= 40;

                quad.Render(BackTexture, position, size, "TexturedRegularAlphaNoZ");
            }
            {
                float lifePct = (float)actor.HitPoints / (float)actor.MaxHitPoints;

                // Scale the life bar to fit within the background texture (originals are same size).
                Vector2 size = new Vector2(LifeTexture.Width * kLifeBarScalarX, LifeTexture.Height * kLifeBarScalarY);
                float yDiff = LifeTexture.Height - size.Y;

                // Position at center bottom of screen.
                Vector2 position = new Vector2((camera.Resolution.X - size.X) / 2, camera.Resolution.Y - (size.Y + yDiff / 2));

                // Adjust for vertical overscan.
                position.Y -= 40;

                // Scale the life bar based on current health.
                size.X *= lifePct;

                // Tint life bar based on current health.
                Vector4 tint = GetLifeTint(lifePct);

                quad.Render(LifeTexture, tint, position, size, "TexturedRegularAlphaNoZ");
            }
        }

        private static void RenderHealthBar(GameActor actor, Camera camera, Vector3 forward)
        {
            GraphicsDevice device = BokuGame.bokuGame.GraphicsDevice;

            /*
            Matrix world = Matrix.CreateConstrainedBillboard(
                actor.WorldHealthBarOffset,
                camera.From,
                localUp,
                camera.ViewDir,
                null);
            */

            Matrix world = Matrix.CreateBillboard(
                actor.WorldHealthBarOffset,
                camera.ActualFrom,
                camera.ViewUp,
                camera.ViewDir);

            Matrix worldViewProj = world * camera.ViewMatrix * camera.ProjectionMatrix;

            float lifePct = (float)actor.HitPoints / (float)actor.MaxHitPoints;

            Parameter(EffectParams.WorldViewProj).SetValue(worldViewProj);
            Parameter(EffectParams.LifeTint).SetValue(GetLifeTint(lifePct));
            Parameter(EffectParams.LifePct).SetValue(lifePct);

            device.SetVertexBuffer(vertexBuf);
            device.Indices = UI2D.Shared.QuadIndexBuff;

            Effect.CurrentTechnique.Passes[0].Apply();

            device.DrawIndexedPrimitives(PrimitiveType.TriangleList, 0, 0, 4, 0, 2);
        }

        private static Vector4 GetLifeTint(float lifePct)
        {
            float oneMinusLife = 1f - lifePct;

            float r = MathHelper.Clamp(2f * oneMinusLife, 0f, 1f);
            float g = MathHelper.Clamp(2f * lifePct, 0f, 1f);

            return new Vector4(r, g, 0f, 1f);
        }

        private static EffectParameter Parameter(EffectParams param)
        {
            return effectCache.Parameter((int)param);
        }

        internal static void LoadContent(bool immediate)
        {
            if (backTexture == null)
            {
                backTexture = BokuGame.Load<Texture2D>(BokuGame.Settings.MediaPath + @"Textures\HealthBarBack");
            }

            if (lifeTexture == null)
            {
                lifeTexture = BokuGame.Load<Texture2D>(BokuGame.Settings.MediaPath + @"Textures\HealthBarLife");
            }

            if (effect == null)
            {
                effect = BokuGame.Load<Effect>(BokuGame.Settings.MediaPath + @"Shaders\HealthBar");
                ShaderGlobals.RegisterEffect("HealthBar", effect);
            }
        }

        public static void InitDeviceResources(GraphicsDevice device)
        {
            effectCache = new EffectCache<EffectParams>();
            effectCache.Load(Effect);

            // Scale background frame to be of length one on the long side (width).
            kBackSize = new Vector2(BackTexture.Width, BackTexture.Height) / (float)BackTexture.Width;

            // Scale life bar image to fit inside the background frame (original images are of equal size).
            kLifeSize = new Vector2(kBackSize.X * kLifeBarScalarX, kBackSize.Y * kLifeBarScalarY);

            vertexBuf = new VertexBuffer(device, typeof(VertexPositionTexture), 4, BufferUsage.WriteOnly);
            VertexPositionTexture[] verts = new VertexPositionTexture[4] {
                new VertexPositionTexture(new Vector3(0, 0, 0), new Vector2(0, 0)),
                new VertexPositionTexture(new Vector3(0, 0, 0), new Vector2(1, 0)),
                new VertexPositionTexture(new Vector3(0, 0, 0), new Vector2(1, 1)),
                new VertexPositionTexture(new Vector3(0, 0, 0), new Vector2(0, 1)),
            };
            vertexBuf.SetData<VertexPositionTexture>(verts);
        }

        public static void UnloadContent()
        {
            BokuGame.Release(ref vertexBuf);
            BokuGame.Release(ref effect);
            BokuGame.Release(ref lifeTexture);
            BokuGame.Release(ref backTexture);
        }

        /// <summary>
        /// Recreate render targets.
        /// </summary>
        public static void DeviceReset(GraphicsDevice device)
        {
        }

        #endregion
    }
}
