
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

using Boku.Base;
using Boku.Fx;
using Boku.SimWorld;
using Boku.SimWorld.Path;
using Boku.SimWorld.Terra;
using Boku.Common;
using Boku.Common.ParticleSystem;
using Boku.Common.Xml;
using Boku.Programming;
using Boku.UI;
using Boku.UI2D;
using Boku.Input;
using Boku.Audio;

namespace Boku
{
    /// <summary>
    /// UpdateObject for InGame -> EditObject
    /// </summary>
    public partial class InGame : GameObject, INeedsDeviceReset
    {

        public partial class EditObjectUpdateObj : BaseEditUpdateObj
        {
            public Object editFocusObject = null;           // The object, if any, under the cursor.
            public Object cutPasteObject = null;            // Temp holding ref for cut and paste.
            private float cutPasteRotation = 0.0f;          // Rotation to paste at.
            public Object selectedObject = null;            // The selected object.

            public GameThing lastClonedThing = null;        // Temporary ref to most recently clone thing used to prevent 
                                                            // collisions between this thing and the selected object.

            public Distortion selectionHighLight = null;
//            private float selectedObjectHeight = 0.0f;      // Height above terrain for selected object.
            private float selectedObjectRotation = 0.0f;    // Rotation around Z relative to camera.
            private GameActor lastSelectedActor = null;

            private Object snapObject = null;               // The Object we last snapped to.
            private float snapLerp = 0.15f;                 // How strong the snap is.

            public GameActor LastSelectedActor
            {
                get { return lastSelectedActor; }
                set
                {
                    if (lastSelectedActor != value)
                    {
                        if (lastSelectedActor != null)
                        {
                            lastSelectedActor.ClearCreatableCache();
                        }
                        lastSelectedActor = value;
                        if (lastSelectedActor != null)
                        {
                            lastSelectedActor.CacheCreatables();
                        }
                    }
                }
            }

            /// <summary>
            /// The object currently in the cut/paste buffer.
            /// </summary>
            public Object CutPasteObject
            {
                get { return cutPasteObject; }
                set { cutPasteObject = value; }
            }

            /// <summary>
            /// Rotation to be used when pasting objects.
            /// </summary>
            public float CutPasteRotation
            {
                get { return cutPasteRotation; }
                set { cutPasteRotation = value; }
            }

            public bool HaveClipboard
            {
                get { return cutPasteObject != null; }
            }

            // c'tor
            public EditObjectUpdateObj(InGame parent, ref Shared shared)
                : base(parent, ref shared)
            {
                newItemSelectorShim = new UIShim(AddUISelector, out newItemSelector, false);
            }   // end of EditObjectUpdateObj c'tor


