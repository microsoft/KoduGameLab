
using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Net;

using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Audio;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using Microsoft.Xna.Framework.Storage;


using Boku.Audio;
using Boku.Base;
using Boku.Common;
using Boku.Common.Sharing;
using Boku.Common.Xml;
using Boku.Fx;
using Boku.Programming;
using Boku.UI;
using Boku.UI2D;
using Boku.Input;
using Boku.Scenes;
using Boku.SimWorld;
using Boku.Web;

using BokuShared;
using BokuShared.Wire;

namespace Boku
{
    /// <summary>
    /// In game loading and saving menu.  Should appear rendered
    /// over the top of the in-game level.
    /// </summary>
    public class MiniHub : GameObject, INeedsDeviceReset
    {
        public static MiniHub Instance = null;

        public static string emptyWorldFileName = @"03a1b038-fd3f-492f-b18c-2a197fe68701.Xml";

        private Texture2D homeTexture = null;

        public NewWorldDialog newWorldDialog;
        
        protected class Shared : INeedsDeviceReset
        {
            public Camera camera = new PerspectiveUICamera();

            public ModularMenu menu = null;

            public Matrix worldMatrix = Matrix.Identity;

            public Texture2D inGameImage = null;      // This is the same image as above but with more filtering.  This
                                                    // is used as a backdrop to the mini-hub.

            public CommunityShareMenu communityShareMenu = new CommunityShareMenu();
            // c'tor
            public Shared(MiniHub parent)
            {
                // Create text elements.
                // Start with a blob of common parameters.
                UIGridElement.ParamBlob blob = new UIGridElement.ParamBlob();
                blob.width = 5.0f;
                blob.height = 0.75f;
                blob.edgeSize = 0.06f;
                blob.Font = UI2D.Shared.GetGameFont30Bold;
                blob.textColor = Color.White;
                blob.dropShadowColor = Color.Black;
                blob.useDropShadow = true;
                blob.invertDropShadow = false;
                blob.unselectedColor = new Color(new Vector3(4, 100, 90) / 255.0f);
                blob.selectedColor = new Color(new Vector3(5, 180, 160) / 255.0f);
                blob.normalMapName = @"Slant0Smoothed5NormalMap";
                blob.justify = UIGrid2DTextElement.Justification.Center;

                menu = new ModularMenu(blob, Strings.Localize("miniHub.minihub"));
                menu.OnChange = parent.OnChange;
                menu.OnCancel = parent.OnCancel;
                menu.OnSelect = parent.OnSelect;
                menu.WorldMatrix = Matrix.CreateScale(1.4f);
                //menu.AcceptStartForCancel = true;
                menu.UseRtCoords = false;
                menu.HelpOverlay = "MiniHub";

                BuildMenu();
            }

            public void BuildMenu()
            {
                menu.DeleteAll();

                menu.AddText(Strings.Localize("miniHub.reset"));
                menu.AddText(Strings.Localize("miniHub.edit"));
                menu.AddText(Strings.Localize("miniHub.save"));
                menu.AddText(Strings.Localize("miniHub.publish"));
                menu.AddText(Strings.Localize("miniHub.load"));
                menu.AddText(Strings.Localize("miniHub.emptyLevel"));

#if NETFX_CORE
                // Disable printing since WinRT doesn't support just sending a text file to the printer.
#else
                menu.AddText(Strings.Localize("miniHub.print"));
#endif

                menu.AddText(Strings.Localize("miniHub.quit"));
            }   // end of BuildMenu()


            public void LoadContent(bool immediate)
            {
                BokuGame.Load(menu, immediate);
            }   // end of MiniHub Shared LoadContent()

            public void InitDeviceResources(GraphicsDevice device)
            {
                menu.InitDeviceResources(device);
            }

            public void UnloadContent()
            {
                BokuGame.Unload(menu);
            }   // end of MiniHub Shared UnloadContent()

            /// <summary>
            /// Recreate render targets
            /// </summary>
            /// <param name="graphics"></param>
            public void DeviceReset(GraphicsDevice device)
            {
                BokuGame.DeviceReset(menu, device);
            }

        }   // end of class MiniHub.Shared

