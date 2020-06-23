//#define LOG
using System;
using System.Collections.Generic;
using System.Diagnostics;

using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

using Boku.Base;
using Boku.Common;
using Boku.Audio;

namespace Boku.UI2D
{
    /// <summary>
    /// Specialization of the standard UIGrid for the load level menu.  We need to have a custom
    /// Refresh() method to handle the rotation and scaling of the tiles and we also need a 
    /// custom Render() method to render the tiles from the outside-in so that the drop
    /// shadows layer correctly.
    /// </summary>
    public class LoadLevelMenuUIGrid : UIGrid
    {
        /// <summary>
        /// The number of tiles in the menu.
        /// </summary>
        public const int kWidth = 18;

        /// <summary>
        /// The index of the front-most tile (for proper z-ordering when drawing)
        /// </summary>
        public const int kFront = 8;

        #region Members

        private Orientation[] baseOrients;
        /// <summary>

        /// The current orientation array is used during dragging to store the intermediate positions
        /// of each element as they move under the user's touch. Once the touch is released, the
        /// elements return to their base orientations.
        /// </summary>
        private Orientation[] currentOrients;
        
        private UIGridElement.ParamBlob blob;

        /// <summary>
        /// This is the amount of distance in the grid coordinate space that each element has from its neighbors.
        /// </summary>
        private float ELEMENT_SPACING_X = 1.5f;

        #endregion

        #region Accessors

        public UIGridLevelElement this[int index]
        {
            get { return Get(index); }
        }

        public LevelMetadata CurrentLevel
        {
            get
            {
                UIGridLevelElement e = Get(kFront);
                if (e != null)
                    return e.Level;
                return null;
            }
        }

        public Guid CurrentLevelId
        {
            get
            {
                if (CurrentLevel != null)
                    return CurrentLevel.WorldId;
                return Guid.Empty;
            }
        }

        /// <summary>
        /// True when no valid levels exist in the grid.
        /// </summary>
        public bool NoValidLevels
        {
            get
            {
                bool result = true;
                
                for (int i = 0; i < kWidth; i++)
                {
                    UIGridLevelElement e = Get(i);
                    if (e != null && e.Level != null)
                    {
                        result = false;
                        break;
                    }
                }

                return result;
            }
        }

        #endregion

        #region Public

        public LoadLevelMenuUIGrid(
            UIGridEvent onSelect,
            UIGridEvent onCancel,
            UIGridEvent onMoveLeft,
            UIGridEvent onMoveRight,
            string uiMode)
            : base(onSelect, onCancel, new Point(kWidth, 1), uiMode)
        {
            MovedLeft += onMoveLeft;
            MovedRight += onMoveRight;

            RenderEndsIn = true;        // In order to get the shadows layering correctly.
            IgnoreFocusChanged = true;  // No clicks on focus change.

            // Set up the blob for info common to all tiles.
            blob = new UIGridElement.ParamBlob();
            blob.width = 2.0f;
            blob.height = 2.0f;
            blob.edgeSize = 0.2f;
            blob.selectedColor = Color.Black;
            blob.unselectedColor = Color.Black;
            blob.Font = UI2D.Shared.GetGameFont15_75;
            blob.textColor = Color.White;
            blob.dropShadowColor = Color.Transparent;
            blob.useDropShadow = true;
            blob.invertDropShadow = false;
            blob.normalMapName = null;

            baseOrients = new Orientation[kWidth] {
                new Orientation(new Vector3(-8 * ELEMENT_SPACING_X, 0.0f, 0.0f), 0.2f, 0.36f),
                new Orientation(new Vector3(-7 * ELEMENT_SPACING_X, 0.0f, 0.0f), 0.3f, 0.32f),
                new Orientation(new Vector3(-6 * ELEMENT_SPACING_X, 0.0f, 0.0f), 0.4f, 0.28f),
                new Orientation(new Vector3(-5 * ELEMENT_SPACING_X, 0.0f, 0.0f), 0.5f, 0.24f),
                new Orientation(new Vector3(-4 * ELEMENT_SPACING_X, 0.0f, 0.0f), 0.6f, 0.2f),
                new Orientation(new Vector3(-3 * ELEMENT_SPACING_X, 0.0f, 0.0f), 0.7f, 0.16f),
                new Orientation(new Vector3(-2 * ELEMENT_SPACING_X, -0.2f, 0.0f), 0.75f, 0.14f),
                new Orientation(new Vector3(-1 * ELEMENT_SPACING_X, -0.18f, 0.0f), 0.79f, 0.08f),
                new Orientation(new Vector3(0, 0.0f, 0.0f), 1.2f, 0.0f),
                new Orientation(new Vector3(1 * ELEMENT_SPACING_X, 0.1f, 0.0f), 0.79f, -0.1f),
                new Orientation(new Vector3(2 * ELEMENT_SPACING_X, 0.25f, 0.0f), 0.7f, -0.15f),
                new Orientation(new Vector3(3 * ELEMENT_SPACING_X, 0.4f, 0.0f), 0.63f, -0.19f),
                new Orientation(new Vector3(4 * ELEMENT_SPACING_X, 0.5f, 0.0f), 0.58f, -0.25f),
                new Orientation(new Vector3(5 * ELEMENT_SPACING_X, 0.47f, 0.0f), 0.52f, -0.3f),
                new Orientation(new Vector3(6 * ELEMENT_SPACING_X, 0.4f, 0.0f), 0.47f, -0.35f),
                new Orientation(new Vector3(7 * ELEMENT_SPACING_X, 0.4f, 0.0f), 0.42f, -0.4f),
                new Orientation(new Vector3(8 * ELEMENT_SPACING_X, 0.4f, 0.0f), 0.35f, -0.45f),
                new Orientation(new Vector3(9 * ELEMENT_SPACING_X, 0.4f, 0.0f), 0.3f, -0.5f),
            };

            currentOrients = new Orientation[kWidth];
            for (int i = 0; i < kWidth; i++)
            {
                currentOrients[i] = new Orientation(baseOrients[i]);
            }
        }