            /// <summary>
            /// EditObjectUpdateObj Update()
            /// </summary>
            /// <param name="camera"></param>
            public override void Update()
            {
                base.Update();

                // If we're channging update modes, don't bother to finish this update call.
                if (InGame.inGame.pendingUpdateMode != UpdateMode.None)
                {
                    return;
                }

                float secs = Time.WallClockFrameSeconds;

                ThoughtBalloonManager.Update(shared.camera);

                // In case any displays are active.
                shared.smallTextDisplay.Update(shared.camera);
                shared.scrollableTextDisplay.Update(shared.camera);

                // If we're just setting the camera's position 
                // rather than actually editing the world.
                if (EditWorldParameters.CameraSetMode)
                {
                    GamePadInput pad = GamePadInput.GetGamePad0();

                    // Assign the new position to the 3d cursor and then read it back since
                    // it will modify the height to accommodate the terrain.
                    parent.cursor3D.Position = shared.CursorPosition;
                    shared.CursorPosition = parent.cursor3D.Position;

                    if (Actions.Select.WasPressed)
                    {
                        Actions.Select.ClearAllWasPressedState();

                        // Need to return to EditWorldParameters.
                        InGame.inGame.CurrentUpdateMode = UpdateMode.EditWorldParameters;
                    }
                }
                else
                {

                    // We need to update our child objects first so they have first shot at grabbing any input.
                    shared.addItemHelpCard.Update();

                    // HACK  There's still a lingering lock up that sometimes occurs when
                    // exiting the programming UI.  It appears that the command map stack
                    // is getting out of sync with the current InGame mode and the help overlay
                    // stack.  The problem seems to have a distinct signature so until the
                    // bug can be tracked down and fixed we can work around it with this hack.
                    // What this is doing is checking to see if the "signature" of the 
                    // problem is in evidence and if so, then adjusting things to make them
                    // better.  I'm leaving an assert in the code so we don't just forget about it.
                    if (CommandStack.Depth() == 1 && CommandStack.Peek().name == "PreGameBase")
                    {
                        //Debug.Assert(false, "Hack to fix lockup problem, you should be able to ignore and resume with no trouble.  Please let **** know if this works.");

                        // Remove PreGameBase and replace with InGameEditBase.
                        CommandStack.Pop(CommandStack.Peek());
                        CommandStack.Push(commandMap);

                        // Fix up HelpOverlay.
                        while (HelpOverlay.Depth() > 1)
                        {
                            HelpOverlay.Pop();
                        }
                        HelpOverlay.Push("ObjectEditEmptyClipboard");
                    }


                    // Check if we have input focus.  Don't do any input
                    // related update if we don't.
                    if (CommandStack.Peek() == commandMap)
                    {
                        // Grab the current state of the gamepad.
                        GamePadInput pad = GamePadInput.GetGamePad0();

                        // Switch to Mini-Hub?
                        if (Actions.MiniHub.WasPressed)
                        {
                            Actions.MiniHub.ClearAllWasPressedState();

                            parent.SwitchToMiniHub();
                            return;
                        }

                        shared.editWayPoint.Update();

                        // Unselect selected object, exit WayPoint mode or short-cut back to run mode.
                        if (Actions.Unselect.WasPressed)
                        {
                            Actions.Unselect.ClearAllWasPressedState();

                            if (selectedObject != null)
                            {
                                selectedObject = null;
                            }
                            else
                            {
                                // Exit back to ToolMenu.
                                parent.CurrentUpdateMode = UpdateMode.ToolMenu;
                                GamePadInput.ClearAllWasPressedState();

                                return;
                            }
                        }

                        //Note: we need to handle input that updates selected/focus objects before processing those objects
                        //Otherwise, our results may be a frame old, and anything relying on checking if an object is selected may not update correctly
                        //Without this here, the pipe logic that only updates a pipe's snap position when it's selected was failing to run the final frame, leaving
                        //objects placed by gamepad in an unsnapped position.
                        if (GamePadInput.ActiveMode == GamePadInput.InputMode.GamePad)
                        {
                            //
                            // Handle cut & paste.  Note this also takes care of cloning.
                            //
                            if (Actions.Cut.WasPressed)
                            {
                                Actions.Cut.ClearAllWasPressedState();

                                // If there's anything to cut, do so.
                                if (editFocusObject != null)
                                {
                                    CutAction();
                                }
                            }

                            // Copy
                            if (Actions.Copy.WasPressed)
                            {
                                Actions.Copy.ClearAllWasPressedState();

                                // If there's anything to copy, do so.
                                if (editFocusObject != null)
                                {
                                    CopyAction();
                                }
                            }

                            if (Actions.Paste.WasPressed)
                            {
                                Actions.Paste.ClearAllWasPressedState();

                                if (editFocusObject != null)
                                {
                                    // There's something under the cursor so clone it.
                                    CloneAction();
                                    // Ok, so we're calling return here and you may very well wonder why.  Let
                                    // me enlighten you.  When we clone an object we set the LastClonedThing
                                    // ref.  This is so we can ignore collisions between the cloned thing and
                                    // the selected object until the user has moved them apart.  Once they are
                                    // far enough apart that they no longer collide we can clear the ref and
                                    // everthing works as expected.  But here's the rub.  Below here we call
                                    // UpdateWorld() which does the collision checks.  If we don't return here
                                    // then the collision checks will be done, no collisions will be found
                                    // between the cloned thing and the selected(picked up) object and the 
                                    // LastClonedThing ref will be cleared.  The reason that the checks will
                                    // fail is that the cloned thing doesn't exist in the world yet.  Yes, more
                                    // issues with the delayed refresh stuff.  So, by returning here we let
                                    // Refresh do it's thing and add the the newly cloned thing to the world
                                    // and then again on the next frame everything will once again work.
                                    // Honestly, when was the last time you saw this much of a comment on a 
                                    // return statement?
                                    return;
                                }
                                else
                                {
                                    // Nothing in the way so paste whatever's in the buffer, if anything.
                                    PasteAction();
                                }
                            }


                            // The A button
                            if (Actions.Add.WasPressed)
                            {
                                Actions.Add.ClearAllWasPressedState();

                                // If waypoint editing.
                                if (!shared.editWayPoint.Active)
                                {
                                    if (editFocusObject == null)
                                    {
                                        // Nothing in focus so bring up the new item selector.
                                        ActivateNewItemSelector(false);
                                    }
                                    else
                                    {
                                        // We've got an object in focus so toggle it's selected state.
                                        ToggleSelectedStateOfFocusObject();
                                    }
                                }   // end else not waypoint editing.
                            }   // end if A button pressed
                        }


                        // Assign the new position to the 3d cursor and then read it back since
                        // it will modify the height to accommodate the terrain.
                        parent.cursor3D.Position = shared.CursorPosition;
                        shared.CursorPosition = parent.cursor3D.Position;

                        if (GamePadInput.ActiveMode == GamePadInput.InputMode.KeyboardMouse)
                        {
                            parent.MouseEdit.DoObject(parent.Camera);
                        }
                        else if (GamePadInput.ActiveMode == GamePadInput.InputMode.Touch)
                        {
                            parent.TouchEdit.DoObject(parent.Camera);
                        }

                        // Determine whether or not we have an object in focus, ie under the cursor.
                        if (selectedObject != null)
                        {
                            // Obviously, if we have a selected object it by definition is in focus.
                            editFocusObject = selectedObject;
                            LastSelectedActor = editFocusObject as GameActor;

                            // While we're here, let's also make sure that it's position 
                            // matches the cursor.  This handles dragging the object with 
                            // the cursor and creating dust as the object is dragged.
                            DragSelectedObject();

                            // Also handle tweaking of the height of the selected object.
                            GameActor actor = selectedObject as GameActor;
                            if (actor != null)
                            {
                                //float delta = 5.0f * secs * (pad.RightTrigger - pad.LeftTrigger);
                                float delta = 0.0f;
                                delta += Actions.Raise.IsPressed ? 1.0f : 0.0f;
                                delta += Actions.Lower.IsPressed ? -1.0f : 0.0f;
                                delta *= 2.0f * secs;
                                if (delta != 0.0f)
                                {
                                    actor.HeightOffset += delta;
                                    shared.camera.ChangeHeightOffset(actor.Movement.Altitude);
                                    InGame.IsLevelDirty = true;
                                }
                            }
                        }
                        else
                        {
                            // Nothing is selected so see if there's something under the cursor.
                            Object focusObj = editFocusObject;
                            CheckForEditFocusObject();

                            // Has the focus object changed?  If so, see if it's a valid GameThing.
                            // If it is, have it react to the cursor.
                            if (focusObj != editFocusObject)
                            {
                                GameThing thing = editFocusObject as GameThing;
                                if (thing != null)
                                {
                                    thing.ReactToCursor();
                                }
                            }

                            GameActor actor = parent.ActiveActor;
                            if (actor != null)
                            {
                                if (actor.IsTree)
                                {
                                    GameActor newTree = ChangeTreeType(actor);
                                    if (newTree != null)
                                    {
                                        actor = newTree;
                                        editFocusObject = null;
                                        LastSelectedActor = null;
                                        InGame.IsLevelDirty = true;
                                    }
                                }
                                if (!actor.IsTree || 
                                    (GamePadInput.ActiveMode == GamePadInput.InputMode.KeyboardMouse) || 
                                    (GamePadInput.ActiveMode == GamePadInput.InputMode.Touch))
                                {
                                    float delta = 0.0f;
                                    delta += Actions.Bigger.IsPressed ? 1.0f : 0.0f;
                                    delta += Actions.Smaller.IsPressed ? -1.0f : 0.0f;
                                    delta *= 2.0f * secs;
                                    if (delta != 0.0f)
                                    {
                                        actor.ReScale *= 1.0f + delta;

                                        float minScale = 0.2f;
                                        float maxScale = 4.0f;
                                        actor.ReScale = MyMath.Clamp<float>(actor.ReScale, minScale, maxScale);
                                        InGame.IsLevelDirty = true;
                                    }
                                }
                            }
                        }

                        if (shared.editWayPoint.Active)
                        {
                            shared.camera.ChangeHeightOffsetNotHeight(shared.editWayPoint.CameraHeightOffset);
                        }
                        else if (editFocusObject != null)
                        {
                            GameActor actor = editFocusObject as GameActor;
                            if (actor != null)
                            {
                                shared.camera.ChangeHeightOffset(actor.Movement.Altitude);
                            }
                        }
                        else
                        {
                            shared.camera.SetDefaultHeightOffset(shared.CursorPosition, 0.5f);
                        }

                        //
                        // Brain surgery.
                        //
                        if (Actions.Program.WasPressed)
                        {
                            Actions.Program.ClearAllWasPressedState();

                            GameActor actor = parent.ActiveActor;
                            if (actor != null)
                            {
                                if (parent.ShowEditor(actor))
                                {
                                    InGame.IsLevelDirty = true;

                                    // Hide the color palette and then return.  If we just fall 
                                    // through then the color palette will be re-enabled.
                                    ColorPalette.Active = false;
                                }
                                return;
                            }
                        }

                        //
                        // Tweak settings.
                        //
                        if (Actions.Tweak.WasPressed)
                        {
                            Actions.Tweak.ClearAllWasPressedState();

                            GameActor actor = parent.ActiveActor;
                            if (actor != null)
                            {
                                // Tell EditObjectParameters which actor's params we want to edit.
                                parent.shared.editObjectParameters.Actor = actor;
                                // Switch modes.
                                parent.CurrentUpdateMode = UpdateMode.EditObjectParameters;
                                InGame.inGame.SaveEditCamera();

                                /// Don't dirty the world here. If anything changes, the EditObjectParameters
                                /// will dirty the world appropriately. We con't need to dirty the world
                                /// just to look at them.

                                // Hide the color palette and then return.  If we just fall 
                                // through then the color palette will be re-enabled.
                                ColorPalette.Active = false;
                                return;
                            }
                        }

                        // Check state of color palette and respond to any inputs.
                        UpdateColorPalette(pad);

                        UpdateFocusEffects();
                    }   // end if we have input focus.

                }   // end if editing as opposed to just moving the camera around.

                // Update the camera last so all user input is accounted for.
                UpdateCamera();

                // Do the common bits of the Update().
                UpdateWorld();

                UpdateHelpOverlay();

                // TODO (****) Not sure this is 100% the right place for this.
                ToolTipManager.Update();

            }   // end of EditObjectUpdateObj Update()

