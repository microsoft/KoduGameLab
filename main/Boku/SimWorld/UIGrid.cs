// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.


using System;
using System.Collections;

using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Audio;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using Microsoft.Xna.Framework.Storage;

using Boku.Base;
using Boku.Common;
using Boku.Input;

namespace Boku.SimWorld
{
    public delegate void UIGridEvent(UIGrid grid);

    /// <summary>
    /// A grid layout UI menu.  This can be set up as a 2d grid or as
    /// an either vertical or horizontal 1d list.  The c'tor takes a 
    /// Point type which defines the maximum allowable dimensions for
    /// the grid.  The grid may actually be any size up to this limit.
    /// This allows minor resizing of the grid without having to always
    /// throw it all away and start over.
    /// </summary>
    public class UIGrid
    {
        public event UIGridEvent Select = null;
        public event UIGridEvent Cancel = null;

        private Point maxDimensions;
        private Point actualDimensions;
        private UIGridElement[ , ] grid = null;

        // Local transform for this grid.
        private Matrix localMatrix = Matrix.Identity;
        // Local combined with parent.
        private Matrix worldMatrix = Matrix.Identity;

        private bool active = false;

        private Point focusIndex;   // Which element currently has focus.

        private bool dirty = true;  // Has something been added, moved, etc?

        private bool scrolling = false;     // If this is false, the elements have a static position
                                            // on the screen.  If this is true then the current focus
                                            // element is centered and the whole grid is scrolled as
                                            // the selection changes.
        private Vector3 scrollOffset = new Vector3();   // Offset caused by scrolling.

        private Vector2 spacing = new Vector2(1.0f);    // Center to center spacing of grid elements.

        private CommandMap commandMap = null;

        #region Accessors
        /// <summary>
        /// Grids are by default set up inactive.  Setting active to true causes the
        /// grid to push itself onto the input focus stack and start rendering itself.
        /// If a second Grid is activated while the first is still active then both
        /// will render but only the second will have focus until it is deactivated.
        /// At that point focus will return to the first grid.  This allows grids to 
        /// be "layered".
        /// </summary>
        public bool Active
        {
            get { return active; }
            set
            {
                if (active != value)
                {
                    active = value;
                    if (active)
                    {
                        CommandStack.Push(commandMap);
                    }
                    else
                    {
                        CommandStack.Pop(commandMap);
                    }
                }
            }
        }
        public Matrix LocalMatrix
        {
            get { return localMatrix; }
            set { localMatrix = value; }
        }
        public Vector2 Spacing
        {
            get { return spacing; }
            set { spacing = value; }
        }
        /// <summary>
        /// If this is false, the elements have a static position
        /// on the screen.  If this is true then the current focus
        /// element is centered and the whole grid is scrolled as
        /// the selection changes.
        /// </summary>
        public bool Scrolling
        {
            get { return scrolling; }
            set { scrolling = value; scrollOffset = new Vector3(); dirty = true; }
        }
        public Point SelectionIndex
        {
            get { return focusIndex; }
        }
        public UIGridElement SelectionElement
        {
            get { return grid[ focusIndex.X, focusIndex.Y ]; }
        }
        #endregion

        // c'tor
        public UIGrid(UIGridEvent onSelect, UIGridEvent onCancel, Point maxDimensions)
        {
            this.Select = onSelect;
            this.Cancel = onCancel;
            this.maxDimensions = maxDimensions;
            grid = new UIGridElement[ maxDimensions.X, maxDimensions.Y ];

            // Init to null.
            for (int j = 0; j < maxDimensions.Y; j++)
            {
                for (int i = 0; i < maxDimensions.X; i++)
                {
                    grid[ i, j ] = null;
                }
            }
            actualDimensions = new Point(0, 0);
            focusIndex = new Point(0, 0);       // TODO need to decide how to set this if not already set.  Should never point
                                                // to a null element?  Does this mean we can't have a row of diagonal elements?
                                                // Why bother about supporting something that's stupid, just limit it.

            this.commandMap = CommandMap.Deserialize(this, "UiGridControl.xml");
      
        }   // end of UIGrid c'tor

        //
        //  CommandMap event handlers.
        //
        public void MoveUp()
        {
            if (focusIndex.Y > 0)
            {
                --focusIndex.Y;
            }
            else
            {
                // TODO Bzzt sound.  Trying to move to an invalid location.
            }

            Scroll();
        }

        public void MoveDown()
        {
            if (focusIndex.Y < actualDimensions.Y - 1)
            {
                ++focusIndex.Y;
            }
            else
            {
                // TODO Bzzt sound.
            }

            Scroll();
        }
        
