using Windows.Storage;

namespace Kazyx.Uwpmm.Utility
{
    public class Preference
    {
        private const string key_download_postview = "DownloadPostview";

        public void SetEnablePostviewDownload(bool toDownload)
        {
            var settings = ApplicationData.Current.LocalSettings;
            settings.Values[key_download_postview] = toDownload;
        }

        public bool GetIsDownloadPostviewEnabled()
        {
            var settings = ApplicationData.Current.LocalSettings;
            if (!settings.Values.ContainsKey(key_download_postview))
            {
                SetEnablePostviewDownload(true);
            }
            return (bool)settings.Values[key_download_postview];
        }
    }
}
