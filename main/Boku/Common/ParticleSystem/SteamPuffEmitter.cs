
using System;
using System.Collections;
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
    /// A dead simple emitter which just acts as a timer
    /// and calls CreateSteamPuff every n seconds.
    /// </summary>
    public class SteamPuffEmitter : BaseEmitter
    {
        private float timeBetweenPuffs = 1.0f;
        private double lastPuffTime = 0.0;

        private float density = 1.0f;

        #region Accessors
        public float Density
        {
            get { return density; }
            set { density = value; }
        }
        #endregion

        public SteamPuffEmitter(float timeBetweenPuffs)
            : base(InGame.inGame.ParticleSystemManager)
        {
            this.timeBetweenPuffs = timeBetweenPuffs;

            // Randomize the start time.
            lastPuffTime = Time.GameTimeTotalSeconds - BokuGame.bokuGame.rnd.NextDouble() * timeBetweenPuffs;
        }

        public override void Update()
        {
            if (Dying)
            {
                // SteamPuffs don't own their own particles (they're all shared) so we don't
                // need to wait until they're all gone.  We can immediately remove ourselves.
                RemoveFromManager();
            }
            else
            {
                if (Time.GameTimeTotalSeconds > lastPuffTime + timeBetweenPuffs)
                {
                    ExplosionManager.CreateSteamPuff(position, density, scale);
                    lastPuffTime = Time.GameTimeTotalSeconds;
                }
            }

        }   // end of Update()

    }   // end of class SteamPuffEmitter

}   // end of namespace Boku.Common
