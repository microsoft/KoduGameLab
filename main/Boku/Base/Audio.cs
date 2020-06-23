using System;
using System.Collections.Generic;

using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Audio;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using Microsoft.Xna.Framework.Storage;
using Boku.Common;

namespace Boku.Base
{
    public class AudioCue
    {
        public delegate void OnCompleteDelegate(AudioCue cue);
        public OnCompleteDelegate OnComplete;

        private static AudioEmitter emitter = new AudioEmitter();

        private bool played;
        private GameThing thing;
        private Cue cue;

        public string Name
        {
            get { return cue.Name; }
        }

        public bool IsPlaying
        {
            get { return cue.IsPlaying; }
        }

        public bool IsComplete
        {
            get { return played && cue.IsStopped; }
        }

        public void Set(Cue cue, GameThing thing)
        {
            played = false;
            this.thing = thing;
            this.cue = cue;
        }

        public void Reset()
        {
            if (OnComplete != null)
                OnComplete(this);

            OnComplete = null;
            played = false;
            thing = null;
            cue = null;
        }

        public void Play()
        {
            cue.Play();
            played = true;
        }

        public void Stop(AudioStopOptions options)
        {
            cue.Stop(options);
        }

        public void Apply3D(AudioListener listener)
        {
            if (thing != null)
            {
                // GameThing.Movement doesn't expose an "Up" property, so just use Camera.Up
                emitter.Up = InGame.inGame.Camera.Up;
                emitter.Position = thing.Movement.Position;
                emitter.Velocity = thing.Movement.Velocity;
                emitter.Forward = thing.Movement.Facing;
                // Apply new 3D values to the audio cue
                cue.Apply3D(listener, emitter);
            }
        }
    }

    class Audio
    {
        private AudioEngine engine;

        private WaveBank wavebank;
        private SoundBank soundbank;

        private List<AudioCue> activeCues;
        private List<AudioCue> spareCues;
        private AudioListener listener;

        double lastFrameTimeSeconds;

        private SoundBank SoundBank
        {
            get { return soundbank; }
        }
  
        public Audio()
        {
            engine = new AudioEngine(@"Content\Audio\boku temp.xgs");
            wavebank = new WaveBank(engine, @"Content\Audio\wave bank.xwb");
            soundbank = new SoundBank(engine, @"Content\Audio\sound bank.xsb");

            activeCues = new List<AudioCue>();
            spareCues = new List<AudioCue>();

            listener = new AudioListener();
        }

//        PerfTimer updateTimer = new PerfTimer(">>> Update Audio ");

        public void Update()
        {
//            this.updateTimer.Start();

            // Limit audio updates to 60 fps
            if (Time.GameTimeTotalSeconds > lastFrameTimeSeconds &&
                Time.GameTimeTotalSeconds - lastFrameTimeSeconds < 1 / 60.0f)
                return;

            lastFrameTimeSeconds = Time.GameTimeTotalSeconds;

            engine.Update();

            // Update 3D audio listener values from camera state
            listener.Position = InGame.inGame.Camera.From;
            listener.Up = InGame.inGame.Camera.Up;
            listener.Forward = InGame.inGame.Camera.At;
            listener.Velocity = Vector3.Zero; // Camera doesn't expose a "Velocity" property

            // Loop through active audio cues, updating 3D values and recycling completed cues
            for (int cueIndex = 0; cueIndex < activeCues.Count; ++cueIndex)
            {
                AudioCue cue = activeCues[cueIndex];

                // If cue is done playing, recycle it. Because we create
                // and destroy AudioCue objects frequently, this helps avoid
                // unnecessary garbage collector activity.
                if (cue.IsComplete)
                {
                    cue.Reset();
                    spareCues.Add(cue);
                    activeCues.RemoveAt(cueIndex);
                    --cueIndex;
                    continue;
                }

                // Update cue's 3D audio values
                cue.Apply3D(listener);
            }

//            this.updateTimer.Stop();
        }

        /// <summary>
        /// Kill the Looping categories
        /// </summary>
        public void StopGameMusic()
        {
            engine.GetCategory("Music").Stop(AudioStopOptions.Immediate);
            engine.GetCategory("EnvTemp").Stop(AudioStopOptions.Immediate);
        }

        /// <summary>
        /// Pause game audio, but don't stop, because game things may be holding on to their cues.
        /// </summary>
        public void PauseGameAudio()
        {
            engine.GetCategory("Music").Pause();
            engine.GetCategory("Foley").Pause();
            engine.GetCategory("EnvTemp").Pause();
        }

        /// <summary>
        /// Resume paused audio
        /// </summary>
        public void ResumeGameAudio()
        {
            engine.GetCategory("Music").Resume();
            engine.GetCategory("Foley").Resume();
            engine.GetCategory("EnvTemp").Resume();
        }

        public void SetVolume(string category, float volume)
        {
            engine.GetCategory(category).SetVolume(volume);
            if (category == "Foley")
            {
                engine.GetCategory("EnvTemp").SetVolume(volume);
            }
        }

        public AudioCue GetCue(string name, GameThing thing)
        {
            AudioCue cue;

            if (spareCues.Count > 0)
            {
                // Reuse a spare AudioCue instance
                cue = spareCues[0];
                spareCues.RemoveAt(0);
            }
            else
            {
                // No spare AudioCue instances available, so create a new one
                cue = new AudioCue();
            }

            /// Ugly hack to disable 3d for non-positional sounds.
            /// Later we'll know whether a given sound is non-positional by examining its tag set.
            if (name.StartsWith("Mystery") || name.StartsWith("Driving") || name.StartsWith("Dramatic"))
                thing = null;

            // Initialize the sound cue
            cue.Set(SoundBank.GetCue(name), thing);

            // Apply initial 3D audio values
            cue.Apply3D(listener);

            // Keep track of the audio cue until it completes
            activeCues.Add(cue);

            return cue;
        }

        public AudioCue GetCue(string name)
        {
            return GetCue(name, null);
        }
    }
}
