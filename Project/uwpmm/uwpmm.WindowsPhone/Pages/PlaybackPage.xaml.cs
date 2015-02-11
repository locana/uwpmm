using Kazyx.RemoteApi;
using Kazyx.RemoteApi.AvContent;
using Kazyx.RemoteApi.Camera;
using Kazyx.Uwpmm.CameraControl;
using Kazyx.Uwpmm.Common;
using Kazyx.Uwpmm.Control;
using Kazyx.Uwpmm.DataModel;
using Kazyx.Uwpmm.Playback;
using Kazyx.Uwpmm.UPnP;
using Kazyx.Uwpmm.UPnP.ContentDirectory;
using Kazyx.Uwpmm.Utility;
using NtImageProcessor.MetaData.Misc;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Windows.Phone.UI.Input;
using Windows.Storage;
using Windows.UI.Core;
using Windows.UI.ViewManagement;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media.Imaging;
using Windows.UI.Xaml.Navigation;

// The Basic Page item template is documented at http://go.microsoft.com/fwlink/?LinkID=390556

namespace Kazyx.Uwpmm.Pages
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class PlaybackPage : Page
    {
        private NavigationHelper navigationHelper;

        public const string AUTO_JUMP_TO_DLNA_FLAG = "auto_jump_to_dlna";

        private const bool LOAD_DUMMY_CONTENTS = false;

        private HttpClient HttpClient = new HttpClient();

        public PlaybackPage()
        {
            this.InitializeComponent();

            this.navigationHelper = new NavigationHelper(this);
            this.navigationHelper.LoadState += this.NavigationHelper_LoadState;
            this.navigationHelper.SaveState += this.NavigationHelper_SaveState;

            CommandBarManager.SetEvent(AppBarItem.DownloadMultiple, (sender, e) =>
            {
                DebugUtil.Log("Download clicked");
                UpdateRemoteSelectionMode(SelectivityFactor.CopyToPhone);
                UpdateInnerState(ViewerState.RemoteSelecting);
            });
            CommandBarManager.SetEvent(AppBarItem.DeleteMultiple, (sender, e) =>
            {
                DebugUtil.Log("Delete clicked");
                switch (InnerState)
                {
                    case ViewerState.LocalSingle:
                        UpdateLocalSelectionMode(SelectivityFactor.Delete);
                        UpdateInnerState(ViewerState.LocalSelecting);
                        break;
                    case ViewerState.RemoteSingle:
                        UpdateRemoteSelectionMode(SelectivityFactor.Delete);
                        UpdateInnerState(ViewerState.RemoteSelecting);
                        break;
                }
            });
            CommandBarManager.SetEvent(AppBarItem.ShowDetailInfo, (sender, e) =>
            {
                PhotoScreen.DetailInfoVisibility = Visibility.Visible;
                BottomAppBar = CommandBarManager.Clear()
                    .Icon(AppBarItem.RotateRight)
                    .Icon(AppBarItem.HideDetailInfo)
                    .Icon(AppBarItem.RotateLeft)
                    .CreateNew(0.5);
            });
            CommandBarManager.SetEvent(AppBarItem.HideDetailInfo, (sender, e) =>
            {
                PhotoScreen.DetailInfoVisibility = Visibility.Collapsed;
                BottomAppBar = CommandBarManager.Clear()
                    .Icon(AppBarItem.RotateRight)
                    .Icon(AppBarItem.ShowDetailInfo)
                    .Icon(AppBarItem.RotateLeft)
                    .CreateNew(0.5);
            });
            CommandBarManager.SetEvent(AppBarItem.Ok, (sender, e) =>
            {
                DebugUtil.Log("Ok clicked");
                switch (InnerState)
                {
                    case ViewerState.AppSettings:
                        CloseAppSettingPanel();
                        break;
                    case ViewerState.RemoteSelecting:
                        switch (RemoteGridSource.SelectivityFactor)
                        {
                            case SelectivityFactor.CopyToPhone:
                                FetchSelectedImages();
                                break;
                            case SelectivityFactor.Delete:
                                DeleteSelectedImages();
                                break;
                            default:
                                DebugUtil.Log("Nothing to do for current SelectivityFactor");
                                break;
                        }
                        UpdateRemoteSelectionMode(SelectivityFactor.None);
                        UpdateInnerState(ViewerState.RemoteSingle);
                        break;
                    case ViewerState.LocalSelecting:
                        switch (LocalGridSource.SelectivityFactor)
                        {
                            case SelectivityFactor.Delete:
                                DeleteSelectedLocalImages();
                                break;
                            default:
                                DebugUtil.Log("Nothing to do for current SelectivityFactor");
                                break;
                        }
                        UpdateLocalSelectionMode(SelectivityFactor.None);
                        UpdateInnerState(ViewerState.LocalSingle);
                        break;
                    default:
                        DebugUtil.Log("Nothing to do for current InnerState");
                        break;
                }
            });
            CommandBarManager.SetEvent(AppBarItem.AppSetting, (sender, e) =>
            {
                DebugUtil.Log("AppSettings clicked");
                OpenAppSettingPanel();
            });

            CommandBarManager.SetEvent(AppBarItem.RotateRight, (sender, e) =>
            {
                PhotoScreen.RotateImage(Control.Rotation.Right);
            });

            CommandBarManager.SetEvent(AppBarItem.RotateLeft, (sender, e) =>
            {
                PhotoScreen.RotateImage(Control.Rotation.Left);
            });

            CommandBarManager.SetEvent(AppBarItem.Refresh, (sender, e) =>
            {
                InitializeRemoteGridContents();
                var task = InitializeRemote();
            });

            var storage_access_settings = new SettingSection(SystemUtil.GetStringResource("SettingSection_ContentsSync"));
            AppSettings.Children.Add(storage_access_settings);
            storage_access_settings.Add(new CheckBoxSetting(
                new AppSettingData<bool>(SystemUtil.GetStringResource("Setting_PrioritizeOriginalSize"), SystemUtil.GetStringResource("Guide_PrioritizeOriginalSize"),
                    () => { return ApplicationSettings.GetInstance().PrioritizeOriginalSizeContents; },
                    enabled => { ApplicationSettings.GetInstance().PrioritizeOriginalSizeContents = enabled; })));

            var contents_type_settings = new SettingSection("Contents type");
            contents_type_settings.Add(new ComboBoxSetting(
                new AppSettingData<int>("Contents type", "Filter types",
                    () => { return (int)ApplicationSettings.GetInstance().RemoteContentsSet; },
                    newValue =>
                    {
                        if (newValue != -1)
                        {
                            ApplicationSettings.GetInstance().RemoteContentsSet = (ContentsSet)newValue;
                            InitializeRemoteGridContents();
                        }
                    },
                    SettingValueConverter.FromContentsSet(EnumUtil<ContentsSet>.GetValueEnumerable()))));
            AppSettings.Children.Add(contents_type_settings);

            HideSettingAnimation.Completed += HideSettingAnimation_Completed;

            MovieScreen.SeekOperated += async (sender, arg) =>
            {
                ChangeProgressText(SystemUtil.GetStringResource("ProgressBar_Processing"));
                try
                {
                    await PlaybackModeHelper.SeekMovieStreamingAsync(TargetDevice.Api.AvContent, MovieStreamHelper.INSTANCE.MoviePlaybackData, arg.SeekPosition);
                }
                catch (RemoteApi.RemoteApiException) { }
                HideProgress();
            };
            MovieScreen.OnPlaybackOperationRequested += async (sender, arg) =>
            {
                try
                {
                    switch (arg.Request)
                    {
                        case PlaybackRequest.Start:
                            ChangeProgressText(SystemUtil.GetStringResource("ProgressBar_Processing"));
                            await TargetDevice.Api.AvContent.StartStreamingAsync();
                            break;
                        case PlaybackRequest.Pause:
                            ChangeProgressText(SystemUtil.GetStringResource("ProgressBar_Processing"));
                            await TargetDevice.Api.AvContent.PauseStreamingAsync();
                            break;
                    }
                    HideProgress();
                }
                catch (RemoteApiException ex) { DebugUtil.Log(ex.StackTrace); }
            };
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
        /*
        public ObservableDictionary DefaultViewModel
        {
            get { return this.defaultViewModel; }
        }
         * */

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
        protected override async void OnNavigatedTo(NavigationEventArgs e)
        {
            this.navigationHelper.OnNavigatedTo(e);

            if (e.NavigationMode != NavigationMode.New)
            {
                this.navigationHelper.GoBack();
                return;
            }
            ChangeProgressText("processing.");


            UpdateInnerState(ViewerState.LocalSingle);

            Canceller = new CancellationTokenSource();

            InitializeRemoteGridContents();
            UnsupportedMessage.Visibility = Visibility.Collapsed;

            RemoteGridSource = new AlbumGroupCollection();
            LocalGridSource = new AlbumGroupCollection(false);

            CloseMovieStream();
            MovieDrawer.DataContext = MovieStreamHelper.INSTANCE.MoviePlaybackData;

            PhotoScreen.DataContext = PhotoData;
            SetStillDetailVisibility(false);

            LoadLocalContents();

            PictureDownloader.Instance.Failed += OnDLError;
            PictureDownloader.Instance.Fetched += OnFetched;
            PictureDownloader.Instance.QueueStatusUpdated += OnFetchingImages;

            var devices = NetworkObserver.INSTANCE.CameraDevices;
            var dlna = NetworkObserver.INSTANCE.CdsProviders;
            if (devices.Count > 0)
            {
                DebugUtil.Log("Apply " + devices[0].FriendlyName + " as target");
                TargetDevice = devices[0]; // TODO choise of device should be exposed to user.
                await SetUpRemoteApiDevice();
            }
            else if (dlna.Count > 0)
            {
                DebugUtil.Log("Apply " + dlna[0].FriendlyName + " as target");
                UpnpDevice = dlna[0];
            }
            else
            {
                DebugUtil.Log("No target device detected. Search again.");
                NetworkObserver.INSTANCE.CdsDiscovered += NetworkObserver_CdsDiscovered;
                NetworkObserver.INSTANCE.CameraDiscovered += NetworkObserver_CameraDiscovered;
                NetworkObserver.INSTANCE.SearchCamera();
                NetworkObserver.INSTANCE.SearchCds();
            }

            await DefaultPivotLockState();

            MovieStreamHelper.INSTANCE.StreamClosed += MovieStreamHelper_StreamClosed;
            MovieStreamHelper.INSTANCE.StatusChanged += MovieStream_StatusChanged;

            var param = e.Parameter as string;
            if (param == AUTO_JUMP_TO_DLNA_FLAG)
            {
                MoveToRemotePivot();
            }
        }

        private void MoveToRemotePivot()
        {
            if (!PivotRoot.IsLocked)
            {
                PivotRoot.SelectedIndex = 1;
            }
        }

        private async void NetworkObserver_CameraDiscovered(object sender, CameraDeviceEventArgs e)
        {
            DebugUtil.Log("NetworkObserver_CameraDiscovered: " + e.CameraDevice.FriendlyName);
            NetworkObserver.INSTANCE.CdsDiscovered -= NetworkObserver_CdsDiscovered;
            NetworkObserver.INSTANCE.CameraDiscovered -= NetworkObserver_CameraDiscovered;
            await Dispatcher.RunAsync(CoreDispatcherPriority.High, () =>
            {
                TargetDevice = e.CameraDevice;
            });
            await SetUpRemoteApiDevice();
            await DefaultPivotLockState();
        }

        private async Task SetUpRemoteApiDevice()
        {
            await TargetDevice.Observer.StartAsync();
            UpdateStorageInfo();
            TargetDevice.Status.PropertyChanged += Status_PropertyChanged;
            MovieStreamHelper.INSTANCE.MoviePlaybackData.SeekAvailable = TargetDevice.Api.Capability.IsSupported("seekStreamingPosition");
        }

        async void NetworkObserver_CdsDiscovered(object sender, CdServiceEventArgs e)
        {
            DebugUtil.Log("NetworkObserver_CdsDiscovered: " + e.CdService.FriendlyName);
            NetworkObserver.INSTANCE.CdsDiscovered -= NetworkObserver_CdsDiscovered;
            NetworkObserver.INSTANCE.CameraDiscovered -= NetworkObserver_CameraDiscovered;
            await Dispatcher.RunAsync(CoreDispatcherPriority.High, () =>
            {
                UpnpDevice = e.CdService;
            });
            await DefaultPivotLockState();
        }

        private void UpdateRemoteSelectionMode(SelectivityFactor factor)
        {
            if (RemoteGridSource == null) { return; }

            RemoteGridSource.SelectivityFactor = factor;
            switch (factor)
            {
                case SelectivityFactor.None:
                    RemoteGrid.SelectionMode = ListViewSelectionMode.None;
                    HeaderBlocker.Visibility = Visibility.Collapsed;
                    break;
                case SelectivityFactor.CopyToPhone:
                    RemoteGrid.SelectionMode = ListViewSelectionMode.Multiple;
                    HeaderBlocker.Visibility = Visibility.Visible;
                    HeaderBlockerText.Text = SystemUtil.GetStringResource("Viewer_Header_SelectingToDownload");
                    break;
                case SelectivityFactor.Delete:
                    RemoteGrid.SelectionMode = ListViewSelectionMode.Multiple;
                    HeaderBlocker.Visibility = Visibility.Visible;
                    HeaderBlockerText.Text = SystemUtil.GetStringResource("Viewer_Header_SelectingToDelete");
                    break;
            }
        }

        private void UpdateLocalSelectionMode(SelectivityFactor factor)
        {
            if (LocalGridSource == null) { return; }

            LocalGridSource.SelectivityFactor = factor;
            switch (factor)
            {
                case SelectivityFactor.None:
                    LocalGrid.SelectionMode = ListViewSelectionMode.None;
                    HeaderBlocker.Visibility = Visibility.Collapsed;
                    break;
                case SelectivityFactor.Delete:
                    LocalGrid.SelectionMode = ListViewSelectionMode.Multiple;
                    HeaderBlocker.Visibility = Visibility.Visible;
                    HeaderBlockerText.Text = SystemUtil.GetStringResource("Viewer_Header_SelectingToDelete");
                    break;
            }
        }

        protected override void OnNavigatedFrom(NavigationEventArgs e)
        {
            MovieStreamHelper.INSTANCE.StreamClosed -= MovieStreamHelper_StreamClosed;
            MovieStreamHelper.INSTANCE.StatusChanged -= MovieStream_StatusChanged;

            if (TargetDevice != null)
            {
                TargetDevice.Status.PropertyChanged -= Status_PropertyChanged;
                TargetDevice = null;
            }
            UpnpDevice = null;

            PictureDownloader.Instance.Failed -= OnDLError;
            PictureDownloader.Instance.Fetched -= OnFetched;
            PictureDownloader.Instance.QueueStatusUpdated -= OnFetchingImages;

            ThumbnailCacheLoader.INSTANCE.CleanupRemainingTasks();

            CloseMovieStream();
            MovieDrawer.DataContext = null;

            if (Canceller != null)
            {
                Canceller.Cancel();
            }
            if (RemoteGridSource != null)
            {
                RemoteGridSource.Clear();
                RemoteGridSource = null;
            }
            if (LocalGridSource != null)
            {
                LocalGridSource.Clear();
                LocalGridSource = null;
            }

            HideProgress();

            UpdateInnerState(ViewerState.OutOfPage);

            this.navigationHelper.OnNavigatedFrom(e);
        }

        #endregion

        private TargetDevice _TargetDevice = null;
        private TargetDevice TargetDevice
        {
            set
            {
                _TargetDevice = value;
                if (value != null)
                {
                    RemoteTitleBlock.Text = value.FriendlyName;
                }
            }
            get { return _TargetDevice; }
        }

        private UpnpDevice _UpnpDevice = null;
        private UpnpDevice UpnpDevice
        {
            set
            {
                _UpnpDevice = value;
                if (value != null)
                {
                    RemoteTitleBlock.Text = value.FriendlyName;
                }
            }
            get { return _UpnpDevice; }
        }

        private StatusBar statusBar = StatusBar.GetForCurrentView();

        CommandBarManager CommandBarManager = new CommandBarManager();

        private ViewerState InnerState = ViewerState.LocalSingle;

        private void UpdateInnerState(ViewerState state)
        {
            InnerState = state;
            UpdateAppBar();
        }

        private void UpdateAppBar()
        {
            var task = Dispatcher.RunAsync(CoreDispatcherPriority.Normal, () =>
            {
                switch (InnerState)
                {
                    case ViewerState.RemoteSelecting:
                    case ViewerState.LocalSelecting:
                        BottomAppBar = CommandBarManager.Clear()
                            .Icon(AppBarItem.Ok)
                            .CreateNew(0.5);
                        break;
                    case ViewerState.LocalSingle:
                        UpdateLocalSelectionMode(SelectivityFactor.None);
                        {
                            var tmp = CommandBarManager.Clear()
                                .NoIcon(AppBarItem.AppSetting);
                            if (LocalGridSource != null && LocalGridSource.Count != 0)
                            {
                                tmp.Icon(AppBarItem.DeleteMultiple);
                            }
                            BottomAppBar = tmp.CreateNew(0.5);
                        }
                        break;
                    case ViewerState.RemoteSingle:
                        UpdateRemoteSelectionMode(SelectivityFactor.None);
                        {
                            var tmp = CommandBarManager.Clear()
                                .NoIcon(AppBarItem.AppSetting);
                            if (UpnpDevice != null || TargetDevice != null)
                            {
                                tmp.Icon(AppBarItem.Refresh);
                            }
                            if (RemoteGridSource != null && RemoteGridSource.Count != 0)
                            {
                                tmp.Icon(AppBarItem.DownloadMultiple)
                                .Icon(AppBarItem.DeleteMultiple);
                            }
                            BottomAppBar = tmp.CreateNew(0.5);
                        }
                        break;
                    case ViewerState.AppSettings:
                        BottomAppBar = CommandBarManager.Clear()
                            .Icon(AppBarItem.Ok)
                            .CreateNew(0.5);
                        break;
                    case ViewerState.LocalStillPlayback:
                    case ViewerState.RemoteStillPlayback:
                        if (PhotoScreen.DetailInfoVisibility == Visibility.Visible)
                        {
                            BottomAppBar = CommandBarManager.Clear()
                                .Icon(AppBarItem.RotateRight)
                                .Icon(AppBarItem.HideDetailInfo)
                                .Icon(AppBarItem.RotateLeft)
                                .CreateNew(0.5);
                        }
                        else
                        {
                            BottomAppBar = CommandBarManager.Clear()
                                .Icon(AppBarItem.RotateRight)
                                .Icon(AppBarItem.ShowDetailInfo)
                                .Icon(AppBarItem.RotateLeft)
                                .CreateNew(0.5);
                        }
                        break;
                    default:
                        BottomAppBar = null;
                        break;
                }
            });
        }

        private async Task DefaultPivotLockState()
        {
            await Dispatcher.RunAsync(CoreDispatcherPriority.High, () =>
            {
                var locked = !ShouldUnlockPivot(TargetDevice, UpnpDevice);
                if (locked)
                {
                    PivotRoot.SelectedIndex = 0;
                }
                PivotRoot.IsLocked = locked;
            });
        }

        private static bool ShouldUnlockPivot(TargetDevice device, UpnpDevice upnp)
        {
            if (LOAD_DUMMY_CONTENTS)
            {
                DebugUtil.Log("Unlock Pivot: Dummy contents mode enabled.");
                return true;
            }
            if (device != null && device.StorageAccessSupported)
            {
                DebugUtil.Log("Unlock Pivot: Remote API device available.");
                return true;
            }
            if (upnp != null)
            {
                DebugUtil.Log("Unlock Pivot: DLNA device available.");
                return true;
            }

            DebugUtil.Log("Lock Pivot: no available target.");
            return false;
        }

        private bool IsRemoteInitialized = false;
        internal PhotoPlaybackData PhotoData = new PhotoPlaybackData();

        void MovieStream_StatusChanged(object sender, StreamingStatusEventArgs e)
        {
            DebugUtil.Log("StreamStatusChanged: " + e.Status.Status + " - " + e.Status.Factor);
            switch (e.Status.Factor)
            {
                case StreamStatusChangeFactor.FileError:
                case StreamStatusChangeFactor.MediaError:
                case StreamStatusChangeFactor.OtherError:
                    ShowToast(SystemUtil.GetStringResource("Viewer_StreamClosedByExternalCause"));
                    CloseMovieStream();
                    break;
                default:
                    break;
            }
            MovieStreamHelper.INSTANCE.MoviePlaybackData.StreamingStatus = e.Status.Status;
            MovieStreamHelper.INSTANCE.MoviePlaybackData.StreamingStatusTransitionFactor = e.Status.Factor;
        }

        private async void CloseMovieStream()
        {
            UpdateInnerState(ViewerState.RemoteSingle);
            MovieStreamHelper.INSTANCE.Finish();
            await Dispatcher.RunAsync(CoreDispatcherPriority.Normal, async () =>
            {
                await DefaultPivotLockState();
                MovieScreen.Reset();
                MovieDrawer.Visibility = Visibility.Collapsed;
            });
        }

        void MovieStreamHelper_StreamClosed(object sender, EventArgs e)
        {
            DebugUtil.Log("StreamClosed");
            CloseMovieStream();
        }

        void Status_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            switch (e.PropertyName)
            {
                case "Storages":
                    UpdateStorageInfo();
                    break;
            }
        }

        private void UpdateStorageInfo()
        {
            if (TargetDevice != null)
            {
                var storages = TargetDevice.Status.Storages;
                StorageAvailable = storages != null && storages.Count != 0 && storages[0].StorageID != StorageId.NoMedia;
            }
            else
            {
                StorageAvailable = false;
            }
        }

        private async void LoadLocalContents()
        {
            ChangeProgressText(SystemUtil.GetStringResource("Progress_LoadingLocalContents"));

            var loader = new LocalContentsLoader();
            loader.SingleContentLoaded += LocalContentsLoader_SingleContentLoaded;
            try
            {
                await loader.Load(ContentsSet.Images, Canceller).ConfigureAwait(false);
                HideProgress();
            }
            catch
            {
                //TODO
                ShowToast("[TMP] Failed to load local contents.");
            }
            finally
            {
                loader.SingleContentLoaded -= LocalContentsLoader_SingleContentLoaded;
            }
        }

        async void LocalContentsLoader_SingleContentLoaded(object sender, SingleContentEventArgs e)
        {
            if (InnerState == ViewerState.OutOfPage) return;

            bool updateAppBarAfterAdded = false;
            if (LocalGridSource != null && LocalGridSource.Count == 0)
            {
                updateAppBarAfterAdded = true;
            }
            await Dispatcher.RunAsync(CoreDispatcherPriority.High, () =>
            {
                if (LocalGridSource != null)
                {
                    LocalGridSource.Add(e.File);
                    if (updateAppBarAfterAdded)
                    {
                        UpdateAppBar();
                    }
                }
            });
        }

#if DEBUG
        DummyContentsLoader loader = new DummyContentsLoader();

        private async Task AddDummyContentsAsync()
        {
            loader.PartLoaded += RemoteContentsLoader_PartLoaded;

            try
            {
                await loader.Load(ContentsSet.Images, Canceller).ConfigureAwait(false);
            }
            finally
            {
                loader.PartLoaded -= RemoteContentsLoader_PartLoaded;
            }
        }

        private async Task AddPartDummyContentsAsync(RemainingContentsHolder holder)
        {
            loader.PartLoaded += RemoteContentsLoader_PartLoaded;

            try
            {
                await loader.LoadRemainingAsync(holder, ContentsSet.Images, Canceller).ConfigureAwait(false);
            }
            finally
            {
                loader.PartLoaded -= RemoteContentsLoader_PartLoaded;
            }
        }
#endif

        private CancellationTokenSource Canceller;

        private AlbumGroupCollection RemoteGridSource;

        private AlbumGroupCollection LocalGridSource;

        private bool CheckRemoteCapability()
        {
            if (TargetDevice == null && UpnpDevice == null)
            {
                DebugUtil.Log("Device not found");
                return false;
            }

            if (TargetDevice != null && TargetDevice.Api.AvContent != null)
            {
                return true;
            }
            if (UpnpDevice != null)
            {
                return true;
            }

            return false;
        }

        private void OnStorageAvailabilityChanged(bool availability)
        {
            DebugUtil.Log("RemoteViewerPage: OnStorageAvailabilityChanged - " + availability);

            if (availability)
            {
                if (PivotRoot.SelectedIndex == 1 && CheckRemoteCapability())
                {
                    var storages = TargetDevice.Status.Storages;
                    if (storages[0].RecordableImages != -1 || storages[0].RecordableMovieLength != -1)
                    {
                        ShowToast(SystemUtil.GetStringResource("Viewer_RefreshAutomatically"));
                        var initTask = InitializeRemote();
                    }
                }
            }
            else
            {
                InitializeRemoteGridContents();
                ShowToast(SystemUtil.GetStringResource("Viewer_StorageDetached"));
                UpdateInnerState(ViewerState.RemoteNoMedia);
            }
        }

        private async void InitializeRemoteGridContents()
        {
            DebugUtil.Log("InitializeRemoteGridContents");
            IsRemoteInitialized = false;
            await Dispatcher.RunAsync(CoreDispatcherPriority.High, () =>
            {
                RemoteGridSource.Clear();
                UpdateAppBar();
            });
        }

        private bool _StorageAvailable = false;
        private bool StorageAvailable
        {
            set
            {
                var notify = value != _StorageAvailable;
                _StorageAvailable = value;
                if (notify)
                {
                    OnStorageAvailabilityChanged(value);
                }
            }
            get { return _StorageAvailable; }
        }

        private async Task InitializeRemote()
        {
            IsRemoteInitialized = true;

            if (TargetDevice != null)
            {
                await InitializeRemoteApiDevice();
            }
            else if (UpnpDevice != null)
            {
                await InitializeDlnaDevice();
            }
        }

        private async Task InitializeDlnaDevice()
        {
            var loader = new DlnaContentsLoader(UpnpDevice);
            loader.PartLoaded += RemoteContentsLoader_PartLoaded;

            ChangeProgressText(SystemUtil.GetStringResource("Progress_FetchingContents"));

            try
            {
                await loader.Load(ContentsSet.Images, Canceller).ConfigureAwait(false);
                DebugUtil.Log("DlnaContentsLoader completed");
                if (RemoteGridSource.Count == 0)
                {
                    // TODO
                    ShowToast("[TMP] Remote storage is empty");
                }
                HideProgress();
            }
            catch (SoapException e)
            {
                DebugUtil.Log("SoapException while loading: " + e.StatusCode);
                HideProgress();
                // TODO
                ShowToast("[TMP] Image item search is failed");
            }
            finally
            {
                loader.PartLoaded -= RemoteContentsLoader_PartLoaded;
            }
        }

        private async Task InitializeRemoteApiDevice()
        {
            try
            {
                ChangeProgressText(SystemUtil.GetStringResource("Progress_ChangingCameraState"));
                await PlaybackModeHelper.MoveToContentTransferModeAsync(TargetDevice.Api.Camera, TargetDevice.Status, 20000).ConfigureAwait(false);
                DebugUtil.Log("ModeTransition successfully finished.");

                ChangeProgressText(SystemUtil.GetStringResource("Progress_CheckingStorage"));

                var loader = new RemoteApiContentsLoader(TargetDevice);
                loader.PartLoaded += RemoteContentsLoader_PartLoaded;

                ChangeProgressText(SystemUtil.GetStringResource("Progress_FetchingContents"));

                try
                {
                    await loader.Load(ApplicationSettings.GetInstance().RemoteContentsSet, Canceller).ConfigureAwait(false);
                    DebugUtil.Log("RemoteApiContentsLoader completed");
                    HideProgress();
                }
                catch (StorageNotSupportedException)
                {
                    // This will never happen no camera devices.
                    DebugUtil.Log("storage scheme is not supported");
                    HideProgress();
                    ShowToast(SystemUtil.GetStringResource("Viewer_StorageAccessNotSupported"));
                }
                catch (NoStorageException)
                {
                    DebugUtil.Log("No storages");
                    HideProgress();
                    ShowToast(SystemUtil.GetStringResource("Viewer_NoStorage"));
                    return;
                }
                finally
                {
                    loader.PartLoaded -= RemoteContentsLoader_PartLoaded;
                }
            }
            catch (Exception e)
            {
                DebugUtil.Log(e.StackTrace);
                HideProgress();
                ShowToast(SystemUtil.GetStringResource("Viewer_FailedToRefreshContents"));
            }
        }

        async void RemoteContentsLoader_PartLoaded(object sender, ContentsLoadedEventArgs e)
        {
            if (InnerState == ViewerState.OutOfPage) return;

            await Dispatcher.RunAsync(CoreDispatcherPriority.High, () =>
            {
                if (RemoteGridSource != null)
                {
                    DebugUtil.Log("Adding " + e.Contents.Count + " contents to RemoteGrid");
                    // RemoteGridSource.AddRange(e.Contents);
                    bool updateAppBarAfterAdded = false;
                    if (RemoteGridSource.Count == 0)
                    {
                        updateAppBarAfterAdded = true;
                    }
                    foreach (var content in e.Contents)
                    {
                        RemoteGridSource.Add(content);
                        if (updateAppBarAfterAdded)
                        {
                            UpdateAppBar();
                            updateAppBarAfterAdded = false;
                        }
                    }
                }
            });
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

        private async void OnFetched(StorageFolder folder, StorageFile file, GeotaggingResult geotaggingResult)
        {
            if (InnerState == ViewerState.OutOfPage) return;

            DebugUtil.Log("ViewerPage: OnFetched");
            await Dispatcher.RunAsync(CoreDispatcherPriority.High, () =>
            {
                var content = new ContentInfo
                {
                    Protected = false,
                    ContentType = ContentKind.StillImage,
                    GroupName = folder.DisplayName,
                };
                var thumb = new Thumbnail(content, file);
                LocalGridSource.Add(thumb);
            });
        }

        private void OnDLError(ImageFetchError error, GeotaggingResult geotaggingResult)
        {
            DebugUtil.Log("ViewerPage: OnDLError");
            // TODO show toast according to error cause...
        }

        private void OnFetchingImages(int count)
        {
            if (InnerState == ViewerState.OutOfPage) return;

            if (count != 0)
            {
                ChangeProgressText(string.Format(SystemUtil.GetStringResource("ProgressMessageFetching"), count));
            }
            else
            {
                HideProgress();
            }
        }

        private async void SetStillDetailVisibility(bool visible)
        {
            if (visible)
            {
                PivotRoot.IsLocked = true;
                HideProgress();
                IsViewingDetail = true;
                PhotoScreen.Visibility = Visibility.Visible;
                RemoteGrid.IsEnabled = false;
                LocalGrid.IsEnabled = false;
                if (PivotRoot.SelectedIndex == 0)
                {
                    UpdateInnerState(ViewerState.LocalStillPlayback);
                }
                else if (PivotRoot.SelectedIndex == 1)
                {
                    UpdateInnerState(ViewerState.RemoteStillPlayback);
                }
            }
            else
            {
                await DefaultPivotLockState();
                HideProgress();
                IsViewingDetail = false;
                PhotoScreen.Visibility = Visibility.Collapsed;
                RemoteGrid.IsEnabled = true;
                LocalGrid.IsEnabled = true;
                if (PivotRoot.SelectedIndex == 0)
                {
                    UpdateInnerState(ViewerState.LocalSingle);
                }
                else if (PivotRoot.SelectedIndex == 1)
                {
                    UpdateInnerState(ViewerState.RemoteSingle);
                }
            }
        }

        void InitBitmapBeforeOpen()
        {
            DebugUtil.Log("Before open");
            PhotoScreen.Init();
        }

        private void ReleaseDetail()
        {
            PhotoScreen.ReleaseImage();
            SetStillDetailVisibility(false);
        }

        private bool IsViewingDetail = false;

        private void Pivot_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            UpdateRemoteSelectionMode(SelectivityFactor.None);
            var pivot = sender as Pivot;
            switch (pivot.SelectedIndex)
            {
                case 0:
                    UpdateInnerState(ViewerState.LocalSingle);
                    break;
                case 1:
                    OnRemotePivotDisplayed();
                    break;
            }
        }

        private void OnRemotePivotDisplayed()
        {
#if DEBUG
            if (LOAD_DUMMY_CONTENTS && !IsRemoteInitialized)
            {
                UpdateInnerState(ViewerState.RemoteSingle);
                var task = InitializeRemote();
                var dummytask = AddDummyContentsAsync();
            }
#endif

            if (CheckRemoteCapability())
            {
                if (IsRemoteInitialized)
                {
                    UpdateInnerState(ViewerState.RemoteSingle);
                }
                else
                {
                    if (StorageAvailable)
                    {
                        UpdateInnerState(ViewerState.RemoteSingle);
                        var task = InitializeRemote();
                    }
                    else if (UpnpDevice != null)
                    {
                        UpdateInnerState(ViewerState.RemoteSingle);
                        var task = InitializeRemote();
                    }
                    else
                    {
                        ShowToast(SystemUtil.GetStringResource("Viewer_NoStorage"));
                        UpdateInnerState(ViewerState.RemoteNoMedia);
                    }
                }
            }
            else
            {
                UpdateInnerState(ViewerState.RemoteUnsupported);
                ShowToast(SystemUtil.GetStringResource("Viewer_StorageAccessNotSupported"));
                UnsupportedMessage.Visibility = Visibility.Visible;
            }
        }

        private async void DeleteSelectedImages()
        {
            DebugUtil.Log("DeleteSelectedImages");
            var items = RemoteGrid.SelectedItems;
            if (items.Count == 0)
            {
                HideProgress();
                return;
            }

            var copy = new List<object>(items);

            var uris = copy
                .Select(item => (item as Thumbnail).Source as RemoteApiContentInfo)
                .Where(info => info != null)
                .Select(info => info.Uri).ToList();

            var dlna = copy
                .Select(item => (item as Thumbnail).Source as DlnaContentInfo)
                .Where(info => info != null)
                .Select(info => info.Id).ToList();

            foreach (var item in copy)
            {
                var data = item as Thumbnail;
                RemoteGridSource.Remove(data);
            }

            await DeleteRemoteApiContents(new TargetContents { ContentUris = uris });
            await DeleteDlnaContentsAsync(dlna);
            UpdateAppBar();
        }

        private async void DeleteSelectedLocalImages()
        {
            DebugUtil.Log("DeleteSelectedLocalImages: " + LocalGrid.SelectedItems.Count);
            var items = LocalGrid.SelectedItems;
            if (items.Count == 0)
            {
                HideProgress();
                return;
            }

            foreach (var data in new List<object>(items).Select(item => item as Thumbnail).Where(thumb => thumb.CacheFile != null))
            {
                await TryDeleteLocalFile(data);
            }
            UpdateAppBar();
        }

        private void FetchSelectedImages()
        {
            var items = RemoteGrid.SelectedItems;
            if (items.Count == 0)
            {
                HideProgress();
                return;
            }

            foreach (var item in new List<object>(items))
            {
                try
                {
                    EnqueueImageDownload(item as Thumbnail);
                }
                catch (Exception e)
                {
                    DebugUtil.Log(e.StackTrace);
                }
            }
        }

        private void EnqueueImageDownload(Thumbnail source)
        {
            if (ApplicationSettings.GetInstance().PrioritizeOriginalSizeContents && source.Source.OriginalUrl != null)
            {
                PictureDownloader.Instance.Enqueue(new Uri(source.Source.OriginalUrl));
            }
            else
            {
                // Fallback to large size image
                PictureDownloader.Instance.Enqueue(new Uri(source.Source.LargeUrl));
            }
        }

        private async void ShowToast(string message)
        {
            DebugUtil.Log(message);
            await Dispatcher.RunAsync(CoreDispatcherPriority.Normal, () =>
            {
                Toast.PushToast(new Control.ToastContent() { Text = message });
            });
        }

        private void Playback_Click(object sender, RoutedEventArgs e)
        {
            var item = sender as MenuFlyoutItem;
            var content = item.DataContext as Thumbnail;
            var task = PlaybackContent(content);
        }

        private async Task PlaybackContent(Thumbnail content)
        {
            if (content == null || content.Source == null || content.Source.ContentType == null)
            {
                ShowToast("Information for playback is lacking.");
                return;
            }

            switch (content.Source.ContentType)
            {
                case ContentKind.StillImage:
                    ChangeProgressText(SystemUtil.GetStringResource("Progress_OpeningDetailImage"));
                    try
                    {
                        var res = await HttpClient.GetAsync(new Uri(content.Source.LargeUrl));
                        if (res.StatusCode != HttpStatusCode.OK)
                        {
                            HideProgress();
                            return;
                        }

                        using (var strm = await res.Content.ReadAsStreamAsync())
                        {
                            var replica = new MemoryStream();

                            strm.CopyTo(replica); // Copy to the new stream to avoid stream crash issue.
                            if (replica.Length <= 0)
                            {
                                return;
                            }
                            replica.Seek(0, SeekOrigin.Begin);

                            await Dispatcher.RunAsync(CoreDispatcherPriority.Normal, async () =>
                            {
                                try
                                {
                                    var _bitmap = new BitmapImage();
                                    _bitmap.SetSource(replica.AsRandomAccessStream());
                                    PhotoScreen.SourceBitmap = _bitmap;
                                    InitBitmapBeforeOpen();
                                    PhotoScreen.SetBitmap();
                                    try
                                    {
                                        PhotoData.MetaData = await NtImageProcessor.MetaData.JpegMetaDataParser.ParseImageAsync(replica);
                                    }
                                    catch (UnsupportedFileFormatException)
                                    {
                                        PhotoData.MetaData = null;
                                        PhotoScreen.DetailInfoVisibility = Visibility.Collapsed;
                                    }
                                    SetStillDetailVisibility(true);
                                }
                                finally
                                {
                                    if (replica != null)
                                    {
                                        replica.Dispose();
                                    }
                                }
                            });
                        }
                    }
                    catch (Exception ex)
                    {
                        DebugUtil.Log(ex.StackTrace);
                        HideProgress();
                    }
                    break;
                case ContentKind.MovieMp4:
                case ContentKind.MovieXavcS:
                    if (MovieStreamHelper.INSTANCE.IsProcessing)
                    {
                        MovieStreamHelper.INSTANCE.Finish();
                    }

                    var item = content.Source as ContentInfo;
                    if (item.RemotePlaybackAvailable)
                    {
                        if (TargetDevice == null || TargetDevice.Api.AvContent == null)
                        {
                            ShowToast(SystemUtil.GetStringResource("Viewer_NoAvContentApi"));
                            return;
                        }

                        PivotRoot.IsLocked = true;
                        UpdateInnerState(ViewerState.RemoteMoviePlayback);
                        MovieDrawer.Visibility = Visibility.Visible;
                        ChangeProgressText(SystemUtil.GetStringResource("Progress_OpeningMovieStream"));
                        var started = await MovieStreamHelper.INSTANCE.Start(TargetDevice.Api.AvContent, new PlaybackContent
                        {
                            Uri = (item as RemoteApiContentInfo).Uri,
                            RemotePlayType = RemotePlayMode.SimpleStreaming
                        }, content.Source.Name);
                        if (!started)
                        {
                            ShowToast(SystemUtil.GetStringResource("Viewer_FailedPlaybackMovie"));
                            CloseMovieStream();
                        }
                        HideProgress();
                    }
                    else
                    {
                        ShowToast(SystemUtil.GetStringResource("Viewer_UnplayableContent"));
                    }
                    break;
                default:
                    break;
            }
        }

        private void CopyToPhone_Click(object sender, RoutedEventArgs e)
        {
            var item = sender as MenuFlyoutItem;
            try
            {
                EnqueueImageDownload(item.DataContext as Thumbnail);
            }
            catch (Exception ex)
            {
                DebugUtil.Log(ex.StackTrace);
            }
        }

        private void Delete_Click(object sender, RoutedEventArgs e)
        {
            var item = sender as MenuFlyoutItem;
            var thumb = item.DataContext as Thumbnail;
            var content = thumb.Source;

            if (content is RemoteApiContentInfo)
            {
                var data = content as RemoteApiContentInfo;
                var contents = new TargetContents();
                contents.ContentUris = new List<string>();
                contents.ContentUris.Add(data.Uri);
                var task = DeleteRemoteApiContents(contents);
            }
            else if (content is DlnaContentInfo)
            {
                var data = content as DlnaContentInfo;
                var list = new List<string>();
                list.Add(data.Id);
                var task = DeleteDlnaContentsAsync(list);
            }

            RemoteGridSource.Remove(thumb);
        }

        private async Task DeleteRemoteApiContents(TargetContents contents)
        {
            if (TargetDevice == null || TargetDevice.Api == null)
            {
                // Nothing to do.
                return;
            }

            var av = TargetDevice.Api.AvContent;
            if (av != null && contents != null)
            {
                ChangeProgressText(SystemUtil.GetStringResource("Progress_DeletingSelectedContents"));
                try
                {
                    await av.DeleteContentAsync(contents).ConfigureAwait(false);
                    DebugUtil.Log("Delete contents completed");
                }
                catch
                {
                    ShowToast(SystemUtil.GetStringResource("Viewer_FailedToDeleteContents"));
                }
                finally
                {
                    HideProgress();
                }
            }
            else
            {
                DebugUtil.Log("Not ready to delete contents");
            }
        }

        private async Task DeleteDlnaContentsAsync(IList<string> objectIdList)
        {
            if (UpnpDevice == null)
            {
                // Nothing to do.
                return;
            }

            var cds = UpnpDevice.Services[URN.ContentDirectory];
            ChangeProgressText(SystemUtil.GetStringResource("Progress_DeletingSelectedContents"));

            foreach (var id in objectIdList)
            {
                try
                {
                    await cds.Control(new DestroyObjectRequest
                    {
                        ObjectID = id,
                    }).ConfigureAwait(false);
                }
                catch (SoapException e)
                {
                    DebugUtil.Log("Failed to delete " + e.StatusCode);
                }
            }

            HideProgress();
        }

        private void OpenAppSettingPanel()
        {
            AppSettingPanel.Visibility = Visibility.Visible;
            UpdateInnerState(ViewerState.AppSettings);
            ShowSettingAnimation.Begin();
        }

        private void CloseAppSettingPanel()
        {
            HideSettingAnimation.Begin();
            UpdateInnerState(ViewerState.RemoteSingle);
        }

        void HideSettingAnimation_Completed(object sender, object e)
        {
            AppSettingPanel.Visibility = Visibility.Collapsed;
            if (InnerState != ViewerState.OutOfPage && PivotRoot.SelectedIndex == 1)
            {
                OnRemotePivotDisplayed();
            }
        }

        private async void PivotRoot_Loaded(object sender, RoutedEventArgs e)
        {
            await DefaultPivotLockState().ConfigureAwait(false);
        }

        private void Page_Loaded(object sender, RoutedEventArgs e)
        {
            HardwareButtons.BackPressed += HardwareButtons_BackPressed;
        }

        private void Page_Unloaded(object sender, RoutedEventArgs e)
        {
            HardwareButtons.BackPressed -= HardwareButtons_BackPressed;
        }

        void HardwareButtons_BackPressed(object sender, BackPressedEventArgs e)
        {
            DebugUtil.Log("Backkey pressed.");
            if (IsViewingDetail)
            {
                DebugUtil.Log("Release detail.");
                ReleaseDetail();
                e.Handled = true;
            }

            if (MovieDrawer.Visibility == Visibility.Visible || MovieStreamHelper.INSTANCE.IsProcessing)
            {
                DebugUtil.Log("Close movie stream.");
                CloseMovieStream();
                e.Handled = true;
            }

            if (RemoteGrid.SelectionMode == ListViewSelectionMode.Multiple)
            {
                DebugUtil.Log("Set selection mode none.");
                UpdateInnerState(ViewerState.RemoteSingle);
                e.Handled = true;
            }

            if (LocalGrid.SelectionMode == ListViewSelectionMode.Multiple)
            {
                DebugUtil.Log("Set selection mode none.");
                UpdateInnerState(ViewerState.LocalSingle);
                e.Handled = true;
            }

            if (AppSettingPanel.Visibility == Visibility.Visible)
            {
                DebugUtil.Log("Close app setting panel.");
                CloseAppSettingPanel();
                e.Handled = true;
            }
        }

        private void RemoteGrid_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            GridSelectionChanged(sender, e, false);
        }

        private void LocalGrid_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            GridSelectionChanged(sender, e, true);
        }

        private void GridSelectionChanged(object sender, SelectionChangedEventArgs e, bool isLocal)
        {
            var selector = sender as GridView;
            if (selector.SelectionMode == ListViewSelectionMode.Multiple)
            {
                DebugUtil.Log("SelectionChanged in multi mode");
                var contents = selector.SelectedItems;
                DebugUtil.Log("Selected Items: " + contents.Count);
                if (contents.Count > 0)
                {
                    UpdateInnerState(isLocal ? ViewerState.LocalSelecting : ViewerState.RemoteSelecting);
                }
                else
                {
                    UpdateInnerState(isLocal ? ViewerState.LocalMulti : ViewerState.RemoteMulti);
                }
            }
        }

        private void RemoteGrid_Loaded(object sender, RoutedEventArgs e)
        {
            RemoteSources.Source = RemoteGridSource;
            (RemoteSemanticZoom.ZoomedOutView as ListViewBase).ItemsSource = RemoteSources.View.CollectionGroups;
        }

        private void RemoteGrid_Unloaded(object sender, RoutedEventArgs e)
        {
            RemoteSources.Source = null;
        }

        private void RemoteGrid_Holding(object sender, HoldingRoutedEventArgs e)
        {
            FlyoutBase.ShowAttachedFlyout(sender as FrameworkElement);
        }

        private void LocalGrid_Loaded(object sender, RoutedEventArgs e)
        {
            LocalSources.Source = LocalGridSource;
            (LocalSemanticZoom.ZoomedOutView as ListViewBase).ItemsSource = LocalSources.View.CollectionGroups;
        }

        private void LocalGrid_Unloaded(object sender, RoutedEventArgs e)
        {
            LocalSources.Source = null;
        }

        private void LocalGrid_Holding(object sender, HoldingRoutedEventArgs e)
        {
            FlyoutBase.ShowAttachedFlyout(sender as FrameworkElement);
        }

        private void LocalPlayback_Click(object sender, RoutedEventArgs e)
        {
            var item = sender as MenuFlyoutItem;
            var data = item.DataContext as Thumbnail;
            DisplayLocalDetailImage(data);
        }

        private async void DisplayLocalDetailImage(Thumbnail content)
        {
            if (IsViewingDetail)
            {
                return;
            }

            ChangeProgressText(SystemUtil.GetStringResource("Progress_OpeningDetailImage"));

            try
            {
                using (var stream = await content.CacheFile.OpenStreamForReadAsync())
                {
                    var bitmap = new BitmapImage();
                    await bitmap.SetSourceAsync(stream.AsRandomAccessStream());
                    PhotoScreen.SourceBitmap = bitmap;
                    InitBitmapBeforeOpen();
                    PhotoScreen.SetBitmap();
                    try
                    {
                        PhotoData.MetaData = await NtImageProcessor.MetaData.JpegMetaDataParser.ParseImageAsync(stream);
                    }
                    catch (UnsupportedFileFormatException)
                    {
                        PhotoData.MetaData = null;
                        PhotoScreen.DetailInfoVisibility = Visibility.Collapsed;
                    }
                    SetStillDetailVisibility(true);
                }
            }
            catch
            {
                HideProgress();
                ShowToast(SystemUtil.GetStringResource("Viewer_FailedToOpenDetail"));
            }
        }

        private async void LocalDelete_Click(object sender, RoutedEventArgs e)
        {
            var item = sender as MenuFlyoutItem;
            var data = item.DataContext as Thumbnail;
            if (data.CacheFile != null)
            {
                await TryDeleteLocalFile(data);
            }
        }

        private async Task TryDeleteLocalFile(Thumbnail data)
        {
            try
            {
                LocalGridSource.Remove(data);
                DebugUtil.Log("Delete " + data.CacheFile.DisplayName);
                await data.CacheFile.DeleteAsync();
            }
            catch (Exception ex)
            {
                DebugUtil.Log("Failed to delete file: " + ex.StackTrace);
            }
        }

        private void RemoteThumbnailGrid_Tapped(object sender, TappedRoutedEventArgs e)
        {
            if (RemoteGrid.SelectionMode == ListViewSelectionMode.Multiple)
            {
                return;
            }
            var grid = sender as Grid;

            var content = grid.DataContext as Thumbnail;
            if (content.IsContent)
            {
                var task = PlaybackContent(content);
            }
            else
            {
                var holder = grid.DataContext as RemainingContentsHolder;
                RemoteGridSource.Remove(holder, false);
                var loadTask = LoadRemainingContents(holder);
            }
        }

        private void LocalThumbnailGrid_Tapped(object sender, TappedRoutedEventArgs e)
        {
            if (LocalGrid.SelectionMode == ListViewSelectionMode.Multiple)
            {
                return;
            }

            var image = sender as Grid;
            var content = image.DataContext as Thumbnail;
            DisplayLocalDetailImage(content);
        }

        private async void FetchMore_Click(object sender, RoutedEventArgs e)
        {
            var item = sender as MenuFlyoutItem;
            var holder = item.DataContext as RemainingContentsHolder;
            RemoteGridSource.Remove(holder, false);
            await LoadRemainingContents(holder);
        }

        private async Task LoadRemainingContents(RemainingContentsHolder holder)
        {
#if DEBUG
            if (LOAD_DUMMY_CONTENTS)
            {
                var task = AddPartDummyContentsAsync(holder);
                return;
            }
#endif
            ContentsLoader loader = null;

            if (TargetDevice != null)
            {
                loader = new RemoteApiContentsLoader(TargetDevice);
            }
            else if (UpnpDevice != null)
            {
                loader = new DlnaContentsLoader(UpnpDevice);
            }
            else
            {
                return;
            }

            loader.PartLoaded += RemoteContentsLoader_PartLoaded;
            ChangeProgressText(SystemUtil.GetStringResource("Progress_FetchingContents"));
            try
            {
                await loader.LoadRemainingAsync(holder, ApplicationSettings.GetInstance().RemoteContentsSet, Canceller);
            }
            catch (Exception e)
            {
                DebugUtil.Log(e.StackTrace);
                HideProgress();
                ShowToast(SystemUtil.GetStringResource("Viewer_FailedToRefreshContents"));
            }
            finally
            {
                loader.PartLoaded -= RemoteContentsLoader_PartLoaded;
            }
        }
    }

    public enum ViewerState
    {
        LocalSingle,
        LocalStillPlayback,
        LocalMulti,
        LocalSelecting,
        RemoteUnsupported,
        RemoteNoMedia,
        RemoteSingle,
        RemoteMulti,
        RemoteSelecting,
        RemoteStillPlayback,
        RemoteMoviePlayback,
        AppSettings,
        OutOfPage,
    }
}
