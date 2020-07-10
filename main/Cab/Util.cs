// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System;
using System.Collections.Generic;
using System.Text;
using System.IO;
using System.Runtime.InteropServices;

#if NETFX_CORE
using Windows.Storage;
using Windows.Storage.FileProperties;
using Windows.Storage.Streams;
#endif

namespace Cab
{
    public static class Util
    {
        /// <summary>
        /// This helper method does what Marshal.PtrToStringAuto should do, but doesn't.
        /// </summary>
        /// <param name="ptr"></param>
        /// <returns></returns>
        public static string StringFromPtr(IntPtr ptr)
        {
            string filename;
            string filenameUnic = Marshal.PtrToStringUni(ptr);
            string filenameAnsi = Marshal.PtrToStringAnsi(ptr);

            if (filenameAnsi.Length > 1)
                filename = filenameAnsi;
            else
                filename = filenameUnic;

            return filename;
        }

#if NETFX_CORE

        /// <summary>
        /// Get RTL-compatible file attribute flags.
        /// </summary>
        /// <param name="fa"></param>
        /// <returns></returns>
        public static ushort FileAttributesToDos(FileAttributes fa)
        {
            ushort bits = Rtl._A_NORMAL;

            if (0 != (fa & FileAttributes.ReadOnly))
                bits |= Rtl._A_RDONLY;

            if (0 != (fa & FileAttributes.Archive))
                bits |= Rtl._A_ARCH;

            return bits;
        }

        /// <summary>
        /// Get .NET-compatible seek type value.
        /// </summary>
        /// <param name="seektype"></param>
        /// <returns></returns>
        public static SeekOrigin SeekOriginFromDos(int seektype)
        {
            if (seektype == Rtl.SEEK_END)
                return SeekOrigin.End;
            else if (seektype == Rtl.SEEK_SET)
                return SeekOrigin.Begin;
            else
                return SeekOrigin.Current;
        }
        
        /// <summary>
        /// Get .NET-compatible file mode value.
        /// </summary>
        /// <param name="oflag"></param>
        /// <returns></returns>
        public static FileMode FileModeFromDos(Rtl.OFLAG oflag)
        {
            if (0 != (oflag & Rtl.OFLAG._O_TRUNC))
                return FileMode.Create;
            else if (0 != (oflag & Rtl.OFLAG._O_CREAT))
                return FileMode.OpenOrCreate;
            else if (0 != (oflag & Rtl.OFLAG._O_APPEND))
                return FileMode.Append;
            else
                return FileMode.Open;
        }

        /// <summary>
        /// Get .NET-compatible file access value.
        /// </summary>
        /// <param name="oflag"></param>
        /// <param name="pmode"></param>
        /// <returns></returns>
        public static FileAccess FileAccessFromDos(Rtl.OFLAG oflag, Rtl.PMODE pmode)
        {
            if (0 != (oflag & Rtl.OFLAG._O_RDONLY))
                return FileAccess.Read;
            else if (0 == (pmode & Rtl.PMODE._S_IWRITE))
                return FileAccess.Read;
            else
                return FileAccess.ReadWrite;
        }
        
#else

        /// <summary>
        /// Get RTL-compatible file attribute flags.
        /// </summary>
        /// <param name="fa"></param>
        /// <returns></returns>
        public static ushort FileAttributesToDos(FileAttributes fa)
        {
            ushort bits = Rtl._A_NORMAL;

            if (0 != (fa & FileAttributes.ReadOnly))
                bits |= Rtl._A_RDONLY;

            if (0 != (fa & FileAttributes.Hidden))
                bits |= Rtl._A_HIDDEN;

            if (0 != (fa & FileAttributes.System))
                bits |= Rtl._A_SYSTEM;

            if (0 != (fa & FileAttributes.Archive))
                bits |= Rtl._A_ARCH;

            return bits;
        }

        /// <summary>
        /// Get .NET-compatible seek type value.
        /// </summary>
        /// <param name="seektype"></param>
        /// <returns></returns>
        public static SeekOrigin SeekOriginFromDos(int seektype)
        {
            if (seektype == Rtl.SEEK_END)
                return SeekOrigin.End;
            else if (seektype == Rtl.SEEK_SET)
                return SeekOrigin.Begin;
            else
                return SeekOrigin.Current;
        }

        /// <summary>
        /// Get .NET-compatible file mode value.
        /// </summary>
        /// <param name="oflag"></param>
        /// <returns></returns>
        public static FileMode FileModeFromDos(Rtl.OFLAG oflag)
        {
            if (0 != (oflag & Rtl.OFLAG._O_TRUNC))
                return FileMode.Create;
            else if (0 != (oflag & Rtl.OFLAG._O_CREAT))
                return FileMode.OpenOrCreate;
            else if (0 != (oflag & Rtl.OFLAG._O_APPEND))
                return FileMode.Append;
            else
                return FileMode.Open;
        }

        /// <summary>
        /// Get .NET-compatible file access value.
        /// </summary>
        /// <param name="oflag"></param>
        /// <param name="pmode"></param>
        /// <returns></returns>
        public static FileAccess FileAccessFromDos(Rtl.OFLAG oflag, Rtl.PMODE pmode)
        {
            if (0 != (oflag & Rtl.OFLAG._O_RDONLY))
                return FileAccess.Read;
            else if (0 == (pmode & Rtl.PMODE._S_IWRITE))
                return FileAccess.Read;
            else
                return FileAccess.ReadWrite;
        }

        /// <summary>
        /// Get RTL-compatible file date and time values.
        /// </summary>
        /// <param name="dt"></param>
        /// <param name="pdate"></param>
        /// <param name="ptime"></param>
        public static void DateTimeToDos(DateTime dt, out ushort pdate, out ushort ptime)
        {
            long filetime = dt.ToFileTimeUtc();
            System.Runtime.InteropServices.ComTypes.FILETIME ft;
            ft.dwLowDateTime = (int)(filetime & 0xFFFFFFFF);
            ft.dwHighDateTime = (int)(filetime >> 32);
            Win32.FileTimeToDosDateTime(ref ft, out pdate, out ptime);
        }
#endif
    }
}
