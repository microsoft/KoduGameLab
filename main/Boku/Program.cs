//#define IMPORT_DEBUG

#if EXTERNAL || true
# define GLOBAL_CATCH    // include the global exception handler.
# if XBOX
#  define GLOBAL_CATCH_XBOX
# else
#  if !NETFX_CORE       // Disable global catch for WinRT.  Replace later?  TODO (scoy)
#   define GLOBAL_CATCH_PC
#  endif
# endif
#endif

#if EXTERNAL
# define UPDATE_CHECK    // check for new version at startup.
#endif

//#define DISABLE_STUDIOK 

using System;
using System.Net;
using System.Threading;
using System.Runtime.InteropServices;
using System.IO;
using System.Text;
using System.Diagnostics;
using System.Collections.Generic;
using System.Reflection;
using System.Xml.Serialization;
using BokuShared.Wire;
#if !NETFX_CORE
using System.Globalization;
// <<<<<<<<<<<<<<<<<<<<<<<<<<<<<< FULL SCREEN WINDOWED MODE FIX
using System.Windows.Forms;
// FULL SCREEN WINDOWED MODE FIX >>>>>>>>>>>>>>>>>>>>>>>>>>>>>
#endif

using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Storage;

using Microsoft.Xna.Framework.Input;
using Microsoft.Xna.Framework;

using KoiX.Text;

using Boku.Common;
using Boku.Common.Sharing;
using Boku.Common.Xml;
using Boku.Web;
using Boku.Analyses;

using BokuShared;
using Boku.Common.Localization;

namespace Boku
{
    //Class that holds version information from service. 
    public class UpdateInfo
    {
        public string releaseNotesUrl = "";
        public string updateUrl = "";
        public Version latestVersion;

        //Construct from wire message.
        public UpdateInfo(Message_Version version)
        {
            latestVersion = new Version(version.Major,version.Minor,version.Build,version.Revision);
            releaseNotesUrl = version.ReleaseNotesUrl;
            updateUrl = version.UpdateUrl;
        }

    }
    static partial class Program2
    {
        public static Mutex InstanceMutex;
        static string kOptInForUpdatesFilename = @"Options\1F2B5B79-6EB0-45c4-A8BD-0EBDF4EE10C3.opt";
        static string kOptInForInstrumentationFilename = @"Options\C90D3C0E-D0B4-4aa6-B35D-0A1D9931FB38.opt";

        static string CrashCookieFilename = "Crash.txt";

        public static Version ThisVersion;
        public static string CurrentKCodeVersion="9";   // Version of the KCode.
                                                        // 4 -> 5 : Add local variables and Squash.
                                                        // 5 -> 6 : New movement code.  Make missiles targetable.
                                                        // 6 -> 7 : Add Settings slider tiles as well as some settings as scores.
                                                        // 7 -> 8 : Add naming of characters and the ability to sense named characters.
                                                        // 8 -> 9 : Move linked level target from XmlWorldData to ReflexData.
        
        public static string UpdateCode;

        public static UpdateInfo updateInfo=null;

        public static CmdLine CmdLine;

        public static string MicrobitCmdLine = null;

        public static SiteOptions SiteOptions;

        public static bool InstallerOptCheckForUpdates;
        public static bool InstallerOptSendInstrumentation;

        public static bool bShowVersionWarning = false;

        // If set, then jump directly into this level at startup.
        // This gets set when importing a world.
        public static string StartupWorldFilename;

#if GLOBAL_CATCH_XBOX
        // The XBOX will popup a Guide message box on unhandled exception.
        static bool messageBoxShowing = false;
#endif

        static bool localizedFilesUpdated = false;
        static public void langCallback()
        {
            localizedFilesUpdated = true;
        }