            /// <summary>
            /// Call MakeTreeChange to change a tree's type.
            /// </summary>
            /// <param name="orig"></param>
            /// <returns></returns>
            public GameActor ChangeTreeType(GameActor orig)
            {
                if ((orig != null) && orig.IsClone)
                {
                    orig = parent.GetCreatable(orig.CreatableId);
                }
                return MakeTreeChange(orig);
            }
            /// <summary>
            /// Cycle through tree types. Order is:
            /// BigYucca1
            /// Yucca1
            /// Yucca2
            /// Yucca3
            /// Things get interesting for a creatable.
            /// If it's the creatable master
            ///     set the new tree's Creatable flag to true
            ///     loop over all the source clones making new ones of them
            ///         and then set their creatable id to the new master's.
            /// else if it's a creatable clone
            ///     get it's master and change it's type.
            /// </summary>
            /// <param name="orig"></param>
            /// <returns></returns>
            public GameActor MakeTreeChange(GameActor orig)
            {
                GamePadInput pad = GamePadInput.GetGamePad0();
                GameActor tree = null;
                if (Actions.PrevTreeType.WasPressedOrRepeat)
                {
                    if (orig.StaticActor.NonLocalizedName == "BigYucca1")
                        tree = parent.AddActorAtCursor(ActorFactory.Create(ActorManager.GetActor("Yucca1")));
                    else if (orig.StaticActor.NonLocalizedName == "Yucca1")
                        tree = parent.AddActorAtCursor(ActorFactory.Create(ActorManager.GetActor("Yucca2")));
                    else if (orig.StaticActor.NonLocalizedName == "Yucca2")
                        tree = parent.AddActorAtCursor(ActorFactory.Create(ActorManager.GetActor("Yucca3")));
                    else if (orig.StaticActor.NonLocalizedName == "Yucca3")
                        tree = parent.AddActorAtCursor(ActorFactory.Create(ActorManager.GetActor("BigYucca1")));
                    Debug.Assert(tree != null, "Unknown tree type?");
                }
                else if (Actions.NextTreeType.WasPressedOrRepeat)
                {
                    if (orig.StaticActor.NonLocalizedName == "BigYucca1")
                        tree = parent.AddActorAtCursor(ActorFactory.Create(ActorManager.GetActor("Yucca3")));
                    else if (orig.StaticActor.NonLocalizedName == "Yucca1")
                        tree = parent.AddActorAtCursor(ActorFactory.Create(ActorManager.GetActor("BigYucca1")));
                    else if (orig.StaticActor.NonLocalizedName == "Yucca2")
                        tree = parent.AddActorAtCursor(ActorFactory.Create(ActorManager.GetActor("Yucca1")));
                    else if (orig.StaticActor.NonLocalizedName == "Yucca3")
                        tree = parent.AddActorAtCursor(ActorFactory.Create(ActorManager.GetActor("Yucca2")));
                    Debug.Assert(tree != null, "Unknown tree type?");
                }
                if (tree != null)
                {
                    parent.CloneActor(tree, orig);

                    if (orig.Creatable)
                    {
                        /// Probably just want to purge anything in the recycle bin under
                        /// orig.CreatableID.
                        ActorFactory.ClearCreatable(orig.CreatableId);

                        tree.Creatable = true;
                        List<GameActor> origClones = new List<GameActor>();
                        parent.GetClones(orig.CreatableId, origClones);
                        for (int i = 0; i < origClones.Count; ++i)
                        {
                            GameActor srcClone = origClones[i] as GameActor;
                            if (srcClone != null)
                            {
                                GameActor dstClone = MakeTreeChange(srcClone);
                                dstClone.CreatableId = tree.CreatableId;
                            }
                        }
                    }

                    orig.Deactivate();
                }

                return tree;
            }

