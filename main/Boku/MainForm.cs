
using System.Windows.Forms;

namespace Boku
{
    // System.Drawing and the XNA Framework both define Color types.
    // To avoid conflicts, we define shortcut names for them both.
    using GdiColor = System.Drawing.Color;
    using XnaColor = Microsoft.Xna.Framework.Color;

    /// <summary>
    /// Custom form provides the main user interface for the program.
    /// In this sample we used the designer to add a splitter pane to the form,
    /// which contains a SpriteFontControl and a SpinningTriangleControl.
    /// </summary>
    public partial class MainForm : Form
    {
        static MainForm _instance;
        static public MainForm Instance
        {
            get
            {
                if (_instance == null)
                {
                    _instance = new MainForm();
                }

                return _instance;
            }
        }

        XNAControl xnaControl;

        public MainForm()
        {
            InitializeComponent();

            PostInit();

            ConnectEventHandlers();

            _instance = this;

        }

        void InitializeComponent()
        {
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(MainForm));

            this.xnaControl = new Boku.XNAControl();
            this.SuspendLayout();

            // 800x600 is our minimum allowable size.
            System.Drawing.Size size = new System.Drawing.Size(800, 600);

            // 
            // xnaControl
            // 
            this.xnaControl.Location = new System.Drawing.Point(0, 0);
            this.xnaControl.Name = "xnaControl";
            this.xnaControl.Size = size;
            this.xnaControl.TabIndex = 2;
            this.xnaControl.Text = "XNAControl";
            this.xnaControl.Click += new System.EventHandler(this.xnaControl1_Click);
            // 
            // MainForm
            // 
            this.Icon = ((System.Drawing.Icon)(resources.GetObject("$this.Icon")));
            this.ClientSize = size;
            this.WindowState = FormWindowState.Maximized;
            this.Controls.Add(this.xnaControl);
            this.MinimumSize = new System.Drawing.Size(size.Width + 16, size.Height + 39);
            this.Name = "MainForm";

            this.ResumeLayout(false);

            // To handle Alt-Enter for full screen toggle.
            this.KeyPreview = true; 
            this.KeyDown += new KeyEventHandler(MainForm_KeyDown);

            //this.ResizeRedraw = false;
            this.ResizeEnd += new System.EventHandler(ResizeEndHandler);
        }

        /// <summary>
        /// Just look for alt-enter to handle fullscreen toggle.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        void MainForm_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Alt && e.KeyValue == 13)
            {
                ToggleFullScreen();
                e.Handled = true;
            }
        }

        System.Drawing.Rectangle winBounds;
        bool fullScreen = false;
        public void ToggleFullScreen()
        {
            fullScreen = !fullScreen;
            if (fullScreen)
            {
                // Save bounds so we restore to the same size.
                winBounds = this.Bounds;

                // Enter fullscreen mode.
                this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.None;
                this.Bounds = Screen.PrimaryScreen.Bounds;
                this.WindowState = FormWindowState.Maximized;
                this.TopMost = true;
            }
            else
            {
                // Restore windowed mode.
                this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.Sizable;
                this.WindowState = FormWindowState.Normal;
                this.Bounds = winBounds;
                this.TopMost = false;
            }
        }   // end of ToggleFullScreen()

        void PostInit()
        {
            this.ClientSizeChanged += clientSizeChanged;

        }

        void xnaControl1_Click(object sender, System.EventArgs e)
        {

        }

        void ResizeEndHandler(object sender, System.EventArgs e)
        {

        }
    }
}
