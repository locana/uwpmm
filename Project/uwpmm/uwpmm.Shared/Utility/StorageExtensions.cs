using System;
using System.Threading.Tasks;
using Windows.Storage;

namespace Kazyx.Uwpmm.Utility
{
    public static class StorageExtensions
    {
        /// <summary>
        /// Delete children items of the directory recursively.
        /// </summary>
        /// <param name="root"></param>
        /// <param name="includeThis">Delete this folder itself or not.</param>
        /// <returns></returns>
        public static async Task DeleteDirectoryRecursiveAsync(this StorageFolder root, bool includeThis = true)
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
    }
}
