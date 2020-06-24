
using System;
using System.Collections.Generic;
using System.Diagnostics;

using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Audio;

using KoiX;

using Boku.Base;
using Boku.Common;

namespace Boku.Audio
{
    public class AudioCue
    {
        public delegate void OnCompleteDelegate(AudioCue cue);
        public OnCompleteDelegate OnComplete;

        #region Members

        private bool played;
        private GameThing thing = null;     // If not null, this is the thing emitting the sound.
        private Vector3 position;           // If spatial == true and thing == null, use this position as the source of the sound.
        private Cue cue;

        private double startTime = 0.0;

        private Sound sound;
        private bool spatial = true;
        #endregion Members

        #region Accessors
        /// <summary>
        /// The name of the sound we play.
        /// </summary>
        public string Name
        {
            get 
            {
                return cue != null ? cue.Name : "";
            }
        }

        /// <summary>
        /// Whether we are currently playing.
        /// </summary>
        public bool IsPlaying
        {
            get 
            {
                return cue != null ? cue.IsPlaying : false;
            }
        }

        /// <summary>
        /// Whether we have played to completion.
        /// </summary>
        public bool IsComplete
        {
            get 
            {
                return cue != null ? played && cue.IsStopped : played;
            }
        }

        /// <summary>
        /// The time we started playing in WallClock seconds.
        /// </summary>
        public double StartTime
        {
            get { return startTime; }
        }
        /// <summary>
        /// Whether we are spatialized. Requires a GameThing to be true.
        /// </summary>
        public bool Spatial
        {
            get { return spatial; }
            internal set
            {
                if (thing != null)
                {
                    spatial = value;
                }
            }
        }

        /// <summary>
        /// An optional taxonomy Sound that spawned us.
        /// </summary>
        internal Sound Sound
        {
            get { return sound; }
            set { sound = value; }
        }
        /// <summary>
        /// The sound emitter at our center (if spatialized).
        /// </summary>
        internal GameThing Emitter
        {
            get { return thing; }
            set 
            { 
                thing = value;
                if (thing != null)
                {
                    Spatial = true;
                }
            }
        }

        public bool Played
        {
            get { return played; }
            set { played = value; }
        }

        #endregion Accessors

        #region Public

        
        /// <summary>
        /// Set up with a sound and an optional thing to follow.
        /// Doesn't start playing, that requires a separate call.
        /// </summary>
        /// <param name="cue"></param>
        /// <param name="thing"></param>
        public void Set(Cue cue, GameThing thing)
        {
            played = false;
            this.thing = thing;
            this.spatial = (thing != null);
            DeviceResetX.Release(ref this.cue);
            this.cue = cue;
        }

        /// <summary>
        /// Set up with a spatialized sound with a fixed position.
        /// Doesn't start playing, that requires a separate call.
        /// </summary>
        /// <param name="cue"></param>
        /// <param name="thing"></param>
        public void Set(Cue cue, Vector3 position)
        {
            played = false;
            this.thing = null;
            this.spatial = true;
            this.position = position;
            DeviceResetX.Release(ref this.cue);
            this.cue = cue;
        }

        /// <summary>
        /// Tear down totally.
        /// </summary>
        public void Reset()
        {
            if (OnComplete != null)
                OnComplete(this);

            DetachSound();

            OnComplete = null;
            thing = null;
            spatial = false;
            DeviceResetX.Release(ref cue);
            cue = null;
        }

        /// <summary>
        /// Start playing.
        /// </summary>
        public void Play()
        {
            if (cue != null && !cue.IsPlaying)
            {
                cue.Play();
            }

            played = true;
            startTime = Time.WallClockTotalSeconds;
            BokuGame.Audio.AddCueToActiveList(this);
        }
        /// <summary>
        /// Stop playing as authored.
        /// </summary>
        public void Stop()
        {
            if (cue != null)
            {
                cue.Stop(AudioStopOptions.AsAuthored);
            }
        }
        /// <summary>
        /// Stop playing immediately.
        /// </summary>
        public void StopImmediate()
        {
            if (cue != null)
            {
                cue.Stop(AudioStopOptions.Immediate);
            }
        }

        /// <summary>
        /// Set the current volume for this sound
        /// </summary>
        /// <param name="vol"></param>
        public void SetVolume(float vol)
        {
            if (cue != null)
            {
                cue.SetVariable("Volume", vol);
            }
        }

        /// <summary>
        /// Apply 3d effects if spatialized.
        /// </summary>
        /// <param name="listener"></param>
        public void Apply3D(AudioListener listener)
        {
            if (Spatial)
            {
                if (cue != null)
                {
                    AudioEmitter emitter = Audio.Emitter;
                    if (thing != null)
                    {
                        emitter.Up = thing.Movement.LocalMatrix.Backward;   // Actually up.  Yeah, I know...
                        emitter.Position = thing.Movement.Position;
                        emitter.Velocity = thing.Movement.Velocity;
                        emitter.Forward = thing.Movement.Facing;
                    }
                    else
                    {
                        emitter.Position = position;
                        emitter.Up = Vector3.UnitZ;
                        emitter.Velocity = Vector3.Zero;
                        emitter.Forward = Vector3.UnitX;
                    }
                    // Apply new 3D values to the audio cue
                    cue.Apply3D(listener, emitter);
                }
            }
        }

        #endregion Public

        #region Internal
        /// <summary>
        /// True when our sound is ready to repeat.
        /// </summary>
        /// <returns></returns>
        internal bool VirtualDone()
        {
            return (sound != null) && sound.Done(this);
        }
        /// <summary>
        /// Break connection with any Sound object we're associated with.
        /// </summary>
        internal void DetachSound()
        {
            if (Sound != null)
            {
                Sound.Detach(this);
            }
        }
        #endregion Internal
    }
}
