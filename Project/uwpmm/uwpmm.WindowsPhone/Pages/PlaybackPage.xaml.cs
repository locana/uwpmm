﻿using Kazyx.RemoteApi.AvContent;
using Kazyx.RemoteApi.Camera;
using Kazyx.Uwpmm.CameraControl;
using Kazyx.Uwpmm.Common;
using Kazyx.Uwpmm.DataModel;
using Kazyx.Uwpmm.Utility;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Threading;
using System.Threading.Tasks;
using Windows.UI.Core;
using Windows.UI.ViewManagement;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
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
        private ObservableDictionary defaultViewModel = new ObservableDictionary();

        public PlaybackPage()
        {
            this.InitializeComponent();

            this.navigationHelper = new NavigationHelper(this);
            this.navigationHelper.LoadState += this.NavigationHelper_LoadState;
            this.navigationHelper.SaveState += this.NavigationHelper_SaveState;

            // Comment out until application bar manager is imported.
            /*
            abm.SetEvent(IconMenu.DownloadMultiple, (sender, e) =>
            {
                DebugUtil.Log("Download clicked");
                if (GridSource != null)
                {
                    GridSource.SelectivityFactor = SelectivityFactor.CopyToPhone;
                }
                RemoteImageGrid.IsSelectionEnabled = true;
            });
            abm.SetEvent(IconMenu.DeleteMultiple, (sender, e) =>
            {
                DebugUtil.Log("Delete clicked");
                if (GridSource != null)
                {
                    GridSource.SelectivityFactor = SelectivityFactor.Delete;
                }
                RemoteImageGrid.IsSelectionEnabled = true;
            });
            abm.SetEvent(IconMenu.ShowDetailInfo, (sender, e) =>
            {
                PhotoPlaybackScreen.DetailInfoVisibility = Visibility.Visible;
                ApplicationBar = abm.Clear().Enable(IconMenu.HideDetailInfo).CreateNew(0.5);
            });
            abm.SetEvent(IconMenu.HideDetailInfo, (sender, e) =>
            {
                PhotoPlaybackScreen.DetailInfoVisibility = Visibility.Collapsed;
                ApplicationBar = abm.Clear().Enable(IconMenu.ShowDetailInfo).CreateNew(0.5);
            });

            abm.SetEvent(IconMenu.Ok, (sender, e) =>
            {
                DebugUtil.Log("Ok clicked");
                switch (InnerState)
                {
                    case ViewerState.AppSettings:
                        CloseAppSettingPanel();
                        break;
                    case ViewerState.RemoteSelecting:
                        switch (GridSource.SelectivityFactor)
                        {
                            case SelectivityFactor.CopyToPhone:
                                UpdateInnerState(ViewerState.Sync);
                                FetchSelectedImages();
                                break;
                            case SelectivityFactor.Delete:
                                UpdateInnerState(ViewerState.RemoteSingle);
                                DeleteSelectedImages();
                                break;
                            default:
                                DebugUtil.Log("Nothing to do for current SelectivityFactor");
                                break;
                        }
                        break;
                    default:
                        DebugUtil.Log("Nothing to do for current InnerState");
                        break;
                }
            });
            abm.SetEvent(IconMenu.ApplicationSetting, (sender, e) =>
            {
                DebugUtil.Log("AppSettings clicked");
                OpenAppSettingPanel();
            });
             * */

            // Comment out until setting screen is imported.
            /*
            var storage_access_settings = new SettingSection(SystemUtil.GetStringResource("SettingSection_ContentsSync"));
            AppSettings.Children.Add(storage_access_settings);
            storage_access_settings.Add(new CheckBoxSetting(
                new AppSettingData<bool>(SystemUtil.GetStringResource("Setting_PrioritizeOriginalSize"), SystemUtil.GetStringResource("Guide_PrioritizeOriginalSize"),
                    () => { return ApplicationSettings.GetInstance().PrioritizeOriginalSizeContents; },
                    enabled => { ApplicationSettings.GetInstance().PrioritizeOriginalSizeContents = enabled; })));

            HideSettingAnimation.Completed += HideSettingAnimation_Completed;
            */


            // TODO: If seek is supported, set vallback of seek bar and enable it.
            //MoviePlaybackScreen.SeekOperated += (NewValue) =>
            //{
            //};
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

            if (e.NavigationMode != NavigationMode.New)
            {
                this.navigationHelper.GoBack();
                return;
            }

            UpdateInnerState(ViewerState.Local);

            DeleteRemoteGridFacially();
            UpdateStorageInfo();
            UnsupportedMessage.Visibility = Visibility.Collapsed;

            GridSource = new DateGroupCollection();
            /*
            RemoteImageGrid.ItemsSource = GridSource;

            groups = new ThumbnailGroup();
            LocalImageGrid.DataContext = groups;
            */
            CloseMovieStream();
            /*
            MovieDrawer.DataContext = MovieStreamHelper.INSTANCE.MoviePlaybackData;

            PhotoPlaybackScreen.DataContext = PhotoData;
            SetStillDetailVisibility(false);
            LoadLocalContents();
             * */

#if DEBUG
            // AddDummyContentsAsync();
#endif
            /*
            PictureSyncManager.Instance.Failed += OnDLError;
            PictureSyncManager.Instance.Fetched += OnFetched;
            PictureSyncManager.Instance.Downloader.QueueStatusUpdated += OnFetchingImages;
             * */
            if (TargetDevice != null)
            {
                TargetDevice.Status.PropertyChanged += Status_PropertyChanged;
            }
            MovieStreamHelper.INSTANCE.StreamClosed += MovieStreamHelper_StreamClosed;
            MovieStreamHelper.INSTANCE.StatusChanged += MovieStream_StatusChanged;
        }

        protected override void OnNavigatedFrom(NavigationEventArgs e)
        {
            MovieStreamHelper.INSTANCE.StreamClosed -= MovieStreamHelper_StreamClosed;
            MovieStreamHelper.INSTANCE.StatusChanged -= MovieStream_StatusChanged;
            if (TargetDevice != null)
            {
                TargetDevice.Status.PropertyChanged -= Status_PropertyChanged;
            }
            // PictureSyncManager.Instance.Failed -= OnDLError;
            // PictureSyncManager.Instance.Fetched -= OnFetched;
            // PictureSyncManager.Instance.Downloader.QueueStatusUpdated -= OnFetchingImages;

            CloseMovieStream();
            // MovieDrawer.DataContext = null;

            if (Canceller != null)
            {
                Canceller.Cancel();
            }
            if (GridSource != null)
            {
                GridSource.Clear();
                GridSource = null;
            }

            /*
            if (groups != null && groups.Group != null)
            {
                groups.Group.Clear();
                groups = null;
            }
             * */

            HideProgress();

            CurrentUuid = null;

            UpdateInnerState(ViewerState.OutOfPage);

            this.navigationHelper.OnNavigatedFrom(e);
        }

        #endregion


        private TargetDevice TargetDevice { set; get; }

        private StatusBar statusBar = StatusBar.GetForCurrentView();

        private ViewerState InnerState = ViewerState.Local;

        private void UpdateInnerState(ViewerState state)
        {
            InnerState = state;

            // Comment out until application bar manager is ported.
            /*
            await SystemUtil.GetCurrentDispatcher().RunAsync(CoreDispatcherPriority.Normal, () =>
            {
                switch (InnerState)
                {
                    case ViewerState.RemoteSelecting:
                        ApplicationBar = abm.Clear().Enable(IconMenu.Ok).CreateNew(0.5);
                        break;
                    case ViewerState.RemoteSingle:
                        ApplicationBar = abm.Clear().Enable(IconMenu.DownloadMultiple).Enable(IconMenu.DeleteMultiple).Enable(IconMenu.ApplicationSetting).CreateNew(0.5);
                        break;
                    case ViewerState.AppSettings:
                        ApplicationBar = abm.Clear().Enable(IconMenu.Ok).CreateNew(0.5);
                        break;
                    case ViewerState.LocalStillPlayback:
                    case ViewerState.RemoteStillPlayback:
                        if (PhotoPlaybackScreen.DetailInfoVisibility == System.Windows.Visibility.Visible)
                        {
                            ApplicationBar = abm.Clear().Enable(IconMenu.HideDetailInfo).CreateNew(0.5);
                        }
                        else
                        {
                            ApplicationBar = abm.Clear().Enable(IconMenu.ShowDetailInfo).CreateNew(0.5);
                        }
                        break;
                    default:
                        ApplicationBar = null;
                        break;
                }
            });
             * */
        }

        private void UnlockPivot()
        {
            if (TargetDevice != null && TargetDevice.Status.StorageAccessSupported)
            {
                PivotRoot.IsLocked = false;
            }
        }

        // Comment out until application bar manager is ported.
        // private readonly AppBarManager abm = new AppBarManager();

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
        }

        private async void CloseMovieStream()
        {
            UpdateInnerState(ViewerState.RemoteSingle);
            MovieStreamHelper.INSTANCE.Finish();
            await SystemUtil.GetCurrentDispatcher().RunAsync(CoreDispatcherPriority.Normal, () =>
            {
                UnlockPivot();
                // MoviePlaybackScreen.Reset();
                // MovieDrawer.Visibility = Visibility.Collapsed;
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

        /*
        ThumbnailGroup groups = null;

        private void LoadLocalContents()
        {
            var lib = new MediaLibrary();
            PictureAlbum CameraRoll = null;
            foreach (var album in lib.RootPictureAlbum.Albums)
            {
                if (album.Name == "Camera Roll")
                {
                    CameraRoll = album;
                    break;
                }
            }
            if (CameraRoll == null)
            {
                DebugUtil.Log("No camera roll. Going back");
                ShowToast(SystemUtil.GetStringResource("Viewer_NoCameraRoll"));
                return;
            }
            LoadThumbnails(CameraRoll);
        }

        private async void LoadThumbnails(PictureAlbum album)
        {
            ChangeProgressText(SystemUtil.GetStringResource("Progress_LoadingLocalContents"));
            var group = new List<ThumbnailData>();
            await Task.Run(() =>
            {
                foreach (var pic in album.Pictures)
                {
                    group.Add(new ThumbnailData(pic));
                }
            });
            group.Reverse();

            await SystemUtil.GetCurrentDispatcher().RunAsync(CoreDispatcherPriority.Normal, () =>
            {
                if (group != null)
                {
                    groups.Group = new ObservableCollection<ThumbnailData>(group);
                }
            });
            HideProgress();
        }
         * */

        private string CurrentUuid { set; get; }

#if DEBUG
        private async void AddDummyContentsAsync()
        {
            TargetDevice.Status.StorageAccessSupported = true;
            UnlockPivot();

            if (CurrentUuid == null)
            {
                CurrentUuid = DummyContentsGenerator.RandomUuid();
            }

            for (int i = 0; i < 1; i++)
            {
                foreach (var date in DummyContentsGenerator.RandomDateList(50))
                {
                    var list = new List<RemoteThumbnail>();
                    foreach (var content in DummyContentsGenerator.RandomContentList(50))
                    {
                        list.Add(new RemoteThumbnail(CurrentUuid, date, content));
                    }
                    await Task.Delay(500);
                    await SystemUtil.GetCurrentDispatcher().RunAsync(CoreDispatcherPriority.Normal, () =>
                    {
                        if (GridSource != null)
                        {
                            GridSource.AddRange(list);
                        }
                    });
                }
                await Task.Delay(500);
            }
        }
#endif

        private CancellationTokenSource Canceller;

        private DateGroupCollection GridSource;

        private bool CheckRemoteCapability()
        {
            if (TargetDevice == null)
            {
                DebugUtil.Log("Device not found");
                return false;
            }
            CurrentUuid = TargetDevice.Udn;

            if (TargetDevice.Api.AvContent == null)
            {
                DebugUtil.Log("AvContent service is not supported");
                return false;
            }

            return true;
        }

        private async void OnStorageAvailabilityChanged(bool availability)
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
                        InitializeRemote();
                    }
                }
            }
            else
            {
                DeleteRemoteGridFacially();
                ShowToast(SystemUtil.GetStringResource("Viewer_StorageDetached"));
                UpdateInnerState(ViewerState.RemoteNoMedia);
                if (TargetDevice != null && TargetDevice.Udn != null)
                {
                    await ThumbnailCacheLoader.INSTANCE.DeleteCache(TargetDevice.Udn);
                }
            }
        }

        private async void DeleteRemoteGridFacially()
        {
            IsRemoteInitialized = false;
            await SystemUtil.GetCurrentDispatcher().RunAsync(CoreDispatcherPriority.Normal, () => { GridSource.Clear(); });
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

        private async void InitializeRemote()
        {
            IsRemoteInitialized = true;
            if (TargetDevice == null)
            {
                return;
            }

            try
            {
                ChangeProgressText(SystemUtil.GetStringResource("Progress_ChangingCameraState"));
                await PlaybackModeHelper.MoveToContentTransferModeAsync(TargetDevice.Api.Camera, TargetDevice.Status, 20000);

                ChangeProgressText(SystemUtil.GetStringResource("Progress_CheckingStorage"));
                if (!await PlaybackModeHelper.IsStorageSupportedAsync(TargetDevice.Api.AvContent))
                {
                    // This will never happen no camera devices.
                    DebugUtil.Log("storage scheme is not supported");
                    ShowToast(SystemUtil.GetStringResource("Viewer_StorageAccessNotSupported"));
                    return;
                }

                var storages = await PlaybackModeHelper.GetStoragesUriAsync(TargetDevice.Api.AvContent);
                if (storages.Count == 0)
                {
                    DebugUtil.Log("No storages");
                    ShowToast(SystemUtil.GetStringResource("Viewer_NoStorage"));
                    return;
                }

                Canceller = new CancellationTokenSource();

                ChangeProgressText(SystemUtil.GetStringResource("Progress_FetchingContents"));
                await PlaybackModeHelper.GetDateListAsEventsAsync(TargetDevice.Api.AvContent, storages[0], OnDateListUpdated, Canceller);
            }
            catch (Exception e)
            {
                DebugUtil.Log(e.StackTrace);
                HideProgress();
                ShowToast(SystemUtil.GetStringResource("Viewer_FailedToRefreshContents"));
            }
        }

        private async void OnDateListUpdated(DateListEventArgs args)
        {
            if (InnerState == ViewerState.OutOfPage) return;

            foreach (var date in args.DateList)
            {
                try
                {
                    ChangeProgressText(SystemUtil.GetStringResource("Progress_FetchingContents"));
                    await PlaybackModeHelper.GetContentsOfDayAsEventsAsync(
                        TargetDevice.Api.AvContent, date, true, OnContentListUpdated, Canceller);
                    HideProgress();
                }
                catch (Exception e)
                {
                    DebugUtil.Log(e.StackTrace);
                    // Ignore each error
                }
            }
        }

        private async void OnContentListUpdated(ContentListEventArgs args)
        {
            if (InnerState == ViewerState.OutOfPage) return;

            var list = new List<RemoteThumbnail>();
            foreach (var content in args.ContentList)
            {
                list.Add(new RemoteThumbnail(TargetDevice.Udn, args.DateInfo, content));
            }

            await SystemUtil.GetCurrentDispatcher().RunAsync(CoreDispatcherPriority.Normal, () =>
            {
                if (GridSource != null)
                {
                    GridSource.AddRange(list);
                }
            });
        }

        private async void HideProgress()
        {
            await SystemUtil.GetCurrentDispatcher().RunAsync(CoreDispatcherPriority.Normal, async () =>
            {
                await statusBar.HideAsync();
            });
        }

        private async void ChangeProgressText(string text)
        {
            DebugUtil.Log(text);
            await SystemUtil.GetCurrentDispatcher().RunAsync(CoreDispatcherPriority.Normal, async () =>
            {
                statusBar.ProgressIndicator.Text = text;
                await statusBar.ShowAsync();
            });
        }

        /*
        private async void OnFetched(Picture pic, Geoposition pos)
        {
            if (InnerState == ViewerState.OutOfPage) return;

            DebugUtil.Log("ViewerPage: OnFetched");
            await SystemUtil.GetCurrentDispatcher().RunAsync(CoreDispatcherPriority.Normal, () =>
            {
                var groups = LocalImageGrid.DataContext as ThumbnailGroup;
                if (groups == null)
                {
                    return;
                }
                groups.Group.Insert(0, new ThumbnailData(pic));
            });
        }

        private void OnDLError(ImageDLError error)
        {
            DebugUtil.Log("ViewerPage: OnDLError");
            // TODO show toast according to error cause...
        }
         * */

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

        /*
        private async void ThumbnailImage_Tap(object sender, System.Windows.Input.GestureEventArgs e)
        {
            if (IsViewingDetail)
            {
                return;
            }
            ChangeProgressText(SystemUtil.GetStringResource("Progress_OpeningDetailImage"));
            var img = sender as Image;
            var thumb = img.DataContext as ThumbnailData;
            await Task.Run(async () =>
            {
                await SystemUtil.GetCurrentDispatcher().RunAsync(CoreDispatcherPriority.Normal, () =>
                {
                    try
                    {
                        using (var strm = thumb.picture.GetImage())
                        {
                            using (var replica = new MemoryStream())
                            {
                                strm.CopyTo(replica); // Copy to the new stream to avoid stream crash issue.
                                if (replica.Length <= 0)
                                {
                                    return;
                                }
                                replica.Seek(0, SeekOrigin.Begin);

                                var _bitmap = new BitmapImage();
                                _bitmap.SetSource(replica.AsRandomAccessStream());
                                PhotoPlaybackScreen.SourceBitmap = _bitmap;
                                InitBitmapBeforeOpen();
                                PhotoPlaybackScreen.SetBitmap();
                                try
                                {
                                    PhotoData.MetaData = NtImageProcessor.MetaData.JpegMetaDataParser.ParseImage((Stream)replica);
                                }
                                catch (UnsupportedFileFormatException)
                                {
                                    PhotoData.MetaData = null;
                                    PhotoPlaybackScreen.DetailInfoVisibility = Visibility.Collapsed;
                                }
                                SetStillDetailVisibility(true);
                            }
                        }
                    }
                    catch (InvalidOperationException)
                    {
                        ShowToast(SystemUtil.GetStringResource("Viewer_FailedToOpenDetail"));
                    }
                });
            });
        }

        private void ImageGrid_Loaded(object sender, RoutedEventArgs e)
        {
            var selector = sender as LongListMultiSelector;
            selector.ItemsSource = GridSource;
        }

        private void ImageGrid_Unloaded(object sender, RoutedEventArgs e)
        {
            var selector = sender as LongListMultiSelector;
            selector.ItemsSource = null;
        }

        private void ImageGrid_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            var selector = sender as LongListMultiSelector;
            if (selector.IsSelectionEnabled)
            {
                DebugUtil.Log("SelectionChanged in multi mode");
                var contents = selector.SelectedItems;
                DebugUtil.Log("Selected Items: " + contents.Count);
                if (contents.Count > 0)
                {
                    UpdateInnerState(ViewerState.RemoteSelecting);
                }
                else
                {
                    UpdateInnerState(ViewerState.RemoteMulti);
                }
            }
        }

        private void SetStillDetailVisibility(bool visible)
        {
            if (visible)
            {
                PivotRoot.IsLocked = true;
                HideProgress();
                IsViewingDetail = true;
                PhotoPlaybackScreen.Visibility = Visibility.Visible;
                RemoteImageGrid.IsEnabled = false;
                LocalImageGrid.IsEnabled = false;
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
                UnlockPivot();
                HideProgress();
                IsViewingDetail = false;
                PhotoPlaybackScreen.Visibility = Visibility.Collapsed;
                RemoteImageGrid.IsEnabled = true;
                LocalImageGrid.IsEnabled = true;
                if (PivotRoot.SelectedIndex == 0)
                {
                    UpdateInnerState(ViewerState.Local);
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
            PhotoPlaybackScreen.Init();
        }

        private void viewport_ManipulationStarted(object sender, System.Windows.Input.ManipulationStartedEventArgs e)
        {
            PhotoPlaybackScreen.viewport_ManipulationStarted(sender, e);
        }

        private void viewport_ManipulationDelta(object sender, System.Windows.Input.ManipulationDeltaEventArgs e)
        {
            PhotoPlaybackScreen.viewport_ManipulationDelta(sender, e);
        }

        private void viewport_ManipulationCompleted(object sender, System.Windows.Input.ManipulationCompletedEventArgs e)
        {
            PhotoPlaybackScreen.viewport_ManipulationCompleted(sender, e);
        }

        private void PhoneApplicationPage_BackKeyPress(object sender, CancelEventArgs e)
        {
            if (IsViewingDetail)
            {
                ReleaseDetail();
                e.Cancel = true;
            }
            if (MovieDrawer.Visibility == Visibility.Visible || MovieStreamHelper.INSTANCE.IsProcessing)
            {
                CloseMovieStream();
                e.Cancel = true;
            }

            if (RemoteImageGrid.IsSelectionEnabled)
            {
                RemoteImageGrid.IsSelectionEnabled = false;
                e.Cancel = true;
            }

            if (AppSettingPanel.Visibility == Visibility.Visible)
            {
                CloseAppSettingPanel();
                e.Cancel = true;
            }
        }

        private void ReleaseDetail()
        {
            PhotoPlaybackScreen.ReleaseImage();
            SetStillDetailVisibility(false);
            // poor codes to avoid LongListMultiSelector freezing
            LocalImageGrid.Margin = new Thickness(0.1);
            LocalImageGrid.UpdateLayout();
            LocalImageGrid.Margin = new Thickness(0);
            LocalImageGrid.UpdateLayout();
        }

        private bool IsViewingDetail = false;

        private async void ImageGrid_ItemRealized(object sender, ItemRealizationEventArgs e)
        {
            if (e.ItemKind == LongListSelectorItemKind.Item)
            {
                var content = e.Container.Content as RemoteThumbnail;
                if (content != null)
                {
                    await content.FetchThumbnailAsync();
                }
            }
        }
        */

        private void Pivot_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            // RemoteImageGrid.IsSelectionEnabled = false;
            var pivot = sender as Pivot;
            switch (pivot.SelectedIndex)
            {
                case 0:
                    UpdateInnerState(ViewerState.Local);
                    break;
                case 1:
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
                                InitializeRemote();
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
                    break;
            }
        }

        /*
        private void DeleteSelectedImages()
        {
            var items = RemoteImageGrid.SelectedItems;
            if (items.Count == 0)
            {
                HideProgress();
                return;
            }
            var contents = new TargetContents();
            contents.ContentUris = new List<string>();
            foreach (var item in items)
            {
                var data = item as RemoteThumbnail;
                contents.ContentUris.Add(data.Source.Uri);
            }
            DeleteContents(contents);
            RemoteImageGrid.IsSelectionEnabled = false;
        }

        private void FetchSelectedImages()
        {
            var items = RemoteImageGrid.SelectedItems;
            if (items.Count == 0)
            {
                HideProgress();
                return;
            }
            foreach (var item in items)
            {
                try
                {
                    EnqueueImageDownload(item as RemoteThumbnail);
                }
                catch (Exception e)
                {
                    DebugUtil.Log(e.StackTrace);
                }
            }
            RemoteImageGrid.IsSelectionEnabled = false;
        }

        private void EnqueueImageDownload(RemoteThumbnail source)
        {
            if (ApplicationSettings.GetInstance().PrioritizeOriginalSizeContents && source.Source.OriginalUrl != null)
            {
                PictureSyncManager.Instance.Enqueue(new Uri(source.Source.OriginalUrl));
                return;
            }
            // Fallback to large size image
            PictureSyncManager.Instance.Enqueue(new Uri(source.Source.LargeUrl));
        }
        */

        private async void ShowToast(string message)
        {
            await SystemUtil.GetCurrentDispatcher().RunAsync(CoreDispatcherPriority.Normal, () =>
            {
                ToastMessage.Text = message;
                ToastApparance.Begin();
            });
        }

        private async void ToastApparance_Completed(object sender, object e)
        {
            await Task.Delay(3000);
            await Dispatcher.RunAsync(CoreDispatcherPriority.Normal, () => { ToastDisApparance.Begin(); });
        }

        /*
        private void RemoteImageGrid_IsSelectionEnabledChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
            var selector = (sender as LongListMultiSelector);
            if (!selector.IsSelectionEnabled && GridSource != null)
            {
                GridSource.SelectivityFactor = SelectivityFactor.None;
                HeaderBlocker.Visibility = Visibility.Collapsed;
                UnlockPivot();
            }
            if (selector.IsSelectionEnabled && GridSource != null)
            {
                switch (GridSource.SelectivityFactor)
                {
                    case SelectivityFactor.CopyToPhone:
                        HeaderBlockerText.Text = SystemUtil.GetStringResource("Viewer_Header_SelectingToDownload");
                        HeaderBlocker.Visibility = Visibility.Visible;
                        break;
                    case SelectivityFactor.Delete:
                        HeaderBlockerText.Text = SystemUtil.GetStringResource("Viewer_Header_SelectingToDelete");
                        HeaderBlocker.Visibility = Visibility.Visible;
                        break;
                }
                PivotRoot.IsLocked = true;
            }
            if (PivotRoot.SelectedIndex == 1)
            {
                if (selector.IsSelectionEnabled)
                {
                    UpdateInnerState(ViewerState.RemoteMulti);
                }
                else
                {
                    UpdateInnerState(ViewerState.RemoteSingle);
                }
            }
        }

        private void PhoneApplicationPage_OrientationChanged(object sender, OrientationChangedEventArgs e)
        {
            DebugUtil.Log("Orientation changed: " + e.Orientation);
            switch (e.Orientation)
            {
                case PageOrientation.LandscapeLeft:
                    MoviePlaybackScreen.Margin = new Thickness(12, 12, 72, 12);
                    break;
                case PageOrientation.LandscapeRight:
                    MoviePlaybackScreen.Margin = new Thickness(72, 12, 12, 12);
                    break;
                case PageOrientation.Portrait:
                    MoviePlaybackScreen.Margin = new Thickness(12);
                    break;
            }
        }

        private void RemoteThumbnail_Tap(object sender, System.Windows.Input.GestureEventArgs e)
        {
            if (RemoteImageGrid.IsSelectionEnabled)
            {
                DebugUtil.Log("Ignore tap in multi-selection mode.");
                return;
            }

            var image = sender as Image;
            var content = image.DataContext as RemoteThumbnail;
            PlaybackContent(content);
        }

        private void Playback_Click(object sender, RoutedEventArgs e)
        {
            var item = sender as MenuItem;
            var content = item.DataContext as RemoteThumbnail;
            PlaybackContent(content);
        }

        private async void PlaybackContent(RemoteThumbnail content)
        {
            if (content != null)
            {
                switch (content.Source.ContentType)
                {
                    case ContentKind.StillImage:
                        ChangeProgressText(SystemUtil.GetStringResource("Progress_OpeningDetailImage"));
                        try
                        {
                            using (var strm = await Downloader.GetResponseStreamAsync(new Uri(content.Source.LargeUrl)))
                            {
                                var replica = new MemoryStream();

                                strm.CopyTo(replica); // Copy to the new stream to avoid stream crash issue.
                                if (replica.Length <= 0)
                                {
                                    return;
                                }
                                replica.Seek(0, SeekOrigin.Begin);

                                await SystemUtil.GetCurrentDispatcher().RunAsync(CoreDispatcherPriority.Normal, () =>
                                {
                                    try
                                    {
                                        var _bitmap = new BitmapImage();
                                        _bitmap.SetSource(replica.AsRandomAccessStream());
                                        PhotoPlaybackScreen.SourceBitmap = _bitmap;
                                        InitBitmapBeforeOpen();
                                        PhotoPlaybackScreen.SetBitmap();
                                        try
                                        {
                                            PhotoData.MetaData = NtImageProcessor.MetaData.JpegMetaDataParser.ParseImage((Stream)replica);
                                        }
                                        catch (UnsupportedFileFormatException)
                                        {
                                            PhotoData.MetaData = null;
                                            PhotoPlaybackScreen.DetailInfoVisibility = System.Windows.Visibility.Collapsed;
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
                        var av = TargetDevice.Api.AvContent;

                        if (av != null)
                        {
                            if (content.Source.RemotePlaybackAvailable)
                            {
                                PivotRoot.IsLocked = true;
                                UpdateInnerState(ViewerState.RemoteMoviePlayback);
                                MovieDrawer.Visibility = Visibility.Visible;
                                ChangeProgressText(SystemUtil.GetStringResource("Progress_OpeningMovieStream"));
                                var started = await MovieStreamHelper.INSTANCE.Start(av, new PlaybackContent
                                {
                                    Uri = content.Source.Uri,
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
                        }
                        else
                        {
                            DebugUtil.Log("Not ready to start stream: " + content.Source.Uri);
                            ShowToast(SystemUtil.GetStringResource("Viewer_NoAvContentApi"));
                        }
                        break;
                    default:
                        break;
                }
            }
        }

        private void CopyToPhone_Click(object sender, RoutedEventArgs e)
        {
            var item = sender as MenuItem;
            try
            {
                EnqueueImageDownload(item.DataContext as RemoteThumbnail);
            }
            catch (Exception ex)
            {
                DebugUtil.Log(ex.StackTrace);
            }
        }

        private void Delete_Click(object sender, RoutedEventArgs e)
        {
            var item = sender as MenuItem;
            var data = item.DataContext as RemoteThumbnail;

            var contents = new TargetContents();
            contents.ContentUris = new List<string>();
            contents.ContentUris.Add(data.Source.Uri);
            DeleteContents(contents);
        }
        */

        private async void DeleteContents(TargetContents contents)
        {
            var av = TargetDevice.Api.AvContent;
            if (av != null && contents != null)
            {
                ChangeProgressText(SystemUtil.GetStringResource("Progress_DeletingSelectedContents"));
                try
                {
                    await av.DeleteContentAsync(contents);
                    DeleteRemoteGridFacially();
                    if (PivotRoot.SelectedIndex == 1)
                    {
                        InitializeRemote();
                    }
                }
                catch
                {
                    DebugUtil.Log("Failed to delete contents");
                    ShowToast(SystemUtil.GetStringResource("Viewer_FailedToDeleteContents"));
                    HideProgress();
                }
            }
            DebugUtil.Log("Not ready to delete contents");
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
        }

        private void PivotRoot_Loaded(object sender, RoutedEventArgs e)
        {
            if (TargetDevice == null)
            {
                return;
            }

            var pivot = sender as Pivot;
            pivot.IsLocked = !TargetDevice.Status.StorageAccessSupported;
        }

        private void ThumbnailImage_Tapped(object sender, Windows.UI.Xaml.Input.TappedRoutedEventArgs e)
        {

        }
    }

    public enum ViewerState
    {
        Local,
        LocalStillPlayback,
        RemoteUnsupported,
        RemoteNoMedia,
        RemoteSingle,
        RemoteMulti,
        RemoteSelecting,
        Sync,
        RemoteStillPlayback,
        RemoteMoviePlayback,
        AppSettings,
        OutOfPage,
    }
}
