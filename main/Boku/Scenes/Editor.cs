// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.


// On exit from the kode editor, the kode's Xml will be output to the clipboard.
// This helps support grabbing kode snippets for some of the help options.
// This is disabled by default because it seems to throw fairly regularly, causing
// the transition out of the editor to stutter.
//#define OUTPUT_KODE_TO_CLIPBOARD

//#define BLANK_AT_END
//#define UGLY_DEBUG_HACK

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
using Boku.Programming;
using Boku.Input;
using Boku.Audio;
using Boku.Fx;
using Boku.Common.Gesture;

namespace Boku
{
    /// <summary>
    /// This represents the Editor scene
    /// 
    /// GamePad Back or Key Esc - return to Sim
    /// </summary>
    public class Editor : GameObject, ITransform, INeedsDeviceReset
    {
        protected string pageLabel = Strings.Localize("programming.page");
        protected const string upidTaskFormat = CardSpace.IconPrefix + ".task{0}";
        
        public class Shared
        {
            public UiCamera camera = new UiCamera();
            public UiCamera cameraOverlay = new UiCamera();

            public AABB2D leftShoulderBox = new AABB2D();
            public AABB2D rightShoulderBox = new AABB2D();
            public AABB2D taskHandleBox = new AABB2D();

            public MouseMenu rightClickMenu = new MouseMenu();  // Activated on right mouse click.
                                                                // Elements filled in at activation time.

            // The touch selector is used to change between pages
            public AABB2D[] touchPageSelectorPageBoxes = new AABB2D[Brain.kCountDefaultTasks];
            public AABB2D touchPageSelectorBackBox = new AABB2D();
            
            // Used when requesting a change to a specific page, rather than simply going to next or prev.
            public int changeTaskId = 0;
            public int activeTaskId = 0;

            public bool showHelp = false;   // User chose help from pop-up menu.

            public Shared()
            {
                RecenterCamera();

                rightClickMenu.OnSelect = RightClickOnSelect;
                rightClickMenu.OnCancel = RightClickOnCancel;
            }

            public void RecenterCamera()
            {
                this.camera.Center();
                // Add a little angle to the camera view.
                Vector3 adjust = new Vector3( -4.0f, 0.0f, 0.0f );
                this.camera.From += adjust;
            }

            public void RightClickOnSelect(MouseMenu menu)
            {
                Editor parent = InGame.inGame.Editor;

                if (menu.CurString == Strings.Localize("programming.help"))
                {
                    // User wants help on the current tile.
                    showHelp = true;
                }
                else if (menu.CurString == Strings.Localize("programming.cutTile"))
                {
                    ReflexCard tile = menu.Object as ReflexCard;
                    if (tile != null)
                    {
                        tile.ClearCard(null, null);
                    }
                } 
                else if (menu.CurString == Strings.Localize("programming.copyTile"))
                {
                    // intentionally left blank
                } 
                else if (menu.CurString == Strings.Localize("programming.pasteTile"))
                {
                    // intentionally left blank
                }
                else if (menu.CurString == Strings.Localize("programming.cutRow"))
                {
                    ReflexHandle rh = menu.Object as ReflexHandle;
                    if (rh != null)
                    {
                        // Make a copy of the reflex first.
                        ReflexHandle.reflexBlock.Init(rh.LineNumber - 1);
                        ReflexHandle.reflexBlock.Copy();
                        // Then cut.
                        ReflexHandle.reflexBlock.Cut();
                    }
                } 
                else if (menu.CurString == Strings.Localize("programming.copyRow"))
                {
                    ReflexHandle rh = menu.Object as ReflexHandle;
                    if (rh != null)
                    {
                        ReflexHandle.reflexBlock.Init(rh.LineNumber - 1);
                        ReflexHandle.reflexBlock.Copy();
                    }

                }
                else if (menu.CurString == Strings.Localize("programming.pasteRow"))
                {
                    ReflexHandle rh = menu.Object as ReflexHandle;
                    if (rh != null)
                    {
                        ReflexHandle.reflexBlock.Init(rh.LineNumber - 1);
                        ReflexHandle.reflexBlock.Paste();
                    }
                }
                else if (menu.CurString == Strings.Localize("programming.cutPage"))
                {
                    if (CutPasteBuffer == null)
                    {
                        CutPasteBuffer = new List<ReflexData>();
                    }
                    // Remove any existing reflexes.
                    CutPasteBuffer.Clear();

                    // Copy the reflex data to the buffer.
                    for (int i = 0; i < parent.ActivePanels.Count; i++)
                    {
                        CutPasteBuffer.Add(parent.ActivePanels[i].Reflex.Data);
                    }

                    // Add a blank reflex to the end.
                    parent.InsertReflex(null);

                    // Delete all the other reflexes.
                    int count = parent.ActivePanels.Count - 1;
                    for (int i = 0; i < count; i++)
                    {
                        parent.ActivePanels[0].RemoveReflex();
                    }

                    // Cause camera offset to be reset.
                    parent.firstTime = true;
                
                }
                else if (menu.CurString == Strings.Localize("programming.copyPage"))
                {
                    if (CutPasteBuffer == null)
                    {
                        CutPasteBuffer = new List<ReflexData>();
                    }
                    // Remove any existing reflexes.
                    CutPasteBuffer.Clear();

                    // Copy the reflex data to the buffer.
                    for (int i = 0; i < parent.ActivePanels.Count; i++)
                    {
                        CutPasteBuffer.Add(parent.ActivePanels[i].Reflex.Data);
                    }
                }
                else if (menu.CurString == Strings.Localize("programming.pastePage"))
                {
                    if (CutPasteBuffer != null)
                    {
                        int prevReflexCount = parent.ActivePanels.Count;
                        // Add the buffer reflexes onto the end of the existing reflexes.
                        parent.IndexActivePanel = parent.ActivePanels.Count - 1;
                        for (int i = 0; i < CutPasteBuffer.Count; i++)
                        {
                            // Insert a new reflex at the end of the list.
                            ReflexPanel panel = InGame.inGame.Editor.InsertReflex(null);

                            // Paste the cut/paste code into this new panel and tell it to rebuild.
                            panel.Reflex.Paste(CutPasteBuffer[i]);
                            panel.uiRebuild = true;
                        }

                        // If there was only a single reflex before the paste and it was empty, delete it.
                        if (prevReflexCount == 1)
                        {
                            if (parent.ActivePanels[0].Reflex.Data.IsEmpty())
                            {
                                parent.ActivePanels[0].RemoveReflex();
                            }
                        }

                        // Make sure all the panels aren't hot.
                        for (int i = 0; i < parent.ActivePanels.Count; i++)
                        {
                            ReflexPanel panel = parent.ActivePanels[i];
                            panel.pendingState = ReflexPanel.States.Active;
                            foreach (IControl control in panel.listControls)
                            {
                                control.Hot = false;
                            }
                        }

                        // Move the cursor to the page handle.
                        parent.IndexActivePanel = -1;
                        UiCursor.ActiveCursor.Parent = parent.fugly;
                    }
                }
            }   // end of RightClickOnSelect()

            public void RightClickOnCancel(MouseMenu menu)
            {
            }   // end of RightClickOnCancel()
        }

        protected class UpdateObj : UpdateObject
        {
            private Editor parent = null;
            private Shared shared = null;
            public List<UpdateObject> updateList = null; // Children's update list.

            public bool bDraggingUpDown = false;
            public float currentDragY = 0.0f;
            const float k_DragThreshold = 80.0f;

            private CommandMap commandMap;
            public CommandMap commandMapPageHandle;

            public UpdateObj(Editor parent, Shared shared)
            {
                this.parent = parent;
                this.shared = shared;

                commandMap = new CommandMap(@"BrainEditor");
                commandMapPageHandle = new CommandMap(@"PageHandle");

                updateList = new List<UpdateObject>();
            }   // end of UpdateObj c'tor

            //static int tic = 0;   // debug

            // Timer for slowing down moving reflexes with the mouse.
            double nextMoveTime = 0;

