using Kazyx.RemoteApi;
using Kazyx.Uwpmm.CameraControl;
using Kazyx.Uwpmm.Utility;

namespace Kazyx.Uwpmm.DataModel
{
    class ControlPanelDataSource : ObservableBase
    {
        private ServerDevice _Device;
        public ServerDevice Device
        {
            set { _Device = value; }
            get { return _Device; }
        }

        public ControlPanelDataSource(ServerDevice camera)
        {
            this.Device = camera;
            Device.Status.PropertyChanged += (sender, e) =>
            {
                GenericPropertyChanged(e.PropertyName);
            };
            Device.Api.AvailiableApisUpdated += (sender, e) =>
            {
                NotifyChangedOnUI("IsAvailableExposureMode");
                NotifyChangedOnUI("IsAvailableShootMode");
                NotifyChangedOnUI("IsAvailableBeepMode");
                NotifyChangedOnUI("IsAvailableSelfTimer");
                NotifyChangedOnUI("IsAvailablePostviewSize");
                NotifyChangedOnUI("IsAvailableStillImageSize");
                NotifyChangedOnUI("IsAvailableWhiteBalance");
                NotifyChangedOnUI("IsAvailableColorTemperture");
            };
            Device.Api.ServerVersionDetected += (sender, e) =>
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

        public bool IsRestrictedApiAvailable
        {
            get { return Device.Api.Capability.Version.IsLiberated; }
        }

        public bool IsAvailableExposureMode
        {
            get
            {
                return Device.Api.Capability.IsAvailable("setExposureMode") &&
                    Device.Status.ExposureMode != null;
            }
        }

        public string[] CandidatesExposureMode
        {
            get
            {
                return SettingValueConverter.FromExposureMode(Device.Status.ExposureMode).candidates;
            }
        }

        public int SelectedIndexExposureMode
        {
            get
            {
                return SettingValueConverter.GetSelectedIndex(Device.Status.ExposureMode);
            }
            set
            {
                SetSelectedAsCurrent(Device.Status.ExposureMode, value);
            }
        }

        public bool IsAvailableShootMode
        {
            get
            {
                return Device.Api.Capability.IsAvailable("setShootMode") &&
                    Device.Status.ShootMode != null;
            }
        }

        public string[] CandidatesShootMode
        {
            get
            {
                return SettingValueConverter.FromShootMode(Device.Status.ShootMode).candidates;
            }
        }

        public int SelectedIndexShootMode
        {
            get
            {
                return SettingValueConverter.GetSelectedIndex(Device.Status.ShootMode);
            }
            set
            {
                SetSelectedAsCurrent(Device.Status.ShootMode, value);
            }
        }

        public bool IsAvailableBeepMode
        {
            get
            {
                return Device.Api.Capability.IsAvailable("setBeepMode") &&
                    Device.Status.BeepMode != null;
            }
        }

        public string[] CandidatesBeepMode
        {
            get
            {
                return SettingValueConverter.FromBeepMode(Device.Status.BeepMode).candidates;
            }
        }

        public int SelectedIndexBeepMode
        {
            get
            {
                return SettingValueConverter.GetSelectedIndex(Device.Status.BeepMode);
            }
            set
            {
                SetSelectedAsCurrent(Device.Status.BeepMode, value);
            }
        }

        public bool IsAvailablePostviewSize
        {
            get
            {
                return Device.Api.Capability.IsAvailable("setPostviewImageSize") &&
                    Device.Status.PostviewSize != null;
            }
        }

        public string[] CandidatesPostviewSize
        {
            get
            {
                return SettingValueConverter.FromPostViewSize(Device.Status.PostviewSize).candidates;
            }
        }

        public int SelectedIndexPostviewSize
        {
            get
            {
                return SettingValueConverter.GetSelectedIndex(Device.Status.PostviewSize);
            }
            set
            {
                SetSelectedAsCurrent(Device.Status.PostviewSize, value);
            }
        }

        public bool IsAvailableSelfTimer
        {
            get
            {
                return Device.Api.Capability.IsAvailable("setSelfTimer") &&
                    Device.Status.SelfTimer != null;
            }
        }

        public string[] CandidatesSelfTimer
        {
            get
            {
                return SettingValueConverter.FromSelfTimer(Device.Status.SelfTimer).candidates;
            }
        }

        public int SelectedIndexSelfTimer
        {
            get
            {
                return SettingValueConverter.GetSelectedIndex(Device.Status.SelfTimer);
            }
            set
            {
                SetSelectedAsCurrent(Device.Status.SelfTimer, value);
            }
        }

        public bool IsAvailableStillImageSize
        {
            get
            {
                return Device.Api.Capability.IsAvailable("setStillSize") &&
                    Device.Status.StillImageSize != null;
            }
        }

        public string[] CandidatesStillImageSize
        {
            get
            {
                return SettingValueConverter.FromStillImageSize(Device.Status.StillImageSize).candidates;
            }
        }

        public int SelectedIndexStillImageSize
        {
            get
            {
                return SettingValueConverter.GetSelectedIndex(Device.Status.StillImageSize);
            }
            set
            {
                SetSelectedAsCurrent(Device.Status.StillImageSize, value);
            }
        }


        public int SelectedIndexWhiteBalance
        {
            get
            {
                return SettingValueConverter.GetSelectedIndex(Device.Status.WhiteBalance);
            }
            set
            {
                SetSelectedAsCurrent(Device.Status.WhiteBalance, value);
            }
        }

        public string[] CandidatesWhiteBalance
        {
            get
            {
                return SettingValueConverter.FromWhiteBalance(Device.Status.WhiteBalance).candidates;
            }
        }

        public bool IsAvailableWhiteBalance
        {
            get
            {
                return Device.Api.Capability.IsAvailable("setWhiteBalance") &&
                    Device.Status.WhiteBalance != null;
            }
        }

        public bool IsAvailableColorTemperture
        {
            get
            {
                var status = Device.Status;
                return IsAvailableWhiteBalance &&
                    status.ColorTempertureCandidates.ContainsKey(status.WhiteBalance.current) &&
                    status.ColorTempertureCandidates[status.WhiteBalance.current].Length != 0 &&
                    status.ColorTemperture != -1;
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
