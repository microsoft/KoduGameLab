
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

    /// <summary>
    /// A block represents a series of contiguous reflexes in a task.
    /// In current usage, all the blocks are children of the first reflex,
    /// eg they all have a greater indent level than the first one.
    /// Designed as a helper class for moving/cut/paste of reflexes.
    /// </summary>
    public class ReflexBlock
    {
        #region Members

        public int Index = -1;
        public int Size = 0;
        public int OriginalIndent = 0;
        
        #endregion

        #region Accesssors

        /// <summary>
        /// Like the Moving value on a ReflexPanel but includes the whole block.
        /// </summary>
        public bool Moving
        {
            get 
            {
                List<ReflexPanel> panels = InGame.inGame.Editor.ActivePanels;
                return panels[Index].Moving;
            }
            set
            {
                if (Moving != value)
                {
                    List<ReflexPanel> panels = InGame.inGame.Editor.ActivePanels;
                    for (int i = Index; i < Index + Size; i++)
                    {
                        panels[i].Moving = value;
                    }

                    if (value == false)
                    {
                        // Now that we've stopped moving the block, we may need to 
                        // adjust its indent level.
                        ValidateIndent();
                    }
                }
            }
        }   // end of Moving

        #endregion

        #region Public

        /// <summary>
        /// Takes the index as the beginning reflex in the block and
        /// sets the rest of the values accordingly.
        /// </summary>
        /// <param name="index">Index of first reflex in block.</param>
        public void Init(int index)
        {
            List<ReflexPanel> panels = InGame.inGame.Editor.ActivePanels;
            Task task = panels[0].Reflex.Task;

            Index = index;
            OriginalIndent = ((Reflex)task.reflexes[Index]).Indentation;
            Size = 1;

            for (int i = index + 1; i < panels.Count; i++)
            {
                Reflex reflex = (Reflex)task.reflexes[i];
                if (reflex.Indentation > OriginalIndent)
                {
                    ++Size;
                }
                else
                {
                    break;
                }
            }
        }   // end of Init()

        /// <summary>
        /// If possible, indents the block by one.
        /// </summary>
        /// <param name="userTriggerd">Was this indent user triggered or automatic?</param>
        public void Indent(bool userTriggered)
        {
            List<ReflexPanel> panels = InGame.inGame.Editor.ActivePanels;
            Task task = panels[0].Reflex.Task;

            int curIndent = ((Reflex)task.reflexes[Index]).Indentation;

            // Don't allow block to be indented further if it's already
            // indented more than the previous reflex or at the top of the task.
            if (Index != 0 && (((Reflex)task.reflexes[Index - 1]).Indentation >= curIndent))
            {
                for (int i = Index; i < Index + Size; i++)
                {
                    Reflex reflex = (Reflex)task.reflexes[i];
                    ++reflex.Indentation;
                }
            }

            if (userTriggered)
            {
                ++OriginalIndent;
            }
        }   // end of IndentBlock()

        /// <summary>
        /// If possible, unindents the block by one.
        /// </summary>
        /// <param name="userTriggerd">Was this indent user triggered or automatic?</param>
        public void Unindent(bool userTriggered)
        {
            List<ReflexPanel> panels = InGame.inGame.Editor.ActivePanels;
            Task task = panels[0].Reflex.Task;

            int curIndent = ((Reflex)task.reflexes[Index]).Indentation;

            if (curIndent > 0)
            {
                for (int i = Index; i < Index + Size; i++)
                {
                    Reflex reflex = (Reflex)task.reflexes[i];
                    --reflex.Indentation;
                }
            }

            if (userTriggered)
            {
                --OriginalIndent;
            }
        }   // end of UnindentBlock()

        public void MoveUp()
        {
            // If we're already at the top, do nothing.
            if (Index > 0)
            {
                List<ReflexPanel> panels = InGame.inGame.Editor.ActivePanels;

                // Find the block above this on and swap places with it.
                int topIndex = Index - 1;
                while (topIndex > 0)
                {
                    if (panels[topIndex].Reflex.Indentation <= panels[Index].Reflex.Indentation)
                    {
                        break;
                    }
                    --topIndex;
                }

                ReflexBlock topBlock = new ReflexBlock();
                topBlock.Init(topIndex);

                // If the block above this one actually contains this block then
                // we need to shorten the top block.  This can happen if you're 
                // moving a child reflex out of a block.
                if (Index < topBlock.Index + topBlock.Size)
                {
                    topBlock.Size = Index - topBlock.Index;
                }

                SwapBlocks(this, topBlock);
            }
        }   // end of MoveUp()

        public void MoveDown()
        {
            List<ReflexPanel> panels = InGame.inGame.Editor.ActivePanels;
            Task task = panels[0].Reflex.Task;

            // Find the block below this one and swap places with it.
            int bottomIndex = Index + Size;
            // If there are no reflexes below this one, there's 
            // no place to go so don;t do anything.
            if (bottomIndex < panels.Count)
            {
                ReflexBlock bottomBlock = new ReflexBlock();
                bottomBlock.Init(bottomIndex);

                SwapBlocks(this, bottomBlock);
            }

        }   // end of MoveDown()

        /// <summary>
        /// Exchanges the position of 2 blocks of reflexes in the current task.
        /// Also aniamtes the UI.
        /// Assumes that the camera should end up looking at b0
        /// </summary>
        /// <param name="b0"></param>
        /// <param name="b1"></param>
        public void SwapBlocks(ReflexBlock b0, ReflexBlock b1)
        {
            List<ReflexPanel> panels = InGame.inGame.Editor.ActivePanels;
            Task task = panels[0].Reflex.Task;

            // Why isn't this a constant somewhere???
            float heightPanel = panels[0].BoundingBox.Max.Y - panels[0].BoundingBox.Min.Y;

            // How far to move b0's panels.
            int steps0 = b0.Index < b1.Index ? b1.Size : -b1.Size;

            for (int i = b0.Index; i < b0.Index + b0.Size; i++)
            {
                panels[i].AnimatePanelMove(-steps0 * heightPanel);
            }

            // Move the camera the same amount.
            InGame.inGame.Editor.MoveCamera(-steps0 * heightPanel);

            // Move b1's panels.
            int steps1 = b1.Index < b0.Index ? b0.Size : -b0.Size;

            for (int i = b1.Index; i < b1.Index + b1.Size; i++)
            {
                panels[i].AnimatePanelMove(-steps1 * heightPanel);
            }

            /*
            Debug.Print("before");
            Debug.Print("panels");
            for (int i = 0; i < panels.Count; i++)
            {
                Debug.Print(panels[i].UniqueNum.ToString() + " " + panels[i].LineNumber.ToString());
            }
            Debug.Print("reflexes");
            for (int i = 0; i < task.reflexes.Count; i++)
            {
                Debug.Print(((Reflex)task.reflexes[i]).UniqueNum.ToString());
            }
            */

            // Now actually swap the panels.  Copy the panel refs to
            // an array and then copy them back in their new position.
            ReflexPanel[] panelArray = new ReflexPanel[panels.Count];
            panels.CopyTo(panelArray);

            // b0's elements
            for (int i = 0; i < b0.Size; i++)
            {
                panels[b0.Index + i + steps0] = panelArray[b0.Index + i];
            }
            // b1's elements
            for (int i = 0; i < b1.Size; i++)
            {
                panels[b1.Index + i + steps1] = panelArray[b1.Index + i];
            }

            // Now do the same for the reflexes.
            Reflex[] reflexArray = new Reflex[task.reflexes.Count];
            task.reflexes.CopyTo(reflexArray);

            // b0's elements
            for (int i = 0; i < b0.Size; i++)
            {
                task.reflexes[b0.Index + i + steps0] = reflexArray[b0.Index + i];
            }
            // b1's elements
            for (int i = 0; i < b1.Size; i++)
            {
                task.reflexes[b1.Index + i + steps1] = reflexArray[b1.Index + i];
            }

            // Update line numbers.
            for (int i = 0; i < panels.Count; i++)
            {
                panels[i].LineNumber = i + 1;
            }

            // Hack mode...
            // In the rendering of the editor we need to keep the order of the reflexes
            // in sync.  This is so the mouse hit testing can know which reflex it is
            // hitting.  So, reorder the elements in the renderobject also.
            // Without doing this, what happens is that the reflexes in the panel
            // and reflex arrays (already 1 too many) are reordered but the render list
            // isn't.  When the mouse hit testing finds a hit it has no way to associate
            // that with the actual reflex being hit so we have to rely on its index
            // in the renderlist.  Hence the need to keep them in sync and a warning to
            // future generations that overly aggressive abstraction will come back to bite you.

            List<RenderObject> rlist = InGame.inGame.Editor.renderObj.renderList;
            RenderObject[] rArray = new RenderObject[rlist.Count];
            rlist.CopyTo(rArray);

            // b0's elements
            for (int i = 0; i < b0.Size; i++)
            {
                rlist[b0.Index + i + steps0] = rArray[b0.Index + i];
            }
            // b1's elements
            for (int i = 0; i < b1.Size; i++)
            {
                rlist[b1.Index + i + steps1] = rArray[b1.Index + i];
            }


            /*
            Debug.Print("before");
            Debug.Print("panels");
            for (int i = 0; i < panels.Count; i++)
            {
                Debug.Print(panels[i].UniqueNum.ToString() + " " + panels[i].LineNumber.ToString());
            }
            Debug.Print("reflexes");
            for (int i = 0; i < task.reflexes.Count; i++)
            {
                Debug.Print(((Reflex)task.reflexes[i]).UniqueNum.ToString());
            }
            */

            // Update the active panel to stil point to the same reflex.
            InGame.inGame.Editor.IndexActivePanel = b0.Index + steps0;

            // Update the values in the blocks.
            b0.Index += steps0;
            b1.Index += steps1;

        }   // end of SwapBlocks()

        /// <summary>
        /// Copies the data from the current block into the cut/paste buffer
        /// and then removes the reflexes.
        /// </summary>
        public void Cut()
        {
            // Copy all the ReflexData off to the buffer.
            Copy();

            // Add a blank reflex to the end if this block went to the end.
            List<ReflexPanel> panels = InGame.inGame.Editor.ActivePanels;
            if (Index + Size >= panels.Count)
            {
                InGame.inGame.Editor.InsertReflex(null);
            }

            // Delete all the reflexes in the block.
            for (int i = 0; i < Size; i++)
            {
                InGame.inGame.Editor.ActivePanels[Index].RemoveReflex();
            }
        }   // end of Cut()

        /// <summary>
        /// Copies the reflex data from the curent block to the cut/paste buffer.
        /// </summary>
        public void Copy()
        {
            // Create buffer if needed.
            if (ReflexPanel.CutPasteBuffer == null)
            {
                ReflexPanel.CutPasteBuffer = new List<ReflexData>();
            }

            // Remove any existing reflexes.
            ReflexPanel.CutPasteBuffer.Clear();

            // Copy the reflex data to the buffer.
            List<ReflexPanel> panels = InGame.inGame.Editor.ActivePanels;
            for (int i = Index; i < Index + Size; i++)
            {
                ReflexPanel.CutPasteBuffer.Add(panels[i].Reflex.Data);
            }

        }   // end of Copy()

        /// <summary>
        /// Paste the contents of the cut/paste buffer into the brain.
        /// </summary>
        public void Paste()
        {
            // Anything there?
            if (ReflexPanel.CutPasteBuffer == null || ReflexPanel.CutPasteBuffer.Count == 0)
            {
                return;
            }

            // We want to paste the reflexs bottom up into the current position.  This
            // moves the current reflex and any subsequent ones down.

            Editor editor = InGame.inGame.Editor;
            ReflexPanel panel = editor.ActivePanel;

            for (int i = ReflexPanel.CutPasteBuffer.Count - 1; i >= 0; i--)
            {
                panel.InsertReflex();

                // The newly inserted panel should have become the active one.
                panel = editor.ActivePanel;

                // Paste the cut/paste code into this new panel and tell it to rebuild.
                panel.Reflex.Paste(ReflexPanel.CutPasteBuffer[i]);
                panel.AnimatePanelIndent(true);
                panel.uiRebuild = true;
            }

            // Update block with info from cut/paste buffer.
            Size = ReflexPanel.CutPasteBuffer.Count;
            OriginalIndent = ReflexPanel.CutPasteBuffer[0].Indentation;

            // Ensure indent is correct.
            ValidateIndent();

        }   // end of Paste()

        #endregion

        #region Internal

        public class ReflexComparer : IComparer<RenderObject>
        {
            public int Compare(RenderObject x, RenderObject y)
            {
                int result = 0;

                ControlRenderObj cx = x as ControlRenderObj;
                ControlRenderObj cy = y as ControlRenderObj;

                if (cx != null && cy != null)
                {
                    ITransform ix = cx as ITransform;
                    ITransform iy = cy as ITransform;

                    if (ix.World.Translation.Y != iy.World.Translation.Y)
                    {
                        result = ix.World.Translation.Y < iy.World.Translation.Y ? 1 : -1;
                    }
                }

                return result;
            }
        }   // end of class ReflexComparer


        /// <summary>
        /// After an action this adjusts the indent level to ensure it's valid.
        /// </summary>
        private void ValidateIndent()
        {
            List<ReflexPanel> panels = InGame.inGame.Editor.ActivePanels;

            int max = Index == 0 ? 0 : panels[Index - 1].Reflex.Indentation + 1;
            while (panels[Index].Reflex.Indentation > max)
            {
                Unindent(false);
            }
            while (panels[Index].Reflex.Indentation < max && panels[Index].Reflex.Indentation < OriginalIndent)
            {
                Indent(false);
            }

            // Moving a block may cause the following block to also need to move.
            // If there are reflexes following this block create a new block and validate it.
            if (Index + Size < panels.Count)
            {
                ReflexBlock block = new ReflexBlock();
                block.Init(Index + Size);
                block.ValidateIndent();
            }

        }   // end of ValidateIndent()

        #endregion
    }   // end of class ReflexBlock

}   // end of namespace Boku.UI
