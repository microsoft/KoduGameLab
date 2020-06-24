#define CAMERA_GHOSTING

using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Xml.Serialization;

using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Audio;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using Microsoft.Xna.Framework.Storage;

using KoiX;
using KoiX.Input;
using KoiX.Managers;
using KoiX.Scenes;
using KoiX.Text;
using KoiX.UI.Dialogs;

using Boku.Base;
using Boku.Fx;
using Boku.SimWorld;
using Boku.SimWorld.Path;
using Boku.SimWorld.Terra;
using Boku.SimWorld.Collision;
using Boku.Common;
using Boku.Common.Gesture;
using Boku.Common.ParticleSystem;
using Boku.Common.Xml;
using Boku.Common.Sharing;
using Boku.Programming;
using Boku.UI;
using Boku.UI2D;
using Boku.Input;
using Boku.Audio;
using Boku.Animatics;
#if !NETFX_CORE
    using TouchHook;
#endif

namespace Boku
{
#if NETFX_CORE
    // Faked up enum just ot get things to compile.  Values don't
    // matter since we're not trying to actually set presence.
    public enum GamerPresenceMode
    {
        FreePlay,
        EditingLevel,
        FoundSecret,
        StuckOnAHardBit,
        AtMenu,
        LookingForGames,
    }
#endif

    /// <summary>
    /// This is the running Sim and editors.
    /// </summary>
    public partial class InGame : GameObject, INeedsDeviceReset
    {
        public class Shared : INeedsDeviceReset
        {
            #region Members

            public SmoothCamera camera = null;
            public Vector3 cursorPosition;          // Used in all modes.  This is what the camera is looking at.

            public float editCameraRotation = 0.0f;

            public string runTimeLightRig;

            public Vector2 editBrushPosition = new Vector2();
            public Vector2 editBrushStart = new Vector2();
            public bool editBrushMoved = false;     // Did the position change significantly?
            public bool editBrushAllowedForTouch = false; //allow edit brush changes to apply?  only true when the user is actively moving the cursor over non-UI sections of the screen
            public float editBrushRadius = 5.0f;
            public int editBrushTextureIndex = 0;   // The texture we're painting with or the heightmap mode (raise, lower, smooth, roughen)
            public bool editBrushSizeActive = true;

            public ToolBar.TouchControls.BrushActionIDs currentTouchAction = ToolBar.TouchControls.BrushActionIDs.NUMBER_OF_Buttons;
            public ToolBar.TouchControls.BrushActionIDs previousGoodTouchAction = ToolBar.TouchControls.BrushActionIDs.NUMBER_OF_Buttons;
            
            public Vector2 dragTerrainStartPosition;

            public int curObjectColor = ColorPalette.GetIndexFromColor(Classification.Colors.White);    // The current color as displayed on our color palette.

            public bool heightMapModified = false;          // Booleans used to determine if we need to save out a user

            public ParticleSystemManager particleSystemManager = new ParticleSystemManager();
            public Compass compass = new Compass();

            public bool raisedWayPoint = false;

            public DustEmitter dustEmitter = null;  // Used for create trails of dust when objects are dragged.

            public ShowBudget budgetHUD = new ShowBudget();

            public ToolMenu toolMenu = new ToolMenu();
            public EditWorldParameters editWorldParameters = new EditWorldParameters();
            public EditObjectParameters editObjectParameters = new EditObjectParameters();
            public WayPointEdit editWayPoint = new WayPointEdit();

            public ProgrammingHelpCard programmingHelpCard = null;
            public AddItemHelpCard addItemHelpCard = null;
            public TextEditor textEditor = null;         // Editor for 'say' verb and 'said' filter.
            public TextLineDialog textLineDialog = null;         // Editor for single line text.

            public MicrobitPatternEditor microbitPatternEditor = null;

            public ModularMessageDialog tooManyLightsMessage = null;

            public bool renderWorldAsThumbnail = false; // When this is set to true instead of rendering the world as normal
                                                        // we instead just render the blurred, thumbnail image.  This is 
                                                        // useful for edit modes where we don't especially need to see the world
                                                        // but stil want to have some impression of it.  This is also generally
                                                        // much faster than rendering the world live which means that the UI 
                                                        // interaction with the edit mode is better.
            public bool refreshThumbnail = false;

            // Values which are set on the level after scanning through all the
            // character programs.  These are used to disable default mouse/camera
            // interactions when the user has programmed the mouse explicitly.
            public bool programUsesMouseInput = false;
            public bool programUsesLeftMouse = false;
            public bool programUsesRightMouse = false;
            public bool programUsesMouseHover = false;

            /// <summary>
            /// In Edit Mode when snapToGrid is true the cursor is snapped to a grid
            /// which matches the block size in the terrain.
            /// </summary>
            private bool snapToGrid = false;

            #endregion

            #region Accessors

            private Vector3 snapError = Vector3.Zero;

            public Vector3 CursorPosition
            {
                get
                {
                    Vector3 pos = cursorPosition;

                    if (InGame.inGame.SnapToGrid)
                    {
                        pos -= snapError;
                        Vector3 prevPos = pos;

                        pos = InGame.SnapPosition(pos);

                        snapError = pos - prevPos;
                    }

                    return pos;
                }
                set
                {
                    cursorPosition = value;
                }
            }

            /// <summary>
            /// Get the ToolMenu.
            /// </summary>
            public ToolMenu ToolMenu
            {
                get { return toolMenu; }
            }

            public ShowBudget BudgetHUD
            {
                get { return budgetHUD; }
            }

            public bool SnapToGrid
            {
                get { return snapToGrid && InGame.inGame.CurrentUpdateMode != UpdateMode.RunSim; }
                set { snapToGrid = value; }
            }

            #endregion

            #region Public

            // c'tor
            public Shared()
            {
                programmingHelpCard = new ProgrammingHelpCard();
                addItemHelpCard = new AddItemHelpCard();
                textEditor = new TextEditor();
                textLineDialog = new TextLineDialog();
                microbitPatternEditor = new MicrobitPatternEditor();

                dustEmitter = new DustEmitter(particleSystemManager);
                dustEmitter.AddToManager();
                dustEmitter.Active = true;
                dustEmitter.Emitting = false;
                dustEmitter.PositionJitter = 0.2f;
                dustEmitter.EmissionRate = 1.25f;
                dustEmitter.LinearEmission = true;
                dustEmitter.StartRadius = 0.45f;
                dustEmitter.EndRadius = 2.5f;
                dustEmitter.StartAlpha = 0.4f;
                dustEmitter.EndAlpha = 0.0f;
                dustEmitter.MinLifetime = 0.5f;       // Particle lifetime.
                dustEmitter.MaxLifetime = 2.0f;

                ModularMessageDialog.ButtonHandler handlerA = delegate(ModularMessageDialog dialog)
                {
                    // Deactivate dialog.
                    dialog.Deactivate();
                };
                tooManyLightsMessage = new ModularMessageDialog(Strings.Localize("tools.tooManyLights"),
                                                                handlerA, Strings.Localize("textDialog.continue"),
                                                                null, null,
                                                                null, null,
                                                                null, null
                                                                );
            }


            /// <summary>
            /// Tries to keep the camera from penetrating the ground.
            /// </summary>
            public void KeepCameraAboveGround()
            {
                // If in first person mode, let the bot's terrain collision handle this.
                if (CameraInfo.FirstPersonActive)
                {
                    return;
                }

                // Sample the terrain altitudes around the bounding sphere.
                Vector3 center = camera.BoundingSphere.Center;
                float radius = 1.5f * camera.BoundingSphere.Radius;
                float altitude = Terrain.GetTerrainAndPathHeight(center);
                altitude = Math.Max(altitude, Terrain.GetTerrainAndPathHeight(center + new Vector3(radius, 0.0f, 0.0f)));
                altitude = Math.Max(altitude, Terrain.GetTerrainAndPathHeight(center + new Vector3(-radius, 0.0f, 0.0f)));
                altitude = Math.Max(altitude, Terrain.GetTerrainAndPathHeight(center + new Vector3(0.0f, radius, 0.0f)));
                altitude = Math.Max(altitude, Terrain.GetTerrainAndPathHeight(center + new Vector3(0.0f, -radius, 0.0f)));
                radius *= 0.7071f;
                altitude = Math.Max(altitude, Terrain.GetTerrainAndPathHeight(center + new Vector3(radius, radius, 0.0f)));
                altitude = Math.Max(altitude, Terrain.GetTerrainAndPathHeight(center + new Vector3(radius, -radius, 0.0f)));
                altitude = Math.Max(altitude, Terrain.GetTerrainAndPathHeight(center + new Vector3(-radius, -radius, 0.0f)));
                altitude = Math.Max(altitude, Terrain.GetTerrainAndPathHeight(center + new Vector3(-radius, radius, 0.0f)));
                
                // Use samples to calc height above the ground.
                float height = center.Z - camera.BoundingSphere.Radius - altitude;

                float cameraMinHeight = 0.1f;   // We want the lowest part of the camera to be at least this high.
                if (height < cameraMinHeight)
                {
                    camera.BumpedZ = cameraMinHeight - height;
                }
            }   // end of InGame.Shared KeepCameraAboveGround()

            public double GetLevelPlaySeconds()
            {
                return Time.GameTimeTotalSeconds - InGame.inGame.startTime - InGame.inGame.totalPauseTime;
            }

            public double GetLevelLoadedSeconds()
            {
                return Time.GameTimeTotalSeconds - InGame.inGame.loadTime - InGame.inGame.totalLoadedPauseTime;
            }

            public void ResetLevelPlaySeconds()
            {
#if !NETFX_CORE
                //Debug.Print("-->played seconds on 'ResetLevel'.  Total play seconds: " + InGame.inGame.shared.GetLevelPlaySeconds());
                //Debug.Print("*->loaded seconds on 'ResetLevel'.  Total loaded seconds: " + InGame.inGame.shared.GetLevelLoadedSeconds());
#endif
                
                InGame.inGame.startTime = Time.GameTimeTotalSeconds;
                InGame.inGame.pauseTime = InGame.inGame.startTime;
                InGame.inGame.totalPauseTime = 0.0;
            }

            #endregion

            #region Internal

            public void LoadContent(bool immediate)
            {
                BokuGame.Load(compass, immediate);
                BokuGame.Load(particleSystemManager, immediate);
                BokuGame.Load(toolMenu, immediate);
                BokuGame.Load(editWorldParameters, immediate);
                BokuGame.Load(editObjectParameters, immediate);

                BokuGame.Load(programmingHelpCard, immediate);
                BokuGame.Load(addItemHelpCard, immediate);
                BokuGame.Load(textEditor, immediate);
                BokuGame.Load(textLineDialog, immediate);
                BokuGame.Load(microbitPatternEditor, immediate);

            }   // end of InGame Shared LoadContent()

            public void InitDeviceResources(GraphicsDevice device)
            {
                programmingHelpCard.InitDeviceResources(device);
                addItemHelpCard.InitDeviceResources(device);
                textEditor.InitDeviceResources(device);
                textLineDialog.InitDeviceResources(device);
                microbitPatternEditor.InitDeviceResources(device);
                editObjectParameters.InitDeviceResources(device);
                editWorldParameters.InitDeviceResources(device);
            }

            public void UnloadContent()
            {
                BokuGame.Unload(compass);
                BokuGame.Unload(particleSystemManager);
                BokuGame.Unload(toolMenu);
                BokuGame.Unload(editWorldParameters);
                BokuGame.Unload(editObjectParameters);

                BokuGame.Unload(programmingHelpCard);
                BokuGame.Unload(addItemHelpCard);
                BokuGame.Unload(textEditor);
                BokuGame.Unload(textLineDialog);
                BokuGame.Unload(microbitPatternEditor);
            }   // end of InGame Shared UnloadContent()

            /// <summary>
            /// Recreate render targets
            /// </summary>
            /// <param name="graphics"></param>
            public void DeviceReset(GraphicsDevice device)
            {
                BokuGame.DeviceReset(compass, device);
                BokuGame.DeviceReset(particleSystemManager, device);
                BokuGame.DeviceReset(toolMenu, device);
                BokuGame.DeviceReset(editWorldParameters, device);
                BokuGame.DeviceReset(editObjectParameters, device);

                BokuGame.DeviceReset(programmingHelpCard, device);
                BokuGame.DeviceReset(addItemHelpCard, device);
                BokuGame.DeviceReset(textEditor, device);
                BokuGame.DeviceReset(textLineDialog, device);
                BokuGame.DeviceReset(microbitPatternEditor, device);
            }

            #endregion

        }   // end of class Shared

        //
        //
        //  Note:   The UpdateObjects have been moved into their own
        //          files.  See the InGame*.cs files.
        //
        //

        protected class RenderObj : RenderObject, INeedsDeviceReset
        {
            private Shared shared = null;
            public List<RenderObject> renderList = null; // Children's render list.

            private ShadowCamera shadowCamera = null;

            private RenderTarget2D effectsRenderTarget = null;
            private RenderTarget2D distortRenderTarget0 = null;
            private RenderTarget2D distortRenderTarget1 = null;

            private RenderTarget2D shadowFullRenderTarget = null;
            private RenderTarget2D shadowSmallRenderTarget0 = null;
            private RenderTarget2D shadowSmallRenderTarget1 = null;
            private Texture2D shadowTexture = null;

            private RenderTarget2D fullRenderTarget0 = null;
            private RenderTarget2D fullRenderTarget1 = null;
            private RenderTarget2D smallRenderTarget0 = null;
            private RenderTarget2D smallEffectThumb = null;
            private RenderTarget2D smallNoEffect = null;
            private RenderTarget2D thumbRenderTarget = null;

            private RenderTarget2D bloomRenderTarget = null;
            private RenderTarget2D glowRenderTarget = null;
            private RenderTarget2D tinyRenderTarget0 = null;

            private Texture2D thumbNail = null;

            private CopyFilter copyFilter = null;
            private Box4x4BlurFilter boxFilter = null;
            private GaussianFilter gaussianFilter = null;
            private ThresholdFilter thresholdFilter = null;
            private DOF_Filter dofFilter = null;

            private SkyBox skybox = null;

            private const float kTouchCursorInterpSpeed = 10.0f;
            private Vector2 touchCursorRenderPos = Vector2.Zero;
            private bool bTouchCursorRendered = false;

            public RenderTarget2D EffectsRenderTarget
            {
                get { return effectsRenderTarget; }
            }

            public Texture2D ThumbNail
            {
                get 
                {
                    if (thumbNail == null)
                        try
                        {
                            //InGame.SetRenderTarget(thumbRenderTarget);
                            //boxFilter.Render(fullRenderTarget1);
                            //InGame.RestoreRenderTarget();
                            thumbNail = thumbRenderTarget;
                        }
                        catch { }
                    return thumbNail;
                }
                set
                {
                    thumbNail = value;
                }
            }
            public Texture2D SmallThumbNail
            {
                get
                {
                    return smallEffectThumb;
                }
            }

            public Texture2D FullRenderTarget0
            {
                get { return fullRenderTarget0; }
            }
            public Texture2D FullRenderTarget1
            {
                get { return fullRenderTarget1; }
            }

            public ShadowCamera ShadowCamera
            {
                get { return shadowCamera; }
            }

            public RenderObj(ref Shared shared)
            {
                this.shared = shared;

                shadowCamera = new ShadowCamera();

                renderList = new List<RenderObject>();
            }   // end of RenderObj c'tor

            public void RefreshSmallThumbnail(RenderTarget2D src, RenderTarget2D dst)
            {
                RestoreViewportToFull();

                SpriteBatch batch = KoiLibrary.SpriteBatch;

                Microsoft.Xna.Framework.Rectangle dstRect = new Microsoft.Xna.Framework.Rectangle(0, 0, dst.Width, dst.Height);
                // We need to use the current ScreenSize to set the srcRect.  This accounts
                // for the fact that our window may be smaller that the RT we're rendering into.
                Microsoft.Xna.Framework.Rectangle srcRect = new Microsoft.Xna.Framework.Rectangle(0, 0, (int)(BokuGame.ScreenSize.X + BokuGame.ScreenPosition.X), (int)(BokuGame.ScreenSize.Y + BokuGame.ScreenPosition.Y));

                InGame.SetRenderTarget(dst);
                batch.Begin(SpriteSortMode.Deferred, BlendState.Opaque);
                {
                    // Note: we should be doing a box filter here but the filter code doesn't support partial images.
                    batch.Draw(src, dstRect, srcRect, Color.White);
                }
                batch.End();

                // Just smooth the last frame for the Mini-Hub's use.
                InGame.SetRenderTarget(smallRenderTarget0);
                gaussianFilter.RenderHorizontal(dst);

                InGame.SetRenderTarget(dst);
                gaussianFilter.RenderVertical(smallRenderTarget0);

                // Restore the original backbuffer (original rendertarget).
                InGame.RestoreRenderTarget();
            }
            public void RefreshSaveThumbnail(RenderTarget2D src, RenderTarget2D dst)
            {
                InGame.SetRenderTarget(dst);
                try
                {
                    boxFilter.Render(src);
                }
                catch
                {
                    // If we're here it's because we want to display the blurred
                    // image from in-game and we've just come out of a reset so
                    // there is no image to blur.  This still looks ok though...
                }

                // Restore the original backbuffer (original rendertarget).
                InGame.RestoreRenderTarget();
            }
            protected void DoDistortion(Camera camera)
            {
                if (DistortionManager.EnabledSM3)
                {
                    FBXModel.LockLowLOD = FBXModel.LockLOD.kLow;

                    GraphicsDevice device = KoiLibrary.GraphicsDevice;

                    // Set the rendertarget(s)
                    InGame.SetRenderTargets(distortRenderTarget0, distortRenderTarget1);

                    // Clear the z-buffer.
                    InGame.Clear(Color.Transparent);

                    SetViewportToScreen();

                    DistortionManager.RenderSM3(camera, effectsRenderTarget);

                    // Restore the original backbuffer.
                    InGame.RestoreRenderTarget();

                    FBXModel.LockLowLOD = FBXModel.LockLOD.kAny;
                }

            }

            protected void RenderObjects(Camera camera)
            {
                PushBatching(true);
                for (int i = 0; i < renderList.Count; i++)
                {
                    RenderObject obj = (RenderObject)renderList[i];
                    obj.Render(camera);
                }

                PopBatching(false);
            }

            private List<FBXModel.RenderPack> renderBatch = new List<FBXModel.RenderPack>(100);
            public void AddBatch(FBXModel.RenderPack pack)
            {
                renderBatch.Add(pack);
            }
            public bool PushBatching(bool on)
            {
                return FBXModel.PushBatching(on);
            }
            public void PopBatching(bool on)
            {
                if (FBXModel.Batching)
                {
                    FBXModel.RenderBatches(renderBatch);
                }
                FBXModel.PopBatching(on);
            }

