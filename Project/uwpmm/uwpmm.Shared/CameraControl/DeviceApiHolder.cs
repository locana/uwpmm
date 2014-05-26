﻿using Kazyx.DeviceDiscovery;
using Kazyx.RemoteApi;
using Kazyx.Uwpmm.DataModel;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Kazyx.Uwpmm.CameraControl
{
    public class DeviceApiHolder
    {
        public string UDN { private set; get; }

        public string DeviceName { private set; get; }

        public string FriendlyName { private set; get; }

        private readonly CameraApiClient _Camera;
        public CameraApiClient Camera { get { return _Camera; } }

        private readonly SystemApiClient _System;
        public SystemApiClient System { get { return _System; } }

        public DeviceApiHolder(ScalarDeviceInfo info)
        {
            UDN = info.UDN;
            DeviceName = info.ModelName;
            FriendlyName = info.FriendlyName;

            if (info.Endpoints.ContainsKey("camera"))
            {
                _Camera = new CameraApiClient(info.Endpoints["camera"]);
            }
            if (info.Endpoints.ContainsKey("system"))
            {
                _System = new SystemApiClient(info.Endpoints["system"]);
            }
            capability.PropertyChanged += api_PropertyChanged;
        }

        void api_PropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            switch (e.PropertyName)
            {
                case "Version":
                    OnServerVersionDetected();
                    OnAvailableApisUpdated();
                    break;
                case "SupportedApis":
                    OnSupportedApisUpdated();
                    break;
                case "AvailableApis":
                    OnAvailableApisUpdated();
                    break;
            }
        }

        private readonly ApiCapabilityContainer capability = new ApiCapabilityContainer();

        public ApiCapabilityContainer Capability
        {
            get { return capability; }
        }

        internal async Task RetrieveApiList()
        {
            if (Camera != null)
            {
                foreach (var method in await Camera.GetMethodTypesAsync())
                {
                    Capability.AddSupported(method);
                }
            }
            if (System != null)
            {
                foreach (var method in await System.GetMethodTypesAsync())
                {
                    Capability.AddSupported(method);
                }
            }

            if (SupportedApisUpdated != null)
            {
                SupportedApisUpdated.Invoke(this, new SupportedApiEventArgs(Capability.SupportedApis));
            }
        }

        public delegate void SupportedApiEventHandler(object sender, SupportedApiEventArgs e);

        public SupportedApiEventHandler SupportedApisUpdated;

        protected void OnSupportedApisUpdated()
        {
            if (SupportedApisUpdated != null)
            {
                SupportedApisUpdated(this, new SupportedApiEventArgs(Capability.SupportedApis));
            }
        }

        public delegate void VersionEventHandler(object sender, VersionEventArgs e);

        public VersionEventHandler ServerVersionDetected;

        protected void OnServerVersionDetected()
        {
            if (ServerVersionDetected != null)
            {
                ServerVersionDetected(this, new VersionEventArgs(Capability.Version));
            }
        }

        public delegate void AvailableApiEventHandler(object sender, AvailableApiEventArgs e);

        public AvailableApiEventHandler AvailiableApisUpdated;

        protected void OnAvailableApisUpdated()
        {
            if (AvailiableApisUpdated != null)
            {
                AvailiableApisUpdated(this, new AvailableApiEventArgs(Capability.AvailableApis));
            }
        }
    }

    public class SupportedApiEventArgs : EventArgs
    {
        private readonly Dictionary<string, List<string>> apis;

        internal SupportedApiEventArgs(Dictionary<string, List<string>> apis)
        {
            this.apis = apis;
        }

        public Dictionary<string, List<string>> SupportedApis
        {
            get { return apis; }
        }
    }

    public class VersionEventArgs : EventArgs
    {
        private readonly ServerVersion version;

        internal VersionEventArgs(ServerVersion version)
        {
            this.version = version;
        }

        public ServerVersion Version
        {
            get { return version; }
        }
    }

    public class AvailableApiEventArgs : EventArgs
    {
        private readonly string[] apis;

        internal AvailableApiEventArgs(string[] apis)
        {
            this.apis = apis;
        }

        public string[] AvailableApis
        {
            get { return apis; }
        }
    }
}
