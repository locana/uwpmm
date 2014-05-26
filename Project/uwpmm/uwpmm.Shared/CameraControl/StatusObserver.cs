using Kazyx.RemoteApi;
using Kazyx.Uwpmm.DataModel;
using System;
using System.Diagnostics;
using System.Threading.Tasks;

namespace Kazyx.Uwpmm.CameraControl
{
    public class StatusObserver
    {
        private readonly CameraApiClient camera;

        public StatusObserver(CameraApiClient camera)
        {
            this.camera = camera;
        }

        private CameraStatus target = null;

        public bool IsProcessing { get { return target != null; } }

        private int failure_count = 0;

        private const int RETRY_LIMIT = 3;

        private const int RETRY_INTERVAL_SEC = 3;

        public event Action EndByError;

        private ApiVersion version = ApiVersion.V1_0;

        public async Task<bool> Start(CameraStatus status, ApiVersion version = ApiVersion.V1_0)
        {
            Debug.WriteLine("StatusObserver: Start");
            if (IsProcessing)
            {
                Debug.WriteLine("StatusObserver: Already processing");
                return false;
            }

            this.version = version;
            failure_count = 0;
            if (!await Refresh())
            {
                Debug.WriteLine("StatusObserver: Failed to start");
                return false;
            }

            this.target = status;
            PollingLoop();
            return true;
        }

        public void Stop()
        {
            Debug.WriteLine("StatusObserver: Stop");
            target = null;
        }

        public async Task<bool> Refresh()
        {
            Debug.WriteLine("StatusObserver: Refresh");
            try
            {
                Update(await camera.GetEventAsync(false, version));
            }
            catch (RemoteApiException e)
            {
                Debug.WriteLine("StatusObserver: Refresh failed - " + e.code);
                return false;
            }
            return true;
        }

        private void Update(Event status)
        {
            // TODO update target
        }

        private async void PollingLoop()
        {
            if (!IsProcessing)
            {
                return;
            }

            try
            {
                OnSuccess(await camera.GetEventAsync(true, version));
            }
            catch (RemoteApiException e)
            {
                OnError(e.code);
            }
        }

        private void OnSuccess(Event @event)
        {
            failure_count = 0;
            Update(@event);
            PollingLoop();
        }

        private async void OnError(StatusCode code)
        {
            switch (code)
            {
                case StatusCode.Timeout:
                    Debug.WriteLine("GetEvent timeout without any event. Retry for the next event");
                    PollingLoop();
                    return;
                case StatusCode.NotAcceptable:
                case StatusCode.CameraNotReady:
                case StatusCode.IllegalState:
                case StatusCode.ServiceUnavailable:
                case StatusCode.Any:
                    if (failure_count++ < RETRY_LIMIT)
                    {
                        Debug.WriteLine("GetEvent failed - retry " + failure_count + ", status: " + code);
                        await Task.Delay(TimeSpan.FromSeconds(RETRY_INTERVAL_SEC));
                        PollingLoop();
                        return;
                    }
                    break;
                case StatusCode.DuplicatePolling:
                    Debug.WriteLine("GetEvent failed duplicate polling");
                    return;
                default:
                    Debug.WriteLine("GetEvent failed with code: " + code);
                    break;
            }

            Debug.WriteLine("StatusObserver Error limit");

            if (IsProcessing)
            {
                Stop();
                if (EndByError != null)
                {
                    EndByError.Invoke();
                }
            }
        }
    }
}