            private void HandleTouchInput()
            {
                TouchContact touch = TouchInput.GetOldestTouch();
                if ((!KoiLibrary.LastTouchedDeviceIsTouch) || (touch == null))
                {
                    return;
                }

                Vector2 touchHit = touch.position;
                bool changingPage = false;
                bool hitPage = false;
                bool hitSomething = false;
                bool changed = false;

                SwipeGestureRecognizer swipeGesture = TouchGestureManager.Get().SwipeGesture;
                DragGestureRecognizer dragGesture = TouchGestureManager.Get().DragGesture;

                // Check for Swipe gesture to switch tasks.  Don't allow Swipe to happen if the panel is "moving".
                // "Moving" implies the row handle is selected and the user is probably trying to indent/unindent
                // the row instead of swiping to another ask.
                if (parent.ActivePanel != null && !parent.ActivePanel.Moving && swipeGesture.WasRecognized)
                {
                    switch (swipeGesture.SwipeDirection)
                    {
                        case Boku.Programming.Directions.East:

                            shared.changeTaskId -= 1;
                            shared.changeTaskId = (shared.changeTaskId < 0) ? (shared.touchPageSelectorPageBoxes.Length - 1) : shared.changeTaskId;
                            parent.NavTaskChange(null, null);
                            changingPage = true;
                            break;

                        case Boku.Programming.Directions.West:

                            shared.changeTaskId += 1;
                            shared.changeTaskId = (shared.changeTaskId >= shared.touchPageSelectorPageBoxes.Length) ? 0 : shared.changeTaskId;
                            parent.NavTaskChange(null, null);
                            changingPage = true;
                            break;
                    }
                }
                else if (dragGesture.IsDragging) //Check for dragging up and down.
                {
                    bDraggingUpDown = true;

                    currentDragY += dragGesture.DragPosition.Y - dragGesture.DragPrevPosition.Y;

                    if (Math.Abs(currentDragY) >= k_DragThreshold)
                    {
                        if (currentDragY < 0)
                        {
                            if (parent.IndexActivePanel < (parent.ActivePanels.Count - 1))
                            {
                                parent.NavReflexNext(null, null);
                                changed = true;
                                hitSomething = true;
                            }
                        }
                        else
                        {
                            if (parent.IndexActivePanel > 0)
                            {
                                parent.NavReflexPrev(null, null);
                                changed = true;
                                hitSomething = true;
                            }
                        }

                        currentDragY = 0.0f;
                    }
                }

                //Reset drag if we're dragging and we recognize the swipe or we're not dragging anymore.
                if (bDraggingUpDown && (!dragGesture.IsDragging || swipeGesture.WasRecognized))
                {
                    bDraggingUpDown = false;
                    currentDragY = 0.0f;
                }



                // Test for hits on the page buttons in order to change tasks
                TapGestureRecognizer tapGesture = TouchGestureManager.Get().TapGesture;
                TouchHoldGestureRecognizer holdGesture = TouchGestureManager.Get().TouchHoldGesture;

                // Hitting the page panel deselects any active panel
                if (shared.touchPageSelectorBackBox.Contains(touchHit))
                {
                    hitSomething = true;
                    //parent.IndexActivePanel = -1;
                }

                for (int i = 0; i < Brain.kCountDefaultTasks; i++)
                {
                    if ((tapGesture.WasRecognized && shared.touchPageSelectorPageBoxes[i].Contains(tapGesture.Position)) ||
                        (holdGesture.WasRecognized && shared.touchPageSelectorPageBoxes[i].Contains(holdGesture.Position)))
                    {
                        hitPage = true;

                        if (shared.activeTaskId != i)
                        {
                            // Page change requested!
                            shared.changeTaskId = i;
                            parent.NavTaskChange(null, null);
                            changingPage = true;
                            break;
                        }
                    }
                }

                if (hitPage &&
                    !changingPage &&
                    holdGesture.WasRecognized)
                {
                    // Set up menu for page and activate.
                    MouseMenu menu = InGame.inGame.Editor.RightClickMenu;
                    menu.DeleteAll();
                    menu.AddText(Strings.Localize("programming.cutPage"));
                    menu.AddText(Strings.Localize("programming.copyPage"));
                    if (CutPasteBuffer != null && CutPasteBuffer.Count != 0)
                    {
                        menu.AddText(Strings.Localize("programming.pastePage"));
                    }
                    menu.Object = null;
                    menu.Activate(touchHit);
                }

                if (!hitSomething)
                {
                    // If use clicks on a reflex, make sure it's the in-focus one.
                    for (int i = 0; i < parent.renderObj.renderList.Count; i++)
                    {
                        ControlRenderObj cro = parent.renderObj.renderList[i] as ControlRenderObj;
                        if (cro != null)
                        {
                            ITransform itrans = cro as ITransform;
                            Matrix mat = Matrix.Invert(itrans.World);

                            IBounding ibound = cro as IBounding;
                            BoundingBox box = ibound.BoundingBox;

                            Vector2 hitUV = TouchInput.GetHitOrtho(touch.position, shared.camera, ref mat, false);

                            if (hitUV.X > box.Min.X && hitUV.X < box.Max.X && hitUV.Y > box.Min.Y && hitUV.Y < box.Max.Y)
                            {
                                changed = false;

                                if (TouchInput.WasTouched)
                                {
                                    touch.TouchedObject = cro;
                                    hitSomething = true;
                                }

                                if ((tapGesture.WasRecognized || holdGesture.WasRecognized) && touch.TouchedObject == cro)
                                {
                                    int steps = i - parent.IndexActivePanel;
                                    while (steps > 0)
                                    {
                                        parent.NavReflexNext(null, null);
                                        --steps;
                                        changed = true;
                                    }
                                    while (steps < 0)
                                    {
                                        parent.NavReflexPrev(null, null);
                                        ++steps;
                                        changed = true;
                                    }
                                    hitSomething = true;
                                }

                                // If we clicked on the already active reflex, dig in a little deeper and figure out which tile we hit (if any).
                                // If the tile is not the in-focus one, make it so.
                                // If the tile is the in-focus one, bring up the pie menu.
                                if (!changed && parent.ActivePanel != null)
                                {
                                    ReflexPanel panel = parent.ActivePanel;

                                    for (int j = 0; j < panel.listControls.Count; j++)
                                    {
                                        ITransform itrans2 = panel.listControls[j] as ITransform;

                                        IBounding ibound2 = null;
                                        if (panel.listControls[j] is ReflexHandle)
                                        {
                                            ReflexHandle rh = panel.listControls[j] as ReflexHandle;
                                            ControlRenderObj cro2 = rh.renderObj as ControlRenderObj;

                                            ibound2 = cro2 as IBounding;
                                        }
                                        else if (panel.listControls[j] is ReflexCard)
                                        {
                                            ReflexCard rc = panel.listControls[j] as ReflexCard;
                                            ControlRenderObj cro2 = rc.renderObj as ControlRenderObj;

                                            ibound2 = cro2 as IBounding;
                                        }
                                        else
                                        {
                                            // Huh?
                                            continue;
                                        }

                                        BoundingBox box2 = ibound2.BoundingBox;

                                        Vector3 position = new Vector3(box2.Min.X, box2.Min.Y, 0.0f);
                                        Vector3 boxSize = new Vector3(box2.Max.X - box2.Min.X, box2.Max.Y - box2.Min.Y, 0.0f);

                                        position = Vector3.Transform(position, itrans2.World);

                                        Point pnt = shared.camera.WorldToScreenCoords(position);
                                        Vector2 pos = new Vector2(pnt.X, pnt.Y);

                                        pnt = shared.camera.WorldToScreenCoords(position + boxSize);
                                        Vector2 size = pos - new Vector2(pnt.X, pnt.Y);
                                        size.X = -size.X;

                                        // Adjust for bizarre yuck foo.
                                        if (j == 0)
                                        {
                                            // Reflex handle
                                            pos.Y -= 95.0f * shared.camera.TutorialScale;
                                            size.X += 10 * shared.camera.TutorialScale;
                                            size.Y -= 35 * shared.camera.TutorialScale;
                                        }
                                        else
                                        {
                                            if (size.X > 40)
                                            {
                                                // Tile
                                                pos.X += 22.0f * shared.camera.TutorialScale;
                                                pos.Y -= 45.0f * shared.camera.TutorialScale;
                                                size.X -= 5.0f * shared.camera.TutorialScale;
                                                size.Y += 55.0f * shared.camera.TutorialScale;
                                            }
                                            else
                                            {
                                                // Plus sign
                                                size.X += 10 * shared.camera.TutorialScale;
                                                size.Y += 80 * shared.camera.TutorialScale;
                                                pos.Y -= 72.0f * shared.camera.TutorialScale;
                                            }
                                        }

                                        // Now we can do a straight-up hit test.
                                        AABB2D hitBox = new AABB2D(pos, pos + size);

                                        Vector2 hit = touch.position;

                                        if (hitBox.Contains(hit))
                                        {
                                            hitSomething = true;
                                            if (TouchInput.WasTouched)
                                            {
                                                touch.TouchedObject = panel.listControls[j];
                                                hitSomething = true;

                                                // If it's the handle, pick up the reflex.
                                                if (j == 0)
                                                {
                                                    ReflexHandle rh = panel.listControls[j] as ReflexHandle;
                                                    if (rh != null)
                                                    {
                                                        rh.MoveReflex(null, null);
                                                    }
                                                }
                                            }

                                            if ((tapGesture.WasRecognized || holdGesture.WasRecognized)
                                                && touch.TouchedObject == panel.listControls[j])
                                            {
                                                int activeCard = panel.ActiveCard;
                                                if (activeCard != j)
                                                {
                                                    // If the tile we hit is not in focus, move the cursor to it.
                                                    int steps = activeCard - j;
                                                    while (steps < 0)
                                                    {
                                                        parent.NavCardNext(null, null);
                                                        ++steps;
                                                    }
                                                    while (steps > 0)
                                                    {
                                                        parent.NavCardPrev(null, null);
                                                        --steps;
                                                    }
                                                }

                                                // The tile we hit is now in focus, bring up pie selector on left mouse
                                                // or rightClickMenu on right mouse.
                                                ReflexCard reflexCard = panel.listControls[j] as ReflexCard;
                                                if (reflexCard != null)
                                                {
                                                    if (tapGesture.WasRecognized)
                                                    {
                                                        reflexCard.updateObjEditCards.ActivatePieSelector();
                                                    }
                                                    else if (holdGesture.WasRecognized)
                                                    {
                                                        // If we haven't clicked on a "null" card
                                                        if (reflexCard.Card.upid != "null")
                                                        {
                                                            // Set up menu for tiles and activate.
                                                            MouseMenu menu = InGame.inGame.Editor.RightClickMenu;
                                                            menu.DeleteAll();
                                                            menu.AddText(Strings.Localize("programming.cutTile"));
                                                            menu.AddText(Strings.Localize("programming.help"));
                                                            menu.Object = reflexCard;
                                                            menu.Activate(hit + new Vector2(0.0f, -10.0f));
                                                            // Delete Tile.
                                                            //reflexCard.ClearCard(null, null);
                                                        }
                                                    }
                                                }

                                                // ReflexHandle
                                                if (j == 0)
                                                {
                                                    ReflexHandle rh = panel.listControls[j] as ReflexHandle;
                                                    if (rh != null)
                                                    {
                                                        if (tapGesture.WasRecognized)
                                                        {
                                                            rh.PlaceReflex(null, null);
                                                        }
                                                        else if (holdGesture.WasRecognized)
                                                        {
                                                            // Set up menu for reflex handle and activate.
                                                            MouseMenu menu = InGame.inGame.Editor.RightClickMenu;
                                                            menu.DeleteAll();
                                                            menu.AddText(Strings.Localize("programming.cutRow"));
                                                            menu.AddText(Strings.Localize("programming.copyRow"));
                                                            // Only add paste if there's something in the buffer.
                                                            if (ReflexPanel.CutPasteBuffer != null && ReflexPanel.CutPasteBuffer.Count != 0)
                                                            {
                                                                menu.AddText(Strings.Localize("programming.pasteRow"));
                                                            }
                                                            menu.Object = rh;
                                                            menu.Activate(hit + new Vector2(0.0f, -10.0f));
                                                            //rh.RemoveReflex(null, null);
                                                        }
                                                    }
                                                }
                                            }
                                        }
                                        else
                                        {
                                            // HitBox doesn't contain a hit so see if we're moving a whole reflex.
                                            if (j == 0 && TouchInput.IsTouched && touch.TouchedObject == panel.listControls[0])
                                            {
                                                ReflexHandle rh = panel.listControls[j] as ReflexHandle;
                                                if (rh != null)
                                                {
                                                    // Need to look on which side of the hitbox we're on and move accordingly.
                                                    // Note this may cause runaway vertical scrolling so we'll have to allow for some delay.

                                                    hitSomething = true;
                                                    // Wait until we're allowed to move again.
                                                    if (nextMoveTime < Time.WallClockTotalSeconds)
                                                    {
                                                        // Up/Down scrolling
                                                        if (touch.position.Y > hitBox.Max.Y)
                                                        {
                                                            ReflexHandle.reflexBlock.MoveDown();

                                                            float dy = touch.position.Y - hitBox.Max.Y;
                                                            if (dy > 100)
                                                            {
                                                                nextMoveTime = Time.WallClockTotalSeconds + 0.2f;
                                                            }
                                                            else
                                                            {
                                                                nextMoveTime = Time.WallClockTotalSeconds + 0.6f;
                                                            }
                                                        }
                                                        else if (touch.position.Y < hitBox.Min.Y)
                                                        {
                                                            ReflexHandle.reflexBlock.MoveUp();

                                                            float dy = hitBox.Min.Y - touch.position.Y;
                                                            if (dy > 100)
                                                            {
                                                                nextMoveTime = Time.WallClockTotalSeconds + 0.2f;
                                                            }
                                                            else
                                                            {
                                                                nextMoveTime = Time.WallClockTotalSeconds + 0.6f;
                                                            }
                                                        }
                                                    }

                                                    // Indent/Unindent
                                                    // Wait until we're allowed to move again.
                                                    // The reason we have the wait here is that without it, because of the
                                                    // twitched motion we would get oscillations in the indentation of the 
                                                    // row.  This is because the hit box is based on the current position 
                                                    // of the element rather than it's intended position.
                                                    if (nextMoveTime < Time.WallClockTotalSeconds)
                                                    {
                                                        if (touch.position.X < hitBox.Min.X)
                                                        {
                                                            ReflexHandle.reflexBlock.Unindent(true);
                                                            nextMoveTime = Time.WallClockTotalSeconds + 0.2f;
                                                        }
                                                        else if (touch.position.X > hitBox.Max.X)
                                                        {
                                                            ReflexHandle.reflexBlock.Indent(true);
                                                            nextMoveTime = Time.WallClockTotalSeconds + 0.2f;
                                                        }
                                                    }

                                                }
                                            }
                                        }

                                    }   // end loop over panel's listcontrols

                                }   // end if reflex is in-focus    

                            }   // end if touch hits reflex.

                            // If touch is not pressed be sure to restore handle position.
                            if (KoiLibrary.LastTouchedDeviceIsTouch && TouchInput.WasReleased)
                            {
                                ReflexPanel panel = parent.ActivePanel;
                                if (panel != null)
                                {
                                    ReflexHandle rh = panel.listControls[0] as ReflexHandle;
                                    if (rh != null && panel.Moving)
                                    {
                                        rh.PlaceReflex(null, null);
                                    }
                                }
                            }
                        }   // end if cro != null
                    } // looped all panels
                }   // end of if touch not causing a page change

                //Annoying way of checking if the sub menus accepted touch input this frame. :S
                if (tapGesture.WasRecognized && !hitSomething &&
                    !InGame.inGame.shared.programmingHelpCard.WasTouchedThisFrame &&
                    !InGame.inGame.shared.textEditor.WasTouchedThisFrame &&
                    !InGame.inGame.shared.microbitPatternEditor.WasTouchedThisFrame)
                {
                    if (null != touch && touch.TouchedObject == null)
                    {
                        Actions.Cancel.ClearAllWasPressedState();
                        GamePadInput.ClearAllWasPressedState(3);

                        parent.Hide(true);
                        Foley.PlayBack();
                    }
                }
            }   // end of HandleTouchInput()

