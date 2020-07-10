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
using KoiX.Text;

using Boku.Base;
using Boku.Audio;
using Boku.Fx;
using Boku.Common.TutorialSystem;

namespace Boku.Common
{
    /// <summary>
    /// Provides a single entry point for creating ThoughtBalloons anywhere in the scene.  
    /// </summary>
    public class ThoughtBalloonManager
    {
        private static int numBalloons = 50;                        // Max number of active balloons.
        private static List<ThoughtBalloon> activeBalloons = null;
        private static List<ThoughtBalloon> spareBalloons = null;

        private static Effect effect;
        private static Texture2D frameTexture;
        private static Texture2D tutorialFocusArrow;

        #region Accessors
        
        public static Effect Effect
        {
            get { return effect; }
        }
        public static Texture2D FrameTexture
        {
            get { return frameTexture; }
        }

        #endregion

        // c'tor
        private ThoughtBalloonManager()
        {
        }

        public static void Init()
        {
            activeBalloons = new List<ThoughtBalloon>();
            spareBalloons = new List<ThoughtBalloon>();

            // Create all the balloons we'll need and add them to the spares list.
            for (int i = 0; i < numBalloons; i++)
            {
                ThoughtBalloon balloon = new ThoughtBalloon();
                spareBalloons.Add(balloon);
            }
        }   // end of c'tor

        public static void Update(Camera camera)
        {
            // Loop through the list backwards in case we remove one.
            for (int i = activeBalloons.Count - 1; i >= 0; i--)
            {
                ThoughtBalloon balloon = activeBalloons[i];
                bool alive = balloon.Update(camera);
                if (!alive)
                {
                    // Before removing balloon, let the SaidStringManager know.
                    SaidStringManager.AddEntry(balloon.Thinker as GameActor, balloon.RawText, false);

                    activeBalloons.RemoveAt(i);
                    spareBalloons.Add(balloon);
                }
            }   // end of loop over list.

        }   // end of ThoughtBalloonManager Update()

        /// <summary>
        /// Render any active thought balloons.
        /// </summary>
        /// <param name="camera"></param>
        /// <param name="renderFirstPerson">Only render 1st person balloon(s).</param>
        public static void Render(Camera camera, bool renderFirstPerson)
        {
            if (activeBalloons.Count > 0)
            {
                if (InGame.inGame.renderEffects != InGame.RenderEffect.Normal)
                {
                    Effect.CurrentTechnique = Effect.Techniques[InGame.inGame.renderEffects.ToString()];
                }
                else
                {
                    Effect.CurrentTechnique = Effect.Techniques["ColorPass"];
                }

                for (int i = 0; i < activeBalloons.Count; i++)
                {
                    if (renderFirstPerson == activeBalloons[i].Thinker.FirstPerson)
                    {
                        activeBalloons[i].Render(camera);
                    }
                }
            }

            // Help out the tutorial mode...
            GameActor actor = TutorialManager.FocusActor;
            if (TutorialManager.Active && actor != null)
            {
                Matrix world = Matrix.CreateBillboard(actor.WorldThoughtBalloonOffset, camera.ActualFrom, camera.ViewUp, camera.ViewDir);

                Vector3 pos3 = TutorialManager.FocusActor.Movement.Position + TutorialManager.FocusActor.ThoughtBalloonOffset;
                pos3 = Vector3.Transform(pos3, camera.ViewProjectionMatrix);
                
                // Adjust for Z.  This gives us homogeneous coords.
                Vector2 pos = new Vector2(pos3.X / pos3.Z, pos3.Y / pos3.Z);
                
                // Map to screen coords.
                Vector2 screenPos = (0.5f * pos + new Vector2(0.5f, 0.5f)) * BokuGame.ScreenSize;
                screenPos.Y = BokuGame.ScreenSize.Y - screenPos.Y;

                // Figure out rectangle size
                Vector2 size = new Vector2(tutorialFocusArrow.Width, tutorialFocusArrow.Height);
                size = 6.0f * size / pos3.Z;
                Rectangle rect = new Rectangle((int)(screenPos.X - size.X / 2.0f), (int)(screenPos.Y - size.Y * 0.75f), (int)size.X, (int)size.Y);

                SpriteBatch batch = KoiLibrary.SpriteBatch;
                batch.Begin(SpriteSortMode.Deferred, BlendState.NonPremultiplied);
                batch.Draw(tutorialFocusArrow, rect, Color.White);
                batch.End();
            }

        }   // end of ThoughtBalloonManager Render()

