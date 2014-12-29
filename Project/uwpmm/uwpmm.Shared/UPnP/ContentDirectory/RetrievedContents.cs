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
            var res = body.Element(NS_U + action + "Response");
            var result = res.Element("Result");
            var root = result.Element(NS_DIDL + "DIDL-Lite");

            var numReturned = int.Parse(res.Element("NumberReturned").Value);
            var total = int.Parse(res.Element("TotalMatches").Value);
            var updateId = res.Element("UpdateID").Value;

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
                    // ChildCount = OptionalValueHelper.ParseInt(element.Attribute("childCount"), -1),
                    ChildCount = (int?)element.Attribute("childCount"),
                    WriteStatus = (string)element.Element(NS_DIDL + "writeStatus"),
                };
                containers.Add(container);
            }

            var items = new List<Item>();
            foreach (var element in root.Elements(NS_DIDL + "item"))
            {
                var item = new Item
                {
                    Id = element.Attribute("id").Value,
                    ParentId = element.Attribute("parentID").Value,
                    Title = element.Element(NS_DC + "title").Value,
                    Class = element.Element(NS_UPNP + "class").Value,
                    Restricted = BoolConversionHelper.Parse(element.Attribute("restricted")),
                    WriteStatus = (string)element.Element(NS_DIDL + "writeStatus"),
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
