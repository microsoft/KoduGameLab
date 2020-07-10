// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.


using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;

using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Audio;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using Microsoft.Xna.Framework.Storage;

using KoiX;
using KoiX.Input;
using KoiX.Text;

using Boku.Base;
using Boku.Common;
using Boku.Common.Xml;
using Boku.UI;
using Boku.UI2D;
using Boku.Input;
using Boku.Programming;
using Boku.SimWorld;
using Boku.Web;
using Boku.Fx;

using Boku.Audio;
using BokuShared;

namespace Boku
{
    /// <summary>
    /// Help card for display while user is in add item menu.
    /// </summary>
    public class AddItemHelpCard : GameObject, INeedsDeviceReset
    {
        protected class Shared : INeedsDeviceReset
        {

            #region Members
    
            public AddItemHelpCard parent = null;

            public ActorHelp actorHelp = null;

            public Camera camera = null;
            public Camera camera1k = null;      // Camera for rendering to the 1024x768 rt.
            public Texture2D thumbnail = null;  // Scene image for background.

            // The menu grids.
            public UIGrid examplesGrid = null;  // The list of pre-programmed bots.
            public int maxExamples = 20;        // Arbitrary.

            public GameActor actor = null;      // The actor just added that we need to program.  Doesn't exist yet...
            public string curActorName = null;
            public TextBlob descBlob = null;    // Description of character/item.
            public int topLine = 0;             // Which line of the description is being shown at the default starting position.
            public int descOffset = 0;          // Vertical offset (in pixels) for beginning of description.
            public int descTop = 88;            // Magic numbers all determined by pushing stuff around in Photoshop to match Brian's design.
            public int descMargin = 320;
            public int descWidth = 580;

            UIGridElement.ParamBlob examplesBlob = null;

            public AABB2D chooseBox = new AABB2D();     // Mouse hit box around "Choose <A>"
            public AABB2D leftStickBox = new AABB2D();  // Mouse hit box around left stick icon.
            public AABB2D rightStickBox = new AABB2D(); // Mouse hit box around right stick icon.

            #endregion

            #region Accessors
            #endregion

            #region Public

            // c'tor
            public Shared(AddItemHelpCard parent)
            {
                this.parent = parent;

                descBlob = new TextBlob(KoiX.SharedX.GetGameFont24, "replace me", descWidth);

                // We're rendering the camera specific parts into a 1024x768 rendertarget and
                // then copying (with masking) into the 1280x720 rt and finally cropping it 
                // as needed for 4:3 display.
                camera = new PerspectiveUICamera();
                camera.Resolution = new Point(1280, 720);
                camera1k = new PerspectiveUICamera();
                camera1k.Resolution = new Point(1024, 768);

                // Create examples grid.
                examplesGrid = new UIGrid(parent.OnSelect, parent.OnCancel, new Point(1, maxExamples), @"AddItemHelpCard.ExamplesGrid");
                Matrix mat = Matrix.CreateTranslation(0.4f, 0.0f, 0.0f);
                examplesGrid.LocalMatrix = mat;
                examplesGrid.Scrolling = true;
                examplesGrid.UseMouseScrollWheel = true;
                examplesGrid.Spacing = new Vector2(0.0f, 0.0f);

                // Set up the blob for info common to all preprogrammed bots.
                examplesBlob = new UIGridElement.ParamBlob();
                examplesBlob.width = 7.5f;
                examplesBlob.height = 1.35f;
                examplesBlob.edgeSize = 0.2f;
                examplesBlob.selectedColor = Color.Transparent;
                examplesBlob.unselectedColor = Color.Transparent;
                examplesBlob.textColor = Color.White;
                examplesBlob.dropShadowColor = Color.Black;
                examplesBlob.useDropShadow = false;
                examplesBlob.invertDropShadow = false;
                examplesBlob.justify = TextHelper.Justification.Left;
                examplesBlob.normalMapName = @"QuarterRound4NormalMap";
                examplesBlob.ignorePowerOf2 = true;
                examplesBlob.greyFlatShader = true;

            }   // end of Shared c'tor

