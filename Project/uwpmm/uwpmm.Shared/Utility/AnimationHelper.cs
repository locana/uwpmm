using System;
using System.Collections.Generic;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Media.Animation;

namespace Kazyx.Uwpmm.Utility
{
    public class AnimationHelper
    {
        public static Storyboard CreateSlideAnimation(FrameworkElement target, FadeSide edge, FadeType fadetype, TimeSpan d, EventHandler<object> completed = null)
        {
            double distance = 0;
            var sb = new Storyboard();
            var slide = new DoubleAnimationUsingKeyFrames();
            var fade = new DoubleAnimationUsingKeyFrames();
            var transform = new TranslateTransform();
            var duration = d.Milliseconds;

            var KeyframeTimes = new List<double>() { 0, duration / 6, duration }; // 3 key frames.
            List<double> KeyframeDistance = new List<double>();
            List<double> KeyframeOpacity = new List<double>();

            Storyboard.SetTarget(slide, transform);
            Storyboard.SetTargetProperty(fade, "Opacity");
            Storyboard.SetTarget(fade, target);
            target.RenderTransform = transform;

            switch (fadetype)
            {
                case FadeType.FadeIn:
                    KeyframeOpacity = new List<double>() { 0, 0.8, 1.0 };

                    switch (edge)
                    {
                        case FadeSide.Top:
                            distance = -target.ActualHeight;
                            Storyboard.SetTargetProperty(slide, "Y");
                            KeyframeDistance = new List<double>() { distance, distance * 0.5, 0 };
                            break;
                        case FadeSide.Bottom:
                            distance = target.ActualHeight;
                            Storyboard.SetTargetProperty(slide, "Y");
                            KeyframeDistance = new List<double>() { distance, distance * 0.5, 0 };
                            break;
                    }
                    break;
                case FadeType.FadeOut:
                    KeyframeOpacity = new List<double>() { 1.0, 0.6, 0 };

                    switch (edge)
                    {
                        case FadeSide.Top:
                            distance = -target.ActualHeight;
                            Storyboard.SetTargetProperty(slide, "Y");
                            KeyframeDistance = new List<double>() { 0, distance * 0.2, distance };
                            break;
                        case FadeSide.Bottom:
                            distance = target.ActualHeight;
                            Storyboard.SetTargetProperty(slide, "Y");
                            KeyframeDistance = new List<double>() { 0, distance * 0.2, distance };
                            break;
                    }
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

    public enum FadeSide
    {
        Left,
        Top,
        Right,
        Bottom,
    }

    public enum FadeType
    {
        FadeIn,
        FadeOut,
    }
}