        protected class UpdateObj : UpdateObject
        {
            private MiniHub parent = null;
            private Shared shared = null;

            public UpdateObj(MiniHub parent, Shared shared)
            {
                this.parent = parent;
                this.shared = shared;
            }

            public override void Update()
            {
                if (AuthUI.IsModalActive)
                {
                    return;
                }

                if (parent.newWorldDialog.Active)
                {
                    parent.newWorldDialog.Update();
                    return;
                }

                // We need to do this ever frame instead of just at activation 
                // time since deactivation of the previous scene and activation 
                // of this scene don't always happen in that order.
                AuthUI.ShowStatusDialog();

                parent.saveLevelDialog.Update();
                shared.communityShareMenu.Update();

                parent.saveChangesMessage.Update();
                parent.saveChangesWithDiscardMessage.Update();
                parent.shareSuccessMessage.Update();
                parent.noCommunityMessage.Update();

                // If any of the dialogs are active, we don't want to look for input.
                if (parent.saveLevelDialog.Active
                    || parent.saveChangesMessage.Active
                    || parent.saveChangesWithDiscardMessage.Active
                    || parent.shareSuccessMessage.Active
                    || parent.noCommunityMessage.Active
                    )
                {
                    return;
                }

                // Ensure camera matches screen.
                shared.camera.Resolution = new Point((int)BokuGame.ScreenSize.X, (int)BokuGame.ScreenSize.Y);
                shared.camera.Update();

                // Update menu.
                shared.menu.Update(shared.camera, ref shared.worldMatrix);

                // Ensure the help overlay is up to date.
                HelpOverlay.RefreshTexture();

            }   // end of MiniHub UpdateObj Update()

            void Callback_OpenMainMenu(AsyncOperation op)
            {
                if (shared.menu.SetValue(Strings.Localize("miniHub.quit")))
                {
                    parent.OnSelect(shared.menu);
                }
            }


            private void Callback_PutWorldData(AsyncResult result)
            {
                if (result.Success)
                    parent.shareSuccessMessage.Activate();
                else
                    parent.noCommunityMessage.Activate();
            }

            public override void Activate()
            {
                parent.noCommunityMessage.Deactivate();
                // Do this here so that it happens after the previous object
                // has had a chance to deactivate.
                HelpOverlay.Push("MiniHub");
            }

            public override void Deactivate()
            {
            }

        }   // end of class MiniHub UpdateObj  

        protected class RenderObj : RenderObject
        {
            private Shared shared;
            private MiniHub parent = null;

            public RenderObj(MiniHub parent, Shared shared)
            {
                this.shared = shared;
                this.parent = parent;
            }

