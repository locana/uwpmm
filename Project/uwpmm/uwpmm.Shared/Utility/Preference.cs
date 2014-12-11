using Windows.Storage;

namespace Kazyx.Uwpmm.Utility
{
    public class Preference
    {
        private Preference() { }

        private const string key_sync_postview = "SyncPostview";
        private const string interval_enable_key = "interval_enable";
        private const string interval_time_key = "interval_time";
        private const string display_take_image_button_key = "display_take_image_button";
        private const string display_histogram_key = "display_histogram";
        private const string add_geotag = "add_geotag";
        private const string fraiming_grids = "fraiming_grids";
        private const string framing_grids_color = "framing_grids_color";
        private const string fibonacci_origin = "fibonacci_origin";
        private const string request_focus_frame_info = "request_focus_frame_info";
        private const string prioritize_original_contents = "prioritize_original_contents";

        private static T GetProperty<T>(string key, T defaultValue)
        {
            var settings = ApplicationData.Current.LocalSettings;
            if (!settings.Values.ContainsKey(key))
            {
                settings.Values[key] = defaultValue;
            }
            return (T)settings.Values[key];
        }

        private static void SetProperty<T>(string key, T value)
        {
            var settings = ApplicationData.Current.LocalSettings;
            settings.Values[key] = value;
        }

        public static bool PostviewSyncEnabled
        {
            get { return GetProperty(key_sync_postview, true); }
            set { SetProperty(key_sync_postview, value); }
        }

        public static bool IntervalShootingEnabled
        {
            get { return GetProperty(key_sync_postview, false); }
            set { SetProperty(key_sync_postview, value); }
        }

        public static int IntervalTime
        {
            get { return GetProperty(interval_time_key, 10); }
            set { SetProperty(interval_time_key, value); }
        }

        public static bool ShootButtonVisible
        {
            get { return GetProperty(display_take_image_button_key, true); }
            set { SetProperty(display_take_image_button_key, value); }
        }

        public static bool HistogramVisible
        {
            get { return GetProperty(display_histogram_key, true); }
            set { SetProperty(display_histogram_key, value); }
        }

        public static bool GeoTaggingEnabled
        {
            get { return GetProperty(add_geotag, false); }
            set { SetProperty(add_geotag, value); }
        }

        public static string FramingGridType
        {
            get { return GetProperty(fraiming_grids, FramingGridTypes.Off); }
            set { SetProperty(fraiming_grids, value); }
        }

        public static string FramingGridColor
        {
            get { return GetProperty(framing_grids_color, FramingGridColors.White); }
            set { SetProperty(framing_grids_color, value); }
        }

        public static string FibonacciOrigin
        {
            get { return GetProperty(fibonacci_origin, FibonacciLineOrigins.UpperLeft); }
            set { SetProperty(fibonacci_origin, value); }
        }

        public static bool FocusFrameEnabled
        {
            get { return GetProperty(request_focus_frame_info, true); }
            set { SetProperty(request_focus_frame_info, value); }
        }

        public static bool OriginalSizeContentsPrioritized
        {
            get { return GetProperty(prioritize_original_contents, false); }
            set { SetProperty(prioritize_original_contents, value); }
        }
    }
}
