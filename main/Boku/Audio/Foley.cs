
using System;
using System.Collections.Generic;
using System.Diagnostics;

using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Audio;

using KoiX;

using Boku.Base;
using Boku.Common;
using Boku.SimWorld.Terra;

namespace Boku.Audio
{
    /// <summary>
    /// A manager class that provides a layer between the game code and 
    /// directly playing the audio cues.  The intention is to allow a 
    /// better way to throttle the playing of foley sounds so that they 
    /// don't slam performance.
    /// 
    /// Features:
    /// We can prevent the same sound from being played twice in the same frame.
    /// We can easily control how much of a time gap to wait before allowing another instance of a sound to be played.
    /// We have only 1 place to change if we want to change a sound.  This is especially nice to UI sounds.
    /// </summary>
    public class Foley
    {
        /// <summary>
        /// Enum for the type of sound to be played when
        /// the bot or terrain is collided with.
        /// </summary>
        public enum CollisionSound
        {
            dirt,
            grass,
            rock,
            metalHard,
            metalSoft,
            plasticHard,
            plasticSoft,
            rover,
            unknown
        }

        public class FoleySound
        {
            #region Members

            private string soundName = null;                // The resouce name of the sound we're playing.

            private int frame = -1;                         // The last frame this sound was played.  
                                                            // This allows us to ensure that we don't 
                                                            // trigger the same sound twice per frame.
            private double timeGap = 0.1;                   // Minimum time between instances of this sound.
            private double lastTime = 0.0f;                 // Last time this sound was played.

            private bool singleton = false;                 // Is this sound a singleton, ie a looping UI sound that should
                                                            // only ever has a single instance playing.

            #endregion

            #region Public

            public FoleySound(string soundName, double minTimeGap)
            {
                this.soundName = soundName;
                this.timeGap = minTimeGap;
            }   // end of c'tor

            public FoleySound(string soundName, double minTimeGap, bool singleton)
            {
                this.soundName = soundName;
                this.timeGap = minTimeGap;
                this.singleton = singleton;
            }   // end of c'tor

            /// <summary>
            /// Version of Play with null emitter used for UI, etc.
            /// </summary>
            public void Play()
            {
                Play(null);
            }

            /// <summary>
            /// Play a spacialized sound attatched to a GameThing.
            /// </summary>
            /// <param name="emitter"></param>
            public void Play(GameThing emitter)
            {
                // Don't allow mute actors to make noise.
                GameActor actor = emitter as GameActor;
                if (actor != null && actor.Mute)
                    return;

                // If a singleton and already playing, ignore this call.
                if(singleton && Playing(emitter))
                    return;

                if (BokuGame.Audio.Enabled)
                {
                    // Ensure we haven't already played this sound this frame.
                    if (frame == Time.FrameCounter)
                        return;

                    // Make sure enough time has elapsed.
                    double dt = Time.WallClockTotalSeconds - lastTime;
                    if (dt < timeGap)
                        return;

                    AudioCue cue = BokuGame.Audio.GetCue(soundName, emitter, true);
                    cue.Play();
                    frame = Time.FrameCounter;
                    lastTime = Time.WallClockTotalSeconds;

                }   // end if enabled.
            }   // end of Play()

            /// <summary>
            /// Play a spacialized sound from a fixed position.
            /// </summary>
            /// <param name="position">Where the sound is coming from.</param>
            /// <param name="volume">In range 0..1</param>
            public void Play(Vector3 position, float volume)
            {
                if (BokuGame.Audio.Enabled)
                {
                    // Ensure we haven't already played this sound this frame.
                    if (frame == Time.FrameCounter)
                        return;

                    // Make sure enough time has elapsed.
                    double dt = Time.WallClockTotalSeconds - lastTime;
                    if (dt < timeGap)
                        return;

                    AudioCue cue = BokuGame.Audio.GetCue(soundName, position);
                    cue.SetVolume(volume);
                    cue.Play();
                    frame = Time.FrameCounter;
                    lastTime = Time.WallClockTotalSeconds;

                }   // end if enabled.
            }   // end of Play()

