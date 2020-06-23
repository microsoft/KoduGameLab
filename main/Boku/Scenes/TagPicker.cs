
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
using Microsoft.Xna.Framework.Storage;


using Boku.Base;
using Boku.Common;
using Boku.Common.Xml;
using Boku.Common.Sharing;
using Boku.UI;
using Boku.UI2D;
using Boku.Input;
using Boku.Programming;
using Boku.SimWorld;
using Boku.Web;
using Boku.Fx;

using Boku.Audio;
using BokuShared;

namespace Boku
{
    /// <summary>
    /// A simple wrap around the CHeckBoxList used for tags that
    /// lets us share common code.
    /// </summary>
    public class TagPicker : INeedsDeviceReset
    {
        #region Members

        private ModularCheckboxList tagList = null;
        
        #endregion

        #region Accessors

        /// <summary>
        /// Is the picker currently acitve?
        /// </summary>
        public bool Active
        {
            get { return tagList.Active; }
            set 
            {
                if (value)
                {
                    tagList.Activate(useRtCoords: true);
                }
                else
                {
                    tagList.Deactivate();
                }
            }
        }

        /// <summary>
        /// Delegate called when exiting.
        /// </summary>
        public ModularCheckboxList.UICheckboxListEvent OnExit
        {
            set { tagList.OnExit = value; }
        }

        /// <summary>
        /// Transform for whole picker.
        /// </summary>
        public Matrix WorldMatrix
        {
            get { return tagList.WorldMatrix; }
            set { tagList.WorldMatrix = value; }
        }

        #endregion

        #region Public

        /// <summary>
        /// c'tor
        /// </summary>
        public TagPicker()
        {
            tagList = new ModularCheckboxList();

            // Add regular categories.
            for (int i = (int)Genres.Action; i < (int)Genres.Last; i <<= 1)
            {
                Genres genre = (Genres)i;

                if (genre == Genres.StarterWorlds)
                    continue;

                tagList.AddItem(Strings.GetGenreName(i), false, (Genres)i);
            }

        }   // end of c'tor

        /*
        public void AddItem(string text, bool check)
        {
            tagList.AddItem(text, check, null);
        }   // end of AddItem()

        public void AddItem(string text, bool check, object obj)
        {
            tagList.AddItem(text, check, obj);
        }   // end of AddItem()
        */

        /// <summary>
        /// Initializes the current state of the tag picker to match the passed in tags.
        /// </summary>
        /// <param name="tags"></param>
        public void SetTags(int curTags)
        {
            for (int i = 0; i < tagList.NumItems; i++)
            {
                ModularCheckboxList.CheckboxItem item = tagList.GetItem(i);
                item.Check = (curTags & (int)(Genres)item.Obj) != 0;
            }
        }   // end of SetTags()

        /// <summary>
        /// Returns the current state of the tag picker.
        /// </summary>
        /// <returns></returns>
        public int GetTags()
        {
            int tags = 0;

            for (int i = 0; i < tagList.NumItems; i++)
            {
                ModularCheckboxList.CheckboxItem item = tagList.GetItem(i);
                if (item.Check)
                {
                    tags |= (int)(Genres)item.Obj;
                }
            }

            return tags;
        }   // end of GetTags()

        public void Update(Camera camera, ref Matrix world)
        {
            if (Active)
            {
                tagList.Update(camera, ref world);

                if (Actions.Select.WasPressed)
                {
                    Actions.Select.ClearAllWasPressedState();
                    
                    tagList.CurChecked = !tagList.CurChecked;
                }
                if (Actions.Cancel.WasPressed)
                {
                    Actions.Cancel.ClearAllWasPressedState();

                    tagList.CallOnExit();
                    tagList.Deactivate();
                }
            }

        }   // end of Update()

        public void Render(Camera camera)
        {
            tagList.Render(camera);
        }   // end of Render()

        #endregion

        #region Internal

        public void LoadContent(bool immediate)
        {
            tagList.LoadContent(immediate);
        }

        public void InitDeviceResources(GraphicsDevice device)
        {
            tagList.InitDeviceResources(device);
        }

        public void UnloadContent()
        {
            tagList.UnloadContent();
        }

        /// <summary>
        /// Recreate render targets
        /// </summary>
        /// <param name="graphics"></param>
        public void DeviceReset(GraphicsDevice device)
        {
            BokuGame.DeviceReset(tagList, device);
        }

        #endregion

    }   // end of class TagPicker

}   // end of namespace Boku
