using Kazyx.RemoteApi;
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
    }
}
