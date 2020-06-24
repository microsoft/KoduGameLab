
using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;

#if WINDOWS
using System.Windows.Forms;
#endif

using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Audio;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using Microsoft.Xna.Framework.Storage;

using KoiX.Input;

namespace KoiX
{
    /// <summary>
    /// Static singleton used to get app information into the class library.
    /// This is how the app tells the library which content manager to use and
    /// which path to get content from.
    /// </summary>
    public class KoiLibrary
    {
        public enum InputDevice
        {
            None = 0x00,
            Gamepad = 0x01,
            Mouse = 0x02,
            Keyboard = 0x04,
            Touch = 0x08,
            // KeyboardMouse is problematic since you can't test for it properly, ie
            // if (LastTouchedDevice == KeyboardMouse) will fail if device is just Mouse.
            //KeyboardMouse = Keyboard | Mouse,
        };

        #region Members

        static ContentManager contentManager = null;
        static String mediaPath = null;  // Media path for all data.  Each content project will have its own dir under this.

        // Provides a common place to access the graphics device.  This also eliminates
        // the need for the library objects to hold onto a device reference.
        static GraphicsDevice graphicsDevice = null;

        static InputEventManager inputEventManager = null;

        static InputDevice lastTouched = InputDevice.Mouse;
        static InputDevice prevLastTouched = InputDevice.None;

        static SpriteBatch batch = null;
        static SpriteFont uiFont = null;

        // Position of parent window for WinForms hosted apps.
        static Vector2 parentPosition = Vector2.Zero;

        // For windowed apps the backbuffer may be smaller than the window region.
        // This gives the window region.
        static Rectangle clientRect = new Rectangle();

        static Random rnd = null;

        // Only used when no XNA_CONTROL_BASED
        static Game game = null;

#if DEBUG
        public static string DebugString = null;
#endif

        #endregion

        #region Accessors

        
        protected static ContentManager ContentManager
        {
            get { return contentManager; }
            set { contentManager = value; }
        }
        
        /// <summary>
        /// Root media path for all data.  Each content project will have its own dir under this.
        /// Default is:
        ///     Content -- app specific content.
        ///     KoiXContent -- KoiLibrary content.
        /// </summary>
        public static String MediaPath
        {
            get { return mediaPath; }
        }

        public static GraphicsDevice GraphicsDevice
        {
            get { return graphicsDevice; }
        }

        /// <summary>
        /// Should we be using ViewportSize or ClientRect?  When are they different?
        /// TODO (****) Figure this out and document here.
        /// </summary>
        public static Point ViewportSize
        {
            get { return new Point(graphicsDevice.Viewport.Width, graphicsDevice.Viewport.Height); }
        }

        /// <summary>
        /// For windowed apps the backbuffer may be smaller than the window region.
        /// This gives the window region.
        /// </summary>
        public static Rectangle ClientRect
        {
            get { return clientRect; }
        }

        public static InputEventManager InputEventManager
        {
            get { return inputEventManager; }
        }

        /// <summary>
        /// 10 point font used by UI.  May also be used by app.
        /// </summary>
        public static SpriteFont UIFont10
        {
            get { return uiFont; }
        }

        /// <summary>
        /// SpriteBatch used by UI.  May also be used by app.
        /// </summary>
        public static SpriteBatch SpriteBatch
        {
            get { return batch; }
        }

        /// <summary>
        /// Gives the last touched input device.
        /// Note that this treats keyaboard and mouse
        /// as seperate devices.
        /// </summary>
        public static InputDevice LastTouchedDevice
        {
            get { return lastTouched; }
        }

        public static bool LastTouchedDeviceIsMouse
        {
            get { return lastTouched == InputDevice.Mouse; }
        }

        public static bool LastTouchedDeviceIsKeyboard
        {
            get { return lastTouched == InputDevice.Keyboard; }
        }

        public static bool LastTouchedDeviceIsTouch
        {
            get { return lastTouched == InputDevice.Touch; }
        }

        public static bool LastTouchedDeviceIsGamepad
        {
            get { return lastTouched == InputDevice.Gamepad; }
        }

        public static bool LastTouchedDeviceIsKeyboardMouse
        {
            get { return lastTouched == InputDevice.Keyboard || lastTouched == InputDevice.Mouse; }
        }

