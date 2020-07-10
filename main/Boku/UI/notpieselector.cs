// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.


using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;

using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;

using KoiX;
using KoiX.Input;


using Boku.Base;
using Boku.Common;
using Boku.UI;
using Boku.Programming;
using Boku.Input;
using Boku.SimWorld.Terra;

namespace Boku.UI
{
    public class NotPieSelector : UiSelector
    {
        public class UpdateObj : UpdateObject
        {
            private NotPieSelector parent = null;
            private CommandMap commandMap;

            public bool OwnsFocus()
            {
                return commandMap == CommandStack.Peek();
            }

            public UpdateObj(NotPieSelector parent)
                : base()
            {
                this.parent = parent;

                commandMap = new CommandMap(@"NotPieSelector");
                commandMap.name = @"NotPieSelector " + "NOT!!!";
            }
            public override void Update()
            {
                if (OwnsFocus())
                {
                    GamePadInput pad = GamePadInput.GetGamePad0();

                    if (Actions.Cancel.WasPressed)
                    {
                        Actions.Cancel.ClearAllWasPressedState();

                        // TODO (****) Get rid of the args...
                        parent.OnCancel(null, null);

                        Deactivate();
                    }

                    // Check if the currently selected item has help available.
                    // If it doesn't, suppress the help overlay text saying that
                    // help is available.  Note this also makes for a nice place
                    // to set a breakpoint to find all those things we still
                    // need to create help for.
                    Editor editor = InGame.inGame.Editor;
                    //bool helpAvailable = false;
                    //HelpOverlay.SuppressYButton = !helpAvailable;

                    if (pad.ButtonY.WasPressed)
                    {
                        pad.ButtonY.ClearAllWasPressedState();
                    }

                    // Handle stick movement, since that's kind of the point.
                    //parent.UpdateSelection(null, new StickEventArgs(pad.LeftStick));

                    // Clear out any other button presses.  This normally shouldn't be needed
                    // but if we're in the programming UI the reflex may think can still safely
                    // steal input.
                    GamePadInput.ClearAllWasPressedState();
                }

                if (parent.WhileHighlit != null)
                    parent.WhileHighlit();

            }
            public override void Activate()
            {
                CommandStack.Push(commandMap);
            }
            public override void Deactivate()
            {
                CommandStack.Pop(commandMap);
            }
        }

        public class RenderObj : RenderObject, ITransform
        {
            private Transform localToWorld = new Transform();
            private ITransform parent = null;
            private NotPieSelector owner;

            private PerspectiveUICamera camera = new PerspectiveUICamera();
         
            public bool Active
            {
                get;
                private set;
            }
            Transform ITransform.Local
            {
                get { return localToWorld; }
                set { localToWorld = value; }
            }
            Matrix ITransform.World
            {
                get 
                {
                    return parent != null
                        ? localToWorld.Matrix * parent.World
                        : localToWorld.Matrix;
                }
            }
            ITransform ITransform.Parent
            {
                get { return parent; }
                set { parent = value; }
            }
            private NotPieSelector Owner
            {
                get { return owner; }
            }

            public RenderObj(NotPieSelector owner)
                : base()
            {
                this.owner = owner;
            }
            bool ITransform.Compose()
            {
                return false;
            }
            void ITransform.Recalc(ref Matrix parentMatrix)
            {
            }
            public override void Render(Camera camera)
            {
                if (Active)
                {
                    if (InGame.inGame.Editor.RenderPieMenus)
                    {
                    }
                    else
                    {
                        // If not rendering, just store away for rendering later.
                        InGame.inGame.Editor.NotPieMenuList.Add(owner);
                    }
                }
            }
            public override void Activate()
            {
                Active = true;
            }
            public override void Deactivate()
            {
                Active = false;
            }
        }

        // make in constructor, once we have this.
        private RenderObj renderObj = null;
        private UpdateObj updateObj = null;
        private Object parent = null;

        private ushort lastTerrainIndex = TerrainMaterial.EmptyMatIdx;
        private int lastWaterIndex = -1;

        private enum States
        {
            Inactive,
            Active,
        }
        private States state = States.Inactive;
        private States pendingState = States.Inactive;

        public override Object Parent
        {
            get { return parent; }
        }

        public RenderObj RenderObject
        {
            get { return renderObj; }
            private set { renderObj = value; }
        }

        public UpdateObj UpdateObject
        {
            get { return updateObj; }
            private set { updateObj = value; }
        }

        public bool SelectWater { get; set; }
        public bool SetWater { get; set; }

        public string Name { get; set; }

        public int CurrentIndex { get; protected set; }

