// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.


using System;
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

namespace Boku.UI
{
    public abstract class SelectorLayout
    {
        protected ITransform parent;
        protected float maxOrbit;
        protected float maxItemRadius;
        protected float maxInterItemSpacing;

        public Object Parent
        {
            get
            {
                return parent;
            }
            set
            {
                parent = value as ITransform;
            }
        }
        public float MaxOrbit
        {
            get
            {
                return maxOrbit;
            }
        }
        public float MaxItemRadius
        {
            get
            {
                return maxItemRadius;
            }
        }
        public float MaxInterItemSpacing
        {
            get
            {
                return this.maxInterItemSpacing;
            }
        }
        public virtual bool ShowPreviewItem
        {
            get
            {
                return true;
            }
        }

        public abstract void LayoutComposedItem(Object item);
        public abstract void LayoutItems(List<UiSelector.ItemData> items);
        protected void CalcLayoutInfoFromItems(out float circumference, out float maxRadius, List<UiSelector.ItemData> items, int rings)
        {
            circumference = 0.0f;
            UiSelector.ItemData itemData;
            // if an odd count and even number of rings, impose an empty spot
            if ((rings %2) == 0 && (items.Count % 2) == 1)
            {
                itemData = items[0];
                IBounding boundingItem = itemData.item as IBounding;
                circumference += boundingItem.BoundingSphere.Radius * 2.0f / (float)rings;
            }
            maxRadius = float.NegativeInfinity;
            for (int indexItem = 0; indexItem < items.Count; indexItem++)
            {
                itemData = items[indexItem];
                IBounding boundingItem = itemData.item as IBounding;
                circumference += boundingItem.BoundingSphere.Radius * 2.0f / (float)rings;
                maxRadius = MathHelper.Max(maxRadius, boundingItem.BoundingSphere.Radius);
            }
        }
    }

}
