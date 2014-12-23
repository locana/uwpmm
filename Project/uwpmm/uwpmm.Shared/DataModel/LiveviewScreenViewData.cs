
using Kazyx.RemoteApi.Camera;
using Kazyx.Uwpmm.CameraControl;
using Kazyx.Uwpmm.Utility;
using System;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Media.Imaging;
namespace Kazyx.Uwpmm.DataModel
{
    public class LiveviewScreenViewData : ObservableBase
    {
        readonly TargetDevice Device;

        public LiveviewScreenViewData(TargetDevice d)
        {
            Device = d;
            Device.Status.PropertyChanged += (sender, e) =>
            {
                NotifyChangedOnUI("ZoomPositionInCurrentBox");
                NotifyChangedOnUI("ZoomBoxIndex");
                NotifyChangedOnUI("ZoomBoxNum");
                NotifyChangedOnUI("ShutterButtonImage");
                NotifyChangedOnUI("ShutterButtonEnabled");
                NotifyChangedOnUI("RecDisplayVisibility");
                NotifyChangedOnUI("ProgressDisplayVisibility");
                NotifyChangedOnUI("ShootModeImage");
                NotifyChangedOnUI("ExposureModeImage");
                NotifyChangedOnUI("MemoryCardStatusImage");
                NotifyChangedOnUI("RecordbaleAmount");
                NotifyChangedOnUI("RecordingCount");
                NotifyChangedOnUI("RecordingCountVisibility");



                //NotifyChangedOnUI("MaxProgramShift");
                //NotifyChangedOnUI("MinProgramShift");
                //NotifyChangedOnUI("ProgramShiftVisibility");
                NotifyChangedOnUI("EvVisibility");
                NotifyChangedOnUI("EvDisplayValue");
                //if (Device.Api.Capability.IsAvailable("setExposureCompensation"))
                //{
                //    //NotifyChangedOnUI("MinEvIndex");
                //    //NotifyChangedOnUI("MaxEvIndex");
                //    //NotifyChangedOnUI("CurrentEvIndex");
                //NotifyChangedOnUI("MinEvLabel");
                //NotifyChangedOnUI("MaxEvLabel");
                //}
                NotifyChangedOnUI("FnumberVisibility");
                NotifyChangedOnUI("FnumberDisplayValue");

                //if (Device.Api.Capability.IsAvailable("setFNumber"))
                //{
                //    NotifyChangedOnUI("MaxFNumberIndex");
                //    NotifyChangedOnUI("CurrentFNumberIndex");
                //    NotifyChangedOnUI("MaxFNumberLabel");
                //    NotifyChangedOnUI("MinFNumberLabel");
                //}

                NotifyChangedOnUI("ISOVisibility");
                NotifyChangedOnUI("ISODisplayValue");

                //if (Device.Api.Capability.IsAvailable("setIsoSpeedRate"))
                //{
                //    NotifyChangedOnUI("MaxIsoIndex");
                //    NotifyChangedOnUI("CurrentIsoIndex");
                //    NotifyChangedOnUI("MinIsoLabel");
                //    NotifyChangedOnUI("MaxIsoLabel");
                //}
                NotifyChangedOnUI("ShutterSpeedVisibility");
                NotifyChangedOnUI("ShutterSpeedDisplayValue");

                //if (Device.Api.Capability.IsAvailable("setShutterSpeed"))
                //{
                //    NotifyChangedOnUI("MaxShutterSpeedIndex");
                //    NotifyChangedOnUI("CurrentShutterSpeedIndex");
                //    NotifyChangedOnUI("MaxShutterSpeedLabel");
                //    NotifyChangedOnUI("MinShutterSpeedLabel");
                //}
            };
            Device.Api.AvailiableApisUpdated += (sender, e) =>
            {
                NotifyChangedOnUI("ZoomInterfacesVisibility");
                NotifyChangedOnUI("ShutterButtonEnabled");
                NotifyChangedOnUI("RecDisplayVisibility");
                NotifyChangedOnUI("FNumberBrush");
                NotifyChangedOnUI("ShutterSpeedBrush");
                NotifyChangedOnUI("EvBrush");
                NotifyChangedOnUI("IsoBrush");
                NotifyChangedOnUI("ShutterSpeedVisibility");
                NotifyChangedOnUI("ShutterSpeedDisplayValue");
                NotifyChangedOnUI("ISOVisibility");
                NotifyChangedOnUI("ISODisplayValue");
                NotifyChangedOnUI("FnumberVisibility");
                NotifyChangedOnUI("FnumberDisplayValue");
                NotifyChangedOnUI("EvVisibility");
            };
        }