        public override void Clear()
        {
            for (int i = 0; i < kWidth; i++)
            {
                currentOrients[i] = new Orientation(baseOrients[i]);
            }
            base.Clear();
        }

        /// <summary>
        /// Loads the correct "level" data to each of the grid elements,
        /// </summary>
        /// <param name="cursor"></param>
        public void Reload(ILevelSetCursor cursor)
        {
            Log("Begin Reload");

            for (int i = 0; i < kWidth; ++i)
            {
                LevelMetadata level = cursor[i - kFront];

                UIGridLevelElement existing = Get(i);

                // If they already match then we're good.
                if (existing != null && existing.Level == level)
                    continue;

                // If there's no element and no level, also good.
                if (existing == null && level == null)
                    continue;

                if (level != null)
                {
                    // Need to create a new element
                    if (existing == null)
                    {
                        existing = CreateElement(level);
                        Add(existing, i, 0);
                        existing.SetOrientation(baseOrients[i]);
                    }
                    existing.Level = level;
                }
                else
                {
                    // No level, so set element's ref to null.
                    existing.Level = null;
                }
            }
            
            Log("End Reload");
        }

        public UIGridLevelElement Get(int index)
        {
            return (UIGridLevelElement)grid[index, 0];
        }

        public UIGridLevelElement CreateElement(LevelMetadata level)
        {
            UIGridLevelElement tile = new UIGridLevelElement(blob);

            BokuGame.Load(tile, true);

            if (level == null)
                return tile;

            tile.Level = level;

            return tile;
        }

        public void Shift(int amount)
        {
            while (amount != 0)
            {
                if (amount > 0)
                {
                    // Cursor goes right, grid elements go left.
                    UIGridLevelElement e = Get(0);
                    for (int i = 0; i < kWidth - 1; i++)
                    {
                        Add(Get(i + 1), i, 0);
                    }
                    Add(e, kWidth - 1, 0);
                    if (e != null)
                    {
                        e.Level = null;
                        e.Dirty = true;
                        e.SetOrientation(baseOrients[kWidth - 1]);
                        Foley.PlayShuffle();
                    }
                    --amount;
                }
                else
                {
                    // Cursor goes left, grid elements go right.
                    UIGridLevelElement e = Get(kWidth - 1);
                    for (int i = kWidth - 1; i > 0; i--)
                    {
                        Add(Get(i - 1), i, 0);
                    }
                    Add(e, 0, 0);
                    if (e != null)
                    {
                        e.Level = null;
                        e.Dirty = true;
                        e.SetOrientation(baseOrients[0]);
                        Foley.PlayShuffle();
                    }
                    ++amount;
                }

                if (!NoValidLevels)
                {
                    Foley.PlayShuffle();
                }
            }

        }   // end of Shift()

