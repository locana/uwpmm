#if DEBUG
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using Windows.Storage;
#endif

namespace Kazyx.Uwpmm.Utility
{
    public class DebugUtil
    {
#if DEBUG
        private static StringBuilder LogBuilder = new StringBuilder();

        private const string LOG_ROOT = "/log_store/";

        private const string LOG_EXTENSION = ".txt";

        private const int MaxLength = 48 * 1024; // Byte

        private static readonly object Lock = new object();
#endif

        /// <summary>
        /// Show given string on Debug log and keep to local instance.
        /// </summary>
        /// <param name="s">Log mesage</param>
        public static void Log(string s)
        {
#if DEBUG
            lock (Lock)
            {
                Debug.WriteLine(s);
                LogBuilder.Append(s);
                LogBuilder.Append("\n");
                if (LogBuilder.Length > MaxLength)
                {
                    Flush();
                }
            }
#endif
        }

#if DEBUG
        public static async void Flush(bool crash = false)
        {
            var root = ApplicationData.Current.TemporaryFolder;
            var folder = await StorageUtil.GetOrCreateDirectoryAsync(root, LOG_ROOT);
            var time = DateTimeOffset.Now.ToLocalTime().ToString("yyyyMMdd-HHmmss");
            var filename = time + (crash ? "_crash" : "") + LOG_EXTENSION;
            var file = await StorageUtil.GetOrCreateFileAsync(folder, filename);

            Debug.WriteLine("\n\nFlush log file: {0}\n\n", filename);

            using (var str = await file.OpenStreamForWriteAsync())
            {
                using (var writer = new StreamWriter(str))
                {
                    writer.Write(LogBuilder.ToString());
                }
            }
            LogBuilder.Clear();
        }

        public static async Task<List<string>> LogFiles()
        {
            var root = ApplicationData.Current.TemporaryFolder;
            var folder = await StorageUtil.GetOrCreateDirectoryAsync(root, LOG_ROOT);

            var list = new List<string>();

            var files = await folder.GetFilesAsync();
            if (files != null)
            {
                foreach (var file in files)
                {
                    list.Add(file.Name);
                }
            }
            return list;
        }

        public static async Task<string> GetFile(string filename)
        {
            var root = ApplicationData.Current.TemporaryFolder;
            var folder = await StorageUtil.GetOrCreateDirectoryAsync(root, LOG_ROOT);

            var file = await folder.GetFileAsync(filename);
            if (file == null)
            {
                return "";
            }

            using (var stream = await file.OpenStreamForReadAsync())
            {
                using (var reader = new StreamReader(stream))
                {
                    return await reader.ReadToEndAsync();
                }
            }
        }
#endif
    }
}
