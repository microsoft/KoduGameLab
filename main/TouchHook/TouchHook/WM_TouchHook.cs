// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

//#define STRESS_TEST

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Runtime.InteropServices;

#if NETFX_CORE
    using Windows.Foundation;
    using Windows.Security;
#else
    using System.Security.Permissions;
    using System.Windows.Forms;
    using System.Drawing;
#endif

using System.Diagnostics;

namespace TouchHook
{
    public class WM_TouchHook : WindowsHook
    {
        #region Fields and Properties
        private int mdTouchInputSize;
        private Win32.WndProcDelegate gestureDelegate;

#if STRESS_TEST
        static public List<TouchEventArgs> touchEventArgsTrackList = new List<TouchEventArgs>();
        static public List<TouchEventArgs> touchEventArgsTrackDownList = new List<TouchEventArgs>();
#endif

        // Touch event handlers
        public event EventHandler<TouchEventArgs> TouchDown;   // touch down event handler
        public event EventHandler<TouchEventArgs> TouchUp;     // touch up event handler
        public event EventHandler<TouchEventArgs> TouchMove;   // touch move event handler

        public static bool DisableNativePressAndHoldGesture { get; set; }

        #region Handling Spurious WM_MOUSEMOVE Events

        ///// <summary>
        ///// This is how long we will wait before discarding our touch captures. If we receive
        ///// mouse events after this time differential, we will consider them legitimate.
        ///// </summary>
        //private static int MAX_CAPTURE_MSECS = 12000;

        ///// <summary>
        ///// This is how much difference in both the x and y coordinates the new MouseMove events
        ///// must be before we consider it a legitimate mouse movement.
        ///// </summary>
        //private static int MIN_CAPTURE_DIFF = 200;

        public class CapturedTouch
        {
            #region Fields and Properties
            public int id;
            public Point point;
            public UInt32 captureTime;
            #endregion

            public CapturedTouch(int id, Point point, UInt32 captureTime)
            {
                this.id = id;
                this.point = point;
                this.captureTime = captureTime;
            }
        }
        public static List<CapturedTouch> capturedTouches = new List<CapturedTouch>();
        #endregion

        private int lpPrevWndProc;
        #endregion

        public bool IsTouchAvailable()
        {
            int bitMask = Win32.GetSystemMetrics(Win32.SM_DIGITIZER);
            //the 0x40 mask indicates whether the "multi-touch" bit is set (even if this is set, we need to test max touches to 
            //ensure we have over 2 for full gesture support)
            if ((bitMask & 0x40) > 0)
            {
                return true;
            }
            return false;
        }

        public int GetMaxTouches()
        {
            return Win32.GetSystemMetrics(Win32.SM_MAXIMUMTOUCHES);
        }

        public WM_TouchHook(IntPtr hWnd, HookType hookType)
            : base(hWnd, hookType) //HookType.WH_CALLWNDPROC) //HookType.WH_GETMESSAGE)
        {
            DisableNativePressAndHoldGesture = true; // false;
            //HookInvoked += new HookEventHandler(TouchHookInvoked);

            mdTouchInputSize = Marshal.SizeOf(new TOUCHINPUT());
            gestureDelegate = new Win32.WndProcDelegate(this.GestureWndProc);
        }

        protected int GestureWndProc(IntPtr hWnd, uint msg, IntPtr wParam, IntPtr lParam)
        {
            if (msg == Win32.WM_TABLET_QUERYSYSTEMGESTURESTATUS)
            {
                if (DisableNativePressAndHoldGesture)
                {
                    return (int)Win32.TABLET_DISABLE_PRESSANDHOLD; // | (int)Win32.TABLET_DISABLE_FLICKS | (int)Win32.TABLET_ENABLE_MULTITOUCHDATA;
                }
            }
            else if (msg == Win32.WM_TOUCH)
            {
                DecodeTouch(wParam, lParam);
                return 0;
            }

            return Win32.CallWindowProc(lpPrevWndProc, hWnd, msg, wParam, lParam);
        }

        [DllImport("gdi32.dll", CharSet = CharSet.Auto, SetLastError = true, ExactSpelling = true)]
        public static extern int GetDeviceCaps(IntPtr hDC, int nIndex);

