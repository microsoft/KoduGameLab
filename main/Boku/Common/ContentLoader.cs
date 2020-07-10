// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System;
using System.Threading;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;

using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Audio;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using Microsoft.Xna.Framework.Storage;

using KoiX;

using Boku.Base;

namespace Boku.Common
{
    /// <summary>
    /// Manages INeedsDeviceReset calls either synchronously or asynchronously.
    /// During app initialization, default operation is asynchronous. Once init
    /// is complete, we switch to default synchronous load behavior.
    /// </summary>
    public static partial class ContentLoader
    {
        #region Fields
#if NETFX_CORE
        private static ContentManager contentManager;
#endif
        private static bool defaultImmediate = true;
        private static Queue<INeedsDeviceReset> queuedLoads = new Queue<INeedsDeviceReset>();
        private static Queue<INeedsDeviceReset> queuedInits = new Queue<INeedsDeviceReset>();

        #endregion

        #region Delegates

        public delegate void OnLoadCompleteDelegate();
        public static OnLoadCompleteDelegate OnLoadComplete;

        #endregion

        #region Accessors

        public static ContentManager ContentManager
        {
#if NETFX_CORE
            get { return contentManager ?? (contentManager = new ContentManager(BokuGame.bokuGame.Services)); }
#else
            get { return XNAControl.ContentManager; }
#endif
        }

        public static bool DefaultImmediate
        {
            get { return defaultImmediate; }
            set { defaultImmediate = value; }
        }

        #endregion

        #region Public

        public static void Load(INeedsDeviceReset item)
        {
            Load(item, defaultImmediate);
        }

        public static void Load(INeedsDeviceReset item, bool immediate)
        {
            if (item == null)
                return;

            if (immediate)
            {
                item.LoadContent(immediate);
                item.InitDeviceResources(KoiLibrary.GraphicsDevice);
                BokuGame.Loaded(item);
            }
            else
            {
                // Put the item in the load queue.
                queuedLoads.Enqueue(item);
            }
        }

        public static void Unload(INeedsDeviceReset foo)
        {
            if (foo != null)
            {
                foo.UnloadContent();
            }
        }

        public static void Update()
        {
            long startTime = Environment.TickCount * TimeSpan.TicksPerMillisecond;

            // Only process queued inits if there are no queued loads.
            if (queuedLoads.Count == 0 && queuedInits.Count > 0)
            {
                int startCount = queuedInits.Count;

                // Process queued inits
                while (queuedInits.Count > 0)
                {
                    INeedsDeviceReset item = queuedInits.Dequeue();

                    if (item != null)
                    {
                        // Perform the init
                        item.InitDeviceResources(KoiLibrary.GraphicsDevice);
                        BokuGame.Loaded(item);
                    }

                    // Limit to 1/4 second of processing.
                    long currTime = Environment.TickCount * TimeSpan.TicksPerMillisecond;
                    if (currTime - startTime > 250)
                        break;
                }

                // All loads and inits complete?
                if (startCount > 0 && queuedLoads.Count == 0 && queuedInits.Count == 0)
                {
                    // Make the completion callback.
                    if (OnLoadComplete != null)
                        OnLoadComplete();
                }

            }
            else
            {
                // Process queued loads
                while (queuedLoads.Count > 0)
                {
                    INeedsDeviceReset item = queuedLoads.Dequeue();

                    if (item != null)
                    {
                        // Perform load
                        item.LoadContent(false);

                        // Queue for init
                        queuedInits.Enqueue(item);
                    }

                    // Limit to 1/4 second of processing.
                    long currTime = Environment.TickCount * TimeSpan.TicksPerMillisecond;
                    if (currTime - startTime > 250)
                        break;
                }
            }
        }

        #endregion
    }
}
