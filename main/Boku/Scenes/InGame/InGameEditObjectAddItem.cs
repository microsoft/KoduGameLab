// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.


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
using Boku.SimWorld;
using Boku.SimWorld.Path;
using Boku.Common;
using Boku.Programming;
using Boku.UI;
using Boku.UI2D;
using Boku.Input;
using Boku.Audio;

namespace Boku
{
    /// <summary>
    /// UpdateObject for InGame -> EditObject
    /// 
    /// This just takes all the methods needed for the add item menu and
    /// puts them out into a seperate file for organizational purposes.
    /// 
    /// </summary>
    public partial class InGame : GameObject, INeedsDeviceReset
    {

        public partial class EditObjectUpdateObj : BaseEditUpdateObj
        {
            public UIShim newItemSelectorShim = null;
            public UiSelector newItemSelector = null;

            /// <summary>
            /// Take a menu item, translates it out from selector
            /// to not collide through the pie slices
            /// </summary>
            private void CenterMenuItem(Object obj, float offset)
            {
                ITransform trans = obj as ITransform;
                
                if (trans != null)
                {
                    trans.Local.Translation += new Vector3(0.0f, 0.0f, offset);
                    trans.Local.Compose();
                }
            }

            private const float sizeMenuItem = 0.5f;
            private const float radialOffset = 0.1f;    // Adds an offset to the radial distance from the center of the pie menu the
                                                        // the menu item appears.  Strictly aethestic.  Warning if this gets big then
                                                        // the pie menu gets bigger and doesn't fit on the screen.
            private const float groupRadialOffset = -0.15f;     // Radial offset for big groups (bots...).

            private const float zOffset = 0.5f;         // Used to keep the billboards off the pie slices.

            /// <summary>
            /// Flag for explicit setting of where any added object will go. Otherwise
            /// default is for object to be added at cursor.
            /// </summary>
            private bool addPositionIsExplicit = false;
            /// <summary>
            /// Position to add any added object. Undefined if !addPositionIsExplicit.
            /// </summary>
            private Vector2 addPosition = Vector2.Zero;

