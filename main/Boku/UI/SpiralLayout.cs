// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.


using System;
using System.Collections;
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
    class SpiralLayout : SelectorLayout
    {
        public override void LayoutComposedItem(Object item)
        {
            ITransform transformItem = item as ITransform;
            // centered
            transformItem.Local.Translation = Vector3.Zero;
            transformItem.Compose();
        }
        public override void LayoutItems(ArrayList items)
        {
            float neededCircumference;

            // calc needed circumference
            //
            CalcLayoutInfoFromItems(out neededCircumference, out this.maxItemRadius, items, 1);

            // calc radius of the spiral
            //
            float radius = neededCircumference / MathHelper.TwoPi;
            float spacingCircumference = 0.0f; // spacing between items on the circumference
            float radiusSpiral = this.maxItemRadius * 2.0f;

            {
                // radius is too small, must increase 
                // this is assuming the center composed item is the same size as the other items
                radius = this.maxItemRadius * 2.0f;
                // and provide the extra spacing between items on the circumference
                float newCircumference = MathHelper.TwoPi * radius;
                spacingCircumference = (newCircumference - neededCircumference) / items.Count;
                neededCircumference = newCircumference;
            }


            // layout items into position spiraling on the circumference
            //
            float headingArcLength = 0.0f;
            for (int indexItem = 0; indexItem < items.Count; indexItem++)
            {
                OrbitalSelector.ItemData itemData = items[indexItem] as OrbitalSelector.ItemData;
                IBounding boundingItem = itemData.item as IBounding;
                ITransform transformItem = itemData.item as ITransform;

                // skip the first one so it is top dead center
                if (indexItem != 0)
                {
                    // update angle along arc
                    headingArcLength += boundingItem.BoundingSphere.Radius;
                }

                // create a vector to the current heading
                float headingAngle = headingArcLength / radius;
                Vector3 heading = new Vector3((float)(Math.Sin(headingAngle) * radiusSpiral), (float)(Math.Cos(headingAngle) * radiusSpiral), 0.0f);

                // set position
                transformItem.Local.Translation = heading;
                transformItem.Compose();

                // update angle along arc
                headingArcLength += boundingItem.BoundingSphere.Radius + spacingCircumference;
                // update the spiral radius
                radiusSpiral += boundingItem.BoundingSphere.Radius / 6.0f;
            }
            this.maxOrbit = radiusSpiral;
        }
    }
}