        private static readonly BitmapImage StillImage = new BitmapImage(new Uri("ms-appx:///Assets/LiveviewScreen/Camera.png", UriKind.Absolute));
        private static readonly BitmapImage CamImage = new BitmapImage(new Uri("ms-appx:///Assets/LiveviewScreen/Camcorder.png", UriKind.Absolute));
        private static readonly BitmapImage AudioImage = new BitmapImage(new Uri("ms-appx:///Assets/LiveviewScreen/Music.png", UriKind.Absolute));
        private static readonly BitmapImage StopImage = new BitmapImage(new Uri("ms-appx:///Assets/LiveviewScreen/Stop.png", UriKind.Absolute));
        private static readonly BitmapImage IntervalStillImage = new BitmapImage(new Uri("ms-appx:///Assets/LiveviewScreen/IntervalStillRecButton.png", UriKind.Absolute));
        private static readonly BitmapImage ContShootingImage = new BitmapImage(new Uri("ms-appx:///Assets/LiveviewScreen/ContShootingButton.png", UriKind.Absolute));

        private static readonly BitmapImage StillModeImage = new BitmapImage(new Uri("ms-appx:///Assets/LiveviewScreen/mode_photo.png", UriKind.Absolute));
        private static readonly BitmapImage MovieModeImage = new BitmapImage(new Uri("ms-appx:///Assets/LiveviewScreen/mode_movie.png", UriKind.Absolute));
        private static readonly BitmapImage IntervalModeImage = new BitmapImage(new Uri("ms-appx:///Assets/LiveviewScreen/mode_interval.png", UriKind.Absolute));
        private static readonly BitmapImage AudioModeImage = new BitmapImage(new Uri("ms-appx:///Assets/LiveviewScreen/mode_audio.png", UriKind.Absolute));

        private static readonly BitmapImage AModeImage = new BitmapImage(new Uri("ms-appx:///Assets/LiveviewScreen/ExposureMode_A.png", UriKind.Absolute));
        private static readonly BitmapImage IAModeImage = new BitmapImage(new Uri("ms-appx:///Assets/LiveviewScreen/ExposureMode_iA.png", UriKind.Absolute));
        private static readonly BitmapImage IAPlusModeImage = new BitmapImage(new Uri("ms-appx:///Assets/LiveviewScreen/ExposureMode_iAPlus.png", UriKind.Absolute));
        private static readonly BitmapImage MModeImage = new BitmapImage(new Uri("ms-appx:///Assets/LiveviewScreen/ExposureMode_M.png", UriKind.Absolute));
        private static readonly BitmapImage SModeImage = new BitmapImage(new Uri("ms-appx:///Assets/LiveviewScreen/ExposureMode_S.png", UriKind.Absolute));
        private static readonly BitmapImage PModeImage = new BitmapImage(new Uri("ms-appx:///Assets/LiveviewScreen/ExposureMode_P.png", UriKind.Absolute));
        private static readonly BitmapImage PShiftModeImage = new BitmapImage(new Uri("ms-appx:///Assets/LiveviewScreen/ExposureMode_P_shift.png", UriKind.Absolute));

        private static readonly BitmapImage AvailableMediaImage = new BitmapImage(new Uri("ms-appx:///Assets/LiveviewScreen/memory_card.png", UriKind.Absolute));
        private static readonly BitmapImage NoMediaImage = new BitmapImage(new Uri("ms-appx:///Assets/LiveviewScreen/no_memory_card.png", UriKind.Absolute));