            /// <summary>
            /// Aligns the selected object with the cursor.
            /// </summary>
            protected void DragSelectedObject()
            {
                DragSelectedObject(selectedObject as GameThing, parent.Cursor3D.Position2d, true);
            }

            /// <summary>
            /// Moves the thing to the position.
            /// </summary>
            /// <param name="pos"></param>
            public void DragSelectedObject(GameThing thing, Vector2 pos, bool rotate)
            {
                // While we're here, let's also make sure that it's position matches the cursor.
                if (thing != null)
                {
                    float thingRot = thing.Movement.RotationZ;
                    Vector2 thingPos = new Vector2(thing.Movement.Position.X,
                                                    thing.Movement.Position.Y);

                    rotate = rotate && (thingRot != parent.Camera.Rotation + selectedObjectRotation);

                    if (thingPos != pos || rotate)
                    {
                        InGame.IsLevelDirty = true;
                    }

                    /// Set to the new position so we query the right place. Setting Z to maxvalue
                    /// means we'll be on top of any paths around.
                    thing.Movement.Position = new Vector3(pos, float.MaxValue);
                    thing.Movement.Position = new Vector3(pos, thing.GetPreferredAltitude());
                    if (rotate)
                    {
                        thing.Movement.RotationZ = parent.Camera.Rotation + selectedObjectRotation;
                    }

                    // Update the camera's target height to match the changing height of the thing.
                    shared.camera.ChangeHeightOffset(thing.Movement.Altitude);
                }

                // As long as we're moving something, let's kick up some dust.
                // Update dust emitter but only if we're not in water.
                if (Terrain.GetWaterBase(parent.cursor3D.Position) == 0.0f)
                {
                    shared.dustEmitter.Position = parent.cursor3D.Position;
                    // If we're changing state from off to on we want to "reset" the previous position
                    // so we don't get a trail from there to our new position.
                    if (!shared.dustEmitter.Emitting)
                    {
                        shared.dustEmitter.ResetPreviousPosition();
                    }
                    shared.dustEmitter.Emitting = true;
                }

                // If we still have a selected object, turn the cursor red to show that the object is selected.
                if (selectedObject != null)
                {
                    parent.cursor3D.DiffuseColor = Color.Red.ToVector4();
                }
            }   // DragSelectedObject()


