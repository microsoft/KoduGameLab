
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text;

using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input.Touch;

#if !NETFX_CORE
    #if WINDOWS_PHONE
    #else
        using TouchHook;
        using System.Windows.Forms;
    #endif
#endif

namespace KoiX.Input
{
    /// <summary>
    /// Class for flags shared by PCTouchInput and PhoneTouchInput
    /// </summary>
    public static class LowLevelTouchInput
    {
        #region Members

        static bool wasTouched = false;             // Any touches this frame?

        static bool ignoreTouchEvents = false;      // Global switch to ignore all Touch events.
        static bool ignoreUntilReleased = false;    // Ignore touch events util we have a frame with none.

        static List<TouchSample> touchSampleList;   // List we pass to consumers.

        #endregion

        #region Accessors

        /// <summary>
        /// Flag which controls whether or not Touch events are processed.
        /// </summary>
        public static bool IgnoreTouchEvents
        {
            get { return ignoreTouchEvents; }
            set { ignoreTouchEvents = value; }
        }

        /// <summary>
        /// Was any touch input created this frame?
        /// </summary>
        public static bool WasTouched
        {
            get { return wasTouched; }
            set { wasTouched = value; }
        }

        public static bool IgnoreUntilReleased
        {
            get { return ignoreUntilReleased; }
            set { ignoreUntilReleased = value; }
        }

        public static List<TouchSample> Touches
        {
            get { return touchSampleList; }
            set { touchSampleList = value; }
        }

        #endregion

    }   // end of class LowLevelTouchInput

}   // end of namespace KoiX.Input
