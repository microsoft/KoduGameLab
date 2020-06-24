
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
using KoiX.Managers;
using KoiX.Scenes;
using KoiX.Text;
using KoiX.UI.Dialogs;

using Boku;
using Boku.Audio;
using Boku.Base;
using Boku.Common;
using Boku.Common.Gesture;
using Boku.Fx;
using Boku.Input;
using Boku.SimWorld;
using Boku.SimWorld.Terra;
using Boku.UI;
using Boku.UI2D;


namespace Boku.Scenes.InGame.MouseEditTools
{
    public class EditObjectsTool : BaseMouseEditTool, INeedsDeviceReset
    {
        #region Members

        static EditObjectsTool instance = null;

        AddItemPieMenuDialog addItemPieMenu;
        AddItemPieMenuDialog addItemGroup1Menu;
        AddItemPieMenuDialog addItemGroup2Menu;
        AddItemPieMenuDialog addItemObjectsMenu;
        AddItemPieMenuDialog addItemTreesMenu;
        AddItemPieMenuDialog addItemPipesMenu;
        AddItemPieMenuDialog addItemUnderwaterMenu;
        AddItemPieMenuDialog addItemRocksMenu;
        AddItemPieMenuDialog addItemPathsMenu;

        OffActorPopupDialog offActorMenu;
        OnActorPopupDialog onActorMenu;

        GameActor focusActor = null;        // Actor under cursor.
        GameActor cachedActor = null;       // Actor whose creatables list has been cached.
        int focusColorIndex;                // Color index of focus actor's color in ColorPalette.
        GameActor dragActor = null;         // Pretty much just used for dragging.
        HitInfo MouseTouchHitInfo = null;   // Current terrain hit, if any.


        Texture2D closeSquareTexture = null;        // Current texture we're using.
        Texture2D closeSquareLitTexture = null;     // Selected version.
        Texture2D closeSquareUnlitTexture = null;   // Unselected version.




        #endregion Members

        #region Accessors

        public GameActor FocusActor
        {
            get { return focusActor; }
            set
            {
                if (focusActor != value)
                {
                    focusActor = value;
                    if (focusActor != null && focusActor != cachedActor)
                    {
                        cachedActor = focusActor;
                        cachedActor.CacheCreatables();
                    }
                }
            }
        }
        public GameActor SelectedActor
        {
            get { return dragActor; }
        }

        /// <summary>
        /// Are we currently dragging an actor around?
        /// </summary>
        public bool DraggingObject
        {
            get { return dragActor != null; }
        }

        static public AddItemPieMenuDialog AddItemPieMenu
        {
            get { return instance.addItemPieMenu; }
        }

        #endregion

        #region Public

        // c'tor
        public EditObjectsTool()
        {
            HelpOverlayID = @"EditObjects";

            // Get references.
            inGame = Boku.InGame.inGame;
            shared = inGame.shared;

            addItemPieMenu = new AddItemPieMenuDialog();
            addItemGroup1Menu = new AddItemPieMenuDialog();
            addItemGroup2Menu = new AddItemPieMenuDialog();
            addItemObjectsMenu = new AddItemPieMenuDialog();
            addItemTreesMenu = new AddItemPieMenuDialog();
            addItemPipesMenu = new AddItemPieMenuDialog();
            addItemUnderwaterMenu = new AddItemPieMenuDialog();
            addItemRocksMenu = new AddItemPieMenuDialog();
            addItemPathsMenu = new AddItemPieMenuDialog();

            offActorMenu = new OffActorPopupDialog();
            onActorMenu = new OnActorPopupDialog();

            SetUpAddItemMenu();

        }   // end of c'tor

        public static BaseMouseEditTool GetInstance()
        {
            if (instance == null)
            {
                instance = new EditObjectsTool();
            }
            return instance;
        }   // end of GetInstance()

        Vector3 actorOffset;


        void DoScaleActor(float deltaScale, GameActor actorToScale)
        {
            if (actorToScale != null)
            {
                actorToScale.ReScale = MathHelper.Clamp(actorToScale.ReScale + deltaScale, EditObjectParameters.k_ObjectMinScale, EditObjectParameters.k_ObjectMaxScale);
                Boku.InGame.IsLevelDirty = true;
            }
        }

        void DoRotateActor(float rotationDelta, GameActor actorToRotate)
        {
            if (actorToRotate != null)
            {
                actorToRotate.Movement.RotationZ -= rotationDelta;
                Boku.InGame.IsLevelDirty = true;
            }
        }

