
using System;
using System.Collections;
using System.Diagnostics;
using System.IO;
using System.Xml.Serialization;

using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Audio;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using Microsoft.Xna.Framework.Storage;

using Boku.Base;
using Boku.Fx;
using Boku.Scenes.InGame.MouseEditTools;
using Boku.SimWorld;
using Boku.SimWorld.Path;
using Boku.SimWorld.Terra;
using Boku.Common;
using Boku.Common.Gesture;
using Boku.Common.ParticleSystem;
using Boku.Common.Xml;
using Boku.Programming;
using Boku.UI;
using Boku.UI2D;
using Boku.Input;
using Boku.Audio;

#if NOTES

    TODO (mouse)  Tag things that need to be looked at with this.

    Try using the shim and pie menu from InGameEditObjectAddItem.  It should be (or could be) made publically accessible.

#endif



namespace Boku
{
    /// <summary>
    /// UpdateObject for InGame -> EditObject
    /// </summary>
    public partial class InGame : GameObject, INeedsDeviceReset
    {
        public partial class TouchEditUpdateObj : BaseEditUpdateObj
        {
            #region Members

            private ToolBar toolBar = new ToolBar();
            private TouchEditToolBox toolBox = new TouchEditToolBox();

            // This is the mode to enter upon activation.  This allows us
            // to control which mode we're in when returning from something
            // like the object tweak screen.
            private ToolMode returnMode = ToolMode.CameraMove;

            #endregion

            #region Accessors

            public ToolBar ToolBar
            {
                get { return toolBar; }
            }

            public TouchEditToolBox ToolBox
            {
                get { return toolBox; }
            }

            /// <summary>
            /// True if in an editing mode that is changing the height map or materials.
            /// </summary>
            public bool EditingTerrain
            {
                get { return toolBox.Active && toolBox.EditingTerrain; }
            }

            /// <summary>
            /// This is the mode to enter upon activation.  This allows us
            /// to control which mode we're in when returning from something
            /// like the object tweak screen.
            /// </summary>
            public ToolMode ReturnMode
            {
                get { return returnMode; }
                set { returnMode = value; }
            }

            /// <summary>
            /// Are any of the pickers active?
            /// </summary>
            public bool PickersActive
            {
                get { return ToolBox.PickersActive; }
            }
            #endregion

            #region Public

            // c'tor
            public TouchEditUpdateObj(InGame parent, ref Shared shared)
                : base(parent, ref shared)
            {
                commandMap = new CommandMap("TouchEditBase");
            }   // end of TouchEditUpdateObj c'tor