            /// <summary>
            /// Renders the shadow texture for the terrain.  Note that objects are rendered
            /// in white over a black background.
            /// </summary>
            private void RenderShadowTexture()
            {
                FBXModel.LockLowLOD = FBXModel.LockLOD.kLow;
                GraphicsDevice device = KoiLibrary.GraphicsDevice;

                // Set the rendertarget(s)
                InGame.SetRenderTarget(shadowFullRenderTarget);

                // Clear the shadow rendertarget.
                InGame.Clear(Color.Transparent);

                // Set up the camera.
                shadowCamera.Locate(shared.camera);

                InGame.inGame.renderEffects = RenderEffect.ShadowPass;

                // Render the waypoints and roads.  Note that we don't 
                // render these shadows while terrain heights are being
                // changed.
                if (InGame.inGame.terrain.LastEdit != Time.FrameCounter)
                {
                    bool batching = InGame.inGame.PushBatching(true);
                    shared.editWayPoint.RenderWayPointSelection(shadowCamera);

                    WayPoint.Alpha = 1.0f;
                    if (InGame.inGame.CurrentUpdateMode == UpdateMode.RunSim)
                    {
                        WayPoint.RenderRoads(shadowCamera);
                    }
                    else
                    {
                        WayPoint.RenderRoads(shadowCamera);
                        WayPoint.RenderPaths(shadowCamera, true);
                    }
                    InGame.inGame.PopBatching(batching);
                }

                // Render all the objects.
                RenderObjects(shadowCamera);

                InGame.inGame.renderEffects = RenderEffect.Normal;

                // Filter down to small RT.
                InGame.SetRenderTarget(shadowSmallRenderTarget0);
                boxFilter.Render(shadowFullRenderTarget);

                // Do Gaussian blur.
                InGame.SetRenderTarget(shadowSmallRenderTarget1);
                gaussianFilter.RenderVertical(shadowSmallRenderTarget0);

                InGame.SetRenderTarget(shadowSmallRenderTarget0);
                gaussianFilter.RenderHorizontal(shadowSmallRenderTarget1);

                // Restore the original backbuffer (original rendertarget).
                InGame.RestoreRenderTarget();

                // This leaves the blurred shadow texture in shadowSmallRenderTarget0.
                shadowTexture = shadowSmallRenderTarget0;
                shadowCamera.ShadowTexture = shadowTexture;

                FBXModel.LockLowLOD = FBXModel.LockLOD.kAny;

            }   // end of RenderShadowTexture()

            private static bool doDumpRT = true;
            public override void Render(Camera camera)
            {
                // If the window size has changed, we need to reallocate the rendertargets.
                ReallocateRenderTargets();

                GraphicsDevice device = KoiLibrary.GraphicsDevice;

                if (doDumpRT)
                {
                    doDumpRT = false;
                    SharedX.DumpRT();
                }

                if (InGame.inGame.saveLevelDialog.Active)
                {
                    InGame.inGame.saveLevelDialog.Render(camera);
                    return;
                }

                InGame.inGame.Terrain.PostEditCheck(InGame.inGame.gameThingList);

                // Only render effects if user wants them.
                bool doEffects = BokuSettings.Settings.PostEffects;

                /*
                if (BokuGame.bokuGame.miniHub.PendingActive && !BokuGame.bokuGame.miniHub.Active)
                {
                    // Let the mini-hub know where the blurred in game image is.
                    BokuGame.bokuGame.miniHub.InGameImage = SmallThumbNail;
                    RefreshThumbnail = true;
                }
                */

                // Ensure the help overlay is up to date.
                HelpOverlay.RefreshTexture();

                // If one of the edit modes is active, instead of rendering the full scene just
                // fill the screen with the thumbnail ala what we do for the mini-hub.  This has 
                // a couple of advantages:  First, it decreases the visual clutter.  The
                // programming UI is already quite busy and this helps tone down what's behind it.
                // Second, if the current scene has quite a few objects the frame rate can be bad
                // which causes the UI experience to to bad.  Since the programming UI is the key
                // UI that the user interacts with we need it to be as fluid as possible.

                // TODO (scoy) This has gotten a bit out of hand.  Need to rethink the rendering
                // order of everything and simplify this.
                if (InGame.inGame.RenderWorldAsThumbnail && !InGame.RefreshThumbnail)
                {
                    // We want to render everything to a rendertarget first and the
                    // stretch/scale that rendertarget to fit the actualy screen.  This
                    // Also lets us support tutorial mode.
                    RenderTarget2D rt = SharedX.RenderTargetDepthStencil1280_720;
                    Vector2 screenSize = BokuGame.ScreenSize;
                    Vector2 rtSize = new Vector2(rt.Width, rt.Height);
                    ScreenSpaceQuad quad = ScreenSpaceQuad.GetInstance();

                    // The help cards render the background themselves since they need
                    // to shrink/crop the UI to fit the current resolution.
                    if (shared.programmingHelpCard.Active)
                    {
                        shared.programmingHelpCard.Render(camera);
                    }
                    else if (shared.addItemHelpCard.Active)
                    {
                        shared.addItemHelpCard.Render(camera);
                    }
                    else if (shared.textEditor.Active)
                    {
                        shared.textEditor.Render(camera);
                    }
                    else if (shared.microbitPatternEditor.Active)
                    {
                        shared.microbitPatternEditor.Render();
                    }
                    else
                    {

                        //
                        // If we're using the thumbnail as a background, render it here.  Since it's just a blurred
                        // version for context feel free to stretch to fit screen.
                        //
                        Texture2D smallThumb = SmallThumbNail;
                        if (InGame.inGame.RenderWorldAsThumbnail && !InGame.RefreshThumbnail)
                        {
                            // Always need to clear just to be sure z-buffer is reset.  Note this doesn't 
                            // seem to be needed on desktop build but is required on WinRT.
                            InGame.Clear(new Color(20, 20, 20));

                            // The thumbnail may be invalid if we have a device reset while it's being used.
                            // In this case, just start with a black screen.
                            if (!smallThumb.GraphicsDevice.IsDisposed)
                            {
                                SpriteBatch batch = KoiLibrary.SpriteBatch;
                                batch.Begin(SpriteSortMode.Deferred, BlendState.Opaque);
                                {
                                    // Stretch thumbnail across whole screen.  Don't worry about distorting it
                                    // since it's blurred anyway.
                                    Microsoft.Xna.Framework.Rectangle dstRect = new Microsoft.Xna.Framework.Rectangle(0, 0, device.Viewport.Width, device.Viewport.Height);

                                    Microsoft.Xna.Framework.Rectangle srcRect;
                                    if (BokuGame.ScreenPosition.Y > 0)
                                    {
                                        // Set srcRect to ignore part of image at top of screen.
                                        int y = (int)(BokuGame.ScreenPosition.Y * smallThumb.Height / (float)BokuGame.ScreenSize.Y);
                                        srcRect = new Microsoft.Xna.Framework.Rectangle(0, y, smallThumb.Width, smallThumb.Height - y);
                                    }
                                    else
                                    {
                                        // Set srcRect to cover full thumbnail.
                                        srcRect = new Microsoft.Xna.Framework.Rectangle(0, 0, smallThumb.Width, smallThumb.Height);
                                    }
                                    batch.Draw(smallThumb, dstRect, srcRect, Color.White);
                                }
                                batch.End();
                            }
                        }   // If we need the thumbnail as background.


                        //
                        // Render directly to backbuffer rather than to RT.
                        //
                        SetViewportToScreen();

                        // These are the parameter editing objects which, when activated also
                        // set shared.renderWorldAsThumbnail to true so this is the only place
                        // we need to consider rendering them.
                        // TODO (scoy) Track this down and fix.
                        // Note the try/catch clause is there because the UIGridTextListElement has
                        // something wrong with it that causes issues on device reset.  For some
                        // reason, after failing once everything works fine.
                        try
                        {
                            if (shared.editWorldParameters.Active)
                            {
                                shared.editWorldParameters.Render();
                            }
                            if (shared.editObjectParameters.Active)
                            {
                                shared.editObjectParameters.Render();
                            }
                        }
                        catch
                        {
                        }

                        /*
                        // Copy the rendered scene with the UI to the rendertarget.
                        // Note, since the UI tends to go top to bottom we always stretch/squish to fit vertically.
                        {
                            SetViewportToScreen();

                            SpriteBatch batch = KoiLibrary.SpriteBatch;
                            batch.Begin(SpriteSortMode.Deferred, BlendState.NonPremultiplied);
                            {
                                float scale = BokuGame.ScreenSize.Y / (float)rt.Height;
                                Microsoft.Xna.Framework.Rectangle dstRect = new Microsoft.Xna.Framework.Rectangle((int)((BokuGame.ScreenSize.X - rt.Width * scale) / 2.0f), 0, (int)(rt.Width * scale), (int)(rt.Height * scale));
                                batch.Draw(rt, dstRect, Color.White);
                            }
                            batch.End();
                        }
                        */
                        /*
                        float rtAspect = rtSize.X / rtSize.Y;
                        Vector2 position = Vector2.Zero;
                        Vector2 newSize = screenSize;

                        newSize.X = rtAspect * newSize.Y;
                        position.X = (screenSize.X - newSize.X) / 2.0f;

                        quad.Render(rt, position + BokuGame.ScreenPosition, newSize, @"TexturedRegularAlpha");
                        */
                    }

                }
                else
                {
                    // else not rendering as thumbnail.
                    bool skipFrame = Time.SkipThisFrame;

                    try
                    {
                        // If we think we want to skip this frame 
                        // see if there's a valid texture to grab.
                        if (skipFrame)
                        {
                            Texture2D t = fullRenderTarget0;
                        }
                    }
                    catch
                    {
                        // If not, don't try and skip this frame.
                        skipFrame = false;
                    }

                    if (!skipFrame)
                    {

                        RenderShadowTexture();

                        // Set the rendertarget(s)
                        InGame.SetRenderTarget(fullRenderTarget0);

                        // Clear the z-buffer.
                        InGame.Clear(Color.Transparent);
                        //device.Clear(ClearOptions.DepthBuffer | ClearOptions.Stencil, Color.Red, 1.0f, 0);

                        // Set viewport to match screen for tutorial mode.
                        SetViewportToScreen();

                        // Update the camera with the new viewport info.
                        shared.camera.Resolution = new Point((int)BokuGame.ScreenSize.X, (int)BokuGame.ScreenSize.Y);
                        shared.camera.Update();

                        // Render the skybox.
                        skybox.Render(shared.camera, false);

                        ScreenSpaceQuad ss = ScreenSpaceQuad.GetInstance();

                        //
                        //
                        //  Start by rendering opaque objects.
                        //
                        //
                        
                        bool editingTerrain = InGame.inGame.EditingTerrain
                                            || (InGame.inGame.CurrentUpdateMode == UpdateMode.TouchEdit && InGame.inGame.touchEditUpdateObj.EditingTerrain)
                                            || (InGame.inGame.CurrentUpdateMode == UpdateMode.MouseEdit && InGame.inGame.mouseEditUpdateObj.EditingTerrain)
                                            || (InGame.inGame.CurrentUpdateMode == UpdateMode.MouseEdit && EditWorldScene.CurrentToolMode == EditWorldScene.ToolMode.EraseObjects && !InGame.inGame.mouseEditUpdateObj.ToolBar.Hovering);
                        
                        bool renderBrush = ToolBarDialog.IsActive && ToolBarDialog.NeedsBrush;

                        // If the alt key is pressed, then don't display the terrain brushes since we're in eyedropper mode.
                        // It feels like there should be a better way to do this...
                        if (InGame.inGame.CurrentUpdateMode == UpdateMode.MouseEdit && EditWorldScene.CurrentToolMode == EditWorldScene.ToolMode.TerrainPaint && KeyboardInputX.AltIsPressed)
                        {
                            renderBrush = false;
                        }

                        // Depending on mode, render terrain with either a visible brush and no shadows or with shadows.
                        if (renderBrush)
                        {
                            // Render the terrain with brush.
                            InGame.inGame.terrain.RenderEditMode(
                                shared.camera,
                                shadowTexture,
                                shared.editBrushPosition,
                                shared.editBrushStart,
                                shared.editBrushRadius);
                        }
                        else
                        {
                            // Render the terrain.
                            InGame.inGame.terrain.Render(shared.camera, false, false);
                        }

                        // Don't render objects if editing terrain texture or heightmap.
                        // Hack here - we know the terrain tree is disabled while the terrain is
                        // being actively edited, but will be re-enabled between brush strokes.
                        // So we only render the world objects when the tree is enabled.
                        // Render the list of objects using our local camera.
                        RenderObjects(shared.camera);

                        // TODO Remove the cursor from the game thing list.
                        // Render the cursor.

                        //
                        //
                        // Now render transparent objects.  Back to front sorting would be nice...
                        //
                        //

                        // Don't render waypoints if running or if in texture mode.
                        bool batching = InGame.inGame.PushBatching(true);
                        if (InGame.inGame.CurrentUpdateMode != UpdateMode.RunSim)
                        {
                            // Ghost waypoints if editing the heightmap or texture.
                            if (editingTerrain)
                            {
                                WayPoint.Alpha = 0.4f;
                            }
                            else
                            {
                                WayPoint.Alpha = 1.0f;
                            }

                            if (
                                (InGame.inGame.CurrentUpdateMode == UpdateMode.ToolBox && InGame.inGame.terrain.Editing)
                                || (InGame.inGame.CurrentUpdateMode == UpdateMode.MouseEdit && EditWorldScene.EditingTerrain)
                                || (InGame.inGame.CurrentUpdateMode == UpdateMode.TouchEdit && EditWorldScene.EditingTerrain)
                                )
                            {
                                // If editing the height map the path node positions may need
                                // to be recalculated.
                                if (InGame.inGame.terrain.LastEdit == Time.FrameCounter)
                                {
                                    WayPoint.RecalcWayPointNodeHeights();
                                }
                            }
                            else
                            {
                                // Only render roads if not editing height map since the cost
                                // of dynamically recreating the road geometry is a bit much
                                // for realtime response.
                                WayPoint.RenderRoads(shared.camera);
                            }

                            // Always render paths.
                            WayPoint.RenderPaths(shared.camera, false);
                        }
                        else
                        {
                            WayPoint.RenderRoads(shared.camera);
                            if (DebugPathFollow)
                            {
                                // Always render paths.
                                WayPoint.RenderPaths(shared.camera, false);
                            }
                        }

                        InGame.inGame.PopBatching(batching);

                        DistortionManager.RenderBloomSM2(shared.camera);
                        
                        // Render the water.
                        InGame.inGame.terrain.RenderWater(shared.camera, false);
                        Ripple.Render(shared.camera);

                        // ThoughtBalloons
                        ThoughtBalloonManager.Render(shared.camera, false);

                        // Health bars
                        HealthBarManager.Render(shared.camera);

                        // Debug show current kode page.
                        DebugShowCodePageManager.Render(shared.camera);

                        // Scoreboard && GuiButtons
                        if (InGame.inGame.CurrentUpdateMode == UpdateMode.RunSim)
                        {
                            Scoreboard.Render(shared.camera);

                            GUIButtonManager.Render();

                            TouchVirtualController.Render();
                        }

                        // Render any particles.
                        shared.particleSystemManager.Render(shared.camera);

                        shared.editWayPoint.RenderWayPointSelection(shared.camera);
                        
                        Fx.Luz.DebugDrawLuz(shared.camera);
                        FirstPersonEffectMgr.Render(shared.camera);
                        Shield.Render(shared.camera);

                        // Never display creatable lines in RunSim.
                        if (InGame.inGame.CurrentUpdateMode != UpdateMode.RunSim)
                        {
                            // Display lines for creatables/clones.
                            if (KoiLibrary.LastTouchedDeviceIsGamepad)
                            {
                                // GamePad input version.
                                if (InGame.inGame.editObjectUpdateObj.LastSelectedActor != null)
                                {
                                    InGame.inGame.editObjectUpdateObj.LastSelectedActor.DisplayCreatableLines(shared.camera);
                                }
                            }
                            else if (KoiLibrary.LastTouchedDeviceIsKeyboardMouse)
                            {
                                // Key/Mouse input version.
                                if (EditWorldScene.CurrentToolMode == EditWorldScene.ToolMode.EditObject)
                                {
                                    GameActor focus = InGame.inGame.mouseEditUpdateObj.ToolBox.EditObjectsToolInstance.FocusActor;
                                    if (focus != null)
                                    {
                                        focus.DisplayCreatableLines(shared.camera);
                                    }
                                }
                            }
                            else
                            {
                                //Touch input version
                                if (EditWorldScene.CurrentToolMode == EditWorldScene.ToolMode.EditObject)
                                {
                                    GameActor focus = InGame.inGame.touchEditUpdateObj.ToolBox.EditObjectsToolInstance.FocusActor;
                                    if (focus != null)
                                    {
                                        focus.DisplayCreatableLines(shared.camera);
                                    }
                                }
                            }

                            // Render anchor lines from bots straight down to the ground.  These provide
                            // a visual reference for where the user needs to move to select the bots.
                            // Only render if cursor is near bot.
                            // In KeyMouse mode the user can also click on the anchor point and drag the bot.
                            {
                                Vector3 cursorPos = Vector3.Zero;

                                if (KoiLibrary.LastTouchedDeviceIsGamepad)
                                {
                                    cursorPos = InGame.inGame.Cursor3D.Position;
                                }
                                else if (KoiLibrary.LastTouchedDeviceIsTouch)
                                {
                                    TouchEdit touchEdit = Boku.InGame.inGame.TouchEdit;
                                    HitInfo MouseTouchHitInfo = TouchEdit.MouseTouchHitInfo;
                                    cursorPos = MouseTouchHitInfo.TerrainPosition;
                                }
                                else
                                {
                                    MouseEdit mouseEdit = Boku.InGame.inGame.MouseEdit;
                                    HitInfo MouseTouchHitInfo = MouseEdit.MouseTouchHitInfo;

                                    cursorPos = MouseTouchHitInfo.TerrainPosition;
                                }

                                for (int i = 0; i < InGame.inGame.gameThingList.Count; i++)
                                {
                                    GameActor actor = InGame.inGame.gameThingList[i] as GameActor;
                                    if (actor != null)
                                    {
                                        Vector3 pos = actor.Movement.Position;
                                        Vector3 delta = pos - cursorPos;
                                        delta.Z = 0;
                                        float len = delta.Length();
                                        if (len < 10.0f)
                                        {
                                            float alpha = MathHelper.SmoothStep(1.0f, 0.0f, len / 10.0f);
                                            Vector3 anchor = pos;
                                            anchor.Z = 0;
                                            Utils.DrawLine(InGame.inGame.Camera, pos, anchor, new Vector4(0, 0, 0, alpha));
                                        }
                                    }
                                }
                            }
                        }   // end if not RunSim

                        GameActor.DisplayLinesOfPerception(shared.camera);

#if CAMERA_GHOSTING
                        InGame.inGame.RenderGhosts(shared.camera);
#endif // CAMERA_GHOSTING

                        // Render debug helpers...
                        /*
                        {
                            Ray ray = new Ray(shared.camera.From, shared.camera.ViewDir);
                            float dist = 0.0f;
                            bool hit = Terrain.VirtualMap.Intersect(ray, ref dist);
                            if (hit)
                            {
                                Vector3 hitPoint = ray.Position + dist * ray.Direction;
                                Utils.DrawAxis(shared.camera, hitPoint);
                                Vector3 normal = Terrain.GetNormal(hitPoint);
                                Utils.DrawLine(shared.camera, hitPoint, hitPoint + normal, Color.White);
                            }
                        }

                        // Debug test.  Uncomment this to see where Boku is looking.
                        //Terrain.RenderTestRays(shared.camera);
                        */




//#if DEBUG
//                        //draw the last positions (debug build only)

//                        //draw some cross hairs (with an offset) of were the user is touching the screen
//                        if (TouchInput.TouchCount > 0)
//                        {
//                            for (int i = 0; i < TouchInput.TouchCount; i++)
//                            {
//                                TouchContact touch = TouchInput.GetTouchContactByIndex(i);
//                                if (touch != null)
//                                {
//                                    Vector2 crossHairOffset = new Vector2(0.0f, -20.0f);
                                    
//                                    Utils.Draw2DCrossHairs(touch.startPosition + crossHairOffset, new Vector4(1.0f, 1.0f, 1.0f, 1.0f));
//                                    Utils.Draw2DCrossHairs(touch.previousPosition + crossHairOffset, new Vector4(0.0f, 0.0f, 1.0f, 1.0f));
//                                    Utils.Draw2DCrossHairs(touch.position + crossHairOffset, new Vector4(1.0f, 0.0f, 0.0f, 1.0f));
//                                }
//                            }
//                        }

//#endif

                        // Note that everything rendered below this section will
                        // not appear in the thumbnail or be affected by DOF.

                        //
                        //
                        // Render scene a second time for effects info.  Note that we don't need to clear the depth buffer.
                        //
                        //

                        if (doEffects)
                        {
                            // Set the rendertarget(s)
                            // Note this previously used  OffscreenDepthBuffer for depth so we need
                            // effectsRenderTarget to also have depth.
                            InGame.SetRenderTarget(effectsRenderTarget);

                            // Clear to get the same results as rendering the skybox w/ DepthPass true.
                            // TODO (scoy) The comment above mentions that we don't need to clear the depth buffer
                            // but this version of clear does clear the depth buffer.  Is that a problem???
                            
                            // TODO (scoy) Why clear to transparent red???
                            InGame.Clear(new Color(1, 0, 0, 0));

                            // Render the terrain.
                            InGame.inGame.terrain.Render(shared.camera, true, false);

                            bool wayPointBatching = InGame.inGame.PushBatching(true);
                            if (InGame.inGame.CurrentUpdateMode != UpdateMode.RunSim)
                            {
                                bool editingHeightMap = (InGame.inGame.CurrentUpdateMode == UpdateMode.ToolBox && InGame.inGame.terrain.Editing)
                                                        || (InGame.inGame.CurrentUpdateMode == UpdateMode.MouseEdit && InGame.inGame.mouseEditUpdateObj.EditingTerrain)
                                                        || (InGame.inGame.CurrentUpdateMode == UpdateMode.TouchEdit && InGame.inGame.touchEditUpdateObj.EditingTerrain);
                                if (!editingHeightMap)
                                {
                                    // Only render roads if not editing height map since the cost
                                    // of dynamically recreating the road geometry is a bit much
                                    // for realtime response.
                                    WayPoint.RenderRoads(shared.camera);

                                    WayPoint.RenderPaths(shared.camera, false);
                                }
                            }
                            else
                            {
                                WayPoint.RenderRoads(shared.camera);
                            }
                            InGame.inGame.PopBatching(wayPointBatching);

                            // Render the list of objects using our local camera.
                            InGame.inGame.renderEffects = RenderEffect.DepthPass;
                            RenderObjects(shared.camera);

                            // ThoughtBalloons
                            ThoughtBalloonManager.Render(shared.camera, false);

                            if (InGame.inGame.CurrentUpdateMode == UpdateMode.RunSim)
                            {
                                Scoreboard.RenderEffects(shared.camera);
                            }

                            /// This line renders the blur aspect of the shields. I'm not
                            /// really loving it right now. It works, but I'm not sure it
                            /// improves the look.
                            //Shield.Render(shared.camera);

                            // Render any particles.
                            // This may no longer be necessary, if we aren't blurring the skybox.
                            //shared.particleSystemManager.Render(shared.camera);

                            //
                            // Done rendering effects pass.
                            //

                            InGame.inGame.renderEffects = RenderEffect.Normal;

                            // Resolve the rendertarget(s).


                            // Restore the original backbuffer (original rendertarget).
                            InGame.RestoreRenderTarget();

                            DoDistortion(shared.camera);

                        }   // End of effects rendering.

                        if (doEffects)
                        {
                            RefreshSmallThumbnail(fullRenderTarget0, smallNoEffect);
                            Texture2D smallThumb = smallNoEffect;

                            if (BokuSettings.Settings.PostEffects)
                            {
                                // Filter for bloom RT.  We have to start with the full
                                // size image otherwise we end up with ugly artifacts.
                                InGame.SetRenderTarget(fullRenderTarget1);
                                thresholdFilter.Render(fullRenderTarget0, 5.0f);

                                InGame.SetRenderTarget(smallRenderTarget0);
                                boxFilter.Render(fullRenderTarget1);


                                InGame.SetRenderTarget(bloomRenderTarget);
                                boxFilter.Render(smallRenderTarget0);


                                InGame.SetRenderTarget(tinyRenderTarget0);
                                gaussianFilter.RenderHorizontal(bloomRenderTarget);


                                InGame.SetRenderTarget(bloomRenderTarget);
                                gaussianFilter.RenderVertical(tinyRenderTarget0);


                                InGame.RestoreRenderTarget();

                                // Now do the explicit glow (like for selection and the like).
                                InGame.SetRenderTarget(smallRenderTarget0);
                                Clear(Color.Transparent);

                                DistortionManager.RenderBloomSM3(shared.camera, effectsRenderTarget);

                                InGame.SetRenderTarget(glowRenderTarget);
                                boxFilter.Render(smallRenderTarget0);


                                InGame.SetRenderTarget(tinyRenderTarget0);
                                gaussianFilter.RenderHorizontal(glowRenderTarget);


                                InGame.SetRenderTarget(glowRenderTarget);
                                gaussianFilter.RenderVertical(tinyRenderTarget0);


                                InGame.RestoreRenderTarget();
                            }
                            else
                            {
                                // Restore the original backbuffer (original rendertarget).
                                InGame.RestoreRenderTarget();
                            }

                            RestoreViewportToFull();
                            InGame.Clear(Color.Black);      // TODO (scoy) not needed???

                            if (InGame.RefreshThumbnail)
                            {
                                SetRenderTarget(fullRenderTarget1);
                            }

                            // The composition needs to be done with full VP.
                            RestoreViewportToFull();

                            // Finally, pull it all together.
                            dofFilter.Render(fullRenderTarget0,
                                smallThumb,
                                bloomRenderTarget,
                                glowRenderTarget,
                                effectsRenderTarget,
                                distortRenderTarget0,
                                distortRenderTarget1
                                );



                            
                            /*
                            // Debug...
                            //InGame.Clear(Color.Coral);

                            //SetViewportToScreen();
                            RestoreViewportToFull();

                            SpriteBatch batch = new SpriteBatch(device);
                            batch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend);
                            {
                                //batch.Draw(fullRenderTarget0, Vector2.Zero, new Color(1, 1, 1, 0.5f));

                                Microsoft.Xna.Framework.Rectangle rect = new Microsoft.Xna.Framework.Rectangle(20, 200, 400, 260);
                                batch.Draw(glowRenderTarget, rect, new Color(1, 1, 1, 1.0f));
                            }
                            batch.End();
                            */                            


                            if (InGame.RefreshThumbnail)
                            {
                                InGame.inGame.renderObj.RefreshSmallThumbnail(fullRenderTarget1, smallEffectThumb);
                                InGame.inGame.renderObj.RefreshSaveThumbnail(fullRenderTarget1, thumbRenderTarget);

                                InGame.RestoreRenderTarget();
                                SetViewportToScreen();

                                copyFilter.Render(fullRenderTarget1);

                                InGame.RefreshThumbnail = false;
                            }

                            // For debugging...
                            //copyFilter.Render(effectsRenderTarget.Texture);
                            //copyFilter.Render(shadowRenderTarget.Texture);
                        }
                        else
                        {
                            if (InGame.RefreshThumbnail)
                            {
                                InGame.inGame.renderObj.RefreshSmallThumbnail(fullRenderTarget0, smallEffectThumb);
                                InGame.inGame.renderObj.RefreshSaveThumbnail(fullRenderTarget0, thumbRenderTarget);
                                InGame.RefreshThumbnail = false;
                            }

                            // Restore the original backbuffer (original rendertarget).
                            InGame.RestoreRenderTarget();

                            // Not rendering effects, so just do a copy.
                            SpriteBatch batch = KoiLibrary.SpriteBatch;
                            batch.Begin(SpriteSortMode.Deferred, BlendState.Opaque);
                            {
                                // Note, fullRenderTarget0 may be larger than the current screen so only take the part we care about. 
                                Microsoft.Xna.Framework.Rectangle dstRect = new Microsoft.Xna.Framework.Rectangle((int)BokuGame.ScreenPosition.X, (int)BokuGame.ScreenPosition.Y, (int)BokuGame.ScreenSize.X, (int)BokuGame.ScreenSize.Y);
                                Microsoft.Xna.Framework.Rectangle srcRect = dstRect;
                                batch.Draw(fullRenderTarget0, dstRect, srcRect, Color.White);
                            }
                            batch.End();
                        }
                    }
                    else
                    {
                        // We're in frame skipping mode so just use the last rendering of the world.
                        SpriteBatch batch = KoiLibrary.SpriteBatch;
                        batch.Begin(SpriteSortMode.Deferred, BlendState.Opaque);
                        Microsoft.Xna.Framework.Rectangle dstRect = new Microsoft.Xna.Framework.Rectangle((int)BokuGame.ScreenPosition.X, (int)BokuGame.ScreenPosition.Y, (int)BokuGame.ScreenSize.X, (int)BokuGame.ScreenSize.Y);
                        batch.Draw(fullRenderTarget0, dstRect, Color.White);
                        batch.End();
                    }

                    // Set viewport to take into account tutorial mode changing the screen.
                    SetViewportToScreen();
                    
                    // Render touch cursor (if any touches).  Note this MUST be after viewport is set.
                    RenderTouchCursor();

                    bool active3DMenus = shared.ToolMenu.Active
                        || InGame.inGame.CurrentUpdateMode == UpdateMode.EditObject
                        || InGame.inGame.mouseEditUpdateObj.ToolBox.Active
                        || InGame.inGame.touchEditUpdateObj.ToolBox.Active;

                    //make sure this happens for gamepad edit modes as well - in some cases it wasn't causing artifacts
                    //from left over stencil/depth buffer values
                    if (active3DMenus)
                    {
                        /// Back to rendering to primary, but we still haven't cleared it. Just clear
                        /// the depth, we've just blitted to cover the whole color buffer.
                        device.Clear(ClearOptions.DepthBuffer | ClearOptions.Stencil, Color.Red, 1.0f, 0);
                    }

                    // We have a rendered frame now, we don't need the static (from file)
                    // thumbnail anymore.
                    ThumbNail = null;

                    // Render 2D, screen space overlays.
                    if (Terrain.Current.ShowCompass || InGame.inGame.CurrentUpdateMode != UpdateMode.RunSim)
                    {
                        shared.compass.Render(shared.camera);
                    }

                    // NOTE This got removed when touch was added.  No clue what thought was there.  
                    // Adding it back in like this may cause other flakiness.
                    // If in object edit mode, render the color palette.
                    if (InGame.inGame.CurrentUpdateMode == UpdateMode.EditObject || InGame.inGame.CurrentUpdateMode == UpdateMode.TweakObject)
                    {
                        RenderColorMenu(shared.curObjectColor);
                    }

                    // Render first person thoughts.
                    ThoughtBalloonManager.Render(shared.camera, true);

                    // Write scores to screen.
                    if (InGame.inGame.CurrentUpdateMode == UpdateMode.RunSim)
                    {
                        VictoryOverlay.Render();
                    }

                    if ((InGame.inGame.CurrentUpdateMode != UpdateMode.RunSim || Terrain.Current.ShowResourceMeter) && Terrain.Current.EnableResourceLimiting)
                    {
                        shared.BudgetHUD.Render();
                    }

                    if (shared.SnapToGrid)
                    {
                        SpriteBatch batch = KoiLibrary.SpriteBatch;
                        GetFont Font = KoiX.SharedX.GetGameFont18Bold;

                        Color shadowColor = new Color(0, 0, 0, 200);
                        Color textColor = new Color(240, 240, 240);

                        bool brushInUse = false;
                        Vector2 position = Vector2.Zero;
                        if (InGame.inGame.CurrentUpdateMode == UpdateMode.MouseEdit)
                        {
                            Vector3 loc = Vector3.Zero;
                            if (MouseEdit.MouseTouchHitInfo.ActorHit != null && EditWorldScene.CurrentToolMode == EditWorldScene.ToolMode.EditObject)
                            {
                                loc = MouseEdit.MouseTouchHitInfo.ActorHit.Movement.Position;
                            }
                            else
                            {
                                loc = MouseEdit.MouseTouchHitInfo.TerrainPosition;
                            }
                            position = new Vector2(loc.X * 2.0f, loc.Y * 2.0f);

                            if (EditWorldScene.CurrentToolMode == EditWorldScene.ToolMode.TerrainPaint
                                || EditWorldScene.CurrentToolMode == EditWorldScene.ToolMode.TerrainRaiseLower)
                            {
                                brushInUse = true;
                            }
                        }
                        else if (InGame.inGame.CurrentUpdateMode == UpdateMode.TouchEdit)
                        {
                            Vector3 loc = Vector3.Zero;
                            if (TouchEdit.MouseTouchHitInfo.ActorHit != null && EditWorldScene.CurrentToolMode == EditWorldScene.ToolMode.EditObject)
                            {
                                loc = TouchEdit.MouseTouchHitInfo.ActorHit.Movement.Position;
                            }
                            else
                            {
                                loc = TouchEdit.MouseTouchHitInfo.TerrainPosition;
                            }
                            position = new Vector2(loc.X * 2.0f, loc.Y * 2.0f);

                            if (EditWorldScene.CurrentToolMode == EditWorldScene.ToolMode.TerrainPaint
                                || EditWorldScene.CurrentToolMode == EditWorldScene.ToolMode.TerrainRaiseLower)
                            {
                                brushInUse = true;
                            }
                        }
                        else
                        {
                            if (InGame.inGame.CurrentUpdateMode == UpdateMode.TerrainMaterial
                                || InGame.inGame.CurrentUpdateMode == UpdateMode.TerrainUpDown)
                            {
                                brushInUse = true;
                            }

                            position = new Vector2(InGame.inGame.shared.CursorPosition.X * 2.0f, InGame.inGame.shared.CursorPosition.Y * 2.0f);
                        }

                        string str = null;
                        if (brushInUse)
                        {
                            float radius = InGame.inGame.shared.editBrushRadius;
                            str = (position - new Vector2(radius)).ToString() + "-" + (position + new Vector2(radius) - Vector2.One).ToString() + " : " + ((int)Math.Round(radius * 2)).ToString();
                        }
                        else
                        {
                            position -= new Vector2(0.5f, 0.5f);
                            str = position.ToString();
                        }

                        /*
                        if (Time.FrameCounter % 20 == 0)
                            Debug.Print("brush position " + InGame.inGame.shared.editBrushPosition.ToString() + "  " + (2 * InGame.inGame.shared.editBrushPosition).ToString() + "  " + InGame.inGame.shared.editBrushRadius.ToString());
                        */

                        Vector2 pos = new Vector2(device.Viewport.Width, device.Viewport.Height);
                        pos.X = device.Viewport.Width - pos.X;
                        float margin = 4.0f;
                        pos.X += margin;
                        pos.Y -= Font().LineSpacing + margin;

                        batch.Begin();
                        {
                            TextHelper.DrawString(Font, str, pos, shadowColor);
                            pos -= Vector2.One;
                            TextHelper.DrawString(Font, str, pos, textColor);
                        }
                        batch.End();
                    }

                }

#if DEBUG_PINCH_GESTURE
                //Pinch gesture debug text
                PinchGestureRecognizer pinchGesture = TouchGestureManager.Get().PinchGesture;

                //if (pinchGesture.IsPinching)
                {
                    string str = "[PINCH DEBUG] State:" + pinchGesture.GetPinchState() + ", Scale: " + pinchGesture.Scale + ", Normal: " + TouchEdit.s_lastNormal;

                    SpriteBatch batch = KoiLibrary.SpriteBatch;
                    GetFont Font = Shared.GetGameFont18Bold;

                    Color textColor = new Color(50, 50, 200);

                    Vector2 position = Vector2.Zero;

                    Vector2 pos = new Vector2(device.Viewport.Width, 20.0f);
                    pos *= 1.0f - XmlOptionsData.OverscanPercent / 100.0f;
                    pos.X = device.Viewport.Width - pos.X + 20.0f;
                    float margin = 4.0f;
                    pos.X += margin;
                    pos.Y += Font().LineSpacing + margin;

                    batch.Begin();
                    TextHelper.DrawString(Font, str, pos, textColor);
                    batch.End();
                }

#endif


                // Tool tips.
                ToolTipManager.Render(shared.camera);

                // Overlay help screen.
                HelpOverlay.Render();

                // Restore Viewport from tutorial mode.
                // TODO (scoy) Try to keep pushing this further down until everything
                // renders with the modified viewport.
                RestoreViewportToFull();

                UnDoStack.Render();
                RenderMessageStack();

                shared.tooManyLightsMessage.Render();

                // Render PreGame, if any.
                if (InGame.inGame.preGame != null && InGame.inGame.preGame.Active)
                {
                    InGame.inGame.preGame.Render(camera);
                }

                AnimationSet.AnimationDebug();

                VirtualKeyboard.Render();

                /*
                goto debug_label;
            debug_label:
                device.SetRenderTarget(0, null);
                // DEBUGDISPLAY
                {
                    // device.Clear(Color.Red);
                    ScreenSpaceQuad ssquad = ScreenSpaceQuad.GetInstance();
                    Texture2D rt = distortRenderTarget1;
                    ssquad.Render(rt, Vector2.Zero, new Vector2(rt.Width, rt.Height), "TexturedNoAlpha");
                }
                */
            }   // end of RenderObj Render()