            public void AddUISelector(List<GameObject> childList, out UiSelector uiSelector, bool ignorePaths)
            {
                //
                // Set up the pie menu for choosing new objects to add to the scene.
                //
                uiSelector = new PieSelector(null, "Sim.EditObjects.PickThing");

                // Size used for all items.
                Vector2 size = new Vector2(1.0f, 1.0f);

                // Boku
                var boku = ActorManager.GetActor("BokuBot");
                uiSelector.AddItem(new ActorMenuItem(uiSelector, boku.LocalizedName, boku.MenuTextureFile, boku, size, radialOffset));

                // Rover
                var rover = ActorManager.GetActor("Rover");
                uiSelector.AddItem(new ActorMenuItem(uiSelector, rover.LocalizedName, rover.MenuTextureFile, rover, size, radialOffset));

                // Fruit
                var fruit = ActorManager.GetActor("Fruit");
                uiSelector.AddItem(new ActorMenuItem(uiSelector, fruit.LocalizedName, fruit.MenuTextureFile, fruit, size, radialOffset));

                #region BotGroup1
                {
                    RenderObject group = new ActorMenuItem(uiSelector, null, @"Textures\Tiles\group.botsI", null, size, radialOffset);

                    UiSelector selectorGroup = new PieSelector(group, "Sim.EditObjects.PickToken");

                    // move it out front
                    ITransform transformSelector = selectorGroup as ITransform;
                    transformSelector.Local.Translation = new Vector3(0.0f, 0.0f, sizeMenuItem);
                    transformSelector.Compose();

                    selectorGroup.ComposeDefault += NewItemSelectorComposeDefault;
                    selectorGroup.Select += SelectNewItemSelector;
                    selectorGroup.Cancel += CancelNewItemSelector;

                    uiSelector.AddGroup(group, selectorGroup);

                    // bot group items

                    var actors = ActorManager.GetActorsInGroup("BotGroup1");
                    for (int i = 0; i < actors.Count; i++)
                    {
                        selectorGroup.AddItem(new ActorMenuItem(selectorGroup, actors[i].LocalizedName, actors[i].MenuTextureFile, actors[i], size, groupRadialOffset));
                    }                    
                }
                #endregion

                #region BotGroup2
                // some might think a subroutine would be a better way to structure this...
                {
                    RenderObject group = new ActorMenuItem(uiSelector, null, @"Textures\Tiles\group.botsII", null, size, radialOffset);

                    UiSelector selectorGroup = new PieSelector(group, "Sim.EditObjects.PickToken");

                    // move it out front
                    ITransform transformSelector = selectorGroup as ITransform;
                    transformSelector.Local.Translation = new Vector3(0.0f, 0.0f, sizeMenuItem);
                    transformSelector.Compose();

                    selectorGroup.ComposeDefault += NewItemSelectorComposeDefault;
                    selectorGroup.Select += SelectNewItemSelector;
                    selectorGroup.Cancel += CancelNewItemSelector;

                    uiSelector.AddGroup(group, selectorGroup);

                    // bot group items                
                    var actors = ActorManager.GetActorsInGroup("BotGroup2");
                    for (int i = 0; i < actors.Count; i++)
                    {
                        selectorGroup.AddItem(new ActorMenuItem(selectorGroup, actors[i].LocalizedName, actors[i].MenuTextureFile, actors[i], size, groupRadialOffset));
                    }  
                }
                #endregion

                #region objects group (star, coin, heart)
                {
                    RenderObject group = new ActorMenuItem(uiSelector, null, @"Textures\Tiles\group.objects", null, size, radialOffset);

                    UiSelector selectorGroup = new PieSelector(group, "Sim.EditObjects.PickToken");

                    // move it out front
                    ITransform transformSelector = selectorGroup as ITransform;
                    transformSelector.Local.Translation = new Vector3(0.0f, 0.0f, sizeMenuItem);
                    transformSelector.Compose();

                    selectorGroup.ComposeDefault += NewItemSelectorComposeDefault;
                    selectorGroup.Select += SelectNewItemSelector;
                    selectorGroup.Cancel += CancelNewItemSelector;

                    uiSelector.AddGroup(group, selectorGroup);


                    var actors = ActorManager.GetActorsInGroup("ObjectGroup");
                    for (int i = 0; i < actors.Count; i++)
                    {
                        selectorGroup.AddItem(new ActorMenuItem(selectorGroup, actors[i].LocalizedName, actors[i].MenuTextureFile, actors[i], size, groupRadialOffset));
                    }  
                }

                #endregion

                #region trees group
                // trees group
                //
                {
                    RenderObject group = new ActorMenuItem(uiSelector, null, @"filter.tree", null, size, radialOffset);

                    UiSelector selectorGroup = new PieSelector(group, "Sim.EditObjects.PickTree");

                    // move it out front
                    ITransform transformSelector = selectorGroup as ITransform;
                    transformSelector.Local.Translation = new Vector3(0.0f, 0.0f, sizeMenuItem);
                    transformSelector.Compose();

                    selectorGroup.ComposeDefault += NewItemSelectorComposeDefault;
                    selectorGroup.Select += SelectNewItemSelector;
                    selectorGroup.Cancel += CancelNewItemSelector;

                    uiSelector.AddGroup(group, selectorGroup);

                    // Trees group items
                    var actors = ActorManager.GetActorsInGroup("TreeGroup");
                    for (int i = 0; i < actors.Count; i++)
                    {
                        selectorGroup.AddItem(new ActorMenuItem(selectorGroup, actors[i].LocalizedName, actors[i].MenuTextureFile, actors[i], size, groupRadialOffset));
                    }
                }
                #endregion

                #region pipe group
                {
                    RenderObject group = new ActorMenuItem(uiSelector, null, @"filter.pipe", null, size, radialOffset);

                    UiSelector selectorGroup = new PieSelector(group, "Sim.EditObjects.PickPipe");

                    // move it out front
                    ITransform transformSelector = selectorGroup as ITransform;
                    transformSelector.Local.Translation = new Vector3(0.0f, 0.0f, sizeMenuItem);
                    transformSelector.Compose();

                    selectorGroup.ComposeDefault += NewItemSelectorComposeDefault;
                    selectorGroup.Select += SelectNewItemSelector;
                    selectorGroup.Cancel += CancelNewItemSelector;

                    uiSelector.AddGroup(group, selectorGroup);

                    // Trees group items
                    var actors = ActorManager.GetActorsInGroup("PipeGroup");
                    for (int i = 0; i < actors.Count; i++)
                    {
                        selectorGroup.AddItem(new ActorMenuItem(selectorGroup, actors[i].LocalizedName, actors[i].MenuTextureFile, actors[i], size, groupRadialOffset));
                    }
                }
                #endregion

                #region underwater group
                // underwater group
                //
                {
                    RenderObject group = new ActorMenuItem(uiSelector, null, @"filter.underwater", null, size, radialOffset);

                    UiSelector selectorGroup = new PieSelector(group, "Sim.EditObjects.PickUnderwater");

                    // move it out front
                    ITransform transformSelector = selectorGroup as ITransform;
                    transformSelector.Local.Translation = new Vector3(0.0f, 0.0f, sizeMenuItem);
                    transformSelector.Compose();

                    selectorGroup.ComposeDefault += NewItemSelectorComposeDefault;
                    selectorGroup.Select += SelectNewItemSelector;
                    selectorGroup.Cancel += CancelNewItemSelector;

                    uiSelector.AddGroup(group, selectorGroup);

                    // Trees group items
                    var actors = ActorManager.GetActorsInGroup("UnderwaterGroup");
                    for (int i = 0; i < actors.Count; i++)
                    {
                        selectorGroup.AddItem(new ActorMenuItem(selectorGroup, actors[i].LocalizedName, actors[i].MenuTextureFile, actors[i], size, groupRadialOffset));
                    }
                }
                #endregion

                #region rock group
                // rock group
                //
                {
                    var rock = ActorManager.GetActor("Rock");
                    RenderObject group = new ActorMenuItem(uiSelector, rock.LocalizedName, @"filter.rock", null, size, radialOffset);

                    UiSelector selectorGroup = new PieSelector(group, "Sim.EditObjects.PickRock");

                    // move it out front
                    ITransform transformSelector = selectorGroup as ITransform;
                    transformSelector.Local.Translation = new Vector3(0.0f, 0.0f, sizeMenuItem);
                    transformSelector.Compose();

                    selectorGroup.ComposeDefault += NewItemSelectorComposeDefault;
                    selectorGroup.Select += SelectNewItemSelector;
                    selectorGroup.Cancel += CancelNewItemSelector;

                    uiSelector.AddGroup(group, selectorGroup);

                    // Trees group items
                    var actors = ActorManager.GetActorsInGroup("RockGroup");
                    for (int i = 0; i < actors.Count; i++)
                    {
                        selectorGroup.AddItem(new ActorMenuItem(selectorGroup, actors[i].LocalizedName, actors[i].MenuTextureFile, actors[i], size, groupRadialOffset));
                    }
                }
                #endregion

                // WayPoint
                #region waypoint group
                if(!ignorePaths)
                {
                    RenderObject group = new ActorMenuItem(uiSelector, null, @"Textures\Tiles\modifier.waypointwhite", null, size, radialOffset);

                    UiSelector selectorGroup = new PieSelector(group, "Sim.EditObjects.PickToken");

                    ITransform transformSelector = selectorGroup as ITransform;
                    transformSelector.Local.Translation = new Vector3(0.0f, 0.0f, sizeMenuItem);
                    transformSelector.Compose();

                    selectorGroup.ComposeDefault += NewItemSelectorComposeDefault;
                    selectorGroup.Select += SelectNewItemSelector;
                    selectorGroup.Cancel += CancelNewItemSelector;

                    uiSelector.AddGroup(group, selectorGroup);

                    selectorGroup.AddItem(new ActorMenuItem(selectorGroup, Strings.Localize("actorNames.pathGeneric"), @"filter.pathplain", null, size, radialOffset));
                    selectorGroup.AddItem(new ActorMenuItem(selectorGroup, Strings.Localize("actorNames.pathRoad"), @"filter.pathroad", null, size, radialOffset));
                    selectorGroup.AddItem(new ActorMenuItem(selectorGroup, Strings.Localize("actorNames.pathWall"), @"filter.pathwall", null, size, radialOffset));
                    selectorGroup.AddItem(new ActorMenuItem(selectorGroup, Strings.Localize("actorNames.pathVeggie"), @"filter.pathflora", null, size, radialOffset));

                    //uiSelector.AddItem(new ActorMenuItem(uiSelector, Strings.Localize("actorNames.wayPoint"), null, @"modifier.waypointwhite", null, size, radialOffset));

                    // move the selector into position
                    {
                        //ITransform transformSelector = uiSelector as ITransform;
                        //transformSelector.Local.RotationY = MathHelper.ToRadians(14.0f);
                        //transformSelector.Local.RotationX = MathHelper.ToRadians(14.0f);
                        //transformSelector.Compose();
                    }
                    // Add the selector to the child list.
                }
                #endregion waypoint group

                uiSelector.ComposeDefault += NewItemSelectorComposeDefault;
                uiSelector.Select += SelectNewItemSelector;
                uiSelector.Cancel += CancelNewItemSelector;
                childList.Add(uiSelector);

            }   // end of AddUISelector()

