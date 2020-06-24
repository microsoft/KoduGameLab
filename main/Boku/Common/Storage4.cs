
#if !PREBOOT
//#define IMPORT_DEBUG
#endif

using System;
using System.Collections.Generic;
using System.Text;
using System.IO;
using System.IO.Compression;
using System.Diagnostics;

#if NETFX_CORE
    using Windows.Foundation;
    using Windows.Management;
    using Windows.Storage;
    using Windows.Storage.FileProperties;
    using Windows.Storage.Search;
    using Windows.Storage.Streams;
    using Windows.System.UserProfile;
#else
    using System.Management;
#endif 

using System.Threading;
using System.Threading.Tasks;
using System.Xml.Serialization;

using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Storage;


using Boku;

namespace Boku.Common
{
    [Flags]
    public enum StorageSource
    {
        TitleSpace = 1 << 0,
        UserSpace = 1 << 1,
        All = TitleSpace | UserSpace
    }

#if NETFX_CORE
    public static class MyExtensions
    {
        /// <summary>
        /// Extension methods to replace close.
        /// </summary>
        /// <param name="stream"></param>
        public static void Close(this Stream stream)
        {
            stream.Flush();
            stream.Dispose();
        }

        public static void Close(this StreamWriter sw)
        {
            sw.Flush();
            sw.Dispose();
        }

        public static void Close(this StreamReader sr)
        {
            sr.Dispose();
        }

        public static void Close(this StringWriter sw)
        {
            sw.Flush();
            sw.Dispose();
        }
    }
#endif

    public partial class Storage4
    {
        #region Members

        static string startupDir;               // Directory where exe started.  Used as root for titlespace
        static string userLocation;             // Root path of userspace.
        static string titleLocation;            // Root path of titlespace.
        static string userOverrideLocation;     // Location given by the user to override normal location.

        static string uniqueMachineID = String.Empty;   // Filled in during Init()

#if NETFX_CORE
        static StorageFolder TitleSpaceFolder;          // Filled in during Init().
        static public StorageFolder UserSpaceFolder;    // In WinRT this is the app's local storage folder
        static StorageFolder TempFolder;
#endif

        #endregion

        #region Accessors

        /// <summary>
        /// Root path to user space.
        /// </summary>
        public static string UserLocation
        {
            get { return userLocation; }
        }

        /// <summary>
        /// Root path to title space.
        /// </summary>
        public static string TitleLocation
        {
            get { return titleLocation; }
        }

        /// <summary>
        /// Set during startup, this is where the exe resides and
        /// acts as the root of title space.
        /// </summary>
        public static string StartupDir
        {
            set
            {
                if (!string.IsNullOrEmpty(value))
                {
                    startupDir = value;
                    titleLocation = value;
                }
            }
        }

        public static string UserOverrideLocation
        {
            set
            {
                if(!string.IsNullOrEmpty(value))
                {
                    // TODO (scoy) Should we add more validation that the path is good?
                    // What should we check for?
                    userOverrideLocation = value;
                    userLocation = value;
                }
            }
            get { return userOverrideLocation; }
        }

        /// <summary>
        /// Returns the hashed MACAddress for this machine.
        /// </summary>
        public static string UniqueMachineID
        {
            get { return uniqueMachineID; }
        }

#if NETFX_CORE
        static string username = "";
        // Current username
        public static string Username
        {
            get 
            {
                if(string.IsNullOrEmpty(username))
                {
                    try
                    {
                        // First, try getting name user has previously used.
                        username = Boku.Common.Xml.XmlOptionsData.Username;

                        // If still empty, try getting name from system.
                        if(string.IsNullOrEmpty(username) && UserInformation.NameAccessAllowed)
                        {
                            IAsyncOperation<string> op = UserInformation.GetFirstNameAsync();
                            op.AsTask<string>().Wait();
                            username = op.GetResults();
                        }
                    }
                    catch
                    {
                    }

                }

                return username;
            }
            set { username = value; }
        }
#endif

        #endregion

        #region Public

        //
        // NOTE : Trying to ifdef individual lines to get NETFX_CORE to work with .Net is 
        // too messy so I've just created two large sections.

#if NETFX_CORE

        /// <summary>
        /// One time init of storage.
        /// </summary>
        public static void Init()
        {
            Debug.Assert(userOverrideLocation == null, "Init should be called before this is set.");

            // For WinRT userLocation is relative to users MyDocuments folder rather than being full path.
            //userLocation = @"SavedGames\Boku\Player1";
            userLocation = @"";

            TitleSpaceFolder = Windows.ApplicationModel.Package.Current.InstalledLocation;
            //UserSpaceFolder = KnownFolders.DocumentsLibrary;
            UserSpaceFolder = ApplicationData.Current.LocalFolder;
            TempFolder = ApplicationData.Current.TemporaryFolder;

            uniqueMachineID = GetHashedMachineID();
        }


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
        }

