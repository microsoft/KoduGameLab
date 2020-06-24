using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.Drawing;
using System.Text;
using System.Windows.Forms;
using System.IO;

using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

using Boku;

namespace BokuPreBoot
{
    public partial class MainWindow : Form
    {
        private GraphicsDeviceService graphicsSvc;
        private SiteOptions siteOptions;

        /* Required functionality for Windows Forms initialization */
        public MainWindow()
        {
            InitializeComponent();                                              // required winforms boilerplate

            graphicsSvc = GraphicsDeviceService.AddRef(this.Handle, 10, 10);    // get an XNA graphics device for later use

            siteOptions = SiteOptions.Load(Boku.Common.StorageSource.TitleSpace | Boku.Common.StorageSource.UserSpace);

            ConstrainToHardwareCapabilities();
            CopySettingsToUI();
        }

        /* If user presses the cancel button, just close without saving */
        private void CancelBtn_Click(object sender, EventArgs e)
        {
            Close();
        }

        // TODO (scoy) Remove this.
        /* Perform a load-time init of the UI based on whether the hardware is *capable* of
         * shader model 3. Note that there are subsequent constraint passes if a user
         * chooses shader model 2 explicitly. This one only relates to hardware constraint.
         * 
         * This is done before the settings are loaded into the UI, so we will also
         * constrain the settings before they are used to intialize the UI
         */
        private static bool hwSupportsHiDef = false;
        private void ConstrainToHardwareCapabilities()
        {
            // Determine is current hardware supports HiDef.
            foreach (GraphicsAdapter ga in GraphicsAdapter.Adapters)
            {
                if (ga.IsDefaultAdapter)
                {
                    if (ga.IsProfileSupported(GraphicsProfile.Reach))
                    {
                        hwSupportsHiDef = false;
                    }
                    if (ga.IsProfileSupported(GraphicsProfile.HiDef))
                    {
                        hwSupportsHiDef = true;
                    }

                    break;
                }
            }

            // Adjust UI to match.
            if (hwSupportsHiDef)
            {
                StatusTB.Text = @"This computer supports HiDef. All graphics options are available.";
            }
            else
            {
                StatusTB.Text = @"This computer does not support HiDef; only Standard graphics options will be available, and Advanced features will be disabled.";
                ShadMod3RB.Enabled = false;
                // We don't actually set the checked / unchecked state of any UI; instead we constrain the 
                // settings file, and it will set all the control values properly.
                BokuSettings.ConstrainToReach();
            }

        }   // end of ConstrainToHardwareCapabilities()

        /* Flag to indicate the UI control changes are coming from a load and not from the user. */
        static bool isLoading;
        /* Change UI state to match a settings object. */
        private void CopySettingsToUI()
        {
            isLoading = true;       // to supress validation

            BokuSettings settings = BokuSettings.Settings;          // abbreviate for readability

            if (settings.PreferReach || !hwSupportsHiDef)
            {
                ShadMod2RB.Checked = true;
            } else {
                ShadMod3RB.Checked = true;
            }

            PostFXCk.Checked = settings.PostEffects;
            AntiAliasCk.Checked = settings.AntiAlias;
            AnimationCk.Checked = settings.Animation;

            AudioCk.Checked = settings.Audio;
            communityCk.Checked = siteOptions.CommunityEnabled;

            InitUserFolder(settings);

            VsyncCk.Checked = settings.Vsync;
            SpriteFontCk.Checked = !settings.UseSystemFontRendering;

            isLoading = false;
        }

        private void CopyUIToSettings()
        {
            BokuSettings settings = BokuSettings.Settings;

            settings.PreferReach = ShadMod2RB.Checked;

            settings.PostEffects = PostFXCk.Checked;
            settings.AntiAlias = AntiAliasCk.Checked;
            settings.Animation = AnimationCk.Checked;

            settings.PreferReach = ShadMod2RB.Checked;

            settings.Audio = AudioCk.Checked;
            siteOptions.CommunityEnabled = communityCk.Checked;

            settings.UserFolder = GetUserFolder();

            settings.Vsync = VsyncCk.Checked;
            settings.UseSystemFontRendering = !SpriteFontCk.Checked;
        }