            public void ActivateNewItemSelector(bool ignorePaths)
            {
                // Do we need to update things to relfect a change in whether or not we're ignoreing paths?
                if (newItemSelectorShim.IgnorePaths != ignorePaths)
                {
                    BokuGame.Unload(newItemSelectorShim);
                    newItemSelectorShim = new UIShim(AddUISelector, out newItemSelector, ignorePaths);
                    BokuGame.Load(newItemSelectorShim);
                    newItemSelectorShim.InitDeviceResources(BokuGame.bokuGame.GraphicsDevice);
                }

                Foley.PlayProgrammingMoveOut();

                // Restore the default item color to white.
                shared.curObjectColor = ColorPalette.GetIndexFromColor(Classification.Colors.White);

                newItemSelectorShim.Activate();
                newItemSelector.IndexDefaultItem = -1;

            }   // end of EditObjectUpdateObj ActivateNewItemSelector()

            /// <summary>
            /// Activate the add item pie menu, but record where any added item should go.
            /// </summary>
            /// <param name="addPosition"></param>
            public void ActivateNewItemSelector(Vector2 addPosition, bool ignorePaths)
            {
                this.addPosition = addPosition;
                this.addPositionIsExplicit = true;

                ActivateNewItemSelector(ignorePaths);

                // This may seem a bit strange but it's here for a reason.  On MouseInput.Left.WasReleased
                // the pie selector checks if the ClickedOnObject matches the current, in focus object.  If
                // they match it is selected.  If they're both null, the selector thinks the user has
                // clicked away and closes.  By setting this to a valid object ref we ensure that the 
                // first left release is ignored.  Which is what we want since the press was what brought
                // up the pie selector in the first place.
                MouseInput.ClickedOnObject = this;
                TouchContact touch = TouchInput.GetOldestTouch();
                if (touch != null)
                    touch.TouchedObject = this;
            }

