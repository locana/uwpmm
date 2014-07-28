using Kazyx.Liveview;
using Kazyx.RemoteApi;
using Kazyx.Uwpmm.Utility;
using System;
using System.Diagnostics;
using System.Threading.Tasks;
using Windows.Storage;

namespace Kazyx.Uwpmm.CameraControl
{
    public class SequentialOperation
    {
        public static async Task<TargetDevice> SetUp(DeviceApiHolder api, LvStreamProcessor liveview)
        {
            Debug.WriteLine("Set up control");
            try
            {
                await api.RetrieveApiList();
                var info = await api.Camera.GetApplicationInfoAsync();
                api.Capability.Version = new ServerVersion(info.Version);
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
                        throw new Exception();
                    }
                }

                if (api.Capability.IsSupported("setCurrentTime"))
                {
                    await api.System.SetCurrentTimeAsync(DateTimeOffset.Now);
                }
                var target = new TargetDevice(api);
                await target.Observer.Start();
                return target;
            }
            catch (RemoteApiException e)
            {
                Debug.WriteLine("Failed setup: " + e.code);
                throw e;
            }
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

        public static async Task<bool> TakePicture(DeviceApiHolder api, Action<StorageFile> ImageSaved, bool awaiting = false)
        {
            Debug.WriteLine("Taking picture sequence");
            try
            {
                var urls = awaiting ? await api.Camera.AwaitTakePictureAsync() : await api.Camera.ActTakePictureAsync();
                Debug.WriteLine("Success taking picture");

                if (ImageSaved == null)
                {
                    return true;
                }

                if (App.Settings.PostviewSyncEnabled)
                {
                    foreach (var url in urls)
                    {
                        try
                        {
                            var uri = new Uri(url);
                            var file = await PictureDownloader.DownloadToSave(uri);
                            ImageSaved.Invoke(file);
                            return true;
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
            return await TakePicture(api, ImageSaved, true);
        }
    }
}
