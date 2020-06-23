using System;
using System.Text;
using System.Runtime.InteropServices;

namespace Cab
{
    public static class Cabinet
    {
        public const int CB_MAX_CHUNK = 32768;
        public const int CB_MAX_DISK = 0x7ffffff;
        public const int CB_MAX_FILENAME = 256;
        public const int CB_MAX_CABINET_NAME = 256;
        public const int CB_MAX_CAB_PATH = 256;
        public const int CB_MAX_DISK_NAME = 256;

        public const int tcompMASK_TYPE = 0x000F;
        public const int tcompTYPE_NONE = 0x0000;
        public const int tcompTYPE_MSZIP = 0x0001;
        public const int tcompTYPE_QUANTUM = 0x0002;
        public const int tcompTYPE_LZX = 0x0003;
        public const int tcompBAD = 0x000F;
        public const int tcompMASK_LZX_WINDOW = 0x1F00;
        public const int tcompLZX_WINDOW_LO = 0x0F00;
        public const int tcompLZX_WINDOW_HI = 0x1500;
        public const int tcompSHIFT_LZX_WINDOW = 8;
        public const int tcompMASK_QUANTUM_LEVEL = 0x00F0;
        public const int tcompQUANTUM_LEVEL_LO = 0x0010;
        public const int tcompQUANTUM_LEVEL_HI = 0x0070;
        public const int tcompSHIFT_QUANTUM_LEVEL = 4;
        public const int tcompMASK_QUANTUM_MEM = 0x1F00;
        public const int tcompQUANTUM_MEM_LO = 0x0A00;
        public const int tcompQUANTUM_MEM_HI = 0x1500;
        public const int tcompSHIFT_QUANTUM_MEM = 8;
        public const int tcompMASK_RESERVED = 0xE000;

        public const int cpuUNKNOWN = -1;
        public const int cpu80286 = 0;
        public const int cpu80386 = 1;

        public enum FCIERROR : int
        {
            FCIERR_NONE,
            FCIERR_OPEN_SRC,
            FCIERR_READ_SRC,
            FCIERR_ALLOC_FAIL,
            FCIERR_TEMP_FILE,
            FCIERR_BAD_COMPR_TYPE,
            FCIERR_CAB_FILE,
            FCIERR_USER_ABORT,
            FCIERR_MCI_FAIL,
        }

        public enum FDIERROR : int
        {
            FDIERROR_NONE,
            FDIERROR_CABINET_NOT_FOUND,
            FDIERROR_NOT_A_CABINET,
            FDIERROR_UNKNOWN_CABINET_VERSION,
            FDIERROR_CORRUPT_CABINET,
            FDIERROR_ALLOC_FAIL,
            FDIERROR_BAD_COMPR_TYPE,
            FDIERROR_MDI_FAIL,
            FDIERROR_TARGET_FILE,
            FDIERROR_RESERVE_MISMATCH,
            FDIERROR_WRONG_CABINET,
            FDIERROR_USER_ABORT,
        }

        public enum FDIDECRYPTTYPE : int
        {
            fdidtNEW_CABINET,
            fdidtNEW_FOLDER,
            fdidtDECRYPT,
        }


