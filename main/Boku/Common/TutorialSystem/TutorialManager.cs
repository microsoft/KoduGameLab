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

using Boku.Audio;
using Boku.Base;
using Boku.Fx;
using Boku.Common;
using Boku.Common.Xml;

namespace Boku.Common.TutorialSystem
{
    /// <summary>
    /// Class for handling the tutorial system.  All access to this class should be via static methods.
    /// </summary>
    public partial class TutorialManager
    {
        #region Members

        private static Crumb[] crumbsArray = null;
        // Versions of the above list sorted to bias based on current input mode.
        // This helps to create a preferred path between modes that is biased to
        // the current input mode.  For instance, going from Programming to InGame.
        // With mouse input we want to go through MouseEditMode but with touch
        // input we want TouchEditMode instead.  The sorting of these lists
        // combined with the way FindNextCrumb() works gives us the results we want.
        private static List<Crumb> crumbListMouse = null;
        private static List<Crumb> crumbListTouch = null;
        private static List<Crumb> crumbListGamepad = null;

        // List used when finding path from curMode to targetMode.
        protected struct CrumbNode
        {
            public Crumb crumb;
            public GameMode mode;
            public int parentIndex;

            public CrumbNode(Crumb crumb, GameMode mode, int parent)
            {
                this.crumb = crumb;
                this.mode = mode;
                this.parentIndex = parent;
            }
        }
        private static List<CrumbNode> crumbList = new List<CrumbNode>();
        private static Crumb emptyCrumb = null;     // Used when we're already at the target mode.

        private static bool active = false;
        private static float targetPositionY = 0.0f;

        private static RenderTarget2D rt = null;
        private static UI2D.Shared.GetFont font = UI2D.Shared.GetGameFont24Bold;
        private static TextBlob titleBlob = null;
        private static TextBlob instructionBlob = null;
        private static Texture2D backdrop = null;
        private static Texture2D dropShadow = null;

        private static ModalDisplay modalDisplay = null;

        private static Step curStep = null;
        private static int curStepIndex = 0;
        private static Crumb curCrumb = null;
        private static bool targetModeReached = false;

        private static AABB2D backdropBox = new AABB2D();

        private static GameActor focusActor = null;

        #endregion

        #region Accessors

        public static bool Active
        {
            get { return active; }
        }

        /// <summary>
        /// This is the actor, if any, that should have the arrow rendered pointing at it.
        /// </summary>
        public static GameActor FocusActor
        {
            get { return focusActor; }
        }

        /// <summary>
        /// Is the tutorial system's modal display active?
        /// </summary>
        public static bool ModalDisplayIsActive
        {
            get { return modalDisplay.Active; }
        }

        /// <summary>
        /// The index of the current step.  This shouldn't be needed outside of 
        /// the tutorial system but it's been exposed so that the PreGame code
        /// can shut itself off if we're in a tutorial.
        /// </summary>
        public static int CurStepIndex
        {
            get { return curStepIndex; }
        }

        #endregion

        #region Public

        /// <summary>
        /// Hidden c'tor so no one tries to instantiate this class.
        /// </summary>
        private TutorialManager()
        {
        }

        static bool DebugMode
        {
            get { return XmlOptionsData.ShowTutorialDebug; }
        }

