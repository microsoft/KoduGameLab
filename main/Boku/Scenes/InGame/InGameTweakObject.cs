
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

using KoiX;
using KoiX.Input;

using Boku.Base;
using Boku.Fx;
using Boku.SimWorld;
using Boku.SimWorld.Path;
using Boku.SimWorld.Terra;
using Boku.Common;
using Boku.Common.ParticleSystem;
using Boku.Programming;
using Boku.UI;
using Boku.UI2D;
using Boku.Input;

namespace Boku
{
    /// <summary>
    /// UpdateObject for InGame -> TweakObject
    /// </summary>
    public partial class InGame : GameObject, INeedsDeviceReset
    {

        protected partial class TweakObjectUpdateObj : BaseEditUpdateObj
        {
            public Object editFocusObject = null;           // The object, if any, under the cursor.

            public Distortion selectionHighLight = null;

            private const float snapCaptureRadius = 1.0f;   // If GameThing is within this range, snap cursor to it.
            private const float snapReleaseRadius = 1.5f;   // If cursor is more than this far away from the last thing
                                                            // we snapped to, reset snapGameThing to null.
            private Object snapObject = null;               // The Object we last snapped to.
            private float snapLerp = 0.15f;                 // How strong the snap is.


            // c'tor
            public TweakObjectUpdateObj(InGame parent, ref Shared shared)
                : base(parent, ref shared)
            {
            }   // end of TweakObjectUpdateObj c'tor


