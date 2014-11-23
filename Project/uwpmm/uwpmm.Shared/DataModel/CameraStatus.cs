using Kazyx.RemoteApi;
using Kazyx.RemoteApi.Camera;
using System.Collections.Generic;

namespace Kazyx.Uwpmm.DataModel
{
    public class CameraStatus : ObservableBase
    {
        private Capability<string> _ExposureMode;
        public Capability<string> ExposureMode
        {
            set
            {
                _ExposureMode = value;
                NotifyChangedOnUI("ExposureMode");
            }
            get { return _ExposureMode; }
        }

        private Capability<string> _ShootMode;
        public Capability<string> ShootMode
        {
            set
            {
                _ShootMode = value;
                NotifyChangedOnUI("ShootMode");
            }
            get { return _ShootMode; }
        }

        private Capability<string> _PostviewSize;
        public Capability<string> PostviewSize
        {
            set
            {
                _PostviewSize = value;
                NotifyChangedOnUI("PostviewSize");
            }
            get { return _PostviewSize; }
        }

        private Capability<string> _BeepMode;
        public Capability<string> BeepMode
        {
            set
            {
                _BeepMode = value;
                NotifyChangedOnUI("BeepMode");
            }
            get { return _BeepMode; }
        }

        private Capability<int> _SelfTimer;
        public Capability<int> SelfTimer
        {
            set
            {
                _SelfTimer = value;
                NotifyChangedOnUI("SelfTimer");
            }
            get { return _SelfTimer; }
        }

        private Capability<StillImageSize> _StillImageSize;
        public Capability<StillImageSize> StillImageSize
        {
            set
            {
                _StillImageSize = value;
                NotifyChangedOnUI("StillImageSize");
            }
            get { return _StillImageSize; }
        }

        private Capability<string> _WhiteBalance;
        public Capability<string> WhiteBalance
        {
            set
            {
                _WhiteBalance = value;
                NotifyChangedOnUI("WhiteBalance");
                NotifyChangedOnUI("ColorTemperture");
            }
            get { return _WhiteBalance; }
        }

        private int _ColorTemperture = -1;
        public int ColorTemperture
        {
            set
            {
                _ColorTemperture = value;
                NotifyChangedOnUI("ColorTemperture");
            }
            get { return _ColorTemperture; }
        }

        private Dictionary<string, int[]> _ColorTempertureCandidates;
        public Dictionary<string, int[]> ColorTempertureCandidates
        {
            set
            {
                _ColorTempertureCandidates = value;
                NotifyChangedOnUI("ColorTemperture");
            }
            get { return _ColorTempertureCandidates; }
        }

        private Capability<string> _ShutterSpeed;
        public Capability<string> ShutterSpeed
        {
            set
            {
                _ShutterSpeed = value;
                NotifyChangedOnUI("ShutterSpeed");
            }
            get { return _ShutterSpeed; }
        }

        private Capability<string> _ISOSpeedRate;
        public Capability<string> ISOSpeedRate
        {
            set
            {
                _ISOSpeedRate = value;
                NotifyChangedOnUI("ISOSpeedRate");
            }
            get { return _ISOSpeedRate; }
        }

        private Capability<string> _FNumber;
        public Capability<string> FNumber
        {
            set
            {
                _FNumber = value;
                NotifyChangedOnUI("FNumber");
            }
            get { return _FNumber; }
        }

        private string _Status = EventParam.NotReady;
        public string Status
        {
            set
            {
                if (value != _Status)
                {
                    _Status = value;
                    NotifyChangedOnUI("Status");
                }
            }
            get { return _Status; }
        }

        private ZoomInfo _ZoomInfo = null;
        public ZoomInfo ZoomInfo
        {
            set
            {
                _ZoomInfo = value;
                NotifyChangedOnUI("ZoomInfo");
            }
            get { return _ZoomInfo; }
        }


