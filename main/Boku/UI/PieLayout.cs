
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
    public class PieLayout : SelectorLayout
    {
        public override void LayoutComposedItem(Object item)
        {
            ITransform transfromItem = item as ITransform;
            // centered
            transfromItem.Local.Translation = Vector3.Zero;
            transfromItem.Compose();
        }
        public override void LayoutItems(ArrayList items)
        {
            float neededCircumference;

            // calc needed circumference
            //
            CalcLayoutInfoFromItems(out neededCircumference, out this.maxItemRadius, items, 1);
            
            // calc radius of the pie
            //
            float radius = neededCircumference / MathHelper.TwoPi;
            float spacingCircumference = 0.0f; // spacing between items on the circumference

            if (radius < this.maxItemRadius * 2.0f)
            {
                // radius is too small, must increase 
                // this is assuming the center composed item is the same size as the other items
                radius = this.maxItemRadius * 2.0f;
                // and provide the extra spacing between items on the circumference
                float newCircumference = MathHelper.TwoPi * radius;
                spacingCircumference = (newCircumference - neededCircumference) / items.Count;
                neededCircumference = newCircumference;
            }

            this.maxOrbit = radius;
            // layout items into position on the circumference
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
                Vector3 heading = new Vector3((float)(Math.Sin(headingAngle) * radius), (float)(Math.Cos(headingAngle) * radius), 0.0f);

                // set position by animations
                // transformItem.Local = Matrix.CreateTranslation(heading);
                TwitchManager.GetVector3 get = delegate(Object param)
                        {
                            return Vector3.Zero;
                        };
                TwitchManager.SetVector3 set = delegate(Vector3 value, Object param)
                        {
                            ITransform transformObject = param as ITransform;
                            if (transformObject != null)
                            {
                                transformObject.Local.Translation = value;
                                transformObject.Compose();
                            }
                        };
                TwitchManager.Vector3Twitch twitch = new TwitchManager.Vector3Twitch(
                        get,
                        set,
                        heading,
                        0.2f,
                        TwitchCurve.Shape.EaseOut,
                        transformItem);
                twitch.Play();

                // update angle along arc
                headingArcLength += boundingItem.BoundingSphere.Radius + spacingCircumference;
            }
        }
    }

    public class StaggeredPieLayout : SelectorLayout
    {
        public override void LayoutComposedItem(Object item)
        {
            ITransform transfromItem = item as ITransform;
            // centered
            transfromItem.Local.Translation = Vector3.Zero;
            transfromItem.Compose();
        }
        public override void LayoutItems(ArrayList items)
        {
            float neededCircumference;

            // calc needed circumference
            //
            CalcLayoutInfoFromItems(out neededCircumference, out this.maxItemRadius, items, 1);
            
            // calc radius of the pie
            //
            float radius = neededCircumference / MathHelper.TwoPi;
            float spacingCircumference = 0.0f; // spacing between items on the circumference
            bool staggerOddItems = false;

            if (radius < this.maxItemRadius * 2.0f)
            {
                // so few items they are one row spaced around

                // radius is too small, must increase 
                // this is assuming the center composed item is the same size as the other items
                radius = this.maxItemRadius * 2.0f;
                // and provide the extra spacing between items on the circumference
                float newCircumference = MathHelper.TwoPi * radius;
                spacingCircumference = (newCircumference - neededCircumference) / items.Count;
                neededCircumference = newCircumference;
            }
            else if (radius > this.maxItemRadius * 4.0f)
            {
                staggerOddItems = true;
                CalcLayoutInfoFromItems(out neededCircumference, out this.maxItemRadius, items, 2);
                radius = neededCircumference / MathHelper.TwoPi;
            }

            this.maxOrbit = radius;
            if (staggerOddItems)
            {
                this.maxOrbit += this.maxItemRadius * 2.0f;
            }
            // layout items into position on the circumference
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
                float itemRadius = radius;
                if (staggerOddItems && (indexItem % 2) == 1)
                {
                    itemRadius = this.maxOrbit;
                }
                Vector3 heading = new Vector3((float)(Math.Sin(headingAngle) * itemRadius), (float)(Math.Cos(headingAngle) * itemRadius), 0.0f);

                // set position by animations
                // transformItem.Local = Matrix.CreateTranslation(heading);
                TwitchManager.GetVector3 get = delegate(Object param)
                        {
                            return Vector3.Zero;
                        };
                TwitchManager.SetVector3 set = delegate(Vector3 value, Object param)
                        {
                            ITransform transformObject = param as ITransform;
                            if (transformObject != null)
                            {
                                transformObject.Local.Translation = value;
                                transformObject.Compose();
                            }
                        };
                TwitchManager.Vector3Twitch twitch = new TwitchManager.Vector3Twitch(
                        get,
                        set,
                        heading,
                        0.2f,
                        TwitchCurve.Shape.EaseOut,
                        transformItem);
                twitch.Play();

                if (!staggerOddItems)
                {
                    // update angle along arc
                    headingArcLength += boundingItem.BoundingSphere.Radius + spacingCircumference;
                }
            }
        }
    }
    public class GridLayout : SelectorLayout
    {
        public override bool ShowPreviewItem
        {
            get
            {
                return false;
            }
        }
        public override void LayoutComposedItem(Object item)
        {
            Debug.Assert(false, "Composed Item Not Supported with GridLayout");
        }
        protected void CalcLayoutInfoFromItems(out float maxRadius, out int rows, out int cols, ArrayList items)
        {
            maxRadius = 0.0f;
            OrbitalSelector.ItemData itemData;
            int itemsInView = items.Count;
            rows = (int)(Math.Sqrt((double)itemsInView));
            // if its even but not 2 rows
            if ((rows % 2) == 0 && rows != 2)
            {
                // even, odd it up
                rows++;
            }
            cols = itemsInView / rows;
            for (int indexItem = 0; indexItem < items.Count; indexItem++)
            {
                itemData = items[indexItem] as OrbitalSelector.ItemData;
                IBounding boundingItem = itemData.item as IBounding;
                maxRadius = MathHelper.Max(maxRadius, boundingItem.BoundingSphere.Radius);
            }
        }
        public override void LayoutItems(ArrayList items)
        {
            float neededCircumference;
            int rows;
            int cols;
            // calc needed circumference
            //
            CalcLayoutInfoFromItems( out this.maxItemRadius, out rows, out cols, items);

            float itemWidth = this.maxItemRadius * 2.0f * (float)Math.Sin( Math.PI * 0.25 ) + 0.1f;
            this.maxOrbit = this.maxItemRadius * (float)rows / 2.0f;
            this.maxInterItemSpacing = this.maxItemRadius * 2.0f;
            for (int indexItem = 0; indexItem < items.Count; indexItem++)
            {
                OrbitalSelector.ItemData itemData = items[indexItem] as OrbitalSelector.ItemData;
                IBounding boundingItem = itemData.item as IBounding;
                ITransform transformItem = itemData.item as ITransform;

                int col = (indexItem % cols) - (cols / 2);
                int row = (indexItem / cols) - (rows / 2);

                Vector3 position = new Vector3((float)col * itemWidth, (float)row * -itemWidth, 0.0f);
                Debug.Print("[{0}][{1}] ({2,8:F},{3,8:F})", col, row, position.X, position.Y);
                this.maxOrbit = MathHelper.Max( this.maxOrbit, Math.Abs(position.X) );

                // set position by animations
                // transformItem.Local = Matrix.CreateTranslation(heading);
                TwitchManager.GetVector3 get = delegate(Object param)
                        {
                            return Vector3.Zero;
                        };
                TwitchManager.SetVector3 set = delegate(Vector3 value, Object param)
                        {
                            ITransform transformObject = param as ITransform;
                            if (transformObject != null)
                            {
                                transformObject.Local.Translation = value;
                                transformObject.Compose();
                            }
                        };
                TwitchManager.Vector3Twitch twitch = new TwitchManager.Vector3Twitch(
                        get,
                        set,
                        position,
                        0.2f,
                        TwitchCurve.Shape.EaseOut,
                        transformItem);
                twitch.Play();
            }
            
/*
            // layout items into position on a grid starting top left
            //
            int ring = 1;
            int cols = 3; // first ring is 3x3
            int colEnd = (cols / 2);
            int col = -(colEnd);
            int row = -(colEnd);
            int colInc = 1;
            int rowInc = 0;

            float itemWidth = this.maxItemRadius * 2.0f;

            for (int indexItem = 0; indexItem < items.Count; indexItem++)
            {
                OrbitalSelector.ItemData itemData = items[indexItem] as OrbitalSelector.ItemData;
                IBounding boundingItem = itemData.item as IBounding;
                ITransform transformItem = itemData.item as ITransform;


                Vector3 position = new Vector3((float)col * itemWidth, (float)row * -itemWidth, 0.0f);
                Debug.Print("[{0}][{1}] ({2,8:F},{3,8:F})", col, row, position.X, position.Y);

                col += colInc;
                row += rowInc;
                // reached right
                if (col > colEnd)
                {
                    col = colEnd;
                    colInc = 0;
                    rowInc = 1;
                    row += rowInc;
                }
                // reached bottom
                if (row > colEnd)
                {
                    row = colEnd;
                    colInc = -1;
                    rowInc = 0;
                    col += colInc;
                }
                // reached left
                if (col < -colEnd)
                {
                    col = -colEnd;
                    colInc = 0;
                    rowInc = -1;
                    row += rowInc;
                }
                // reached top
                if (col == -colEnd && row == -colEnd)
                {
                    cols += 2;
                    colEnd = (cols / 2);
                    col = -(colEnd);
                    row = -(colEnd);
                    colInc = 1;
                    rowInc = 0;
                }

                // set position by animations
                // transformItem.Local = Matrix.CreateTranslation(heading);
                TwitchManager.GetVector3 get = delegate(Object param)
                        {
                            return Vector3.Zero;
                        };
                TwitchManager.SetVector3 set = delegate(Vector3 value, Object param)
                        {
                            ITransform transformObject = param as ITransform;
                            if (transformObject != null)
                            {
                                transformObject.Local.Translation = value;
                                transformObject.Compose();
                            }
                        };
                TwitchManager.Vector3Twitch twitch = new TwitchManager.Vector3Twitch(
                        get,
                        set,
                        position,
                        0.2f,
                        TwitchCurve.Shape.EaseOut,
                        transformItem);
                twitch.Play();
            }
            this.maxOrbit = colEnd * itemWidth;
 */ 
        }
    }
}