        static GameMode lastGameMode;
        public static void Update()
        {
            // Lazy creation since we don't have a c'tor.
            if (modalDisplay == null)
            {
                modalDisplay = new ModalDisplay(OnContinue, OnBack, OnExitTutorial);
            }

            if (emptyCrumb == null)
            {
                emptyCrumb = new Crumb();
            }

            if (Active)
            {
                modalDisplay.Update();

                InGame.RestoreViewportToFull();

                // Adjust BokuGame screen size and position depending on whether or not the modal dialog is active.
                // When the dislog is active, we don't have the bar across the top so we should return to full size.
                // Note, when display is changing active/inactive we want to twitch.  When the screen size changes
                // because the window got resized we don't want to twitch.
                // TODO (****) Twitching taken out since it was getting mixed up.
                if (modalDisplay.Active)
                {
                    // Calc new screen size and position for full viewport.
                    BokuGame.ScreenSize = new Vector2(BokuGame.bokuGame.GraphicsDevice.Viewport.Width, BokuGame.bokuGame.GraphicsDevice.Viewport.Height);
                    BokuGame.ScreenPosition = Vector2.Zero;
                }
                else
                {
                    // Yes, this looks clumsy.  The issue here is that the modalDisplay Update call may have
                    // deactivated tutorial mode.  If so, Active is no longer true and we don't want to mess
                    // with the screen position and size.
                    if (Active)
                    {
                        // Set the screen size to leave some room for the tutorial stuff.
                        float rtHeight = rt != null ? rt.Height : 0;
                        BokuGame.ScreenSize = new Vector2(BokuGame.bokuGame.GraphicsDevice.Viewport.Width, BokuGame.bokuGame.GraphicsDevice.Viewport.Height - rtHeight);
                        targetPositionY = BokuGame.bokuGame.GraphicsDevice.Viewport.Height - BokuGame.ScreenSize.Y;
                        BokuGame.ScreenPosition = new Vector2(0, targetPositionY);
                    }
                }
            }
            else
            {
                // Tutorial mode is not active.
            }
#if DEBUG
            // For testing purposes...
            if (KeyboardInput.WasPressed(Keys.F12))
            {
                if (Active)
                {
                    Deactivate();
                }
                else
                {
                    Activate();
                }
            }
#endif

            // Always keep this up to date in case someone else wants to use it.
            SetGameMode();

            if (curGameMode != lastGameMode)
            {
                lastGameMode = curGameMode;
            }

            // Test for null shouldn't be needed in normal operation but allows
            // for debugging of screen position and scaling without having to 
            // run a full tutorial level.
            if (Active && InGame.XmlWorldData != null)
            {
                curStep = null;
                if (InGame.XmlWorldData.tutorialSteps.Count > 0)
                {
                    // Is the current step modal?  If so, activate the modal display if needed.
                    curStep = InGame.XmlWorldData.tutorialSteps[curStepIndex];
                    if (modalDisplay.Active == false && curStep.DisplayMode == Step.Display.Modal)
                    {
                        modalDisplay.Activate(curStep.GamepadText, curStep.MouseText, curStep.TouchText, true, true);
                    }
                }
                else
                {
                    // This only happens when the tutorial system is activated in debug mode
                    // and there's no actual tutorial.  So, create a fake, nonmodal step.
                    curStep = new Step();
                    curStep.DisplayMode = Step.Display.NonModal;
                    curStep.GamepadText = "Test";
                    curStep.MouseText = "Test";
                    curStep.TargetModeGamepad = GameMode.MainMenu;
                    curStep.TargetModeMouse = GameMode.MainMenu;
                }

                // NonModal?
                if (curStep.DisplayMode == Step.Display.NonModal)
                {
                    curCrumb = null;

                    targetModeReached |= CurGameMode == curStep.TargetMode;

                    // HACK When the target mode is GamepadEditObject and the curretn mode is
                    // GamepadEditObjectFocus we give the user the instruction to move the cursor
                    // to a blank spot int he world.  This kind of sucks.  So detect that case
                    // and set reached to true for either.
                    if (curStep.TargetMode == GameMode.GamepadEditObject && CurGameMode == GameMode.GamepadEditObjectFocus)
                    {
                        targetModeReached = true;
                    }

                    // Check for completion.  If we have a completion test then use that
                    // as the criteria.  If not, we consider the step complete when the 
                    // target mode is reached.

                    if (curStep.CompletionTest == null)
                    {
                        if (targetModeReached)
                        {
                            OnContinue();
                        }
                    }
                    else
                    {
                        if (targetModeReached && curStep.CompletionTest.Evaluate())
                        {
                            OnContinue();
                        }
                    }

                    // Hack.  Right now the code is set up to latch targetModeReached until that step is 
                    // completed.  This behavior is required for some things to work correctly but this 
                    // also causes a problem; when the user moves away from the target mode they no
                    // longer get instructions on how to get to the target mode.  So hack in special
                    // case code for times where this a problem.
                    if ((targetModeReached && CurGameMode != curStep.TargetMode) &&
                        (
                            curStep.TargetMode == GameMode.Programming ||
                            curStep.TargetMode == GameMode.AddItem
                        ))
                    {
                        targetModeReached = false;
                    }

                    // Note yet at the target mode?  Get next crumb.
                    if (!targetModeReached)
                    {
                        curCrumb = FindNextCrumb(curStep.TargetMode);
                        //Debug.Assert(curCrumb != null, "Don't know how to get there from here...");
                    }

                    // Swallow any mouse clicks just in case user thinks to click on display.
                    // TODO (****) With changes, mouse hits will never be outside of active screen
                    // so this should never happen...
                    /*
                    if (GamePadInput.ActiveMode == GamePadInput.InputMode.KeyboardMouse)
                    {
                        Vector2 mouseHit = new Vector2(MouseInput.Position.X, MouseInput.Position.Y);
                        if (MouseInput.Left.WasPressedOrRepeat)
                        {
                            if (backdropBox.Contains(mouseHit))
                            {
                                MouseInput.Left.ClearAllWasPressedState();
                                Foley.PlayNoBudget();
                            }
                        }
                        if (MouseInput.Right.WasPressedOrRepeat)
                        {
                            if (backdropBox.Contains(mouseHit))
                            {
                                MouseInput.Right.ClearAllWasPressedState();
                                Foley.PlayNoBudget();
                            }
                        }
                        if (MouseInput.Middle.WasPressedOrRepeat)
                        {
                            if (backdropBox.Contains(mouseHit))
                            {
                                MouseInput.Middle.ClearAllWasPressedState();
                                Foley.PlayNoBudget();
                            }
                        }
                    }
                    */
                }   // end if non-Modal

                // Find focus actor if needed.
                string focusActorName = curStep.TargetCharacter;
                if (string.IsNullOrEmpty(focusActorName))
                {
                    focusActor = null;
                }
                else
                {
                    // ALWAYS set the character to null since it may be destroyed any frame 
                    // and we don't want to reference a missing character.
                    focusActor = null;
                    if (focusActor == null || focusActor.DisplayNameNumber != focusActorName)
                    {
                        for (int i = 0; i < InGame.inGame.gameThingList.Count; i++)
                        {
                            GameActor actor = InGame.inGame.gameThingList[i] as GameActor;
                            if (actor != null && actor.DisplayNameNumber == focusActorName)
                            {
                                // Found it!
                                focusActor = actor;
                                break;
                            }
                        }
                    }
                }

            }   // end if active.

        }   // end of Update()

