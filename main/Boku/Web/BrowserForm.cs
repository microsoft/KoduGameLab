
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

namespace Boku.Web
{

    // (TODO (****) BROKEN
#if !NETFX_CORE
    public partial class BrowserForm : Form
    {

        public System.Windows.Forms.WebBrowser WebBrowser
        {
            get { return webBrowser1; }
        }

        public BrowserForm()
        {
            InitializeComponent();
        }
    }
#endif
}