            /// <summary>
            /// TweakObjectUpdateObj Update()
            /// </summary>
            /// <param name="camera"></param>
            public override void Update()
            {
                base.Update();

                float secs = Time.WallClockFrameSeconds;

                // Do the common bits of the Update().
                UpdateCamera();
                UpdateWorld();

                // Check if we have input focus.  Don't do any input
                // related update if we don't.
                if (CommandStack.Peek() == commandMap)
                {
                    // Grab the current state of the gamepad.
                    GamePadInput pad = GamePadInput.GetGamePad0();

                    // Switch to Mini-Hub?
                    if (pad.Start.WasPressed)
                    {
                        parent.SwitchToMiniHub();
                        return;
                    }

                    // If B is pressed, return to the ToolMenu
                    if (pad.ButtonB.WasPressed)
                    {
                        parent.CurrentUpdateMode = UpdateMode.ToolMenu;
                        return;
                    }

                    // Assign the new position to the 3d cursor and then read it back since
                    // it will modify the height to accommodate the terrain.
                    parent.cursor3D.Position = shared.CursorPosition;
                    shared.CursorPosition = parent.cursor3D.Position;

                    // See if there's something under the cursor.
                    CheckForEditFocusObject();

                    if (editFocusObject != null)
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
                    // Y button edits the brain.
                    //
                    if (pad.ButtonY.WasPressed)
                    {
                        GameActor actor = editFocusObject as GameActor;
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
                    // A button edits the tweakables.
                    //
                    if (pad.ButtonA.WasPressed)
                    {
                        GameActor actor = editFocusObject as GameActor;
                        if (actor != null)
                        {
                            // If we're editing a clone, edit its creatable instead.
                            if (actor.IsClone)
                                actor = parent.GetCreatable(actor.CreatableId);

                            // Tell EditObjectParameters which actor's params we want to edit.
                            parent.shared.editObjectParameters.Actor = actor;
                            // Switch modes.
                            parent.CurrentUpdateMode = UpdateMode.EditObjectParameters;
                            InGame.IsLevelDirty = true;

                            // Hide the color palette and then return.  If we just fall 
                            // through then the color palette will be re-enabled.
                            ColorPalette.Active = false;
                            return;
                        }
                    }

                    // Check state of color palette and respond to any inputs.
                    UpdateColorPalette(pad);

                    UpdateAura();
                }   // end if we have input focus.

            }   // end of TweakObjectUpdateObj Update()


            /// <summary>
            /// Check if the cursor is close enough to anything to have that
            /// object be considered "in focus".  If so, snap toward it.
            /// </summary>
            protected void CheckForEditFocusObject()
            {
                // Find the nearest GameThing.
                editFocusObject = null;
                Vector3 lookAt = parent.cursor3D.Position;
                float distance = float.MaxValue;
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

                float secs = Time.WallClockFrameSeconds;

                // See if we need to snap.
                if (snapObject == null)
                {
                    if (distance < snapCaptureRadius)
                    {
                        float z = lookAt.Z;
                        GameThing thing = editFocusObject as GameThing;
                        if (thing != null)
                        {
                            Vector3 thingPos = thing.Movement.Position;
                            lookAt = Vector3.Lerp(lookAt, thingPos, snapLerp * secs * 30.0f);
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

                    float dist = delta.Length();
                    if (dist > snapReleaseRadius)
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
                        lookAt.Z = z;
                        parent.cursor3D.Position = lookAt;
                        shared.CursorPosition = parent.cursor3D.Position;
                    }
                }

                // We've now found the nearest object.  If close enough, keep it 
                // and turn the cursor blue to indicate it is in focus.  If not 
                // close enough set the cursor to white and null the focus object.
                const float nearThreshold = 0.5f;
                if (distance > nearThreshold)
                {
                    editFocusObject = null;
                    parent.cursor3D.DiffuseColor = Color.White.ToVector4();
                    HelpOverlay.Pop();
                    HelpOverlay.Push("ObjectTweak");
                }
                else
                {
                    parent.cursor3D.DiffuseColor = Color.Blue.ToVector4();
                    if (editFocusObject as GameActor != null)
                    {
                        if (String.Compare(HelpOverlay.Peek(), "ObjectTweakFocusProgrammable") != 0)
                        {
                            HelpOverlay.Pop();
                            HelpOverlay.Push("ObjectTweakFocusProgrammable");
                        }
                    }
                    else
                    {
                        if (String.Compare(HelpOverlay.Peek(), "ObjectTweakFocus") != 0)
                        {
                            HelpOverlay.Pop();
                            HelpOverlay.Push("ObjectTweakFocus");
                        }
                    }
                }

            }   // end of CheckForEditFocusObject()

            // TODO (****) this is identical to the one in InGameEditObject.  Should figure out how to share.
            /// <summary>
            /// Handles any changes to the ColorPalette and,
            /// if needed the color of the in focus object.
            /// </summary>
            /// <param name="pad"></param>
            protected void UpdateColorPalette(GamePadInput pad)
            {
                // Do we care about the color palette?  Yes, if there's an active waypoint node or
                // if the object under the cursor can be colored.
                GameThing thing = editFocusObject as GameThing;
                if (thing != null)
                {
                    ColorPalette.Active = true;
                    // If there's no active node then make sure that the color displayed by 
                    // the palette matches the color of the object under the cursor.
                    shared.curObjectColor = ColorPalette.GetIndexFromColor(thing.ClassColor);
                }
                else
                {
                    // WayPointEdit takes care of colors itself.
                    ColorPalette.Active = shared.editWayPoint.Active;
                }


                if (ColorPalette.Active)
                {
                    // If the color palette is active, also check to see if the user is changing the color.
                    // DPad left/right used to select color.
                    {
                        bool changed = false;

                        if (pad.DPadLeft.WasPressed || pad.DPadLeft.WasRepeatPressed)
                        {
                            shared.curObjectColor = (shared.curObjectColor + ColorPalette.NumEntries - 1) % ColorPalette.NumEntries;
                            changed = true;
                        }
                        if (pad.DPadRight.WasPressed || pad.DPadRight.WasRepeatPressed)
                        {
                            shared.curObjectColor = (shared.curObjectColor + 1) % ColorPalette.NumEntries;
                            changed = true;
                        }

                        if (changed)
                        {
                            // If we have something in focus, change its color.
                            if (editFocusObject != null)
                            {
                                if (editFocusObject is GameThing)
                                {
                                    ((GameThing)editFocusObject).ClassColor = ColorPalette.GetColorFromIndex(shared.curObjectColor);
                                }
                            }
                            InGame.IsLevelDirty = true;
                        }   // end if color changed
                    }   // end if DPad left/right pressed

                }   // end if WayPointColorPalette is active.

            }   // end of UpdateColorPalette()


            //
            // TODO (****) These are (mostly) identical to the one in InGameEditObject.  Should figure out how to share.
            //

            protected void UpdateAura()
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
                        MakeAura(focusThing);
                        Debug.Assert(selectionHighLight != null);
                        selectionHighLight.TintAura(0.0f, 0.0f, 4.0f);
                    }
                    else
                    {
                        RemoveAura();
                    }
                }
            }
            protected Distortion RemoveAura()
            {
                if (selectionHighLight != null)
                {
                    selectionHighLight.Die();
                    selectionHighLight = null;
                }
                return null;
            }
            protected Distortion MakeAura(GameThing thing)
            {
                if (selectionHighLight != null)
                {
                    if (selectionHighLight.Owner == thing)
                    {
                        return selectionHighLight;
                    }
                    RemoveAura();
                }

                selectionHighLight = parent.MakeAura(thing);

                return selectionHighLight;
            }

            protected Vector3 AtCursor
            {
                get { return parent.cursor3D.Position; }
            }


            public override void Activate()
            {
                base.Activate();

                if (parent.State != States.Paused)
                {
                    HelpOverlay.Push("ObjectTweak");
                }
                parent.cursor3D.Activate();
                RemoveAura();
                shared.editWayPoint.Clear();
            }   // end of TweakObjectUpdateObj Activate()

            public override void Deactivate()
            {
                base.Deactivate();

                ColorPalette.Active = false;
                RemoveAura();

                // Force the camera back to not having an offset.
                shared.camera.SetDefaultHeightOffset(shared.CursorPosition, 0.5f);

                shared.editWayPoint.Clear();

                HelpOverlay.Pop();

            }   // end of TweakObjectUpdateObj Deactivate()

        }   // end of class TweakObjectUpdateObj

    }   // end of class InGame

}   // end of namespace Boku