            public override void Activate()
            {

            }   // end of RenderObj Activate()
            
            public override void Deactivate()
            {

            }   // end of RenderObj Deactivate()


            public void LoadContent(bool immediate)
            {
            }   // end of InGame RenderObj LoadContent()

            public void InitDeviceResources(GraphicsDevice device)
            {
                ReallocateRenderTargets();

                copyFilter = new CopyFilter();
                boxFilter = new Box4x4BlurFilter();
                gaussianFilter = new GaussianFilter();
                thresholdFilter = new ThresholdFilter();
                dofFilter = new DOF_Filter();

                skybox = new SkyBox();

                DistortionManager.PartyEnabled = BokuSettings.Settings.PostEffects;

                // Load device dependent bits.
                BokuGame.Load(copyFilter);
                BokuGame.Load(boxFilter);
                BokuGame.Load(gaussianFilter);
                BokuGame.Load(thresholdFilter);
                BokuGame.Load(dofFilter);

                BokuGame.Load(skybox);

            }   // end of InGame Shared InitDeviceResources()

            /// <summary>
            /// Reallocates (or allocates) rendertargets to match existing backbuffer size.
            /// </summary>
            public void ReallocateRenderTargets()
            {
                GraphicsDevice device = KoiLibrary.GraphicsDevice;
                int width = (int)(BokuGame.ScreenPosition.X + BokuGame.ScreenSize.X);
                int height = (int)(BokuGame.ScreenPosition.Y + BokuGame.ScreenSize.Y);

                if (fullRenderTarget0 == null || fullRenderTarget0.Width != width || fullRenderTarget0.Height != height)
                {
                    // Release everything we're going to reallocate.
                    DeviceResetX.Release(ref fullRenderTarget0);
                    DeviceResetX.Release(ref fullRenderTarget1);
                    DeviceResetX.Release(ref effectsRenderTarget);
                    DeviceResetX.Release(ref distortRenderTarget0);
                    DeviceResetX.Release(ref distortRenderTarget1);
                    DeviceResetX.Release(ref smallRenderTarget0);
                    DeviceResetX.Release(ref smallNoEffect);
                    DeviceResetX.Release(ref smallEffectThumb);
                    DeviceResetX.Release(ref bloomRenderTarget);
                    DeviceResetX.Release(ref glowRenderTarget);
                    DeviceResetX.Release(ref tinyRenderTarget0);
                    DeviceResetX.Release(ref shadowFullRenderTarget);
                    DeviceResetX.Release(ref shadowSmallRenderTarget0);
                    DeviceResetX.Release(ref shadowSmallRenderTarget1);
                    DeviceResetX.Release(ref thumbRenderTarget);

                    int numSamples = BokuSettings.Settings.AntiAlias ? 8 : 1;
#if NETFX_CORE
                    numSamples = 0;
#endif
                    fullRenderTarget0 = new RenderTarget2D(device, width, height, false, SurfaceFormat.Color, DepthFormat.Depth24Stencil8, numSamples, RenderTargetUsage.PlatformContents);
                    fullRenderTarget1 = new RenderTarget2D(device, width, height, false, SurfaceFormat.Color, DepthFormat.Depth24Stencil8, numSamples, RenderTargetUsage.PlatformContents);

                    int shadowTargetSize = 1024;
                    shadowFullRenderTarget = new RenderTarget2D(device, shadowTargetSize, shadowTargetSize, false, SurfaceFormat.Color, DepthFormat.None, 1, RenderTargetUsage.PlatformContents);
                    shadowSmallRenderTarget0 = new RenderTarget2D(device, shadowTargetSize / 4, shadowTargetSize / 4, false, SurfaceFormat.Color, DepthFormat.None, 1, RenderTargetUsage.PlatformContents);
                    shadowSmallRenderTarget1 = new RenderTarget2D(device, shadowTargetSize / 4, shadowTargetSize / 4, false, SurfaceFormat.Color, DepthFormat.None, 1, RenderTargetUsage.PlatformContents);

                    ShadowCamera.LoadShadowMask(@"Textures\shadowmask");

                    if (BokuSettings.Settings.PostEffects)
                    {
                        effectsRenderTarget = new RenderTarget2D(device, width, height, false, SurfaceFormat.Color, DepthFormat.Depth24Stencil8, 1, RenderTargetUsage.PlatformContents);
                    }

                    if (DistortionManager.EnabledSM3)
                    {
                        distortRenderTarget0 = new RenderTarget2D(device, width, height, false, SurfaceFormat.Color, DepthFormat.None, 1, RenderTargetUsage.PlatformContents);
                        distortRenderTarget1 = new RenderTarget2D(device, width, height, false, SurfaceFormat.Color, DepthFormat.None, 1, RenderTargetUsage.PlatformContents);
                    }

                    // Create several rendertargets 1/16 the size of the original.
                    smallRenderTarget0 = new RenderTarget2D(device, width / 4, height / 4, false, SurfaceFormat.Color, DepthFormat.None, 1, RenderTargetUsage.PlatformContents);
                    smallNoEffect = new RenderTarget2D(device, width / 4, height / 4, false, SurfaceFormat.Color, DepthFormat.None, 1, RenderTargetUsage.PlatformContents);
                    smallEffectThumb = new RenderTarget2D(device, width / 4, height / 4, false, SurfaceFormat.Color, DepthFormat.None, 1, RenderTargetUsage.PlatformContents);
                    thumbRenderTarget = new RenderTarget2D(device, 128, 128, false, SurfaceFormat.Color, DepthFormat.None, 1, RenderTargetUsage.PlatformContents);

                    SetRenderTarget(smallEffectThumb);
                    Clear(Color.Black);
                    SetRenderTarget(smallNoEffect);
                    Clear(Color.Black);
                    RestoreRenderTarget();

                    if (BokuSettings.Settings.PostEffects)
                    {
                        // And a couple 1/16 the size of the small ones.
                        bloomRenderTarget = new RenderTarget2D(device, width / 16, height / 16, false, SurfaceFormat.Color, DepthFormat.None, 1, RenderTargetUsage.PlatformContents);
                        glowRenderTarget = new RenderTarget2D(device, width / 16, height / 16, false, SurfaceFormat.Color, DepthFormat.None, 1, RenderTargetUsage.PlatformContents);
                        tinyRenderTarget0 = new RenderTarget2D(device, width / 16, height / 16, false, SurfaceFormat.Color, DepthFormat.None, 1, RenderTargetUsage.PlatformContents);
                    }

                }
            }   // end of ReallocateRenderTargets()