            public override void Render(Camera camera)
            {
                if (parent.saveLevelDialog.Active)
                {
                    parent.saveLevelDialog.Render(null);
                }
                else
                {
                    ScreenSpaceQuad ssquad = ScreenSpaceQuad.GetInstance();

                    // This clear is not needed in desktop build but is required to clear z-buffer in WinRT version.
                    // TODO (****) Maybe issue is that z-test should be off for this?
                    InGame.Clear(new Color(20, 20, 20));

                    // Fill the background with the thumbnail if valid.
                    RenderTarget2D smallThumb = InGame.inGame.SmallThumbNail as RenderTarget2D;
                    if (smallThumb != null && !smallThumb.IsDisposed)
                    {
                        SpriteBatch batch = UI2D.Shared.SpriteBatch;
                        batch.Begin(SpriteSortMode.Deferred, BlendState.Opaque);
                        {
                            // Stretch thumbnail across whole screen.  Don't worry about distorting it
                            // since it's blurred anyway.
                            Microsoft.Xna.Framework.Rectangle dstRect = new Microsoft.Xna.Framework.Rectangle((int)BokuGame.ScreenPosition.X, (int)BokuGame.ScreenPosition.Y, (int)BokuGame.ScreenSize.X, (int)BokuGame.ScreenSize.Y);

                            Microsoft.Xna.Framework.Rectangle srcRect;
                            if (BokuGame.ScreenPosition.Y > 0)
                            {
                                // Set srcRect to ignore part of image at top of screen.
                                int y = (int)(BokuGame.ScreenPosition.Y * smallThumb.Height / (float)BokuGame.ScreenSize.Y);
                                srcRect = new Microsoft.Xna.Framework.Rectangle(0, y, smallThumb.Width, smallThumb.Height - y);
                            }
                            else
                            {
                                // Set srcRect to cover full thumbnail.
                                srcRect = new Microsoft.Xna.Framework.Rectangle(0, 0, smallThumb.Width, smallThumb.Height);
                            }
                            batch.Draw(smallThumb, dstRect, srcRect, Color.White);
                        }
                        batch.End();
                    }
                    else
                    {
                        // No thumbnail so just clear to black.
                        InGame.Clear(new Color(20, 20, 20));
                    }

                    InGame.SetViewportToScreen();

                    if (parent.newWorldDialog.Active)
                    {
                        // Hide the dialog if auth UI is active.  Just keeps things cleaner.
                        if (!AuthUI.IsModalActive)
                        {
                            // If options menu is active, render instead of main menu.
                            parent.newWorldDialog.Render(BokuGame.ScreenSize);
                        }
                    }
                    else
                    {
                        // Render menu using local camera.
                        shared.camera.Resolution = new Point((int)BokuGame.ScreenSize.X, (int)BokuGame.ScreenSize.Y);
                        shared.camera.Update();
                        Fx.ShaderGlobals.SetCamera(shared.camera);
                        shared.menu.Render(shared.camera);

                        {
                            CameraSpaceQuad quad = CameraSpaceQuad.GetInstance();
                            // Position Home icon on same baseline as menu title text.
                            // Assume uniform scaling.
                            float height = shared.menu.Height * shared.menu.WorldMatrix.M22;
                            float y = height / 2.0f - 0.18f;
                            quad.Render(shared.camera, parent.homeTexture, new Vector2(-2.4f, y), new Vector2(1.2f, 1.2f), "TexturedRegularAlpha");
                        }

                        // Render sign out button if user is logged in.
                        Vector2 screenSize = BokuGame.ScreenSize;

                        // Messages will only render if active.
                        parent.saveChangesMessage.Render();
                        parent.saveChangesWithDiscardMessage.Render();
                        parent.shareSuccessMessage.Render();
                        parent.noCommunityMessage.Render();

                        shared.communityShareMenu.Render();

                        HelpOverlay.Render();
                    }
                }

            }   // end of Render()

            public override void Activate()
            {
            }

            public override void Deactivate()
            {
            }

        }   // end of class MiniHub RenderObj     



        // List objects.
        protected Shared shared = null;
        protected RenderObj renderObj = null;
        protected UpdateObj updateObj = null;

        private enum States
        {
            Inactive,
            Active,
        }
        private States state = States.Inactive;
        private States pendingState = States.Inactive;

        private CommandMap commandMap = new CommandMap("App.MiniHub");   // Placeholder for stack.

        private SaveLevelDialog saveLevelDialog = new SaveLevelDialog();
        private ModularMessageDialog saveChangesMessage = null;
        private ModularMessageDialog saveChangesWithDiscardMessage = null;
        private ModularMessageDialog shareSuccessMessage = null;
        private ModularMessageDialog noCommunityMessage = null;

        protected int selectionIndex = -1;

        private bool saveChangesActivated = false;

        #region Accessors
        public bool Active
        {
            get { return (state == States.Active); }
        }
        public bool PendingActive
        {
            get { return pendingState == States.Active; }
        }
        public Texture2D InGameImage
        {
            set { shared.inGameImage = value; }
        }
        #endregion

