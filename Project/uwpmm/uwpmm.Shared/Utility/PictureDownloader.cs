using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Net;
using System.Net.Http;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Threading.Tasks;
using Windows.Storage;
using Windows.UI.Core;

namespace Kazyx.Uwpmm.Utility
{
    public class PictureDownloader
    {
        private const string DIRECTORY_NAME = "uwpmm";

        private const int BUFFER_SIZE = 2048;

        private static readonly HttpClient HttpClient = new HttpClient();

        private static readonly PictureDownloader instance = new PictureDownloader();
        public static PictureDownloader Instance
        {
            get { return instance; }
        }

        public Action<StorageFile> Fetched;

        public Action<ImageFetchError> Failed;

        protected void OnFetched(StorageFile file)
        {
            DebugUtil.Log("PictureSyncManager: OnFetched");
            if (Fetched != null)
            {
                Fetched(file);
            }
        }

        protected void OnFailed(ImageFetchError error)
        {
            DebugUtil.Log("PictureSyncManager: OnFailed" + error);
            if (Failed != null)
            {
                Failed(error);
            }
        }

        public async void Enqueue(Uri uri)
        {
            DebugUtil.Log("PictureDownloader: Enqueue " + uri.AbsolutePath);
            await SystemUtil.GetCurrentDispatcher().RunAsync(CoreDispatcherPriority.Low, () =>
            {
                var req = new DownloadRequest { Uri = uri, Completed = OnFetched, Error = OnFailed };
                DebugUtil.Log("Enqueue " + uri.AbsoluteUri);
                DownloadQueue.Enqueue(req);
                if (QueueStatusUpdated != null)
                {
                    QueueStatusUpdated(DownloadQueue.Count);
                }
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

                        if (QueueStatusUpdated != null)
                        {
                            QueueStatusUpdated(DownloadQueue.Count);
                        }
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
                var res = await HttpClient.GetAsync(req.Uri, HttpCompletionOption.ResponseContentRead);
                switch (res.StatusCode)
                {
                    case HttpStatusCode.OK:
                        break;
                    case HttpStatusCode.Gone:
                        req.Error(ImageFetchError.Gone);
                        return;
                    default:
                        req.Error(ImageFetchError.Network);
                        return;
                }

                using (var resStream = await res.Content.ReadAsStreamAsync())
                {
                    var library = KnownFolders.PicturesLibrary;
                    StorageFolder folder = null;
                    folder = await library.GetFolderAsync(DIRECTORY_NAME);
                    if (folder == null)
                    {
                        DebugUtil.Log("Create folder: " + DIRECTORY_NAME);
                        folder = await library.CreateFolderAsync(DIRECTORY_NAME);
                    }

                    var filename = string.Format(DIRECTORY_NAME + "_{0:yyyyMMdd_HHmmss}.jpg", DateTime.Now);
                    DebugUtil.Log("Create file: " + filename);

                    var file = await folder.CreateFileAsync(filename, CreationCollisionOption.GenerateUniqueName);
                    using (var stream = await file.OpenAsync(FileAccessMode.ReadWrite))
                    {
                        var buffer = new byte[BUFFER_SIZE];
                        using (var os = stream.GetOutputStreamAt(0))
                        {
                            int read = 0;
                            while ((read = resStream.Read(buffer, 0, BUFFER_SIZE)) != 0)
                            {
                                await os.WriteAsync(buffer.AsBuffer(0, read));
                            }
                        }
                    }
                    req.Completed(file);
                    return;
                }
            }
            catch (Exception e)
            {
                DebugUtil.Log(e.Message);
                DebugUtil.Log(e.StackTrace);
                req.Error(ImageFetchError.Unknown); // TODO
            }
        }
    }

    public class DownloadRequest
    {
        public Uri Uri;
        // public Geoposition GeoPosition;
        public Action<StorageFile> Completed;
        public Action<ImageFetchError> Error;
    }

    public enum ImageFetchError
    {
        Network,
        Saving,
        Argument,
        DeviceInternal,
        GeotagAlreadyExists,
        GeotagAddition,
        Gone,
        Unknown,
        None,
    }
}
