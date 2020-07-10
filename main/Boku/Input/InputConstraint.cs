// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System;
using System.Collections.Generic;
using System.Text;

namespace Boku.Input
{
    /// <summary>
    /// This class is used to manage the active input constraints for the input system.  
    /// It is primarily used by the Tutorial (a member of one) to expose the management.
    /// The issue today is that not all of Boku�s user interaction uses one system nor 
    /// do all systems allow this to be used without interjecting specific checks at 
    /// every use location scattering lots of code and definition throughout the code. 
    /// This should be improved.
    /// </summary>
    public class InputConstraint
    {
        public enum RestrictionTypes
        {
            EnableAllExcept, // use the exception list as a set of items to disable
            DisableAllExcept, // use the exception list as a set of items to enable
        };

        /// <summary>
        /// define how the exceptions list is used.  
        /// This allows the tutorial to define the smaller set of items, 
        /// the disabled set or the enabled set.
        /// </summary>
        public RestrictionTypes restriction;
        /// <summary>
        /// This is the list of identifiers for the input exceptions.  
        /// Currently these are strings to allow the most flexibility (late binding) 
        /// of creating tutorials or UI input.  It could be replaced with an enum 
        /// that included an entry for every input description (like NavigateMenuUp, not StickUp).
        /// See Input classes like CommandMap for more details how to define these when 
        /// using that system.
        /// </summary>
        public List<string> exceptions = new List<string>(); 

        /// <summary>
        /// called at the input site to test if the input known by the id is active
        /// </summary>
        /// <param name="id">the identifier of the input</param>
        /// <returns>true if the input is usable</returns>
        public bool UsableCommand( string id )
        {
            bool usable = true;
            if (id != null && this.exceptions.Count > 0)
            {
                bool inList = this.exceptions.Contains(id);
                if (this.restriction == RestrictionTypes.DisableAllExcept)
                {
                    usable = inList;
                }
                else if (this.restriction == RestrictionTypes.EnableAllExcept)
                {
                    usable = !inList;
                }
            }
            return usable;
        }
    }
}
