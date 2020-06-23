

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;

using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Audio;
using Microsoft.Xna.Framework.Graphics;

using Boku.Base;
using Boku.Common;
using Boku.Common.Xml;

namespace Boku.Audio
{
    public partial class Audio
    {
        #region Members
        readonly private bool enabled;
        
        private AudioEngine engine = null;

        private WaveBank inMemoryWavebank = null;
        private WaveBank inMemoryWavebank2 = null;
        private WaveBank streamingWavebank = null;
        private SoundBank soundbank = null;

        private WaveBank startupWavebank = null;
        private SoundBank startupSoundbank = null;

        private AudioListener listener = null;

        private static AudioEmitter emitter = null;

        private List<AudioCue> activeCues = new List<AudioCue>();
        private List<AudioCue> spareCues = new List<AudioCue>();

        private double lastFrameTimeSeconds;
        //        PerfTimer updateTimer = new PerfTimer(">>> Update Audio ");

        #endregion Members

        #region Accessors

        public SoundBank SoundBank
        {
            get { return soundbank; }
        }
        public SoundBank StartupSoundBank
        {
            get { return startupSoundbank; }
        }
        public static AudioEmitter Emitter
        {
            get { return emitter; }
        }
        public bool Enabled
        {
            get { return enabled; }
        }
        public List<AudioCue> ActiveCues
        {
            get { return activeCues; }
        }
        #endregion Accessors

        #region Public
        public Audio(bool enabled)
        {
            if (enabled)
            {
                try
                {
#if NETFX_CORE
                    engine = new AudioEngine(@"Content\Audio\KoduMG.xgs");

                    listener = new AudioListener();
                    emitter = new AudioEmitter();

                    startupWavebank = new WaveBank(engine, @"Content\Audio\Startup Wave Bank.xwb");
                    startupSoundbank = new SoundBank(engine, @"Content\Audio\Startup Sound Bank.xsb");
                    inMemoryWavebank = new WaveBank(engine, @"Content\Audio\In Memory Wave Bank.xwb");
                    inMemoryWavebank2 = new WaveBank(engine, @"Content\Audio\In Memory Wave Bank2.xwb");
                    streamingWavebank = new WaveBank(engine, @"Content\Audio\Streaming Wave Bank.xwb");
                    soundbank = new SoundBank(engine, @"Content\Audio\Sound Bank.xsb");
#else
                    engine = new AudioEngine(Path.Combine(Storage4.TitleLocation, @"Content\Audio\Kodu.xgs"));

                    listener = new AudioListener();
                    emitter = new AudioEmitter();

                    // Load these early and synchronously so that the title screen
                    // can play the startup sound while we asynchronously load
                    // additional assets, including the "real" sound and wave banks.
                    startupWavebank = new WaveBank(engine, Path.Combine(Storage4.TitleLocation, @"Content\Audio\Startup Wave Bank.xwb"));
                    startupSoundbank = new SoundBank(engine, Path.Combine(Storage4.TitleLocation, @"Content\Audio\Startup Sound Bank.xsb"));

                    // Load these later.
                    //inMemoryWavebank = new WaveBank(engine, @"Content\Audio\In Memory Wave Bank.xwb");
                    //inMemoryWavebank2 = new WaveBank(engine, @"Content\Audio\In Memory Wave Bank2.xwb");
                    //streamingWavebank = new WaveBank(engine, @"Content\Audio\Streaming Wave Bank.xwb");
                    //soundbank = new SoundBank(engine, @"Content\Audio\Sound Bank.xsb");
#endif

                    activeCues = new List<AudioCue>();
                    spareCues = new List<AudioCue>();

                    SetVolume("UI", XmlOptionsData.UIVolume);
                    SetVolume("Foley", XmlOptionsData.FoleyVolume);
                    SetVolume("Music", XmlOptionsData.MusicVolume);

                }
                catch(Exception e)
                {
                    if (e == null)
                    {
                    }
                    // Failed to create AudioEngine, so run with audio disabled.
                    // Note that this will modify audio-based gameplay since the
                    // hearable database is driven by what audio cues are active.
                    engine = null;
                }
            }
            this.enabled = engine != null;
        }

