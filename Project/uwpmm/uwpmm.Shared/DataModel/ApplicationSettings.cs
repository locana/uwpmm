using Kazyx.Uwpmm.Control;
using Kazyx.Uwpmm.Playback;
using Kazyx.Uwpmm.Utility;
using System;
using Windows.UI;
using Windows.UI.Core;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Media;

namespace Kazyx.Uwpmm.DataModel
{
    public class ApplicationSettings : ObservableBase
    {
        private static ApplicationSettings sSettings = new ApplicationSettings();
        public Action SettingUpdated;

        private ApplicationSettings()
        {
            IsPostviewTransferEnabled = Preference.PostviewSyncEnabled;
            IsIntervalShootingEnabled = Preference.IntervalShootingEnabled;
            IntervalTime = Preference.IntervalTime;
            IsShootButtonDisplayed = Preference.ShootButtonVisible;
            IsHistogramDisplayed = Preference.HistogramVisible;
            GeotagEnabled = Preference.GeoTaggingEnabled;
            GridType = Preference.FramingGridType;
            GridColor = Preference.FramingGridColor;
            FibonacciLineOrigin = Preference.FibonacciOrigin;
            RequestFocusFrameInfo = Preference.FocusFrameEnabled;
            PrioritizeOriginalSizeContents = Preference.OriginalSizeContentsPrioritized;
            RemoteContentsSet = Preference.RemoteContentsSet;
        }

        public static ApplicationSettings GetInstance()
        {
            return sSettings;
        }

        private bool _IsPostviewTransferEnabled = true;

        public bool IsPostviewTransferEnabled
        {
            set
            {
                if (_IsPostviewTransferEnabled != value)
                {
                    Preference.PostviewSyncEnabled = value;
                    _IsPostviewTransferEnabled = value;
                    NotifyChangedOnUI("IsPostviewTransferEnabled");
                }
            }
            get
            {
                return _IsPostviewTransferEnabled;
            }
        }

        private bool _PrioritizeOriginalSizeContents = false;
        public bool PrioritizeOriginalSizeContents
        {
            set
            {
                if (_PrioritizeOriginalSizeContents != value)
                {
                    Preference.OriginalSizeContentsPrioritized = value;
                    _PrioritizeOriginalSizeContents = value;
                    NotifyChangedOnUI("PrioritizeOriginalSizeContents");
                }
            }
            get { return _PrioritizeOriginalSizeContents; }
        }

        private bool _IsIntervalShootingEnabled = false;
        public bool IsIntervalShootingEnabled
        {
            set
            {
                if (_IsIntervalShootingEnabled != value)
                {
                    Preference.IntervalShootingEnabled = value;
                    _IsIntervalShootingEnabled = value;

                    NotifyChangedOnUI("IsIntervalShootingEnabled");
                    NotifyChangedOnUI("IntervalTimeVisibility");


                    // exclusion
                    if (value)
                    {
                        /* TODO
                        if (manager.Status.IsAvailable("setSelfTimer") && manager.IntervalManager != null)
                        {
                            SetSelfTimerOff();
                        }
                         * */
                    }
                }
            }
            get
            {
                return _IsIntervalShootingEnabled;
            }
        }

        /* TODO
        private async void SetSelfTimerOff()
        {
            try
            {
                await manager.CameraApi.SetSelfTimerAsync(SelfTimerParam.Off);
            }
            catch (RemoteApiException e)
            {
                DebugUtil.Log("Failed to set selftimer off: " + e.code);

            }
        }
         * */

        public Visibility IntervalTimeVisibility
        {
            get { return IsIntervalShootingEnabled ? Visibility.Visible : Visibility.Collapsed; }
        }

        private int _IntervalTime = 10;

        public int IntervalTime
        {
            set
            {
                if (_IntervalTime != value)
                {
                    Preference.IntervalTime = value;
                    _IntervalTime = value;
                    // DebugUtil.Log("IntervalTime changed: " + value);
                    NotifyChangedOnUI("IntervalTime");
                }
            }
            get
            {
                return _IntervalTime;
            }
        }

        private bool _IsShootButtonDisplayed = true;
        public bool IsShootButtonDisplayed
        {
            set
            {
                if (_IsShootButtonDisplayed != value)
                {
                    Preference.ShootButtonVisible = value;
                    _IsShootButtonDisplayed = value;
                    NotifyChangedOnUI("ShootButtonVisibility");
                    DebugUtil.Log("ShootbuttonVisibility updated: " + value.ToString());
                }
            }
            get
            {
                return _IsShootButtonDisplayed;
            }
        }