        void HandleFocusGlow()
        {
            if (!KoiLibrary.LastTouchedDeviceIsKeyboardMouse) { return; }

            // If the mouse took over from the touch, it should clear any
            // highlights the touch had going.
            inGame.TouchEdit.Clear();

            Camera camera = inGame.shared.camera;
            MouseEdit mouseEdit = inGame.MouseEdit;

            MouseTouchHitInfo = MouseEdit.MouseTouchHitInfo;
            mouseEdit.SetFocusActorGlow(camera);

            // If cursor is over actor, toggle color palette and set/clear FocusActor.
            if (MouseTouchHitInfo.HaveActor)
            {
                FocusActor = MouseTouchHitInfo.ActorHit;
                focusColorIndex = ColorPalette.GetIndexFromColor(FocusActor.Classification.Color);
                Boku.InGame.ColorPalette.Active = true;
            }
            else
            {
                FocusActor = null;
                Boku.InGame.ColorPalette.Active = false;
            }

        }   // end of HandleFocusGlow()

        public override void Update()
        {
            if (Active)
            {
                inGame.shared.addItemHelpCard.Update();

                HandleFocusGlow();

            }   // end if active.

            // This tool is odd enough compared to the standard 
            // tools that we don't want to call the base update.
            // The standard tools are designed to "paint" their 
            // effects while this one manipulates actors.
            //base.Update();

        }   // end of Update()

        public void Render(Camera camera)
        {
            if (Active)
            {
                Boku.InGame.RenderColorMenu(focusColorIndex);
            }
        }   // end of Render()

        #endregion Public

        #region InputEventHandler

        public override void RegisterForInputEvents()
        {
            KoiLibrary.InputEventManager.RegisterForEvent(this, InputEventManager.Event.MouseLeftDown);
            KoiLibrary.InputEventManager.RegisterForEvent(this, InputEventManager.Event.MousePosition);
            KoiLibrary.InputEventManager.RegisterForEvent(this, InputEventManager.Event.MouseRightDown);
            KoiLibrary.InputEventManager.RegisterForEvent(this, InputEventManager.Event.Keyboard);
            KoiLibrary.InputEventManager.RegisterForEvent(this, InputEventManager.Event.GamePad);
            KoiLibrary.InputEventManager.RegisterForEvent(this, InputEventManager.Event.Tap);
            KoiLibrary.InputEventManager.RegisterForEvent(this, InputEventManager.Event.OnePointDrag);
            KoiLibrary.InputEventManager.RegisterForEvent(this, InputEventManager.Event.TwoPointDrag);
            KoiLibrary.InputEventManager.RegisterForEvent(this, InputEventManager.Event.Hold);
        }   // RegisterForInputEvents()

        public override bool ProcessMouseLeftDownEvent(MouseInput input)
        {
            Debug.Assert(Active);

            if (KoiLibrary.InputEventManager.MouseFocusObject == null)
            {
                // Claim mouse focus as ours.
                KoiLibrary.InputEventManager.MouseFocusObject = this;

                // Register to get left up events.
                KoiLibrary.InputEventManager.RegisterForEvent(this, InputEventManager.Event.MouseLeftUp);

                // Starting to drag?
                if (FocusActor != null)
                {
                    dragActor = FocusActor;
                    actorOffset = dragActor.Movement.Position - MouseTouchHitInfo.TerrainPosition;
                }
                else
                {
                    SpriteCamera camera = ToolBarDialog.Camera;
                    Vector3 worldPosition = MouseEdit.MouseTouchHitInfo.TerrainPosition;
                    ShowAddItemMenu(camera, worldPosition: worldPosition);
                }

                return true;
            }

            return base.ProcessMouseLeftDownEvent(input);
        }   // end of ProcessMouseLeftDownEvent()

        public override bool ProcessMousePositionEvent(MouseInput input)
        {
            Debug.Assert(Active);

            // Keep FocusActor up to date.

            if (KoiLibrary.InputEventManager.MouseFocusObject == this)
            {
                SmoothCamera camera = inGame.Camera;

                // Dragging?
                if (dragActor != null)
                {
                    DragActor();
                    return true;
                }

                // Focus actor?
                // Color palette?

                if (LowLevelMouseInput.Left.IsPressed)
                {

                }
                else if (LowLevelMouseInput.Right.IsPressed)
                {

                }

                return true;
            }

            return base.ProcessMousePositionEvent(input);
        }   // end of ProcessMousePositionEvent()