        public enum DeviceCap
        {
            HORZRES = 8,            // Logical horizontal resolution.
            VERTRES = 10,           // Logicalvertical resolution. 
            DESKTOPHORZRES = 118,   // Physical horizontal resolution.
            DESKTOPVERTRES = 117,   // Physical vertical resolution.
        }

        // Scaling factors used to adjust touch locations for 
        // system screen scaling.
        float scalingFactorX = 1;
        float scalingFactorY = 1;

        public override void InstallHook()
        {
            base.InstallHook();

            lpPrevWndProc = Win32.SetWindowLong(hWnd, Win32.GWL_WNDPROC, gestureDelegate);
            if (lpPrevWndProc == 0)
            {
                Console.WriteLine("Setting of the new WndProc failed...");
            }

            // Note: the RegisterTouchWindow call doesn't exist for XP so we
            // need to catch this and just pretend it failed.
            try
            {
                //PV- Add TWF_WANTPALM flag
                //this flag disables palm rejection which reduces delays for getting WM_TOUCH messages
                if (!Win32.RegisterTouchWindow(this.hWnd, Win32.TWF_WANTPALM))
                {
                    UninstallHook();
                }
            }
            catch
            {
                UninstallHook();
            }


            // Get DPI information so we can properly scale touch locations.
            // Commented out since this has magically no longer become necessary.  Yeah Windows.
            /*
            Graphics g = Graphics.FromHwnd(IntPtr.Zero);
            IntPtr desktop = g.GetHdc();

            scalingFactorX = GetDeviceCaps(desktop, (int)DeviceCap.DESKTOPHORZRES) / (float)GetDeviceCaps(desktop, (int)DeviceCap.HORZRES);
            scalingFactorY = GetDeviceCaps(desktop, (int)DeviceCap.DESKTOPVERTRES) / (float)GetDeviceCaps(desktop, (int)DeviceCap.VERTRES);
            */
        }

        public override void UninstallHook()
        {
            base.UninstallHook();

            Win32.SetWindowLong(hWnd, Win32.GWL_WNDPROC, lpPrevWndProc);
        }

        public int TouchHookInvoked(object sender, HookEventArgs e)
        {
            if (hookType == HookType.WH_CALLWNDPROC)
            {
                //Console.WriteLine("e.cwstruct.message=0x" + e.cwstruct.message.ToString("X"));

                //windows 8 style
                switch (e.cwstruct.message)
                {
                    case Win32.WM_TOUCH:
                        {
                            //Console.WriteLine("Touch recieved");
                            DecodeTouch(e.cwstruct.wparam, e.cwstruct.lparam);
                        }
                        break;

                    //case Win32.WM_SETCURSOR:
                    //    // Nullify touch-generated mouse messages so they don't get passed down the chain.
                    //    e.cwstruct.message = Win32.WM_NULL;
                    //    break;
                    default:
                        break;
                }
            }
            else
                if (hookType == HookType.WH_GETMESSAGE)
                {
                    //windows 7 style
                    //Console.WriteLine("e.message.msg=0x" + e.message.msg.ToString("X") );
                    switch (e.message.msg)
                    {
                        case Win32.WM_TOUCH:
                            {
                                //Console.WriteLine("Touch recieved");
                                //DecodeTouch(e.message.wparam, e.message.lparam);
                            }
                            break;
                        case Win32.WM_MOUSEMOVE:
                        case Win32.WM_LBUTTONDOWN:
                        case Win32.WM_LBUTTONDBLCLK:
                        case Win32.WM_LBUTTONUP:
                        case Win32.WM_MOUSELEAVE:
                            //case Win32.WM_SETCURSOR:
                            {
                                IntPtr extraInfoPtr = Win32.GetMessageExtraInfo();
                                long extraInfo = extraInfoPtr.ToInt64();

                                bool generatedFromTouch = false;

                                // Determine whether these mouse events were generated by a touch.
                                if ((extraInfo & Win32.MOUSEEVENTF_FROMTOUCH) == Win32.MOUSEEVENTF_FROMTOUCH)
                                {
                                    generatedFromTouch = true;
                                }
                                else if (e.message.msg == Win32.WM_MOUSEMOVE)
                                {
                                    if (FoundSimilarTouchCapture(e.message.time, e.message.lparam))
                                    {
                                        generatedFromTouch = true;
                                    }
                                    else
                                    {
                                        // JW - Useful debug lines to find out why mousemoves were accepted.
                                        //int xPos = Win32.GET_X_LPARAM(e.message.lparam);
                                        //int yPos = Win32.GET_Y_LPARAM(e.message.lparam);
                                        //Console.Write("MouseMove: " + xPos + ":" + yPos + "\n");
                                        //OutputCapturedTouches();
                                    }
                                }

                                if (generatedFromTouch)
                                {
                                    // Nullify touch-generated mouse messages so they don't get passed down the chain.
                                    e.message.msg = Win32.WM_NULL;
                                }
                                else
                                {
                                    // Legitimate mouse events have begun, so our previous touch captures
                                    // are no longer valid for comparison.
                                    capturedTouches.Clear();
                                }
                            }
                            break;
                        default:
                            break;
                    }
                }

            return 0;
        }