        private bool _IsHistogramDisplayed = true;
        public bool IsHistogramDisplayed
        {
            set
            {
                if (_IsHistogramDisplayed != value)
                {
                    Preference.HistogramVisible = value;
                    _IsHistogramDisplayed = value;
                    NotifyChangedOnUI("IsHistogramDisplayed");
                }
            }
            get { return _IsHistogramDisplayed; }
        }

        private bool _GeotagEnabled = false;
        public bool GeotagEnabled
        {
            set
            {
                if (_GeotagEnabled != value)
                {
                    Preference.GeoTaggingEnabled = value;
                    _GeotagEnabled = value;
                    NotifyChangedOnUI("GeotagEnabled");
                    NotifyChangedOnUI("GeopositionStatusVisibility");
                }
            }
            get { return _GeotagEnabled; }
        }

        private bool _RequestFocusFrameInfo = true;
        public bool RequestFocusFrameInfo
        {
            set
            {
                if (_RequestFocusFrameInfo != value)
                {
                    Preference.FocusFrameEnabled = value;
                    _RequestFocusFrameInfo = value;
                    NotifyChangedOnUI("RequestFocusFrameInfo");
                }
            }
            get
            {
                return _RequestFocusFrameInfo;
            }
        }

        private FramingGridTypes _GridType = FramingGridTypes.Off;
        public FramingGridTypes GridType
        {
            set
            {
                if (_GridType != value)
                {
                    DebugUtil.Log("GridType updated: " + value);
                    Preference.FramingGridType = value;
                    _GridType = value;
                    NotifyChangedOnUI("GridType");
                }
            }
            get { return _GridType; }
        }

        private FramingGridColors _GridColor = FramingGridColors.White;
        public FramingGridColors GridColor
        {
            set
            {
                if (_GridColor != value)
                {
                    Preference.FramingGridColor = value;
                    _GridColor = value;
                    NotifyChangedOnUI("GridColor");
                    NotifyChangedOnUI("GridColorBrush");
                }
            }
            get { return _GridColor; }
        }

        public SolidColorBrush GridColorBrush
        {
            get
            {
                Color color;
                switch (this.GridColor)
                {
                    case FramingGridColors.White:
                        color = Color.FromArgb(200, 200, 200, 200);
                        break;
                    case FramingGridColors.Black:
                        color = Color.FromArgb(200, 50, 50, 50);
                        break;
                    case FramingGridColors.Red:
                        color = Color.FromArgb(200, 250, 30, 30);
                        break;
                    case FramingGridColors.Green:
                        color = Color.FromArgb(200, 30, 250, 30);
                        break;
                    case FramingGridColors.Blue:
                        color = Color.FromArgb(200, 30, 30, 250);
                        break;
                    default:
                        color = Color.FromArgb(200, 200, 200, 200);
                        break;

                }
                return new SolidColorBrush() { Color = color };
            }
        }

        private FibonacciLineOrigins _FibonacciLineOrigin = FibonacciLineOrigins.UpperLeft;
        public FibonacciLineOrigins FibonacciLineOrigin
        {
            get { return _FibonacciLineOrigin; }
            set
            {
                if (value != _FibonacciLineOrigin)
                {
                    Preference.FibonacciOrigin = value;
                    this._FibonacciLineOrigin = value;
                    NotifyChangedOnUI("FibonacciLineOrigin");
                }
            }
        }

        public Visibility ShootButtonVisibility
        {
            get
            {
                if (_IsShootButtonDisplayed && !ShootButtonTemporaryCollapsed)
                {
                    return Visibility.Visible;
                }
                else
                {
                    return Visibility.Collapsed;
                }
            }
        }

        private bool _ShootButtonTemporaryCollapsed = false;
        public bool ShootButtonTemporaryCollapsed
        {
            get { return _ShootButtonTemporaryCollapsed; }
            set
            {
                if (value != _ShootButtonTemporaryCollapsed)
                {
                    _ShootButtonTemporaryCollapsed = value;
                    NotifyChangedOnUI("ShootButtonVisibility");
                }
            }
        }

        private ContentsSet _RemoteContentsType = ContentsSet.ImagesAndMovies;
        public ContentsSet RemoteContentsSet
        {
            get { return _RemoteContentsType; }
            set
            {
                if (value != _RemoteContentsType)
                {
                    Preference.RemoteContentsSet = value;
                    _RemoteContentsType = value;
                    NotifyChangedOnUI("RemoteContentsSet");
                }
            }
        }

        protected override void NotifyChangedOnUI(string name, CoreDispatcherPriority priority = CoreDispatcherPriority.Normal)
        {
            base.NotifyChangedOnUI(name, priority);
            SettingUpdated.Raise();
        }
    }
}
