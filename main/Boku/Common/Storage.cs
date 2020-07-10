// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.


//#define OPEN_READ_DEBUG

using System;
using System.Collections.Generic;
using System.Text;
using System.IO;
using System.Diagnostics;
using System.Threading;

using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Storage;


using System.Xml.Serialization;

#if !NETFX_CORE
    using System.Management;
    using Microsoft.Win32;
#endif

#if CLIENT
using Boku.Common.Sharing;
using System.Windows.Forms;
#endif

namespace Boku.Common
{
    /*
    [Flags]
    public enum StorageSource
    {
        TitleSpace = 1 << 0,
        UserSpace = 1 << 1,
        LocalSpace = 1 << 2,
        All = TitleSpace | UserSpace | LocalSpace
    }
    */
    public partial class Storage
    {
#if END_OF__THE_WORLD
        #region constants
        
        public const int kFileOperationRetryCount = 5;
        public const int kFileOperationRetryWaitMs = 50;
        
        #endregion

        #region Members

        private static StorageDevice storageDevice = null;
        private static StorageContainer storageContainer = null;
        private static bool dirty = false;
        private static bool failed = false;
        private static string titleLocation = StorageContainer.TitleLocation;
        private static string userLocation = String.Empty;
        private static string userLocal = String.Empty;
        private static string userOverride = String.Empty;
        private static bool localEqualsUser = true;

        private static bool requestOpen = false;        // Set when storage selection is requested.
        private static bool corruptDevice = false;      // Set to true when we continuously fail.
        private static bool userChoseNoStorage = false; // Set if users presses <B> on the storage selection guide dialog.

        private static string macAddress = GetHashedMACAddress();

        #endregion Members

        #region Accessors
        
        /// <summary>
        /// Path where built in data (mostly .xml) lives.
        /// </summary>
        public static string TitleLocation
        {
            get { return titleLocation; }
            set { titleLocation = value; }
        }

        /// <summary>
        /// Where user specific (e.g. options) data goes.
        /// </summary>
        public static string UserLocation
        {
            get { return userLocation; }
            set { userLocation = value; }
        }

        /// <summary>
        /// Where save games go.
        /// </summary>
        public static string UserLocal
        {
            get { return userLocal; }
            private set { userLocal = value; }
        }

        /// <summary>
        /// An override of the path to the user specific data (e.g. options).
        /// This may be different from userLocal, where save games may be pooled with
        /// other users.
        /// </summary>
        public static string UserOverride
        {
            get { return userOverride; }
            set 
            { 
                userLocation = userOverride = value;
                localEqualsUser = userLocation == userLocal;
            }
        }

        /// <summary>
        /// This will go true if we fail to reconnect to our real storage container
        /// after a flush. In that case, there's nothing to do but force a restart.
        /// </summary>
        public static bool Failed
        {
            get { return failed; }
            private set { failed = value; }
        }

        /// <summary>
        /// Set to true when we think we've got a corrupt device.
        /// </summary>
        public static bool CorruptDevice
        {
            get { return corruptDevice; }
            set { corruptDevice = value; }
        }

        /// <summary>
        /// Set to true when user hit <B> on device selection dialog.
        /// </summary>
        public static bool UserChoseNoStorage
        {
            get { return userChoseNoStorage; }
            set { userChoseNoStorage = value; }
        }

        public static StorageDevice StorageDevice
        {
            get { return Storage.storageDevice; }
        }

        public static string MACAddress
        {
            get { return macAddress; }
        }

        #endregion Accessors


        #region Sys Mgmt - Init and Shutdown and Update
        /// <summary>
        /// Dispose and go dormant, normally only at end of program.
        /// </summary>
        public static void Shutdown()
        {
            if (storageContainer != null)
            {
                storageContainer.Dispose();
            }
            storageContainer = null;
            storageDevice = null;
            dirty = false;
        }
        /// <summary>
        /// Has the system been started up yet?
        /// </summary>
        public static bool Initialized
        {
            get
            {
                return !Failed && (storageDevice != null) && storageDevice.IsConnected;
            }
        }
        /// <summary>
        /// Pulse the system and give it time to get asynchronously on its feet.
        /// </summary>
        /// <returns></returns>
        public static bool Update()
        {
            if (Initialized)
            {
                // We're already set up and ready to go
                return true;
            }
            ReInit();
            return false;
        }