            public void UnloadContent()
            {
                DeviceResetX.Release(ref effectsRenderTarget);
                DeviceResetX.Release(ref distortRenderTarget0);
                DeviceResetX.Release(ref distortRenderTarget1);

                DeviceResetX.Release(ref fullRenderTarget0);
                DeviceResetX.Release(ref fullRenderTarget1);
                DeviceResetX.Release(ref smallRenderTarget0);
                DeviceResetX.Release(ref smallEffectThumb);
                DeviceResetX.Release(ref smallNoEffect);
                DeviceResetX.Release(ref thumbRenderTarget);

                DeviceResetX.Release(ref shadowFullRenderTarget);
                DeviceResetX.Release(ref shadowSmallRenderTarget0);
                DeviceResetX.Release(ref shadowSmallRenderTarget1);

                ShadowCamera.ReleaseShadowMask();

                DeviceResetX.Release(ref bloomRenderTarget);
                DeviceResetX.Release(ref glowRenderTarget);
                DeviceResetX.Release(ref tinyRenderTarget0);

                BokuGame.Unload(copyFilter);
                BokuGame.Unload(boxFilter);
                BokuGame.Unload(gaussianFilter);
                BokuGame.Unload(thresholdFilter);
                BokuGame.Unload(dofFilter);

                BokuGame.Unload(skybox);

            }   // end of InGame RenderObj UnloadContent()

            /// <summary>
            /// Recreate render targets
            /// </summary>
            /// <param name="graphics"></param>
            public void DeviceReset(GraphicsDevice device)
            {
                // This can be optimized. Limited to render targets.
                UnloadContent();
                LoadContent(true);
                InitDeviceResources(device);
            }


            public void RenderMessageStack()
            {
                for (int i = 0; i < messages.Count; i++)
                {
                    messages[i].render(messages[i].data);
                }
            }
            private struct Message
            {
                public MessageRender render;
                public object data;

                public Message(MessageRender render, object data)
                {
                    this.render = render;
                    this.data = data;
                }
            };
            private List<Message> messages = new List<Message>();
            public void AddMessage(MessageRender render, object data)
            {
                Debug.Assert(FindMessage(render, data) == -1, "Adding same message twice");
                messages.Add(new Message(render, data));
            }
            public void EndMessage(MessageRender render, object data)
            {
                int iMessage = FindMessage(render, data);
                if (iMessage >= 0)
                {
                    messages.RemoveAt(iMessage);
                }
            }

            private int FindMessage(MessageRender render, object data)
            {
                for (int i = 0; i < messages.Count; ++i)
                {
                    if ((messages[i].render == render)
                        && (messages[i].data == data))
                    {
                        return i;
                    }
                }
                return -1;
            }

            private void RenderTouchCursor()
            {
                float size = Math.Min(BokuGame.ScreenSize.X, BokuGame.ScreenSize.Y);
                Vector2 cursorSize = new Vector2(size * 0.05f, size * 0.05f); //cursor takes up 5%

                //only render when touches are present
                if (TouchInput.TouchCount > 0)
                {
                    Vector2 targetCursorPos = touchCursorRenderPos;

                    if (TouchInput.TouchCount == 1)
                    {
                        targetCursorPos = TouchInput.Touches[0].position;
                    }
                    else
                    {
                        //take first two touch positions - not ideal, but sort of nonsensical anyway (we don't ever use more than 2 for app actions)
                        targetCursorPos = (TouchInput.Touches[0].position + TouchInput.Touches[1].position) * 0.5f;
                    }

                    if (bTouchCursorRendered && false)
                    {
                        touchCursorRenderPos = MyMath.InterpTo(touchCursorRenderPos, targetCursorPos, kTouchCursorInterpSpeed);
                    }
                    else
                    {
                        touchCursorRenderPos = targetCursorPos;
                    }
                
                    SpriteBatch batch = KoiLibrary.SpriteBatch;
                    batch.Begin(SpriteSortMode.Deferred, BlendState.NonPremultiplied);
                    {
                        Vector2 pos = touchCursorRenderPos - cursorSize / 2.0f;
                        Microsoft.Xna.Framework.Rectangle dstRect = new Microsoft.Xna.Framework.Rectangle((int)pos.X, (int)pos.Y, (int)cursorSize.X, (int)cursorSize.Y);
                        batch.Draw(ButtonTextures.TouchCursor, dstRect, Color.White);
                    }
                    batch.End();

                    bTouchCursorRendered = true;
                }
                else
                {
                    bTouchCursorRendered = false;
                }
            }   // end of RenderTouchCursor()

        }   // end of class RenderObj


        public abstract class InGameUpdateObject : UpdateObject, INeedsDeviceReset
        {
            public override void Update()
            {
                InGame.inGame.saveLevelDialog.Update();

                // If we're not in run mode or WorldTweak or..., we must be editing.  
                // So, look at the input mode and switch update modes if needed.
                // TODO (mouse) Do we need to set up / fix up cursor stuff here???
                if (InGame.inGame.CurrentUpdateMode != UpdateMode.RunSim 
                    && InGame.inGame.CurrentUpdateMode != UpdateMode.EditWorldParameters
                    && InGame.inGame.CurrentUpdateMode != UpdateMode.SelectNextLevel
                    && InGame.inGame.CurrentUpdateMode != UpdateMode.EditObjectParameters)
                {
                    if (KoiLibrary.LastTouchedDeviceIsGamepad &&
                        (InGame.inGame.CurrentUpdateMode == UpdateMode.MouseEdit || InGame.inGame.CurrentUpdateMode == UpdateMode.TouchEdit))
                    {
                        InGame.inGame.CurrentUpdateMode = UpdateMode.ToolMenu;
                    }
                    else if (KoiLibrary.LastTouchedDeviceIsTouch &&
                        (InGame.inGame.CurrentUpdateMode != UpdateMode.TouchEdit ))
                    {
                        InGame.inGame.CurrentUpdateMode = UpdateMode.TouchEdit;
                    }

                    // Touch also uses UpdateMode.MouseEdit;
                    else if (KoiLibrary.LastTouchedDeviceIsKeyboardMouse && InGame.inGame.CurrentUpdateMode != UpdateMode.MouseEdit)
                    {
                        InGame.inGame.CurrentUpdateMode = UpdateMode.MouseEdit;
                    }
                    
                }
            }   // end of Update()

#if !NETFX_CORE
            void Callback_OpenMiniHub(AsyncOperation op)
            {
                InGame.inGame.SwitchToMiniHub();
            }
#endif

            public override void Activate()
            {
            }

            public override void Deactivate()
            {
            }

            public virtual void LoadContent(bool immediate)
            {
            }

            public virtual void InitDeviceResources(GraphicsDevice device)
            {
            }

            public virtual void UnloadContent()
            {
            }

            public virtual void DeviceReset(GraphicsDevice device)
            {
            }
        }

        public delegate void InGameSaveDelegate();

        public delegate void MessageRender(object data);

        public static InGame inGame = null;     // Provide a ref to easily get back to this object.

        // Children.
        private Terrain terrain = null;
        private Cursor3D cursor3D = null;
        private GameThing cursorClone = null;
        private Editor editor = null;
        private PreGameBase preGame = null;
        private MouseEdit mouseEdit = null;
        private TouchEdit touchEdit = null; //PV: define the touch edit variable
        private static ColorPalette colorPalette = null;
        public AABB objectBounds = new AABB();
        public AABB totalBounds = new AABB(); // this is combined object and terrain.

        private double loadTime = 0.0; // time of level loaded
        private double startTime = 0.0; // time of start playing the level
        private double pauseTime = 0.0; // time at level paused
        private double totalPauseTime = 0.0;
        private double totalLoadedPauseTime = 0.0;

        private SaveLevelDialog saveLevelDialog = null;
        private InGameSaveDelegate saveLevelOnCancel = null;
        private InGameSaveDelegate saveLevelOnComplete = null;

        public List<GameThing> gameThingList = null;    // The list containing all the GameThings currently in the world.
                                                        // This includes Bokus and fruit.
        public bool LinkingLevels = false;              // Are we in the middle of switching levels.  This is needed to handle
                                                        // the Autosave properly.

        #region Accessors

        public double LevelPlaySeconds
        {
            get { return Time.GameTimeTotalSeconds - startTime - totalPauseTime; }
        }
        
        public double LevelLoadedSeconds
        {
            get { return Time.GameTimeTotalSeconds - loadTime - totalLoadedPauseTime; }
        }
        

        /// <summary>
        /// Does any koding in the current level use the mouse sensor?
        /// </summary>
        public bool ProgramUsesMouseInput
        {
            get { return shared.programUsesMouseInput; }
            set { shared.programUsesMouseInput = value; }
        }

        /// <summary>
        /// Does any koding in the current level use the left mouse filter?
        /// </summary>
        public bool ProgramUsesLeftMouse
        {
            get { return shared.programUsesLeftMouse; }
            set { shared.programUsesLeftMouse = value; }
        }

        /// <summary>
        /// Does any koding in the current level use the right mouse filter?
        /// </summary>
        public bool ProgramUsesRightMouse
        {
            get { return shared.programUsesRightMouse; }
            set { shared.programUsesRightMouse = value; }
        }
        
        /// <summary>
        /// Does any koding in the current level use the mouse hover filter?
        /// </summary>
        public bool ProgramUsesMouseHover
        {
            get { return shared.programUsesMouseHover; }
            set { shared.programUsesMouseHover = value; }
        }

        public static ColorPalette ColorPalette
        {
            get { return colorPalette; }
        }

        public MouseEdit MouseEdit
        {
            get { return mouseEdit; }
        }

        //PV: public accessor for the touchEdit variable
        public TouchEdit TouchEdit
        {
            get { return touchEdit; } 
        }       

        public bool SnapToGrid
        {
            get { return shared.SnapToGrid; }
            set 
            { 
                shared.SnapToGrid = value;

                if (value)
                {
                    shared.editBrushRadius = (float)Math.Floor(shared.editBrushRadius);
                }
            }
        }

        /// <summary>
        /// Computes current total level bounds from stashed object bounds and queried
        /// current terrain bounds.
        /// </summary>
        public static AABB TotalBounds
        {
            get
            {
                inGame.totalBounds.Set(Vector3.Zero, Vector3.Zero);

                Vector3 terraMin = Terrain.Min;
                Vector3 terraMax = Terrain.Max;
                if (terraMin != terraMax)
                {
                    inGame.totalBounds.Set(terraMin, terraMax);
                }

                if (inGame.objectBounds.Min != inGame.objectBounds.Max)
                {
                    inGame.totalBounds.Union(inGame.objectBounds);
                }

                return inGame.totalBounds;
            }
        }

        public bool DialogActive
        {
            get { return saveLevelDialog.Active || ModularMessageDialogManager.Instance.IsDialogActive(); }
        }

        public RenderTarget2D EffectsRenderTarget
        {
            get { return renderObj.EffectsRenderTarget; }
        }

        #endregion

        public enum RenderEffect
        {
            DistortionPass,
            ShadowPass,
            BloomPass,
            DepthPass,
            Aura,
            GhostPass,

            Normal,  // Make sure Normal remains last on the list, because it's usual
                    // to have a textured and a non-textured variant of normal. If
                    // they're kept in a list, and Normal is the last effect, then
                    // they can be indexed as [Normal] and [Normal+1].

            NumRenderEffects
        }

        public string[] renderEffectNames = new string[(int)RenderEffect.NumRenderEffects] {
            RenderEffect.DistortionPass.ToString(),
            RenderEffect.ShadowPass.ToString(),
            RenderEffect.BloomPass.ToString(),
            RenderEffect.DepthPass.ToString(),
            RenderEffect.Aura.ToString(),
            RenderEffect.GhostPass.ToString(),
            RenderEffect.Normal.ToString(),
        };
        public RenderEffect renderEffects = RenderEffect.Normal;          // This is basically a global that lets the gameThings know that they should be
                                                    // rendering to the effects buffer instead of rendering color.  It would be nice
                                                    // if this was just an arg to the render call but than would mean changing 
                                                    // RenderObject which means that just about everything needs to change.  Of course
                                                    // all these non-gameThings would then have a paramter to their render call that
                                                    // they ignore resulting in more code churn for less efficient code.  Sweet.
                                                    // This should all get re-thought when we get a chance.


        // List objects.
        protected RenderObj renderObj = null;

        static public void Clear(Vector4 color)
        {
            Clear(new Color(color));
        }
        static public void Clear(Color color)
        {
            GraphicsDevice device = KoiLibrary.GraphicsDevice;
            ClearOptions options = ClearOptions.Target;

            // See if we have a depth buffer before trying to clear it.
            RenderTargetBinding[] bindings = device.GetRenderTargets();
            if (bindings != null && bindings.Length > 0)
            {
                RenderTarget2D rt = bindings[0].RenderTarget as RenderTarget2D;
                if (rt != null && rt.DepthStencilFormat != DepthFormat.None)
                {
                    options |= ClearOptions.DepthBuffer | ClearOptions.Stencil;
                }
            }
            device.Clear(options, color, 1.0f, 0);
        }
        static public void SetRenderTarget(RenderTarget2D targ)
        {
            GraphicsDevice device = KoiLibrary.GraphicsDevice;
            device.SetRenderTarget(targ);
        }

        static public Vector2 GetCurrentRenderTargetSize()
        {
            GraphicsDevice device = KoiLibrary.GraphicsDevice;
            Vector2 result = Vector2.Zero;

            RenderTargetBinding[] bindings = device.GetRenderTargets();
            if (bindings != null && bindings.Length > 0)
            {
                RenderTarget2D rt = bindings[0].RenderTarget as RenderTarget2D;
                if (rt != null)
                {
                    result = new Vector2(rt.Width, rt.Height);
                }
            }
            return result;
        }

        static public RenderTarget2D GetCurrentRenderTarget()
        {
            GraphicsDevice device = KoiLibrary.GraphicsDevice;
            RenderTarget2D result = null;

            RenderTargetBinding[] bindings = device.GetRenderTargets();
            if (bindings != null && bindings.Length > 0)
            {
                result = bindings[0].RenderTarget as RenderTarget2D;
            }
            return result;
        }

        /// <summary>
        /// Multiple render target version of set.
        /// </summary>
        /// <param name="targ0"></param>
        /// <param name="targ1"></param>
        static public void SetRenderTargets(RenderTarget2D targ0, RenderTarget2D targ1)
        {
            GraphicsDevice device = KoiLibrary.GraphicsDevice;
            RenderTargetBinding[] bindings =
            {
                new RenderTargetBinding(targ0),
                new RenderTargetBinding(targ1),
            };

            device.SetRenderTargets(bindings);
        }

        // TODO (scoy) There's something not right here since topLevelRenderTarget
        // never gets set.  Maybe what we really want is some kind of stack
        // mechanism so we can push/pop the current rendertarget.  This would help
        // out with on-demand loading of things like CardSpace tile textures.
        // Note, to do this right we should probably also keep track of the device
        // state when we push and restore it on pop.
        public static RenderTarget2D topLevelRenderTarget = null;
        static public void RestoreRenderTarget()
        {
            GraphicsDevice device = KoiLibrary.GraphicsDevice;

            device.SetRenderTarget(topLevelRenderTarget);
        }