            private void HandleMouseInput()
            {
                if (!KoiLibrary.LastTouchedDeviceIsKeyboardMouse)
                {
                    return;
                }
                Vector2 mouseHit = LowLevelMouseInput.PositionVec;

                // Test for switching tasks or moving the focus to the task handle.  We
                // group these together since the shoulder buttons overlay the task handle.
                if (shared.leftShoulderBox.LeftPressed(mouseHit))
                {
                    parent.NavTaskPrev(null, null);
                }
                if (shared.rightShoulderBox.LeftPressed(mouseHit))
                {
                    parent.NavTaskNext(null, null);
                }

                bool mouseOverShoulderButtons = shared.leftShoulderBox.Contains(mouseHit) || shared.rightShoulderBox.Contains(mouseHit);

                // If the mouse is over one of the shoulder buttons we need to ignore it for this test.
                if (!mouseOverShoulderButtons)
                {
                    if (shared.taskHandleBox.LeftPressed(mouseHit))
                    {
                        // Move focus to task handle.
                        while (parent.IndexActivePanel > -1)
                        {
                            parent.NavReflexPrev(null, null);
                        }
                    }

                    if (shared.taskHandleBox.Contains(mouseHit) && parent.IndexActivePanel == -1 && LowLevelMouseInput.Right.WasPressed)
                    {
                        // Set up menu for page and activate.
                        MouseMenu menu = InGame.inGame.Editor.RightClickMenu;
                        menu.DeleteAll();
                        menu.AddText(Strings.Localize("programming.cutPage"));
                        menu.AddText(Strings.Localize("programming.copyPage"));
                        if (CutPasteBuffer != null && CutPasteBuffer.Count != 0)
                        {
                            menu.AddText(Strings.Localize("programming.pastePage"));
                        }
                        menu.Object = null;
                        menu.Activate(mouseHit);
                    }
                }

                // Scroll wheel to go through reflexes.
                int scroll = LowLevelMouseInput.DeltaScrollWheel;
                if (scroll > 0 && parent.IndexActivePanel >= 0)
                {
                    parent.NavReflexPrev(null, null);
                }
                else if (scroll < 0)
                {
                    parent.NavReflexNext(null, null);
                }

                // If the mouse is over one of the shoulder buttons we need to ignore it for this test.
                if (!mouseOverShoulderButtons)
                {
                    // If use clicks on a reflex, make sure it's the in-focus one.
                    for (int i = 0; i < parent.renderObj.renderList.Count; i++)
                    {
                        ControlRenderObj cro = parent.renderObj.renderList[i] as ControlRenderObj;
                        if (cro != null)
                        {
                            ITransform itrans = cro as ITransform;
                            Matrix mat = Matrix.Invert(itrans.World);

                            IBounding ibound = cro as IBounding;
                            BoundingBox box = ibound.BoundingBox;

                            Vector2 hitUV = LowLevelMouseInput.GetHitOrtho(shared.camera, ref mat, useRtCoords:false);

                            if (hitUV.X > box.Min.X && hitUV.X < box.Max.X && hitUV.Y > box.Min.Y && hitUV.Y < box.Max.Y)
                            {
                                bool changed = false;

                                if (LowLevelMouseInput.Left.WasPressed)
                                {
                                    MouseInput.ClickedOnObject = cro;
                                }
                                if (LowLevelMouseInput.Left.WasReleased && MouseInput.FrameDelayedClickedOnObject == cro)
                                {
                                    int steps = i - parent.IndexActivePanel;
                                    while (steps > 0)
                                    {
                                        parent.NavReflexNext(null, null);
                                        --steps;
                                        changed = true;
                                    }
                                    while (steps < 0)
                                    {
                                        parent.NavReflexPrev(null, null);
                                        ++steps;
                                        changed = true;
                                    }
                                }

                                // If we clicked on the already active reflex, dig in a little deeper and figure out which tile we hit (if any).
                                // If the tile is not the in-focus one, make it so.
                                // If the tile is the in-focus one, bring up the pie menu.
                                if (!changed && parent.ActivePanel != null)
                                {
                                    ReflexPanel panel = parent.ActivePanel;

                                    for (int j = 0; j < panel.listControls.Count; j++)
                                    {
                                        ITransform itrans2 = panel.listControls[j] as ITransform;

                                        IBounding ibound2 = null;
                                        if (panel.listControls[j] is ReflexHandle)
                                        {
                                            ReflexHandle rh = panel.listControls[j] as ReflexHandle;
                                            ControlRenderObj cro2 = rh.renderObj as ControlRenderObj;

                                            ibound2 = cro2 as IBounding;
                                        }
                                        else if (panel.listControls[j] is ReflexCard)
                                        {
                                            ReflexCard rc = panel.listControls[j] as ReflexCard;
                                            ControlRenderObj cro2 = rc.renderObj as ControlRenderObj;

                                            ibound2 = cro2 as IBounding;
                                        }
                                        else
                                        {
                                            // Huh?
                                            continue;
                                        }

                                        BoundingBox box2 = ibound2.BoundingBox;

                                        Vector3 position = new Vector3(box2.Min.X, box2.Min.Y, 0.0f);
                                        Vector3 boxSize = new Vector3(box2.Max.X - box2.Min.X, box2.Max.Y - box2.Min.Y, 0.0f);

                                        position = Vector3.Transform(position, itrans2.World);

                                        Point pnt = shared.camera.WorldToScreenCoords(position);
                                        Vector2 pos = new Vector2(pnt.X, pnt.Y);

                                        pnt = shared.camera.WorldToScreenCoords(position + boxSize);
                                        Vector2 size = pos - new Vector2(pnt.X, pnt.Y);
                                        size.X = -size.X;

                                        // Adjust for bizarre yuck foo.
                                        if (j == 0)
                                        {
                                            // Reflex handle
                                            pos.Y -= 95.0f * shared.camera.TutorialScale;
                                            size.X += 10 * shared.camera.TutorialScale;
                                            size.Y -= 35 * shared.camera.TutorialScale;
                                        }
                                        else
                                        {
                                            // Ungly hack. We differentiate between a plus sign and a tile by their width.
                                            if (size.X > 80)
                                            {
                                                // Tile
                                                pos.X += 22.0f * shared.camera.TutorialScale;
                                                pos.Y -= 45.0f * shared.camera.TutorialScale;
                                                size.X -= 5.0f * shared.camera.TutorialScale;
                                                size.Y += 55.0f * shared.camera.TutorialScale;
                                            }
                                            else
                                            {
                                                // Plus sign
                                                size.X += 10 * shared.camera.TutorialScale;
                                                size.Y += 80 * shared.camera.TutorialScale;
                                                pos.Y -= 72.0f * shared.camera.TutorialScale;
                                            }
                                        }

                                        // Now we can do a straight-up hit test.
                                        AABB2D hitBox = new AABB2D(pos, pos + size);

                                        Vector2 hit = LowLevelMouseInput.PositionVec;

                                        if (hitBox.Contains(hit))
                                        {
                                            if (LowLevelMouseInput.Left.WasPressed)
                                            {
                                                LowLevelMouseInput.Left.ClearAllWasPressedState();
                                                MouseInput.ClickedOnObject = panel.listControls[j];

                                                // If it's the handle, pick up the reflex.
                                                if (j == 0)
                                                {
                                                    ReflexHandle rh = panel.listControls[j] as ReflexHandle;
                                                    if (rh != null)
                                                    {
                                                        rh.MoveReflex(null, null);
                                                    }
                                                }
                                            }
                                            // Use right click to activate right click menu.
                                            if (LowLevelMouseInput.Right.WasPressed)
                                            {
                                                LowLevelMouseInput.Right.ClearAllWasPressedState();
                                                MouseInput.ClickedOnObject = panel.listControls[j];
                                            }
                                            if ((LowLevelMouseInput.Left.WasReleased || LowLevelMouseInput.Right.WasReleased) && MouseInput.FrameDelayedClickedOnObject == panel.listControls[j])
                                            {
                                                int activeCard = panel.ActiveCard;
                                                if (activeCard != j)
                                                {
                                                    // If the tile we hit is not in focus, move the cursor to it.
                                                    int steps = activeCard - j;
                                                    while (steps < 0)
                                                    {
                                                        parent.NavCardNext(null, null);
                                                        ++steps;
                                                    }
                                                    while (steps > 0)
                                                    {
                                                        parent.NavCardPrev(null, null);
                                                        --steps;
                                                    }
                                                }

                                                // The tile we hit is now in focus, bring up pie selector on left mouse
                                                // or rightClickMenu on right mouse.
                                                ReflexCard reflexCard = panel.listControls[j] as ReflexCard;
                                                if (reflexCard != null)
                                                {
                                                    if (LowLevelMouseInput.Left.WasReleased)
                                                    {
                                                        reflexCard.updateObjEditCards.ActivatePieSelector();
                                                    }
                                                    else if (LowLevelMouseInput.Right.WasReleased)
                                                    {
                                                        // If we haven't clicked on a "null" card
                                                        if (reflexCard.Card.upid != "null")
                                                        {
                                                            // Set up menu for tiles and activate.
                                                            MouseMenu menu = InGame.inGame.Editor.RightClickMenu;
                                                            menu.DeleteAll();
                                                            menu.AddText(Strings.Localize("programming.cutTile"));
                                                            menu.AddText(Strings.Localize("programming.help"));
                                                            menu.Object = reflexCard;
                                                            menu.Activate(hit + new Vector2(0.0f, -10.0f));
                                                            // Delete Tile.
                                                            //reflexCard.ClearCard(null, null);
                                                        }
                                                    }
                                                }

                                                // ReflexHandle
                                                if (j == 0)
                                                {
                                                    ReflexHandle rh = panel.listControls[j] as ReflexHandle;
                                                    if (rh != null)
                                                    {
                                                        if (LowLevelMouseInput.Left.WasReleased)
                                                        {
                                                            rh.PlaceReflex(null, null);
                                                        }
                                                        else if (LowLevelMouseInput.Right.WasReleased)
                                                        {
                                                            // Set up menu for reflex handle and activate.
                                                            MouseMenu menu = InGame.inGame.Editor.RightClickMenu;
                                                            menu.DeleteAll();
                                                            menu.AddText(Strings.Localize("programming.cutRow"));
                                                            menu.AddText(Strings.Localize("programming.copyRow"));
                                                            // Only add paste if there's something in the buffer.
                                                            if (ReflexPanel.CutPasteBuffer != null && ReflexPanel.CutPasteBuffer.Count != 0)
                                                            {
                                                                menu.AddText(Strings.Localize("programming.pasteRow"));
                                                            }
                                                            menu.Object = rh;
                                                            menu.Activate(hit + new Vector2(0.0f, -10.0f));
                                                            //rh.RemoveReflex(null, null);
                                                        }
                                                    }
                                                }
                                            }

                                        }
                                        else
                                        {
                                            // HitBox doesn't contain a hit so see if we're moving a whole reflex.
                                            if (j == 0 && LowLevelMouseInput.Left.IsPressed && MouseInput.ClickedOnObject == panel.listControls[0])
                                            {
                                                ReflexHandle rh = panel.listControls[j] as ReflexHandle;
                                                if (rh != null)
                                                {
                                                    // Need to look on which side of the hitbox we're on and move accordingly.
                                                    // Note this may cause runaway vertical scrolling so we'll have to allow for some delay.

                                                    // Wait until we're allowed to move again.
                                                    if (nextMoveTime < Time.WallClockTotalSeconds)
                                                    {
                                                        // Up/Down scrolling
                                                        if (LowLevelMouseInput.Position.Y > hitBox.Max.Y)
                                                        {
                                                            ReflexHandle.reflexBlock.MoveDown();

                                                            float dy = LowLevelMouseInput.Position.Y - hitBox.Max.Y;
                                                            if (dy > 100)
                                                            {
                                                                nextMoveTime = Time.WallClockTotalSeconds + 0.2f;
                                                            }
                                                            else
                                                            {
                                                                nextMoveTime = Time.WallClockTotalSeconds + 0.6f;
                                                            }
                                                        }
                                                        else if (LowLevelMouseInput.Position.Y < hitBox.Min.Y)
                                                        {
                                                            ReflexHandle.reflexBlock.MoveUp();

                                                            float dy = hitBox.Min.Y - LowLevelMouseInput.Position.Y;
                                                            if (dy > 100)
                                                            {
                                                                nextMoveTime = Time.WallClockTotalSeconds + 0.2f;
                                                            }
                                                            else
                                                            {
                                                                nextMoveTime = Time.WallClockTotalSeconds + 0.6f;
                                                            }
                                                        }
                                                    }

                                                    // Indent/Unindent
                                                    // Wait until we're allowed to move again.
                                                    // The reason we have the wait here is that without it, because of the
                                                    // twitched motion we would get oscillations in the indentation of the 
                                                    // row.  This is because the hit box is based on the current position 
                                                    // of the element rather than it's intended position.
                                                    if (nextMoveTime < Time.WallClockTotalSeconds)
                                                    {
                                                        if (LowLevelMouseInput.Position.X < hitBox.Min.X)
                                                        {
                                                            ReflexHandle.reflexBlock.Unindent(true);
                                                            nextMoveTime = Time.WallClockTotalSeconds + 0.2f;
                                                        }
                                                        else if (LowLevelMouseInput.Position.X > hitBox.Max.X)
                                                        {
                                                            ReflexHandle.reflexBlock.Indent(true);
                                                            nextMoveTime = Time.WallClockTotalSeconds + 0.2f;
                                                        }
                                                    }

                                                }
                                            }
                                        }

                                    }   // end loop over panel's listcontrols

                                }   // end if reflex is in-focus    

                            }   // end if mouse hits reflex.

                            // If mouse is not pressed be sure to restore handle position.
                            if (KoiLibrary.LastTouchedDeviceIsKeyboardMouse && LowLevelMouseInput.Left.WasReleased)
                            {
                                ReflexPanel panel = parent.ActivePanel;
                                if (panel != null)
                                {
                                    ReflexHandle rh = panel.listControls[0] as ReflexHandle;
                                    if (rh != null && panel.Moving)
                                    {
                                        rh.PlaceReflex(null, null);
                                    }
                                }
                            }

                        }   // end if cro != null
                    }
                }   // end of if mouse not over a shoulder button.
            }

