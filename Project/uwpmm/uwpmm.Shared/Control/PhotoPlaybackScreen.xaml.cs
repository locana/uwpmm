using Kazyx.Uwpmm.Utility;
using System;
using Windows.Foundation;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Media.Animation;
using Windows.UI.Xaml.Media.Imaging;

// The User Control item template is documented at http://go.microsoft.com/fwlink/?LinkId=234236

namespace Kazyx.Uwpmm.Control
{
    public sealed partial class PhotoPlaybackScreen : UserControl
    {
        public PhotoPlaybackScreen()
        {
            this.InitializeComponent();
        }

        public Visibility DetailInfoVisibility
        {
            get { return DetailInfoPanel.Visibility; }
            set { DetailInfoPanel.Visibility = value; }
        }

        public void RotateImage(Rotation r)
        {
            var transform = Image.RenderTransform as CompositeTransform;
            if (transform != null)
            {
                switch (r)
                {
                    case Rotation.Left:
                        RotateSmoothly(Image, -90);
                        break;
                    case Rotation.Right:
                        RotateSmoothly(Image, 90);
                        break;
                }
            }
        }

        public void RotateSmoothly(UIElement target, double angle)
        {
            var transform = Image.RenderTransform as CompositeTransform;

            var duration = new Duration(TimeSpan.FromMilliseconds(200));
            var sb = new Storyboard() { Duration = duration };
            var animation = new DoubleAnimationUsingKeyFrames();

            sb.Children.Add(animation);

            Storyboard.SetTarget(animation, transform);
            Storyboard.SetTargetProperty(animation, "Rotation");

            animation.KeyFrames.Add(new EasingDoubleKeyFrame() { KeyTime = KeyTime.FromTimeSpan(TimeSpan.FromMilliseconds(0)), Value = transform.Rotation });
            animation.KeyFrames.Add(new EasingDoubleKeyFrame() { KeyTime = KeyTime.FromTimeSpan(TimeSpan.FromMilliseconds(60)), Value = transform.Rotation + angle * 0.7 });
            animation.KeyFrames.Add(new EasingDoubleKeyFrame() { KeyTime = KeyTime.FromTimeSpan(TimeSpan.FromMilliseconds(180)), Value = transform.Rotation + angle });

            sb.Begin();
        }

        public static readonly DependencyProperty SourceBitmapProperty = DependencyProperty.Register(
            "SourceBitmap",
            typeof(BitmapImage),
            typeof(PhotoPlaybackScreen),
            new PropertyMetadata(null, new PropertyChangedCallback(PhotoPlaybackScreen.OnSourceBitmapUpdated)));

        private static void OnSourceBitmapUpdated(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            (d as PhotoPlaybackScreen).SourceBitmap = (BitmapImage)e.NewValue;
        }

        const double MaxScale = 5.0;
        const double MinScale = 0.9;

        BitmapImage _SourceBitmap;
        public BitmapImage SourceBitmap
        {
            get { return _SourceBitmap; }
            set
            {
                _SourceBitmap = value;
            }
        }

        public void SetBitmap()
        {
            Image.Source = _SourceBitmap;
        }

        public void Init()
        {
            var transform = Image.RenderTransform as CompositeTransform;
            if (transform != null)
            {
                transform.ScaleX = 1;
                transform.ScaleY = 1;
                transform.Rotation = 0;
                transform.TranslateX = 0;
                transform.TranslateY = 0;
            }
        }

        internal void ReleaseImage()
        {
            _SourceBitmap = null;
        }

        private void Image_ManipulationDelta(object sender, ManipulationDeltaRoutedEventArgs e)
        {
            UIElement element = sender as UIElement;
            var parent = (sender as Image).Parent as ScrollViewer;
            CompositeTransform transform = element.RenderTransform as CompositeTransform;
            if (transform != null && parent != null)
            {
                transform.ScaleX = LimitToRange(transform.ScaleX * e.Delta.Scale, MinScale, MaxScale);
                transform.ScaleY = LimitToRange(transform.ScaleY * e.Delta.Scale, MinScale, MaxScale);

                var diagonalSize = Math.Sqrt(Math.Pow(element.RenderSize.Width, 2) + Math.Pow(element.RenderSize.Height, 2)) * transform.ScaleX;
                var translateLimitX = diagonalSize / 3 + parent.ActualWidth / 3;
                var translateLimitY = diagonalSize / 4 + parent.ActualHeight / 3;
                transform.TranslateX = LimitToRange(transform.TranslateX + e.Delta.Translation.X, -translateLimitX, translateLimitX);
                transform.TranslateY = LimitToRange(transform.TranslateY + e.Delta.Translation.Y, -translateLimitY, translateLimitY);
            }
        }

        double LimitToRange(double value, double min, double max)
        {
            if (value > max) { return max; }
            if (value < min) { return min; }
            return value;
        }

        private void Image_DoubleTapped(object sender, DoubleTappedRoutedEventArgs e)
        {
            Init();
        }
    }

    public enum Rotation
    {
        Right,
        Left,
    }
}