        public void LoadContent(bool immediate)
        {
            if (engine != null)
            {
                if (inMemoryWavebank == null)
                {
                    try
                    {
#if NETFX_CORE
                        inMemoryWavebank = new WaveBank(engine, @"Content\Audio\in memory wave bank.xwb");
#else
                        inMemoryWavebank = new WaveBank(engine, Path.Combine(Storage4.TitleLocation, @"Content\Audio\in memory wave bank.xwb"));
#endif
                    }
                    catch (Exception e)
                    {
                        if (e == null)
                        {
                        }
                    }
                }
                if (inMemoryWavebank2 == null)
                {
                    try
                    {
#if NETFX_CORE
                        inMemoryWavebank2 = new WaveBank(engine, @"Content\Audio\in memory wave bank2.xwb");
#else
                        inMemoryWavebank2 = new WaveBank(engine, Path.Combine(Storage4.TitleLocation, @"Content\Audio\in memory wave bank2.xwb"));
#endif
                    }
                    catch (Exception e)
                    {
                        if (e == null)
                        {
                        }
                    }
                }
                if (streamingWavebank == null)
                {
                    try
                    {
#if NETFX_CORE
                        streamingWavebank = new WaveBank(engine, @"Content\Audio\streaming wave bank.xwb", 0, 16);
#else
                        streamingWavebank = new WaveBank(engine, Path.Combine(Storage4.TitleLocation, @"Content\Audio\streaming wave bank.xwb"), 0, 16);
#endif
                    }
                    catch (Exception e)
                    {
                        if (e == null)
                        {
                        }
                    }
                }

                if (soundbank == null)
                {
                    try
                    {
#if NETFX_CORE
                        soundbank = new SoundBank(engine, @"Content\Audio\sound bank.xsb");
#else
                        soundbank = new SoundBank(engine, Path.Combine(Storage4.TitleLocation, @"Content\Audio\sound bank.xsb"));
#endif
                    }
                    catch (Exception e)
                    {
                        if (e == null)
                        {
                        }
                    }
                }

                // Needed to init the streaming wavebank.
                engine.Update();
            }
        }

        public void InitDeviceResources(GraphicsDevice device)
        {
        }

        public void UnloadContent()
        {
        }

        public void DeviceReset(GraphicsDevice device)
        {
        }

        public void PlayStartupSound()
        {
            if (enabled && startupSoundbank != null)
            {
                Cue cue = startupSoundbank.GetCue("Boku Enter");
                cue.Play();
            }
        }


        /// <summary>
        /// Pass an update to the underlying audio engine, as well as
        /// cull dead sounds from our active list. Finally, give active
        /// cues a chance to apply 3d effects.
        /// </summary>
        public void Update()
        {
            //            this.updateTimer.Start();

            // Limit audio updates to 60 fps
            //if (Time.GameTimeTotalSeconds > lastFrameTimeSeconds &&
            //    Time.GameTimeTotalSeconds - lastFrameTimeSeconds < 1 / 60.0f)
            //    return;

            lastFrameTimeSeconds = Time.GameTimeTotalSeconds;

            if (Enabled)
            {
                engine.Update();

                if (InGame.inGame != null)
                {
                    // Update 3D audio listener values from camera state
                    listener.Position = InGame.inGame.Camera.ActualFrom;
                    listener.Up = InGame.inGame.Camera.ViewUp;
                    listener.Forward = InGame.inGame.Camera.ViewDir;
                    listener.Velocity = Vector3.Zero; // Camera doesn't expose a "Velocity" property
                }
            }

            // Loop through active audio cues, updating 3D values and recycling completed cues
            for (int cueIndex = activeCues.Count - 1; cueIndex >= 0; --cueIndex)
            {
                AudioCue cue = activeCues[cueIndex];

                if (cue.VirtualDone())
                {
                    cue.DetachSound();
                }

                // If cue is done playing, recycle it. Because we create
                // and destroy AudioCue objects frequently, this helps avoid
                // unnecessary garbage collector activity.
                if (cue.IsComplete)
                {
                    cue.Reset();
                    spareCues.Add(cue);
                    activeCues.RemoveAt(cueIndex);
                    continue;
                }

                // Update cue's 3D audio values
                if (Enabled)
                {
                    cue.Apply3D(listener);
                }
            }
            //            this.updateTimer.Stop();
        }

        public void AddCueToActiveList(AudioCue audioCue)
        {
            if (!activeCues.Contains(audioCue))
            {
                activeCues.Add(audioCue);
            }
        }   // end of AddCueToActiveList()