            /// <summary>
            /// Tells a cue to stop.  Useful for looping sounds.  :-)
            /// </summary>
            /// <param name="emitter"></param>
            public void Stop(GameThing emitter)
            {
                List<AudioCue> activeCues = BokuGame.Audio.ActiveCues;

                for (int i = 0; i < activeCues.Count; i++)
                {
                    if (activeCues[i].Emitter == emitter && activeCues[i].Name == soundName && activeCues[i].IsPlaying)
                    {
                        activeCues[i].Stop();
                    }
                }
            }   // end of Stop()

            /// <summary>
            /// Check if a cue is already playing.  Useful for looping sounds.  :-)
            /// </summary>
            /// <param name="emitter"></param>
            public bool Playing(GameThing emitter)
            {
                bool result = false;

                List<AudioCue> activeCues = BokuGame.Audio.ActiveCues;

                for (int i = 0; i < activeCues.Count; i++)
                {
                    if (activeCues[i].Emitter == emitter && activeCues[i].Name == soundName && activeCues[i].IsPlaying)
                    {
                        result = true;
                    }
                }

                return result;
            }   // end of Playing()

            #endregion

            #region Internal

            private void Restart(AudioCue audioCue, GameThing emitter)
            {
                audioCue.Reset();
                Cue cue = BokuGame.Audio.SoundBank.GetCue(soundName);
                audioCue.Set(cue, emitter);
                audioCue.Play();
                frame = Time.FrameCounter;
                lastTime = Time.WallClockTotalSeconds;
            }   // end of Restart()

            #endregion

        }   // end of class FoleySound

        #region Members

        // KeyAction sounds.
        private static FoleySound boom;
        private static FoleySound create;
        private static FoleySound damage;
        private static FoleySound drop;
        private static FoleySound eatBig;
        private static FoleySound eatSmall;
        private static FoleySound emoAngry;
        private static FoleySound emoCrazy;
        private static FoleySound emoFlowers;
        private static FoleySound emoHappy;
        private static FoleySound emoLove;
        private static FoleySound emoSad;
        private static FoleySound emoStars;
        private static FoleySound emoSwearing;
        private static FoleySound fire;
        private static FoleySound rapidFire;
        private static FoleySound give;
        private static FoleySound glow;
        private static FoleySound grab;
        private static FoleySound heal;
        private static FoleySound kick;
        private static FoleySound pop;
        private static FoleySound stun;
        private static FoleySound reset;
        private static FoleySound say;
        private static FoleySound score;
        private static FoleySound scoreDown;
        private static FoleySound shoot;
        private static FoleySound splash;   // Not realy a KA sound...
        private static FoleySound vanish;
        // Rover-specific KAs
        private static FoleySound beam;
        private static FoleySound inspect;
        private static FoleySound scan;
        private static FoleySound photo;

        // Fan-specific KAs
        private static FoleySound fanStart;
        private static FoleySound fanStop;
        private static FoleySound fanLoop;

        // Sound effects.
        //private static FoleySound kick;
        private static FoleySound heavyCollision;
        private static FoleySound explosion;
        private static FoleySound repeatingLaser;

        private static FoleySound rockBreak;
        private static FoleySound rockSizzle;
        private static FoleySound rockScanned;

        private static FoleySound camouflageOn;
        private static FoleySound camouflageOff;

        // UI sounds, Omni.
        private static FoleySound back;
        private static FoleySound clickUp;
        private static FoleySound clickDown;
        private static FoleySound clone;
        private static FoleySound colorChange;
        private static FoleySound confirmErase;
        private static FoleySound cursor;
        private static FoleySound cut;
        private static FoleySound earthDown;
        private static FoleySound earthUp;
        private static FoleySound eraseLand;
        private static FoleySound editPath;
        private static FoleySound paint;
        private static FoleySound endGame;
        private static FoleySound lose;
        private static FoleySound makePath;
        private static FoleySound menuLoop;
        private static FoleySound pressA;
        private static FoleySound pressStart;
        private static FoleySound shuffle;
        private static FoleySound waterLower;
        private static FoleySound waterRaise;
        private static FoleySound win;

        // UI sounds.  Non-localized.
        private static FoleySound programmingClick;
        private static FoleySound programmingAdd;
        private static FoleySound programmingDelete;
        private static FoleySound programmingMoveOut;
        private static FoleySound programmingMoveBack;
        private static FoleySound simEnter;
        private static FoleySound noBudget;

