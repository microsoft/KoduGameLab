// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.



using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;

using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Storage;

using Boku.Base;
using Boku.Common;
using Boku.Common.Xml;
using Boku.Fx;

namespace Boku
{
    public partial class InGame : GameObject, INeedsDeviceReset
    {
        /// <summary>
        /// Class to keep track of autosaves for implementing undo and redo.
        /// Note that this is a pure static class, but doesn't need to be.
        /// </summary>
        public class UnDoStack
        {
            #region Members
            /// <summary>
            /// Owning object.
            /// </summary>
            private static InGame inGame = null;

            /// <summary>
            /// The stack.
            /// </summary>
            private static string[] stack = null;

            /// <summary>
            /// Stack pointer to base of the stack, the farthest you can undo toward.
            /// </summary>
            private static int idxBase = 0;
            /// <summary>
            /// Stack pointer to the top of the stack, the farthest you can redo toward.
            /// </summary>
            private static int idxTop = 0;
            /// <summary>
            /// Stack pointer to current position in stack.
            /// </summary>
            private static int _idxAt = 0;

            /// <summary>
            /// Number of levels to try for. Actual may be less if there isn't space.
            /// </summary>
            private const int kStartNumLevels = 11;

            private static AABB2D undoHitBox = new AABB2D();
            private static AABB2D redoHitBox = new AABB2D();

            private static Color redoTextColor = Color.White;       // Actual color used for rendering.
            private static Color undoTextColor = Color.White;

            private static Color redoTargetTextColor = Color.White; // Color we're twitching toward.
            private static Color undoTargetTextColor = Color.White;

            private static Color noHoverColor = Color.White;
            private static Color hoverColor = new Color(50, 255, 50);
            
            #endregion Members

            #region Accessors

            /// <summary>
            /// The deepest the stack can get.
            /// </summary>
            public static int MaxUnDoLevel
            {
                get { return stack != null ? stack.Length : 0; }
                set
                {
                    TrimStack(value);
                }
            }
            /// <summary>
            /// The number of undo operations available before all are undone.
            /// </summary>
            public static int NumUnDo
            {
                get
                {
                    int numUnDo = IdxAt - idxBase;
                    if (numUnDo < 0)
                        numUnDo += MaxUnDoLevel;
                    return numUnDo;
                }
            }
            /// <summary>
            /// The number of redo operations available before all are redone.
            /// </summary>
            public static int NumReDo
            {
                get
                {
                    int numReDo = idxTop - IdxAt;
                    if (numReDo < 0)
                        numReDo += MaxUnDoLevel;
                    return numReDo;
                }
            }

            /// <summary>
            /// True if there are any undo operations available.
            /// </summary>
            public static bool HaveUnDo
            {
                get { return IdxAt != idxBase; }
            }
            /// <summary>
            /// True if there are any redo operations available.
            /// </summary>
            public static bool HaveReDo
            {
                get { return IdxAt != idxTop; }
            }

            /// <summary>
            /// Do we have any state to restore?
            /// </summary>
            public static bool HaveAnything
            {
                get { return HaveReDo || HaveUnDo; }
            }

            /// <summary>
            /// Return the current index in the circular queue. On set,
            /// record new value in Options, for resume.
            /// </summary>
            private static int IdxAt
            {
                get { return _idxAt; }
                set 
                { 
                    _idxAt = value;
                    if (_idxAt != XmlOptionsData.LastAutoSave)
                    {
                        XmlOptionsData.LastAutoSave = _idxAt;
                    }
                }
            }

            /// <summary>
            /// Mouse hit box for undo button
            /// </summary>
            public static AABB2D UndoHitBox
            {
                get { return undoHitBox; }
            }

            public static AABB2D RedoHitBox
            {
                get { return redoHitBox; }
            }

            #endregion Accessors

            #region Public
            /// <summary>
            /// Prime us with an autosave and set up stack pointers.
            /// Should be called when a new level is loaded.
            /// </summary>
            public static void Init()
            {
                ZeroStack(0);
                stack[idxBase] = @"AutoSave" + Storage4.UniqueMachineID + @"\AutoSave0";
                Save();
                InGame.AutoSaved = false;
            }

            /// <summary>
            /// Create a new undo snapshot.
            /// </summary>
            public static void Store()
            {
                string name = @"AutoSave" + Storage4.UniqueMachineID + @"\AutoSave" + Next(IdxAt, 1).ToString();
                Push(name);
            }

