using Kazyx.Uwpmm.Utility;
using System;
using System.Collections.Generic;
using System.Linq;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Media.Animation;
using Windows.UI.Xaml.Media.Imaging;

// The User Control item template is documented at http://go.microsoft.com/fwlink/?LinkId=234236

namespace Kazyx.Uwpmm.Control
{
    public sealed partial class Toast : UserControl
    {
        public Toast()
        {
            this.InitializeComponent();
        }

        private List<ToastContent> Contents = new List<ToastContent>();
        private bool Running = false;

        public void PushToast(ToastContent content)
        {
            DebugUtil.Log("Enqueue toast: " + content.Text);
            Contents.Add(content);
            if (!Running) { DequeueToast(); }
        }

        void DequeueToast()
        {
            if (Contents.Count == 0) { return; }
            ToastGrid.DataContext = Contents.ElementAt(0);
            DebugUtil.Log("Dequeue toast:" + Contents.ElementAt(0).Text);
            Running = true;
            AnimationHelper.CreateSlideInAnimation(ToastGrid, AnimationOrientation.Down, TimeSpan.FromMilliseconds(120), SlideInAnimationCompleted).Begin();
        }

        async void SlideInAnimationCompleted(object sender, object e)
        {
            await System.Threading.Tasks.Task.Delay(TimeSpan.FromMilliseconds(3000));

            AnimationHelper.CreateSlideInAnimation(ToastGrid, AnimationOrientation.Up, TimeSpan.FromMilliseconds(120), (e_, sender_) =>
            {
                Contents.RemoveAt(0);
                Running = false;
                DequeueToast();
            }).Begin();
        }
    }

    public class ToastContent
    {
        public ToastContent() { }
        public String Text { get; set; }
        public BitmapImage Icon { get; set; }
    }
}
