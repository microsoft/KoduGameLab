
using System;
using System.Collections;

using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Audio;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using Microsoft.Xna.Framework.Storage;

using Boku.Common;

namespace Boku.UI2D
{
    public enum ClickType
    {
        WasPressed,
        IsPressed,
        WasReleased,
        None
    }

    /// <summary>
    /// UI Grid element used for 2d terrain editing brushes.  Basically it's just a 
    /// wrapper around a drawable item, text blob image etc...
    /// </summary>
    public class ScrollContainer
    {
        #region Members
            private float width;
            private float height;
            private float scrollOffset = 0.0f;
            private AABB2D hitbox = null;
        #endregion
        
        #region Public

            public ScrollContainer(Vector2 size)
            {
                this.width = size.X;
                this.height = size.Y;
                this.hitbox = new AABB2D(new Vector2(0.0f, 0.0f), new Vector2(size.X, size.Y));
            }

            public void UpdatePosition(Vector2 pos)
            {
                hitbox.Set(pos, pos + new Vector2(width, height));
            }

            virtual public void ResetWidth()        { ;}

            virtual public void Render(Vector2 pos) 
            {
            } // needs to be pure virtual
            virtual public void Hover(Vector2 pos)  { ;}

            /// <summary>
            /// These functions handle when the container is clicked.
            /// The 'wasReleased' version allows differing behavior depending
            /// on whether the click has been released this frame or not.
            /// </summary>
            /// <param name="pos"></param>
            /// <returns></returns>
            virtual public Object Click(Vector2 pos) 
            {
                return null; 
            }
            virtual public bool Click(Vector2 pos, out Object obj)
            { 
                obj = null; return false; 
            }
            virtual public bool Click(Vector2 pos, out Object obj, ClickType clickType)
            {
                obj = null; return false;
            }

            /// <summary>
            ///  Allows container item to set focus to a "previous" item
            /// </summary>
            /// <returns>True if set, False if no 'prev' item to set to</returns>
            virtual public bool SetPrevFocus() { return false;}

            /// <summary>
            ///  Allows container item to set focus to a "next" item
            /// </summary>
            /// <returns>True if set, False if no 'next' item to set to</returns>
            virtual public bool SetNextFocus() { return false; }

            /// <summary>
            /// Allows container item to set focus to the last item in the container
            /// </summary>
            /// <returns></returns>
            virtual public bool SetFocusLast() { return false; }

            /// <summary>
            /// Tells the container item to enable focus on items. If not enabled, then
            /// the container may decide not to respond to focus press requests.
            /// </summary>
            virtual public void EnableFocus() { }

            /// <summary>
            /// Tells the container item to disable focus
            /// </summary>
            virtual public void DisableFocus() { }

            /// <summary>
            /// Tells the container item to reset to the first item, if any
            /// </summary>
            virtual public void ResetFocus() { }

            virtual public int GetNumFocus() { return 0; }

            /// <summary>
            ///  Ask the container item whether focus is currently enabled
            /// </summary>
            virtual public bool IsFocusEnabled() { return false; }

            /// <summary>
            /// Activate the container item that is the current focus
            /// </summary>
            virtual public void Press() { }

            public bool IsInFocus(Vector2 pos)
            {
                return hitbox.Contains(pos);
            }
        #endregion

        #region Accessors
        public float SetScrollOffset
        {
            get{ return scrollOffset; }
            set{ scrollOffset = value; }
        }
        public float Height
        {
            get { return height; }
            set { height = value; }
        }
        public float Width
        {
            get { return width; }
            set { width = value; }
        }
        public float Top
        {
            get { return hitbox.Min.Y; }
        }

        public float Bottom
        {
            get { return hitbox.Max.Y; }
        }
        #endregion


    }   // end of class ScrollContainer

}   // end of namespace Boku.UI2D