            #endregion

            #region Internal

            /// <summary>
            /// Set up the Examples grid based on the ActorHelp for the current actor.
            /// </summary>
            public void SetUpGrid()
            {
                examplesGrid.Clear();

                // We set this here instead of the c'tor since the renderObj doesn't exist at that point...
                examplesBlob.Font = KoiX.SharedX.GetGameFont20;

                if (actorHelp != null && actorHelp.programs != null)
                {
                    for (int i = 0; i < actorHelp.programs.Count; i++)
                    {
                        UIGrid2DProgrammedBotElement e = new UIGrid2DProgrammedBotElement(examplesBlob, actorHelp, i);
                        examplesGrid.Add(e, 0, i);
                    }

                    // Start with the top one in focus.
                    examplesGrid.SelectionIndex = new Point(0, 0);

                    // Allow the geometry for the examples to load.
                    examplesGrid.LoadContent(true);
                }

            }   // end of SetUpGrid()



            public void LoadContent(bool immediate)
            {
                BokuGame.Load(examplesGrid, immediate);
            }   // end of AddItemHelpCard Shared LoadContent()

            public void InitDeviceResources(GraphicsDevice device)
            {
            }   // end of InitDeviceResources()

            public void UnloadContent()
            {
                BokuGame.Unload(examplesGrid);
            }   // end of AddItemHelpCard Shared UnloadContent()

            /// <summary>
            /// Recreate render targets.
            /// </summary>
            /// <param name="graphics"></param>
            public void DeviceReset(GraphicsDevice device)
            {
                BokuGame.DeviceReset(examplesGrid, device);
            }

            #endregion

        }   // end of class Shared

        protected class UpdateObj : UpdateObject
        {
            #region Members

            private AddItemHelpCard parent = null;
            private Shared shared = null;

            #endregion

            #region Public

            public UpdateObj(AddItemHelpCard parent, Shared shared)
            {
                this.parent = parent;
                this.shared = shared;
            }

            public override void Update()
            {
                int maxLines = 3;
                bool changed = false;
                int numLines = shared.descBlob != null ? shared.descBlob.NumLines : 0;

                // MouseInput
                {
                    Vector2 mouseHit = LowLevelMouseInput.GetAspectRatioAdjustedPosition(shared.camera, true);

                    if (shared.chooseBox.LeftPressed(mouseHit))
                    {
                        Select();
                    }

                    if (shared.descBlob != null && shared.rightStickBox.LeftPressed(mouseHit))
                    {
                        // Up?
                        if (mouseHit.Y < (shared.rightStickBox.Min.Y + shared.rightStickBox.Max.Y) / 2.0f)
                        {
                            // Up
                            if (shared.topLine > 0)
                            {
                                --shared.topLine;
                                changed = true;
                            }
                        }
                        else
                        {
                            // Down
                            if (numLines - maxLines > shared.topLine)
                            {
                                ++shared.topLine;
                                changed = true;
                            }
                        }
                    }

                    if (shared.leftStickBox.LeftPressed(mouseHit))
                    {
                        // Up?
                        if (mouseHit.Y < (shared.leftStickBox.Min.Y + shared.leftStickBox.Max.Y) / 2.0f)
                        {
                            // Up
                            shared.examplesGrid.MoveUp();
                        }
                        else
                        {
                            // Down
                            shared.examplesGrid.MoveDown();
                        }
                    }

                    // If we get a mouse click outside of the help area, just exit.
                    // Use the leftStickBox and chooseBox as the extents of our box.
                    AABB2D bigBox = new AABB2D(shared.leftStickBox.Min, shared.chooseBox.Max);
                    if (LowLevelMouseInput.Left.WasPressed && !bigBox.Contains(mouseHit))
                    {
                        // Done
                        parent.Deactivate();
                    }

                }   // end of mouse input

                // Our children may have input focus but we can still steal away the buttons we care about.
                GamePadInput pad = GamePadInput.GetGamePad0();

                if (Actions.Cancel.WasPressed)
                {
                    Actions.Cancel.ClearAllWasPressedState();

                    // Done
                    parent.Deactivate();
                }

                if (Actions.Select.WasPressed)
                {
                    Actions.Select.ClearAllWasPressedState();
                    Select();
                }

                if (shared.descBlob != null)
                {
                    if (Actions.AltUp.WasPressedOrRepeat)
                    {
                        Actions.AltUp.ClearAllWasPressedState();

                        if (shared.topLine > 0)
                        {
                            --shared.topLine;
                            changed = true;
                        }
                    }

                    if (Actions.AltDown.WasPressedOrRepeat)
                    {
                        Actions.AltDown.ClearAllWasPressedState();

                        if (numLines - maxLines > shared.topLine)
                        {
                            ++shared.topLine;
                            changed = true;
                        }
                    }

                    if (changed)
                    {
                        // Start a twitch to move the description text offset.
                        TwitchManager.Set<float> set = delegate(float value, Object param) { shared.descOffset = (int)value; };
                        TwitchManager.CreateTwitch<float>(shared.descOffset, -shared.topLine * SharedX.GameFont24.LineSpacing, set, 0.2f, TwitchCurve.Shape.EaseInOut);
                    }
                }

                // If we're not shutting down, update the child grids.
                if (parent.Active)
                {
                    Matrix world = Matrix.Identity;

                    if (shared.examplesGrid != null)
                    {
                        shared.examplesGrid.Update(ref world);
                    }

                }   // end if not shutting down.

            }   // end of Update()