        // c'tor
        public MiniHub()
        {
            MiniHub.Instance = this;

            shared = new Shared(this);

            // Create the RenderObject and UpdateObject parts of this mode.
            updateObj = new UpdateObj(this, shared);
            renderObj = new RenderObj(this, shared);

            saveLevelDialog.OnButtonPressed += OnSaveLevelDialogButton;

            //
            // Set up SaveChangesDialogs
            //
            {
                ModularMessageDialog.ButtonHandler handlerA = delegate(ModularMessageDialog dialog)
                {
                    // User chose "save"

                    // Deactivate dialog.
                    dialog.Deactivate();

                    // Activate saveLevelDialog.
                    saveLevelDialog.Activate();
                };
                ModularMessageDialog.ButtonHandler handlerB = delegate(ModularMessageDialog dialog)
                {
                    // User chose "back"

                    // Deactivate dialog.
                    dialog.Deactivate();

                    // Clear the flag since the user backed out.  This way if they
                    // try again the save changes message will be displayed again.
                    saveChangesActivated = false;

                    // Reactivate the mini-hub grid.
                    shared.menu.Active = true;
                };
                ModularMessageDialog.ButtonHandler handlerX = delegate(ModularMessageDialog dialog)
                {
                    // User chose "discard"

                    // Deactivate self and go wherever we were going...
                    dialog.Deactivate();

                    // Deactivate mini-hub.
                    //Deactivate();

                    // We gave the user the opportunity to save changes and he chose 
                    // not to so call OnSelect() once more to get them on their way.
                    if (saveChangesActivated)
                    {
                        OnSelect(shared.menu);
                        saveChangesActivated = false;
                    }
                };
                saveChangesMessage = new ModularMessageDialog(
                    Strings.Localize("textDialog.saveChangesPrompt"),
                    handlerA, Strings.Localize("textDialog.save"),
                    handlerB, Strings.Localize("textDialog.back"),
                    null, null,
                    null, null
                    );
                saveChangesWithDiscardMessage = new ModularMessageDialog(
                    Strings.Localize("textDialog.saveChangesPrompt"),
                    handlerA, Strings.Localize("textDialog.save"),
                    handlerB, Strings.Localize("textDialog.back"),
                    handlerX, Strings.Localize("textDialog.discard"),
                    null, null
                    );
            }

            //
            // Set up ShareSuccessDialog
            //
            {
                ModularMessageDialog.ButtonHandler handlerB = delegate(ModularMessageDialog dialog)
                {
                    // User chose "back"

                    // Deactivate dialog.
                    dialog.Deactivate();

                    // Make sure grid is still active.
                    shared.menu.Active = true;
                };
                shareSuccessMessage = new ModularMessageDialog(
                    Strings.Localize("miniHub.shareSuccessMessage"),
                    null, null,
                    handlerB, Strings.Localize("textDialog.back"),
                    null, null,
                    null, null
                    );
            }

            //
            // Set up NoCommunityDialog
            //
            {
                ModularMessageDialog.ButtonHandler handlerB = delegate(ModularMessageDialog dialog)
                {
                    // User chose "back"

                    // Deactivate dialog.
                    dialog.Deactivate();

                    // Make sure grid is still active.
                    shared.menu.Active = true;
                };
                noCommunityMessage = new ModularMessageDialog(
                    Strings.Localize("miniHub.noCommunityMessage"),
                    null, null,
                    handlerB, Strings.Localize("textDialog.back"),
                    null, null,
                    null, null
                    );
            }

            //
            //  Set up NewWorld dialog.
            //
            NewWorldDialog.OnAction OnSelectWorld = delegate(string level)
            {
                // Deactivate main menu and go into editor with empty level.
                string levelFilename = Path.Combine(BokuGame.Settings.MediaPath, BokuGame.BuiltInWorldsPath, level + ".Xml");
                if (BokuGame.bokuGame.inGame.LoadLevelAndRun(levelFilename, keepPersistentScores: false, newWorld: true, andRun: false))
                {
                    Deactivate();
                    InGame.inGame.Activate();
                    InGame.inGame.CurrentUpdateMode = InGame.UpdateMode.ToolMenu;
                }
                else
                {
                    shared.menu.Active = true;
                }
            };
            NewWorldDialog.OnAction OnCancel = delegate(string level)
            {
                shared.menu.Active = true;
            };
            newWorldDialog = new NewWorldDialog(OnSelectWorld, OnCancel);

        }   // end of MiniHub c'tor