        public BitmapImage ShutterButtonImage
        {
            get
            {
                if (Device.Status.ShootMode == null)
                {
                    return StillImage;
                }
                switch (Device.Status.ShootMode.Current)
                {
                    case ShootModeParam.Still:
                        return StillImage;
                    case ShootModeParam.Movie:
                        return CamImage;
                    case ShootModeParam.Audio:
                        return AudioImage;
                    case ShootModeParam.Interval:
                        return IntervalStillImage;
                    default:
                        return null;
                }
            }
        }

        public BitmapImage ShootModeImage
        {
            get
            {
                if (Device.Status.ShootMode == null) { return null; }
                switch (Device.Status.ShootMode.Current)
                {
                    case ShootModeParam.Still:
                        return StillModeImage;
                    case ShootModeParam.Movie:
                        return MovieModeImage;
                    case ShootModeParam.Interval:
                        return IntervalModeImage;
                    case ShootModeParam.Audio:
                        return AudioImage;
                }
                return null;
            }
        }

        public BitmapImage ExposureModeImage
        {
            get
            {
                if (Device.Status.ExposureMode == null) { return null; }
                switch (Device.Status.ExposureMode.Current)
                {
                    case ExposureMode.Intelligent:
                        return IAModeImage;
                    case ExposureMode.Superior:
                        return IAPlusModeImage;
                    case ExposureMode.Program:
                        return PModeImage;
                    case ExposureMode.Aperture:
                        return AModeImage;
                    case ExposureMode.SS:
                        return SModeImage;
                    case ExposureMode.Manual:
                        return MModeImage;
                }
                return null;
            }
        }

        public BitmapImage MemoryCardStatusImage
        {
            get
            {
                if (Device.Status.Storages == null) return null;
                foreach (var storage in Device.Status.Storages)
                {
                    if (storage.RecordTarget)
                    {
                        switch (storage.StorageID)
                        {
                            case "No Media":
                                return NoMediaImage;
                            case "Memory Card 1":
                            default:
                                return AvailableMediaImage;
                        }
                    }
                }
                return NoMediaImage;
            }
        }

        public bool ShutterButtonEnabled
        {
            get
            {
                if (Device.Api.Capability == null)
                {
                    return false;
                }
                switch (Device.Status.ShootMode.Current)
                {
                    case ShootModeParam.Still:
                        if (Device.Status.Status == EventParam.Idle) { return true; }
                        break;
                    case ShootModeParam.Movie:
                        if (Device.Status.Status == EventParam.Idle || Device.Status.Status == EventParam.MvRecording) { return true; }
                        break;
                    case ShootModeParam.Audio:
                        if (Device.Status.Status == EventParam.Idle || Device.Status.Status == EventParam.AuRecording) { return true; }
                        break;
                    case ShootModeParam.Interval:
                        if (Device.Status.Status == EventParam.Idle || Device.Status.Status == EventParam.ItvRecording) { return true; }
                        break;
                }
                return false;
            }
        }

        public Visibility ZoomInterfacesVisibility
        {
            get
            {
                if (Device.Api.Capability.IsAvailable("actZoom")) { return Visibility.Visible; }
                return Visibility.Collapsed;
            }
        }

        public Visibility RecDisplayVisibility
        {
            get
            {
                if (Device.Status == null) { return Visibility.Collapsed; }
                switch (Device.Status.Status)
                {
                    case EventParam.MvRecording:
                    case EventParam.AuRecording:
                    case EventParam.ItvRecording:
                        return Visibility.Visible;
                }

                return Visibility.Collapsed;
            }
        }

        public Visibility ProgressDisplayVisibility
        {
            get
            {
                if (Device.Status == null) { return Visibility.Visible; }
                switch (Device.Status.Status)
                {
                    case EventParam.Idle:
                    case EventParam.MvRecording:
                    case EventParam.AuRecording:
                    case EventParam.ItvRecording:
                        return Visibility.Collapsed;
                }
                return Visibility.Visible;
            }
        }

