using Kazyx.Uwpmm.Utility;
using System;
using System.IO;
using System.Reflection;
using Windows.ApplicationModel;
using Windows.UI.Core;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Documents;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;

// The Blank Page item template is documented at http://go.microsoft.com/fwlink/?LinkID=390556

namespace Kazyx.Uwpmm.Pages
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class AboutPage : Page
    {
        public AboutPage()
        {
            this.InitializeComponent();
        }

        /// <summary>
        /// Invoked when this page is about to be displayed in a Frame.
        /// </summary>
        /// <param name="e">Event data that describes how this page was reached.
        /// This parameter is typically used to configure the page.</param>
        protected override void OnNavigatedTo(NavigationEventArgs e)
        {
        }

        private static bool IsManifestLoaded = false;
        private static string version = "";
        private static string license = "";
        private static string copyright = "Copyright (c) 2015 kazyx";
        private const string developer = "kazyx and naotaco (@naotaco_dev)";

        private void Page_Loaded(object sender, RoutedEventArgs e)
        {
            if (!IsManifestLoaded)
            {
                LoadManifestXmlValues();
            }
            VERSION_STR.Text = version;

            FormatRichText(Repository, SystemUtil.GetStringResource("RepoURL"));

            COPYRIGHT.Text = copyright;

            DEV_BY.Text = developer;

            LoadLicenseFile();
        }

        private static void LoadManifestXmlValues()
        {
            var assembly = (typeof(App)).GetTypeInfo().Assembly;
            version = assembly.GetName().Version.ToString();

            //var element = XDocument.Load("Package.appxmanifest").Root.Element("App");
            //version = element.Attribute("Version").Value;
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

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            /*
            var task = new ShareStatusTask();
            task.Status = "@scrap_support ";
            task.Show();
             * */
        }
    }
}
