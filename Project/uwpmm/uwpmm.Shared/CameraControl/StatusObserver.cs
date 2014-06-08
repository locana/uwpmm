using Kazyx.RemoteApi;
using Kazyx.Uwpmm.DataModel;
using System;
using System.Diagnostics;
using System.Threading.Tasks;

namespace Kazyx.Uwpmm.CameraControl
{
    public class StatusObserver
    {
        private readonly DeviceApiHolder api;

        public StatusObserver(DeviceApiHolder api)
        {
            this.api = api;
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
            this.target = status;

            failure_count = 0;
            if (!await Refresh())
            {
                Debug.WriteLine("StatusObserver: Failed to start");
                return false;
            }

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
                await Update(await api.Camera.GetEventAsync(false, version));
            }
            catch (RemoteApiException e)
            {
                Debug.WriteLine("StatusObserver: Refresh failed - " + e.code);
                return false;
            }
            return true;
        }

        private async Task Update(Event status)
        {
            // TODO update target
            if (status.AvailableApis != null)
            {
                api.Capability.AvailableApis = status.AvailableApis;
            }

            if (status.ShootModeInfo != null)
            {
                target.ShootMode = status.ShootModeInfo;
            }
            if (status.ExposureMode != null)
            {
                target.ExposureMode = status.ExposureMode;
            }
            if (status.PostviewSizeInfo != null)
            {
                target.PostviewSize = status.PostviewSizeInfo;
            }
            if (status.SelfTimerInfo != null)
            {
                target.SelfTimer = status.SelfTimerInfo;
            }
            if (status.BeepMode != null)
            {
                target.BeepMode = status.BeepMode;
            }
            if (status.StillImageSize != null)
            {
                if (status.StillImageSize.CapabilityChanged)
                {
                    try
                    {
                        var size = await api.Camera.GetAvailableStillSizeAsync();
                        Array.Sort(size.candidates, CompareStillSize);
                        target.StillImageSize = size;
                    }
                    catch (RemoteApiException)
                    {
                        Debug.WriteLine("Failed to get still image size capability");
                    }
                }
                else
                {
                    target.StillImageSize.current = status.StillImageSize.Current;
                    target.StillImageSize = target.StillImageSize;
                }
            }
        }

        private async void PollingLoop()
        {
            if (!IsProcessing)
            {
                return;
            }

            try
            {
                OnSuccess(await api.Camera.GetEventAsync(true, version));
            }
            catch (RemoteApiException e)
            {
                OnError(e.code);
            }
        }

        private async void OnSuccess(Event @event)
        {
            failure_count = 0;
            await Update(@event);
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

        private static int CompareStillSize(StillImageSize x, StillImageSize y)
        {
            if (x == null && y == null)
            {
                return 0;
            }
            if (x == null)
            {
                return -1;
            }
            if (y == null)
            {
                return 1;
            }

            if (!x.SizeDefinition.EndsWith("M") || !y.SizeDefinition.EndsWith("M"))
            {
                var comp = x.SizeDefinition.CompareTo(y.SizeDefinition);
                if (comp == 0)
                {
                    return x.AspectRatio.CompareTo(y.AspectRatio);
                }
                else
                {
                    return comp;
                }
            }

            var xv = (int)double.Parse(x.SizeDefinition.Substring(0, x.SizeDefinition.Length - 1)) * 100;
            var yv = (int)double.Parse(y.SizeDefinition.Substring(0, y.SizeDefinition.Length - 1)) * 100;

            if (xv == yv)
            {
                return x.AspectRatio.CompareTo(y.AspectRatio);
            }
            else
            {
                return xv < yv ? 1 : -1;
            }
        }
    }
}
