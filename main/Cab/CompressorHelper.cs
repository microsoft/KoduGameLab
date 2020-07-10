// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.


using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.Diagnostics;

#if NETFX_CORE
    using System.Runtime.InteropServices;
    using System.Threading;
    using System.Threading.Tasks;

    using Windows.Foundation;
    using Windows.Management;
    using Windows.Storage;
    using Windows.Storage.FileProperties;
    using Windows.Storage.Search;
    using Windows.Storage.Streams;
#endif

namespace Cab
{

#if NETFX_CORE

    // Summary:
    //     Defines constants for read, write, or read/write access to a file.
    // [Serializable]
    [Flags]
    [ComVisible(true)]
    public enum FileAccess
    {
        // Summary:
        //     Read access to the file. Data can be read from the file. Combine with Write
        //     for read/write access.
        Read = 1,
        //
        // Summary:
        //     Write access to the file. Data can be written to the file. Combine with Read
        //     for read/write access.
        Write = 2,
        //
        // Summary:
        //     Read and write access to the file. Data can be written to and read from the
        //     file.
        ReadWrite = 3,
    }

    // Summary:
    //     Specifies how the operating system should open a file.
    // [Serializable]
    [ComVisible(true)]
    public enum FileMode
    {
        // Summary:
        //     Specifies that the operating system should create a new file. This requires
        //     System.Security.Permissions.FileIOPermissionAccess.Write. If the file already
        //     exists, an System.IO.IOException is thrown.
        CreateNew = 1,
        //
        // Summary:
        //     Specifies that the operating system should create a new file. If the file
        //     already exists, it will be overwritten. This requires System.Security.Permissions.FileIOPermissionAccess.Write.
        //     System.IO.FileMode.Create is equivalent to requesting that if the file does
        //     not exist, use System.IO.FileMode.CreateNew; otherwise, use System.IO.FileMode.Truncate.
        //     If the file already exists but is a hidden file, an System.UnauthorizedAccessException
        //     is thrown.
        Create = 2,
        //
        // Summary:
        //     Specifies that the operating system should open an existing file. The ability
        //     to open the file is dependent on the value specified by System.IO.FileAccess.
        //     A System.IO.FileNotFoundException is thrown if the file does not exist.
        Open = 3,
        //
        // Summary:
        //     Specifies that the operating system should open a file if it exists; otherwise,
        //     a new file should be created. If the file is opened with FileAccess.Read,
        //     System.Security.Permissions.FileIOPermissionAccess.Read is required. If the
        //     file access is FileAccess.Write then System.Security.Permissions.FileIOPermissionAccess.Write
        //     is required. If the file is opened with FileAccess.ReadWrite, both System.Security.Permissions.FileIOPermissionAccess.Read
        //     and System.Security.Permissions.FileIOPermissionAccess.Write are required.
        //     If the file access is FileAccess.Append, then System.Security.Permissions.FileIOPermissionAccess.Append
        //     is required.
        OpenOrCreate = 4,
        //
        // Summary:
        //     Specifies that the operating system should open an existing file. Once opened,
        //     the file should be truncated so that its size is zero bytes. This requires
        //     System.Security.Permissions.FileIOPermissionAccess.Write. Attempts to read
        //     from a file opened with Truncate cause an exception.
        Truncate = 5,
        //
        // Summary:
        //     Opens the file if it exists and seeks to the end of the file, or creates
        //     a new file. FileMode.Append can only be used in conjunction with FileAccess.Write.
        //     Attempting to seek to a position before the end of the file will throw an
        //     System.IO.IOException and any attempt to read fails and throws an System.NotSupportedException.
        Append = 6,
    }

