// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.


// Uncomment this to debug scene switching.
//#define DEBUG_SWITCH

using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using System.Xml;
using System.Xml.Serialization;

using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Audio;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using Microsoft.Xna.Framework.Input.Touch;
using Microsoft.Xna.Framework.Storage;

using KoiX;
using KoiX.Input;

namespace KoiX.Managers
{
    /// <summary>
    /// Static class which managers which Scene is currently active.
    /// </summary>
    public static class SceneManager
    {
        class SceneParams
        {
            public int FrameDelay;
            public BaseScene PendingScene;
            public object[] Arguments;
            public Transition CurTransition;
            public Color FadeColor;
            public float FadeTime;
            public TwitchCurve.Shape FadeShape;
        }

        #region Members

        public enum Transition
        {
            Cut,
            Fade,
            ColorFade,
            ScaleInFade,    // New scene shrinks/fades into place.
            ScaleOutFade,   // Old scene grows/fades away.
            SlideInFromLeft,
            SlideInFromRight,
        }

        static Dictionary<string, BaseScene> nameDict = new Dictionary<string, BaseScene>();

        static RenderTarget2D rt;       // Needs to stay same size as viewport.
        static RenderTarget2D rt2;

        static BaseScene pendingScene;  // The Scene we will switch to on the next Update().
                                        // Set to null on switch.
        static object[] arguments;      // Args for pending Scene's Activate call.

        static BaseScene curScene;      // Current scene.
        static BaseScene prevScene;     // May be useful for debug.

        static Transition curTransition = Transition.Cut; // Transition to use on next scene switch.
        static Color fadeColor = Color.Black;
        static float fadeTime = 0.0f;
        static TwitchCurve.Shape fadeShape = TwitchCurve.Shape.Linear;

        static bool inTransition = false;

        static double sceneStartTime = 0.0f;    // When did the current scene start?

        static float t = 0;

        static Task loadGraphicsTask;
        static bool initialized = false;

        static List<SceneParams> delayedSwitches = new List<SceneParams>();

        // Default transition values.
        public static Transition DefaultTransition = Transition.Cut;
        public static Color DefaultFadeColor = Color.Black;
        public static float DefaultTransitionTime = 0.0f;
        public static TwitchCurve.Shape DefaultTransitionCurveShape = TwitchCurve.Shape.EaseOut;

        #endregion

        #region Accessors

        public static BaseScene CurrentScene
        {
            get { return curScene; }
        }

        public static BaseScene PreviousScene
        {
            get { return prevScene; }
        }

        /// <summary>
        /// Time when the current scene was activated.
        /// Based on Time.WallClockTotalSeconds
        /// </summary>
        public static double SceneStartTime
        {
            get { return sceneStartTime; }
        }

        #endregion

        #region Public