        /// <summary>
        /// Get called back by the SaveLevelDialog when the user makes a choice.
        /// </summary>
        /// <param name="dialog"></param>
        private void OnSaveLevelDialogButton(SaveLevelDialog dialog)
        {
            if (dialog.Button == SaveLevelDialog.SaveLevelDialogButtons.Cancel)
            {
                // The user backed out so reactivate the menu.
                shared.menu.Active = true;

                // Even if the SaveLevelDialog was triggered by the SaveChangesMessage
                // we don't care.  Back the user all the way out back to the mini-hub.
                saveChangesActivated = false;
            }

            if (dialog.Button == SaveLevelDialog.SaveLevelDialogButtons.Save)
            {
                // Done.  If this was caused by the SaveChanges dialog popping up then
                // we need to return to wherever the user was trying to go in the 
                // first place.  If this was caused by the user explicitely saving
                // then we should return to running.

                if (saveChangesActivated)
                {
                    // Now that the level has been saved, call OnSelect again
                    // so that we do whatever the user chose in the first place.
                    OnSelect(shared.menu);
                    saveChangesActivated = false;
                }
                else
                {
                    // Return to editing game.
                    Deactivate();
                    InGame.inGame.Activate();

                    // Force Day lighting since we're going into edit mode.
                    BokuGame.bokuGame.shaderGlobals.SetLightRig("Day");
                }
            }
        }   // end of OnSaveLevelDialogButton()

