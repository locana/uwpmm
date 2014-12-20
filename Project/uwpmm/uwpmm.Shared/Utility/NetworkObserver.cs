using Kazyx.DeviceDiscovery;
using Kazyx.Uwpmm.CameraControl;
using System;
using System.Collections.Generic;

namespace Kazyx.Uwpmm.Utility
{
    public class NetworkObserver
    {
        private static NetworkObserver sInstance = new NetworkObserver();

        public static NetworkObserver INSTANCE
        {
            get { return sInstance; }
        }

        private NetworkObserver()
        {
            discovery.SonyCameraDeviceDiscovered += discovery_SonyCameraDeviceDiscovered;
        }

        private List<TargetDevice> devices = new List<TargetDevice>();

        public List<TargetDevice> Devices
        {
            get { return new List<TargetDevice>(devices); }
        }

        public event EventHandler<DeviceEventArgs> Discovered;

        protected void OnDiscovered(TargetDevice device)
        {
            Discovered.Raise(this, new DeviceEventArgs { Device = device });
        }

        void discovery_SonyCameraDeviceDiscovered(object sender, SonyCameraDeviceEventArgs e)
        {
            var api = new DeviceApiHolder(e.SonyCameraDevice);
            var device = new TargetDevice(e.SonyCameraDevice.UDN, api);
            devices.Add(device);
            OnDiscovered(device);
        }

        private SsdpDiscovery discovery = new SsdpDiscovery();

        public void ForceOffline(TargetDevice device)
        {
            devices.Remove(device);
        }

        public void Search()
        {
            discovery.SearchSonyCameraDevices();
        }

        public void Clear()
        {
            devices.Clear();
        }
    }

    public class DeviceEventArgs : EventArgs
    {
        public TargetDevice Device { set; get; }
    }
}