        public static void Update()
        {
            if (loadGraphicsTask != null)
            {
                if (loadGraphicsTask.IsFaulted)
                {
                    Debug.Assert(false);
                }

                // If we try and switch scenes before the 
                // loading task is completed don't do anything.
                if (curScene != null && pendingScene != null && !loadGraphicsTask.IsCompleted)
                {
                    return;
                }
            }

            /*
            // Debug foo
            if (LowLevelKeyboardInput.RawKey == Keys.Up)
            {
                DialogManagerX.Zoom += 0.1f;
            }
            else if (LowLevelKeyboardInput.RawKey == Keys.Down)
            {
                DialogManagerX.Zoom -= 0.1f;
            }
            else if (LowLevelKeyboardInput.RawKey == Keys.Space)
            {
                DialogManagerX.Zoom = 1.0f;
            }
            */
            
            //try
            {
                Point winSize = new Point(KoiLibrary.ClientRect.Width, KoiLibrary.ClientRect.Height);

                Point desiredRTSize = winSize;

                // Reach limits RT to 2k x 2k
                if (KoiLibrary.GraphicsDevice.GraphicsProfile == GraphicsProfile.Reach)
                {
                    desiredRTSize.X = Math.Min(desiredRTSize.X, 2048);
                    desiredRTSize.Y = Math.Min(desiredRTSize.Y, 2048);
                }

                // HiDef limits RT to 4k x 4k
                if (KoiLibrary.GraphicsDevice.GraphicsProfile == GraphicsProfile.HiDef)
                {
                    desiredRTSize.X = Math.Min(desiredRTSize.X, 4096);
                    desiredRTSize.Y = Math.Min(desiredRTSize.Y, 4096);
                }

                // Update rendertarget
                if (rt == null || rt.Width != desiredRTSize.X || rt.Height != desiredRTSize.Y)
                {
                    DeviceResetX.Release(ref rt);
                    DeviceResetX.Release(ref rt2);

                    rt = new RenderTarget2D(KoiLibrary.GraphicsDevice, desiredRTSize.X, desiredRTSize.Y, false, SurfaceFormat.Color, DepthFormat.None, 1, RenderTargetUsage.PlatformContents);
                    rt2 = new RenderTarget2D(KoiLibrary.GraphicsDevice, desiredRTSize.X, desiredRTSize.Y, false, SurfaceFormat.Color, DepthFormat.None, 1, RenderTargetUsage.PlatformContents);
                }

                // Switch Scenes?
                if (pendingScene != null)
                {
                    // If we're doing a fancy transition we need to capture a snapshot
                    // of the previous scene.  We need to do this before calling Deactivate
                    // on the current scene and Activate on the new one otherwise the content
                    // displayed by the DialogManager is wrong.
                    if (curTransition != Transition.Cut)
                    {
                        GraphicsDevice device = KoiLibrary.GraphicsDevice;

                        if (curScene != null)
                        {
                            curScene.Render(rt);

                            // This doesn't work because the Activate/Deactivate calls have already
                            // gone out to the scenes so the DialogManager has the wrong content.
                            device.SetRenderTarget(rt);
                            DialogManagerX.Render();
                            device.SetRenderTarget(null);
                        }
                        else
                        {
                            device.SetRenderTarget(rt);
                            device.Clear(fadeColor);
                            device.SetRenderTarget(null);
                        }
                    }

                    // Swap scenes.
                    prevScene = curScene;
                    curScene = pendingScene;
                    pendingScene = null;
                    curScene.PrevScene = prevScene;

                    if (prevScene != null)
                    {
                        prevScene.Deactivate();
                        InputEventManager.InputEventHandlerListSet.ValidateSceneSwitch();
                    }
                    curScene.Activate(arguments);
                    sceneStartTime = Time.WallClockTotalSeconds;

                    if (curTransition != Transition.Cut)
                    {
                        inTransition = true;
                        t = 0;
                        {
                            TwitchManager.Set<float> set = delegate(float value, Object param) { t = value; inTransition = t < 1; };
                            TwitchManager.CreateTwitch<float>(0.0f, 1.0f, set, fadeTime, fadeShape);
                        }
                    }
                }

                // Note we don't have to call ProcessTouchHits directly since
                // we give that to PCTouchInput to do.  We need to do it this
                // way to get the ordering correct:
                //   Read the touches.
                //   Process for hits and add the hits to the touch samples (ProcessTouchHits()).
                //   Feed the modified hits to gesture and event processing.

                ProcessMouseHits();

                // Call update on the current scene.
                if (curScene != null)
                {
                    curScene.Update();
                }
            }
            /*
            catch (Exception e)
            {
                if (e != null)
                {
                    throw e;
                }
            }
            */

            // Check for any delayed scene switches.
            for (int i = delayedSwitches.Count - 1; i >= 0; i--)
            {
                SceneParams sp = delayedSwitches[i];
                --sp.FrameDelay;

                if (sp.FrameDelay <= 0)
                {
                    delayedSwitches.RemoveAt(i);

                    SwitchToScene(sp.PendingScene, transition: sp.CurTransition, color: sp.FadeColor, transitionTime: sp.FadeTime, shape: sp.FadeShape, args: sp.Arguments);
                }
            }

        }   // end of Update()

