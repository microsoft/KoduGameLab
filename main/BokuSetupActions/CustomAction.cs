using System;
using System.Collections.Generic;
using System.Text;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Threading;
using Microsoft.Deployment.WindowsInstaller;
using Microsoft.Win32;

namespace BokuSetupActions
{
    public class CustomActions
    {
        private static bool ArrayContains(string[] arr, string val)
        {
            foreach (string str in arr)
            {
                if (str == val)
                    return true;
            }
            return false;
        }

        [CustomAction]
        public static ActionResult DetectSoftwarePackages(Session session)
        {
            session.Log("Begin DetectSoftwarePackages");

            bool netFramework;
            bool xnaFramework;

            netFramework = DetectNetFramework40();
            xnaFramework = DetectXnaFramework40();

            session["NETFRAMEWORK40"] = netFramework ? "1" : null;
            session["XNAFRAMEWORK40"] = xnaFramework ? "1" : null;

            session.Log(String.Format("DetectSoftwarePackages: {0} {1}", netFramework, xnaFramework));
            
            session.Log("End DetectSoftwarePackages");

            return ActionResult.Success;
        }

        public static bool DetectNetFramework40()
        {
            bool exists;

            try
            {
                RegistryKey key = Registry.LocalMachine.OpenSubKey(@"SOFTWARE\Microsoft\NET Framework Setup\NDP\v4\Client", false);

                if (key != null)
                {
                    string[] valnames = key.GetValueNames();
                    exists = ArrayContains(valnames, "Install");
                    key.Close();
                }
                else
                {
                    exists = false;
                }

            }
            catch
            {
                exists = false;
            }

            return exists;
        }

        public static bool DetectXnaFramework40()
        {
            bool exists;

            try
            {
                RegistryKey key = Registry.LocalMachine.OpenSubKey(@"SOFTWARE\Microsoft\XNA\Framework\v4.0", false);

                if (key != null)
                {
                    string[] valnames = key.GetValueNames();
                    exists = ArrayContains(valnames, "Installed");
                    key.Close();
                }
                else
                {
                    exists = false;
                }
            }
            catch
            {
                exists = false;
            }

            return exists;
        }

        [CustomAction]
        public static ActionResult UninstallClickOnceKodu(Session session)
        {
            session.Log("Begin UninstallClickOnceKodu");

            UninstallClickOnceKodu();

            session.Log("End UninstallClickOnceKodu");

            return ActionResult.Success;
        }

        public static void UninstallClickOnceKodu()
        {
            UninstallClickOnceKodu(Registry.LocalMachine, "Kodu - Installed for All Users");
            UninstallClickOnceKodu(Registry.LocalMachine, "Boku - Installed for All Users");
            UninstallClickOnceKodu(Registry.CurrentUser, "Kodu - Installed for Current User");
            UninstallClickOnceKodu(Registry.CurrentUser, "Boku - Installed for Current User");
        }

        private static void UninstallClickOnceKodu(RegistryKey rootKey, string koduKeyName)
        {
            try
            {
                RegistryKey baseKey = rootKey.OpenSubKey(@"Software\Microsoft\Windows\CurrentVersion\Uninstall", true);

                if (baseKey != null)
                {
                    RegistryKey koduKey = baseKey.OpenSubKey(koduKeyName, false);

                    if (koduKey != null)
                    {
                        string uninstallString = koduKey.GetValue("UninstallString") as string;

                        if (uninstallString != null)
                        {
                            try
                            {
                                string[] parts = uninstallString.Split('/');

                                string args = "";
                                for (int i = 1; i < parts.Length; ++i)
                                    args += " /" + parts[i].Trim();

                                ProcessStartInfo psi = new ProcessStartInfo();
                                psi.FileName = parts[0].Trim();
                                psi.Arguments = args.Trim();

                                Process proc = Process.Start(psi);

                                proc.WaitForInputIdle();

                                AutomateYesButtonClick();

                                proc.WaitForExit();
                            }
                            catch { }
                        }

                        koduKey.Close();
                        baseKey.DeleteSubKey(koduKeyName, false);
                    }

                    baseKey.Close();
                }
            }
            catch { }
        }

        private static void AutomateYesButtonClick()
        {
            IntPtr hWnd = IntPtr.Zero;

            // Try repeatedly to find the yes/no dialog box.
            int count = 0;
            while (true)
            {
                hWnd = Win32.FindWindowByCaption(IntPtr.Zero, "Kodu Installer");

                if (hWnd != IntPtr.Zero)
                    break;

                if (count > 100)
                    break;

                count += 1;

                Thread.Sleep(100);
            }

            if (hWnd == null)
                return;

            Win32.EnumChildWindows(hWnd, AutomateYesButtonClick_EnumChildWindowsCallback, IntPtr.Zero);
        }

        private static bool AutomateYesButtonClick_EnumChildWindowsCallback(IntPtr hWnd, IntPtr lParam)
        {
            int len = Win32.GetWindowTextLength(hWnd);
            StringBuilder sb = new StringBuilder(len + 1);
            Win32.GetWindowText(hWnd, sb, sb.Capacity);

            string text = sb.ToString();

            if (text == "&Yes")
            {
                const uint BM_CLICK = 0x00F5;
                Win32.SendMessage(hWnd, BM_CLICK, IntPtr.Zero, IntPtr.Zero);
                return false;
            }
            else
            {
                return true;
            }
        }
    }
}
