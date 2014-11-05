using Kazyx.RemoteApi;
using Kazyx.RemoteApi.Camera;
using Kazyx.Uwpmm.CameraControl;
using Kazyx.Uwpmm.DataModel;
using Kazyx.Uwpmm.Utility;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading.Tasks;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Media;

namespace Kazyx.Uwpmm.Settings
{
    class SettingPanelBuilder
    {
        private readonly ControlPanelDataSource DataSource;

        private readonly Binding VisibilityBinding;

        private DeviceApiHolder Api { get { return DataSource.Device.Api; } }

        private CameraStatus Status { get { return DataSource.Device.Status; } }

        private Dictionary<string, StackPanel> Panels = new Dictionary<string, StackPanel>();

        public static SettingPanelBuilder CreateNew(TargetDevice device)
        {
            return new SettingPanelBuilder(device);
        }

        private SettingPanelBuilder(TargetDevice device)
        {
            DataSource = new ControlPanelDataSource(device);

            Panels.Add("setShootMode", BuildComboBoxPanel("ShootMode", "ShootMode", OnShootModeChanged));
            Panels.Add("setExposureMode", BuildComboBoxPanel("ExposureMode", "ExposureMode", OnExposureModeChanged));
            Panels.Add("setFocusMode", BuildComboBoxPanel("FocusMode", "FocusMode", OnFocusModeChanged));
            Panels.Add("setWhiteBalance", BuildComboBoxPanel("WhiteBalance", "WhiteBalance", OnWhiteBalanceChanged));
            Panels.Add("ColorTemperture", BuildColorTemperturePanel());
            Panels.Add("setMovieQuality", BuildComboBoxPanel("MovieQuality", "MovieQuality", OnMovieQualityChanged));
            Panels.Add("setSteadyMode", BuildComboBoxPanel("SteadyMode", "SteadyMode", OnSteadyModeChanged));
            Panels.Add("setSelfTimer", BuildComboBoxPanel("SelfTimer", "SelfTimer", OnSelfTimerChanged));
            Panels.Add("setStillSize", BuildComboBoxPanel("StillImageSize", "StillImageSize", OnStillImageSizeChanged));
            Panels.Add("setPostviewImageSize", BuildComboBoxPanel("PostviewSize", "Setting_PostViewImageSize", OnPostviewSizeChanged));
            Panels.Add("setViewAngle", BuildComboBoxPanel("ViewAngle", "ViewAngle", OnViewAngleChanged));
            Panels.Add("setBeepMode", BuildComboBoxPanel("BeepMode", "BeepMode", OnBeepModeChanged));
            Panels.Add("setFlashMode", BuildComboBoxPanel("FlashMode", "FlashMode", OnFlashModeChanged));

            VisibilityBinding = new Binding()
            {
                Source = DataSource,
                Path = new PropertyPath("IsRestrictedApiAvailable"),
                Mode = BindingMode.OneWay,
                Converter = new BoolToVisibilityConverter(),
                FallbackValue = Visibility.Collapsed
            };
        }

        public List<StackPanel> GetPanelsToShow()
        {
            var list = new List<StackPanel>();

            foreach (var key in Panels.Keys)
            {
                if (Api.Capability.IsSupported(key) ||
                    (key == "ColorTemperture" && Api.Capability.IsSupported("setWhiteBalance")))
                {
                    list.Add(Panels[key]);
                }
                if (Api.Capability.IsRestrictedApi(key))
                {
                    Panels[key].SetBinding(StackPanel.VisibilityProperty, VisibilityBinding);
                }
            }

            return list;
        }

        private async void OnFocusModeChanged(object sender, SelectionChangedEventArgs e)
        {
            await OnSelectionChanged(sender, Status.FocusMode, Api.Camera.SetFocusModeAsync);
        }

        private async void OnMovieQualityChanged(object sender, SelectionChangedEventArgs e)
        {
            await OnSelectionChanged(sender, Status.MovieQuality, Api.Camera.SetMovieQualityAsync);
        }

        private async void OnSteadyModeChanged(object sender, SelectionChangedEventArgs e)
        {
            await OnSelectionChanged(sender, Status.SteadyMode, Api.Camera.SetSteadyModeAsync);
        }