        private static bool CurStepExcluded()
        {
            bool excluded = false;
            excluded |= GamePadInput.ActiveMode == GamePadInput.InputMode.KeyboardMouse && (curStep.Exclusion == Step.Exclusions.Mouse || curStep.Exclusion == Step.Exclusions.MouseGamepad || curStep.Exclusion == Step.Exclusions.MouseTouch);
            excluded |= GamePadInput.ActiveMode == GamePadInput.InputMode.GamePad && (curStep.Exclusion == Step.Exclusions.Gamepad || curStep.Exclusion == Step.Exclusions.MouseGamepad || curStep.Exclusion == Step.Exclusions.TouchGamepad);
            excluded |= GamePadInput.ActiveMode == GamePadInput.InputMode.Touch && (curStep.Exclusion == Step.Exclusions.Touch || curStep.Exclusion == Step.Exclusions.MouseTouch || curStep.Exclusion == Step.Exclusions.TouchGamepad);

            return excluded;
        }

        private static void OnContinue()
        {
            do
            {
                // Continue to next step.
                ++curStepIndex;

                // Reset.
                targetModeReached = false;

                // Check if we're done.
                if (InGame.XmlWorldData.tutorialSteps.Count <= curStepIndex)
                {
                    Deactivate();
                    break;
                }
                curStep = InGame.XmlWorldData.tutorialSteps[curStepIndex];
            } while (CurStepExcluded());

            Foley.PlayHeal(null);

        }   // end of OnContinue()