            /// <summary>
            /// What happens if the user chooses one of the pre-programmed options.
            /// </summary>
            private void Select()
            {
                if (parent.parent != null)
                {
                    // Call the OnSelect() method on the pie menu.  This will cause it to go through 
                    // its normal process of creating and adding the new actor to the scene.
                    parent.parent.OnSelect(null, null);

                    // Now, need to get the brain of the bot we just added and
                    // stuff it full of code.  Yumm, stuffed bot brains...
                    if (shared.actor != null)
                    {
                        // Got brain?
                        Brain brain = shared.actor.Brain;
                        ActorHelp help = shared.actorHelp;
                        if (help.programs != null && help.programs.Count > 0)
                        {
                            ExampleProgram program = help.programs[shared.examplesGrid.SelectionIndex.Y];

                            for (int page = 0; page < program.pages.Count; page++)
                            {
                                Task task = (Task)brain.tasks[page];

                                for (int r = 0; r < program.pages[page].reflexes.Length; r++)
                                {
                                    ReflexData clip = program.pages[page].reflexes[r];
                                    Reflex reflex = new Reflex(task);
                                    task.AddReflex(reflex);
                                    reflex.Paste(clip);
                                }
                            }
                        }
                    }

                    Instrumentation.IncrementCounter(Instrumentation.CounterId.AddItemHelpCardInsertExample);
                }

                parent.Deactivate();
            }   // end of Select()

            #endregion

            #region Internal

            public override void Activate()
            {
            }

            public override void Deactivate()
            {
            }

            #endregion

        }   // end of class AddItemHelpCard UpdateObj  

        protected class RenderObj : RenderObject, INeedsDeviceReset
        {
            #region Members

            private Shared shared;
            
            private Effect effect = null;

            public Texture2D rightStickTexture = null;
            public Texture2D leftStickTexture = null;
            public Texture2D glassTileHighlight = null;
            public Texture2D whiteHighlight = null;
            public Texture2D blackHighlight = null;
            public Texture2D normalMap = null;

            private Base9Grid glassTile = null;
            private Base9Grid descTile = null;
            private Base9Grid examplesTile = null;
            private float edgeSize = 0.1f;