            public override void Update()
            {
                if (AuthUI.IsModalActive)
                {
                    return;
                }

                // <<<<<<<<<<<<<<<<<<<<<<<<<<<<<< FULL SCREEN WINDOWED MODE FIX
                // TODO Maybe limit the resolution we tell the camera about so that
                // the kode doesn't feel too small on big screens???
                shared.camera.Resolution = new Point((int)BokuGame.ScreenSize.X, (int)BokuGame.ScreenSize.Y);
                // FULL SCREEN WINDOWED MODE FIX >>>>>>>>>>>>>>>>>>>>>>>>>>>>>
                
                shared.camera.Update();

                // Ugly, ugly, ugly.  When a paste happens the renderlist objects get totally out
                // of sync with the reflex and panel arrays.  The easiest way to keep them correct
                // is just to sort them by their Y offset.
                ReflexBlock.ReflexComparer comp = new ReflexBlock.ReflexComparer();
                InGame.inGame.Editor.renderObj.renderList.Sort(comp);

                bool editorActive = InGame.inGame.shared.textEditor.Active 
                                    || InGame.inGame.shared.microbitPatternEditor.Active 
                                    || InGame.inGame.shared.editObjectParameters.Active
                                    || InGame.inGame.shared.editWorldParameters.Active;

                // Update the help card, text editor and right click menu first so they get a shot at stealing input.
                InGame.inGame.shared.programmingHelpCard.Update();
                InGame.inGame.shared.textEditor.Update();
                if (InGame.inGame.shared.textLineDialog.Active)
                {
                    InGame.inGame.shared.textLineDialog.Update();
                }
                InGame.inGame.shared.microbitPatternEditor.Update();
                InGame.inGame.shared.editObjectParameters.Update();
                InGame.inGame.shared.editWorldParameters.Update();

                if (editorActive)
                {
                    // If editors are active (or were before the above update), bail early so no input leaks through.
                    // Also be sure to clear any tooltips which would otherwise be rendered over the top of the editor.
                    // This might benefit from some looking at.  We need to call Update() here because this early-outs
                    // but it might make sense to move the call to Update() to above here.
                    ToolTipManager.Update();
                    ToolTipManager.Clear();
                    return;
                }

                // Need to cache the active state of the RightClickMenu.  The problem is that after
                // deactivating, touch events can still leak through.  The long term solution is to
                // do something like ignoreUntilReleased for touch.  Until then, this should fix the problem.
                bool rightClickMenuWasActive = InGame.inGame.Editor.RightClickMenu.Active;
                InGame.inGame.Editor.RightClickMenu.Update();

                // JW - Bail early if loadLevelMenu is active. We don't want to steal input from that scene!
                if (BokuGame.bokuGame.loadLevelMenu.Active)
                {
                    return;
                }

                // Bail early if rightClickMenu is active.
                if (InGame.inGame.Editor.RightClickMenu.Active || rightClickMenuWasActive)
                {
                    // First, check that the command stack is right.  Argh.
                    if (CommandStack.Peek() != InGame.inGame.Editor.RightClickMenu.CommandMap)
                    {
                        // Force to the top...
                        CommandStack.Pop(InGame.inGame.Editor.RightClickMenu.CommandMap);
                        CommandStack.Push(InGame.inGame.Editor.RightClickMenu.CommandMap);
                    }

                    return;
                }

                // Update the parent's list of objects.
                //Debug.Print("");
                for (int i = 0; i < updateList.Count; i++)
                {
                    UpdateObject obj = updateList[i] as UpdateObject;
                    Debug.Assert(obj != null);
                    obj.Update();
                    //Debug.Print(obj.UniqueNum.ToString());
                }

                // WARNING!!!  Ugly hack in place.  Whenever the cursor is on
                // the Page Handle, check the command and help overlay stacks
                // and rebuild them to a correct, known state.
                if (parent.IndexActivePanel == -1)
                {
                    //
                    // Help overlay
                    //
                    if (HelpOverlay.Depth() != 3 ||
                        (HelpOverlay.Peek(0) != @"PageHandleEmptyPasteBuffer" && HelpOverlay.Peek(0) != @"PageHandleFullPasteBuffer") ||
                        HelpOverlay.Peek(1) != @"Programming" ||
                        HelpOverlay.Peek(2) != @"RunSimulation")
                    {
                        // Rebuild help overlay stack.
                        HelpOverlay.Clear();
                        HelpOverlay.Push(@"RunSimulation");
                        HelpOverlay.Push(@"Programming");

                        // Push Page Handle
                        if (CutPasteBuffer == null)
                        {
                            HelpOverlay.Push(@"PageHandleEmptyPasteBuffer");
                        }
                        else
                        {
                            HelpOverlay.Push(@"PageHandleFullPasteBuffer");
                        }
                    }

                    //
                    // CommandStack
                    //
                    if (CommandStack.Depth() != 3 ||
                        CommandStack.Peek(0) != commandMapPageHandle ||
                        CommandStack.Peek(1) != commandMap ||
                        CommandStack.Peek(2) != InGame.inGame.EditBaseCommandMap)
                    {
                        // Empty the stack.
                        while (CommandStack.Depth() > 0)
                        {
                            CommandStack.Pop(CommandStack.Peek());
                        }

                        // Rebuild the stack.
                        CommandStack.Push(InGame.inGame.EditBaseCommandMap);    // InGameEditBase
                        CommandStack.Push(commandMap);                          // BrainEditor
                        CommandStack.Push(commandMapPageHandle);                // PageHandle
                    }

                    // Now for some final hackery...  For some reason after a paste some
                    // of the handles for the reflexes show up hot.  Force them to chill.
                    for (int i = 0; i < parent.ActivePanels.Count; i++)
                    {
                        ReflexPanel panel = parent.ActivePanels[i];
                        ReflexHandle handle = panel.listControls[0] as ReflexHandle;
                        if (handle != null && handle.State == ReflexHandle.States.Hot)
                        {
                            handle.PendingState = ReflexHandle.States.Active;
                            BokuGame.objectListDirty = true;
                        }
                    }
                }
                else
                {
                    // More command stack hackery.
                    // After a full page delete the command stack ends up with an extra ReflexHandle.

                    if (CommandStack.Depth() == 5 && CommandStack.Peek(2).name == @"ReflexHandle")
                    {
                        CommandStack.Remove(2);

                        // Also take this opportunity to move back to the Page Handle.
                        parent.NavReflexPrev(null, null);
                    }

                }

                /*
                if (tic != (int)Time.WallClockTotalSeconds)
                {
                    tic = (int)Time.WallClockTotalSeconds;

                    Debug.Print("===" + tic.ToString());
                    Debug.Print("camera at " + parent.Camera.At.ToString());
                    // always 0 0 0 Debug.Print(parent.localTransform.OriginTranslation.ToString());
                    // always 0 0 0 Debug.Print(parent.localTransform.Translation.ToString());
                    Debug.Print("panels " + parent.activePanels.Count.ToString());
                    for (int i = 0; i < parent.activePanels.Count; i++)
                    {
                        ReflexPanel p = parent.ActivePanels[i];
                        ITransform t = p as ITransform;
                        Debug.Print("  " + i.ToString() + " local " + t.Local.Translation.ToString());
                        Debug.Print("  " + i.ToString() + " world " + t.World.Translation.ToString());
                    }

                }
                */

                bool editObjectParametersActive = InGame.inGame.shared.editObjectParameters.Active;
                bool editWorldParametersActive = InGame.inGame.shared.editWorldParameters.Active;

                // Since we're here we know that the editor is active.  We also
                // know that the children have already been updated meaning that
                // any input they're interested in they've already taken and 
                // cleared.  So, at this point we can just assume that any
                // remaining input is ours to do with as we please with no
                // further need to check input focus EXCEPT if the helpCard,
                // pic selector or text editor is up.

                if (!CommandStack.Peek().name.StartsWith(@"ProgrammingHelpCard") 
                    && !CommandStack.Peek().name.StartsWith(@"AddItemHelpCard")
                    && !CommandStack.Peek().name.StartsWith(@"PieSelector")
                    && !CommandStack.Peek().name.StartsWith(@"NotPieSelector")
                    && !CommandStack.Peek().name.StartsWith(@"TextEditor")
                    && !CommandStack.Peek().name.StartsWith(@"MicrobitPatternEditor")
                    && !editObjectParametersActive
                    && !editWorldParametersActive
                    && !CommandStack.Peek().name.StartsWith(@"InGameEditWorldParameters"))
                {
                    HandleTouchInput();
                    HandleMouseInput();

                    GamePadInput pad = GamePadInput.GetGamePad0();

                    if (parent.IndexActivePanel == -1)
                    {
                        // On Page Handle...

                        // Exit programming.
                        if (Actions.Cancel.WasPressed)
                        {
                            Actions.Cancel.ClearAllWasPressedState();
                            GamePadInput.ClearAllWasPressedState(3);

                            parent.Hide(pad.Back.WasPressed);

                            CommandStack.Pop(this.commandMapPageHandle);
                            HelpOverlay.Pop();
                        }

                        // Navigate back to reflexes.
                        if (Actions.Down.WasPressedOrRepeat)
                        {
                            Actions.Down.ClearAllWasPressedState();
                            GamePadInput.ClearAllWasPressedState(3);

                            parent.NavReflexNext(null, null);
                        }

                        // Cut full page.
                        if (Actions.Cut.WasPressed)
                        {
                            Actions.Cut.ClearAllWasPressedState();

                            // Copy all the ReflexData off to the buffer.
                            if (CutPasteBuffer == null)
                            {
                                CutPasteBuffer = new List<ReflexData>();
                            }
                            // Remove any existing reflexes.
                            CutPasteBuffer.Clear();

                            // Copy the reflex data to the buffer.
                            for (int i = 0; i < parent.ActivePanels.Count; i++)
                            {
                                CutPasteBuffer.Add(parent.ActivePanels[i].Reflex.Data);
                            }

                            // Add a blank reflex to the end.
                            parent.InsertReflex(null);

                            // Delete all the other reflexes.
                            int count = parent.ActivePanels.Count - 1;
                            for (int i = 0; i < count; i++)
                            {
                                parent.ActivePanels[0].RemoveReflex();
                            }

                            // Cause camera offset to be reset.
                            parent.firstTime = true;
                        }

                        // Copy full page
                        if (Actions.ProgrammingEditorCopy.WasPressed)
                        {
                            Actions.ProgrammingEditorCopy.ClearAllWasPressedState();

                            // Copy all the ReflexData off to the buffer.
                            if (CutPasteBuffer == null)
                            {
                                CutPasteBuffer = new List<ReflexData>();
                            }
                            // Remove any existing reflexes.
                            CutPasteBuffer.Clear();

                            // Copy the reflex data to the buffer.
                            for (int i = 0; i < parent.ActivePanels.Count; i++)
                            {
                                CutPasteBuffer.Add(parent.ActivePanels[i].Reflex.Data);
                            }

                            HelpOverlay.Pop();
                            if (Editor.CutPasteBuffer == null)
                            {
                                HelpOverlay.Push("PageHandleEmptyPasteBuffer");
                            }
                            else
                            {
                                HelpOverlay.Push("PageHandleFullPasteBuffer");
                            }

                        }

                        // Paste full page.
                        if (Actions.Paste.WasPressed)
                        {
                            Actions.Paste.ClearAllWasPressedState();

                            if (CutPasteBuffer != null)
                            {
                                int prevReflexCount = parent.ActivePanels.Count;
                                // Add the buffer reflexes onto the end of the existing reflexes.
                                parent.IndexActivePanel = parent.ActivePanels.Count - 1;
                                for (int i = 0; i < CutPasteBuffer.Count; i++)
                                {
                                    // Insert a new reflex at the end of the list.
                                    ReflexPanel panel = InGame.inGame.Editor.InsertReflex(null);

                                    // Paste the cut/paste code into this new panel and tell it to rebuild.
                                    panel.Reflex.Paste(CutPasteBuffer[i]);
                                    panel.uiRebuild = true;
                                }

                                // If there was only a single reflex before the paste and it was empty, delete it.
                                if (prevReflexCount == 1)
                                {
                                    if (parent.ActivePanels[0].Reflex.Data.IsEmpty())
                                    {
                                        parent.ActivePanels[0].RemoveReflex();
                                    }
                                }

                                // Make sure all the panels aren't hot.
                                for (int i = 0; i < parent.ActivePanels.Count; i++)
                                {
                                    ReflexPanel panel = parent.ActivePanels[i];
                                    panel.pendingState = ReflexPanel.States.Active;
                                    foreach (IControl control in panel.listControls)
                                    {
                                        control.Hot = false;
                                    }
                                }

                                // Move the cursor to the page handle.
                                parent.IndexActivePanel = -1;
                                UiCursor.ActiveCursor.Parent = parent.fugly;
                            }
                        }

                        // When on page handle, have left/right change pages.
                        if (Actions.Prev.WasPressedOrRepeat || Actions.Left.WasPressedOrRepeat)
                        {
                            Actions.Prev.ClearAllWasPressedState();
                            Actions.Left.ClearAllWasPressedState();
                            GamePadInput.ClearAllWasPressedState(3);

                            parent.NavTaskPrev(null, null);
                        }

                        if (Actions.Next.WasPressedOrRepeat || Actions.Right.WasPressedOrRepeat)
                        {
                            Actions.Next.ClearAllWasPressedState();
                            Actions.Right.ClearAllWasPressedState();
                            GamePadInput.ClearAllWasPressedState(3);

                            parent.NavTaskNext(null, null);
                        }
                    }
                    else
                    {
                        // Else not on page handle.

                        // Go back to tool menu if either <B> or <Back> is pressed.
                        if (Actions.Cancel.WasPressed)
                        {
                            Actions.Cancel.ClearAllWasPressedState();
                            GamePadInput.ClearAllWasPressedState(3);

                            parent.Hide(pad.Back.WasPressed);
                            Foley.PlayBack();
                        }

                        if (Actions.Up.WasPressedOrRepeat)
                        {
                            Actions.Up.ClearAllWasPressedState();
                            GamePadInput.ClearAllWasPressedState(2);

                            parent.NavReflexPrev(null, null);
                        }

                        if (Actions.Down.WasPressedOrRepeat)
                        {
                            Actions.Down.ClearAllWasPressedState();
                            GamePadInput.ClearAllWasPressedState(2);

                            parent.NavReflexNext(null, null);
                        }

                        // Don't allow side to side cursor movement when a reflex is being moved.
                        if (parent.ActivePanel == null || !parent.ActivePanel.Moving)
                        {
                            if (Actions.Left.WasPressedOrRepeat)
                            {
                                Actions.Left.ClearAllWasPressedState();
                                GamePadInput.ClearAllWasPressedState(3);

                                parent.NavCardPrev(null, null);
                            }

                            if (Actions.Right.WasPressedOrRepeat)
                            {
                                Actions.Right.ClearAllWasPressedState();
                                GamePadInput.ClearAllWasPressedState(3);

                                parent.NavCardNext(null, null);
                            }
                        }
                    }

                    // Don't allow switching tasks while the pie menu is active.
                    if (!CommandStack.Peek().name.StartsWith(@"PieSelector"))
                    {
                        if (Actions.Prev.WasPressedOrRepeat)
                        {
                            Actions.Prev.ClearAllWasPressedState();
                            GamePadInput.ClearAllWasPressedState(3);

                            parent.NavTaskPrev(null, null);
                        }
                        else if (Actions.Next.WasPressedOrRepeat)
                        {
                            Actions.Next.ClearAllWasPressedState();
                            GamePadInput.ClearAllWasPressedState(3);

                            parent.NavTaskNext(null, null);
                        }
                    }

                    // Print the kode for the current actor?
                    if (Actions.PrintKodu.WasPressed && (CommandStack.Peek().name.StartsWith(@"PageHandle") || CommandStack.Peek().name.StartsWith(@"ReflexHandle")))
//                    if (Actions.PrintKodu.WasPressed && CommandStack.Peek().name.StartsWith(@"PageHandle"))
                    {
                        Actions.PrintKodu.ClearAllWasPressedState();

                        Print.PrintProgramming(parent.GameActor);
                    }
                }

                // Don't mess with ToolTips or help if pie selector, text editor, terrain, water picker or ProgrammingHelpCard is active.
                bool dialogActive = CommandStack.Peek().name.StartsWith(@"MessageDialog");
                bool pieActive = CommandStack.Peek().name.StartsWith(@"PieSelector");
                bool pickerActive = CommandStack.Peek().name.StartsWith(@"TerrainEdit");
                bool notPieActive = CommandStack.Peek().name.StartsWith(@"NotPieSelector");
                bool textEditorActive = CommandStack.Peek().name.StartsWith(@"TextEditor");
                bool microbitPatternEditorActive = CommandStack.Peek().name.StartsWith(@"MicrobitPatternEditor");
                if (!(dialogActive || pieActive || pickerActive || notPieActive || textEditorActive || microbitPatternEditorActive || editObjectParametersActive || editWorldParametersActive || ProgrammingHelpCard.Instance.Active))
                {
                    // In touch mode this can get out of sync.
                    // Force it to a good state.  (HACK)
                    // The problem appears to be that the right click menu is updated way earlier than
                    // the code that normally looks at input.  We can't move the right click menu Update 
                    // to that location because it needs to bail early to prevent input leaking from the
                    // touch input.  Problem is a combo of touch input leaking along with all this code
                    // being too brittle.  Need to decide whether to make touch act like other input
                    // or rethink all input.
                    if (parent.ActivePanel == null && parent.IndexActivePanel != -1)
                    {
                        parent.IndexActivePanel = -1;
                    }
                    
                    if (parent.IndexActivePanel == -1)
                    {
                        // On page icon at top of display.
                        ITransform trans = parent.fugly as ITransform;
                        if (trans != null)
                        {
                            Vector3 pos = trans.World.Translation;
                            pos += new Vector3(0.5f, 0.0f, 0.0f);
                            Point loc = shared.camera.WorldToScreenCoords(pos);
                            ToolTipManager.ShowTip(Strings.Localize("toolTips.pageHandleLabel"), Strings.Localize("toolTips.pageHandleDesc"), new Vector2(loc.X, loc.Y), false);
                        }
                        else
                        {
                            ToolTipManager.Clear();
                        }
                    } 
                    else
                    {
                        ReflexCard card = parent.ActivePanel.listControls[parent.ActivePanel.ActiveCard] as ReflexCard;
                        if (card != null)
                        {
                            string upid = card.Card.upid;

                            string label = CardSpace.Cards.GetLabel(upid);
                            string desc = CardSpace.Cards.GetHelpDescription(upid);

                            if (desc != null)
                            {
                                ITransform trans = card as ITransform;
                                Vector3 pos = trans.World.Translation;
                                pos += new Vector3(0.5f, -0.5f, 0.0f);
                                Point loc = shared.camera.WorldToScreenCoords(pos);
                                if (upid != "null")
                                {
                                    if (!KoiLibrary.LastTouchedDeviceIsTouch)
                                    {
                                        ToolTipManager.ShowTip(label, desc, new Vector2(loc.X, loc.Y), true);
                                    }
                                    
                                    HelpOverlay.SuppressYButton = false;

                                    if (Actions.Help.WasPressed || shared.showHelp)
                                    {
                                        Actions.Help.ClearAllWasPressedState();

                                        ProgrammingElement focusElement = card.Card;
                                        InGame.inGame.shared.programmingHelpCard.Activate(focusElement, null);
                                    }

                                }
                                else
                                {
                                    // Showing "blank"
                                    if (!KoiLibrary.LastTouchedDeviceIsTouch)
                                    {
                                        ToolTipManager.ShowTip(label, desc, new Vector2(loc.X, loc.Y), true);
                                    }
                                    HelpOverlay.SuppressYButton = true;
                                }
                            }
                            else
                            {
                                HelpOverlay.SuppressYButton = true;
                            }
                        }
                        else if (parent.ActivePanel.ActiveCard == 0)
                        {
                            // On row handle.  (Don't show tip when moving since cut/past isn't valid then).
                            if (!parent.ActivePanel.Moving)
                            {
                                ITransform trans = parent.ActivePanel as ITransform;
                                Vector3 pos = trans.World.Translation;
                                pos += new Vector3(-3.3f, -0.45f, 0.0f);
                                Point loc = shared.camera.WorldToScreenCoords(pos);
                                ToolTipManager.ShowTip(Strings.Localize("toolTips.rowHandleLabel"), Strings.Localize("toolTips.rowHandleDesc"), new Vector2(loc.X, loc.Y), false);
                            }
                            else
                            {
                                ToolTipManager.Clear();
                            }
                        }
                        else
                        {
                            ToolTipManager.Clear();
                        }
                    }
                    shared.showHelp = false;
                }


                // Also if we're in touch mode, we want to clear the tip.
                
                // If the text editor is active (adding text for 'say' verb) then
                // we want to disable the tooltip since it will be rendered on top
                // of the editor.
                if (KoiLibrary.LastTouchedDeviceIsTouch ||
                    textEditorActive || microbitPatternEditorActive || editObjectParametersActive || editWorldParametersActive)
                {
                    ToolTipManager.Clear();
                }

                ToolTipManager.Update();

                // Always have a blank row available.  Two behaviour options:
                // 1) Always keep a blank row at the end.
                // 2) Only insert a new blank row when no blank rows exist.
#if BLANK_AT_END
                // Look at the last reflex.  If it's not completely blank,
                // add a new blank reflex to the programming.
                ReflexPanel reflexPanel = parent.ActivePanels[parent.ActivePanels.Count - 1];
                ReflexData data = reflexPanel.Reflex.Data;
                if (!data.IsEmpty())
                {
                    parent.InsertReflex(null);
                }
#else
                bool blank = false;
                for (int i = 0; i < parent.ActivePanels.Count; i++)
                {
                    ReflexPanel reflexPanel = parent.ActivePanels[i];
                    ReflexData data = reflexPanel.Reflex.Data;
                    if (data.IsEmpty())
                    {
                        blank = true;
                        break;
                    }
                }
                if (!blank)
                {
                    parent.InsertReflex(null);
                }
#endif

            }   // end of UpdateObj Update()