        public static void Render()
        {
            //try
            {
                GraphicsDevice device = KoiLibrary.GraphicsDevice;
                SpriteBatch batch = KoiLibrary.SpriteBatch;

                // Blend the old one over the top if needed.
                if (inTransition)
                {
                    switch (curTransition)
                    {
                        case Transition.Fade:
                            {
                                // Render the current scene.
                                curScene.Render(null);
                                DialogManagerX.Render();
                                curScene.PostDialogRender(null);

                                // Blend old scene over it, fading out.
                                batch.Begin(SpriteSortMode.Immediate, BlendState.NonPremultiplied);
                                {
                                    batch.Draw(rt, Vector2.Zero, new Color(1, 1, 1, 1.0f - t));
                                }
                                batch.End();
                            }
                            break;
                        case Transition.ColorFade:
                            {
                                if (t < 0.5f)
                                {
                                    // Fade from old to color.
                                    device.Clear(fadeColor);
                                    batch.Begin(SpriteSortMode.Immediate, BlendState.NonPremultiplied);
                                    {
                                        batch.Draw(rt, Vector2.Zero, Color.White * (1.0f - t * 2.0f));
                                    }
                                    batch.End();
                                }
                                else
                                {
                                    // Render the current scene to rt.
                                    curScene.Render(rt);
                                    DialogManagerX.Render();
                                    curScene.PostDialogRender(rt);

                                    // Fade from color to new.
                                    device.Clear(fadeColor);
                                    batch.Begin(SpriteSortMode.Immediate, BlendState.NonPremultiplied);
                                    {
                                        batch.Draw(rt, Vector2.Zero, Color.White * ((t - 0.5f) * 2.0f));
                                    }
                                    batch.End();
                                }
                            }
                            break;
                        case Transition.ScaleInFade:
                            {
                                // Render the current scene.
                                curScene.Render(rt2);
                                DialogManagerX.Render();
                                curScene.PostDialogRender(rt2);

                                int width = KoiLibrary.ClientRect.Width;
                                int height = KoiLibrary.ClientRect.Height;
                                float scaleAmount = width * 0.1f * (1.0f - t);
                                Rectangle rect = new Rectangle((int)(0 - scaleAmount), (int)(0 - scaleAmount), (int)(width + 2 * scaleAmount), (int)(height + 2 * scaleAmount));

                                // Blend old scene over it, fading out.
                                batch.Begin(SpriteSortMode.Immediate, BlendState.AlphaBlend);
                                {
                                    // Render scaled version of new scene.
                                    batch.Draw(rt2, rect, Color.White);
                                    // Blended with old scene.
                                    batch.Draw(rt, Vector2.Zero, Color.White * (1.0f - t));
                                }
                                batch.End();
                            }
                            break;
                        case Transition.ScaleOutFade:
                            {
                                // Render the current scene at normal size.
                                curScene.Render(rt2);
                                DialogManagerX.Render();
                                curScene.PostDialogRender(rt2);

                                int width = KoiLibrary.ClientRect.Width;
                                int height = KoiLibrary.ClientRect.Height;
                                float scaleAmountX = width * 0.2f * t;
                                float scaleAmountY = height * 0.2f * t;
                                Rectangle rect = new Rectangle((int)(0 - scaleAmountX), (int)(0 - scaleAmountY), (int)(width + 2 * scaleAmountX), (int)(height + 2 * scaleAmountY));

                                // Blend old scene over it, fading out and zooming in..
                                batch.Begin(SpriteSortMode.Immediate, BlendState.AlphaBlend);
                                {
                                    // Render new scene.
                                    batch.Draw(rt2, Vector2.Zero, Color.White);
                                    // Blended with scaled version of old scene.
                                    batch.Draw(rt, rect, Color.White * (1.0f - t));
                                }
                                batch.End();
                            }
                            break;
                        case Transition.SlideInFromLeft:
                            {
                                // Render the current scene and dialogs to rt2.
                                device.SetRenderTarget(rt2);
                                curScene.Render(null);
                                DialogManagerX.Render();
                                curScene.PostDialogRender(null);
                                device.SetRenderTarget(null);

                                float offset = rt.Width * t;
                                batch.Begin(SpriteSortMode.Immediate, BlendState.AlphaBlend);
                                {
                                    // Render new scene.
                                    batch.Draw(rt2, new Vector2(-rt.Width + offset, 0), Color.White);
                                    // Render old scene.
                                    batch.Draw(rt, new Vector2(offset, 0), Color.White);
                                }
                                batch.End();
                            }
                            break;
                        case Transition.SlideInFromRight:
                            {
                                // Render the current scene and dialogs to rt2.
                                device.SetRenderTarget(rt2);
                                curScene.Render(null);
                                DialogManagerX.Render();
                                curScene.PostDialogRender(null);
                                device.SetRenderTarget(null);

                                float offset = rt.Width * t;
                                batch.Begin(SpriteSortMode.Immediate, BlendState.AlphaBlend);
                                {
                                    // Render new scene.
                                    batch.Draw(rt2, new Vector2(rt.Width - offset, 0), Color.White);
                                    // Render old scene.
                                    batch.Draw(rt, new Vector2(-offset, 0), Color.White);
                                }
                                batch.End();
                            }
                            break;
                    }
                }
                else
                {
                    // Not in transition.
                    // Render the current scene.
                    if (curScene != null)
                    {
                        curScene.Render(null);
                    }
                    DialogManagerX.Render();

                    // Now render anything that should appear on top of the dialogs.
                    // Put here for the greeter Kodu on MainMenu but could be useful
                    // for other effects.
                    if (curScene != null)
                    {
                        curScene.PostDialogRender(null);
                    }
                }
            }
            /*
            catch (Exception e)
            {
                if (e != null)
                {
                    throw e;
                }
            }
            */
        }   // end of Render()

