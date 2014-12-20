using Kazyx.ImageStream;
using Kazyx.RemoteApi;
using Kazyx.Uwpmm.DataModel;
using Kazyx.Uwpmm.Utility;
using System;
using System.Threading.Tasks;

namespace Kazyx.Uwpmm.CameraControl
{
    public class SequentialOperation
    {
        public static async Task SetUp(TargetDevice device, StreamProcessor liveview)
        {
            DebugUtil.Log("Set up control");
            try
            {
                await device.Api.RetrieveApiList();
                var info = await device.Api.Camera.GetApplicationInfoAsync();
                device.Api.Capability.Version = new ServerVersion(info.Version);
                device.Api.Capability.AvailableApis = await device.Api.Camera.GetAvailableApiListAsync();

                if (device.Api.Capability.IsSupported("startRecMode"))
                {
                    await device.Api.Camera.StartRecModeAsync();
                }
                if (device.Api.Capability.IsAvailable("startLiveview"))
                {
                    var res = await OpenLiveviewStream(device.Api, liveview);
                    if (!res)
                    {
                        DebugUtil.Log("Failed to open liveview connection.");
                        throw new Exception();
                    }
                }

                if (device.Api.Capability.IsSupported("setCurrentTime"))
                {
                    try
                    {
                        await device.Api.System.SetCurrentTimeAsync(DateTimeOffset.UtcNow, (int)DateTimeOffset.Now.Offset.TotalMinutes);
                    }
                    catch (RemoteApiException) { }
                }

                await device.Observer.Start();
            }
            catch (RemoteApiException e)
            {
                DebugUtil.Log("Failed setup: " + e.code);
                throw e;
            }
        }

        public static async Task<bool> OpenLiveviewStream(DeviceApiHolder api, StreamProcessor liveview)
        {
            DebugUtil.Log("Open liveview stream");
            try
            {
                var url = await api.Camera.StartLiveviewAsync();
                return await liveview.OpenConnection(new Uri(url));
            }
            catch (RemoteApiException e)
            {
                DebugUtil.Log("Failed to startLiveview: " + e.code);
                return false;
            }
        }

        public static async Task<bool> CloseLiveviewStream(DeviceApiHolder api, StreamProcessor liveview)
        {
            DebugUtil.Log("Close liveview stream");
            try
            {
                liveview.CloseConnection();
                await api.Camera.StopLiveviewAsync();
                return true;
            }
            catch (RemoteApiException e)
            {
                DebugUtil.Log("Failed to stopLiveview: " + e.code);
                return false;
            }
        }

        public static async Task<bool> ReOpenLiveviewStream(DeviceApiHolder api, StreamProcessor liveview)
        {
            DebugUtil.Log("Reopen liveview stream");
            liveview.CloseConnection();
            await Task.Delay(2000);
            return await OpenLiveviewStream(api, liveview);
        }

        public static async Task<bool> TakePicture(DeviceApiHolder api)
        {
            return await TakePicture(api, false);
        }

        private static async Task<bool> TakePicture(DeviceApiHolder api, bool awaiting = false)
        {
            DebugUtil.Log("Taking picture sequence");
            try
            {
                var urls = awaiting ? await api.Camera.AwaitTakePictureAsync() : await api.Camera.ActTakePictureAsync();
                DebugUtil.Log("Success taking picture");

                if (ApplicationSettings.GetInstance().IsPostviewTransferEnabled)
                {
                    foreach (var url in urls)
                    {
                        try
                        {
                            var uri = new Uri(url);
                            PictureDownloader.Instance.Enqueue(uri);
                        }
                        catch (Exception e)
                        {
                            DebugUtil.Log(e.Message);
                            DebugUtil.Log(e.StackTrace);
                            return false;
                        }
                    }
                    return true;
                }
                else
                {
                    return true;
                }
            }
            catch (RemoteApiException e)
            {
                if (e.code != StatusCode.StillCapturingNotFinished)
                {
                    DebugUtil.Log("Failed to take picture: " + e.code);
                    throw e;
                }
            }
            DebugUtil.Log("Take picture timeout: await for completion");
            return await TakePicture(api, true);
        }
    }
}
