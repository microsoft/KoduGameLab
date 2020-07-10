// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

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
    // (TODO (****) BROKEN
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