        public override bool ProcessMouseLeftUpEvent(MouseInput input)
        {
            Debug.Assert(Active);

            // Release focus.
            KoiLibrary.InputEventManager.MouseFocusObject = null;

            // Dragging?
            if (dragActor != null)
            {
                dragActor = null;

                return true;
            }

            return base.ProcessMouseLeftUpEvent(input);
        }   // end of ProcessMouseLeftUpEvent()

        public override bool ProcessMouseRightDownEvent(MouseInput input)
        {
            Debug.Assert(Active);

            if (KoiLibrary.InputEventManager.MouseFocusObject == null)
            {
                SpriteCamera camera = ToolBarDialog.Camera;

                // Right click launches menus.  Decide which menu 
                // based on whether or not we have a FocusActor.
                // Starting to drag?
                if (FocusActor != null)
                {
                    ShowOnActorMenu(camera, screenPosition: input.Position, actor: FocusActor);
                }
                else
                {
                    Vector3 worldPosition = MouseEdit.MouseTouchHitInfo.TerrainPosition;
                    ShowOffActorMenu(camera, screenPosition: input.Position, worldPosition: worldPosition);
                }

                return true;
            }

            return base.ProcessMouseRightDownEvent(input);
        }   // end of ProcessMouseRightDownEvent()

        public override bool ProcessKeyboardEvent(KeyInput input)
        {
            Debug.Assert(Active);

            switch(input.Key)
            {
                case Keys.Delete:
                    if (FocusActor != null)
                    {
                        Boku.InGame.inGame.editObjectUpdateObj.CutAction(FocusActor);
                        return true;
                    }
                    break;
            }


            return base.ProcessKeyboardEvent(input);
        }   // end of ProcessKeyboardEvent()

        public override bool ProcessTouchTapEvent(TapGestureEventArgs gesture)
        {
            MouseEdit.UpdateHitInfo(inGame.Camera, gesture.Position);

            SpriteCamera camera = ToolBarDialog.Camera;

            if (MouseEdit.MouseTouchHitInfo.ActorHit != null)
            {
                ShowOnActorMenu(camera, gesture.Position, MouseEdit.MouseTouchHitInfo.ActorHit);
            }
            else
            {
                Vector3 worldPosition = MouseEdit.MouseTouchHitInfo.TerrainPosition;
                ShowAddItemMenu(camera, worldPosition: worldPosition);
            }

            return true;
        }   // end of ProcessTouchTapEvent()

        public override bool ProcessTouchOnePointDragEvent(OnePointDragGestureEventArgs gesture)
        {
            MouseEdit.UpdateHitInfo(inGame.Camera, gesture.CurrentPosition);

            // Are we beginning to drag an actor?  Must have an actor and no one else owning focus.
            if (gesture.Gesture == GestureType.OnePointDragBegin && MouseEdit.MouseTouchHitInfo.ActorHit != null && KoiLibrary.InputEventManager.TouchFocusObject == null)
            {
                // Take focus.
                KoiLibrary.InputEventManager.TouchFocusObject = this;
                dragActor = MouseEdit.MouseTouchHitInfo.ActorHit;
                actorOffset = dragActor.Movement.Position - MouseEdit.MouseTouchHitInfo.TerrainPosition;

                return true;
            }

            if (KoiLibrary.InputEventManager.TouchFocusObject == this)
            {
                // End of drag?  Release focus.
                if (gesture.Gesture == GestureType.OnePointDragEnd)
                {
                    KoiLibrary.InputEventManager.TouchFocusObject = null;
                    dragActor = null;

                    return true;
                }

                if (dragActor != null)
                {
                    DragActor();

                    return true;
                }
            }

            return base.ProcessTouchOnePointDragEvent(gesture);
        }   // end of ProcessTouchOnePointDragEvent()

        public override bool ProcessTouchTwoPointDragEvent(TwoPointDragGestureEventArgs gesture)
        {
            // if on actor -> rotate/size actor
            // else left fall through

            return base.ProcessTouchTwoPointDragEvent(gesture);
        }   // end of ProcessTouchTwoPointDragEvent()

        public override bool ProcessTouchHoldEvent(TapGestureEventArgs gesture)
        {
            MouseEdit.UpdateHitInfo(inGame.Camera, gesture.Position);

            SpriteCamera camera = ToolBarDialog.Camera;

            if (MouseEdit.MouseTouchHitInfo.ActorHit == null)
            {
                Vector3 worldPosition = MouseEdit.MouseTouchHitInfo.TerrainPosition;
                ShowOffActorMenu(camera, gesture.Position, worldPosition: worldPosition);
            }
            else
            {
                ShowOnActorMenu(camera, gesture.Position, MouseEdit.MouseTouchHitInfo.ActorHit);
            }

            return true;
        }   // end of ProcessTouchHoldEvent()


