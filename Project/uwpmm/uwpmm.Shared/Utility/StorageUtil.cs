using System;
using System.Threading.Tasks;
using Windows.Storage;

namespace Kazyx.Uwpmm.Utility
{
    public class StorageUtil
    {
        private StorageUtil() { }

        /// <summary>
        /// Delete children items of the directory recursively.
        /// </summary>
        /// <param name="root"></param>
        /// <param name="includeThis">Delete this folder itself or not.</param>
        /// <returns></returns>
        public static async Task DeleteDirectoryRecursiveAsync(StorageFolder root, bool includeThis = true)
        {
            if (root == null) { return; }

            foreach (var file in await root.GetFilesAsync())
            {
                await file.DeleteAsync(StorageDeleteOption.PermanentDelete);
            }
            foreach (var child in await root.GetFoldersAsync())
            {
                await DeleteDirectoryRecursiveAsync(child);
            }
            if (includeThis)
            {
                await root.DeleteAsync(StorageDeleteOption.PermanentDelete);
            }
        }

        /// <summary>
        /// Get sub-directory async. If it does not exist, create new and return it.
        /// </summary>
        /// <param name="parent">Parent directory</param>
        /// <param name="path">Sub directory name</param>
        /// <returns>Requested directory</returns>
        public static async Task<StorageFolder> GetOrCreateDirectoryAsync(StorageFolder parent, string path)
        {
            var dir = await parent.GetFolderAsync(path);
            if (dir == null)
            {
                dir = await parent.CreateFolderAsync(path);
            }
            return dir;
        }

        public static async Task<StorageFile> GetOrCreateFileAsync(StorageFolder parent, string path)
        {
            var file = await parent.GetFileAsync(path);
            if (file == null)
            {
                file = await parent.CreateFileAsync(path);
            }
            return file;
        }
    }
}
