using Kazyx.Uwpmm.Common;
using Kazyx.Uwpmm.Utility;
using System;
using System.IO;
using System.Reflection;
using Windows.ApplicationModel;
using Windows.System;
using Windows.UI.Core;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Documents;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;

namespace Kazyx.Uwpmm.Pages
{
    public sealed partial class AboutPage : Page
    {
        private NavigationHelper navigationHelper;

        public AboutPage()
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
        }

        protected override void OnNavigatedFrom(NavigationEventArgs e)
        {
            this.navigationHelper.OnNavigatedFrom(e);
        }

        #endregion

        private static bool IsManifestLoaded = false;
        private static string version = "";
        private static string license = "";
        private static string copyright = "";
        private const string developer = "kazyx and naotaco (@naotaco_dev)";

        private void Page_Loaded(object sender, RoutedEventArgs e)
        {
            if (!IsManifestLoaded)
            {
                LoadAssemblyInformation();
            }
            VERSION_STR.Text = version;

            FormatRichText(Repository, SystemUtil.GetStringResource("RepoURL"));

            COPYRIGHT.Text = copyright;

            DEV_BY.Text = developer;

            LoadLicenseFile();
        }

        private static void LoadAssemblyInformation()
        {
            var assembly = (typeof(App)).GetTypeInfo().Assembly;
            version = assembly.GetName().Version.ToString();
            foreach (var attr in assembly.CustomAttributes)
            {
                if (attr.AttributeType == typeof(AssemblyCopyrightAttribute))
                {
                    copyright = attr.ConstructorArguments[0].Value.ToString();
                    break;
                }
            }
        }

        private async void LoadLicenseFile()
        {
            if (string.IsNullOrEmpty(license))
            {
                var installedFolder = Package.Current.InstalledLocation;
                var folder = await installedFolder.GetFolderAsync("Assets");
                var file = await folder.GetFileAsync("License.txt");
                var stream = await file.OpenReadAsync();
                var reader = new StreamReader(stream.AsStreamForRead());
                license = reader.ReadToEnd();
                license = license.Replace("\r\n", "\n").Replace("\r", "\n").Replace("\n", "\r\n"); // Avoid autocrlf effect
            }
            await SystemUtil.GetCurrentDispatcher().RunAsync(CoreDispatcherPriority.Normal, () =>
            {
                FormatRichText(Contents, license);
            });
        }

        private static void FormatRichText(Paragraph place, string text)
        {
            if (text != null && text.Length != 0)
            {
                char[] separators = { ' ', '\n', '\t', '　' };
                var words = text.Split(separators);
                foreach (var word in words)
                {
                    if (word.StartsWith("http://") || word.StartsWith("https://"))
                    {
                        place.Inlines.Add(GetAsLink(word));
                        place.Inlines.Add(new Run()
                        {
                            Text = " ",
                        });
                    }
                    else
                    {
                        place.Inlines.Add(new Run()
                        {
                            Text = word + " ",
                        });
                    }
                }
            }
        }

        private static Hyperlink GetAsLink(string word)
        {
            var hl = new Hyperlink
            {
                NavigateUri = new Uri(word),
                Foreground = (SolidColorBrush)(Application.Current.Resources["ProgressBarForegroundThemeBrush"]),
            };

            hl.Inlines.Add(new Run()
            {
                Text = word
            });

            return hl;
        }

        private async void Support_Click(object sender, RoutedEventArgs e)
        {
            var success = await Launcher.LaunchUriAsync(new Uri(@"https://twitter.com/scrap_support"));
            if (!success) DebugUtil.Log("Failed to open FAQ page.");
        }

        private async void FAQ_Click(object sender, RoutedEventArgs e)
        {
            var success = await Launcher.LaunchUriAsync(new Uri(@"http://locana.github.io/locana_faq.html"));
            if (!success) DebugUtil.Log("Failed to open FAQ page.");
        }
    }
}
