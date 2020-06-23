
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
    public class BaseEmitter : ArbitraryComparable
    {
        protected ParticleSystemManager manager = null;     // The manager that holds this emitter.
        protected List<object> particleList = null;         // Really should have a base particle type.
        private bool inManager = false;

        public Vector3 position;
        protected Vector3 prevPosition;
        protected bool active = false;
        protected bool emitting = true;         // Are we still emitting particles?
        protected bool dying = false;           // Should we remove ourself from the manager when all our paritcles are dead?
        protected bool persistent = false;      // If true, this emitter should not be removed by the manager's "ClearAllEmitters" function.    
        protected float scale = 1.0f;

        public enum Use
        {
            Never = 0x0,
            Regular = 0x1,
            Distort = 0x2
        };
        protected Use usage = Use.Regular;           // When this emitter gets rendered. Can be or'd


        #region Accessors
        /// <summary>
        /// Where the emitter is.
        /// </summary>
        public Vector3 Position
        {
            get { return position; }
            set { position = value; }
        }
        /// <summary>
        /// Where the emitter was last frame.
        /// </summary>
        public Vector3 PreviousPosition
        {
            get 
            {
                if (prevPosition.X == float.MaxValue)
                    ResetPreviousPosition();
                return prevPosition; 
            }
            set { prevPosition = value; }
        }
        /// <summary>
        /// Is this emitter active?  If not then it does no updates and none of its particles are rendered.
        /// </summary>
        public bool Active
        {
            get { return active; }
            set { active = value; }
        }
        /// <summary>
        /// Are we still emitting particles?  This is generally used in conjunction with Dying for emitters that are
        /// no longer needed.  Setting Emitting to false will stop the creation of new particles.  Setting Dying to
        /// true will let the existing particles live out their lifespan before the emitter kills itself off.
        /// </summary>
        public bool Emitting
        {
            get { return emitting; }
            set { emitting = value; }
        }
        /// <summary>
        /// Should this emitter remove itself from the manager when all its particles have faded away?
        /// This is generally used in conjunction with Emitting for emitters that are no longer needed.
        /// Setting Emitting to false will stop the creation of new particles.  Setting Dying to true will 
        /// let the existing particles live out their lifespan before the emitter kills itself off.
        /// </summary>
        public bool Dying
        {
            get { return dying; }
            set { dying = value; }
        }
        /// <summary>
        /// True if this particle emitter has been added to the 
        /// DistortionManager hence needing removal upon death.
        /// </summary>
        public Use Usage
        {
            get { return usage; }
            set { usage = value; }
        }
        /// <summary>
        /// True if the input usage matches any of this emitter's usages.
        /// </summary>
        /// <param name="usage"></param>
        /// <returns></returns>
        public bool HasUsage(Use usage)
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

        /// <summary>
        /// If true, this emitter should not be removed by the manager's "ClearAllEmitters" function.    
        /// </summary>
        public bool Persistent
        {
            get { return persistent; }
            set { persistent = value; }
        }
        /// <summary>
        /// Multiplicative scale for the particles emitted by this emitter.
        /// Scale should be applied at spawn time, changing the scale should not
        /// affect existing particles.
        /// </summary>
        public float Scale
        {
            get { return scale; }
            set { scale = value; }
        }
        #endregion

        // c'tor
        public BaseEmitter(ParticleSystemManager manager)
        {
            this.manager = manager;
            particleList = new List<object>();

            // Don't add self to manager's list by default.  This will be handled
            // by owning object's Refresh call.
            //manager.AddEmitter(this);

            // Force the prevPosition to be "reset" on first call.
            prevPosition.X = float.MaxValue;
        }   // end of c'tor

        /// <summary>
        /// Add this emitter to the manager's active list of emitters.
        /// </summary>
        public void AddToManager()
        {
            if (!inManager)
            {
                manager.AddEmitter(this);
            }
        }   // end of BaseEmitter AddSelf()

        /// <summary>
        /// Remove this emitter to the manager's active list of emitters.
        /// </summary>
        public virtual void RemoveFromManager()
        {
            if (inManager)
            {
                manager.RemoveEmitter(this);
            }
        }   // end of BaseEmitter RemoveSelf()

        /// <summary>
        /// Update the value of the previous position to
        /// match the current position.
        /// </summary>
        public void ResetPreviousPosition()
        {
            prevPosition = position;
        }

        /// <summary>
        /// Removes any particles from the emitter.
        /// </summary>
        public virtual void FlushAllParticles()
        {
        }   // end of BaseEmitter FlushAllParticles()

        public virtual void Update()
        {
        }   // end of BaseEmitter Update()

        public virtual void Render(Camera camera)
        {
        }   // end of BaseEmitter Render()

        public virtual int ParticleCount()
        {
            return particleList != null ? particleList.Count : 0;
        }

    }   // end of class BaseEmitter

}   // end of namespace Boku.Common
