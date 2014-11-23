using System;
using System.Collections.Generic;
using System.Text;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Media;

namespace Kazyx.Uwpmm.Utility
{
    public static class ResourceManager
    {
        public static Brush AccentColorBrush
        {
            get { return (Brush)Application.Current.Resources["PhoneAccentBrush"]; }
        }
    }
}
