using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace SQL_Parser.Extensions
{
    public static class SubstringExtensions
    {
        /// <summary>
        /// Get string value between [first] a and [last] b.
        /// </summary>
        public static string Between(this string value, string a, string b, bool firstOccurrence = false) 
        {
            int posA = value.IndexOf(a);
            int posB = 0;
            if (firstOccurrence)
            {
                posB = value.IndexOf(b);
            }
            else
            {
                posB = value.LastIndexOf(b);
            }
            if (posA == -1)
            {
                return "";
            }
            if (posB == -1)
            {
                return "";
            }
            int adjustedPosA = posA + a.Length;
            if (adjustedPosA >= posB)
            {
                return "";
            }
            return value.Substring(adjustedPosA, posB - adjustedPosA);
        }

        /// <summary>
        /// Get string value after [first] a.
        /// </summary>
        public static string Before(this string value, string a)
        {
            int posA = value.IndexOf(a);
            if (posA == -1)
            {
                return "";
            }
            return value.Substring(0, posA);
        }

        /// <summary>
        /// Get string value after [last] a.
        /// </summary>
        public static string After(this string value, string a)
        {
            int posA = value.LastIndexOf(a);
            if (posA == -1)
            {
                return "";
            }
            int adjustedPosA = posA + a.Length;
            if (adjustedPosA >= value.Length)
            {
                return "";
            }
            return value.Substring(adjustedPosA);
        }
    }

}
