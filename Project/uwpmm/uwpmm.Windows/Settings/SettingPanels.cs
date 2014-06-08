using Kazyx.RemoteApi;
using Kazyx.Uwpmm.CameraControl;
using Kazyx.Uwpmm.DataModel;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading.Tasks;
using Windows.ApplicationModel.Resources;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Data;

namespace Kazyx.Uwpmm.Settings
{
    class SettingPanels
    {
        private readonly ControlPanelDataSource source;

        private Dictionary<string, StackPanel> Panels = new Dictionary<string, StackPanel>();

        private ServerDevice Camera;

        public SettingPanels(ControlPanelDataSource source)
        {
            this.source = source;
            Panels.Add("setShootMode", GetComboBoxPanel("ShootMode", ResourceLoader.GetForCurrentView().GetString("ShootMode"), OnShootModeChanged));
            Panels.Add("setExposureMode", GetComboBoxPanel("ExposureMode", ResourceLoader.GetForCurrentView().GetString("ExposureMode"), OnExposureModeChanged));
            Panels.Add("setSelfTimer", GetComboBoxPanel("SelfTimer", ResourceLoader.GetForCurrentView().GetString("SelfTimer"), OnSelfTimerChanged));
            Panels.Add("setStillSize", GetComboBoxPanel("StillImageSize", ResourceLoader.GetForCurrentView().GetString("StillImageSize"), OnStillImageSizeChanged));
            Panels.Add("setPostviewImageSize", GetComboBoxPanel("PostviewSize", ResourceLoader.GetForCurrentView().GetString("Setting_PostViewImageSize"), OnPostviewSizeChanged));
            Panels.Add("setBeepMode", GetComboBoxPanel("BeepMode", ResourceLoader.GetForCurrentView().GetString("BeepMode"), OnBeepModeChanged));
        }

        public List<StackPanel> SwitchDevice(ServerDevice camera)
        {
            this.Camera = camera;

            var list = new List<StackPanel>();

            Binding VisibilityBinding = new Binding()
            {
                Source = source,
                Path = new PropertyPath("IsRestrictedApiVisible"),
                Mode = BindingMode.OneWay,
                FallbackValue = Visibility.Collapsed
            };

            foreach (var key in Panels.Keys)
            {
                if (Camera.Api.Capability.IsSupported(key))
                {
                    list.Add(Panels[key]);
                }
                if (Camera.Api.Capability.IsRestrictedApi(key))
                {
                    Panels[key].SetBinding(StackPanel.VisibilityProperty, VisibilityBinding);
                }
            }

            return list;
        }

        private async void OnShootModeChanged(object sender, SelectionChangedEventArgs e)
        {
            await OnComboBoxChanged<string>(sender, Camera.Status.ShootMode,
                async (selected) => { await Camera.Api.Camera.SetShootModeAsync(selected); });
        }

        private async void OnExposureModeChanged(object sender, SelectionChangedEventArgs e)
        {
            await OnComboBoxChanged<string>(sender, Camera.Status.ExposureMode,
                async (selected) => { await Camera.Api.Camera.SetExposureModeAsync(selected); });
        }

        private async void OnSelfTimerChanged(object sender, SelectionChangedEventArgs e)
        {
            await OnComboBoxChanged<int>(sender, Camera.Status.SelfTimer,
                async (selected) => { await Camera.Api.Camera.SetSelfTimerAsync(selected); });
        }

        private async void OnPostviewSizeChanged(object sender, SelectionChangedEventArgs e)
        {
            await OnComboBoxChanged<string>(sender, Camera.Status.PostviewSize,
                async (selected) => { await Camera.Api.Camera.SetPostviewImageSizeAsync(selected); });
        }

        private async void OnBeepModeChanged(object sender, SelectionChangedEventArgs e)
        {
            await OnComboBoxChanged<string>(sender, Camera.Status.BeepMode,
                async (selected) => { await Camera.Api.Camera.SetBeepModeAsync(selected); });
        }

        private async void OnStillImageSizeChanged(object sender, SelectionChangedEventArgs e)
        {
            await OnComboBoxChanged<StillImageSize>(sender, Camera.Status.StillImageSize,
                async (selected) => { await Camera.Api.Camera.SetStillImageSizeAsync(selected); });
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
            await Camera.Observer.Refresh();
        }

        private StackPanel GetComboBoxPanel(string key, string title, SelectionChangedEventHandler handler)
        {

            var box = new ComboBox
            {
                Margin = new Thickness(4, 0, 0, 0)
            };
            box.SetBinding(ComboBox.IsEnabledProperty, new Binding
            {
                Source = source,
                Path = new PropertyPath("IsAvailable" + key),
                Mode = BindingMode.OneWay
            });
            box.SetBinding(ComboBox.ItemsSourceProperty, new Binding
            {
                Source = source,
                Path = new PropertyPath("Candidates" + key),
                Mode = BindingMode.OneWay
            });
            box.SetBinding(ComboBox.SelectedIndexProperty, new Binding
            {
                Source = source,
                Path = new PropertyPath("SelectedIndex" + key),
                Mode = BindingMode.TwoWay
            });
            box.SelectionChanged += handler;

            var parent = GetBasicPanel(title);
            parent.Children.Add(box);
            return parent;
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
