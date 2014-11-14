using Kazyx.RemoteApi;
using Kazyx.Uwpmm.CameraControl;
using Kazyx.Uwpmm.Utility;
using System.Collections.Generic;
using System.Diagnostics;

namespace Kazyx.Uwpmm.DataModel
{
    class ControlPanelDataSource : ObservableBase
    {
        private TargetDevice _Device;
        public TargetDevice Device
        {
           private set
            {
                _Device = value;
                NotifyChangedOnUI(""); // Notify all properties are changed.
            }
            get { return _Device; }
        }

        public ControlPanelDataSource(TargetDevice target)
        {
            this.Device = target;
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
                NotifyChangedOnUI("IsAvailableFocusMode");
                NotifyChangedOnUI("IsAvailableMovieQuality");
                NotifyChangedOnUI("IsAvailableFlashMode");
                NotifyChangedOnUI("IsAvailableSteadyMode");
                NotifyChangedOnUI("IsAvailableViewAngle");
            };
            Device.Api.ServerVersionDetected += (sender, e) =>
            {
                NotifyChangedOnUI("IsRestrictedApiVisible");
            };
        }

        private void GenericPropertyChanged(string name)
        {
            DebugUtil.Log("PropertyChanged: " + name);
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

        public List<string> CandidatesExposureMode
        {
            get
            {
                return SettingValueConverter.FromExposureMode(Device.Status.ExposureMode).Candidates;
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
                ParameterUtil.SetSelectedAsCurrent(Device.Status.ExposureMode, value);
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

        public List<string> CandidatesShootMode
        {
            get
            {
                return SettingValueConverter.FromShootMode(Device.Status.ShootMode).Candidates;
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
                ParameterUtil.SetSelectedAsCurrent(Device.Status.ShootMode, value);
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

        public List<string> CandidatesBeepMode
        {
            get
            {
                return SettingValueConverter.FromBeepMode(Device.Status.BeepMode).Candidates;
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
                ParameterUtil.SetSelectedAsCurrent(Device.Status.BeepMode, value);
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

        public List<string> CandidatesPostviewSize
        {
            get
            {
                return SettingValueConverter.FromPostViewSize(Device.Status.PostviewSize).Candidates;
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
                ParameterUtil.SetSelectedAsCurrent(Device.Status.PostviewSize, value);
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

        public List<string> CandidatesSelfTimer
        {
            get
            {
                return SettingValueConverter.FromSelfTimer(Device.Status.SelfTimer).Candidates;
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
                ParameterUtil.SetSelectedAsCurrent(Device.Status.SelfTimer, value);
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

        public List<string> CandidatesStillImageSize
        {
            get
            {
                return SettingValueConverter.FromStillImageSize(Device.Status.StillImageSize).Candidates;
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
                ParameterUtil.SetSelectedAsCurrent(Device.Status.StillImageSize, value);
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
                ParameterUtil.SetSelectedAsCurrent(Device.Status.WhiteBalance, value);
            }
        }

        public List<string> CandidatesWhiteBalance
        {
            get
            {
                return SettingValueConverter.FromWhiteBalance(Device.Status.WhiteBalance).Candidates;
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
                    status.ColorTempertureCandidates != null &&
                    status.WhiteBalance.Current != null &&
                    status.ColorTempertureCandidates.ContainsKey(status.WhiteBalance.Current) &&
                    status.ColorTempertureCandidates[status.WhiteBalance.Current].Length != 0 &&
                    status.ColorTemperture != -1;
            }
        }


        public int CpSelectedIndexViewAngle
        {
            get
            {
                return SettingValueConverter.GetSelectedIndex(Device.Status.ViewAngle);
            }
            set
            {
                ParameterUtil.SetSelectedAsCurrent(Device.Status.ViewAngle, value);
            }
        }

        public List<string> CpCandidatesViewAngle
        {
            get
            {
                return SettingValueConverter.FromViewAngle(Device.Status.ViewAngle).Candidates;
            }
        }

        public bool CpIsAvailableViewAngle
        {
            get
            {
                return Device.Api.Capability.IsAvailable("setViewAngle") &&
                    Device.Status.BeepMode != null;
            }
        }

        public int CpSelectedIndexSteadyMode
        {
            get
            {
                return SettingValueConverter.GetSelectedIndex(Device.Status.SteadyMode);
            }
            set
            {
                ParameterUtil.SetSelectedAsCurrent(Device.Status.SteadyMode, value);
            }
        }

        public List<string> CpCandidatesSteadyMode
        {
            get
            {
                return SettingValueConverter.FromSteadyMode(Device.Status.SteadyMode).Candidates;
            }
        }

        public bool CpIsAvailableSteadyMode
        {
            get
            {
                return Device.Api.Capability.IsAvailable("setSteadyMode") &&
                    Device.Status.SteadyMode != null;
            }
        }

        public int CpSelectedIndexMovieQuality
        {
            get
            {
                return SettingValueConverter.GetSelectedIndex(Device.Status.MovieQuality);
            }
            set
            {
                ParameterUtil.SetSelectedAsCurrent(Device.Status.MovieQuality, value);
            }
        }

        public List<string> CpCandidatesMovieQuality
        {
            get
            {
                return SettingValueConverter.FromMovieQuality(Device.Status.MovieQuality).Candidates;
            }
        }

        public bool CpIsAvailableMovieQuality
        {
            get
            {
                return Device.Api.Capability.IsAvailable("setMovieQuality") &&
                    Device.Status.MovieQuality != null;
            }
        }

        public int CpSelectedIndexFlashMode
        {
            get
            {
                return SettingValueConverter.GetSelectedIndex(Device.Status.FlashMode);
            }
            set
            {
                ParameterUtil.SetSelectedAsCurrent(Device.Status.FlashMode, value);
            }
        }

        public List<string> CpCandidatesFlashMode
        {
            get
            {
                return SettingValueConverter.FromFlashMode(Device.Status.FlashMode).Candidates;
            }
        }

        public bool CpIsAvailableFlashMode
        {
            get
            {
                return Device.Api.Capability.IsAvailable("setFlashMode") &&
                    Device.Status.FlashMode != null;
            }
        }

        public int CpSelectedIndexFocusMode
        {
            get
            {
                return SettingValueConverter.GetSelectedIndex(Device.Status.FocusMode);
            }
            set
            {
                ParameterUtil.SetSelectedAsCurrent(Device.Status.FocusMode, value);
            }
        }

        public List<string> CpCandidatesFocusMode
        {
            get
            {
                return SettingValueConverter.FromFocusMode(Device.Status.FocusMode).Candidates;
            }
        }

        public bool CpIsAvailableFocusMode
        {
            get
            {
                return Device.Api.Capability.IsAvailable("setFocusMode") &&
                    Device.Status.FocusMode != null;
            }
        }
    }
}
