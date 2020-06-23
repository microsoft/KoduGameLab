
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
    public delegate void UiSelectorActionEvent(UiSelector selector);
    /// <summary>
    /// 
    /// </summary>
    /// <param name="selector"></param>
    /// <param name="item"></param>
    /// <param name="indexItem"></param>
    /// <returns>The return object must be a RenderObject and also support ITransform and IBounding.</returns>
    public delegate RenderObject UiSelectorComposeDefaultCallback(UiSelector selector, Object item, Object param, int indexItem);

    public abstract class UiSelector : GameObject, INeedsDeviceReset
    {
        public event UiSelectorActionEvent Select;
        public event UiSelectorActionEvent Cancel;
        public UiSelectorComposeDefaultCallback ComposeDefault;

        protected List<ItemData> items = new List<ItemData>();
        protected int indexDefaultItem = -1;
        protected int indexSelectedItem;
        protected const int indexCenteredItem = -1;
        protected AudioCue cueClick;
        protected SelectorLayout layout;
        protected RenderObject composedObject;
        protected RenderObject composedObjectPending;
        protected const float hotZTranslate = 0.1f;
        protected Billboard billboardCursorCw;
        protected Billboard billboardCursorCcw;

        protected UiSelector activeSubSelector;
        protected UiSelector parentSelector;

        public delegate void HighlightDelegate();
        public HighlightDelegate WhileHighlit;

        public class ItemData : INeedsDeviceReset
        {
            public Object item;
            public ProgrammingElement progElement;
            protected List<RenderObject> adornments;

            public ItemData(Object item, ProgrammingElement progElement)
            {
                this.item = item;
                this.progElement = progElement;
            }

            public void LoadContent(bool immediate)
            {
                if (this.adornments != null)
                {
                    foreach (INeedsDeviceReset renderObj in this.adornments)
                    {
                        BokuGame.Load(renderObj, immediate);
                    }
                }
                INeedsDeviceReset o = this.item as INeedsDeviceReset;
                BokuGame.Load(o, immediate);
            }

            public void InitDeviceResources(GraphicsDevice device)
            {
            }

            public void UnloadContent()
            {
                if (this.adornments != null)
                {
                    foreach (INeedsDeviceReset renderObj in this.adornments)
                    {
                        BokuGame.Unload(renderObj);
                    }
                }
                INeedsDeviceReset o = this.item as INeedsDeviceReset;
                BokuGame.Unload(o);
            }

            /// <summary>
            /// Recreate render targets
            /// </summary>
            /// <param name="graphics"></param>
            public void DeviceReset(GraphicsDevice device)
            {
                if (this.adornments != null)
                {
                    foreach (INeedsDeviceReset adornment in this.adornments)
                    {
                        BokuGame.DeviceReset(adornment, device);
                    }
                }

                INeedsDeviceReset o = this.item as INeedsDeviceReset;
                BokuGame.DeviceReset(o, device);
            }

            public void AddAdornment(RenderObject adornment)
            {
                if (this.adornments == null)
                {
                    this.adornments = new List<RenderObject>();
                }
                this.adornments.Add( adornment );
            }

            public List<RenderObject> Adornments
            {
                get { return this.adornments; }
            }

            public void OnHighlight(UiSelector owner)
            {
                if (progElement != null)
                {
                    if(progElement.OnHighlightDel != null)
                        progElement.OnHighlightDel();

                    if (progElement.WhileHighlitDel != null)
                        owner.WhileHighlit += progElement.WhileHighlitDel;
                }
            }

            public void OnUnHighlight(UiSelector owner)
            {
                if ((progElement != null) && (owner.WhileHighlit != null))
                {
                    if (progElement.WhileHighlitDel != null)
                        owner.WhileHighlit -= progElement.WhileHighlitDel;

                    if (progElement.OnUnHighlightDel != null)
                        progElement.OnUnHighlightDel();
                }
            }
        }

        public class GroupData : ItemData, INeedsDeviceReset
        {
            public UiSelector selectorGroup;
            public GroupData(Object item, ProgrammingElement param, UiSelector selectorGroup)
                : base(item, param)
            {
                this.selectorGroup = selectorGroup;
            }

            void INeedsDeviceReset.LoadContent(bool immediate)
            {
                INeedsDeviceReset obj = this.item as INeedsDeviceReset;
                BokuGame.Load(obj, immediate);
                BokuGame.Load(this.selectorGroup, immediate);
                base.LoadContent(immediate);
            }

            void INeedsDeviceReset.InitDeviceResources(GraphicsDevice device)
            {
                base.InitDeviceResources(device);
            }

            void INeedsDeviceReset.UnloadContent()
            {
                INeedsDeviceReset obj = this.item as INeedsDeviceReset;
                BokuGame.Unload(obj);
                BokuGame.Unload(this.selectorGroup);
                base.UnloadContent();
            }

            /// <summary>
            /// Recreate render targets
            /// </summary>
            /// <param name="graphics"></param>
            void INeedsDeviceReset.DeviceReset(GraphicsDevice device)
            {
            }

        }

        public int AddItem(Object item)
        {
            return this.AddItem(item, null);
        }

        public int AddItem(Object item, ProgrammingElement param)
        {
            int idx = items.Count;
            items.Add(new ItemData(item, param));
            return idx;
        }

        public int AddGroup(Object item, UiSelector selectorGroup)
        {
            return this.AddGroup(item, null, selectorGroup);
        }

        public int AddGroup(Object item, ProgrammingElement param, UiSelector selectorGroup)
        {
            int idx = items.Count;
            items.Add(new GroupData(item, param, selectorGroup));
            return idx;
        }

        public ItemData this[int index]
        {
            get { return this.items[index]; }
            set { this.items[index] = value; }
        }

        public SelectorLayout Layout
        {
            get { return this.layout; }
        }

        public void RemoveAt(int indexItem)
        {
            this.items.RemoveAt(indexItem);
        }

        public int Count
        {
            get { return this.items.Count; }
        }

        public void Clear()
        {
            this.items.Clear();
        }

        public int IndexDefaultItem
        {
            get { return this.indexDefaultItem; }
            set
            {
                int indexNew = value;

                if (indexNew >= this.items.Count)
                {
                    indexNew = this.items.Count - 1;
                }
                if (indexNew < 0)
                {
                    indexNew = indexCenteredItem;
                }
                if (indexNew != this.indexDefaultItem)
                {
                    this.indexDefaultItem = indexNew;
                }
            }
        }

        public Object ObjectSelectedItem
        {
            get
            {
                ItemData item = this.items[this.indexSelectedItem];
                return item.item;
            }
        }

        public abstract Object Parent
        {
            get;
        }

        public UiSelector ParentSelector
        {
            get { return parentSelector; }
        }

        public Object ParamSelectedItem
        {
            get
            {
                if (this.IndexSelectedItem != -1)
                {
                    ItemData item = this.items[this.indexSelectedItem];
                    return item.progElement;
                }
                else
                {
                    return null;
                }
            }
        }

        public ItemData SelectedItem
        {
            get
            {
                if (indexSelectedItem == -1)
                {
                    return null;
                }
                else
                {
                    return items[indexSelectedItem];
                }
            }
        }

        public List<ItemData> Items
        {
            get { return items; }
        }

        public virtual void OnSelect(Object sender, EventArgs args)
        {
            HideCursor();
            if (this.indexDefaultItem == this.indexSelectedItem ||
                   indexCenteredItem == this.indexSelectedItem)
            {
                if (this.parentSelector != null)
                {
                    this.Deactivate();
                    this.parentSelector.ShowCursor();
                    this.parentSelector = null;
                }
                if (this.Cancel != null)
                {
                    this.Cancel(this);
                }
                
            }
            else
            {
                GroupData groupData = SelectedItem as GroupData;
                if (groupData != null)
                {
                    // group was activated
                    this.activeSubSelector = groupData.selectorGroup;
                    groupData.selectorGroup.parentSelector = this;
                    this.activeSubSelector.Activate();
                    this.activeSubSelector.ShowCursor();
                }
                else
                {
                    if (this.parentSelector != null)
                    {
                        this.Deactivate();
                        this.parentSelector.ShowCursor();
                        this.parentSelector = null;
                    }
                    
                }
                
                // item was activated
                if (this.Select != null)
                {
                    this.Select(this);
                }
            }
        }

        public override void Deactivate()
        {
            if ((IndexSelectedItem >= 0) && (WhileHighlit != null))
            {
                items[IndexSelectedItem].OnUnHighlight(this);
            }
        }

        public virtual void OnCancel(Object sender, EventArgs args)
        {
            HideCursor();
            if (this.parentSelector != null)
            {
                this.Deactivate();
                this.parentSelector.ShowCursor();
                this.parentSelector = null;
            }
            
            if (this.Cancel != null)
            {
                this.Cancel(this);
            }
        }
        public int IndexSelectedItem
        {
            get
            {
                return this.indexSelectedItem;
            }
            set
            {
                int indexNew = value;

                if (indexNew >= this.items.Count)
                {
                    indexNew = this.items.Count - 1;
                }
                if (indexNew < 0)
                {
                    indexNew = indexCenteredItem;
                }
                if (indexNew != this.indexSelectedItem || indexNew == indexCenteredItem)
                {
                    if (indexNew != this.indexSelectedItem)
                    {
                        if (indexSelectedItem >= 0)
                        {
                            /// Unhighlight the old
                            items[indexSelectedItem].OnUnHighlight(this);
                        }
                        if (indexNew >= 0)
                        {
                            /// And highlight the new
                            items[indexNew].OnHighlight(this);
                        }

                        Foley.PlayClick();
                    }

                    IControl controlItem;
                    ITransform transformItem;
                    if (this.indexSelectedItem >= 0 && this.indexSelectedItem < this.items.Count)
                    {
                        ItemData itemData = this.items[this.indexSelectedItem];
                        controlItem = itemData.item as IControl;
                        if (controlItem != null)
                        {
                            controlItem.Hot = false;
                        }
/* Removed to fix issue 
                        // move it back (aka hot)
                        transformItem = itemData.item as ITransform;
                        // remove it if it has a value
                        transformItem.Local.Translation = transformItem.Local.Translation - new Vector3(0.0f, 0.0f, transformItem.Local.Translation.Z);
                        transformItem.Compose();
 */ 
                    }

                    RenderObject selectedItem = null;
                    Object selectedParam = null;
                    if (indexNew != indexCenteredItem)
                    {
                        ItemData itemData = this.items[indexNew];
                        selectedItem = itemData.item as RenderObject;
                        selectedParam = itemData.progElement;
                    }

                    // request the composed item layout and if an object was generated add it to the render list
                    if (this.layout != null && this.layout.ShowPreviewItem && ComposeDefault != null)
                    {
                        int indexCompose;
                        if (indexNew == indexCenteredItem)
                        {
                            indexCompose = this.indexDefaultItem;
                        }
                        else
                        {
                            indexCompose = indexNew;
                        }
                        RenderObject composedObjectNew = ComposeDefault(this, selectedItem, selectedParam, indexCompose);
                        if (composedObjectNew != null)
                        {
                            this.layout.LayoutComposedItem(composedObjectNew);
                        }
                        this.composedObjectPending = composedObjectNew;
                        BokuGame.objectListDirty = true;
                    }

                    if (selectedItem != null)
                    {
                        // for center selection we don't want to effect the orbiting items state
                        controlItem = selectedItem as IControl;
                        if (controlItem != null)
                        {
                            controlItem.Hot = true;
                        }

                        transformItem = selectedItem as ITransform;

                        MoveCursor(transformItem.Local.Translation, indexNew );
/* Removed to fix issue 
                        // move it out (aka hot)
                        transformItem.Local.Translation += new Vector3(0.0f, 0.0f, hotZTranslate);
                        transformItem.Compose();
 */ 
                    }
                    else
                    {
                        MoveCursor(Vector3.Zero, indexNew );
                    }
                    this.indexSelectedItem = indexNew;
                }
            }
        }
        protected abstract void MoveCursor(Vector3 position, int indexNew);
        protected abstract void HideCursor();
        protected abstract void ShowCursor();

        void INeedsDeviceReset.LoadContent(bool immediate)
        {
            for (int i = 0; i < items.Count; i++)
            {
                INeedsDeviceReset o = items[i] as INeedsDeviceReset;
                BokuGame.Load(o, immediate);
            }
        }

        void INeedsDeviceReset.InitDeviceResources(GraphicsDevice device)
        {
        }

        void INeedsDeviceReset.UnloadContent()
        {
            for (int i = 0; i < items.Count; i++)
            {
                INeedsDeviceReset o = items[i] as INeedsDeviceReset;
                BokuGame.Unload(o);
            }
        }

        void INeedsDeviceReset.DeviceReset(GraphicsDevice device)
        {
            for (int i = 0; i < items.Count; i++)
            {
                INeedsDeviceReset o = items[i] as INeedsDeviceReset;
                BokuGame.DeviceReset(o, device);
            }
        }

        public bool RefreshSubSelector(List<UpdateObject> updateList, List<RenderObject> renderList)
        {
            bool result = false;
            if (this.activeSubSelector != null)
            {
                result = this.activeSubSelector.Refresh( updateList, renderList );
                if (result)
                {
                    this.activeSubSelector = null;
                }
            }
            return result;
        }
    }
}
