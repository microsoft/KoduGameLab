// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.


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
using Microsoft.Xna.Framework.Input.Touch;
using Microsoft.Xna.Framework.Storage;

using KoiX;
using KoiX.Geometry;
using KoiX.Input;
using KoiX.Managers;
using KoiX.Text;
using KoiX.UI;
using KoiX.UI.Dialogs;

using Boku;
using Boku.Audio;
using Boku.Common;
using Boku.Common.Xml;

namespace KoiX.UI
{
    /// <summary>
    /// This is the widgetSet that displays the list of browser worlds as seen
    /// in the LoadLevelScene.  Each world tile is considered a widget.
    /// </summary>
    public partial class BrowserWorldsDisplay : WidgetSet
    {
        #region Members

        /// <summary>
        /// Number of tiles in this display.  Also used by the cursors.
        /// </summary>
        public const int numTiles = 18;

        /// <summary>
        /// The index of the front-most tile (for proper z-ordering when drawing)
        /// </summary>
        public const int front = 8;

        Callback ShiftLeft;
        Callback ShiftRight;
        Callback SelectWorld;
        
        Tile[] tiles = new Tile[numTiles];

        /// <summary>
        /// Base transforms for the fan of tiles.
        /// </summary>
        Transform[] baseTransforms;
        
        /// <summary>
        /// The current transform array is used during dragging to store the intermediate positions
        /// of each element as they move under the user's touch. Once the touch is released, the
        /// elements return to their base orientations.
        /// </summary>
        Transform[] currentTransforms;

        /// <summary>
        /// This is the amount of distance in the grid coordinate space that each element has from its neighbors.
        /// </summary>
        Vector2 tileSpacing = new Vector2(180.0f, 50.0f);

        Texture2D missingThumbnail;

        bool renderFocusTile = true;    // Can be set to false to allow tile to be manually rendered later.

        LevelBrowserType browserType = LevelBrowserType.Local;  // Needed for decoration in Community.

        #endregion

        #region Accessors

        /// <summary>
        /// True when no valid levels exist.
        /// </summary>
        public bool NoValidLevels
        {
            get
            {
                bool result = true;

                for (int i = 0; i < numTiles; i++)
                {
                    Tile tile = tiles[i];
                    if (tile != null && tile.Level != null)
                    {
                        result = false;
                        break;
                    }
                }

                return result;
            }
        }

        /// <summary>
        /// If set to false, the tile for the world in focus is not rendered.  It
        /// can then manually be rendered by calling RenderFocusTile().
        /// Defaults to true.
        /// </summary>
        public bool RenderFocusWorld
        {
            get { return renderFocusTile; }
            set { renderFocusTile = value; }
        }

        /// <summary>
        /// Get the thumbnail texture for the current focus world.
        /// </summary>
        public Texture2D FocusWorldThumbnail
        {
            get
            {
                Texture2D result = null;

                Tile tile = tiles[front];
                if (tile != null)
                {
                    result = tile.Thumbnail;
                }
                
                return result;
            }
        }

        public Tile CurFocusTile
        {
            get { return tiles[front]; }
        }

        #endregion

        #region Public

