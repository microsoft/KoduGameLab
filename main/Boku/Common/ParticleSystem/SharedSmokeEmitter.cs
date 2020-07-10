// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.


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

using KoiX;

using Boku.Base;
using Boku.Fx;

namespace Boku.Common.ParticleSystem
{
    public class SharedSmokeEmitter : BaseSharedEmitter
    {
        #region Rendering elements
        public struct SmokeParticle
        {
            public Vector3 position;
            public Vector3 velocity;
            public Vector3 acceleration;
            public float startRadius;   // Radius at start of life. 
            public float endRadius;     // Radius at end of life.
            public float lifetime;      // Lifetime in seconds.
            public float rotationRate;  // Radians per second.  Initial rotation is set randomly.
            public Vector3 flash;       // Start time for emissive flash followed by length and tail
            public Vector4 color;       // White.  Alpha is starting alpha.  Goes to 0 by death.
        }

        protected struct SmokeVertex : IVertexType
        {
            public Vector3 position;
            public Vector3 velocity;
            public Vector3 acceleration;
            public Vector2 uv;
            public Vector2 radius;
            public Vector2 times;
            public Vector2 rotation;
            public Vector3 flash;
            public Vector4 color;

            static VertexDeclaration decl = null;
            static private VertexElement[] elements = new VertexElement[]
            {
                new VertexElement(0, VertexElementFormat.Vector3, VertexElementUsage.Position, 0),              // Origin.
                new VertexElement(12, VertexElementFormat.Vector3, VertexElementUsage.TextureCoordinate, 0),    // Velocity.
                new VertexElement(24, VertexElementFormat.Vector3, VertexElementUsage.TextureCoordinate, 1),    // Acceleration.
                new VertexElement(36, VertexElementFormat.Vector2, VertexElementUsage.TextureCoordinate, 2),    // Texture2D UVs.
                new VertexElement(44, VertexElementFormat.Vector2, VertexElementUsage.TextureCoordinate, 3),    // Start/end radius.
                new VertexElement(52, VertexElementFormat.Vector2, VertexElementUsage.TextureCoordinate, 4),    // Birth/death time.
                new VertexElement(60, VertexElementFormat.Vector2, VertexElementUsage.TextureCoordinate, 5),    // Rotation start/rate.
                new VertexElement(68, VertexElementFormat.Vector3, VertexElementUsage.TextureCoordinate, 6),    // Flash scale, offset, tailscale
                new VertexElement(80, VertexElementFormat.Vector4, VertexElementUsage.Color, 0),                // Color.  Alpha is starting alpha, goes to 0 by death.

                // Also need start/end alpha ??? use color alpha as start and always assume 0 alpha for end?
                // Also need starting rotation and rotation rate

                // size == 96
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

        }   // end of class SmokeParticle

        #endregion Rendering elements

        #region Members

        private Effect effect = null;        // TODO (****) Make this private.

        private DynamicVertexBuffer vbuf = null;
        private int stride = 96;
        private SmokeVertex[] localVertices = null;
        private Texture2D texture = null;

        private bool newParticles = false;  // Any new particles this frame?

        #endregion

        #region Accessors
        protected virtual string TextureName
        {
            get { return "Smoke";  }
        }
        protected virtual string TechniqueName
        {
            get { return @"TexturedColorPassNormalAlpha"; }
        }
        /// <summary>
        /// Override this to true to avoid the lighting effecting your color. Like with a dark
        /// light rig you don't want your explosions in black.
        /// </summary>
        public virtual bool IsEmissive
        {
            get { return false; }
        }


        protected Effect Effect
        {
            get { return effect; }
        }
        #endregion

        #region Public

        public SharedSmokeEmitter(ParticleSystemManager manager, int maxParticles)
            : base(manager)
        {
            this.maxParticles = Math.Min(kMaxParticles, maxParticles);
            particleDeathTimeList = new float[this.maxParticles];

            localVertices = new SmokeVertex[this.maxParticles * 4];     // 4 vertices per particle.

            for (int p = 0; p < this.maxParticles; p++)
            {
                // Init the UV coords which never change.
                localVertices[p * 4 + 0].uv = new Vector2(0.0f, 0.0f);
                localVertices[p * 4 + 1].uv = new Vector2(1.0f, 0.0f);
                localVertices[p * 4 + 2].uv = new Vector2(1.0f, 1.0f);
                localVertices[p * 4 + 3].uv = new Vector2(0.0f, 1.0f);
            }

        }   // end of c'tor

