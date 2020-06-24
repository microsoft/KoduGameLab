using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;

#if NETFX_CORE
#else
    using System.Data;
    using System.Drawing;
    using System.Windows.Forms;
#endif

namespace Boku
{
    // (TODO (scoy) BROKEN
#if !NETFX_CORE
    public partial class LoginDialog : Form
    {
        public LoginDialog()
        {
            InitializeComponent();
        }
    }
#endif
}
