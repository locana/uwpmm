using System;

namespace Kazyx.Uwpmm.UPnP
{
    public class BoolConversionHelper
    {
        public static bool From(string val)
        {
            if (val == null)
            {
                return false;
            }

            var result = false;
            if (bool.TryParse(val, out result))
            {
                return result;
            }

            var num = 0;
            int.TryParse(val, out num);
            return Convert.ToBoolean(num);
        }
    }
}
