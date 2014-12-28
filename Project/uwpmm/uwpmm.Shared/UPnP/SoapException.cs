using System;

namespace Kazyx.Uwpmm.UPnP
{
    public class SoapException : Exception
    {
        public int StatusCode { private set; get; }

        public SoapException(int code)
            : base()
        {
            StatusCode = code;
        }
    }
}
