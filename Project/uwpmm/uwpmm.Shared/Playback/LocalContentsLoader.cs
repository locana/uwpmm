using Kazyx.RemoteApi.AvContent;
using Kazyx.Uwpmm.DataModel;
using Kazyx.Uwpmm.Utility;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Windows.Storage;

namespace Kazyx.Uwpmm.Playback
{
    public class LocalContentsLoader : ContentsLoader
    {
        public event EventHandler<SingleContentEventArgs> SingleContentLoaded;

        protected void OnContentLoaded(Thumbnail file)
        {
            SingleContentLoaded.Raise(this, new SingleContentEventArgs { File = file });
        }

        public override async Task Load(CancellationTokenSource cancel)
        {
            var library = KnownFolders.PicturesLibrary;

            foreach (var folder in await library.GetFoldersAsync())
            {
                DebugUtil.Log("Load from local picture folder: " + folder.Name);
                await LoadContentsAsync(folder, cancel).ConfigureAwait(false);
                if (cancel != null && cancel.IsCancellationRequested)
                {
                    OnCancelled();
                    break;
                }
            }

            OnCompleted();
        }

        private async Task LoadContentsAsync(StorageFolder folder, CancellationTokenSource cancel)
        {
            var list = new List<StorageFile>();
            await LoadPicturesRecursively(list, folder, cancel).ConfigureAwait(false);

            if (cancel != null && cancel.IsCancellationRequested)
            {
                return;
            }

            var thumbs = new List<Thumbnail>();
            foreach (var file in list)
            {
                var content = new ContentInfo
                {
                    Protected = false,
                    ContentType = ContentKind.StillImage,
                    GroupName = folder.DisplayName,
                };
                var thumb = new Thumbnail(content, file);

                SingleContentLoaded.Raise(this, new SingleContentEventArgs { File = thumb });
                thumbs.Add(thumb);
            }

            OnPartLoaded(thumbs);
        }

        private async Task LoadPicturesRecursively(List<StorageFile> into, StorageFolder folder, CancellationTokenSource cancel)
        {
            var files = await folder.GetFilesAsync();

            if (cancel != null && cancel.IsCancellationRequested)
            {
                return;
            }

            foreach (var file in files)
            {
                switch (file.ContentType)
                {
                    case "image/jpeg":
                    case "image/png":
                    case "image/bmp":
                    case "image/gif":
                        into.Add(file);
                        break;
                    default:
                        break;
                }
            }

            foreach (var child in await folder.GetFoldersAsync())
            {
                if (cancel != null && cancel.IsCancellationRequested)
                {
                    return;
                }
                await LoadPicturesRecursively(into, child, cancel).ConfigureAwait(false);
            }
        }
    }

    public class SingleContentEventArgs : EventArgs
    {
        public Thumbnail File { set; get; }
    }
}
