using System;
using System.Collections.Generic;
using System.Text;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Media.Animation;

namespace Kazyx.Uwpmm.Utility
{
    public class AnimationHelper
    {
        public static Storyboard CreateSlideInAnimation(FrameworkElement target, AnimationOrientation orientation, TimeSpan d, EventHandler<object> completed = null)
        {
            double distance = 0;
            var sb = new Storyboard();
            var slide = new DoubleAnimationUsingKeyFrames();
            var fade = new DoubleAnimationUsingKeyFrames();
            var transform = new TranslateTransform();
            var duration = d.Milliseconds;

            Storyboard.SetTarget(slide, transform);
            Storyboard.SetTargetProperty(fade, "Opacity");
            Storyboard.SetTarget(fade, target);
            target.RenderTransform = transform;

            switch (orientation)
            {
                case AnimationOrientation.Up:
                case AnimationOrientation.Down:
                    distance = -target.ActualHeight;
                    transform.Y = distance;
                    Storyboard.SetTargetProperty(slide, "Y");
                    break;
                case AnimationOrientation.Left:
                case AnimationOrientation.Right:
                    distance = -target.ActualWidth;
                    transform.X = distance;
                    Storyboard.SetTargetProperty(slide, "X");
                    break;
            }

            var KeyframeTimes = new List<double>() { 0, duration / 6, duration }; // 3 key frames.
            List<double> KeyframeDistance = new List<double>();
            List<double> KeyframeOpacity = new List<double>();

            switch (orientation)
            {
                case AnimationOrientation.Down:
                    KeyframeDistance = new List<double>() { distance, distance * 0.2, 0 };
                    KeyframeOpacity = new List<double>() { 0, 0.8, 1.0 };
                    break;
                case AnimationOrientation.Up:
                    KeyframeDistance = new List<double>() { 0, distance * 0.5, distance };
                    KeyframeOpacity = new List<double>() { 1.0, 0.6, 0 };
                    break;
                case AnimationOrientation.Left:
                    // todo: tune-up
                    KeyframeDistance = new List<double>() { 0, distance * 0.5, distance };
                    KeyframeOpacity = new List<double>() { 1.0, 0.6, 0 };
                    break;
                case AnimationOrientation.Right:
                    // todo
                    KeyframeDistance = new List<double>() { distance, distance * 0.2, 0 };
                    KeyframeOpacity = new List<double>() { 0, 0.8, 1.0 };
                    break;
            }

            for (int i = 0; i < KeyframeTimes.Count; i++)
            {
                slide.KeyFrames.Add(new EasingDoubleKeyFrame() { KeyTime = KeyTime.FromTimeSpan(TimeSpan.FromMilliseconds(KeyframeTimes[i])), Value = KeyframeDistance[i] });
                fade.KeyFrames.Add(new EasingDoubleKeyFrame() { KeyTime = KeyTime.FromTimeSpan(TimeSpan.FromMilliseconds(KeyframeTimes[i])), Value = KeyframeOpacity[i] });
            }

            sb.Children.Add(slide);
            sb.Children.Add(fade);
            if (completed != null) { sb.Completed += completed; }
            return sb;
        }
    }

    public enum AnimationOrientation
    {
        Up,
        Down,
        Left,
        Right,
    }
}