        public void MoveRight()
        {
            if (focusIndex.X < actualDimensions.X - 1)
            {
                ++focusIndex.X;
            }
            else
            {
                // TODO Bzzt sound.
            }

            Scroll();
        }

        public void MoveLeft()
        {
            if (focusIndex.X > 0)
            {
                --focusIndex.X;
            }
            else
            {
                // TODO Bzzt sound.  Trying to move to an invalid location.
            }

            Scroll();
        }

        public void OnSelect()
        {
            Select(this);
        }

        public void OnCancel()
        {
            Cancel(this);
        }

        /// <summary>
        /// This is called when the focus has moved to scroll the grid.
        /// </summary>
        private void Scroll()
        {
            if (scrolling)
            {
                // Without a twitch...
                //scrollOffset = grid[ focusIndex.X, focusIndex.Y ].Position;

                // Create a twitch to scroll the grid.
                TwitchManager.GetVector3 get = delegate(Object param) { return scrollOffset; };
                TwitchManager.SetVector3 set = delegate(Vector3 value, Object param) { scrollOffset = value; };
                TwitchManager.Vector3Twitch twitch = new TwitchManager.Vector3Twitch(get, set, grid[focusIndex.X, focusIndex.Y].Position, 0.15f, TwitchCurve.Shape.Linear, null);
                twitch.Start();
            }
        }

        public void Update(Camera camera, ref Matrix parentMatrix)
        {

            // Update grid state
            if (parentMatrix != null)
            {
                worldMatrix = localMatrix * parentMatrix;
                worldMatrix.Translation -= scrollOffset;
            }

            if (dirty)
            {
                Refresh();
                dirty = false;
            }

            // Call update on each child object.
            for (int j = 0; j < maxDimensions.Y; j++)
            {
                for (int i = 0; i < maxDimensions.X; i++)
                {
                    if (grid[ i, j ] != null)
                    {
                        grid[ i, j ].Update(ref worldMatrix);
                    }
                }
            }

        }   // end of UIGrid Update()

        public void Render(Camera camera)
        {
            if (active)
            {
                // Render each child object.
                for (int j = 0; j < maxDimensions.Y; j++)
                {
                    for (int i = 0; i < maxDimensions.X; i++)
                    {
                        if (grid[ i, j ] != null)
                        {
                            if (focusIndex == new Point(i, j))
                            {
                                // Render as focus object.
                                grid[ i, j ].Fade = 1.0f;
                                grid[ i, j ].Render(camera);
                            }
                            else
                            {
                                // Render as non-focus object.
                                grid[ i, j ].Fade = 0.5f;
                                grid[ i, j ].Render(camera);
                            }
                        }
                    }
                }
            
            }   // end of if active.

        }   // end of UIGrid Render()

        /// <summary>
        /// Needs to be called whenever something has changed in the grid
        /// to recalculate the sizes/positions, etc.
        /// </summary>
        private void Refresh()
        {
            Vector2 upperLeftPosition = new Vector2(-(actualDimensions.X - 1.0f) / 2.0f * spacing.X, (actualDimensions.Y - 1.0f) / 2.0f * spacing.Y);

            // Set position of each child.
            for (int j = 0; j < maxDimensions.Y; j++)
            {
                for (int i = 0; i < maxDimensions.X; i++)
                {
                    if (grid[ i, j ] != null)
                    {
                        grid[ i, j ].Position = new Vector3(upperLeftPosition + spacing * new Vector2(i, -j), 0.0f);
                    }
                }
            }

            if (scrolling)
            {
                scrollOffset = grid[ focusIndex.X, focusIndex.Y ].Position;
            }
        }   // end of UIGrid Refresh()

        /// <summary>
        /// Adds a new element to the grid.
        /// </summary>
        /// <param name="element">the new element to add</param>
        /// <param name="i">column for new element</param>
        /// <param name="j">row for new element</param>
        public void Add(UIGridElement element, int i, int j)
        {
            if (i >= maxDimensions.X || j >= maxDimensions.Y)
            {
                throw new Exception(@"Trying to add an element at an index location outside of the grid.");
            }
            grid[ i, j ] = element;
            actualDimensions.X = Math.Max(actualDimensions.X, i + 1);
            actualDimensions.Y = Math.Max(actualDimensions.Y, j + 1);
            dirty = true;
        }   // end of UIGrid Add()

    }   // end of class UIGrid

}   // end of namespace Boku.SimWorld