            /// <summary>
            /// Check if the cursor is close enough to anything to have that
            /// object be considered "in focus".  If so, snap toward it.
            /// </summary>
            protected void CheckForEditFocusObject()
            {
                if ((GamePadInput.ActiveMode == GamePadInput.InputMode.KeyboardMouse)||
                    GamePadInput.ActiveMode == GamePadInput.InputMode.Touch)
                {
                    editFocusObject = null;
                    return;
                }

                // There's no selectedObject so see if something is in focus.

                bool somethingInfocus = editFocusObject != null;

                // Find the nearest GameThing.
                editFocusObject = null;
                Vector3 lookAt = parent.cursor3D.Position;
                float distance = float.MaxValue;
                if (!shared.editWayPoint.AddMode)
                {
                    for (int i = 0; i < parent.gameThingList.Count; i++)
                    {
                        GameThing thing = (GameThing)parent.gameThingList[i];
                        Vector3 thingPos = thing.Movement.Position;

                        Vector3 delta = thingPos - lookAt;
                        delta.Z = 0.0f;     // Only consider horizontal distances.
                        float dist = delta.Length();
                        if (dist < distance)
                        {
                            distance = dist;
                            editFocusObject = thing;
                        }
                    }
                }

                object editWayPointObject = shared.editWayPoint.CheckCursor(distance);

                if (editWayPointObject != null)
                {
                    distance = shared.editWayPoint.SnapDistance(new Vector2(lookAt.X, lookAt.Y));
                    editFocusObject = editWayPointObject;
                }
                else if (!(snapObject is GameThing))
                {
                    snapObject = null;
                }
                if (shared.editWayPoint.MoveMode)
                {
                    /// If we're moving things, the things will follow the cursor, 
                    /// the cursor doesn't need to also follow them.
                    editFocusObject = null;
                }

                float secs = Time.WallClockFrameSeconds;

                // See if we need to snap.
                if (snapObject == null)
                {
                    float captureRadius = SnapCaptureRadius;

                    // Bias capture radius for large objects.
                    GameThing thing = editFocusObject as GameThing;
                    if (thing != null)
                    {
                        captureRadius += thing.BoundingSphere.Radius / 3.0f;
                    }

                    if (distance < captureRadius)
                    {
                        float z = lookAt.Z;
                        if (thing != null)
                        {
                            Vector3 thingPos = thing.Movement.Position;
                            lookAt = Vector3.Lerp(lookAt, thingPos, snapLerp * secs * 30.0f);
                        }
                        WayPoint.Node node = editFocusObject as WayPoint.Node;
                        if (node != null)
                        {
                            lookAt = Vector3.Lerp(lookAt, node.Position, snapLerp * secs * 30.0f);
                        }
                        WayPoint.Edge edge = editFocusObject as WayPoint.Edge;
                        if (edge != null)
                        {
                            Vector3 target = edge.HandlePosition;
                            target.Z = Terrain.GetTerrainHeightFlat(target);
                            lookAt = Vector3.Lerp(lookAt, target, snapLerp * secs * 30.0f);
                        }
                        lookAt.Z = z;
                        parent.cursor3D.Position = lookAt;
                        shared.CursorPosition = parent.cursor3D.Position;
                        snapObject = editFocusObject;
                    }
                }
                else
                {
                    // See if we've moved far enough away from the 
                    // previous snap to reset.
                    GameThing thing = snapObject as GameThing;
                    Vector3 delta = new Vector3();
                    if (thing != null)
                    {
                        Vector3 thingPos = thing.Movement.Position;
                        delta = thingPos - lookAt;
                    }
                    WayPoint.Node node = snapObject as WayPoint.Node;
                    if (node != null)
                    {
                        delta = node.Position - lookAt;
                    }
                    WayPoint.Edge edge = snapObject as WayPoint.Edge;
                    if (edge != null)
                    {
                        Vector3 target = edge.HandlePosition;
                        target.Z = Terrain.GetTerrainHeightFlat(target);
                        delta = target - lookAt;
                    }

                    float dist = delta.Length();
                    if (dist > SnapReleaseRadius)
                    {
                        snapObject = null;
                    }
                    else
                    {
                        float z = lookAt.Z;
                        GameThing editThing = editFocusObject as GameThing;
                        if (editThing != null)
                        {
                            Vector3 editThingPos = editThing.Movement.Position;
                            lookAt = Vector3.Lerp(lookAt, editThingPos, snapLerp * secs * 30.0f);
                        }
                        WayPoint.Node editNode = editFocusObject as WayPoint.Node;
                        if (editNode != null)
                        {
                            lookAt = Vector3.Lerp(lookAt, editNode.Position, snapLerp * secs * 30.0f);
                        }
                        WayPoint.Edge editEdge = editFocusObject as WayPoint.Edge;
                        if (editEdge != null)
                        {
                            Vector3 target = editEdge.HandlePosition;
                            target.Z = Terrain.GetTerrainHeightFlat(target);
                            lookAt = Vector3.Lerp(lookAt, target, snapLerp * secs * 30.0f);
                        }

                        lookAt.Z = z;
                        parent.cursor3D.Position = lookAt;
                        shared.CursorPosition = parent.cursor3D.Position;
                    }
                }

                if (!shared.editWayPoint.Active)
                {
                    // We've now found the nearest object.  If close enough, keep it 
                    // and turn the cursor blue to indicate it is in focus.  If not 
                    // close enough set the cursor to white and null the focus object.
                    const float nearThreshold = 0.5f;
                    if (distance > nearThreshold)
                    {
                        editFocusObject = null;
                        parent.cursor3D.DiffuseColor = Color.White.ToVector4();
                    }
                    else
                    {
                        parent.cursor3D.DiffuseColor = Color.Blue.ToVector4();
                        GameActor actor = editFocusObject as GameActor;
                        Debug.Assert(actor != null);
                        LastSelectedActor = actor;
                    }
                }

                if (editFocusObject != null && !somethingInfocus)
                {
                    Foley.PlayCursor();
                }

            }   // end of CheckForEditFocusObject()