            private DepthStencilState depthStencilState = null;
            private DepthStencilState depthStencilStateNoWrite = null;

            #endregion

            #region Public

            public RenderObj(Shared shared)
            {
                this.shared = shared;

                depthStencilState = new DepthStencilState();
                depthStencilState.StencilEnable = true;
                depthStencilState.StencilFunction = CompareFunction.NotEqual;
                depthStencilState.ReferenceStencil = 0;

                depthStencilStateNoWrite = new DepthStencilState();
                depthStencilStateNoWrite.StencilEnable = true;
                depthStencilStateNoWrite.StencilFunction = CompareFunction.NotEqual;
                depthStencilStateNoWrite.ReferenceStencil = 0;
                depthStencilStateNoWrite.StencilWriteMask = 0x00;
            }

            public override void Render(Camera camera)
            {
                GraphicsDevice device = KoiLibrary.GraphicsDevice;

                RenderTarget2D rtFull = SharedX.RenderTargetDepthStencil1280_720;   // Rendertarget we render whole display into.
                RenderTarget2D rt1k = SharedX.RenderTargetDepthStencil1024_768;

                Vector2 screenSize = BokuGame.ScreenSize;
                Vector2 rtSize = new Vector2(rtFull.Width, rtFull.Height);

                ScreenSpaceQuad ssquad = ScreenSpaceQuad.GetInstance();

                Color greyTextColor = new Color(127, 127, 127);
                Color shadowTextColor = new Color(0, 0, 0, 20);
                Color greenTextColor = new Color(0, 255, 12);
                Color whiteTextColor = new Color(255, 255, 255);
                Vector2 shadowOffset = new Vector2(0, 6);

                // Render the description text and examples into the 1k rendertarget.
                InGame.SetRenderTarget(rt1k);
                InGame.Clear(Color.Transparent);

                SpriteBatch batch = KoiLibrary.SpriteBatch;
                GetFont FontHuge = SharedX.GetGameFont20;

                // Set up params for rendering UI with this camera.
                BokuGame.bokuGame.shaderGlobals.SetCamera(shared.camera1k);

                //
                // Render the samples grid.
                //
                bool noExamples = shared.examplesGrid == null || shared.examplesGrid.ActualDimensions == Point.Zero;
                if (!noExamples)
                {
                    // Clear the stencil buffer.
                    device.Clear(ClearOptions.Stencil, Color.Transparent, 1.0f, 0);

                    // Render the new stencil mask.  Magic numbers from Photoshop.
                    ssquad.RenderStencil(Vector4.One, new Vector2(100, 300), new Vector2(820, 400));

                    // Turn off stencil writing while rendering the grid.
                    device.DepthStencilState = depthStencilStateNoWrite;

                    shared.examplesGrid.Render(shared.camera1k);

                    // Restore default.
                    device.DepthStencilState = DepthStencilState.Default;
                }

                // Render the scene to our rendertarget.
                InGame.SetRenderTarget(rtFull);

                // Set up params for rendering UI with this camera.
                BokuGame.bokuGame.shaderGlobals.SetCamera(shared.camera);

                InGame.Clear(Color.Transparent);

                // Set up effect for rendering tiles.
                effect.CurrentTechnique = effect.Techniques["NormalMappedNoTexture"];

                effect.Parameters["Alpha"].SetValue(1.0f);
                effect.Parameters["SpecularColor"].SetValue(Vector4.Zero);
                effect.Parameters["SpecularPower"].SetValue(16.0f);
                effect.Parameters["NormalMap"].SetValue(normalMap);

                // Render tiles.
                Matrix world = Matrix.Identity;
                world.Translation = new Vector3(-3.4f, 2.5f, 0.0f);
                effect.Parameters["WorldMatrix"].SetValue(world);
                effect.Parameters["WorldViewProjMatrix"].SetValue(world * shared.camera.ViewProjectionMatrix);
                effect.Parameters["DiffuseColor"].SetValue(new Vector4(1.0f, 1.0f, 1.0f, 0.2f));
                glassTile.Render(effect);

                world.Translation = new Vector3(1.15f, 2.5f, 0.0f);
                effect.Parameters["WorldMatrix"].SetValue(world);
                effect.Parameters["WorldViewProjMatrix"].SetValue(world * shared.camera.ViewProjectionMatrix);
                effect.Parameters["DiffuseColor"].SetValue(new Vector4(1.0f, 1.0f, 1.0f, 1.0f));
                descTile.Render(effect);

                world.Translation = new Vector3(0.0f, -1.1f, 0.0f);
                effect.Parameters["WorldMatrix"].SetValue(world);
                effect.Parameters["WorldViewProjMatrix"].SetValue(world * shared.camera.ViewProjectionMatrix);
                effect.Parameters["DiffuseColor"].SetValue(new Vector4(0.0f, 0.0f, 0.0f, 1.0f));
                examplesTile.Render(effect);

                // Render highlights on tiles.
                CameraSpaceQuad quad = CameraSpaceQuad.GetInstance();
                device.BlendState = SharedX.BlendStateColorWriteRGB;

                // Glass tile.
                quad.Render(shared.camera, glassTileHighlight, Vector4.One, 1.0f, new Vector2(-3.4f, 2.9f), new Vector2(2.02f, 1.25f), "TexturedRegularAlpha");

                // Desc tile.
                quad.Render(shared.camera, whiteHighlight, new Vector4(0.6f, 1.0f, 0.8f, 0.2f), 1.0f, new Vector2(1.15f, 1.85f), new Vector2(6.52f, 0.7f), "TexturedRegularAlpha");

                // Examples tile.
                quad.Render(shared.camera, blackHighlight, Vector4.One, 1.0f, new Vector2(0.0f, 0.3f), new Vector2(8.82f, 2.0f), "TexturedRegularAlpha");

                device.BlendState = BlendState.AlphaBlend;

                // Actor Icon.
                if (shared.actorHelp.upid != null)
                {
                    Texture2D actorImage = CardSpace.Cards.CardFaceTexture(shared.actorHelp.upid);
                    if (actorImage != null)
                    {
                        quad.Render(shared.camera, actorImage, new Vector2(-3.4f, 2.5f), new Vector2(1.8f, 1.8f), @"TexturedRegularAlpha");
                    }
                }

                // Stick Icons.
                ssquad.Render(leftStickTexture, new Vector2(181, 560), new Vector2(leftStickTexture.Width, leftStickTexture.Height), @"TexturedRegularAlpha");
                Vector2 min = new Vector2(181, 560);
                Vector2 max = min + new Vector2(leftStickTexture.Width, leftStickTexture.Height);
                shared.leftStickBox.Set(min, max);
                if (shared.descBlob.NumLines > 3)
                {
                    ssquad.Render(rightStickTexture, new Vector2(1036, 70), new Vector2(rightStickTexture.Width, rightStickTexture.Height), @"TexturedRegularAlpha");
                    min = new Vector2(1036, 70);
                    max = min + new Vector2(rightStickTexture.Width, rightStickTexture.Height);
                    shared.rightStickBox.Set(min, max);
                }

                // A button
                ssquad.Render(ButtonTextures.AButton, new Vector2(1000, 245), new Vector2(80, 80), "TexturedRegularAlpha");
                shared.chooseBox.Set(new Vector2(990 - SharedX.GetGameFont24().MeasureString(Strings.Localize("helpCard.choose")).X, 245), new Vector2(1060, 245 + 55));

                // Text labels.

                // Actor description
                // Render the new stencil mask.  Magic numbers from Photoshop.
                ssquad.RenderStencil(Vector4.One, new Vector2(310, 75), new Vector2(590, 140));

                device.DepthStencilState = depthStencilState;

                // Description.
                if (shared.descBlob != null)
                {
                    Vector2 pos = new Vector2(shared.descMargin, shared.descTop + shared.descOffset);
                    pos.X = 450;
                    shared.descBlob.RenderText(null, pos, greyTextColor, maxLines: 3);
                }

                // Restore DepthStencilState to default.
                device.DepthStencilState = DepthStencilState.Default;

                batch.Begin();

                // Actor name
                TextHelper.DrawString(SharedX.GetGameFont30Bold, shared.curActorName, new Vector2(450, 30) + shadowOffset, shadowTextColor);
                TextHelper.DrawString(SharedX.GetGameFont30Bold, shared.curActorName, new Vector2(450, 30), greyTextColor);
                
                // Create
                TextHelper.DrawString(SharedX.GetGameFont24Bold, Strings.Localize("helpCard.create"), new Vector2(268, 250), whiteTextColor);
                
                // Choose
                TextHelper.DrawString(SharedX.GetGameFont24, Strings.Localize("helpCard.choose"), new Vector2(990 - SharedX.GetGameFont24().MeasureString(Strings.Localize("helpCard.choose")).X, 250), greyTextColor);

                batch.End();


                // Now render the contents of the rt1k texture but with the edges blended using the mask.
                Vector4 limits = new Vector4(0.4f, 0.41f, 0.857f, 0.93f);
                ssquad.RenderWithYLimits(rt1k, limits, new Vector2((rtFull.Width - rt1k.Width) / 2, 0), new Vector2(rt1k.Width, rtFull.Height), @"TexturedPreMultAlpha");

                // Restore rt for final rendering.
                InGame.RestoreRenderTarget();

                device.Clear(ClearOptions.DepthBuffer, Color.Pink, 1.0f, 0);

                // Start by using the blurred version of the scene as a backdrop.
                if (!shared.thumbnail.GraphicsDevice.IsDisposed)
                {
                    //InGame.Clear(Color.Transparent);
                    ssquad.Render(shared.thumbnail, Vector2.Zero, new Vector2(device.Viewport.Width, device.Viewport.Height), @"TexturedNoAlpha");
                }
                else
                {
                    Color backgroundColor = new Color(16, 66, 52);  // 1/4 strength turquoise.
                    InGame.Clear(backgroundColor);
                }

                // Copy the rendered scene to the rendertarget.
                float rtAspect = rtSize.X / rtSize.Y;
                Vector2 position = Vector2.Zero;
                Vector2 newSize = screenSize;

                newSize.X = rtAspect * newSize.Y;
                position.X = (screenSize.X - newSize.X) / 2.0f;

                ssquad.Render(rtFull, position + BokuGame.ScreenPosition, newSize, @"TexturedRegularAlpha");

            }   // end of AddItemHelpCard RenderObj Render()