            public override void Update()
            {
                base.Update();

                // If a modal hint is active, completely bypass any update.  This stops
                // any input from bleeding through to the editor while the display is up.
                if (CommandStack.Peek().name == "ModalHint")
                {
                    return;
                }

                // If we're just setting the camera's position 
                // rather than actually editing the world.
                if (EditWorldParameters.CameraSetMode)
                {
                    if (HelpOverlay.Peek() != "CameraSetMode")
                    {
                        HelpOverlay.Pop();
                        HelpOverlay.Push("CameraSetMode");
                    }

                    // Assign the new position to the 3d cursor and then read it back since
                    // it will modify the height to accommodate the terrain.
                    parent.cursor3D.Position = shared.CursorPosition;
                    shared.CursorPosition = parent.cursor3D.Position;

                    TapGestureRecognizer tapGesture = TouchGestureManager.Get().TapGesture;

                    if (Actions.Select.WasPressed)
                    {
                        Actions.Select.ClearAllWasPressedState();

                        // Need to return to EditWorldParameters.
                        InGame.inGame.CurrentUpdateMode = UpdateMode.EditWorldParameters;
                    }
                    else if (tapGesture.WasRecognized &&
                        HelpOverlay.MouseHitBottomText(TouchInput.GetAsPoint(tapGesture.Position)))
                    {
                        InGame.inGame.CurrentUpdateMode = UpdateMode.EditWorldParameters;
                    }
                }
                else
                {
                    // Not in set camera mode, so do normal stuff.


                    // Are any of the overlay/popup elements active?
                    bool stuffActive = toolBox.PickersActive
                                        || toolBox.SlidersActive
                                        || toolBox.MenusActive
                                        || InGame.inGame.editObjectUpdateObj.newItemSelectorShim.State == UIShim.States.Active;

                    if (!stuffActive)
                    {
                        // Only thing on HelpOverlay stack should be current tool
                        // or the pickers.
                        int depth = HelpOverlay.Depth();
                        bool valid = true;
                        valid = valid && depth < 3;
                        if (valid && depth > 0)
                        {
                            string curHelp = HelpOverlay.Peek(depth - 1);
                            valid = valid && curHelp != null && curHelp.StartsWith("TouchEdit");
                        }

                        if (!valid)
                        {
                            HelpOverlay.Clear();
                            HelpOverlay.Push("MouseEditBase");
                        }
                    }


                    bool newToolSelected = false;

                    // Don't update the toolbar if we're currently dragging an object.
                    EditObjectsTool tool = toolBox.ActiveTool as EditObjectsTool;
                    if (tool != null)
                    {
                        stuffActive |= tool.DraggingObject;
                    }

                    // TODO (mouse) Should this also be skipped when menus are active?
                    // Don't update the ToolBar when the pickers or sliders are active.
                    if (!stuffActive)
                    {
                        newToolSelected = toolBar.Update();
                    }
                    else
                    {
                        //
                        // Put up a specific help overlay?
                        //

                        //Note: We let the brush/material pickers handel their own help overlays
                        //if (toolBox.PickersActive)
                        //{
                        //    HelpOverlay.ReplaceTop("MouseEditPicker");
                        //}

                        if (toolBox.SlidersActive)
                        {
                            HelpOverlay.ReplaceTop("MouseEditSlider");
                        }

                        if (toolBox.MenusActive)
                        {
                            HelpOverlay.ReplaceTop("MouseEditMenu");
                        }

                        if (InGame.inGame.editObjectUpdateObj.newItemSelectorShim.State == UIShim.States.Active)
                        {
                            HelpOverlay.ReplaceTop("MouseEditAddItemPieMenu");
                        }
                    }

                    // Is this the first update after returning from
                    // a different update obj?  If so, see if we need
                    // to return to a specific mode.
                    if (ReturnMode != ToolMode.CameraMove)
                    {
                        toolBar.CurrentMode = ReturnMode;
                        ReturnMode = ToolMode.CameraMove;
                        newToolSelected = true;
                    }

                    // Always shut down the ToolBox when moving the camera.
                    if (toolBar.CurrentMode == ToolMode.CameraMove && toolBox.Active)
                    {
                        toolBox.Deactivate();
                    }

                    if (newToolSelected)
                    {
                        switch (toolBar.CurrentMode)
                        {
                            case ToolMode.Home:
                                Foley.PlayPressStart();
                                InGame.inGame.SwitchToMiniHub();
                                return;
                            // break;
                            case ToolMode.RunGame:
                                if (InGame.IsLevelDirty)
                                {
                                    InGame.UnDoStack.Store();
                                }
                                InGame.inGame.CurrentUpdateMode = InGame.UpdateMode.RunSim;
                                return;
                            //break;
                            case ToolMode.WorldTweak:
                                InGame.inGame.CurrentUpdateMode = InGame.UpdateMode.EditWorldParameters;
                                return;
                            //break;

                            case ToolMode.CameraMove:
                                break;

                            case ToolMode.EditObject:
                                toolBox.Activate();
                                toolBox.CurrentMode = ToolMode.EditObject;
                                break;
                            case ToolMode.Paths:
                                toolBox.Activate();
                                toolBox.CurrentMode = ToolMode.Paths;
                                break;

                            case ToolMode.TerrainPaint:
                                toolBox.Activate();
                                toolBox.CurrentMode = ToolMode.TerrainPaint;
                                break;
                            case ToolMode.TerrainRaiseLower:
                                toolBox.Activate();
                                toolBox.CurrentMode = ToolMode.TerrainRaiseLower;
                                break;
                            case ToolMode.TerrainSpikeyHilly:
                                toolBox.Activate();
                                toolBox.CurrentMode = ToolMode.TerrainSpikeyHilly;
                                break;
                            case ToolMode.TerrainSmoothLevel:
                                toolBox.Activate();
                                toolBox.CurrentMode = ToolMode.TerrainSmoothLevel;
                                break;
                            case ToolMode.WaterRaiseLower:
                                toolBox.Activate();
                                toolBox.CurrentMode = ToolMode.WaterRaiseLower;
                                break;
                            case ToolMode.DeleteObjects:
                                toolBox.Activate();
                                toolBox.CurrentMode = ToolMode.DeleteObjects;
                                break;

                            default:
                                Debug.Assert(false);
                                break;
                        }
                    }   // end if new tool selected.

                    // Update the toolBox.  We pass in the hovering state because when
                    // hovering we don't want the toolBox to update the current tool
                    // but it still has to update the pickers.
                    toolBox.Update(toolBar.Hovering);


                    // Ignore mini-hub and run commands if the pie menu is active
                    if (!(toolBar.CurrentMode == ToolMode.EditObject && inGame.editObjectUpdateObj.newItemSelectorShim.State == UIShim.States.Active))
                    {
                        // Mini-hub.
                        if (Actions.MiniHub.WasPressed)
                        {
                            Actions.MiniHub.ClearAllWasPressedState();

                            Foley.PlayPressStart();
                            parent.SwitchToMiniHub();

                            return;
                        }

                        // RunSim
                        if (Actions.RunSim.WasPressed)
                        {
                            Actions.RunSim.ClearAllWasPressedState();

                            if (InGame.IsLevelDirty)
                            {
                                // If we were using the magic brush, ensure everything is cleaned up.
                                Terrain.Current.EndSelection();
                                InGame.UnDoStack.Store();
                            }
                            InGame.inGame.CurrentUpdateMode = InGame.UpdateMode.RunSim;
                            return;
                        }
                    }
                }


                // Update the camera.
                UpdateCamera();

                // Do the common bits of the Update().
                UpdateWorld();

                //InGame.inGame.toolBoxUpdateObj.Update();
                UpdateEditBrush();

                // TODO (****) Not sure this is 100% the right place for this.
                ToolTipManager.Update();
                ThoughtBalloonManager.Update(inGame.shared.camera);


            }   // end of Update()