        private Capability<string> _FocusMode;
        public Capability<string> FocusMode
        {
            set
            {
                _FocusMode = value;
                NotifyChangedOnUI("FocusMode");
            }
            get { return _FocusMode; }
        }

        private Capability<string> _MovieQuality;
        public Capability<string> MovieQuality
        {
            set
            {
                _MovieQuality = value;
                NotifyChangedOnUI("MovieQuality");
            }
            get { return _MovieQuality; }
        }

        private Capability<string> _SteadyMode;
        public Capability<string> SteadyMode
        {
            set
            {
                _SteadyMode = value;
                NotifyChangedOnUI("SteadyMode");
            }
            get { return _SteadyMode; }
        }

        private Capability<int> _ViewAngle;
        public Capability<int> ViewAngle
        {
            set
            {
                _ViewAngle = value;
                NotifyChangedOnUI("ViewAngle");
            }
            get { return _ViewAngle; }
        }

        private Capability<string> _FlashMode;
        public Capability<string> FlashMode
        {
            set
            {
                _FlashMode = value;
                NotifyChangedOnUI("FlashMode");
            }
            get { return _FlashMode; }
        }

        private TouchFocusStatus _TouchFocusStatus;
        public TouchFocusStatus TouchFocusStatus
        {
            set
            {
                _TouchFocusStatus = value;
                NotifyChangedOnUI("TouchFocusStatus");
            }
            get
            {
                return _TouchFocusStatus;
            }
        }

        private EvCapability _EvInfo;
        public EvCapability EvInfo
        {
            set
            {
                _EvInfo = value;
                NotifyChangedOnUI("EvInfo");
            }
            get { return _EvInfo; }
        }

        private StorageInfo[] _Storages;
        public StorageInfo[] Storages
        {
            set
            {
                _Storages = value;
                NotifyChangedOnUI("Storages");
            }
            get { return _Storages; }
        }

        private string _LiveviewOrientation;
        public string LiveviewOrientation
        {
            set
            {
                _LiveviewOrientation = value;
                NotifyChangedOnUI("LiveviewOrientation");
            }
            get { return _LiveviewOrientation == null ? Orientation.Straight : _LiveviewOrientation; }
        }

        private List<string> _PictureUrls;
        public List<string> PictureUrls
        {
            set
            {
                _PictureUrls = value;
                NotifyChangedOnUI("PictureUrls");
            }
            get { return _PictureUrls; }
        }

        private bool _ProgramShiftActivated = false;
        public bool ProgramShiftActivated
        {
            set
            {
                _ProgramShiftActivated = value;
                NotifyChangedOnUI("ProgramShiftActivated");
            }
            get { return _ProgramShiftActivated; }
        }

        private ProgramShiftRange _ProgramShiftRange;
        public ProgramShiftRange ProgramShiftRange
        {
            set
            {
                _ProgramShiftRange = value;
                NotifyChangedOnUI("ProgramShiftRange");
            }
            get
            {
                return _ProgramShiftRange;
            }
        }

        private string _FocusStatus;
        public string FocusStatus
        {
            set
            {
                _FocusStatus = value;
                NotifyChangedOnUI("FocusStatus");
            }
            get { return _FocusStatus; }
        }

        private bool _IsLiveviewAvailable = false;
        public bool IsLiveviewAvailable
        {
            set
            {
                if (_IsLiveviewAvailable != value)
                {
                    _IsLiveviewAvailable = value;
                    NotifyChangedOnUI("IsLiveviewAvailable");
                }
            }
            get
            {
                return _IsLiveviewAvailable;
            }
        }

        private bool _IsFocusFrameInfoAvailable = false;
        public bool IsLiveviewFrameInfoAvailable
        {
            set
            {
                if (_IsFocusFrameInfoAvailable != value)
                {
                    _IsFocusFrameInfoAvailable = value;
                    NotifyChangedOnUI("IsFocusFrameInfoAvailable");
                }
            }
            get
            {
                return _IsFocusFrameInfoAvailable;
            }
        }
    }
}
