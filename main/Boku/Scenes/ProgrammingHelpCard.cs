
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
    /// Help card for display while user is in programming editor.
    /// </summary>
    public class ProgrammingHelpCard : GameObject, INeedsDeviceReset
    {
        protected class Shared : INeedsDeviceReset
        {

            #region Members

            public ProgrammingHelpCard parent = null;
            public Camera camera = null;
            public Camera camera1k = null;      // Camera for rendering to the 1024x768 rt.
            public Texture2D thumbnail = null;  // Scene image for background.

            // The menu grids.
            public UIGrid tilesGrid = null;     // The tiles across the top of the screen.
            public UIGrid examplesGrid = null;  // The list of example reflexes.
            public int maxExamples = 20;        // Arbitrary.

            public int curTileIndex = -1;       // Index into tilesGrid for active tile.
            public float unselectedSize = 1.0f; // Size of tiles in tilesGrid.
            public float selectedSize = 1.0f;

            public TextBlob descBlob = null;
            public int topLine = 0;
            public float descOffset = 0.0f;     // Offset due to scrolling.

            UIGridElement.ParamBlob tilesBlob = null;
            UIGridElement.ParamBlob examplesBlob = null;

            public AABB2D leftStickBox = new AABB2D();      // Mouse hit boxes.
            public AABB2D rightStickBox = new AABB2D();
            public AABB2D insertBox = new AABB2D();
            public AABB2D backBox = new AABB2D();
            public AABB2D overallBox = new AABB2D();        // Overall help region.

            #endregion

            #region Accessors
            #endregion

            #region Public

            // c'tor
            public Shared(ProgrammingHelpCard parent)
            {
                this.parent = parent;

                GraphicsDevice device = KoiLibrary.GraphicsDevice;

                descBlob = new TextBlob(KoiX.SharedX.GetGameFont24, "replace me", 700);
                descBlob.LineSpacingAdjustment = -5;

                // We're rendering the camera specific parts into a 1024x768 rendertarget and
                // then copying (with masking) into the 1280x720 rt and finally cropping it 
                // as needed for 4:3 display.
                camera = new PerspectiveUICamera();
                camera.Resolution = new Point(1280, 720);
                camera1k = new PerspectiveUICamera();
                camera1k.Resolution = new Point(1024, 768);

                // Create tiles grid.
                tilesGrid = new UIGrid(parent.OnSelect, parent.OnCancel, new Point(20, 1), @"ProgrammingHelpCard.TilesGrid");
                Matrix mat = Matrix.CreateTranslation(0.0f, 2.5f, 0.0f);
                tilesGrid.LocalMatrix = mat;
                tilesGrid.AlwaysReadInput = true;   // The examples grid and this grid will need to be able to handle input simultaniously.

                // Create examples grid.
                examplesGrid = new UIGrid(parent.OnSelect, parent.OnCancel, new Point(1, maxExamples), @"ProgrammingHelpCard.ExamplesGrid");
                mat = Matrix.CreateTranslation(-2.625f, -0.5f, 0.0f);
                examplesGrid.LocalMatrix = mat;
                examplesGrid.Scrolling = true;
                examplesGrid.UseRightStick = false;
                examplesGrid.UseLeftStick = true;
                examplesGrid.UseDPad = false;

                // Set up the blob for info common to all tiles.
                tilesBlob = new UIGridElement.ParamBlob();
                tilesBlob.width = 1.0f;
                tilesBlob.height = 1.0f;
                tilesBlob.edgeSize = 0.1f;
                tilesBlob.selectedColor = new Color(246, 236, 75, 255);     // Pale yellow.
                tilesBlob.unselectedColor = new Color(246, 236, 75, 255);
                tilesBlob.textColor = Color.White;
                tilesBlob.dropShadowColor = Color.Black;
                tilesBlob.useDropShadow = true;
                tilesBlob.invertDropShadow = false;
                tilesBlob.normalMapName = @"QuarterRound4NormalMap";
                tilesBlob.greyFlatShader = true;
                tilesBlob.ignorePowerOf2 = true;

                examplesBlob = new UIGridElement.ParamBlob();
                examplesBlob.width = 8.5f;
                examplesBlob.height = 1.5f;
                examplesBlob.edgeSize = 0.2f;
                examplesBlob.selectedColor = Color.Transparent;
                examplesBlob.unselectedColor = Color.Transparent;
                examplesBlob.textColor = new Color(127, 127, 127);
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

            public void SetUpGrids(ProgrammingElement focusElement)
            {
                //
                // Get the info about the current reflex.
                //
                Editor editor = InGame.inGame.Editor;
                GameActor actor = editor.GameActor;
                ReflexData reflexData = editor.ActivePanel.Reflex.Copy();

                // Add focusElement tile to reflexData.
                if (focusElement != null)
                {
                    int focusIndex = editor.ActivePanel.ActiveCard;

                    // Since there is no single list of elements we need to 
                    // just work our way through the reflex trying to figure 
                    // out where the new tile should go.

                    for (; ; ) // Fake a loop just to give us something to break out of.
                    {
                        // Sensor?
                        if (focusElement as Sensor != null)
                        {
                            reflexData.Sensor = (Sensor)focusElement;
                            break;
                        }
                        if (reflexData.Sensor != null && !(reflexData.Sensor is NullSensor))
                        {
                            focusIndex--;
                        }
                        focusIndex--;

                        // Filter?
                        if (focusElement as Filter != null)
                        {
                            // Replace an existing filter?
                            if (focusIndex < reflexData.Filters.Count)
                            {
                                reflexData.Filters[focusIndex] = (Filter)focusElement;
                            }
                            else
                            {
                                reflexData.Filters.Add((Filter)focusElement);
                            }
                            break;
                        }
                        focusIndex -= reflexData.Filters.Count + 1;     // Also account for the blank at the end of the line...

                        // Actuator?
                        if (focusElement as Actuator != null)
                        {
                            reflexData.Actuator = (Actuator)focusElement;
                            break;
                        }
                        focusIndex--;

                        // Selector?
                        if (focusElement as Selector != null)
                        {
                            reflexData.Selector = (Selector)focusElement;
                            break;
                        }
                        // This can happen if we're replacing a Selector with a Modifier.  
                        // It's kind of questionable whether or not that is a valid thing
                        // to do but since the system currently supports doing so we have
                        // to be able to handle the case.
                        if (focusIndex == 0)
                        {
                            reflexData.Selector = null;
                        }
                        if (reflexData.Selector != null)
                        {
                            focusIndex--;
                        }

                        // Modifiers
                        if (focusElement as Modifier != null)
                        {
                            // Replace an existing modifier?
                            if (focusIndex >= 0 && focusIndex < reflexData.Modifiers.Count)
                            {
                                reflexData.Modifiers[focusIndex] = (Modifier)focusElement;
                            }
                            else
                            {
                                reflexData.Modifiers.Add((Modifier)focusElement);
                            }
                            break;
                        }

                        Debug.Assert(false);

                    }   // end of infinite loop.

                }

                //
                // Set up tilesGrid
                //

                // Clear out any existing tiles.
                tilesGrid.Clear();

                // Populate the grid with tiles for each entry in the reflex.
                int index = 0;
                UIGrid2DDualTextureElement e = null;

                // Sensor
                if (reflexData.Sensor != null)
                {
                    e = new UIGrid2DDualTextureElement(tilesBlob, reflexData.sensorUpid);
                    e.Scale = unselectedSize;
                    e.Tag = reflexData.Sensor;
                    tilesGrid.Add(e, index++, 0);
                }

                // Filter(s)
                if (reflexData.filterUpids != null)
                {
                    for (int i = 0; i < reflexData.filterUpids.Length; i++)
                    {
                        e = new UIGrid2DDualTextureElement(tilesBlob, reflexData.filterUpids[i]);
                        e.Scale = unselectedSize;
                        e.Tag = reflexData.Filters[i];
                        tilesGrid.Add(e, index++, 0);
                    }
                }

                // Actuator
                if (reflexData.Actuator != null)
                {
                    e = new UIGrid2DDualTextureElement(tilesBlob, reflexData.actuatorUpid);
                    e.Scale = unselectedSize;
                    e.Tag = reflexData.Actuator;
                    tilesGrid.Add(e, index++, 0);
                }

                // Selector
                if (reflexData.Selector != null)
                {
                    e = new UIGrid2DDualTextureElement(tilesBlob, reflexData.selectorUpid);
                    e.Scale = unselectedSize;
                    e.Tag = reflexData.Selector;
                    tilesGrid.Add(e, index++, 0);
                }

                // Modifier(s)
                if (reflexData.modifierUpids != null)
                {
                    for (int i = 0; i < reflexData.modifierUpids.Length; i++)
                    {
                        e = new UIGrid2DDualTextureElement(tilesBlob, reflexData.modifierUpids[i]);
                        e.Scale = unselectedSize;
                        e.Tag = reflexData.Modifiers[i];
                        tilesGrid.Add(e, index++, 0);
                    }
                }

                // Figure out which should be active.
                tilesGrid.SelectionIndex = new Point(0, 0);
                for (int i = 0; i < tilesGrid.ActualDimensions.X; i++)
                {
                    if (tilesGrid.Get(i, 0).Tag == focusElement)
                    {
                        tilesGrid.SelectionIndex = new Point(i, 0);
                        break;
                    }
                }
                // Force a re-size to start.
                curTileIndex = -1;

                // Allow the geometry for the tiles to load.
                tilesGrid.LoadContent(true);


                //
                // Set up samplesGrid
                //

                SetUpExamplesGrid();

            }   // end of SetUpGrids()

            /// <summary>
            /// Sets up the examples grid.  Broken out seperately since this
            /// needs to be done more often that setting up the tilesGrid.
            /// </summary>
            public void SetUpExamplesGrid()
            {
                // Clear out any existing examples.
                examplesGrid.Clear();

                Editor editor = InGame.inGame.Editor;
                GameActor actor = editor.GameActor;
                ReflexData reflexData = editor.ActivePanel.Reflex.Copy();

                // Get the list of examples.
                UIGridElement gridElement = tilesGrid.SelectionElement;
                ProgrammingElement selected = gridElement != null ? (ProgrammingElement)gridElement.Tag : null;
                Help.RankAndSortProgrammingExamples(actor, reflexData, selected);
                List<ExamplePage> exampleList = Help.ProgrammingExamples;

                examplesBlob.Font = KoiX.SharedX.GetGameFont20;

                UIGrid2DExampleElement te = null;
                int index = 0;
                for (int i = 0; i < Math.Min(exampleList.Count, maxExamples); i++)
                {
                    ExamplePage example = exampleList[i];

                    // If we've run out of good examples, bail.  But always let at least 1 through.
                    if (example.rank < 0 && index > 0)
                        break;

                    // For now, ignore multi-line examples.
                    if (example.reflexes.Length != 1)
                        continue;

                    te = new UIGrid2DExampleElement(examplesBlob, example);
                    te.Tag = null;

                    examplesGrid.Add(te, 0, index++);
                }

                // Start with the top one in focus.
                examplesGrid.SelectionIndex = new Point(0, 0);

                // Allow the geometry for the examples to load.
                examplesGrid.LoadContent(true);

            }   // end of SetUpExamplesGrid()

            public void LoadContent(bool immediate)
            {
                BokuGame.Load(tilesGrid, immediate);
                BokuGame.Load(examplesGrid, immediate);
            }   // end of ProgrammingHelpCard Shared LoadContent()

            public void InitDeviceResources(GraphicsDevice device)
            {
            }   // end of InitDeviceResources()

            public void UnloadContent()
            {
                BokuGame.Unload(tilesGrid);
                BokuGame.Unload(examplesGrid);
            }   // end of ProgrammingHelpCard Shared UnloadContent()

            /// <summary>
            /// Recreate render targets
            /// </summary>
            /// <param name="graphics"></param>
            public void DeviceReset(GraphicsDevice device)
            {
                BokuGame.DeviceReset(tilesGrid, device);
                BokuGame.DeviceReset(examplesGrid, device);
            }

            #endregion

        }   // end of class Shared

        protected class UpdateObj : UpdateObject
        {
            #region Members

            private ProgrammingHelpCard parent = null;
            private Shared shared = null;

            #endregion

            #region Public

            public UpdateObj(ProgrammingHelpCard parent, Shared shared)
            {
                this.parent = parent;
                this.shared = shared;
            }

            public override void Update()
            {
                // Our children may have input focus but we can still steal away the buttons we care about.
                GamePadInput pad = GamePadInput.GetGamePad0();

                int maxLines = 3;
                bool changed = false;
                int numLines = shared.descBlob != null ? shared.descBlob.NumLines : 0;

                parent.touched = false;

                // TouchInupt
                if (KoiLibrary.LastTouchedDeviceIsTouch && TouchInput.TouchCount > 0)
                {
                    parent.touched = true;

                    TouchContact touch = TouchInput.GetOldestTouch();
                    Debug.Assert(null != touch);

                    // Check if user clicked on any of the tiles across the top of the page.
                    for (int i = 0; i < shared.tilesGrid.ActualDimensions.X; i++)
                    {
                        UIGrid2DDualTextureElement e = shared.tilesGrid.Get(i, 0) as UIGrid2DDualTextureElement;
                        Matrix mat = e.InvWorldMatrix;
                        Vector2 hitUV = LowLevelMouseInput.GetHitUV(shared.camera, ref mat, e.Width, e.Height, true);
                        if (hitUV.X > 0 && hitUV.X < 1 && hitUV.Y > 0 && hitUV.Y < 1)
                        {
                            if (TouchInput.WasTouched)
                            {
                                touch.TouchedObject = shared.tilesGrid.SelectionElement;
                            }
                            if (TouchInput.WasReleased && touch.TouchedObject == shared.tilesGrid.SelectionElement)
                            {
                                int steps = i - shared.tilesGrid.SelectionIndex.X;
                                while (steps != 0)
                                {
                                    if (steps > 0)
                                    {
                                        shared.tilesGrid.MoveRight();
                                        --steps;
                                    }
                                    else
                                    {
                                        shared.tilesGrid.MoveLeft();
                                        ++steps;
                                    }
                                }
                            }
                        }
                    }


                    Vector2 touchHit = TouchInput.GetAspectRatioAdjustedPosition(touch.position, shared.camera, true);

                    // InsertExample
                    if (shared.insertBox.Touched(touch, touchHit))
                    {
                        Insert();
                    }

                    // Back
                    if (shared.backBox.Touched(touch, touchHit))
                    {
                        touch.TouchedObject = this;
                        // Done
                        parent.Deactivate();
                    }

                    // Scroll examples
                    if (shared.leftStickBox.Touched(touch, touchHit))
                    {
                        // Up?
                        if (touchHit.Y < (shared.leftStickBox.Min.Y + shared.leftStickBox.Max.Y) / 2.0f)
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


                    // Scroll description
                    if (shared.descBlob != null && shared.rightStickBox.Touched(touch, touchHit))
                    {
                        // Up?
                        if (touchHit.Y < (shared.rightStickBox.Min.Y + shared.rightStickBox.Max.Y) / 2.0f)
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

                    // If the user clicked outside of the help region, assume he wants to exit.
                    if (TouchInput.WasTouched && !shared.overallBox.Contains(touchHit))
                    {
                        parent.Deactivate();
                    }

                }

                if (KoiLibrary.LastTouchedDeviceIsKeyboardMouse)
                // MouseInput
                {
                    // Check if user clicked on any of the tiles across the top of the page.
                    for (int i = 0; i < shared.tilesGrid.ActualDimensions.X; i++)
                    {
                        UIGrid2DDualTextureElement e = shared.tilesGrid.Get(i, 0) as UIGrid2DDualTextureElement;
                        Matrix mat = e.InvWorldMatrix;
                        Vector2 hitUV = LowLevelMouseInput.GetHitUV(shared.camera, ref mat, e.Width, e.Height, true);
                        if (hitUV.X > 0 && hitUV.X < 1 && hitUV.Y > 0 && hitUV.Y < 1)
                        {
                            if (LowLevelMouseInput.Left.WasPressed)
                            {
                                MouseInput.ClickedOnObject = shared.tilesGrid.SelectionElement;
                            }
                            if (LowLevelMouseInput.Left.WasReleased && MouseInput.ClickedOnObject == shared.tilesGrid.SelectionElement)
                            {
                                int steps = i - shared.tilesGrid.SelectionIndex.X;
                                while (steps != 0)
                                {
                                    if (steps > 0)
                                    {
                                        shared.tilesGrid.MoveRight();
                                        --steps;
                                    }
                                    else
                                    {
                                        shared.tilesGrid.MoveLeft();
                                        ++steps;
                                    }
                                }
                            }
                        }
                    }


                    Vector2 mouseHit = LowLevelMouseInput.GetAspectRatioAdjustedPosition(shared.camera, true);

                    // InsertExample
                    if (shared.insertBox.LeftPressed(mouseHit))
                    {
                        Insert();
                    }

                    // Back
                    if (shared.backBox.LeftPressed(mouseHit))
                    {
                        // Done
                        parent.Deactivate();
                    }

                    // Scroll examples
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

                    // Scroll Examples via mouse wheel
                    int scroll = LowLevelMouseInput.DeltaScrollWheel;
                    if (scroll > 0)
                    {
                        shared.examplesGrid.MoveUp();
                    }
                    else if (scroll < 0)
                    {
                        shared.examplesGrid.MoveDown();
                    }

                    // Scroll description
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

                    // If the user clicked outside of the help region, assume he wants to exit.
                    if (LowLevelMouseInput.Left.WasPressed && !shared.overallBox.Contains(mouseHit))
                    {
                        parent.Deactivate();
                    }

                }   // end of mouse input

                if (Actions.Cancel.WasPressed)
                {
                    Actions.Cancel.ClearAllWasPressedState();

                    // Done
                    parent.Deactivate();
                }

                if (Actions.Select.WasPressed)
                {
                    Actions.Select.ClearAllWasPressedState();

                    Insert();
                }

                if (shared.descBlob != null)
                {
                    if (Actions.RightUp.WasPressedOrRepeat)
                    {
                        if (shared.topLine > 0)
                        {
                            --shared.topLine;
                            changed = true;
                        }
                    }

                    if (Actions.RightDown.WasPressedOrRepeat)
                    {
                        if (numLines - maxLines > shared.topLine)
                        {
                            ++shared.topLine;
                            changed = true;
                        }
                    }

                    if (changed)
                    {
                        // Start a twitch to move the description text offset.
                        TwitchManager.Set<float> set = delegate(float val, Object param) { shared.descOffset = (int)val; };
                        TwitchManager.CreateTwitch<float>(shared.descOffset, -shared.topLine * shared.descBlob.TotalSpacing, set, 0.2f, TwitchCurve.Shape.OvershootOut);
                    }
                }

                // If we're not shutting down, update the child grids.
                if (parent.Active)
                {
                    Matrix world = Matrix.Identity;

                    if (shared.tilesGrid != null)
                    {
                        // If the selection has changed, we need to start some twitches
                        // to change the size of the affected tiles.
                        if (shared.tilesGrid.SelectionIndex.X != shared.curTileIndex)
                        {
                            // Do this before the twitch so that any CPU lag is done before the animation starts.
                            shared.SetUpExamplesGrid();

                            // Shrink unselected tile to smaller size.
                            if (shared.curTileIndex != -1)
                            {
                                UIGrid2DDualTextureElement e = (UIGrid2DDualTextureElement)shared.tilesGrid.Get(shared.curTileIndex, 0);
                                if (e != null)
                                {
                                    TwitchManager.Set<float> set = delegate(float val, Object param) { e.Scale = val; shared.tilesGrid.Dirty = true; };
                                    TwitchManager.CreateTwitch<float>(e.Scale, shared.unselectedSize, set, 0.15f, TwitchCurve.Shape.EaseInOut);
                                }
                            }

                            {
                                UIGrid2DDualTextureElement e = (UIGrid2DDualTextureElement)shared.tilesGrid.SelectionElement;
                                if (e != null)
                                {
                                    TwitchManager.Set<float> set = delegate(float val, Object param) { e.Scale = val; shared.tilesGrid.Dirty = true; };
                                    TwitchManager.CreateTwitch<float>(e.Scale, shared.selectedSize, set, 0.15f, TwitchCurve.Shape.EaseInOut);
                                }
                            }

                            shared.curTileIndex = shared.tilesGrid.SelectionIndex.X;
                            if (shared.tilesGrid.SelectionElement != null)
                            {
                                string str = CardSpace.Cards.GetHelpDescription(((ProgrammingElement)shared.tilesGrid.SelectionElement.Tag).upid);
                                shared.descBlob.RawText = str.Trim();
                            }
                            else
                            {
                                // TODO (****) what description should we have for an empty reflex?  Maybe something talking about 'when' and 'do'?
                                shared.descBlob.RawText = CardSpace.Cards.GetHelpDescription(@"huh?");
                            }
                            shared.descOffset = 0;
                            shared.topLine = 0;
                        }

                        shared.tilesGrid.Update(ref world);
                    }

                    if (shared.examplesGrid != null)
                    {
                        shared.examplesGrid.Update(ref world);
                    }

                }   // end if not shutting down.

            }   // end of Update()

            #endregion

            #region Internal

            /// <summary>
            /// Insert the current example reflex.
            /// </summary>
            private void Insert()
            {
                // Insert a new reflex below the current one.
                Editor editor = InGame.inGame.Editor;
                ReflexPanel panel = editor.ActivePanel;

                panel.InsertReflex();

                // The newly inserted panel should become the active one.
                panel = editor.ActivePanel;

                // Get the reflex data from the current example.
                UIGrid2DExampleElement e = (UIGrid2DExampleElement)shared.examplesGrid.SelectionElement;
                ReflexData clip = e.Example.reflexes[0];

                // Paste the example code into this new panel and tell it to rebuild.
                panel.Reflex.Paste(clip);
                panel.uiRebuild = true;

                // Wake up the cursor.
                UiCursor.ActiveCursor.Activate();

                // Now close the pie selector (if that was our parent) and return to the programming UI.
                // parent will be null if we were launched from the programming UI.
                if (parent.parent != null)
                {
                    parent.parent.Deactivate();

                    // But, the pie selectors may be nested.  So crawl around and see if we can find them all.
                    // Nested pie selectors are hosted on billboards which in turn are hosted on slices of 
                    // other pie selectors.  Yes, it's turtles all the way down.
                    Billboard bb = parent.parent.Parent as Billboard;
                    while (bb != null)
                    {
                        PieSelector.RenderObjSlice slice = bb.Parent as PieSelector.RenderObjSlice;
                        bb = null;
                        if (slice != null)
                        {
                            PieSelector pie = slice.transformParent as PieSelector;
                            if (pie != null)
                            {
                                pie.Deactivate();
                                bb = pie.Parent as Billboard;
                            }
                        }
                    }
                }

                Instrumentation.IncrementCounter(Instrumentation.CounterId.ProgrammingHelpCardInsertExample);

                parent.Deactivate();

            }   // end of Insert()

            public override void Activate()
            {
            }

            public override void Deactivate()
            {
            }

            #endregion

        }   // end of class ProgrammingHelpCard UpdateObj  

        protected class RenderObj : RenderObject, INeedsDeviceReset
        {
            #region Members

            private Shared shared;

            public Texture2D backgroundTexture = null;    // The background frame we render over.  Includes the stick images.
            public Texture2D rightStickTexture = null;
            public Texture2D leftStickTexture = null;

            public GetFont Font = SharedX.GetGameFont20;

            private DepthStencilState depthStencilWrite;
            private DepthStencilState depthStencilRead;

            #endregion

            #region Public

            public RenderObj(Shared shared)
            {
                this.shared = shared;

                depthStencilWrite = new DepthStencilState();
                depthStencilWrite.StencilEnable = true;
                depthStencilWrite.StencilFunction = CompareFunction.NotEqual;
                depthStencilWrite.ReferenceStencil = 0;

                depthStencilRead = new DepthStencilState();
                depthStencilRead.StencilEnable = true;
                depthStencilRead.StencilFunction = CompareFunction.NotEqual;
                depthStencilRead.ReferenceStencil = 0;
                depthStencilRead.StencilWriteMask = 0x00;
            }

            public override void Render(Camera camera)
            {
                GraphicsDevice device = KoiLibrary.GraphicsDevice;

                RenderTarget2D rtFull = SharedX.RenderTargetDepthStencil1280_720;   // Rendertarget we render whole display into.
                RenderTarget2D rt1k = SharedX.RenderTargetDepthStencil1024_768;

                Vector2 screenSize = BokuGame.ScreenSize;
                Vector2 rtSize = new Vector2(rtFull.Width, rtFull.Height);

                CameraSpaceQuad csquad = CameraSpaceQuad.GetInstance();
                ScreenSpaceQuad ssquad = ScreenSpaceQuad.GetInstance();

                Color darkTextColor = new Color(20, 20, 20);
                Color greyTextColor = new Color(127, 127, 127);
                Color greenTextColor = new Color(8, 123, 110);
                Color whiteTextColor = new Color(255, 255, 255);
                Color shadowTextColor = new Color(240, 240, 240);
                Vector2 shadowOffset = new Vector2(0, 6);

                // Render the description text and examples into the 1k rendertarget.
                InGame.SetRenderTarget(rt1k);
                InGame.Clear(Color.Transparent);

                // Set up params for rendering UI with this camera.
                BokuGame.bokuGame.shaderGlobals.SetCamera(shared.camera1k);



                //
                // Render the samples grid.
                //
                if (shared.examplesGrid != null)
                {
                    shared.examplesGrid.Render(shared.camera1k);
                }

                /*
                //
                // Description text.
                //
                // Clear area where text will go.
                ssquad.Render(Vector4.Zero, new Vector2(132, 175), new Vector2(760, 110), "SolidColorNoAlpha");

                SpriteBatch batch = KoiLibrary.SpriteBatch;
                SpriteFont font = Font();

                // Description.
                if (shared.descBlob != null)
                {
                    Vector2 pos = new Vector2(150, 171 + shared.descOffset);
                    if (shared.descBlob.NumLines == 1)
                    {
                        pos.Y += shared.descBlob.TotalSpacing;
                    }
                    if (shared.descBlob.NumLines == 2)
                    {
                        pos.Y += 0.5f * shared.descBlob.TotalSpacing;
                    }
                    shared.descBlob.RenderWithButtons(pos, greyTextColor, false);
                }
                */

                /*
                //
                // Description text.
                //

                // Create mask to contain description text.
                ssquad.RenderStencil(Vector4.One, new Vector2(132, 175), new Vector2(760, 110));

                device.DepthStencilState = depthStencilRead;

                SpriteBatch batch = KoiLibrary.SpriteBatch;

                shared.descBlob.RawText = "Now is the time\nfor all good men\nto come to the aid\nof their country.\nNow is the time\nfor all good men\nto come to the aid\nof their country.";

                // Description.
                if (shared.descBlob != null)
                {
                    Vector2 pos = new Vector2(150, 171 + shared.descOffset);
                    if (shared.descBlob.NumLines == 1)
                    {
                        pos.Y += shared.descBlob.TotalSpacing;
                    }
                    if (shared.descBlob.NumLines == 2)
                    {
                        pos.Y += 0.5f * shared.descBlob.TotalSpacing;
                    }
                    shared.descBlob.RenderWithButtons(pos, greyTextColor, false);
                }

                //device.RenderState.StencilEnable = false;
                device.DepthStencilState = DepthStencilState.None;

                //
                // Render the samples grid.
                //
                if (shared.examplesGrid != null)
                {
                    // Clear the stencil buffer.
                    device.Clear(ClearOptions.Stencil, Color.Transparent, 1.0f, 0);

                    // Render the new stencil mask.
                    ssquad.RenderStencil(Vector4.One, new Vector2(132, 314), new Vector2(760, 425));

                    device.DepthStencilState = depthStencilRead;

                    shared.examplesGrid.Render(shared.camera1k);

                    device.DepthStencilState = DepthStencilState.None;
                }
                */


                // Render the scene to our rendertarget.
                InGame.SetRenderTarget(rtFull);

                // Set up params for rendering UI with this camera.
                BokuGame.bokuGame.shaderGlobals.SetCamera(shared.camera);

                InGame.Clear(Color.Transparent);

                // Now render the background frames.
                ssquad.Render(backgroundTexture, new Vector2((rtFull.Width - rt1k.Width) / 2, 0), new Vector2(rt1k.Width, rtFull.Height), @"TexturedRegularAlpha");

                // Now render the contents of the rt1k texture but with the edges blended using the mask.
                Vector4 limits = new Vector4(0.48f, 0.495f, 0.88f, 0.962f);
                ssquad.RenderWithYLimits(rt1k, limits, new Vector2((rtFull.Width - rt1k.Width) / 2, 0), new Vector2(rt1k.Width, rtFull.Height), @"TexturedRegularAlpha");

                // Render the tiles grid.
                if (shared.tilesGrid != null)
                {
                    shared.tilesGrid.Render(shared.camera);
                }

                //
                // Description text.
                //

                SpriteBatch batch = KoiLibrary.SpriteBatch;

                // Description.
                if (shared.descBlob != null)
                {
                    Vector2 pos = new Vector2(280, 157);
                    if (shared.descBlob.NumLines == 1)
                    {
                        pos.Y += shared.descBlob.TotalSpacing;
                    }
                    if (shared.descBlob.NumLines == 2)
                    {
                        pos.Y += 0.5f * shared.descBlob.TotalSpacing;
                    }
                    shared.descBlob.RenderText(null, pos, greyTextColor, startLine: shared.topLine, maxLines: 3);
                }

                //
                // Add labels.
                //
                {
                    device.BlendState = SharedX.BlendStateColorWriteRGB;

                    batch.Begin();

                    Font = SharedX.GetGameFont24Bold;
                    TextHelper.DrawString(Font, Strings.Localize("helpCard.examples"), new Vector2(280, 300) + shadowOffset, shadowTextColor);
                    TextHelper.DrawString(Font, Strings.Localize("helpCard.examples"), new Vector2(280, 300), greyTextColor);

                    Font = SharedX.GetGameFont24;
                    Vector2 strSize = Font().MeasureString(Strings.Localize("helpCard.insert"));
                    // Pixel align.
                    strSize.X = (int)(strSize.X + 1);
                    strSize.Y = (int)(strSize.Y + 1);
                    Vector2 buttonSize = new Vector2(50, 0);

                    TextHelper.DrawString(Font, Strings.Localize("helpCard.insert"), new Vector2(975 - strSize.X, 300), greyTextColor);
                    Vector2 min = new Vector2(975 - strSize.X, 300);
                    Vector2 max = min + strSize + buttonSize;
                    shared.insertBox.Set(min, max);

                    strSize = Font().MeasureString(Strings.Localize("helpCard.back"));
                    // Pixel align.
                    strSize.X = (int)(strSize.X + 1);
                    strSize.Y = (int)(strSize.Y + 1);
                    TextHelper.DrawString(Font, Strings.Localize("helpCard.back"), new Vector2(975 - strSize.X, 645), greyTextColor);
                    min = new Vector2(975 - strSize.X, 645);
                    max = min + strSize + buttonSize;
                    shared.backBox.Set(min, max);

                    // We know that the back box is in the lower right hand corner of the help information.
                    // We can reflect this across the center of the screen to get the upper left hand corner
                    // and use this overall region for our overall hit box.  Any clicks outside of this region
                    // will be assumed to be the user wanting to exit.
                    min.X = rtFull.Width - max.X;
                    min.Y = rtFull.Height - max.Y;
                    shared.overallBox.Set(min, max);

                    batch.End();

                    device.BlendState = BlendState.NonPremultiplied;
                }

                //
                // Render button icons.
                //
                ssquad.Render(ButtonTextures.AButton, new Vector2(980, 305), new Vector2(56, 56), @"TexturedRegularAlpha");
                ssquad.Render(ButtonTextures.BButton, new Vector2(980, 650), new Vector2(56, 56), @"TexturedRegularAlpha");

                // Stick Icons.
                ssquad.Render(leftStickTexture, new Vector2(228, 560), new Vector2(leftStickTexture.Width, leftStickTexture.Height), @"TexturedRegularAlpha");
                shared.leftStickBox.Set(new Vector2(228, 560), new Vector2(228, 560) + new Vector2(leftStickTexture.Width, leftStickTexture.Height));

                if (shared.descBlob.NumLines > 3)
                {
                    ssquad.Render(rightStickTexture, new Vector2(989, 166), new Vector2(rightStickTexture.Width, rightStickTexture.Height), @"TexturedRegularAlpha");
                    shared.rightStickBox.Set(new Vector2(989, 166), new Vector2(989, 166) + new Vector2(rightStickTexture.Width, rightStickTexture.Height));
                }

                InGame.RestoreRenderTarget();

                device.Clear(ClearOptions.DepthBuffer, Color.Pink, 1.0f, 0);

                // Start by using the blurred version of the scene as a backdrop.
                if (!shared.thumbnail.GraphicsDevice.IsDisposed)
                {
                    //InGame.Clear(Color.TransparentBlack);
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


            }   // end of ProgrammingHelpCard RenderObj Render()

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
                if (backgroundTexture == null)
                {
                    backgroundTexture = KoiLibrary.LoadTexture2D(@"Textures\HelpCard\ProgrammingBackground");
                }

                if (rightStickTexture == null)
                {
                    rightStickTexture = KoiLibrary.LoadTexture2D(@"Textures\HelpCard\RightStick");
                }
                if (leftStickTexture == null)
                {
                    leftStickTexture = KoiLibrary.LoadTexture2D(@"Textures\HelpCard\LeftStick");
                }

            }   // end of InitDeviceResources()

            public void UnloadContent()
            {
                DeviceResetX.Release(ref backgroundTexture);
                DeviceResetX.Release(ref rightStickTexture);
                DeviceResetX.Release(ref leftStickTexture);

            }   // end of ProgrammingHelpCard RenderObj UnloadContent()

            /// <summary>
            /// Recreate render targets
            /// </summary>
            /// <param name="graphics"></param>
            public void DeviceReset(GraphicsDevice device)
            {
            }

            #endregion

        }   // end of class ProgrammingHelpCard RenderObj     

        #region Members

        public static ProgrammingHelpCard Instance = null;

        /// <summary>
        /// We need to have a ref to the parent PieSelector since, if we paste in
        /// a line of example code, we also need to deactivate the pie selector.
        /// </summary>
        public PieSelector parent = null;

        private bool touched = false;
        public bool WasTouchedThisFrame { get { return touched; } }

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

        private CommandMap commandMap = new CommandMap("ProgrammingHelpCard");  // Placeholder for stack.

        #endregion

        #region Accessors

        public bool Active
        {
            get { return (state == States.Active); }
        }

        #endregion

        #region Public

        // c'tor
        public ProgrammingHelpCard()
        {
            ProgrammingHelpCard.Instance = this;

            shared = new Shared(this);

            // Create the RenderObject and UpdateObject parts of this mode.
            updateObj = new UpdateObj(this, shared);
            renderObj = new RenderObj(shared);

        }   // end of ProgrammingHelpCard c'tor

        public void OnSelect(UIGrid grid)
        {
            // We should never actually get here.  The ProgrammingHelpCard UpdateObj 
            // should consume all 'A' presses before the grids get them...

            Debug.Assert(false);

        }   // end of OnSelect()

        public void OnCancel(UIGrid grid)
        {
            // We should never actually get here.  The ProgrammingHelpCard UpdateObj 
            // should consume all 'B' presses before the grids get them...

            Debug.Assert(false);

        }   // end of OnCancel()

        public void Update()
        {
            touched = false;

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
            Activate(null, null);
        }

        private object timerInstrument = null;

        public void Activate(ProgrammingElement focusElement, PieSelector parent)
        {
            this.parent = parent;

            if (state != States.Active)
            {
                // Do stack handling here.  If we do it in the update object we have no
                // clue which order things get pushed and popped and madness ensues.
                CommandStack.Push(commandMap);

                state = States.Active;
                shared.SetUpGrids(focusElement);

                if (shared.tilesGrid != null)
                {
                    shared.tilesGrid.Active = true;
                }
                if (shared.examplesGrid != null)
                {
                    shared.examplesGrid.Active = true;
                }

                // Get the current scene thumbnail.
                shared.thumbnail = InGame.inGame.SmallThumbNail;

                HelpOverlay.Push(@"HelpCardProgramming");

                ToolTipManager.Clear();

                timerInstrument = Instrumentation.StartTimer(Instrumentation.TimerId.ProgrammingHelpCards);
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
                if (shared.tilesGrid != null)
                {
                    shared.tilesGrid.Active = false;
                }
                if (shared.examplesGrid != null)
                {
                    shared.examplesGrid.Active = false;
                }

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

        }   // end of ProgrammingHelpCard LoadContent()

        public void InitDeviceResources(GraphicsDevice device)
        {
            BokuGame.Load(shared, true);    // This needs to be done after the aux menus are set up.
        }

        public void UnloadContent()
        {
            BokuGame.Unload(shared);
            BokuGame.Unload(renderObj);
        }   // end of ProgrammingHelpCard UnloadContent()

        /// <summary>
        /// Recreate render targets
        /// </summary>
        /// <param name="graphics"></param>
        public void DeviceReset(GraphicsDevice device)
        {
        }

        #endregion

    }   // end of class ProgrammingHelpCard

}   // end of namespace Boku