            #endregion

            #region Internal

            public override void Activate()
            {
            }

            public override void Deactivate()
            {
            }

            /// <summary>
            /// Helper function to save some typing...
            /// </summary>
            /// <param name="tex"></param>
            /// <param name="path"></param>
            public void LoadTexture(ref Texture2D tex, string path)
            {
                if (tex == null)
                {
                    tex = KoiLibrary.LoadTexture2D(path);
                }
            }   // end of LoadTexture()

            public void LoadContent(bool immediate)
            {
            }   // end of LoadContent()

            public void InitDeviceResources(GraphicsDevice device)
            {
                // Init the effect.
                if (effect == null)
                {
                    effect = KoiLibrary.LoadEffect(@"Shaders\UI2D");
                    ShaderGlobals.RegisterEffect("UI2D", effect);
                }

                if (rightStickTexture == null)
                {
                    rightStickTexture = KoiLibrary.LoadTexture2D(@"Textures\HelpCard\RightStick");
                }
                if (leftStickTexture == null)
                {
                    leftStickTexture = KoiLibrary.LoadTexture2D(@"Textures\HelpCard\LeftStick");
                }
                if (glassTileHighlight == null)
                {
                    glassTileHighlight = KoiLibrary.LoadTexture2D(@"Textures\HelpCard\GlassTileHighlight");
                }
                if (whiteHighlight == null)
                {
                    whiteHighlight = KoiLibrary.LoadTexture2D(@"Textures\GridElements\WhiteHighlight");
                }
                if (blackHighlight == null)
                {
                    blackHighlight = KoiLibrary.LoadTexture2D(@"Textures\HelpCard\BlackHighlight");
                }
                if (normalMap == null)
                {
                    normalMap = KoiLibrary.LoadTexture2D(@"Textures\UI2D\FlatNormalMap");
                }

                glassTile = new Base9Grid(2.2f, 2.2f, edgeSize);
                descTile = new Base9Grid(6.7f, 2.2f, edgeSize);
                examplesTile = new Base9Grid(9.0f, 5.0f, edgeSize);

                BokuGame.Load(glassTile);
                BokuGame.Load(descTile);
                BokuGame.Load(examplesTile);

            }   // end of InitDeviceResources()

