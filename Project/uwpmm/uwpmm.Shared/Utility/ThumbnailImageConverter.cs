using Kazyx.Uwpmm.DataModel;
using System;
using System.IO;
using System.Threading.Tasks;
using Windows.Storage;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Media.Imaging;

namespace Kazyx.Uwpmm.Utility
{
    public class ThumbnailImageConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, string language)
        {
            var bmp = new BitmapImage();

            var task = Task.Run(async () =>
            {
                var path = value as string;
                if (string.IsNullOrEmpty(path))
                {
                    return null;
                }
                var root = ApplicationData.Current.TemporaryFolder;

                try
                {
                    var file = await root.GetFileAsync(path);
                    var stream = await file.OpenStreamForReadAsync();
                    await bmp.SetSourceAsync(stream.AsRandomAccessStream());
                    return bmp;
                }
                catch
                {
                    return null;
                }
            });

            return new AsyncConversionSource<BitmapImage>(task);
        }

        public object ConvertBack(object value, Type targetType, object parameter, string language)
        {
            throw new NotImplementedException("ConvertBack is not implemented.");
        }
    }
}