            public override void Activate()
            {
                if (!active)
                {
                    base.Activate();

                    CommandStack.Push(commandMap);
                    HelpOverlay.Push("MouseEditBase");  // This will get replaced by the active tool.

                    // Default start in Camera mode since it's interactive and non-destructive.
                    toolBar.CurrentMode = ToolMode.CameraMove;
                    // Prime the pump for toolBar rendering.
                    toolBar.Update();

                    // No need for a tool icon in the upper left since they're always 
                    // visible at the bottom of the screen.  Use this for something else?
                    HelpOverlay.ToolIcon = null;

                    /// Don't null out the cutPasteObject. That way, if we load up
                    /// a new level, we can still paste it in, allowing copy of objects
                    /// from one level to another. ***
                    parent.Cursor3D.Activate();
                    parent.Cursor3D.Hidden = true;
                    parent.Cursor3D.Rep = Cursor3D.Visual.Edit;
                    parent.Cursor3D.DiffuseColor = new Vector4(1, 1, 1, 1);

                    // TODO (mouse)
                    //RemoveFocusEffects();
                    shared.editWayPoint.Clear();
                }
            }   // end of Activate()

            public override void Deactivate()
            {
                if (active)
                {
                    CommandStack.Pop(commandMap);

                    base.Deactivate();

                    HelpOverlay.Pop();

                    ColorPalette.Active = false;

                    InGame.SetReturnEditMode(toolBar.CurrentMode);

                    // If we get deactivated while the AddItem menu is active, shut it down.
                    if (InGame.inGame.editObjectUpdateObj.newItemSelectorShim.State == UIShim.States.Active)
                    {
                        InGame.inGame.editObjectUpdateObj.newItemSelectorShim.Deactivate();
                    }

                    // TODO (mouse)
                    //RemoveFocusEffects();
                    //LastSelectedActor = null;

                    // Force the camera back to not having an offset.
                    shared.camera.SetDefaultHeightOffset(shared.CursorPosition, 0.5f);

                    shared.editWayPoint.Clear();

                    toolBox.Deactivate();

                    // Make the cursor visible so that the GamePad or RunSim
                    // side of things can choose what to do with it.
                    inGame.Cursor3D.Hidden = false;
                    inGame.Cursor3D.Rep = Cursor3D.Visual.Edit;
                }
            }   // end of Deactivate()