            /// <summary>
            /// Copies the inFocus or selected object to the 
            /// cut/paste buffer and deletes it from the world.
            /// </summary>
            internal void CutAction(object editObject)
            {
                if (editObject != null)
                {
                    // There is something under the cursor so cut it.
                    cutPasteObject = editObject;
                    GameThing thing = cutPasteObject as GameThing;
                    if (thing != null)
                    {
                        cutPasteRotation = thing.Movement.RotationZ - shared.camera.Rotation;
                        DeleteThingFromScene(thing);

                        snapObject = null;
                        selectedObject = null;

                        Foley.PlayCut();

                        InGame.IsLevelDirty = true;
                    }
                    // Clear selection highlights from the cut object
                    InGame.inGame.TouchEdit.Clear();
                    InGame.inGame.MouseEdit.Clear();
                }
            }   // end of CutAction()
            protected void CutAction()
            {
                CutAction(editFocusObject);
            }

            /// <summary>
            /// Copies the inFocus or selected object to the 
            /// cut/paste buffer.
            /// </summary>
            internal void CopyAction(object editObject)
            {
                if (editObject != null)
                {
                    // There is something under the cursor so copy it.
                    cutPasteObject = editObject;
                    GameThing thing = cutPasteObject as GameThing;
                    cutPasteRotation = thing.Movement.RotationZ - shared.camera.Rotation;

                    Foley.PlayCut();

                    InGame.IsLevelDirty = true;
                }
            }   // end of CopyAction()
            protected void CopyAction()
            {
                CopyAction(editFocusObject);
            }