        /// <summary>
        /// Returns StorageFile associated with given path and source.
        /// </summary>
        /// <param name="path">full path of file</param>
        /// <param name="source"></param>
        /// <returns></returns>
        public static StorageFile GetStorageFile(string fullPath, StorageSource sources)
        {
            // Note, for GetStorageFile() calls we don't need to split the 
            // path from the filename.

            // If both StorageSource flags are set, try user space first.
            StorageFile file = null;

            // Try UserSpace.
            if ((sources & StorageSource.UserSpace) != 0)
            {
                file = GetStorageFile(UserSpaceFolder, fullPath);
            }

            // Try TitleSpace.
            if (file == null && (sources & StorageSource.TitleSpace) != 0)
            {
                file = GetStorageFile(TitleSpaceFolder, fullPath);
            }

            return file;
        }

        /// <summary>
        /// Given an absolute file path, returns the StorageFile associated
        /// with that file, if it exists.  Used primarily for when user
        /// double-clicks on Kodu (of Kodu2) file.
        /// </summary>
        /// <param name="absolutePath"></param>
        /// <returns></returns>
        public static StorageFile GetStorageFile(string absolutePath)
        {
            StorageFile file = null;

            try
            {
                IAsyncOperation<StorageFile> fileAsync = StorageFile.GetFileFromPathAsync(absolutePath);
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
        }

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
        }