        static Viewport originalViewport = new Viewport();
        /// <summary>
        /// Set the viewport to take into account the smaller screen that
        /// occurs during tutoral mode.
        /// </summary>
        static public void SetViewportToScreen()
        {
            try
            {
                GraphicsDevice device = KoiLibrary.GraphicsDevice;
                Viewport vp = device.Viewport;

                vp.X = (int)BokuGame.ScreenPosition.X;
                vp.Y = (int)BokuGame.ScreenPosition.Y;
                vp.Width = (int)BokuGame.ScreenSize.X;
                vp.Height = (int)BokuGame.ScreenSize.Y;

                device.Viewport = vp;
            }
            catch
            {
            }
        }
        static public void RestoreViewportToFull()
        {
            GraphicsDevice device = KoiLibrary.GraphicsDevice;

            if (originalViewport.Width > 0)
            {
                device.Viewport = originalViewport;
            }
        }
        static public void CaptureFullViewport()
        {
            GraphicsDevice device = KoiLibrary.GraphicsDevice;
            originalViewport = device.Viewport;
        }
        /// <summary>
        /// Sets the viewport to match the current rendertarget size.
        /// </summary>
        static public void SetViewportToRendertarget()
        {
            try
            {
                GraphicsDevice device = KoiLibrary.GraphicsDevice;
                Viewport vp = device.Viewport;

                Vector2 size = GetCurrentRenderTargetSize();

                vp.X = 0;
                vp.Y = 0;
                vp.Width = (int)size.X;
                vp.Height = (int)size.Y;

                device.Viewport = vp;
            }
            catch
            {
            }
        }
        /// <summary>
        /// Returns a thumbnail texture with the current scene in it.
        /// Returns null if missing or invalid.
        /// </summary>
        public Texture2D SmallThumbNail
        {
            get 
            {
                InGame.RefreshThumbnail = true;
                return renderObj.SmallThumbNail; 
            }
        }

        protected InGameUpdateObject updateObj = null;                // Current update object.
        protected InGameUpdateObject pendingUpdateObj = null;

        protected RunSimUpdateObj runSimUpdateObj = null;
        protected ToolMenuUpdateObj toolMenuUpdateObj = null;
        public EditObjectUpdateObj editObjectUpdateObj = null;
        protected TweakObjectUpdateObj tweakObjectUpdateObj = null;
        protected EditObjectParametersUpdateObj editObjectParametersUpdateObj = null;
        protected EditWorldParametersUpdateObj editWorldParametersUpdateObj = null;
        protected SelectNextLevelUpdateObj selectNextLevelUpdateObj = null;
        protected ToolBoxUpdateObj toolBoxUpdateObj = null;
        public MouseEditUpdateObj mouseEditUpdateObj = null;
        public TouchEditUpdateObj touchEditUpdateObj = null;

        public List<GameActor> brainUpdates = new List<GameActor>();
        public List<GameThing> chassisUpdates = new List<GameThing>();

        public bool IsRunSimActive
        {
            get { return runSimUpdateObj.Active; }
        }

        public void RegisterBrain(GameActor ga)
        {
            // We don't want to register the brains of creatables since we
            // don't want them to be run.  
            if (!ga.Creatable)
            {
                ga.BrainRegistered = Register(ga, brainUpdates, ga.BrainRegistered);
            }
        }
        public void RegisterChassis(GameThing gt)
        {
            // Note that we specifically do want to register the chassis 
            // of creatables since the chassis update keeps animations and
            // partical systems (smoke) in place and working.
            gt.ChassisRegistered = Register(gt, chassisUpdates, gt.ChassisRegistered);
        }
        public void RegisterCollide(GameActor ga)
        {
            if (!ga.Creatable)
            {
                // HACK Clam uses a fixed position chassis BUT uses the 
                // normal collision sphere so we need to special case it.
                //
                // The real problem is that "movers" are expected to use a simple
                // collision sphere while "static props" (castle, trees, etc) use
                // a collection of collision primitives.  The clam even though it's
                // a static prop doesn't have a collection of primitives and so needs
                // to use its collision sphere.  So, this hack tells the system to 
                // treat the clam as a mover.
                // Seagrass is even a worse hack.  We do the same thing so that collisions
                // are detected but we only care about them being reported as bumps.  We
                // don't allow seagrass to block movement (physical collisions).
                if ((ga.Chassis != null)
                    && (!ga.Chassis.FixedPosition || ga.Classification.name.Equals("clam") || ga.Classification.name.Equals("seagrass")))
                {
                    CollSys.RegisterMover(ga);
                }
                else
                {
                    CollSys.RegisterBlocker(ga);
                }
            }
            
        }
        protected bool Register(GameActor ga, List<GameActor> list, bool done)
        {
            if (!done)
            {
                list.Add(ga);
            }
            return true;
        }
        protected bool Register(GameThing gt, List<GameThing> list, bool done)
        {
            if (!done)
            {
                list.Add(gt);
            }
            return true;
        }

        private void UnregisterAllBrains()
        {
            for (int i = 0; i < brainUpdates.Count; ++i)
            {
                brainUpdates[i].BrainRegistered = false;
            }

            brainUpdates.Clear();
        }


        public void UnRegisterBrain(GameActor ga)
        {
            ga.BrainRegistered = UnRegister(ga, brainUpdates, ga.BrainRegistered);
        }
        public void UnRegisterChassis(GameThing gt)
        {
            gt.ChassisRegistered = UnRegister(gt, chassisUpdates, gt.ChassisRegistered);
        }
        public void UnRegisterCollide(GameActor ga)
        {
            // Same hack as in RegisterCollide().
            if ((ga.Chassis != null)
                && (!ga.Chassis.FixedPosition || ga.Classification.name.Equals("clam") || ga.Classification.name.Equals("seagrass")))
            {
                CollSys.UnregisterMover(ga);
            }
            else
            {
                CollSys.UnregisterBlocker(ga);
            }
        }   // end of UnregisterCollide()

        // SGI_MOD added to stop all sounds that were triggered by tiles to stop playing
        public void StopAllSounds()
        {
            for (int i = 0; i < brainUpdates.Count; ++i)
            {
                BokuGame.Audio.StopAllSounds(brainUpdates[i].AudioCues);
            }
        }

        protected bool UnRegister(GameActor ga, List<GameActor> list, bool done)
        {
            if (done)
            {
                list.Remove(ga);
            }
            return false;
        }
        protected bool UnRegister(GameThing gt, List<GameThing> list, bool done)
        {
            if (done)
            {
                list.Remove(gt);
            }
            return false;
        }

        public void UpdateObjects()
        {
            // Give the MouseEdit subsystem a chance to cache away anything we're
            // moused over.  Note that this is used for both editing the world and
            // to support the MouseSensor when in RunSim.
            MouseEdit.Update(shared.camera);

            // Fake running at slow frame rate.
            //System.Threading.Thread.Sleep(100); // DEBUG ONLY HACK  REMOVE BEFORE CHECKING IN!!!

            //don't update objects when dialog is up
            if (ModularMessageDialogManager.Instance.IsDialogActive())
            {
                return;
            }
            
            // This call has been MOVED to the TOP of the BokuGame::Update() call.
            // Go there for a comment on why this change was done.
            // TouchEdit.Update(shared.camera); 

            // Don't update if we're paused.
            if (Time.Paused == false)
            {
                GameActor.UpdateLinesOfPerception();

                // Reset previous positions and velocities for all objects.
                // This needs to be done for all objects before any collision and/or movement.
                // Also reset the DesiredMovement structure.  This needs to be done before
                // any of the brains are run each frame.
                for (int i = 0; i < chassisUpdates.Count; i++)
                {
                    chassisUpdates[i].Movement.SetPreviousPositionVelocity();
                    chassisUpdates[i].DesiredMovement.Reset();
                }

                // Clear the lists used by the camera when following multiple objects 
                // or in first person mode.
                CameraInfo.ResetIgnoreList();

                // Give bots a chance to do stuff before brain update starts.
                for (int i = 0; i < brainUpdates.Count; ++i)
                {
                    brainUpdates[i].PreBrainUpdate();
                }

                // Update all the brains.  Note that movement contraints may cause the bot
                // to change position so this must be done after resetting the previous
                // position and velocity.

                // First update all sensors for every bot. We do not make any changes
                // to the environment state at this time to ensure all bots are sensing
                // into the same dataset.
                for (int i = 0; i < brainUpdates.Count; ++i)
                {
                    brainUpdates[i].UpdateSensors(BrainCategories.NotSpecified);
                }

                // After the sensors have updated, we must bring the scoreboard up to the
                // present by erasing the changes made by the previous actuator update.
                Scoreboard.FreshenScores();
                for (int i = 0; i < brainUpdates.Count; ++i)
                {
                    brainUpdates[i].localScores.FreshenScores();
                }

                // After all sensors have queried the environment, run the actuators.
                for (int i = 0; i < brainUpdates.Count; ++i)
                {
                    brainUpdates[i].UpdateActuators();
                }

                // Delay the end of path process by 1 frame before clearing out all the flags
                // Allows for all sensors to detect the EOP 
                for (int i = 0; i < brainUpdates.Count; ++i)
                {
                    // If already set, this indicates that EOP happened last frame so clear state.
                    if (brainUpdates[i].ReadyToProcessEOP)
                    {
                        brainUpdates[i].ReadyToProcessEOP = false;
                        brainUpdates[i].ReachedEOP = Classification.Colors.None;
                    }
                    else if (brainUpdates[i].ReachedEOP != Classification.Colors.None)
                    {
                        // We found an end of path, indicate we should process it.
                        brainUpdates[i].ReadyToProcessEOP = true;
                    }
                }

                // Now that all the actuators are run and the CameraInfo state is fully set
                // we can update the first person state of all the bots.
                for (int i = 0; i < brainUpdates.Count; i++)
                {
                    brainUpdates[i].UpdateFirstPersonState();
                }

                // Give bots a chance to do stuff after brain update is done.
                for (int i = 0; i < brainUpdates.Count; ++i)
                {
                    brainUpdates[i].PostBrainUpdate();
                }

                // Now that the brains have run, resolve any conflicts in the Follow
                // and NeverFollow lists.
                CameraInfo.ResolveFollowLists();

                //
                // Do pre-collision update.  This does most of the dynamics for the object.  
                // Also build up current camera focus list at the same time.
                //
                for (int i = 0; i < chassisUpdates.Count; i++)
                {
                    GameActor ga = chassisUpdates[i] as GameActor;
                    // Actors may have their speed tweaked.
                    if (ga != null)
                    {
                        ga.Chassis.MovementSpeedModifier = ga.MovementSpeedModifier;
                        ga.Chassis.TurningSpeedModifier = ga.TurningSpeedModifier;
                        ga.Chassis.LinearAccelerationModifier = ga.LinearAccelerationModifier;
                        ga.Chassis.TurningAccelerationModifier = ga.TurningAccelerationModifier;
                    }

                    chassisUpdates[i].Chassis.PreCollisionTestUpdate(chassisUpdates[i]);

                    // If this object is being held then update its velocity to match the 
                    // holding object's so that the collision testing works correctly.
                    if (ga != null && ga.ActorHoldingThis != null)
                    {
                        AdjustHeldActor(ga);
                    }

                    // Have we fallen off the edge of the world?  Die, die, die!
                    if (ga != null
                        && ga.CurrentState == GameThing.State.Active
                        && ga.Movement.Position.Z < -50.0f)
                    {
                        ga.Deactivate();
                        return;
                    }
                }

                if (pendingState != States.Inactive)
                {
                    BokuGame.CheckRefresh();
                }

                /// Update the collision system and perform collision tests. This will cause
                /// callbacks to GameActor.ApplyCollisions. It uses the Movement.PrevPosition,
                /// and the current Movement.Position as a desired position.
                CollSys.Update();

                //
                // Do collision testing with glass walls and terrain.
                //
                // Note that this is going over the whole gameThingList instead
                // of just the chassisUpdates list.  This is because anything 
                // added during the brain update may get pushed into an invalid
                // location before it has a chance to get added to the list.
                for (int i = 0; i < chassisUpdates.Count; i++)
                {
                    GameActor ga = chassisUpdates[i] as GameActor;
                    if (ga != null)
                    {
                        //
                        // Do collision testing with the edge of the world and terrain walls.
                        //
                        ga.Chassis.CollideWithTerrainWalls(ga);

                        //
                        // Do final update.
                        //
                        ga.Chassis.PostCollisionTestUpdate(ga);

                        ga.PostCollide();

                        if (ga.TweakImmobile || ga.TweakImmobileNoRot)
                        {
                            ga.Movement.Position = ga.Movement.PrevPosition;
                        }

                        // Apply positional constraints imposed by the brain again.  We also apply 
                        // them in CollideWithTerrainWalls but the position may have been moved again
                        // in PostCollisionTestUpdate().
                        if (ga.Chassis.Constraints != ConstraintModifier.Constraints.None)
                        {
                            Vector3 position = ga.Movement.Position;
                            Vector3 velocity = ga.Movement.Velocity;
                            ga.Chassis.ApplyConstraints(ga.Movement, ref position, ref velocity);
                            ga.Movement.Position = position;
                            ga.Movement.Velocity = velocity;
                        }
                        // Clear constraints.
                        ga.Chassis.Constraints = ConstraintModifier.Constraints.None;

                    }
                }

            }

#if CAMERA_GHOSTING
            CheckGhost(shared.camera);
#endif // CAMERA_GHOSTING
        }

        /// <summary>
        /// Affect the held object by dragging it along with the actor holding it.
        /// </summary>
        /// <param name="ga"></param>
        private void AdjustHeldActor(GameActor ga)
        {
            ga.Movement.Velocity = ga.ActorHoldingThis.Movement.Velocity;

            // Move this held actor to the holding position of the holding actor.
            float secs = Time.GameTimeFrameSeconds;
            float strength = Math.Min(1.0f, 10.0f * secs);
            Vector3 position = ga.ActorHoldingThis.WorldHoldingPosition;
            float terraHeight = Terrain.GetTerrainAndPathHeight(position);
            float waterHeight = Terrain.GetWaterBase(position);

            float altitudeBase = ga.StayAboveWater ? MathHelper.Max(terraHeight, waterHeight) : terraHeight;

            position.Z = Math.Max(position.Z, altitudeBase + ga.Chassis.MinHeight);

            ga.Movement.Position = MyMath.Lerp(ga.Movement.Position, position, strength);
        }

        public Shared shared = null;

        public enum UpdateMode
        {
            RunSim,
            EditObject,
            ToolBox,
            EditWorldParameters,

            // Combines EditObject, ToolBox and EditWorldParamters.
            MouseEdit,

            TouchEdit, //PV: touch mode

            // Sub modes of EditObject
            TweakObject,
            EditObjectParameters,

            // Sub modes of ToolBox
            ToolMenu,
            TerrainUpDown,
            TerrainMaterial,
            TerrainWater,
            TerrainFlatten,
            TerrainRoughHill,
            DeleteObjects,

            // Not really a valid mode but having this enum is useful for the ToolMenu.
            MiniHub,

            // Used for overlaying the Load Level Menu when coming from the Edit World Parameters

            SelectNextLevel,

            NumModes,

            None,   // Use to "clear" pendingUpdateMode.
        }



        private UpdateMode currentUpdateMode = UpdateMode.None;     // Start in None, this forces a reset the first time we set any mode.
        private UpdateMode pendingUpdateMode = UpdateMode.None;     // The mode we're switching to.
        private UpdateMode previousUpdateMode = UpdateMode.RunSim;  // Where we were before we got to where we are.

        public enum States
        {
            Inactive,
            Active,
            Paused,
        }
        
        private States state = States.Inactive;
        private States pendingState = States.Inactive;
        
        public void AddBatch(FBXModel.RenderPack pack)
        {
            renderObj.AddBatch(pack);
        }
        public bool PushBatching(bool on)
        {
            return renderObj.PushBatching(on);
        }
        public void PopBatching(bool on)
        {
            renderObj.PopBatching(on);
        }

        #region Accessors
        public bool EditingTerrain
        {
            get
            {
                return InGame.inGame.CurrentUpdateMode == UpdateMode.TerrainUpDown
                    || InGame.inGame.CurrentUpdateMode == UpdateMode.TerrainMaterial
                    || InGame.inGame.CurrentUpdateMode == UpdateMode.TerrainWater
                    || InGame.inGame.CurrentUpdateMode == UpdateMode.TerrainFlatten
                    || InGame.inGame.CurrentUpdateMode == UpdateMode.TerrainRoughHill
                    || InGame.inGame.CurrentUpdateMode == UpdateMode.DeleteObjects
                    || (InGame.inGame.CurrentUpdateMode == UpdateMode.MouseEdit && InGame.inGame.mouseEditUpdateObj.EditingTerrain)
                    || (InGame.inGame.CurrentUpdateMode == UpdateMode.TouchEdit && InGame.inGame.touchEditUpdateObj.EditingTerrain);
            }
        }
        public States State
        {
            get { return state; }
        }

        public static bool RefreshThumbnail
        {
            get { return inGame.shared.refreshThumbnail; }
            set { inGame.shared.refreshThumbnail = value; }
        }

        /// <summary>
        /// This should be the standard way to get/set the current update mode.
        /// All the goo that needs to happen is then done in Refresh().
        /// </summary>
        public UpdateMode CurrentUpdateMode
        {
            get { return currentUpdateMode; }
            set
            {
                if (currentUpdateMode != value)
                {
                    // Transitioning between edit and run modes save away the appropriate camera. 
                    if (currentUpdateMode == UpdateMode.RunSim)
                    {
                        SavePlayModeCamera();
                    }
                    else
                    {
                        SaveEditCamera();
                    }

                    switch (value)
                    {
                        case UpdateMode.RunSim:
                            pendingUpdateObj = runSimUpdateObj;
                            break;
                        case UpdateMode.MiniHub:
                            pendingUpdateObj = runSimUpdateObj;
                            break;
                        case UpdateMode.MouseEdit:
                            pendingUpdateObj = mouseEditUpdateObj;
                            break;
                        case UpdateMode.TouchEdit:
                            pendingUpdateObj = touchEditUpdateObj;
                            break;
                        case UpdateMode.ToolMenu:
                            pendingUpdateObj = toolMenuUpdateObj;
                            break;
                        case UpdateMode.EditObject:
                            pendingUpdateObj = editObjectUpdateObj;
                            break;
                        case UpdateMode.EditWorldParameters:
                            pendingUpdateObj = editWorldParametersUpdateObj;
                            break;
                        case UpdateMode.SelectNextLevel:
                            pendingUpdateObj = selectNextLevelUpdateObj;
                            break;
                        case UpdateMode.EditObjectParameters:
                            pendingUpdateObj = editObjectParametersUpdateObj;
                            break;
                        case UpdateMode.TweakObject:
                            pendingUpdateObj = tweakObjectUpdateObj;
                            break;

                        case UpdateMode.ToolBox:
                            Debug.Assert(false, "This shouldn't be set directly.  Rather it should be set via one of its tools.");
                            pendingUpdateObj = toolBoxUpdateObj;
                            break;
                        case UpdateMode.TerrainUpDown:
                        case UpdateMode.TerrainMaterial:
                        case UpdateMode.TerrainWater:
                        case UpdateMode.TerrainFlatten:
                        case UpdateMode.TerrainRoughHill:
                            pendingUpdateObj = toolBoxUpdateObj;
                            break;

                        case UpdateMode.DeleteObjects:
                            pendingUpdateObj = toolBoxUpdateObj;
                            break;

                        default:
                            Debug.Assert(false);
                            break;
                    }
                    BokuGame.objectListDirty = true;
                    pendingUpdateMode = value;
                    previousUpdateMode = currentUpdateMode;
                }
            }
        }   // end of CurrentUpdateMode Accessors