            /// <summary>
            /// Paste the contents of the cut/paste buffer to the world.
            /// </summary>
            internal void PasteAction(object editObject, Vector3 position)
            {
                if ((editObject == null) && !shared.editWayPoint.Active)
                {
                    // Nothing under cursor so paste if we have something.
                    // Pasting
                    GameActor actor = cutPasteObject as GameActor;
                    if (actor != null)
                    {
                        float rotation = cutPasteRotation + shared.camera.Rotation;
                        if (rotation > MathHelper.TwoPi)
                            rotation -= MathHelper.TwoPi;
                        else if (rotation < 0)
                            rotation += MathHelper.TwoPi;
                        GameActor clone = parent.CloneInPlace(actor, position, rotation);

                        Instrumentation.IncrementCounter(Instrumentation.CounterId.PasteItem);

                        if (clone != null)
                        {
                            parent.DistortionPulse(clone, true);
                            InGame.IsLevelDirty = true;
                        }
                    }

                    // TODO How do we paste waypoint nodes and edges?
                }
            }   // end of PasteAction()
            protected void PasteAction()
            {
                PasteAction(editFocusObject, parent.Cursor3D.Position);
            }

            /// <summary>
            /// Clones the object at the cursor.
            /// </summary>
            internal void CloneAction(object editObject)
            {
                GameActor actor = editObject as GameActor;
                if (actor != null)
                {
                    GameActor clone = parent.CloneInPlace(actor, actor.Movement.Position, actor.Movement.RotationZ);

                    Instrumentation.IncrementCounter(Instrumentation.CounterId.CloneItem);

                    if (clone != null)
                    {
                        parent.DistortionPulse(clone, true);
                        InGame.IsLevelDirty = true;
                        if (LastSelectedActor != null)
                        {
                            LastSelectedActor.CacheCreatables();
                        }

                        Foley.PlayClone();
                    }
                }
            }   // end of CloneAction()
            protected void CloneAction()
            {
                CloneAction(editFocusObject);
            }

            /// <summary>
            /// Kind of what the name says...
            /// </summary>
            public void ToggleSelectedStateOfFocusObject()
            {
                // Toggle selected state.
                if ((GamePadInput.ActiveMode == GamePadInput.InputMode.GamePad)
                    && selectedObject == null)
                {
                    // Change from unselected to selected.
                    GameThing thing = editFocusObject as GameThing;
                    if (thing != null)
                    {
                        selectedObject = editFocusObject;
                        float thingRot = thing.Movement.RotationZ;
                        selectedObjectRotation = thingRot - parent.Camera.Rotation;
                        Debug.Assert(selectedObject is GameActor);
                    }

                    // Jump dust emitter to the right position.
                    shared.dustEmitter.Position = parent.cursor3D.Position;
                    shared.dustEmitter.ResetPreviousPosition();
                }
                else
                {
                    selectedObject = null;
                }
            }   // end of ToggleSelectedStateOfFocusObject()

            protected string UpdateGamePadHelpOverlay()
            {
                string helpID = null;
                if (editFocusObject == null)
                {
                    if (HaveClipboard)
                    {
                        helpID = "ObjectEdit";
                    }
                    else
                    {
                        helpID = "ObjectEditEmptyClipboard";
                    }
                }
                else if (selectedObject != null)
                {
                    helpID = "ObjectEditSelectedProgrammable";
                }
                else
                {
                    GameActor actor = editFocusObject as GameActor;
                    // EditFocusObject may be WayPoint.Node or Edge.
                    if (actor != null)
                    {
                        if (actor.IsTree)
                        {
                            helpID = "TreeEditFocusProgrammable";
                        }
                        else
                        {
                            helpID = "ObjectEditFocusProgrammable";
                        }
                    }
                }
                return helpID;
            }
            protected void UpdateHelpOverlay()
            {
                string helpID = null;
                if (EditWorldParameters.CameraSetMode)
                {
                    helpID = "CameraSetMode";
                }
                else
                {
                    helpID = shared.editWayPoint.UpdateHelpOverlay();
                    if (helpID == null)
                    {
                        if (GamePadInput.ActiveMode == GamePadInput.InputMode.Touch)
                            helpID = parent.touchEdit.UpdateHelpOverlay();
                        else
                            helpID = parent.mouseEdit.UpdateHelpOverlay();
                    }
                    if (helpID == null)
                    {
                        helpID = UpdateGamePadHelpOverlay();
                    }
                }
                if (helpID != null)
                {
                    ReplaceTopHelpOverlay(helpID);
                }
            }