            /// <summary>
            /// Store the current top of the stack again, presumably
            /// because the name changed.  This makers sure that the
            /// autosave is up to date.
            /// 
            /// TODO (****) Right now this is only used after saving the level
            /// to update the level's name, description, and ???.  Should we
            /// just be able to update those directly?
            /// </summary>
            /// <returns>Whether anything was done.</returns>
            public static bool OverwriteTopOfStack()
            {
                return Save();
            }

            /// <summary>
            /// Go back to previous version (if any available).
            /// </summary>
            /// <returns>Whether anything was available.</returns>
            public static bool UnDo()
            {
                // If the user is editing waypoints, we need to stop this.
                Boku.InGame.WayPointEdit.touchOver.StopAdding();
                Boku.InGame.WayPointEdit.mouseOver.StopAdding();

                if (HaveUnDo)
                {
                    Pop();
                    return true;
                }
                return false;
            }
            /// <summary>
            /// Go forward to next version (if any)
            /// </summary>
            /// <returns>Whether anything was available.</returns>
            public static bool ReDo()
            {
                // If the user is editing waypoints, we need to stop this.
                Boku.InGame.WayPointEdit.touchOver.StopAdding();
                Boku.InGame.WayPointEdit.mouseOver.StopAdding();

                if (HaveReDo)
                {
                    IdxAt = Next(IdxAt, 1);
                    Load(false);

                    return true;
                }
                return false;
            }

            /// <summary>
            /// Initialize and load from the most recent autosave file.
            /// </summary>
            /// <returns>true on success</returns>
            public static bool Resume()
            {
                bool result = false;

                if (HaveResume())
                {
                    int resumeIndex = XmlOptionsData.LastAutoSave;
                    result = Init(@"AutoSave" + Storage4.UniqueMachineID + @"\AutoSave" + resumeIndex, resumeIndex);
                }
                return result;
            }

            /// <summary>
            /// Return whether there exists a valid file to resume from the last session.
            /// No file integrity is checked, just existence.
            /// </summary>
            /// <returns></returns>
            public static bool HaveResume()
            {
                int lastAuto = XmlOptionsData.LastAutoSave;
                if (lastAuto >= 0)
                {
                    string fullPath = BokuGame.Settings.MediaPath 
                        + BokuGame.UnDoPath
                        + @"AutoSave"
                        + lastAuto.ToString()
                        + @".Xml";
                    if (Storage4.FileExists(fullPath, StorageSource.UserSpace))
                    {
                        return true;
                    }
                }
                return false;
            }

            /// <summary>
            /// Updates the text color to show hover status.
            /// </summary>
            /// <param name="mouseHit"></param>
            public static void UpdateTextColor(Vector2 mouseHit)
            {
                Color newColor = undoHitBox.Contains(mouseHit) ? hoverColor : noHoverColor;
                if(newColor != undoTargetTextColor)
                {
                    undoTargetTextColor = newColor;
                    Vector3 curColor = new Vector3(undoTextColor.R / 255.0f, undoTextColor.G / 255.0f, undoTextColor.B / 255.0f);
                    Vector3 destColor = new Vector3(newColor.R / 255.0f, newColor.G / 255.0f, newColor.B / 255.0f);

                    TwitchManager.Set<Vector3> set = delegate(Vector3 value, Object param)
                    {
                        undoTextColor.R = (byte)(value.X * 255.0f + 0.5f);
                        undoTextColor.G = (byte)(value.Y * 255.0f + 0.5f);
                        undoTextColor.B = (byte)(value.Z * 255.0f + 0.5f);
                    };
                    TwitchManager.CreateTwitch<Vector3>(curColor, destColor, set, 0.1f, TwitchCurve.Shape.EaseOut);
                }

                newColor = redoHitBox.Contains(mouseHit) ? hoverColor : noHoverColor;
                if (newColor != redoTargetTextColor)
                {
                    redoTargetTextColor = newColor;
                    Vector3 curColor = new Vector3(redoTextColor.R / 255.0f, redoTextColor.G / 255.0f, redoTextColor.B / 255.0f);
                    Vector3 destColor = new Vector3(newColor.R / 255.0f, newColor.G / 255.0f, newColor.B / 255.0f);

                    TwitchManager.Set<Vector3> set = delegate(Vector3 value, Object param)
                    {
                        redoTextColor.R = (byte)(value.X * 255.0f + 0.5f);
                        redoTextColor.G = (byte)(value.Y * 255.0f + 0.5f);
                        redoTextColor.B = (byte)(value.Z * 255.0f + 0.5f);
                    };
                    TwitchManager.CreateTwitch<Vector3>(curColor, destColor, set, 0.1f, TwitchCurve.Shape.EaseOut);
                }

            }   // end of UpdateTextColor()

