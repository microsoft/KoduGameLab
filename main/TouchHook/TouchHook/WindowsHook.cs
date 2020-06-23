using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Runtime.InteropServices;
using System.Diagnostics;
using System.Reflection;

#if NETFX_CORE
    using Windows.Security;
#else
    using System.Security.Permissions;
#endif

/* Author: Neil Petrick
 * 
 * License: Public Domain.
 * 
 * Usage:
 *
 * Inherit from this class, and override the WndProc function in your derived class, 
 * in which you handle your windows messages.
 * 
 * To start recieving the message, create an instance of your derived class, passing in the
 * window handle of the window you want to listen for messages for.
 * 
 * in XNA: this would be the Game.Window.Handle property
 * in Winforms Form.Handle property
 */

namespace TouchHook
{
    public class WindowsHook : IDisposable
    {
        /// <summary>
        ///  Defines the windows proc delegate to pass into the windows hook
        /// </summary>                  
        public delegate int HookDelegate(int nCode, IntPtr wParam, IntPtr lParam);

        protected IntPtr hHook = IntPtr.Zero;
        protected IntPtr hWnd = IntPtr.Zero;
        protected HookType hookType;
        // Stored here to stop it from getting garbage collected
        protected HookDelegate hookDelegate = null;

        /// <summary>
        /// Event delegate
        /// </summary>
        /// <param name="hWnd"></param>
        public delegate int HookEventHandler(object sender, HookEventArgs e);

        /// <summary>
        /// Event: HookInvoked
        /// </summary>
        /// <param name="hWnd"></param>
        public event HookEventHandler HookInvoked;
        protected int OnHookInvoked(HookEventArgs e)
        {
            int result = 0;
            if (HookInvoked != null)
            {
                result = HookInvoked(this, e);
            }
            return result;
        }

        /// <summary>
        /// Class constructor
        /// </summary>
        /// <param name="hWnd">
        /// Handle to the window we are going to hook into
        /// </param>
        /// <param name="hook">
        /// Determines what type of hook this is
        /// </param>
        public WindowsHook(IntPtr hWnd, HookType hook)
        {
            this.hWnd = hWnd;

            this.hookType = hook;
            this.hookDelegate = new HookDelegate(this.CoreHookProc);
        }

        public WindowsHook(IntPtr hWnd, HookType hook, HookDelegate proc)
        {
            this.hWnd = hWnd;

            this.hookType = hook;
            this.hookDelegate = proc;
        }

        ~WindowsHook()
        {
            Dispose(false);
        }

        public virtual void InstallHook()
        {
            Process process = Process.GetCurrentProcess();
            ProcessModule module = process.MainModule;
            IntPtr hModule = Win32.GetModuleHandle(module.ModuleName);

            uint threadId = Win32.GetWindowThreadProcessId(hWnd, IntPtr.Zero);

            hHook = Win32.SetWindowsHookEx(hookType, hookDelegate, hModule, threadId);
        }

        public virtual void UninstallHook()
        {
            if (hHook != IntPtr.Zero)
            {
                Win32.UnhookWindowsHookEx(hHook);
            }
        }

#if !NETFX_CORE
        [PermissionSet(SecurityAction.Demand, Name = "FullTrust")]
#endif
        protected int CoreHookProc(int nCode, IntPtr wParam, IntPtr lParam)
        {
            //Message msg = (Message)Marshal.PtrToStructure(lParam, typeof(Message));
            //CallWndStruct cwstruct = (CallWndStruct)Marshal.PtrToStructure(lParam, typeof(CallWndStruct));

            //Console.WriteLine("CoreHookProc: m.msg=0x" + m.msg.ToString("X"));
            //Console.WriteLine("CoreHookProc: nCode=" + nCode.ToString() + "wParam=0x" + wParam.ToString("X"));

            HookEventArgs e = new HookEventArgs();
            e.HookCode = nCode;
            e.wParam = wParam;
            e.lParam = lParam;
            //e.message = m;
            if (hookType == HookType.WH_CALLWNDPROC)
                e.cwstruct = (CallWndStruct)Marshal.PtrToStructure(lParam, typeof(CallWndStruct));
            else
            if (hookType == HookType.WH_GETMESSAGE)
                e.message = (Message)Marshal.PtrToStructure(lParam, typeof(Message));
            else
                Debug.Assert(false, "Unexpected Hook Type!");

            if (nCode >= 0 ) //&& wParam != IntPtr.Zero)
            {
                int result = OnHookInvoked(e);
                if (result != 0)
                {
                    return result;
                }
            }

            //TODO: Refactor to avoid duplicate code.
            if (hookType == HookType.WH_CALLWNDPROC)
                return Win32.CallNextHookEx(hHook, nCode, wParam, ref e.cwstruct); //windows 8

            return Win32.CallNextHookEx(hHook, nCode, wParam, ref e.message); //windows 7
        }

        #region IDisposable Members

        public void Dispose()
        {
            Dispose(true);
        }

        private void Dispose(bool disposing)
        {
            if (disposing)
            {
                // Free managed resources here
            }
            // Free unmanaged resources here
            UninstallHook();
        }

        #endregion
    }
}