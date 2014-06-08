
using Kazyx.RemoteApi;

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
    }
}