            #endregion

            #region Internal

            /// <summary>
            /// Replace the inherited version with one that's MouseEdit specific.
            /// </summary>
            public new void UpdateCamera()
            {
                float secs = Time.WallClockFrameSeconds;

                Vector3 lookAt = parent.Camera.At;
                Vector3 lookFrom = parent.Camera.From;

                Vector2 position = new Vector2(shared.CursorPosition.X, shared.CursorPosition.Y);


                //allow camera movement in all modes except edit object (or when no object is selected)
                if ((toolBar.CurrentMode != ToolMode.EditObject || parent.TouchEdit.HighLit==null))
                {
                    //allow pinch and rotate regardless of mode
                    parent.TouchEdit.DoCamera(parent.Camera);
                    parent.TouchEdit.DoZoom(parent.Camera);

                    if (CommandStack.Peek() == commandMap)
                    {
                        //don't pitch/yaw in other modes
                        parent.TouchEdit.AddPitchYawByDrag(parent.Camera);
                    }

                    parent.TouchEdit.ProcessCameraRotation(parent.Camera);

                    //Note: the above may affect camera desired At - don't use the cursor position until they finish
                    position = new Vector2(parent.Camera.DesiredAt.X, parent.Camera.DesiredAt.Y);
                    position = parent.TouchEdit.DoCursor(parent.Camera, position);              
                }

                Vector3 worldCenter = (Terrain.Max + Terrain.Min) * 0.5f;

                // Keep cursor within 50 units of existing terrain.
                float maxBrush = InGame.kMaxBrushRadius;

                Vector2 min = new Vector2(InGame.inGame.totalBounds.Min.X, InGame.inGame.totalBounds.Min.Y) - new Vector2(maxBrush, maxBrush);
                Vector2 max = new Vector2(InGame.inGame.totalBounds.Max.X, InGame.inGame.totalBounds.Max.Y) + new Vector2(maxBrush, maxBrush);

                position.X = MathHelper.Clamp(position.X, min.X, max.X);
                position.Y = MathHelper.Clamp(position.Y, min.Y, max.Y);

                //set the z height to zero so the camera doesn't jump when we switch camera modes
                shared.CursorPosition = new Vector3(position.X, position.Y, 0.0f); 

                // Keep the camera from going into the ground.
                shared.KeepCameraAboveGround();

                //keep the cursor within 4 times of the world size
                float maxWorldRadius = (Terrain.Max - Terrain.Min).Length() * 0.5f * 4.0f;

                Vector3 dirVec = shared.CursorPosition - worldCenter;
                float dirLen = dirVec.Length();

                //clamp if we are to far from the world center
                if (dirLen >= maxWorldRadius)
                {
                    //don't normalize if world radius is zero (player deleted all terrain)
                    if (dirLen>0.0f)
                    {
                        dirVec.Normalize();
                    }
                    shared.CursorPosition = worldCenter + dirVec * maxWorldRadius;
                }

                parent.Camera.DesiredAt = shared.CursorPosition;

                // Finally, call Update on the camera to let all the changes filter through.
                parent.Camera.Update();
            }   // end of UpdateCamera()

            public override void DeviceReset(GraphicsDevice device)
            {
                base.DeviceReset(device);

                toolBar.DeviceReset(device);
                toolBox.DeviceReset(device);
            }   // end of DeviceReset()

            public override void InitDeviceResources(GraphicsDevice device)
            {
                base.InitDeviceResources(device);

                toolBar.InitDeviceResources(device);
                toolBox.InitDeviceResources(device);
            }

            public override void LoadContent(bool immediate)
            {
                base.LoadContent(immediate);

                toolBar.LoadContent(immediate);
                toolBox.LoadContent(immediate);
            }   // end of LoadContent()

            public override void UnloadContent()
            {
                base.UnloadContent();

                toolBar.UnloadContent();
                toolBox.UnloadContent();
            }   // end of UnloadContent()

            #endregion

        }   // end of class MouseEditUpdateObj

    }   // end of class InGame

}   // end of namespace Boku