        /// <summary>
        /// OnSelect method used by mini-hub grid.  If the level is dirty and needs to 
        /// be saved the SaveChagesDialog will be activated.  Upon its deactivation 
        /// the level should no longer be marked dirty and OnSelect() will get called 
        /// again allowing the user's action to be executed.
        /// </summary>
        /// <param name="grid"></param>
        public void OnSelect(ModularMenu menu)
        {
            // Prevent the button pressed from leaking into runtime.
            GamePadInput.IgnoreUntilReleased(Buttons.A);

            // In every case, we need to reset the level to its starting state.
            InGame.inGame.ResetSim(preserveScores: false, removeCreatablesFromScene: false, keepPersistentScores: false);
            // Resetting the sim just started up all game audio, let's pause it down again.
            // It will be resumed when we go back into sim mode.
            BokuGame.Audio.PauseGameAudio();

            // Flag to let us know if the level needs saving.  If the save changes 
            // dialog has already been activated then just set this to false.
            bool needToSaveLevel = (InGame.IsLevelDirty || InGame.AutoSaved) && !saveChangesActivated;

            // Does the current world belong to the user.  Required to share to community.
            // Test the genre flag and also special case look at empty world.
            bool isMyWorld = false;
            if (InGame.XmlWorldData != null)
            {
                bool genreTest = ((int)InGame.XmlWorldData.genres & (int)Genres.MyWorlds) != 0;
                bool newWorldTest = InGame.XmlWorldData.Filename == emptyWorldFileName;
                if (genreTest && !newWorldTest)
                {
                    isMyWorld = true;
                }
            }

            // Normally there would be a switch here but if we compare strings 
            // we proof ourselves against changes in the order of the elements.
            if (menu.CurString == Strings.Localize("miniHub.reset"))
            {
                // Reset.
                // We've already done a Reset, so force to RunSim mode if we already aren't.
                Deactivate();
                InGame.inGame.Activate();
                InGame.inGame.CurrentUpdateMode = InGame.UpdateMode.RunSim;
                InGame.inGame.RestorePlayModeCamera();

                // The ResetSim above doesn't ApplyInlining since it's generally
                // meant for resetting into the editor.  In this case we're going
                // into RunSim mode so be sure to apply inlining first.
                InGame.ApplyInlining();

                if (InGame.inGame.PreGame != null)
                {
                    InGame.inGame.PreGame.Active = true;
                }
            }
            else if (menu.CurString == Strings.Localize("miniHub.edit"))
            {
                // Edit level.
                Deactivate();
                InGame.inGame.Activate();
                InGame.inGame.CurrentUpdateMode = InGame.UpdateMode.ToolMenu;
            }
            else if (menu.CurString == Strings.Localize("miniHub.save"))
            {
                // Save
                saveLevelDialog.Activate();
            }
            else if (menu.CurString == Strings.Localize("miniHub.publish"))
            {
                // Offer to save first.  Need to save if world has changed or is world doesn't belong to user.
                if (needToSaveLevel || !isMyWorld)
                {
                    saveChangesActivated = true;
                    saveChangesMessage.Activate();
                }
                else
                {
                    var level =LevelMetadata.CreateFromXml(InGame.XmlWorldData);

                    shared.communityShareMenu.Activate(level);
                }
            }
            else if (menu.CurString == Strings.Localize("miniHub.load"))
            {
                // Load.

                // If we're back here and saveChangesActivated is true then the
                // user was given the option to save changes and chose Discard.
                // So don't offer to save again.
                if (!saveChangesActivated && needToSaveLevel)
                {
                    saveChangesActivated = true;
                    saveChangesWithDiscardMessage.Activate();
                }
                else
                {
                    saveChangesActivated = false;

                    // Deactivate mini-hub and bring up loading menu.
                    Deactivate();
                    //InGame.inGame.DiscardTerrain();
                    BokuGame.bokuGame.loadLevelMenu.LocalLevelMode = LoadLevelMenu.LocalLevelModes.General;
                    BokuGame.bokuGame.loadLevelMenu.ReturnToMenu = LoadLevelMenu.ReturnTo.MiniHub;
                    BokuGame.bokuGame.loadLevelMenu.Activate();
                }
            }
            else if (menu.CurString == Strings.Localize("miniHub.emptyLevel"))
            {
                // Empty Level.
                // If saveChangesActivated is already true then user chose Discard and
                // we can ignore the needToSaveLevel flag.
                if (!saveChangesActivated && needToSaveLevel)
                {
                    saveChangesActivated = true;
                    saveChangesWithDiscardMessage.Activate();
                }
                else
                {
                    saveChangesActivated = false;

                    // Undo any previous warping.
                    ScreenWarp.FitRtToScreen(BokuGame.ScreenSize);

                    newWorldDialog.Active = true;
                }
            }
            else if (menu.CurString == Strings.Localize("miniHub.print"))
            {
                Print.PrintProgramming();

                // We don't want to exit the mini-hub so re-activate the menu.
                shared.menu.Active = true;
            }
            else if (menu.CurString == Strings.Localize("miniHub.quit"))
            {
                // Exit to main menu.
                // If we're back here and saveChangesActivated is true then the
                // user was given the option to save changes and chose Discard.
                // So don't offer to save again.
                if (!saveChangesActivated && needToSaveLevel)
                {
                    saveChangesActivated = true;
                    saveChangesWithDiscardMessage.Activate();
                }
                else
                {
                    saveChangesActivated = false;

                    // Wave bye, bye.  Go back to the main menu
                    Deactivate();
                    InGame.inGame.StopAllSounds();
                    BokuGame.bokuGame.mainMenu.Activate();
                }
            }

        }   // end of MiniHub OnSelect()

        public void OnChange(ModularMenu menu)
        {
            // Nothing to do here...
        }

        public void OnCancel(ModularMenu menu)
        {
            // Prevent the button pressed from leaking into runtime.
            GamePadInput.IgnoreUntilReleased(Buttons.B);

            // Never mind.  Just deactivate the mini hub and reactivate InGame.
            Deactivate();
            InGame.inGame.Activate();
        }