            public override void Activate()
            {
                CommandStack.Push(commandMap);
            }
            public override void Deactivate()
            {
                CommandStack.Pop(commandMap);
                ToolTipManager.Clear();

#if !EXTERNAL
                if (BokuGame.Running)
                {
#if !NETFX_CORE

#if OUTPUT_KODE_TO_CLIPBOARD
                    // Copy program to the clipboard (to aid help database generation)
                    ExampleProgram program = ExampleProgram.FromBrain(parent.gameActor.Brain);
                    System.IO.MemoryStream stream = new System.IO.MemoryStream();
                    System.Xml.Serialization.XmlSerializer serializer = new System.Xml.Serialization.XmlSerializer(typeof(ExampleProgram));
                    serializer.Serialize(stream, program);
                    stream.Position = 0;
                    System.IO.StreamReader reader = new System.IO.StreamReader(stream);
                    try { System.Windows.Forms.Clipboard.SetText(reader.ReadToEnd()); }
                    catch { }
#endif  // OUTPUT_KODE_TO_CLIPBOARD

#endif  // !NETFX_CORE
                }   // end if Running
#endif
            }
        }   // end of class UpdateObj

        public class RenderObj : RenderObject, INeedsDeviceReset
        {
            private Shared shared = null;
            //private TextureCube envTexture;
            public List<RenderObject> renderList = null; // Children's render list.
            public string labelTask;
            public Texture2D taskTexture = null;

            public Texture2D[] touchPageSelectorTextures = new Texture2D[Brain.kCountDefaultTasks];
            public Vector2 selectedPageSize = new Vector2(128, 128);
            public Vector2 unselectedPageSize = new Vector2(96, 96);

//            public Billboard rowBackdrop = null;

            public RenderObj( Editor parent, Shared shared )
            {
                this.shared = shared;
                renderList = new List<RenderObject>();

            }   // end of RenderObj c'tor

            // For offsetting the camera when it's over an empty plus sign.
            private Vector3 cameraOffset = Vector3.Zero;    // Value used to render camera.
            private Vector3 targetOffset = Vector3.Zero;    // Value we're twitching toward.

            public override void Render(Camera camera)
            {
                // Render mode for capturing programming tiles w/o pencil or background.
                bool debugRender = Actions.ShiftPrintScreen.WasPressed;

                GraphicsDevice device = KoiLibrary.GraphicsDevice;

                if (debugRender)
                {
                    device.Clear(Color.Transparent);
                }

                // Match viewport size to actual screen.
                InGame.SetViewportToScreen();

                BokuGame.bokuGame.shaderGlobals.SetValues(Editor.effect);
                BokuGame.bokuGame.shaderGlobals.SetCamera(Editor.effect, shared.camera);

#if NETFX_CORE
                // TODO (****) Not sure why this is needed for MG but not for XNA.
                KoiLibrary.GraphicsDevice.Clear(ClearOptions.DepthBuffer, Color.Pink, 1.0f, 0);
#endif

                // Don't render editor UI when HelpCard is active.  It
                // still works if you do but it looks more cluttered.
                if (!(InGame.inGame.shared.programmingHelpCard.Active || InGame.inGame.shared.addItemHelpCard.Active || InGame.inGame.shared.textEditor.Active || InGame.inGame.shared.microbitPatternEditor.Active || InGame.inGame.shared.editObjectParameters.Active || InGame.inGame.shared.editWorldParameters.Active))
                {

                    Editor.effect.Parameters["LightDirection0"].SetValue(new Vector4(Vector3.Normalize(new Vector3(4.0f, -4.0f, -11.5f)), 1.0f));
                    Editor.effect.Parameters["LightColor0"].SetValue(new Vector3(1.0f, 1.0f, 1.0f));
                    Editor.effect.Parameters["EyeLocation"].SetValue(new Vector4(shared.camera.From, 1.0f));

                    Editor.effect.Parameters["Shininess"].SetValue(1.0f);
                    Editor.effect.Parameters["ShadowAttenuation"].SetValue(0.5f);

                    //effect.Parameters["EnvironmentMap"].SetValue(envTexture);

                    // Render the parent's list of objects using our local camera.
                    // Do this first so it writes beneath the task icon.

                    // Figure out where the cursor should be.
                    UiCamera c = shared.camera as UiCamera;
                    c.Offset = Vector3.Zero;
                    ITransform cursorTransform = UiCursor.ActiveCursor as ITransform;
                    float margin = 1.5f;
                    if (cursorTransform.World.Translation.X + margin > c.Width / 2.0f)
                    {
                        Vector3 offset = c.Offset;
                        offset.X = cursorTransform.World.Translation.X + margin - c.Width / 2.0f;
                        c.Offset = offset;
                    }

                    // Use the leftShoulderBox as a reference for where the pencil should be.
                    Vector3 tut = Vector3.Zero;
                    float unitsPerPixel = c.Height / c.Resolution.Y;
                    tut.Y = shared.leftShoulderBox.Min.Y * unitsPerPixel;
                    // Note the 0.3 added on is just to adjust where the pencil points relative to the task icon.
                    tut.Y = c.TutorialScale * (c.Height / 2.0f - tut.Y - 0.3f);

                    // Update fugly to know where to put the cursor, if needed.
                    InGame.inGame.Editor.fugly.Local.Translation = c.At + tut;


                    // Now, through the wonders of ITransform, we need to tell the cursor to incorporate 
                    // the position from its parent.  Note that we can't just set the parent to null and 
                    // set the values directly.  Oh no, that would be too easy, too sane, too understandable.  
                    // Instead we have to create a convoluted placeholder object which actually ends up being 
                    // ignored for the most part.  How cool is that?
                    if (cursorTransform.Parent == InGame.inGame.Editor.fugly)
                    {
                        Matrix world = InGame.inGame.Editor.fugly.World;
                        cursorTransform.Recalc(ref world);
                    }

                    // Cull objects vertically since program lists may extend 
                    // way above or below the current camera view.  For objects
                    // that we can't figure out the extents for, just render
                    // them anyway.
                    float cameraMinY = shared.camera.At.Y - shared.camera.Height / 2.0f;
                    float camerMaxY = shared.camera.At.Y + shared.camera.Height / 2.0f;

                    InGame.inGame.Editor.RenderPieMenus = false;
                    InGame.inGame.Editor.PieMenuList.Clear();
                    InGame.inGame.Editor.NotPieMenuList.Clear();

                    for (int i = 0; i < renderList.Count; i++)
                    {
                        ControlRenderObj cro = renderList[i] as ControlRenderObj;

                        /*
                        for (int ii = 0; ii < cro.ListActivePartInfos.Count; ii++)
                        {
                            List<object> fooList = cro.ListActivePartInfos[ii];

                            for (int jj = 0; jj < fooList.Count; jj++)
                            {
                                PartInfo partInfo = fooList[jj] as PartInfo;

                                if (partInfo != null)
                                {
                                    partInfo.DiffuseColor = new Vector4(1, 1, 1, 1);
                                }
                            }
                        }
                        */


                        Debug.Assert(cro != null);

                        IBounding ibox = cro as IBounding;
                        ITransform itrans = cro as ITransform;

                        Debug.Assert(ibox != null);
                        Debug.Assert(itrans != null);

                        float min = itrans.Local.Translation.Y + ibox.BoundingBox.Min.Y;
                        float max = itrans.Local.Translation.Y + ibox.BoundingBox.Max.Y;
                        if (max >= cameraMinY && min <= camerMaxY)
                        {
                            cro.Render(shared.camera);
                        }
                    }
                    InGame.inGame.Editor.RenderPieMenus = true;

                    RenderPageSelector();

                    // HACK Need to get the pencil to point closer to the plus signs on empy tile spaces.
                    // So, detect this condition and apply an offset to the camera.  Yes, this is seriously
                    // ugly but I have no diea how to figure out how to move the cursor...
                    bool overPlus = false;
                    if (InGame.inGame.Editor.ActivePanel != null)
                    {
                        int curTile = InGame.inGame.Editor.ActivePanel.ActiveCard;
                        object foo = InGame.inGame.Editor.ActivePanel.listControls[curTile];

                        ReflexCard card = foo as ReflexCard;

                        if (card != null)
                        {
                            if (card.Card is NullActuator || card.Card is NullSensor || card.Card is NullFilter || card.Card is NullSelector)
                            {
                                overPlus = true;
                            }
                        }
                    }

                    Vector3 target = overPlus ? new Vector3(0.2f, 0.2f, 0.0f) : Vector3.Zero;
                    if (target != targetOffset)
                    {
                        targetOffset = target;
                        TwitchManager.Set<Vector3> offsetSet = delegate(Vector3 value, Object param) { cameraOffset = value; };
                        TwitchManager.CreateTwitch<Vector3>(cameraOffset, targetOffset, offsetSet, 0.1, TwitchCurve.Shape.EaseIn);
                    }

                    shared.camera.At += cameraOffset;

                    // HACK -- Since we now want the page icon to be on top of the programming but also
                    // have any active pie menu on top of the page icon we render all the things in the 
                    // renderObj list first (except the pie menu which we turn off via a hack to Editor).  
                    // But we still need the cursor to be on top of the page icon.  So, render it now.
                    if (!debugRender)
                    {
                        UiCursor.ActiveCursor.Render(shared.camera);
                    }

                    shared.camera.At -= cameraOffset;

                    // Now we can render the pie menus.  During the initial rendering pass
                    // we should have updated the references.
                    if (InGame.inGame.Editor.PieMenuList.Count > 0)
                    {
                        for (int i = 0; i < InGame.inGame.Editor.PieMenuList.Count; i++)
                        {
                            InGame.inGame.Editor.PieMenuList[i].renderObj.Render(shared.camera);
                        }
                    }
                    if (InGame.inGame.Editor.NotPieMenuList.Count > 0)
                    {
                        for (int i = 0; i < InGame.inGame.Editor.NotPieMenuList.Count; i++)
                        {
                            InGame.inGame.Editor.NotPieMenuList[i].RenderObject.Render(shared.camera);
                        }
                    }

                    // The ToolTips need to be rendered after all the editor bits otherwise
                    // they end up conflicting.  In particular, the pieselector rendering is
                    // interleaved with the reflex rendering so if we have the pieselector
                    // render the ToolTips then we sometimes get reflexes being rendered on
                    // top of them.
                    if (!debugRender)
                    {
                        ToolTipManager.Render(shared.camera);
                    }

                    InGame.inGame.Editor.RightClickMenu.Render();

                    //Text entery dialog
                    if (InGame.inGame.shared.textLineDialog.Active)
                    {
                        InGame.inGame.shared.textLineDialog.Render();
                    }
#if UGLY_DEBUG_HACK
                    // Debug hack to show bounds for tiles.
                    {
                        ReflexPanel panel = InGame.inGame.Editor.ActivePanel;
                        if (panel != null)
                        {
                            for (int j = 0; j < panel.listControls.Count; j++)
                            {
                                ITransform itrans2 = panel.listControls[j] as ITransform;

                                IBounding ibound2 = null;
                                if (panel.listControls[j] is ReflexHandle)
                                {
                                    ReflexHandle rh = panel.listControls[j] as ReflexHandle;
                                    ControlRenderObj cro2 = rh.renderObj as ControlRenderObj;

                                    ibound2 = cro2 as IBounding;
                                }
                                else if (panel.listControls[j] is ReflexCard)
                                {
                                    ReflexCard rc = panel.listControls[j] as ReflexCard;
                                    ControlRenderObj cro2 = rc.renderObj as ControlRenderObj;

                                    ibound2 = cro2 as IBounding;
                                }
                                else
                                {
                                    // Huh?
                                    continue;
                                }

                                BoundingBox box2 = ibound2.BoundingBox;

                                Matrix mat2 = Matrix.Identity;
                                Vector2 hitUV2 = LowLevelMouseInput.GetHitOrtho(shared.camera, ref mat2, false);

                                Vector3 position = new Vector3(box2.Min.X, box2.Min.Y, 0.0f);
                                Vector3 boxSize = new Vector3(box2.Max.X - box2.Min.X, box2.Max.Y - box2.Min.Y, 0.0f);

                                position = Vector3.Transform(position, itrans2.World);

                                Point pnt = shared.camera.WorldToScreenCoords(position);
                                pos = new Vector2(pnt.X, pnt.Y);

                                pnt = shared.camera.WorldToScreenCoords(position + boxSize);
                                size = pos - new Vector2(pnt.X, pnt.Y);
                                size.X = -size.X;

                                // Adjust for bizarre yuck foo.
                                if (j == 0)
                                {
                                    // Reflex handle
                                    pos.Y -= 95.0f * shared.camera.TutorialScale;
                                    size.X += 10 * shared.camera.TutorialScale;
                                    size.Y -= 35 * shared.camera.TutorialScale;
                                }
                                else
                                {
                                    if (size.X > 40)
                                    {
                                        // Tile
                                        pos.X += 22.0f * shared.camera.TutorialScale;
                                        pos.Y -= 45.0f * shared.camera.TutorialScale;
                                        size.X -= 5.0f * shared.camera.TutorialScale;
                                        size.Y += 55.0f * shared.camera.TutorialScale;
                                    }
                                    else
                                    {
                                        // Plus sign
                                        size.X += 10 * shared.camera.TutorialScale;
                                        size.Y += 80 * shared.camera.TutorialScale;
                                        pos.Y -= 72.0f * shared.camera.TutorialScale;
                                    }
                                }

                                quad.Render(new Vector4(1, 0, 0, 0.5f), pos, size);
                            }
                        }
                    }
#endif  // UGLY_DEBUG_HACK

                }

                // Restore viewport.
                InGame.RestoreViewportToFull();

            }   // end of Render()