            public void UnloadContent()
            {
                DeviceResetX.Release(ref effect);

                BokuGame.Unload(glassTile);
                glassTile = null;
                BokuGame.Unload(descTile);
                descTile = null;
                BokuGame.Unload(examplesTile);
                examplesTile = null;

                DeviceResetX.Release(ref rightStickTexture);
                DeviceResetX.Release(ref leftStickTexture);
                DeviceResetX.Release(ref glassTileHighlight);
                DeviceResetX.Release(ref whiteHighlight);
                DeviceResetX.Release(ref blackHighlight);
                DeviceResetX.Release(ref normalMap);
            }   // end of AddItemHelpCard RenderObj UnloadContent()

            public void DeviceReset(GraphicsDevice device)
            {
            }

            #endregion

        }   // end of class AddItemHelpCard RenderObj     

        #region Members

        public static AddItemHelpCard Instance = null;

        /// <summary>
        /// We need to have a ref to the parent PieSelector since, if we paste in
        /// a line of example code, we also need to deactivate the pie selector.
        /// </summary>
        public PieSelector parent = null;

        // List objects.
        protected Shared shared = null;
        protected RenderObj renderObj = null;
        protected UpdateObj updateObj = null;

        private enum States
        {
            Inactive,
            Active,
        }
        private States state = States.Inactive;