        /// <summary>
        /// The main entry point for the application.
        /// </summary>
#if NETFX_CORE
        static public void Main(string[] args)
#else
        // Must specify STA threading model to be allowed clipboard access.
        [STAThread]
        static public void Main(string[] args)
#endif
        {
#if GLOBAL_CATCH
            try
            {
#endif

#if NETFX_CORE
            Debug.Assert(false, "Need to figure out how to get version info");
            ThisVersion = new Version(1, 4, 182, 0);
            UpdateCode = "055B31F9-07F8-479b-875F-F03279DF595E";
            // Alt approach found on web.  Can it be made to work?
            /*
            var asmName = this.GetType().AssemblyQualifiedName;
            var versionExpression = new System.Text.RegularExpressions.Regex("Version=(?<version>[0-9.]*)");
            var m = versionExpression.Match(asmName);
            string version = String.Empty;
            if (m.Success)
            {
                version = m.Groups["version"].Value;
            }
            */
#else

                ThisVersion = Assembly.GetExecutingAssembly().GetName().Version;
                Assembly asm = Assembly.GetExecutingAssembly();
                var attr = (asm.GetCustomAttributes(typeof(GuidAttribute), true));
                UpdateCode = (attr[0] as GuidAttribute).Value;
#endif

                // Fake command line args to test double-click to launch
                //args = new string[3] { args[0], @"/Import", @"C:\Users\scoy\My Documents\New World 3, by Stephen Coy.Kodu2" };

                CmdLine = new CmdLine(args);

#if !NETFX_CORE
                if (CmdLine.Exists("?") || CmdLine.Exists("HELP"))
                {
                    System.Windows.Forms.MessageBox.Show(
                        "  /FPS \t- display FPS\r\n" +
                        "  /F \t- full screen\r\n" +
                        "  /S \t- sync refresh\r\n" +
                        "  /W 1280 \t- width\r\n" +
                        "  /H 1024 \t- height\r\n" +
                        "  /EFFECTS \t- turn on depth of field and bloom effects\r\n" +
                        "  /NOEFFECTS \t- turn off depth of field and bloom effects\r\n" +
                        "  /NOAUDIO \t- turn off audio\r\n" +
                        "  /PATH <save folder> \t- override save folder\r\n" +
                        "  /UPDATE \t- check for updates\r\n" +
                        "  /NOUPDATE \t- do not check for updates\r\n" +
                        "  /INSTRUMENTATION \t- send usage information\r\n" +
                        "  /NOINSTRUMENTATION \t- do not send usage information\r\n" +
                        "  /IMPORT <filename> \t- unpack the kodu level package to your downloads area\r\n" +
                        "  /LOGON \t- ask player for username\r\n" +
                        "  /ANALYTICS \t- run analytics on game being loaded\r\n" +
                        "  /LOCALIZATION <language> \t- report localization information that is missing in the specified language.\r\n" +
                        "  /PIESIZE <int> \t- pie menu maximum size.\r\n" +
                        "  /NOMICROBIT \t- Do not scan for attached BBC micro:bits\r\n" +
                        "  /MICROBIT \"COM3 E:\"\t- Try to enable micro:bit with given com port and drive letter.  The quotes are required.\r\n" +
                        "");

                    return;
                }
#endif

                {
                    // Initialize level import/export facility
                    // ====================================================
                    // This is done before preventing multiple instances
                    // so that if an import was specified on the command
                    // line then it will be moved to the imports folder,
                    // allowing the already running instance of Kodu to
                    // pick it up the next time the user enters the load
                    // level menu.
                    Storage4.Init();
#if !NETFX_CORE
                    Storage4.StartupDir = Application.StartupPath;

                    // Note, we need to get the user override location before
                    // import otherwise we send the files to the wrong place.
                    // We don't need to do this for WinRT since we can't change
                    // the user location.
                    BokuSettings settings = BokuSettings.Settings;
                    if (!string.IsNullOrEmpty(settings.UserFolder))
                    {
                        Storage4.UserOverrideLocation = settings.UserFolder;
                    }
#endif

                    if (!LevelPackage.Initialize(CmdLine))
                    {
                        // Must be bad folder.
                        return;
                    }

#if !NETFX_CORE
                    // Restore default state for now.
                    Storage4.ResetUserOverrideLocation();
#endif
                    // ====================================================
                }

                // If running Win Store build, see if we need to copy over old levels.
                if(WinStoreHelpers.RunningAsUWP && XmlOptionsData.OldStoreLevelsCopied == false)
                {
                    // Path starts with %LocalAppData% == c:\users\scoy.REDMOND\AppData\Local
                    // Then add Packages\Microsoft.Kodu*\LocalState\Content\Xml\Levels
                    // The * will have to be figured out just by looking at the first bit.  I think it's tied to the user???
                    
                    // Levels\MyWorlds                  *.dds, *.jpg, *.Xml
                    // Levels\MyWorlds\Stuff            *.Xml
                    // Levels\Stuff\TerrainHeightMaps   *.Raw

                    // Wrap everything inside a try/catch.  If anything fails
                    // for any reason we'll just bail and not try again.
                    try
                    {
                        // Try and find path to old levels.
                        string path = "%LocalAppData%\\Packages";
                        path = Environment.ExpandEnvironmentVariables(path);
                        var directories = Directory.EnumerateDirectories(path, "Microsoft.Kodu*");
                        // There should only be one but if we find multiple examples, just try them all.
                        foreach(string dir in directories)
                        {
                            // Figure out source paths for files.
                            string levelsPath = Path.Combine(dir, "LocalState\\Content\\Xml\\Levels");
                            string worldsPath = Path.Combine(levelsPath, "MyWorlds");
                            string stuffPath = Path.Combine(levelsPath, "MyWorlds\\Stuff");
                            string terrainPath = Path.Combine(levelsPath, "Stuff\\TerrainHeightMaps");

                            // Destination paths.
                            string destLevelsPath = Path.Combine(Storage4.UserLocation, "Content\\Xml\\Levels");
                            string destWorldsPath = Path.Combine(destLevelsPath, "MyWorlds");
                            string destStuffPath = Path.Combine(destLevelsPath, "MyWorlds\\Stuff");
                            string destTerrainPath = Path.Combine(destLevelsPath, "Stuff\\TerrainHeightMaps");

                            // Ensure all the destination paths exist.  Note this also creates the intermediate folders.
                            Storage4.CreateDirectory(destStuffPath);
                            Storage4.CreateDirectory(destTerrainPath);

                            // Copy the files.
                            CopyFiles(worldsPath, destWorldsPath, "*.dds");
                            CopyFiles(worldsPath, destWorldsPath, "*.jpg");
                            CopyFiles(worldsPath, destWorldsPath, "*.Xml");
                            CopyFiles(stuffPath, destStuffPath, "*.Xml");
                            CopyFiles(terrainPath, destTerrainPath, "*.Raw");
                            
                        }   // end of loop over directories.
                    }
                    catch(Exception e)
                    {
                        if (e != null)
                        {
                        }
                    }

                    XmlOptionsData.OldStoreLevelsCopied = true;
                }

                {
                    // Prevent multiple instances of Kodu
                    // ====================================================
                    bool instanceMutexCreated;
                    InstanceMutex = new Mutex(false, @"Local\Boku", out instanceMutexCreated);

                    // If we didn't create the shared mutex, then another
                    // instance of Boku already exists.
                    if (!instanceMutexCreated)
                        return;
                    // ====================================================
                }

                {
                    // Load Site Options
                    // ====================================================
                    SiteOptions = SiteOptions.Load(StorageSource.All);
                    // ====================================================
                }

                {
                    // Load the unique site id.
                    // ====================================================
                    SiteID.Initialize();
                    // ====================================================
                }

                {
                    // Process the Import Directive
                    // ====================================================
                    // We're importing a level from the command line. Do the
                    // import and set it as the startup world so that we can
                    // jump right into it.

#if !NETFX_CORE
                    // First, set the userOverrideLocation so we import to the correct location.
                    BokuSettings settings = BokuSettings.Settings;
                    if (!string.IsNullOrEmpty(settings.UserFolder))
                    {
                        Storage4.UserOverrideLocation = settings.UserFolder;
                    }
#endif

                    List<Guid> importedLevels = new List<Guid>();
                    bool importOk = LevelPackage.ImportAllLevels(importedLevels);

                    if (!importOk)
                    {
                        bShowVersionWarning = true;
                    }

#if IMPORT_DEBUG
                LevelPackage.DebugPrint("Done importing");
                LevelPackage.DebugPrint("Files imported");
                foreach (Guid guid in importedLevels)
                {
                    LevelPackage.DebugPrint("    " + guid.ToString());
                }
#endif

                if (importedLevels.Count > 0)
                {
                    StartupWorldFilename = BokuGame.Settings.MediaPath + BokuGame.DownloadsPath + importedLevels[0].ToString() + ".Xml";
#if IMPORT_DEBUG
                    LevelPackage.DebugPrint("StartupWorldFilename : " + StartupWorldFilename);
#endif
                    }
                    // check here for the Analytics flag
                    if (CmdLine.Exists("ANALYTICS"))
                    {
                        //run my code here?
                        //Console.WriteLine("Begin Analytics");
                        //ObjectAnalysis oa = new ObjectAnalysis();
                        //oa.beginAnalysis(MainMenu.StartupWorldFilename.ToString());
                    }

                }

                {
                    // DebugLog.NewRun();

                    // Initialize Localization Resources.
                    Unicode.Init(); // Needed for loading localizations.
                    LocalizationResourceManager.Init();

                    // Update to Latest resources of the Default Language
                    LocalizationResourceManager.UpdateResources(LocalizationResourceManager.DefaultLanguage);

                    // Localization options
                    // ====================================================
                    // Allow command line option to override user choice iff user choise is "".
                    // If XmlOptionsData has a valid choice, always use it.
                    string lang = XmlOptionsData.Language;
                    string commandLineLang = CmdLine.GetString("LOCALIZATION", "");

                    // If we haven't previously set a language preference, select one 
                    // from the current locale.
                    if (string.IsNullOrEmpty(lang))
                    {
                        if (string.IsNullOrEmpty(commandLineLang))
                        {
#if NETFX_CORE                        
                        if (Windows.System.UserProfile.GlobalizationPreferences.Languages.Count > 0)
#endif
                            {
                                try
                                {
                                    // Get current language.
#if NETFX_CORE
                                lang = Windows.System.UserProfile.GlobalizationPreferences.Languages[0];
                                lang = lang.Substring(0, 2);
#else
                                    lang = CultureInfo.CurrentUICulture.TwoLetterISOLanguageName;
#endif

                                    // Verify that it's a supported language.
                                    bool valid = false;
                                    foreach (LocalizationResourceManager.SupportedLanguage supportedLang in LocalizationResourceManager.SupportedLanguages)
                                    {
                                        if (string.Compare(lang, supportedLang.Language, StringComparison.OrdinalIgnoreCase) == 0)
                                        {
                                            valid = true;
                                            break;
                                        }
                                    }

                                    if (!valid)
                                    {
                                        lang = "EN";
                                    }
                                }
                                catch
                                {
                                    lang = "EN";
                                }
                            }
#if NETFX_CORE                        
                        else
                        {
                            lang = "EN";
                        }
#endif
                        }
                        else
                        {
                            lang = commandLineLang;
                        }
                        // Persist language choice.
                        XmlOptionsData.Language = lang;
                    }

                    // Always create missing loc report except when English is the language.
#if NETFX_CORE
                if (string.Compare(lang, "EN", StringComparison.CurrentCultureIgnoreCase) != 0)
#else
                    if (string.Compare(lang, "EN", true) != 0)
#endif
                    {
                        Localizer.ShouldReportMissing = true;
                    }

                    if (!String.IsNullOrEmpty(lang))
                    {
                        Localizer.LocalLanguage = lang;
                        if (lang != LocalizationResourceManager.DefaultLanguage)
                        {
                            localizedFilesUpdated = false;
                            LocalizationResourceManager.UpdateResources(lang, langCallback);

                            while (!localizedFilesUpdated)
                            {
#if !NETFX_CORE
                                Thread.Sleep(10);
#endif
                            }
                        }
                    }

                    // Record current language to instrumentation.
                    if (!String.IsNullOrEmpty(lang))
                    {
                        Instrumentation.RecordDataItem(Instrumentation.DataItemId.Language, lang);
                    }
                }

                {
                    BokuSettings settings = BokuSettings.Settings;

#if NETFX_CORE
                settings.FullScreen = true;
#endif

                    // Apply Settings from the command Line
                    // ====================================================
                    //XmlOptionsData.ShowFramerate = CmdLine.GetBool("FPS", XmlOptionsData.ShowFramerate);
                    settings.FullScreen = CmdLine.GetBool("F", settings.FullScreen);
                    BokuGame.syncRefresh = CmdLine.GetBool("S", BokuGame.syncRefresh);
                    BokuGame.Logon = CmdLine.GetBool("Logon", SiteOptions.Logon);
                    DateTime endMarsMode = new DateTime(2012, 10, 1, 0, 0, 0);
                    if (CmdLine.Exists("MARS") || DateTime.Now < endMarsMode)
                    {
                        BokuGame.bMarsMode = true;
                    }
                    if (CmdLine.Exists("W"))
                    {
                        settings.ResolutionX = CmdLine.GetInt("W", settings.ResolutionX);
                    }
                    if (CmdLine.Exists("H"))
                    {
                        settings.ResolutionY = CmdLine.GetInt("H", settings.ResolutionY);
                    }
                    settings.PostEffects = CmdLine.GetBool("Effects", settings.PostEffects);
                    settings.PostEffects = !CmdLine.GetBool("NoEffects", !settings.PostEffects);
                    settings.LowModels = CmdLine.GetBool("LowModels", settings.LowModels);
                    settings.Audio = !CmdLine.GetBool("NoAudio", !settings.Audio);

                    // Update flags for update checking and instrumentation gathering from both the command line arguments and privacy options chosen during installation.

                    // XmlOptionsData will default to these values if these options have not been overridden in the Options screen.
#if NETFX_CORE
                InstallerOptCheckForUpdates = Storage4.FileExists(kOptInForUpdatesFilename, StorageSource.TitleSpace);
                InstallerOptSendInstrumentation = Storage4.FileExists(kOptInForInstrumentationFilename, StorageSource.TitleSpace);
#else
                    InstallerOptCheckForUpdates = File.Exists(Storage4.TitleLocation + @"\" + kOptInForUpdatesFilename);
                    InstallerOptSendInstrumentation = File.Exists(Storage4.TitleLocation + @"\" + kOptInForInstrumentationFilename);
#endif

#if NETFX_CORE
                // For the WinRT version assume that update notifications
                // are handled by the store.
                SiteOptions.CheckForUpdates = false;
#endif

                    // XmlOptionData.CheckForUpdates combines the installer option
                    // as well as any user override.
                    SiteOptions.CheckForUpdates = XmlOptionsData.CheckForUpdates;

#if !UPDATE_CHECK
                    // Internal builds override this.  Why?
                    SiteOptions.CheckForUpdates = false;
#endif

                    if (XmlOptionsData.SendInstrumentationWasSet)
                    {
                        // Note that this seems inverted because of the stupid naming.
                        SiteOptions.InstrumentationUnchecked = XmlOptionsData.SendInstrumentation;
                    }

                    // Allow command line arguments to override in-game settings.
                    if (CmdLine.Exists("Update"))
                    {
                        SiteOptions.CheckForUpdates = true;
                    }
                    if (CmdLine.Exists("NoUpdate"))
                    {
                        SiteOptions.CheckForUpdates = false;
                    }
                    if (CmdLine.Exists("Instrumentation"))
                    {
                        // Note that this seems inverted because of the stupid naming.
                        SiteOptions.InstrumentationUnchecked = true;
                    }
                    if (CmdLine.Exists("NoInstrumentation"))
                    {
                        // Note that this seems inverted because of the stupid naming.
                        SiteOptions.InstrumentationUnchecked = false;
                    }
                    if (CmdLine.Exists("MICROBIT"))
                    {
                        MicrobitCmdLine = CmdLine.GetString("MICROBIT", null);
                    }

                    /// This is fortuitously timed. We have already pulled the settings file
                    /// out of the real user folder (somewhere in Documents\Saved Games\...).
                    /// If we override the user path now to some central shared spot, we
                    /// get individualized settings from BokuSettings, but then shared levels
                    /// from the central source.
                    string userPath = CmdLine.GetString("PATH", "");
                    if (!string.IsNullOrEmpty(userPath))
                    {
                        settings.UserFolder = userPath;
                    }
                    if (!string.IsNullOrEmpty(settings.UserFolder))
                    {
                        Storage4.UserOverrideLocation = settings.UserFolder;
                    }

#if !NETFX_CORE
                    if (!XmlOptionsData.ShowMicrobitTiles)
                    {
                        // Scan for attached microbits (but don't connect to them yet). If any are found,
                        // RefreshDevices will modify XmlOptionsData to make the microbit programming tiles
                        // permanently visible in the tile picker.
                        Input.MicrobitManager.RefreshDevices(false);
                    }
#endif
                    // ====================================================
                }

                {
                    // Record this installation's unique ID to instrumentation.
                    Instrumentation.RecordDataItem(Instrumentation.DataItemId.InstallationUniqueId, SiteID.Instance.Value.ToString());

#if !NETFX_CORE
                    StartupForm.Startup();
                    StartupForm.EnableCancelButton(false);
                    StartupForm.SetProgressStyle(System.Windows.Forms.ProgressBarStyle.Marquee);
#endif

                    // Get the latest version number.
                    // ====================================================

                    // See if an update is available.
                    if (SiteOptions.CheckForUpdates && !WinStoreHelpers.RunningAsUWP)
                    {
                        FetchLatestVersionFromServer(SiteOptions.Product);

                        var ignoreVersion = new Version(SiteOptions.IgnoreVersion);
                        if (updateInfo != null && ThisVersion < updateInfo.latestVersion
                            && updateInfo.latestVersion != ignoreVersion
                        )
                        {
#if NETFX_CORE
                        // TODO (scoy) Do we have a different version checking scheme for Store Apps?
#else
                            StartupForm.Shutdown();

                            var updateForm = new UpdateForm();

                            //Localized update dialog.
                            updateForm.Text = Strings.Localize("Update.FormTitle");

                            var text = Strings.Localize("Update.UpdateMessage");
                            updateForm.MessageLabel.Text = text.Replace("^", "");//Remove link delimiters.
                            updateForm.MessageLabel.LinkArea = new System.Windows.Forms.LinkArea(text.IndexOf("^"), text.LastIndexOf("^") - text.IndexOf("^") - 1); //Set link area based on ^ delimiters.

                            text = Strings.Localize("Update.ReleaseNotesMessage");
                            updateForm.RelaseNotesLabel.Text = text.Replace("^", "");//Remove link delimiters.
                            updateForm.RelaseNotesLabel.LinkArea = new System.Windows.Forms.LinkArea(text.IndexOf("^"), text.LastIndexOf("^") - text.IndexOf("^") - 1);//Set link area based on ^ delimiters.

                            updateForm.CurrentVersionLabel.Text = Strings.Localize("Update.CurrentVersion");
                            updateForm.NewVersionLabel.Text = Strings.Localize("Update.LatestVersion");

                            updateForm.UpdateButton.Text = Strings.Localize("Update.UpdateButtonText");
                            updateForm.IgnoreButton.Text = Strings.Localize("Update.IgnoreButtonText");
                            updateForm.RemindButton.Text = Strings.Localize("Update.RemindButtonText");

                            //Set version info in dialog.
                            updateForm.CurrentVersion.Text = ThisVersion.ToString();
                            updateForm.NewVersion.Text = updateInfo.latestVersion.ToString();

                            //Setup links in dialog from UpdateInfo.
                            updateForm.RelaseNotesLabel.Links[0].LinkData = updateInfo.releaseNotesUrl;
                            updateForm.RelaseNotesLabel.LinkClicked += (s, e) =>
                            {
                                System.Diagnostics.Process.Start(e.Link.LinkData.ToString());
                            };
                            updateForm.MessageLabel.Links[0].LinkData = SiteOptions.KGLUrl;
                            updateForm.MessageLabel.LinkClicked += (s, e) =>
                            {
                                System.Diagnostics.Process.Start(e.Link.LinkData.ToString());
                            };

                            var dialogResult = updateForm.ShowDialog();

                            if (dialogResult == System.Windows.Forms.DialogResult.Yes)
                            {
                                //Show update page and exit.
                                Process.Start(updateInfo.updateUrl);
                                Process.GetCurrentProcess().Kill();
                            }

                            if (dialogResult == System.Windows.Forms.DialogResult.Ignore)
                            {
                                //Write ignore version to options.
                                SiteOptions.IgnoreVersion = updateInfo.latestVersion.ToString();
                                SiteOptions.Save();
                            }

#endif
                        }
                    }

#if !NETFX_CORE
                    StartupForm.SetStatusText("Starting up...");
#endif

                    // ====================================================

#if NETFX_CORE
                {
                    var factory = new MonoGame.Framework.GameFrameworkViewSource<BokuGame>();
                    Windows.ApplicationModel.Core.CoreApplication.Run(factory);
                }
#else

                    // TODO (scoy) *** See notes!!!!
                    // Consider starting MainForm here and putting init of BokuGame into XNAControl.
                    // Do we still need/want StartForm?
                    //BokuGame game = new BokuGame();

                    // Move these to be called from XNAControl so that device and content manager exist first?!?
                    //BokuGame.bokuGame.Initialize();
                    //BokuGame.bokuGame.LoadContent();
                    //BokuGame.bokuGame.BeginRun();

                    if (WinStoreHelpers.RunningAsUWP)
                    {
                        string applicationDataPath = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
                    }

                    StartupForm.Shutdown();
                    Application.Run(MainForm.Instance);


                    /*
                    using (BokuGame game = new BokuGame())
                    {
                        //try
                        {
                            game.Run();
                        }
                        //catch (Exception ex)
                        {
                        //    Console.WriteLine(ex.InnerException);
                        }
                    }
                    */

#endif

#if !NETFX_CORE
                    // In case the app was closed while in play mode with a microbit attached. Release microbits
                    // so that the serial port receive thread doesn't block application exit.
                    Boku.Input.MicrobitManager.ReleaseDevices();
#endif

                    FlushInstrumentation();

                    // ====================================================
                }
#if GLOBAL_CATCH
            }
            catch (Exception ex)
            {
                // For both Xbox and PC write out a file to act as the crash cookie.
                {
                    Stream stream = Storage4.OpenWrite(CrashCookieFilename);
                    byte[] buffer = { 42 };
                    stream.Write(buffer, 0, 1);
                    stream.Close();
                }

                
#if !XBOX
                // Be sure mouse cursor is on regardless of current input mode.
                BokuGame.bokuGame.IsMouseVisible = true;

                StartupForm.Shutdown();
                
                // Show the crash report dialog box unless we're running the debugger.
                if (!Debugger.IsAttached)
                {

                    string gfxString;
                    try
                    {
                        gfxString = String.Format("Adapter: {0}", GraphicsAdapter.DefaultAdapter.Description);
                    }
                    catch
                    {
                        gfxString = "(Error getting graphics adapter information)";
                    }

                    // On PC, show the crash report dialog box.

                    string errorReport =
                        ex.Message + "\r\n" +
                        ThisVersion.ToString() + "\r\n" +
                        gfxString + "\r\n\r\n" +
                        ex.StackTrace;
                    ErrorForm errorForm = new ErrorForm();
                    errorForm.textBoxError.Text = errorReport;
                    if (System.Windows.Forms.DialogResult.OK == errorForm.ShowDialog())
                    {
                        string addInfo =
                            ex.GetType().Name + "\r\n" +
                            "Kodu: " + ThisVersion.ToString() + "\r\n" +
                            gfxString + "\r\n" +
                            "WLID: " + errorForm.textBoxLiveId.Text + "\r\n\r\n" + 
                            errorForm.textBoxAddInfo.Text;
                        SendErrorReport(ex.Message, ex.StackTrace, addInfo);
                    }

                    Process.GetCurrentProcess().Kill();
                }
#else // !XBOX
                // On XBOX, show the error in a Guide message box unless we're running the debugger.
                if (GamerServices.IsInitialized && !Debugger.IsAttached)
                {
                    // The Guide message box only supports messages up to 255 chars in length.
                    string msg;

                    if(ex is System.IO.IOException)
                    {
                        msg = Strings.Localize("error.outOfDiskSpace");
                    }   
                    else
                    {
                        if (ex.StackTrace.Length > 255)
                            msg = ex.StackTrace.Substring(0, 255);
                        else
                            msg = ex.StackTrace;
                    }

                    List<string> buttons = new List<string>();
                    buttons.Add("OK");

                    messageBoxShowing = true;
                    Guide.BeginShowMessageBox(
                        ex.Message,
                        msg,
                        buttons,
                        0,
                        Microsoft.Xna.Framework.GamerServices.MessageBoxIcon.Error,
                        CrashMessageBoxClosed,
                        null);

                    // Hang out till the message box closes.
                    while (messageBoxShowing)
                    {
                        Thread.Sleep(1);
                    }
                }
                else
                {
                    // GamerServices was not initialized at the time of the crash, so forward it to the OS.
                    throw;
                }
#endif // !XBOX
            }
#endif // GLOBAL_CATCH

            // Prevent the garbage collector from optimizing away our shared mutex instance.
            GC.KeepAlive(InstanceMutex);

        }   // end of Main()

        /// <summary>
        /// Copies any files that match the searchPattern string from src to dst.
        /// </summary>
        /// <param name="srcDir"></param>
        /// <param name="dstDir"></param>
        /// <param name="searchPattern"></param>
        static void CopyFiles(string srcDir, string dstDir, string searchPattern)
        {
            try
            {
                string[] filePaths = Directory.GetFiles(srcDir, searchPattern);

                foreach (string srcPath in filePaths)
                {
                    string dstPath = Path.Combine(dstDir, Path.GetFileName(srcPath));
                    File.Copy(srcPath, dstPath);
                }
            }
            catch (Exception e)
            {
                // If the file has already been copied over, this will throw.
                // No worries...
                if (e != null)
                {
                }
            }
        }   // end of CopyFiles()

    }   // end of class Program2

    /// This chunk of the Program class manages the task of fetching the latest
    /// version number from the server to determine whether an update is available.
    static partial class Program2
    {
        static bool getCurrentVersionComplete = false;
        private static void FetchLatestVersionFromServer(string productName)
        {
#if !NETFX_CORE
            StartupForm.EnableCancelButton(false);
            StartupForm.SetProgressStyle(System.Windows.Forms.ProgressBarStyle.Marquee);
            StartupForm.SetStatusText("Checking for updates...");
#endif

            try
            {
                Web.Trans.GetCurrentVersion trans = new Boku.Web.Trans.GetCurrentVersion(productName, GetCurrentVersionCallback, null);

                if (trans.Send())
                {
                    int timeSpent = 0;
                    while (!getCurrentVersionComplete && timeSpent < 30 * 1000)
                    {
                        // Pump web request callbacks.
                        Web.Trans.Request.Update();
#if NETFX_CORE
                        {
                            System.Threading.Tasks.Task delayTask = System.Threading.Tasks.Task.Delay(10);
                            delayTask.ConfigureAwait(false);
                            delayTask.Wait();
                        }
#else
                        System.Threading.Thread.Sleep(10);
#endif
                        timeSpent += 10;
                    }
                }
            }
            catch
            {
                updateInfo = null;
                getCurrentVersionComplete = true;
            }
        }
        static void GetCurrentVersionCallback(object param)
        {
            Web.Trans.GetCurrentVersion.Result result = (Web.Trans.GetCurrentVersion.Result)param;

            if (result.success)
            {
                updateInfo = new UpdateInfo(result.version);
            }

            getCurrentVersionComplete = true;
        }
    }



    /// This chunk of the Program class manages the task of sending crash reports and instrumentation.
    static partial class Program2
    {
        static bool instrumentationFlushed = false;
        static void InstrumentationFlushed(object param)
        {
            instrumentationFlushed = true;
        }

        static void FlushInstrumentation()
        {
            try
            {
                if (SiteOptions.Instrumentation)
                {
                    int timeSpent = 0;
                    if (Common.Instrumentation.Flush(InstrumentationFlushed))
                    {
                        // Give it 30 seconds to complete.
                        while (!instrumentationFlushed && timeSpent < 30 * 1000)
                        {
                            // Pump web request callbacks.
                            Web.Trans.Request.Update();
#if NETFX_CORE
                            {
                                System.Threading.Tasks.Task delayTask = System.Threading.Tasks.Task.Delay(10);
                                delayTask.ConfigureAwait(false);
                                delayTask.Wait();
                            }
#else
                            System.Threading.Thread.Sleep(10);
#endif
                            timeSpent += 10;
                        }
                    }
                }
            }
            catch { }
        }

#if GLOBAL_CATCH
        static bool errorReportSent = false;

        static void ErrorReportSent(object param)
        {
            errorReportSent = true;
        }

        static void SendErrorReport(string errorMessage, string stackTrace, string addInfo)
        {
            try
            {
                Web.Trans.ReportError trans = new Web.Trans.ReportError(
                    errorMessage,
                    stackTrace,
                    addInfo,
                    ErrorReportSent,
                    null);

                if (trans.Send())
                {
                    int timeSpent = 0;
                    while (!errorReportSent && timeSpent < 30 * 1000)
                    {
                        // Pump web request callbacks.
                        Web.Trans.Request.Update();
                        System.Threading.Thread.Sleep(10);
                        timeSpent += 10;
                    }
                }
            }
            catch { }
        }
#endif
    }

}   // end of namespace Boku
