using Kazyx.ImageStream;
using Kazyx.RemoteApi;
using Kazyx.RemoteApi.Camera;
using Kazyx.Uwpmm.CameraControl;
using Kazyx.Uwpmm.Common;
using Kazyx.Uwpmm.Control;
using Kazyx.Uwpmm.DataModel;
using Kazyx.Uwpmm.Settings;
using Kazyx.Uwpmm.Utility;
using NtImageProcessor;
using NtNfcLib;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Windows.Devices.Geolocation;
using Windows.Foundation;
using Windows.Networking.Connectivity;
using Windows.Networking.Proximity;
using Windows.Phone.UI.Input;
using Windows.Storage;
using Windows.Storage.FileProperties;
using Windows.System;
using Windows.UI.Core;
using Windows.UI.Popups;
using Windows.UI.ViewManagement;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Media.Animation;
using Windows.UI.Xaml.Media.Imaging;
using Windows.UI.Xaml.Navigation;

// The Basic Page item template is documented at http://go.microsoft.com/fwlink/?LinkID=390556

namespace Kazyx.Uwpmm.Pages
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class MainPage : Page
    {
        private NavigationHelper navigationHelper;
        private ObservableDictionary defaultViewModel = new ObservableDictionary();
        private LiveviewScreenViewData screen_view_data;
        private OptionalElementsViewData optionalElementsViewData;
        private HistogramCreator HistogramCreator;
        private StatusBar statusBar = StatusBar.GetForCurrentView();

        public MainPage()
        {
            this.InitializeComponent();

            this.navigationHelper = new NavigationHelper(this);
            this.navigationHelper.LoadState += this.NavigationHelper_LoadState;
            this.navigationHelper.SaveState += this.NavigationHelper_SaveState;
        }

        /// <summary>
        /// Gets the <see cref="NavigationHelper"/> associated with this <see cref="Page"/>.
        /// </summary>
        public NavigationHelper NavigationHelper
        {
            get { return this.navigationHelper; }
        }

        /// <summary>
        /// Gets the view model for this <see cref="Page"/>.
        /// This can be changed to a strongly typed view model.
        /// </summary>
        public ObservableDictionary DefaultViewModel
        {
            get { return this.defaultViewModel; }
        }

        /// <summary>
        /// Populates the page with content passed during navigation.  Any saved state is also
        /// provided when recreating a page from a prior session.
        /// </summary>
        /// <param name="sender">
        /// The source of the event; typically <see cref="NavigationHelper"/>
        /// </param>
        /// <param name="e">Event data that provides both the navigation parameter passed to
        /// <see cref="Frame.Navigate(Type, Object)"/> when this page was initially requested and
        /// a dictionary of state preserved by this page during an earlier
        /// session.  The state will be null the first time a page is visited.</param>
        private void NavigationHelper_LoadState(object sender, LoadStateEventArgs e)
        {
        }

        /// <summary>
        /// Preserves state associated with this page in case the application is suspended or the
        /// page is discarded from the navigation cache.  Values must conform to the serialization
        /// requirements of <see cref="SuspensionManager.SessionState"/>.
        /// </summary>
        /// <param name="sender">The source of the event; typically <see cref="NavigationHelper"/></param>
        /// <param name="e">Event data that provides an empty dictionary to be populated with
        /// serializable state.</param>
        private void NavigationHelper_SaveState(object sender, SaveStateEventArgs e)
        {
        }

        #region NavigationHelper registration

        /// <summary>
        /// The methods provided in this section are simply used to allow
        /// NavigationHelper to respond to the page's navigation methods.
        /// <para>
        /// Page specific logic should be placed in event handlers for the  
        /// <see cref="NavigationHelper.LoadState"/>
        /// and <see cref="NavigationHelper.SaveState"/>.
        /// The navigation parameter is available in the LoadState method 
        /// in addition to page state preserved during an earlier session.
        /// </para>
        /// </summary>
        /// <param name="e">Provides data for navigation methods and event
        /// handlers that cannot cancel the navigation request.</param>
        protected override void OnNavigatedTo(NavigationEventArgs e)
        {
            this.navigationHelper.OnNavigatedTo(e);

            NavigatedByInAppBackTransition = e.NavigationMode == NavigationMode.Back;
            SetupNetworkObserver();
            UpdateMainDescription();
        }

        private void SetupNetworkObserver()
        {
            NetworkObserver.INSTANCE.CameraDiscovered += NetworkObserver_Discovered;
            NetworkObserver.INSTANCE.CdsDiscovered += NetworkObserver_CdsDiscovered;
        }

        void NetworkObserver_DevicesCleared(object sender, EventArgs e)
        {
            UpdateMainDescription();
        }

        protected override async void OnNavigatedFrom(NavigationEventArgs e)
        {
            PivotRoot.IsLocked = false;
            if (target != null)
            {
                await SequentialOperation.CleanupShootingMode(target);
            }
            liveview.CloseConnection();
            RemoveEventHandlers();
            // TearDownCurrentTarget();
            TearDownNetworkObserver();
            this.navigationHelper.OnNavigatedFrom(e);
        }

        private void TearDownNetworkObserver()
        {
            NetworkObserver.INSTANCE.CameraDiscovered -= NetworkObserver_Discovered;
            NetworkObserver.INSTANCE.CdsDiscovered -= NetworkObserver_CdsDiscovered;
        }

        #endregion

        /// <summary>
        /// Called from App.xaml.cs on suspending.
        /// </summary>
        public void OnSuspending()
        {
            if (PeriodicalShootingTask != null && PeriodicalShootingTask.IsRunning)
            {
                PeriodicalShootingTask.Stop();
            }

            cancel.CancelIfNotNull();

            TearDownCurrentTarget();
            PivotRoot.IsLocked = false;
            PivotRoot.SelectedIndex = 0;
            stayEntrance = false;
        }

        private void RemoveEventHandlers()
        {
            if (target != null)
            {
                target.Status.PropertyChanged -= Status_PropertyChanged;
            }
        }

        private void TearDownCurrentTarget()
        {
            LayoutRoot.DataContext = null;
            CreateEntranceAppBar();
            RemoveEventHandlers();
            var _target = target;
            if (_target != null)
            {
                target = null;
                _target.Observer.Stop();
                LiveviewScreen.Visibility = Visibility.Collapsed;
            }
            liveview.CloseConnection();
        }

        private bool NavigatedByInAppBackTransition = false;

        CommandBarManager _CommandBarManager = new CommandBarManager();
        Geolocator _Geolocator;

        bool ControlPanelDisplayed = false;

        private async void Page_Loaded(object sender, RoutedEventArgs e)
        {
            _CommandBarManager.SetEvent(AppBarItem.ControlPanel, (s, args) =>
            {
                if (ControlPanelDisplayed) { StartToHideControlPanel(); }
                else { StartToShowControlPanel(); }
            });
            _CommandBarManager.SetEvent(AppBarItem.CancelTouchAF, async (s, args) =>
                {
                    if (target == null || target.Api == null) { return; }
                    await target.Api.Camera.CancelTouchAFAsync();
                });
            _CommandBarManager.SetEvent(AppBarItem.AppSetting, (s, args) =>
            {
                OpenAppSettingPanel();
            });
            _CommandBarManager.SetEvent(AppBarItem.Ok, (s, args) =>
            {
                CloseAppSettingPanel();
            });
            _CommandBarManager.SetEvent(AppBarItem.AboutPage, (s, args) =>
            {
                Frame.Navigate(typeof(AboutPage));
            });
            //            _CommandBarManager.SetEvent(AppBarItem.LoggerPage, (s, args) =>
            //            {
            //                Frame.Navigate(typeof(LogViewerPage));
            //            });
            _CommandBarManager.SetEvent(AppBarItem.PlaybackPage, (s, args) =>
            {
                Frame.Navigate(typeof(PlaybackPage));
            });
            _CommandBarManager.SetEvent(AppBarItem.WifiSetting, async (s, args) =>
            {
                NetworkObserver.INSTANCE.ForceRestart();
                await Launcher.LaunchUriAsync(new Uri("ms-settings-wifi:"));
            });
            _CommandBarManager.SetEvent(AppBarItem.Donation, (s, args) =>
            {
                Frame.Navigate(typeof(HiddenPage));
            });

            if ((App.Current as App).IsTrialVersion)
            {
                TrialSuffix.Visibility = Visibility.Visible;
            }

            PivotRoot.SelectionChanged += PivotRoot_SelectionChanged;

            HardwareButtons.BackPressed += HardwareButtons_BackPressed;
            HardwareButtons.CameraHalfPressed += HardwareButtons_CameraHalfPressed;
            HardwareButtons.CameraReleased += HardwareButtons_CameraReleased;
            HardwareButtons.CameraPressed += HardwareButtons_CameraPressed;

            NetworkObserver.INSTANCE.DevicesCleared += NetworkObserver_DevicesCleared;
            NetworkObserver.INSTANCE.ForceRestart();
            UpdateMainDescription();

            NetworkInformation.NetworkStatusChanged += NetworkInformation_NetworkStatusChanged;

            _FocusFrameSurface.OnTouchFocusOperated += async (obj, args) =>
            {
                DebugUtil.Log("Touch AF operated: " + args.X + " " + args.Y);
                if (target == null || target.Api == null || !target.Api.Capability.IsAvailable("setTouchAFPosition")) { return; }
                try
                {
                    await target.Api.Camera.SetAFPositionAsync(args.X, args.Y);
                }
                catch (RemoteApiException ex)
                {
                    DebugUtil.Log(ex.StackTrace);
                }
            };

            FnumberSlider.SliderOperated += async (s, arg) =>
            {
                DebugUtil.Log("Fnumber operated: " + arg.Selected);
                try { await target.Api.Camera.SetFNumberAsync(arg.Selected); }
                catch (RemoteApiException) { }
            };
            SSSlider.SliderOperated += async (s, arg) =>
            {
                DebugUtil.Log("SS operated: " + arg.Selected);
                try { await target.Api.Camera.SetShutterSpeedAsync(arg.Selected); }
                catch (RemoteApiException) { }
            };
            ISOSlider.SliderOperated += async (s, arg) =>
            {
                DebugUtil.Log("ISO operated: " + arg.Selected);
                try { await target.Api.Camera.SetISOSpeedAsync(arg.Selected); }
                catch (RemoteApiException) { }
            };
            EvSlider.SliderOperated += async (s, arg) =>
            {
                DebugUtil.Log("Ev operated: " + arg.Selected);
                try { await target.Api.Camera.SetEvIndexAsync(arg.Selected); }
                catch (RemoteApiException) { }
            };
            ProgramShiftSlider.SliderOperated += async (s, arg) =>
            {
                DebugUtil.Log("Program shift operated: " + arg.OperatedStep);
                try { await target.Api.Camera.SetProgramShiftAsync(arg.OperatedStep); }
                catch (RemoteApiException) { }
            };

            ApplicationSettings.GetInstance().PropertyChanged += (s, args) =>
            {
                switch (args.PropertyName)
                {
                    case "IsIntervalShootingEnabled":
                        if (!ApplicationSettings.GetInstance().IsIntervalShootingEnabled)
                        {
                            // When periodical shooting is turned off, stop.
                            if (PeriodicalShootingTask != null && PeriodicalShootingTask.IsRunning)
                            {
                                PeriodicalShootingTask.Stop();
                            }
                        }
                        break;
                }
            };

            InitializeUI();
            InitializeProximityDevice();

            MediaDownloader.Instance.Fetched += PictureFetched;
            MediaDownloader.Instance.Failed += PictureFetchFailed;

            await NetworkObserver.INSTANCE.Initialize();
            UpdateMainDescription();
        }

        void NetworkInformation_NetworkStatusChanged(object sender)
        {
            DebugUtil.Log("NetworkStatusChanged. Unlock stay entrance mode.");
            stayEntrance = false;
        }

        private void PictureFetchFailed(DownloaderError err, GeotaggingResult tagResult)
        {
            ShowError(SystemUtil.GetStringResource("ErrorMessage_ImageDL_Other") + err + " " + tagResult);
        }

        private async void PictureFetched(Windows.Storage.StorageFolder folder, StorageFile file, GeotaggingResult tagResult)
        {
            var thumb = await file.GetThumbnailAsync(ThumbnailMode.ListView, 100);
            await Dispatcher.RunAsync(CoreDispatcherPriority.Normal, () =>
            {
                var image = new BitmapImage();
                image.SetSource(thumb);
                var path = file.Path.Split('\\');
                var name = "\r\n" + path[path.Length - 1];
                var text = SystemUtil.GetStringResource("Message_ImageDL_Succeed");
                switch (tagResult)
                {
                    case GeotaggingResult.OK:
                        text = SystemUtil.GetStringResource("Message_ImageDL_Succeed_withGeotag");
                        break;
                    case GeotaggingResult.GeotagAlreadyExists:
                        text = SystemUtil.GetStringResource("ErrorMessage_ImageDL_DuplicatedGeotag");
                        break;
                    case GeotaggingResult.UnExpectedError:
                        text = SystemUtil.GetStringResource("ErrorMessage_ImageDL_Geotagging");
                        break;
                }
                Toast.PushToast(new Control.ToastContent() { Text = text + name, Icon = image });
                thumb.Dispose();
            });
        }

        async void HardwareButtons_CameraPressed(object sender, CameraEventArgs e)
        {
            if (IsContinuousShootingMode()) { await StartContShooting(); }
            else { ShutterButtonPressed(); }
        }

        async void HardwareButtons_CameraReleased(object sender, CameraEventArgs e)
        {
            if (target == null || target.Api == null) { return; }
            if (target.Api.Capability.IsAvailable("cancelHalfPressShutter"))
            {
                try
                {
                    await target.Api.Camera.CancelHalfPressShutterAsync();
                }
                catch (RemoteApiException) { }
            }
            await StopContShooting();
        }

        async void HardwareButtons_CameraHalfPressed(object sender, CameraEventArgs e)
        {
            if (target == null || target.Api == null || !target.Api.Capability.IsAvailable("actHalfPressShutter")) { return; }
            try
            {
                await target.Api.Camera.ActHalfPressShutterAsync();
            }
            catch (RemoteApiException) { }
        }

        private void Page_Unloaded(object sender, RoutedEventArgs e)
        {
            _CommandBarManager.ClearEvents();
            PivotRoot.SelectionChanged -= PivotRoot_SelectionChanged;
            HardwareButtons.BackPressed -= HardwareButtons_BackPressed;
            HardwareButtons.CameraHalfPressed -= HardwareButtons_CameraHalfPressed;
            HardwareButtons.CameraReleased -= HardwareButtons_CameraReleased;
            HardwareButtons.CameraPressed -= HardwareButtons_CameraPressed;
            MediaDownloader.Instance.Fetched -= PictureFetched;
            MediaDownloader.Instance.Failed -= PictureFetchFailed;
            NetworkInformation.NetworkStatusChanged -= NetworkInformation_NetworkStatusChanged;
            NetworkObserver.INSTANCE.DevicesCleared -= NetworkObserver_DevicesCleared;
            NetworkObserver.INSTANCE.Finish();
            StopProximityDevice();
        }

        private void EnableGeolocator()
        {
            _Geolocator = null;
            _Geolocator = new Geolocator();

            try
            {
                _Geolocator.DesiredAccuracy = PositionAccuracy.Default;
                _Geolocator.ReportInterval = 3000;

                _Geolocator.PositionChanged += _Geolocator_PositionChanged;
                _Geolocator.StatusChanged += _Geolocator_StatusChanged;
            }
            catch (Exception ex)
            {
                if ((uint)ex.HResult == 0x80004004)
                {
                    // might be capability issue.
                    DebugUtil.Log("Error: capability issue, maybe.");
                    ShowError(SystemUtil.GetStringResource("ErrorMessage_LocationAccessUnauthorized"));
                }
            }
            screen_view_data.GeopositionEnabled = true;
        }

        private void DisableGeolocator()
        {
            try
            {
                _Geolocator.StatusChanged -= _Geolocator_StatusChanged;
                _Geolocator.PositionChanged -= _Geolocator_PositionChanged;
            }
            catch (Exception ex)
            {
                if ((uint)ex.HResult == 0x80004004)
                {
                    // might be capability issue.
                    DebugUtil.Log("Error: capability issue, maybe.");
                }
            }

            GeopositionManager.INSTANCE.Clear();
            screen_view_data.GeopositionEnabled = false;
        }

        void _Geolocator_StatusChanged(Geolocator sender, StatusChangedEventArgs args)
        {
            screen_view_data.GeopositionStatus = args.Status;
            if (args.Status != PositionStatus.Ready)
            {
                GeopositionManager.INSTANCE.Clear();
            }
        }

        private void _Geolocator_PositionChanged(Geolocator sender, PositionChangedEventArgs args)
        {
            GeopositionManager.INSTANCE.LatestPosition = args.Position;
        }

        void InitializeUI()
        {
            HistogramControl.Init(Histogram.ColorType.White, 1500);

            HistogramCreator = null;
            HistogramCreator = new HistogramCreator(HistogramCreator.HistogramResolution.Resolution_128);
            HistogramCreator.OnHistogramCreated += async (r, g, b) =>
            {
                await Dispatcher.RunAsync(CoreDispatcherPriority.Normal, () =>
                {
                    HistogramControl.SetHistogramValue(r, g, b);
                });
            };
            HistogramControl.DataContext = ApplicationSettings.GetInstance();
            ShutterButtonWrapper.DataContext = ApplicationSettings.GetInstance();
            InitializeAppSettingPanel();

            optionalElementsViewData = new OptionalElementsViewData() { AppSetting = ApplicationSettings.GetInstance() };
            FramingGuideSurface.DataContext = optionalElementsViewData;

            NfcAvailable.Visibility = Visibility.Collapsed;
            NfcDataGrid.Visibility = Visibility.Collapsed;
            WifiPassword.Text = "";
        }

        private void CreateEntranceAppBar()
        {
            this.BottomAppBar = _CommandBarManager.Clear()//
                .Icon(AppBarItem.WifiSetting)//
                .NoIcon(AppBarItem.AboutPage)//
                .Icon(AppBarItem.PlaybackPage)//
                //                .NoIcon(AppBarItem.LoggerPage)//
                .Icon(AppBarItem.Donation)//
                .CreateNew(0.6);
        }

        private void CreateCameraControlAppBar()
        {
            this.BottomAppBar = _CommandBarManager.Clear()//
                .Icon(AppBarItem.AppSetting)//
                .Icon(AppBarItem.ControlPanel)//
                .Icon(AppBarItem.PlaybackPage)//
                .CreateNew(0.6);
        }

        private void EmptyAppBar()
        {
            this.BottomAppBar = _CommandBarManager.Clear().CreateNew(0.6);
        }

        async void PivotRoot_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            switch ((sender as Pivot).SelectedIndex)
            {
                case 0:
                    await Task.Delay(TimeSpan.FromMilliseconds(500));
                    LiveViewPageUnloaded();
                    break;
                case 1:
                    stayEntrance = false;
                    await Task.Delay(TimeSpan.FromMilliseconds(500));
                    LiveViewPageLoaded();
                    break;
            }
        }

        private async void LiveViewPageLoaded()
        {
            if (target != null)
            {
                SetUpShooting();
                await LockRootPivotASync();
            }
            else if (!NetworkObserver.INSTANCE.IsConnectedToCamera)
            {
                GoToEntranceScreen();
            }
            else
            {
                EmptyAppBar();
                NetworkObserver.INSTANCE.ForceRestart();
            }
        }

        private async Task LockRootPivotASync()
        {
            await Dispatcher.RunAsync(CoreDispatcherPriority.Normal, async () =>
            {
                await Task.Delay(TimeSpan.FromMilliseconds(500));
                PivotRoot.IsLocked = true;
            });
        }

        private void LiveViewPageUnloaded()
        {
            DebugUtil.Log("LiveviewPage Unloaded");
            Ready = false;
            TearDownCurrentTarget();
        }

        private void Entrance_Loaded(object sender, RoutedEventArgs e)
        {
            CreateEntranceAppBar();
        }

        void HardwareButtons_BackPressed(object sender, BackPressedEventArgs e)
        {
            if (PivotRoot.SelectedIndex == 0)
            {
                cancel.CancelIfNotNull();
                stayEntrance = false;
                NetworkObserver.INSTANCE.Finish();
                return;
            }

            if (ControlPanelDisplayed)
            {
                StartToHideControlPanel();
                e.Handled = true;
                return;
            }

            if (AppSettingPanel.Visibility == Visibility.Visible)
            {
                CloseAppSettingPanel();
                e.Handled = true;
                return;
            }

            if (ShootingParamSliders.Visibility == Visibility.Visible)
            {
                OpenCloseSliders();
                e.Handled = true;
                return;
            }

            NetworkObserver.INSTANCE.ForceRestart();
            cancel.CancelIfNotNull();
            GoToEntranceScreen(true);
            e.Handled = true;
        }

        private void StartToShowControlPanel()
        {
            AnimationHelper.CreateSlideAnimation(new AnimationRequest()
            {
                Target = ControlPanelScrollViewer,
                Duration = TimeSpan.FromMilliseconds(150),
                Completed = (sender, obj) =>
                {
                    ControlPanelDisplayed = true;
                }
            }, FadeSide.Right, FadeType.FadeIn).Begin();
        }

        private void StartToHideControlPanel()
        {
            AnimationHelper.CreateSlideAnimation(new AnimationRequest()
            {
                Target = ControlPanelScrollViewer,
                Duration = TimeSpan.FromMilliseconds(150),
                Completed = (sender, obj) =>
                {
                    ControlPanelDisplayed = false;
                }
            },
                FadeSide.Right, FadeType.FadeOut).Begin();
        }

        private TargetDevice target;
        private StreamProcessor liveview = new StreamProcessor();
        private ImageDataSource liveview_data = new ImageDataSource();
        private ImageDataSource postview_data = new ImageDataSource();

        private async void NetworkObserver_CdsDiscovered(object sender, CdServiceEventArgs e)
        {
            UpdateMainDescription();
        }

        private void UpdateMainDescription()
        {
            var task = Dispatcher.RunAsync(CoreDispatcherPriority.Normal, () =>
            {
                if (NetworkObserver.INSTANCE.IsConnectedToCamera)
                {
                    if (NetworkObserver.INSTANCE.CameraDevices.Count != 0)
                    {
                        MainDescription.Text = SystemUtil.GetStringResource("Guide_StartLiveView").Replace("<ssid>", NetworkObserver.INSTANCE.PreviousSsid);
                        return;
                    }
                    else if (NetworkObserver.INSTANCE.CdsProviders.Count != 0)
                    {
                        if (SUPRESS_MEDIA_SERVER_DISCOVERY.All(name => name != NetworkObserver.INSTANCE.CdsProviders[0].FriendlyName))
                        {
                            MainDescription.Text = SystemUtil.GetStringResource("FoundDlnaDeviceGuide").Replace("<ssid>", NetworkObserver.INSTANCE.PreviousSsid);
                            Toast.PushToast(new Control.ToastContent()
                            {
                                Icon = new BitmapImage(new Uri("ms-appx:///Assets/AppBar/appBar_playback.png", UriKind.Absolute)),
                                Text = SystemUtil.GetStringResource("OpenGallery"),
                                Duration = TimeSpan.FromSeconds(8)
                            });
                            return;
                        }
                    }
                    MainDescription.Text = SystemUtil.GetStringResource("WifiConnectionGuide").Replace("<ssid>", NetworkObserver.INSTANCE.PreviousSsid);
                }
                else
                {
                    MainDescription.Text = SystemUtil.GetStringResource("Guide_WiFiNotEnabled");
                    return;
                }
            });
        }

        private string[] SUPRESS_MEDIA_SERVER_DISCOVERY = { "DSC-QX10", "DSC-QX100" };

        private bool stayEntrance = false;

        void NetworkObserver_Discovered(object sender, CameraDeviceEventArgs e)
        {
            UpdateMainDescription();

            target = e.CameraDevice;

            if (stayEntrance)
            {
                return;
            }

            SetUpShooting();
        }

        private CancellationTokenSource cancel;

        private bool _Ready = false;
        private bool Ready
        {
            set
            {
                if (_Ready != value)
                {
                    _Ready = value;
                    DebugUtil.Log(value ? "Ready" : "Unready");
                }
            }
            get { return _Ready; }
        }
        private bool SetupInProgress = false;

        private async void SetUpShooting()
        {
            if (Ready || SetupInProgress)
            {
                DebugUtil.Log("Suppress duplicated setup");
                return;
            }

            await Dispatcher.RunAsync(CoreDispatcherPriority.Normal, () =>
            {
                ChangeProgressText(SystemUtil.GetStringResource("ProgressMessageConnecting"));
            });

            var tmpTarget = target;

            if (tmpTarget == null)
            {
                HideProgress();
                DebugUtil.Log("No target to setup");
                GoToEntranceScreen();
                return;
            }

            cancel.CancelIfNotNull();
            cancel = new CancellationTokenSource(15000);
            try
            {
                SetupInProgress = true;
                await SequentialOperation.SetUp(tmpTarget, liveview, cancel);
                Ready = true;
            }
            catch (OperationCanceledException) { return; }
            catch (Exception ex)
            {
                DebugUtil.Log("Failed setup: " + ex.Message);
                GoToEntranceScreen();
                ShowError(SystemUtil.GetStringResource("ErrorMessage_fatal"));
                return;
            }
            finally
            {
                cancel = null;
                SetupInProgress = false;
                HideProgress();
            }

            await Dispatcher.RunAsync(CoreDispatcherPriority.Normal, async () =>
            {
                screen_view_data = new LiveviewScreenViewData(tmpTarget);
                Liveview.DataContext = screen_view_data;
                SetLiveviewDataContext(screen_view_data);
                ShowLiveviewScreen();
                ShutterButton.DataContext = screen_view_data;
                FramingGuideSurface.DataContext = new OptionalElementsViewData() { Liveview = screen_view_data, AppSetting = ApplicationSettings.GetInstance() };
                BatteryStatusDisplay.DataContext = tmpTarget.Status.BatteryInfo;

                tmpTarget.Status.PropertyChanged += Status_PropertyChanged;
                tmpTarget.Api.AvailiableApisUpdated += Api_AvailiableApisUpdated;

                if (PivotRoot.SelectedIndex == 0)
                {
                    GoToLiveviewScreen();
                }
                else
                {
                    // if already on shooting pivot, just lock it.
                    await LockRootPivotASync();
                }

                CreateCameraControlAppBar();

                ControlPanel.Children.Clear();
                var panels = SettingPanelBuilder.CreateNew(tmpTarget);
                var pn = panels.GetPanelsToShow();
                foreach (var panel in pn)
                {
                    ControlPanel.Children.Add(panel);
                }

                if (!await SetupFocusFrame(ApplicationSettings.GetInstance().RequestFocusFrameInfo))
                {
                    GoToEntranceScreen();
                    return;
                }

                _FocusFrameSurface.ClearFrames();

                ShootingParamSliders.DataContext = new ShootingParamViewData() { Status = tmpTarget.Status, Liveview = screen_view_data };

                if (ApplicationSettings.GetInstance().GeotagEnabled) { EnableGeolocator(); }
                else { DisableGeolocator(); }
            });
        }

        async void Api_AvailiableApisUpdated(object sender, AvailableApiEventArgs e)
        {
            await SetupFocusFrame(ApplicationSettings.GetInstance().RequestFocusFrameInfo);

            if (target.Api.Capability.IsAvailable("setLiveviewFrameInfo"))
            {
                FocusFrameSetting.SettingVisibility = Visibility.Visible;
            }
            else
            {
                FocusFrameSetting.SettingVisibility = Visibility.Collapsed;
            }
        }

        private async Task<bool> SetupFocusFrame(bool RequestFocusFrameEnabled)
        {
            if (target == null)
            {
                DebugUtil.Log("No target to set up focus frame is available.");
                return false;
            }
            if (target.Api.Capability.IsAvailable("setLiveviewFrameInfo"))
            {
                await target.Api.Camera.SetLiveviewFrameInfoAsync(new FrameInfoSetting() { TransferFrameInfo = RequestFocusFrameEnabled });
            }

            if (RequestFocusFrameEnabled && !target.Api.Capability.IsSupported("setLiveviewFrameInfo") && target.Api.Capability.IsAvailable("setTouchAFPosition"))
            {
                // For devices which does not support to transfer focus frame info, draw focus frame itself.
                _FocusFrameSurface.SelfDrawTouchAFFrame = true;
            }
            else { _FocusFrameSurface.SelfDrawTouchAFFrame = false; }
            return true;
        }

        void Status_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            var status = sender as CameraStatus;
            switch (e.PropertyName)
            {
                case "FocusStatus":
                    DebugUtil.Log("Focus status changed: " + status.FocusStatus);
                    if (status.FocusStatus == Kazyx.RemoteApi.Camera.FocusState.Focused)
                    {
                        ShowCancelTouchAFButton();
                        _FocusFrameSurface.Focused = true;
                    }
                    else
                    {
                        HideCancelTouchAFButton();
                        _FocusFrameSurface.Focused = false;
                    }
                    break;
                case "TouchFocusStatus":
                    DebugUtil.Log("TouchFocusStatus changed: " + status.TouchFocusStatus.Focused);
                    if (status.TouchFocusStatus.Focused)
                    {
                        ShowCancelTouchAFButton();
                        _FocusFrameSurface.Focused = true;
                    }
                    else
                    {
                        HideCancelTouchAFButton();
                        _FocusFrameSurface.Focused = false;
                    }
                    break;
                case "BatteryInfo":
                    BatteryStatusDisplay.BatteryInfo = status.BatteryInfo;
                    break;
                case "ContShootingResult":
                    if (ApplicationSettings.GetInstance().IsPostviewTransferEnabled)
                    {
                        foreach (var result in status.ContShootingResult)
                        {
                            MediaDownloader.Instance.EnqueuePostViewImage(new Uri(result.PostviewUrl, UriKind.Absolute), GeopositionManager.INSTANCE.LatestPosition);
                        }
                    }
                    break;
                case "Status":
                    if (status.Status == EventParam.Idle)
                    {
                        // When recording is stopped, clear recording time.
                        status.RecordingTimeSec = 0;
                    }
                    break;
                case "ShootMode":
                    DebugUtil.Log("Shootmode updated: " + status.ShootMode.Current);
                    if (status.ShootMode.Current == ShootModeParam.Audio) { ToAudioMode(); }
                    else { FromAudioMode(); }
                    break;
                default:
                    break;
            }
        }

        private async void FromAudioMode()
        {
            if (target != null && target.Api != null && liveview != null)
            {
                await SequentialOperation.OpenLiveviewStream(target.Api, liveview);
            }
        }

        private void ShowLiveviewScreen()
        {
            if (LiveviewScreen != null) { LiveviewScreen.Visibility = Visibility.Visible; }
        }

        private void HideLiveviewScreen()
        {
            if (LiveviewScreen != null) { LiveviewScreen.Visibility = Visibility.Collapsed; }
        }

        private async void ToAudioMode()
        {
            if (target != null && target.Api != null && liveview != null && liveview.ConnectionState == ConnectionState.Connected)
            {
                await SequentialOperation.CloseLiveviewStream(target.Api, liveview);
            }
        }

        private void HideCancelTouchAFButton()
        {
            this.BottomAppBar = _CommandBarManager.Disable(AppBarItemType.WithIcon, AppBarItem.CancelTouchAF).CreateNew(0.6);
        }

        void ShowCancelTouchAFButton()
        {
            this.BottomAppBar = _CommandBarManager.Icon(AppBarItem.CancelTouchAF).CreateNew(0.6);
        }

        private void GoToLiveviewScreen()
        {
            PivotRoot.SelectedIndex = 1;
        }

        private void GoToEntranceScreen(bool disableAutoStart = false)
        {
            var task = Dispatcher.RunAsync(CoreDispatcherPriority.Normal, () =>
            {
                stayEntrance = disableAutoStart;
                PivotRoot.IsLocked = false;
                PivotRoot.SelectedIndex = 0;
            });
        }

        private bool IsRendering = false;

        async void liveview_JpegRetrieved(object sender, JpegEventArgs e)
        {
            if (!screen_view_data.FramingGridDisplayed) { screen_view_data.FramingGridDisplayed = true; }

            if (IsRendering) { return; }

            IsRendering = true;
            await LiveviewUtil.SetAsBitmap(e.Packet.ImageData, liveview_data, HistogramCreator, Dispatcher);
            IsRendering = false;
        }

        async void liveview_FocusFrameRetrieved(object sender, FocusFrameEventArgs e)
        {
            await Dispatcher.RunAsync(CoreDispatcherPriority.Normal, () =>
            {
                _FocusFrameSurface.SetFocusFrames(e.Packet.FocusFrames);
            });
        }

        async void liveview_Closed(object sender, EventArgs e)
        {
            DebugUtil.Log("Liveview connection closed");
            IsRendering = true;
            await Dispatcher.RunAsync(CoreDispatcherPriority.High, () =>
            {
                liveview_data.Image = null;
                if (target != null && target.Status != null && target.Status.ShootMode != null
                    && target.Status.ShootMode.Current == ShootModeParam.Audio)
                {
                    HideLiveviewScreen();
                }
            });
            IsRendering = false;
        }

        private void LiveviewImage_Loaded(object sender, RoutedEventArgs e)
        {
            var image = sender as Image;
            SetLiveviewDataContext(screen_view_data);
            image.DataContext = liveview_data;
            liveview.JpegRetrieved += liveview_JpegRetrieved;
            liveview.FocusFrameRetrieved += liveview_FocusFrameRetrieved;
            liveview.Closed += liveview_Closed;
        }

        private void LiveviewImage_Unloaded(object sender, RoutedEventArgs e)
        {
            var image = sender as Image;
            image.DataContext = null;
            liveview_data.ScreenViewData = null;
            liveview.JpegRetrieved -= liveview_JpegRetrieved;
            liveview.FocusFrameRetrieved -= liveview_FocusFrameRetrieved;
            liveview.Closed -= liveview_Closed;
        }

        void SetLiveviewDataContext(LiveviewScreenViewData data)
        {
            if (liveview_data != null && data != null)
            {
                DebugUtil.Log("Setting liveview data context.");
                liveview_data.ScreenViewData = data;
            }
            else
            {
                DebugUtil.Log("Failed to set liveview data context.");
            }
        }

        private async void ZoomOutButton_Click(object sender, RoutedEventArgs e)
        {
            try { await target.Api.Camera.ActZoomAsync(ZoomParam.DirectionOut, ZoomParam.ActionStop); }
            catch (RemoteApiException ex) { DebugUtil.Log(ex.StackTrace); }
        }

        private async void ZoomOutButton_Tapped(object sender, TappedRoutedEventArgs e)
        {
            try { await target.Api.Camera.ActZoomAsync(ZoomParam.DirectionOut, ZoomParam.Action1Shot); }
            catch (RemoteApiException ex) { DebugUtil.Log(ex.StackTrace); }
        }

        private async void ZoomOutButton_Holding(object sender, HoldingRoutedEventArgs e)
        {
            try { await target.Api.Camera.ActZoomAsync(ZoomParam.DirectionOut, ZoomParam.ActionStart); }
            catch (RemoteApiException ex) { DebugUtil.Log(ex.StackTrace); }
        }

        private async void ZoomInButton_Click(object sender, RoutedEventArgs e)
        {
            try { await target.Api.Camera.ActZoomAsync(ZoomParam.DirectionIn, ZoomParam.ActionStop); }
            catch (RemoteApiException ex) { DebugUtil.Log(ex.StackTrace); }
        }

        private async void ZoomInButton_Tapped(object sender, TappedRoutedEventArgs e)
        {
            try { await target.Api.Camera.ActZoomAsync(ZoomParam.DirectionIn, ZoomParam.Action1Shot); }
            catch (RemoteApiException ex) { DebugUtil.Log(ex.StackTrace); }
        }

        private async void ZoomInButton_Holding(object sender, HoldingRoutedEventArgs e)
        {
            try { await target.Api.Camera.ActZoomAsync(ZoomParam.DirectionIn, ZoomParam.ActionStart); }
            catch (RemoteApiException ex) { DebugUtil.Log(ex.StackTrace); }
        }

        private async void ShutterButton_Click(object sender, RoutedEventArgs e)
        {
            if (IsContinuousShootingMode())
            {
                await StopContShooting();
            }
        }

        private async void ShutterButton_Holding(object sender, HoldingRoutedEventArgs e)
        {
            if (IsContinuousShootingMode())
            {
                await StartContShooting();
            }
            else { ShutterButtonPressed(); }
        }

        private void ShutterButton_Tapped(object sender, TappedRoutedEventArgs e)
        {
            if (IsContinuousShootingMode()) { ShowToast(SystemUtil.GetStringResource("Message_ContinuousShootingGuide")); }
            else { ShutterButtonPressed(); }
        }

        private async Task StartContShooting()
        {
            if (target == null) { return; }
            if ((PeriodicalShootingTask == null || !PeriodicalShootingTask.IsRunning) && IsContinuousShootingMode())
            {
                try
                {
                    await target.Api.Camera.StartContShootingAsync();
                }
                catch (RemoteApiException ex)
                {
                    DebugUtil.Log(ex.StackTrace);
                    ShowError(SystemUtil.GetStringResource("ErrorMessage_shootingFailure"));
                }
            }
        }

        private async Task StopContShooting()
        {
            if (target == null) { return; }
            if ((PeriodicalShootingTask == null || !PeriodicalShootingTask.IsRunning) && IsContinuousShootingMode())
            {
                try
                {
                    await SequentialOperation.StopContinuousShooting(target.Api);
                }
                catch (RemoteApiException ex)
                {
                    DebugUtil.Log(ex.StackTrace);
                    ShowError(SystemUtil.GetStringResource("Error_StopContinuousShooting"));
                }
            }
        }

        bool IsContinuousShootingMode()
        {
            return target != null && target.Status != null &&
                target.Status.ShootMode.Current == ShootModeParam.Still &&
                target.Status.ContShootingMode != null &&
                (target.Status.ContShootingMode.Current == ContinuousShootMode.Cont ||
                target.Status.ContShootingMode.Current == ContinuousShootMode.SpeedPriority);
        }

        PeriodicalShootingTask PeriodicalShootingTask;

        async void ShutterButtonPressed()
        {
            if (target == null || target.Status.ShootMode == null) { return; }

            switch (target.Status.ShootMode.Current)
            {
                case ShootModeParam.Still:
                    await TakeStillImage();
                    break;
                case ShootModeParam.Movie:
                    if (target.Status.Status == EventParam.Idle)
                    {
                        TryToRecord(target.Api.Camera.StartMovieRecAsync);
                    }
                    else if (target.Status.Status == EventParam.MvRecording)
                    {
                        TryToStop(target.Api.Camera.StopMovieRecAsync);
                    }
                    break;
                case ShootModeParam.Audio:
                    if (target.Status.Status == EventParam.Idle)
                    {
                        TryToRecord(target.Api.Camera.StartAudioRecAsync);
                    }
                    else if (target.Status.Status == EventParam.AuRecording)
                    {
                        TryToStop(target.Api.Camera.StopAudioRecAsync);
                    }
                    break;
                case ShootModeParam.Interval:
                    if (target.Status.Status == EventParam.Idle)
                    {
                        TryToRecord(target.Api.Camera.StartIntervalStillRecAsync);
                    }
                    else if (target.Status.Status == EventParam.ItvRecording)
                    {
                        TryToStop(target.Api.Camera.StopIntervalStillRecAsync);
                    }
                    break;
                case ShootModeParam.Loop:
                    if (target.Status.Status == EventParam.Idle)
                    {
                        TryToRecord(target.Api.Camera.StartLoopRecAsync);
                    }
                    else if (target.Status.Status == EventParam.LoopRecording)
                    {
                        TryToStop(target.Api.Camera.StopLoopRecAsync);
                    }
                    break;
            }
        }

        private async Task TakeStillImage()
        {
            if (PeriodicalShootingTask != null && PeriodicalShootingTask.IsRunning)
            {
                PeriodicalShootingTask.Stop();
                return;
            }
            if (ApplicationSettings.GetInstance().IsIntervalShootingEnabled &&
                (target.Status.ContShootingMode == null || (target.Status.ContShootingMode != null && target.Status.ContShootingMode.Current == ContinuousShootMode.Single)))
            {
                PeriodicalShootingTask = SetupPeriodicalShooting();
                PeriodicalShootingTask.Start();
                return;
            }
            try
            {
                await SequentialOperation.TakePicture(target.Api, GeopositionManager.INSTANCE.LatestPosition);
                if (!ApplicationSettings.GetInstance().IsPostviewTransferEnabled)
                {
                    ShowToast(SystemUtil.GetStringResource("Message_ImageCapture_Succeed"));
                }
            }
            catch (RemoteApiException ex)
            {
                DebugUtil.Log(ex.StackTrace);
                ShowError(SystemUtil.GetStringResource("ErrorMessage_shootingFailure"));
            }
        }

        private delegate Task RemoteRequestTask();
        private async void TryToRecord(RemoteRequestTask task)
        {
            try { await task(); }
            catch (RemoteApiException ex)
            {
                DebugUtil.Log(ex.StackTrace);
                ShowError(SystemUtil.GetStringResource("ErrorMessage_shootingFailure"));
            }
        }

        private async void TryToStop(RemoteRequestTask task)
        {
            try { await task(); }
            catch (RemoteApiException ex)
            {
                DebugUtil.Log(ex.StackTrace);
                ShowError(SystemUtil.GetStringResource("ErrorMessage_fatal"));
            }
        }

        private PeriodicalShootingTask SetupPeriodicalShooting()
        {
            var task = new PeriodicalShootingTask(new List<TargetDevice>() { target }, ApplicationSettings.GetInstance().IntervalTime);
            task.Tick += async (result) =>
            {
                await Dispatcher.RunAsync(CoreDispatcherPriority.Normal, () =>
                {
                    switch (result)
                    {
                        case PeriodicalShootingTask.ShootingResult.Skipped:
                            ShowToast(SystemUtil.GetStringResource("PeriodicalShooting_Skipped"));
                            break;
                        case PeriodicalShootingTask.ShootingResult.Succeed:
                            ShowToast(SystemUtil.GetStringResource("Message_ImageCapture_Succeed"));
                            break;
                    };
                });
            };
            task.Stopped += async (reason) =>
            {
                await Dispatcher.RunAsync(CoreDispatcherPriority.Normal, () =>
                {
                    switch (reason)
                    {
                        case PeriodicalShootingTask.StopReason.ShootingFailed:
                            ShowError(SystemUtil.GetStringResource("ErrorMessage_Interval"));
                            break;
                        case PeriodicalShootingTask.StopReason.SkipLimitExceeded:
                            ShowError(SystemUtil.GetStringResource("PeriodicalShooting_SkipLimitExceed"));
                            break;
                        case PeriodicalShootingTask.StopReason.RequestedByUser:
                            ShowToast(SystemUtil.GetStringResource("PeriodicalShooting_StoppedByUser"));
                            break;
                    };
                });
            };
            task.StatusUpdated += async (status) =>
            {
                await Dispatcher.RunAsync(CoreDispatcherPriority.Normal, () =>
                {
                    DebugUtil.Log("Status updated: " + status.Count);
                    if (status.IsRunning)
                    {
                        PeriodicalShootingStatus.Visibility = Visibility.Visible;
                        PeriodicalShootingStatusText.Text = SystemUtil.GetStringResource("PeriodicalShooting_Status")
                            .Replace("__INTERVAL__", status.Interval.ToString())
                            .Replace("__PHOTO_NUM__", status.Count.ToString());
                    }
                    else { PeriodicalShootingStatus.Visibility = Visibility.Collapsed; }
                });
            };
            return task;
        }

        private void ShowError(string error)
        {
            var task = Dispatcher.RunAsync(CoreDispatcherPriority.Normal, async () =>
            {
                var dialog = new MessageDialog(error);
                await dialog.ShowAsync();
            });
        }

        private void LiveviewImage_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            var rh = (sender as Image).RenderSize.Height;
            var rw = (sender as Image).RenderSize.Width;
            this._FocusFrameSurface.Height = rh;
            this._FocusFrameSurface.Width = rw;
            this.FramingGuideSurface.Height = rh;
            this.FramingGuideSurface.Width = rw;
        }

        private void ShowToast(string s)
        {
            Toast.PushToast(new Control.ToastContent() { Text = s });
        }

        ProximityDevice _ProximityDevice;
        long ProximitySubscribeId;

        private void InitializeProximityDevice()
        {
            StopProximityDevice();

            try
            {
                _ProximityDevice = ProximityDevice.GetDefault();
            }
            catch (FileNotFoundException)
            {
                _ProximityDevice = null;
                DebugUtil.Log("Caught ununderstandable exception. ");
                return;
            }
            catch (COMException)
            {
                _ProximityDevice = null;
                DebugUtil.Log("Caught ununderstandable exception. ");
                return;
            }

            if (_ProximityDevice == null)
            {
                DebugUtil.Log("It seems this is not NFC available device");
                return;
            }

            try
            {
                ProximitySubscribeId = _ProximityDevice.SubscribeForMessage("NDEF", ProximityMessageReceivedHandler);
            }
            catch (Exception e)
            {
                _ProximityDevice = null;
                DebugUtil.Log("Caught ununderstandable exception. " + e.Message + e.StackTrace);
                return;
            }

            NfcAvailable.Visibility = Visibility.Visible;
        }

        private void StopProximityDevice()
        {
            if (_ProximityDevice != null)
            {
                _ProximityDevice.StopSubscribingForMessage(ProximitySubscribeId);
                _ProximityDevice = null;
            }
        }

        private async void ProximityMessageReceivedHandler(ProximityDevice sender, ProximityMessage message)
        {
            var parser = new NdefParser(message);
            var ndefRecords = new List<NdefRecord>();

            var err = "";

            try { ndefRecords = parser.Parse(); }
            catch (NoSonyNdefRecordException) { err = SystemUtil.GetStringResource("ErrorMessage_CantFindSonyRecord"); }
            catch (NoNdefRecordException) { err = SystemUtil.GetStringResource("ErrorMessage_ParseNFC"); }
            catch (NdefParseException) { err = SystemUtil.GetStringResource("ErrorMessage_ParseNFC"); }
            catch (Exception) { err = SystemUtil.GetStringResource("ErrorMessage_fatal"); }

            if (err != "")
            {
                DebugUtil.Log("Failed to read NFC: " + err);
                await Dispatcher.RunAsync(CoreDispatcherPriority.Normal, () => { ShowError(err); });
            }

            if (ndefRecords.Count > 0)
            {
                foreach (NdefRecord r in ndefRecords)
                {
                    if (r.SSID.Length > 0 && r.Password.Length > 0)
                    {
                        await Dispatcher.RunAsync(CoreDispatcherPriority.Normal, async () =>
                        {
                            if (PivotRoot.SelectedIndex != 0) { return; }

                            WifiPassword.Text = r.Password;
                            NfcDataGrid.Visibility = Visibility.Visible;

                            var sb = new StringBuilder();
                            sb.Append(SystemUtil.GetStringResource("Message_NFC_succeed"));
                            sb.Append(System.Environment.NewLine);
                            sb.Append(System.Environment.NewLine);
                            sb.Append("SSID: ");
                            sb.Append(r.SSID);
                            sb.Append(System.Environment.NewLine);
                            sb.Append("Password: ");
                            sb.Append(r.Password);
                            sb.Append(System.Environment.NewLine);
                            sb.Append(System.Environment.NewLine);
                            var dialog = new MessageDialog(sb.ToString());
                            await dialog.ShowAsync();
                        });
                        break;
                    }
                }
            }
        }

        private AppSettingData<bool> geoSetting;
        private AppSettingData<int> gridColorSetting;
        private AppSettingData<int> fibonacciOriginSetting;
        private AppSettingData<bool> FocusFrameSetting;

        private void OpenAppSettingPanel()
        {
            // TODO: cancel touch AF and close other panels.
            AppSettingPanel.Visibility = Visibility.Visible;
            ShowSettingAnimation.Begin();
            this.BottomAppBar = _CommandBarManager.Clear().Icon(AppBarItem.Ok).CreateNew(0.6);
        }

        private void CloseAppSettingPanel()
        {
            HideSettingAnimation.Begin();
            CreateCameraControlAppBar();
        }

        private void InitializeAppSettingPanel()
        {
            var limited = (App.Current as App).IsFunctionLimited;

            var image_settings = new SettingSection(SystemUtil.GetStringResource("SettingSection_Image"));

            AppSettings.Children.Add(image_settings);

            image_settings.Add(new ToggleSetting(
                new AppSettingData<bool>(SystemUtil.GetStringResource("PostviewTransferSetting"), SystemUtil.GetStringResource("Guide_ReceiveCapturedImage"),
                () => { return ApplicationSettings.GetInstance().IsPostviewTransferEnabled; },
                enabled => { ApplicationSettings.GetInstance().IsPostviewTransferEnabled = enabled; })));

            var geoGuide = limited ? "TrialMessage" : "AddGeotag_guide";
            geoSetting = new AppSettingData<bool>(SystemUtil.GetStringResource("AddGeotag"), SystemUtil.GetStringResource(geoGuide),
                () =>
                {
                    if (limited) { return false; }
                    else { return ApplicationSettings.GetInstance().GeotagEnabled; }
                },
                enabled =>
                {
                    ApplicationSettings.GetInstance().GeotagEnabled = enabled;
                    if (enabled) { EnableGeolocator(); }
                    else { DisableGeolocator(); }
                });
            var geoToggle = new ToggleSetting(geoSetting);

            if (limited)
            {
                ApplicationSettings.GetInstance().GeotagEnabled = false;
                geoSetting.IsActive = false;
            }
            image_settings.Add(geoToggle);

            var display_settings = new SettingSection(SystemUtil.GetStringResource("SettingSection_Display"));

            AppSettings.Children.Add(display_settings);

            display_settings.Add(new ToggleSetting(
                new AppSettingData<bool>(SystemUtil.GetStringResource("DisplayTakeImageButtonSetting"), SystemUtil.GetStringResource("Guide_DisplayTakeImageButtonSetting"),
                () => { return ApplicationSettings.GetInstance().IsShootButtonDisplayed; },
                enabled => { ApplicationSettings.GetInstance().IsShootButtonDisplayed = enabled; })));

            display_settings.Add(new ToggleSetting(
                new AppSettingData<bool>(SystemUtil.GetStringResource("DisplayHistogram"), SystemUtil.GetStringResource("Guide_Histogram"),
                () => { return ApplicationSettings.GetInstance().IsHistogramDisplayed; },
                enabled => { ApplicationSettings.GetInstance().IsHistogramDisplayed = enabled; })));

            FocusFrameSetting = new AppSettingData<bool>(SystemUtil.GetStringResource("FocusFrameDisplay"), SystemUtil.GetStringResource("Guide_FocusFrameDisplay"),
                () => { return ApplicationSettings.GetInstance().RequestFocusFrameInfo; },
                async enabled =>
                {
                    ApplicationSettings.GetInstance().RequestFocusFrameInfo = enabled;
                    await SetupFocusFrame(enabled);
                    if (!enabled) { _FocusFrameSurface.ClearFrames(); }
                });
            display_settings.Add(new ToggleSetting(FocusFrameSetting));

            display_settings.Add(new ComboBoxSetting(
                new AppSettingData<int>(SystemUtil.GetStringResource("FramingGrids"), SystemUtil.GetStringResource("Guide_FramingGrids"),
                    () => { return (int)ApplicationSettings.GetInstance().GridType; },
                    setting =>
                    {
                        if (setting < 0) { return; }
                        ApplicationSettings.GetInstance().GridType = (FramingGridTypes)setting;
                        DisplayGridColorSetting(ApplicationSettings.GetInstance().GridType != FramingGridTypes.Off);
                        DisplayFibonacciOriginSetting(ApplicationSettings.GetInstance().GridType == FramingGridTypes.Fibonacci);
                    },
                    SettingValueConverter.FromFramingGrid(EnumUtil<FramingGridTypes>.GetValueEnumerable()))));

            gridColorSetting = new AppSettingData<int>(SystemUtil.GetStringResource("FramingGridColor"), null,
                    () => { return (int)ApplicationSettings.GetInstance().GridColor; },
                    setting =>
                    {
                        if (setting < 0) { return; }
                        ApplicationSettings.GetInstance().GridColor = (FramingGridColors)setting;
                    },
                    SettingValueConverter.FromFramingGridColor(EnumUtil<FramingGridColors>.GetValueEnumerable()));
            display_settings.Add(new ComboBoxSetting(gridColorSetting));

            fibonacciOriginSetting = new AppSettingData<int>(SystemUtil.GetStringResource("FibonacciSpiralOrigin"), null,
                () => { return (int)ApplicationSettings.GetInstance().FibonacciLineOrigin; },
                setting =>
                {
                    if (setting < 0) { return; }
                    ApplicationSettings.GetInstance().FibonacciLineOrigin = (FibonacciLineOrigins)setting;
                },
                SettingValueConverter.FromFibonacciLineOrigin(EnumUtil<FibonacciLineOrigins>.GetValueEnumerable()));
            display_settings.Add(new ComboBoxSetting(fibonacciOriginSetting));

            HideSettingAnimation.Completed += HideSettingAnimation_Completed;
        }

        private void DisplayGridColorSetting(bool displayed)
        {
            if (gridColorSetting != null)
            {
                gridColorSetting.IsActive = displayed;
            }
        }

        private void DisplayFibonacciOriginSetting(bool displayed)
        {
            if (fibonacciOriginSetting != null)
            {
                fibonacciOriginSetting.IsActive = displayed;
            }
        }

        private void HideSettingAnimation_Completed(object sender, object e)
        {
            AppSettingPanel.Visibility = Visibility.Collapsed;
        }

        public void StartOpenSliderAnimation(double from, double to)
        {
            var duration = new Duration(TimeSpan.FromMilliseconds(200));
            var sb = new Storyboard() { Duration = duration };
            var da = new DoubleAnimation() { Duration = duration };

            sb.Children.Add(da);

            var rt = new RotateTransform();

            Storyboard.SetTarget(da, rt);
            Storyboard.SetTargetProperty(da, "Angle");
            da.From = from;
            da.To = to;

            OpenSliderImage.RenderTransform = rt;
            OpenSliderImage.RenderTransformOrigin = new Point(0.5, 0.5);
            sb.Begin();
        }

        private void Grid_ManipulationCompleted(object sender, ManipulationCompletedRoutedEventArgs e)
        {
            OpenCloseSliders();
        }

        private void Grid_Tapped(object sender, TappedRoutedEventArgs e)
        {
            OpenCloseSliders();
        }

        private void OpenCloseSliders()
        {
            if (ShootingParamSliders.Visibility == Visibility.Visible)
            {
                ShootingParamSliders.Visibility = Visibility.Collapsed;
                StartOpenSliderAnimation(180, 0);
            }
            else
            {
                ShootingParamSliders.Visibility = Visibility.Visible;
                StartOpenSliderAnimation(0, 180);
            }
        }

        private async void HideProgress()
        {
            DebugUtil.Log("Hide Progress");
            await Dispatcher.RunAsync(CoreDispatcherPriority.Normal, async () =>
            {
                await statusBar.ProgressIndicator.HideAsync();
            });
        }

        private async void ChangeProgressText(string text)
        {
            DebugUtil.Log("Show Progress: " + text);
            await Dispatcher.RunAsync(CoreDispatcherPriority.High, async () =>
            {
                statusBar.ProgressIndicator.ProgressValue = null;
                statusBar.ProgressIndicator.Text = text;
                await statusBar.ProgressIndicator.ShowAsync();
            });
        }

    }
}
