
using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;

using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Audio;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using Microsoft.Xna.Framework.Input.Touch;
using Microsoft.Xna.Framework.Storage;

using KoiX;
using KoiX.Input;
using KoiX.Managers;
using KoiX.Text;
using KoiX.UI;
using KoiX.UI.Dialogs;

using Boku;
using Boku.Common;

namespace KoiX.Scenes
{
    public class RunSimScene : BaseScene
    {
        #region Members

        WidgetSet upperLeftWidgetSet;   // Set for holding widgets in the upper left hand corner.
        BackButton backButton;          // Returns to edit mode.

        // TODO (scoy) Do we want to add GUI buttons here?

        #endregion

        #region Accessors
        #endregion

        #region Public

        // c'tor
        public RunSimScene()
            : base("RunSimScene")
        {
            backButton = new BackButton(fullScreenDialog, new RectangleF());
            backButton.TargetScene = "EditWorldScene";
            upperLeftWidgetSet = new WidgetSet(fullScreenDialog, new RectangleF(), Orientation.Vertical, horizontalJustification: Justification.Left, verticalJustification: Justification.Top);
            upperLeftWidgetSet.FitToParentDialog = true;
            upperLeftWidgetSet.AddWidget(backButton);
            fullScreenDialog.AddWidget(upperLeftWidgetSet);

            // Create a camera for the fullScreenDialog.  This will allow us
            // to resize the backbutton based on the screen resolution.
            fullScreenDialogCamera = new SpriteCamera();

        }   // end of c'tor

        public override void Update()
        {
            if (Active)
            {
                // Make sure we're in the correct mode.
                InGame.inGame.CurrentUpdateMode = InGame.UpdateMode.RunSim;

                // Adjust zoom to match any change in window size.
                SetCameraToTargetResolution(fullScreenDialogCamera);

                BokuGame.bokuGame.shaderGlobals.Update();
            }
        }   // end of Update()

        public override void Render(RenderTarget2D rt)
        {
            GraphicsDevice device = KoiLibrary.GraphicsDevice;

            if (rt != null)
            {
                device.SetRenderTarget(rt);
            }

            // Set up lighting.
            BokuGame.bokuGame.shaderGlobals.Render(InGame.inGame.Camera);

            // Render all the active objects.
            BokuGame.gameListManager.Render();

            if (rt != null)
            {
                device.SetRenderTarget(null);
            }
        }   // end of Render()

        public override void RegisterForEvents()
        {
            base.RegisterForEvents();

            KoiLibrary.InputEventManager.RegisterForEvent(this, InputEventManager.Event.Keyboard);
            KoiLibrary.InputEventManager.RegisterForEvent(this, InputEventManager.Event.GamePad);

        }   // end of RegisterForEvents()

        /// <summary>
        /// Activate this scene.
        /// </summary>
        /// <param name="args">optional argument list.  Most Scenes will not use one but for those cases where it's needed this is here.</param>
        /// <summary>
        /// Shouldn't be called by user code.  Is called by SceneManager when 
        /// switching scenes.
        /// </summary>
        public override void Activate(params object[] args)
        {
            if (!Active)
            {
                if (args != null)
                {
                    foreach (object arg in args)
                    {
                        // Do something with each arg...
                    }
                }

                InGame.inGame.Activate();
                InGame.inGame.CurrentUpdateMode = InGame.UpdateMode.RunSim;

                // We no longer need the help overlay for RunSim mode so
                // deactivate it here.  This will prevent it rendering.
                HelpOverlay.Active = false;

                base.Activate(args);
            }
        }   // end of default Activate()

        /// <summary>
        /// Shouldn't be called by user code.  Is called by SceneManager when 
        /// switching scenes.
        /// </summary>
        public override void Deactivate()
        {
            if (Active)
            {
                // Reactivate since we're most likely going back into edit world.
                HelpOverlay.Active = true;

                // Normally the toolbar selection is sticky.  But when coming back
                // from RunSim we want to to go to the default CameraMove tool.
                ToolBarDialog.ResetToCameraMove();

                base.Deactivate();
            }
        }   // end of default Deactivate()

        #endregion

        #region InputEventHandler

        public override bool ProcessKeyboardEvent(KeyInput input)
        {
            Debug.Assert(Active);
            if (input.Key == Keys.Escape || input.Key == Keys.Back)
            {
                backButton.OnButtonSelect();
                return true;
            }

            if (input.Key == Keys.Pause)
            {
                // Toggle paused state.
                Time.Paused = !Time.Paused;
                return true;
            }

            if (input.Key == Keys.Home)
            {
                SceneManager.SwitchToScene("HomeMenuScene", frameDelay: 1);
                // Refresh the thumbnail during our 1 frame delay.
                InGame.RefreshThumbnail = true;
                return true;
            }

            return base.ProcessKeyboardEvent(input);
        }   // end of ProcessKeyboardEvent()

        public override bool ProcessGamePadEvent(GamePadInput pad)
        {
            Debug.Assert(Active);

            if (Time.Paused)
            {
                if (pad.ButtonA.WasPressed)
                {
                    UnpauseGame();
                    return true;
                }
            }
            else
            {
                // We want to make pause hard to get into so it requires both triggers and both stickButtons to be pressed.
                if (pad.LeftStickButton.IsPressed && pad.RightStickButton.IsPressed && pad.LeftTriggerButton.IsPressed && pad.RightTriggerButton.IsPressed)
                {
                    PauseGame();
                    return true;
                }
            }

            if (pad.Back.WasPressed)
            {
                SceneManager.SwitchToScene("HomeMenuScene", frameDelay: 1);
                // Refresh the thumbnail during our 1 frame delay.
                InGame.RefreshThumbnail = true;
                return true;
            }

            return base.ProcessGamePadEvent(pad);
        }

        #endregion

        #region Internal

        void PauseGame()
        {
            Time.Paused = true;
            HelpOverlay.Push("PauseGame");
            GamePadInput.GetGamePad0().IgnoreLeftStickUntilZero();
            GamePadInput.GetGamePad0().IgnoreRightStickUntilZero();
        }   // end of PauseGame()

        void UnpauseGame()
        {
            Time.Paused = false;
            HelpOverlay.Pop();
        }   // end of UnpauseGame()

        public override void LoadContent()
        {
            base.LoadContent();
        }

        public override void UnloadContent()
        {
            base.UnloadContent();
        }

        #endregion


    }   // end of class RunSimScene

}   // end of namespace KoiX.Scenes
