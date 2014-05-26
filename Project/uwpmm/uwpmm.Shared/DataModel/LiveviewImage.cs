using Windows.UI.Core;
using Windows.UI.Xaml.Media.Imaging;

namespace Kazyx.Uwpmm.DataModel
{
    public class LiveviewImage : ObservableBase
    {
        private BitmapImage _Image = null;
        public BitmapImage Image
        {
            set
            {
                _Image = value;
                //NotifyChangedOnUI("Image", CoreDispatcherPriority.High);
                NotifyChanged("Image");
            }
            get
            {
                return _Image;
            }
        }
    }
}
