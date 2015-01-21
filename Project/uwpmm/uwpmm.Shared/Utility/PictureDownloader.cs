using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Threading.Tasks;
using Windows.Devices.Geolocation;
using Windows.Storage;
using Windows.UI.Core;

namespace Kazyx.Uwpmm.Utility
{
    public class PictureDownloader
    {
        private PictureDownloader() { }

        private const string DIRECTORY_NAME = "uwpmm";

        private const int BUFFER_SIZE = 2048;

        private static readonly HttpClient HttpClient = new HttpClient();

        private static readonly PictureDownloader instance = new PictureDownloader();
        public static PictureDownloader Instance
        {
            get { return instance; }
        }

        public Action<StorageFolder, StorageFile, GeotaggingResult> Fetched;

        public Action<ImageFetchError, GeotaggingResult> Failed;

        protected void OnFetched(StorageFolder folder, StorageFile file, GeotaggingResult geotaggingResult)
        {
            DebugUtil.Log("PictureSyncManager: OnFetched");
            Fetched.Raise(folder, file, geotaggingResult);
        }

        protected void OnFailed(ImageFetchError error, GeotaggingResult geotaggingResult)
        {
            DebugUtil.Log("PictureSyncManager: OnFailed" + error);
            Failed.Raise(error, geotaggingResult);
        }

        public async void Enqueue(Uri uri, Geoposition position = null)
        {
            DebugUtil.Log("PictureDownloader: Enqueue " + uri.AbsolutePath);
            await SystemUtil.GetCurrentDispatcher().RunAsync(CoreDispatcherPriority.Low, () =>
            {
                var req = new DownloadRequest { Uri = uri, Completed = OnFetched, Error = OnFailed, GeoPosition = position };
                DebugUtil.Log("Enqueue " + uri.AbsoluteUri);
                DownloadQueue.Enqueue(req);
                QueueStatusUpdated.Raise(DownloadQueue.Count);
                ProcessQueueSequentially();
            });
        }

        private Task task;

        private readonly Queue<DownloadRequest> DownloadQueue = new Queue<DownloadRequest>();
        public Action<int> QueueStatusUpdated;

        private void ProcessQueueSequentially()
        {
            if (task == null)
            {
                DebugUtil.Log("Create new task");
                task = Task.Factory.StartNew(async () =>
                {
                    while (DownloadQueue.Count != 0)
                    {
                        DebugUtil.Log("Dequeue - remaining " + DownloadQueue.Count);
                        await DownloadToSave(DownloadQueue.Dequeue());

                        QueueStatusUpdated.Raise(DownloadQueue.Count);
                    }
                    DebugUtil.Log("Queue end. Kill task");
                    task = null;
                });
            }
        }

        private async Task DownloadToSave(DownloadRequest req)
        {
            DebugUtil.Log("Download picture: " + req.Uri.OriginalString);
            try
            {
                GeotaggingResult GeotaggingResult = GeotaggingResult.NotRequested;

                var res = await HttpClient.GetAsync(req.Uri, HttpCompletionOption.ResponseContentRead).ConfigureAwait(false);
                switch (res.StatusCode)
                {
                    case HttpStatusCode.OK:
                        break;
                    case HttpStatusCode.Gone:
                        req.Error.Raise(ImageFetchError.Gone, GeotaggingResult);
                        return;
                    default:
                        req.Error.Raise(ImageFetchError.Network, GeotaggingResult);
                        return;
                }

                Stream imageStream = await res.Content.ReadAsStreamAsync().ConfigureAwait(false);
                if (req.GeoPosition != null)
                {
                    try
                    {
                        imageStream = await NtImageProcessor.MetaData.MetaDataOperator.AddGeopositionAsync(imageStream, req.GeoPosition, false);
                        GeotaggingResult = Utility.GeotaggingResult.OK;
                    }
                    catch (NtImageProcessor.MetaData.Misc.GpsInformationAlreadyExistsException)
                    {
                        GeotaggingResult = GeotaggingResult.GeotagAlreadyExists;
                    }
                    catch (Exception)
                    {
                        GeotaggingResult = GeotaggingResult.UnExpectedError;
                    }
                }

                using (imageStream)
                {
                    var library = KnownFolders.PicturesLibrary;

                    DebugUtil.Log("Create folder: " + DIRECTORY_NAME);
                    var folder = await library.CreateFolderAsync(DIRECTORY_NAME, CreationCollisionOption.OpenIfExists);

                    var filename = string.Format(DIRECTORY_NAME + "_{0:yyyyMMdd_HHmmss}.jpg", DateTime.Now);
                    DebugUtil.Log("Create file: " + filename);

                    var file = await folder.CreateFileAsync(filename, CreationCollisionOption.GenerateUniqueName);
                    using (var stream = await file.OpenAsync(FileAccessMode.ReadWrite))
                    {
                        var buffer = new byte[BUFFER_SIZE];
                        using (var os = stream.GetOutputStreamAt(0))
                        {
                            int read = 0;
                            while ((read = imageStream.Read(buffer, 0, BUFFER_SIZE)) != 0)
                            {
                                await os.WriteAsync(buffer.AsBuffer(0, read));
                            }
                        }
                    }
                    req.Completed.Raise(folder, file, GeotaggingResult);
                    return;
                }
            }
            catch (Exception e)
            {
                DebugUtil.Log(e.Message);
                DebugUtil.Log(e.StackTrace);
                req.Error.Raise(ImageFetchError.Unknown, GeotaggingResult.NotRequested); // TODO
            }
        }
    }

    public class DownloadRequest
    {
        public Uri Uri;
        public Geoposition GeoPosition;
        public Action<StorageFolder, StorageFile, GeotaggingResult> Completed;
        public Action<ImageFetchError, GeotaggingResult> Error;
    }

    public enum ImageFetchError
    {
        Network,
        Saving,
        Argument,
        DeviceInternal,
        Gone,
        Unknown,
        None,
    }

    public enum GeotaggingResult
    {
        OK,
        GeotagAlreadyExists,
        UnExpectedError,
        NotRequested,
    }
}
