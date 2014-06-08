using Kazyx.RemoteApi;
using Kazyx.Uwpmm.CameraControl;
using Kazyx.Uwpmm.Utility;
using System.Diagnostics;
using Windows.UI.Xaml;

namespace Kazyx.Uwpmm.DataModel
{
    class ControlPanelDataSource : ObservableBase
    {
        private readonly ServerDevice Camera;

        public ControlPanelDataSource(ServerDevice camera)
        {
            this.Camera = camera;
            Camera.Status.PropertyChanged += (sender, e) =>
            {
                GenericPropertyChanged(e.PropertyName);
            };
            Camera.Api.AvailiableApisUpdated += (sender, e) =>
            {
                NotifyChangedOnUI("IsAvailableExposureMode");
                NotifyChangedOnUI("IsAvailableShootMode");
                NotifyChangedOnUI("IsAvailableBeepMode");
                NotifyChangedOnUI("IsAvailableSelfTimer");
                NotifyChangedOnUI("IsAvailablePostviewSize");
                NotifyChangedOnUI("IsAvailableStillImageSize");
            };
            Camera.Api.ServerVersionDetected += (sender, e) =>
            {
                NotifyChangedOnUI("IsRestrictedApiVisible");
            };
        }

        private void GenericPropertyChanged(string name)
        {
            NotifyChanged("Candidates" + name);
            NotifyChanged("SelectedIndex" + name);
            NotifyChanged("IsAvailable" + name);
        }

        public Visibility IsRestrictedApiVisible
        {
            get { return Camera.Api.Capability.Version.IsLiberated ? Visibility.Visible : Visibility.Collapsed; }
        }

        public bool IsAvailableExposureMode
        {
            get
            {
                return Camera.Api.Capability.IsAvailable("setExposureMode") &&
                    Camera.Status.ExposureMode != null;
            }
        }

        public string[] CandidatesExposureMode
        {
            get
            {
                return SettingValueConverter.FromExposureMode(Camera.Status.ExposureMode).candidates;
            }
        }

        public int SelectedIndexExposureMode
        {
            get
            {
                return SettingValueConverter.GetSelectedIndex(Camera.Status.ExposureMode);
            }
            set
            {
                SetSelectedAsCurrent(Camera.Status.ExposureMode, value);
            }
        }

        public bool IsAvailableShootMode
        {
            get
            {
                return Camera.Api.Capability.IsAvailable("setShootMode") &&
                    Camera.Status.ShootMode != null;
            }
        }

        public string[] CandidatesShootMode
        {
            get
            {
                return SettingValueConverter.FromShootMode(Camera.Status.ShootMode).candidates;
            }
        }

        public int SelectedIndexShootMode
        {
            get
            {
                return SettingValueConverter.GetSelectedIndex(Camera.Status.ShootMode);
            }
            set
            {
                SetSelectedAsCurrent(Camera.Status.ShootMode, value);
            }
        }

        public bool IsAvailableBeepMode
        {
            get
            {
                return Camera.Api.Capability.IsAvailable("setBeepMode") &&
                    Camera.Status.BeepMode != null;
            }
        }

        public string[] CandidatesBeepMode
        {
            get
            {
                return SettingValueConverter.FromBeepMode(Camera.Status.BeepMode).candidates;
            }
        }

        public int SelectedIndexBeepMode
        {
            get
            {
                return SettingValueConverter.GetSelectedIndex(Camera.Status.BeepMode);
            }
            set
            {
                SetSelectedAsCurrent(Camera.Status.BeepMode, value);
            }
        }

        public bool IsAvailablePostviewSize
        {
            get
            {
                return Camera.Api.Capability.IsAvailable("setPostviewImageSize") &&
                    Camera.Status.PostviewSize != null;
            }
        }

        public string[] CandidatesPostviewSize
        {
            get
            {
                return SettingValueConverter.FromPostViewSize(Camera.Status.PostviewSize).candidates;
            }
        }

        public int SelectedIndexPostviewSize
        {
            get
            {
                return SettingValueConverter.GetSelectedIndex(Camera.Status.PostviewSize);
            }
            set
            {
                SetSelectedAsCurrent(Camera.Status.PostviewSize, value);
            }
        }

        public bool IsAvailableSelfTimer
        {
            get
            {
                return Camera.Api.Capability.IsAvailable("setSelfTimer") &&
                    Camera.Status.SelfTimer != null;
            }
        }

        public string[] CandidatesSelfTimer
        {
            get
            {
                return SettingValueConverter.FromSelfTimer(Camera.Status.SelfTimer).candidates;
            }
        }

        public int SelectedIndexSelfTimer
        {
            get
            {
                return SettingValueConverter.GetSelectedIndex(Camera.Status.SelfTimer);
            }
            set
            {
                SetSelectedAsCurrent(Camera.Status.SelfTimer, value);
            }
        }

        public bool IsAvailableStillImageSize
        {
            get
            {
                return Camera.Api.Capability.IsAvailable("setStillSize") &&
                    Camera.Status.StillImageSize != null;
            }
        }

        public string[] CandidatesStillImageSize
        {
            get
            {
                return SettingValueConverter.FromStillImageSize(Camera.Status.StillImageSize).candidates;
            }
        }

        public int SelectedIndexStillImageSize
        {
            get
            {
                return SettingValueConverter.GetSelectedIndex(Camera.Status.StillImageSize);
            }
            set
            {
                SetSelectedAsCurrent(Camera.Status.StillImageSize, value);
            }
        }

        private static void SetSelectedAsCurrent<T>(Capability<T> capability, int index)
        {
            if (index == -1)
            {
                return;
            }

            if (capability != null)
            {
                if (capability.candidates.Length > index)
                {
                    capability.current = capability.candidates[index];
                }
                else
                {
                    capability.current = default(T);
                }
            }
        }
    }
}