        /// <summary>
        /// Open file for reading.
        /// </summary>
        /// <param name="filePath">Path relative to storage source location.</param>
        /// <param name="sources">Which source(s) to look in.  If both, will look in UserSpace first.</param>
        /// <returns></returns>
        public static Stream OpenRead(string filePath, StorageSource sources)
        {
            Stream stream = null;

            // If both StorageSource flags are set, try user space first.
            StorageFile file = null;

            // Try UserSpace.
            if ((sources & StorageSource.UserSpace) != 0)
            {
                file = GetStorageFile(UserSpaceFolder, Path.Combine(UserLocation, filePath));
            }

            // Try TitleSpace.
            if (file == null && (sources & StorageSource.TitleSpace) != 0)
            {
                file = GetStorageFile(TitleSpaceFolder, filePath);
            }

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
        /// Open file for write.  Since TitleSpace is read-only it is
        /// assumed that filePath is relative to UserSpace.
        /// </summary>
        /// <param name="filePath"></param>
        /// <returns></returns>
        public static Stream OpenWrite(string filePath)
        {
            Stream stream = null;

            // UserSpace.
            StorageFile file = CreateStorageFile(UserSpaceFolder, Path.Combine(UserLocation, filePath));

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
        /// Close a stream.
        /// </summary>
        /// <param name="stream"></param>
        public static void Close(Stream stream)
        {
            if (stream != null)
            {
                // Would be nice if WinRT had an IsOpen or IsDisposed accessor...
                bool closed = !(stream.CanRead || stream.CanSeek || stream.CanWrite);
                if (!closed)
                {
                    stream.Flush();
                    stream.Dispose();
                }
            }
        }   // end of Close()

        /// <summary>
        /// Copys the StorageFile to the StorageFolder.  Returns a 
        /// StorageFile for the newly created file.
        /// </summary>
        /// <param name="file"></param>
        /// <param name="destFolder"></param>
        /// <param name="filename"></param>
        /// <returns></returns>
        public static StorageFile CopyStorageFile(StorageFile file, StorageFolder destFolder)
        {
            try
            {
                IAsyncOperation<StorageFile> fileAsync = file.CopyAsync(destFolder);
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
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="path"></param>
        /// <param name="filter">Assume filter is of the form "*.ext".  Will strip off the '*' and use the extesion for filtering.</param>
        /// <param name="sources"></param>
        /// <returns></returns>
        public static string[] GetFiles(string path, string filter, StorageSource sources)
        {
            // Build a set of query options using the filter.
            List<string> fileTypeFilter = new List<string>();
            fileTypeFilter.Add(filter.Substring(1));
            var queryOptions = new QueryOptions(CommonFileQuery.OrderByName, fileTypeFilter);
            queryOptions.FolderDepth = FolderDepth.Shallow;

            string[] filenames = null;

            // UserSpace.
            if ((sources & StorageSource.UserSpace) != 0)
            {
                try
                {
                    // Find the right subfolder.
                    StorageFolder userSpaceFolder = GetStorageFolder(UserSpaceFolder, Path.Combine(userLocation, path));

                    if (userSpaceFolder != null)
                    {
                        var query = userSpaceFolder.CreateFileQueryWithOptions(queryOptions);

                        IAsyncOperation<IReadOnlyList<StorageFile>> files = query.GetFilesAsync();
                        files.AsTask<IReadOnlyList<StorageFile>>().Wait();
                        IReadOnlyList<StorageFile> list = files.GetResults();

                        filenames = new string[list.Count];
                        for (int i = 0; i < list.Count; i++)
                        {
                            filenames[i] = list[i].Name;
                        }
                    }
                }
                catch (Exception e)
                {
                    if (e != null)
                    {
                    }
                }
            }

            // TitleSpace.
            if ((sources & StorageSource.TitleSpace) != 0)
            {
                try
                {
                    // Find the right subfolder.
                    StorageFolder titleSpaceFolder = GetStorageFolder(TitleSpaceFolder, path);

                    if (titleSpaceFolder != null)
                    {
                        var query = titleSpaceFolder.CreateFileQueryWithOptions(queryOptions);

                        IAsyncOperation<IReadOnlyList<StorageFile>> files = query.GetFilesAsync();
                        files.AsTask<IReadOnlyList<StorageFile>>().Wait();
                        IReadOnlyList<StorageFile> list = files.GetResults();

                        string[] titleFilenames = new string[list.Count];
                        for (int i = 0; i < list.Count; i++)
                        {
                            titleFilenames[i] = list[i].Name;
                        }
                        // Apparently Concat() is not safe for WinRT...
                        //filenames = Concat(titleFilenames, filenames);
                        filenames = Concat(titleFilenames, filenames);
                    }
                }
                catch (Exception e)
                {
                    if (e != null)
                    {
                    }
                }
            }

            return filenames;
        }

        /// <summary>
        /// Returns a list of files in the given path.
        /// </summary>
        /// <param name="path"></param>
        /// <param name="sources"></param>
        /// <returns></returns>
        public static string[] GetFiles(string path, StorageSource sources)
        {
            string[] filenames = null;

            // UserSpace.
            if ((sources & StorageSource.UserSpace) != 0)
            {
                try
                {
                    // Find the right subfolder.
                    StorageFolder userSpaceFolder = GetStorageFolder(UserSpaceFolder, path);

                    if (userSpaceFolder != null)
                    {
                        IAsyncOperation<IReadOnlyList<StorageFile>> files = userSpaceFolder.GetFilesAsync();
                        files.AsTask<IReadOnlyList<StorageFile>>().Wait();
                        IReadOnlyList<StorageFile> list = files.GetResults();

                        filenames = new string[list.Count];
                        for (int i = 0; i < list.Count; i++)
                        {
                            filenames[i] = list[i].Name;
                        }
                    }
                }
                catch (Exception e)
                {
                    if (e != null)
                    {
                    }
                }
            }

            // TitleSpace.
            if ((sources & StorageSource.TitleSpace) != 0)
            {
                try
                {
                    // Find the right subfolder.
                    StorageFolder titleSpaceFolder = GetStorageFolder(TitleSpaceFolder, path);

                    if (titleSpaceFolder != null)
                    {
                        IAsyncOperation<IReadOnlyList<StorageFile>> files = titleSpaceFolder.GetFilesAsync();
                        files.AsTask<IReadOnlyList<StorageFile>>().Wait();
                        IReadOnlyList<StorageFile> list = files.GetResults();

                        string[] titleFilenames = new string[list.Count];
                        for (int i = 0; i < list.Count; i++)
                        {
                            titleFilenames[i] = list[i].Name;
                        }
                        // Apparently Concat() is not safe for WinRT...
                        //filenames = Concat(titleFilenames, filenames);
                        filenames = Concat(titleFilenames, filenames);
                    }
                }
                catch (Exception e)
                {
                    if (e != null)
                    {
                    }
                }
            }

            return filenames;
        }   // end of GetFiles()

        /// <summary>
        /// Return whether a file exists in either title or user space.
        /// Looks in user space first.
        /// </summary>
        /// <param name="name"></param>
        /// <returns></returns>
        public static bool FileExists(string filePath, StorageSource sources)
        {
            bool result = false;

            // UserSpace.
            if ((sources & StorageSource.UserSpace) != 0)
            {
                if(GetStorageFile(UserSpaceFolder, Path.Combine(UserLocation, filePath)) != null)
                {
                    result = true;
                }
            }

            // TitleSpace.
            if (!result && (sources & StorageSource.TitleSpace) != 0)
            {
                if(GetStorageFile(TitleSpaceFolder, filePath) != null)
                {
                    result = true;
                }
            }

            return result;
        }   // end of FileExists()

        public static bool DirExists(string dirPath, StorageSource sources)
        {
            bool result = false;

            // UserSpace.
            if ((sources & StorageSource.UserSpace) != 0)
            {
                if (GetStorageFolder(UserSpaceFolder, Path.Combine(UserLocation, dirPath)) != null)
                {
                    result = true;
                }
            }

            // TitleSpace.
            if (!result && (sources & StorageSource.TitleSpace) != 0)
            {
                if (GetStorageFolder(TitleSpaceFolder, dirPath) != null)
                {
                    result = true;
                }
            }

            return result;
        }   // end of DirExists()

        /// <summary>
        /// Delete a file.  Assumes userSpace.
        /// </summary>
        /// <param name="filePath"></param>
        /// <returns>true if successful</returns>
        public static bool Delete(string filePath)
        {
            bool result = false;

            StorageFile file = GetStorageFile(UserSpaceFolder, Path.Combine(UserLocation, filePath));

            if (file != null)
            {
                result = Delete(file);
            }

            return result;
        }   // end of Delete()

        /// <summary>
        /// Delete a file.  Assumes userSpace.
        /// </summary>
        /// <param name="file"></param>
        /// <returns>true if successful</returns>
        public static bool Delete(StorageFile file)
        {
            bool result = false;

            if (file != null)
            {
                try
                {
                    IAsyncAction action = file.DeleteAsync();
                    action.AsTask().ConfigureAwait(false);
                    action.GetResults();
                    result = true;
                }
                catch
                {
                }
            }

            return result;
        }   // end of Delete()

        public static string[] ReadAllLines(string filePath, StorageSource sources)
        {
            string[] result = null;

            // UserSpace.
            if ((sources & StorageSource.UserSpace) != 0)
            {
                StorageFile file = GetStorageFile(UserSpaceFolder, Path.Combine(UserLocation, filePath));
                if (file != null)
                {
                    IAsyncOperation<IList<string>> foo = PathIO.ReadLinesAsync(file.Path);
                    foo.AsTask<IList<string>>().Wait();
                    IList<string> lines = foo.GetResults();

                    result = new string[lines.Count];
                    lines.CopyTo(result, 0);
                }
            }

            // TitleSpace.
            if (result == null && (sources & StorageSource.TitleSpace) != 0)
            {
                StorageFile file = GetStorageFile(TitleSpaceFolder, filePath);
                if (file != null)
                {
                    IAsyncOperation<IList<string>> foo = PathIO.ReadLinesAsync(file.Path);
                    foo.AsTask<IList<string>>().Wait();
                    IList<string> lines = foo.GetResults();

                    result = new string[lines.Count];
                    lines.CopyTo(result, 0);
                }
            }

            return result;
        }   // end of ReadAllLines()

        public static DateTime GetLastWriteTimeUtc(string filePath, StorageSource sources)
        {
            DateTime result = DateTime.MinValue;

            StorageFile file = null;
            // UserSpace.
            if ((sources & StorageSource.UserSpace) != 0)
            {
                file = GetStorageFile(UserSpaceFolder, Path.Combine(UserLocation, filePath));
            }

            // TitleSpace.
            if (result != DateTime.MinValue && (sources & StorageSource.TitleSpace) != 0)
            {
                file = GetStorageFile(TitleSpaceFolder, filePath);
            }

            if (file != null)
            {
                IAsyncOperation<BasicProperties> foo = file.GetBasicPropertiesAsync();
                foo.AsTask<BasicProperties>().Wait();
                BasicProperties props = foo.GetResults();

                result = props.DateModified.DateTime;
            }

            return result;

        }   // end of GetLastWriteTimeUtc()

        public static StorageFolder GetTempStorageFolder()
        {
            return TempFolder;
        }

        /// <summary>
        /// Creates a StorageFile in the temp folder.
        /// </summary>
        /// <returns></returns>
        public static StorageFile GetTempStorageFile()
        {
            Guid guid = Guid.NewGuid();
            string filename = guid.ToString() + ".tmp";
            StorageFile file = Storage4.CreateStorageFile(TempFolder, filename);

            return file;
        }

        /// <summary>
        /// Helper function which moves all the files in the srcPath folder to the dstPath folder.
        /// </summary>
        /// <param name="srcPath"></param>
        /// <param name="dstPath"></param>
        public static async void MoveAllFiles(string srcPath, string dstPath)
        {
            StorageFolder srcFolder = GetStorageFolder(UserSpaceFolder, srcPath);
            StorageFolder dstFolder = GetStorageFolder(UserSpaceFolder, dstPath);

            Debug.Assert(srcFolder != null);
            Debug.Assert(dstFolder != null);

            IReadOnlyList<StorageFile> files = await srcFolder.GetFilesAsync();

            foreach (StorageFile file in files)
            {
                await file.MoveAsync(dstFolder, file.Name, NameCollisionOption.ReplaceExisting);
            }

        }   // end of MoveAllFiles()

        /// <summary>
        /// Generates a unique ID for the current machine.  Used to make autosave files unique.
        /// Original version used Hhashed MAC Address but that's not available in WinRT.
        /// </summary>
        /// <returns></returns>
        private static string GetHashedMachineID()
        {
            string result = String.Empty;

            try
            {
                // Generate a unique string based on several hardware identifiers.  Probably overkill.
                Windows.System.Profile.HardwareToken hardwareToken = Windows.System.Profile.HardwareIdentification.GetPackageSpecificToken(null);
                using (DataReader dataReader = DataReader.FromBuffer(hardwareToken.Id))
                {
                    int offset = 0;
                    while (offset < hardwareToken.Id.Length)
                    {
                        byte[] hardwareEntry = new byte[4];
                        dataReader.ReadBytes(hardwareEntry);

                        // CPU ID of the processor || Size of the memory || Serial number of the disk device || BIOS
                        if ((hardwareEntry[0] == 1 || hardwareEntry[0] == 2 || hardwareEntry[0] == 3 || hardwareEntry[0] == 9) && hardwareEntry[1] == 0)
                        {
                            if (!string.IsNullOrEmpty(result))
                            {
                                result += "|";
                            }
                            result += string.Format("{0}.{1}", hardwareEntry[2], hardwareEntry[3]);
                        }
                        offset += 4;
                    }
                }

            }
            catch (Exception e)
            {
                if (e != null)
                {
                }
            }

            result = result.GetHashCode().ToString();

            return result;
        }   // end of GetHashedMachineID()

#else // not NETFX_CORE

        /// <summary>
        /// One time init of storage.
        /// </summary>
        public static void Init()
        {
            Debug.Assert(userOverrideLocation == null, "Init should be called before this is set.");

            // Create default user location.
            userLocation = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Personal), @"SavedGames\Boku\Player1");

            uniqueMachineID = GetHashedMACAddress();
        }

        /// <summary>
        /// Resets the userOverrideLocation and userLocation to default values.
        /// We have this because there are some startup ordering issues we're
        /// trying to work around related to importing files.
        /// </summary>
        public static void ResetUserOverrideLocation()
        {
            // Create default user location.
            userLocation = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Personal), @"SavedGames\Boku\Player1");
            userOverrideLocation = null;
        }

