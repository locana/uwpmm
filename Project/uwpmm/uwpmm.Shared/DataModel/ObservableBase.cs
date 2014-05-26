﻿using System;
using System.ComponentModel;
using System.Diagnostics;
using System.Runtime.InteropServices;
using Windows.UI.Core;

namespace Kazyx.Uwpmm.DataModel
{
    public abstract class ObservableBase : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;

        protected async void NotifyChangedOnUI(string name, CoreDispatcherPriority priority = CoreDispatcherPriority.Normal)
        {
            await CoreWindow.GetForCurrentThread().Dispatcher.RunAsync(priority, () =>
            {
                NotifyChanged(name);
            });
        }

        protected void NotifyChanged(string name)
        {
            if (PropertyChanged != null)
            {
                try
                {
                    PropertyChanged(this, new PropertyChangedEventArgs(name));
                }
                catch (COMException)
                {
                    Debug.WriteLine("Caught COMException: LiveviewData");
                }
            }
        }
    }
}
