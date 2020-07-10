// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System;
using System.Threading;

namespace Boku.Input
{
#if !NETFX_CORE
    public class CommPort : CommLine
    {
        public delegate void OnReceiveCallback(string cmd);
        public delegate void OnOpenCallback();

        private OnReceiveCallback onReceive;
        private OnOpenCallback onOpen;
        private CommLineSettings commSettings;

        public CommPort(string portName, int baudRate, OnReceiveCallback onReceive, OnOpenCallback onOpen)
        {
            this.onReceive = onReceive;
            this.onOpen = onOpen;
            this.commSettings = new CommLineSettings()
            {
                port = portName,
                baudRate = baudRate,
                rxTerminator = ASCII.LF,
                txTerminator = new ASCII[1] { ASCII.LF },
            };
            this.Setup(this.commSettings);
        }

        protected override CommBase.CommBaseSettings CommSettings()
        {
            return this.commSettings;
        }

        public void WriteLine(string cmd)
        {
            this.Send(cmd);
        }

        protected override void OnRxLine(string s)
        {
            ThreadPool.QueueUserWorkItem((object state) =>
            {
                this.onReceive.Invoke(s);
            });
        }

        protected override bool AfterOpen()
        {
            ThreadPool.QueueUserWorkItem((object state) =>
            {
                this.onOpen.Invoke();
            });
            return true;
        }
    }
#endif
}