        // Collision Sounds.
        private static FoleySound collisionDirt;
        private static FoleySound collisionGrass;
        private static FoleySound collisionRock;
        private static FoleySound collisionPlasticHard;
        private static FoleySound collisionPlasticSoft;
        private static FoleySound collisionMetalHard;
        private static FoleySound collisionMetalSoft;
        private static FoleySound collisionRover;

        #endregion

        #region Accessors
        #endregion

        #region Public

        // c'tor, We're strictly static so we don't need this.
        private Foley()
        {
        }   // end of c'tor

        public static void Init()
        {
            //
            // KeyAction
            //
            if(boom == null)
            {
                boom = new FoleySound(@"KA_Boom", 0.05);
            }

            if(create == null)
            {
                create = new FoleySound(@"KA_Create", 0.05);
            }

            if(damage == null)
            {
                damage = new FoleySound(@"KA_Damage", 0.05);
            }

            if(drop == null)
            {
                drop = new FoleySound(@"KA_Drop", 0.05);
            }

            if(eatBig == null)
            {
                eatBig = new FoleySound(@"KA_Eat_Big", 0.05);
            }

            if(eatSmall == null)
            {
                eatSmall = new FoleySound(@"KA_Eat_Small", 0.05);
            }

            if(emoAngry == null)
            {
                emoAngry = new FoleySound(@"KA_Emo_Angry", 0.05);
            }

            if(emoCrazy == null)
            {
                emoCrazy = new FoleySound(@"KA_Emo_Crazy", 0.05);
            }

            if(emoFlowers == null)
            {
                emoFlowers = new FoleySound(@"KA_Emo_Flowers", 0.05);
            }

            if(emoHappy == null)
            {
                emoHappy = new FoleySound(@"KA_Emo_Happy", 0.05);
            }

            if(emoLove == null)
            {
                emoLove = new FoleySound(@"KA_Emo_Love", 0.05);
            }

            if(emoSad == null)
            {
                emoSad = new FoleySound(@"KA_Emo_Sad", 0.05);
            }

            if(emoStars == null)
            {
                emoStars = new FoleySound(@"KA_Emo_Stars", 0.05);
            }

            if(emoSwearing == null)
            {
                emoSwearing = new FoleySound(@"KA_Emo_Swearing", 0.05);
            }

            if (fire == null)
            {
                fire = new FoleySound(@"KA_Shoot", 0.02);
            }

            if (rapidFire == null)
            {
                rapidFire = new FoleySound(@"KA_RapidFire", 0.02);
            }

            if(give == null)
            {
                give = new FoleySound(@"KA_Give", 0.05);
            }

            if (glow == null)
            {
                glow = new FoleySound(@"KA_Glow", 0.05);
            }

            if (grab == null)
            {
                grab = new FoleySound(@"KA_Grab", 0.05);
            }

            if(heal == null)
            {
                heal = new FoleySound(@"KA_Heal", 0.05);
            }

            if(kick == null)
            {
                kick = new FoleySound(@"KA_Kick", 0.05);
            }

            if(pop == null)
            {
                pop = new FoleySound(@"KA_Pop", 0.05);
            }

            if (stun == null)
            {
                stun = new FoleySound(@"KA_Stun", 0.05);
            }

            if (reset == null)
            {
                reset = new FoleySound(@"KA_Reset", 0.05);
            }

            if (say == null)
            {
                say = new FoleySound(@"KA_Say", 0.05);
            }

            if(score == null)
            {
                score = new FoleySound(@"KA_Score", 0.05);
            }

            if(scoreDown == null)
            {
                scoreDown = new FoleySound(@"KA_Score_Down", 0.05);
            }

            if(shoot == null)
            {
                shoot = new FoleySound(@"KA_Shoot 2", 0.05);
            }

            if (splash == null)
            {
                splash = new FoleySound(@"ud_Splash", 0.05);
            }

            if (vanish == null)
            {
                vanish = new FoleySound(@"KA_Vanish", 0.05);
            }

            //
            // Rover-specific KAs
            //
            if (beam == null)
            {
                beam = new FoleySound(@"sfx_kodu_laser_fire", 0.02);
            }
            if (inspect == null)
            {
                inspect = new FoleySound(@"sfx_kodu_drill", 0.02);
            }
            if (scan == null)
            {
                scan = new FoleySound(@"sfx_kodu_scan", 0.02);
            }
            if (photo == null)
            {
                photo = new FoleySound(@"sfx_kodu_photo", 0.02);
            }

            //Fan specific KAs

            if (fanStart == null)
            {
                fanStart = new FoleySound(@"sfx_kodu_fan_start", 0.1);
            }

            if (fanStop == null)
            {
                fanStop = new FoleySound(@"sfx_kodu_fan_stop", 0.1);
            }

            if (fanLoop == null)
            {
                fanLoop = new FoleySound(@"sfx_kodu_fan_loop", 0.1);
            }


            //
            // Effects
            //

            if (kick == null)
            {
                kick = new FoleySound(@"ImpactOnWoodFloorSet", 0.05);
            }

            if (heavyCollision == null)
            {
                heavyCollision = new FoleySound(@"Tank Hit", 0.1);
            }

            if (explosion == null)
            {
                explosion = new FoleySound(@"Explosion", 0.1);
            }

            if (repeatingLaser == null)
            {
                repeatingLaser = new FoleySound(@"Repeating Laser", 0.1);
            }

            if (rockBreak == null)
            {
                rockBreak = new FoleySound(@"sfx_kodu_rock_break", 0.01);
            }

            if (rockSizzle == null)
            {
                rockSizzle = new FoleySound(@"sfx_kodu_sizzle", 0.01);
            }

            if (rockScanned == null)
            {
                rockScanned = new FoleySound(@"sfx_kodu_scan_success", 0.01);
            }

            if (camouflageOn == null)
            {
                camouflageOn = new FoleySound(@"sfx_kodu_octopus_camo_on", 0.01);
            }

            if (camouflageOff == null)
            {
                camouflageOff = new FoleySound(@"sfx_kodu_octopus_camo_off", 0.01);
            }

            //
            // UI Sounds, Omni
            //

            if (back == null)
            {
                back = new FoleySound(@"UI_Back", 0.05);
            }

            if (clickUp == null)
            {
                clickUp = new FoleySound(@"UI_ClickUp", 0.05);
            }

            if (clickDown == null)
            {
                clickDown = new FoleySound(@"UI_ClickDown", 0.05);
            }

            if (clone == null)
            {
                clone = new FoleySound(@"UI_Clone", 0.05);
            }

            if (colorChange == null)
            {
                colorChange = new FoleySound(@"UI_ColorChange", 0.05);
            }

            if (confirmErase == null)
            {
                confirmErase = new FoleySound(@"UI_ConfirmErase", 0.05);
            }

            if (cursor == null)
            {
                cursor = new FoleySound(@"UI_Cursor", 0.05);
            }

            if (cut == null)
            {
                cut = new FoleySound(@"UI_Cut", 0.05);
            }

            if (earthDown == null)
            {
                earthDown = new FoleySound(@"UI_EarthDown", 0.05, true);
            }

            if (earthUp == null)
            {
                earthUp = new FoleySound(@"UI_EarthUp", 0.05, true);
            }

            if (eraseLand == null)
            {
                eraseLand = new FoleySound(@"UI_EraseLand", 0.05, true);
            }

            if (editPath == null)
            {
                editPath = new FoleySound(@"UI_EditPath", 0.05);
            }

            if (paint == null)
            {
                paint = new FoleySound(@"UI_Paint", 0.05, true);
            }

            if (endGame == null)
            {
                endGame = new FoleySound(@"UI_EndGame", 0.05);
            }

            if (lose == null)
            {
                lose = new FoleySound(@"UI_Lose", 0.05);
            }

            if (makePath == null)
            {
                makePath = new FoleySound(@"UI_MakePath", 0.05);
            }

            if (menuLoop == null)
            {
                menuLoop = new FoleySound(@"UI_MenuLoop", 0.05);
            }

            if (pressA == null)
            {
                pressA = new FoleySound(@"UI_PressA", 0.05);
            }

            if (pressStart == null)
            {
                pressStart = new FoleySound(@"UI_PressStart", 0.05);
            }

            if (shuffle == null)
            {
                shuffle = new FoleySound(@"UI_Shuffle", 0.05);
            }

            if (waterLower == null)
            {
                waterLower = new FoleySound(@"UI_Water_Lower", 0.05, true);
            }

            if (waterRaise == null)
            {
                waterRaise = new FoleySound(@"UI_Water_Raise", 0.05, true);
            }

            if (win == null)
            {
                win = new FoleySound(@"UI_Win", 0.05);
            }


            //
            // UI Sounds
            //

            if (programmingClick == null)
            {
                programmingClick = new FoleySound(@"programming click", 0.0);
            }

            if (programmingAdd == null)
            {
                programmingAdd = new FoleySound(@"programming add", 0.0);
            }

            if (programmingDelete == null)
            {
                programmingDelete = new FoleySound(@"programming delete", 0.0);
            }

            if (programmingMoveOut == null)
            {
                programmingMoveOut = new FoleySound(@"programming move out", 0.0);
            }

            if (programmingMoveBack == null)
            {
                programmingMoveBack = new FoleySound(@"programming move back", 0.0);
            }

            if (simEnter == null)
            {
                simEnter = new FoleySound(@"Sim Enter", 0.0);
            }

            if (noBudget == null)
            {
                noBudget = new FoleySound(@"No Budget", 0.0);
            }


            //
            // Collision Sounds
            //
            if (collisionDirt == null)
            {
                collisionDirt = new FoleySound(@"Collide_Dirt", 0.05);
            }

            if (collisionGrass == null)
            {
                collisionGrass = new FoleySound(@"Collide_Grass", 0.05);
            }

            if (collisionRock == null)
            {
                collisionRock = new FoleySound(@"Collide_Rock", 0.05);
            }

            if (collisionPlasticHard == null)
            {
                collisionPlasticHard = new FoleySound(@"Collide_Plastic_Hard", 0.05);
            }

            if (collisionPlasticSoft == null)
            {
                collisionPlasticSoft = new FoleySound(@"Collide_Plastic_Soft", 0.05);
            }

            if (collisionMetalHard == null)
            {
                collisionMetalHard = new FoleySound(@"Collide_Metal_Hard", 0.05);
            }

            if (collisionMetalSoft == null)
            {
                collisionMetalSoft = new FoleySound(@"Collide_Metal_Soft", 0.05);
            }

            if (collisionRover == null)
            {
                collisionRover = new FoleySound(@"sfx_kodu_crash", 0.05);
            }

        }   // end of Init()

