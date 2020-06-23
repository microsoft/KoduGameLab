using System;
using System.IO;
using System.Text;
using System.Diagnostics;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Reflection;

#if NETFX_CORE
    using Windows.Storage;
#endif

namespace Cab
{
    /// <summary>
    /// Extracts all files from a CAB archive.
    /// 
    /// Usage:
    ///     Decompressor decomp = new Decompressor();
    ///     decomp.Create();
    ///     decomp.Expand(null, @"C:\MyCabFiles\MyArchive.cab");
    ///     decomp.Destroy();
    ///     
    /// Limitations:
    ///     Due to limitations in the system component "cabinet.dll", only one
    ///     Decompress class may exist in the "created" state at at time.
    ///     
    /// </summary>
    public partial class Decompressor
    {
        public Decompressor()
        {
            // Store a null file stream at index zero, since cabinet.dll doesn't like
            // file handles with a value of zero (even though it is a valid file handle value!)
            streams.Add(Stream.Null);
            filenames.Add("null");

            InitDelegates();
        }

        /// <summary>
        /// Override to receive progress callbacks during decompression.
        /// </summary>
        public virtual void Progress()
        {
        }

        /// <summary>
        /// Override to selectively skip files when extracting, or modify their
        /// destination path and filename information.
        /// </summary>
        /// <param name="filepath">The rooted path where the file will be extracted to. You may optionally modify this value.</param>
        /// <param name="filename">The filename without any path information. You may optionally modify this value.</param>
        /// <returns>true if the file should be extracted, false if it should be skipped</returns>
        public virtual bool QueryExpandFile(ref string filepath, ref string filename)
        {
            return true;
        }

        /// <summary>
        /// Initialize the decompressor context. Due to limitations in cabinet.dll,
        /// Only one decomressor may be in the "created" state at any time.
        /// </summary>
        public void Create()
        {
            // cabinet.dll, I call upon you... Create a decompressor context!
            hfdi = Cabinet.FDICreate(
                FdiAllocDelegate,
                FdiFreeDelegate,
                FdiOpenDelegate,
                FdiReadDelegate,
                FdiWriteDelegate,
                FdiCloseDelegate,
                FdiSeekDelegate,
                Cabinet.cpuUNKNOWN,
                ref erf);

            if (hfdi == IntPtr.Zero)
            {
                throw new Exception("Failed to create CAB decompressor context.");
            }
        }

        /// <summary>
        /// Expand all files contained in the specified CAB archive.
        /// </summary>
        /// <param name="destinationFolder">Where the extracted files will go.
        /// Folder heirarchy in the CAB will be created relative to this location.
        /// If null, current working directory is used.</param>
        /// <param name="fullPathToCabinet">Full path to the archive you want to decompress.</param>
        public void Expand(string destinationFolder, string fullPathToCabinet)
        {
#if NETFX_CORE
            //Debug.Assert(!String.IsNullOrEmpty(destinationFolder));
            //Debug.Assert(destinationFolder.Contains(":"), "destinationFolder needs to be full path.");
            //Debug.Assert(fullPathToCabinet.Contains(":"), "fullPathToCabinet needs to be full path.");
#else
            // if destination folder is null, use current working folder
            if (destinationFolder == null)
                destinationFolder = Assembly.GetExecutingAssembly().Location;

            destinationFolder = Path.GetFullPath(destinationFolder);
#endif

            // Ensure the path ends with a backslash.
            if (!destinationFolder.EndsWith("" + Cabinet.DirectorySeparatorChar))
                destinationFolder = destinationFolder + Cabinet.DirectorySeparatorChar;

#if !NETFX_CORE
            // Create the folder if necessary
            if (!Directory.Exists(destinationFolder))
                Directory.CreateDirectory(destinationFolder);

            fullPathToCabinet = Path.GetFullPath(fullPathToCabinet);
#endif

            // Separate path and filename information, since this is
            // how cabinet.dll wants it.
            string filename = Path.GetFileName(fullPathToCabinet);
            string folder = Path.GetDirectoryName(fullPathToCabinet);

            // Ensure folder ends with a backslash
            if (!folder.EndsWith("" + Cabinet.DirectorySeparatorChar))
                folder = folder + Cabinet.DirectorySeparatorChar;

            // Store this for use later.
            this.destinationFolder = destinationFolder;

            // cabinet.dll, I call upon your power... Extract these files!
            bool success = Cabinet.FDICopy(
                hfdi,
                filename,
                folder,
                0,
                FdiNotifyDelegate,
                IntPtr.Zero,
                IntPtr.Zero);

            if (!success)
            {
                throw new Exception("Failed to expand cabinet: " + fullPathToCabinet + " with error : " + erf.erfOper.ToString()); 
            }
        }

