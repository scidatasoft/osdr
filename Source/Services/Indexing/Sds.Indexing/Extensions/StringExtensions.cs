using System;
using System.Collections.Generic;
using System.Text;

namespace Sds.Indexing.Extensions
{
    public static class StringExtensions
    {
        public static string ToPascalCase(this string s)
        {
            char[] a = s.ToCharArray();
            a[0] = char.ToUpper(a[0]);
            return new string(a);
        }

        public static string ToFirstLowerLetter(this string s)
        {
            char[] a = s.ToCharArray();
            a[0] = char.ToLower(a[0]);
            return new string(a);
        }
    }
}