        /// <summary>
        /// Play audio for actor/actor collision
        /// </summary>
        /// <param name="actor0"></param>
        /// <param name="actor1"></param>
        public static void PlayCollision(GameThing actor0, GameThing actor1)
        {
            PlayActorCollision(actor0);
            PlayActorCollision(actor1);
        }   // end of PlayCollision()

        /// <summary>
        /// Play audio for actor/terrrain collision
        /// </summary>
        /// <param name="actor"></param>
        /// <param name="material">Material index.</param>
        public static void PlayCollision(GameThing actor, ushort material)
        {
            // Get the proper collision sound from the material index.
            if (!TerrainMaterial.IsValid(material, false, false))
            {
                //Debug.Assert(false, "Why do we have an invalid material here?");
                // Why?  Well because we sometimes clear the material in the feeler
                // collision testing.  So it's no longer a valid way to tell what 
                // terrain we're over.  In general, it works when we're on the ground
                // but not when jumping.  Argh.

                collisionMetalSoft.Play(actor);
                return;
            }

            PlayActorCollision(actor);

            CollisionSound cs = TerrainMaterial.Get(material).CollisionSound;
            switch (cs)
            {
                case CollisionSound.plasticHard:
                    collisionPlasticHard.Play(actor);
                    break;
                case CollisionSound.plasticSoft:
                    collisionPlasticSoft.Play(actor);
                    break;
                case CollisionSound.metalHard:
                    collisionMetalHard.Play(actor);
                    break;
                case CollisionSound.metalSoft:
                    collisionMetalSoft.Play(actor);
                    break;
                case CollisionSound.rover:
                    collisionRover.Play(actor);
                    break;
                case CollisionSound.dirt:
                    collisionDirt.Play(actor);
                    break;
                case CollisionSound.grass:
                    collisionGrass.Play(actor);
                    break;
                case CollisionSound.rock:
                    collisionRock.Play(actor);
                    break;
                default:
                    break;
            }
        }   // end of PlayCollision()

