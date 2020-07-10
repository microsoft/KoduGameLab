// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System;
using Boku.Common.Sharing;
using BokuShared;
using System.Linq;

namespace Boku.Common
{
    /// <summary>
    /// A filter that matches all levels.
    /// </summary>
    public class LevelSetFilterByKeywords : LevelSetFilterByGenre
    {
        private string searchString = "";

        public string SearchString
        {
            get { return searchString; }
            set
            {
                if (value != searchString)
                {
                    searchString = value;
                    Dirty = true;
                } 
            }
        }

        override public bool Matches(LevelMetadata item)
        {
            //check for server side matching 
            if (ServerSideMatching)
            {
                return true;
            }

            //check if Genre matches first
            if (!base.Matches(item))
                return false;

            var words = searchString.ToLower().Split(new[] { ' ' }, System.StringSplitOptions.RemoveEmptyEntries);

            if (words == null || words.Length < 1)
            {
                return true; //no keywords so match everything.
            }

            //now match keywords
            return words.All(word => item.Description.ToLower().Contains(word) 
                || item.Name.ToLower().Contains(word) 
                || item.Creator.ToLower().Contains(word));

        }
    }
}