        public enum FDINOTIFICATIONTYPE : int
        {
            fdintCABINET_INFO,
            fdintPARTIAL_FILE,
            fdintCOPY_FILE,
            fdintCLOSE_FILE_INFO,
            fdintNEXT_CABINET,
            fdintENUMERATE,
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct ERF
        {
            public int erfOper;
            public int erfType;
            public int fError;
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct CCAB
        {
            public static CCAB Create()
            {
                CCAB ccab = new CCAB();
                ccab.szDisk = new char[CB_MAX_DISK_NAME];
                ccab.szCab = new char[CB_MAX_CABINET_NAME];
                ccab.szCabPath = new char[CB_MAX_CAB_PATH];
                return ccab;
            }

            public uint cb;
            public uint cbFolderThresh;
            public uint cbReserveCFHeader;
            public uint cbReserveCFFolder;
            public uint cbReserveCFData;
            public int iCab;
            public int iDisk;
            public int fFailOnIncompressible;
            public ushort setID;
            [MarshalAs(UnmanagedType.ByValArray, SizeConst = CB_MAX_DISK_NAME)]
            public char[] szDisk;
            [MarshalAs(UnmanagedType.ByValArray, SizeConst = CB_MAX_CABINET_NAME)]
            public char[] szCab;
            [MarshalAs(UnmanagedType.ByValArray, SizeConst = CB_MAX_CAB_PATH)]
            public char[] szCabPath;
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct FDICABINETINFO
        {
            public int cbCabinet;
            public ushort cFolders;
            public ushort cFiles;
            public ushort setID;
            public ushort iCabinet;
            [MarshalAs(UnmanagedType.Bool)]
            public bool fReserve;
            [MarshalAs(UnmanagedType.Bool)]
            public bool hasprev;
            [MarshalAs(UnmanagedType.Bool)]
            public bool hasnext;
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct FDINOTIFICATION
        {
            public int cb;
            public IntPtr psz1;
            public IntPtr psz2;
            public IntPtr psz3;
            public IntPtr pv;
            public int hf;
            public ushort date;
            public ushort time;
            public ushort attribs;
            public ushort setID;
            public ushort iCabinet;
            public ushort iFolder;
            public FDIERROR fdie;
        }

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        public delegate int FciFilePlacedFn(
            ref CCAB pccab,
            IntPtr pszFile,
            int cbFile,
            int fContinuation,
            IntPtr pv);

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        public delegate IntPtr FciAllocFn(
            uint cb);

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        public delegate void FciFreeFn(
            IntPtr memory);

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        public delegate int FciOpenFn(
            IntPtr pszFile,
            Rtl.OFLAG oflag,
            Rtl.PMODE pmode,
            ref int err,
            IntPtr pv);

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        public delegate uint FciReadFn(
            int hf,
            IntPtr memory,
            uint cb,
            ref int err,
            IntPtr pv);

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        public delegate uint FciWriteFn(
            int hf,
            IntPtr memory,
            uint cb,
            ref int err,
            IntPtr pv);

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        public delegate int FciCloseFn(
            int hf,
            ref int err,
            IntPtr pv);

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        public delegate int FciSeekFn(
            int hf,
            int dist,
            int seektype,
            ref int err,
            IntPtr pv);

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        public delegate int FciDeleteFn(
            IntPtr pszFile,
            ref int err,
            IntPtr pv);

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        public delegate int FciGetTempFileFn(
            IntPtr pszTempName,
            int cbTempName,
            IntPtr pv);

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        public delegate int FciGetNextCabinetFn(
            ref CCAB pccab,
            uint cbPrevCab,
            IntPtr pv);

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        public delegate int FciStatusFn(
            uint typeStatus,
            uint cb1,
            uint cb2,
            IntPtr pv);

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        public delegate int FciGetOpenInfoFn(
            IntPtr pszName,
            ref ushort pdate,
            ref ushort ptime,
            ref ushort pattribs,
            ref int err,
            IntPtr pv);

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        public delegate IntPtr FdiAllocFn(
            uint cb);

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        public delegate void FdiFreeFn(
            IntPtr memory);

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        public delegate int FdiNotifyFn(
            FDINOTIFICATIONTYPE fdint,
            ref FDINOTIFICATION pfdin);

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        public delegate int FdiOpenFn(
            IntPtr pszFile,
            Rtl.OFLAG oflag,
            Rtl.PMODE pmode);

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        public delegate uint FdiReadFn(
            int hf,
            IntPtr pv,
            uint cb);

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        public delegate uint FdiWriteFn(
            int hf,
            IntPtr pv,
            uint cb);

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        public delegate int FdiCloseFn(
            int hf);

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        public delegate int FdiSeekFn(
            int hf,
            int dist,
            int seektype);

#if !NETFX_CORE
        [DllImport("cabinet.dll", CallingConvention = CallingConvention.Cdecl)]
        public static extern int FCICreate(
            ref ERF perf,
            FciFilePlacedFn pfnfcifp,
            FciAllocFn pfna,
            FciFreeFn pfnf,
            FciOpenFn pfnopen,
            FciReadFn pfnread,
            FciWriteFn pfnwrite,
            FciCloseFn pfnclose,
            FciSeekFn pfnseek,
            FciDeleteFn pfndelete,
            FciGetTempFileFn pfnfcigtf,
            ref CCAB pccab,
            IntPtr pv);

        [DllImport("cabinet.dll", CallingConvention = CallingConvention.Cdecl)]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool FCIAddFile(
            int handle,
            [MarshalAs(UnmanagedType.LPStr)]
            string pszSourceFile,
            [MarshalAs(UnmanagedType.LPStr)]
            string pszFileName,
            int fExecute,
            FciGetNextCabinetFn pfnfcignc,
            FciStatusFn pfnfcis,
            FciGetOpenInfoFn pfnfcgoi,
            ushort typeCompress);

        [DllImport("cabinet.dll", CallingConvention = CallingConvention.Cdecl)]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool FCIFlushCabinet(
            int handle,
            int fGetNextCab,
            FciGetNextCabinetFn pfnfcignc,
            FciStatusFn pfnfcis);

        [DllImport("cabinet.dll", CallingConvention = CallingConvention.Cdecl)]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool FCIFlushFolder(
            int handle,
            FciGetNextCabinetFn pfnfcignc,
            FciStatusFn pfnfcis);

        [DllImport("cabinet.dll", CallingConvention = CallingConvention.Cdecl)]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool FCIDestroy(
            int handle);

#endif

        [DllImport("cabinet.dll", CallingConvention = CallingConvention.Cdecl)]
        public static extern IntPtr FDICreate(
            FdiAllocFn pfna,
            FdiFreeFn pfnf,
            FdiOpenFn pfnopen,
            FdiReadFn pfnread,
            FdiWriteFn pfnwrite,
            FdiCloseFn pfnclose,
            FdiSeekFn pfnseek,
            int cputype,
            ref ERF perf);
        
        [DllImport("cabinet.dll", CallingConvention = CallingConvention.Cdecl)]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool FDIIsCabinet(
            int hf,
            ref FDICABINETINFO pfdici);

        [DllImport("cabinet.dll", CallingConvention = CallingConvention.Cdecl)]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool FDICopy(
            IntPtr hfdi,
            [MarshalAs(UnmanagedType.LPStr)]
            string pszCabinet,
            [MarshalAs(UnmanagedType.LPStr)]
            string pszCabPath,
            int flags,
            FdiNotifyFn pfnnotify,
            IntPtr unused,
            IntPtr pvUser);

        [DllImport("cabinet.dll", CallingConvention = CallingConvention.Cdecl)]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool FDIDestroy(
            IntPtr hfdi);

        static public char DirectorySeparatorChar = '\\';
    }
}