        private static void PlayActorCollision(GameThing actor)
        {
            CollisionSound cs = actor.CollisionSound;
            switch (cs)
            {
                case CollisionSound.plasticHard:
                    collisionPlasticHard.Play(actor);
                    break;
                case CollisionSound.plasticSoft:
                    collisionPlasticSoft.Play(actor);
                    break;
                case CollisionSound.metalHard:
                    collisionMetalHard.Play(actor);
                    break;
                case CollisionSound.metalSoft:
                    collisionMetalSoft.Play(actor);
                    break;
                case CollisionSound.rover:
                    collisionRover.Play(actor);
                    break;
                case CollisionSound.dirt:
                    collisionDirt.Play(actor);
                    break;
                case CollisionSound.grass:
                    collisionGrass.Play(actor);
                    break;
                case CollisionSound.rock:
                    collisionRock.Play(actor);
                    break;
                default:
                    break;
            }
        }   // end of PlayActorCollision()

        //
        //
        // Single sound play methods.
        //
        //

        //
        // KeyActions
        //
        public static void PlayBoom(GameThing emitter)
        {
            boom.Play(emitter);
        }   // end of PlayBoom()

        public static void PlayCreate(GameThing emitter)
        {
            create.Play(emitter);
        }   // end of PlayCreate()

