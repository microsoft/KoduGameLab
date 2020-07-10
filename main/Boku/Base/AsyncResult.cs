// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System;

namespace Boku.Base
{
    /// <summary>
    /// Base class for async results.
    /// </summary>
    public class AsyncResult
    {
        /// <summary>
        /// Whether or not the operation was successful.
        /// </summary>
        public bool Success;

        /// <summary>
        /// The user-provided parameter.
        /// </summary>
        public object Param;

        /// <summary>
        /// The amount of time it took to complete the operation.
        /// </summary>
        public double Seconds;
    }
}
