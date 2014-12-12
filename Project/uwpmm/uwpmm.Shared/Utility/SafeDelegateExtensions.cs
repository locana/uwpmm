using System;
using System.ComponentModel;

namespace Kazyx.Uwpmm.Utility
{
    public static class EventHandlerExtensions
    {
        public static void Raise<T>(this EventHandler<T> @event, object sender, T e) where T : EventArgs
        {
            if (@event != null)
            {
                @event(sender, e);
            }
        }

        public static void Raise(this EventHandler @event, object sender, EventArgs e)
        {
            if (@event != null)
            {
                @event(sender, e);
            }
        }

        public static void Raise(this PropertyChangedEventHandler @event, object sender, PropertyChangedEventArgs e)
        {
            if (@event != null)
            {
                @event(sender, e);
            }
        }
    }

    public static class ActionExtensions
    {
        public static void Raise(this Action action)
        {
            if (action != null)
            {
                action();
            }
        }

        public static void Raise<T>(this Action<T> action, T p)
        {
            if (action != null)
            {
                action(p);
            }
        }

        public static void Raise<T1, T2>(this Action<T1, T2> action, T1 p1, T2 p2)
        {
            if (action != null)
            {
                action(p1, p2);
            }
        }
    }
}