        public static void PlayDamage(GameThing emitter)
        {
            damage.Play(emitter);
        }   // end of PlayDamage()

        public static void PlayDrop(GameThing emitter)
        {
            drop.Play(emitter);
        }   // end of PlayDrop()

        public static void PlayEatBig(GameThing emitter)
        {
            eatBig.Play(emitter);
        }   // end of PlayEatBig()

        public static void PlayEatSmall(GameThing emitter)
        {
            eatSmall.Play(emitter);
        }   // end of PlayEatSmall()

        public static void PlayEmoAngry(GameThing emitter)
        {
            emoAngry.Play(emitter);
        }   // end of PlayEmoAngry()

        public static void PlayEmoCrazy(GameThing emitter)
        {
            emoCrazy.Play(emitter);
        }   // end of PlayEmoCrazy()

        public static void PlayEmoFlowers(GameThing emitter)
        {
            emoFlowers.Play(emitter);

            emoLove.Stop(emitter);
            emoStars.Stop(emitter);
            emoSwearing.Stop(emitter);
        }   // end of PlayEmoFlowers()

        public static void PlayEmoHappy(GameThing emitter)
        {
            emoHappy.Play(emitter);
        }   // end of PlayEmoHappy()

        public static void PlayEmoLove(GameThing emitter)
        {
            emoLove.Play(emitter);

            emoFlowers.Stop(emitter);
            emoStars.Stop(emitter);
            emoSwearing.Stop(emitter);
        }   // end of PlayEmoLove()

        public static void PlayEmoSad(GameThing emitter)
        {
            emoSad.Play(emitter);
        }   // end of PlayEmoSad()

        public static void PlayEmoStars(GameThing emitter)
        {
            emoStars.Play(emitter);

            emoFlowers.Stop(emitter);
            emoLove.Stop(emitter);
            emoSwearing.Stop(emitter);
        }   // end of PlayEmoStars()

        public static void PlayEmoSwearing(GameThing emitter)
        {
            emoSwearing.Play(emitter);

            emoFlowers.Stop(emitter);
            emoLove.Stop(emitter);
            emoStars.Stop(emitter);
        }   // end of PlayEmoSwearing()

        public static void PlayEmoNone(GameThing emitter)
        {
            emoFlowers.Stop(emitter);
            emoLove.Stop(emitter);
            emoStars.Stop(emitter);
            emoSwearing.Stop(emitter);
        }   // end of PlayEmoNone()

        public static void PlayFire(GameThing emitter)
        {
            fire.Play(emitter);
        }   // end of PlayFire()

        public static void PlayRapidFire(GameThing emitter)
        {
            rapidFire.Play(emitter);
        }   // end of PlayRapidFire()

        public static void PlayGive(GameThing emitter)
        {
            give.Play(emitter);
        }   // end of PlayGive()

        public static void PlayGlow(GameThing emitter)
        {
            // This audio effect breaks the levels Sequencer and Drive & Play. These levels
            // use the glow effect to indicate which rock is playing a sound. When the glow
            // effect itself makes a sound, chaos ensues, masking the beautiful music these
            // levels create.

            //glow.Play(emitter);
        }   // end of PlayGlow()