        private static void OnBack()
        {
            // Go back to most recent modal step.
            while (curStepIndex > 0)
            {
                --curStepIndex;

                // Found the previous modal step?
                if (InGame.XmlWorldData.tutorialSteps[curStepIndex].DisplayMode == Step.Display.Modal)
                {
                    break;
                }
            }

        }   // end of OnBack()

        private static void OnExitTutorial()
        {
            // Exit tutorial mode.
            Deactivate();
        }   // end of OnExitTutorial()

        public static void PreRender()
        {
            // Decide which font to use based on screen width.
            UI2D.Shared.GetFont prevFont = font;
            font = BokuGame.ScreenSize.X > 1280 ? UI2D.Shared.GetGameFont24Bold : UI2D.Shared.GetGameFont18Bold;

            // Did the font or window size change?  If so, reallocate the rendertarget.
            if (font != prevFont || rt == null || BokuGame.ScreenSize.X > rt.Width)
            {
                InGame.RelRT("TutorialRT", rt);
                BokuGame.Release(ref rt);
                CreateRenderTarget();
            }

            if (backdrop == null || rt == null)
            {
                return;
            }

            ScreenSpaceQuad quad = ScreenSpaceQuad.GetInstance();

            InGame.SetRenderTarget(rt);

            // Clear and add highlight.
            quad.Render(backdrop, Vector2.Zero, new Vector2(rt.Width, rt.Height), "TexturedRegularAlpha");

            if (Active)
            {
                int indent = 10;

                // Lazy allocation.
                if (titleBlob == null)
                {
                    titleBlob = new TextBlob(font, "test", (int)(rt.Width - 2.0f * indent));
                    //titleBlob.ProgrammingTileBackdrop = true;
                }
                if (instructionBlob == null)
                {
                    instructionBlob = new TextBlob(font, "test", (int)(rt.Width - 2.0f * indent));
                    //instructionBlob.ProgrammingTileBackdrop = true;
                }

                // Font may have changed, keep the blobs up to date.
                titleBlob.Font = font;
                instructionBlob.Font = font;

                // Raw strings to put into blobs.
                string titleStr = null;
                string instructionStr = null;

                // We only care about the non-modal text if there's no modal display.
                // TODO We could think about displaying this under the modal display but we'd have to 
                // add a drop shadow first to avoid cluttering things up.
                if (!modalDisplay.Active)
                {
                    // We should only display text when the tutorial mode is fully open.
                    bool display = targetPositionY > 1 && BokuGame.ScreenPosition.Y > targetPositionY - 2.0f;
                    if (display)
                    {
                        if (curCrumb != null || targetModeReached)
                        {
                            // First line should be the goal of this section of the tutorial.  High level.
                            titleStr = curStep.GoalText;

                            // Second line is either from the crumb telling us where to go OR from the step telling us what to do now that we're here.
                            if (curCrumb == null)
                            {
                                if (GamePadInput.ActiveMode == GamePadInput.InputMode.KeyboardMouse)
                                {
                                    instructionStr = curStep.MouseText;
                                }
                                else if (GamePadInput.ActiveMode == GamePadInput.InputMode.Touch)
                                {
                                    instructionStr = curStep.TouchText;
                                }
                                else    // gamepad
                                {
                                    instructionStr = curStep.GamepadText;
                                }
                            }
                            else
                            {
                                if (GamePadInput.ActiveMode == GamePadInput.InputMode.KeyboardMouse)
                                {
                                    instructionStr = curCrumb.MouseText;
                                }
                                else if (GamePadInput.ActiveMode == GamePadInput.InputMode.Touch)
                                {
                                    instructionStr = curCrumb.TouchText;
                                }
                                else    // gamepad
                                {
                                    instructionStr = curCrumb.GamepadText;
                                }
                            }
#if DEBUG
                            // Add in some debug info.
                            //instructionStr += "\nCurMode = " + curGameMode.ToString() + "   curTargetMode = " + curStep.TargetMode.ToString();
#endif
                        }
                        else
                        {
                            if (DebugMode)
                            {
                                // We've got no crumb.  Need to add one!
                                instructionStr = "Missing Crumb!";
                                // Add in some debug info.
                                instructionStr += "\nCurMode = " + curGameMode.ToString() + "   HelpOverlay = " + HelpOverlay.Peek() + "\nUpdateMode = " + InGame.inGame.CurrentUpdateMode.ToString();
                                if (curStep != null)
                                {
                                    instructionStr += "\nTargetMode = " + curStep.TargetMode;
                                }
                            }
                        }

                        // Render text blob.

                        // TODO Center text vertically and if fewer lines, increase the spacing a bit.

                        titleBlob.RawText = titleStr;
                        instructionBlob.RawText = instructionStr;

                        //Color titleColor = new Color(50, 255, 50);  // Same green as the hover text color we user elsewhere.
                        Color titleColor = new Color(20, 20, 20);   // Not quite black.
                        //Color titleColor = new Color(250, 190, 50); // Amber.
                        Color shadowColor = new Color(0, 0, 0, 40);
                        Color lightGrey = new Color(200, 200, 200);
                        Color darkGrey = new Color(100, 100, 100);

                        Color textColor = darkGrey;
                        if (DebugMode && curGameMode == GameMode.Unknown)
                        {
                            textColor = Color.Red;
                        }
                        titleBlob.RenderWithButtons(new Vector2(indent, 0), titleColor, shadowColor, new Vector2(0, 2), maxLines: 4);
                        // Vertically center the instruction text.
                        int yOffset = 0;
                        if (instructionBlob.NumLines == 1)
                        {
                            yOffset = instructionBlob.TotalSpacing;
                        }
                        else if (instructionBlob.NumLines == 2)
                        {
                            yOffset = (int)(instructionBlob.TotalSpacing / 2.0f);
                        }
                        instructionBlob.RenderWithButtons(new Vector2(indent, titleBlob.TotalSpacing + yOffset - 2), textColor, shadowColor, new Vector2(0, 2), maxLines: 4);

                    }   // end if display true

                }   // end if not modal active
            }   // end if tutorial mode active

            InGame.RestoreRenderTarget();
        
        }   // end of PreRender()

