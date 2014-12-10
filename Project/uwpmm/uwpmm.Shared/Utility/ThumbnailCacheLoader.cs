using Kazyx.Uwpmm.DataModel;
using System;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Threading.Tasks;
using Windows.Graphics.Imaging;
using Windows.Storage;
using Windows.UI.Core;
using Windows.UI.Xaml.Media.Imaging;

namespace Kazyx.Uwpmm.Utility
{
    public class ThumbnailCacheLoader
    {
        private const string CACHE_ROOT = "thumb_cache/";
        private const string CACHE_ROOT_TMP = "tmp/thumb_cache/";

        private readonly HttpClient HttpClient;

        private ThumbnailCacheLoader()
        {
            CreateCacheRoot();
            HttpClient = new HttpClient();
        }

        private async void CreateCacheRoot()
        {
            var root = ApplicationData.Current.TemporaryFolder;
            await StorageUtil.GetOrCreateDirectoryAsync(root, CACHE_ROOT);
            await StorageUtil.GetOrCreateDirectoryAsync(root, CACHE_ROOT_TMP);
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
                await StorageUtil.DeleteDirectoryRecursiveAsync(cacheRoot, false);

                var tmpCacheRoot = await root.GetFolderAsync(CACHE_ROOT_TMP);
                await StorageUtil.DeleteDirectoryRecursiveAsync(tmpCacheRoot, false);
            }
            else
            {
                DebugUtil.Log("Delete thumbnail cache of " + uuid);

                var uuidRoot = await root.GetFolderAsync(CACHE_ROOT + uuid.Replace(":", "-"));
                await StorageUtil.DeleteDirectoryRecursiveAsync(uuidRoot);

                var uuidTmpRoot = await root.GetFolderAsync(CACHE_ROOT_TMP + uuid.Replace(":", "-"));
                await StorageUtil.DeleteDirectoryRecursiveAsync(uuidTmpRoot);
            }
        }

        /// <summary>
        /// Asynchronously download thumbnail image and return local cache path.
        /// </summary>
        /// <param name="uuid">UUID of the target device.</param>
        /// <param name="content">Source of thumbnail image.</param>
        /// <returns>Path to local thumbnail cache.</returns>
        public async Task<string> GetCachePathAsync(string uuid, ContentInfo content)
        {
            var uri = new Uri(content.ThumbnailUrl);
            var directory = CACHE_ROOT + uuid.Replace(":", "-") + "/";
            var directory_tmp = CACHE_ROOT_TMP + uuid.Replace(":", "-") + "/";
            var filename = content.CreatedTime.Replace(":", "-").Replace("/", "-") + "--" + Path.GetFileName(uri.LocalPath);
            var filepath = directory + filename;
            var filepath_tmp = directory_tmp + filename;

            var folder = ApplicationData.Current.TemporaryFolder;
            await StorageUtil.GetOrCreateDirectoryAsync(folder, directory);
            await StorageUtil.GetOrCreateDirectoryAsync(folder, directory_tmp);

            if (await folder.GetFileAsync(filepath) != null)
            {
                return filepath;
            }
            if (await folder.GetFileAsync(filepath_tmp) != null)
            {
                return await GetResizedCachePathAsync(filepath_tmp, filepath);
            }

            var res = await HttpClient.GetAsync(uri, HttpCompletionOption.ResponseContentRead);
            if (res.StatusCode != HttpStatusCode.OK)
            {
                return null;
            }
            using (var stream = await res.Content.ReadAsStreamAsync())
            {
                if (await folder.GetFileAsync(filepath_tmp) == null)
                {
                    var dst = await folder.CreateFileAsync(filepath_tmp);
                    var outStream = await dst.OpenStreamForWriteAsync();
                    await stream.CopyToAsync(outStream);
                }
                return await GetResizedCachePathAsync(filepath_tmp, filepath);
            }
        }

        private async Task<string> GetResizedCachePathAsync(string path, string resizedPath)
        {
            var tcs = new TaskCompletionSource<string>();

            var dispatcher = SystemUtil.GetCurrentDispatcher();
            if (dispatcher == null) { throw new InvalidOperationException("Failed to obtain dispatcher"); }

            await dispatcher.RunAsync(CoreDispatcherPriority.Low, async () =>
            {
                try
                {
                    var root = ApplicationData.Current.TemporaryFolder;
                    if (await root.GetFileAsync(resizedPath) != null)
                    {
                        tcs.TrySetResult(resizedPath);
                        return;
                    }

                    var src = await StorageUtil.GetOrCreateFileAsync(root, path);
                    var file = await StorageUtil.GetOrCreateFileAsync(root, resizedPath);

                    using (var srcStream = await src.OpenStreamForReadAsync())
                    {
                        var original = new BitmapImage();
                        original.CreateOptions = BitmapCreateOptions.None;
                        var rndStream = srcStream.AsRandomAccessStream();
                        rndStream.Seek(0);
                        original.SetSource(rndStream);
                        var max = Math.Max(original.PixelHeight, original.PixelWidth);
                        var scale = (float)THUMBNAIL_SIZE / (float)max;
                        var wbmp = new WriteableBitmap((int)(original.PixelWidth * scale), (int)(original.PixelHeight * scale));
                        rndStream.Seek(0);
                        wbmp.SetSource(rndStream);
                        byte[] pixels = null;
                        using (var pixelStream = wbmp.PixelBuffer.AsStream())
                        {
                            pixels = new byte[(uint)pixelStream.Length];
                            await pixelStream.ReadAsync(pixels, 0, pixels.Length);
                        }
                        using (var stream = await file.OpenStreamForWriteAsync())
                        {
                            var enc = await BitmapEncoder.CreateAsync(BitmapEncoder.JpegEncoderId, stream.AsRandomAccessStream());
                            enc.SetPixelData(BitmapPixelFormat.Bgra8, BitmapAlphaMode.Ignore, (uint)wbmp.PixelWidth, (uint)wbmp.PixelHeight, 96, 96, pixels);
                            await enc.FlushAsync();
                            DebugUtil.Log("New thumbnail cache: " + resizedPath);
                            tcs.TrySetResult(resizedPath);
                        }
                    }
                }
                catch (Exception e)
                {
                    DebugUtil.Log(e.StackTrace);
                    tcs.TrySetException(e);
                }
            });

            return await tcs.Task;
        }
    }
}
