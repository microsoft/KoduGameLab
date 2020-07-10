// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System;
using System.Collections.Generic;
using System.Text;
using System.Diagnostics;


namespace Boku.Common.Sharing
{
    /// <summary>
    /// A delayed no-op operation. Useful for making a callback at some point in the future.
    /// </summary>
    public class TimeDelayedOperation : AsyncOperation
    {
        float duration;

        public TimeDelayedOperation(float duration, AsyncOpCallback callback, object param, object pwner)
            : base(AsyncOperations.Timer, callback, param, pwner)
        {
            this.duration = duration;
            RequiresSignIn = false;
        }

        public override void Dispose()
        {
        }

        protected override void IStart()
        {
            // Twitches are just so convienent! and cross-platform.
            TwitchManager.Set<float> set = delegate(float val, Object param) { };
            TwitchManager.CreateTwitch<float>(0, 1, set, duration, TwitchCurve.Shape.Linear, null, OnTwitchComplete);
        }

        private void OnTwitchComplete(object param)
        {
            Complete(true);
        }
    }
}