        public static bool ReInit()
        {
            // If we have a device, it's unplugged, so throw it away and start over.
            Shutdown();

            // Get the setup process started
#if XBOX
            if (BokuGame.bokuGame != null)
            {
                if (!requestOpen && !Guide.IsVisible)
                {
                    try
                    {
                        Guide.BeginShowStorageDeviceSelector(SelectDoneCallback, null);
                        requestOpen = true;
                    }
                    catch
                    {
                        /// Probably the guide got opened but IsVisible wasn't
                        /// set yet. We'll just try again later.
                    }
                }
            }
#else // PC is always playerOne
            if (!requestOpen)
            {
                Guide.BeginShowStorageDeviceSelector(PlayerIndex.One, SelectDoneCallback, null);
                requestOpen = true;
            }
#endif

            // Selection pending
            return false;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <returns>True on success, false on failure.</returns>
        private static bool OpenContainer()
        {
            bool success = true;

#if XBOX
            try
            {
                storageContainer = storageDevice.OpenContainer("Kodu Save");
            }
            catch
            {
                // Something failed so try again.
                success = false;
            }
#else // PC
            storageContainer = storageDevice.OpenContainer("Boku");
#endif // PC

            return success;
        }

        /// <summary>
        /// Initializes storage once a valid device is found.
        /// </summary>
        /// <returns>true on success</returns>
        private static bool InitStorageContainer()
        {
            bool success = true;

            ClearPaths();
            if (OpenContainer())
            {
                SetPaths();
                WaitForStorage();
            }
            else
            {
                // OpenContainer failed
                success = false;

                System.Windows.Forms.MessageBox.Show(
                    "InitStorageContainer failure.",
                    "InitStorageContainer failure.",
                    System.Windows.Forms.MessageBoxButtons.OK,
                    System.Windows.Forms.MessageBoxIcon.Asterisk);

            }

            return success;
        }   // end of InitStorageContainer

        /// <summary>
        /// Callback for Guide.BeginShowStorageDeviceSelector().
        /// </summary>
        /// <param name="result"></param>
        private static void SelectDoneCallback(IAsyncResult result)
        {
            requestOpen = false;

            storageDevice = Guide.EndShowStorageDeviceSelector(result);

            if (storageDevice != null)
            {
                if(!Storage.InitStorageContainer())
                {
                    // OpenContainer failed so try again.
                    if (!requestOpen
#if CLIENT
                        && !GamerServices.IsGuideVisible
#endif
                        )
                    {
                        try
                        {
                            Guide.BeginShowStorageDeviceSelector(SelectDoneCallback, null);
                            requestOpen = true;
                        }
                        catch
                        {
                            /// Probably the guide got opened but IsVisible wasn't
                            /// set yet. We'll just try again later.
                        }
                    }
                }
            }
            else
            {
                // The user chose <B> from the dialog.  Inform user that they
                // must choose a storage device.  Also allow them to quit.
                userChoseNoStorage = true;
            }
        }   // end of SelectDoneCallback()

        private static void ClearPaths()
        {
            userLocal = string.Empty;
            userLocal = string.Empty;
        }
        private static void SetPaths()
        {
            userLocal = storageContainer.Path;

#if !XBOX
            // Instead of using the storageContainer's path we can
            // get the path from the user's registry.  This makes things
            // work much nicer with school systems which have redirected
            // their students PCs to read/write to a network share.
            RegistryKey regKey = Registry.CurrentUser.CreateSubKey(@"Software\Microsoft\Windows\CurrentVersion\Explorer\User Shell Folders");
            userLocal = regKey.GetValue("Personal") as string;
            userLocal += @"\SavedGames\Boku\Player1";
#endif

            if (userOverride != String.Empty)
            {
                userLocation = userOverride;
                localEqualsUser = false;
            }
            else
            {
                userLocation = userLocal;
                localEqualsUser = true;
            }
        }

        /// <summary>
        /// Check that the file we created in WaitForStorage is still there. If not,
        /// then we are in a StorageContainer failure state, and need to be restarted.
        /// </summary>
        /// <returns>true if we're in a failed state</returns>
        private static void CheckCookie()
        {
            Failed = !Exists(_testFile, StorageSource.LocalSpace);
        }
        /// <summary>
        /// The name of our test cookie file.
        /// </summary>
        private static string _testFile = @"Content\Xml\Kookie.dat";

        #endregion Sys Mgmt - Init and Shutdown and Update

        #region Public Interfaces

        /// <summary>
        /// Loop over the storage creating a file and then testing for its existence.
        /// This is because Storage seems a bit slow to initialize, so may return a
        /// false !Exists() when fresh from Initializing.
        /// </summary>
        public static void WaitForStorage()
        {
            corruptDevice = false;

            bool exists = false;
            int retryCount = kFileOperationRetryCount;
            while (!exists && retryCount > 0)
            {
                try
                {
                    // Note OpenWrite also makes multiple attempts before throwing.
                    Stream stream = OpenWrite(_testFile, StorageSource.LocalSpace);
                    Close(stream);

                    exists = Exists(_testFile, StorageSource.LocalSpace);
                }
                catch(Exception e)
                {
#if !Xbox
                    string error = e.Message;
                    if (e.InnerException != null && e.InnerException.Message != null)
                    {
                        error += "\n" + e.InnerException.Message;
                    }
                    System.Windows.Forms.MessageBox.Show(
                        error
                        + "\nfile : " + _testFile
                        + "\nspace : " + StorageSource.LocalSpace.ToString()
                        + "\npathBase : " + PathBase(StorageSource.LocalSpace)
                        + "\npath : " + Combine(PathBase(StorageSource.LocalSpace), _testFile),
                        "WaitForStorage failure.",
                        System.Windows.Forms.MessageBoxButtons.OK,
                        System.Windows.Forms.MessageBoxIcon.Asterisk);
#endif
                    Thread.Sleep(kFileOperationRetryWaitMs);    // Sleep perchance to dream while giving storage some cycles.
                }

                // Uncomment this to fake for testing a corrupt MU.
                // If < 10Gb assume it's an MU and force a failure.
                /*
                if (Storage.storageDevice.TotalSpace < 10000000000)
                {
                    exists = false;
                }
                */

                --retryCount;
            }

            // Reset dirty, our cookie test write doesn't need to force a flush.
            dirty = false;

            if (retryCount == 0)
            {
                // Something has gone wrong with the storage.  Most likely someone
                // is playing with their memory unit.  Shut down the current storage
                // device.  This will cause everything to punt back to the MainMenu
                // where another attempt to init the storage will occur.

#if !Xbox
                System.Windows.Forms.MessageBox.Show(
                    "retry count exceeded",
                    "WaitForStorage failure.",
                    System.Windows.Forms.MessageBoxButtons.OK,
                    System.Windows.Forms.MessageBoxIcon.Asterisk);
#endif

                // Set flag so corrupt-storage dialog will be activated.
                corruptDevice = true;
                Shutdown();
            }

        }   // end of WaitForStorage()

        /// <summary>
        /// Flush file changes to the underlying storage container.
        /// If this is not done after a level save, then power cycling
        /// the xbox, pulling the MU, etc. would result in data loss.
        /// </summary>
        public static void FlushContainer()
        {
            /// Only flush on the 360. It isn't necessary on PC, and
            /// will eventually cause a crash because of the framework's
            /// handle leak issue.
            if (dirty)
            {
#if XBOX
                ClearPaths();
                // Dispose the container and then open a new one.
                if (storageContainer != null)
                {
                    storageContainer.Dispose();
                }
                if (OpenContainer())
                {
                    SetPaths();
                    CheckCookie();
                }
#endif // XBOX
                dirty = false;
            }
        }

        #region Generic Files
        /// <summary>
        /// Open a stream for read access.
        /// Checks user local space first, then game system space.
        /// </summary>
        /// <param name="name"></param>
        /// <returns></returns>
        public static Stream OpenRead(string name)
        {
            return OpenRead(name, StorageSource.All);
        }

        /// <summary>
        /// Open a stream for read access.
        /// </summary>
        /// <param name="name"></param>
        /// <param name="wantRetry"></param>
        /// <returns></returns>
        public static Stream OpenRead(string name, bool wantRetry)
        {
            return OpenRead(name, StorageSource.All, wantRetry);
        }

        /// <summary>
        /// Open a stream for read access.
        /// </summary>
        /// <param name="name"></param>
        /// <param name="sources"></param>
        /// <returns></returns>
        public static Stream OpenRead(string name, StorageSource sources)
        {
            return OpenRead(name, sources, false);
        }

        /// <summary>
        /// Open a stream for read access.
        /// </summary>
        /// <param name="name"></param>
        /// <param name="sources"></param>
        /// <param name="wantRetry"></param>
        /// <returns></returns>
        public static Stream OpenRead(string name, StorageSource sources, bool wantRetry)
        {

#if OPEN_READ_DEBUG
            // Hack to try and debug file open errors.
            try
            {
                string p = Environment.GetFolderPath(Environment.SpecialFolder.Personal) + @"\OpenRead.txt";
                TextWriter tw = new StreamWriter(p, false);

                // Write a line of text to the file
                tw.WriteLine("filename : " + name);
                tw.WriteLine("sources : " + sources.ToString());

                StorageSource src = CheckSources(sources);

                tw.WriteLine("checked sources : " + src.ToString());

                bool local = 0 != (src & StorageSource.LocalSpace);
                tw.WriteLine("local : " + local.ToString());
                if (local)
                {
                    string path = Combine(PathBase(StorageSource.LocalSpace), name);
                    tw.WriteLine("    path : " + path.ToString());

                    bool exists = File.Exists(path);
                    tw.WriteLine("    exists : " + exists.ToString());

                    if (exists)
                    {
                        tw.WriteLine("        opening");
                        Stream str = null;
                        try
                        {
                            str = File.OpenRead(path);
                        }
                        catch (Exception e)
                        {
                            tw.WriteLine("        fail : ", e.Message);
                            tw.WriteLine("             : ", e.InnerException.Message);
                        }
                        finally
                        {
                            if (str != null)
                            {
                                str.Close();
                                tw.WriteLine("        closing");
                            }
                        }
                    }
                }

                bool user = 0 != (src & StorageSource.UserSpace);
                tw.WriteLine("user : " + user.ToString());
                if (user)
                {
                    string path = Combine(PathBase(StorageSource.UserSpace), name);
                    tw.WriteLine("    path : " + path.ToString());

                    bool exists = File.Exists(path);
                    tw.WriteLine("    exists : " + exists.ToString());

                    if (exists)
                    {
                        tw.WriteLine("        opening");
                        Stream str = null;
                        try
                        {
                            str = File.OpenRead(path);
                        }
                        catch (Exception e)
                        {
                            tw.WriteLine("        fail : ", e.Message);
                            tw.WriteLine("             : ", e.InnerException.Message);
                        }
                        finally
                        {
                            if (str != null)
                            {
                                str.Close();
                                tw.WriteLine("        closing");
                            }
                        }
                    }
                }

                bool title = 0 != (src & StorageSource.TitleSpace);
                tw.WriteLine("title : " + title.ToString());
                if (title)
                {
                    string path = Combine(PathBase(StorageSource.TitleSpace), name);
                    tw.WriteLine("    path : " + path.ToString());

                    bool exists = File.Exists(path);
                    tw.WriteLine("    exists : " + exists.ToString());

                    if (exists)
                    {
                        tw.WriteLine("        opening");
                        Stream str = null;
                        try
                        {
                            str = File.OpenRead(path);
                        }
                        catch (Exception e)
                        {
                            tw.WriteLine("        fail : ", e.Message);
                            tw.WriteLine("             : ", e.InnerException.Message);
                        }
                        finally
                        {
                            if (str != null)
                            {
                                str.Close();
                                tw.WriteLine("        closing");
                            }
                        }
                    }
                }

                tw.WriteLine("");

                tw.Close();
            }
            catch (Exception e)
            {
                System.Windows.Forms.MessageBox.Show(e.Message + "\n" + e.InnerException.Message);
            }
#endif



            Stream stream = null;

            int attempts = 0;
            while (true)
            {
                if (Initialized)
                {
                    sources = CheckSources(sources);
                    /// Try first on local space
                    if (0 != (sources & StorageSource.LocalSpace))
                    {
                        string path = Combine(PathBase(StorageSource.LocalSpace), name);

                        if (File.Exists(path))
                        {
                            stream = File.OpenRead(path);
                        }
                    }
                    /// If not there, try user space.
                    if ((stream == null)
                        && (0 != (sources & StorageSource.UserSpace)))
                    {
                        string path = Combine(PathBase(StorageSource.UserSpace), name);

                        if (File.Exists(path))
                        {
                            stream = File.OpenRead(path);
                        }
                    }
                }
                // This next bit is the last chance to get it. If
                // we fail, we want to generate the exception, so no
                // try/catch wrapper here.
                if (stream == null && 0 != (sources & StorageSource.TitleSpace))
                {
                    string path = Combine(PathBase(StorageSource.TitleSpace), name);

                    if (File.Exists(path))
                    {
#if !PREBOOT && !ADDIN
                        // This file exists in title space, so log the filename.
                        BokuGame.LogContentFileLoaded(path);
#endif
                        stream = File.OpenRead(path);
                    }
                }

                if (stream != null)
                    break;

                if (!wantRetry)
                    break;

                if (attempts >= kFileOperationRetryCount)
                    break;
                Thread.Sleep(kFileOperationRetryWaitMs);
                attempts += 1;
            }

            if (stream == null)
            {
#if XBOX
                // Do nothing?
#else
                throw new FileNotFoundException(String.Format("File not found in {0}: {1}", sources, name));
#endif
            }

            return stream;
        }

        /// <summary>
        /// Open a stream for write access in user space.
        /// </summary>
        /// <param name="name"></param>
        /// <returns></returns>
        public static Stream OpenWrite(string name)
        {
            StorageSource source = CheckWriteSource(name);

            return OpenWrite(name, source);
        }

        /// <summary>
        /// Opens a new storage stream for writing.
        /// Throws an exception after n failed attempts.
        /// </summary>
        /// <param name="name"></param>
        /// <param name="source"></param>
        /// <returns>the open stream on success</returns>
        public static Stream OpenWrite(string name, StorageSource source)
        {
            if ((source & StorageSource.TitleSpace) != 0)
            {
#if !Xbox
                System.Windows.Forms.MessageBox.Show(
                    "\nsource = " + source.ToString()
                    + "\nPathBase = " + PathBase(source)
                    + "\n name = " + name,
                    "OpenWrite failure, trying to write to title space.",
                    System.Windows.Forms.MessageBoxButtons.OK,
                    System.Windows.Forms.MessageBoxIcon.Asterisk);
#endif

                throw new Exception("Storage may not write to title space.");
            }

            dirty = true;

            int attempts = 0;

            Stream stream = null;

            while (stream == null)
            {
                try
                {
                    CreateDirectory(name, source);

                    stream = new FileStream(
                        Combine(PathBase(source), name),
                        FileMode.Create,
                        FileAccess.Write,
                        FileShare.None);
                }
                catch
                {
                    if (attempts >= kFileOperationRetryCount)
                        throw;
                    Thread.Sleep(kFileOperationRetryWaitMs);
                    ++attempts;
                }
            }

            if (stream == null)
            {
#if !Xbox
                System.Windows.Forms.MessageBox.Show(
                    "\nsource = " + source.ToString() 
                    + "\nPathBase = " + PathBase(source) 
                    + "\n name = " + name,
                    "OpenWrite failure.",
                    System.Windows.Forms.MessageBoxButtons.OK, 
                    System.Windows.Forms.MessageBoxIcon.Asterisk);
#endif
            }

            return stream;
        }   // end of OpenWrite()

        /// <summary>
        /// Close the stream and free up system resources
        /// </summary>
        /// <param name="stream"></param>
        public static void Close(Stream stream)
        {
            if (stream != null)
                stream.Close();
        }

        /// <summary>
        /// Return whether a file in either title or user space
        /// </summary>
        /// <param name="name"></param>
        /// <returns></returns>
        public static bool Exists(string name)
        {
            return Exists(name, StorageSource.All);
        }

        public static bool Exists(string name, StorageSource sources)
        {
            if (name != null)
            {
                if (Initialized)
                {
                    sources = CheckSources(sources);
                    if (0 != (sources & StorageSource.LocalSpace))
                    {
                        var localPath = Combine(PathBase(StorageSource.LocalSpace), name);
                        if(File.Exists(localPath))
                            return true;
                    }
                    if (0 != (sources & StorageSource.UserSpace))
                    {
                        var userPath = Combine(PathBase(StorageSource.UserSpace), name);
                        if(File.Exists(userPath))
                            return true;
                    }
                }

                if (0 != (sources & StorageSource.TitleSpace))
                {
                    var titlePath = Combine(PathBase(StorageSource.TitleSpace), name);
                    if(File.Exists(titlePath))
                        return true;
                }
            }

            return false;
        }

        /// <summary>
        /// Return whether a directory exists in either title or user space
        /// </summary>
        /// <param name="name"></param>
        /// <returns></returns>
        public static bool ExistsDir(string name)
        {
            return ExistsDir(name, StorageSource.All);
        }

        public static bool ExistsDir(string name, StorageSource sources)
        {
            if (name != null)
            {
                if (Initialized)
                {
                    sources = CheckSources(sources);
                    if (0 != (sources & StorageSource.LocalSpace))
                    {
                        var localPath = Combine(PathBase(StorageSource.LocalSpace), name);
                        if (Directory.Exists(localPath))
                            return true;
                    }
                    if (0 != (sources & StorageSource.UserSpace))
                    {
                        var userPath = Combine(PathBase(StorageSource.UserSpace), name);
                        if (Directory.Exists(userPath))
                            return true;
                    }
                }

                if (0 != (sources & StorageSource.TitleSpace))
                {
                    var titlePath = Combine(PathBase(StorageSource.TitleSpace), name);
                    if (Directory.Exists(titlePath))
                        return true;
                }
            }

            return false;
        }

        /// <summary>
        /// Return FileInfo if file exists in local, user or title space (otherwise returns null.)
        /// </summary>
        public static FileInfo GetInfo(string name)
        {
            return GetInfo(name, StorageSource.All);
        }

        /// <summary>
        /// Return FileInfo if file exists (otherwise returns null.)
        /// </summary>
        public static FileInfo GetInfo(string name, StorageSource sources)
        {
            if (name != null)
            {
                if (Initialized)
                {
                    sources = CheckSources(sources);
                    if (0 != (sources & StorageSource.LocalSpace))
                    {
                        var path = Combine(PathBase(StorageSource.LocalSpace), name);
                        if (File.Exists(path))
                        {
                            return new FileInfo(path);
                        }
                    }
                    if (0 != (sources & StorageSource.UserSpace))
                    {
                        var path = Combine(PathBase(StorageSource.UserSpace), name);
                        if (File.Exists(path))
                        {
                            return new FileInfo(path);
                        }
                    }
                }

                if (0 != (sources & StorageSource.TitleSpace))
                {
                    var path = Combine(PathBase(StorageSource.TitleSpace), name);
                    if (File.Exists(path))
                    {
                        return new FileInfo(path);
                    }
                }
            }

            return null;
        }

        /// <summary>
        /// Get write time. Looks in user space first if initialized,
        /// then in title space.
        /// </summary>
        /// <param name="name"></param>
        /// <returns></returns>
        public static DateTime GetLastWriteTimeUtc(string name)
        {
#if !XBOX
            if (Initialized && File.Exists(Combine(PathBase(StorageSource.UserSpace), name)))
            {
                return File.GetLastWriteTimeUtc(Combine(PathBase(StorageSource.UserSpace), name));
            }

            return File.GetLastWriteTimeUtc(Combine(PathBase(StorageSource.TitleSpace), name));
#else
            // Hm, how to implement correctly on Xbox...
            return new DateTime(0);
#endif
        }

        /// <summary>
        /// Set write time. Looks in user space first if initialized,
        /// then in title space.
        /// </summary>
        /// <param name="name"></param>
        /// <returns></returns>
        public static void SetLastWriteTimeUtc(string name, DateTime dateTimeUtc)
        {
#if !XBOX
            if (Initialized && File.Exists(Combine(PathBase(StorageSource.UserSpace), name)))
            {
                File.SetLastWriteTimeUtc(Combine(PathBase(StorageSource.UserSpace), name), dateTimeUtc);
            }
            else
            {
                File.SetLastWriteTimeUtc(Combine(PathBase(StorageSource.TitleSpace), name), dateTimeUtc);
            }
#else
            // Hm, how to implement on Xbox...
#endif
        }

        /// <summary>
        /// Delete the file. Only checks user space, since title space is read-only.
        /// </summary>
        /// <param name="name"></param>
        /// <returns></returns>
        public static bool Delete(string name)
        {
            // Only bother checking in user space, 
            // because we can't delete out of title space anyway
            if (Initialized)
            {
                StorageSource source = CheckWriteSource(name);
                string filename = Combine(PathBase(source), name);
                if (File.Exists(filename))
                {
                    dirty = true;

                    File.Delete(filename);
                    return true;
                }
            }
            return false;
        }

        /// <summary>
        /// Construct an appropriate absolute path name
        /// </summary>
        /// <param name="name"></param>
        /// <param name="titleSpace"></param>
        /// <returns></returns>
        public static string FullPathName(string name, StorageSource source)
        {
            return Combine(PathBase(source), name);
        }

        /// <summary>
        /// Find all files with given relative path. Checks title space FIRST, then user.
        /// Local space not checked, because only Autosave lives there now, unless
        /// sources is exactly LocalSpace.
        /// </summary>
        /// <param name="path"></param>
        /// <returns></returns>
        public static String[] GetFiles(string path, StorageSource sources)
        {
            String[] list = null;

            if (0 != (sources & StorageSource.TitleSpace))
            {
                string dirpath = GetDirectoryName(path, StorageSource.TitleSpace);
                if (Directory.Exists(dirpath))
                {
                    list = Directory.GetFiles(dirpath);
                    Strip(list, PathBase(StorageSource.TitleSpace));
                }
            }

            if (Initialized)
            {
                sources = CheckSources(sources);
                if (0 != (sources & StorageSource.UserSpace))
                {
                    string dirpath = GetDirectoryName(path, StorageSource.UserSpace);
                    if (Directory.Exists(dirpath))
                    {
                        String[] userList = Directory.GetFiles(dirpath);
                        Strip(userList, PathBase(StorageSource.UserSpace));
                        list = Concat(userList, list);
                    }
                }
                if (sources == StorageSource.LocalSpace)
                {
                    string dirpath = GetDirectoryName(path, StorageSource.LocalSpace);
                    if (Directory.Exists(dirpath))
                    {
                        String[] userList = Directory.GetFiles(dirpath);
                        Strip(userList, PathBase(StorageSource.LocalSpace));
                        list = Concat(userList, list);
                    }
                }
            }

            return list;
        }

        /// <summary>
        /// Find all files with given relative path and filter. Checks title space FIRST, then user.
        /// Local space not checked, because only Autosave lives there now.
        /// </summary>
        /// <param name="path"></param>
        /// <returns></returns>
        public static String[] GetFiles(string path, string filter, StorageSource sources)
        {
            String[] list = null;

            if (0 != (sources & StorageSource.TitleSpace))
            {
                string dirpath = GetDirectoryName(path, StorageSource.TitleSpace);
                if (Directory.Exists(dirpath))
                {
                    list = Directory.GetFiles(dirpath, filter);
                    Strip(list, PathBase(StorageSource.TitleSpace));
                }
            }

            if (Initialized && 0 != (sources & StorageSource.UserSpace))
            {
                string dirpath = GetDirectoryName(path, StorageSource.UserSpace);
                if (Directory.Exists(dirpath))
                {
                    String[] userList = Directory.GetFiles(dirpath, filter);
                    Strip(userList, PathBase(StorageSource.UserSpace));
                    list = Concat(userList, list);
                }
            }

            return list;
        }

#if !XBOX360
        /// <summary>
        /// Find all files with given relative path and filter. Checks title space FIRST, then user.
        /// Only option we ever use is SearchTopLevelOnly, which seems to be default behavior 
        /// anyway. Not supported on 360. Should be nuked?
        /// </summary>
        /// <param name="path"></param>
        /// <returns></returns>
        public static String[] GetFiles(string path, string filter, SearchOption option, StorageSource sources)
        {
            String[] list = null;

            if (0 != (sources & StorageSource.TitleSpace))
            {
                string dirpath = GetDirectoryName(path, StorageSource.TitleSpace);
                if (Directory.Exists(dirpath))
                {
                    list = Directory.GetFiles(dirpath, filter, option);
                    Strip(list, PathBase(StorageSource.TitleSpace));
                }
            }

            if (Initialized && 0 != (sources & StorageSource.UserSpace))
            {
                string dirpath = GetDirectoryName(path, StorageSource.UserSpace);
                if (Directory.Exists(dirpath))
                {
                    String[] userList = Directory.GetFiles(dirpath, filter, option);
                    Strip(userList, PathBase(StorageSource.UserSpace));
                    list = Concat(userList, list);
                }
            }

            return list;
        }
#endif // !XBOX360


        #endregion Generic Files


        #endregion Public Interfaces


        #region Path Helpers
        /// <summary>
        /// This helper exists because Path.Combine is so brain dead,
        /// if the second argument starts with a slash, it is returned
        /// as the full path. So Combine("c:\", "\foo.txt") returns "\foo.txt".
        /// 
        /// BUT don't do this if the second arg starts with a double slash.
        /// This happens in classroom settings where user storage is directed to a network share.
        /// </summary>
        /// <param name="prefix"></param>
        /// <param name="postfix"></param>
        /// <returns></returns>
        private static string Combine(string prefix, string postfix)
        {
            int startIndex = 0;
            if (postfix.StartsWith(@"\") && !postfix.StartsWith(@"\\"))
            {
                startIndex = 1;
            }
            string comb = Path.Combine(prefix, postfix.Substring(startIndex));
            return comb;
        }

        /// <summary>
        /// Combine two lists of file names
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
        }

        /// <summary>
        /// Strip off given prefix (adding trailing \\ to prefix if necessary).
        /// </summary>
        /// <param name="list"></param>
        /// <param name="prefix"></param>
        private static void Strip(String[] list, string prefix)
        {
            if (list != null)
            {
                Int32 startIdx = prefix.Length;
                // Make sure we strip off the leading \\
                if (!prefix.EndsWith("\\"))
                {
                    startIdx += "\\".Length;
                }
                for (int i = 0; i < list.Length; ++i)
                {
                    list[i] = list[i].Substring(startIdx);
                }
            }
        }

        /// <summary>
        /// Return title or user base path as requested.
        /// </summary>
        /// <param name="titleSpace"></param>
        /// <returns></returns>
        private static string PathBase(StorageSource source)
        {
            if (source == StorageSource.TitleSpace)
            {
                return TitleLocation;
            }

            if (source == StorageSource.LocalSpace)
            {
                return UserLocal;
            }

            if (source == StorageSource.UserSpace)
            {
                return UserLocation;
            }

            throw new Exception("Invalid storage source");
        }

        /// <summary>
        /// Internal - strip out the name of the directory
        /// Note that if it's writable, it's in user space.
        /// If it's for read, it might be in either space
        /// </summary>
        /// <param name="name"></param>
        /// <returns></returns>
        private static string GetDirectoryName(string name, StorageSource source)
        {
            string dirName = Path.GetDirectoryName(name);
            return Path.Combine(PathBase(source), dirName);
        }

        /// <summary>
        /// Internal - create the writable directory if it doesn't exist.
        /// Note that since it's writable, it's in user space, not title space.
        /// </summary>
        /// <param name="name"></param>
        /// <returns></returns>
        public static void CreateDirectory(string name, StorageSource source)
        {
            try
            {
                Directory.CreateDirectory(GetDirectoryName(name, source));
            }
            catch(Exception e)
            {
                // Just here for debug.
                if (e != null)
                {
                }

                // Pass on the pain.
                throw e;
            }
        }   // end of CreateDirectory()

        /// <summary>
        /// If sources contains both local and user, and they are actually the same
        /// folder, then strip one out to avoid unnecessary file IO.
        /// </summary>
        /// <param name="sources"></param>
        /// <returns></returns>
        private static StorageSource CheckSources(StorageSource sources)
        {
            if (localEqualsUser)
            {
                StorageSource localAndUser = StorageSource.LocalSpace | StorageSource.UserSpace;
                if ((sources & localAndUser) == localAndUser)
                {
                    sources &= ~StorageSource.LocalSpace;
                }
            }
            return sources;
        }
        /// <summary>
        /// Unpretty means to ensure autosaves go to local storage, even if
        /// a regular save would go off to some user specified place.
        /// </summary>
        /// <param name="fileName"></param>
        /// <returns></returns>
        private static StorageSource CheckWriteSource(string fileName)
        {
            if (!localEqualsUser && fileName.ToUpper().Contains("AUTOSAVE"))
            {
                return StorageSource.LocalSpace;
            }
            return StorageSource.UserSpace;
        }
        #endregion Path Helpers

        /// <summary>
        /// Gets the hashed MAC address of the current machine.  Used to make autosave files unique.
        /// </summary>
        /// <returns></returns>
        private static string GetHashedMACAddress()
        {
            string MACAddress = String.Empty;
#if !XBOX
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
#endif
            MACAddress = MACAddress.GetHashCode().ToString();

            return MACAddress;
        } 


    }   // end of class Storage

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
            return Storage.OpenRead(filename, StorageSource.All, true);
        }

        public override Stream OpenRead(string filename, int flags)
        {
            return Storage.OpenRead(filename, (StorageSource)flags, true);
        }

        public override Stream OpenWrite(string filename)
        {
            return Storage.OpenWrite(filename);
        }

        public override void Close(Stream stream)
        {
            Storage.Close(stream);
        }
#endif  // END_OF_THE_WORLD
    }


}
