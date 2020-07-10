// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.


using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;

using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Audio;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using Microsoft.Xna.Framework.Storage;

using Boku.Base;
using Boku.Common;
using Boku.UI;
using Boku.Programming;
using Boku.Input;
using Boku.Audio;

namespace Boku.UI
{

    public class ReflexHandle : GameObject, IControl, ITransform
    {
        public const string idHandle = "row handle";

        /// <summary>
        /// Static varaible used when moving/cutting/pasting blocks of reflexes.
        /// </summary>
        public static ReflexBlock reflexBlock = new ReflexBlock();

        protected class UpdateObjMoveReflex : UpdateControl
        {
            private ReflexHandle parent;

            private CommandMap commandMap;
            
            public UpdateObjMoveReflex(ReflexHandle parent)
            {
                this.parent = parent;

                commandMap = new CommandMap(@"ReflexHandleMove");
            }

            public override void Update()
            {
                if (CommandStack.Peek() == commandMap)
                {
                    GamePadInput pad = GamePadInput.GetGamePad0();

                    if (Actions.Select.WasPressed)
                    {
                        Actions.Select.ClearAllWasPressedState();
                        GamePadInput.ClearAllWasPressedState(3);

                        parent.PlaceReflex(null, null);
                    }

                    if(Actions.ComboUp.WasPressedOrRepeat)
                    {
                        Actions.ComboUp.ClearAllWasPressedState();
                        GamePadInput.ClearAllWasPressedState(3);

                        parent.MoveUp(null, null);
                    }

                    if (Actions.ComboDown.WasPressedOrRepeat)
                    {
                        Actions.ComboDown.ClearAllWasPressedState();
                        GamePadInput.ClearAllWasPressedState(3);

                        parent.MoveDown(null, null);
                    }

                    if (Actions.ComboRight.WasPressed)
                    {
                        // Indent the current block.
                        reflexBlock.Indent(true);
                    }

                    if (Actions.ComboLeft.WasPressed)
                    {
                        // Unindent the current block.
                        reflexBlock.Unindent(true);
                    }
                }
            }

            public override void Activate()
            {
                CommandStack.Push(commandMap);
                HelpOverlay.Push("RowMove");
            }
            public override void Deactivate()
            {
                CommandStack.Pop(commandMap);
                HelpOverlay.Pop();
            }
            public override void AddCommands(CommandMap map)
            {
                commandMap.Add(map);
            }
            public override void RemoveCommands(CommandMap map)
            {
                commandMap.Remove(map);
            }
            public override void AddCommandsToControl(IControl control)
            {
                control.AddCommands(this.commandMap);
            }
            public override void RemoveCommandsFromControl(IControl control)
            {
                control.RemoveCommands(this.commandMap);
            }
        }

        protected class UpdateObjEditReflex : UpdateControl
        {
            private ReflexHandle parent;

            private CommandMap commandMap;

            public UpdateObjEditReflex(ReflexHandle parent)
            {
                this.parent = parent;

                commandMap = new CommandMap(@"ReflexHandle");
            }
            public override void Update()
            {
                // Check for input focus.
                if (commandMap == CommandStack.Peek())
                {
                    GamePadInput pad = GamePadInput.GetGamePad0();

                    if (Actions.Select.WasPressed)
                    {
                        Actions.Select.ClearAllWasPressedState(3);

                        parent.MoveReflex(null, null);
                    }

                    /*
                    // Replaced w/ auto blank line feature.
                    if(pad.ButtonY.WasPressed)
                    {
                        parent.InsertReflex(null, null);

                        GamePadInput.ClearAllWasPressedState(3);
                    }
                    */

                    // Cut.
                    if (Actions.Cut.WasPressed)
                    {
                        Actions.Cut.ClearAllWasPressedState(3);

                        reflexBlock.Init(this.parent.LineNumber - 1);
                        reflexBlock.Cut();
                    }

                    // Copy
                    if (Actions.X.WasPressed)
                    {
                        Actions.X.ClearAllWasPressedState();

                        // Update the cut/paste buffer with the current ReflexData.
                        reflexBlock.Init(this.parent.LineNumber - 1);
                        reflexBlock.Copy();

                        HelpOverlay.Pop();
                        if (ReflexPanel.CutPasteBuffer == null)
                        {
                            HelpOverlay.Push("RowHandleEmptyPasteBuffer");
                        }
                        else
                        {
                            HelpOverlay.Push("RowHandleFullPasteBuffer");
                        }
                    }

                    // Paste
                    if (Actions.Paste.WasPressed)
                    {
                        Actions.Paste.ClearAllWasPressedState(3);

                        reflexBlock.Init(this.parent.LineNumber - 1);
                        reflexBlock.Paste();
                    }

                }   // end of if we have input focus.

                // Ensure the reflex has the correct indent.
                //ReflexPanel curPanel = (ReflexPanel)parent.parent;
                //curPanel.AnimatePanelIndent(false);

            }   // end of Update()

