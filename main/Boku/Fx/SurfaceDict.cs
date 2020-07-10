// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System;
using System.Collections.Generic;

namespace Boku.Fx
{
    public class SurfaceDict : BokuShared.XmlData<SurfaceDict>
    {
        #region Members

        private List<Surface> surfaces = new List<Surface>();

        #endregion Members

        #region Accessors

        /// <summary>
        /// The raw list. Don't search through this, use lookup functions below.
        /// </summary>
        public List<Surface> Surfaces
        {
            get { return surfaces; }
            set { surfaces = value; }
        }

        #endregion Accessors

        #region Public

        public SurfaceDict()
        {
        }

        /// <summary>
        /// Look up a surface index by name (after we've been loaded).
        /// Will return null for an empty string name. Will throw an
        /// exception for an unfound name.
        /// </summary>
        /// <param name="name"></param>
        /// <returns></returns>
        public int IndexOf(string name)
        {
            if (!string.IsNullOrEmpty(name))
            {
                search_scratch.Name = name;
                return surfaces.BinarySearch(search_scratch);
            }
            return -1;
        }
        /// <summary>
        /// Look up a surface by name (after we've been loaded).
        /// Will return null for an empty string name. Will throw an
        /// exception for an unfound name.
        /// </summary>
        /// <param name="name"></param>
        /// <returns></returns>
        public Surface Surface(string name)
        {
            int idx = IndexOf(name);
            if (idx >= 0)
            {
                return surfaces[idx];
            }
            return null;
        }
        /// <summary>
        /// Helper scratch object to search for.
        /// </summary>
        private Surface search_scratch = new Surface();
        #endregion Public

        #region Internal
        protected override bool OnLoad()
        {
            Surfaces.Sort();

            return true;
        }

        #endregion Internal
    }
}
