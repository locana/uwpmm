
namespace Kazyx.Uwpmm.UPnP.ContentDirectory
{
    public abstract class Content
    {
        public string Id { get; set; }
        public string ParentId { get; set; }
        public bool Restricted { get; set; }

        public string Title { get; set; }
    }

    public class Item : Content
    {
        public string MimeType { get; set; }
        public string ResourceUrl { get; set; }
    }

    public class Container : Content
    {
        public int ChildCount { get; set; }
        public bool Searchable { get; set; }
        public string WriteStatus { get; set; }
    }

    public enum BrowseFlag
    {
        BrowseMetadata,
        BrowseDirectChildren,
    }
}