    // Summary:
    //     Contains constants for controlling the kind of access other System.IO.FileStream
    //     objects can have to the same file.
    // [Serializable]
    [Flags]
    [ComVisible(true)]
    public enum FileShare
    {
        // Summary:
        //     Declines sharing of the current file. Any request to open the file (by this
        //     process or another process) will fail until the file is closed.
        None = 0,
        //
        // Summary:
        //     Allows subsequent opening of the file for reading. If this flag is not specified,
        //     any request to open the file for reading (by this process or another process)
        //     will fail until the file is closed. However, even if this flag is specified,
        //     additional permissions might still be needed to access the file.
        Read = 1,
        //
        // Summary:
        //     Allows subsequent opening of the file for writing. If this flag is not specified,
        //     any request to open the file for writing (by this process or another process)
        //     will fail until the file is closed. However, even if this flag is specified,
        //     additional permissions might still be needed to access the file.
        Write = 2,
        //
        // Summary:
        //     Allows subsequent opening of the file for reading or writing. If this flag
        //     is not specified, any request to open the file for reading or writing (by
        //     this process or another process) will fail until the file is closed. However,
        //     even if this flag is specified, additional permissions might still be needed
        //     to access the file.
        ReadWrite = 3,
        //
        // Summary:
        //     Allows subsequent deleting of a file.
        Delete = 4,
        //
        // Summary:
        //     Makes the file handle inheritable by child processes. This is not directly
        //     supported by Win32.
        Inheritable = 16,
    }

    // Summary:
    //     Provides attributes for files and directories.
    //[Serializable]
    [Flags]
    [ComVisible(true)]
    public enum FileAttributes
    {
        // Summary:
        //     The file is read-only.
        ReadOnly = 1,
        //
        // Summary:
        //     The file is hidden, and thus is not included in an ordinary directory listing.
        Hidden = 2,
        //
        // Summary:
        //     The file is a system file. The file is part of the operating system or is
        //     used exclusively by the operating system.
        System = 4,
        //
        // Summary:
        //     The file is a directory.
        Directory = 16,
        //
        // Summary:
        //     The file's archive status. Applications use this attribute to mark files
        //     for backup or removal.
        Archive = 32,
        //
        // Summary:
        //     Reserved for future use.
        Device = 64,
        //
        // Summary:
        //     The file is normal and has no other attributes set. This attribute is valid
        //     only if used alone.
        Normal = 128,
        //
        // Summary:
        //     The file is temporary. File systems attempt to keep all of the data in memory
        //     for quicker access rather than flushing the data back to mass storage. A
        //     temporary file should be deleted by the application as soon as it is no longer
        //     needed.
        Temporary = 256,
        //
        // Summary:
        //     The file is a sparse file. Sparse files are typically large files whose data
        //     are mostly zeros.
        SparseFile = 512,
        //
        // Summary:
        //     The file contains a reparse point, which is a block of user-defined data
        //     associated with a file or a directory.
        ReparsePoint = 1024,
        //
        // Summary:
        //     The file is compressed.
        Compressed = 2048,
        //
        // Summary:
        //     The file is offline. The data of the file is not immediately available.
        Offline = 4096,
        //
        // Summary:
        //     The file will not be indexed by the operating system's content indexing service.
        NotContentIndexed = 8192,
        //
        // Summary:
        //     The file or directory is encrypted. For a file, this means that all data
        //     in the file is encrypted. For a directory, this means that encryption is
        //     the default for newly created files and directories.
        Encrypted = 16384,
    }

    /// <summary>
    /// Class which tries to emulate old File class...
    /// </summary>
    static public class File
    {

        public static Stream Open(string fullPath, FileMode fileMode)
        {
            return Open(fullPath, fileMode, FileAccess.Write, FileShare.None);
        }

        public static Stream Open(string fullPath, FileMode fileMode, FileAccess fileAccess, FileShare fileShare)
        {
            // Split full path into path and filename.
            string path = Path.GetDirectoryName(fullPath);
            string filename = Path.GetFileName(fullPath);

            StorageFolder folder = GetStorageFolder(UserSpaceFolder, path);

            Stream stream = null;
            if (fileAccess == FileAccess.Read)
            {
                stream = OpenRead(folder, filename);
            }
            else
            {
                stream = OpenWrite(folder, filename);
            }

            return stream;

        }   // end of Open()

