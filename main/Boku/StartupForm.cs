using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Text;
using System.Threading;
using System.Diagnostics;

#if NETFX_CORE
#else
    using System.Data;
    using System.Drawing;
    using System.Windows.Forms;
#endif

using Boku.Common;

namespace Boku
{
    // (TODO (****) BROKEN
#if !NETFX_CORE
    public partial class StartupForm : Form
    {
        static AutoResetEvent signal;
        static StartupForm form;

        private StartupForm()
        {
            InitializeComponent();
        }

        public static void Startup()
        {
            if (form == null)
            {
                signal = new AutoResetEvent(false);

                Thread thread = new Thread(new ThreadStart(FormProc));
                thread.Start();

                signal.WaitOne();
            }
        }

        public static void Shutdown()
        {
            try
            {
                if (form != null && !form.IsDisposed)
                {
                    form.Invoke(new MethodInvoker(form.Close));
                    signal.WaitOne();
                }
            }
            catch { }
        }

        public static void SetStatusText(string text)
        {
            if (form != null)
            {
                form.Invoke(new SetStringDelegate(form.ISetStatusText), new object[] { text });
            }
        }

        public static void SetProgressStyle(ProgressBarStyle style)
        {
            if (form != null)
            {
                form.Invoke(new SetIntDelegate(form.ISetProgressStyle), new object[] { (int)style });
            }
        }

        public static void SetProgressValue(int value)
        {
            if (form != null)
            {
                form.Invoke(new SetIntDelegate(form.ISetProgressValue), new object[] { value });
            }
        }

        public static void IncProgressValue(int amount)
        {
            if (form != null)
            {
                form.Invoke(new SetIntDelegate(form.IIncProgressValue), new object[] { amount });
            }
        }

        public static void SetProgressMax(int value)
        {
            if (form != null)
            {
                form.Invoke(new SetIntDelegate(form.ISetProgressMax), new object[] { value });
            }
        }

        public static void EnableCancelButton(bool enabled)
        {
            if (form != null)
            {
                form.Invoke(new SetBoolDelegate(form.IEnableCancelButton), new object[] { enabled });
            }
        }

        delegate void SetStringDelegate(string text);
        delegate void SetIntDelegate(int value);
        delegate void SetBoolDelegate(bool value);

        void ISetStatusText(string text)
        {
            try
            {
                System.Console.WriteLine(text);
                progressLabel.Text = text;
            }
            catch { }
        }

        void ISetProgressStyle(int style)
        {
            try
            {
                ProgressBarStyle newStyle = (ProgressBarStyle)style;
                progressBar.Style = newStyle;
            }
            catch { }
        }

        void ISetProgressValue(int value)
        {
            try
            {
                progressBar.Value = value;
            }
            catch { }
        }

        void IIncProgressValue(int amount)
        {
            try
            {
                progressBar.Value += amount;
            }
            catch { }
        }

        void ISetProgressMax(int value)
        {
            try
            {
                progressBar.Maximum = value;
            }
            catch { }
        }

        void IEnableCancelButton(bool enabled)
        {
            cancelButton.Enabled = enabled;
            cancelButton.Visible = enabled;
        }

        static void FormProc()
        {
            form = new StartupForm();

            Application.EnableVisualStyles();
            Application.Run(form);

            form = null;
            
            signal.Set();
        }

        private void UpdateForm_Activated(object sender, EventArgs e)
        {
            signal.Set();
        }

        private void cancelButton_Click(object sender, EventArgs e)
        {
            cancelButton.Enabled = false;
            cancelButton.Visible = false;
            progressLabel.Text = "Canceling...";
        }

        private void StartupForm_Load(object sender, EventArgs e)
        {
            this.Text = Strings.Localize("shareHub.appName") + " (" + Program2.ThisVersion.ToString() + ", " + Program2.SiteOptions.Product + ")";
        }
    }
#endif
}