        private CommandMap commandMap = new CommandMap("AddItemHelpCard");  // Placeholder for stack.

        #endregion

        #region Accessors

        public bool Active
        {
            get { return (state == States.Active); }
        }

        /// <summary>
        /// The GameActor to be programmed.  Should be set after 
        /// the actor has been created and added to the scene.
        /// </summary>
        public GameActor Actor
        {
            set { shared.actor = value; }
        }

        #endregion

        #region Public

        // c'tor
        public AddItemHelpCard()
        {
            AddItemHelpCard.Instance = this;

            shared = new Shared(this);

            // Create the RenderObject and UpdateObject parts of this mode.
            updateObj = new UpdateObj(this, shared);
            renderObj = new RenderObj(shared);

        }   // end of AddItemHelpCard c'tor

        public void OnSelect(UIGrid grid)
        {
            // We should never actually get here.  The AddItemHelpCard UpdateObj 
            // should consume all 'A' presses before the grids get them...

            Debug.Assert(false);

        }   // end of OnSelect()

        public void OnCancel(UIGrid grid)
        {
            // We should never actually get here.  The AddItemHelpCard UpdateObj 
            // should consume all 'B' presses before the grids get them...

            Debug.Assert(false);

        }   // end of OnCancel()

        public void Update()
        {
            if (Active)
            {
                updateObj.Update();
            }
        }   // end of Update()

        public void Render(Camera camera)
        {
            if (Active)
            {
                renderObj.Render(camera);
            }
        }   // end of Render()

        #endregion

        #region Internal

        override public void Activate()
        {
            Activate(null, null, null);
        }

        private object timerInstrument = null;

