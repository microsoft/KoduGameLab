// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System;

namespace Boku.Common
{
    /// <summary>
    /// A filter that matches all levels.
    /// </summary>
    public class LevelSetFilterAny : ILevelSetFilter
    {
        public bool Dirty { get; set; }
        public bool ServerSideMatching { get; set; }//True allows server side to handle matches
        public bool Matches(LevelMetadata item) { return true; }
    }
}