        static StorageFolder UserSpaceFolder = ApplicationData.Current.LocalFolder;

        /// <summary>
        /// Starting with the given folder as the root, uses the path to find
        /// the wanted folder.
        /// </summary>
        /// <param name="rootFolder">Where we're starting.</param>
        /// <param name="path">Path to wanted folder.  Should NOT have a filename.</param>
        /// <returns></returns>
        public static StorageFolder GetStorageFolder(StorageFolder rootFolder, string path)
        {
            StorageFolder folder = null;
            char[] seperator = { '\\' };
            string[] names = path.Split(seperator);

            try
            {
                // Start at the root.
                folder = rootFolder;

                // Iterate down the chain.
                foreach (string name in names)
                {
                    if (!string.IsNullOrEmpty(name))
                    {
                        // Note we're using CreateFolderAsync here instead of GetFolderAsync.
                        // This forces the folder to be created if it doesn't already exist.
                        IAsyncOperation<StorageFolder> op = folder.CreateFolderAsync(name, CreationCollisionOption.OpenIfExists);
                        op.AsTask<StorageFolder>().Wait();
                        folder = op.GetResults();
                    }
                }
            }
            catch (Exception e)
            {
                if (e != null)
                {
                }
                folder = null;
            }

            return folder;
        }

        public static StorageFolder GetFolderFromPath(string fullPath)
        {
            StorageFolder folder = null;

            try
            {
                IAsyncOperation<StorageFolder> op = StorageFolder.GetFolderFromPathAsync(fullPath);
                op.AsTask<StorageFolder>().Wait();
                folder = op.GetResults();
            }
            catch (Exception)
            {
            }

            return folder;
        }   // end of GetFolderFromPath()

        /// <summary>
        /// 
        /// </summary>
        /// <param name="folder"></param>
        /// <param name="filename"></param>
        /// <returns></returns>
        public static Stream OpenRead(StorageFolder folder, string filename)
        {
            Stream stream = null;
            StorageFile file = null;

            file = GetStorageFile(folder, filename);

            try
            {
                if (file != null)
                {
                    Task<Stream> foo = file.OpenStreamForReadAsync();
                    foo.ConfigureAwait(false);
                    stream = foo.Result;
                }
            }
            catch (Exception e)
            {
                if (e != null)
                {
                }
            }

            return stream;
        }   // end of OpenRead()

        /// <summary>
        /// Returns a StorageFile for the given folder and path.
        /// Returns null if file not found.
        /// </summary>
        /// <param name="folder"></param>
        /// <param name="path"></param>
        /// <returns></returns>
        public static StorageFile GetStorageFile(StorageFolder folder, string path)
        {
            StorageFile file = null;

            try
            {
                IAsyncOperation<StorageFile> fileAsync = folder.GetFileAsync(path);
                fileAsync.AsTask<StorageFile>().Wait();
                file = fileAsync.GetResults();
            }
            catch (Exception e)
            {
                if (e != null)
                {
                }
            }

            return file;
        }   // end of GetStorageFile()

        public static StorageFile GetStorageFile(string fullPath)
        {
            StorageFile file = null;

            // Split full path into path and filename.
            string path = Path.GetDirectoryName(fullPath);
            string filename = Path.GetFileName(fullPath);

            StorageFolder folder = GetFolderFromPath(path);
            file = GetStorageFile(folder, filename);

            return file;
        }   // end of GetStorageFile()

        /// <summary>
        /// 
        /// </summary>
        /// <param name="folder"></param>
        /// <param name="filename"></param>
        /// <returns></returns>
        public static Stream OpenWrite(StorageFolder folder, string filename)
        {
            Stream stream = null;

            StorageFile file = CreateStorageFile(folder, filename);

            try
            {
                if (file != null)
                {
                    Task<Stream> foo = file.OpenStreamForWriteAsync();
                    foo.ConfigureAwait(false);
                    stream = foo.Result;
                }
            }
            catch (Exception e)
            {
                if (e != null)
                {
                }
            }

            return stream;
        }   // end of OpenWrite()