        #endregion

        #region Internal

        void ShowOnActorMenu(SpriteCamera camera, Vector2 screenPosition, GameActor actor)
        {
            // Show the menu where we clicked on screen.
            RectangleF rect = onActorMenu.Rectangle;
            rect.SetPosition(camera.ScreenToCamera(screenPosition));
            onActorMenu.Rectangle = rect;
            onActorMenu.FocusActor = actor;

            DialogManagerX.ShowDialog(onActorMenu, camera);

        }   // end of ShowOnActorMenu()

        void ShowOffActorMenu(SpriteCamera camera, Vector2 screenPosition, Vector3 worldPosition)
        {
            // Show the menu where we clicked on screen.
            RectangleF rect = offActorMenu.Rectangle;
            rect.SetPosition(camera.ScreenToCamera(screenPosition));
            offActorMenu.Rectangle = rect;
            offActorMenu.WorldPosition = worldPosition;

            DialogManagerX.ShowDialog(offActorMenu, camera);

        }   // end of ShowOffActorMenu()

        void ShowAddItemMenu(SpriteCamera camera, Vector3 worldPosition)
        {
            Debug.Assert(worldPosition != Vector3.Zero, "Technically, Zero is valid but more likely it means something is wrong.");

            addItemPieMenu.SetParams(parent: null, offset: Vector2.Zero, addPosition: worldPosition.XY());

            DialogManagerX.ShowDialog(addItemPieMenu);

        }   // end of ShowAddItemMenu()


        void SetUpAddItemMenu()
        {
            //
            // AddItemPieMenu
            //
            StaticActor boku = ActorManager.GetActor("BokuBot");
            addItemPieMenu.AddElement(new KoiX.UI.PieMenuElement(addItemPieMenu, boku.NonLocalizedName, boku.MenuTextureFile, staticActor: boku));
            
            StaticActor rover = ActorManager.GetActor("Rover");
            addItemPieMenu.AddElement(new KoiX.UI.PieMenuElement(addItemPieMenu, rover.NonLocalizedName, rover.MenuTextureFile, staticActor: rover));

            StaticActor fruit = ActorManager.GetActor("Fruit");
            addItemPieMenu.AddElement(new KoiX.UI.PieMenuElement(addItemPieMenu, fruit.NonLocalizedName, fruit.MenuTextureFile, staticActor: fruit));

            CreateGroup(addItemPieMenu, "BotGroup1", @"Textures\Tiles\group.botsI", addItemGroup1Menu);
            CreateGroup(addItemPieMenu, "BotGroup2", @"Textures\Tiles\group.botsII", addItemGroup2Menu);
            CreateGroup(addItemPieMenu, "ObjectGroup", @"Textures\Tiles\group.objects", addItemObjectsMenu);
            CreateGroup(addItemPieMenu, "TreeGroup", @"Textures\Tiles\filter.tree", addItemTreesMenu);
            CreateGroup(addItemPieMenu, "PipeGroup", @"Textures\Tiles\filter.pipe", addItemPipesMenu);
            CreateGroup(addItemPieMenu, "UnderwaterGroup", @"Textures\Tiles\filter.underwater", addItemUnderwaterMenu);
            CreateGroup(addItemPieMenu, "RockGroup", @"Textures\Tiles\filter.rock", addItemRocksMenu);

            // Path group handled explicitely.
            /*
            addItemPieMenu.AddElement(new KoiX.UI.PieMenuElement(addItemPieMenu, "paths", @"Textures\Tiles\filter.pathplain", child: addItemPathsMenu));
            addItemPathsMenu.AddElement(new KoiX.UI.PieMenuElement(addItemPathsMenu, Strings.Localize("actorNames.pathGeneric"), @"Textures\Tiles\filter.pathplain"));
            addItemPathsMenu.AddElement(new KoiX.UI.PieMenuElement(addItemPathsMenu, Strings.Localize("actorNames.pathRoad"), @"Textures\Tiles\filter.pathroad"));
            addItemPathsMenu.AddElement(new KoiX.UI.PieMenuElement(addItemPathsMenu, Strings.Localize("actorNames.pathWall"), @"Textures\Tiles\filter.pathwall"));
            addItemPathsMenu.AddElement(new KoiX.UI.PieMenuElement(addItemPathsMenu, Strings.Localize("actorNames.pathVeggie"), @"Textures\Tiles\filter.pathflora"));
            */
        }   // end of SetUpAddItemMenu()