        /// <summary>
        /// Kill the Looping categories
        /// </summary>
        public void StopGameMusic()
        {
            if (engine != null)
            {
#if NETFX_CORE
                engine.GetCategory("Music").Stop();
                engine.GetCategory("EnvTemp").Stop();
#else
                engine.GetCategory("Music").Stop(AudioStopOptions.Immediate);
                engine.GetCategory("EnvTemp").Stop(AudioStopOptions.Immediate);
#endif
            }
        }

        public void StopAllAudio()
        {
            if (engine != null)
            {
                StopGameMusic();
#if NETFX_CORE
                engine.GetCategory("Foley").Stop();
#else
                engine.GetCategory("Foley").Stop(AudioStopOptions.Immediate);
#endif
            }
        }
        /// <summary>
        /// Pause game audio, but don't stop, because game things may be holding on to their cues.
        /// </summary>
        public void PauseGameAudio()
        {
            if (engine != null)
            {
                engine.GetCategory("Music").Pause();
                engine.GetCategory("Foley").Pause();
                engine.GetCategory("EnvTemp").Pause();
            }
        }

        /// <summary>
        /// Resume paused audio
        /// </summary>
        public void ResumeGameAudio()
        {
            if (engine != null)
            {
                engine.GetCategory("Music").Resume();
                engine.GetCategory("Foley").Resume();
                engine.GetCategory("EnvTemp").Resume();
            }
        }

        /// <summary>
        /// Set the volume for the given category. volume is [0..1].
        /// </summary>
        /// <param name="category"></param>
        /// <param name="volume"></param>
        public void SetVolume(string category, float volume)
        {
            if (engine != null)
            {
                engine.GetCategory(category).SetVolume(volume);
                if (category == "Foley")
                {
                    engine.GetCategory("EnvTemp").SetVolume(volume);
                }
            }
        }

        /// <summary>
        /// Full version of GetCue allows the overriding of whether it is spatialized.
        /// </summary>
        /// <param name="name"></param>
        /// <param name="thing"></param>
        /// <param name="spatial"></param>
        /// <returns></returns>
        public AudioCue GetCue(string name, GameThing thing, bool? spatial)
        {
            AudioCue audioCue = null;

            if (spareCues.Count > 0)
            {
                // Reuse a spare AudioCue instance
                audioCue = spareCues[0];
                spareCues.RemoveAt(0);
            }
            else
            {
                // No spare AudioCue instances available, so create a new one
                audioCue = new AudioCue();
            }

            // Initialize the sound cue
            if (Enabled)
            {
                try
                {
                    Cue cue = SoundBank.GetCue(name);
                    audioCue.Set(cue, thing);

                    if (spatial != null)
                        audioCue.Spatial = spatial.Value;

                    // Apply initial 3D audio values
                    audioCue.Apply3D(listener);
                }
                catch
                {
                    audioCue.Set(null, thing);
                }
            }
            else
            {
                audioCue.Set(null, thing);
            }

            // Cue is not added to activeCues list until played.

            return audioCue;
        }
        /// <summary>
        /// Create a new cue. Thing is optional, but necessary for applying 3d effects.
        /// </summary>
        /// <param name="name"></param>
        /// <param name="thing"></param>
        /// <returns></returns>
        public AudioCue GetCue(string name, GameThing thing)
        {
            return GetCue(name, thing, null);
        }

        /// <summary>
        /// Version of GetCue spatialized with a fixed position.
        /// </summary>
        /// <param name="name"></param>
        /// <param name="position"></param>
        /// <returns></returns>
        public AudioCue GetCue(string name, Vector3 position)
        {
            AudioCue audioCue = null;

            if (spareCues.Count > 0)
            {
                // Reuse a spare AudioCue instance
                audioCue = spareCues[0];
                spareCues.RemoveAt(0);
            }
            else
            {
                // No spare AudioCue instances available, so create a new one
                audioCue = new AudioCue();
            }

            // Initialize the sound cue
            if (Enabled)
            {
                Cue cue = SoundBank.GetCue(name);
                audioCue.Set(cue, position);

                // Apply initial 3D audio values
                audioCue.Apply3D(listener);
            }
            else
            {
                audioCue.Set(null, position);
            }
            // Cue is not added to activeCues list until played.

            return audioCue;
        }

        /// <summary>
        /// Create a new non-spatialized (no 3d) cue.
        /// </summary>
        /// <param name="name"></param>
        /// <returns></returns>
        public AudioCue GetCue(string name)
        {
            return GetCue(name, null);
        }

        #endregion Public
    }
}
