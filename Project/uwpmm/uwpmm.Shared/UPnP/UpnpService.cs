using Kazyx.Uwpmm.Utility;
using System;
using System.Threading.Tasks;
using System.Xml.Linq;
using Windows.Web.Http;

namespace Kazyx.Uwpmm.UPnP
{
    public class UpnpService
    {
        public string ServiceType { get; set; }
        public string ServiceId { get; set; }
        public string ScpdUrl { get; set; }
        public string ControlUrl { get; set; }
        public string EventSubUrl { get; set; }

        private HttpClient HttpClient = new HttpClient();

        public async Task<Response> Control(Request request)
        {
            var content = new HttpStringContent(request.BuildMessage());
            content.Headers["Content-Type"] = "text/xml";
            var uri = new Uri(ControlUrl);
            var response = await HttpClient.PostAsync(uri, content);

            if (response.IsSuccessStatusCode)
            {
                var res = await response.Content.ReadAsStringAsync();
                return request.ParseResponse(XDocument.Parse(res));
            }
            else
            {
                DebugUtil.Log("Http Status Error in SOAP request: " + response.StatusCode);
                throw new SoapException((int)response.StatusCode);
            }
        }
    }
}