        private void OutputCapturedTouches()
        {
            for (int i = 0; i < capturedTouches.Count; i++)
            {
                Console.Write("Captured Touch id #" + capturedTouches[i].id +
                    " " + capturedTouches[i].point.X + ":" + capturedTouches[i].point.Y + "\n");
            }
        }

        private static bool FoundSimilarTouchCapture(UInt32 captureTime, IntPtr lParam)
        {
            /*
            int xPos = Win32.GET_X_LPARAM(lParam);
            int yPos = Win32.GET_Y_LPARAM(lParam);

            for(int i = 0; i < capturedTouches.Count; i++)
            {
                // JW - Disabled checks. Check for captured touch only.
                // Recent enough touch?
                //UInt32 timeDiff = captureTime -capturedTouches[i].captureTime;
                //if (timeDiff < MAX_CAPTURE_MSECS)
                //{
                    //int xDiff = Math.Abs(capturedTouches[i].point.X - xPos);
                    //int yDiff = Math.Abs(capturedTouches[i].point.Y - yPos);
                    //if ((xDiff < MIN_CAPTURE_DIFF) && (yDiff < MIN_CAPTURE_DIFF))
                    //{
                        return true;
                    //}
                //}
            }
             */
            return false;
        }

        /// <summary>
        /// Extracts lower 16-bit word from a 32-bit int
        /// </summary>
        /// <param name="number">input</param>
        /// <returns>lower word</returns>
        private static int LoWord(Int32 number)
        {
            return (number & 0xffff);
        }