            public void CancelNewItemSelector(UiSelector selector)
            {
                Foley.PlayProgrammingMoveBack();

                if (selector == this.newItemSelector)
                {
                    newItemSelectorShim.Deactivate();
                }
            }   // end of EditObjectUpdateObj CancelNewItemSelector()

            public void SelectNewItemSelector(UiSelector selector)
            {
                UiSelector.GroupData groupData = selector.SelectedItem as UiSelector.GroupData;
                if (groupData != null)
                {
                    // sub group was activated
                    Foley.PlayProgrammingMoveOut();
                }
                else
                {
                    Instrumentation.IncrementCounter(Instrumentation.CounterId.AddItem);

                    GameActor thingToColor = null;
                    GameActor thingToDistort = null;

                    //Object item = selector.ObjectSelectedItem;

                    ActorMenuItem item = selector.ObjectSelectedItem as ActorMenuItem;
                    if (item != null)
                    {
                        if (item.StaticActor != null)
                        {
                            if (addPositionIsExplicit)
                            {
                                ///Client has requested a specific position to add this guy.
                                ///Give the people what they want.
                                thingToColor = parent.AddActor(
                                    ActorFactory.Create(item.StaticActor),
                                    new Vector3(addPosition, float.MaxValue),
                                    InGame.inGame.shared.camera.Rotation);

                                // Reset
                                addPositionIsExplicit = false;
                            }
                            else
                            {
                                /// Default is to add at the cursor position.
                                thingToColor = parent.AddActorAtCursor(ActorFactory.Create(item.StaticActor));
                            }

                            // Give a ref to the actor to the HelpCard.  If it's active it 
                            // can then program the actor's brain.
                            InGame.inGame.shared.addItemHelpCard.Actor = thingToColor as GameActor;
                        }
                        else if(item.TextureFilename.StartsWith("filter.path"))
                        {
                            if (parent.UnderBudget)
                            {
                                /// If the waypoint editor is active, let it handle this.
                                if(!shared.editWayPoint.Active)
                                {
                                    int pathType = 0;
                                    if (item.TextureFilename.EndsWith("road"))
                                        pathType = Road.LastRoadCreated;
                                    else if (item.TextureFilename.Contains("wall"))
                                        pathType = Road.LastWallCreated;
                                    else if (item.TextureFilename.Contains("flora"))
                                        pathType = Road.LastVegCreated;
                                    Road.GenIndex = pathType;
                                    Vector3 pos = addPositionIsExplicit
                                        ? new Vector3(addPosition, float.MaxValue)
                                        : parent.cursor3D.Position;
                                    shared.editWayPoint.NewPath(pos, shared.curObjectColor);
                                }
                            }
                            else
                            {
                                Instrumentation.IncrementCounter(Instrumentation.CounterId.AddItemNoBudget);

                                Foley.PlayNoBudget();
                            }
                        }
                    }
                    else
                    {
                        Debug.Assert(false, "How did this happen?");
                    }


                    if (thingToColor != null)
                    {
                        Foley.PlayProgrammingAdd();

                        shared.curObjectColor = ColorPalette.GetIndexFromColor(thingToColor.ClassColor);
                        //                        thingToColor.Classcolor = ColorPalette.GetColorFromIndex(shared.curObjectColor);
                        thingToDistort = thingToColor;
                    }

                    if (thingToDistort != null)
                    {
                        parent.DistortionPulse(thingToDistort, true);
                    }

                    newItemSelectorShim.Deactivate();

                    InGame.IsLevelDirty = true;
                }
            }   // end of EditObjectUpdateObj SelectNewItemSelector()

            public RenderObject NewItemSelectorComposeDefault(UiSelector selector, Object item, Object param, int indexItem)
            {
                RenderObject renderObject = null;

                return renderObject;
            }   // end of EditObjectUpdateObj ComposeDefault()

            public override void LoadContent(bool immediate)
            {
                BokuGame.Load(newItemSelector, immediate);

                BokuGame.Load(newItemSelectorShim, immediate);
            }

            public override void InitDeviceResources(GraphicsDevice device)
            {
                BokuGame.InitDeviceResources(newItemSelector, device);

                BokuGame.InitDeviceResources(newItemSelectorShim, device);
            }

            public override void UnloadContent()
            {
                BokuGame.Unload(newItemSelector);

                BokuGame.Unload(newItemSelectorShim);
            }

            public override void DeviceReset(GraphicsDevice device)
            {
                BokuGame.DeviceReset(newItemSelector, device);

                BokuGame.DeviceReset(newItemSelectorShim, device);
            }

        }   // end of class EditObjectUpdateObj

    }   // end of class InGame

}   // end of namespace Boku

