using System.Collections.Generic;
using System.Xml.Linq;

namespace Kazyx.Uwpmm.UPnP
{
    public class UpnpDescriptionParser
    {
        private const string NS_UPNP = "{urn:schemas-upnp-org:device-1-0}";

        public static UpnpDevice ParseDescription(XDocument xml)
        {
            var device = xml.Root.Element(NS_UPNP + "device");
            var services = device.Element(NS_UPNP + "serviceList");
            var udn = device.Element(NS_UPNP + "UDN").Value;
            var fn = device.Element(NS_UPNP + "friendlyName").Value;
            var mn = device.Element(NS_UPNP + "modelName").Value;

            var result = new List<UpnpService>();
            foreach (var service in services.Elements("service"))
            {
                result.Add(new UpnpService
                {
                    ServiceType = service.Element("serviceType").Value,
                    ServiceId = service.Element("serviceId").Value,
                    ScpdUrl = service.Element("SCPDURL").Value,
                    ControlUrl = service.Element("controlURL").Value,
                    EventSubUrl = service.Element("eventSubURL").Value,
                });
            }

            return new UpnpDevice
            {
                FriendlyName = fn,
                ModelName = mn,
                UDN = udn,
                Services = result
            };
        }
    }
}