        /// <summary>
        /// Decodes and handles WM_TOUCH message
        /// Unpacks message arguments and invokes appropriate touch events.
        /// </summary>
        /// <param name="message">window message</param>
        private void DecodeTouch(IntPtr wparam, IntPtr lparam)
        {
            // More than one touchinput structure may be with each message, so an array is needed 
            // to get all event information

            // Actual number of touch inputs
            int inputCount = LoWord(wparam.ToInt32());

            //just in case
            if (inputCount == 0)
            {
                Win32.CloseTouchInputHandle(lparam);
                return;
            }

            // Allocate the memory for the touch input structs
            TOUCHINPUT[] inputs;
            inputs = new TOUCHINPUT[inputCount];

            // Unpack message parameter into the array of TOUCHINPUT structures
            // Each one represents a message for one single contact
            if (!Win32.GetTouchInputInfo(lparam, inputCount, inputs, mdTouchInputSize))
            {
                // Get touch info failed
                Int32 lastError = Win32.GetLastError();

                Debug.Assert(false, "GetTouchInputInfo() failed error=0x", lastError.ToString("X"));

                return;
            }

            for (int i = 0; i < inputCount; ++i)
            {
                TOUCHINPUT touchInput = inputs[i];

                /*
                if ((touchInput.dwFlags & Win32.TOUCHEVENTF_MOVE) == 0)
                {
                    Console.Write(i.ToString() + ")x=" + touchInput.x.ToString());
                    Console.Write(", y=" + touchInput.y.ToString());
                    Console.Write(", Flags=0x" + touchInput.dwFlags.ToString("X"));
                    Console.Write(", Mask =0x" + touchInput.dwMask.ToString("X"));
                    Console.Write(", Time =0x" + touchInput.dwTime.ToString("X"));
                    Console.Write(", Info=0x" + touchInput.dwExtraInfo.ToString("X"));
                    Console.Write(", cx=0x" + touchInput.cxContact.ToString("X"));
                    Console.WriteLine(", cy=0x" + touchInput.cyContact.ToString("X"));
                }
                */

                // Assign a handler to this message
                EventHandler<TouchEventArgs> handler = null;
                if ((touchInput.dwFlags & Win32.TOUCHEVENTF_DOWN) != 0)
                {
#if STRESS_TEST
                    Console.WriteLine("Touch Down:"+touchInput.dwID.ToString()+" wparam=" + wparam.ToString("X") + ", lparam=" + lparam.ToString("X"));
#endif
                    handler = TouchDown;
                }
                else if ((touchInput.dwFlags & Win32.TOUCHEVENTF_UP) != 0)
                {
#if STRESS_TEST
                    Console.WriteLine("Touch Up:" + touchInput.dwID.ToString() + " wparam=" + wparam.ToString("X") + ", lparam=" + lparam.ToString("X"));
#endif
                    handler = TouchUp;
                }
                else if ((touchInput.dwFlags & Win32.TOUCHEVENTF_MOVE) != 0)
                {
#if STRESS_TEST
                    //Console.WriteLine("Touch Move:" + touchInput.dwID.ToString() + " wparam=" + wparam.ToString("X") + ", lparam=" + lparam.ToString("X"));
#endif
                    handler = TouchMove;
                }

                if (handler != null)
                {
                    TouchEventArgs args = new TouchEventArgs();

                    // Convert message parameters into a touch event argument structure
                    // Convert to pixels and convert screen to client coordinates 
                    // See [winuser.h] for:
                    //      #define TOUCH_COORD_TO_PIXEL(l)         ((l) / 100)
                    args.contactX = touchInput.cxContact / 100;
                    args.contactY = touchInput.cyContact / 100;
                    args.id = touchInput.dwID;
                    // Touch coordinates are in absolute screen space, not bound to the client rectangle.
                    // These will have to be converted into client space after the event is captured.
                    args.x = (int)(touchInput.x / 100 / scalingFactorX);
                    args.y = (int)(touchInput.y / 100 / scalingFactorY);

                    args.time = touchInput.dwTime;
                    args.mask = touchInput.dwMask;
                    args.flags = touchInput.dwFlags;

                    handler(this, args);

#if STRESS_TEST
                    //record the event
                    if ((touchInput.dwFlags & Win32.TOUCHEVENTF_DOWN) != 0)
                    {
                        args.wparam = wparam;
                        args.lparam = lparam;

                        TouchEventArgs other = new TouchEventArgs(args);
                        TouchEventArgs down = new TouchEventArgs(args);

                        string Message = "";

                        int foundID =0 ;
                        int count = touchEventArgsTrackList.Count;

                        //scan to see if this event exists
                        foreach (TouchEventArgs touchArgs in touchEventArgsTrackList)
                        {
                            if (touchArgs.id == args.id)
                            {
                                foundID++;
                                Message += args.id.ToString() + ',';
                            }
                        }

                        if (foundID>0)
                        {
                            DialogResult result;
                            Message +=  '\n';
                            result = MessageBox.Show("id count=" + foundID.ToString() + "\n"
                                             + Message
                                             ,
                                             "Duplicate id's found",
                                             MessageBoxButtons.OKCancel, MessageBoxIcon.Exclamation);
                        }

                        touchEventArgsTrackList.Add(other);
                        touchEventArgsTrackDownList.Add(down);
                    }

                    if ((touchInput.dwFlags & Win32.TOUCHEVENTF_UP) != 0)
                    {
                        //scan to see if this event has a matching down
                        foreach (TouchEventArgs touchArgs in touchEventArgsTrackList)
                        {
                            if (touchArgs.id == args.id)
                            {
                                touchEventArgsTrackList.Remove(touchArgs);
                                break;
                            }
                        }
                    }
#endif

                }
            }

            Win32.CloseTouchInputHandle(lparam);
        }
    }
}
