using Kazyx.RemoteApi.AvContent;
using Kazyx.Uwpmm.Utility;
using System;
using System.Text;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media.Imaging;

// The User Control item template is documented at http://go.microsoft.com/fwlink/?LinkId=234236

namespace Kazyx.Uwpmm.Control
{
    public sealed partial class MoviePlaybackScreen : UserControl
    {
        public MoviePlaybackScreen()
        {
            this.InitializeComponent();
            SeekBar.AddHandler(PointerReleasedEvent, new PointerEventHandler(Slider_PointerReleased), true);

        }

        public event EventHandler<SeekBarOperationArgs> SeekOperated;
        public event EventHandler<PlaybackRequestArgs> OnPlaybackOperationRequested;

        private void Slider_PointerReleased(object sender, PointerRoutedEventArgs e)
        {
            if (SeekOperated != null && Duration.TotalMilliseconds > 0)
            {
                SeekOperated(this, new SeekBarOperationArgs() { SeekPosition = TimeSpan.FromMilliseconds(Duration.TotalMilliseconds * (sender as Slider).Value / 1000) });
            }
        }

        public TimeSpan CurrentPosition
        {
            get { return (TimeSpan)GetValue(CurrentPositionProperty); }
            set
            {
                SetValue(CurrentPositionProperty, value);
                UpdatePlaybackPosition(value, this.Duration);
            }
        }

        public static readonly DependencyProperty CurrentPositionProperty = DependencyProperty.Register(
            "CurrentPosition",
            typeof(TimeSpan),
            typeof(MoviePlaybackScreen),
            new PropertyMetadata(new TimeSpan(0, 0, 0), new PropertyChangedCallback(MoviePlaybackScreen.OnCurrentPositionChanged)));

        private static void OnCurrentPositionChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            //DebugUtil.Log("Current position updated: " + ((TimeSpan)e.NewValue).TotalSeconds);
            (d as MoviePlaybackScreen).UpdatePlaybackPosition((TimeSpan)e.NewValue, (d as MoviePlaybackScreen).Duration);
        }

        void UpdatePlaybackPosition(TimeSpan current, TimeSpan duration)
        {
            if (duration.TotalMilliseconds <= 0)
            {
                PlaybackInfo.Visibility = Windows.UI.Xaml.Visibility.Collapsed;
                return;
            }
            double value = current.TotalMilliseconds / duration.TotalMilliseconds * 1000;
            if (value < 0 || value > 1000) { return; }
            this.SeekBar.Value = value;
            this.ProgressBar.Value = value;
            PositionText.Text = ToString(current);
            PlaybackInfo.Visibility = Windows.UI.Xaml.Visibility.Visible;
        }

        public TimeSpan Duration
        {
            get { return (TimeSpan)GetValue(DurationProperty); }
            set
            {
                SetValue(DurationProperty, value);
                UpdateDurationDisplay(value);
            }
        }

        public static readonly DependencyProperty DurationProperty = DependencyProperty.Register(
            "Duration",
            typeof(TimeSpan),
            typeof(MoviePlaybackScreen),
            new PropertyMetadata(new TimeSpan(0, 0, 0), new PropertyChangedCallback(MoviePlaybackScreen.OnDurationChanged)));