        public static void SwitchToScene(BaseScene scene, Transition transition = Transition.Cut, Color color = default(Color), float transitionTime = 0, TwitchCurve.Shape shape = TwitchCurve.Shape.EaseOut, int frameDelay = 0, params object[] args)
        {

            if (curScene != null)
            {
#if DEBUG_SWITCH
                Debug.Print("switch : " + curScene.Name + " -> " + scene.Name);
#endif
            }

            Debug.Assert(curScene != scene, "Why are we trying to switch to the already active scene?");

            if (frameDelay > 0)
            {
                // Cache params for however many frames before making the switch.
                SceneParams sp = new SceneParams();
                sp.FrameDelay = frameDelay;
                sp.PendingScene = scene;
                sp.Arguments = args;
                sp.CurTransition = transition;
                sp.FadeColor = color;
                sp.FadeTime = transitionTime;
                sp.FadeShape = shape;

                delayedSwitches.Add(sp);
            }
            else
            {
                if (curScene != scene)
                {
                    pendingScene = scene;
                    arguments = args;
                    curTransition = transition;
                    fadeColor = color;
                    fadeTime = transitionTime;
                    fadeShape = shape;
                }
            }
        }   // end of SwitchToScene()

        public static void SwitchToScene(string name, Transition transition = Transition.Cut, Color color = default(Color), float transitionTime = 0, TwitchCurve.Shape shape = TwitchCurve.Shape.EaseOut, int frameDelay = 0, params object[] args)
        {
            BaseScene scene;
            if (nameDict.TryGetValue(name, out scene))
            {
                SwitchToScene(scene, transition:transition, color: color, transitionTime: transitionTime, shape: shape, frameDelay: frameDelay, args: args);
            }
            else
            {
                Debug.Assert(false, "Scene not found in dictionary: " + name);
            }
        }   // end of SwitchToScene()

        public static void RegisterScene(BaseScene scene)
        {
#if DEBUG
            if (nameDict.ContainsKey(scene.Name))
            {
                Debug.Assert(false, "Can't have multiple scenes with the same name.");
            }
#endif

            nameDict.Add(scene.Name, scene);
        }   // end of Register()

        public static void UnregisterScene(BaseScene scene)
        {
            Debug.Assert(false, "E_NOT_IMPL");
        }   // end of Register()

        /// <summary>
        /// Gets a scene from the SceneManager based on its name.
        /// Returns null on error but should not be used this way.
        /// Asserts in Debug.
        /// </summary>
        /// <param name="name"></param>
        /// <returns></returns>
        public static BaseScene GetSceneFromName(string name)
        {
            BaseScene scene;
            nameDict.TryGetValue(name, out scene);

            Debug.Assert(scene != null, "Why?");

            return scene;
        }   // end of GetSceneFromName() 

        #endregion

        #region Internal