            public override void Activate()
            {
                CommandStack.Push(commandMap);
            }
            public override void Deactivate()
            {
                CommandStack.Pop(commandMap);
            }

            public override void AddCommands(CommandMap map)
            {
                commandMap.Add(map);
            }
            public override void RemoveCommands(CommandMap map)
            {
                commandMap.Remove(map);
            }
            public override void AddCommandsToControl(IControl control)
            {
                control.AddCommands(this.commandMap);
            }
            public override void RemoveCommandsFromControl(IControl control)
            {
                control.RemoveCommands(this.commandMap);
            }
        }

        private Object parent;

        public ControlRenderObj renderObj;

        protected UpdateControl updateObj; // active update object
        protected UpdateControl updateObjPending;

        protected UpdateObjMoveReflex updateObjMoveReflex;
        protected UpdateObjEditReflex updateObjEditReflex;
        protected int lineNumber; // one based number that is the reflex line number this handle is within
        protected static List<Texture2D> rtLineNumber = new List<Texture2D>();

        /// <summary>
        /// Dispose of any textures allocated 
        /// </summary>
        public static void Dispose()
        {
            foreach (Texture2D texture in rtLineNumber)
            {
                texture.Dispose();
            }
            rtLineNumber.Clear();
        }

        public static void LoadContent(bool immediate)
        {
        }

        public static void InitDeviceResources(GraphicsDevice device)
        {
        }

        public static void UnloadContent()
        {
            rtLineNumber.Clear();
        }

        /// <summary>
        /// Recreate render targets.
        /// </summary>
        public static void DeviceReset(GraphicsDevice device)
        {
        }

        public enum States
        {
            Inactive,
            Active,
            Hot,
            Disabled,
        }
        private States state = States.Inactive;
        private States pendingState = States.Inactive;

        public States State
        {
            get { return state; }
            set { state = value; }
        }
        public States PendingState
        {
            get { return pendingState; }
            set 
            { 
                pendingState = value;
                if (pendingState == States.Hot && InGame.inGame.Editor.IndexActivePanel != -1)
                {
                    UiCursor.ActiveCursor.Parent = this;
                }
            }
        }
        private Reflex reflex;
//        private ControlCollection controls;

        public ReflexHandle(Object parent, Reflex reflex, ControlCollection controls)
        {
            this.parent = parent;
            this.reflex = reflex;
//            this.controls = controls;

            renderObj = controls.InstanceControlRenderObj(parent, idHandle);

            UpdateReflexLineNumber();

            updateObjMoveReflex = new UpdateObjMoveReflex( this );
            updateObjEditReflex = new UpdateObjEditReflex(this);

            updateObjPending = updateObjEditReflex;
        }