        private static void OnDurationChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            //DebugUtil.Log("Duration updated: " + ((TimeSpan)e.NewValue).TotalSeconds);
            (d as MoviePlaybackScreen).UpdateDurationDisplay((TimeSpan)e.NewValue);
        }

        void UpdateDurationDisplay(TimeSpan duration)
        {
            if (duration.TotalMilliseconds <= 0)
            {
                this.DurationText.Text = "--:--:--";
            }
            else
            {
                this.DurationText.Text = ToString(duration);
            }
        }

        private static string ToString(TimeSpan time)
        {
            StringBuilder sb = new StringBuilder();
            if (time.TotalMilliseconds < 0) { return "--:--:--"; }
            if (time.Hours > 0)
            {
                sb.Append(String.Format("{0:D2}", time.Hours));
                sb.Append(":");
            }
            sb.Append(String.Format("{0:D2}", time.Minutes));
            sb.Append(":");
            sb.Append(String.Format("{0:D2}", time.Seconds));
            return sb.ToString();
        }

        public static readonly DependencyProperty SeekAvailabilityProperty = DependencyProperty.Register(
            "SeekAvailable",
            typeof(bool),
            typeof(MoviePlaybackScreen),
            new PropertyMetadata(false, new PropertyChangedCallback(MoviePlaybackScreen.OnSeekAvailabilityChanged)));

        private static void OnSeekAvailabilityChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            //DebugUtil.Log("Seek availability changed: " + (bool)(e.NewValue));
            (d as MoviePlaybackScreen).UpdateBarDisplay((bool)(e.NewValue));
        }

        public bool SeekAvailable
        {
            get { return (bool)GetValue(SeekAvailabilityProperty); }
            set
            {
                SetValue(SeekAvailabilityProperty, value);
                UpdateBarDisplay(value);
            }
        }

        void UpdateBarDisplay(bool SeekAvailable)
        {
            if (SeekAvailable)
            {
                this.ProgressBar.Visibility = Windows.UI.Xaml.Visibility.Collapsed;
                this.SeekBar.Visibility = Windows.UI.Xaml.Visibility.Visible;
            }
            else
            {
                this.ProgressBar.Visibility = Windows.UI.Xaml.Visibility.Visible;
                this.SeekBar.Visibility = Windows.UI.Xaml.Visibility.Collapsed;
            }
        }

        public static readonly DependencyProperty PlaybackStatusProperty = DependencyProperty.Register(
            "PlaybackStatus",
            typeof(string),
            typeof(MoviePlaybackScreen),
            new PropertyMetadata("", new PropertyChangedCallback(MoviePlaybackScreen.OnPlaybackStatusUpdated)));

        private static void OnPlaybackStatusUpdated(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            (d as MoviePlaybackScreen).UpdatePlaybackStatus((string)(e.NewValue));
        }

        public string PlaybackStatus
        {
            get { return (string)GetValue(PlaybackStatusProperty); }
            set { SetValue(PlaybackStatusProperty, value); }
        }

        void UpdatePlaybackStatus(string status)
        {
            PlaybackStatusIcon.Visibility = Windows.UI.Xaml.Visibility.Visible;
            switch (status)
            {
                case StreamStatus.Paused:
                    PlaybackStatusIcon.Source = new BitmapImage(new Uri("ms-appx:///Assets/PlaybackScreen/playback_paused.png", UriKind.Absolute));
                    break;
                case StreamStatus.Started:
                    PlaybackStatusIcon.Source = new BitmapImage(new Uri("ms-appx:///Assets/PlaybackScreen/playback_playing.png", UriKind.Absolute));
                    break;
                case StreamStatus.PausedByEdge:
                    PlaybackStatusIcon.Source = new BitmapImage(new Uri("ms-appx:///Assets/PlaybackScreen/playback_stopped.png", UriKind.Absolute));
                    break;
                default:
                    PlaybackStatusIcon.Visibility = Windows.UI.Xaml.Visibility.Collapsed;
                    break;
            }
        }

        public void Reset()
        {
            if (SeekAvailable)
            {
                this.SeekBar.Value = 0;
            }
            else
            {
                this.ProgressBar.Value = 0;
            }
            PositionText.Text = "--";
            DurationText.Text = "--:--";
            PlaybackInfo.Visibility = Windows.UI.Xaml.Visibility.Collapsed;
        }

        private void Screen_Tapped(object sender, TappedRoutedEventArgs e)
        {
            if (OnPlaybackOperationRequested != null)
            {
                var r = PlaybackRequest.Start;
                switch (this.PlaybackStatus)
                {
                    case StreamStatus.Paused:
                        r = PlaybackRequest.Start;
                        break;
                    case StreamStatus.Started:
                        r = PlaybackRequest.Pause;
                        break;
                    case StreamStatus.PausedByEdge:
                        r = PlaybackRequest.Start;
                        break;
                }
                OnPlaybackOperationRequested(this, new PlaybackRequestArgs() { Request = r });
            }
        }
    }

    public class SeekBarOperationArgs
    {
        public TimeSpan SeekPosition { get; set; }
    }

    public class PlaybackRequestArgs
    {
        public PlaybackRequest Request { get; set; }
    }

    public enum PlaybackRequest
    {
        Start,
        Pause,
    }
}