        /// <summary>
        /// Create a file in the given folder and path.
        /// Will return null on failure.
        /// Note that the path does not need to exist.  If it doesn't
        /// it will be silently created.
        /// </summary>
        /// <param name="folder"></param>
        /// <param name="path"></param>
        /// <returns>StorageFIle assocaited with new file.  Null on error</returns>
        public static StorageFile CreateStorageFile(StorageFolder folder, string path)
        {
            StorageFile file = null;

            try
            {
                IAsyncOperation<StorageFile> fileAsync = folder.CreateFileAsync(path, CreationCollisionOption.ReplaceExisting);
                fileAsync.AsTask<StorageFile>().Wait();
                file = fileAsync.GetResults();
            }
            catch (Exception e)
            {
                if (e != null)
                {
                }
            }

            return file;
        }   // end of CreateStorageFile()

        /// <summary>
        /// Delete a file.
        /// </summary>
        /// <param name="fullPath"></param>
        /// <returns>true if successful</returns>
        public static bool Delete(string fullPath)
        {
            bool result = false;

            StorageFile file = GetStorageFile(fullPath);
            result = Delete(file);

            return result;
        }   // end of Delete()

        public static bool Delete(StorageFile file)
        {
            bool result = false;

            if (file != null)
            {
                var action = file.DeleteAsync();
                action.AsTask().ConfigureAwait(false);
                action.GetResults();
                result = true;
            }

            return result;
        }   // end of Delete()

        static public FileAttributes GetAttributes(string fullPath)
        {
            StorageFile file = GetStorageFile(fullPath);

            Windows.Storage.FileAttributes fileAttributes = file.Attributes;

            FileAttributes attributes = 0;

            if (fileAttributes.HasFlag(Windows.Storage.FileAttributes.Archive))
            {
                attributes |= FileAttributes.Archive;
            }
            if (fileAttributes.HasFlag(Windows.Storage.FileAttributes.Directory))
            {
                attributes |= FileAttributes.Directory;
            }
            if (fileAttributes.HasFlag(Windows.Storage.FileAttributes.Normal))
            {
                attributes |= FileAttributes.Normal;
            }
            if (fileAttributes.HasFlag(Windows.Storage.FileAttributes.ReadOnly))
            {
                attributes |= FileAttributes.ReadOnly;
            }
            if (fileAttributes.HasFlag(Windows.Storage.FileAttributes.Temporary))
            {
                attributes |= FileAttributes.Temporary;
            }

            return attributes;
        }   // end of GetAttributes()

        public static DateTime GetLastWriteTimeUtc(string fullPath)
        {
            DateTime result = DateTime.MinValue;

            StorageFile file = GetStorageFile(fullPath);

            if (file != null)
            {
                IAsyncOperation<BasicProperties> foo = file.GetBasicPropertiesAsync();
                foo.AsTask<BasicProperties>().Wait();
                BasicProperties props = foo.GetResults();

                result = props.DateModified.DateTime;
            }

            return result;
        }   // end of GetLastWriteTimeUtc()

    }   // end of File()

#endif

    public interface ICompressorHelper
    {
        Stream Open(string filename, FileMode fileMode);
        Stream Open(string filename, FileMode fileMode, FileAccess fileAccess, FileShare fileShare);

        void Delete(string filename);

        FileAttributes GetAttributes(string filename);

        DateTime GetLastWriteTimeUtc(string filename);

        string GetTempFileName();
    }

    public class FileHelper : ICompressorHelper
    {
        public Stream Open(string filename, FileMode fileMode)
        {
            return File.Open(filename, fileMode);
        }
        public Stream Open(string filename, FileMode fileMode, FileAccess fileAccess, FileShare fileShare)
        {
            return File.Open(filename, fileMode, fileAccess, fileShare);
        }

        public void Delete(string filename)
        {
            File.Delete(filename);
        }

        public FileAttributes GetAttributes(string fileName)
        {
            return File.GetAttributes(fileName);
        }

