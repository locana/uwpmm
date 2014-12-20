using Kazyx.Uwpmm.DataModel;

namespace Kazyx.Uwpmm.CameraControl
{
    public class TargetDevice
    {
        public TargetDevice(string udn, DeviceApiHolder api)
        {
            Udn = udn;
            _Api = api;
            _Status = new CameraStatus();
            _Observer = new StatusObserver(this);
        }

        public string Udn { private set; get; }

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

        public bool StorageAccessSupported
        {
            get { return Api.AvContent != null; }
        }
    }
}