        public int ZoomPositionInCurrentBox
        {
            get
            {
                if (Device.Status.ZoomInfo == null) { return 0; }
                DebugUtil.Log("Zoom pos " + Device.Status.ZoomInfo.PositionInCurrentBox);
                return Device.Status.ZoomInfo.PositionInCurrentBox;
            }
        }

        public int ZoomBoxIndex
        {
            get
            {
                if (Device.Status.ZoomInfo == null) { return 0; }
                return Device.Status.ZoomInfo.CurrentBoxIndex;
            }
        }

        public int ZoomBoxNum
        {
            get
            {
                if (Device.Status.ZoomInfo == null) { return 0; }
                return Device.Status.ZoomInfo.NumberOfBoxes;
            }
        }

        public string RecordbaleAmount
        {
            get
            {
                if (Device.Status.Storages == null || Device.Status.ShootMode == null) { return ""; }
                foreach (StorageInfo storage in Device.Status.Storages)
                {
                    if (storage.RecordTarget)
                    {
                        switch (Device.Status.ShootMode.Current)
                        {
                            case ShootModeParam.Still:
                            case ShootModeParam.Interval:
                                if (storage.RecordableImages == -1) { return ""; }
                                return storage.RecordableImages.ToString();
                            case ShootModeParam.Movie:
                            case ShootModeParam.Audio:
                                if (storage.RecordableMovieLength == -1) { return ""; }
                                return storage.RecordableMovieLength.ToString() + " min.";
                            default:
                                break;
                        }
                    }
                }
                return "";
            }
        }

        public string RecordingCount
        {
            get
            {
                if (Device.Status.ShootMode == null) { return ""; }
                switch (Device.Status.ShootMode.Current)
                {
                    case ShootModeParam.Movie:
                    case ShootModeParam.Audio:
                        if (Device.Status.RecordingTimeSec < 0) { return ""; }
                        var min = Device.Status.RecordingTimeSec / 60;
                        var sec = Device.Status.RecordingTimeSec - min * 60;
                        return min.ToString("##00") + ":" + sec.ToString("00");
                    case ShootModeParam.Interval:
                        if (Device.Status.NumberOfShots < 0) { return ""; }
                        return Device.Status.NumberOfShots + " pics.";
                }
                return "";
            }
        }

        public Visibility RecordingCountVisibility
        {
            get
            {
                if (Device.Status.ShootMode == null) { return Visibility.Collapsed; }
                switch (Device.Status.ShootMode.Current)
                {
                    case ShootModeParam.Movie:
                        if (Device.Status.RecordingTimeSec >= 0 && Device.Status.Status == EventParam.MvRecording) { return Visibility.Visible; }
                        break;
                    case ShootModeParam.Audio:
                        if (Device.Status.RecordingTimeSec >= 0 && Device.Status.Status == EventParam.AuRecording) { return Visibility.Visible; }
                        break;
                    case ShootModeParam.Interval:
                        if (Device.Status.NumberOfShots >= 0 && Device.Status.Status == EventParam.ItvRecording) { return Visibility.Visible; }
                        break;
                }
                return Visibility.Collapsed;
            }
        }


        public Visibility ShutterSpeedVisibility
        {
            get
            {
                if (Device.Status == null || Device.Status.ShutterSpeed == null || Device.Status.ShutterSpeed.Current == null || !Device.Api.Capability.IsAvailable("getShutterSpeed")) { return Visibility.Collapsed; }
                else { return Visibility.Visible; }
            }
        }

        public string ShutterSpeedDisplayValue
        {
            get
            {
                if (Device.Status == null || Device.Status.ShutterSpeed == null || Device.Status.ShutterSpeed.Current == null)
                {
                    return "--";
                }
                else
                {
                    return Device.Status.ShutterSpeed.Current;
                }
            }
        }

        public Visibility ISOVisibility
        {
            get
            {
                if (Device.Status == null || Device.Status.ISOSpeedRate == null || Device.Status.ISOSpeedRate.Current == null || !Device.Api.Capability.IsAvailable("getIsoSpeedRate")) { return Visibility.Collapsed; }
                else { return Visibility.Visible; }
            }
        }

