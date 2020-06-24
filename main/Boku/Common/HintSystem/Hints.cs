
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

using Boku.Base;
using Boku.Fx;
using Boku.Common.Xml;
using Boku.Common.HintSystem;

namespace Boku.Common
{
    /// <summary>
    /// Provides support for the hints which are presented to users as a toast.  When active
    /// the user can either mouse click on the toast or press <Y> to bring up a modal dialog
    /// with more detailed help.  This modal dialog will also allow them to dismiss the hint
    /// with a "don't show me this again" option.  This value will be persisted in the 
    /// OptionsData.Xml file since that's where we store global settings.  
    /// </summary>
    public class Hints
    {
        #region Memembers

        /// <summary>
        /// List of all hints in the system.
        /// </summary>
        private static List<BaseHint> hints = null;

        private static BaseHint activeHint = null;

        private static ModalHint modalHint = new ModalHint();
        private static ScrollableModalHint scrollableModalHint = new ScrollableModalHint();

        private static PerspectiveUICamera camera = new PerspectiveUICamera();

        #endregion

        #region Accessors
        #endregion

        #region Public

        public static void Init()
        {
            ToastManager.Init();

            hints = new List<BaseHint>();

            //
            // Add all the hints here
            //
            hints.Add(new ResourceMeterHint());
            hints.Add(new FullResourceHint());
            hints.Add(new NoControllerHint());
            hints.Add(new QuietMainMenuHint());
            hints.Add(new WaterTerrainHint());
            hints.Add(new WaterNoTerrainHint());
            hints.Add(new GamepadNoFilterHint());
            hints.Add(new SwitchNoTargetHint());
            hints.Add(new NoTerrainToRaiseHint());
#if !NETFX_CORE
            hints.Add(new MicrobitNeedsResetHint());
#endif

            // Disable hints that the user has previously dismissed.
            // Yes this is n^2 but the lists should be short enough 
            // so you'll never notice.
            List<string> disList = XmlOptionsData.DisabledHintIDs;
            if (disList != null)
            {
                for(int i=0; i < disList.Count; i++)
                {
                    for (int j = 0; j < hints.Count; j++)
                    {
                        if (disList[i] == hints[j].ID)
                        {
                            hints[j].Disabled = true;
                        }
                    }
                }
            }

        }   // end of Init()

        public static void Update(bool allowNewHints)
        {
            if (!XmlOptionsData.ShowHints)
                return;

            // Don't bother to update hints if we've already got an active one.
            if(activeHint == null && allowNewHints)
            {
                for (int i = 0; i < hints.Count; i++)
                {
                    if (!hints[i].Disabled)
                    {
                        if (hints[i].Update())
                        {
                            activeHint = hints[i];
                            break;
                        }
                    }
                }

                // Did we activate one?
                if (activeHint != null)
                {
                    // If so, kick off the toast display.
                    ToastManager.ShowToast(activeHint.ToastText, activeHint.ModalText != null);
                }

            }

            // Update the displays.
            if (activeHint != null)
            {
                if (ToastManager.Update())
                {
                    if (activeHint.ModalText != null && allowNewHints)
                    {
                        modalHint.Activate(activeHint, true, useRtCoords: false);
                        if (modalHint.Overflow)
                        {
                            // Set up scrollable display instead.
                            modalHint.Deactivate();
                            scrollableModalHint.Activate(activeHint, true, useRtCoords: false);
                        }
                    }
                }
            }

            // Update modal displays.
            modalHint.Update(camera);
            scrollableModalHint.Update(camera);

            // If none of the displays are active, be sure activeHint is also null.
            if (!ToastManager.Active && !modalHint.Active && !scrollableModalHint.Active)
            {
                activeHint = null;
            }

        }   // end of Update()

        /// <summary>
        /// Renders the currently active hint, if any.  Note that the hint
        /// may be being displayed in either its toast or modal form.
        /// </summary>
        public static void Render()
        {
            if (!XmlOptionsData.ShowHints)
                return;

            modalHint.Render();
            scrollableModalHint.Render();
            ToastManager.Render();

        }   // end of Render()

        /// <summary>
        /// Clears the Disabled flag on all hints.
        /// </summary>
        public static void RestoreAllHints()
        {
            for (int i = 0; i < hints.Count; i++)
            {
                hints[i].Disabled = false;
            }
        }

        #endregion

        #region Internal

        public static void LoadContent(bool immediate)
        {
            ToastManager.LoadContent(immediate);
            modalHint.LoadContent(immediate);
            scrollableModalHint.LoadContent(immediate);
        }

        public static void InitDeviceResources(GraphicsDevice device)
        {
            ToastManager.InitDeviceResources(device);
            modalHint.InitDeviceResources(device);
            scrollableModalHint.InitDeviceResources(device);
        }

        public static void UnloadContent()
        {
            ToastManager.UnloadContent();
            modalHint.UnloadContent();
            scrollableModalHint.UnloadContent();
        }

        public static void DeviceReset(GraphicsDevice device)
        {
            ToastManager.DeviceReset(device);
            modalHint.DeviceReset(device);
            scrollableModalHint.DeviceReset(device);
        }

        #endregion
    }   // end of class Hints

}   // end of namespace Boku.Common

