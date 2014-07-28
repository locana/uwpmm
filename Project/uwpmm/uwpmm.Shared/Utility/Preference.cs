using Windows.Storage;

namespace Kazyx.Uwpmm.Utility
{
    public class Preference
    {
        private const string key_sync_postview = "SyncPostview";

        public bool PostviewSyncEnabled
        {
            get
            {
                var settings = ApplicationData.Current.LocalSettings;
                if (!settings.Values.ContainsKey(key_sync_postview))
                {
                    settings.Values[key_sync_postview] = true; // Enabled by default
                }
                return (bool)settings.Values[key_sync_postview];
            }
            set
            {
                var settings = ApplicationData.Current.LocalSettings;
                settings.Values[key_sync_postview] = value;
            }
        }
    }
}
