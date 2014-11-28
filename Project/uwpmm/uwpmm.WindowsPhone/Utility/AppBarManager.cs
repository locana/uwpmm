using System;
using System.Collections.Generic;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Media;

namespace Kazyx.Uwpmm.Utility
{
    public class CommandBarManager
    {

        public CommandBarManager()
        {
            this.Init();
        }

        public CommandBar bar = new CommandBar();

        readonly AppBarButton CameraSettingButton = new AppBarButton() { Icon = new BitmapIcon() { UriSource = new Uri("ms-appx:///Assets/AppBar/appbar_cameraSetting.png", UriKind.Absolute) }, Label = "Camera settings" };
        readonly AppBarButton AppSettingButton = new AppBarButton() { Icon = new BitmapIcon() { UriSource = new Uri("ms-appx:///Assets/AppBar/feature.settings.png", UriKind.Absolute) }, Label = "Application settings" };
        readonly AppBarButton CancelTouchFocusButton = new AppBarButton() { Icon = new BitmapIcon() { UriSource = new Uri("ms-appx:///Assets/AppBar/appbar_cancel.png", UriKind.Absolute) }, Label = "Cancel touch AF" };
        readonly AppBarButton AboutPageButton = new AppBarButton() { Label = "About this application" };
        readonly AppBarButton LoggerPageButton = new AppBarButton() { Label = "Logger" };
        readonly AppBarButton PlaybackPageButton = new AppBarButton() { Label = "Playback mode" };

        readonly Dictionary<AppBarItem, AppBarButton> AppBarItems = new Dictionary<AppBarItem, AppBarButton>();
        readonly Dictionary<AppBarItemType, SortedSet<AppBarItem>> EnabledItems = new Dictionary<AppBarItemType, SortedSet<AppBarItem>>();

        void Init()
        {
            EnabledItems.Add(AppBarItemType.Primary, new SortedSet<AppBarItem>());
            EnabledItems.Add(AppBarItemType.Secondary, new SortedSet<AppBarItem>());

            AppBarItems.Add(AppBarItem.ControlPanel, CameraSettingButton);
            AppBarItems.Add(AppBarItem.AppSetting, AppSettingButton);
            AppBarItems.Add(AppBarItem.AboutPage, AboutPageButton);
            AppBarItems.Add(AppBarItem.LoggerPage, LoggerPageButton);
            AppBarItems.Add(AppBarItem.PlaybackPage, PlaybackPageButton);
            AppBarItems.Add(AppBarItem.CancelTouchAF, CancelTouchFocusButton);
        }

        public CommandBarManager SetEvent(AppBarItem item, Windows.UI.Xaml.RoutedEventHandler handler)
        {
            AppBarItems[item].Click += handler;
            return this;
        }

        public CommandBarManager Clear()
        {
            foreach (var items in EnabledItems)
            {
                items.Value.Clear();
            }
            return this;
        }

        public CommandBarManager Icon(AppBarItem item)
        {
            return Enable(AppBarItemType.Primary, item);
        }

        public CommandBarManager NoIcon(AppBarItem item)
        {
            return Enable(AppBarItemType.Secondary, item);
        }

        private CommandBarManager Enable(AppBarItemType type, AppBarItem item)
        {
            if (!EnabledItems[type].Contains(item))
            {
                EnabledItems[type].Add(item);
            }
            return this;
        }

        public CommandBarManager Disable(AppBarItemType type, AppBarItem item)
        {
            if (EnabledItems[type].Contains(item))
            {
                EnabledItems[type].Remove(item);
            }
            return this;
        }

        public CommandBar CreateNew(double opacity)
        {
            bar = new CommandBar()
            {
                Background = Application.Current.Resources["AppBarBackgroundThemeBrush"] as Brush,
                Opacity = opacity,
            };
            foreach (AppBarItem item in EnabledItems[AppBarItemType.Primary])
            {
                bar.PrimaryCommands.Add(AppBarItems[item]);
            }
            foreach (AppBarItem item in EnabledItems[AppBarItemType.Secondary])
            {
                bar.SecondaryCommands.Add(AppBarItems[item]);
            }
            return bar;
        }

        public bool IsEnabled(AppBarItemType type, AppBarItem item)
        {
            return EnabledItems[type].Contains(item);
        }
    }

    public enum AppBarItemType
    {
        Primary,
        Secondary,
    }

    public enum AppBarItem
    {
        ControlPanel,
        AppSetting,
        AboutPage,
        PlaybackPage,
        LoggerPage,
        CancelTouchAF,
    }
}
