using Kazyx.RemoteApi;
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
    class SettingPanels
    {
        private readonly ControlPanelDataSource DataSource;

        private Dictionary<string, StackPanel> Panels = new Dictionary<string, StackPanel>();

        public SettingPanels(ControlPanelDataSource source)
        {
            this.DataSource = source;
            Panels.Add("setShootMode", GetComboBoxPanel("ShootMode", "ShootMode", OnShootModeChanged));
            Panels.Add("setExposureMode", GetComboBoxPanel("ExposureMode", "ExposureMode", OnExposureModeChanged));
            Panels.Add("setWhiteBalance", GetComboBoxPanel("WhiteBalance", "WhiteBalance", OnWhiteBalanceChanged));
            Panels.Add("ColorTemperture", GetColorTemperturePanel());
            Panels.Add("setSelfTimer", GetComboBoxPanel("SelfTimer", "SelfTimer", OnSelfTimerChanged));
            Panels.Add("setStillSize", GetComboBoxPanel("StillImageSize", "StillImageSize", OnStillImageSizeChanged));
            Panels.Add("setPostviewImageSize", GetComboBoxPanel("PostviewSize", "Setting_PostViewImageSize", OnPostviewSizeChanged));
            Panels.Add("setBeepMode", GetComboBoxPanel("BeepMode", "BeepMode", OnBeepModeChanged));

            VisibilityBinding = new Binding()
            {
                Source = source,
                Path = new PropertyPath("IsRestrictedApiAvailable"),
                Mode = BindingMode.OneWay,
                Converter = new BoolToVisibilityConverter(),
                FallbackValue = Visibility.Collapsed
            };
        }

        private readonly Binding VisibilityBinding;

        public List<StackPanel> SwitchDevice(ServerDevice device)
        {
            var list = new List<StackPanel>();

            DataSource.Device = device;
            foreach (var key in Panels.Keys)
            {
                if (DataSource.Device.Api.Capability.IsSupported(key) ||
                    (key == "ColorTemperture" && DataSource.Device.Api.Capability.IsSupported("setWhiteBalance")))
                {
                    list.Add(Panels[key]);
                }
                if (DataSource.Device.Api.Capability.IsRestrictedApi(key))
                {
                    Panels[key].SetBinding(StackPanel.VisibilityProperty, VisibilityBinding);
                }
            }

            return list;
        }

        private async void OnShootModeChanged(object sender, SelectionChangedEventArgs e)
        {
            await OnComboBoxChanged<string>(sender, DataSource.Device.Status.ShootMode,
                async (selected) => { await DataSource.Device.Api.Camera.SetShootModeAsync(selected); });
        }

        private async void OnExposureModeChanged(object sender, SelectionChangedEventArgs e)
        {
            await OnComboBoxChanged<string>(sender, DataSource.Device.Status.ExposureMode,
                async (selected) => { await DataSource.Device.Api.Camera.SetExposureModeAsync(selected); });
        }

        private async void OnSelfTimerChanged(object sender, SelectionChangedEventArgs e)
        {
            await OnComboBoxChanged<int>(sender, DataSource.Device.Status.SelfTimer,
                async (selected) => { await DataSource.Device.Api.Camera.SetSelfTimerAsync(selected); });
        }

        private async void OnPostviewSizeChanged(object sender, SelectionChangedEventArgs e)
        {
            await OnComboBoxChanged<string>(sender, DataSource.Device.Status.PostviewSize,
                async (selected) => { await DataSource.Device.Api.Camera.SetPostviewImageSizeAsync(selected); });
        }

        private async void OnBeepModeChanged(object sender, SelectionChangedEventArgs e)
        {
            await OnComboBoxChanged<string>(sender, DataSource.Device.Status.BeepMode,
                async (selected) => { await DataSource.Device.Api.Camera.SetBeepModeAsync(selected); });
        }

        private async void OnStillImageSizeChanged(object sender, SelectionChangedEventArgs e)
        {
            await OnComboBoxChanged<StillImageSize>(sender, DataSource.Device.Status.StillImageSize,
                async (selected) => { await DataSource.Device.Api.Camera.SetStillImageSizeAsync(selected); });
        }

        private async void OnWhiteBalanceChanged(object sender, SelectionChangedEventArgs e)
        {
            await OnComboBoxChanged<string>(sender, DataSource.Device.Status.WhiteBalance,
                async (selected) =>
                {
                    if (selected != WhiteBalanceMode.Manual)
                    {
                        DataSource.Device.Status.ColorTemperture = -1;
                        await DataSource.Device.Api.Camera.SetWhiteBalanceAsync(new WhiteBalance { Mode = selected, ColorTemperature = -1 }, false);
                    }
                    else
                    {
                        var min = DataSource.Device.Status.ColorTempertureCandidates[WhiteBalanceMode.Manual][0];
                        await DataSource.Device.Api.Camera.SetWhiteBalanceAsync(new WhiteBalance { Mode = selected, ColorTemperature = min }, true);
                        DataSource.Device.Status.ColorTemperture = min;
                        if (ColorTempertureSlider != null)
                        {
                            var val = DataSource.Device.Status.ColorTempertureCandidates[selected];
                            ColorTempertureSlider.Maximum = val[val.Length - 1];
                            ColorTempertureSlider.Minimum = val[0];
                            ColorTempertureSlider.Value = DataSource.Device.Status.ColorTemperture;
                        }
                    }
                });
        }

        private delegate Task AsyncAction<T>(T arg);

        private async Task OnComboBoxChanged<T>(object sender, Capability<T> param, AsyncAction<T> action)
        {
            if (param == null || param.candidates == null || param.candidates.Length == 0)
            {
                return;
            }

            var selected = (sender as ComboBox).SelectedIndex;

            if (selected < 0 || param.candidates.Length <= selected)
            {
                Debug.WriteLine("ignore out of range");
                return;
            }

            try
            {
                await action.Invoke(param.candidates[selected]);
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

        private StackPanel GetComboBoxPanel(string key, string title_key, SelectionChangedEventHandler handler)
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

            var parent = GetBasicPanel(SystemUtil.GetStringResource(title_key));
            parent.Children.Add(box);
            return parent;
        }

        private Slider ColorTempertureSlider = null;

        private StackPanel GetColorTemperturePanel()
        {
            var slider = CreateSlider(null, null);
            slider.Value = 0;

            slider.ManipulationCompleted += async (sender, e) =>
            {
                var target = ParameterUtil.AsValidColorTemperture((int)slider.Value, DataSource.Device.Status);
                slider.Value = target;
                try
                {
                    await DataSource.Device.Api.Camera.SetWhiteBalanceAsync(new WhiteBalance { Mode = DataSource.Device.Status.WhiteBalance.current, ColorTemperature = target }, true);
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
                Source = DataSource.Device.Status,
                Path = new PropertyPath("ColorTemperture"),
                Mode = BindingMode.OneWay,
            });

            slider.SetBinding(Slider.ValueProperty, new Binding()
            {
                Source = DataSource.Device.Status,
                Path = new PropertyPath("ColorTemperture"),
                Mode = BindingMode.TwoWay
            });

            ColorTempertureSlider = slider;

            var parent = GetBasicPanel(SystemUtil.GetStringResource("WB_ColorTemperture"));
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

        private static Slider CreateSlider(int? min, int? max)
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

        private static StackPanel GetBasicPanel(string title)
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
}