        public static void PlayGrab(GameThing emitter)
        {
            grab.Play(emitter);
        }   // end of PlayGrab()

        public static void PlayHeal(GameThing emitter)
        {
            heal.Play(emitter);
        }   // end of PlayHeal()

        /// <summary>
        /// Should be renamed PlayLaunch???
        /// </summary>
        /// <param name="emitter"></param>
        public static void PlayKick(GameThing emitter)
        {
            kick.Play(emitter);
        }   // end of PlayKick()

        public static void PlayPop(GameThing emitter)
        {
            pop.Play(emitter);
        }   // end of PlayPop()

        public static void PlayStun(GameThing emitter)
        {
            stun.Play(emitter);
        }   // end of PlayStun()

        public static void PlayReset(GameThing emitter)
        {
            reset.Play(emitter);
        }   // end of PlayReset()

        public static void PlaySay(GameThing emitter)
        {
            say.Play(emitter);
        }   // end of PlaySay()

        public static void PlayScore(GameThing emitter)
        {
            score.Play(emitter);
        }   // end of PlayScore()

        public static void PlayScoreDown(GameThing emitter)
        {
            scoreDown.Play(emitter);
        }   // end of PlayScoreDown()

        public static void PlayShoot(GameThing emitter)
        {
            shoot.Play(emitter);
        }   // end of PlayShoot()

        public static void PlaySplash(Vector3 position, float volume)
        {
            splash.Play(position, volume);
        }   // end of PlaySplash()

        public static void PlayVanish(GameThing emitter)
        {
            vanish.Play(emitter);
        }   // end of PlayVanish()
        
        //Rover-specific KAs
        public static void PlayBeam(GameThing emitter)
        {
            beam.Play(emitter);
        }   // end of PlayBeam()

        public static void PlayInspect(GameThing emitter)
        {
            inspect.Play(emitter);
        }   // end of PlayInspect()

        public static void PlayScan(GameThing emitter)
        {
            scan.Play(emitter);
        }   // end of PlayScan()

        public static void PlayPhoto(GameThing emitter)
        {
            photo.Play(emitter);
        }   // end of PlayPhoto()

        //Fan-specific KAs
        public static void PlayFanStart(GameThing emitter)
        {
            fanStart.Play(emitter);
        }   // end of PlayFanStart()

        public static void PlayFanStop(GameThing emitter)
        {
            fanStop.Play(emitter);
        }   // end of PlayFanStop()

        public static void PlayFanLoop(GameThing emitter)
        {
            fanLoop.Play(emitter);
        }   // end of PlayFanLoop()

        public static void StopFanLoop(GameThing emitter)
        {
            fanLoop.Stop(emitter);
        }   // end of PlayFanLoop()

        //
        // Effects
        //

        /*
        public static void PlayKick(GameThing emitter)
        {
            kick.Play(emitter);
        }   // end of PlayKick()
        */

        public static void PlayHeavyCollision(GameThing emitter)
        {
            heavyCollision.Play(emitter);
        }   // end of PlayHeavyCollision()

        public static void PlayExplosion(GameThing emitter)
        {
            explosion.Play(emitter);
        }   // end of PlayExplosion()

        public static void PlayRepeatingLaser(GameThing emitter)
        {
            repeatingLaser.Play(emitter);
        }   // end of PlayRepeatingLaser()

        public static void PlayRockBreak(GameThing emitter)
        {
            rockBreak.Play(emitter);
        }   // end of PlayBeam()

        public static void PlayRockSizzle(GameThing emitter)
        {
            rockSizzle.Play(emitter);
        }   // end of PlayRockSizzle()

        public static void PlayRockScanned(GameThing emitter)
        {
            rockScanned.Play(emitter);
        }   // end of PlayRockScanned()

        public static void PlayCamouflageOn(GameThing emitter)
        {
            camouflageOn.Play(emitter);
        }   // end of PlayCamouflageOn()

        public static void PlayCamouflageOff(GameThing emitter)
        {
            camouflageOff.Play(emitter);
        }   // end of PlayCamouflageOff()

        //
        // UI sounds, Omni
        //

        public static void PlayBack()
        {
            back.Play();
        }   // end of PlayBack()