        /// <summary>
        /// Did the input device change this frame.  This may or may not
        /// be useful since it will trigger on keyboard->mouse which you
        /// may just want to treat as the same.
        /// </summary>
        public static bool LastTouchedDeviceChanged
        {
            get { return lastTouched != prevLastTouched; }
        }

        /// <summary>
        /// Gives the last touched input device for teh previous frame.
        /// this can be compared to LastTouchedDevice to detect any
        /// change in input device.
        /// TODO (****) Is this the only use for this?  Maybe expose
        /// a bool InputDeviceChanged?
        /// </summary>
        /*
        public static InputDevice PrevLastTouchedDevice
        {
            get { return prevLastTouched; }
        }
        */

        public static Random Random
        {
            get { return rnd; }
        }

        public static Game Game
        {
            get { return game; }
        }

        #endregion

        // c'tor
        KoiLibrary()
        {
        }   // end of KoiLibrary c'tor

#if WINDOWS
        /// <summary>
        /// Init version to be used for Windows apps built around XNAControl.
        /// </summary>
        /// <param name="mainForm"></param>
        public static void Init(ContentManager contentManager, Form mainForm, IntPtr xnaControlHandle)
        {
            KoiLibrary.contentManager = contentManager;
            KoiLibrary.mediaPath = @"Content";

            //PerlinNoise.ValueTableInit(42);

            rnd = new Random(); 

            inputEventManager = new InputEventManager();

            GamePadInput.Init();
            LowLevelMouseInput.Init();
            LowLevelKeyboardInput.Init();

#if WINDOWS_PHONE
            PhoneTouchInput.Init();
#else
            PCTouchInput.Init(xnaControlHandle);
#endif

#if WINDOWS
            //WinFormsKeyboardInput.Init(null);
            WinFormsKeyboardInput.Init(mainForm);
#endif
            KeyboardInputX.Init();

            TwitchManager.Init();
            //Textures.Init();

        }   // end of KoiLibrary Init()
#endif


        public static void Update()
        {
            Rectangle rect = new Rectangle(0, 0, ViewportSize.X, ViewportSize.Y);;
            Update(rect);

            //Debug.Assert(false, "This should be depricated in favor of ClientSize.  I think.");
        }

        public static void Update(Rectangle clientRect)
        {
            if (clientRect.Width > 1400)
            {
            }

            KoiLibrary.clientRect = clientRect;

            Time.Update();

            // Only do input if Window is focused.
            if (Boku.XNAControl.Instance.Focused)
            {

                // Gamepad
                if (GamePadInput.Update())
                {
                    prevLastTouched = lastTouched;
                    lastTouched = InputDevice.Gamepad;
                }

                // Touch
#if WINDOWS_PHONE
            if (PhoneTouchInput.Update())
            {
                lastTouched = InputDevice.Touch;
            }
#else
                if (PCTouchInput.Update())
                {
                    prevLastTouched = lastTouched;
                    lastTouched = InputDevice.Touch;
                    // Tell mouse system to ignore events.  This prevents
                    // touch events which get promoted to mouse events from
                    // being acted upon twice.
                    LowLevelMouseInput.IgnoreMouseEvents = true;
                }
                else
                {
                    LowLevelMouseInput.IgnoreMouseEvents = false;
                }
#endif

                // Mouse
                if (LowLevelMouseInput.Update())
                {
                    prevLastTouched = lastTouched;
                    lastTouched = InputDevice.Mouse;
                }

                // Keys as text.
                if (LowLevelKeyboardInput.Update())
                {
                    prevLastTouched = lastTouched;
                    lastTouched = InputDevice.Keyboard;
                }

                // Keys as buttons.
                if (KeyboardInputX.Update())
                {
                    prevLastTouched = lastTouched;
                    lastTouched = InputDevice.Keyboard;
                }

#if WINDOWS
                // Windows Forms based keyboard input.  If initialized with
                // null, this does nothing.
                if (WinFormsKeyboardInput.Update())
                {
                    prevLastTouched = lastTouched;
                    lastTouched = InputDevice.Keyboard;
                }
#endif

                if (LastTouchedDeviceIsGamepad)
                {
                    KoiLibrary.InputEventManager.ProcessGamePadEvent(GamePadInput.GetGamePad0());
                }

            }   // end if focused

            TwitchManager.Update();
        }   // end KoiLibrary Update()