        public BrowserWorldsDisplay(BaseDialog parentDialog, LevelBrowserType browserType, Callback ShiftLeft, Callback ShiftRight, Callback SelectWorld)
            : base(parentDialog: parentDialog, rect: RectangleF.EmptyRect, orientation: Orientation.None)
        {
            this.browserType = browserType;
            this.ShiftLeft = ShiftLeft;
            this.ShiftRight = ShiftRight;
            this.SelectWorld = SelectWorld;

            baseTransforms = new Transform[numTiles] {
                new Transform(new Vector2(-8, 1.0f) * tileSpacing, 0.2f, -0.36f),
                new Transform(new Vector2(-7, 1.0f) * tileSpacing, 0.3f, -0.32f),
                new Transform(new Vector2(-6, 1.0f) * tileSpacing, 0.4f, -0.28f),
                new Transform(new Vector2(-5, 1.0f) * tileSpacing, 0.5f, -0.24f),
                new Transform(new Vector2(-4, 1.0f) * tileSpacing, 0.6f, -0.2f),
                new Transform(new Vector2(-3, 1.0f) * tileSpacing, 0.7f, -0.16f),
                new Transform(new Vector2(-2, 1.2f) * tileSpacing, 0.75f, -0.14f),
                new Transform(new Vector2(-1, 1.18f) * tileSpacing, 0.79f, -0.08f),
                new Transform(new Vector2(0,  0.0f) * tileSpacing, 1.2f, 0.0f),
                new Transform(new Vector2(1.5f,  0.9f) * tileSpacing, 0.79f, 0.1f),
                new Transform(new Vector2(2.5f,  0.75f) * tileSpacing, 0.7f, 0.15f),
                new Transform(new Vector2(3.5f,  0.6f) * tileSpacing, 0.63f, 0.19f),
                new Transform(new Vector2(4.5f,  0.5f) * tileSpacing, 0.58f, 0.25f),
                new Transform(new Vector2(5.5f,  0.53f) * tileSpacing, 0.52f, 0.3f),
                new Transform(new Vector2(6.5f,  0.6f) * tileSpacing, 0.47f, 0.35f),
                new Transform(new Vector2(7.5f,  0.6f) * tileSpacing, 0.42f, 0.4f),
                new Transform(new Vector2(8.5f,  0.6f) * tileSpacing, 0.35f, 0.45f),
                new Transform(new Vector2(9.5f,  0.6f) * tileSpacing, 0.3f, 0.5f),
            };

            currentTransforms = new Transform[numTiles];
            for (int i = 0; i < numTiles; i++)
            {
                currentTransforms[i] = new Transform(baseTransforms[i]);
            }

        }   // end of c'tor

        public override void Update(SpriteCamera camera, Vector2 parentPosition)
        {
            this.parentPosition = parentPosition;

            foreach (Tile tile in tiles)
            {
                if (tile != null)
                {
                    tile.Update(camera, parentPosition + Position);
                }
            }

        }   // end of Update()

        public override void Render(SpriteCamera camera, Vector2 parentPosition)
        {
            // Note that we do all rendering from the outside-in.  This results
            // in the center tiles being rendered on top of the ones further out.
            // The infocus tile is rendered seperately so it can mbe layerd correctly
            // with the flyout dialog.
            for (int i = numTiles - 1; i > front; i--)
            {
                if (tiles[i] != null)
                {
                    tiles[i].Render(camera, browserType);
                }
            }
            for (int i = 0; i < front; i++)
            {
                if (tiles[i] != null)
                {
                    tiles[i].Render(camera, browserType);
                }
            }

            if (renderFocusTile)
            {
                RenderFocusTile(camera, browserType);
            }

        }   // end of render()

        /// <summary>
        /// Renders just the in-focus tile.
        /// </summary>
        public void RenderFocusTile(SpriteCamera camera, LevelBrowserType browserType)
        {
            if (tiles[front] != null)
            {
                tiles[front].Render(camera, browserType);
            }
        }   // end of RenderFocusTile()

        public override void RegisterForInputEvents()
        {
            // Call base register first.  By putting the child widgets on the input stacks
            // first we can then put oursleves on and have priority.
            base.RegisterForInputEvents();

            //KoiLibrary.InputEventManager.RegisterForEvent(this, InputEventManager.Event.MouseLeftDown);
            //KoiLibrary.InputEventManager.RegisterForEvent(this, InputEventManager.Event.MouseWheel);

            //KoiLibrary.InputEventManager.RegisterForEvent(this, InputEventManager.Event.Tap);
            //KoiLibrary.InputEventManager.RegisterForEvent(this, InputEventManager.Event.OnePointDrag);

            //KoiLibrary.InputEventManager.RegisterForEvent(this, InputEventManager.Event.Keyboard);          // Control keys.
            //KoiLibrary.InputEventManager.RegisterForEvent(this, InputEventManager.Event.WinFormsKeyboard);  // Text.

            KoiLibrary.InputEventManager.RegisterForEvent(this, InputEventManager.Event.GamePad);

        }   // end of RegisterForInputEvents()

