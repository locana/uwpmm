using Kazyx.Uwpmm.Utility;
using System;
using System.Text;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;

// The User Control item template is documented at http://go.microsoft.com/fwlink/?LinkId=234236

namespace Kazyx.Uwpmm.Control
{
    public sealed partial class MoviePlaybackScreen : UserControl
    {
        public MoviePlaybackScreen()
        {
            this.InitializeComponent();
        }


        public static readonly DependencyProperty CurrentPositionProperty = DependencyProperty.Register(
            "Type",
            typeof(TimeSpan),
            typeof(MoviePlaybackScreen),
            new PropertyMetadata(new TimeSpan(0, 0, 0), new PropertyChangedCallback(MoviePlaybackScreen.OnCurrentPositionChanged)));

        private static void OnCurrentPositionChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            (d as MoviePlaybackScreen).CurrentPosition = (TimeSpan)e.NewValue;
        }

        void UpdatePlaybackPosition(TimeSpan current, TimeSpan duration)
        {
            if (duration.TotalMilliseconds <= 0)
            {
                PlaybackInfo.Visibility = Windows.UI.Xaml.Visibility.Collapsed;
                return;
            }
            double value = current.TotalMilliseconds / duration.TotalMilliseconds * 100;
            if (value < 0 || value > 100) { return; }
            if (this.SeekAvailable)
            {
                this.SeekBar.Value = value;
            }
            else
            {
                this.ProgressBar.Value = value;
            }
            PositionText.Text = ToString(current);
            PlaybackInfo.Visibility = Windows.UI.Xaml.Visibility.Visible;
        }

        private TimeSpan _CurrentPosition;
        public TimeSpan CurrentPosition
        {
            get { return _CurrentPosition; }
            set
            {
                if (_CurrentPosition != value)
                {
                    this._CurrentPosition = value;
                    UpdatePlaybackPosition(value, this.Duration);
                }
            }
        }

        private TimeSpan _Duration;
        public TimeSpan Duration
        {
            get { return _Duration; }
            set
            {
                if (value.TotalMilliseconds <= 0)
                {
                    this.DurationText.Text = "--:--:--";
                }
                else
                {
                    _Duration = value;
                    this.DurationText.Text = ToString(value);
                }
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

        public static readonly DependencyProperty DurationProperty = DependencyProperty.Register(
            "Type",
            typeof(TimeSpan),
            typeof(MoviePlaybackScreen),
            new PropertyMetadata(new TimeSpan(0, 0, 0), new PropertyChangedCallback(MoviePlaybackScreen.OnDurationChanged)));

        private static void OnDurationChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            DebugUtil.Log("Duration updated: " + ((TimeSpan)e.NewValue).TotalSeconds);
            (d as MoviePlaybackScreen).Duration = (TimeSpan)e.NewValue;
        }

        private bool _SeekAvailable = false;
        public bool SeekAvailable
        {
            get { return _SeekAvailable; }
            set
            {
                if (_SeekAvailable != value)
                {
                    _SeekAvailable = value;
                    if (value)
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
            }
        }

        public Action<double> SeekOperated;

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

        private void SeekBar_ValueChanged(object sender, RangeBaseValueChangedEventArgs e)
        {
            if (SeekOperated != null)
            {
                SeekOperated(e.NewValue);
            }
        }
    }
}