        /// <summary>
        /// Returns the previous update mode.
        /// </summary>
        public UpdateMode PreviousUpdateMode
        {
            get { return previousUpdateMode; }
        }

        public ParticleSystemManager ParticleSystemManager
        {
            get { return shared.particleSystemManager; }
        }

        public string RunTimeLightRig
        {
            get { return shared.runTimeLightRig; }
            set
            {
                shared.runTimeLightRig = value;
                BokuGame.bokuGame.shaderGlobals.SetLightRig(value);
            }
        }

        /// <summary>
        /// The name of the current light rig.  Note that
        /// this is the internal, English name, not the 
        /// localized one.
        /// </summary>
        public static string LightRig
        {
            get { return xmlWorldData.lightRig; }
            set
            {
                if (xmlWorldData != null)
                {
                    xmlWorldData.lightRig = value;
                    BokuGame.bokuGame.shaderGlobals.SetLightRig(value);
                    InGame.IsLevelDirty = true;
                }
            }
        }

        public static float WindMin
        {
            get { return xmlWorldData.windMin; }
            set
            {
                if (xmlWorldData != null)
                {
                    xmlWorldData.windMin = value;
                    ShaderGlobals.WindMin = value;
                    InGame.IsLevelDirty = true;
                }
            }
        }
        public static float WindMax
        {
            get { return xmlWorldData.windMax; }
            set
            {
                if (xmlWorldData != null)
                {
                    xmlWorldData.windMax = value;
                    ShaderGlobals.WindMax = value;
                    InGame.IsLevelDirty = true;
                }
            }
        }
        /// <summary>
        /// Whether user setting is on to display debug path follow visualization.
        /// This is user debug, NOT development debug.
        /// </summary>
        public static bool DebugPathFollow
        {
            get { return xmlWorldData.debugPathFollow; }
            set
            {
                if (xmlWorldData != null)
                {
                    xmlWorldData.debugPathFollow = value;
                    InGame.IsLevelDirty = true;
                }
            }
        }
        /// <summary>
        /// Whether user setting is on to display debug collision visualization.
        /// This is user debug, NOT developer debug.
        /// </summary>
        public static bool DebugDisplayCollisions
        {
            get { return (xmlWorldData != null) && xmlWorldData.debugDisplayCollisions; }
            set
            {
                if (xmlWorldData != null)
                {
                    xmlWorldData.debugDisplayCollisions = value;
                    InGame.IsLevelDirty = true;
                }
            }
        }
        
        /// <summary>
        /// Whether we show a line from EVERYTHING that sees or hears to 
        /// EVERYTHING it sees or hears. See GameActor version for property
        /// specific to each actor.
        /// </summary>
        public static bool DebugDisplayLinesOfPerception
        {
            get { return (xmlWorldData != null) && xmlWorldData.debugDisplayLinesOfPerception; }
            set
            {
                if (xmlWorldData != null)
                {
                    xmlWorldData.debugDisplayLinesOfPerception = value;
                    InGame.IsLevelDirty = true;
                }
            }
        }
        
        /// <summary>
        /// Displays the currently executing programming page above each actor
        /// only if they have any code at all.
        /// </summary>
        public static bool DebugDisplayCurrentPage
        {
            get { return (xmlWorldData != null) && xmlWorldData.debugDisplayCurrentPage; }
            set
            {
                if (xmlWorldData != null)
                {
                    xmlWorldData.debugDisplayCurrentPage = value;
                    InGame.IsLevelDirty = true;
                }
            }
        }

        public static float LevelFoleyVolume
        {
            get { return xmlWorldData != null ? xmlWorldData.foleyVolume : 1.0f; }
            set
            {
                if (xmlWorldData != null)
                {
                    xmlWorldData.foleyVolume = value;
                    float total = value * XmlOptionsData.FoleyVolume;
                    BokuGame.Audio.SetVolume("Foley", total);
                    InGame.IsLevelDirty = true;
                }
            }
        }
        public static float LevelMusicVolume
        {
            get { return xmlWorldData != null ? xmlWorldData.musicVolume : 1.0f; }
            set
            {
                if (xmlWorldData != null)
                {
                    xmlWorldData.musicVolume = value;
                    float total = value * XmlOptionsData.MusicVolume;
                    BokuGame.Audio.SetVolume("Music", total);
                    InGame.IsLevelDirty = true;
                }
            }
        }
        public static float CameraSpringStrength
        {
            get { return xmlWorldData == null ? 1.0f : xmlWorldData.cameraSpringStrength; }
            set
            {
                if (xmlWorldData != null)
                {
                    xmlWorldData.cameraSpringStrength = value;
                    InGame.IsLevelDirty = true;
                }
            }
        }
        public static bool StartingCamera
        {
            get { return xmlWorldData.startingCamera; }
            set
            {
                if (xmlWorldData != null)
                {
                    xmlWorldData.startingCamera = value;
                    InGame.IsLevelDirty = true;
                }
            }
        }
        public static Vector3 StartingCameraFrom
        {
            get { return xmlWorldData.startingCameraFrom; }
            set
            {
                if (xmlWorldData != null)
                {
                    Debug.Assert(!float.IsNaN(value.X));
                    xmlWorldData.startingCameraFrom = value;
                    InGame.IsLevelDirty = true;
                }
            }
        }
        public static Vector3 StartingCameraAt
        {
            get { return xmlWorldData.startingCameraAt; }
            set
            {
                if (xmlWorldData != null)
                {
                    xmlWorldData.startingCameraAt = value;
                    InGame.IsLevelDirty = true;
                }
            }
        }
        public static float StartingCameraRotation
        {
            get { return xmlWorldData.startingCameraRotation; }
            set
            {
                if (xmlWorldData != null)
                {
                    xmlWorldData.startingCameraRotation = value;
                    InGame.IsLevelDirty = true;
                }
            }
        }
        public static float StartingCameraPitch
        {
            get { return xmlWorldData.startingCameraPitch; }
            set
            {
                if (xmlWorldData != null)
                {
                    xmlWorldData.startingCameraPitch = value;
                    InGame.IsLevelDirty = true;
                }
            }
        }
        public static float StartingCameraDistance
        {
            get { return xmlWorldData.startingCameraDistance; }
            set
            {
                if (xmlWorldData != null)
                {
                    xmlWorldData.startingCameraDistance = value;
                    InGame.IsLevelDirty = true;
                }
            }
        }
        public static bool ShowVirtualController
        {
            get{ return (null != xmlWorldData) && xmlWorldData.showVirtualController; }
            set 
            {
                if (null != xmlWorldData)
                { 
                    xmlWorldData.showVirtualController = value;
                    IsLevelDirty = true;
                }
            }
        }

        // TODO (mouse) Do we need this?
        public GameThing LastClonedThing
        {
            get { return editObjectUpdateObj.lastClonedThing; }
            set { editObjectUpdateObj.lastClonedThing = value; }
        }

        public GameActor ActiveActor
        {
            get
            {
                GameActor actor = null;
                if (KoiLibrary.LastTouchedDeviceIsKeyboardMouse)
                {
                    actor = mouseEdit.HighLit;
                }
                else if (KoiLibrary.LastTouchedDeviceIsTouch)
                {
                    actor = touchEdit.HighLit;
                }
                else
                {
                    actor = editObjectUpdateObj.editFocusObject as GameActor;
                }

                return actor;
            }
        }

        public Terrain Terrain
        {
            get { return terrain; }
        }
        public SmoothCamera Camera
        {
            get { return shared.camera; }
        }

        public void TransitionToLightRig(string newRig, float transitionTime)
        {
            InGame.inGame.shared.runTimeLightRig = newRig;
            BokuGame.bokuGame.shaderGlobals.TransitionToLightRig(newRig, transitionTime);
        }

        public void StopLightRigTransition()
        {
            BokuGame.bokuGame.shaderGlobals.StopLightRigTransition();
        }

        public bool IsSelected(GameActor actor)
        {
            // TODO (mouse) Are we using the same ref?
            GameActor selected = editObjectUpdateObj.editFocusObject as GameActor;
            return (selected != null) && (selected == actor);
        }
        public bool IsPickedUp(GameActor actor)
        {
            // TODO (mouse) Are we using the samer ref?
            GameActor pickedUp = editObjectUpdateObj.selectedObject as GameActor;
            return pickedUp == actor;
        }
        public GameActor GetPickedUpActor()
        {
            // TODO (mouse) Are we using the samer ref?
            return editObjectUpdateObj.selectedObject as GameActor;
        }
        public void ToggleSelectedStateOfFocusObject()
        {
            // TODO (mouse) Are we using the samer ref?
            editObjectUpdateObj.ToggleSelectedStateOfFocusObject();
        }
        public void DragSelectedObject(GameThing dragObject, Vector2 newPos2d, bool rotate)
        {
            // TODO (mouse) Are we using the samer ref?
            editObjectUpdateObj.DragSelectedObject(dragObject, newPos2d, rotate);
        }
        public GameActor ChangeTreeType(GameActor orig)
        {
            // TODO (mouse) Are we using the samer ref?
            return editObjectUpdateObj.ChangeTreeType(orig);
        }
        public bool HaveClipboard
        {
            // TODO (mouse) Are we using the samer ref?
            get { return editObjectUpdateObj.HaveClipboard; }
        }
        public Classification.Colors VictoryTeam
        {
            set
            {
                VictoryOverlay.ActiveTeam = value;
                if (value != Classification.Colors.NotApplicable)
                {
                    Time.Paused = true;
                }
            }
        }
        public GamePadSensor.PlayerId VictoryPlayer
        {
            set
            {
                VictoryOverlay.ActivePlayer = value;
                if (value != GamePadSensor.PlayerId.Dynamic)
                {
                    Time.Paused = true;
                }
            }
        }
        public bool VictoryWinner
        {
            set
            {
                VictoryOverlay.ActiveWinner = value;
                if (value)
                {
                    Time.Paused = true;
                }
            }
        }
        public bool GameOver
        {
            set 
            { 
                VictoryOverlay.ActiveGameOver = value;
                if (value)
                {
                    Time.Paused = true;
                }
            }
        }

        /// <summary>
        /// Get a thumbnail image suitable for saving off.
        /// Don't hold onto this image ref, it is extremely volatile.
        /// Since it's generated on the fly via RenderTargets, get the texture before setting
        /// up your render target to use it.
        /// So, get it, save it and/or render it, let it go, get it again next time you need it.
        /// </summary>
        public Texture2D ThumbNail
        {
            get { return renderObj.ThumbNail; }
            set { renderObj.ThumbNail = value; }
        }

        public Texture2D FullRenderTarget0
        {
            get { return renderObj.FullRenderTarget0; }
        }
        public Texture2D FullRenderTarget1
        {
            get { return renderObj.FullRenderTarget1; }
        }

        public bool RenderWorldAsThumbnail
        {
            get { return shared.renderWorldAsThumbnail; }
            set 
            {
                if (value)
                {
                    InGame.RefreshThumbnail = true;
                }
                shared.renderWorldAsThumbnail = value; 
            }
        }

        public ShadowCamera ShadowCamera
        {
            get { return renderObj.ShadowCamera; }
        }

        /// <summary>
        /// Reutrns the current level's pregame object, if any.
        /// </summary>
        public PreGameBase PreGame
        {
            get { return preGame; }
        }

        public bool PreGameActive
        {
            get { return preGame != null && preGame.Active == true; }
        }

        /// <summary>
        /// Return the current brain editor.
        /// </summary>
        public Editor Editor
        {
            get { return editor; }
        }

        public CommandMap EditBaseCommandMap
        {
            get { return toolMenuUpdateObj.CommandMap; }
        }
        #endregion

        //
        // c'tor
        //
        public InGame()
        {
            InGame.inGame = this;

            /// Must be before tools are initialized in new Shared().
            Water.InitTypes();

            // Create the RenderObject and UpdateObject parts of this mode.
            shared = new Shared();
            runSimUpdateObj = new RunSimUpdateObj(this, ref shared);
            toolMenuUpdateObj = new ToolMenuUpdateObj(this, ref shared);
            editObjectUpdateObj = new EditObjectUpdateObj(this, ref shared);
            tweakObjectUpdateObj = new TweakObjectUpdateObj(this, ref shared);
            toolBoxUpdateObj = new ToolBoxUpdateObj(this, ref shared);
            mouseEditUpdateObj = new MouseEditUpdateObj(this, ref shared);
            touchEditUpdateObj = new TouchEditUpdateObj(this, ref shared);
            editWorldParametersUpdateObj = new EditWorldParametersUpdateObj(this, ref shared);
            editObjectParametersUpdateObj = new EditObjectParametersUpdateObj(this, ref shared);
            selectNextLevelUpdateObj = new SelectNextLevelUpdateObj(this, ref shared);
            
            // Steal the RunSimUpdateObject's update list for the other update objects.
            toolMenuUpdateObj.updateList = runSimUpdateObj.updateList;
            toolBoxUpdateObj.updateList = runSimUpdateObj.updateList;
            editObjectUpdateObj.updateList = runSimUpdateObj.updateList;
            tweakObjectUpdateObj.updateList = runSimUpdateObj.updateList;
            toolBoxUpdateObj.updateList = runSimUpdateObj.updateList;
            mouseEditUpdateObj.updateList = runSimUpdateObj.updateList;
            touchEditUpdateObj.updateList = runSimUpdateObj.updateList;
            editWorldParametersUpdateObj.updateList = runSimUpdateObj.updateList;
            editObjectParametersUpdateObj.updateList = runSimUpdateObj.updateList;
            selectNextLevelUpdateObj.updateList = runSimUpdateObj.updateList;

            // Set the currently active update list.
            updateObj = runSimUpdateObj;
            currentUpdateMode = UpdateMode.None;
            
            renderObj = new RenderObj(ref shared);

            mouseEdit = new MouseEdit(this);

            touchEdit = new TouchEdit(this);
            
            Init();
            ExplosionManager.Init();    // This and the particle system manager should probably 
                                        // be pulled out of InGame and made available everywhere.

            saveLevelDialog = new SaveLevelDialog();
            saveLevelDialog.OnButtonPressed += OnSaveLevelDialogButton;


        }   // end of InGame c'tor

        private void Init()
        {
            shared.camera = new SmoothCamera();

            // Create children and add to lists.

            gameThingList = new List<GameThing>();

            cursor3D = new Cursor3D(new Vector2(), Color.White.ToVector4());

            editor = new Editor();

            // Doesn't need to be done here but is nice since it captures the original viewport value.
            SetViewportToScreen();
        }   // end of InGame Init()

        /// <summary>
        /// Switches out of simulation or level edit modes and activates the mini hub.
        /// NOTE: Caller is responsible for resetting sim if needed.
        /// 
        /// TODO (scoy) This should probably be removed.  The functionality should
        /// eventually go into the appropriate Activate or Deactivate calls for the
        /// scenes we are leaving and entering.
        /// </summary>
        public void SwitchToMiniHub()
        {
            if (updateObj == runSimUpdateObj)
            {
                BokuGame.Audio.PauseGameAudio();
                SavePlayModeCamera();
            }
            else if(IsLevelDirty)
            {
                /// We were editing, but not at the tool menu
                /// we need to save, as if we had backed to the tool
                /// menu before going to mini hub.
                UnDoStack.Store();
            }

            SceneManager.SwitchToScene("HomeMenuScene", frameDelay: 1);
            // Refresh the thumbnail during our 1 frame delay.
            InGame.RefreshThumbnail = true;
        }   // end of InGame SwithToMiniHub()

        /// <summary>
        /// Add a message to be rendered on top of the scene.
        /// </summary>
        /// <param name="render"></param>
        /// <param name="data"></param>
        public static void AddMessage(MessageRender render, object data)
        {
            InGame.inGame.renderObj.AddMessage(render, data);
        }
        /// <summary>
        /// Remove a message which has been rendering on top of the scene.
        /// </summary>
        /// <param name="render"></param>
        /// <param name="data"></param>
        public static void EndMessage(MessageRender render, object data)
        {
            InGame.inGame.renderObj.EndMessage(render, data);
        }

        public static void RenderMessages()
        {
            InGame.inGame.renderObj.RenderMessageStack();
        }

        public GameActor AddActor(GameActor actor)
        {
            actor.Pause();
            actor = (GameActor)AddThing(actor);

            return actor;
        }
        public GameActor AddActorAtCursor(GameActor actor)
        {
            return AddActor(actor, cursor3D.Position, shared.camera.Rotation);
        }
        public GameActor AddActor(GameActor actor, Vector3 pos, float rot)
        {
            actor.Movement.Position = pos;
            actor.Movement.RotationZ = rot;

            // Now that the position is set, we can query for the poper height
            // at this position and set that to match.  Note that you can't 
            // call GetPreferredAltitude() without setting the position first
            // or you'll get an altitude from some other place.

            if (InGame.inGame.CurrentUpdateMode != UpdateMode.RunSim)
            {
                // If the sim is not running, assume we're in some edit mode
                // so put the new actor to the correct height.
                pos.Z = actor.GetPreferredAltitude();
                actor.Movement.Position = pos;
            }

            GameActor a = AddActor(actor);

            if (a != null)
            {
                // Init InsideGlassWalls
                float terrainHeight = Terrain.GetTerrainAndPathHeight(a.Movement.Position);
                a.Chassis.InsideGlassWalls = terrainHeight != 0;
            }
            
            return a;
        }

        public bool IsTheFirstPerson(GameThing thing)
        {
            return thing == CameraInfo.FirstPersonActor;
        }

        /// <summary>
        ///  Drop a distortion pulse modeled after the thing at position.
        /// </summary>
        /// <param name="thing"></param>
        /// <param name="position"></param>
        /// <returns>the thing cloned</returns>
        public GameThing DistortionZap(GameThing thing, bool quiet)
        {
            DistortionManager.AddWithGlow(thing, !quiet);

            return thing;
        }

        public void SetActorColor(GameActor actor)
        {
            actor.ClassColor = ColorPalette.GetColorFromIndex(shared.curObjectColor);
            Foley.PlayColorChange();
            InGame.IsLevelDirty = true;
        }

        #region Bookkeeping for budgets
        #region Members
        private float totalActorCost = 0.0f;
        private float TotalBudget = 300.0f; // should wind up around 300.
        private bool limitBudget = true; // Disable this during loads
        #endregion Members
        #region Accessors
        /// <summary>
        /// Whether we are refusing to add things based on budget. We disable this
        /// during load, so even if a level is somewhat over budget, it will still load
        /// completely.
        /// </summary>
        public bool LimitBudget
        {
            get { return limitBudget && Terrain.Current.EnableResourceLimiting; }
            set { limitBudget = value; }
        }
        /// <summary>
        /// True if budget remains for adding more stuff to the scene.
        /// </summary>
        public bool UnderBudget
        {
            get { return !LimitBudget || (TotalCost < TotalBudget); }
        }
        /// <summary>
        /// True if over budget and denying creation requests.
        /// </summary>
        public bool OverBudget
        {
            get { return !UnderBudget; }
        }
        /// <summary>
        /// Ratio [0..1] of current usage of resources to total budget
        /// </summary>
        public float FractionFull
        {
            get { return MathHelper.Clamp(FractionFullUnclamped, 0.0f, 1.0f); }
        }
        /// <summary>
        /// Fraction of total budget used. Can be greater than 1 if over budget.
        /// </summary>
        public float FractionFullUnclamped
        {
            get { return TotalCost / TotalBudget; }
        }
        /// <summary>
        /// The current total calculated cost for the scene.
        /// </summary>
        public float TotalCost
        {
            get
            {
                return totalActorCost
                    + WayPoint.TotalCost
                    + Terrain.TotalCost;
            }
        }
        #endregion Accessors
        #region Public