        public override bool Refresh(List<UpdateObject> updateList, List<RenderObject> renderList)
        {
            bool result = false;

            if (state != pendingState)
            {
                if (pendingState == States.Active)
                {
                    updateList.Add(updateObj);
                    updateObj.Activate();
                    renderList.Add(renderObj);
                    renderObj.Activate();

                    shared.menu.Active = true;

                    // If we were in first person mode, reset things as needed.  Need to
                    // do this here instead of in Activate since there we still need to
                    // render another frame.
                    if (CameraInfo.FirstPersonActive)
                    {
                        CameraInfo.FirstPersonActor.SetFirstPerson(false);
                        CameraInfo.ResetAllLists();
                        CameraInfo.Mode = CameraInfo.Modes.Edit;
                        InGame.inGame.Camera.FollowCameraDistance = 10.0f;
                    }

                }
                else
                {
                    shared.menu.Active = false;

                    renderObj.Deactivate();
                    renderList.Remove(renderObj);
                    updateObj.Deactivate();
                    updateList.Remove(updateObj);
                }

                state = pendingState;
            }

            return result;
        }
        private object timerInstrument = null;

        override public void Activate()
        {
            if (state != States.Active)
            {
                // Do stack handling here.  If we do it in the update object we have no
                // clue which order things get pushed and popped and madness ensues.
                CommandStack.Push(commandMap);

                pendingState = States.Active;
                BokuGame.objectListDirty = true;

                saveChangesActivated = false;

                HelpOverlay.ToolIcon = null;

                InGame.inGame.RenderWorldAsThumbnail = true;

                timerInstrument = Instrumentation.StartTimer(Instrumentation.TimerId.MiniHubTime);

                Foley.PlayMenuLoop();

                Time.Paused = true;

                AuthUI.ShowStatusDialog();
            }
        }

        override public void Deactivate()
        {
            if (state != States.Inactive && pendingState != States.Inactive)
            {
                // Do stack handling here.  If we do it in the update object we have no
                // clue which order things get pushed and popped and madness ensues.
                CommandStack.Pop(commandMap);
                HelpOverlay.Pop();
                shared.menu.Active = false;

                pendingState = States.Inactive;
                BokuGame.objectListDirty = true;

                InGame.inGame.RenderWorldAsThumbnail = false;

                Instrumentation.StopTimer(timerInstrument);

                Foley.StopMenuLoop();

                Time.Paused = false;

                AuthUI.HideAllDialogs();
            }
        }

        public void LoadContent(bool immediate)
        {
            BokuGame.Load(shared, immediate);
            BokuGame.Load(shareSuccessMessage, immediate);
            BokuGame.Load(noCommunityMessage, immediate);
            BokuGame.Load(saveChangesMessage, immediate);
            BokuGame.Load(saveChangesWithDiscardMessage, immediate);
            BokuGame.Load(saveLevelDialog, immediate);
            BokuGame.Load(newWorldDialog, immediate);

            homeTexture = BokuGame.Load<Texture2D>(BokuGame.Settings.MediaPath + @"Textures\ToolMenu\Home");
        }   // end of MiniHub LoadContent()

        public void InitDeviceResources(GraphicsDevice device)
        {
            saveLevelDialog.InitDeviceResources(device);
        }

        public void UnloadContent()
        {
            BokuGame.Unload(shared);
            BokuGame.Unload(shareSuccessMessage);
            BokuGame.Unload(noCommunityMessage);
            BokuGame.Unload(saveChangesMessage);
            BokuGame.Unload(saveChangesWithDiscardMessage);
            BokuGame.Unload(saveLevelDialog);
            BokuGame.Unload(newWorldDialog);

            BokuGame.Release(ref homeTexture);
        }   // end of MiniHub UnloadContent()

        /// <summary>
        /// Recreate render targets
        /// </summary>
        /// <param name="graphics"></param>
        public void DeviceReset(GraphicsDevice device)
        {
            BokuGame.DeviceReset(shared, device);
            BokuGame.DeviceReset(shareSuccessMessage, device);
            BokuGame.DeviceReset(noCommunityMessage, device);
            BokuGame.DeviceReset(saveChangesMessage, device);
            BokuGame.DeviceReset(saveChangesWithDiscardMessage, device);
            BokuGame.DeviceReset(saveLevelDialog, device);
        }

    }   // end of class MiniHub

}   // end of namespace Boku


