// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Runtime.InteropServices;
using System.Diagnostics;
using System.Reflection;

namespace TouchHook
{
    public enum HookType : int
    {
        WH_JOURNALRECORD = 0,
        WH_JOURNALPLAYBACK = 1,
        WH_KEYBOARD = 2,
        WH_GETMESSAGE = 3,
        WH_CALLWNDPROC = 4,
        WH_CBT = 5,
        WH_SYSMSGFILTER = 6,
        WH_MOUSE = 7,
        WH_HARDWARE = 8,
        WH_DEBUG = 9,
        WH_SHELL = 10,
        WH_FOREGROUNDIDLE = 11,
        WH_CALLWNDPROCRET = 12,
        WH_KEYBOARD_LL = 13,
        WH_MOUSE_LL = 14
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct POINT
    {
        public int X;
        public int Y;

        public POINT(int x, int y)
        {
            this.X = x;
            this.Y = y;
        }

        // If we need implicit conversion to a system point we can uncomment this
        // or even replace it with one that allows conversion to a Vector2
        /*public static implicit operator System.Drawing.Point(POINT p)
        {
            return new System.Drawing.Point(p.X, p.Y);
        }

        public static implicit operator POINT(System.Drawing.Point p)
        {
            return new POINT(p.X, p.Y);
        }*/
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct CallWndStruct 
    {
        public IntPtr lparam;
        public IntPtr wparam;
        public UInt32 message;
        public IntPtr hwnd;
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct Message
    {
        public IntPtr hWnd;
        public UInt32 msg;
        public IntPtr wparam;
        public IntPtr lparam;
        public UInt32 time;
        public POINT pt;
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct TOUCHINPUT
    {
        public int x;                       // touch x client coordinate in pixels
        public int y;                       // touch y client coordinate in pixels
        public IntPtr hSource;
        public int dwID;                    // contact ID
        public int dwFlags;                 // mask which fields in the structure are valid
        public int dwMask;                  // flags
        public int dwTime;                  // touch event time
        public IntPtr dwExtraInfo;         
        public int cxContact;               // x size of the contact area in pixels
        public int cyContact;               // y size of the contact area in pixels
    }

    /// <summary> 
    /// The MOUSEHOOKSTRUCT structure contains information about a mouse event passed  
    /// to a WH_MOUSE hook procedure, MouseProc.  
    /// </summary> 
    [StructLayout(LayoutKind.Sequential)]
    struct MOUSEHOOKSTRUCT 
    {
        public POINT pt;
        public IntPtr hwnd;
        public uint wHitTestCode;
        public IntPtr dwExtraInfo;
    }

    /// <summary> 
    /// The MSLLHOOKSTRUCT structure contains information about a low level mouse input event 
    /// </summary> 
    [StructLayout(LayoutKind.Sequential)] 
    internal struct MSLLHOOKSTRUCT 
    { 
        public POINT pt;        // The x and y coordinates in screen coordinates.  
        public int mouseData;   // The mouse wheel and button info. 
        public int flags; 
        public int time;        // Specifies the time stamp for this message.  
        public IntPtr dwExtraInfo;
    }

    // EventArgs passed to Touch handlers
    public class TouchEventArgs : EventArgs
    {
        public bool IsPrimaryContact
        {
            get { return (flags & Win32.TOUCHEVENTF_PRIMARY) != 0; }
        }

        public int x;                  // touch x client coordinate in pixels
        public int y;                  // touch y client coordinate in pixels
        public int id;                 // contact ID
        public int mask;               // mask which fields in the structure are valid
        public int flags;              // flags
        public int time;               // touch event time
        public int contactX;           // x size of the contact area in pixels
        public int contactY;           // y size of the contact area in pixels

        // Constructor
        public TouchEventArgs() {}

        //PV for testing
        public IntPtr wparam;
        public IntPtr lparam;

        public TouchEventArgs(TouchEventArgs tea) 
        {
            x = tea.x;
            y = tea.y;
            id = tea.id;
            mask = tea.mask;
            flags = tea.flags;
            time = tea.time;
            contactX = tea.contactX;
            contactY = tea.contactY;
            wparam = tea.wparam;
            lparam = tea.lparam;
        }

        public string NiceOutput()
        {
            string s = new String(' ',1);

            s  = "x="+x.ToString() + "\n";
            s += "y=" + y.ToString() + "\n";
            s += "id=" + id.ToString() + "\n";
            s += "mask=0x" + mask.ToString("X") + "\n";
            s += "flags=0x" + flags.ToString("X") + "\n";
            s += "time=" + time.ToString() + "\n";
            s += "contactX=" + contactX.ToString() + "\n";
            s += "contactY=" + contactY.ToString() + "\n";
            s += "wparam=0x" + wparam.ToString("X") + "\n";
            s += "lparam=0x" + lparam.ToString("X") + "\n";

            return s;
        }
        //PV end testing
    }

    // EventArgs passed to Mouse handlers
    public class MouseEventArgs : EventArgs
    {
        public int x;
        public int y;
        public uint hitTestCode;

        // Constructor
        public MouseEventArgs() {}
    }

    public class HookEventArgs : EventArgs
    {
        public int HookCode;        // Hook code
        public IntPtr wParam;       // WPARAM argument
        public IntPtr lParam;       // LPARAM argument
        public Message message;     // Translated LPARAM argument (for windows 7)
        public CallWndStruct cwstruct; // Translated LPARAM argument (for windows 8)
    }

    #region Interop Stuff
    // Thanks to P/Invoke.net.
    // This should contain all the Win32 functions we need to deal with
    public static class Win32
    {
        public delegate Int32 WndProcDelegate(IntPtr hWnd, uint msg, IntPtr wParam, IntPtr lParam);

        [DllImport("user32.dll", SetLastError = true, CharSet = CharSet.Auto)]
        public static extern IntPtr SetWindowsHookEx(HookType hook, WindowsHook.HookDelegate callback,
            IntPtr hMod, uint dwThreadId);

        [DllImport("user32.dll", SetLastError = true, CharSet = CharSet.Auto)]
        public static extern bool UnhookWindowsHookEx(IntPtr hhk);

        [DllImport("user32.dll", SetLastError = true, CharSet = CharSet.Auto)]
        public static extern int CallNextHookEx(IntPtr hhk, int nCode, IntPtr wParam, ref Message m);

        [DllImport("user32.dll", SetLastError = true, CharSet = CharSet.Auto)]
        public static extern int CallNextHookEx(IntPtr hhk, int nCode, IntPtr wParam, ref CallWndStruct cwstruct);

        [DllImport("user32.dll", SetLastError = true, CharSet = CharSet.Auto)]
        public static extern IntPtr GetMessageExtraInfo();

        // These are in the form of macros in Windowsx.h. I wrote the functional equivalents here.
        public static int GET_X_LPARAM(IntPtr lParam)
        {
            return (short)(uint)lParam;  // X is the low word.
        }
        public static int GET_Y_LPARAM(IntPtr lParam)
        {
            return (short)((uint)lParam >> 16); // Y is the high word.
        }

        [DllImport("user32.dll", EntryPoint = "TranslateMessage")]
        public extern static bool TranslateMessage(ref Message m);

        [DllImport("user32.dll")]
        public extern static uint GetWindowThreadProcessId(IntPtr window, IntPtr module);

        [DllImport("kernel32.dll", CharSet = CharSet.Auto)]
        public static extern IntPtr GetModuleHandle(string lpModuleName);

        [DllImport("user32.dll", CharSet = CharSet.Auto)]
        [return: MarshalAs(UnmanagedType.Bool)]
        public extern static bool RegisterTouchWindow(IntPtr hWnd, uint ulFlags);

        [DllImport("user32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        public extern static bool GetTouchInputInfo(IntPtr hTouchInput, int cInputs,
            [In, Out] TOUCHINPUT[] pInputs, int cbSize);

        [DllImport("user32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        public extern static bool CloseTouchInputHandle(IntPtr lParam);
        
        [DllImport("user32")]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool SetGestureConfig(IntPtr hWnd, int dwReserved, int cIDs, ref GESTURECONFIG[] pGestureConfig, int cbSize);

        [DllImport("user32")]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool GetGestureInfo(IntPtr hGestureInfo, ref GESTUREINFO pGestureInfo);

        [DllImport("user32")]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool CloseGestureInfoHandle(IntPtr lParam);

        [DllImport("user32.dll", CharSet = CharSet.Auto, ExactSpelling = true)]
        public static extern int GetSystemMetrics(int nIndex);

        [DllImport("user32.dll")]
        public static extern IntPtr DefWindowProc(IntPtr hWnd, uint uMsg, IntPtr wParam, IntPtr lParam);
      
        [DllImport("Kernel32.dll")]
        public static extern Int32 GetLastError();

        /// <summary>
        /// Changes an attribute of the specified window. The function also sets the 32-bit (long) value at the specified offset into the extra window memory.
        /// </summary>
        /// <param name="hWnd">A handle to the window and, indirectly, the class to which the window belongs..</param>
        /// <param name="nIndex">The zero-based offset to the value to be set. Valid values are in the range zero through the number of bytes of extra window memory, minus the size of an integer. To set any other value, specify one of the following values: GWL_EXSTYLE, GWL_HINSTANCE, GWL_ID, GWL_STYLE, GWL_USERDATA, GWL_WNDPROC </param>
        /// <param name="dwNewLong">The replacement value.</param>
        /// <returns>If the function succeeds, the return value is the previous value of the specified 32-bit integer. 
        /// If the function fails, the return value is zero. To get extended error information, call GetLastError. </returns>
        [DllImport("user32.dll")]
        public static extern Int32 SetWindowLong(IntPtr hWnd, int nIndex, Int32 dwNewLong);
        [DllImport("user32.dll")]
        public static extern Int32 SetWindowLong(IntPtr hWnd, int nIndex, WndProcDelegate newProc);
        
        [DllImport("user32.dll")]
        public static extern Int32 CallWindowProc(Int32 lpPrevWndFunc, IntPtr hWnd, uint Msg, IntPtr wParam, IntPtr lParam);

        public static POINT MakePoint(IntPtr lParam)
        {
            MOUSEHOOKSTRUCT input = (MOUSEHOOKSTRUCT)Marshal.PtrToStructure(lParam, typeof(MOUSEHOOKSTRUCT));
            return input.pt;
        }

        public const uint TWF_NONE = 0;
        public const uint TWF_FINETOUCH = 1;
        public const uint TWF_WANTPALM = 2;

        #region SetWindowLong/GetWindowLong constants

        public const Int32 GWL_WNDPROC = (-4);
        public const Int32 GWL_HINSTANCE = (-6);
        public const Int32 GWL_HWNDPARENT = (-8);
        public const Int32 GWL_STYLE = (-16);
        public const Int32 GWL_EXSTYLE = (-20);
        public const Int32 GWL_USERDATA = (-21);
        public const Int32 GWL_ID = (-12);
    
        #endregion

        #region Touch constants and Structs
        /*
         * Touch input flag values (TOUCHINPUT.dwFlags) [winuser.h]
         */
        public const UInt32 TOUCHEVENTF_MOVE = 0x0001;
        public const UInt32 TOUCHEVENTF_DOWN = 0x0002;
        public const UInt32 TOUCHEVENTF_UP = 0x0004;
        public const UInt32 TOUCHEVENTF_INRANGE = 0x0008;
        public const UInt32 TOUCHEVENTF_PRIMARY = 0x0010;
        public const UInt32 TOUCHEVENTF_NOCOALESCE = 0x0020;
        public const UInt32 TOUCHEVENTF_PEN = 0x0040;
        public const UInt32 TOUCHEVENTF_PALM = 0x0080;
        /*
         * Touch input mask values (TOUCHINPUT.dwMask) [winuser.h]
         */
        public const UInt32 TOUCHINPUTMASKF_TIMEFROMSYSTEM = 0x0001;  // the dwTime field contains a system generated value
        public const UInt32 TOUCHINPUTMASKF_EXTRAINFO = 0x0002;  // the dwExtraInfo field is valid
        public const UInt32 TOUCHINPUTMASKF_CONTACTAREA = 0x0004;  // the cxContact and cyContact fields are valid

        #endregion

        #region Mouse constants and structs
        public const UInt32 MOUSEEVENTF_FROMTOUCH = 0xFF515700;
        #endregion

        #region Gesture constants
        /* One of the fields in GESTUREINFO structure is type of Int64 (8 bytes).
         * The relevant gesture information is stored in lower 4 bytes. This
         * bit mask is used to get 4 lower bytes from this argument.
         */
        public const Int64 ULL_ARGUMENTS_BIT_MASK = 0x00000000FFFFFFFF;
        /* Multitouch/Touch glue
         * Touch event window message constants [winuser.h]
         */
        public const int WM_GESTURENOTIFY = 0x011A;
        public const int WM_GESTURE = 0x0119;
        

        /*
         * System Metric flags
         */
        public const int SM_DIGITIZER = 94;
        public const int SM_MAXIMUMTOUCHES = 95;
        /*
         * Gesture configuration flags - GESTURECONFIG.dwWant or GESTURECONFIG.dwBlock
         */

        /*
         * Common gesture configuration flags - set GESTURECONFIG.dwID to zero
         */
        public const int GC_ALLGESTURES = 0x00000001;
        /*
         * Zoom gesture configuration flags - set GESTURECONFIG.dwID to GID_ZOOM
         */
        public const int GC_ZOOM = 0x00000001;

        /*
         * Pan gesture configuration flags - set GESTURECONFIG.dwID to GID_PAN
         */
        /*
         * Enables all pan gestures when this flag is set
         */
        public const int GC_PAN = 0x00000001;
        /*
         * Vertical pans with one finger
         */
        public const int GC_PAN_WITH_SINGLE_FINGER_VERTICALLY = 0x00000002;
        /*
         * Horizontal pans with one finger.
         */
        public const int GC_PAN_WITH_SINGLE_FINGER_HORIZONTALLY = 0x00000004;
        /*
         * Panning with a gutter boundary around the edges of pannable region. 
         * The gutter boundary limits perpendicular movement to a primary direction 
         * until a threshold is reached to break out of the gutter.
         */
        public const int GC_PAN_WITH_GUTTER = 0x00000008;
        /* 
         * Panning with inertia to smoothly slow when pan gestures stop.
         */
        public const int GC_PAN_WITH_INERTIA = 0x00000010;

        /*
         * Rotate gesture configuration flags - set GESTURECONFIG.dwID to GID_ROTATE
         */
        public const int GC_ROTATE = 0x00000001;

        /*
         * Two finger tap gesture configuration flags - set GESTURECONFIG.dwID to GID_TWOFINGERTAP
         */
        public const int GC_TWOFINGERTAP = 0x00000001;

        /*
         * PressAndTap gesture configuration flags - set GESTURECONFIG.dwID to GID_PRESSANDTAP
         */
        public const int GC_PRESSANDTAP = 0x00000001;
        public const int GC_ROLLOVER = GC_PRESSANDTAP;

        /* Maximum number of gestures that can be included                                                        
         * in a single call to SetGestureConfig / GetGestureConfig
         */
        public const int GESTURECONFIGMAXCOUNT = 256;           

        // Gesture IDs 
        public const int GID_BEGIN = 1;
        public const int GID_END = 2;
        public const int GID_ZOOM = 3;
        public const int GID_PAN = 4;
        public const int GID_ROTATE = 5;
        public const int GID_TWOFINGERTAP = 6;
        public const int GID_PRESSANDTAP = 7;

        // Gesture flags - GESTUREINFO.dwFlags
        public const int GF_BEGIN = 0x00000001;
        public const int GF_INERTIA = 0x00000002;
        public const int GF_END = 0x00000004;
        
        /* Gesture configuration structure
         *   - Used in SetGestureConfig and GetGestureConfig
         *   - Note that any setting not included in either GESTURECONFIG.dwWant
         *     or GESTURECONFIG.dwBlock will use the parent window's preferences
         *     or system defaults.
         *
         * Touch API defined structures [winuser.h]
         */
        [StructLayout(LayoutKind.Sequential)]
        public struct GESTURECONFIG
        {
            public int dwID;    // gesture ID
            public int dwWant;  // settings related to gesture ID that are to be turned on
            public int dwBlock; // settings related to gesture ID that are to be turned off
        }

        /*  Gesture information structure
         *    - Pass the HGESTUREINFO received in the WM_GESTURE message lParam 
         *      into the GetGestureInfo function to retrieve this information.
         *    - If cbExtraArgs is non-zero, pass the HGESTUREINFO received in 
         *      the WM_GESTURE message lParam into the GetGestureExtraArgs 
         *     function to retrieve extended argument information.
         */
        [StructLayout(LayoutKind.Sequential)]
        public struct GESTUREINFO
        {
            public int cbSize;           // size, in bytes, of this structure
            // (including variable length Args 
            // field)
            public int dwFlags;          // see GF_* flags
            public int dwID;             // gesture ID, see GID_* defines
            public IntPtr hwndTarget;    // handle to window targeted by this 
            // gesture
            [MarshalAs(UnmanagedType.Struct)]
            public POINT ptsLocation;  // current location of this gesture
            public int dwInstanceID;     // internally used
            public int dwSequenceID;     // internally used
            public Int64 ullArguments;   // arguments for gestures whose 
            // arguments fit in 8 BYTES
            public int cbExtraArgs;      // size, in bytes, of extra arguments, 
            // if any, that accompany this gesture
        }
        /// <summary>
        /// Taken from GCI_ROTATE_ANGLE_FROM_ARGUMENT.
        /// Converts from "binary radians" to traditional radians.
        /// </summary>
        public static double ArgToRadians(Int64 arg)
        {
            return ((((double)(arg) / 65535.0) * 4.0 * 3.14159265) - 2.0 * 3.14159265);
        }
        #endregion

        #region Gesture configuration constants [Tpcshrd.h]
        
        public const UInt32 WM_TABLET_DEFBASE = 0x02C0;
        public const UInt32 WM_TABLET_MAXOFFSET = 0x20;
        public const UInt32 WM_TABLET_ADDED = (WM_TABLET_DEFBASE + 8);
        public const UInt32 WM_TABLET_DELETED = (WM_TABLET_DEFBASE + 9);
        public const UInt32 WM_TABLET_FLICK = (WM_TABLET_DEFBASE + 11);
        public const UInt32 WM_TABLET_QUERYSYSTEMGESTURESTATUS = (WM_TABLET_DEFBASE + 12);
       
        public const UInt32 TABLET_DISABLE_PRESSANDHOLD        = 0x00000001; // disables press and hold (right-click) gesture
        public const UInt32 TABLET_DISABLE_PENTAPFEEDBACK      = 0x00000008; // disables UI feedback on pen up (waves)
        public const UInt32 TABLET_DISABLE_PENBARRELFEEDBACK   = 0x00000010; // disables UI feedback on pen button down (circle)
        public const UInt32 TABLET_DISABLE_TOUCHUIFORCEON      = 0x00000100;
        public const UInt32 TABLET_DISABLE_TOUCHUIFORCEOFF     = 0x00000200;
        public const UInt32 TABLET_DISABLE_TOUCHSWITCH         = 0x00008000;
        public const UInt32 TABLET_DISABLE_FLICKS              = 0x00010000; // disables pen flicks (back, forward, drag down, drag up)
        public const UInt32 TABLET_ENABLE_FLICKSONCONTEXT      = 0x00020000;
        public const UInt32 TABLET_ENABLE_FLICKLEARNINGMODE    = 0x00040000;
        public const UInt32 TABLET_DISABLE_SMOOTHSCROLLING     = 0x00080000;
        public const UInt32 TABLET_DISABLE_FLICKFALLBACKKEYS   = 0x00100000;
        public const UInt32 TABLET_ENABLE_MULTITOUCHDATA       = 0x01000000;
        
        #endregion

        #region Message type constants from [winuser.h]

        public const UInt32 WM_ACTIVATE = 0x0006;
        public const UInt32 WM_ACTIVATEAPP = 0x001C;
        public const UInt32 WM_AFXFIRST = 0x0360;
        public const UInt32 WM_AFXLAST = 0x037F;
        public const UInt32 WM_APP = 0x8000;
        public const UInt32 WM_ASKCBFORMATNAME = 0x030C;
        public const UInt32 WM_CANCELJOURNAL = 0x004B;
        public const UInt32 WM_CANCELMODE = 0x001F;
        public const UInt32 WM_CAPTURECHANGED = 0x0215;
        public const UInt32 WM_CHANGECBCHAIN = 0x030D;
        public const UInt32 WM_CHANGEUISTATE = 0x0127;
        public const UInt32 WM_CHAR = 0x0102;
        public const UInt32 WM_CHARTOITEM = 0x002F;
        public const UInt32 WM_CHILDACTIVATE = 0x0022;
        public const UInt32 WM_CLEAR = 0x0303;
        public const UInt32 WM_CLOSE = 0x0010;
        public const UInt32 WM_COMMAND = 0x0111;
        public const UInt32 WM_COMPACTING = 0x0041;
        public const UInt32 WM_COMPAREITEM = 0x0039;
        public const UInt32 WM_CONTEXTMENU = 0x007B;
        public const UInt32 WM_COPY = 0x0301;
        public const UInt32 WM_COPYDATA = 0x004A;
        public const UInt32 WM_CREATE = 0x0001;
        public const UInt32 WM_CTLCOLORBTN = 0x0135;
        public const UInt32 WM_CTLCOLORDLG = 0x0136;
        public const UInt32 WM_CTLCOLOREDIT = 0x0133;
        public const UInt32 WM_CTLCOLORLISTBOX = 0x0134;
        public const UInt32 WM_CTLCOLORMSGBOX = 0x0132;
        public const UInt32 WM_CTLCOLORSCROLLBAR = 0x0137;
        public const UInt32 WM_CTLCOLORSTATIC = 0x0138;
        public const UInt32 WM_CUT = 0x0300;
        public const UInt32 WM_DEADCHAR = 0x0103;
        public const UInt32 WM_DELETEITEM = 0x002D;
        public const UInt32 WM_DESTROY = 0x0002;
        public const UInt32 WM_DESTROYCLIPBOARD = 0x0307;
        public const UInt32 WM_DEVICECHANGE = 0x0219;
        public const UInt32 WM_DEVMODECHANGE = 0x001B;
        public const UInt32 WM_DISPLAYCHANGE = 0x007E;
        public const UInt32 WM_DRAWCLIPBOARD = 0x0308;
        public const UInt32 WM_DRAWITEM = 0x002B;
        public const UInt32 WM_DROPFILES = 0x0233;
        public const UInt32 WM_ENABLE = 0x000A;
        public const UInt32 WM_ENDSESSION = 0x0016;
        public const UInt32 WM_ENTERIDLE = 0x0121;
        public const UInt32 WM_ENTERMENULOOP = 0x0211;
        public const UInt32 WM_ENTERSIZEMOVE = 0x0231;
        public const UInt32 WM_ERASEBKGND = 0x0014;
        public const UInt32 WM_EXITMENULOOP = 0x0212;
        public const UInt32 WM_EXITSIZEMOVE = 0x0232;
        public const UInt32 WM_FONTCHANGE = 0x001D;
        public const UInt32 WM_GETDLGCODE = 0x0087;
        public const UInt32 WM_GETFONT = 0x0031;
        public const UInt32 WM_GETHOTKEY = 0x0033;
        public const UInt32 WM_GETICON = 0x007F;
        public const UInt32 WM_GETMINMAXINFO = 0x0024;
        public const UInt32 WM_GETOBJECT = 0x003D;
        public const UInt32 WM_GETTEXT = 0x000D;
        public const UInt32 WM_GETTEXTLENGTH = 0x000E;
        public const UInt32 WM_HANDHELDFIRST = 0x0358;
        public const UInt32 WM_HANDHELDLAST = 0x035F;
        public const UInt32 WM_HELP = 0x0053;
        public const UInt32 WM_HOTKEY = 0x0312;
        public const UInt32 WM_HSCROLL = 0x0114;
        public const UInt32 WM_HSCROLLCLIPBOARD = 0x030E;
        public const UInt32 WM_ICONERASEBKGND = 0x0027;
        public const UInt32 WM_IME_CHAR = 0x0286;
        public const UInt32 WM_IME_COMPOSITION = 0x010F;
        public const UInt32 WM_IME_COMPOSITIONFULL = 0x0284;
        public const UInt32 WM_IME_CONTROL = 0x0283;
        public const UInt32 WM_IME_ENDCOMPOSITION = 0x010E;
        public const UInt32 WM_IME_KEYDOWN = 0x0290;
        public const UInt32 WM_IME_KEYLAST = 0x010F;
        public const UInt32 WM_IME_KEYUP = 0x0291;
        public const UInt32 WM_IME_NOTIFY = 0x0282;
        public const UInt32 WM_IME_REQUEST = 0x0288;
        public const UInt32 WM_IME_SELECT = 0x0285;
        public const UInt32 WM_IME_SETCONTEXT = 0x0281;
        public const UInt32 WM_IME_STARTCOMPOSITION = 0x010D;
        public const UInt32 WM_INITDIALOG = 0x0110;
        public const UInt32 WM_INITMENU = 0x0116;
        public const UInt32 WM_INITMENUPOPUP = 0x0117;
        public const UInt32 WM_INPUTLANGCHANGE = 0x0051;
        public const UInt32 WM_INPUTLANGCHANGEREQUEST = 0x0050;
        public const UInt32 WM_KEYDOWN = 0x0100;
        public const UInt32 WM_KEYFIRST = 0x0100;
        public const UInt32 WM_KEYLAST = 0x0108;
        public const UInt32 WM_KEYUP = 0x0101;
        public const UInt32 WM_KILLFOCUS = 0x0008;
        public const UInt32 WM_LBUTTONDBLCLK = 0x0203;
        public const UInt32 WM_LBUTTONDOWN = 0x0201;
        public const UInt32 WM_LBUTTONUP = 0x0202;
        public const UInt32 WM_MBUTTONDBLCLK = 0x0209;
        public const UInt32 WM_MBUTTONDOWN = 0x0207;
        public const UInt32 WM_MBUTTONUP = 0x0208;
        public const UInt32 WM_MDIACTIVATE = 0x0222;
        public const UInt32 WM_MDICASCADE = 0x0227;
        public const UInt32 WM_MDICREATE = 0x0220;
        public const UInt32 WM_MDIDESTROY = 0x0221;
        public const UInt32 WM_MDIGETACTIVE = 0x0229;
        public const UInt32 WM_MDIICONARRANGE = 0x0228;
        public const UInt32 WM_MDIMAXIMIZE = 0x0225;
        public const UInt32 WM_MDINEXT = 0x0224;
        public const UInt32 WM_MDIREFRESHMENU = 0x0234;
        public const UInt32 WM_MDIRESTORE = 0x0223;
        public const UInt32 WM_MDISETMENU = 0x0230;
        public const UInt32 WM_MDITILE = 0x0226;
        public const UInt32 WM_MEASUREITEM = 0x002C;
        public const UInt32 WM_MENUCHAR = 0x0120;
        public const UInt32 WM_MENUCOMMAND = 0x0126;
        public const UInt32 WM_MENUDRAG = 0x0123;
        public const UInt32 WM_MENUGETOBJECT = 0x0124;
        public const UInt32 WM_MENURBUTTONUP = 0x0122;
        public const UInt32 WM_MENUSELECT = 0x011F;
        public const UInt32 WM_MOUSEACTIVATE = 0x0021;
        public const UInt32 WM_MOUSEFIRST = 0x0200;
        public const UInt32 WM_MOUSEHOVER = 0x02A1;
        public const UInt32 WM_MOUSELAST = 0x020D;
        public const UInt32 WM_MOUSELEAVE = 0x02A3;
        public const UInt32 WM_MOUSEMOVE = 0x0200;
        public const UInt32 WM_MOUSEWHEEL = 0x020A;
        public const UInt32 WM_MOUSEHWHEEL = 0x020E;
        public const UInt32 WM_MOVE = 0x0003;
        public const UInt32 WM_MOVING = 0x0216;
        public const UInt32 WM_NCACTIVATE = 0x0086;
        public const UInt32 WM_NCCALCSIZE = 0x0083;
        public const UInt32 WM_NCCREATE = 0x0081;
        public const UInt32 WM_NCDESTROY = 0x0082;
        public const UInt32 WM_NCHITTEST = 0x0084;
        public const UInt32 WM_NCLBUTTONDBLCLK = 0x00A3;
        public const UInt32 WM_NCLBUTTONDOWN = 0x00A1;
        public const UInt32 WM_NCLBUTTONUP = 0x00A2;
        public const UInt32 WM_NCMBUTTONDBLCLK = 0x00A9;
        public const UInt32 WM_NCMBUTTONDOWN = 0x00A7;
        public const UInt32 WM_NCMBUTTONUP = 0x00A8;
        public const UInt32 WM_NCMOUSEMOVE = 0x00A0;
        public const UInt32 WM_NCPAINT = 0x0085;
        public const UInt32 WM_NCRBUTTONDBLCLK = 0x00A6;
        public const UInt32 WM_NCRBUTTONDOWN = 0x00A4;
        public const UInt32 WM_NCRBUTTONUP = 0x00A5;
        public const UInt32 WM_NEXTDLGCTL = 0x0028;
        public const UInt32 WM_NEXTMENU = 0x0213;
        public const UInt32 WM_NOTIFY = 0x004E;
        public const UInt32 WM_NOTIFYFORMAT = 0x0055;
        public const UInt32 WM_NULL = 0x0000;
        public const UInt32 WM_PAINT = 0x000F;
        public const UInt32 WM_PAINTCLIPBOARD = 0x0309;
        public const UInt32 WM_PAINTICON = 0x0026;
        public const UInt32 WM_PALETTECHANGED = 0x0311;
        public const UInt32 WM_PALETTEISCHANGING = 0x0310;
        public const UInt32 WM_PARENTNOTIFY = 0x0210;
        public const UInt32 WM_PASTE = 0x0302;
        public const UInt32 WM_PENWINFIRST = 0x0380;
        public const UInt32 WM_PENWINLAST = 0x038F;
        public const UInt32 WM_POWER = 0x0048;
        public const UInt32 WM_POWERBROADCAST = 0x0218;
        public const UInt32 WM_PRINT = 0x0317;
        public const UInt32 WM_PRINTCLIENT = 0x0318;
        public const UInt32 WM_QUERYDRAGICON = 0x0037;
        public const UInt32 WM_QUERYENDSESSION = 0x0011;
        public const UInt32 WM_QUERYNEWPALETTE = 0x030F;
        public const UInt32 WM_QUERYOPEN = 0x0013;
        public const UInt32 WM_QUEUESYNC = 0x0023;
        public const UInt32 WM_QUIT = 0x0012;
        public const UInt32 WM_RBUTTONDBLCLK = 0x0206;
        public const UInt32 WM_RBUTTONDOWN = 0x0204;
        public const UInt32 WM_RBUTTONUP = 0x0205;
        public const UInt32 WM_RENDERALLFORMATS = 0x0306;
        public const UInt32 WM_RENDERFORMAT = 0x0305;
        public const UInt32 WM_SETCURSOR = 0x0020;
        public const UInt32 WM_SETFOCUS = 0x0007;
        public const UInt32 WM_SETFONT = 0x0030;
        public const UInt32 WM_SETHOTKEY = 0x0032;
        public const UInt32 WM_SETICON = 0x0080;
        public const UInt32 WM_SETREDRAW = 0x000B;
        public const UInt32 WM_SETTEXT = 0x000C;
        public const UInt32 WM_SETTINGCHANGE = 0x001A;
        public const UInt32 WM_SHOWWINDOW = 0x0018;
        public const UInt32 WM_SIZE = 0x0005;
        public const UInt32 WM_SIZECLIPBOARD = 0x030B;
        public const UInt32 WM_SIZING = 0x0214;
        public const UInt32 WM_SPOOLERSTATUS = 0x002A;
        public const UInt32 WM_STYLECHANGED = 0x007D;
        public const UInt32 WM_STYLECHANGING = 0x007C;
        public const UInt32 WM_SYNCPAINT = 0x0088;
        public const UInt32 WM_SYSCHAR = 0x0106;
        public const UInt32 WM_SYSCOLORCHANGE = 0x0015;
        public const UInt32 WM_SYSCOMMAND = 0x0112;
        public const UInt32 WM_SYSDEADCHAR = 0x0107;
        public const UInt32 WM_SYSKEYDOWN = 0x0104;
        public const UInt32 WM_SYSKEYUP = 0x0105;
        public const UInt32 WM_TCARD = 0x0052;
        public const UInt32 WM_TIMECHANGE = 0x001E;
        public const UInt32 WM_TIMER = 0x0113;
        public const UInt32 WM_TOUCH = 0x0240;
        public const UInt32 WM_UNDO = 0x0304;
        public const UInt32 WM_UNINITMENUPOPUP = 0x0125;
        public const UInt32 WM_USER = 0x0400;
        public const UInt32 WM_USERCHANGED = 0x0054;
        public const UInt32 WM_VKEYTOITEM = 0x002E;
        public const UInt32 WM_VSCROLL = 0x0115;
        public const UInt32 WM_VSCROLLCLIPBOARD = 0x030A;
        public const UInt32 WM_WINDOWPOSCHANGED = 0x0047;
        public const UInt32 WM_WINDOWPOSCHANGING = 0x0046;
        public const UInt32 WM_WININICHANGE = 0x001A;
        public const UInt32 WM_XBUTTONDBLCLK = 0x020D;
        public const UInt32 WM_XBUTTONDOWN = 0x020B;
        public const UInt32 WM_XBUTTONUP = 0x020C;
        #endregion

    }
    #endregion
}
