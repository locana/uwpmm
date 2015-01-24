using Kazyx.DeviceDiscovery;
using Kazyx.Uwpmm.CameraControl;
using Kazyx.Uwpmm.UPnP;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml.Linq;

namespace Kazyx.Uwpmm.Utility
{
    public class NetworkObserver
    {
        private static NetworkObserver sInstance = new NetworkObserver();

        public static NetworkObserver INSTANCE
        {
            get { return sInstance; }
        }

        private SsdpDiscovery discovery = new SsdpDiscovery();
        private SsdpDiscovery cdsDiscovery = new SsdpDiscovery();

        private NetworkObserver()
        {
            discovery.SonyCameraDeviceDiscovered += discovery_SonyCameraDeviceDiscovered;
            cdsDiscovery.DescriptionObtained += cdsDiscovery_DescriptionObtained;
            discovery.Finished += discovery_Finished;
            cdsDiscovery.Finished += cdsDiscovery_Finished;
        }

        void cdsDiscovery_Finished(object sender, EventArgs e)
        {
            OnDlnaDiscoveryFinished();
        }

        public event EventHandler CdsDiscoveryFinished;

        protected void OnDlnaDiscoveryFinished()
        {
            CdsDiscoveryFinished.Raise(this, null);
        }

        void discovery_Finished(object sender, EventArgs e)
        {
            OnCameraDiscoveryFinished();
        }

        public event EventHandler CameraDiscoveryFinished;

        protected void OnCameraDiscoveryFinished()
        {
            CameraDiscoveryFinished.Raise(this, null);
        }

        private Dictionary<string, TargetDevice> devices = new Dictionary<string, TargetDevice>();

        public List<TargetDevice> CameraDevices
        {
            get { return new List<TargetDevice>(devices.Values); }
        }

        private Dictionary<string, UpnpDevice> cdServices = new Dictionary<string, UpnpDevice>();

        public List<UpnpDevice> CdsProviders
        {
            get { return new List<UpnpDevice>(cdServices.Values); }
        }

        public event EventHandler<CameraDeviceEventArgs> CameraDiscovered;

        protected void OnDiscovered(TargetDevice device)
        {
            CameraDiscovered.Raise(this, new CameraDeviceEventArgs { CameraDevice = device });
        }

        public event EventHandler<CdServiceEventArgs> CdsDiscovered;

        protected void OnDiscovered(UpnpDevice device)
        {
            CdsDiscovered.Raise(this, new CdServiceEventArgs { CdService = device });
        }

        void discovery_SonyCameraDeviceDiscovered(object sender, SonyCameraDeviceEventArgs e)
        {
            var device = new TargetDevice(e.SonyCameraDevice, e.LocalAddress);
            lock (devices)
            {
                if (devices.ContainsKey(e.SonyCameraDevice.UDN))
                {
                    return;
                }
                devices.Add(device.Udn, device);
            }
            OnDiscovered(device);
        }

        void cdsDiscovery_DescriptionObtained(object sender, DeviceDescriptionEventArgs e)
        {
            try
            {
                var device = UpnpDescriptionParser.ParseDescription(XDocument.Parse(e.Description), e.Location);
                device.LocalAddress = e.LocalAddress;

                lock (cdServices)
                {
                    if (cdServices.ContainsKey(device.UDN))
                    {
                        return;
                    }

                    if (device.Services.Any(service => service.Key == URN.ContentDirectory))
                    {
                        DebugUtil.Log("CDS found. Notify discovered.");
                        cdServices.Add(device.UDN, device);
                        OnDiscovered(device);
                    }
                }
            }
            catch (Exception ex)
            {
                DebugUtil.Log("failed to parse upnp device description.");
                DebugUtil.Log(ex.StackTrace);
            }

        }

        public void ForceOffline(TargetDevice device)
        {
            devices.Remove(device.Udn);
        }

        public void ForceOffline(UpnpDevice device)
        {
            cdServices.Remove(device.UDN);
        }

        public void SearchCds()
        {
            cdsDiscovery.SearchUpnpDevices("urn:schemas-upnp-org:service:ContentDirectory:1");
        }

        public void SearchCamera()
        {
            discovery.SearchSonyCameraDevices();
        }

        public void Clear()
        {
            devices.Clear();
            cdServices.Clear();
        }
    }

    public class CdServiceEventArgs : EventArgs
    {
        public UpnpDevice CdService { set; get; }
    }

    public class CameraDeviceEventArgs : EventArgs
    {
        public TargetDevice CameraDevice { set; get; }
    }
}