        /// <summary>
        /// Helper function for loading effects.  If running HiDef tries to
        /// load hiDef version first.  If not there then it falls back to Reach.
        /// </summary>
        /// <param name="path"></param>
        /// <returns></returns>
        public static Effect LoadEffect(string path)
        {
            Effect effect = null;
            // For shaders, if HiDef, look in HiDef content first.
            if (graphicsDevice.GraphicsProfile == GraphicsProfile.HiDef)
            {
                // Will throw if not there.  Just swallow the exception and let fall through.
                try
                {
                    effect = KoiLibrary.ContentManager.Load<Effect>(Path.Combine(@"ContentHiDef", path));
                }
                catch(ContentLoadException e) 
                {
                    if (e != null)
                    {
                        //Debug.Assert(false, "Should have HiDef version of this shader...");
                    }
                }
            }

            // This will also throw if not there but we can't swallow it.  Let it blow up.
            if (effect == null)
            {
                effect = KoiLibrary.ContentManager.Load<Effect>(Path.Combine(KoiLibrary.MediaPath, path));
            }

            return effect;
        }   // end of LoadEffect()

        public static Texture2D LoadTexture2D(string path)
        {
            Texture2D texture = KoiLibrary.ContentManager.Load<Texture2D>(Path.Combine(KoiLibrary.MediaPath, path));

            return texture;
        }   // end of LoadTexture2D

        public static SpriteFont LoadSpriteFont(string path)
        {
            SpriteFont spriteFont = KoiLibrary.ContentManager.Load<SpriteFont>(Path.Combine(KoiLibrary.MediaPath, path));

            return spriteFont;
        }   // end of LoadSpriteFont

        public static SoundEffect LoadSoundEffect(string path)
        {
            SoundEffect soundEffect = KoiLibrary.ContentManager.Load<SoundEffect>(Path.Combine(KoiLibrary.MediaPath, path));

            return soundEffect;
        }   // end of SoundEffect

        public static void LoadContent(GraphicsDevice graphicsDevice)
        {
            KoiLibrary.graphicsDevice = graphicsDevice;

            KoiLibrary.clientRect = new Rectangle(0, 0, ViewportSize.X, ViewportSize.Y);

            // Static library objects.

#if !NETFX_CORE
            //MouseUI.UIQuad.LoadContent(GraphicsDevice);
            //MouseUI.UITextures.LoadContent();
#endif

            //uiFont = ContentManager.Load<SpriteFont>(Path.Combine(MediaPath, @"KoiXContent\Fonts\Calibri12");
            batch = new SpriteBatch(GraphicsDevice);


#if !NETFX_CORE
            //GenericEffects.LoadContent();
#endif

        }   // end of KoiLibrary LoadContent()

        public static void UnloadContent()
        {
            KoiLibrary.graphicsDevice = null;

            // Static library objects.

            uiFont = null;
            batch = null;

#if !NETFX_CORE
            //GenericEffects.UnloadContent();
#endif

        }   // end of KoiLibrary UnloadContent()


        // TODO 4.0
        //static DepthStencilBuffer savedDB = null;
        /*
        public static void SetRenderTarget( RenderTarget2D rt, DepthStencilBuffer db )
        {
            GraphicsDevice device = GraphicsDevice;

            if (savedDB == null)
            {
                savedDB = device.DepthStencilBuffer;
            }
            device.DepthStencilBuffer = db;

            device.SetRenderTarget( 0, rt );
        }   // end of KoiLibrary SetRenderTarget()
        */
        /*
        public static void SetRenderTarget( RenderTarget2D rt )
        {
            SetRenderTarget( rt );
        }   // end of KoiLibrary SetRenderTarget()

        public static void RestoreRenderTarget()
        {
            GraphicsDevice device = GraphicsDevice;

            device.SetRenderTarget( null );
            // TODO 4.0
            //device.SetRenderTarget(0, null);
            //device.DepthStencilBuffer = savedDB;
        }   // end of KoiLibrary RestoreRenderTarget()
        */
        /*
        public static void Clear( Color color )
        {
            GraphicsDevice device = GraphicsDevice;

            ClearOptions options = ClearOptions.Target;
            if (device.DepthStencilBuffer != null)
            {
                options |= ClearOptions.DepthBuffer | ClearOptions.Stencil;
            }
            device.Clear( options, color, 1.0f, 0 );
        }   // end of KoiLibrary Clear()
        */

    }   // end of class KoiLibrary

}   // end of namespace KoiX


