using Kazyx.Uwpmm.DataModel;

namespace Kazyx.Uwpmm.CameraControl
{
    public class TargetDevice
    {
        public TargetDevice(DeviceApiHolder api)
        {
            _Api = api;
            _Observer = new StatusObserver(api);
            _Status = new CameraStatus();
        }

        private readonly DeviceApiHolder _Api;
        public DeviceApiHolder Api
        {
            get { return _Api; }
        }

        private StatusObserver _Observer;
        public StatusObserver Observer
        {
            get { return _Observer; }
        }

        private CameraStatus _Status;
        public CameraStatus Status
        {
            get { return _Status; }
        }
    }
}
