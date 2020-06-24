using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace KoiX.UI
{
    /// <summary>
    /// Base dialog class for non-modal dialogs.  The only thing
    /// that differentiates modal from non-modal dialogs is this
    /// type.  The DialogManager changes behavior based on this.
    /// </summary>
    public class BaseDialogNonModal : BaseDialog
    {
        #region Members
        #endregion

        #region Accessors
        #endregion

        #region Public

        public BaseDialogNonModal(ThemeSet theme = null)
            : base(theme: theme)
        {
            IsModalDialog = false;
        }

        #endregion

        #region Internal
        #endregion

    }   // end of class BaseDialogNonModal

}   // end of namespace KoiX.UI
