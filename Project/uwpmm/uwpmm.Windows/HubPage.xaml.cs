using Kazyx.DeviceDiscovery;
using Kazyx.Liveview;
using Kazyx.RemoteApi;
using Kazyx.Uwpmm.CameraControl;
using Kazyx.Uwpmm.Common;
using Kazyx.Uwpmm.Data;
using Kazyx.Uwpmm.DataModel;
using Kazyx.Uwpmm.Utility;
using System;
using System.Collections.ObjectModel;
using Windows.UI.Core;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Navigation;

// The Universal Hub Application project template is documented at http://go.microsoft.com/fwlink/?LinkID=391955

namespace Kazyx.Uwpmm
{
    /// <summary>
    /// A page that displays a grouped collection of items.
    /// </summary>
    public sealed partial class HubPage : Page
    {
        private NavigationHelper navigationHelper;
        private ObservableDictionary defaultViewModel = new ObservableDictionary();

        /// <summary>
        /// Gets the NavigationHelper used to aid in navigation and process lifetime management.
        /// </summary>
        public NavigationHelper NavigationHelper
        {
            get { return this.navigationHelper; }
        }

        /// <summary>
        /// Gets the DefaultViewModel. This can be changed to a strongly typed view model.
        /// </summary>
        public ObservableDictionary DefaultViewModel
        {
            get { return this.defaultViewModel; }
        }

        public HubPage()
        {
            this.InitializeComponent();
            this.navigationHelper = new NavigationHelper(this);
            this.navigationHelper.LoadState += this.NavigationHelper_LoadState;
        }

        /// <summary>
        /// Populates the page with content passed during navigation.  Any saved state is also
        /// provided when recreating a page from a prior session.
        /// </summary>
        /// <param name="sender">
        /// The source of the event; typically <see cref="NavigationHelper"/>
        /// </param>
        /// <param name="e">Event data that provides both the navigation parameter passed to
        /// <see cref="Frame.Navigate(Type, object)"/> when this page was initially requested and
        /// a dictionary of state preserved by this page during an earlier
        /// session.  The state will be null the first time a page is visited.</param>
        private async void NavigationHelper_LoadState(object sender, LoadStateEventArgs e)
        {
            // TODO: Create an appropriate data model for your problem domain to replace the sample data
            var sampleDataGroup = await SampleDataSource.GetGroupAsync("Group-4");
            this.DefaultViewModel["Section3Items"] = sampleDataGroup;
        }

        /// <summary>
        /// Invoked when a HubSection header is clicked.
        /// </summary>
        /// <param name="sender">The Hub that contains the HubSection whose header was clicked.</param>
        /// <param name="e">Event data that describes how the click was initiated.</param>
        void Hub_SectionHeaderClick(object sender, HubSectionHeaderClickEventArgs e)
        {
            HubSection section = e.Section;
            var group = section.DataContext;
            this.Frame.Navigate(typeof(SectionPage), ((SampleDataGroup)group).UniqueId);
        }

        /// <summary>
        /// Invoked when an item within a section is clicked.
        /// </summary>
        /// <param name="sender">The GridView or ListView
        /// displaying the item clicked.</param>
        /// <param name="e">Event data that describes the item clicked.</param>
        void ItemView_ItemClick(object sender, ItemClickEventArgs e)
        {
            // Navigate to the appropriate destination page, configuring the new page
            // by passing required information as a navigation parameter
            var itemId = ((SampleDataItem)e.ClickedItem).UniqueId;
            this.Frame.Navigate(typeof(ItemPage), itemId);
        }
        #region NavigationHelper registration

        /// <summary>
        /// The methods provided in this section are simply used to allow
        /// NavigationHelper to respond to the page's navigation methods.
        /// Page specific logic should be placed in event handlers for the  
        /// <see cref="Common.NavigationHelper.LoadState"/>
        /// and <see cref="Common.NavigationHelper.SaveState"/>.
        /// The navigation parameter is available in the LoadState method 
        /// in addition to page state preserved during an earlier session.
        /// </summary>
        protected override void OnNavigatedTo(NavigationEventArgs e)
        {
            this.navigationHelper.OnNavigatedTo(e);
            discovery.ScalarDeviceDiscovered += discovery_ScalarDeviceDiscovered;
        }

        protected override void OnNavigatedFrom(NavigationEventArgs e)
        {
            this.navigationHelper.OnNavigatedFrom(e);
            discovery.ScalarDeviceDiscovered -= discovery_ScalarDeviceDiscovered;
            liveview.CloseConnection();
        }

        #endregion

        private DeviceApiHolder api = null;
        private SoDiscovery discovery = new SoDiscovery();
        private LvStreamProcessor liveview = new LvStreamProcessor();
        private LiveviewImage liveview_data = new LiveviewImage();

        private ObservableCollection<string> logs = new ObservableCollection<string>();

        private async void Log(string message)
        {
            await Dispatcher.RunAsync(CoreDispatcherPriority.Low, () =>
            {
                logs.Add(message);
            });
        }

        void discovery_ScalarDeviceDiscovered(object sender, ScalarDeviceEventArgs e)
        {
            Log("A camera device is discovered: " + e.ScalarDevice.ModelName);
            api = new DeviceApiHolder(e.ScalarDevice);
        }

        async void liveview_JpegRetrieved(object sender, JpegEventArgs e)
        {
            await LiveviewUtil.SetAsBitmap(e.JpegData, liveview_data, Dispatcher);
        }

        void liveview_Closed(object sender, EventArgs e)
        {
            Log("Liveview connection closed");
        }

        private void SearchButton_Click(object sender, Windows.UI.Xaml.RoutedEventArgs e)
        {
            api = null;
            discovery.SearchScalarDevices(TimeSpan.FromSeconds(5));
            Log("Trying to search camera device.");
        }

        private async void ConnectButton_Click(object sender, Windows.UI.Xaml.RoutedEventArgs e)
        {
            if (api == null)
            {
                Log("Camera device is not found yet.");
                return;
            }

            var result = await SequentialOperation.SetUp(api, liveview);
            Log(result ? "Set up completed" : "Failed to set up");
        }

        private async void TakePictureButton_Click(object sender, Windows.UI.Xaml.RoutedEventArgs e)
        {
            if (api == null)
            {
                Log("Camera device is not found yet.");
                return;
            }

            try
            {
                var result = await SequentialOperation.TakePicture(api);
                if (result)
                {
                    Log("Success takeing picture");
                }
                else
                {
                    Log("Failed to take picture");
                }
            }
            catch (RemoteApiException ex)
            {
                Log("Failed to take picture: " + ex.code);
            }
        }

        private void DebugMonitor_Loaded(object sender, Windows.UI.Xaml.RoutedEventArgs e)
        {
            var listbox = sender as ListBox;
            listbox.ItemsSource = logs;
        }

        private void DebugMonitor_Unloaded(object sender, Windows.UI.Xaml.RoutedEventArgs e)
        {
            var listbox = sender as ListBox;
            listbox.ItemsSource = null;
        }

        private void LiveviewImage_Loaded(object sender, Windows.UI.Xaml.RoutedEventArgs e)
        {
            var image = sender as Image;
            image.DataContext = liveview_data;
            liveview.JpegRetrieved += liveview_JpegRetrieved;
            liveview.Closed += liveview_Closed;
        }

        private void LiveviewImage_Unloaded(object sender, Windows.UI.Xaml.RoutedEventArgs e)
        {
            var image = sender as Image;
            image.DataContext = null;
            liveview.JpegRetrieved -= liveview_JpegRetrieved;
            liveview.Closed -= liveview_Closed;
        }
    }
}
