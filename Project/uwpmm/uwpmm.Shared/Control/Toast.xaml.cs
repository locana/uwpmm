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
            CreateAnimation(ToastGrid.ActualHeight, Orientation.Down, SlideInAnimationCompleted).Begin();
        }

        async void SlideInAnimationCompleted(object sender, object e)
        {
            var height = ToastGrid.ActualHeight;
            await System.Threading.Tasks.Task.Delay(TimeSpan.FromMilliseconds(3000));

            CreateAnimation(height, Orientation.Up, (e_, sender_) =>
            {
                Contents.RemoveAt(0);
                Running = false;
                DequeueToast();
            }).Begin();
        }

        private Storyboard CreateAnimation(double height, Orientation orientation, EventHandler<object> completed = null)
        {
            var sb = new Storyboard();
            var slide = new DoubleAnimationUsingKeyFrames();
            var fade = new DoubleAnimationUsingKeyFrames();
            SlideTransform.Y = -height;

            if (orientation == Orientation.Down)
            {
                slide.KeyFrames.Add(new EasingDoubleKeyFrame() { KeyTime = KeyTime.FromTimeSpan(TimeSpan.FromMilliseconds(0)), Value = -height });
                slide.KeyFrames.Add(new EasingDoubleKeyFrame() { KeyTime = KeyTime.FromTimeSpan(TimeSpan.FromMilliseconds(20)), Value = -height * 0.2 });
                slide.KeyFrames.Add(new EasingDoubleKeyFrame() { KeyTime = KeyTime.FromTimeSpan(TimeSpan.FromMilliseconds(120)), Value = 0 });

                fade.KeyFrames.Add(new EasingDoubleKeyFrame() { KeyTime = KeyTime.FromTimeSpan(TimeSpan.FromMilliseconds(0)), Value = 0 });
                fade.KeyFrames.Add(new EasingDoubleKeyFrame() { KeyTime = KeyTime.FromTimeSpan(TimeSpan.FromMilliseconds(20)), Value = 0.8 });
                fade.KeyFrames.Add(new EasingDoubleKeyFrame() { KeyTime = KeyTime.FromTimeSpan(TimeSpan.FromMilliseconds(120)), Value = 1.0 });
            }
            else if (orientation == Orientation.Up)
            {
                slide.KeyFrames.Add(new EasingDoubleKeyFrame() { KeyTime = KeyTime.FromTimeSpan(TimeSpan.FromMilliseconds(0)), Value = 0 });
                slide.KeyFrames.Add(new EasingDoubleKeyFrame() { KeyTime = KeyTime.FromTimeSpan(TimeSpan.FromMilliseconds(20)), Value = -height * 0.5 });
                slide.KeyFrames.Add(new EasingDoubleKeyFrame() { KeyTime = KeyTime.FromTimeSpan(TimeSpan.FromMilliseconds(120)), Value = -height });

                fade.KeyFrames.Add(new EasingDoubleKeyFrame() { KeyTime = KeyTime.FromTimeSpan(TimeSpan.FromMilliseconds(0)), Value = 1.0 });
                fade.KeyFrames.Add(new EasingDoubleKeyFrame() { KeyTime = KeyTime.FromTimeSpan(TimeSpan.FromMilliseconds(20)), Value = 0.2 });
                fade.KeyFrames.Add(new EasingDoubleKeyFrame() { KeyTime = KeyTime.FromTimeSpan(TimeSpan.FromMilliseconds(120)), Value = 0 });
            }

            Storyboard.SetTargetProperty(slide, "Y");
            Storyboard.SetTarget(slide, SlideTransform);

            Storyboard.SetTargetProperty(fade, "Opacity");
            Storyboard.SetTarget(fade, ToastGrid);

            sb.Children.Add(slide);
            sb.Children.Add(fade);
            if (completed != null) { sb.Completed += completed; }
            return sb;
        }

        enum Orientation
        {
            Up,
            Down,
        }
    }

    public class ToastContent
    {
        public ToastContent() { }
        public String Text { get; set; }
        public BitmapImage Icon { get; set; }
    }
}