        /// <summary>
        /// One-time initialization.  Loads graphics content for scenes.
        /// </summary>
        public static void Init()
        {
            if (!initialized)
            {
                if (pendingScene != null)
                {
                    pendingScene.LoadContent();

                    // TODO Think about a separate task for each scene.  This
                    // would us to check individual scenes before switching to
                    // them rather than waiting for all to be done.  Worth it???
                    
                    // Start background thread loading the rest of the scenes.
                    loadGraphicsTask = Task.Factory.StartNew(() => LoadGraphicsAllScenes(pendingScene));

                    initialized = true;
                }

                PCTouchInput.ProcessTouches = ProcessTouchHits;
            }
        }   // end of Init()

        /// <summary>
        /// Loads content for all scenes.  Note this results in the first scene
        /// being loaded twice unless passed in as ignore.
        /// </summary>
        /// <param name="ignore">Scene, if any, to be ignored.</param>
        static void LoadGraphicsAllScenes(BaseScene ignore)
        {
            foreach (KeyValuePair<string, BaseScene> kvp in nameDict)
            {
                if (kvp.Value != ignore)
                {
                    kvp.Value.LoadContent();
                }
            }
        }

        #endregion

        #region Internal

        /// <summary>
        /// Do hit testing for mouse.  This allows the hover 
        /// state to be set on the appropriate widget.
        /// </summary>
        static void ProcessMouseHits()
        {
            KoiLibrary.InputEventManager.MouseHitObject = null;

            // Only do mouse testing if mouse is last touched.  This prevents us
            // looking at the mouse position when it was set by promoting a touch input.
            if (KoiLibrary.LastTouchedDeviceIsMouse)
            {
                // Give DialogManager first shot at mouse input.
                DialogManagerX.ProcessMouseHits();

                // Only test local objects if DialogManager didn't get a hit AND there is no modal dialog active.
                if(KoiLibrary.InputEventManager.MouseHitObject == null && !DialogManagerX.ModalDialogIsActive && curScene != null)
                {
                    // Find the object, if any, under the mouse.
                    // Only override the current value if we hit something.
                    Vector2 mouseHit = LowLevelMouseInput.Position.ToVector2();
                    InputEventHandler hitObject = curScene.HitTest(mouseHit);
                    KoiLibrary.InputEventManager.MouseHitObject = hitObject;
                }
            }
        }   // end of ProcessMouseHits()

        /// <summary>
        /// Callback that is given to PCTouchInput and allows us
        /// to tweek the inputs before the events are generated.
        /// In this case we're doing hit testing to decide which 
        /// widget to offer a tap to first.  It also allows the 
        /// widget to know whether the touch was over itself.
        /// </summary>
        static void ProcessTouchHits(List<TouchSample> sampleList)
        {
            // Associate touches with widgets.
            KoiLibrary.InputEventManager.TouchHitObject = null;
            if (sampleList.Count > 0)
            {
                // Give DialogManager first shot at touch input.
                DialogManagerX.ProcessTouchHits(sampleList);

                // Only test local objects if DialogManager processing set TouchHitObject.
                // We need to test curScene here since user may create touch inputs
                // before the first scene is activated.
                if (KoiLibrary.InputEventManager.TouchHitObject == null && curScene != null)
                {
                    // Do hit testing with first touch only since they'll
                    // all be at the same position.
                    KoiLibrary.InputEventManager.TouchHitObject = curScene.HitTest(sampleList[0].Position);
                    // If widget is found, assign it as the hit object for all the touches.
                    foreach (TouchSample touchSample in sampleList)
                    {
                        touchSample.HitObject = KoiLibrary.InputEventManager.TouchHitObject;
                    }

                    /*
                    Debug.Print("\nframe : " + Time.FrameCounter.ToString());
                    foreach (TouchSample touchSample in sampleList)
                    {
                        InputEventHandler ieh = touchSample.HitObject;
                        if (ieh == null)
                        {
                            Debug.Print("    hit null");
                        }
                        else
                        {
                            Debug.Print("    num : " + ieh.UniqueNum.ToString() + "  " + touchSample.State.ToString());
                        }
                    }
                    */
                }
            }   // end of if we have touches.

        }   // end of ProcessTouchHits()

        #endregion

    }   // end of class SceneManager

}   // end of namespace KoiX.Managers
