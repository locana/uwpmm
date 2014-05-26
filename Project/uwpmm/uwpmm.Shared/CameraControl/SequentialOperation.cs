using Kazyx.Liveview;
using Kazyx.RemoteApi;
using Kazyx.Uwpmm.Utility;
using System;
using System.Diagnostics;
using System.Threading.Tasks;

namespace Kazyx.Uwpmm.CameraControl
{
    public class SequentialOperation
    {
        public static async Task<bool> SetUp(DeviceApiHolder api, LvStreamProcessor liveview)
        {
            Debug.WriteLine("Set up control");
            try
            {
                await api.RetrieveApiList();
                var info = await api.Camera.GetApplicationInfoAsync();
                api.Capability.Version = new ServerVersion(info.version);
                api.Capability.AvailableApis = await api.Camera.GetAvailableApiListAsync();

                if (api.Capability.IsSupported("startRecMode"))
                {
                    await api.Camera.StartRecModeAsync();
                }
                if (api.Capability.IsAvailable("startLiveview"))
                {
                    var res = await OpenLiveviewStream(api, liveview);
                    if (!res)
                    {
                        Debug.WriteLine("Failed to open liveview connection.");
                        return false;
                    }
                }

                if (api.Capability.IsSupported("setCurrentTime"))
                {
                    await api.System.SetCurrentTimeAsync(DateTimeOffset.Now);
                }
            }
            catch (RemoteApiException e)
            {
                Debug.WriteLine("Failed setup: " + e.code);
                return false;
            }

            return true;
        }

        public static async Task<bool> OpenLiveviewStream(DeviceApiHolder api, LvStreamProcessor liveview)
        {
            Debug.WriteLine("Open liveview stream");
            try
            {
                var url = await api.Camera.StartLiveviewAsync();
                return await liveview.OpenConnection(url);
            }
            catch (RemoteApiException e)
            {
                Debug.WriteLine("Failed to startLiveview: " + e.code);
                return false;
            }
        }

        public static async Task<bool> CloseLiveviewStream(DeviceApiHolder api, LvStreamProcessor liveview)
        {
            Debug.WriteLine("Close liveview stream");
            try
            {
                liveview.CloseConnection();
                await api.Camera.StopLiveviewAsync();
                return true;
            }
            catch (RemoteApiException e)
            {
                Debug.WriteLine("Failed to stopLiveview: " + e.code);
                return false;
            }
        }

        public static async Task<bool> ReOpenLiveviewStream(DeviceApiHolder api, LvStreamProcessor liveview)
        {
            Debug.WriteLine("Reopen liveview stream");
            liveview.CloseConnection();
            await Task.Delay(2000);
            return await OpenLiveviewStream(api, liveview);
        }

        public static async Task<bool> TakePicture(DeviceApiHolder api, bool awaiting = false)
        {
            Debug.WriteLine("Taking picture sequence");
            try
            {
                var urls = awaiting ? await api.Camera.AwaitTakePictureAsync() : await api.Camera.ActTakePictureAsync();
                Debug.WriteLine("Success taking picture");

                if (App.Settings.GetIsDownloadPostviewEnabled())
                {
                    foreach (var url in urls)
                    {
                        try
                        {
                            var uri = new Uri(url);
                            return await PictureDownloader.DownloadToSave(uri);
                        }
                        catch (Exception e)
                        {
                            Debug.WriteLine(e.Message);
                            Debug.WriteLine(e.StackTrace);
                            return false;
                        }
                    }
                    return false;
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
                    Debug.WriteLine("Failed to take picture: " + e.code);
                    throw e;
                }
            }
            Debug.WriteLine("Take picture timeout: await for completion");
            return await TakePicture(api, true);
        }
    }
}