        public string ISODisplayValue
        {
            get
            {
                if (Device.Status == null || Device.Status.ISOSpeedRate == null || Device.Status.ISOSpeedRate.Current == null) { return "ISO: --"; }
                else { return "ISO " + Device.Status.ISOSpeedRate.Current; }
            }
        }

        public Visibility ProgramShiftVisibility
        {
            get
            {
                if (Device.Status == null || Device.Status.ProgramShiftRange == null || Device.Status.ExposureMode == null || Device.Status.ExposureMode.Current != ExposureMode.Program) { return Visibility.Collapsed; }
                else { return Visibility.Visible; }
            }
        }

        private int _ProgramShift = 0;
        public int ProgramShift
        {
            get
            {
                if (Device.Status == null || !Device.Status.ProgramShiftActivated)
                {
                    _ProgramShift = 0;
                }
                return _ProgramShift;
            }
            set
            {
                _ProgramShift = value;
            }
        }

        public int MaxProgramShift
        {
            get
            {
                if (Device.Status == null || Device.Status.ProgramShiftRange == null) { return 0; }
                else { return Device.Status.ProgramShiftRange.Max; }
            }
        }

        public int MinProgramShift
        {
            get
            {
                if (Device.Status == null || Device.Status.ProgramShiftRange == null) { return 0; }
                else { return Device.Status.ProgramShiftRange.Min; }
            }
        }

        public Visibility FnumberVisibility
        {
            get
            {
                if (Device.Status == null || Device.Status.FNumber == null || Device.Status.FNumber.Current == null || !Device.Api.Capability.IsAvailable("getFNumber")) { return Visibility.Collapsed; }
                else { return Visibility.Visible; }
            }
        }

        public string FnumberDisplayValue
        {
            get
            {
                if (Device.Status == null || Device.Status.FNumber == null || Device.Status.FNumber.Current == null) { return "F--"; }
                else { return "F" + Device.Status.FNumber.Current; }
            }
        }

        public Visibility EvVisibility
        {
            get
            {
                if (Device.Status == null || Device.Status.EvInfo == null || !Device.Api.Capability.IsAvailable("getExposureCompensation")) { return Visibility.Collapsed; }
                else { return Visibility.Visible; }
            }
        }


        public Brush FNumberBrush
        {
            get
            {
                if (Device.Status == null || !Device.Api.Capability.IsAvailable("setFNumber")) { return ResourceManager.ForegroundBrush; }
                else { return ResourceManager.AccentColorBrush; }
            }
        }

        public Brush ShutterSpeedBrush
        {
            get
            {
                if (Device.Status == null || !Device.Api.Capability.IsAvailable("setShutterSpeed")) { return ResourceManager.ForegroundBrush; }
                else { return ResourceManager.AccentColorBrush; }
            }
        }

        public Brush EvBrush
        {
            get
            {
                if (Device.Status == null || !Device.Api.Capability.IsAvailable("setExposureCompensation")) { return ResourceManager.ForegroundBrush; }
                else { return ResourceManager.AccentColorBrush; }
            }
        }

        public Brush IsoBrush
        {
            get
            {
                if (Device.Status == null || !Device.Api.Capability.IsAvailable("setIsoSpeedRate")) { return ResourceManager.ForegroundBrush; }
                else { return ResourceManager.AccentColorBrush; }
            }
        }

        public string EvDisplayValue
        {
            get
            {
                if (Device.Status == null || Device.Status.EvInfo == null) { return ""; }
                else
                {
                    var value = EvConverter.GetEv(Device.Status.EvInfo.CurrentIndex, Device.Status.EvInfo.Candidate.IndexStep);
                    var strValue = Math.Round(value, 1, MidpointRounding.AwayFromZero).ToString("0.0");

                    if (value < 0) { return "EV " + strValue; }
                    else if (value == 0.0f) { return "EV " + strValue; }
                    else { return "EV +" + strValue; }
                }
            }
        }
    }
}