        /// <summary>
        /// Flushes open files and destroys the decompressor context.
        /// </summary>
        public void Destroy()
        {
            // cabinet.dll, I call upon you... Destroy this context!
            bool success = Cabinet.FDIDestroy(hfdi);

            if (!success)
            {
                throw new Exception("Failed to destroy cab decompressor.");
            }
        }
    }

    partial class Decompressor
    {
        IntPtr hfdi;

        Cabinet.ERF erf;

        List<Stream> streams = new List<Stream>();

        List<string> filenames = new List<string>();

        string destinationFolder;


        byte[] scratchBuffer = new byte[0];

        void ResizeScratchBuffer(int size)
        {
            if (scratchBuffer.Length < size)
            {
                scratchBuffer = new byte[size];
            }
        }

        Cabinet.FdiAllocFn FdiAllocDelegate;
        Cabinet.FdiFreeFn FdiFreeDelegate;
        Cabinet.FdiNotifyFn FdiNotifyDelegate;
        Cabinet.FdiOpenFn FdiOpenDelegate;
        Cabinet.FdiReadFn FdiReadDelegate;
        Cabinet.FdiWriteFn FdiWriteDelegate;
        Cabinet.FdiCloseFn FdiCloseDelegate;
        Cabinet.FdiSeekFn FdiSeekDelegate;

        void InitDelegates()
        {
            FdiAllocDelegate = new Cabinet.FdiAllocFn(FdiAlloc);
            FdiFreeDelegate = new Cabinet.FdiFreeFn(FdiFree);
            FdiNotifyDelegate = new Cabinet.FdiNotifyFn(FdiNotify);
            FdiOpenDelegate = new Cabinet.FdiOpenFn(FdiOpen);
            FdiReadDelegate = new Cabinet.FdiReadFn(FdiRead);
            FdiWriteDelegate = new Cabinet.FdiWriteFn(FdiWrite);
            FdiCloseDelegate = new Cabinet.FdiCloseFn(FdiClose);
            FdiSeekDelegate = new Cabinet.FdiSeekFn(FdiSeek);
        }

        IntPtr FdiAlloc(uint cb)
        {
            return Marshal.AllocHGlobal((int)cb);
        }

        void FdiFree(IntPtr memory)
        {
            Marshal.FreeHGlobal(memory);
        }

