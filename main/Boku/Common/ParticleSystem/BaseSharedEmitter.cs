
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

namespace Boku.Common.ParticleSystem
{
    /// <summary>
    /// Base class for shared emitters.  Shared emitters don't have any position, they just allow
    /// particle sources to add particles to themselves and throw away dead particles.
    /// </summary>
    public abstract class BaseSharedEmitter : ArbitraryComparable, INeedsDeviceReset
    {
        #region Members

        protected static int kMaxParticles = 5000;          // Max number of particles for any shared emitter.
        protected static short[] localIndices = null;       // Shared among all.
        protected static IndexBuffer ibuf = null;


        protected ParticleSystemManager manager = null;     // The manager that holds this emitter.
        private bool inManager = false;

        protected float[] particleDeathTimeList = null;     // An array containing the death times for all the particles.  The index for 
                                                            // each particle corresponds to its position in the vertex buffer.  This way
                                                            // we can determine which particles to render without having to touch the 
                                                            // data in the VB.
        protected int maxParticles = 0;                     // Maximum particles in this emitter.
        protected int firstParticle = 0;                    // Index of first active particle.
        protected int numActiveParticles = 0;               // Number of active particles.

        protected bool active = false;

        protected BaseEmitter.Use usage = BaseEmitter.Use.Regular;  // When this emitter gets rendered. Can be or'd

        #endregion

        #region Accessors

        /// <summary>
        /// The emitters usage, ie how the particles are to be rendered.
        /// </summary>
        public BaseEmitter.Use Usage
        {
            get { return usage; }
            set { usage = value; }
        }
        
        /// <summary>
        /// True if the input usage matches any of this emitter's usages.
        /// </summary>
        /// <param name="usage"></param>
        /// <returns></returns>
        public bool HasUsage(BaseEmitter.Use usage)
        {
            return (this.usage & usage) != 0;
        }
        
        /// <summary>
        /// Only the manager should be accessing this as the system
        /// comes and goes from it's ownership.
        /// </summary>
        public bool InManager
        {
            get { return inManager; }
            set { inManager = value; }
        }
        #endregion

        #region Public

        public BaseSharedEmitter(ParticleSystemManager manager)
        {
            this.manager = manager;

            // Init localIndices if needed.
            if (localIndices == null)
            {
                localIndices = new short[kMaxParticles * 6];    // 1 particle == 2 triangles == 6 indices.
                
                int v = 0;
                int i = 0;
                for (int p = 0; p < kMaxParticles; p++)
                {
                    localIndices[i++] = (short)(v + 0);
                    localIndices[i++] = (short)(v + 1);
                    localIndices[i++] = (short)(v + 2);
                    localIndices[i++] = (short)(v + 0);
                    localIndices[i++] = (short)(v + 2);
                    localIndices[i++] = (short)(v + 3);
                    v += 4;
                }
            }
        }   // end of c'tor

        public virtual void FlushAllParticles()
        {
            numActiveParticles = 0;
            firstParticle = 0;
        }   // end of BaseSharedEmitter FlushAllParticles()

        public virtual void Update()
        {
            // Never trust a particle over 40.
            // We need to kill off any particles that have exceeded their lifetime.  But, we
            // want to keep the active particles grouped contiguously.  So, starting at the 
            // beginning of the list, keep moving the firstParticle index forward until we 
            // find a live particle.
            float time = (float)Time.GameTimeTotalSeconds;

            int index = firstParticle;
            int active = numActiveParticles;
            for (int i = 0; i < active; i++)
            {
                if (particleDeathTimeList[index] <= time)
                {
                    // Kill one off.
                    firstParticle = (firstParticle + 1) % maxParticles;
                    --numActiveParticles;
                }
                else
                {
                    // Found the first live one.  Bail.
                    break;
                }

                index = (index + 1) % maxParticles;
            }

        }   // end of BaseSharedEmitter Update()

        /// <summary>
        /// Sets up all the common stuff needed for rendering a batch of these emitters.  
        /// This includes setting the technique and any parameters that don't change from
        /// one batch to the next.
        /// </summary>
        public virtual void PreRender(Camera camera)
        {
        }   // end of BaseSharedEmitter PreRender()

        /// <summary>
        /// Any post-render stuff that needs to be restored after rendering a batch.
        /// </summary>
        public virtual void PostRender()
        {
        }   // end of BaseSharedEmitter PostRender()

        public virtual void Render(Camera camera)
        {
        }   // end of BaseSharedEmitter Render()

        #endregion

        #region Internal

        protected virtual void Render(Effect effect)
        {
            if (numActiveParticles > 0)
            {
                GraphicsDevice device = BokuGame.bokuGame.GraphicsDevice;

                int startIndex = firstParticle;
                int endIndex = (startIndex + numActiveParticles) % maxParticles;

                // Render all passes.
                for (int i = 0; i < effect.CurrentTechnique.Passes.Count; i++)
                {
                    EffectPass pass = effect.CurrentTechnique.Passes[i];
                    pass.Apply();
                    if (startIndex < endIndex)
                    {
                        // The active range of particles is contiguous.
#if NETFX_CORE_OLD
                        // MG only supports starting from 0.
                        int numParticles = endIndex;
                        device.DrawIndexedPrimitives(PrimitiveType.TriangleList, 0, 0, numParticles * 4, 0, numParticles * 2);
#else
                        int numParticles = numActiveParticles;
                        device.DrawIndexedPrimitives(PrimitiveType.TriangleList, 0, startIndex * 4, numParticles * 4, startIndex * 6, numParticles * 2);
#endif
                    }
                    else
                    {
                        // The active range of particles wraps from the end of the array around to the beginning.
#if NETFX_CORE_OLD
                        // MG only supports starting from 0.
                        int numParticles = endIndex;
                        device.DrawIndexedPrimitives(PrimitiveType.TriangleList, 0, 0, maxParticles * 4, 0, maxParticles * 2);
#else
                        int numParticles = maxParticles - startIndex;
                        device.DrawIndexedPrimitives(PrimitiveType.TriangleList, 0, startIndex * 4, numParticles * 4, startIndex * 6, numParticles * 2);
                        numParticles = numActiveParticles - numParticles;
                        if (numParticles > 0)
                        {
                            device.DrawIndexedPrimitives(PrimitiveType.TriangleList, 0, 0, numParticles * 4, 0, numParticles * 2);
                        }
#endif
                    }
                }

            }
        }

        public virtual void LoadContent(bool immediate)
        {
        }   // end of BaseSharedEmitter LoadContent()

        public virtual void InitDeviceResources(GraphicsDevice device)
        {
            if (ibuf == null)
            {
                ibuf = new IndexBuffer(device, IndexElementSize.SixteenBits, kMaxParticles * 6, BufferUsage.WriteOnly);
                ibuf.SetData<short>(localIndices);
            }
        }

        public virtual void UnloadContent()
        {
            BokuGame.Release(ref ibuf);
        }   // end of BaseSharedEmitter UnloadContent()

        public virtual void DeviceReset(GraphicsDevice device)
        {
        }

        #endregion

    }   // end of class BaseSharedEmitter

}   // end of namespace Boku.Common.ParticleSystem