        public static void Render()
        {
            if (!active || backdrop == null || rt == null)
            {
                return;
            }

            ScreenSpaceQuad quad = ScreenSpaceQuad.GetInstance();

            Vector2 size = new Vector2(rt.Width, rt.Height);
            backdropBox.Set(Vector2.Zero, size);

            // Tutorial info needs to go at top of "real" screen.
            InGame.RestoreViewportToFull();
            quad.Render(rt, Vector2.Zero, size, "TexturedNoAlpha");

            if (modalDisplay.Active)
            {
                quad.Render(dropShadow, Vector2.Zero, BokuGame.ScreenSize, "TexturedRegularAlpha");
            }
            modalDisplay.Render();

            // Display debug spew if active.
            if(DebugMode && active)
            {
                Vector2 pos = new Vector2(20, 400);
                SpriteBatch batch = UI2D.Shared.SpriteBatch;
                Color color = Color.Yellow;

                TextBlob blob = new TextBlob(UI2D.Shared.GetGameFont20, "", 1000);

                string text = "Tutorial Manager\n";

                try
                {
                    text += "  current game mode : " + curGameMode.ToString() + "\n";
                    text += "  current help overlay : " + HelpOverlay.Peek() + "\n";
                    text += "  current step\n";
                    text += "    display mode : " + curStep.DisplayMode.ToString() + "\n";
                    text += "    target mode : " + curStep.TargetMode.ToString() + "\n";
                    if (curStep.CompletionTest != null)
                    {
                        text += "    completion test : " + curStep.CompletionTest.Name.ToString() + "\n";
                        text += "      args : " + curStep.CompletionTest.Args.ToString() + "\n";
                    }
                    text += "  current input mode : " + GamePadInput.ActiveMode.ToString() + "\n";
                    if (curCrumb != null)
                    {
                        text += "current crumb id " + curCrumb.id + "\n";
                    }
                }
                catch
                {
                }

                blob.RawText = text;

                blob.RenderWithButtons(pos, Color.Black, outlineColor: Color.White, outlineWidth: 1.2f);

            }

        }   // end of Render()