            public void RenderPageSelector()
            {
                if (KoiLibrary.LastTouchedDeviceIsKeyboardMouse)
                {
                    RenderGamepadPageSelector();
                }
                else if (KoiLibrary.LastTouchedDeviceIsGamepad)
                {
                    RenderGamepadPageSelector();
                }
                else if (KoiLibrary.LastTouchedDeviceIsTouch)
                {
                    RenderTouchPageSelector();
                }
                else
                {
                    Debug.Assert(true, "Unknown active input mode!");
                }
            }

            public void RenderTouchPageSelector()
            {
                float scale = Math.Min((float)KoiLibrary.GraphicsDevice.Viewport.Height / 1024.0f, 1.0f);
                int center = KoiLibrary.GraphicsDevice.Viewport.Width / 2;
                scale *= shared.camera.TutorialScale;
                ScreenSpaceQuad quad = ScreenSpaceQuad.GetInstance();

                // First render the plate underneath the task icons.
                Vector2 plateSize = 1.2f * scale * new Vector2(1066, 110);
                Vector2 platePos = new Vector2(center - plateSize.X / 2.0f, 0);
                quad.Render(pageBackdropTexture, platePos, plateSize, "TexturedRegularAlpha");
                shared.touchPageSelectorBackBox.Set(platePos, platePos + plateSize);

                // Render each of the page icons. The current page is made larger.
                Vector2 iconSize = scale * new Vector2(96, 96);
                Vector2 iconPos = new Vector2(platePos.X + 23, platePos.Y + 5);
                for (int i = 0; i < Brain.kCountDefaultTasks; i++)
                {
                    Vector2 boxSize = Vector2.Zero;
                    if( shared.activeTaskId == i )
                    {
                        iconSize = scale * selectedPageSize;
                        boxSize = iconSize;
                    }
                    else
                    {
                        iconSize = scale * unselectedPageSize;
                        boxSize = new Vector2(iconSize.X, (selectedPageSize * scale).Y);
                    }

                    quad.Render(touchPageSelectorTextures[i], iconPos, iconSize, @"TexturedRegularAlpha");
                    shared.touchPageSelectorPageBoxes[i].Set(
                        iconPos, iconPos + boxSize /** 40.0f / 64.0f*/);
                    iconPos.X += iconSize.X;
                }
            }

            public void RenderGamepadPageSelector()
            {
                ScreenSpaceQuad quad = ScreenSpaceQuad.GetInstance();
                float scale = Math.Min(BokuGame.ScreenSize.Y / 1024.0f, 1.0f);
                int center = (int)(BokuGame.ScreenSize.X / 2);

                // First render the plate underneath the task icon and the shoulder buttons.
                scale *= shared.camera.TutorialScale;
                Vector2 size = 1.2f * scale * new Vector2(256, 128);
                Vector2 pos = new Vector2(center - size.X / 2.0f, 0);
                quad.Render(pageBackdropTexture, pos, size, "TexturedRegularAlpha");
                shared.taskHandleBox.Set(pos, pos + size);

                SpriteBatch batch = KoiLibrary.SpriteBatch;

                GetFont Font = SharedX.GetGameFont18Bold;

                Vector2 labelSize = Font().MeasureString(labelTask);

                // Render the matching shoulder button glyphs
                size = scale * new Vector2(100, 100);
                pos.X = center - size.X - scale * 20.0f;
                pos.Y += 30;
                quad.Render(ButtonTextures.LeftShoulderArrow, pos, size, @"TexturedRegularAlpha");
                shared.leftShoulderBox.Set(pos, pos + size * 40.0f / 64.0f);

                // Reflect pos across centerline for right arrow.  The 40/60 factor is because the
                // button textures only use the top 40x40 region of their 64x64 textures.
                pos.X = 2.0f * center - pos.X - size.X * 40.0f / 64.0f;
                quad.Render(ButtonTextures.RightShoulderArrow, pos, size, @"TexturedRegularAlpha");
                shared.rightShoulderBox.Set(pos, pos + size * 40.0f / 64.0f);

                // Task icon.
                pos.X = center - scale * 64;
                pos.Y += -32.0f * scale;
                size = scale * new Vector2(128, 128);
                quad.Render(taskTexture, pos, size, @"TexturedRegularAlpha");
            }               

            public override void Activate()
            {
//                rowBackdrop.Activate();
            }
            public override void Deactivate()
            {

            }

            public void LoadContent(bool immediate)
            {
            }

            public void InitDeviceResources(GraphicsDevice device)
            {
            }

            public void UnloadContent()
            {
                DeviceResetX.Release(ref taskTexture);
            }

            public void DeviceReset(GraphicsDevice device)
            {
            }

        }   

        protected static Effect effect = null;
        protected ControlCollection controls = null;
        protected GameActor gameActor;
        protected static SpriteFont fontLineNumbers = null;
        protected static Texture2D pageBackdropTexture = null;

        private static List<ReflexData> cutPasteBuffer = null;

        public ControlCollection Controls
        {
            get { return controls; }
        }

        /// <summary>
        /// Some place to hang the UI cursor when we want it to be on the page handle.
        /// </summary>
        public Fugly fugly = new Fugly();

        // Children.
        protected List<ReflexPanel> childPanels = new List<ReflexPanel>(); // all, including transitional
        protected List<ReflexPanel> activePanels = new List<ReflexPanel>(); // active only
        protected List<ReflexPanel> lastPanels = new List<ReflexPanel>();

        // List to store away pie menus so they can be rendered in the correct order.
        protected List<PieSelector> pieMenuList = new List<PieSelector>();
        protected List<NotPieSelector> notPieMenuList = new List<NotPieSelector>();

        // List objects.
        public Shared shared;
        public RenderObj renderObj;
        protected UpdateObj updateObj;

        private enum States
        {
            Inactive,
            Active,
            UiRebuild,
            PrevTask,
            NextTask,
            ChangeTask,
            WaitingForTaskChange,
            FinishTaskChange,
        }
        private States state = States.Inactive;
        private States pendingState = States.Inactive;

        private float heightPanel = 0.0f;
        private int indexActivePanel = 0;

        private Transform localTransform = new Transform();

        protected const float zOffsetEtherPanel = 2.0f;

        protected bool backWasPressed = false;      // Did we exit while pressing <back> instead of <b>?

        private bool renderPieMenus = true;         // Used to get the rendering to layer correctly.

        #region Accessors

        public bool BackWasPressed
        {
            get { return backWasPressed; }
            set { backWasPressed = value; }
        }

        public bool Active
        {
            get { return (state != States.Inactive); }
        }
        
        public GameActor GameActor
        {
            set
            {
                if (this.state != States.Inactive)
                {
                    throw new System.Exception("GameActor being changed while editor is active, this is not allowed");
                }
                this.gameActor = value;
            }
            get
            {
                return this.gameActor;
            }
        }
        
        public static Effect Effect
        {
            get { return effect; }
            set { effect = value; }
        }

        /// <summary>
        /// Returns the currently active ReflexPanel.
        /// </summary>
        public ReflexPanel ActivePanel
        {
            get 
            {
                if (IndexActivePanel > -1 && IndexActivePanel < activePanels.Count)
                {
                    return activePanels[IndexActivePanel];
                }
                else
                {
                    return null;
                }
            }
        }

        /// <summary>
        /// Returns the list of active reflex panels.
        /// </summary>
        public List<ReflexPanel> ActivePanels
        {
            get { return activePanels; }
        }

        /// <summary>
        /// Returns the index of the currently active reflex panel.
        /// </summary>
        public int IndexActivePanel
        {
            get { return indexActivePanel; }
            set { indexActivePanel = value; }
        }

        /// <summary>
        /// The CutPaste buffer used to cut and paste full pages of programming.
        /// </summary>
        public static List<ReflexData> CutPasteBuffer
        {
            get { return Editor.cutPasteBuffer; }
            set { Editor.cutPasteBuffer = value; }
        }

        public UiCamera Camera
        {
            get { return shared.camera; }
        }

        /// <summary>
        /// Internal flag used to get around rendering order issues.
        /// </summary>
        public bool RenderPieMenus
        {
            get { return renderPieMenus; }
            set { renderPieMenus = value; }
        }

        /// <summary>
        /// Temp place to store ref so we don't have to hunt it down twice.
        /// </summary>
        public List<PieSelector> PieMenuList
        {
            get { return pieMenuList; }
        }

        /// <summary>
        /// Temp place to store ref so we don't have to hunt it down twice.
        /// </summary>
        public List<NotPieSelector> NotPieMenuList
        {
            get { return notPieMenuList; }
        }

        /// <summary>
        /// Menu activated on right click.
        /// </summary>
        public MouseMenu RightClickMenu
        {
            get { return shared.rightClickMenu; }
        }

        #endregion
        
        public Editor()
        {
            shared = new Shared();

            // Create the RenderObject and UpdateObject parts of this mode.
            updateObj = new UpdateObj(this, shared);
            renderObj = new RenderObj(this, shared);

            // Instantiate the AABB2Ds for the touch page selector array
            for( int i = 0; i < Brain.kCountDefaultTasks; i++)
            {
                shared.touchPageSelectorPageBoxes[i] = new AABB2D();
            }

            BokuGame.Load(this);
        }

        public override bool Refresh(List<UpdateObject> updateList, List<RenderObject> renderList)
        {
            bool result = false;

            if (state != pendingState)
            {
                if (pendingState == States.Active)
                {
                    gameActor.Brain.Validate();

                    CreateUI();

                    this.shared.RecenterCamera();
                    
                    CreateUiForTask(gameActor.Brain.ActiveTask, gameActor.Brain.ActiveTaskId);

                    updateList.Add(updateObj);
                    updateObj.Activate();
                    renderList.Add(renderObj);
                    renderObj.Activate();

                    firstTime = true;
                }
                else if (pendingState == States.Inactive)
                {
                    renderObj.Deactivate();
                    renderList.Remove(renderObj);
                    updateObj.Deactivate();
                    updateList.Remove(updateObj);
                    CleanupTasks();
                    
                    ReleaseUiPanels( this.activePanels);
                    ReleaseUi();
                }
                else if (pendingState == States.UiRebuild)
                {
                    // simple rebuild
                    CleanupTask(gameActor.Brain.ActiveTask, false);
                    ReleaseUiPanels( this.activePanels);
                    RefreshChildren();
                    this.shared.RecenterCamera();
                    CreateUiForTask(gameActor.Brain.ActiveTask, gameActor.Brain.ActiveTaskId); // always the row handle
                    pendingState = States.Active;
                }
                else if (pendingState == States.PrevTask)
                {
                    StartTaskChange( -1 );
                    this.pendingState = States.WaitingForTaskChange;
                }
                else if (pendingState == States.NextTask)
                {
                    StartTaskChange( 1 );
                    this.pendingState = States.WaitingForTaskChange;
                }
                else if (pendingState == States.ChangeTask)
                {
                    StartTaskChange(shared.changeTaskId - gameActor.Brain.ActiveTaskId);
                    this.pendingState = States.WaitingForTaskChange;
                }
                else if (pendingState == States.FinishTaskChange)
                {
                    ReleaseUiPanels(this.lastPanels);
                    pendingState = States.Active;
                }

                state = pendingState;
            }

            RefreshChildren();

            
            return result;
        }   // end of Refresh() 

