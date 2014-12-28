
namespace Kazyx.Uwpmm.UPnP.ContentDirectory
{
    public abstract class ContentDirectoryRequest : Request
    {
        public override string URN { get { return "urn:schemas-upnp-org:service:ContentDirectory:1"; } }
    }

    public abstract class ContentDirectoryResponse : Response
    {
        protected const string NS_U = "{urn:schemas-upnp-org:service:ContentDirectory:1}";
        protected const string NS_DC = "{http://purl.org/dc/elements/1.1/}";
        protected const string NS_UPNP = "{urn:schemas-upnp-org:metadata-1-0/upnp/}";
    }
}