            /// <summary>
            /// Render our UI to the screen, but only if now is appropriate.
            /// </summary>
            public static void Render()
            {
                // In mouse mode, don't render undo/redo icons if pickers are active.
                bool mouseEdit = inGame.CurrentUpdateMode == UpdateMode.MouseEdit
                                && !InGame.inGame.touchEditUpdateObj.PickersActive;

                if ((inGame.CurrentUpdateMode == UpdateMode.ToolMenu || mouseEdit)
                    && HaveAnything)
                {
                    GraphicsDevice device = BokuGame.bokuGame.GraphicsDevice;

                    // Calc position for undo/redo text.
                    int x = 0;
                    int y = (int)(BokuGame.ScreenSize.Y * 0.66f);
                    int buttonSize = 40;

                    UI2D.Shared.GetFont Font = UI2D.Shared.GetGameFont18Bold;
                    SpriteBatch batch = UI2D.Shared.SpriteBatch;

                    // Handle small screens.
                    if (BokuGame.ScreenSize.Y < 720)
                    {
                        Font = UI2D.Shared.GetGameFont15_75;
                        buttonSize = (int)(buttonSize * 0.8f);
                    }

                    if (HaveReDo)
                    {
                        ScreenSpaceQuad quad = ScreenSpaceQuad.GetInstance();

                        Texture2D texture = GamePadInput.ActiveMode == GamePadInput.InputMode.GamePad ? ButtonTextures.YButton : ButtonTextures.Redo;
                        quad.Render(texture,
                            Vector4.One,
                            new Vector2(x, y),
                            new Vector2(buttonSize),
                            @"TexturedRegularAlpha");

                        redoHitBox.Set(new Vector2(x, y), new Vector2(x, y) + new Vector2(buttonSize - 12));
                    }
                    else
                    {
                        redoHitBox.Set(Vector2.Zero, Vector2.Zero);
                    }

                    if (HaveUnDo)
                    {
                        ScreenSpaceQuad quad = ScreenSpaceQuad.GetInstance();
                        Texture2D texture = GamePadInput.ActiveMode == GamePadInput.InputMode.GamePad ? ButtonTextures.XButton : ButtonTextures.Undo;
                        quad.Render(texture,
                            Vector4.One,
                            new Vector2(x, y + buttonSize - 12),
                            new Vector2(buttonSize),
                            @"TexturedRegularAlpha");

                        undoHitBox.Set(new Vector2(x, y + buttonSize - 12), new Vector2(x, y + buttonSize - 12) + new Vector2(buttonSize - 12));
                    }
                    else
                    {
                        undoHitBox.Set(Vector2.Zero, Vector2.Zero);
                    }

                    Color darkGrey = new Color(10, 10, 10);

#if NETFX_CORE
                    batch.Begin();
                    if (HaveReDo)
                    {
                        string str = Strings.Localize("undoStack.redo") + "(" + NumReDo.ToString() + ")";
                        TextHelper.DrawStringWithShadow(Font,
                            batch,
                            x + buttonSize - 12,
                            y - 4,
                            str,
                            redoTextColor, darkGrey, false);
                        Vector2 max = redoHitBox.Max;
                        max.X += Font().MeasureString(str).X;
                        redoHitBox.Set(redoHitBox.Min, max);
                    }

                    if (HaveUnDo)
                    {
                        string str = Strings.Localize("undoStack.undo") + "(" + NumUnDo.ToString() + ")";
                        TextHelper.DrawStringWithShadow(Font,
                            batch,
                            x + buttonSize - 12,
                            y + buttonSize - 16,
                            str,
                            undoTextColor, darkGrey, false);
                        Vector2 max = undoHitBox.Max;
                        max.X += Font().MeasureString(str).X;
                        undoHitBox.Set(undoHitBox.Min, max);
                    }
                    batch.End();
#else
                    SysFont.StartBatch(null);
                    if (HaveReDo)
                    {
                        string str = Strings.Localize("undoStack.redo") + "(" + NumReDo.ToString() + ")";
                        SysFont.DrawString(str, new Vector2(x + buttonSize - 12, y - 4), new RectangleF(), Font().systemFont, redoTextColor, outlineColor: Color.Black, outlineWidth: 1.5f);
                        Vector2 max = redoHitBox.Max;
                        max.X += Font().MeasureString(str).X;
                        redoHitBox.Set(redoHitBox.Min, max);
                    }

                    if (HaveUnDo)
                    {
                        string str = Strings.Localize("undoStack.undo") + "(" + NumUnDo.ToString() + ")";
                        SysFont.DrawString(str, new Vector2(x + buttonSize - 12, y + buttonSize - 16), new RectangleF(), Font().systemFont, undoTextColor, outlineColor: Color.Black, outlineWidth: 1.5f);
                        Vector2 max = undoHitBox.Max;
                        max.X += Font().MeasureString(str).X;
                        undoHitBox.Set(undoHitBox.Min, max);
                    }
                    SysFont.EndBatch();
#endif
                }
            }
            #endregion Public