        /// <summary>
        /// This is the dialog that is shown when changing to a linked 
        /// level but the current level has yet to be saved.
        /// </summary>
        /// <param name="onCancel"></param>
        /// <param name="onSaveComplete"></param>
        public void ShowInGameSaveDialog(InGameSaveDelegate onCancel, InGameSaveDelegate onSaveComplete)
        {
            saveLevelOnCancel = onCancel;
            saveLevelOnComplete = onSaveComplete;
            saveLevelDialog.Activate();
        }
        /// <summary>
        /// Try to add something to the scene, keeping budget considerations in
        /// mind. On success, returns the thing passed in. On failure (because
        /// over budget), returns null.
        /// </summary>
        /// <param name="thing"></param>
        /// <param name="ignoreResourceBudget">If true, ignores the resource budget constraints.</param>
        /// <returns></returns>
        public GameThing AddThing(GameThing thing, bool ignoreResourceBudget = false)
        {
            if (thing != null)
            {
                Debug.Assert(totalActorCost + 0.1f >= 0.0f);

                if (!thing.EnterScene())
                {
                    // If in the editor and we failed to create a light, tell the user why.
                    if (thing is Boku.Light && InGame.inGame.CurrentUpdateMode == UpdateMode.EditObject)
                    {
                        // Because of the delayed refresh if we just activate the text display here
                        // it's help overlay will get popped when the pie selector is deactivated
                        // and we'll end up with the pie selector's help.  So, replace it with the
                        // TextDisplay's and all will be fine...
                        if (HelpOverlay.Peek() == "PieSelectorAddItem")
                        {
                            HelpOverlay.Pop();
                            HelpOverlay.Push("TextDisplay");
                        }
                        shared.tooManyLightsMessage.Activate();
                        if (Terrain.Current.ShowResourceMeter)
                            Foley.PlayNoBudget();
                    }
                    ActorFactory.Recycle(thing as GameActor);
                    thing = null;
                }
                else if (OverBudget && !ignoreResourceBudget)
                {
                    thing.ExitScene();
                    Instrumentation.IncrementCounter(Instrumentation.CounterId.AddItemNoBudget);
                    if (Terrain.Current.ShowResourceMeter)
                        Foley.PlayNoBudget();
                    ActorFactory.Recycle(thing as GameActor);
                    thing = null;
                }
                else
                {
                    totalActorCost += thing.Cost;
                    gameThingList.Add(thing);
                }
            }
            return thing;
        }
        /// <summary>
        /// Delegate to pass around for adding things to the scene. InGame hands out
        /// its AddThing function instead of its gameThingList.
        /// </summary>
        /// <param name="thing"></param>
        /// <returns></returns>
        public delegate GameThing AddThingDelegate(GameThing thing, bool ignoreResourceBudget = false);
        #endregion Public


        #region Internal
        /// <summary>
        /// Zero out the actor cost on unloading a scene. It should already 
        /// be about zero because we've just removed everything, but this prevents
        /// accumulation of slight errors.
        /// </summary>
        private void ResetActorCost()
        {
            totalActorCost = 0.0f;
            WayPoint.ResetCost();
        }
        /// <summary>
        /// Remove the specified thing from the scene, crediting the budget.
        /// </summary>
        /// <param name="which"></param>
        private void RemoveThing(int which)
        {
            GameThing thing = gameThingList[which];
            if (thing != null)
            {
                thing.ExitScene();

                Debug.Assert(totalActorCost + 0.1f >= thing.Cost);
                totalActorCost -= thing.Cost;
                gameThingList.RemoveAt(which);
            }
        }
        #endregion Internal
        #endregion Bookkeeping for budgets

        /// <summary>
        /// Compute the object bounds. Note that we don't want these kept up to date,
        /// as that would cause the sky dome to react every time a bot exploded. So 
        /// these are only computed at load.
        /// </summary>
        private void MakeObjectBounds()
        {
            bool boundsEmpty = true;
            for (int i = 0; i < gameThingList.Count; ++i)
            {
                GameThing thing = gameThingList[i];
                if (boundsEmpty)
                {
                    Vector3 pos = thing.WorldCollisionCenter;
                    objectBounds.Set(pos, pos);
                    boundsEmpty = false;
                }
                else
                {
                    objectBounds.Union(thing.WorldCollisionCenter);
                }
            }
            if (boundsEmpty)
            {
                objectBounds.Set(Vector3.Zero, Vector3.Zero);
            }
        }

        public static void ResetLevelLoadedTime()
        {
#if !NETFX_CORE
            //Debug.Print("-->loaded seconds pre 'ResetLevelLoaded'.  Total loaded seconds: " + InGame.inGame.shared.GetLevelLoadedSeconds());
#endif

            InGame.inGame.loadTime = Time.GameTimeTotalSeconds;
            InGame.inGame.totalLoadedPauseTime = 0.0;
        }

        // We need inlining to happen when:
        //      Resume is chosen
        //      A level is run from Load Worlds
        //      Switch from edit to run mode
        //      A linked level is run
        // Because of this we call ApplyInlining() from a couple of places
        // which, in the Load Worlds case means it gets called twice.  So,
        // keep track of the frame when inlining is applied and don't allow
        // inlining twice on the same frame.
        static int lastInlineFrame = 0;

        /// <summary>
        /// Does the actual inlining of code wherever the user has used the "inline" actuator.
        /// </summary>
        public static void ApplyInlining()
        {
            if(lastInlineFrame == Time.FrameCounter)
            {
                return;
            }
            lastInlineFrame = Time.FrameCounter;

            int currentTask = 0;

            // Go through all characters
            for (int gi = 0; gi < InGame.inGame.gameThingList.Count; gi++)
            {
                GameThing thing = InGame.inGame.gameThingList[gi];
                GameActor actor = thing as GameActor;
                if (actor == null)
                    continue;
                currentTask = 0;
                // Go through pages
                foreach (Task task in actor.Brain.tasks)
                {
                    int rcount = task.reflexes.Count;
                    // Go through reflexes
                    for (int ri = 0; ri < rcount; ri++)
                    {
                        Reflex reflex = (Reflex)task.reflexes[ri];

                        // Find the inline actuator
                        if (reflex.Actuator != null && reflex.actuatorUpid == "actuator.inlinetask")
                        {
                            int mcount = reflex.Modifiers.Count;
                            // Find the task modifier
                            for (int mi = 0; mi < mcount; mi++)
                            {
                                Modifier modifier = (Modifier)reflex.Modifiers[mi];
                                if (modifier != null && modifier.GetType() == typeof(TaskModifier))
                                {
                                    TaskModifier taskModifier = (TaskModifier)modifier;
                                    TaskModifier.TaskIds taskId = taskModifier.taskid;

                                    Task inlinedTask = actor.Brain.tasks[(int)taskId];
                                    Reflex curReflex = reflex;
                                    int extraIndent = curReflex.Indentation + 1; // The base indentation level for copied relexes
                                    foreach (Reflex copyReflex in inlinedTask.reflexes)
                                    {
                                        ReflexData clip = copyReflex.Copy();
                                        clip.Indentation += extraIndent; // Indent copied reflex
                                        Reflex newReflex = new Reflex(task);
                                        task.InsertReflexAfter(curReflex, newReflex);
                                        newReflex.Paste(clip);
                                        curReflex = newReflex;
                                    }
                                }
                            }
                            mcount = reflex.Modifiers.Count; // Update number of modifiers (to appease visual studio)
                        }
                        rcount = task.reflexes.Count; // Update number of reflexes in case more were added
                    }
                    currentTask++; // Update the id of the current task
                }
            }
        }   // end of ApplyInlining()




        public override bool Refresh(List<UpdateObject> updateList, List<RenderObject> renderList)
        {
            bool result = false;

            /// We can recycle any actors deleted here if either
            /// a) We're currently in RunSim mode (what the recycling was built for)
            /// b) We're entering RunSim mode (as we're about to purge a bunch of stuff
            ///     to reload the level).
            /// Note that on a) we may come in as RunSim mode but leave as edit mode, but then
            /// we'll have just flushed a bunch of stuff that we want recycled anyway.
            /// We also set enabled for recycling on the end of this function in case the mode
            /// has changed. The big goal here is to disallow recycling while editing, as
            /// in cut and paste.
            ActorFactory.Enabled = CurrentUpdateMode != UpdateMode.EditObject && CurrentUpdateMode != UpdateMode.MouseEdit;

            // Call refresh on child list.
            // Note that we're always using the runSimUpdateObject's list even though
            // it may not be the active object since the other UpdateObjects just
            // reference it.
            for (int i = gameThingList.Count - 1; i >= 0; --i)
            {
                GameThing thing = gameThingList[i];
                if (thing.Refresh(runSimUpdateObj.updateList, renderObj.renderList))
                {
                    RemoveThing(i);
                }
            }

            cursor3D.Refresh(runSimUpdateObj.updateList, renderObj.renderList);

            // specifically tie this to the pasted in update list due to us being paused
            editor.Refresh(updateList, renderList);

            if (state == States.Paused)
            {
                // check the child object for deactivation and re-enable ourself
                if (!editor.Active)
                {
                    pendingState = States.Active;

                    // If the user pressed the back button to exit the editor
                    // change modes to the tool menu.
                    // TODO (mouse) WIll this cause problems?  Flickering?
                    if (editor.BackWasPressed)
                    {
                        CurrentUpdateMode = UpdateMode.ToolMenu;
                    }
                }
            }

            // Are we changing update modes?
            if (pendingUpdateObj != null)
            {
                // If in PreGame mode, we need to exit that first.
                if (preGame != null && preGame.Active)
                {
                    preGame.Active = false;
                }


                // Swap out the current updateObj for the pending one.
                updateList.Remove(updateObj);
                updateObj.Deactivate();

                updateObj = pendingUpdateObj;
                pendingUpdateObj = null;

                // Hack work around to get past this delayed refresh list stuff.
                if (!updateList.Contains(updateObj))
                {
                    updateList.Add(updateObj);
                }
                updateObj.Activate();

                if ((pendingUpdateMode == UpdateMode.ToolMenu || pendingUpdateMode == UpdateMode.MouseEdit)
                    && (currentUpdateMode != UpdateMode.RunSim)
                    && IsLevelDirty)
                {
                    // Check for dirty here?
                    UnDoStack.Store();
                }
                currentUpdateMode = pendingUpdateMode;
                pendingUpdateMode = UpdateMode.None;

                // Do a bunch of transition dependent stuff.
                // Currently, and this may change at any minute, the only non-editing mode we 
                // have is RunSim.  So, we only need to save and/or reload when going in and
                // out of RunSim mode.  Well, actually, not quite.  

                // Are we transitioning out of an edit mode.  If so, trigger an autosave.
                if(currentUpdateMode == UpdateMode.RunSim)
                {
                    HelpOverlay.ToolIcon = null;
                }
                else
                {
                    // Pause the sim when editing.
                    PauseAllGameThings();
                }

                // Are we transitioning into an editing mode from RunSim?
                if (previousUpdateMode == UpdateMode.RunSim)
                {
                    //cancel any pending transitions
                    StopLightRigTransition();
                    Terrain.Current.StopSkyTransition();

                    // Reload everything from Xml level data.
                    ResetSim(preserveScores: false, removeCreatablesFromScene: currentUpdateMode == UpdateMode.RunSim, keepPersistentScores: false);

                    // Force Day lighting since we're going into edit mode.
                    BokuGame.bokuGame.shaderGlobals.SetLightRig("Day");

                    // Restore the edit camera position unless we're already
                    // in free/edit mode in which case just leave the camera alone.  
                    if (CameraInfo.Mode != CameraInfo.Modes.Edit)
                    {
                        RestoreEditCamera(true);
                    }

                    // Refresh children in gameThingList to make the above ResetSim work.
                    // This is required to delete the deactivated game things.
                    for (int i = gameThingList.Count - 1; i >= 0; --i)
                    {
                        GameThing thing = gameThingList[i];
                        if (thing.Refresh(runSimUpdateObj.updateList, renderObj.renderList))
                        {
                            RemoveThing(i);
                        }
                    }

                    // Move the 3d cursor at the camera focus current position.  Don't activate it
                    // though.  That will be handled by the appropriate UpdateObject's Activate().
                    if (CameraInfo.CameraFocusGameActor != null && CameraInfo.Mode != CameraInfo.Modes.Edit)
                    {
                        cursor3D.Position = CameraInfo.CameraFocusGameActor.Movement.Position;
                    }
                    else
                    {
                        if (CameraInfo.Mode != CameraInfo.Modes.Edit)
                        {
                            // Pick a new position somewhere in front of the current camera.
                            Vector3 target = shared.camera.From + 13.0f * shared.camera.ViewDir;
                            target.Z = Terrain.GetTerrainAndPathHeight(target);
                            cursor3D.Position = target;
                        }
                    }

                    // TODO We need to rethink whether or not things have 
                    // a paused state as well as pausing via stopping time.
                    PauseAllGameThings();

                    // Set camera angle to be close to the current
                    // camera to make the transition less abrupt.
                    Vector2 dir = new Vector2(-shared.camera.ViewDir.X, -shared.camera.ViewDir.Y);
                    dir.Normalize();
                    shared.editCameraRotation = (float)Math.Acos(dir.X);
                    if (dir.Y < 0.0f)
                    {
                        shared.editCameraRotation = -shared.editCameraRotation;
                    }

                    // Show all the creatables.
                    AddCreatablesToScene();
                }

                // If we're going into run mode.
                if (currentUpdateMode == UpdateMode.RunSim)
                {
                    // We generally use the Day time rig for edit mode so transition 
                    // into the game's light rig.
                    BokuGame.bokuGame.shaderGlobals.SetLightRig(xmlWorldData.lightRig);

                    // Restore any valid run time mode camera values.  If we have a starting
                    // camera then use that, else use the last saved play mode camera.
                    if (InGame.StartingCamera)
                    {
                        RestoreStartingCamera();
                    }
                    else
                    {
                        RestorePlayModeCamera();
                    }

                    // Needed to make sure that deactivated objects are actually removed from
                    // the list otherwise they may get saved along with the newly activated ones.
                    Refresh(BokuGame.gameListManager.updateList, BokuGame.gameListManager.renderList);

                    // After flushing deactivated objects, deactivate the creatables.
                    RemoveCreatablesFromScene();
                    
                    cursor3D.Deactivate();

                    // We want to do the inlining as late as possible
                    // so the inlined results don't get autosaved out.
                    InGame.ApplyInlining();

                    InGame.inGame.SetUpPreGame();
                }

                GameActor.ClearLinesOfPerception();

                BokuGame.objectListDirty = true;

            }   // end if changing update modes.

            if (pendingState != state)
            {
                if (pendingState == States.Active)
                {
                    // Hack work around to get past this delayed refresh list stuff.
                    if (!updateList.Contains(updateObj))
                    {
                        updateList.Add(updateObj);
                    }
                    updateObj.Activate();
                    if (state == States.Inactive)
                    {
                        GC.Collect(); // major level cleanup
                        renderList.Add(renderObj);
                        renderObj.Activate();
                    }
                }
                else if (pendingState == States.Inactive)
                {
                    // If running then pause everything.
                    if (currentUpdateMode == UpdateMode.RunSim)
                    {
                        PauseAllGameThings();
                    }
                    else
                    {
                        // Must have been editing.  Autosave.
                    }

                    renderObj.Deactivate();
                    renderList.Remove(renderObj);
                    if (state == States.Active)
                    {
                        updateObj.Deactivate();
                        updateList.Remove(updateObj);
                    }
                }
                else if (pendingState == States.Paused)
                {
                    if (state == States.Active)
                    {
                        //updateObj.Deactivate();
                        updateList.Remove(updateObj);
                    }
                    else if (state == States.Inactive)
                    {
                        renderList.Add(renderObj);
                        renderObj.Activate();
                    }
                }
                state = pendingState;

                BokuGame.objectListDirty = true;
            }

            ActorFactory.Enabled = CurrentUpdateMode != UpdateMode.EditObject && 
                CurrentUpdateMode != UpdateMode.MouseEdit &&
                CurrentUpdateMode != UpdateMode.TouchEdit;

            switch(CurrentUpdateMode)
            {
                case UpdateMode.RunSim:
                    Cursor3D.Rep = Cursor3D.Visual.RunSim;
                    break;
                case UpdateMode.ToolMenu:
                case UpdateMode.EditObject:
                case UpdateMode.DeleteObjects:
                    Cursor3D.Rep = Cursor3D.Visual.Edit;
                    break;

                    // TODO (mouse) Anything need to be done here???

                    /// All others are terrain tools, water tools, or
                    /// don't use a cursor anyway.
                default:
                    Cursor3D.Rep = Cursor3D.Visual.Pointy;
                    break;
            }

            return result;
        }   // end of InGame Refresh()

        private object timerInstrument = null;

        override public void Activate()
        {
            if (state != States.Active)
            {
                if (Terrain == null)
                {
                    UnDoStack.Resume();
                }
                pendingState = States.Active;
                BokuGame.objectListDirty = true;

                timerInstrument = Instrumentation.StartTimer(Instrumentation.TimerId.InGame);

                Foley.PlaySimEnter();

                // Rebuild help overlay stack.
                HelpOverlay.Push("RunSimulation");
            }
        }   // end of InGame Activate()

        override public void Deactivate()
        {
            if (state != States.Inactive)
            {
                pendingState = States.Inactive;
                BokuGame.objectListDirty = true;

                // stop timer
                Instrumentation.StopTimer(timerInstrument);

                // Whether we were in sim mode or edit mode we 
                // need to pop everything off the stack.
                HelpOverlay.Clear();
            }
        }   // end of InGame Deactivate()

        public bool ShowEditor(GameActor gameActor)
        {
            
            if (state != States.Paused)
            {
                pendingState = States.Paused;
                BokuGame.objectListDirty = true;
            }

            // If the actor is a clone, edit its creatable instead.
            if (gameActor.IsClone)
            {
                GameActor creatableActor = GetCreatable(gameActor.CreatableId);

                if (creatableActor != null)
                {
                    // Edit the clone's creatable master.
                    gameActor = creatableActor;
                }
                else
                {
                    // Band-aid: This was an orphaned creatable clone. Workaround this bug by cleaning up the orphan, making it an individual.
                    gameActor.CreatableId = Guid.Empty;
                }
            }

            if (gameActor != null)
            {
                editor.GameActor = gameActor;
                editor.Activate();

                // Hide the color palette.
                ColorPalette.Active = false;
            }

            return gameActor != null;
        }

        /// <summary>
        /// Just a wrapper for calling into the ColorPalette's Render method.
        /// </summary>
        /// <param name="colorIndex"></param>
        public static void RenderColorMenu(int colorIndex)
        {
            colorPalette.Render(colorIndex);
        }   // end of WayPoint RenderColorMenu()