        // Gather up the UI state and persist to the settings file
        private void SaveSettings()
        {
            CopyUIToSettings();
            BokuSettings.Save();
            siteOptions.Save();
        }

        private void OkBtn_Click(object sender, EventArgs e)
        {
            Launch();
        }

        private void Launch()
        {
            string programs = System.Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles);
            string progDir = programs + "\\Microsoft Research\\Boku\\";
            string curDir = System.Environment.CurrentDirectory + "\\";
            // the app might be located in our current directory
            string appLocal = curDir + "Boku.exe";
            // otherwise look for the app in the installed programs directory
            string appInstalled = progDir + "Boku.exe";

            string app = null;
            string dir = null;

            if (System.IO.File.Exists(appLocal))
            {
                dir = curDir;
                app = appLocal;
            }
            else if (System.IO.File.Exists(appInstalled))
            {
                dir = progDir;
                app = appInstalled;
            }
            if (null != app)
            {
                SaveSettings();

                Process bokuProc = new Process();

                bokuProc.StartInfo.FileName = app;
                bokuProc.StartInfo.WorkingDirectory = dir;

                bokuProc.Start();

                Close();
            }
        }

        private void Apply_Click(object sender, EventArgs e)
        {
            SaveSettings();
            Close();
        }

        private void ShadMod2RB_CheckedChanged(object sender, EventArgs e)
        {
            // note that this will be called whether the 3 or 2 button was pressed
            // because they are radio buttons and change in tandem.
            bool sm2Selected = ShadMod2RB.Checked;

            if (sm2Selected)
            {
                PostFXCk.Checked = false;       // and turn clear the check, if any
                PostFXCk.Enabled = false;       // disable post effects checkbox

                if (!isLoading)
                {
                    // if we're loading, the incoming set will already be constrained elsewhere
                    // if the user, however, is setting shader model 2, we need to constrain appropriately
                    CopyUIToSettings();
                    
                    // TODO (scoy)  Figure out where to check for Reach v HiDef hw and constrain here.
                    //BokuSettings.ConstrainToShaderModel2();

                    CopySettingsToUI();
                }
            }
            else
            {
                PostFXCk.Enabled = true;        // enable post-processing effects
            }
        }

        #region User Folder Override
        private static FolderBrowserDialog folderDialog = new FolderBrowserDialog();
        private static string userFolder = "";
        public static string UserFolderPublic
        {
            get { return userFolder; }
        }
        /// <summary>
        /// Callback for the file selection dialog.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void UserFolder_Click(object sender, EventArgs e)
        {
            if (folderDialog.ShowDialog() == DialogResult.OK)
            {
                userFolder = folderDialog.SelectedPath;
                SavePath.Text = userFolder;
            }
        }

        /// <summary>
        /// Initialize user folder, either disabling or setting up.
        /// </summary>
        /// <param name="settings"></param>
        private void InitUserFolder(BokuSettings settings)
        {
            /*
            if (!siteOptions.UserFolder)
            {
                UserFolder.Visible = false;
                SavePath.Visible = false;
            }
            else
            */
            {
                folderDialog.ShowNewFolderButton = true;
                folderDialog.Description = "Optional save folder override";
                if (settings.UserFolder == "")
                {
                    folderDialog.SelectedPath = DefaultUserFolder();
                }
                else
                {
                    folderDialog.SelectedPath = settings.UserFolder;
                }
                SavePath.Text = folderDialog.SelectedPath;
            }
        }
        /// <summary>
        /// Default value for the user folder. This is never written, just stored as empty string.
        /// </summary>
        /// <returns></returns>
        private static string DefaultUserFolder()
        {
            string folder = Boku.Common.Storage4.UserLocation;
            return folder;
        }
        /// <summary>
        /// Return either a folder if different from default, or empty string for default.
        /// </summary>
        /// <returns></returns>
        private static string GetUserFolder()
        {
            if (userFolder != DefaultUserFolder())
                return userFolder;
            else
                return "";
        }

        /// <summary>
        /// Text box to allow cut and paste version of folder selection.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void SavePath_TextChanged(object sender, EventArgs e)
        {
            if (SavePath.Text == "")
            {
                SavePath.Text = DefaultUserFolder();
            }
            userFolder = SavePath.Text;
            folderDialog.SelectedPath = userFolder;
        }
        #endregion User Folder Override

    }
}