            #region Internal
            /// <summary>
            /// Set up a blank stack.
            /// </summary>
            private static void ZeroStack(int index)
            {
                inGame = InGame.inGame;

                idxBase = idxTop = IdxAt = index;

                stack = new string[kStartNumLevels];
            }
            /// <summary>
            /// Given an autosave name, initialize to it and load it.
            /// </summary>
            /// <param name="autoName"></param>
            /// <returns>true on success</returns>
            private static bool Init(string autoName, int index)
            {
                ZeroStack(index);

                stack[idxBase] = autoName;
                bool result = Load(false);

                return result;
            }


            /// <summary>
            /// Filename at the current location in stack (the currently loaded one).
            /// </summary>
            /// <returns></returns>
            private static string Current()
            {
                return stack[IdxAt];
            }

            /// <summary>
            /// Push another name onto the stack and save the level.
            /// May overwrite the oldest autosaved level.
            /// </summary>
            /// <param name="name"></param>
            private static void Push(string name)
            {
                ClearNext();
                stack[IdxAt] = name;
                Save();
            }

            /// <summary>
            /// Pop and load the next level down the stack.
            /// </summary>
            private static void Pop()
            {
                if (IdxAt != idxBase)
                {
                    IdxAt = Next(IdxAt, -1);
                    Load(false);
                }
            }

            /// <summary>
            /// Load the current named level.
            /// </summary>
            /// <returns>true on success</returns>
            private static bool Load(bool andRun)
            {
                bool result = inGame.LoadAutoSave(Current(), andRun, initUndoStack: false);

                return result;
            }

            /// <summary>
            /// Save the current named level.
            /// </summary>
            /// <returns></returns>
            private static bool Save()
            {
                inGame.AutoSaveLevel(Current());

                return true;
            }

            /// <summary>
            /// Advance and clear out the next slot in the stack.
            /// </summary>
            private static void ClearNext()
            {
                IdxAt = Next(IdxAt, 1);
                if (IdxAt == idxBase)
                    idxBase = Next(idxBase, 1);
                idxTop = IdxAt;
            }

            /// <summary>
            /// Increment the index with wrapping.
            /// </summary>
            /// <param name="i"></param>
            /// <param name="inc"></param>
            /// <returns></returns>
            private static int Next(int i, int inc)
            {
                return (i + inc + stack.Length) % stack.Length;
            }

            /// <summary>
            /// Resize the stack to the new count, preserving existing contents
            /// where possible.
            /// </summary>
            /// <param name="newCount"></param>
            private static void TrimStack(int newCount)
            {
                if (newCount != stack.Length)
                {
                    int oldCount = IdxAt - idxBase;
                    if (oldCount < 0)
                        oldCount += stack.Length;
                    if (oldCount > newCount)
                        oldCount = newCount;
                    int oldIdx = IdxAt - oldCount;
                    if (oldIdx < 0)
                        oldIdx += stack.Length;

                    string[] newStack = new string[newCount];
                    for (int i = 0; i < oldCount; ++i)
                    {
                        newStack[i] = stack[oldIdx];
                        oldIdx = Next(oldIdx, 1);
                    }
                    idxBase = 0;
                    oldIdx = Next(oldIdx, -1);
                    IdxAt = oldIdx;
                    idxTop = oldIdx;
                }
            }

            /// <summary>
            /// Placeholder for when we have a real UI to load/unload.
            /// </summary>
            /// <param name="immediate"></param>
            public static void LoadContent(bool immediate)
            {
            }

            /// <summary>
            /// Placeholder for when we have a real UI to load/unload.
            /// </summary>
            /// <param name="graphics"></param>
            public static void InitDeviceResources(GraphicsDevice device)
            {
            }

            /// <summary>
            /// Placeholder for when we have a real UI to load/unload.
            /// </summary>
            public static void UnloadContent()
            {
            }

            /// <summary>
            /// Recreate render targets.
            /// </summary>
            public static void DeviceReset(GraphicsDevice device)
            {
            }

            #endregion Internal
        };
    };

};
