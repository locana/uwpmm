using System.Collections.Generic;
using System.Xml.Linq;

namespace Kazyx.Uwpmm.UPnP.ContentDirectory
{
    public class RetrievedContents : ContentDirectoryResponse
    {
        public int NumberReturned { get; private set; }
        public int TotalMatches { get; private set; }
        public string UpdateID { get; private set; }

        public Result Result { get; private set; }

        public static RetrievedContents Parse(XDocument xml, string action)
        {
            var body = GetBodyOrThrowError(xml);
            var response = body.Element(NS_U + action + "Response");
            var result = response.Element("Result");
            var root = result.Element(NS_DIDL + "DIDL-Lite");

            var numReturned = int.Parse(response.Element("NumberReturned").Value);
            var total = int.Parse(response.Element("TotalMatches").Value);
            var updateId = response.Element("UpdateID").Value;

            var containers = new List<Container>();
            foreach (var element in root.Elements(NS_DIDL + "container"))
            {
                var container = new Container
                {
                    Id = element.Attribute("id").Value,
                    ParentId = element.Attribute("parentID").Value,
                    Title = element.Element(NS_DC + "title").Value,
                    Class = element.Element(NS_UPNP + "class").Value,
                    Restricted = BoolConversionHelper.Parse(element.Attribute("restricted")),
                    ChildCount = (int?)element.Attribute("childCount"),
                    WriteStatus = (string)element.Element(NS_DIDL + "writeStatus"),
                };
                containers.Add(container);
            }

            var items = new List<Item>();
            foreach (var element in root.Elements(NS_DIDL + "item"))
            {
                var list = new List<Resource>();
                var resources = element.Elements(NS_DIDL + "res");
                foreach (var res in resources)
                {
                    var protocol = (string)res.Attribute("protocolInfo");
                    var resolution = (string)res.Attribute("resolution");
                    var size = (long?)res.Attribute("size");
                    list.Add(new Resource
                    {
                        ProtocolInfo = ProtocolInfo.Parse(res.Attribute("protocolInfo")),
                        Resolution = (string)res.Attribute("resolution"),
                        SizeInByte = (long?)res.Attribute("size"),
                        ResourceUrl = (string)res.Value,
                    });
                }

                var item = new Item
                {
                    Id = element.Attribute("id").Value,
                    ParentId = element.Attribute("parentID").Value,
                    Title = element.Element(NS_DC + "title").Value,
                    Class = element.Element(NS_UPNP + "class").Value,
                    Restricted = BoolConversionHelper.Parse(element.Attribute("restricted")),
                    WriteStatus = (string)element.Element(NS_DIDL + "writeStatus"),
                    Resources = list,
                    Date = (string)element.Element(NS_DC + "date"),
                    Genre = (string)element.Element(NS_UPNP + "upnp"),
                };
                items.Add(item);
            }

            return new RetrievedContents
            {
                Result = new Result
                {
                    Containers = containers,
                    Items = items,
                },
                NumberReturned = numReturned,
                TotalMatches = total,
                UpdateID = updateId,
            };
        }
    }
}
