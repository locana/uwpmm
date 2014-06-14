using Windows.ApplicationModel.Resources;

namespace Kazyx.Uwpmm.Utility
{
    public class SystemUtil
    {
        public static string GetStringResource(string key)
        {
            return ResourceLoader.GetForCurrentView().GetString(key);
        }
    }
}
