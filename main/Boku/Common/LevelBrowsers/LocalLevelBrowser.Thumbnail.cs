// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System;
using System.Collections.Generic;
using System.Threading;
using System.IO;

using Boku.Base;
using Boku.Common.Xml;
using Boku.Common.Sharing;

using BokuShared;

namespace Boku.Common
{
    public partial class LocalLevelBrowser
    {
        private void CompleteThumbnail(LevelMetadata level)
        {
            level.Thumbnail.Loading = false;
            if (level.ThumbnailBytes != null)
            {
                MemoryStream stream = new MemoryStream(level.ThumbnailBytes);
                level.Thumbnail.Texture = Storage4.TextureLoad(stream);
                level.ThumbnailBytes = null;
            }
        }

        private Stream OpenTextureStream(string texFilename)
        {
            return Storage4.TextureFileOpenRead(texFilename);
        }

        private void ShutdownInternal()
        {
        }

        private void LevelAdded_Synched(LevelMetadata level)
        {
            for (int i = 0; i < queries.Count; ++i)
            {
                LevelSetQuery query = queries[i] as LevelSetQuery;
                query.LevelAdded(level);
            }
        }

        private void LevelRemoved_Synched(LevelMetadata level)
        {
            for (int i = 0; i < queries.Count; ++i)
            {
                LevelSetQuery query = queries[i] as LevelSetQuery;
                query.LevelRemoved(level);
            }

            LevelBrowserState state = (LevelBrowserState)level.BrowserState;
            state.level = null;
            level.BrowserState = null;
        }
    }
}