        public int LineNumber
        {
            get
            {
                return this.lineNumber;
            }
            set
            {
                if (this.lineNumber != value)
                {
                    this.lineNumber = value;
                    Debug.Assert((this.lineNumber - 1) <= rtLineNumber.Count, "line number being added that skips others");
                    if (this.lineNumber > rtLineNumber.Count)
                    {
                        CacheLineNumberTexture();
                    }
                    UpdateReflexLineNumber();
                    AffixLineNumberToCurrentState();
                }
            }
        }

        // reflect ITransform into the renderObj
        Transform ITransform.Local
        {
            get
            {
                ITransform transform = renderObj as ITransform;
                return transform.Local;
            }
            set
            {
                ITransform transform = renderObj as ITransform;
                transform.Local = value;
            }
        }
        Matrix ITransform.World
        {
            get
            {
                ITransform transform = renderObj as ITransform;
                return transform.World;
            }
        }
        bool ITransform.Compose()
        {
            ITransform transform = renderObj as ITransform;
            return transform.Compose();
        }
        void ITransform.Recalc(ref Matrix parentMatrix)
        {
            ITransform transform = renderObj as ITransform;
            transform.Recalc(ref parentMatrix);
            if (UiCursor.ActiveCursor != null && UiCursor.ActiveCursor.Parent == this)
            {
                ITransform transformCursor = UiCursor.ActiveCursor as ITransform;
                Matrix world = transform.World;
                transformCursor.Recalc(ref world);
            }
        }
        ITransform ITransform.Parent
        {
            get
            {
                return this.parent as ITransform;
            }
            set
            {
                this.parent = value;
            }
        }
        public void PlaceReflex(Object sender, EventArgs args)
        {
            Foley.PlayBack();

            reflexBlock.Moving = false;
            UiCursor.ActiveCursor.Parent = this;

            // switch modes
            this.updateObjPending = this.updateObjEditReflex;
            this.renderObj.State = ControlRenderObj.idStateHot;
            AffixLineNumberToCurrentState();
            BokuGame.objectListDirty = true;
        }   // end of PlaceReflex()
        
        public void MoveUp(Object sender, EventArgs args)
        {
            reflexBlock.MoveUp();

            /*
            ReflexPanel parentPanel = this.parent as ReflexPanel;
            parentPanel.MoveReflexUp();
            */

            UiCursor.ActiveCursor.Parent = this;
        }   // end of MoveUp()

        public void MoveDown(Object sender, EventArgs args)
        {
            reflexBlock.MoveDown();

            /*
            List<ReflexPanel> panels = InGame.inGame.Editor.ActivePanels;

            // Loop through all panels moving any "Moving" down.
            for (int i = panels.Count - 1; i >= 0; i--)
            {
                if (panels[i].Moving)
                {
                }
            }
            /*
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
            */
            
            /*
            ReflexPanel parentPanel = this.parent as ReflexPanel;
            parentPanel.MoveReflexDown();
            */

            UiCursor.ActiveCursor.Parent = this;
        }   // end of MoveDown()

        /// <summary>
        /// Put a reflex and its children into "moving" mode.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="args"></param>
        public void MoveReflex(Object sender, EventArgs args)
        {
            Foley.PlayPressA();

            // Init the Moving block.
            ReflexPanel panel = this.parent as ReflexPanel;
            int curIndex = panel.LineNumber - 1;
            reflexBlock.Init(curIndex);
            reflexBlock.Moving = true;

            UiCursor.ActiveCursor.Parent = this;

            // switch modes
            this.updateObjPending = this.updateObjMoveReflex;
            this.renderObj.State = ControlRenderObj.idStateSelected;
            AffixLineNumberToCurrentState();
            BokuGame.objectListDirty = true;
        }   // end of MoveReflex()

        public void InsertReflex(Object sender, EventArgs args)
        {
            ReflexPanel parentPanel = this.parent as ReflexPanel;
            parentPanel.InsertReflex();
        }
        public void RemoveReflex(Object sender, EventArgs args)
        {
            ReflexPanel parentPanel = this.parent as ReflexPanel;
            parentPanel.RemoveReflex();
        }


