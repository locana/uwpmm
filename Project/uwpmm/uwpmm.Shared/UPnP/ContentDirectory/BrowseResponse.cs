using System.Collections.Generic;
using System.Xml.Linq;

namespace Kazyx.Uwpmm.UPnP.ContentDirectory
{
    public class BrowseResponse : ContentDirectoryResponse
    {
        public int NumberReturned { get; private set; }
        public int TotalMatches { get; private set; }
        public string UpdateID { get; private set; }

        public List<Container> Containers { get; private set; }
        public List<Item> Items { get; private set; }

        public static BrowseResponse Parse(XDocument xml)
        {
            var body = GetBodyOrThrowError(xml);
            var res = body.Element(NS_U + "BrowseResponse");
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
                    Restricted = BoolConversionHelper.From(element.Attribute("restricted").Value),
                    Title = element.Element(NS_DC + "title").Value,
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
                    Restricted = BoolConversionHelper.From(element.Attribute("restricted").Value),
                    Title = element.Element(NS_DC + "title").Value,
                };
                items.Add(item);
            }

            return new BrowseResponse
            {
                Containers = containers,
                Items = items,
                NumberReturned = numReturned,
                TotalMatches = total,
                UpdateID = updateId,
            };
        }
    }
}