        private void StartTaskChange( int direction )
        {
            int oldIndexActivePanel = IndexActivePanel;

            int countTasks = gameActor.Brain.TaskCount;
            int indexTask = gameActor.Brain.ActiveTaskId;
                
            indexTask = (indexTask + direction + countTasks) % countTasks;

            SetupUiForTaskChange( -direction );

            // create the new task ui
            this.gameActor.Brain.ActiveTaskId = indexTask;
            CreateUiForTask(gameActor.Brain.ActiveTask, gameActor.Brain.ActiveTaskId);

            // Return to the same reflex.
            if (oldIndexActivePanel == -1)
            {
                // If we were on the page handle, return there.
                NavReflexPrev(null, null);
            }
            else
            {
                // Not on the page handle so cycle down to the right one.
                for (int i = 0; i < oldIndexActivePanel; i++)
                {
                    NavReflexNext(null, null);
                }
            }

        }

        private void RefreshChildren()
        {
            // all HUD/floating scenes/objects should refresh after this
            // addes/removes  update objects so command stack has correct order
            for (int indexReflex = 0; indexReflex < this.childPanels.Count; indexReflex++)
            {
                ReflexPanel reflexPanel = this.childPanels[indexReflex];
                if (reflexPanel != null && reflexPanel.Refresh(updateObj.updateList, renderObj.renderList))
                {
                    this.childPanels.RemoveAt(indexReflex);
                    indexReflex--;
                }
            }
        }
        
        private object timerInstrument = null;
        private Texture2D prevToolIcon = null;  // Keep track of the HelpOverlay's toolIcon so we can restore it when done.

        override public void Activate()
        {
            if (state != States.Active)
            {
                pendingState = States.Active;
                BokuGame.objectListDirty = true;

                // Remove what is probably "ObjectEditFocusProgrammable" help.  This will get replaced when we exit programming mode.
                HelpOverlay.Pop();
                // Mark the beginning of the programming mode on the help stack.  This acts like a sentinel.
                HelpOverlay.Push("Programming");
                // Push the initial "Tile" help.  As the focus is moved around this will alternate with the "RowHandle" help.
                // When the pie menu is active, that help will be pushed on top of these.
                HelpOverlay.Push("Tile");

                // Save away current tool icon.
                prevToolIcon = HelpOverlay.ToolIcon;
                // Get the icon that matches the current actor.
                HelpOverlay.ToolIcon = CardSpace.Cards.CardFaceTexture(gameActor.StaticActor.MenuTextureFile);

                InGame.inGame.RenderWorldAsThumbnail = true;
                timerInstrument = Instrumentation.StartTimer(Instrumentation.TimerId.ProgrammingTime);
            }
        }  

        override public void Deactivate()
        {
            if (state != States.Inactive)
            {
                // Make sure these have closed.  Shouldn't be a problem except 
                // when shutting down the editor to handle device reset.
                ProgrammingHelpCard.Instance.Deactivate();
                InGame.inGame.Editor.RightClickMenu.Deactivate();
                InGame.inGame.shared.textEditor.Deactivate();
                InGame.inGame.shared.microbitPatternEditor.Deactivate(saveChanges:false);
                InGame.inGame.shared.editObjectParameters.Deactivate();
                InGame.inGame.shared.editWorldParameters.Deactivate(false);

                pendingState = States.Inactive;
                BokuGame.objectListDirty = true;
                Boku.InGame.IsLevelDirty = true;

                fontLineNumbers = null;

                // In case of device reset we need to strip the command stack back down to InGameEdtBase.
                while (CommandStack.Depth() > 1)
                {
                    CommandStack.Pop(CommandStack.Peek());
                }

                HelpOverlay.Pop();
                GamePadInput.ClearAllWasPressedState(3);

                // Restore tool icon.
                HelpOverlay.ToolIcon = prevToolIcon;

                InGame.inGame.RenderWorldAsThumbnail = false;

                // If the actor is a creatable, prop brain changes to clones.
                if (GameActor.Creatable)
                {
                    GameActor.CopyBrainToClones();
                }

                //stop the timer for programming time
                Instrumentation.StopTimer(timerInstrument);
                StringWriter tw = new StringWriter();
                Print.SerializeActorProgramming(tw, gameActor);
                tw.Close();
                
            }
        }

        public void Hide(bool backWasPressed)
        {
            BackWasPressed = backWasPressed;
            Deactivate();
            if (IndexActivePanel >= 0)
            {
                // update panel states
                IControl panelControl = this.activePanels[IndexActivePanel] as IControl;
                panelControl.Hot = false;
            }
        }
        
        Transform ITransform.Local
        {
            get
            {
                return this.localTransform;
            }
            set
            {
                Debug.Assert(false, "no local transform settable");
            }
        }
        Matrix ITransform.World
        {
            get
            {
                return Matrix.Identity;
            }
        }
        bool ITransform.Compose()
        {
            bool changed = this.localTransform.Compose();
            if (changed)
            {
                RecalcMatrix();
            }
            return changed;
        }
        void ITransform.Recalc(ref Matrix parentMatrix)
        {
            // we have no world transform, nothing to change

            foreach (ITransform transformReflex in this.childPanels)
            {
                transformReflex.Recalc(ref parentMatrix);
            }
            
        }
        ITransform ITransform.Parent
        {
            // we have no parent
            get
            {
                return null; 
            }
            set
            {

            }
        }
        protected void RecalcMatrix()
        {
            // we have no parent
            Matrix parentMatrix = Matrix.Identity;
            ITransform transformThis = this as ITransform;
            transformThis.Recalc(ref parentMatrix);
        }

        
        private const int MaxFilters = 3;
        private const int MaxModifiers = 1;

        private void CreateUI()
        {
            UiCursor.ActiveCursor = new UiCursor(controls.InstanceControlRenderObj(this, "cone"));
            UiCursor.ActiveCursor.Activate();

            // Assign the textures for the touch page selector
            for (int i = 0; i < Brain.kCountDefaultTasks; i++)
            {
                this.renderObj.touchPageSelectorTextures[i] = CardSpace.Cards.CardFaceTexture(
                    string.Format(upidTaskFormat, mapIdTaskToString[i].ToLower()));
            }
        }
        private void ReleaseUi()
        {
            UiCursor.ActiveCursor.Deactivate();
            UiCursor.ActiveCursor = null;
        }

        private void SetupUiForTaskChange(int direction)
        {
            // incase we still have some pending from a previous animation
            ReleaseUiPanels(this.lastPanels);

            float xShift = this.shared.camera.Width * 1.1f * direction;
            Vector3 shift = new Vector3(xShift, 0.0f, 0.0f);
            for (int indexPanel = 0; indexPanel < this.activePanels.Count; indexPanel++)
            {
                IControl panelControl = this.activePanels[indexPanel] as IControl;

                // move panels to the side
                ITransform transformPanel = panelControl as ITransform;
                transformPanel.Local.Translation += shift;
                transformPanel.Compose();
            }

            // move active panels to last panels
            this.lastPanels.AddRange( this.activePanels );
            this.activePanels.Clear();

            this.shared.RecenterCamera();

            // Slide camera for task switch.
            // We've just recentered the camera so now move it off
            // to the side and call SlideCamera to shift it into place.
            shared.camera.At += shift;
            shared.camera.From += shift;
            SlideCamera(-xShift, CameraSlideComplete);
            
        }

        protected void CameraSlideComplete(Object param)
        {
            if (this.pendingState == States.WaitingForTaskChange)
            {
                this.pendingState = States.FinishTaskChange;
                BokuGame.objectListDirty = true;
            }
        }

        protected string[] mapIdTaskToString = { "A", "B", "C", "D", "E", "F", "G", "H", "I", "J", "K", "L" };
        protected string[] mapIdTaskToStringNumbers = { "1", "2", "3", "4", "5", "6", "7", "8", "9", "10", "11", "12" };

        private void CreateUiForTask( Task task, int idTask )
        {
            this.renderObj.labelTask = pageLabel + " " + ((int)(idTask + 1)).ToString();
            this.renderObj.taskTexture = CardSpace.Cards.CardFaceTexture( string.Format( upidTaskFormat, mapIdTaskToString[idTask].ToLower() ) );
            this.shared.activeTaskId = gameActor.Brain.ActiveTaskId;

            Debug.Assert(this.activePanels.Count == 0);

            // all tasks should have one reflex even if empty
            if (task.reflexes.Count == 0)
            {
                Reflex emptyReflex = new Reflex(task);
                task.AddReflex(emptyReflex);
            }

            // for each reflex, instance a row
            ReflexPanel panel;
            for (int indexReflex = 0; indexReflex < task.reflexes.Count; indexReflex++)
            {
                Reflex reflex = task.reflexes[indexReflex] as Reflex;
                reflex.Fill(MaxFilters, MaxModifiers);
                panel = new ReflexPanel(this, reflex, controls);
                panel.LineNumber = indexReflex + 1; // one based
                heightPanel = panel.BoundingBox.Max.Y - panel.BoundingBox.Min.Y;
                ITransform transformPanel = panel as ITransform;
                transformPanel.Local.Translation = new Vector3(0.0f, -heightPanel * indexReflex, 0.0f);
                transformPanel.Compose();
                panel.Activate();
                this.activePanels.Add(panel);
                // add as a child to track for lifetime beyound being in the list
                this.childPanels.Add(panel); 
            }
            // have our update object add its commands to the child controls.
            IControl panelControl;
            foreach (ReflexPanel reflex in this.activePanels)
            {
                panelControl = reflex as IControl;
            }

            this.IndexActivePanel = 0;
            // set the active one hot
            ReflexPanel reflexPanel = this.activePanels[IndexActivePanel];
            reflexPanel.ActiveCard = 1; // skip the row handle as the default card

            panelControl = this.activePanels[IndexActivePanel] as IControl;
            panelControl.Hot = true;
        }

        /// <summary>
        /// WARNING This comment is a total guess but it's better than what the author provided.
        /// For each reflex in the task, Chill() is called.  If the reflex is empty and removeEmpty
        /// is true, then the reflex is removed.
        /// </summary>
        /// <param name="task"></param>
        /// <param name="removeEmpty"></param>
        private void CleanupTask( Task task, bool removeEmpty)
        {       
            for (int iReflex = 0; iReflex < task.reflexes.Count; iReflex++)
            {
                Reflex gameReflex = task.reflexes[iReflex] as Reflex;
                // if an empty reflex just remove it
                if (gameReflex.Chill() && removeEmpty)
                {
                    task.reflexes.RemoveAt(iReflex);
                    iReflex--;
                }
            }
        }

        /// <summary>
        /// WARNING This comment is a total guess but it's better than what the author provided.
        /// Calls CleanupTask() on each task in the brain.  This is only ever called at Deactivate
        /// time so it appears to be a final scrub before saving the tasks.
        /// </summary>
        private void CleanupTasks()
        {
            int countTasks = this.gameActor.Brain.TaskCount;
            for (int indexTask = 0; indexTask < countTasks; indexTask++)
            {
                Task task = this.gameActor.Brain.GetTask(indexTask);
                CleanupTask(task, true);
            }
        }

        private void ReleaseUiPanels(List<ReflexPanel> panels)
        {
            for (int indexReflex = 0; indexReflex < panels.Count; indexReflex++)
            {
                ReflexPanel reflexPanel = panels[indexReflex];
                reflexPanel.Deactivate();
            }
            panels.Clear();
        }
        protected void NavPanel( int indexPanel )
        {
            //Debug.Assert( indexActivePanel >= 0 && indexActivePanel < this.activePanels.Count );
            // Update panel states.
            int indexCard = -1;

            // If we're coming from the Page Handle then we don't have any panel to make non-hot.
            if (IndexActivePanel != -1)
            {
                ReflexPanel panel = this.activePanels[IndexActivePanel];
                indexCard = panel.ActiveCard;

                IControl panelControl = this.activePanels[IndexActivePanel] as IControl;
                panelControl.Hot = false;
            }

            IndexActivePanel = indexPanel;
            {
                IControl panelControl = this.activePanels[IndexActivePanel] as IControl;
                panelControl.Hot = true;

                ReflexPanel panel = this.activePanels[IndexActivePanel];
                if (indexCard != -1)
                {
                    panel.ActiveCard = indexCard;
                }
            }
        }

        public void NavReflexPrev(Object sender, EventArgs args)
        {
            if (IndexActivePanel > 0)
            {
                Foley.PlayClickUp();

                // Move the camera up.
                MoveCamera(heightPanel);

                // Move the backdrop to match the camera.
                //                renderObj.rowBackdrop.Position = shared.camera.At;

                NavPanel(IndexActivePanel - 1);
            }
            else
            {
                Foley.PlayClickUp();

                IControl panelControl = activePanels[IndexActivePanel] as IControl;
                panelControl.Hot = false;

                // Go to Page Handle...
                IndexActivePanel = -1;

                HelpOverlay.Pop();
                if (CutPasteBuffer == null)
                {
                    HelpOverlay.Push("PageHandleEmptyPasteBuffer");
                }
                else
                {
                    HelpOverlay.Push("PageHandleFullPasteBuffer");
                }
                CommandStack.Push(updateObj.commandMapPageHandle);

                // Get cursor to top of page.
                UiCursor.ActiveCursor.Parent = fugly;
            }
        }
        public void NavReflexNext(Object sender, EventArgs args)
        {
            if (IndexActivePanel < this.activePanels.Count - 1)
            {
                Foley.PlayClickDown();

                // Move the camera down but not if we're coming from the PageHandle.
                if (IndexActivePanel != -1)
                {
                    MoveCamera(-heightPanel);
                }
                else
                {
                    // Coming from Page Handle, restore tile help.
                    HelpOverlay.Pop();
                    if (ActivePanels[0].ActiveCard == 0)
                    {
                        if (ReflexPanel.CutPasteBuffer == null)
                        {
                            HelpOverlay.Push("RowHandleEmptyPasteBuffer");
                        }
                        else
                        {
                            HelpOverlay.Push("RowHandleFullPasteBuffer");
                        }
                    }
                    else
                    {
                        HelpOverlay.Push("Tile");
                    }
                    CommandStack.Pop(updateObj.commandMapPageHandle);
                }

                // Move the backdrop to match the camera.
//                renderObj.rowBackdrop.Position = shared.camera.At;

                NavPanel(IndexActivePanel + 1);
            }
  
        }

