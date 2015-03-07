using System;
using System.Collections.Generic;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Media.Animation;

namespace Kazyx.Uwpmm.Utility
{
    public class AnimationHelper
    {
        public static Storyboard CreateSlideAnimation(AnimationRequest request, FadeSide edge, FadeType fadetype)
        {
            double distance = 0;
            var sb = new Storyboard();
            var slide = new DoubleAnimationUsingKeyFrames();
            var fade = new DoubleAnimationUsingKeyFrames();
            var transform = new TranslateTransform();
            var duration = request.Duration.Milliseconds;

            var KeyframeTimes = new List<double>() { 0, duration / 6, duration }; // 3 key frames.
            List<double> KeyframeDistance = new List<double>();
            List<double> KeyframeOpacity = new List<double>();

            Storyboard.SetTarget(slide, transform);
            Storyboard.SetTargetProperty(fade, "Opacity");
            Storyboard.SetTarget(fade, request.Target);
            request.Target.RenderTransform = transform;

            switch (fadetype)
            {
                case FadeType.FadeIn:
                    KeyframeOpacity = new List<double>() { 0, 0.8, 1.0 };

                    switch (edge)
                    {
                        case FadeSide.Top:
                            distance = -request.Target.ActualHeight;
                            Storyboard.SetTargetProperty(slide, "Y");
                            KeyframeDistance = new List<double>() { distance, distance * 0.5, 0 };
                            break;
                        case FadeSide.Bottom:
                            distance = request.Target.ActualHeight;
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
                            distance = -request.Target.ActualHeight;
                            Storyboard.SetTargetProperty(slide, "Y");
                            KeyframeDistance = new List<double>() { 0, distance * 0.2, distance };
                            break;
                        case FadeSide.Bottom:
                            distance = request.Target.ActualHeight;
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
            if (request.Completed != null) { sb.Completed += request.Completed; }
            return sb;
        }

        public static Storyboard CreateSmoothRotateAnimation(AnimationRequest request, double angle)
        {
            var transform = request.Target.RenderTransform as CompositeTransform;

            var duration = new Duration(TimeSpan.FromMilliseconds(200));
            if (request.Duration != null && request.Duration.Milliseconds != 0)
            {
                duration = request.Duration;
            }

            var sb = new Storyboard() { Duration = duration };
            var animation = new DoubleAnimationUsingKeyFrames();

            sb.Children.Add(animation);

            Storyboard.SetTarget(animation, transform);
            Storyboard.SetTargetProperty(animation, "Rotation");

            if (request.Completed != null)
            {
                sb.Completed += request.Completed;
            }

            animation.KeyFrames.Add(new EasingDoubleKeyFrame() { KeyTime = KeyTime.FromTimeSpan(TimeSpan.FromMilliseconds(0)), Value = transform.Rotation });
            animation.KeyFrames.Add(new EasingDoubleKeyFrame() { KeyTime = KeyTime.FromTimeSpan(TimeSpan.FromMilliseconds(duration.TimeSpan.Milliseconds / 3)), Value = transform.Rotation + angle * 0.7 });
            animation.KeyFrames.Add(new EasingDoubleKeyFrame() { KeyTime = KeyTime.FromTimeSpan(TimeSpan.FromMilliseconds(duration.TimeSpan.Milliseconds)), Value = transform.Rotation + angle });
            return sb;
        }
    }

    public class AnimationRequest
    {
        public FrameworkElement Target { get; set; }
        public TimeSpan Duration { get; set; }
        public EventHandler<object> Completed { get; set; }
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