        public static void PlayClick()
        {
            clickUp.Play();
        }   // end of PlayClick()

        public static void PlayClickUp()
        {
            clickUp.Play();
        }   // end of PlayClickUp()

        public static void PlayClickDown()
        {
            clickDown.Play();
        }   // end of PlayClickDown()

        public static void PlayClone()
        {
            clone.Play();
        }   // end of PlayClone()

        public static void PlayColorChange()
        {
            colorChange.Play();
        }   // end of PlayColorChange()

        public static void PlayConfirmErase()
        {
            confirmErase.Play();
        }   // end of PlayConfirmErase()

        public static void PlayCursor()
        {
            cursor.Play();
        }   // end of PlayCursor()

        public static void PlayCut()
        {
            cut.Play();
        }   // end of PlayCut()

        public static void PlayPaste()
        {
            clone.Play();
        }   // end of PlayPaste()

        public static void PlayEarthDown()
        {
            earthDown.Play();
        }   // end of PlayEarthDown()

        public static void StopEarthDown()
        {
            earthDown.Stop(null);
        }   // end of StopEarthDown()

        public static void PlayEarthUp()
        {
            earthUp.Play();
        }   // end of PlayEarthUp()

        public static void StopEarthUp()
        {
            earthUp.Stop(null);
        }   // end of StopEarthUp()

        public static void PlayEraseLand()
        {
            eraseLand.Play();
        }   // end of PlayEraseLand()

        public static void StopEraseLand()
        {
            eraseLand.Stop(null);
        }   // end of StopEraseLand()

        public static void PlayEditPath()
        {
            editPath.Play();
        }   // end of PlayEditPath()

        public static void PlayPaint()
        {
             paint.Play();
        }   // end of PlayPaint()

        public static void StopPaint()
        {
            paint.Stop(null);
        }   // end of StopPaint()

        public static void PlayEndGame()
        {
            endGame.Play();
        }   // end of PlayEndGame()

        public static void PlayLose()
        {
            lose.Play();
        }   // end of PlayLose()

        public static void PlayMakePath()
        {
            makePath.Play();
        }   // end of PlayMakePath()

        public static void PlayMenuLoop()
        {
            menuLoop.Play();
        }   // end of PlayMenuLoop()

        public static void StopMenuLoop()
        {
            menuLoop.Stop(null);
        }   // end of StopMenuLoop();

        public static void PlayPressA()
        {
            pressA.Play();
        }   // end of PlayPressA()

        public static void PlayPressStart()
        {
            pressStart.Play();
        }   // end of PlayPressStart()

        public static void PlayShuffle()
        {
            shuffle.Play();
        }   // end of PlayShuffle()

        public static void PlayLowerWater()
        {
            waterLower.Play();
        }   // end of PlayLowerWater()

        public static void StopLowerWater()
        {
            waterLower.Stop(null);
        }   // end of StopLowerWater()

        public static void PlayRaiseWater()
        {
            waterRaise.Play();
        }   // end of PlayRaiseWater()

        public static void StopRaiseWater()
        {
            waterRaise.Stop(null);
        }   // end of StopRaiseWater()

        public static void PlayWin()
        {
            win.Play();
        }   // end of PlayWin()


        //
        // UI sounds
        //

        public static void PlayProgrammingClick()
        {
            programmingClick.Play(null);
        }   // end of PlayProgrammingClick()

        public static void PlayProgrammingAdd()
        {
            programmingAdd.Play(null);
        }   // end of PlayProgrammingAdd()

        public static void PlayProgrammingDelete()
        {
            programmingDelete.Play(null);
        }   // end of PlayProgrammingDelete()

        public static void PlayProgrammingMoveOut()
        {
            programmingMoveOut.Play(null);
        }   // end of PlayProgrammingMoveOut()

        public static void PlayProgrammingMoveBack()
        {
            if(programmingMoveBack != null)
                programmingMoveBack.Play(null);
        }   // end of PlayProgrammingMoveBack()

        public static void PlaySimEnter()
        {
            simEnter.Play(null);
        }   // end of PlaySimEnter()

        public static void PlayNoBudget()
        {
           noBudget.Play(null);
        }   // end of PlayNoBudget()

        #endregion

        #region Internal
        #endregion

    }   // end of class Foley

}   // end of namespace Boku.Audio