        /// <summary>
        /// Creates a thought balloon over a bot.
        /// </summary>
        /// <param name="thinker">The bot with the thought.</param>
        /// <param name="text">What he's thinking.</param>
        /// <param name="color">Color for balloon outline.</param>
        /// <returns>True if acted upon, false if ignored.</returns>
        public static bool CreateThoughtBalloon(GameThing thinker, string text, Vector4 color)
        {
            return CreateThoughtBalloon(thinker, text, color, false);
        }

        /// <summary>
        /// Creates a thought balloon over a bot.  This version is mostly for edit mode
        /// where you don't want the bot to play its speach sound.  Note that this version 
        /// also kills off any previous thought balloons since we only want one bot at a
        /// time to identify itself.
        /// </summary>
        /// <param name="thinker">The bot with the thought.</param>
        /// <param name="text">What he's thinking.</param>
        /// <param name="color">Color for balloon outline.</param>
        /// <param name="editMode">If true, don't play a bot speach sound.</param>
        /// <returns>True if acted upon, false if ignored.</returns>
        public static bool CreateThoughtBalloon(GameThing thinker, string text, Vector4 color, bool editMode)
        {
            //Debug.Print(text);

            // Early out if we have nothing to say.
            if (text == null || text == "")
            {
                return true;
            }

            // The thought string may have text substitutions in in (eg <score red>) so
            // process those first so we can do vaild text string comparisons.
            string newText = TextHelper.ApplyStringSubstitutions(text, thinker as GameActor);
            string rawText = text;  // Text before substitution.
            bool substitution = newText != text;
            text = newText;

            // If the current bot is already thinking this same thought then just 
            // extend the time for the thought rather than creating a duplicate.
            // If the bot is already thinking another thought then ignore the new
            // thought.  Always replace the string just in case a substitution 
            // has take place.
            // Also use this opportunity to kill off thoughts from other bots if
            // we're in edit mode.
            for (int i = 0; i < activeBalloons.Count; i++)
            {
                ThoughtBalloon balloon = activeBalloons[i];
                if (balloon.Thinker == thinker)
                {
                    // Remove tags.  Need to do this _before_ setting the text 
                    // on the balloon otherwise the tags can show up on screen.
                    text = TextHelper.RemoveTags(text).Trim();

                    // If just thinking the same thought again, extend the time.
                    if (balloon.RawText == rawText)
                    {
                        balloon.RestartTime();
                        balloon.Text = text;    // Update the text in case of a substitution, eg a score changed.
                    }

                    // Check for message being sent.  If so, don't return yet.
                    if (text.Length > 0)
                    {
                        // Current thought has priority, so don't act on new changes.
                        return false;
                    }
                }
                else
                {
                    if (editMode)
                    {
                        balloon.Kill();
                    }
                }
            }


            int count = spareBalloons.Count;
            if (count > 0)
            {
                // Search the existing spares for one that already matches the string.
                ThoughtBalloon balloon = null;
                for (int i = 0; i < spareBalloons.Count; i++)
                {
                    if (spareBalloons[i].Text == text)
                    {
                        // Found a match.
                        balloon = spareBalloons[i];
                        spareBalloons.RemoveAt(i);
                        break;
                    }
                }

                if (InGame.inGame.CurrentUpdateMode == InGame.UpdateMode.RunSim)
                {
                    SaidStringManager.AddEntry(thinker as GameActor, rawText, true);
                }
                string txt = TextHelper.RemoveTags(rawText).Trim();
                if (txt == null || txt == string.Empty)
                {
                    // Must have been just a pure (tag only) message.  In that case, 
                    // also send it with atBeginning set to false.  This lets the
                    // user be a bit sloppy with the triggering.
                    SaidStringManager.AddEntry(thinker as GameActor, rawText, false);

                    return true;
                }

                // If no match found, just grab one.
                if (balloon == null)
                {
                    balloon = spareBalloons[0];
                    spareBalloons.RemoveAt(0);
                }

                // Also remove tags for non-raw text.
                text = TextHelper.RemoveTags(text);

                balloon.Activate(thinker, text, rawText, color);

                activeBalloons.Add(balloon);

                // Call update to set up the balloon for the
                // rendering of its first frame.
                balloon.Update(InGame.inGame.shared.camera);

                if (!editMode)
                {
                    Foley.PlaySay(thinker);
                }

                return true;
            }

            return false;

        }   // end of CreateThoughtBalloon()