        /// <summary>
        /// Open file for reading.
        /// </summary>
        /// <param name="filePath">Path relative to storage source location.</param>
        /// <param name="sources">Which source(s) to look in.  If both, will look in UserSpace first.</param>
        /// <returns></returns>
        public static Stream OpenRead(string filePath, StorageSource sources)
        {
            Stream stream = null;

            try
            {
                // If both StorageSource flags are set, we want to try user space first.

                // Try UserSpace.
                if ((sources & StorageSource.UserSpace) != 0)
                {
                    string fullPath = Path.Combine(UserLocation, filePath);
                    if (File.Exists(fullPath))
                    {
                        stream = File.OpenRead(fullPath);
                    }
                }

                // Try TitleSpace.
                if (stream == null && (sources & StorageSource.TitleSpace) != 0)
                {
                    string fullPath = Path.Combine(TitleLocation, filePath);
                    if (File.Exists(fullPath))
                    {
                        stream = File.OpenRead(fullPath);
                    }
                }
            }
            catch (Exception e)
            {
                string str = e.Message;
                if (e.InnerException != null)
                {
                    str += e.InnerException.Message;
                }
                Debug.Assert(false, str);
            }

            return stream;
        }   // end of OpenRead()

        /// <summary>
        /// Open file for write.  Since TitleSpace is read-only it is
        /// assumed that filePath is relative to UserSpace.
        /// </summary>
        /// <param name="filePath"></param>
        /// <returns></returns>
        public static Stream OpenWrite(string filePath)
        {
            Stream stream = null;

            try
            {
                string fullPath = Path.Combine(UserLocation, filePath);
                // If file exists, delete.
                if(FileExists(filePath, StorageSource.UserSpace))
                {
                    File.Delete(fullPath);
                }
                
                // Ensure the directory exists.
                string dirPath = Path.GetDirectoryName(fullPath);
                if (!Directory.Exists(dirPath))
                {
                    Directory.CreateDirectory(dirPath);
                }

                // Open the stream.
                stream = File.OpenWrite(fullPath);
            }
            catch (Exception e)
            {
                if (e != null)
                {
                    // Debug.Assert(false, e.Message);
                }
            }

            return stream;
        }   // end of OpenWrite()

