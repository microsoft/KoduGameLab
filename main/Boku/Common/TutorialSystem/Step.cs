
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
using KoiX.Input;

using Boku.Base;
using Boku.Fx;
using Boku.Common;
using Boku.Common.Xml;

namespace Boku.Common.TutorialSystem
{
    /// <summary>
    /// A single step in a tutorial.  A full tutorial consists of a list of Steps.
    /// </summary>
    public class Step
    {
        public enum Display
        {
            Modal,      // Information is displayed (large window) to the user and the user must hit Continue to move on.
            NonModal,   // Information is displayed to the user at the top of the screen.  The tutorial moves on when
                        // the Step's completion condition is met.
            None,       // Should be an error.
        };

        /// <summary>
        /// Allows steps to be excluded depending on the current input mode.  By default no steps
        /// are excluded.
        /// </summary>
        public enum Exclusions
        {
            None,
            Mouse,
            Gamepad,
            Touch,
            MouseGamepad,   // Excludes both.
            MouseTouch,
            TouchGamepad,
        }

        #region Members

        /// <summary>
        /// Since XML comments don't persist over serialization/deserialization we have
        /// this comment string.  It's not used for anything internally.  It's just 
        /// there to help out people creating tutorials.
        /// </summary>
        private string comment = null;

        private Display displayMode = Display.None;
        private Exclusions exclusion = Exclusions.None;

        private TutorialManager.GameMode targetModeGamepad = TutorialManager.GameMode.MainMenu;
        private TutorialManager.GameMode targetModeMouse = TutorialManager.GameMode.MainMenu;
        private TutorialManager.GameMode targetModeTouch = TutorialManager.GameMode.MainMenu;

        private string goalText = null;
        private string gamepadText = null;
        private string mouseText = null;
        private string touchText = null;

        // Completion tests for non-modal steps.
        private CompletionTest completionTest = null;

        // Actor name to put an arrow over.
        private string targetCharacter = null;

        // TODO Need to add some completion criteria for non-modal steps.

        #endregion

        #region Accessors

        public string Comment
        {
            get { return comment; }
            set { comment = value; }
        }

        public Display DisplayMode
        {
            get { return displayMode; }
            set { displayMode = value; }
        }

        public Exclusions Exclusion
        {
            get { return exclusion; }
            set { exclusion = value; }
        }

        public TutorialManager.GameMode TargetModeGamepad
        {
            get { return targetModeGamepad; }
            set { targetModeGamepad = value; }
        }
        public TutorialManager.GameMode TargetModeMouse
        {
            get { return targetModeMouse; }
            set { targetModeMouse = value; }
        }

        public TutorialManager.GameMode TargetModeTouch
        {
            get { return targetModeTouch; }
            set { targetModeTouch = value; }
        }

        public string TargetCharacter
        {
            get { return targetCharacter; }
            set { targetCharacter = value; }
        }

        /// <summary>
        /// Returns the target mode for this step based on the current input mode.
        /// </summary>
        [XmlIgnore]
        public TutorialManager.GameMode TargetMode
        {
            get
            {
                if (KoiLibrary.LastTouchedDeviceIsKeyboardMouse)
                    return TargetModeMouse;
                if (KoiLibrary.LastTouchedDeviceIsTouch)
                    return TargetModeTouch;
                if (KoiLibrary.LastTouchedDeviceIsGamepad)
                    return TargetModeGamepad;
    
                // Default?
                return TargetModeGamepad;
            }
        }

        /// <summary>
        /// This is the text displayed on the first line of the non-modal dialog.
        /// </summary>
        public string GoalText
        {
            get { return goalText; }
            set { goalText = value; }
        }

        public string GamepadText
        {
            get { return gamepadText; }
            set { gamepadText = value; }
        }
        public string MouseText
        {
            get { return mouseText; }
            set { mouseText = value; }
        }
        public string TouchText
        {
            get { return touchText; }
            set { touchText = value; }
        }

        
        /// <summary>
        /// Test used for non-modal steps to determine when they've completed.
        /// </summary>
        public CompletionTest CompletionTest
        {
            get { return completionTest; }
            set { completionTest = value; }
        }
        
        #endregion

        #region Public
        #endregion

        #region Internal
        #endregion

    }   // end of class Step

}   // end of namespace Boku.Common.TutorialSystem
