using Kazyx.RemoteApi.Camera;
using Kazyx.Uwpmm.Utility;
using System.Collections.Generic;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;

// The User Control item template is documented at http://go.microsoft.com/fwlink/?LinkId=234236

namespace Kazyx.Uwpmm.Control
{
    public sealed partial class BatteryStatus : UserControl
    {
        public BatteryStatus()
        {
            this.InitializeComponent();
        }
        const double MAX_AMOUNT_WIDTH = 410.0 / 600.0;
        const double AMOUNT_OFFSET = 65.0 / 600.0;
        const double BACKGROUND_OFFSET = 62.0 / 600.0;

        public List<BatteryInfo> BatteryInfo
        {
            set
            {
                SetValue(BatteryInfoProperty, value);
                UpdateBatteryLevelDisplay(value);
            }
            get { return (List<BatteryInfo>)GetValue(BatteryInfoProperty); }
        }

        public static readonly DependencyProperty BatteryInfoProperty = DependencyProperty.Register(
            "BatteryInfo",
            typeof(List<BatteryInfo>),
            typeof(BatteryStatus),
            new PropertyMetadata(null, new PropertyChangedCallback(BatteryStatus.BatteryInfoUpdated)));

        private static void BatteryInfoUpdated(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            (d as BatteryStatus).BatteryInfo = (List<BatteryInfo>)e.NewValue;
        }

        private void UpdateBatteryLevelDisplay(List<BatteryInfo> info)
        {
            if (info == null || FindFirstActiveBattery(info) == null)
            {
                LayoutRoot.Visibility = Windows.UI.Xaml.Visibility.Collapsed;
            }
            else
            {
                LayoutRoot.Visibility = Windows.UI.Xaml.Visibility.Visible;
            }

            var batt = FindFirstActiveBattery(info);

            double w = LayoutRoot.ActualWidth;
            double offset = w * AMOUNT_OFFSET;
            double bg_offset = w * BACKGROUND_OFFSET;

            Amount.Margin = new Thickness(offset);

            if (batt.LevelDenominator > 0)
            {
                double level = (double)batt.LevelNumerator / (double)batt.LevelDenominator;
                Amount.Width = level * MAX_AMOUNT_WIDTH * w;
                Background.Width = level * MAX_AMOUNT_WIDTH * w + (bg_offset - offset) * 2;
            }

            if (batt.Status == RemoteApi.Camera.BatteryStatus.NearEnd)
            {
                Amount.Fill = ResourceManager.AccentColorBrush;
            }
            else
            {
                Amount.Fill = ResourceManager.ForegroundBrush;
            }
        }

        BatteryInfo FindFirstActiveBattery(List<BatteryInfo> batteries)
        {
            foreach (var b in batteries)
            {
                switch (b.Status)
                {
                    case RemoteApi.Camera.BatteryStatus.Active:
                    case RemoteApi.Camera.BatteryStatus.NearEnd:
                        return b;
                }
            }
            return null;
        }

        private void LayoutRoot_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            UpdateBatteryLevelDisplay((List<BatteryInfo>)GetValue(BatteryInfoProperty));
        }
    }
}
