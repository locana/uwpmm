using Kazyx.Uwpmm.DataModel;
using System;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using Windows.Storage;

namespace Kazyx.Uwpmm.Utility
{
    public class ThumbnailCacheLoader
    {
        private const string CACHE_ROOT = "thumb_cache/";

        private readonly HttpClient HttpClient;

        private ThumbnailCacheLoader()
        {
            CreateCacheRoot();
            HttpClient = new HttpClient();
        }

        private async void CreateCacheRoot()
        {
            var root = ApplicationData.Current.TemporaryFolder;
            await root.CreateFolderAsync(CACHE_ROOT, CreationCollisionOption.OpenIfExists);
        }

        private static readonly ThumbnailCacheLoader instance = new ThumbnailCacheLoader();

        public static ThumbnailCacheLoader INSTANCE
        {
            get { return instance; }
        }

        private const int THUMBNAIL_SIZE = 240;

        /// <summary>
        /// 
        /// </summary>
        /// <param name="uuid">Specify uuid to delete directory for the device, otherwise delete all of stored cache.</param>
        public async Task DeleteCache(string uuid = null)
        {
            var root = ApplicationData.Current.TemporaryFolder;
            if (uuid == null)
            {
                DebugUtil.Log("Delete all of thumbnail cache.");

                var cacheRoot = await root.GetFolderAsync(CACHE_ROOT);
                await cacheRoot.DeleteDirectoryRecursiveAsync(false);
            }
            else
            {
                DebugUtil.Log("Delete thumbnail cache of " + uuid);

                var uuidRoot = await root.GetFolderAsync(CACHE_ROOT + uuid.Replace(":", "-"));
                await uuidRoot.DeleteDirectoryRecursiveAsync();
            }
        }

        /// <summary>
        /// Asynchronously download thumbnail image and return local storage file.
        /// </summary>
        /// <param name="uuid">UUID of the target device.</param>
        /// <param name="content">Source of thumbnail image.</param>
        /// <returns>Local storage file</returns>
        public async Task<StorageFile> LoadCacheFileAsync(string uuid, ContentInfo content)
        {
            var uri = new Uri(content.ThumbnailUrl);
            var directory = CACHE_ROOT + uuid.Replace(":", "-") + "/";
            var filename = content.CreatedTime.Replace(":", "-").Replace("/", "-") + "--" + Path.GetFileName(uri.LocalPath);

            var root = ApplicationData.Current.TemporaryFolder;
            var folder = await root.CreateFolderAsync(directory, CreationCollisionOption.OpenIfExists);
            var file = await folder.GetFileAsync(filename);

            if (file != null)
            {
                return file;
            }

            var res = await HttpClient.GetAsync(uri, HttpCompletionOption.ResponseContentRead);
            if (res.StatusCode != HttpStatusCode.OK)
            {
                return null;
            }
            using (var stream = await res.Content.ReadAsStreamAsync())
            {
                var dst = await folder.CreateFileAsync(filename, CreationCollisionOption.OpenIfExists);
                using (var outStream = await dst.OpenStreamForWriteAsync())
                {
                    await stream.CopyToAsync(outStream);
                }
                return dst;
            }
        }
    }
}