        public static Stream Open(string filePath, FileMode fileMode)
        {
            Stream stream = null;

            try
            {
                string fullPath = Path.Combine(UserLocation, filePath);

                // Ensure the directory exists.
                string dirPath = Path.GetDirectoryName(fullPath);
                if (!Directory.Exists(dirPath))
                {
                    Directory.CreateDirectory(dirPath);
                }

                // Open the stream.
                stream = File.Open(fullPath, fileMode);
            }
            catch (Exception e)
            {
                Debug.Assert(false, e.Message);
            }

            return stream;
        }   // end of Open()

        /// <summary>
        /// Close a stream.
        /// </summary>
        /// <param name="stream"></param>
        public static void Close(Stream stream)
        {
            if (stream != null)
            {
                stream.Close();
            }
        }   // end of Close()


        public static String[] GetFiles(string path, StorageSource sources)
        {
            return GetFiles(path, null, sources, SearchOption.TopDirectoryOnly);
        }

        /// <summary>
        /// Find all files with given relative path and filter. Checks title space FIRST, then user.
        /// NOTE This returns full path names, not relative ones.
        /// NOTE Does not look in subdirectories.
        /// </summary>
        /// <param name="path"></param>
        /// <param name="pattern"></param>
        /// <param name="sources"></param>
        /// <returns></returns>
        public static String[] GetFiles(string path, string pattern, StorageSource sources)
        {
            return GetFiles(path, pattern, sources, SearchOption.TopDirectoryOnly);
        }