        private int firstNewParticleIndex = -1;
        private int lastNewParticleIndex = -1;

        /// <summary>
        /// Adds a new particle to the shared emitter.
        /// </summary>
        /// <returns>true on success, false on failure (max particle count exceeded)</returns>
        public bool AddParticle(ref SmokeParticle p)
        {
            bool result = false;
            if (numActiveParticles < maxParticles)
            {
                int index = (firstParticle + numActiveParticles) % maxParticles;

                particleDeathTimeList[index] = p.lifetime + (float)Time.GameTimeTotalSeconds;

                if (firstNewParticleIndex == -1)
                {
                    firstNewParticleIndex = index;
                }
                lastNewParticleIndex = index;

                float initialRotation = MathHelper.TwoPi * (float)BokuGame.bokuGame.rnd.NextDouble();

                float birth = (float)Time.GameTimeTotalSeconds;
                index *= 4;
                for (int i = 0; i < 4; i++)
                {
                    localVertices[index + i].position = p.position;
                    localVertices[index + i].velocity = p.velocity;
                    localVertices[index + i].acceleration = p.acceleration;
                    localVertices[index + i].radius.X = p.startRadius;
                    localVertices[index + i].radius.Y = p.endRadius;
                    localVertices[index + i].times.X = birth;
                    localVertices[index + i].times.Y = p.lifetime + birth;
                    if (p.flash.Y > 0)
                    {
                        float scale = 1.0f / p.flash.Y;
                        localVertices[index + i].flash.X = scale;
                        localVertices[index + i].flash.Y = -p.flash.X * scale;
                    }
                    else
                    {
                        localVertices[index + i].flash.X = 0;
                        localVertices[index + i].flash.Y = 1.0f;
                    }
                    if (p.flash.Z > 0)
                    {
                        float scale = 1.0f / p.flash.Z;
                        localVertices[index + i].flash.Z = scale;
                    }
                    else
                    {
                        localVertices[index + i].flash.Z = 0;
                    }
                    localVertices[index + i].rotation.X = initialRotation;
                    localVertices[index + i].rotation.Y = p.rotationRate;
                    localVertices[index + i].color = p.color;
                }

                ++numActiveParticles;
                newParticles = true;

                result = true;
            }

            return result;
        }   // end of SharedSmokeEmitter AddParticle()

        public override void Update()
        {
            //Debug.Print(numActiveParticles.ToString() + @" / " + maxParticles.ToString());

            base.Update();

            if(newParticles)
            {                
                // Only update the part of the vertex buffer that has changed.
                if (firstNewParticleIndex <= lastNewParticleIndex)
                {
                    // The new particles are contiguous.
                    int numParticles = lastNewParticleIndex - firstNewParticleIndex + 1;
                    vbuf.SetData<SmokeVertex>(firstNewParticleIndex * 4 * stride, localVertices, firstNewParticleIndex * 4, numParticles * 4, stride, SetDataOptions.NoOverwrite);
                }
                else
                {
                    // The range of new particles wraps so we need two updates.
                    int numParticles = maxParticles - firstNewParticleIndex;
                    vbuf.SetData<SmokeVertex>(firstNewParticleIndex * 4 * stride, localVertices, firstNewParticleIndex * 4, numParticles * 4, stride, SetDataOptions.NoOverwrite);

                    numParticles = lastNewParticleIndex + 1;
                    vbuf.SetData<SmokeVertex>(0, localVertices, 0, numParticles * 4, stride, SetDataOptions.NoOverwrite);
                }

                firstNewParticleIndex = -1;
                
                newParticles = false;
            }
        }   // end of SharedSmokeEmitter Update()