        private async void OnViewAngleChanged(object sender, SelectionChangedEventArgs e)
        {
            await OnSelectionChanged(sender, Status.ViewAngle, Api.Camera.SetViewAngleAsync);
        }

        private async void OnFlashModeChanged(object sender, SelectionChangedEventArgs e)
        {
            await OnSelectionChanged(sender, Status.FlashMode, Api.Camera.SetFlashModeAsync);
        }

        private async void OnShootModeChanged(object sender, SelectionChangedEventArgs e)
        {
            await OnSelectionChanged(sender, Status.ShootMode, Api.Camera.SetShootModeAsync);
        }

        private async void OnExposureModeChanged(object sender, SelectionChangedEventArgs e)
        {
            await OnSelectionChanged(sender, Status.ExposureMode, Api.Camera.SetExposureModeAsync);
        }

        private async void OnSelfTimerChanged(object sender, SelectionChangedEventArgs e)
        {
            await OnSelectionChanged(sender, Status.SelfTimer, Api.Camera.SetSelfTimerAsync);
        }

        private async void OnPostviewSizeChanged(object sender, SelectionChangedEventArgs e)
        {
            await OnSelectionChanged(sender, Status.PostviewSize, Api.Camera.SetPostviewImageSizeAsync);
        }

        private async void OnBeepModeChanged(object sender, SelectionChangedEventArgs e)
        {
            await OnSelectionChanged(sender, Status.BeepMode, Api.Camera.SetBeepModeAsync);
        }

        private async void OnStillImageSizeChanged(object sender, SelectionChangedEventArgs e)
        {
            await OnSelectionChanged(sender, Status.StillImageSize, Api.Camera.SetStillImageSizeAsync);
        }

        private async void OnWhiteBalanceChanged(object sender, SelectionChangedEventArgs e)
        {
            await OnComboBoxChanged(sender, Status.WhiteBalance,
                async (selected) =>
                {
                    if (selected != WhiteBalanceMode.Manual)
                    {
                        Status.ColorTemperture = -1;
                        await Api.Camera.SetWhiteBalanceAsync(new WhiteBalance { Mode = selected });
                    }
                    else
                    {
                        var min = Status.ColorTempertureCandidates[WhiteBalanceMode.Manual][0];
                        await Api.Camera.SetWhiteBalanceAsync(new WhiteBalance { Mode = selected, ColorTemperature = min });
                        Status.ColorTemperture = min;
                        if (ColorTempertureSlider != null)
                        {
                            var val = Status.ColorTempertureCandidates[selected];
                            ColorTempertureSlider.Maximum = val[val.Length - 1];
                            ColorTempertureSlider.Minimum = val[0];
                            ColorTempertureSlider.Value = Status.ColorTemperture;
                        }
                    }
                });
        }

        private async Task OnSelectionChanged<T>(object sender, Capability<T> param, AsyncAction<T> action)
        {
            await OnComboBoxChanged(sender, param, async (selected) => { await action.Invoke(selected); });
        }

        private async Task OnComboBoxChanged<T>(object sender, Capability<T> param, AsyncAction<T> action)
        {
            if (param == null || param.Candidates == null || param.Candidates.Count == 0)
            {
                return;
            }

            var selected = (sender as ComboBox).SelectedIndex;

            if (selected < 0 || param.Candidates.Count <= selected)
            {
                Debug.WriteLine("ignore out of range");
                return;
            }

            try
            {
                await action.Invoke(param.Candidates[selected]);
                return;
            }
            catch (RemoteApiException e)
            {
                Debug.WriteLine("Failed to set parameter: " + e.code);
            }
            catch (NullReferenceException e)
            {
                Debug.WriteLine("Failed to set parameter: " + e.Message);
            }
            await DataSource.Device.Observer.Refresh();
        }