        /// <summary>
        /// Find all files with given relative path and filter. Checks title space FIRST, then user.
        /// NOTE This returns full path names, not relative ones.
        /// </summary>
        /// <param name="path"></param>
        /// <param name="pattern"></param>
        /// <param name="sources"></param>
        /// <param name="searchOption"></param>
        /// <returns></returns>
        public static String[] GetFiles(string path, string pattern, StorageSource sources, SearchOption searchOption)
        {
            String[] list = null;

            if ((sources & StorageSource.TitleSpace) != 0)
            {
                string fullPath = Path.Combine(TitleLocation, path);
                if(string.IsNullOrEmpty(pattern))
                {
                    if (Directory.Exists(fullPath))
                    {
                        list = Directory.GetFiles(fullPath);
                    }
                }
                else
                {
                    if (Directory.Exists(fullPath))
                    {
                        list = Directory.GetFiles(fullPath, pattern);
                    }
                }
            }

            if ((sources & StorageSource.UserSpace) != 0)
            {
                string fullPath = Path.Combine(UserLocation, path);
                if (Directory.Exists(fullPath))
                {
                    string[] userList = null;
                    if (string.IsNullOrEmpty(pattern))
                    {
                        if (Directory.Exists(fullPath))
                        {
                            userList = Directory.GetFiles(fullPath);
                        }
                    }
                    else
                    {
                        if (Directory.Exists(fullPath))
                        {
                            userList = Directory.GetFiles(fullPath, pattern);
                        }
                    }
                    list = Concat(userList, list);
                }
            }

            return list;
        }

        /// <summary>
        /// Return whether a file exists in either title or user space.
        /// Looks in user space first.
        /// </summary>
        /// <param name="name"></param>
        /// <returns></returns>
        public static bool FileExists(string filePath, StorageSource sources)
        {
            bool result = false;

            try
            {
                if (!string.IsNullOrEmpty(filePath))
                {
                    // Test user space first.
                    if ((sources & StorageSource.UserSpace) != 0)
                    {
                        string fullPath = Path.Combine(UserLocation, filePath);
                        result = File.Exists(fullPath);
                    }

                    // If not found, try title space.
                    if (result == false && (sources & StorageSource.TitleSpace) != 0)
                    {
                        string fullPath = Path.Combine(TitleLocation, filePath);
                        result = File.Exists(fullPath);
                    }
                }
            }
            catch (Exception e)
            {
                string str = e.Message;
                if (e.InnerException != null)
                {
                    str += e.InnerException.Message;
                }
                Debug.Assert(false, str);

#if IMPORT_DEBUG
                LevelPackage.DebugPrint("FileExists threw an error");
                LevelPackage.DebugPrint(e.ToString());
#endif
            }

#if IMPORT_DEBUG
            if (result == false)
            {
                LevelPackage.DebugPrint("FileExists cant't find : " + filePath);
                LevelPackage.DebugPrint("    UserLocation : " + UserLocation);
                LevelPackage.DebugPrint("    TitleLocation : " + TitleLocation);

                string dirPath = Path.GetDirectoryName(filePath);
                string[] files = GetFiles(dirPath, sources);
                LevelPackage.DebugPrint("==Files in : " + dirPath);
                if (files == null || files.Length == 0)
                {
                    LevelPackage.DebugPrint("    none");
                }
                else
                {
                    foreach (string file in files)
                    {
                        LevelPackage.DebugPrint("    " + file);
                    }
                }
            }
#endif

            return result;
        }   // end of FileExists()

