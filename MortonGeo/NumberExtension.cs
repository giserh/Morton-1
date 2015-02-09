using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MortonGeo
{
    public static class NumberExtension
    {
        public static string GetCordinateString(this double input)
        {
            if (input == null)
                throw new ArgumentNullException("null input");
            return input.ToString().Replace(",", ".");
        }


        public static string GetDecimalAsStringAtFixedLength(this float input, int digit)
        {
            string tmp = "{0:0.";

            for (int i = 0; i < digit; i++)
            {
                tmp += "0";
            }
            tmp += "}";
            var result = string.Format(tmp, input).Replace(",", ".");
            return result;
        }

        public static string GetDecimalAsStringAtFixedLength(this double input, int digit)
        {
            string tmp = "{0:0.";

            for (int i = 0; i < digit; i++)
            {
                tmp += "0";
            }
            tmp += "}";
            var result = string.Format(tmp, input).Replace(",", ".");
            return result;
        }
    }
}
