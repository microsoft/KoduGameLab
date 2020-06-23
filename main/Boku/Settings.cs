namespace Boku.Properties {
    
    
    // This class allows you to handle specific events on the settings class:
    //  The SettingChanging event is raised before a setting's value is changed.
    //  The PropertyChanged event is raised after a setting's value is changed.
    //  The SettingsLoaded event is raised after the setting values are loaded.
    //  The SettingsSaving event is raised before the setting values are saved.
    internal sealed partial class Settings {
#if NETFX_CORE
        // Note, adding this here means that it's no longer configurable.
        public string MediaPath = @"Content\";

        private static Settings defaultInstance = new Settings();

        public static Settings Default
        {
            get
            {
                return defaultInstance;
            }
        }

#endif
    }
}