        protected bool SwitchToNormal(List<UpdateObject> updateList, List<RenderObject> renderList)
        {
            bool result = false;
            if (State == States.Inactive)
            {
                updateList.Add(updateObj);
                //updateObj.Activate();
                renderList.Add(renderObj);
                renderObj.Activate();
            }
            else if (State == States.Hot)
            {
                updateObj.Deactivate();
            }
            renderObj.State = ControlRenderObj.idStateNormal;
            AffixLineNumberToCurrentState();
            return result;
        }
        protected bool SwitchToInactive(List<UpdateObject> updateList, List<RenderObject> renderList)
        {
            bool result = false;
            if (State != States.Inactive)
            {
                renderObj.Deactivate();
                renderList.Remove(renderObj);
                if (State == States.Hot)
                {
                    updateObj.Deactivate();
                }
                updateList.Remove(updateObj);
                result = true; // remove us
            }
            return result;
        }
        protected bool SwitchToHot(List<UpdateObject> updateList, List<RenderObject> renderList)
        {
            bool result = false;
            if (State == States.Inactive)
            {
                updateList.Add(updateObj);
                //                updateObj.Activate();
                renderList.Add(renderObj);
                renderObj.Activate();
            }
            updateObj.Activate();

            renderObj.State = ControlRenderObj.idStateHot;
            AffixLineNumberToCurrentState();
            return result;
        }
        protected bool SwitchToDisabled(List<UpdateObject> updateList, List<RenderObject> renderList)
        {
            bool result = false;
            return result;
        }
        protected void ApplyUpdateObjChange(List<UpdateObject> updateList, List<RenderObject> renderList)
        {
            if (updateObj != null)
            {
                if (State == States.Hot)
                {
                    updateObj.Deactivate();
                }
                updateList.Remove(updateObj);
            }

            updateObj = updateObjPending;

            updateList.Add(updateObj);
            if (State == States.Hot)
            {
                updateObj.Activate();
            }
        }
        public override bool Refresh(List<UpdateObject> updateList, List<RenderObject> renderList)
        {
            bool result = false;

            // fix our update object
            // to the active one
            if (updateObj != updateObjPending)
            {
                ApplyUpdateObjChange(updateList, renderList);
            }
            
            if (State != PendingState)
            {
                if (PendingState == States.Active)
                {
                    SwitchToNormal(updateList, renderList);
                }
                else if (PendingState == States.Inactive)
                {
                    result = SwitchToInactive(updateList, renderList);
                }
                else if (PendingState == States.Hot)
                {
                    SwitchToHot(updateList, renderList);
                }
                else if (PendingState == States.Disabled)
                {
                    SwitchToDisabled(updateList, renderList);
                }

                State = PendingState;
            }
            return result;
        }
        override public void Activate()
        {
            if (State != States.Active)
            {
                PendingState = States.Active;
                BokuGame.objectListDirty = true;
            }
        }

        override public void Deactivate()
        {
            if (State != States.Inactive)
            {
                PendingState = States.Inactive;
                BokuGame.objectListDirty = true;
            }
        }
        // IControl
        void IControl.AddCommands(CommandMap map)
        {
            // only add them to the normal state object
            updateObjEditReflex.AddCommands(map);
        }
        void IControl.RemoveCommands(CommandMap map)
        {
            // only add them to the normal state object
            updateObjEditReflex.RemoveCommands(map);
        }

        bool IControl.Hot
        {
            get
            {
                return (State == States.Hot);
            }
            set
            {
                if (value)
                {
                    if (State != States.Hot)
                    {
                        PendingState = States.Hot;
                        BokuGame.objectListDirty = true;
                    }
                    else if (PendingState != States.Hot)
                    {
                        PendingState = States.Hot;
                    }
                }
                else
                {
                    if (State != States.Active)
                    {
                        PendingState = States.Active;
                        BokuGame.objectListDirty = true;
                    }
                    else if (PendingState != States.Active)
                    {
                        PendingState = States.Active;
                    }
                }
            }
        }
        bool IControl.Disabled
        {
            get
            {
                return (State == States.Disabled);
            }
            set
            {
                if (value)
                {
                    if (State != States.Disabled)
                    {
                        PendingState = States.Disabled;
                        BokuGame.objectListDirty = true;
                    }
                    else if (PendingState != States.Disabled)
                    {
                        PendingState = States.Disabled;
                    }
                }
                else
                {
                    if (State != States.Active)
                    {
                        PendingState = States.Active;
                        BokuGame.objectListDirty = true;
                    }
                    else if (PendingState != States.Active)
                    {
                        PendingState = States.Active;
                    }
                }
            }
        }

