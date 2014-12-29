using System.Collections.Generic;

namespace Kazyx.Uwpmm.UPnP
{
    public class UpnpDevice
    {
        public string RootAddress { get; set; }
        public string FriendlyName { get; set; }
        public string ModelName { get; set; }
        public string UDN { get; set; }
        public Dictionary<string, UpnpService> Services { get; set; }
    }
}
