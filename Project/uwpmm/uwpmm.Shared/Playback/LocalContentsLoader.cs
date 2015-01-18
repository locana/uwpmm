using Kazyx.RemoteApi.AvContent;
using Kazyx.Uwpmm.DataModel;
using Kazyx.Uwpmm.Utility;
using System;
using System.Collections.Generic;
using System.Linq;
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

        public override async Task Load(ContentsSet contentsSet, CancellationTokenSource cancel)
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

            var thumbs = list.Select(file =>
                {
                    var thumb = new Thumbnail(new ContentInfo { Protected = false, ContentType = ContentKind.StillImage, GroupName = folder.DisplayName, }, file);
                    SingleContentLoaded.Raise(this, new SingleContentEventArgs { File = thumb });
                    return thumb;
                }).ToList();

            OnPartLoaded(thumbs);
        }

        readonly string[] IMAGE_MIME_TYPES = { "image/jpeg", "image/png", "image/bmp", "image/gif" };

        private async Task LoadPicturesRecursively(List<StorageFile> into, StorageFolder folder, CancellationTokenSource cancel)
        {
            var files = await folder.GetFilesAsync();

            if (cancel != null && cancel.IsCancellationRequested)
            {
                return;
            }

            into.AddRange(files.Where(file => IMAGE_MIME_TYPES.Any(type => file.ContentType.Equals(type, StringComparison.OrdinalIgnoreCase))));

            foreach (var child in await folder.GetFoldersAsync())
            {
                if (cancel != null && cancel.IsCancellationRequested)
                {
                    return;
                }
                await LoadPicturesRecursively(into, child, cancel).ConfigureAwait(false);
            }
        }

        public override Task LoadRemainingAsync(RemainingContentsHolder holder, ContentsSet contentsSet, CancellationTokenSource cancel)
        {
            throw new NotImplementedException();
        }
    }

    public class SingleContentEventArgs : EventArgs
    {
        public Thumbnail File { set; get; }
    }
}