        public NotPieSelector(Object parent, string uiMode)
            : base()
        {
            this.parentSelector = parent as UiSelector;
            this.parent = parent;

            SelectWater = uiMode.Contains("waters");
            SetWater = uiMode.Contains("setwater");

            this.updateObj = new UpdateObj(this);
            this.renderObj = new RenderObj(this);
        }

        protected override void MoveCursor(Vector3 position, int indexNew)
        {
        }
        protected override void HideCursor()
        {
        }
        protected override void ShowCursor()
        {
        }

        public override bool Refresh(List<UpdateObject> updateList, List<RenderObject> renderList)
        {
            bool result = false;

            if (state != pendingState)
            {
                if (pendingState == States.Active)
                {
                    updateList.Add(updateObj);
                    updateObj.Activate();
                    renderList.Add(renderObj);
                    renderObj.Activate();
                }
                else
                {
                    renderObj.Deactivate();
                    renderList.Remove(renderObj);
                    updateObj.Deactivate();
                    updateList.Remove(updateObj);

                    result = true;
                }

                state = pendingState;
            }

            return result;
        }
        public override void Activate()
        {
            if (state != States.Active)
            {
                pendingState = States.Active;
                BokuGame.objectListDirty = true;

                Editor editor = InGame.inGame.Editor;
                if (editor.Active)
                {
                    HelpOverlay.Push(@"PieSelectorProgrammingNoSelection");
                }
                else
                {
                    HelpOverlay.Push(@"PieSelectorAddItemNoSelection");
                }

                if (SelectWater || SetWater)
                {
                    if (SetWater)
                    {
                        lastWaterIndex = RootData.SetWaterTypeIndex;
                    }
                    else
                    {
                        lastWaterIndex = RootData.WaterType;
                    }

                    if (lastWaterIndex < 0)
                        lastWaterIndex = Water.CurrentType;
                }
                else
                {
                    ushort rootDataMatIdx = (ushort)RootData.MaterialType;
                    if (TerrainMaterial.IsValid(rootDataMatIdx, false, false))
                        lastTerrainIndex = rootDataMatIdx;
                    else if (!TerrainMaterial.IsValid(lastTerrainIndex, false, false))
                        lastTerrainIndex = Terrain.CurrentMaterialIndex;
                }
            }
        }

        public override void Deactivate()
        {
            base.Deactivate();

            if (state != States.Inactive && pendingState != States.Inactive)
            {
                pendingState = States.Inactive;
                this.composedObjectPending = null;
                BokuGame.objectListDirty = true;

                for (int i = 0; i < Count; ++i)
                {
                    GroupData groupData = this[i] as GroupData;
                    if (groupData != null)
                    {
                        UiSelector uiSelector = groupData.selectorGroup as UiSelector;
                        if (uiSelector != null)
                        {
                            uiSelector.Deactivate();
                        }
                    }
                }

                HelpOverlay.Pop();
            }
        }   // end of Deactivate()

        protected ReflexCard ReflexRoot
        {
            get
            {
                UiSelector uiSelector = this as UiSelector;
                while (uiSelector != null)
                {
                    ReflexCard card = uiSelector.Parent as ReflexCard;
                    if (card != null)
                    {
                        return card;
                    }
                    uiSelector = uiSelector.ParentSelector;
                }
                return null;
            }
        }
        protected ReflexData RootData
        {
            get
            {
                ReflexCard card = ReflexRoot;
                Debug.Assert(card != null);
                Debug.Assert(card.Reflex != null);
                Debug.Assert(card.Reflex.Data != null);
                return card.Reflex.Data;
            }
        }

        public void OnSetTerrain(int type)
        {
            lastTerrainIndex = Terrain.UISlotToMatIndex(type);
        }
        public int OnGetTerrain()
        {
            Debug.Assert(TerrainMaterial.IsValid(lastTerrainIndex, false, false), "If this assert fires, set 'lastTerrainIndex's initial value to 'TerrainMaterial.DefaultMatIdx' and fire Daryl. (DZ)");//ToDo(DZ): Is this assert msg ship-able?

            return Terrain.MaterialIndexToUISlot(lastTerrainIndex);
        }
        public void OnPickTerrain(int type)
        {
            var matIdx = Terrain.UISlotToMatIndex(type);

            lastTerrainIndex = matIdx;

            RootData.MaterialType = matIdx;

            OnSelect(null, null);

            Deactivate();
        }
        public void OnSetWater(int type)
        {
            lastWaterIndex = type;
        }
        public int OnGetWater()
        {
            return lastWaterIndex;
        }
        public void OnPickWater(int type)
        {
            lastWaterIndex = type;

            if (SetWater)
            {
                RootData.SetWaterTypeIndex = type;
            }
            else
            {
                RootData.WaterType = type;
            }

            OnSelect(null, null);

            Deactivate();
        }
    }

};