        /// <summary>
        /// Called to start the tutorial system.  We may need params later hence
        /// the use of a function rather than a write accessor on Active.
        /// Note that this may called on a thread other than the main thread so
        /// we don't do anything here with the graphics device.
        /// </summary>
        public static void Activate()
        {
            // Start at the beginning.
            curStepIndex = 0;

            if (!Active)
            {
                active = true;
            }

            // Trim tutorial text to eliminate and leading/trailing whitespace.
            if (InGame.XmlWorldData != null)
            {
                for (int i = 0; i < InGame.XmlWorldData.tutorialSteps.Count; i++)
                {
                    Step step = InGame.XmlWorldData.tutorialSteps[i];
                    step.GamepadText = CleanUpString(step.GamepadText);
                    step.MouseText = CleanUpString(step.MouseText);
                    step.TouchText = CleanUpString(step.TouchText);
                    step.GoalText = CleanUpString(step.GoalText);
                }
            }

        }   // end of Activate()

        /// <summary>
        /// Take the text input, breaks it into seperate lines, 
        /// trims those lines, converts any leading colons into
        /// spaces (1 colon == 4 spaces), and then recombines 
        /// the lines and returns the new string.
        /// Also takes null and returns an empty string.
        /// </summary>
        /// <param name="str"></param>
        /// <returns></returns>
        static string CleanUpString(string str)
        {
            if (str == null)
            {
                return string.Empty;
            }

            // Split into seperate lines.
            string[] strings = str.Split('\n');

            // Trim ends.
            for (int i = 0; i < strings.Length; i++)
            {
                strings[i] = strings[i].Trim();
            }

            // Expand colons.
            for (int i = 0; i < strings.Length; i++)
            {
                // Allow up to 3 levels of indent.
                if (strings[i].StartsWith(":::"))
                {
                    strings[i] = strings[i].Replace(":::", "            ");
                }
                else if (strings[i].StartsWith("::"))
                {
                    strings[i] = strings[i].Replace("::", "        ");
                }
                else if (strings[i].StartsWith(":"))
                {
                    strings[i] = strings[i].Replace(":", "    ");
                }
            }

            // Recombine result.
            str = "";
            for (int i = 0; i < strings.Length; i++)
            {
                str += strings[i];
                if (i < strings.Length - 1)
                {
                    str += '\n';
                }
            }

            return str;
        }   // end of CleanUpString()

        public static void Deactivate()
        {
            if(Active)
            {
                active = false;

                // Ensure that we're rendering to the whole screen.
                InGame.RestoreViewportToFull();
                BokuGame.ScreenSize = new Vector2(BokuGame.bokuGame.GraphicsDevice.Viewport.Width, BokuGame.bokuGame.GraphicsDevice.Viewport.Height);
                BokuGame.ScreenPosition = Vector2.Zero;
            }

        }   // end of Deactivate()

        /// <summary>
        /// Outputs the tutorial text to the given TextWriter.
        /// </summary>
        /// <param name="tw"></param>
        public static void Print(TextWriter tw)
        {
            if (InGame.XmlWorldData != null && InGame.XmlWorldData.tutorialSteps.Count > 0)
            {
                tw.WriteLine("\n=====================");
                tw.WriteLine("Tutorial Instructions");
                tw.WriteLine("=====================\n");

                string curGoal = null;
                foreach (Step step in InGame.XmlWorldData.tutorialSteps)
                {
                    if (!string.IsNullOrWhiteSpace(step.GoalText) && curGoal != step.GoalText)
                    {
                        curGoal = step.GoalText;
                        tw.WriteLine("\nGoal: " + curGoal);
                    }
                    if (!string.IsNullOrWhiteSpace(step.MouseText))
                    {
                        tw.WriteLine("      " + step.MouseText);
                    }
                }
            }

        }   // end of Print()

        #endregion

