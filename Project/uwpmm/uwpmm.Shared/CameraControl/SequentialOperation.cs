using Kazyx.ImageStream;
using Kazyx.RemoteApi;
using Kazyx.Uwpmm.DataModel;
using Kazyx.Uwpmm.Playback;
using Kazyx.Uwpmm.Utility;
using System;
using System.Threading.Tasks;
using Windows.Devices.Geolocation;

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
                var info = await device.Api.Camera.GetApplicationInfoAsync().ConfigureAwait(false);
                device.Api.Capability.Version = new ServerVersion(info.Version);
                device.Api.Capability.AvailableApis = await device.Api.Camera.GetAvailableApiListAsync().ConfigureAwait(false);

                await device.Observer.StartAsync().ConfigureAwait(false);

                if (device.Api.AvContent != null)
                {
                    DebugUtil.Log("This device support ContentsTransfer mode. Turn on Shooting mode at first.");
                    if (!await PlaybackModeHelper.MoveToShootingModeAsync(device.Api.Camera, device.Status).ConfigureAwait(false))
                    {
                        DebugUtil.Log("Failed state transition to shooting mode. Maybe in movie streaming mode...");
                        await device.Api.AvContent.StopStreamingAsync().ConfigureAwait(false);
                        DebugUtil.Log("Successfully stopped movie streaming mode");
                        await PlaybackModeHelper.MoveToShootingModeAsync(device.Api.Camera, device.Status).ConfigureAwait(false);
                    }
                }

                if (device.Api.Capability.IsSupported("startRecMode"))
                {
                    await device.Api.Camera.StartRecModeAsync().ConfigureAwait(false);
                }
                if (device.Api.Capability.IsAvailable("startLiveview"))
                {
                    var res = await OpenLiveviewStream(device.Api, liveview).ConfigureAwait(false);
                    if (!res)
                    {
                        DebugUtil.Log("Failed to open liveview connection.");
                        throw new Exception("Failed to open liveview connection.");
                    }
                }

                if (device.Api.Capability.IsSupported("setCurrentTime"))
                {
                    try
                    {
                        await device.Api.System.SetCurrentTimeAsync( //
                            DateTimeOffset.UtcNow, (int)DateTimeOffset.Now.Offset.TotalMinutes).ConfigureAwait(false);
                    }
                    catch (RemoteApiException) { } // This API always fails on some models.
                }
            }
            catch (RemoteApiException e)
            {
                DebugUtil.Log("Failed setup: " + e.code);
                device.Observer.Stop();
                throw e;
            }
        }

        public static async Task<bool> OpenLiveviewStream(DeviceApiHolder api, StreamProcessor liveview)
        {
            DebugUtil.Log("Open liveview stream");
            try
            {
                var url = await api.Camera.StartLiveviewAsync().ConfigureAwait(false);
                return await liveview.OpenConnection(new Uri(url)).ConfigureAwait(false);
            }
            catch (RemoteApiException e)
            {
                DebugUtil.Log("Failed to startLiveview: " + e.code);
                return false;
            }
            catch (Exception e)
            {
                DebugUtil.Log("Unknown error while opening liveview stream: " + e.StackTrace);
                return false;
            }
        }

        public static async Task<bool> CloseLiveviewStream(DeviceApiHolder api, StreamProcessor liveview)
        {
            DebugUtil.Log("Close liveview stream");
            try
            {
                liveview.CloseConnection();
                await api.Camera.StopLiveviewAsync().ConfigureAwait(false);
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
            await Task.Delay(2000).ConfigureAwait(false);
            return await OpenLiveviewStream(api, liveview).ConfigureAwait(false);
        }

        public static async Task<bool> TakePicture(DeviceApiHolder api, Geoposition position)
        {
            return await TakePicture(api, position, false).ConfigureAwait(false);
        }

        private static async Task<bool> TakePicture(DeviceApiHolder api, Geoposition position, bool awaiting = false)
        {
            DebugUtil.Log("Taking picture sequence");
            try
            {
                var urls = awaiting //
                    ? await api.Camera.AwaitTakePictureAsync().ConfigureAwait(false) //
                    : await api.Camera.ActTakePictureAsync().ConfigureAwait(false);
                DebugUtil.Log("Success taking picture");

                if (ApplicationSettings.GetInstance().IsPostviewTransferEnabled)
                {
                    foreach (var url in urls)
                    {
                        try
                        {
                            var uri = new Uri(url);
                            MediaDownloader.Instance.EnqueuePostViewImage(uri, position);
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
            return await TakePicture(api, position, true).ConfigureAwait(false);
        }

        public static async Task StopContinuousShooting(DeviceApiHolder api)
        {
            int retry = 5;
            while (retry-- > 0)
            {
                try
                {
                    await api.Camera.StopContShootingAsync();
                    break;
                }
                catch (RemoteApiException) { }
                DebugUtil.Log("failed to stop cont shooting. retry count: " + retry);
                await Task.Delay(TimeSpan.FromMilliseconds(200));
            }
        }
    }
}