            /// <summary>
            /// Handles any changes to the ColorPalette and,
            /// if needed the color of the in focus object.
            /// </summary>
            /// <param name="pad"></param>
            protected void UpdateColorPalette(GamePadInput pad)
            {
                // Do we care about the color palette?  Yes, if there's an active waypoint node or
                // if the object under the cursor can be colored.
                GameActor actor = parent.ActiveActor;
                if (actor != null)
                {
                    ColorPalette.Active = true;
                    // If there's no active node then make sure that the color displayed by 
                    // the palette matches the color of the object under the cursor.
                    shared.curObjectColor = ColorPalette.GetIndexFromColor(actor.ClassColor);
                }
                else
                {
                    // WayPointEdit takes care of colors itself.
                    ColorPalette.Active = shared.editWayPoint.ColorActive;
                }


                if (ColorPalette.Active)
                {
                    // If the color palette is active, also check to see if the user is changing the color.
                    // DPad left/right used to select color.
                    {
                        bool changed = false;

                        if (Actions.ColorLeft.WasPressedOrRepeat)
                        {
                            shared.curObjectColor = (shared.curObjectColor + ColorPalette.NumEntries - 1) % ColorPalette.NumEntries;
                            changed = true;
                        }
                        if (Actions.ColorRight.WasPressedOrRepeat)
                        {
                            shared.curObjectColor = (shared.curObjectColor + 1) % ColorPalette.NumEntries;
                            changed = true;
                        }

                        if (changed)
                        {
                            // If we have something in focus, change its color.
                            if (actor != null)
                            {
                                actor.ClassColor = ColorPalette.GetColorFromIndex(shared.curObjectColor);
                                Foley.PlayColorChange();
                            }
                            InGame.IsLevelDirty = true;
                        }   // end if color changed
                    }   // end if DPad left/right pressed

                }   // end if WayPointColorPalette is active.

            }   // end of UpdateColorPalette()


            protected void UpdateFocusEffects()
            {
                if (DistortionManager.EnabledSM2 || DistortionManager.EnabledSM3)
                {
                    /// If we have an object under edit focus, we'll
                    /// want to highlight it. If it is selected, we
                    /// highlight it red, else blue.
                    /// First version, only supporting GameThing.
                    /// Will probably want to give GameThing and Waypoint
                    /// (and anything else "highlight"-able) a common base
                    /// class, rather than let this continue this cut&paste
                    /// love fest.
                    GameThing focusThing = editFocusObject as GameThing;
                    if (focusThing != null)
                    {
                        if (MakeFocusEffects(focusThing) != null)
                        {
                            Debug.Assert(selectionHighLight != null);
                            if (selectedObject != null)
                            {
                                selectionHighLight.TintAura(4.0f, 0.0f, 0.0f);
                            }
                            else
                            {
                                selectionHighLight.TintAura(0.0f, 0.0f, 4.0f);
                            }
                        }
                    }
                    else
                    {
                        RemoveFocusEffects();
                    }
                }
            }
            protected Distortion RemoveFocusEffects()
            {
                if (selectionHighLight != null)
                {
                    selectionHighLight.Die();
                    selectionHighLight = null;
                }

                return null;
            }
            protected Distortion MakeFocusEffects(GameThing thing)
            {
                if (GamePadInput.ActiveMode != GamePadInput.InputMode.GamePad)
                {
                    return RemoveFocusEffects();
                }
                if (selectionHighLight != null)
                {
                    if (selectionHighLight.Owner == thing)
                    {
                        return selectionHighLight;
                    }
                    RemoveFocusEffects();
                }

                selectionHighLight = parent.MakeAura(thing);

                return selectionHighLight;
            }

            protected Vector3 AtCursor
            {
                get { return parent.cursor3D.Position; }
            }

            private object timerInstrument = null;

            public override void Activate()
            {
                base.Activate();

                selectedObject = null;      // Never start in selected mode.
                                            // TODO (****) Except, maybe in the case of editing a selected object's parameters.
                                            // When we come back here from exiting
                LastSelectedActor = null;
                /// Don't null out the cutPasteObject. That way, if we load up
                /// a new level, we can still paste it in, allowing copy of objects
                /// from one level to another. ***
                parent.cursor3D.Activate();
                parent.cursor3D.Hidden = false;
                parent.cursor3D.DiffuseColor = new Vector4(1, 1, 1, 1);
                RemoveFocusEffects();
                shared.editWayPoint.Clear();

                timerInstrument = Instrumentation.StartTimer(Instrumentation.TimerId.InGameEditObject);

            }   // end of EditObjectUpdateObj Activate()

            public override void Deactivate()
            {
                base.Deactivate();

                ColorPalette.Active = false;
                RemoveFocusEffects();
                LastSelectedActor = null;

                newItemSelectorShim.Deactivate();

                // Force the camera back to not having an offset.
                shared.camera.SetDefaultHeightOffset(shared.CursorPosition, 0.5f);

                shared.editWayPoint.Clear();

                if (HelpOverlay.Peek() != "RunSimulation")
                {
                    HelpOverlay.Pop();
                }

                Instrumentation.StopTimer(timerInstrument);

            }   // end of EditObjectUpdateObj Deactivate()

        }   // end of class EditObjectUpdateObj

    }   // end of class InGame

}   // end of namespace Boku


