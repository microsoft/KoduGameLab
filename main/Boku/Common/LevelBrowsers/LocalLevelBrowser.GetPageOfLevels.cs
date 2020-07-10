// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System;
using System.Collections.Generic;
using System.Threading;
using System.IO;

using Boku.Base;
using Boku.Common.Xml;

using BokuShared;

namespace Boku.Common
{
    public partial class LocalLevelBrowser
    {
        /// <summary>
        /// Start getting a page of level metadata from the local system.
        /// </summary>
        /// <param name="genreFilter"></param>
        /// <param name="sortBy"></param>
        /// <param name="sortDir"></param>
        /// <param name="first"></param>
        /// <param name="count"></param>
        /// <param name="callback"></param>
        /// <param name="param"></param>
        /// <returns></returns>
        public bool Async_GetPageOfLevels(
            Genres genreFilter,
            SortBy sortBy,
            SortDirection sortDir,
            int first,
            int count,
            BokuAsyncCallback callback,
            object param)
        {
            return false;
        }
    }
}
