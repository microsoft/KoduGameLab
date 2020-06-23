using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;

namespace Boku.Common
{
    static class WinStoreHelpers
    {
        const long APPMODEL_ERROR_NO_PACKAGE = 15700L;

        [DllImport("kernel32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
        static extern int GetCurrentPackageFullName(ref int packageFullNameLength, StringBuilder packageFullName);

        static bool runningAsUWP = false;       // Is the application running as a UWP app.
        static bool runningAsUWPValid = false;  // Is the above bool valid or does it need to be checked?

        static public bool RunningAsUWP
        {
            get
            {
                if (!runningAsUWPValid)
                {
                    runningAsUWP = IsRunningAsUwp();
                    runningAsUWPValid = true;
                }

                //runningAsUWP = true;    // DEBUG HACK, REMOVE BEFORE CHECKING IN!!!

                return runningAsUWP;
            }
        }

        /// <summary>
        /// Returns true if the application is running as a UWP app, 
        /// ie running under Centennial as a Windows Store app.
        /// </summary>
        /// <returns></returns>
        static bool IsRunningAsUwp()
        {
            if (IsWindows7OrLower)
            {
                return false;
            }
            else
            {
                int length = 0;
                StringBuilder sb = new StringBuilder(0);
                int result = GetCurrentPackageFullName(ref length, sb);

                sb = new StringBuilder(length);
                result = GetCurrentPackageFullName(ref length, sb);

                // If result is not ERROR then this is UWP version. 
                bool runningAsUWP = result != APPMODEL_ERROR_NO_PACKAGE;
                return runningAsUWP;
            }
        }

        private static bool IsWindows7OrLower
        {
            get
            {
                int versionMajor = Environment.OSVersion.Version.Major;
                int versionMinor = Environment.OSVersion.Version.Minor;
                double version = versionMajor + (double)versionMinor / 10;
                return version <= 6.1;
            }
        }
    }
}