        public void SplitAt(int index)
        {
            // Not really needed since Reload() takes care of everything.
            return;

            /*
             
            index += kFront;
              
            if (index >= 0 && index < kWidth)
            {
                if (index >= kFront)
                {
                    //ShiftRight(index, 1, true);
                    UIGridLevelElement e = Get(kWidth - 1);
                    for (int i = kWidth - 1; i > index; i--)
                    {
                        // We use Add() instead of setting the position directly 
                        // so that ActualDimensions is kept valid.
                        Add(grid[i - 1, 0], i, 0);
                    }
                    grid[index, 0] = e;
                    if (e != null)
                    {
                        e.SetOrientation(orient[index]);
                        e.Level = null;
                    }
                }
                else
                {
                    //ShiftLeft(index, 1, true);
                    UIGridLevelElement e = Get(0);
                    for (int i = 0; i < index - 1; i++)
                    {
                        // We use Add() instead of setting the position directly 
                        // so that ActualDimensions is kept valid.
                        Add(grid[i + 1, 0], i, 0);
                    }
                    grid[index, 0] = e;
                    if (e != null)
                    {
                        e.SetOrientation(orient[index]);
                        e.Level = null;
                    }
                }
            }
            */

        }   // end of SplitAt()

        /// <summary>
        /// Removes the element at index, collapses the rest of the list, 
        /// and then adds the removed element back to the end of the list.
        /// </summary>
        /// <param name="index"></param>
        public void Remove(int index)
        {
            index += kFront;

            if (index >= 0 && index < ActualDimensions.X)
            {
                UIGridLevelElement e = Get(index);
                for (int i = index; i < kWidth - 1; i++)
                {
                    Add(grid[i + 1, 0], i, 0);
                }
                Add(e, kWidth - 1, 0);
                if (e != null)
                {
                    e.Level = null;
                    e.Dirty = true;
                    e.SetOrientation(baseOrients[kWidth - 1]);
                }
            }
        }

        #endregion // Public

        #region Private

        private void Log(string str)
        {
#if LOG
            Debug.WriteLine(str);
#endif
        }

        private void Unload(int index)
        {
            UIGridLevelElement e = (UIGridLevelElement)grid[index, 0];
            if (e != null)
            {
                Unload(e);
                grid[index, 0] = null;
            }
        }

        private void Unload(UIGridLevelElement e)
        {
            if (e != null)
            {
                e.UnloadInstanceContent();
            }
        }

        #endregion

        #region Internal

        public override void Refresh()
        {
            //base.Refresh();
        }   // end of Refresh()

        public override void Update(ref Matrix parentMatrix)
        {
            focusIndex.X = kFront;
            focusIndex.Y = 0;

            UIGridLevelElement e = Get(kFront);

            if (e != null)
            {
                e.Selected = true;
            }

            // Update orientations.
            if (!isDragging)
            {
                //if (!hasResidualVelocity)
                //{
                //    // Settle a dragged grid back into fixed positions.
                //    SettleGrid();
                //}
                //else
                //{
                if (hasResidualVelocity)
                {
                    // Residual velocity still affecting the grid.
                    UpdateResidualVelocity();
                    for (int i = 0; i < kWidth; i++)
                    {
                        e = Get(i);
                        if (e != null)
                        {
                            e.SetOrientation(currentOrients[i]);
                        }
                    }
                }
                else
                {
                    hasResidualVelocity = false;
                    residualVelocity = 0.0f;

                    // Settle elements back into base orientation.
                    for (int i = 0; i < kWidth; i++)
                    {
                        e = Get(i);
                        if (e != null)
                        {
                            e.TwitchOrientation(baseOrients[i]);
                        }
                    }
                }
            }
            else
            {
                for (int i = 0; i < kWidth; i++)
                {
                    e = Get(i);
                    if (e != null)
                    {
                        e.SetOrientation(currentOrients[i]);
                    }
                }
            }

            base.Update(ref parentMatrix);
        }   // end of Update();

