using Kazyx.Uwpmm.Common;
using Kazyx.Uwpmm.Pages;
using Kazyx.Uwpmm.Utility;
using System;
using Windows.ApplicationModel;
using Windows.ApplicationModel.Activation;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Media.Animation;
using Windows.UI.Xaml.Navigation;

// The Universal Hub Application project template is documented at http://go.microsoft.com/fwlink/?LinkID=391955

namespace Kazyx.Uwpmm
{
    /// <summary>
    /// Provides application-specific behavior to supplement the default Application class.
    /// </summary>
    public sealed partial class App : Application
    {
#if WINDOWS_PHONE_APP
        private TransitionCollection transitions;
#endif

        /// <summary>
        /// Initializes the singleton instance of the <see cref="App"/> class. This is the first line of authored code
        /// executed, and as such is the logical equivalent of main() or WinMain().
        /// </summary>
        public App()
        {
            this.InitializeComponent();
            this.Suspending += this.OnSuspending;
            this.Resuming += OnResuming;
        }

        public bool IsFunctionLimited
        {
            private set;
            get;
        }

        public bool IsTrialVersion
        {
            private set;
            get;
        }

        /// <summary>
        /// Invoked when the application is launched normally by the end user.  Other entry points
        /// will be used when the application is launched to open a specific file, to display
        /// search results, and so forth.
        /// </summary>
        /// <param name="e">Details about the launch request and process.</param>
        protected override async void OnLaunched(LaunchActivatedEventArgs e)
        {
            DebugUtil.Log("OnLaunched");
#if DEBUG
            if (System.Diagnostics.Debugger.IsAttached)
            {
                this.DebugSettings.EnableFrameRateCounter = true;
            }
#endif

            var init = Preference.InitialLaunchedDateTime;
            DebugUtil.Log("Initial launched datetime: " + init.ToString());
#if DEBUG
            IsTrialVersion = true;
#else
            IsTrialVersion = Windows.ApplicationModel.Store.CurrentApp.LicenseInformation.IsTrial;
#endif
            if (IsTrialVersion)
            {
                var diff = DateTimeOffset.Now.Subtract(init);
                IsFunctionLimited = diff.Days > 90;
            }
            else
            {
                IsFunctionLimited = false;
            }

            Frame rootFrame = Window.Current.Content as Frame;

            // Do not repeat app initialization when the Window already has content,
            // just ensure that the window is active
            if (rootFrame == null)
            {
                // Create a Frame to act as the navigation context and navigate to the first page
                rootFrame = new Frame();

                //Associate the frame with a SuspensionManager key                                
                SuspensionManager.RegisterFrame(rootFrame, "AppFrame");

                // TODO: change this value to a cache size that is appropriate for your application
                rootFrame.CacheSize = 1;

#if WINDOWS_APP
                if (e.PreviousExecutionState == ApplicationExecutionState.Terminated)
                {
                    // Restore the saved session state only when appropriate
                    try
                    {
                        await SuspensionManager.RestoreAsync();
                    }
                    catch (SuspensionManagerException)
                    {
                        // Something went wrong restoring state.
                        // Assume there is no state and continue
                    }
                }
#endif

                // Place the frame in the current Window
                Window.Current.Content = rootFrame;
            }

            if (rootFrame.Content == null)
            {
#if WINDOWS_PHONE_APP
                // Removes the turnstile navigation for startup.
                if (rootFrame.ContentTransitions != null)
                {
                    this.transitions = new TransitionCollection();
                    foreach (var c in rootFrame.ContentTransitions)
                    {
                        this.transitions.Add(c);
                    }
                }

                rootFrame.ContentTransitions = null;
                rootFrame.Navigated += this.RootFrame_FirstNavigated;
#endif

                // When the navigation stack isn't restored navigate to the first page,
                // configuring the new page by passing required information as a navigation
                // parameter
                if (!rootFrame.Navigate(typeof(MainPage), e.Arguments))
                {
                    throw new Exception("Failed to create initial page");
                }
            }

#if WINDOWS_PHONE_APP
            NetworkObserver.INSTANCE.Start();
#endif

            // Ensure the current window is active
            Window.Current.Activate();
        }

#if WINDOWS_PHONE_APP
        /// <summary>
        /// Restores the content transitions after the app has launched.
        /// </summary>
        /// <param name="sender">The object where the handler is attached.</param>
        /// <param name="e">Details about the navigation event.</param>
        private void RootFrame_FirstNavigated(object sender, NavigationEventArgs e)
        {
            var rootFrame = sender as Frame;
            rootFrame.ContentTransitions = this.transitions ?? new TransitionCollection() { new NavigationThemeTransition() };
            rootFrame.Navigated -= this.RootFrame_FirstNavigated;
        }
#endif

        /// <summary>
        /// Invoked when application execution is being suspended.  Application state is saved
        /// without knowing whether the application will be terminated or resumed with the contents
        /// of memory still intact.
        /// </summary>
        private async void OnSuspending(object sender, SuspendingEventArgs e)
        {
            DebugUtil.Log("OnSuspending");
            NetworkObserver.INSTANCE.Finish();

#if WINDOWS_PHONE_APP
            var rootFrame = Window.Current.Content as Frame;
            if (rootFrame.Content is MainPage)
            {
                var page = rootFrame.Content as MainPage;
                page.OnSuspending();
                return;
            }
            else if (rootFrame.Content is HiddenPage)
            {
                // Only HiddenPage should not be closed
                return;
            }
            else if (rootFrame.CanGoBack)
            {
                // Other pages should be closed to open Entrance page
                rootFrame.GoBack();
                return;
            }
#else
            var deferral = e.SuspendingOperation.GetDeferral();
            await SuspensionManager.SaveAsync();
            deferral.Complete();
#endif
        }

        void OnResuming(object sender, object e)
        {
            DebugUtil.Log("OnResuming");
#if WINDOWS_PHONE_APP
            var rootFrame = Window.Current.Content as Frame;
            NetworkObserver.INSTANCE.Start();
            if (rootFrame.Content is MainPage)
            {
                var page = rootFrame.Content as MainPage;
                page.OnResuming();
                return;
            }
#endif
        }
    }
}
