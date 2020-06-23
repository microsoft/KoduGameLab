
using System;
using System.Threading;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;

// The IGraphicsDeviceService interface requires a DeviceCreated event, but we
// always just create the device inside our constructor, so we have no place to
// raise that event. The C# compiler warns us that the event is never used, but
// we don't care so we just disable this warning.
#pragma warning disable 67

namespace Boku
{
    /// <summary>
    /// Helper class responsible for creating and managing the GraphicsDevice.
    /// All GraphicsDeviceControl instances share the same GraphicsDeviceService,
    /// so even though there can be many controls, there will only ever be a single
    /// underlying GraphicsDevice. This implements the standard IGraphicsDeviceService
    /// interface, which provides notification events for when the device is reset
    /// or disposed.
    /// </summary>
    class GraphicsDeviceService : IGraphicsDeviceService
    {
        #region Fields


        // Singleton device service instance.
        static GraphicsDeviceService singletonInstance;


        // Keep track of how many controls are sharing the singletonInstance.
        static int referenceCount;


        #endregion


        /// <summary>
        /// Constructor is private, because this is a singleton class:
        /// client controls should use the public AddRef method instead.
        /// </summary>
        GraphicsDeviceService(IntPtr windowHandle, int width, int height)
        {
            parameters = new PresentationParameters();

            parameters.BackBufferWidth = Math.Max(width, 1);
            parameters.BackBufferHeight = Math.Max(height, 1);
            parameters.BackBufferFormat = SurfaceFormat.Color;
            parameters.DepthStencilFormat = DepthFormat.Depth24Stencil8;
            parameters.DeviceWindowHandle = windowHandle;

            parameters.PresentationInterval = PresentInterval.Immediate;
            if (BokuSettings.Settings.Vsync)
            {
                parameters.PresentationInterval = PresentInterval.One;
            }

            parameters.IsFullScreen = false;

            // Turn this off for things that don't benefit from multisampling.
            if (BokuSettings.Settings.AntiAlias == true)
            {
                parameters.MultiSampleCount = 4;
            }
            else
            {
                parameters.MultiSampleCount = 1;
            }


            foreach (GraphicsAdapter ga in GraphicsAdapter.Adapters)
            {
#if NETFX_CORE
                Debug.Assert(false, "Waiting on MG");
                // Assume Reach for now.
                hwSupportsReach = true;
                break;
#else
                if (ga.IsDefaultAdapter)
                {
                    if (ga.IsProfileSupported(GraphicsProfile.Reach))
                    {
                        BokuGame.hwSupportsReach = true;
                    }
                    if (ga.IsProfileSupported(GraphicsProfile.HiDef))
                    {
                        BokuGame.hwSupportsHiDef = true;
                    }

                    break;
                }
#endif
            }

            // Set HiDef iff HW supports AND user doesn't prefer Reach.
            BokuGame.hidef = false;
            if (BokuGame.hwSupportsHiDef && !BokuSettings.Settings.PreferReach)
            {
                BokuGame.hidef = true;
            }
            else
            {
                BokuSettings.ConstrainToReach();

                // Limit max size to 2kx2k since in Reach.
                MainForm.Instance.MaximumSize = new System.Drawing.Size(2048, 2048);
            }



            // Select right profile.
            GraphicsProfile profile = BokuGame.HiDefProfile ? GraphicsProfile.HiDef : GraphicsProfile.Reach;

#if NETFX_CORE
            // For Win8 always force fullscreen and use full device resolution.
            graphics.IsFullScreen = BokuSettings.Settings.FullScreen = true;
            BokuSettings.Settings.ResolutionX = graphics.PreferredBackBufferWidth;
            BokuSettings.Settings.ResolutionY = graphics.PreferredBackBufferHeight;
#else
            //graphics.IsFullScreen = BokuSettings.Settings.FullScreen;
            //graphics.PreferredBackBufferWidth = BokuSettings.Settings.ResolutionX;
            //graphics.PreferredBackBufferHeight = BokuSettings.Settings.ResolutionY;
#endif

            // <<<<<<<<<<<<<<<<<<<<<<<<<<<<<< FULL SCREEN WINDOWED MODE FIX

#if !NETFX_CORE
            // Always start windowed.
            //graphics.IsFullScreen = false;
#endif

            // FULL SCREEN WINDOWED MODE FIX >>>>>>>>>>>>>>>>>>>>>>>>>>>>>

            //graphics.SynchronizeWithVerticalRetrace = syncRefresh;
            //graphics.PreferMultiSampling = BokuSettings.Settings.AntiAlias;





            graphicsDevice = new GraphicsDevice(GraphicsAdapter.DefaultAdapter,
                                                profile,
                                                parameters);

            Mouse.WindowHandle = windowHandle;
            
        }


        /// <summary>
        /// Gets a reference to the singleton instance.
        /// </summary>
        public static GraphicsDeviceService AddRef(IntPtr windowHandle,
                                                   int width, int height)
        {
            // Increment the "how many controls sharing the device" reference count.
            if (Interlocked.Increment(ref referenceCount) == 1)
            {
                // If this is the first control to start using the
                // device, we must create the singleton instance.
                singletonInstance = new GraphicsDeviceService(windowHandle,
                                                              width, height);
            }

            return singletonInstance;
        }


        /// <summary>
        /// Releases a reference to the singleton instance.
        /// </summary>
        public void Release(bool disposing)
        {
            // Decrement the "how many controls sharing the device" reference count.
            if (Interlocked.Decrement(ref referenceCount) == 0)
            {
                // If this is the last control to finish using the
                // device, we should dispose the singleton instance.
                if (disposing)
                {
                    if (DeviceDisposing != null)
                        DeviceDisposing(this, EventArgs.Empty);

                    graphicsDevice.Dispose();
                }

                graphicsDevice = null;
            }
        }

        
        /// <summary>
        /// Resets the graphics device to whichever is bigger out of the specified
        /// resolution or its current size. This behavior means the device will
        /// demand-grow to the largest of all its GraphicsDeviceControl clients.
        /// </summary>
        public void ResetDevice(int width, int height)
        {
            if (DeviceResetting != null)
                DeviceResetting(this, EventArgs.Empty);

            parameters.BackBufferWidth = Math.Max(parameters.BackBufferWidth, width);
            parameters.BackBufferHeight = Math.Max(parameters.BackBufferHeight, height);

            graphicsDevice.Reset(parameters);

            if (DeviceReset != null)
                DeviceReset(this, EventArgs.Empty);
        }

        
        /// <summary>
        /// Gets the current graphics device.
        /// </summary>
        public GraphicsDevice GraphicsDevice
        {
            get { return graphicsDevice; }
        }

        GraphicsDevice graphicsDevice;


        // Store the current device settings.
        PresentationParameters parameters;


        // IGraphicsDeviceService events.
        public event EventHandler<EventArgs> DeviceCreated;
        public event EventHandler<EventArgs> DeviceDisposing;
        public event EventHandler<EventArgs> DeviceReset;
        public event EventHandler<EventArgs> DeviceResetting;
    }
}
