// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Cab
{
    public static class Rtl
    {
        [Flags]
        public enum OFLAG : int
        {
            _O_RDONLY = 0,
            _O_WRONLY = 1,
            _O_RDWR = 2,
            _O_APPEND = 8,
            _O_CREAT = 256,
            _O_TRUNC = 512,
            _O_EXCL = 1024,
            _O_TEXT = 16384,
            _O_BINARY = 32768,
            _O_WTEXT = 65536,
            _O_U16TEXT = 131072,
            _O_U8TEXT = 262144,
        }

        [Flags]
        public enum PMODE : int
        {
            _S_IFMT = 61440,
            _S_IFDIR = 16384,
            _S_IFCHR = 8192,
            _S_IFIFO = 4096,
            _S_IFREG = 32768,
            _S_IREAD = 256,
            _S_IWRITE = 128,
            _S_IEXEC = 64,
        }

        public const int _A_NORMAL = 0;
        public const int _A_RDONLY = 1;
        public const int _A_HIDDEN = 2;
        public const int _A_SYSTEM = 4;
        public const int _A_ARCH = 32;

        public const int SEEK_SET = 0;
        public const int SEEK_CUR = 1;
        public const int SEEK_END = 2;
    }
}