        /// <summary>
        /// Just a wrapper for calling into the ColorPalette's Render method.
        /// </summary>
        /// <param name="activeColor"></param>
        public static void RenderColorMenu(Classification.Colors activeColor)
        {
            int colorIndex = ColorPalette.GetIndexFromColor(activeColor);
            colorPalette.Render(colorIndex);
        }   // end of WayPoint RenderColorMenu()

        public static void ReplaceTopHelpOverlay(string overlay)
        {
            if (HelpOverlay.Peek() != overlay)
            {
                HelpOverlay.Pop();
                HelpOverlay.Push(overlay);
            }
        }

        public Cursor3D Cursor3D
        {
            get { return cursor3D; }
        }
        public void ShowCursor()
        {
            cursor3D.Activate();
        }

        public void HideCursor()
        {
            cursor3D.Deactivate();
        }
       
        /// <summary>
        /// Runs through the current gameThingList and pauses all the objects.
        /// </summary>
        public void PauseAllGameThings()
        {
            for (int i = 0; i < gameThingList.Count; i++)
            {
                GameThing thing = gameThingList[i];
                thing.Pause();
            }
            SharedEmitterManager.Bleeps.Clear();
            BokuGame.objectListDirty = true;

            BokuGame.Audio.PauseGameAudio();
        }   // end of InGame PauseAllGameThings()

        /// <summary>
        /// Runs through the current gameThingList and activates all the objects.
        /// </summary>
        public void ActivateAllGameThings()
        {
            // Before we loop across all the gamethings we want to set all scores and
            // GUI buttons to inactive.  The call to Brain.Wipe() will reactivate any
            // that are still needed.
            GUIButtonManager.DeactivateAllButtons();
            Scoreboard.Reset(ScoreResetFlags.AllSkipPersistent);

            for (int i = 0; i < gameThingList.Count; i++)
            {
                GameThing thing = gameThingList[i];
                thing.Activate();

                // We need to wipe each acotr's brain at this point so that scores
                // work correctly.  In particular, scores must be Active in order to
                // be displayed on the screen.  Part of Wipe goes through all the 
                // reflexes looking for scores that need to be activated.  If this
                // is not done then the first time a score is expected to display it
                // will not.  Since Wipe is called when unloading a level, the next
                // time the level is run the score will display correctly.
                GameActor actor = thing as GameActor;
                if (actor != null)
                {
                    actor.Brain.Wipe();
                }
            }
            BokuGame.objectListDirty = true;

            BokuGame.Audio.ResumeGameAudio();
        }   // end of InGame ActivateAllGameThings()

        /// <summary>
        /// Runs through the current gameThingList and deactivates all the objects.
        /// </summary>
        public void DeactivateAllGameThings()
        {
            for (int i = 0; i < gameThingList.Count; i++)
            {
                GameThing thing = gameThingList[i];
                thing.Deactivate();
            }
            SharedEmitterManager.Bleeps.Clear();
            BokuGame.objectListDirty = true;

            BokuGame.Audio.PauseGameAudio();
            BokuGame.Audio.StopGameMusic();

            Shield.Clear();
            Ripple.Clear();
#if CAMERA_GHOSTING
            ClearGhosts();
#endif // CAMERA_GHOSTING
        }   // end of InGame DeactivateAllGameThings()

        /// <summary>
        /// Create the GameThing that represents the cursor for non-visual use, like in brains
        /// </summary>
        protected void CreateCursorClone()
        {
            if (cursorClone == null)
            {
                cursorClone = new CursorThing(cursor3D);
                cursorClone.Activate();
                AddThing(cursorClone);
            }
        }

        /// <summary>
        /// Takes the input position and snaps it to the grid if SnapToGrid is true.
        /// </summary>
        /// <param name="position"></param>
        /// <returns></returns>
        public static Vector3 SnapPosition(Vector3 position)
        {
            if(InGame.inGame.shared.SnapToGrid)
            {
                Vector2 pos = new Vector2(position.X, position.Y);
                pos = SnapPosition(pos);
                position.X = pos.X;
                position.Y = pos.Y;
            }

            return position;
        }   // end of SnapPosition(Vector3)

        /// <summary>
        /// Takes the input position and snaps it to the grid if SnapToGrid is true.
        /// </summary>
        /// <param name="position"></param>
        /// <returns></returns>
        public static Vector2 SnapPosition(Vector2 position)
        {
            if (InGame.inGame.shared.SnapToGrid)
            {
                bool brushInUse = false;

                if (InGame.inGame.CurrentUpdateMode == UpdateMode.MouseEdit)
                {
                    if (EditWorldScene.CurrentToolMode == EditWorldScene.ToolMode.TerrainPaint
                        || EditWorldScene.CurrentToolMode == EditWorldScene.ToolMode.TerrainRaiseLower)
                    {
                        // Don't consider brush in use if the alt key is pressed.  This probably indicates
                        // that we're using the eyedropper functionality.
                        if(!KeyboardInputX.AltIsPressed)
                            brushInUse = true;
                    }
                }
                else if (InGame.inGame.CurrentUpdateMode == UpdateMode.TerrainMaterial
                    || InGame.inGame.CurrentUpdateMode == UpdateMode.TerrainUpDown)
                {
                    brushInUse = true;
                }

                // We have to scale by 2 and then divide by 2 to match the 0.5 size of the terrain blocks.
                int radius = (int)Math.Round(InGame.inGame.shared.editBrushRadius * 2.0f);
                if (brushInUse && ((radius & 0x01)!=1))
                {
                    position.X = 0.5f * (float)Math.Floor(2.0f * position.X + 0.5f);
                    position.Y = 0.5f * (float)Math.Floor(2.0f * position.Y + 0.5f);
                }
                else
                {
                    // The addition of an offset of 0.25 puts the cursor at the center of the block.
                    position.X = 0.25f + 0.5f * (float)Math.Floor(2.0f * position.X);
                    position.Y = 0.25f + 0.5f * (float)Math.Floor(2.0f * position.Y);
                }
            }

            return position;
        }   // end of SnapPosition(Vector2)

        public void LoadContent(bool immediate)
        {
            BokuGame.Load(renderObj, immediate);
            BokuGame.Load(shared, immediate);
            BokuGame.Load(cursor3D, immediate);
            BokuGame.Load(terrain, immediate);
            BokuGame.Load(editor, immediate);

            VictoryOverlay.LoadContent(immediate);
            ReflexHandle.LoadContent(immediate);

            // Load the graphics for all the things.
            for (int i = 0; i < gameThingList.Count; i++)
            {
                GameThing thing = gameThingList[i];
                BokuGame.Load(thing);
            }

            BokuGame.Load(editObjectUpdateObj);
            BokuGame.Load(mouseEditUpdateObj);
            BokuGame.Load(touchEditUpdateObj);

            if (colorPalette == null)
            {
                colorPalette = new ColorPalette();
            }
            BokuGame.Load(colorPalette, immediate);

            BokuGame.Load(saveLevelDialog, immediate);

            // Viewport should be valid here.  Grab it.
            CaptureFullViewport();
        }

        public void InitDeviceResources(GraphicsDevice device)
        {
            BokuGame.InitDeviceResources(shared, device);

            BokuGame.InitDeviceResources(editObjectUpdateObj, device);
            BokuGame.InitDeviceResources(mouseEditUpdateObj, device);
            BokuGame.InitDeviceResources(touchEditUpdateObj, device);

            ReflexHandle.InitDeviceResources(device);

            Terrain.RebuildAll();

            saveLevelDialog.InitDeviceResources(device);
        }

        public void UnloadContent()
        {
            BokuGame.Unload(renderObj);
            BokuGame.Unload(shared);
            BokuGame.Unload(cursor3D);
            BokuGame.Unload(terrain);
            BokuGame.Unload(editor);

            VictoryOverlay.UnloadContent();
            ReflexHandle.UnloadContent();

            // Unload the graphics for all the things.
            for (int i = 0; i < gameThingList.Count; i++)
            {
                GameThing thing = gameThingList[i];
                BokuGame.Unload(thing);
            }

            BokuGame.Unload(editObjectUpdateObj);
            BokuGame.Unload(mouseEditUpdateObj);
            BokuGame.Unload(touchEditUpdateObj);

            colorPalette = null;

            saveLevelDialog.UnloadContent();
        }   // end of InGame UnloadContent()

        /// <summary>
        /// Recreate render targets
        /// </summary>
        /// <param name="graphics"></param>
        public void DeviceReset(GraphicsDevice device)
        {
            BokuGame.DeviceReset(shared, device);
            BokuGame.DeviceReset(updateObj, device);
            BokuGame.DeviceReset(renderObj, device);

            BokuGame.DeviceReset(editObjectUpdateObj, device);
            BokuGame.DeviceReset(mouseEditUpdateObj, device);
            BokuGame.DeviceReset(touchEditUpdateObj, device);

            VictoryOverlay.DeviceReset(device);
            ReflexHandle.DeviceReset(device);

            BokuGame.DeviceReset(saveLevelDialog, device);
        }

        /// <summary>
        /// Generate a standard glow aura (thickness=0.5) around a gamething.
        /// </summary>
        /// <param name="thing"></param>
        /// <returns></returns>
        public Distortion MakeAura(GameThing thing)
        {
            return MakeAura(thing, 0.5f);
        }

        /// <summary>
        /// Generate a glow aura of specific thickness around a game thing.
        /// </summary>
        /// <param name="thing"></param>
        /// <param name="thickness"></param>
        /// <returns></returns>
        public Distortion MakeAura(GameThing thing, float thickness)
        {
            /// For inner radius Ri and outer radius Ro,
            /// Ro = Ri * scale
            /// also
            /// Ro = Ri + thickness (where thickness is the thickness of the shell).
            float radius = thing.BoundingSphere.Radius;
            float scale = (radius + thickness) / radius;

            Distortion aura = DistortionManager.Add(thing, 2.0f, new Vector3(scale));

            if (aura != null)
                aura.MakeAura();

            return aura;
        }

        /// <summary>
        /// Delete the thing from the level.
        /// </summary>
        /// <param name="thing"></param>
        public static void DeleteThingFromScene(GameThing thing)
        {
            if (thing != null)
            {
                Instrumentation.IncrementCounter(Instrumentation.CounterId.DeleteItem);

                thing.Deactivate();
                GameActor actor = thing as GameActor;

                if (actor != null)
                {
                    if (actor == InGame.inGame.mouseEditUpdateObj.ToolBox.EditObjectsToolInstance.FocusActor)
                    {
                        actor.MakeSelected(false, Vector4.Zero);
                        inGame.mouseEditUpdateObj.ToolBox.EditObjectsToolInstance.FocusActor = null;
                        Boku.InGame.ColorPalette.Active = false;
                    }
                    else if (actor == InGame.inGame.touchEditUpdateObj.ToolBox.EditObjectsToolInstance.FocusActor)
                    {
                        actor.MakeSelected(false, Vector4.Zero);
                        inGame.touchEditUpdateObj.ToolBox.EditObjectsToolInstance.FocusActor = null;
                        Boku.InGame.ColorPalette.Active = false;
                    }

                    GameActor source = null;
                    if (actor.Creatable)
                    {
                        actor.ClearCreatableCache();
                    }
                    else if (actor.IsClone)
                    {
                        source = InGame.inGame.GetCreatable(actor.CreatableId);
                    }

                    // "Cutting" a creatable makes all its clones into individuals.
                    actor.Creatable = false;

                    // "Cutting" a creatable clone makes it into an individual on the clipboard.
                    actor.CreatableId = Guid.Empty;

                    // If the thing we're deleting is a clone, update its parent's cache.
                    if (source != null)
                    {
                        source.CacheCreatables();
                    }
                }

                ExplosionManager.CreateSpark(thing.Movement.Position, 100, 0.1f, 1.5f, new Vector4(0.2f, 0.1f, 3.0f, 1.0f));

                Instrumentation.IncrementCounter(Instrumentation.CounterId.CutItem);
            }
        }

        /// <summary>
        ///  Drop a distortion pulse modeled after the thing at position.
        /// </summary>
        /// <param name="thing"></param>
        /// <param name="position"></param>
        /// <returns>the thing cloned</returns>
        public GameThing DistortionPulse(GameThing thing, bool doSound)
        {
            if (DistortionManager.EnabledSM3)
            {
                DistortionManager.AddWithBump(thing, doSound);
            }

            return thing;
        }

        /// <summary>
        /// Clones the input thing and moves it to the given position.
        /// </summary>
        /// <param name="thing"></param>
        /// <param name="position"></param>
        /// <returns></returns>
        public GameActor CloneInPlace(GameActor actor, Vector3 position, float rotation)
        {
            GameActor clone = null;

            if (actor != null)
            {
                clone = ActorFactory.Create(actor);
                Debug.Assert(clone != null);
                CopyActor(clone, actor);

                // Add a tiny bt of randomness to the position to
                // prevent objects from stacking.'
                Random rnd = BokuGame.bokuGame.rnd;
                position.X += 0.001f * (float)rnd.NextDouble();

                clone = AddActor(clone, position, rotation);
            }

            return clone;

        }   // end of CloneInPlace()

        /// <summary>
        /// We have a destination actor of correct type, make sure it
        /// is identical to the source.
        /// </summary>
        /// <param name="dst"></param>
        /// <param name="src"></param>
        protected void CopyActor(GameActor dst, GameActor src)
        {
            Debug.Assert(dst.GetType() == src.GetType(), "Copying between different types");

            // If the thing we're cloning is already in the world, set the 
            // lastCloneThing ref so that we can prevent it from colliding
            // with the selected object.
            if (gameThingList.Contains(src))
            {
                this.LastClonedThing = dst;
            }

            /// Type check the chassis, and clone if it's the wrong type,
            /// otherwise figure it's okay.
            if (src.Chassis.GetType() != dst.Chassis.GetType())
            {
                dst.Chassis = (BaseChassis)src.Chassis.Clone();
            }

            /// If the movement is the wrong type, create a new one, else
            /// just copy the values.
            if (src.Movement.GetType() != dst.Movement.GetType())
            {
                dst.Movement = (Movement)src.Movement.Clone();
            }
            else
            {
                src.Movement.CopyTo(dst.Movement);
            }

            // Copy the color.
            dst.ClassColor = src.ClassColor;

            // Copy the parameters.
            src.SharedParameters.CopyTo(dst.LocalParameters);

            // Copy the hit points
            dst.InitHitPoints(src.HitPoints);

            // Copy over the initial height offset.
            dst.HeightOffset = src.HeightOffset;

            // Creatable settings
            // NOTE: If we're cloning a creatable, the clone.Creatable flag needs
            // to be cleared but doing so from here (via its accessor) would unregister
            // the creatable. Instead, we let InitAsCreatableClone handle this in a
            // non-destructive way.
            dst.CreatableId = src.CreatableId;
            if (dst.CreatableId != Guid.Empty)
            {
                dst.InitAsCreatableClone();
            }
        }
        protected void CloneActor(GameActor dst, GameActor src)
        {
            // Copy the chassis data.
            dst.Chassis = (BaseChassis)src.Chassis.Clone();

            // If the thing we're cloning is already in the world, set the 
            // lastCloneThing ref so that we can prevent it from colliding
            // with the selected object.
            if (gameThingList.Contains(src))
            {
                this.LastClonedThing = dst;
            }

            // Copy the movement data.
            dst.Movement = (Movement)src.Movement.Clone();

            // Copy the color.
            dst.ClassColor = src.ClassColor;

            // Copy the parameters.
            src.SharedParameters.CopyTo(dst.LocalParameters);

            // Copy hitpoints.
            dst.HitPoints = src.HitPoints;

            // Copy over the initial height offset.
            dst.HeightOffset = src.HeightOffset;


            // Copy brain.
            dst.Brain = Brain.DeepCopy(src.Brain);
            dst.Brain.Wipe();

            // Trigger auto-generation of a new, unique name.
            dst.DisplayNameNumber = null;

            // Creatable settings
            // NOTE: If we're cloning a creatable, the clone.Creatable flag needs
            // to be cleared but doing so from here (via its accessor) would unregister
            // the creatable. Instead, we let InitAsCreatableClone handle this in a
            // non-destructive way.
            dst.CreatableId = src.CreatableId;
            if (dst.CreatableId != Guid.Empty)
            {
                dst.InitAsCreatableClone();
            }
        }
        internal void CutAction(object editObject)
        {
            // TODO (mouse) Do we need to duplicate or just share?
            editObjectUpdateObj.CutAction(editObject);
        }
        internal void CopyAction(object editObject)
        {
            // TODO (mouse) Do we need to duplicate or just share?
            editObjectUpdateObj.CopyAction(editObject);
        }
        internal void PasteAction(object editObject, Vector3 pos)
        {
            // TODO (mouse) Do we need to duplicate or just share?
            editObjectUpdateObj.PasteAction(editObject, pos);
        }
        internal void CloneAction(object editObject)
        {
            // TODO (mouse) Do we need to duplicate or just share?
            editObjectUpdateObj.CloneAction(editObject);
        }


        /// <summary>
        /// When passed in a size less than the total available video memory,
        /// this function locks up that much video memory and won't let it go.
        /// To find out how much video memory you have, pass in 0xffffffff, or
        /// some number larger than you could possibly have. Because if it fails
        /// to lock the requested amount, it spits out the amount that it could
        /// lock, and then releases all of it.
        /// This function works by allocating 256x256x4 rendertargets (which must reside in video
        /// memory) until reaching the goal or failure. On success, it holds onto
        /// the memory, simulating not having it to begin with. On failure, it 
        /// then releases all that allocated memory to not cripple your machine. 
        /// This is a debug/dev function, not a production runtime utility.
        /// </summary>
        public static void DebugCheckVideoMem(UInt64 sizeToSuck)
        {
            GraphicsDevice device = KoiLibrary.GraphicsDevice;
            UInt64 size = 0;
            try
            {
                while (true)
                {
                    RenderTarget2D targ = null;

                    targ = new RenderTarget2D(device,
                        256, 256, false, SurfaceFormat.Color, DepthFormat.None);

                    size += 256 * 256 * 4;
                    _slop.Add(targ);

                    if (size >= sizeToSuck)
                        break;
                }
            }
            catch // Don't really trust drivers to return the correct type -> (OutOfVideoMemoryException)
            {
                Debug.WriteLine(">>> Only alloc'd " + size + " Bytes");

                for (int i = 0; i < _slop.Count; ++i)
                {
                    _slop[i].Dispose();
                    _slop[i] = null;
                }
                _slop.Clear();
                size = 0;
            }
            Debug.WriteLine(">>> " + size + " Bytes Locked");
        }
        private static List<RenderTarget2D> _slop = new List<RenderTarget2D>();

        public bool IsOverUIButton(TouchContact touch, bool ignoreOnDrag)
        {
            if (InGame.inGame.CurrentUpdateMode == UpdateMode.MouseEdit && 
                InGame.inGame.mouseEditUpdateObj.ToolBar.IsOverUIButton(touch, ignoreOnDrag))                
            {
                //mouse mode over mouse UI?
                return true;
            }
            
            if (InGame.inGame.CurrentUpdateMode == UpdateMode.TouchEdit && 
                (InGame.inGame.touchEditUpdateObj.ToolBar.IsOverUIButton(touch, ignoreOnDrag) ||
                 InGame.inGame.touchEditUpdateObj.ToolBox.IsTouchOverMenuButton(touch)))
            {
                //touch mode over touch UI
                return true;
            }
            
            return false;
            
        }

    }   // end of class InGame

}   // end of namespace Boku
