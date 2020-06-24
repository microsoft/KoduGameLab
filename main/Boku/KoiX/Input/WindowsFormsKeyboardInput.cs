
using System;
using System.Collections.Generic;
using System.Diagnostics;

#if WINDOWS
using System.Windows.Forms;
#endif

using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Audio;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using Microsoft.Xna.Framework.Storage;
using Microsoft.Xna.Framework.Content;

using KoiX;

namespace KoiX.Input
{
    public static class WinFormsKeyboardInput
    {
        #region Members

        static Form mainForm = null;
        static List<KeyInput> inputs = null;

        #endregion

        #region Accessors
        #endregion

        #region Public

        public static void Init(Form form)
        {
            mainForm = form;
            if (mainForm != null)
            {
                inputs = new List<KeyInput>();

                mainForm.KeyPreview = true;
                mainForm.KeyPress += new KeyPressEventHandler(KeyPressHandler);
                //mainForm.PreviewKeyDown += new PreviewKeyDownEventHandler(PreviewKeyDownHandler);
            }
        }   // end of Init()

        /// <summary>
        /// Forms based keyboard update.  Basically all this does is to
        /// forward any input keys on the to EventManager and then clear
        /// the list.
        /// </summary>
        /// <returns></returns>
        public static bool Update()
        {
            if (mainForm != null)
            {
                if (inputs.Count > 0)
                {
                    for (int i = 0; i < inputs.Count; i++)
                    {
                        KoiLibrary.InputEventManager.ProcessWinFormsKeyboardEvent(inputs[i]);
                    }

                    inputs.Clear();

                    return true;
                }
            }
            return false;
        }   // end of Update()

        #endregion

        #region Internal

        /// <summary>
        /// Forms based keyboard handler.  Gathers inputs and puts into a list.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        static void KeyPressHandler(object sender, KeyPressEventArgs e)
        {
            if (e != null)
            {
                KeyInput input = new KeyInput(Time.WallClockTotalSeconds, e.KeyChar);
                inputs.Add(input);
            }
        }

        /// <summary>
        /// Peeks at the key and sets IsInputKey to true for arrow keys.  Otherwise they get swallowed.
        /// TODO (scoy) Doesn't even get called.  Maybe because XNAControl is in focus, not the form???
        /// Note, try to get this keyboard handler to deal with arrow (and other specials) keys with autorepeat.
        /// Might be easier to implement autorepeat on other keyboard handler.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        static void PreviewKeyDownHandler(object sender, PreviewKeyDownEventArgs e)
        {
            if (e != null)
            {
                e.IsInputKey = true;
            }
        }

        #endregion

    }   // end of class WinFormsKeyboardInput

}   // end of namespace KoiX.Input