        int FdiNotify(
            Cabinet.FDINOTIFICATIONTYPE fdint,
            ref Cabinet.FDINOTIFICATION pfdin)
        {
            int result = 0;

            switch (fdint)
            {
                case Cabinet.FDINOTIFICATIONTYPE.fdintPARTIAL_FILE:
                    throw new Exception("Multi-part cabinets not supported by this wrapper.");

                case Cabinet.FDINOTIFICATIONTYPE.fdintCOPY_FILE:
                    {
                        // Separate file and path information.
                        string arg = Util.StringFromPtr(pfdin.psz1);
                        string file = Path.GetFileName(arg);
                        string path = Path.GetDirectoryName(arg);

                        // Ensure the path ends in a backslash
                        if (!path.EndsWith("" + Cabinet.DirectorySeparatorChar))
                            path = path + Cabinet.DirectorySeparatorChar;

                        // Build the destination path
                        path = destinationFolder + path;

                        // Optionally
                        if (!QueryExpandFile(ref path, ref file))
                            return 0;

                        // Ensure filename is still just a filename without path info.
                        file = Path.GetFileName(file);

#if NETFX_CORE
                        Debug.Assert(path.Contains(":"), "Needs to be full path");
#else
                        // Ensure path is still rooted.
                        path = Path.GetFullPath(path);
#endif

                        // Ensure the path still ends in a backslash
                        if (!path.EndsWith("" + Cabinet.DirectorySeparatorChar))
                            path = path + Cabinet.DirectorySeparatorChar;

#if !NETFX_CORE
                        // Create the destination path if necessary
                        if (!Directory.Exists(path))
                            Directory.CreateDirectory(path);
#endif

                        string fullPathAndFilename = path + file;

                        int hf = FdiOpen(
                            Marshal.StringToHGlobalAnsi(fullPathAndFilename),
                            Rtl.OFLAG._O_BINARY | Rtl.OFLAG._O_CREAT | Rtl.OFLAG._O_WRONLY,
                            Rtl.PMODE._S_IREAD | Rtl.PMODE._S_IWRITE);

                        return hf;
                    }

                case Cabinet.FDINOTIFICATIONTYPE.fdintCLOSE_FILE_INFO:
                    {
                        FdiClose(pfdin.hf);
                        return 1;
                    }

                case Cabinet.FDINOTIFICATIONTYPE.fdintNEXT_CABINET:
                    throw new Exception("Multi-part cabinets not supported by this wrapper.");

                case Cabinet.FDINOTIFICATIONTYPE.fdintCABINET_INFO:
                case Cabinet.FDINOTIFICATIONTYPE.fdintENUMERATE:
                    // Do nothing.
                    break;

                default:
                    Debug.Assert(false, "A lot of the above is not impl.");

                    Progress();
                    break;
            }

            return result;
        }

        int FdiOpen(
            IntPtr pszFile,
            Rtl.OFLAG oflag,
            Rtl.PMODE pmode)
        {
            string filename = Util.StringFromPtr(pszFile);
            FileMode fileMode = Util.FileModeFromDos(oflag);

            FileAccess fileAccess = Util.FileAccessFromDos(oflag, pmode);

            int hf = -1;

            for (int i = 0; i < streams.Count; ++i)
            {
                if (streams[i] == null)
                {
                    hf = i;
                    break;
                }
            }

            if (hf == -1)
            {
                hf = streams.Count;
                streams.Add(null);
                filenames.Add(null);
            }

#if NETFX_CORE
            // For WinRT the FileAccess is kind of messed up.  It looks like the problem is
            // that the original code expected to be able to open a file with read/write access.
            // WinRT doesn't seem to support that.  So, hack time.  Since we're only using this
            // code for importing .kodu files, if the filename ends in .kodu we open the file
            // for reading, else we open the file for writing.
            if (filename.EndsWith(".kodu", StringComparison.OrdinalIgnoreCase))
            {
                streams[hf] = File.Open(filename, fileMode, FileAccess.Read, FileShare.ReadWrite);
            }
            else
            {
                streams[hf] = File.Open(filename, fileMode, FileAccess.Write, FileShare.ReadWrite);
            }
#else
            streams[hf] = File.Open(filename, fileMode, fileAccess, FileShare.ReadWrite);
#endif

            filenames[hf] = filename;

            return hf;
        }

        uint FdiRead(
            int hf,
            IntPtr pv,
            uint cb)
        {
            Stream stream = streams[hf];

            ResizeScratchBuffer((int)cb);

            int n = stream.Read(scratchBuffer, 0, (int)cb);

            Marshal.Copy(scratchBuffer, 0, pv, n);

            return (uint)n;
        }

        uint FdiWrite(
            int hf,
            IntPtr pv,
            uint cb)
        {
            Stream stream = streams[hf];

            ResizeScratchBuffer((int)cb);

            Marshal.Copy(pv, scratchBuffer, 0, (int)cb);

            stream.Write(scratchBuffer, 0, (int)cb);

            return cb;
        }

        int FdiClose(
            int hf)
        {
            Stream stream = streams[hf];

#if NETFX_CORE
            stream.Flush();
            stream.Dispose();
#else
            stream.Close();
#endif

            streams[hf] = null;
            filenames[hf] = null;

            return 0;
        }

        int FdiSeek(
            int hf,
            int dist,
            int seektype)
        {
            Stream stream = streams[hf];

            SeekOrigin seekOrigin = Util.SeekOriginFromDos(seektype);

            long result = stream.Seek(dist, seekOrigin);

            return (int)result;
        }
    }
}
