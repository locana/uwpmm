using Kazyx.RemoteApi.AvContent;
using Kazyx.Uwpmm.DataModel;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Windows.Storage;

namespace Kazyx.Uwpmm.Utility
{
    public class LocalContentsLoader
    {
        public event EventHandler<FolderContentsEventArgs> FolderContentsLoaded;

        protected void OnFolderLoaded(List<Thumbnail> files)
        {
            FolderContentsLoaded.Raise(this, new FolderContentsEventArgs { Files = files });
        }

        public event EventHandler<SingleContentEventArgs> SingleContentLoaded;

        protected void OnContentLoaded(Thumbnail file)
        {
            SingleContentLoaded.Raise(this, new SingleContentEventArgs { File = file });
        }

        private async Task LoadPicturesRecursively(List<StorageFile> into, StorageFolder folder)
        {
            var files = await folder.GetFilesAsync();

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
                await LoadPicturesRecursively(into, child);
            }
        }

        public async Task LoadContentsAsync(StorageFolder folder)
        {
            var list = new List<StorageFile>();
            await LoadPicturesRecursively(list, folder);

            var thumbs = new List<Thumbnail>();
            foreach (var file in list)
            {
                var content = new ContentInfo
                {
                    Protected = false,
                    ContentType = ContentKind.StillImage,
                };
                var thumb = new Thumbnail(folder.DisplayName, file, content);
                OnContentLoaded(thumb);
                thumbs.Add(thumb);
            }

            OnFolderLoaded(thumbs);
        }
    }

    public class SingleContentEventArgs : EventArgs
    {
        public Thumbnail File { set; get; }
    }

    public class FolderContentsEventArgs : EventArgs
    {
        public List<Thumbnail> Files { set; get; }
    }
}