        public DateTime GetLastWriteTimeUtc(string filename)
        {
            return File.GetLastWriteTimeUtc(filename);
        }

        public string GetTempFileName()
        {
#if NETFX_CORE
            Guid guid = Guid.NewGuid();
            return guid.ToString() + ".tmp";
#else
            return Path.GetTempFileName();
#endif
        }
    }

    public class MemoryHelper : ICompressorHelper
    {
        interface IPseudoFile
        {
            bool IsExpandable { get; }
            bool CanWrite { get; }
            string Name { get; }

            Stream Open(SeekOrigin seek);
            void Delete();
        }
        class PseudoFile : MemoryStream, IPseudoFile
        {
            bool isOpened;
            bool isDeleted;

            public string Name
            {
                get { return name; }
            }
            readonly string name;

            public bool IsExpandable
            {
                get
                {
                    return isExpandable;
                }
            }
            readonly bool isExpandable;

            public override bool CanWrite
            {
                get
                {
                    return IsExpandable && base.CanWrite;
                }
            }

            public PseudoFile(string name) : base()
            {
                this.name = name;
                isExpandable = true;
            }
            public PseudoFile(string name, byte[] bytes) : base(bytes, false)
            {
                this.name = name;
                isExpandable = false;
            }

#if NETFX_CORE
            public void Close()
            {
                if (!isOpened)
                {
                    Flush();
                    Dispose();
                }
                isOpened = false;
            }
#else
            public override void Close()
            {
                if (!isOpened)
                {
                    //Why are we closing a closed stream?
                }
                isOpened = false;
            }
#endif

            public Stream Open(SeekOrigin seek)
            {
                if (isDeleted)
                {
                    throw new Exception("Trying to open a deleted 'file'!");
                }
                if (isOpened)
                {
                    //Why are we trying to open two streams?
                }
                isOpened = true;

                this.Seek(0, seek);

                return this;
            }

            public void Delete()
            {
                isOpened = false;
                isDeleted = true;
#if NETFX_CORE
                Close();
#else
                base.Close();
#endif
            }
        }

        readonly Dictionary<string, IPseudoFile> pseudoFiles;

        public MemoryHelper()
        {
            pseudoFiles = new Dictionary<string, IPseudoFile>();
        }

        /// <summary>
        /// Adds a readonly pseudo file.
        /// </summary>
        public void AddPseudoFile(string filename, byte[] bytes)
        {
            IPseudoFile pseudoFile = new PseudoFile(filename, bytes);
            pseudoFiles.Add(filename, pseudoFile);
        }

