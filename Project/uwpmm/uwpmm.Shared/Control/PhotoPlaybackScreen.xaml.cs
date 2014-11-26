using System;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
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

        public static readonly DependencyProperty MinProperty = DependencyProperty.Register(
            "SourceBitmap",
            typeof(BitmapImage),
            typeof(PhotoPlaybackScreen),
            new PropertyMetadata(null, new PropertyChangedCallback(PhotoPlaybackScreen.OnSourceBitmapUpdated)));

        private static void OnSourceBitmapUpdated(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            (d as PhotoPlaybackScreen).SourceBitmap = (BitmapImage)e.NewValue;
        }

        const double MaxScale = 1.0;

        double _scale = 1.0;
        double _coercedScale;
        double _originalScale;

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

        public void viewport_ManipulationStarted(object sender, ManipulationStartedRoutedEventArgs e)
        {
            _originalScale = _scale;
        }

        public void Init()
        {
            _scale = 0;
            CoerceScale(true);
            _scale = _coercedScale;
        }

        void CoerceScale(bool recompute)
        {
            //if (recompute && _SourceBitmap != null && viewport != null)
            //{
            //    // Calculate the minimum scale to fit the viewport 
            //    var minX = viewport.ActualWidth / _SourceBitmap.PixelWidth;
            //    var minY = viewport.ActualHeight / _SourceBitmap.PixelHeight;

            //    _minScale = Math.Min(minX, minY);
            //    DebugUtil.Log("Minimum scale: " + _minScale);
            //}

            //_coercedScale = Math.Min(MaxScale, Math.Max(_scale, _minScale));
        }

        internal void ReleaseImage()
        {
            _SourceBitmap = null;
        }
        private void Image_ManipulationDelta(object sender, ManipulationDeltaRoutedEventArgs e)
        {
            UIElement element = sender as UIElement;
            CompositeTransform transform = element.RenderTransform as CompositeTransform;
            if (transform != null)
            {
                transform.ScaleX *= e.Delta.Scale;
                transform.ScaleY *= e.Delta.Scale;
                transform.Rotation += e.Delta.Scale / Math.PI;
                transform.TranslateX += e.Delta.Translation.X;
                transform.TranslateY += e.Delta.Translation.Y;
            }
        }
    }
}