        /// <summary>
        /// Shortcut to get a description from a typeName.  Used by the AddItemHelpMenu.
        /// </summary>
        /// <param name="typeName"></param>
        /// <returns></returns>
        public string GetHelpDescription(string typeName)
        {
            string result = null;

            if (typeName != null)
            {
                ActorHelp actorHelp = Help.GetActorHelp(typeName);
                if (actorHelp != null)
                {
                    result = actorHelp.description;
                }
            }

            return result;
        }   // end of GetHelpDescription()

        /// <summary>
        /// Activates the AddItem help card.
        /// </summary>
        /// <param name="parent">Parent pie selector.</param>
        /// <param name="typeName">This is the string that identifies the type of the object we're getting help for.  This is used to get the correct help data from the Help class.</param>
        /// <param name="objectName">This the displayed name of the object/actor.  This comes from the Strings class and may be localized.</param>
        public void Activate(PieSelector parent, string typeName, string objectName)
        {
            ToolTipManager.Clear();

            if (typeName == null || objectName == null)
                return;

            this.parent = parent;

            if (state != States.Active)
            {
                // Ensure we have valid help before activating.
                shared.actorHelp = Help.GetActorHelp(typeName);

                if (shared.actorHelp == null || shared.actorHelp.upid == null)
                    return;

                // Do stack handling here.  If we do it in the update object we have no
                // clue which order things get pushed and popped and madness ensues.
                CommandStack.Push(commandMap);

                state = States.Active;

                shared.curActorName = objectName;
                if (shared.actorHelp.description != null)
                {
                    shared.descBlob.RawText = shared.actorHelp.description.Trim();
                }

                shared.SetUpGrid();

                if (shared.examplesGrid != null)
                {
                    shared.examplesGrid.Active = true;
                }

                // Always start the description at the beginning.
                shared.topLine = 0;
                shared.descOffset = 0;

                // Get the current scene thumbnail.
                shared.thumbnail = InGame.inGame.SmallThumbNail;

                // Tell InGame we're using the thumbnail so no need to do full render.
                InGame.inGame.RenderWorldAsThumbnail = true;

                HelpOverlay.Push(@"HelpCardAddItem");

                timerInstrument = Instrumentation.StartTimer(Instrumentation.TimerId.AddItemHelpCards);
            }
        }   // end of Activate

        override public void Deactivate()
        {
            if (state != States.Inactive)
            {
                // Do stack handling here.  If we do it in the update object we have no
                // clue which order things get pushed and popped and madness ensues.
                CommandStack.Pop(commandMap);

                state = States.Inactive;
                if (shared.examplesGrid != null)
                {
                    shared.examplesGrid.Active = false;
                }

                InGame.inGame.RenderWorldAsThumbnail = false;

                HelpOverlay.Pop();

                Instrumentation.StopTimer(timerInstrument);
            }
        }

        public override bool Refresh(List<UpdateObject> updateList, List<RenderObject> renderList)
        {
            Debug.Assert(false, "This object is not designed to be put into any lists.");
            return true;
        }   // end of Refresh()

        public void LoadContent(bool immediate)
        {
            BokuGame.Load(renderObj, immediate);

        }   // end of AddItemHelpCard LoadContent()

        public void InitDeviceResources(GraphicsDevice device)
        {
            BokuGame.Load(shared, true);    // This needs to be done after the aux menus are set up.
        }

        public void UnloadContent()
        {
            // Always deactivate help and the pie menu on device reset.
            Deactivate();

            BokuGame.Unload(shared);
            BokuGame.Unload(renderObj);
        }   // end of AddItemHelpCard UnloadContent()


        public void DeviceReset(GraphicsDevice device)
        {
            // Always deactivate help and the pie menu on device reset.
            Deactivate();

            BokuGame.DeviceReset(shared, device);
            BokuGame.DeviceReset(renderObj, device);
        }

        #endregion

    }   // end of class AddItemHelpCard

}   // end of namespace Boku