        public Stream Open(string filename, FileMode fileMode)
        {
            return Open(filename, fileMode, FileAccess.ReadWrite);
        }
        public Stream Open(string filename, FileMode fileMode, FileAccess fileAccess)
        {
            Stream result;

            IPseudoFile pseudoFile;

            bool fileExists = pseudoFiles.TryGetValue(filename, out pseudoFile);

            bool wantsWritable = (fileAccess & FileAccess.Write) != 0;

            switch (fileMode)
            {
                case FileMode.Append:
                    if (fileExists)
                    {
                        if (wantsWritable && !pseudoFile.CanWrite)
                        {
                            throw new Exception("'File' is read-only");
                        }

                        result = pseudoFile.Open(SeekOrigin.End);
                    }
                    else
                    {
                        throw new Exception("'File' doesn't exist");
                    }
                    break;

                case FileMode.Create:
                    if (fileExists)
                    {
                        if (wantsWritable && !pseudoFile.CanWrite)
                        {
                            throw new Exception("'File' is read-only");
                        }

                        result = pseudoFile.Open(SeekOrigin.Begin);
                    }
                    else
                    {
                        pseudoFile = new PseudoFile(filename);
                        pseudoFiles.Add(filename, pseudoFile);

                        result = pseudoFile.Open(SeekOrigin.Begin);
                    }
                    break;

                case FileMode.CreateNew:
                    if (fileExists)
                    {
                        throw new Exception("'File' exists");
                    }
                    else
                    {
                        pseudoFile = new PseudoFile(filename);
                        pseudoFiles.Add(filename, pseudoFile);

                        result = pseudoFile.Open(SeekOrigin.Begin);
                    }
                    break;

                case FileMode.Open:
                    if (fileExists)
                    {
                        if (wantsWritable && !pseudoFile.CanWrite)
                        {
                            throw new Exception("'File' is read-only");
                        }

                        result = pseudoFile.Open(SeekOrigin.Begin);
                    }
                    else
                    {
                        throw new Exception("'File' doesn't exist");
                    }
                    break;

                case FileMode.OpenOrCreate:
                    if (fileExists)
                    {
                        if (wantsWritable && !pseudoFile.CanWrite)
                        {
                            throw new Exception("'File' is read-only");
                        }

                        result = pseudoFile.Open(SeekOrigin.Begin);
                    }
                    else
                    {
                        pseudoFile = new PseudoFile(filename);
                        pseudoFiles.Add(filename, pseudoFile);

                        result = pseudoFile.Open(SeekOrigin.Begin);
                    }
                    break;

                case FileMode.Truncate:
                    if (fileExists)
                    {
                        pseudoFile.Delete();

                        pseudoFile = new PseudoFile(filename);
                        pseudoFiles[filename] = pseudoFile;

                        result = pseudoFile.Open(SeekOrigin.Begin);
                    }
                    else
                    {
                        throw new Exception("'File' doesn't exist");
                    }
                    break;

                default:
                    throw new Exception("Unexpected FileMode");
            }

            return result;
        }
        public Stream Open(string filename, FileMode fileMode, FileAccess fileAccess, FileShare fileShare)
        {
            return Open(filename, fileMode, fileAccess);
        }

        public void Delete(string filename)
        {
            IPseudoFile pseudoFile;

            if (pseudoFiles.TryGetValue(filename, out pseudoFile))
            {
                pseudoFile.Delete();
                pseudoFiles.Remove(filename);
            }
            else
            {
                throw new Exception("'File' doesn't exist");
            }
        }

        public FileAttributes GetAttributes(string filename)
        {
            IPseudoFile pseudoFile;

            if (pseudoFiles.TryGetValue(filename, out pseudoFile))
            {
                return FileAttributes.Normal; //ToDo(DZ): What kind of attributes do we want to return here?
            }
            else
            {
                throw new Exception("'File' doesn't exist");
            }
        }

        public DateTime GetLastWriteTimeUtc(string filename)
        {
            return DateTime.UtcNow; //ToDo(DZ): How do we find the latest write time? Or should we just return today's date?
        }

        long tempNameCounter = 0;
        public string GetTempFileName()
        {
            //string tempName = Guid.NewGuid().ToString();
            string tempName;
            do
            {
                tempName = tempNameCounter++.ToString();
            }
            while (pseudoFiles.ContainsKey(tempName));

            pseudoFiles.Add(tempName, new PseudoFile(tempName));

            return tempName;
        }

        //TEMP
        public void WriteStreamsToDisk(string path)
        {
            for (int i = 0; i < pseudoFiles.Count; i++)
            {
                var pair = pseudoFiles.ElementAt(i);

                string name = Path.GetFileName(pair.Key);
                IPseudoFile pseudoFile = pair.Value;

                Stream stream = pseudoFile.Open(SeekOrigin.Begin);

#if NETFX_CORE
                Stream file = File.Open(Path.Combine(path, name), FileMode.Create);
#else
                FileStream file = File.Open(Path.Combine(path, name), FileMode.Create);
#endif

                for (int j = 0; j < stream.Length; j++)
                {
                    file.WriteByte((byte)stream.ReadByte());
                }

#if NETFX_CORE
                stream.Flush();
                stream.Dispose();
                file.Flush();
                file.Dispose();
#else
                stream.Close();
                file.Close();
#endif
            }
        }
    }   // end of class MemoryHelper

}   // end of namespace Cab