        protected void UpdateReflexLineNumber()
        {
            // fixup part infos lists for custom textures
            if (this.renderObj != null && this.lineNumber > 0)
            {
                // instance the list
                renderObj.listStaticPartInfos = renderObj.ListStaticPartInfos;
                if (renderObj.listStaticPartInfos != null)
                {
                    // walk it and update it
                    // the first mesh (one and only)
                    List<PartInfo> meshPartInfos = renderObj.listStaticPartInfos[0];
                    int indexPartInfo = 1;
                    if (indexPartInfo >= meshPartInfos.Count)
                    {
                        indexPartInfo = 0;
                    }
                    // the second part is the face, replace the texture
                    meshPartInfos[indexPartInfo].OverlayTexture = rtLineNumber[this.lineNumber - 1];
                    // force the background off until new model doesn't have a big black one on it
                    //meshPartInfos[indexPartInfo].DiffuseTexture = null;
                }
            }
        }
        protected void AffixLineNumberToCurrentState()
        {
            if (this.renderObj != null && this.lineNumber > 0)
            {
                // instance the list
                renderObj.listActivePartInfos = renderObj.ListActivePartInfos;

                if (renderObj.listActivePartInfos != null)
                {
                    // walk it and update it
                    
                    int indexMeshInfo = 1;
                    if (indexMeshInfo >= renderObj.listActivePartInfos.Count)
                    {
                        indexMeshInfo = 0;
                    }
                    List<PartInfo> meshPartInfos = renderObj.listActivePartInfos[indexMeshInfo];
                    int indexPartInfo = 1;
                    if (indexPartInfo >= meshPartInfos.Count)
                    {
                        indexPartInfo = 0;
                    }
                    if (this.lineNumber <= rtLineNumber.Count)
                    {
                        // the second part is the face, replace the texture
                        meshPartInfos[indexPartInfo].OverlayTexture = rtLineNumber[this.lineNumber - 1];
                        // force the background off until new model doesn't have a big black one on it
                        //meshPartInfos[indexPartInfo].DiffuseTexture = null;
                    }
                }
            }
        }

        protected void CacheLineNumberTexture()
        {
            Texture2D texture;
            const int textureSize = 64;

            RenderTarget2D rt = UI2D.Shared.RenderTarget64_64;

            InGame.SetRenderTarget(rt);
            SpriteBatch batch = UI2D.Shared.SpriteBatch;

            InGame.Clear(Color.Transparent);

            // Draw line number
            string text = lineNumber.ToString();
            // Center line number horizontally on rt.
            Vector2 pos = new Vector2((rt.Width - UI2D.Shared.GetGameFontLineNumbers().MeasureString(text).X) / 2.0f, 0.0f);

            batch.Begin();
            batch.DrawString(UI2D.Shared.GetGameFontLineNumbers(), text, pos, Color.Black);
            batch.End();

            InGame.RestoreRenderTarget();

            // Copy rendertarget result into texture.
            texture = new Texture2D(BokuGame.bokuGame.GraphicsDevice, textureSize, textureSize, false, SurfaceFormat.Color);
            int[] data = new int[textureSize * textureSize];
            rt.GetData<int>(data);
            texture.SetData<int>(data);

            rtLineNumber.Add(texture);
            Debug.Assert(this.lineNumber <= rtLineNumber.Count);
        }
    }
}
