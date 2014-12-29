
using System.Collections.Generic;
namespace Kazyx.Uwpmm.UPnP.ContentDirectory
{
    public class Result
    {
        public IList<Container> Containers { get; set; }
        public IList<Item> Items { get; set; }
    }

    public abstract class Content
    {
        public string Id { get; set; }
        public string ParentId { get; set; }
        public bool Restricted { get; set; }

        public string Title { get; set; }
        public string WriteStatus { get; set; }

        public string Class { get; set; }
    }

    public class Item : Content
    {
        IList<Resource> Resources { get; set; }
    }

    public class Resource
    {
        public string MimeType { get; set; }
        public string ResourceUrl { get; set; }
    }

    public class Container : Content
    {
        public int? ChildCount { get; set; }
        public bool Searchable { get; set; }
    }

    public enum BrowseFlag
    {
        BrowseMetadata,
        BrowseDirectChildren,
    }
}