        /// <summary>
        /// Sets up all the common stuff needed for rendering.  This includes
        /// setting the technique and any parameters that don't change from
        /// one batch to the next.
        /// </summary>
        public override void PreRender(Camera camera)
        {
            GraphicsDevice device = KoiLibrary.GraphicsDevice;

            if (InGame.inGame.renderEffects == InGame.RenderEffect.DistortionPass)
            {
                ShaderGlobals.FixExplicitBloom(0.004f);
                effect.CurrentTechnique = effect.Techniques[@"DistortionPass"];
            }
            else
            {
                // Turn off bloom.
                ShaderGlobals.FixExplicitBloom(0.0f);
                effect.CurrentTechnique = effect.Techniques[TechniqueName];
            }

            // Set up common rendering values.
            effect.Parameters["CurrentTime"].SetValue((float)Time.GameTimeTotalSeconds);

            // Set up world matrix.
            Matrix worldMatrix = Matrix.Identity;
            Matrix worldViewProjMatrix = worldMatrix * camera.ViewProjectionMatrix;

            Vector4 diffuseColor = ShaderGlobals.ParticleTint(false);
            effect.Parameters["DiffuseColor"].SetValue(diffuseColor);
            effect.Parameters["DiffuseTexture"].SetValue(texture);
            effect.Parameters["EyeLocation"].SetValue(new Vector4(camera.ActualFrom, 1.0f));
            effect.Parameters["CameraUp"].SetValue(new Vector4(camera.ViewUp, 1.0f));
            effect.Parameters["WorldMatrix"].SetValue(worldMatrix);
            effect.Parameters["WorldViewProjMatrix"].SetValue(worldViewProjMatrix);
            effect.Parameters["ParticleRadius"].SetValue(
                ShaderGlobals.MakeParticleSizeLimit(1.0f, 0.0f, 200.0f));

            // Cloned from DistortFilter.  Another casualty of the removal of gloabl shader values.
            Vector4 filterScroll = new Vector4(0.0f, 0.0f, 0.0f, 0.0f);
            Vector4 filterScale = new Vector4(0.5f, 0.5f, 0.5f, 0.5f);
            float filterStrength = 0.2f;

            effect.Parameters["BumpScroll"].SetValue(filterScroll);
            effect.Parameters["BumpScale"].SetValue(filterScale);
            effect.Parameters["BumpStrength"].SetValue(filterStrength);
            
            effect.Parameters["DepthTexture"].SetValue(InGame.inGame.EffectsRenderTarget);
            //effect.Parameters["Bump"].SetValue(DistortFilter.BumpTexture);
            effect.Parameters["Bump"].SetValue(DistortionManager.Bump);
            effect.Parameters["DOF_FarPlane"].SetValue(100.0f);

            device.Indices = ibuf;

        }   // end of SharedSmokeEmitter PreRender()

        /// <summary>
        /// Any post-render stuff that needs to be restored after rendering a batch.
        /// </summary>
        public override void PostRender()
        {
            // Restore bloom.
            ShaderGlobals.ReleaseExplicitBloom();
        }   // end of SharedSmokeEmitter PostRender()

        public override void Render(Camera camera)
        {
            if (numActiveParticles > 0)
            {
                GraphicsDevice device = KoiLibrary.GraphicsDevice;
                device.SetVertexBuffer(vbuf);

                Render(effect);
            }
        }   // end of SharedSmokeEmitter Render()

        #endregion

        #region Internal

        public override void LoadContent(bool immediate)
        {
            if (effect == null)
            {
                effect = KoiLibrary.LoadEffect(@"Shaders\SharedParticle2D");
            }

            if (texture == null)
            {
                
                texture = KoiLibrary.LoadTexture2D(@"Textures\" + TextureName);
            }

            base.LoadContent(immediate);
        }   // end of SharedSmokeEmitter LoadContent()

        public override void InitDeviceResources(GraphicsDevice device)
        {
            if (vbuf == null)
            {
                vbuf = new DynamicVertexBuffer(device, typeof(SmokeVertex), 4 * maxParticles, BufferUsage.WriteOnly);
            }

            base.InitDeviceResources(device);
        }

        public override void UnloadContent()
        {
            DeviceResetX.Release(ref effect);
            DeviceResetX.Release(ref vbuf);
            DeviceResetX.Release(ref texture);

            firstParticle = 0;
            numActiveParticles = 0;

            base.UnloadContent();
        }   // end of SharedSmokeEmitter UnloadContent()

        #endregion

    }   // end of class SharedSmokeEmitter : BaseSharedEmitter


}   // end of namespace Boku.Common.ParticleSystem