        private StackPanel BuildComboBoxPanel(string key, string title_key, SelectionChangedEventHandler handler)
        {
            var box = new ComboBox
            {
                Margin = new Thickness(4, 0, 0, 0)
            };
            box.SetBinding(ComboBox.IsEnabledProperty, new Binding
            {
                Source = DataSource,
                Path = new PropertyPath("IsAvailable" + key),
                Mode = BindingMode.OneWay
            });
            box.SetBinding(ComboBox.ItemsSourceProperty, new Binding
            {
                Source = DataSource,
                Path = new PropertyPath("Candidates" + key),
                Mode = BindingMode.OneWay
            });
            box.SetBinding(ComboBox.SelectedIndexProperty, new Binding
            {
                Source = DataSource,
                Path = new PropertyPath("SelectedIndex" + key),
                Mode = BindingMode.TwoWay
            });
            box.SelectionChanged += handler;

            var parent = BuildBasicPanel(SystemUtil.GetStringResource(title_key));
            parent.Children.Add(box);
            return parent;
        }

        private Slider ColorTempertureSlider = null;

        private StackPanel BuildColorTemperturePanel()
        {
            var slider = BuildSlider(null, null);
            slider.Value = 0;

            slider.ManipulationCompleted += async (sender, e) =>
            {
                var target = ParameterUtil.AsValidColorTemperture((int)slider.Value, Status);
                slider.Value = target;
                try
                {
                    await Api.Camera.SetWhiteBalanceAsync(new WhiteBalance { Mode = Status.WhiteBalance.Current, ColorTemperature = target });
                }
                catch (RemoteApiException ex)
                {
                    Debug.WriteLine("Failed to set color temperture: " + ex.code);
                }
                catch (NullReferenceException ex)
                {
                    Debug.WriteLine("Failed to set color temperture: " + ex.Message);
                }
            };

            var indicator = new TextBlock
            {
                HorizontalAlignment = HorizontalAlignment.Left,
                Style = Application.Current.Resources["BaseTextBlockStyle"] as Style,
                Margin = new Thickness(10, 22, 0, 0),
            };

            indicator.SetBinding(TextBlock.TextProperty, new Binding()
            {
                Source = Status,
                Path = new PropertyPath("ColorTemperture"),
                Mode = BindingMode.OneWay,
            });

            slider.SetBinding(Slider.ValueProperty, new Binding()
            {
                Source = Status,
                Path = new PropertyPath("ColorTemperture"),
                Mode = BindingMode.TwoWay
            });

            ColorTempertureSlider = slider;

            var parent = BuildBasicPanel(SystemUtil.GetStringResource("WB_ColorTemperture"));
            (parent.Children[0] as StackPanel).Children.Add(indicator);
            parent.Children.Add(slider);
            parent.SetBinding(StackPanel.VisibilityProperty, new Binding()
            {
                Source = DataSource,
                Path = new PropertyPath("IsAvailableColorTemperture"),
                Mode = BindingMode.OneWay,
                Converter = new BoolToVisibilityConverter()
            });

            return parent;
        }

        private static Slider BuildSlider(int? min, int? max)
        {
            return new Slider
            {
                Maximum = max != null ? max.Value : 1,
                Minimum = min != null ? min.Value : 0,
                Margin = new Thickness(4, 0, 0, 0),
                Width = double.NaN,
                HorizontalAlignment = HorizontalAlignment.Stretch,
                VerticalAlignment = VerticalAlignment.Top,
                Background = Application.Current.Resources["ProgressBarBackgroundThemeBrush"] as Brush
            };
        }

        private static StackPanel BuildBasicPanel(string title)
        {
            var panel = new StackPanel
            {
                HorizontalAlignment = HorizontalAlignment.Stretch,
                MaxWidth = 480
            };

            var titlePanel = new StackPanel
            {
                HorizontalAlignment = HorizontalAlignment.Left,
                Orientation = Windows.UI.Xaml.Controls.Orientation.Horizontal,
                Margin = new Thickness(0, 8, 0, 8)
            };

            titlePanel.Children.Add(new TextBlock
            {
                Text = title,
                HorizontalAlignment = HorizontalAlignment.Left,
                Margin = new Thickness(0, 0, 0, 0),
                Style = Application.Current.Resources["SubheaderTextBlockStyle"] as Style,
            });

            panel.Children.Add(titlePanel);

            return panel;
        }
    }

    delegate Task AsyncAction<T>(T arg);
}