        /// <summary>
        /// Removes any thought balloons thought by the GameThing.
        /// </summary>
        /// <param name="thinker"></param>
        public static void RemoveThoughts(GameThing thinker)
        {
            // Loop through the list backwards in case we remove one.
            for (int i = activeBalloons.Count - 1; i >= 0; i--)
            {
                ThoughtBalloon balloon = activeBalloons[i];
                if (balloon.Thinker == thinker)
                {
                    activeBalloons.RemoveAt(i);
                    spareBalloons.Add(balloon);
                }
            }   // end of loop over list.

        }   // end of ThoughtBalloonManager RemoveThoughts()

        public static void LoadContent(bool immediate)
        {
            // Init the effect.
            if (effect == null)
            {
                effect = KoiLibrary.LoadEffect(@"Shaders\ThoughtBalloon");
                ShaderGlobals.RegisterEffect("ThoughtBalloon", effect);
            }

            // Load the frame texture.
            if (frameTexture == null)
            {
                frameTexture = KoiLibrary.LoadTexture2D(@"Textures\TextEditor\ThoughtBalloon");
            }

            if (tutorialFocusArrow == null)
            {
                tutorialFocusArrow = KoiLibrary.LoadTexture2D(@"Textures\GridElements\TutorialFocusArrow");
            }

        }   // end of LoadContent()

        public static void InitDeviceResources(GraphicsDevice device)
        {
            for (int i = 0; i < spareBalloons.Count; i++)
            {
                spareBalloons[i].InitDeviceResources(device);
            }
        }

        public static void UnloadContent()
        {
            Debug.Assert(activeBalloons != null);
            Debug.Assert(spareBalloons != null);

            // Deactivate any active thought balloons.
            // Loop through the list backwards as we remove them.
            if (activeBalloons != null)
            {
                for (int i = activeBalloons.Count - 1; i >= 0; i--)
                {
                    ThoughtBalloon balloon = activeBalloons[i];
                    Debug.Assert(balloon != null);  // If this is null, something has gone wrong.
                    if (balloon != null)
                    {
                        activeBalloons.RemoveAt(i);
                        spareBalloons.Add(balloon);
                    }
                }   // end of loop over list.
            }

            if (spareBalloons != null)
            {
                for (int i = 0; i < spareBalloons.Count; i++)
                {
                    if (spareBalloons[i] != null)
                    {
                        spareBalloons[i].UnloadContent();
                    }
                }
            }

            DeviceResetX.Release(ref effect);
            DeviceResetX.Release(ref frameTexture);
            DeviceResetX.Release(ref tutorialFocusArrow);
        }   // end of UnloadContent()

        /// <summary>
        /// Recreate render targets.
        /// </summary>
        public static void DeviceReset(GraphicsDevice device)
        {
            for (int i = 0; i < activeBalloons.Count; i++)
            {
                activeBalloons[i].DeviceReset(device);
            }

            for (int i = 0; i < spareBalloons.Count; i++)
            {
                spareBalloons[i].DeviceReset(device);
            }

        }

    }   // end of class ThoughtBalloonManager

}   // end of namespace Boku.Common
