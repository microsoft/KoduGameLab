// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System;

namespace Boku.Base
{
    /// <summary>
    /// Represents a method to be called when an asynchronous operation completes.
    /// </summary>
    /// <param name="state"></param>
    public delegate void BokuAsyncCallback(AsyncResult result);
    // Tacked "boku" to the front to resolve a name collision with System.AsyncCallback
}