        /// <summary>
        /// Helper function to create submenus.
        /// </summary>
        /// <param name="parentMenu"></param>
        /// <param name="groupName"></param>
        /// <param name="groupTexture"></param>
        /// <param name="groupMenu"></param>
        void CreateGroup(AddItemPieMenuDialog parentMenu, string groupName, string groupTexture, AddItemPieMenuDialog groupMenu)
        {
            parentMenu.AddElement(new KoiX.UI.PieMenuElement(parentMenu, groupName, groupTexture, child: groupMenu));

            var actors = ActorManager.GetActorsInGroup(groupName);
            for (int i = 0; i < actors.Count; i++)
            {
                groupMenu.AddElement(new KoiX.UI.PieMenuElement(groupMenu, actors[i].LocalizedName, actors[i].MenuTextureFile, staticActor: actors[i]));
            }                    
        }   // end of CreateGroup()

        protected override void OnActivate()
        {
            base.OnActivate();

            inGame.HideCursor();

            closeSquareTexture = closeSquareUnlitTexture;

        }   // end of OnActivate()

        protected override void OnDeactivate()
        {            
            // Ensure that the selection highlights are off.
            inGame.TouchEdit.Clear();
            inGame.MouseEdit.Clear();

            //reset selection state on deactivation
            FocusActor = null;
            dragActor = null;
            //make sure the color palette doesn't stay up
            Boku.InGame.ColorPalette.Active = false;

            base.OnDeactivate();
        }   // end of OnDeactivate()

        #endregion Internal

        /// <summary>
        /// Moves the actor currently under the mouse/touch.
        /// Note that MouseTouchHitInfo needs to be up to date for this to work.
        /// </summary>
        void DragActor()
        {
            HitInfo MouseTouchHitInfo = MouseEdit.MouseTouchHitInfo;
            Vector3 position = MouseTouchHitInfo.TerrainPosition + actorOffset;
            dragActor.Movement.Position = Boku.InGame.SnapPosition(position);

            // Try and keep the bot directly under the mouse cursor while still being at the correct height.
            // A possible alternative would be to use the cursor's 2d position for the bot and just have the
            // bot float at the appropriate height over the cursor.  This would allow more exact placement of
            // bots over terrain but it would mean a visual disconnect between where the cursor is and where
            // the bot is.  There would also be a jump when the bot is first clicked on since the terrain
            // position of the cursor is most likely further back than the bot's current position.
            if (MouseTouchHitInfo.VerticalOffset == 0.0f)
            {
                Camera camera = inGame.shared.camera; 
                Vector3 terrainToCameraDir = MouseTouchHitInfo.TerrainPosition - camera.From;
                terrainToCameraDir.Normalize();
                position = MouseTouchHitInfo.TerrainPosition + terrainToCameraDir * (dragActor.EditHeight / terrainToCameraDir.Z);
                dragActor.Movement.Position = Boku.InGame.SnapPosition(position);
            }

            // If the actor is supposed to stay above water, try to enforce that.
            // This can have some strange visual effects since it forces the actor to 
            // float above where the mouse cursor is but the alternative is to have
            // actor get dragged under water.
            if (dragActor.StayAboveWater)
            {
                float waterAlt = Terrain.GetWaterBase(position);
                if (waterAlt != 0)
                {
                    position.Z = waterAlt + dragActor.EditHeight;
                    dragActor.Movement.Position = Boku.InGame.SnapPosition(position);
                }
            }

            Boku.InGame.IsLevelDirty = true;
        }   // end of DragActor()

        #region INeedsDeviceReset Members

        public void LoadContent(bool immediate)
        {
            if (closeSquareLitTexture == null)
            {
                closeSquareLitTexture = KoiLibrary.LoadTexture2D(@"Textures\GridElements\CloseSquare");
            }
            if (closeSquareUnlitTexture == null)
            {
                closeSquareUnlitTexture = KoiLibrary.LoadTexture2D(@"Textures\GridElements\CloseSquareDesat");
            }

            addItemPieMenu.LoadContent();
        }

        public void InitDeviceResources(GraphicsDevice device)
        {
        }

        public void UnloadContent()
        {
            DeviceResetX.Release(ref closeSquareLitTexture);
            DeviceResetX.Release(ref closeSquareUnlitTexture);
        }

        public void DeviceReset(GraphicsDevice device)
        {
        }

        #endregion
    }   // class EditObjectsTool

}   // end of namespace Boku.Scenes.InGame.MouseEditTools


