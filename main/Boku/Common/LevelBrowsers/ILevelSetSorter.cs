// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System;

namespace Boku.Common
{
    /// <summary>
    /// Base class for classes that compare levels.
    /// Used by queries to sort their level sets.
    /// </summary>
    public interface ILevelSetSorter
    {
        bool Dirty { get; set; }
        int Compare(LevelMetadata a, LevelMetadata b);
        int JumpCompatibility(LevelMetadata level, string searchString);
    }
}
