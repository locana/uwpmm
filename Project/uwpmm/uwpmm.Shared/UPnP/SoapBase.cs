using System;
using System.Text;
using System.Xml.Linq;

namespace Kazyx.Uwpmm.UPnP
{
    public abstract class Request
    {
        public abstract string URN { get; }
        public abstract string ActionName { get; }

        public string SoapHeader { get { return "SOAPACTION: \"" + URN + "#" + ActionName + "\""; } }

        public string BuildMessage()
        {
            var builder = new StringBuilder();

            builder.Append("<?xml version=\"1.0\" encoding=\"utf-8\"?>")
                .Append("<s:Envelope s:encodingStyle=\"http://schemas.xmlsoap.org/soap/encoding/\" xmlns:s=\"http://schemas.xmlsoap.org/soap/envelope/\">")
                .Append("<s:Body>")
                .Append("<u:").Append(ActionName).Append("\"").Append(URN).Append("\">");

            AppendSpecificMessage(builder);

            builder.Append("</u:").Append(ActionName).Append(">")
                .Append("</s:Body>")
                .Append("</s:Envelope>");

            return builder.ToString();
        }

        protected abstract void AppendSpecificMessage(StringBuilder builder);

        public abstract Response ParseResponse(XDocument xml);
    }

    public abstract class Response
    {
        protected const string NS_S = "{http://schemas.xmlsoap.org/soap/envelope/}";
    }
}