        #region Internal

        /// <summary>
        /// Given the targetMode, find the next crumb that will lead us from
        /// our current mode to the target.
        /// Returns null if no valid Crumb.
        /// </summary>
        /// <param name="targetMode"></param>
        /// <returns></returns>
        private static Crumb FindNextCrumb(GameMode targetMode)
        {
            crumbList.Clear();

            // Pick which list to use basedon current input mode.
            List<Crumb> crumbs = crumbListMouse;
            if (GamePadInput.ActiveMode == GamePadInput.InputMode.Touch)
            {
                crumbs = crumbListTouch;
            }
            else if (GamePadInput.ActiveMode == GamePadInput.InputMode.GamePad)
            {
                crumbs = crumbListGamepad;
            } 

            // Start with the current mode.
            crumbList.Add(new CrumbNode(null, curGameMode, 0));

            int cur = 0;

            do
            {
                // For each crumb.
                for (int i = 0; i < crumbs.Count; i++)
                {
                    Crumb crumb = crumbs[i];
                    for (int j = 0; j < crumb.CurModes.Length; j++)
                    {
                        // Is this crumb a match?
                        if (crumbList[cur].mode == crumb.CurModes[j])
                        {
                            // See if this crumb's target is already in the list, if not, don't add it.
                            bool needToAdd = true;
                            for (int k = 0; k < crumbList.Count; k++)
                            {
                                if (crumbList[k].mode == crumb.targetMode)
                                {
                                    needToAdd = false;
                                    break;
                                }
                            }

                            if (needToAdd)
                            {
                                crumbList.Add(new CrumbNode(crumb, crumb.targetMode, cur));

                                // Are we done?
                                if (crumb.targetMode == targetMode)
                                {
                                    // Work backwards from what we just added and find the mode
                                    // along the path that has the current mode as its parent.
                                    cur = crumbList.Count - 1;
                                    while(crumbList[cur].parentIndex != 0)
                                    {
                                        cur = crumbList[cur].parentIndex;
                                    }

                                    // Found it!
                                    return crumbList[cur].crumb;
                                }
                            }

                            break;
                        }
                    }
                }

                ++cur;
            } while (cur < crumbList.Count);

            // No solution found, return null.
            return null;

        }   // end of FindNextCrumb()

#if DEBUG
        private static bool test = true;
        private static void TestFindNextCrumb()
        {
            // Only run this once.
            if (test)
                test = false;
            else
                return;

            for (int i = 1; i < (int)GameMode.LAST_MODE; i++)
            {
                for (int j = 1; j < (int)GameMode.LAST_MODE; j++)
                {
                    TutorialManager.curGameMode = (GameMode)i;
                    GameMode target = (GameMode)j;
                    Crumb crumb = FindNextCrumb(target);

                    if (i == j && crumb == null)
                    {
                        // Not interesting...
                        continue;
                    }

                    /*
                    if (i == 13 || j == 13 || i == 42 || j == 42 || i == 43 || j == 43 || i == 44 || j == 44)
                    {
                        continue;
                    }
                    */

#if !NETFX_CORE
                    Debug.Print("cur " + TutorialManager.curGameMode.ToString() + " -> " + target.ToString());
#endif
                    if(crumb==null)
                    {
#if !NETFX_CORE
                        Debug.Print("  null crumb");
#endif
                    }
                    else
                    {
#if !NETFX_CORE
                        Debug.Print("  crumb target " + crumb.targetMode.ToString());
#endif
                    }
                    crumb = null;
                }
            }
        }
#endif

