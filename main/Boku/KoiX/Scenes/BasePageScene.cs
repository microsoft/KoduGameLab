
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

namespace KoiX.Scenes
{
    /// <summary>
    /// Base class for Scenes which have a back button and page tab buttons.
    /// </summary>
    public class BasePageScene : BaseScene
    {
        #region Members

        protected WidgetSet upperLeftWidgetSet;     // Set for holding widgets in the upper left hand corner.
        protected BackButton backButton;

        protected WidgetSet lowerRightWidgetSet;    // Set for holding widgets in the lower right hand corner.
        protected NextButton nextButton;

        protected WidgetSet lowerLeftWidgetSet;     // Set for holding widgets in the lower right hand corner.
        protected PrevButton prevButton;

        protected SpriteCamera camera;              // Camera for the content on this page.  The back button and next and prev tabs
                                                    // are contained by the default FullScreenDialog and as such, use the default
                                                    // DialogManager camera.
        protected FullScreenDialog fullScreenContentDialog; // Used for the content of this page.  Will scale as windows size is changed.


        #endregion

        #region Accessors

        /// <summary>
        /// The scene which is switched to when the back button is pressed.
        /// </summary>
        public string BackTargetScene
        {
            get { return backButton.TargetScene; }
            set { backButton.TargetScene = value; }
        }

        /// <summary>
        /// The scene which is switched to when the next button is pressed.
        /// </summary>
        public string NextTargetScene
        {
            get { return nextButton.TargetScene; }
            set { nextButton.TargetScene = value; }
        }

        /// <summary>
        /// The scene which is switched to when the prev button is pressed.
        /// </summary>
        public string PrevTargetScene
        {
            get { return prevButton.TargetScene; }
            set { prevButton.TargetScene = value; }
        }

        #endregion

        #region Public

        // c'tor
        public BasePageScene(string sceneName, string nextLabelId = null, string nextLabelText = null, string prevLabelId = null, string prevLabelText = null)
            : base(sceneName)
        {
            backButton = new BackButton(fullScreenDialog, new RectangleF());
            upperLeftWidgetSet = new WidgetSet(fullScreenDialog, new RectangleF(), Orientation.Vertical, horizontalJustification: Justification.Left, verticalJustification: Justification.Top);
            upperLeftWidgetSet.FitToParentDialog = true;
            upperLeftWidgetSet.AddWidget(backButton);
            fullScreenDialog.AddWidget(upperLeftWidgetSet);

            nextButton = new NextButton(fullScreenDialog, new RectangleF(), labelId: nextLabelId, labelText: nextLabelText);
            nextButton.Transition = SceneManager.Transition.SlideInFromRight;
            lowerRightWidgetSet = new WidgetSet(fullScreenDialog, new RectangleF(), Orientation.Vertical, horizontalJustification: Justification.Right, verticalJustification: Justification.Bottom);
            lowerRightWidgetSet.FitToParentDialog = true;
            lowerRightWidgetSet.AddWidget(nextButton);
            fullScreenDialog.AddWidget(lowerRightWidgetSet);

            prevButton = new PrevButton(fullScreenDialog, new RectangleF(), labelId: prevLabelId, labelText: prevLabelText);
            prevButton.Transition = SceneManager.Transition.SlideInFromLeft;
            lowerLeftWidgetSet = new WidgetSet(fullScreenDialog, new RectangleF(), Orientation.Vertical, horizontalJustification: Justification.Left, verticalJustification: Justification.Bottom);
            lowerLeftWidgetSet.FitToParentDialog = true;
            lowerLeftWidgetSet.AddWidget(prevButton);
            fullScreenDialog.AddWidget(lowerLeftWidgetSet);

            fullScreenContentDialog = new FullScreenDialog(focusable: true);
            fullScreenContentDialog.IgnoreGamepadBackButton = true;

#if DEBUG
            fullScreenContentDialog._name = "FullScreenContentDialog : " + sceneName;
#endif

            camera = new SpriteCamera();
            fullScreenDialogCamera = new SpriteCamera();

        }   // end of c'tor

        public override void Update()
        {
            if (Active)
            {
                // Set UI camera to standard position/zoom to match current resolution.
                SetCameraToTargetResolution(camera);
                camera.Update();

                // Adjust fullScreenDialogCamera to match content camera zoom.
                // This allows the Back, Next, and Prev buttons to scale along
                // with teh rest of the content in the scene.
                fullScreenDialogCamera.Zoom = camera.Zoom;
                fullScreenDialogCamera.Update();

                // Disable buttons if they don't have valid targets.
                if (!nextButton.HasValidTarget)
                {
                    nextButton.Disable();
                }
                if (!prevButton.HasValidTarget)
                {
                    prevButton.Disable();
                }

                Debug.Assert(backButton.HasValidTarget, "Back button should always have a valid target!");
            }

            base.Update();
        }   // end of Update()

        public override void RegisterForEvents()
        {
            base.RegisterForEvents();

            KoiLibrary.InputEventManager.RegisterForEvent(this, InputEventManager.Event.Keyboard);
        }

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

                // If we haven't explicitely given this scene a back target,
                // set the previous scene as the target.
                if (BackTargetScene == null)
                {
                    BackTargetScene = SceneManager.PreviousScene.Name;
                }

                DialogManagerX.ShowDialog(Boku.UI2D.AuthUI.StatusDialog, camera);

                base.Activate(args);

                // We need to give FullScreenContentDialog a higher priority for
                // input events than the default FullScreenDialog.  Since the
                // FullScreenDialog is Shown in the base.Activate call we need
                // to Show the content dialog after calling base.Activate.  This
                // ensures that the Content widgets are higher in the stack than
                // any widget on the regular FullScreenDialog.
                //
                // Note this is only really important if both dialogs have a 
                // widget that is looking for the same input.  Right now this 
                // happens with the Options pages.  We want to be able to tab
                // through the list of options (on the Content dialog) but the 
                // NextPage and PrevPage buttons (on the regular fullscreen dialog)
                // can also be activated by tab and shift-tab.  By ordering 
                // things this was the regular tabbing through the options works
                // and the next and prev buttons are blocked which is the
                // preferred behaviour.
                DialogManagerX.ShowDialog(fullScreenContentDialog, camera);
            }
        }   // endof Activate()

        public override void Deactivate()
        {
            DialogManagerX.KillDialog(Boku.UI2D.AuthUI.StatusDialog); 
            DialogManagerX.KillDialog(fullScreenContentDialog);

            base.Deactivate();
        }

        #endregion

        #region InputEventHandler

        public override bool ProcessKeyboardEvent(KeyInput input)
        {
            Debug.Assert(Active);
            if (input.Key == Keys.Escape)
            {
                backButton.OnButtonSelect();
                return true;
            }

            if (input.Key == Keys.Tab)
            {
                if (input.Shift)
                {
                    prevButton.OnButtonSelect();
                }
                else
                {
                    nextButton.OnButtonSelect();
                }

                return true;
            }
            
            return base.ProcessKeyboardEvent(input);
        }

        #endregion

        #region Internal

        public override void LoadContent()
        {
            fullScreenContentDialog.LoadContent();
            backButton.LoadContent();

            base.LoadContent();
        }

        public override void UnloadContent()
        {
            fullScreenContentDialog.UnloadContent();
            backButton.UnloadContent();

            base.UnloadContent();
        }

        public override void DeviceResetHandler(object sender, EventArgs e)
        {
            fullScreenContentDialog.DeviceResetHandler(sender, e);

            base.DeviceResetHandler(sender, e);
        }

        #endregion


    }   // end of class BasePageScene

}   // end of namespace KoiX.Scenes
