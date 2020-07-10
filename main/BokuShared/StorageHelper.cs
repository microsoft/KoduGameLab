// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.


using System;
using System.Diagnostics;
using System.IO;

#if NETFX_CORE
    using Windows.Storage;
    using Windows.Storage.Streams;
    using System.Threading.Tasks;
#endif

namespace BokuShared
{
    public abstract class StorageHelper
    {
        public abstract Stream OpenRead(string filename);
        public abstract Stream OpenRead(string filename, int flags);
        public abstract Stream OpenWrite(string filename);
        public abstract void Close(Stream stream);
    }

    public class FileStorageHelper : StorageHelper
    {
        private static FileStorageHelper instance;

        public static FileStorageHelper Instance
        {
            get
            {
                if (instance == null)
                    instance = new FileStorageHelper();
                return instance;
            }
        }

        public override Stream OpenRead(string filename)
        {
#if NETFX_CORE
            Debug.Assert(false, "Should never be called in WinRT");
            return null;
#else
            return File.OpenRead(filename);
#endif
        }

        public override Stream OpenRead(string filename, int flags)
        {
#if NETFX_CORE
            Debug.Assert(false, "Should never be called in WinRT");
            return null;
#else
            return File.OpenRead(filename);
#endif
        }

        public override Stream OpenWrite(string filename)
        {
#if NETFX_CORE
            Debug.Assert(false, "Should never be called in WinRT");
            return null;
#else
            return File.OpenWrite(filename);
#endif
        }

        public override void Close(Stream stream)
        {
#if NETFX_CORE
            Debug.Assert(false, "Should never be called in WinRT");
#else
            stream.Close();
#endif
        }

    }
}