        public static void LoadContent(bool immediate)
        {
            if (backdrop == null)
            {
                //backdrop = BokuGame.Load<Texture2D>(BokuGame.Settings.MediaPath + @"Textures\GridElements\TutorialTitle");
                backdrop = BokuGame.Load<Texture2D>(BokuGame.Settings.MediaPath + @"Textures\GridElements\CheckboxWhite");
                //backdrop = BokuGame.Load<Texture2D>(BokuGame.Settings.MediaPath + @"Textures\HelpCard\BlackHighlight");
            }

            if (dropShadow == null)
            {
                dropShadow = BokuGame.Load<Texture2D>(BokuGame.Settings.MediaPath + @"Textures\GridElements\DropShadow");
            }

            CreateRenderTarget();

            if (crumbsArray == null)
            {
                crumbsArray = Crumb.Init();

                crumbListMouse = new List<Crumb>(crumbsArray.Length);
                crumbListTouch = new List<Crumb>(crumbsArray.Length);
                crumbListGamepad = new List<Crumb>(crumbsArray.Length);

                for (int i = 0; i < crumbsArray.Length; i++)
                {
                    crumbListMouse.Add(crumbsArray[i]);
                    crumbListTouch.Add(crumbsArray[i]);
                    crumbListGamepad.Add(crumbsArray[i]);
                }

                // For each list, pull the input specific crumbs to the top.
                int top = 0;
                for (int i = 0; i < crumbsArray.Length; i++)
                {
                    if (crumbListMouse[i].TargetMode.ToString().Contains("Mouse"))
                    {
                        // Swap with top.
                        Crumb tmp = crumbListMouse[top];
                        crumbListMouse[top] = crumbListMouse[i];
                        crumbListMouse[i] = tmp;

                        ++top;
                    }
                }
                top = 0;
                for (int i = 0; i < crumbsArray.Length; i++)
                {
                    if (crumbListTouch[i].TargetMode.ToString().Contains("Touch"))
                    {
                        // Swap with top.
                        Crumb tmp = crumbListTouch[top];
                        crumbListTouch[top] = crumbListTouch[i];
                        crumbListTouch[i] = tmp;

                        ++top;
                    }
                }
                top = 0;
                for (int i = 0; i < crumbsArray.Length; i++)
                {
                    if (crumbListGamepad[i].TargetMode.ToString().Contains("Gamepad"))
                    {
                        // Swap with top.
                        Crumb tmp = crumbListGamepad[top];
                        crumbListGamepad[top] = crumbListGamepad[i];
                        crumbListGamepad[i] = tmp;

                        ++top;
                    }
                }
            }

            if (modalDisplay != null)
            {
                modalDisplay.LoadContent(immediate);
            }

        }   // end of LoadContent()

        public static void InitDeviceResources(GraphicsDevice device)
        {
            CreateRenderTarget();

            modalDisplay.InitDeviceResources(device);
        }   // end of InitDeviceResources()

        public static void UnloadContent()
        {
            InGame.RelRT("TutorialRT", rt);
            BokuGame.Release(ref rt);

            BokuGame.Release(ref backdrop);
            BokuGame.Release(ref dropShadow);

            if (modalDisplay != null)
            {
                modalDisplay.UnloadContent();
            }
        }   // end of UnloadContent()

        public static void DeviceReset(GraphicsDevice device)
        {
            // Recreate rendertarget.
            InGame.RelRT("TutorialRT", rt);
            BokuGame.Release(ref rt);

            CreateRenderTarget();

            modalDisplay.DeviceReset(device);
        }   // end of DeviceReset()

        private static void CreateRenderTarget()
        {
            if (rt == null)
            {
                // Calc rt height to allow for 4 lines of text using our font of choice.
                // Calc rt width to match screen ratio.
                
                int height = 4 * font().LineSpacing;
                // int width = (int)(height * BokuGame.bokuGame.GraphicsDevice.Viewport.Width / (float)BokuGame.bokuGame.GraphicsDevice.Viewport.Height / (1.0f - kScreenFraction));
                //width = (int)MathHelper.Min(width, BokuGame.bokuGame.GraphicsDevice.Viewport.Width);
                int width = (int)BokuGame.ScreenSize.X;

                rt = new RenderTarget2D(
                    BokuGame.bokuGame.GraphicsDevice,
                    width, height, false,
                    SurfaceFormat.Color,
                    DepthFormat.None);
                InGame.GetRT("TutorialRT", rt);
            }
        }   // end of CreateRenderTarget()

        #endregion

    }   // end of class TutorialManager

}   // end of namespace Boku.Common.TutorialSystem