        /*
        public void test()
        {
            //foo("pre test");
            for (int i = 0; i < kWidth-1; i++)
            {
                UIGridLevelElement ei = (UIGridLevelElement)grid[i, 0];
                for (int j = i + 1; j < kWidth; j++)
                {
                    UIGridLevelElement ej = (UIGridLevelElement)grid[j, 0];

                    if (ei != null && ej != null && ei.uniqueNum == ej.uniqueNum)
                    {
                        // boom!
                    }
                }
            }
        }

        public void foo(string label)
        {
            Debug.Print("===== " + label + " =====");
            for (int i = 0; i < kWidth; i++)
            {
                UIGridLevelElement e = (UIGridLevelElement)grid[i, 0];
                if (e == null)
                {
                    Debug.Print(i.ToString() + " null");
                }
                else
                {
                    Debug.Print(i.ToString() + " " + e.uniqueNum.ToString() + " " + e.Level.Name);
                }
            }
        }
        */

        public override void UnloadContent()
        {
            for (int i = 0; i < kWidth; ++i)
            {
                UIGridLevelElement e = (UIGridLevelElement)grid[i, 0];

                if (e != null)
                {
                    BokuGame.Unload(e); // Remove from the INeedsDeviceReset Dictionary.
                    e.UnloadContent();
                }

                grid[i, 0] = null;
            }

            base.UnloadContent();
        }

        public void UnloadInstanceContent()
        {
            for (int i = 0; i < kWidth; ++i)
            {
                Unload(i);
            }
        }

        private void UpdateResidualVelocity()
        {
            float minDelta = 0.0001f;

            if (!SlideByX(residualVelocity) ||
                (Math.Abs(residualVelocity) < minDelta))
            {
                residualVelocity = 0.0f;
                hasResidualVelocity = false;
                return;
            }
        }

        /// <summary>
        /// Performs input handling for the Load Level UI grid in lieu of default grid drag.
        /// </summary>
        /// <param name="camera"></param>
        public override void HandleTouchInput(Camera camera)
        {
            TouchContact touch = TouchInput.GetOldestTouch();
            if (touch == null) { return; }

            if (touch.phase == TouchPhase.Ended)
            {
                isDragging = false;
                return;
            }
            else if (!isDragging)
            {
                // This touch hasn't yet been classified as a drag yet.. so we
                // will see if it meets the criteria.
                float minDiff = MIN_DRAG_START;
                float diff = (touch.position - touch.startPosition).Length();
                if (diff < minDiff)
                {
                    return;
                }
                else
                {
                    isDragging = true;
                }
            }

            // Get local coordinates difference between this touch position and the previous
            Matrix localInvMatrix = Matrix.Invert(LocalMatrix);
            Vector2 currentLocalPos = TouchInput.GetLocalXYFromScreenCoords(
                touch.position,
                camera,
                ref localInvMatrix);
            Vector2 previousLocalPos = TouchInput.GetLocalXYFromScreenCoords(
                touch.previousPosition,
                camera,
                ref localInvMatrix);
            float localXMove = (currentLocalPos - previousLocalPos).X;

            if (SlideByX(localXMove))
            {
                // We did a successful move, so capture the momentum of the touch in pixels/frame
                float duration = 1.0f;

                if (Math.Abs(localXMove) > 0.2f)
                {
                    residualVelocity = localXMove / 4.0f;
                    hasResidualVelocity = true;
                    TwitchManager.Set<float> set = velocityDelegate;
                    TwitchManager.CreateTwitch<float>(residualVelocity, 0, set, duration, 
                        TwitchCurve.Shape.EaseOut);
                }
                else if (touch.phase == TouchPhase.Stationary)
                {
                    // If the user has stopped moving but hasn't ended his touch, we will put
                    // the brakes on any residual velocity, as the user probably wants to settle
                    // here.
                    residualVelocity = 0.0f;
                    hasResidualVelocity = false;
                }
            }
        }