        public void NavTaskPrev(Object sender, EventArgs args)
        {
            int countTasks = gameActor.Brain.TaskCount;
            if (countTasks > 1)
            {
                Foley.PlayShuffle();

                // set active panel no longer hot
                if (IndexActivePanel != -1)
                {
                    IControl panelControl = this.activePanels[IndexActivePanel] as IControl;
                    panelControl.Hot = false;
                }

                this.pendingState = States.PrevTask;
                BokuGame.objectListDirty = true;
            }            
        }

        public void NavTaskNext(Object sender, EventArgs args)
        {
            int countTasks = gameActor.Brain.TaskCount;
            if (countTasks > 1)
            {
                Foley.PlayShuffle();

                // set active panel no longer hot
                if (IndexActivePanel != -1)
                {
                    IControl panelControl = this.activePanels[IndexActivePanel] as IControl;
                    panelControl.Hot = false;
                }

                this.pendingState = States.NextTask;
                BokuGame.objectListDirty = true;
            }
        }

        public void NavTaskChange(Object sender, EventArgs args)
        {
            int countTasks = gameActor.Brain.TaskCount;
            if (countTasks > 1)
            {
                Foley.PlayShuffle();

                // set active panel no longer hot
                if (IndexActivePanel != -1)
                {
                    IControl panelControl = this.activePanels[IndexActivePanel] as IControl;
                    panelControl.Hot = false;
                }

                this.pendingState = States.ChangeTask;
                BokuGame.objectListDirty = true;
            }
        }

        public void NavCardPrev(Object sender, EventArgs args)
        {
            if (IndexActivePanel != -1)
            {
                ReflexPanel panel = this.activePanels[IndexActivePanel];
                panel.NavCardPrev();
            }
        }
        public void NavCardNext(Object sender, EventArgs args)
        {
            if (IndexActivePanel != -1)
            {
                ReflexPanel panel = this.activePanels[IndexActivePanel];
                panel.NavCardNext();
            }
        }
        public void RemoveReflex(ReflexPanel reflexPanel)
        {
            Foley.PlayCut();

            if (this.activePanels.Count == 1)
            {
                // we are about to remove the last one,
                // so insert a blank reflex if all reflexes are gone
                Reflex reflexNew = new Reflex(gameActor.Brain.ActiveTask);
                gameActor.Brain.ActiveTask.InsertReflexBefore(reflexPanel.Reflex, reflexNew);
                this.pendingState = States.UiRebuild;
                // Removing/replacing the last reflex panel causes an extra copy
                // of ReflexHandle command map to get stuck on the command stack.
                // So, until this is all refactored just pop off the top of the
                // stack here and all will be fine.
                CommandStack.Pop(CommandStack.Peek());
            }
            else
            {
                int indexRemove = this.activePanels.IndexOf(reflexPanel);
                int indexNextActive = indexRemove;
                if (indexRemove == this.activePanels.Count - 1)
                {
                    // move the camera up
                    MoveCamera(heightPanel);
                    indexNextActive--;
                    this.IndexActivePanel--;
                }
                else
                {
                    indexNextActive++;
                }

                // set old panel not hot
                IControl panelControl;
                panelControl = reflexPanel as IControl;
                panelControl.Hot = false;
                reflexPanel.Removed = true;

                // set the new active one hot
                ReflexPanel reflexPanelNextActive = this.activePanels[indexNextActive];
                reflexPanelNextActive.ActiveCard = 0;
                panelControl = reflexPanelNextActive as IControl;
                panelControl.Hot = true;

                // animate the removed one back out
                reflexPanel.AnimatePanelShift(-zOffsetEtherPanel, OnRemoveAnimationComplete);

                // animate all below the removed upward
                for (int indexPanel = indexRemove + 1; indexPanel < this.activePanels.Count; indexPanel++)
                {
                    ReflexPanel panel = this.activePanels[indexPanel];
                    panel.LineNumber = indexPanel; // one based but we are just removing one
                    panel.AnimatePanelMove(this.heightPanel);
                }

                // now remove it from the active list
                this.activePanels.RemoveAt(indexRemove);
            }

            // remove the one requested
            gameActor.Brain.ActiveTask.RemoveReflex(reflexPanel.Reflex);
            
            BokuGame.objectListDirty = true;
            
        }
        protected void OnRemoveAnimationComplete(Object param)
        {
            ReflexPanel reflexPanel = param as ReflexPanel;
            reflexPanel.Deactivate(); // this will cause it to be removed from childPanels
        }

        /// <summary>
        /// Inserts a new, blank reflex.
        /// </summary>
        /// <param name="reflexPanel">Reflex after which the new reflex is inserted.  If this is null then the new reflex is added to the end of the list of existing reflexes.</param>
        /// <returns>Newly created panel.</returns>
        public ReflexPanel InsertReflex(ReflexPanel reflexPanel)
        {
            Foley.PlayPaste();

            // If no panel specified, addToEnd is true and we're just 
            // adding the new panel without giving it focus.
            bool addToEnd = reflexPanel == null;
            if (addToEnd)
            {
                reflexPanel = ActivePanels[ActivePanels.Count - 1];
            }

            // Create the new reflex and panel.
            Reflex reflexNew = new Reflex(gameActor.Brain.ActiveTask);
            reflexNew.Fill(MaxFilters, MaxModifiers);
            ReflexPanel panelNew = new ReflexPanel(this, reflexNew, controls);

            // Insert into the brain and UI.
            int indexOldPanel = activePanels.IndexOf(reflexPanel);
            int indexNewPanel = addToEnd ? indexOldPanel + 1 : indexOldPanel;
            gameActor.Brain.ActiveTask.reflexes.Insert(indexNewPanel, reflexNew);
            panelNew.Activate();
            activePanels.Insert(indexNewPanel, panelNew);

            // Ensure we're got the right active panel.
            if (!addToEnd)
            {
                IndexActivePanel = indexNewPanel;
            }

            // Append new panel to the tracking child list.
            childPanels.Add(panelNew);

            IControl panelControl;

            // If addToEnd is true, don't change focus or steal the cursor.
            if (!addToEnd)
            {
                // Get card position.
                int indexPanelActiveCard = reflexPanel.ActiveCard;
                // Set old panel not hot.
                panelControl = reflexPanel as IControl;
                panelControl.Hot = false;
                // Set new one's active card
                panelNew.ActiveCard = indexPanelActiveCard;
            }

            // add our update controls to the new one
            panelControl = panelNew as IControl;

            // place into correct spot and animate
            //
            ITransform transformNewPanel = panelNew as ITransform;
            ITransform transformRefPanel = reflexPanel as ITransform;

            // animate new item from move height down into place at location of reflexPanel
            Vector3 translation = reflexPanel.position;

            // Normally the new panel is inserted at the cursor position eveything below
            // it is moved down.  If addToEnd is true then we want to add the new reflex
            // below the last one and not do any moving.
            if (addToEnd)
            {
                translation.Y -= heightPanel;

                // If the panel we're setting our position relative to happens
                // to be currently picked up we need to take that into account.  
                // Oh how I love having everything be positioned relative to 
                // everything else.
                if (reflexPanel.Moving)
                {
                    translation.Z -= ReflexPanel.zOffsetMovingPanel;
                }
            }
            translation.Z += zOffsetEtherPanel;
            transformNewPanel.Local.Translation = translation;
            transformNewPanel.Compose();
            panelNew.position = translation;
            panelNew.AnimatePanelShift(-zOffsetEtherPanel, null);

            if (!addToEnd)
            {
                // Animate all the remaining reflex panels downward.
                for (int indexPanel = indexNewPanel + 1; indexPanel < activePanels.Count; indexPanel++)
                {
                    ReflexPanel panel = activePanels[indexPanel];
                    panel.AnimatePanelMove(-heightPanel);
                }
            }

            // Reset line numbers for all reflexes below and including the new one.
            for (int indexPanel = indexNewPanel; indexPanel < activePanels.Count; indexPanel++)
            {
                ReflexPanel panel = this.activePanels[indexPanel];
                panel.LineNumber = indexPanel + 1;  // One based line numbers
            }

            BokuGame.objectListDirty = true;

            return panelNew;
        }   // end of InsertReflex()

        private void SwapReflexPanels(ReflexPanel reflexPanelPrev, ReflexPanel reflexPanelNew)
        {
            int indexPrev = this.activePanels.IndexOf(reflexPanelPrev);
            int indexNew = this.activePanels.IndexOf(reflexPanelNew);

            this.activePanels.RemoveAt(indexPrev);
            this.activePanels.Insert(indexPrev, reflexPanelNew);
            this.activePanels.RemoveAt(indexNew);
            this.activePanels.Insert(indexNew, reflexPanelPrev);

            int lineNumber = reflexPanelNew.LineNumber;
            reflexPanelNew.LineNumber = reflexPanelPrev.LineNumber;
            reflexPanelPrev.LineNumber = lineNumber;

            gameActor.Brain.ActiveTask.SwapReflexes(reflexPanelPrev.Reflex, reflexPanelNew.Reflex);
        }

        public void MoveReflexUp(ReflexPanel reflexPanel)
        {
            if (this.IndexActivePanel > 0)
            {
                Foley.PlayClick();

                this.IndexActivePanel--;
                ReflexPanel reflexPanelPrev = this.activePanels[this.IndexActivePanel];

                // swap visual panel locations
                //
                float heightPanel = reflexPanelPrev.BoundingBox.Max.Y - reflexPanelPrev.BoundingBox.Min.Y;

                // move the replaced one down
                reflexPanelPrev.AnimatePanelMove(-heightPanel);

                // move the current one up
                reflexPanel.AnimatePanelMove(heightPanel);

                // move the camera up
                MoveCamera(heightPanel);
                
                // swap the the reflexes 
                SwapReflexPanels(reflexPanelPrev, reflexPanel);
            }
        }
        
        public void MoveReflexDown(ReflexPanel reflexPanel)
        {
            if (this.IndexActivePanel < this.activePanels.Count - 1)
            {
                Foley.PlayClick();

                this.IndexActivePanel++;
                ReflexPanel reflexPanelPrev = this.activePanels[this.IndexActivePanel];

                // swap visual panel locations
                //
                float heightPanel = reflexPanelPrev.BoundingBox.Max.Y - reflexPanelPrev.BoundingBox.Min.Y;

                // move the replaced one up
                reflexPanelPrev.AnimatePanelMove(heightPanel);

                // move the current one down
                reflexPanel.AnimatePanelMove(-heightPanel);

                // move the camera down
                MoveCamera(-heightPanel);

                // swap the the reflexes 
                SwapReflexPanels(reflexPanelPrev, reflexPanel);
            }
        }

        //
        // A colleciton of camera movement functions.
        //


        // The postition to where the camera is going although it may not be there yet.
        private float fromY = 0.0f;
        private float atY = 0.0f;
        public bool firstTime = true;  // Do we need to refresh the starting camera position?

        /// <summary>
        /// Move the camera in Y.
        /// </summary>
        /// <param name="deltaY"></param>
        public void MoveCamera(float deltaY)
        {
            float twitchTime = 0.15f;
            TwitchCurve.Shape curveShape = TwitchCurve.Shape.EaseOut;

            Camera camera = shared.camera;

            if (camera != null)
            {
                if (firstTime)
                {
                    fromY = camera.From.Y;
                    atY = camera.At.Y;
                    firstTime = false;
                }
                
                // "from" position
                {
                    TwitchManager.Set<float> set = delegate(float value, Object param) { Vector3 from = camera.From; from.Y = value; camera.From = from; };
                    TwitchManager.CreateTwitch<float>(camera.From.Y, fromY + deltaY, set, twitchTime, curveShape);
                }

                // "at" position
                {
                    TwitchManager.Set<float> set = delegate(float value, Object param) { Vector3 at = camera.At; at.Y = value; camera.At = at; };
                    TwitchManager.CreateTwitch<float>(camera.At.Y, atY + deltaY, set, twitchTime, curveShape);
                }
                
                fromY += deltaY;
                atY += deltaY;
            }
        }   // end of MoveCamera()

        /// <summary>
        /// Move the camera in X.  Used for task changes.
        /// </summary>
        /// <param name="deltaX"></param>
        /// <param name="callback"></param>
        protected void SlideCamera(float deltaX, TwitchCompleteEvent callback)
        {
            float twitchTime = 0.1f;
            TwitchCurve.Shape curveShape = TwitchCurve.Shape.EaseOut;

            Camera camera = shared.camera;

            firstTime = true;

            if (camera != null)
            {
                // "from" position
                {
                    TwitchManager.Set<float> set = delegate(float value, Object param) { Vector3 from = camera.From; from.X = value; camera.From = from; };
                    TwitchManager.CreateTwitch<float>(camera.At.X, camera.From.X + deltaX, set, 5.0f * twitchTime, curveShape, this, callback);
                }

                // "at" position
                {
                    TwitchManager.Set<float> set = delegate(float value, Object param) { Vector3 at = camera.At; at.X = value; camera.At = at; };
                    TwitchManager.CreateTwitch<float>(camera.At.X, camera.At.X + deltaX, set, 5.0f * twitchTime, curveShape);
                }
            }
        }   // end of SlideCamera()


        public void LoadContent(bool immediate)
        {
            // load the effect
            if (effect == null)
            {
                effect = KoiLibrary.LoadEffect(@"Shaders\UI");
                ShaderGlobals.RegisterEffect("UI", effect);
            }

            if (pageBackdropTexture == null)
            {
                pageBackdropTexture = KoiLibrary.LoadTexture2D(@"Textures\Programming\PageBackdrop");
            }

            controls = new ControlCollection(@"Models\boku_programming_ui-02");
            INeedsDeviceReset resetRender = this.renderObj as INeedsDeviceReset;
            resetRender.LoadContent(immediate);
        }

        public void InitDeviceResources(GraphicsDevice device)
        {
            INeedsDeviceReset resetRender = this.renderObj as INeedsDeviceReset;
            resetRender.InitDeviceResources(device);
        }

        public void UnloadContent()
        {
            INeedsDeviceReset resetRender = this.renderObj as INeedsDeviceReset;
            resetRender.UnloadContent();
            DeviceResetX.Release(ref effect);
            DeviceResetX.Release(ref pageBackdropTexture);
        }

        public void DeviceReset(GraphicsDevice device)
        {
            BokuGame.DeviceReset(renderObj, device);
        }

    }

}   