        /// <summary>
        /// Loads the correct "level" data to each of the grid elements,
        /// </summary>
        /// <param name="cursor"></param>
        public void Reload(ILevelSetCursor cursor)
        {
            for (int i = 0; i < numTiles; ++i)
            {
                LevelMetadata level = cursor[i - front];
                
                Tile existing = tiles[i];

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
                        tiles[i] = existing;
                        existing.SetOrientation(baseTransforms[i]);
                        Widgets.Add(existing);
                    }
                    existing.Level = level;
                }
                else
                {
                    // No level, so set element's ref to null.
                    existing.Level = null;
                }
            }

        }   // end of Reload()

        public Tile Get(int index)
        {
            return tiles[index];
        }

        public Tile CreateElement(LevelMetadata level)
        {
            Tile tile = new Tile(ParentDialog, OnSelect);

            tile.LoadContent();

            if (level == null)
                return tile;

            tile.Level = level;

            tile.Activate();

            return tile;
        }

        /// <summary>
        /// Shifts the tiles within the current array.
        /// Note that this wraps.  Not sure why.  :-(
        /// </summary>
        /// <param name="amount"></param>
        public void Shift(int amount)
        {
            while (amount != 0)
            {
                if (amount > 0)
                {
                    // Cursor goes right, grid elements go left.
                    Tile tile = tiles[0];
                    for (int i = 0; i < numTiles - 1; i++)
                    {
                        tiles[i] = tiles[i + 1];
                        if (tiles[i] != null)
                        {
                            tiles[i].TwitchOrientation(baseTransforms[i]);
                        }
                    }
                    tiles[numTiles - 1] = tile;
                    if (tile != null)
                    {
                        tile.Level = null;
                        tile.RtDirty = true;
                        tile.SetOrientation(baseTransforms[numTiles - 1]);
                        Foley.PlayShuffle();
                    }
                    --amount;
                }
                else
                {
                    // Cursor goes left, grid elements go right.
                    Tile tile = tiles[numTiles - 1];
                    for (int i = numTiles - 1; i > 0; i--)
                    {
                        tiles[i] = tiles[i - 1];
                        if (tiles[i] != null)
                        {
                            tiles[i].TwitchOrientation(baseTransforms[i]);
                        }
                    }
                    tiles[0] = tile;
                    if (tile != null)
                    {
                        tile.Level = null;
                        tile.RtDirty = true;
                        tile.SetOrientation(baseTransforms[0]);
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

        /// <summary>
        /// Resets all the tile transforms.
        /// Used after changing sort order or
        /// removing tiles.
        /// Now used every frame so display is always in sync.  Does
        /// nothing if tiles already in the correct place.
        /// <param name="twitch">Should the positions be twitched?</param>
        /// </summary>
        public void ResetTileTransforms(bool twitch = false)
        {
            for (int i = 0; i < numTiles; i++)
            {
                if (tiles[i] != null)
                {
                    if (twitch)
                    {
                        tiles[i].TwitchOrientation(baseTransforms[i]);
                    }
                    else
                    {
                        tiles[i].SetOrientation(baseTransforms[i]);
                    }
                }
            }
        }   // end of ResetTransforms()

        /// <summary>
        /// Removes the element at index, collapses the rest of the list, 
        /// and then adds the removed element back to the end of the list.
        /// </summary>
        /// <param name="index"></param>
        public void Remove(int index)
        {
            index += front;

            if (index >= 0 && index < numTiles)
            {
                Tile tile = tiles[index];
                for (int i = index; i < numTiles - 1; i++)
                {
                    tiles[i] = tiles[i + 1];
                }
                tiles[numTiles - 1] = tile;
                if (tile != null)
                {
                    tile.Level = null;
                    tile.RtDirty = true;
                    tile.SetOrientation(baseTransforms[numTiles - 1]);
                }
            }
        }   // end of Remove()

        /// <summary>
        /// Callback used by world tiles when clicked or tapped.  This just passes
        /// through to the callbacks given to the c'tor so it's actually the 
        /// LoadLevelScene handling things.
        /// </summary>
        /// <param name="b"></param>
        void OnSelect(BaseWidget w)
        {
            // If the clicked tile is the one in focus, then activate the fly-out menu.
            // Otherwise, shift teh arry of worlds to put the clicked oin tile into the focus position.
            Tile tile = w as Tile;
            if (tile != null)
            {
                if (tiles[front] == tile)
                {
                    if(SelectWorld != null)
                    {
                        SelectWorld(w);
                    }
                }
                else
                {
                    // Figure out how much we need to shift.
                    // First, figure out the index of the clicked on tile.
                    int index = -1;
                    for (int i = 0; i < numTiles; i++)
                    {
                        if (tile == tiles[i])
                        {
                            index = i;
                            break;
                        }
                    }
                    if (index > -1)
                    {
                        int shift = index - front;
                        if(shift > 0)
                        {
                            for (int i = 0; i < shift; i++)
                            {
                                ShiftRight(w);
                            }
                        }
                        else
                        {
                            for (int i = shift; i < 0; i++)
                            {
                                ShiftLeft(w);
                            }
                        }
                    }
                    else
                    {
                        Debug.Assert(false, "How did we get a hit on a tile that's not on the list?");
                    }
                }
            }
        }   // end of OnSelect()

        public override bool ProcessGamePadEvent(GamePadInput pad)
        {
            Debug.Assert(Active);

            if (pad.ButtonA.WasPressed)
            {
                OnSelect(tiles[front]);
                return true;
            }

            // Worlds.
            if (pad.LeftStickLeft.WasPressedOrRepeat || pad.DPadLeft.WasPressedOrRepeat)
            {
                ShiftLeft(this);
                return true;
            }
            if (pad.LeftStickRight.WasPressedOrRepeat || pad.DPadRight.WasPressedOrRepeat)
            {
                ShiftRight(this);
                return true;
            }

            return base.ProcessGamePadEvent(pad);
        }   // end of ProcessGamePAdEvent()

        #endregion

        #region Internal

        public override InputEventHandler HitTest(Vector2 hitLocation)
        {
            InputEventHandler result = null;

            if (Active)
            {
                // Need to apply the HitTest to each of the tiles.
                Vector2 hit = hitLocation - Position;

                // HitTest the tiles from the center out since the center
                // tiles may overlap the ones further out.
                for (int i = front; i >= 0; i--)
                {
                    if (tiles[i] != null && result == null)
                    {
                        result = tiles[i].HitTest(hit);
                    }
                }
                for (int i = front + 1; i < numTiles; i++)
                {
                    if (tiles[i] != null && result == null)
                    {
                        result = tiles[i].HitTest(hit);
                    }
                }
            }

            if (result != null)
            {
            }

            return result;
        }

        public override void LoadContent()
        {
            if (missingThumbnail != null)
            {
                missingThumbnail = KoiLibrary.LoadTexture2D(@"Textures\LoadLevel\WaitLarge");
            }

            foreach (Tile tile in tiles)
            {
                if (tile != null)
                {
                    tile.LoadContent();
                }
            }

            base.LoadContent();
        }

        public override void UnloadContent()
        {
            foreach (Tile tile in tiles)
            {
                tile.UnloadContent();
            }

            DeviceResetX.Release(ref missingThumbnail);

            base.UnloadContent();
        }

        #endregion
    }
}   // end of namespace KoiX.UI
