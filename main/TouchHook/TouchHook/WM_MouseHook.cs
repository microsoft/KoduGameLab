// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Runtime.InteropServices;

namespace TouchHook
{
    public class WM_MouseHook : WindowsHook
    {
        private WindowsHook LowLevelMouseHook = null;
        private int timeStamp;

        public WM_MouseHook(IntPtr hWnd)
            : base( hWnd, HookType.WH_MOUSE )
        {
            HookInvoked += new HookEventHandler(MouseHookInvoked);

            //LowLevelMouseHook = new WindowsHook( hWnd, HookType.WH_MOUSE_LL, new HookDelegate(this.CoreHookProc));
        }

        public override void InstallHook()
        {
            base.InstallHook();

            //LowLevelMouseHook.InstallHook();
        }

        public override void UninstallHook()
        {
            base.UninstallHook();

            //LowLevelMouseHook.UninstallHook();
        }

        // Touch event handlers
        public event EventHandler<MouseEventArgs> MouseDown;   // touch down event handler
        public event EventHandler<MouseEventArgs> MouseUp;     // touch up event handler
        public event EventHandler<MouseEventArgs> MouseMove;   // touch move event handler

        public int MouseHookInvoked(object sender, HookEventArgs e)
        {
            uint message = (uint)e.wParam;
            EventHandler<MouseEventArgs> handler = null;
            MouseEventArgs args = null;
            switch (message)
            {
                case Win32.WM_MOUSEMOVE:
                    {
                        handler = MouseMove;
                        args = DecodeMouseMove(e.lParam);
                    }
                    break;
                case Win32.WM_LBUTTONDOWN:
                    {
                        handler = MouseDown;
                        args = DecodeLowLevelMouse(e.lParam);
                    }
                    break;
                case Win32.WM_LBUTTONUP:
                    {
                        handler = MouseUp;
                        args = DecodeLowLevelMouse(e.lParam);
                    }
                    break;
                default:
                    break;
            }

            if (handler != null && args != null)
            {
                handler(this, args);
            }
            return 0;
        }

        private MouseEventArgs DecodeMouseMove(IntPtr lParam)
        {
            MOUSEHOOKSTRUCT input = (MOUSEHOOKSTRUCT)Marshal.PtrToStructure(lParam, typeof(MOUSEHOOKSTRUCT));

            MouseEventArgs args = new MouseEventArgs();
            args.x = input.pt.X;
            args.y = input.pt.Y;
            args.hitTestCode = input.wHitTestCode;

            return args;
        }

        private MouseEventArgs DecodeLowLevelMouse(IntPtr lParam)
        {
            MSLLHOOKSTRUCT input = (MSLLHOOKSTRUCT)Marshal.PtrToStructure(lParam, typeof(MSLLHOOKSTRUCT));

            if (timeStamp == input.time)
            {
                //Console.WriteLine("Time stamp match: " + timeStamp.ToString());
                //return null;
            }
            timeStamp = input.time;
            MouseEventArgs args = new MouseEventArgs();
            args.x = input.pt.X;
            args.y = input.pt.Y;

            return args;
        }
    }
}
