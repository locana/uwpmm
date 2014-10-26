using Kazyx.Uwpmm.DataModel;
using System;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Threading.Tasks;
using Windows.Storage.Streams;
using Windows.UI.Core;
using Windows.UI.Xaml.Media.Imaging;

namespace Kazyx.Uwpmm.Utility
{
    public class LiveviewUtil
    {
        public static async Task SetAsBitmap(byte[] data, ImageDataSource target, CoreDispatcher Dispatcher = null)
        {
            var stream = new InMemoryRandomAccessStream();
            await stream.WriteAsync(data.AsBuffer());
            stream.Seek(0);
            if (Dispatcher == null)
            {
                Dispatcher = SystemUtil.GetCurrentDispatcher();
            }
            if (Dispatcher == null)
            {
                return;
            }

            await Dispatcher.RunAsync(CoreDispatcherPriority.High, () =>
            {
                var image = new BitmapImage();
                image.SetSource(stream);
                target.Image = image;
                stream.Dispose();
            });
        }
    }
}
