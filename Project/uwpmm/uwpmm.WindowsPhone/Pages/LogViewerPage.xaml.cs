#if DEBUG
using Kazyx.Uwpmm.Utility;
using System.Collections.Generic;
using System;
#endif
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Navigation;
using Kazyx.Uwpmm.Common;
using Windows.UI.Core;

namespace Kazyx.Uwpmm.Pages
{
    public partial class LogViewerPage : Page
    {
        private NavigationHelper navigationHelper;
        private ObservableDictionary defaultViewModel = new ObservableDictionary();

        public LogViewerPage()
        {
            InitializeComponent();

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
#if DEBUG
            LoadLogFiles();
#endif
        }

#if DEBUG
        private async void LoadLogFiles()
        {
            await DebugUtil.Flush();
            var files = await DebugUtil.LogFiles();
            await Dispatcher.RunAsync(CoreDispatcherPriority.Normal, () =>
            {
                DefaultViewModel.Add("LogFilesList", files);
            });
        }
#endif

        protected override void OnNavigatedFrom(NavigationEventArgs e)
        {
            this.navigationHelper.OnNavigatedFrom(e);
        }

        #endregion

#if DEBUG
        List<string> files = new List<string>();
#endif

        private async void FileListBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
#if DEBUG
            var box = sender as ListView;
            var filepath = box.SelectedValue as string;
            ContentHeader.Text = filepath;
            var text = await DebugUtil.GetFile(filepath);
            await Dispatcher.RunAsync(CoreDispatcherPriority.Normal, () =>
            {
                DefaultViewModel.Remove("LogContentText");
                DefaultViewModel.Add("LogContentText", text);
            });
#endif
        }

        private void LogContent_Tapped(object sender, Windows.UI.Xaml.Input.TappedRoutedEventArgs e)
        {
#if DEBUG
            var textblock = sender as TextBlock;
            // DebugUtil.ComposeDebugMail(textblock.Text);
#endif

        }
    }
}