        /// <summary>
        /// Checks if a directory exists.  If both storage sources
        /// are specified, will check user space first.
        /// </summary>
        /// <param name="path"></param>
        /// <param name="sources"></param>
        /// <returns></returns>
        public static bool DirExists(string path, StorageSource sources)
        {
            bool result = false;

            try
            {
                // Test user space first.
                if ((sources & StorageSource.UserSpace) != 0)
                {
                    string fullPath = Path.Combine(UserLocation, path);
                    result = Directory.Exists(fullPath);
                }

                // Test title space.
                if (result == false && (sources & StorageSource.TitleSpace) != 0)
                {
                    string fullPath = Path.Combine(TitleLocation, path);
                    result = Directory.Exists(fullPath);
                }
            }
            catch (Exception e)
            {
                string str = e.Message;
                if (e.InnerException != null)
                {
                    str += e.InnerException.Message;
                }
                Debug.Assert(false, str);
            }

            return result;
        }   // end of DirExists()

        /// <summary>
        /// Internal - create the writable directory if it doesn't exist.
        /// Note that since it's writable, it's in user space, not title space.
        /// NOTE dirPath should not be a filename.
        /// </summary>
        /// <param name="dirPath"></param>
        /// <returns></returns>
        public static void CreateDirectory(string dirPath)
        {
            try
            {
                string fullPath = Path.Combine(UserLocation, dirPath);
                Directory.CreateDirectory(fullPath);
            }
            catch (Exception e)
            {
                string str = e.Message;
                if (e.InnerException != null)
                {
                    str += e.InnerException.Message;
                }
                Debug.Assert(false, str);
            }
        }   // end of CreateDirectory()

        /// <summary>
        /// Deletes the specified file.  Assumes
        /// it must be userspace.  
        /// </summary>
        /// <param name="filePath"></param>
        /// <returns>true on success</returns>
        public static bool Delete(string filePath)
        {
            bool result = false;

            try
            {
                string fullPath = Path.Combine(UserLocation, filePath);

                if (IsReadOnly(fullPath))
                {
                    ClearReadOnly(fullPath);
                }

                File.Delete(fullPath);
                result = true;
            }
            catch (Exception e)
            {
                string str = e.Message;
                if (e.InnerException != null)
                {
                    str += e.InnerException.Message;
                }
                Debug.Assert(false, str);
            }

            return result;
        }   // end of Delete

        /// <summary>
        /// Looks at a file and returns true if the file is read only.
        /// </summary>
        /// <param name="filePath"></param>
        /// <returns></returns>
        public static bool IsReadOnly(string filePath)
        {
            if (File.Exists(filePath))
            {
                FileAttributes attr = File.GetAttributes(filePath);
                if ((attr & FileAttributes.ReadOnly) != 0)
                {
                    return true;
                }
            }

            return false;
        }   // end of IsReadOnly()

        /// <summary>
        /// Ensures that a file is writable (deletable)
        /// </summary>
        /// <param name="filePath"></param>
        public static void ClearReadOnly(string filePath)
        {
            if (File.Exists(filePath))
            {
                FileAttributes attr = File.GetAttributes(filePath);
                attr &= ~FileAttributes.ReadOnly;
                File.SetAttributes(filePath, attr);
            }
        }   // end of ClearReadOnly()

        public static string[] ReadAllLines(string filePath, StorageSource sources)
        {
            string[] lines = null;

            if ((sources & StorageSource.UserSpace) != 0)
            {
                if(FileExists(filePath, StorageSource.UserSpace))
                {
                    string fullPath = Path.Combine(UserLocation, filePath);
                    lines = File.ReadAllLines(fullPath);
                }
            }

            if (lines == null && (sources & StorageSource.TitleSpace) != 0)
            {
                if (FileExists(filePath, StorageSource.TitleSpace))
                {
                    string fullPath = Path.Combine(TitleLocation, filePath);
                    lines = File.ReadAllLines(fullPath);
                }
            }

            return lines;
        }   // end of ReadAllLines()