        private bool SlideByX(float xMove)
        {
            if (Math.Abs(xMove) >= ELEMENT_SPACING_X)
            {
                // Instant jumps of greater than a single tab stop are not needed, nor supported atm.
                return false;
            }
            float newSelectionXPos = currentOrients[SelectionIndex.X].position.X + xMove;

            // Before performing the movement, we need to find out if the movement causes a transition
            // either left or right. First, compute which tab stop we are currently at, and which one
            // we will be after this move.
            // We goto the new tab when we're closer to the next element than we are to our current element.
            float currentTab = (currentOrients[SelectionIndex.X].position.X / ELEMENT_SPACING_X);
            currentTab += (currentTab >= 0) ? 0.5f : -0.5f;
            float newTab = (newSelectionXPos / ELEMENT_SPACING_X);
            newTab += (newTab >= 0) ? 0.5f : -0.5f;
            int tabDiff = (int)newTab - (int)currentTab;

            Debug.Assert(Math.Abs(tabDiff) <= 1);
            if (tabDiff == 1)
            {
                UIGridLevelElement leftElement = Get(SelectionIndex.X - 1);
                if ((leftElement == null) || (leftElement.Level == null))
                {
                    // Nothing to go to.
                    return false;
                }
                MoveLeft();
                // The result of the left move will cause all the grid elements to "Shift()" to the left by 1.
                // Our orientations must move accordingly to line up.
                for (int i = 0; i < kWidth; i++)
                {
                    currentOrients[i].position.X -= ELEMENT_SPACING_X;
                }
            }
            else if (tabDiff == -1)
            {
                UIGridLevelElement rightElement = Get(SelectionIndex.X + 1);
                if ((rightElement == null) || (rightElement.Level == null))
                {
                    // Nothing to go to.
                    return false;
                }
                MoveRight();
                // The result of the right move will cause all the elements to "Shift()" to the right by 1.
                // Our orientations must move accordingly to line up.
                for (int i = 0; i < kWidth; i++)
                {
                    currentOrients[i].position.X += ELEMENT_SPACING_X;
                }
            }

            // Now that the grid has been modified (if necessary), perform the actual slide move and
            // interpolate the orientation of each element to its new values.
            for (int i = 0; i < kWidth; i++)
            {
                float newElementXPos = currentOrients[i].position.X + xMove;

                Orientation nextOrientation = null;
                Orientation prevOrientation = null;
                for (int j = 0; j < kWidth; j++)
                {
                    // Find the lowest base X orientation that matches this element
                    if (baseOrients[j].position.X >= newElementXPos)
                    {
                        // We know that base orientation 'j' is the next orientation,
                        // and 'j-1' contains the previous one.
                        nextOrientation = baseOrients[j];
                        if ((j - 1) >= 0)
                        {
                            prevOrientation = baseOrients[j - 1];
                        }
                        break;
                    }
                }

                if ((nextOrientation == null) || (prevOrientation == null))
                {
                    // This orientation is such that it is beyond the bounds covered by the base orientations.
                    // Therefore no changes to orientation are performed other than an X translation.
                    currentOrients[i].position.X = newElementXPos;
                }
                else
                {
                    // We have a next and a previous orientation to interpolate between. Compute lerp factor
                    float prevToNewX = newElementXPos - prevOrientation.position.X;
                    float prevToNextX = nextOrientation.position.X - prevOrientation.position.X;

                    float lerpFactor = prevToNewX / prevToNextX;

                    // Discover differences between our orientations
                    Vector3 prevToNextPos = nextOrientation.position - prevOrientation.position;
                    float prevToNextScale = nextOrientation.scale - prevOrientation.scale;
                    float prevToNextRotation = nextOrientation.rotation - prevOrientation.rotation;

                    // Using the lerp factor, compute the delta values to add to our orientation.
                    Vector3 posDelta = prevToNextPos * lerpFactor;
                    float scaleDelta = prevToNextScale * lerpFactor;
                    float rotationDelta = prevToNextRotation * lerpFactor;

                    currentOrients[i].position = prevOrientation.position + posDelta;
                    currentOrients[i].scale = prevOrientation.scale + scaleDelta;
                    currentOrients[i].rotation = prevOrientation.rotation + rotationDelta;
                }
            }
            return true;
        }

        private float nfmod(float x, float m)
        {
            return ((x % m) + m) % m;
        }

        #endregion // Internal

        #region Private

        #endregion // Private

    }   // end of class LoadLevelMenuUIGrid

}   // end of namespace Boku.Ui2d