        /// <summary>
        /// Get write time. Looks in user space first
        /// then in title space.
        /// </summary>
        /// <param name="name"></param>
        /// <returns></returns>
        public static DateTime GetLastWriteTimeUtc(string filePath, StorageSource sources)
        {
            DateTime time = DateTime.MinValue;

            if ((sources & StorageSource.UserSpace) != 0)
            {
                if (FileExists(filePath, StorageSource.UserSpace))
                {
                    string fullPath = Path.Combine(UserLocation, filePath);
                    time = File.GetLastWriteTimeUtc(fullPath);
                }
            }

            if (time == DateTime.MinValue && (sources & StorageSource.TitleSpace) != 0)
            {
                if (FileExists(filePath, StorageSource.TitleSpace))
                {
                    string fullPath = Path.Combine(TitleLocation, filePath);
                    time = File.GetLastWriteTimeUtc(fullPath);
                }
            }

            return time;
        }   // end of GetLastWriteTimeUtc()

        /// <summary>
        /// Set write time. Assumes file is in user space.
        /// </summary>
        /// <param name="name"></param>
        /// <returns></returns>
        public static void SetLastWriteTimeUtc(string filePath, DateTime dateTimeUtc)
        {
            if (FileExists(filePath, StorageSource.UserSpace))
            {
                string fullPath = Path.Combine(UserLocation, filePath);
                File.SetLastAccessTimeUtc(fullPath, dateTimeUtc);
            }
        }   // end of SetLastWriteTimeUtc()

        /// <summary>
        /// Gets the hashed MAC address of the current machine.  Used to make autosave files unique.
        /// </summary>
        /// <returns></returns>
        private static string GetHashedMACAddress()
        {
            string MACAddress = String.Empty;

            try
            {
                ManagementClass mc = new ManagementClass("Win32_NetworkAdapterConfiguration");
                ManagementObjectCollection moc = mc.GetInstances();
                foreach (ManagementObject mo in moc)
                {
                    if (MACAddress == String.Empty) // only return MAC Address from first card
                    {
                        if ((bool)mo["IPEnabled"] == true) MACAddress = mo["MacAddress"].ToString();
                    }
                    mo.Dispose();
                }

                MACAddress = MACAddress.Replace(":", "");
            }
            catch
            {
            }

            MACAddress = MACAddress.GetHashCode().ToString();

            return MACAddress;
        }   // end of GetHashedMACAddress()


#endif




        //
        // Methods common to .Net and NETFX_CORE
        //

        public static StreamWriter OpenStreamWriter(string filePath, Encoding encoding = null)
        {
            Stream stream = OpenWrite(filePath);
            StreamWriter sw = null;
            if (stream != null)
            {
                sw = encoding == null ? new StreamWriter(stream) : new StreamWriter(stream, encoding);
            }
            return sw;
        }

        #endregion

        #region Internal

        /// <summary>
        /// Combine two arrays of file names
        /// </summary>
        /// <param name="user"></param>
        /// <param name="title"></param>
        /// <returns></returns>
        private static String[] Concat(String[] user, String[] title)
        {
            String[] total = null;
            int totalLength = 0;
            if (user != null)
                totalLength += user.Length;
            if (title != null)
                totalLength += title.Length;
            if (totalLength > 0)
            {
                total = new String[totalLength];
                int i = 0;
                if (user != null)
                {
                    foreach (string s in user)
                    {
                        total[i++] = s;
                    }
                }
                if (title != null)
                {
                    foreach (string s in title)
                    {
                        total[i++] = s;
                    }
                }
            }
            return total;
        }   // end of Concat()


        #endregion

    }   // end of class Storage4



    public class XnaStorageHelper : BokuShared.StorageHelper
    {
        private static XnaStorageHelper instance;

        public static XnaStorageHelper Instance
        {
            get
            {
                if (instance == null)
                    instance = new XnaStorageHelper();
                return instance;
            }
        }

        public override Stream OpenRead(string filename)
        {
            return Storage4.OpenRead(filename, StorageSource.All);
        }

        public override Stream OpenRead(string filename, int flags)
        {
            return Storage4.OpenRead(filename, (StorageSource)flags);
        }

        public override Stream OpenWrite(string filename)
        {
            return Storage4.OpenWrite(filename);
        }

        public override void Close(Stream stream)
        {
            Storage4.Close(stream);
        }
    }   // end of class XnaStorageHelper


}   // end of namespace Boku.Common
